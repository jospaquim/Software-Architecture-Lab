# Event-Driven Architecture (EDA) with Event Sourcing & CQRS

Implementación completa de Event-Driven Architecture con Event Sourcing y CQRS en C# .NET 8.

##  Arquitectura

```
┌──────────────────────────────────────────────────────────────┐
│                         CLIENT                                │
└───────────┬──────────────────────────────┬───────────────────┘
            │                              │
        Command                         Query
            │                              │
            ▼                              ▼
┌───────────────────────┐      ┌───────────────────────┐
│   WRITE SIDE (CQRS)   │      │   READ SIDE (CQRS)    │
│                       │      │                       │
│   Command Handlers    │      │   Query Handlers      │
│         │             │      │         │             │
│         ▼             │      │         ▼             │
│   Aggregate (ES)      │      │   Read Model          │
│         │             │      │   (Denormalized)      │
│         ▼             │      │                       │
│   Domain Events       │      │                       │
│         │             │      │                       │
└─────────┼─────────────┘      └───────────────────────┘
          │                             ▲
          ▼                             │
┌─────────────────────┐                 │
│    EVENT STORE      │─────────────────┘
│  (Source of Truth)  │      Projections
└─────────────────────┘
```

##  Estructura

```
EDA/
├── EventStore/
│   ├── IEventStore.cs                  # Event Store interface
│   ├── InMemoryEventStore.cs           # In-memory implementation
│   └── EventWrapper.cs                 # Event persistence model
├── WriteModel/                         # CQRS Write Side
│   └── Domain/
│       └── OrderAggregate.cs           # Aggregate with Event Sourcing
├── ReadModel/                          # CQRS Read Side
│   ├── OrderReadModel.cs               # Denormalized read model
│   └── IOrderReadModelRepository.cs    # Read model repository
├── Projections/
│   ├── OrderProjection.cs              # Updates read model from events
│   └── IEventBus.cs                    # Event publishing
├── Sagas/                              # Long-running transactions
└── Infrastructure/
    └── EventStoreDatabaseContext.cs
```

##  Conceptos Clave

### 1. Event Sourcing

**Estado se reconstruye desde eventos, no se almacena directamente.**

#### Ejemplo: Order Aggregate

```csharp
// Traditional approach (state-based)
public class Order
{
    public Guid Id { get; set; }
    public OrderStatus Status { get; set; } // Current state
    public decimal Total { get; set; }
}

// Event Sourcing approach (event-based)
public class OrderAggregate
{
    // State is private
    private OrderStatus Status;
    private decimal Total;

    // Public API are commands
    public void ConfirmOrder()
    {
        // Apply event (which changes state)
        ApplyChange(new OrderConfirmedEvent
        {
            AggregateId = Id,
            Total = Total
        });
    }

    // Event handler (rebuilds state)
    private void Apply(OrderConfirmedEvent @event)
    {
        Status = OrderStatus.Confirmed;
    }

    // Reconstruction from events
    public static OrderAggregate LoadFromHistory(IEnumerable<IEvent> events)
    {
        var order = new OrderAggregate();

        foreach (var @event in events)
        {
            order.Apply((dynamic)@event);
        }

        return order;
    }
}
```

#### Event Store

```csharp
// Events are stored, not current state
Event Store:
[
    { EventId: 1, Type: "OrderCreated", AggregateId: "123", Version: 1, ... },
    { EventId: 2, Type: "ItemAdded", AggregateId: "123", Version: 2, ... },
    { EventId: 3, Type: "OrderConfirmed", AggregateId: "123", Version: 3, ... },
    { EventId: 4, Type: "OrderShipped", AggregateId: "123", Version: 4, ... }
]

// To get current state: replay events
var events = eventStore.GetEvents(aggregateId);
var order = OrderAggregate.LoadFromHistory(events);
// order.Status == Shipped
```

### 2. CQRS (Command Query Responsibility Segregation)

**Separate models for reading and writing.**

#### Write Model (Commands)

```csharp
// Command
public record CreateOrderCommand(Guid CustomerId);

// Command Handler
public class CreateOrderCommandHandler
{
    private readonly IEventStore _eventStore;

    public async Task<Guid> Handle(CreateOrderCommand command)
    {
        // Create aggregate
        var order = OrderAggregate.CreateNew(command.CustomerId);

        // Save events
        await _eventStore.SaveEventsAsync(
            order.Id,
            order.UncommittedEvents,
            expectedVersion: 0);

        return order.Id;
    }
}
```

#### Read Model (Queries)

```csharp
// Query
public record GetOrderQuery(Guid OrderId);

// Query Handler
public class GetOrderQueryHandler
{
    private readonly IOrderReadModelRepository _repository;

    public async Task<OrderReadModel> Handle(GetOrderQuery query)
    {
        // Read from optimized read model
        return await _repository.GetByIdAsync(query.OrderId);
    }
}
```

### 3. Projections

**Update read model when events occur.**

```csharp
public class OrderProjection : IProjection
{
    private readonly IOrderReadModelRepository _readRepository;

    public async Task ProjectAsync(IEvent @event)
    {
        await ((dynamic)this).HandleAsync((dynamic)@event);
    }

    private async Task HandleAsync(OrderCreatedEvent @event)
    {
        // Create read model
        var readModel = new OrderReadModel
        {
            Id = @event.AggregateId,
            CustomerId = @event.CustomerId,
            Status = "Draft",
            CreatedAt = @event.OccurredAt
        };

        await _readRepository.SaveAsync(readModel);
    }

    private async Task HandleAsync(OrderConfirmedEvent @event)
    {
        // Update read model
        var readModel = await _readRepository.GetByIdAsync(@event.AggregateId);
        readModel.Status = "Confirmed";
        readModel.ConfirmedAt = @event.OccurredAt;

        await _readRepository.UpdateAsync(readModel);
    }
}
```

### 4. Eventually Consistent Reads

```
1. Command arrives → Create event → Save to Event Store
2. Event published to Event Bus
3. Projections update Read Model (async)
4. Query can now read updated data

Time: Command → Event Store (0ms)
      Event → Projection → Read Model (10-100ms)

Therefore: Reads are "eventually consistent"
```

##  Ventajas de Event Sourcing + CQRS

| Ventaja | Descripción | Ejemplo |
|---------|-------------|---------|
| **Complete Audit Trail** | All changes are recorded | "Show me all changes to Order #123" |
| **Temporal Queries** | Query state at any point in time | "What was the order status on March 15?" |
| **Event Replay** | Rebuild state, fix bugs, create new projections | Replay events to fix corrupted read model |
| **Scalability** | Read and write databases can scale independently | Read replicas for high traffic |
| **Performance** | Read model optimized for queries | Denormalized data, no joins |
| **Debugging** | Reproduce exact state that caused bug | Replay events to debug |
| **Analytics** | Rich event stream for business intelligence | "How many orders were cancelled last month?" |

##  Desventajas

| Desventaja | Descripción | Mitigación |
|------------|-------------|------------|
| **Complexity** | Much more complex than CRUD | Only use for complex domains |
| **Learning Curve** | Team needs to understand ES/CQRS | Training and documentation |
| **Eventual Consistency** | Reads may be stale | Design UI to handle it |
| **Event Versioning** | Changing event schema is hard | Upcasting, event versioning |
| **Debugging** | Harder to debug distributed system | Correlation IDs, distributed tracing |

##  Cuándo Usar Event Sourcing

###  Casos de Uso Ideales

1. **Financial Systems**
   - Banking transactions
   - Stock trading
   - Accounting (need complete audit trail)

2. **Collaborative Systems**
   - Google Docs-style collaboration
   - Version control systems
   - Multi-user editing

3. **IoT / Sensor Data**
   - Time-series data
   - Event streams
   - Real-time monitoring

4. **Complex Business Processes**
   - Order fulfillment with many steps
   - Insurance claim processing
   - Healthcare patient records

###  Cuándo NO Usar

1. **Simple CRUD applications** - Blog, CMS básico
2. **Small applications** - 1-2 developers, < 6 months
3. **Strong consistency required** - Inventory (can't oversell), Booking systems
4. **Team without experience** - Too complex for inexperienced team

##  Testing

```csharp
[Fact]
public void Order_ConfirmOrder_ShouldRaiseOrderConfirmedEvent()
{
    // Arrange
    var order = OrderAggregate.CreateNew(customerId);
    order.AddItem(productId, "Product", 100m, 2);

    // Act
    order.ConfirmOrder();

    // Assert
    var events = order.UncommittedEvents;
    Assert.Contains(events, e => e is OrderConfirmedEvent);
}

[Fact]
public void Order_LoadFromHistory_ShouldReconstructState()
{
    // Arrange
    var events = new List<IEvent>
    {
        new OrderCreatedEvent { AggregateId = orderId, Version = 1 },
        new ItemAddedEvent { AggregateId = orderId, UnitPrice = 100, Quantity = 2, Version = 2 },
        new OrderConfirmedEvent { AggregateId = orderId, Version = 3 }
    };

    // Act
    var order = OrderAggregate.LoadFromHistory(events);

    // Assert
    Assert.Equal(OrderStatus.Confirmed, order.Status);
    Assert.Equal(200m, order.Total);
}
```

##  Comparación: EDA vs DDD vs Clean Architecture

| Aspecto | EDA + Event Sourcing | DDD | Clean Architecture |
|---------|---------------------|-----|-------------------|
| **Complejidad** | Muy Alta | Alta | Media |
| **Auditabilidad** |  Completa | ️ Limitada | ️ Limitada |
| **Escalabilidad** |  Muy Alta |  Alta |  Alta |
| **Consistencia** |  Eventual |  Inmediata |  Inmediata |
| **Performance Reads** |  Excelente | ️ Media | ️ Media |
| **Learning Curve** |  Muy Alta |  Alta | ️ Media |
| **Mejor para** | Finance, IoT | Complex domains | General APIs |

##  Tecnologías para Producción

### Event Stores
- **EventStoreDB** - Purpose-built for event sourcing
- **Apache Kafka** - Distributed event streaming
- **SQL Server** - Table-based event store
- **CosmosDB** - Cloud event store

### Event Bus
- **RabbitMQ** - Message broker
- **Apache Kafka** - Event streaming
- **Azure Service Bus** - Cloud messaging
- **AWS EventBridge** - Serverless events

### Read Models
- **MongoDB** - Document database
- **Redis** - Cache
- **Elasticsearch** - Search engine
- **PostgreSQL** - Relational DB

##  Recursos

- [Event Sourcing - Martin Fowler](https://martinfowler.com/eaaDev/EventSourcing.html)
- [CQRS Journey - Microsoft](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/jj554200(v=pandp.10))
- [Building Event-Driven Microservices](https://www.oreilly.com/library/view/building-event-driven-microservices/9781492057888/)

---

**Este proyecto es una implementación educativa de Event Sourcing + CQRS con .NET 8.**

Para producción, considera usar EventStoreDB, Kafka, o una solución cloud-native.
