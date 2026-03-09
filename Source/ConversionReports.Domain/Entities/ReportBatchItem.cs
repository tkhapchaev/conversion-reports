using ConversionReports.Domain.Enums;
using ConversionReports.Domain.ValueObjects;

namespace ConversionReports.Domain.Entities;

public class ReportBatchItem
{
	private ReportBatchItem()
	{
	}

	public ReportBatchItem(
		Guid batchId,
		DateTimeOffset periodStartUtc,
		DateTimeOffset periodEndUtc,
		long productId,
		long designId,
		DateTimeOffset createdAtUtc)
	{
		if (batchId == Guid.Empty)
		{
			throw new ArgumentException("Batch id must be provided", nameof(batchId));
		}

		var definition = new ReportDefinition(periodStartUtc, periodEndUtc, productId, designId);
		definition.Validate();

		Id = Guid.NewGuid();
		BatchId = batchId;
		PeriodStartUtc = periodStartUtc;
		PeriodEndUtc = periodEndUtc;
		ProductId = productId;
		DesignId = designId;
		CreatedAtUtc = createdAtUtc;
		State = BatchItemState.Pending;
	}

	public Guid Id { get; private set; }
	public Guid BatchId { get; private set; }
	public DateTimeOffset PeriodStartUtc { get; private set; }
	public DateTimeOffset PeriodEndUtc { get; private set; }
	public long ProductId { get; private set; }
	public long DesignId { get; private set; }
	public BatchItemState State { get; private set; }
	public decimal? ConversionRatio { get; private set; }
	public int? PaymentsCount { get; private set; }
	public string? Error { get; private set; }
	public DateTimeOffset CreatedAtUtc { get; private set; }
	public DateTimeOffset? CompletedAtUtc { get; private set; }

	public ReportDefinition ToDefinition()
	{

		return new ReportDefinition(PeriodStartUtc, PeriodEndUtc, ProductId, DesignId);
	}

	public void MarkProcessing()
	{
		if (State is BatchItemState.Completed or BatchItemState.Processing)
		{

			return;
		}

		State = BatchItemState.Processing;
		Error = null;
	}

	public void Complete(ReportResult result, DateTimeOffset completedAtUtc)
	{
		ArgumentNullException.ThrowIfNull(result);

		State = BatchItemState.Completed;
		ConversionRatio = result.ConversionRatio;
		PaymentsCount = result.PaymentsCount;
		CompletedAtUtc = completedAtUtc;
		Error = null;
	}

	public void Fail(string error)
	{
		if (string.IsNullOrWhiteSpace(error))
		{
			throw new ArgumentException("Error must be provided", nameof(error));
		}

		State = BatchItemState.Failed;
		Error = error;
	}

	public bool Matches(ReportDefinition definition)
	{
		ArgumentNullException.ThrowIfNull(definition);

		return PeriodStartUtc == definition.PeriodStartUtc
			&& PeriodEndUtc == definition.PeriodEndUtc
			&& ProductId == definition.ProductId
			&& DesignId == definition.DesignId;
	}
}
