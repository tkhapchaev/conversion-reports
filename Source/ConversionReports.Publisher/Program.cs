using Confluent.Kafka;
using Confluent.Kafka.Admin;
using ConversionReports.Contracts.Messages;
using System.Globalization;
using System.Text.Json;

var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9094";
var topicName = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "report-requests";

var requestId = ParseGuidOrDefault(args, 0, Guid.NewGuid());
var userId = ParseStringOrDefault(args, 1, "user-1");
var productId = ParseLongOrDefault(args, 2, 100);
var designId = ParseLongOrDefault(args, 3, 200);
var periodStartUtc = ParseDateTimeOffsetOrDefault(args, 4, DateTimeOffset.UtcNow.AddDays(-7));
var periodEndUtc = ParseDateTimeOffsetOrDefault(args, 5, DateTimeOffset.UtcNow);
var requestedAtUtc = ParseDateTimeOffsetOrDefault(args, 6, DateTimeOffset.UtcNow);
var messageId = ParseStringOrDefault(args, 7, Guid.NewGuid().ToString("N"));

ValidateInput(bootstrapServers, topicName, requestId, userId, productId, designId, periodStartUtc, periodEndUtc, requestedAtUtc, messageId);

var message = new ReportRequestedIntegrationEvent(
	requestId,
	messageId,
	userId,
	periodStartUtc,
	periodEndUtc,
	productId,
	designId,
	requestedAtUtc);

await EnsureTopicExistsAsync(bootstrapServers, topicName);

var producerConfig = new ProducerConfig
{
	BootstrapServers = bootstrapServers,
	AllowAutoCreateTopics = false
};

using var producer = new ProducerBuilder<Null, string>(producerConfig).Build();

var payload = JsonSerializer.Serialize(message, new JsonSerializerOptions(JsonSerializerDefaults.Web));
var deliveryResult = await producer.ProduceAsync(topicName, new Message<Null, string> { Value = payload });

producer.Flush(TimeSpan.FromSeconds(5));

Console.WriteLine($"[{DateTime.Now}] Published request {requestId} for user '{userId}' to topic '{deliveryResult.Topic}' at offset {deliveryResult.Offset.Value}");
Console.WriteLine($"Period: {periodStartUtc:O} - {periodEndUtc:O}");
Console.WriteLine($"MessageId: {messageId}");

static Guid ParseGuidOrDefault(string[] args, int index, Guid defaultValue)
{
	if (args.Length <= index || string.IsNullOrWhiteSpace(args[index]))
	{
		return defaultValue;
	}

	if (Guid.TryParse(args[index], out var value))
	{
		return value;
	}

	throw new ArgumentException($"Argument at index {index} must be a valid Guid");
}

static string ParseStringOrDefault(string[] args, int index, string defaultValue)
{
	if (string.IsNullOrWhiteSpace(defaultValue))
	{
		throw new ArgumentException("Default value must be provided", nameof(defaultValue));
	}

	if (args.Length <= index || string.IsNullOrWhiteSpace(args[index]))
	{
		return defaultValue;
	}

	return args[index];
}

static long ParseLongOrDefault(string[] args, int index, long defaultValue)
{
	if (args.Length <= index || string.IsNullOrWhiteSpace(args[index]))
	{
		return defaultValue;
	}

	if (long.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
	{
		return value;
	}

	throw new ArgumentException($"Argument at index {index} must be a valid Int64");
}

static DateTimeOffset ParseDateTimeOffsetOrDefault(string[] args, int index, DateTimeOffset defaultValue)
{
	if (args.Length <= index || string.IsNullOrWhiteSpace(args[index]))
	{
		return defaultValue;
	}

	if (DateTimeOffset.TryParse(
		args[index],
		CultureInfo.InvariantCulture,
		DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
		out var value))
	{
		return value;
	}

	throw new ArgumentException($"Argument at index {index} must be a valid ISO 8601 datetime");
}

static void ValidateInput(
	string bootstrapServers,
	string topicName,
	Guid requestId,
	string userId,
	long productId,
	long designId,
	DateTimeOffset periodStartUtc,
	DateTimeOffset periodEndUtc,
	DateTimeOffset requestedAtUtc,
	string messageId)
{
	if (string.IsNullOrWhiteSpace(bootstrapServers))
	{
		throw new ArgumentException("Bootstrap servers must be provided", nameof(bootstrapServers));
	}

	if (string.IsNullOrWhiteSpace(topicName))
	{
		throw new ArgumentException("Topic name must be provided", nameof(topicName));
	}

	if (requestId == Guid.Empty)
	{
		throw new ArgumentException("Request id must be provided", nameof(requestId));
	}

	if (string.IsNullOrWhiteSpace(userId))
	{
		throw new ArgumentException("User id must be provided", nameof(userId));
	}

	if (productId <= 0)
	{
		throw new ArgumentOutOfRangeException(nameof(productId), "Product id must be greater than zero");
	}

	if (designId <= 0)
	{
		throw new ArgumentOutOfRangeException(nameof(designId), "Design id must be greater than zero");
	}

	if (periodEndUtc <= periodStartUtc)
	{
		throw new ArgumentException("Period end must be greater than period start");
	}

	if (string.IsNullOrWhiteSpace(messageId))
	{
		throw new ArgumentException("Message id must be provided", nameof(messageId));
	}
}

static async Task EnsureTopicExistsAsync(string bootstrapServers, string topicName)
{
	using var adminClient = new AdminClientBuilder(new AdminClientConfig
	{
		BootstrapServers = bootstrapServers
	}).Build();

	try
	{
		await adminClient.CreateTopicsAsync(
		[
			new TopicSpecification
			{
				Name = topicName,
				NumPartitions = 1,
				ReplicationFactor = 1
			}
		]);
	}
	catch (CreateTopicsException exception) when (exception.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
	{
	}
}
