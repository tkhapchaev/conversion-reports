using ConversionReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConversionReports.Application.Interfaces;

public interface IAppDbContext
{
	DbSet<ReportBatch> ReportBatches { get; }
	DbSet<ReportBatchItem> ReportBatchItems { get; }
	DbSet<ReportRequest> ReportRequests { get; }
	DbSet<ProcessedMessage> ProcessedMessages { get; }

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
