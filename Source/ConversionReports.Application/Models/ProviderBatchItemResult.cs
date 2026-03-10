using ConversionReports.Domain.ValueObjects;

namespace ConversionReports.Application.Models;

public record ProviderBatchItemResult
{
	public ProviderBatchItemResult(ReportDefinition definition, ReportResult result)
	{
		Definition = definition ?? throw new ArgumentNullException(nameof(definition));
		Result = result ?? throw new ArgumentNullException(nameof(result));
	}

	public ReportDefinition Definition { get; init; }
	public ReportResult Result { get; init; }
}
