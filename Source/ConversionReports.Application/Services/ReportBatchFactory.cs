using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Options;
using ConversionReports.Domain.Entities;
using Microsoft.Extensions.Options;

namespace ConversionReports.Application.Services;

public class ReportBatchFactory
{
	private readonly IClock _clock;
	private readonly BatchingOptions _options;

	public ReportBatchFactory(IClock clock, IOptions<BatchingOptions> options)
	{
		_clock = clock ?? throw new ArgumentNullException(nameof(clock));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	}

	public ReportBatch Create(string userId)
	{
		if (string.IsNullOrWhiteSpace(userId))
		{
			throw new ArgumentException("User id must be provided", nameof(userId));
		}

		var now = _clock.UtcNow;
		var businessDate = DateOnly.FromDateTime(now.UtcDateTime);

		var nextDayStart = new DateTimeOffset(
			businessDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
			TimeSpan.Zero).AddDays(1);

		var dueAtUtc = nextDayStart <= now.Add(_options.MaxWaitTime)
			? nextDayStart
			: now.Add(_options.MaxWaitTime);

		return new ReportBatch(userId, businessDate, dueAtUtc, now);
	}
}
