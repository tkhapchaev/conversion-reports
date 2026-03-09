using System.Text.Json;
using ConversionReports.Contracts.Messages;

namespace ConversionReports.Infrastructure.Messaging;

internal static class KafkaMessageSerializer
{
	private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

	public static string Serialize(ReportRequestedIntegrationEvent message)
	{
		ArgumentNullException.ThrowIfNull(message);

		return JsonSerializer.Serialize(message, SerializerOptions);
	}

	public static ReportRequestedIntegrationEvent Deserialize(string payload)
	{
		if (string.IsNullOrWhiteSpace(payload))
		{
			throw new ArgumentException("Payload must be provided", nameof(payload));
		}

		var message = JsonSerializer.Deserialize<ReportRequestedIntegrationEvent>(payload, SerializerOptions);

		if (message is null)
		{
			throw new InvalidOperationException("Kafka message payload cannot be deserialized");
		}

		return message;
	}
}
