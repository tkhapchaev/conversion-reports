using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Models;
using ConversionReports.Application.Options;
using ConversionReports.Application.Services;
using ConversionReports.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConversionReports.UnitTests.Services;

public class ReportStatusQueryServiceTests
{
	[Fact]
	public async Task GetAsync_ShouldUseCache_OnSecondRead()
	{
		using var memoryCache = new MemoryCache(new MemoryCacheOptions());

		var cache = new MemoryReportResultCache(memoryCache, Options.Create(new CacheOptions()));
		var repository = new CountingRepository();
		var service = new ReportStatusQueryService(cache, repository);
		var requestId = Guid.NewGuid();

		var first = await service.GetAsync(requestId, CancellationToken.None);
		var second = await service.GetAsync(requestId, CancellationToken.None);

		first.Should().BeEquivalentTo(second);
		repository.CallCount.Should().Be(1);
	}

	[Fact]
	public async Task GetAsync_ShouldReturnNotFoundAndCacheIt_WhenRepositoryReturnsNull()
	{
		using var memoryCache = new MemoryCache(new MemoryCacheOptions());

		var cache = new MemoryReportResultCache(memoryCache, Options.Create(new CacheOptions()));
		var repository = new NullRepository();
		var service = new ReportStatusQueryService(cache, repository);
		var requestId = Guid.Parse("44444444-4444-4444-4444-444444444444");

		var first = await service.GetAsync(requestId, CancellationToken.None);
		var second = await service.GetAsync(requestId, CancellationToken.None);

		first.RequestId.Should().Be(requestId);
		first.State.Should().Be(ReportRequestState.NotFound);
		first.Error.Should().Be("Request was not found");

		second.Should().BeEquivalentTo(first);
		repository.CallCount.Should().Be(1);
	}

	private class CountingRepository : IReportRequestReadRepository
	{
		public int CallCount { get; private set; }

		public Task<ReportRequestStatusDto?> GetStatusAsync(Guid requestId, CancellationToken cancellationToken)
		{
			CallCount++;

			return Task.FromResult<ReportRequestStatusDto?>(
				new ReportRequestStatusDto(
					requestId,
					"user-1",
					ReportRequestState.Pending,
					null,
					null,
					null,
					new DateTimeOffset(2026, 3, 8, 10, 0, 0, TimeSpan.Zero),
					null));
		}
	}

	private class NullRepository : IReportRequestReadRepository
	{
		public int CallCount { get; private set; }

		public Task<ReportRequestStatusDto?> GetStatusAsync(Guid requestId, CancellationToken cancellationToken)
		{
			CallCount++;

			return Task.FromResult<ReportRequestStatusDto?>(null);
		}
	}
}
