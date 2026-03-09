using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Models;

namespace ConversionReports.Application.Services;

public class ReportStatusQueryService
{
	private readonly IReportResultCache _cache;
	private readonly IReportRequestReadRepository _repository;

	public ReportStatusQueryService(IReportResultCache cache, IReportRequestReadRepository repository)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_repository = repository ?? throw new ArgumentNullException(nameof(repository));
	}

	public async Task<ReportRequestStatusDto> GetAsync(Guid requestId, CancellationToken cancellationToken)
	{
		if (requestId == Guid.Empty)
		{
			throw new ArgumentException("Request id must be provided", nameof(requestId));
		}

		if (_cache.TryGet(requestId, out var cached))
		{
			return cached;
		}

		var current = await _repository.GetStatusAsync(requestId, cancellationToken);

		if (current is null)
		{
			current = new ReportRequestStatusDto(
				requestId,
				"unknown",
				ReportRequestState.NotFound,
				null,
				null,
				"Request was not found",
				DateTimeOffset.MinValue,
				null);
		}

		_cache.Set(current);

		return current;
	}
}
