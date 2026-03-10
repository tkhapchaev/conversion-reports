using ConversionReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConversionReports.Infrastructure.Persistence.Configurations;

public class ReportBatchItemConfiguration : IEntityTypeConfiguration<ReportBatchItem>
{
	public void Configure(EntityTypeBuilder<ReportBatchItem> builder)
	{
		builder.ToTable("report_batch_items");

		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id).ValueGeneratedNever();
		builder.Property(x => x.State).HasConversion<int>().IsRequired();
		builder.Property(x => x.ConversionRatio).HasPrecision(18, 4);
		builder.Property(x => x.Error).HasMaxLength(1024);
		builder.Property(x => x.CreatedAtUtc).IsRequired();

		builder.HasIndex(x => new
		{
			x.BatchId,
			x.PeriodStartUtc,
			x.PeriodEndUtc,
			x.ProductId,
			x.DesignId
		}).IsUnique();

		builder.HasOne<ReportBatch>()
			.WithMany(x => x.Items)
			.HasForeignKey(x => x.BatchId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
