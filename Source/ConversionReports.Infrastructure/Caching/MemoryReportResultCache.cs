using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Models;
using ConversionReports.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ConversionReports.Infrastructure.Caching;

public class MemoryReportResultCache : IReportResultCache
{
	private readonly IMemoryCache _cache;
	private readonly CacheOptions _options;

	public MemoryReportResultCache(IMemoryCache cache, IOptions<CacheOptions> options)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	}

	public bool TryGet(Guid requestId, out ReportRequestStatusDto status)
	{
		if (requestId == Guid.Empty)
		{
			throw new ArgumentException("Request id must be provided", nameof(requestId));
		}

		if (_cache.TryGetValue(requestId, out ReportRequestStatusDto? cached) && cached is not null)
		{
			status = cached;

			return true;
		}

		status = default!;

		return false;
	}

	public void Set(ReportRequestStatusDto status)
	{
		ArgumentNullException.ThrowIfNull(status);

		var ttl = status.State switch
		{
			ReportRequestState.Completed => _options.CompletedTtl,
			ReportRequestState.Failed => _options.FailedTtl,
			_ => _options.PendingTtl
		};

		_cache.Set(status.RequestId, status, ttl);
	}

	public void Remove(Guid requestId)
	{
		if (requestId == Guid.Empty)
		{
			throw new ArgumentException("Request id must be provided", nameof(requestId));
		}

		_cache.Remove(requestId);
	}
}
