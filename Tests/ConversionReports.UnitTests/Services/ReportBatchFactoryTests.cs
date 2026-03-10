using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Options;
using ConversionReports.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConversionReports.UnitTests.Services;

public class ReportBatchFactoryTests
{
	[Fact]
	public void Create_ShouldUseMaxWaitTime_WhenItIsEarlierThanNextDayStart()
	{
		var now = new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero);
		var clock = new FakeClock(now);

		var factory = new ReportBatchFactory(
			clock,
			Options.Create(new BatchingOptions { MaxWaitTime = TimeSpan.FromMinutes(15) }));

		var batch = factory.Create("user-1");

		batch.UserId.Should().Be("user-1");
		batch.BusinessDate.Should().Be(new DateOnly(2026, 3, 8));
		batch.CreatedAtUtc.Should().Be(now);
		batch.DueAtUtc.Should().Be(now.AddMinutes(15));
	}

	[Fact]
	public void Create_ShouldUseNextDayStart_WhenItIsEarlierThanMaxWaitTime()
	{
		var now = new DateTimeOffset(2026, 3, 8, 23, 55, 0, TimeSpan.Zero);
		var clock = new FakeClock(now);

		var factory = new ReportBatchFactory(
			clock,
			Options.Create(new BatchingOptions { MaxWaitTime = TimeSpan.FromHours(2) }));

		var batch = factory.Create("user-1");

		batch.DueAtUtc.Should().Be(new DateTimeOffset(2026, 3, 9, 0, 0, 0, TimeSpan.Zero));
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
