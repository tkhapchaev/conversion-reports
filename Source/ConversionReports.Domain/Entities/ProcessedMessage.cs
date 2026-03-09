namespace ConversionReports.Domain.Entities;

public class ProcessedMessage
{
	private ProcessedMessage()
	{
		MessageId = string.Empty;
	}

	public ProcessedMessage(string messageId, DateTimeOffset processedAtUtc)
	{
		if (string.IsNullOrWhiteSpace(messageId))
		{
			throw new ArgumentException("Message id must be provided", nameof(messageId));
		}

		MessageId = messageId;
		ProcessedAtUtc = processedAtUtc;
	}

	public string MessageId { get; private set; }
	public DateTimeOffset ProcessedAtUtc { get; private set; }
}
