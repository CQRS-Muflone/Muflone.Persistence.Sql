using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Muflone.Persistence.Sql.Persistence;

public class EventStoreMapping : IEntityTypeConfiguration<EventRecord>
{
    public void Configure(EntityTypeBuilder<EventRecord> builder)
    {
        builder.ToTable("EventStore", "dbo");
        builder.HasKey(t => t.MessageId);

        builder.Property(t => t.CommitPosition).IsRequired().ValueGeneratedOnAdd();
    }
}