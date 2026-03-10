namespace ConversionReports.Application.Options;

public class CacheOptions
{
	public const string SectionName = "Caching";

	public TimeSpan PendingTtl { get; set; } = TimeSpan.FromSeconds(5);
	public TimeSpan CompletedTtl { get; set; } = TimeSpan.FromMinutes(10);
	public TimeSpan FailedTtl { get; set; } = TimeSpan.FromMinutes(1);
}
