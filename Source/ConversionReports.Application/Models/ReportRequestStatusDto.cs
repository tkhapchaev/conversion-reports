namespace ConversionReports.Application.Models;

public record ReportRequestStatusDto
{
	public ReportRequestStatusDto(
		Guid requestId,
		string userId,
		ReportRequestState state,
		decimal? conversionRatio,
		int? paymentsCount,
		string? error,
		DateTimeOffset createdAtUtc,
		DateTimeOffset? completedAtUtc)
	{
		if (requestId == Guid.Empty)
		{
			throw new ArgumentException("Request id must be provided", nameof(requestId));
		}

		if (string.IsNullOrWhiteSpace(userId))
		{
			throw new ArgumentException("User id must be provided", nameof(userId));
		}

		RequestId = requestId;
		UserId = userId;
		State = state;
		ConversionRatio = conversionRatio;
		PaymentsCount = paymentsCount;
		Error = error;
		CreatedAtUtc = createdAtUtc;
		CompletedAtUtc = completedAtUtc;
	}

	public Guid RequestId { get; init; }
	public string UserId { get; init; }
	public ReportRequestState State { get; init; }
	public decimal? ConversionRatio { get; init; }
	public int? PaymentsCount { get; init; }
	public string? Error { get; init; }
	public DateTimeOffset CreatedAtUtc { get; init; }
	public DateTimeOffset? CompletedAtUtc { get; init; }
}
