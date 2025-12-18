using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Muflone.Persistence.Sql.Models;

namespace Muflone.Persistence.Sql.Mappings;

public class EventStoreMapping : IEntityTypeConfiguration<EventStore>
{
    public void Configure(EntityTypeBuilder<EventStore> builder)
    {
        builder.ToTable("EventStore", "dbo");
        builder.HasKey(e => e.MessageId);
        
        builder.Property(e => e.MessageId)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(e => e.AggregateId)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(e => e.AggregateName)
            .IsRequired()
            .HasMaxLength(250);
        builder.Property(e => e.AggregateType)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(e => e.Data)
            .IsRequired()
            .HasColumnType("varbinary(max)");;
        builder.Property(e => e.Metadata)
            .IsRequired()
            .HasColumnType("varbinary(max)");;
        builder.Property(e => e.Version)
            .IsRequired();
        builder.Property(e => e.CommitPosition)
            .IsRequired()
            .ValueGeneratedOnAddOrUpdate();
    }
}