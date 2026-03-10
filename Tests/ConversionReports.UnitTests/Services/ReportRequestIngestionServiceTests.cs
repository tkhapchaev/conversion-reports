using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Options;
using ConversionReports.Application.Services;
using ConversionReports.Contracts.Messages;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConversionReports.UnitTests.Services;

public class ReportRequestIngestionServiceTests
{
	[Fact]
	public async Task HandleAsync_ShouldAggregateSameUserRequestsIntoSameBatchAndDeduplicateItem()
	{
		await using var database = new SqliteTestDbContextFactory();
		await database.InitializeAsync();

		var clock = new FakeClock(new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero));
		var firstMessage = CreateMessage("msg-1", Guid.NewGuid());
		var secondMessage = CreateMessage("msg-2", Guid.NewGuid());

		await using (var dbContext = database.CreateDbContext())
		{
			var factory = new ReportBatchFactory(clock, Options.Create(new BatchingOptions()));

			var service = new ReportRequestIngestionService(
				dbContext,
				factory,
				clock,
				NullLogger<ReportRequestIngestionService>.Instance);

			await service.HandleAsync(firstMessage, CancellationToken.None);
		}

		await using (var dbContext = database.CreateDbContext())
		{
			var factory = new ReportBatchFactory(clock, Options.Create(new BatchingOptions()));

			var service = new ReportRequestIngestionService(
				dbContext,
				factory,
				clock,
				NullLogger<ReportRequestIngestionService>.Instance);

			await service.HandleAsync(secondMessage, CancellationToken.None);
		}

		await using (var dbContext = database.CreateDbContext())
		{
			dbContext.ReportBatches.Should().HaveCount(1);
			dbContext.ReportBatchItems.Should().HaveCount(1);
			dbContext.ReportRequests.Should().HaveCount(2);
		}
	}

	[Fact]
	public async Task HandleAsync_ShouldIgnoreDuplicateMessageId()
	{
		await using var database = new SqliteTestDbContextFactory();
		await database.InitializeAsync();

		var clock = new FakeClock(new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero));
		var message = CreateMessage("msg-1", Guid.NewGuid());

		await using (var dbContext = database.CreateDbContext())
		{
			var factory = new ReportBatchFactory(clock, Options.Create(new BatchingOptions()));

			var service = new ReportRequestIngestionService(
				dbContext,
				factory,
				clock,
				NullLogger<ReportRequestIngestionService>.Instance);

			await service.HandleAsync(message, CancellationToken.None);
		}

		await using (var dbContext = database.CreateDbContext())
		{
			var factory = new ReportBatchFactory(clock, Options.Create(new BatchingOptions()));

			var service = new ReportRequestIngestionService(
				dbContext,
				factory,
				clock,
				NullLogger<ReportRequestIngestionService>.Instance);

			await service.HandleAsync(message, CancellationToken.None);
		}

		await using (var dbContext = database.CreateDbContext())
		{
			dbContext.ReportRequests.Should().HaveCount(1);
			dbContext.ProcessedMessages.Should().HaveCount(1);
		}
	}

	private static ReportRequestedIntegrationEvent CreateMessage(string messageId, Guid requestId)
	{
		return new ReportRequestedIntegrationEvent(
			requestId,
			messageId,
			"user-1",
			new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero),
			100,
			200,
			new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero));
	}

	private class FakeClock : IClock
	{
		public FakeClock(DateTimeOffset utcNow)
		{
			UtcNow = utcNow;
		}

		public DateTimeOffset UtcNow { get; }
	}
}
