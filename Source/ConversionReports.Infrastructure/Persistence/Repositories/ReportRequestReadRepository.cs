using ConversionReports.Application.Interfaces;
using ConversionReports.Application.Models;
using ConversionReports.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ConversionReports.Infrastructure.Persistence.Repositories;

public class ReportRequestReadRepository : IReportRequestReadRepository
{
	private readonly AppDbContext _dbContext;

	public ReportRequestReadRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public async Task<ReportRequestStatusDto?> GetStatusAsync(Guid requestId, CancellationToken cancellationToken)
	{
		if (requestId == Guid.Empty)
		{
			throw new ArgumentException("Request id must be provided", nameof(requestId));
		}

		var status = await _dbContext.ReportRequests
			.AsNoTracking()
			.Where(x => x.Id == requestId)
			.Select(x => new ReportRequestStatusDto(
				x.Id,
				x.UserId,
				x.BatchItem.State == BatchItemState.Pending
					? ReportRequestState.Pending
					: x.BatchItem.State == BatchItemState.Processing
						? ReportRequestState.Processing
						: x.BatchItem.State == BatchItemState.Completed
							? ReportRequestState.Completed
							: ReportRequestState.Failed,
				x.BatchItem.ConversionRatio,
				x.BatchItem.PaymentsCount,
				x.BatchItem.Error,
				x.CreatedAtUtc,
				x.BatchItem.CompletedAtUtc))
			.SingleOrDefaultAsync(cancellationToken);

		return status;
	}
}
