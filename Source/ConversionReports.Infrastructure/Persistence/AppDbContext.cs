using ConversionReports.Application.Interfaces;
using ConversionReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConversionReports.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	public DbSet<ReportBatch> ReportBatches => Set<ReportBatch>();
	public DbSet<ReportBatchItem> ReportBatchItems => Set<ReportBatchItem>();
	public DbSet<ReportRequest> ReportRequests => Set<ReportRequest>();
	public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
		base.OnModelCreating(modelBuilder);
	}
}
