namespace ConversionReports.Contracts.Messages;

public record ReportRequestedIntegrationEvent
{
	public ReportRequestedIntegrationEvent(
		Guid requestId,
		string messageId,
		string userId,
		DateTimeOffset periodStartUtc,
		DateTimeOffset periodEndUtc,
		long productId,
		long designId,
		DateTimeOffset requestedAtUtc)
	{
		if (requestId == Guid.Empty)
		{
			throw new ArgumentException("Request id must be provided", nameof(requestId));
		}

		if (string.IsNullOrWhiteSpace(messageId))
		{
			throw new ArgumentException("Message id must be provided", nameof(messageId));
		}

		if (string.IsNullOrWhiteSpace(userId))
		{
			throw new ArgumentException("User id must be provided", nameof(userId));
		}

		if (periodEndUtc <= periodStartUtc)
		{
			throw new ArgumentException("Period end must be greater than period start", nameof(periodEndUtc));
		}

		if (productId <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(productId), "Product id must be positive");
		}

		if (designId <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(designId), "Design id must be positive");
		}

		RequestId = requestId;
		MessageId = messageId;
		UserId = userId;
		PeriodStartUtc = periodStartUtc;
		PeriodEndUtc = periodEndUtc;
		ProductId = productId;
		DesignId = designId;
		RequestedAtUtc = requestedAtUtc;
	}

	public Guid RequestId { get; init; }
	public string MessageId { get; init; }
	public string UserId { get; init; }
	public DateTimeOffset PeriodStartUtc { get; init; }
	public DateTimeOffset PeriodEndUtc { get; init; }
	public long ProductId { get; init; }
	public long DesignId { get; init; }
	public DateTimeOffset RequestedAtUtc { get; init; }
}
