namespace ConversionReports.Infrastructure.Options;

public class KafkaOptions
{
	public const string SectionName = "Kafka";

	public string BootstrapServers { get; set; } = "localhost:9094";
	public string TopicName { get; set; } = "report-requests";
	public string ConsumerGroupId { get; set; } = "conversion-reports-service";
	public AutoOffsetResetBehavior AutoOffsetReset { get; set; } = AutoOffsetResetBehavior.Earliest;
}

public enum AutoOffsetResetBehavior
{
	Earliest = 1,
	Latest = 2
}
