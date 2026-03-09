using ConversionReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConversionReports.Infrastructure.Persistence.Configurations;

public class ReportBatchConfiguration : IEntityTypeConfiguration<ReportBatch>
{
	public void Configure(EntityTypeBuilder<ReportBatch> builder)
	{
		builder.ToTable("report_batches");

		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id).ValueGeneratedNever();
		builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
		builder.Property(x => x.BusinessDate).IsRequired();
		builder.Property(x => x.DueAtUtc).IsRequired();
		builder.Property(x => x.CreatedAtUtc).IsRequired();
		builder.Property(x => x.State).HasConversion<int>().IsRequired();

		builder.HasIndex(x => new { x.UserId, x.BusinessDate, x.State });

		builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
		builder.Navigation(x => x.Requests).UsePropertyAccessMode(PropertyAccessMode.Field);
	}
}
