using ConversionReports.Application.Extensions;
using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Models;
using ConversionReports.Domain.Entities;
using ConversionReports.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConversionReports.Application.Services;

public class ReportBatchProcessor
{
	private readonly IAppDbContext _dbContext;
	private readonly IExpensiveReportProvider _provider;
	private readonly IClock _clock;
	private readonly IReportResultCache _cache;
	private readonly ILogger<ReportBatchProcessor> _logger;

	public ReportBatchProcessor(
		IAppDbContext dbContext,
		IExpensiveReportProvider provider,
		IClock clock,
		IReportResultCache cache,
		ILogger<ReportBatchProcessor> logger)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_clock = clock ?? throw new ArgumentNullException(nameof(clock));
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<int> ProcessDueBatchesAsync(CancellationToken cancellationToken)
	{
		var now = _clock.UtcNow;

		var dueBatchIds = await _dbContext.ReportBatches
			.AsEnumerable()
			.Where(x => x.State == BatchState.Open && x.DueAtUtc <= now)
			.OrderBy(x => x.CreatedAtUtc)
			.Select(x => x.Id)
			.ToListAsyncSafe(cancellationToken);

		var processedCount = 0;

		foreach (var batchId in dueBatchIds)
		{
			processedCount += await ProcessBatchAsync(batchId, cancellationToken);
		}

		return processedCount;
	}

	public async Task<int> ProcessBatchAsync(Guid batchId, CancellationToken cancellationToken)
	{
		if (batchId == Guid.Empty)
		{
			throw new ArgumentException("Batch id must be provided", nameof(batchId));
		}

		var batch = await _dbContext.ReportBatches
			.Include(x => x.Items)
			.Include(x => x.Requests)
			.SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);

		if (batch is null)
		{
			return 0;
		}

		if (batch.State != BatchState.Open)
		{
			return 0;
		}

		if (batch.Items.Count == 0)
		{
			batch.Fail("Batch does not contain report items");
			await _dbContext.SaveChangesAsync(cancellationToken);

			return 0;
		}

		try
		{
			batch.MarkProcessing();
			await _dbContext.SaveChangesAsync(cancellationToken);

			var providerResults = await _provider.BuildBatchAsync(
				batch.UserId,
				batch.Items.Select(item => item.ToDefinition()).ToArray(),
				cancellationToken);

			var resultsByDefinition = providerResults.ToDictionary(x => x.Definition);
			var completedAtUtc = _clock.UtcNow;

			foreach (var item in batch.Items)
			{
				if (!resultsByDefinition.TryGetValue(item.ToDefinition(), out var itemResult))
				{
					throw new InvalidOperationException($"Provider did not return a result for ProductId={item.ProductId} and DesignId={item.DesignId}");
				}

				item.Complete(itemResult.Result, completedAtUtc);
			}

			batch.Complete(completedAtUtc);
			await _dbContext.SaveChangesAsync(cancellationToken);

			UpdateCache(batch);

			return 1;
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Failed to process batch {BatchId}", batchId);
			batch.Fail(exception.Message);

			await _dbContext.SaveChangesAsync(cancellationToken);
			UpdateCache(batch);

			return 0;
		}
	}

	private void UpdateCache(ReportBatch batch)
	{
		foreach (var request in batch.Requests)
		{
			var item = batch.Items.Single(x => x.Id == request.BatchItemId);

			var state = item.State switch
			{
				BatchItemState.Pending => ReportRequestState.Pending,
				BatchItemState.Processing => ReportRequestState.Processing,
				BatchItemState.Completed => ReportRequestState.Completed,
				BatchItemState.Failed => ReportRequestState.Failed,
				_ => throw new ArgumentOutOfRangeException(nameof(item.State), "Unknown batch item state")
			};

			var status = new ReportRequestStatusDto(
				request.Id,
				request.UserId,
				state,
				item.ConversionRatio,
				item.PaymentsCount,
				item.Error,
				request.CreatedAtUtc,
				item.CompletedAtUtc);

			_cache.Set(status);
		}
	}
}
