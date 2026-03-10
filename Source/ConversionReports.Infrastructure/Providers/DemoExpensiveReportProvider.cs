using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Models;
using ConversionReports.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ConversionReports.Infrastructure.Providers;

public class DemoExpensiveReportProvider : IExpensiveReportProvider
{
	private readonly ILogger<DemoExpensiveReportProvider> _logger;

	public DemoExpensiveReportProvider(ILogger<DemoExpensiveReportProvider> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<IReadOnlyCollection<ProviderBatchItemResult>> BuildBatchAsync(
		string userId,
		IReadOnlyCollection<ReportDefinition> definitions,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(userId))
		{
			throw new ArgumentException("User id must be provided", nameof(userId));
		}

		ArgumentNullException.ThrowIfNull(definitions);

		_logger.LogInformation(
			"Building expensive report batch for user {UserId} with {ItemCount} items",
			userId,
			definitions.Count);

		await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

		var results = definitions
			.Distinct()
			.Select(definition =>
			{
				var hash = HashCode.Combine(
					definition.PeriodStartUtc,
					definition.PeriodEndUtc,
					definition.ProductId,
					definition.DesignId);

				var views = Math.Abs(hash % 10_000) + 100;
				var payments = Math.Abs((hash / 10) % 200) + 1;
				var ratio = (decimal)payments / views;

				return new ProviderBatchItemResult(definition, ReportResult.Create(ratio, payments));
			})
			.ToArray();

		return results;
	}
}
