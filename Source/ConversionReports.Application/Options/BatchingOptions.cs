namespace ConversionReports.Application.Options;

public class BatchingOptions
{
	public const string SectionName = "Batching";

	public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromHours(24);
	public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromSeconds(30);
}
