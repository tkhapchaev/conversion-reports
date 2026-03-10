using ConversionReports.Domain.Enums;
using ConversionReports.Domain.ValueObjects;

namespace ConversionReports.Domain.Entities;

public class ReportBatch
{
	private readonly List<ReportBatchItem> _items = [];
	private readonly List<ReportRequest> _requests = [];

	private ReportBatch()
	{
		UserId = string.Empty;
	}

	public ReportBatch(string userId, DateOnly businessDate, DateTimeOffset dueAtUtc, DateTimeOffset createdAtUtc)
	{
		if (string.IsNullOrWhiteSpace(userId))
		{
			throw new ArgumentException("User id must be provided", nameof(userId));
		}

		Id = Guid.NewGuid();
		UserId = userId;
		BusinessDate = businessDate;
		DueAtUtc = dueAtUtc;
		CreatedAtUtc = createdAtUtc;
		State = BatchState.Open;
	}

	public Guid Id { get; private set; }
	public string UserId { get; private set; }
	public DateOnly BusinessDate { get; private set; }
	public DateTimeOffset DueAtUtc { get; private set; }
	public DateTimeOffset CreatedAtUtc { get; private set; }
	public DateTimeOffset? ProcessedAtUtc { get; private set; }
	public BatchState State { get; private set; }

	public IReadOnlyCollection<ReportBatchItem> Items => _items;
	public IReadOnlyCollection<ReportRequest> Requests => _requests;

	public ReportRequest AddRequest(
		Guid requestId,
		string sourceMessageId,
		string userId,
		ReportDefinition definition,
		DateTimeOffset createdAtUtc)
	{
		EnsureOpen();

		ArgumentNullException.ThrowIfNull(definition);

		definition.Validate();

		var existingItem = _items.FirstOrDefault(item => item.Matches(definition));
		var item = existingItem ?? CreateNewItem(definition, createdAtUtc);
		var request = new ReportRequest(requestId, sourceMessageId, userId, Id, item.Id, createdAtUtc);

		_requests.Add(request);

		return request;
	}

	public void MarkProcessing()
	{
		EnsureOpen();

		State = BatchState.Processing;

		foreach (var item in _items)
		{
			item.MarkProcessing();
		}
	}

	public void Complete(DateTimeOffset processedAtUtc)
	{
		if (_items.Any(item => item.State != BatchItemState.Completed))
		{
			throw new InvalidOperationException("All items must be completed before batch completion");
		}

		State = BatchState.Completed;
		ProcessedAtUtc = processedAtUtc;
	}

	public void Fail(string error)
	{
		if (string.IsNullOrWhiteSpace(error))
		{
			throw new ArgumentException("Error must be provided", nameof(error));
		}

		State = BatchState.Failed;

		foreach (var item in _items.Where(item => item.State != BatchItemState.Completed))
		{
			item.Fail(error);
		}
	}

	private ReportBatchItem CreateNewItem(ReportDefinition definition, DateTimeOffset createdAtUtc)
	{
		var item = new ReportBatchItem(
			Id,
			definition.PeriodStartUtc,
			definition.PeriodEndUtc,
			definition.ProductId,
			definition.DesignId,
			createdAtUtc);

		_items.Add(item);

		return item;
	}

	private void EnsureOpen()
	{
		if (State != BatchState.Open)
		{
			throw new InvalidOperationException("Batch is not open for new requests");
		}
	}
}
