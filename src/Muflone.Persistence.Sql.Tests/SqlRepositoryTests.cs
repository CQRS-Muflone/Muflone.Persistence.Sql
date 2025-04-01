using System.Text;
using System.Text.Json;
using Muflone.Core;
using Muflone.Messages.Events;
using Muflone.Persistence.Sql.Persistence;

namespace Muflone.Persistence.Sql.Tests;

public class SqlRepositoryTests
{
    private string connectionString =
        "Server=tcp:brewup-sql-server.database.windows.net,1433;Initial Catalog=global-azure-2025;Persist Security Info=False;User ID=brewup-admin;Password=Gilda-IoT-2025;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    
    private readonly SqlRepository _sqlRepository;
    private readonly JsonSerializerOptions _serializerOptions;

    private Guid _aggregateId = new Guid("b11bd78d-a8d9-45d3-a0fb-d5adf1ee9b97");

    public SqlRepositoryTests()
    {
        _sqlRepository = new SqlRepository(new SqlOptions(connectionString));
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        };
    }
    
    [Fact]
    public async Task Can_Save_Aggregate()
    {
        var aggregateTest = new AggregateTest(new TestId(_aggregateId), "Hello Muflone.Persistence.Sql");
        await _sqlRepository.SaveAsync(aggregateTest, Guid.NewGuid());
    }
    
    [Fact]
    public async Task Can_Get_Aggregate()
    {
        var aggregateTest = await _sqlRepository.GetByIdAsync<AggregateTest>(new TestId(_aggregateId));
        Assert.Equal(_aggregateId.ToString(), aggregateTest!.Id.ToString());
    }

    [Fact]
    public void Can_Serialize_And_Deserialize_A_DomainEvent()
    {
        EventSaved @event = new(new TestId(_aggregateId), "Hello Muflone.Persistence.Sql");
        var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, _serializerOptions));
        
        var deserializedEvent = JsonSerializer.Deserialize<EventSaved>(data, _serializerOptions);
        
        Assert.Equal(@event.Message, deserializedEvent!.Message);
    }
}

public sealed class EventSaved(TestId aggregateId, string message)
    : DomainEvent(aggregateId)
{
    public string Message { get; init; } = message;
}

public sealed class TestId(Guid value) 
    : DomainId(value.ToString());

public sealed class AggregateTest : AggregateRoot
{
    private TestId _testId;
        
    protected AggregateTest()
    {}
        
    public AggregateTest(TestId testId, string message)
    {
        RaiseEvent(new EventSaved(testId, message));
    }

    private void Apply(EventSaved @event)
    {
        Id = @event.AggregateId;
        _testId = new TestId(new Guid(@event.AggregateId.Value));
    }
}