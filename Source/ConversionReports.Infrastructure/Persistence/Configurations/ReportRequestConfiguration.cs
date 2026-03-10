using ConversionReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConversionReports.Infrastructure.Persistence.Configurations;

public class ReportRequestConfiguration : IEntityTypeConfiguration<ReportRequest>
{
	public void Configure(EntityTypeBuilder<ReportRequest> builder)
	{
		builder.ToTable("report_requests");

		builder.HasKey(x => x.Id);
		builder.Property(x => x.Id).ValueGeneratedNever();
		builder.Property(x => x.SourceMessageId).HasMaxLength(128).IsRequired();
		builder.Property(x => x.UserId).HasMaxLength(128).IsRequired();
		builder.Property(x => x.CreatedAtUtc).IsRequired();

		builder.HasIndex(x => x.SourceMessageId).IsUnique();
		builder.HasIndex(x => x.BatchItemId);

		builder.HasOne(x => x.Batch)
			.WithMany(x => x.Requests)
			.HasForeignKey(x => x.BatchId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BatchItem)
			.WithMany()
			.HasForeignKey(x => x.BatchItemId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
