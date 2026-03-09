namespace ConversionReports.Domain.ValueObjects;

public record ReportDefinition(
	DateTimeOffset PeriodStartUtc,
	DateTimeOffset PeriodEndUtc,
	long ProductId,
	long DesignId)
{
	public void Validate()
	{
		if (PeriodEndUtc <= PeriodStartUtc)
		{
			throw new ArgumentException("Period end must be greater than period start", nameof(PeriodEndUtc));
		}

		if (ProductId <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(ProductId), "Product id must be positive");
		}

		if (DesignId <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(DesignId), "Design id must be positive");
		}
	}
}
