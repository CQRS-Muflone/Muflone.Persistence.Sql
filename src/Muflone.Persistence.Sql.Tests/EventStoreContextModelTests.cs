using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Muflone.Persistence.Sql.Models;

namespace Muflone.Persistence.Sql.Tests;

public class EventStoreContextModelTests
{
    // Building the model never opens a connection, so a dummy connection string is enough.
    private static EventStoreContext CreateContext() =>
        new(new DbContextOptionsBuilder<EventStoreContext>()
            .UseSqlServer("Server=(local);Database=ModelValidationOnly;Trusted_Connection=True;")
            .Options);

    [Fact]
    public void Model_PassesEfCoreValidation()
    {
        using var context = CreateContext();

        // Accessing Model runs the EF Core model validator, which throws on invalid mappings.
        var exception = Record.Exception(() => context.Model);

        Assert.Null(exception);
    }

    [Fact]
    public void EventStorePosition_Id_IsGeneratedOnlyOnAdd()
    {
        using var context = CreateContext();

        var id = context.Model.FindEntityType(typeof(EventStorePosition))!
            .FindProperty(nameof(EventStorePosition.Id))!;

        Assert.Equal(ValueGenerated.OnAdd, id.ValueGenerated);
    }
}
