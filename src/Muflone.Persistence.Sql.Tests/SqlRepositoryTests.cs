using Microsoft.Extensions.Configuration;
using Muflone.Core;
using Muflone.Messages.Events;
using Muflone.Persistence.Sql.Dispatcher;
using Muflone.Persistence.Sql.Persistence;
using Muflone.Persistence.Sql.Services;

namespace Muflone.Persistence.Sql.Tests;

public class SqlPersistenceTests
{
    private readonly IConfiguration _configuration;
    private readonly SalesOrderId _salesOrderId;
    
    public SqlPersistenceTests()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        
        _configuration = configurationBuilder.Build();
        _salesOrderId = new SalesOrderId(Guid.NewGuid());
    }
    
    [Fact]
    public async Task Can_Save_AggregateStreamAsync()
    {
        // Arrange
        var sqlOptions = _configuration.GetSection("Muflone:SqlStore")
            .Get<SqlOptions>()!;
        var eventHobOptions = _configuration.GetSection("Muflone:EventHub")
            .Get<EventHubOptions>()!;

        // Act
        var aggregate = TestOrder.Create(_salesOrderId, new SalesOrderNumber("1234567890"));
        SqlRepository sqlRepository = new(sqlOptions, eventHobOptions);
        await sqlRepository.SaveAsync(aggregate, Guid.NewGuid(), _ => { }, CancellationToken.None);
        
        var restoredAggregate = await sqlRepository.GetByIdAsync<TestOrder>(_salesOrderId, CancellationToken.None);

        // Assert
        Assert.Equal(aggregate.Id, restoredAggregate!.Id);
    }
    
    [Fact]
    public async Task Can_Get_AggregateStreamByIdAsync()
    {
        // Arrange
        var sqlOptions = _configuration.GetSection("Muflone:SqlStore")
            .Get<SqlOptions>()!;
        var service = new MufloneSqlPersistenceService(sqlOptions);
        var version = 0;

        // Act
        var result = await service.GetAggregateStreamByIdAsync(_salesOrderId.Value, version, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}

internal class TestOrder : AggregateRoot
{
    private SalesOrderNumber _salesOrderNumber;

    protected TestOrder()
    {
    }

    internal static TestOrder Create(SalesOrderId salesOrderId, SalesOrderNumber salesOrderNumber)
    {
        return new TestOrder(salesOrderId, salesOrderNumber);
    }

    private TestOrder(SalesOrderId salesOrderId, SalesOrderNumber salesOrderNumber)
    {
        RaiseEvent(new TestOrderCreated(salesOrderId, salesOrderNumber));
    }

    private void Apply(TestOrderCreated @event)
    {
        Id = @event.AggregateId;
        _salesOrderNumber = @event.SalesOrderNumber;
    }
    
}

internal sealed class SalesOrderId : DomainId
{
    public SalesOrderId(Guid value) : base(value.ToString())
    {
    }
}

internal record SalesOrderNumber(string Value);

internal sealed class TestOrderCreated(SalesOrderId aggregateId, SalesOrderNumber salesOrderNumber) : DomainEvent(aggregateId)
{
    public SalesOrderNumber SalesOrderNumber { get; private set; } = salesOrderNumber;
}