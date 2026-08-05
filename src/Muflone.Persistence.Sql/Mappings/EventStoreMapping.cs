using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Muflone.Persistence.Sql.Mappings;

public class EventStoreMapping : IEntityTypeConfiguration<Models.EventStore>
{
    public void Configure(EntityTypeBuilder<Models.EventStore> builder)
    {
        // EventStore is a SQL Server append-only ledger table (see the
        // MakeEventStoreAppendOnlyLedgerTable migration). Future migrations on this table
        // can't change a column's data type, drop columns, or add non-nullable columns.
        builder.ToTable("EventStore", "dbo");
        builder.HasKey(e => e.MessageId);
        
        builder.Property(e => e.MessageId)
            .IsRequired()
            .HasMaxLength(36);
        builder.Property(e => e.AggregateId)
            .IsRequired()
            .HasMaxLength(36);
        builder.Property(e => e.AggregateName)
            .IsRequired()
            .HasMaxLength(250);
        builder.Property(e => e.AggregateType)
            .IsRequired()
            .HasMaxLength(250);
        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(250);
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
            .ValueGeneratedOnAddOrUpdate()
            .UseIdentityColumn();
    }
}