namespace ConversionReports.Domain.ValueObjects;

public record ReportResult(decimal ConversionRatio, int PaymentsCount)
{
	public static ReportResult Create(decimal conversionRatio, int paymentsCount)
	{
		if (conversionRatio < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(conversionRatio), "Conversion ratio cannot be negative");
		}

		if (paymentsCount < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(paymentsCount), "Payments count cannot be negative");
		}

		return new ReportResult(conversionRatio, paymentsCount);
	}
}
