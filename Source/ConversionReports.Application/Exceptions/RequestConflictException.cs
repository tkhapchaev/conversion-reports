namespace ConversionReports.Application.Exceptions;

public class RequestConflictException : Exception
{
	public RequestConflictException(
		Guid requestId,
		string existingUserId,
		string incomingUserId,
		DateTimeOffset existingPeriodStartUtc,
		DateTimeOffset existingPeriodEndUtc,
		long existingProductId,
		long existingDesignId,
		DateTimeOffset incomingPeriodStartUtc,
		DateTimeOffset incomingPeriodEndUtc,
		long incomingProductId,
		long incomingDesignId)
		: base("Incoming request conflicts with an existing request")
	{
		if (requestId == Guid.Empty)
		{
			throw new ArgumentException("Request id must be provided", nameof(requestId));
		}

		if (string.IsNullOrWhiteSpace(existingUserId))
		{
			throw new ArgumentException("Existing user id must be provided", nameof(existingUserId));
		}

		if (string.IsNullOrWhiteSpace(incomingUserId))
		{
			throw new ArgumentException("Incoming user id must be provided", nameof(incomingUserId));
		}

		RequestId = requestId;
		ExistingUserId = existingUserId;
		IncomingUserId = incomingUserId;
		ExistingPeriodStartUtc = existingPeriodStartUtc;
		ExistingPeriodEndUtc = existingPeriodEndUtc;
		ExistingProductId = existingProductId;
		ExistingDesignId = existingDesignId;
		IncomingPeriodStartUtc = incomingPeriodStartUtc;
		IncomingPeriodEndUtc = incomingPeriodEndUtc;
		IncomingProductId = incomingProductId;
		IncomingDesignId = incomingDesignId;
	}

	public Guid RequestId { get; }
	public string ExistingUserId { get; }
	public string IncomingUserId { get; }
	public DateTimeOffset ExistingPeriodStartUtc { get; }
	public DateTimeOffset ExistingPeriodEndUtc { get; }
	public long ExistingProductId { get; }
	public long ExistingDesignId { get; }
	public DateTimeOffset IncomingPeriodStartUtc { get; }
	public DateTimeOffset IncomingPeriodEndUtc { get; }
	public long IncomingProductId { get; }
	public long IncomingDesignId { get; }
}
