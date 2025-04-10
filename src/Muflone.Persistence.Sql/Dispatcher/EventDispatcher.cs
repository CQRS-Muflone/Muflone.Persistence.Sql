using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Muflone.Messages.Events;
using Muflone.Persistence.Sql.Persistence;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ArgumentNullException = System.ArgumentNullException;

namespace Muflone.Persistence.Sql.Dispatcher;

public class EventDispatcher : IHostedService
{
    private readonly IEventBus _eventBus;
    private readonly IEventStorePositionRepository _eventStorePositionRepository;
    private readonly SqlOptions _sqlOptions;
    private readonly EventProcessorClient _eventProcessorClient;
    private readonly ILogger _logger;
    private Position _lastProcessed;

    public EventDispatcher(ILoggerFactory loggerFactory, SqlOptions sqlOptions, EventProcessorClient eventProcessorClient,
        IEventBus eventBus, IEventStorePositionRepository eventStorePositionRepository)
    {
        _logger = loggerFactory.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
        _sqlOptions = sqlOptions ?? throw new ArgumentNullException(nameof(sqlOptions));
        _eventProcessorClient = eventProcessorClient ?? throw new ArgumentNullException(nameof(eventProcessorClient));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _eventStorePositionRepository = eventStorePositionRepository ?? throw new ArgumentNullException(nameof(eventStorePositionRepository));
        _lastProcessed = new Position(0, 0);
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("EventDispatcher started");
        
        _eventProcessorClient.ProcessEventAsync += EventProcessorClientOnProcessEventAsync;
        _eventProcessorClient.ProcessErrorAsync += EventProcessorClientOnProcessErrorAsync;

        try
        {
            await GetLastPositionAsync();
            await AlignEventStorePositionAsync();
            await _eventProcessorClient.StartProcessingAsync(cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        await GetLastPositionAsync();
    }

    private async Task EventProcessorClientOnProcessEventAsync(ProcessEventArgs eventArgs)
    {
        var messageId = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
        await using var facade = new EventStoreFacade(_sqlOptions.ConnectionString);
        var @event = facade.GetEventByMessageIdAsync(messageId, eventArgs.CancellationToken);
        
        if (@event == null)
            return;
        
        await PublishEvent(@event);
    }
    
    private Task EventProcessorClientOnProcessErrorAsync(ProcessErrorEventArgs arg)
    {
        _logger.LogError($"Error in EventProcessorClient: {arg.Exception.Message}");
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        _logger.LogInformation("EventDispatcher stopped");

        return Task.CompletedTask;
    }

    private async Task AlignEventStorePositionAsync()
    {
        try
        {
            await using var facade = new EventStoreFacade(_sqlOptions.ConnectionString);
            var readResult = facade.EventStore
                .Where(e => e.CommitPosition > _lastProcessed.CommitPosition)
                .ToList();
                    
            var @event = readResult.FirstOrDefault();
            if (@event == null) 
                return;
                    
            await PublishEvent(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in EventProcessorClient: {ex}");
            throw;
        }
    }

    private async Task GetLastPositionAsync()
    {
        var position = await _eventStorePositionRepository.GetLastPositionAsync();
        _lastProcessed = new Position(position.CommitPosition, position.PreparePosition);
    }
    
    private async Task UpdateLastPositionAsync(Position position)
    {
        await _eventStorePositionRepository.SaveAsync(EventStorePosition.Create(position.CommitPosition,
            position.PreparePosition));
    }
    
    private async Task PublishEvent(EventRecord resolvedEvent)
    {
        var processedEvent = ProcessRawEvent(resolvedEvent);
        if (processedEvent != null)
        {
            processedEvent.Headers.Set(Constants.CommitPosition, resolvedEvent.CommitPosition.ToString());
            processedEvent.Headers.Set(Constants.PreparePosition, resolvedEvent.CommitPosition.ToString());
            
            await _eventBus.PublishAsync(processedEvent);
        }

        _lastProcessed = new Position(resolvedEvent.CommitPosition, resolvedEvent.CommitPosition);
        await UpdateLastPositionAsync(_lastProcessed);
    }
    
    private DomainEvent? ProcessRawEvent(EventRecord rawEvent)
    {
        if (rawEvent.Metadata.Length > 0 && rawEvent.Data.Length > 0)
            return DeserializeEvent(rawEvent.Metadata, rawEvent.Data);

        return null;
    }
    
    private DomainEvent? DeserializeEvent(ReadOnlyMemory<byte> metadata, ReadOnlyMemory<byte> data)
    {
        if (JObject.Parse(Encoding.UTF8.GetString(metadata.ToArray())).Property("EventClrTypeName") == null)
            return null;
        var eventClrTypeName = JObject.Parse(Encoding.UTF8.GetString(metadata.ToArray())).Property("EventClrTypeName")!.Value;
        try
        {
            return (DomainEvent?)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data.ToArray()), Type.GetType((string)eventClrTypeName!)!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return null;
        }
    }
}