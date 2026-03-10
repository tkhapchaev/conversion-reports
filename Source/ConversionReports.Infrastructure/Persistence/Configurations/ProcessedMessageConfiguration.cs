using ConversionReports.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConversionReports.Infrastructure.Persistence.Configurations;

public class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
	public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
	{
		builder.ToTable("processed_messages");

		builder.HasKey(x => x.MessageId);
		builder.Property(x => x.MessageId).HasMaxLength(128).IsRequired();
		builder.Property(x => x.ProcessedAtUtc).IsRequired();
	}
}
