using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Models;
using ConversionReports.Application.Services;
using ConversionReports.Domain.Entities;
using ConversionReports.Domain.Enums;
using ConversionReports.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConversionReports.UnitTests.Services;

public class ReportBatchProcessorTests
{
	[Fact]
	public async Task ProcessDueBatchesAsync_ShouldCompleteBatchAndPopulateCache()
	{
		await using var database = new SqliteTestDbContextFactory();
		await database.InitializeAsync();

		var now = new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero);
		var completedAtUtc = now.AddMinutes(1);

		var clock = new FakeClock(now, completedAtUtc);
		var cache = new FakeReportResultCache();
		var provider = new FakeExpensiveReportProvider();

		var requestId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
		var requestId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

		await using (var dbContext = database.CreateDbContext())
		{
			var batch = CreateBatch(now.AddMinutes(-5), now.AddMinutes(-1), requestId1, requestId2);
			dbContext.ReportBatches.Add(batch);

			await dbContext.SaveChangesAsync();
		}

		await using (var dbContext = database.CreateDbContext())
		{
			var processor = new ReportBatchProcessor(
				dbContext,
				provider,
				clock,
				cache,
				NullLogger<ReportBatchProcessor>.Instance);

			var processedCount = await processor.ProcessDueBatchesAsync(CancellationToken.None);

			processedCount.Should().Be(1);
		}

		await using (var dbContext = database.CreateDbContext())
		{
			var batch = dbContext.ReportBatches.Single();
			var item = dbContext.ReportBatchItems.Single();

			batch.State.Should().Be(BatchState.Completed);
			batch.ProcessedAtUtc.Should().Be(completedAtUtc);
			item.State.Should().Be(BatchItemState.Completed);
			item.ConversionRatio.Should().Be(0.25m);
			item.PaymentsCount.Should().Be(42);
			item.CompletedAtUtc.Should().Be(completedAtUtc);
		}

		provider.CallCount.Should().Be(1);
		provider.LastDefinitions.Should().HaveCount(1);
		cache.Stored.Should().HaveCount(2);
		cache.Stored[requestId1].State.Should().Be(ReportRequestState.Completed);
		cache.Stored[requestId1].ConversionRatio.Should().Be(0.25m);
		cache.Stored[requestId1].PaymentsCount.Should().Be(42);
		cache.Stored[requestId2].State.Should().Be(ReportRequestState.Completed);
	}

	[Fact]
	public async Task ProcessDueBatchesAsync_ShouldFailBatch_WhenProviderThrows()
	{
		await using var database = new SqliteTestDbContextFactory();
		await database.InitializeAsync();

		var now = new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero);
		var clock = new FakeClock(now, now.AddMinutes(1));
		var cache = new FakeReportResultCache();

		var provider = new FakeExpensiveReportProvider
		{
			ExceptionToThrow = new InvalidOperationException("Provider failed")
		};

		var requestId = Guid.Parse("33333333-3333-3333-3333-333333333333");

		await using (var dbContext = database.CreateDbContext())
		{
			var batch = CreateBatch(now.AddMinutes(-5), now.AddMinutes(-1), requestId);
			dbContext.ReportBatches.Add(batch);

			await dbContext.SaveChangesAsync();
		}

		await using (var dbContext = database.CreateDbContext())
		{
			var processor = new ReportBatchProcessor(
				dbContext,
				provider,
				clock,
				cache,
				NullLogger<ReportBatchProcessor>.Instance);

			var processedCount = await processor.ProcessDueBatchesAsync(CancellationToken.None);

			processedCount.Should().Be(0);
		}

		await using (var dbContext = database.CreateDbContext())
		{
			var batch = dbContext.ReportBatches.Single();
			var item = dbContext.ReportBatchItems.Single();

			batch.State.Should().Be(BatchState.Failed);
			item.State.Should().Be(BatchItemState.Failed);
			item.Error.Should().Be("Provider failed");
		}

		cache.Stored.Should().ContainKey(requestId);
		cache.Stored[requestId].State.Should().Be(ReportRequestState.Failed);
		cache.Stored[requestId].Error.Should().Be("Provider failed");
	}

	private static ReportBatch CreateBatch(
		DateTimeOffset createdAtUtc,
		DateTimeOffset dueAtUtc,
		params Guid[] requestIds)
	{
		var batch = new ReportBatch(
			"user-1",
			DateOnly.FromDateTime(createdAtUtc.UtcDateTime),
			dueAtUtc,
			createdAtUtc);

		var definition = new ReportDefinition(
			new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero),
			100,
			200);

		for (var index = 0; index < requestIds.Length; index++)
		{
			batch.AddRequest(
				requestIds[index],
				$"msg-{index + 1}",
				"user-1",
				definition,
				createdAtUtc.AddSeconds(index));
		}

		return batch;
	}

	private class FakeClock : IClock
	{
		private readonly Queue<DateTimeOffset> _values;

		public FakeClock(params DateTimeOffset[] values)
		{
			_values = new Queue<DateTimeOffset>(values);
		}

		public DateTimeOffset UtcNow
		{
			get
			{
				if (_values.Count == 0)
				{
					throw new InvalidOperationException("Clock has no more values");
				}

				return _values.Dequeue();
			}
		}
	}

	private class FakeExpensiveReportProvider : IExpensiveReportProvider
	{
		public int CallCount { get; private set; }
		public IReadOnlyCollection<ReportDefinition> LastDefinitions { get; private set; } = Array.Empty<ReportDefinition>();
		public Exception? ExceptionToThrow { get; init; }

		public Task<IReadOnlyCollection<ProviderBatchItemResult>> BuildBatchAsync(
			string userId,
			IReadOnlyCollection<ReportDefinition> definitions,
			CancellationToken cancellationToken)
		{
			CallCount++;
			LastDefinitions = definitions;

			if (ExceptionToThrow is not null)
			{
				throw ExceptionToThrow;
			}

			var results = definitions
				.Select(definition => new ProviderBatchItemResult(definition, ReportResult.Create(0.25m, 42)))
				.ToArray();

			return Task.FromResult<IReadOnlyCollection<ProviderBatchItemResult>>(results);
		}
	}

	private class FakeReportResultCache : IReportResultCache
	{
		public Dictionary<Guid, ReportRequestStatusDto> Stored { get; } = [];

		public bool TryGet(Guid requestId, out ReportRequestStatusDto status)
		{
			return Stored.TryGetValue(requestId, out status!);
		}

		public void Set(ReportRequestStatusDto status)
		{
			Stored[status.RequestId] = status;
		}

		public void Remove(Guid requestId)
		{
			Stored.Remove(requestId);
		}
	}
}
