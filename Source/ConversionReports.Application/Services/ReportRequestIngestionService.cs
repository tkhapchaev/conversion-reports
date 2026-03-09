using ConversionReports.Application.Exceptions;
using ConversionReports.Application.Interfaces;
using ConversionReports.Contracts.Messages;
using ConversionReports.Domain.Entities;
using ConversionReports.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConversionReports.Application.Services;

public class ReportRequestIngestionService
{
	private readonly IAppDbContext _dbContext;
	private readonly ReportBatchFactory _batchFactory;
	private readonly IClock _clock;
	private readonly ILogger<ReportRequestIngestionService> _logger;

	public ReportRequestIngestionService(
		IAppDbContext dbContext,
		ReportBatchFactory batchFactory,
		IClock clock,
		ILogger<ReportRequestIngestionService> logger)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		_batchFactory = batchFactory ?? throw new ArgumentNullException(nameof(batchFactory));
		_clock = clock ?? throw new ArgumentNullException(nameof(clock));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task HandleAsync(ReportRequestedIntegrationEvent message, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(message);

		if (message.RequestId == Guid.Empty)
		{
			throw new ArgumentException("Request id must be provided", nameof(message));
		}

		if (string.IsNullOrWhiteSpace(message.MessageId))
		{
			throw new ArgumentException("Message id must be provided", nameof(message));
		}

		if (string.IsNullOrWhiteSpace(message.UserId))
		{
			throw new ArgumentException("User id must be provided", nameof(message));
		}

		if (await _dbContext.ProcessedMessages.AnyAsync(x => x.MessageId == message.MessageId, cancellationToken))
		{
			_logger.LogInformation("Message {MessageId} has already been processed", message.MessageId);

			return;
		}

		var existingRequest = await _dbContext.ReportRequests
			.Include(x => x.BatchItem)
			.FirstOrDefaultAsync(x => x.Id == message.RequestId, cancellationToken);

		if (existingRequest is not null)
		{
			if (IsEquivalent(existingRequest, message))
			{
				_dbContext.ProcessedMessages.Add(new ProcessedMessage(message.MessageId, _clock.UtcNow));
				await _dbContext.SaveChangesAsync(cancellationToken);

				_logger.LogInformation(
					"Request {RequestId} already exists and matches incoming message {MessageId}",
					message.RequestId,
					message.MessageId);

				return;
			}

			throw CreateConflictException(existingRequest, message);
		}

		var businessDate = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

		var batch = await _dbContext.ReportBatches
			.Include(x => x.Items)
			.Include(x => x.Requests)
			.FirstOrDefaultAsync(
				x => x.UserId == message.UserId &&
					 x.BusinessDate == businessDate &&
					 x.State == Domain.Enums.BatchState.Open,
				cancellationToken);

		if (batch is null)
		{
			batch = _batchFactory.Create(message.UserId);
			_dbContext.ReportBatches.Add(batch);
		}

		var definition = new ReportDefinition(
			message.PeriodStartUtc,
			message.PeriodEndUtc,
			message.ProductId,
			message.DesignId);

		batch.AddRequest(
			message.RequestId,
			message.MessageId,
			message.UserId,
			definition,
			_clock.UtcNow);

		_dbContext.ProcessedMessages.Add(new ProcessedMessage(message.MessageId, _clock.UtcNow));

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private static bool IsEquivalent(ReportRequest existingRequest, ReportRequestedIntegrationEvent message)
	{
		if (existingRequest.BatchItem is null)
		{
			throw new InvalidOperationException("Batch item must be loaded for conflict detection");
		}

		return existingRequest.UserId == message.UserId &&
			   existingRequest.BatchItem.PeriodStartUtc == message.PeriodStartUtc &&
			   existingRequest.BatchItem.PeriodEndUtc == message.PeriodEndUtc &&
			   existingRequest.BatchItem.ProductId == message.ProductId &&
			   existingRequest.BatchItem.DesignId == message.DesignId;
	}

	private static RequestConflictException CreateConflictException(ReportRequest existingRequest, ReportRequestedIntegrationEvent message)
	{
		if (existingRequest.BatchItem is null)
		{
			throw new InvalidOperationException("Batch item must be loaded for conflict detection");
		}

		return new RequestConflictException(
			message.RequestId,
			existingRequest.UserId,
			message.UserId,
			existingRequest.BatchItem.PeriodStartUtc,
			existingRequest.BatchItem.PeriodEndUtc,
			existingRequest.BatchItem.ProductId,
			existingRequest.BatchItem.DesignId,
			message.PeriodStartUtc,
			message.PeriodEndUtc,
			message.ProductId,
			message.DesignId);
	}
}
