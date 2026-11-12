using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Muflone.Persistence.Sql.Models;

namespace Muflone.Persistence.Sql.Mappings;

public class EventStorePositionMapping : IEntityTypeConfiguration<EventStorePosition>
{
    public void Configure(EntityTypeBuilder<EventStorePosition> builder)
    {
        builder.ToTable("EventStorePosition", "dbo");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedOnAddOrUpdate()
            .UseIdentityColumn();
        
    }
}