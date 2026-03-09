using ConversionReports.Application.Models;
using ConversionReports.Domain.ValueObjects;

namespace ConversionReports.Application.Interfaces;

public interface IExpensiveReportProvider
{
	Task<IReadOnlyCollection<ProviderBatchItemResult>> BuildBatchAsync(
		string userId,
		IReadOnlyCollection<ReportDefinition> definitions,
		CancellationToken cancellationToken);
}
