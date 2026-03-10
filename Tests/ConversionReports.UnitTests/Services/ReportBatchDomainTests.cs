using ConversionReports.Domain.Entities;
using ConversionReports.Domain.Enums;
using ConversionReports.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ConversionReports.UnitTests.Services;

public class ReportBatchDomainTests
{
	[Fact]
	public void AddRequest_ShouldReuseSingleBatchItem_ForIdenticalDefinitions()
	{
		var batch = new ReportBatch(
			"user-1",
			new DateOnly(2026, 3, 8),
			new DateTimeOffset(2026, 3, 9, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero));

		var definition = new ReportDefinition(
			new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero),
			100,
			200);

		batch.AddRequest(Guid.NewGuid(), "msg-1", "user-1", definition, DateTimeOffset.UtcNow);
		batch.AddRequest(Guid.NewGuid(), "msg-2", "user-1", definition, DateTimeOffset.UtcNow);

		batch.Items.Should().HaveCount(1);
		batch.Requests.Should().HaveCount(2);
		batch.Requests.Select(x => x.BatchItemId).Distinct().Should().ContainSingle();
	}

	[Fact]
	public void MarkProcessing_ShouldMoveBatchAndItemsToProcessing()
	{
		var batch = CreateBatch();

		batch.MarkProcessing();

		batch.State.Should().Be(BatchState.Processing);
		batch.Items.Should().OnlyContain(x => x.State == BatchItemState.Processing);
	}

	[Fact]
	public void Fail_ShouldMarkIncompleteItemsAsFailed_AndKeepCompletedItemsCompleted()
	{
		var batch = CreateBatch();
		var itemToComplete = batch.Items.First();
		var itemToFail = batch.Items.Last();

		itemToComplete.Complete(ReportResult.Create(0.5m, 10), DateTimeOffset.UtcNow);

		batch.Fail("Provider failed");

		batch.State.Should().Be(BatchState.Failed);
		itemToComplete.State.Should().Be(BatchItemState.Completed);
		itemToFail.State.Should().Be(BatchItemState.Failed);
		itemToFail.Error.Should().Be("Provider failed");
	}

	private static ReportBatch CreateBatch()
	{
		var batch = new ReportBatch(
			"user-1",
			new DateOnly(2026, 3, 8),
			new DateTimeOffset(2026, 3, 9, 0, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero));

		batch.AddRequest(
			Guid.NewGuid(),
			"msg-1",
			"user-1",
			new ReportDefinition(
				new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
				new DateTimeOffset(2026, 3, 8, 0, 0, 0, TimeSpan.Zero),
				100,
				200),
			DateTimeOffset.UtcNow);

		batch.AddRequest(
			Guid.NewGuid(),
			"msg-2",
			"user-1",
			new ReportDefinition(
				new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
				new DateTimeOffset(2026, 2, 8, 0, 0, 0, TimeSpan.Zero),
				101,
				201),
			DateTimeOffset.UtcNow);

		return batch;
	}
}
