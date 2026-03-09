namespace ConversionReports.Domain.Entities;

public class ReportRequest
{
	private ReportRequest()
	{
		SourceMessageId = string.Empty;
		UserId = string.Empty;
	}

	public ReportRequest(
		Guid id,
		string sourceMessageId,
		string userId,
		Guid batchId,
		Guid batchItemId,
		DateTimeOffset createdAtUtc)
	{
		if (id == Guid.Empty)
		{
			throw new ArgumentException("Request id must be provided", nameof(id));
		}

		if (string.IsNullOrWhiteSpace(sourceMessageId))
		{
			throw new ArgumentException("Source message id must be provided", nameof(sourceMessageId));
		}

		if (string.IsNullOrWhiteSpace(userId))
		{
			throw new ArgumentException("User id must be provided", nameof(userId));
		}

		if (batchId == Guid.Empty)
		{
			throw new ArgumentException("Batch id must be provided", nameof(batchId));
		}

		if (batchItemId == Guid.Empty)
		{
			throw new ArgumentException("Batch item id must be provided", nameof(batchItemId));
		}

		Id = id;
		SourceMessageId = sourceMessageId;
		UserId = userId;
		BatchId = batchId;
		BatchItemId = batchItemId;
		CreatedAtUtc = createdAtUtc;
	}

	public Guid Id { get; private set; }
	public string SourceMessageId { get; private set; }
	public string UserId { get; private set; }
	public Guid BatchId { get; private set; }
	public Guid BatchItemId { get; private set; }
	public DateTimeOffset CreatedAtUtc { get; private set; }

	public ReportBatchItem BatchItem { get; private set; } = null!;
	public ReportBatch Batch { get; private set; } = null!;
}
