# Event-Driven Architecture (EDA) - Guía Completa

![EDA](https://microservices.io/i/storingevents.png)

##  Índice

1. [¿Qué es EDA?](#qué-es-eda)
2. [Conceptos Fundamentales](#conceptos-fundamentales)
3. [Event Sourcing](#event-sourcing)
4. [CQRS (Command Query Responsibility Segregation)](#cqrs)
5. [Patrones de Mensajería](#patrones-de-mensajería)
6. [Ventajas y Desventajas](#ventajas-y-desventajas)
7. [Casos de Uso Reales](#casos-de-uso-reales)
8. [Implementación con .NET](#implementación-con-net)
9. [Mejores Prácticas](#mejores-prácticas)

---

## ¿Qué es EDA?

**Event-Driven Architecture** es un patrón arquitectónico donde los componentes del sistema se comunican mediante **eventos** en lugar de llamadas directas.

### Características Clave

-  **Comunicación asíncrona** mediante eventos
-  **Desacoplamiento** entre productores y consumidores
-  **Alta escalabilidad** y procesamiento paralelo
-  **Reactividad** a cambios en tiempo real

### Filosofía Central

> "Los eventos representan hechos que ya ocurrieron en el pasado"

```
Traditional:
Service A ──(calls)──> Service B

Event-Driven:
Service A ──(publishes event)──> Event Bus ──(subscribes)──> Service B
                                     │
                                     └──(subscribes)──> Service C
```

---

## Conceptos Fundamentales

### 1. Event (Evento)

Un **hecho inmutable** que ya ocurrió.

```csharp
/// <summary>
/// Evento: Representa algo que YA sucedió
/// Naming: Pasado (OrderPlaced, CustomerRegistered, PaymentProcessed)
/// </summary>
public class OrderPlacedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    // Data
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public List<OrderItemDto> Items { get; init; }

    // Metadata
    public string EventType => nameof(OrderPlacedEvent);
    public int Version { get; init; } = 1;
}

//  Mal - Imperativo (comando, no evento)
public class PlaceOrderEvent { }

//  Bien - Pasado
public class OrderPlacedEvent { }
```

### 2. Event Bus / Message Broker

Middleware que **transporta eventos** entre productores y consumidores.

```
┌──────────────┐
│  Publisher   │
└──────┬───────┘
       │ publish
       ▼
┌─────────────────────────┐
│    Event Bus            │
│    (RabbitMQ, Kafka)    │
└─────────┬──────┬────────┘
          │      │
   subscribe  subscribe
          ▼      ▼
    ┌─────────┐ ┌─────────┐
    │ Service │ │ Service │
    │    A    │ │    B    │
    └─────────┘ └─────────┘
```

**Opciones populares**:
- **RabbitMQ**: Message broker tradicional, AMQP
- **Apache Kafka**: Event streaming platform, alto throughput
- **Azure Service Bus**: Cloud messaging
- **AWS EventBridge**: Serverless event bus
- **NATS**: Lightweight, high-performance

### 3. Event Handler (Manejador de Eventos)

Componente que **reacciona** a eventos.

```csharp
/// <summary>
/// Event Handler: Reacciona a OrderPlacedEvent
/// </summary>
public class SendOrderConfirmationEmailHandler : IEventHandler<OrderPlacedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<SendOrderConfirmationEmailHandler> _logger;

    public async Task Handle(OrderPlacedEvent @event)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(@event.CustomerId);

            await _emailService.SendAsync(new Email
            {
                To = customer.Email,
                Subject = $"Order Confirmation - {@event.OrderId}",
                Body = $"Your order for ${@event.TotalAmount} has been placed successfully!"
            });

            _logger.LogInformation(
                "Order confirmation email sent for Order {OrderId}",
                @event.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation email");
            // Retry logic, dead letter queue, etc.
        }
    }
}

// Múltiples handlers pueden reaccionar al mismo evento
public class UpdateInventoryHandler : IEventHandler<OrderPlacedEvent>
{
    public async Task Handle(OrderPlacedEvent @event)
    {
        foreach (var item in @event.Items)
        {
            await _inventoryService.DecrementStockAsync(item.ProductId, item.Quantity);
        }
    }
}

public class CreateShipmentHandler : IEventHandler<OrderPlacedEvent>
{
    public async Task Handle(OrderPlacedEvent @event)
    {
        await _shippingService.CreateShipmentAsync(@event.OrderId);
    }
}
```

---

## Event Sourcing

**Event Sourcing** es un patrón donde el **estado se reconstruye desde eventos** en lugar de almacenar el estado actual.

### Concepto

```
Traditional (State-Based):
┌─────────────────┐
│   Order Table   │
├─────────────────┤
│ Id: 123         │
│ Status: Shipped │  ← Solo estado actual
│ Total: $100     │
└─────────────────┘

Event Sourcing (Event-Based):
┌────────────────────────┐
│   Event Store          │
├────────────────────────┤
│ OrderCreated           │ ← Evento 1
│ ItemAdded              │ ← Evento 2
│ ItemAdded              │ ← Evento 3
│ OrderConfirmed         │ ← Evento 4
│ PaymentProcessed       │ ← Evento 5
│ OrderShipped           │ ← Evento 6
└────────────────────────┘
         │
         │ replay events
         ▼
┌────────────────────────┐
│   Order (reconstructed)│
│   Status: Shipped      │
│   Total: $100          │
└────────────────────────┘
```

### Implementación

```csharp
/// <summary>
/// Aggregate con Event Sourcing
/// </summary>
public class Order : AggregateRoot
{
    public OrderId Id { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
    private readonly List<OrderItem> _items = new();

    // Para reconstruir desde eventos
    private Order() { }

    // Para crear nuevo
    public static Order Create(OrderId id, CustomerId customerId)
    {
        var order = new Order();
        order.ApplyChange(new OrderCreatedEvent(id, customerId));
        return order;
    }

    public void AddItem(ProductId productId, decimal price, int quantity)
    {
        ApplyChange(new ItemAddedEvent(Id, productId, price, quantity));
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Order already confirmed");

        ApplyChange(new OrderConfirmedEvent(Id, DateTime.UtcNow));
    }

    // Apply changes (for new events)
    private void ApplyChange(DomainEvent @event)
    {
        ApplyChange(@event, isNew: true);
    }

    // Apply event to state
    private void ApplyChange(DomainEvent @event, bool isNew)
    {
        // Use dynamic dispatch to call appropriate Apply method
        this.AsDynamic().Apply(@event);

        if (isNew)
        {
            _uncommittedEvents.Add(@event);
        }
    }

    // Event handlers (reconstruyen estado)
    private void Apply(OrderCreatedEvent @event)
    {
        Id = @event.OrderId;
        Status = OrderStatus.Draft;
        Total = 0;
    }

    private void Apply(ItemAddedEvent @event)
    {
        _items.Add(new OrderItem(@event.ProductId, @event.Price, @event.Quantity));
        Total += @event.Price * @event.Quantity;
    }

    private void Apply(OrderConfirmedEvent @event)
    {
        Status = OrderStatus.Confirmed;
    }

    // Reconstruir desde eventos
    public static Order LoadFromHistory(IEnumerable<DomainEvent> history)
    {
        var order = new Order();

        foreach (var @event in history)
        {
            order.ApplyChange(@event, isNew: false);
        }

        return order;
    }
}

// Event Store Repository
public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<DomainEvent> events, int expectedVersion);
    Task<IEnumerable<DomainEvent>> GetEventsAsync(Guid aggregateId);
}

public class EventStoreRepository : IOrderRepository
{
    private readonly IEventStore _eventStore;

    public async Task<Order> GetByIdAsync(OrderId id)
    {
        var events = await _eventStore.GetEventsAsync(id.Value);
        return Order.LoadFromHistory(events);
    }

    public async Task SaveAsync(Order order)
    {
        var uncommittedEvents = order.GetUncommittedEvents();
        await _eventStore.SaveEventsAsync(order.Id.Value, uncommittedEvents, order.Version);
        order.MarkEventsAsCommitted();
    }
}
```

### Ventajas de Event Sourcing

| Ventaja | Descripción |
|---------|-------------|
| **Auditoría completa** | Tienes historial completo de todos los cambios |
| **Debugging** | Puedes reproducir exactamente qué pasó |
| **Temporal queries** | "¿Cuál era el estado el 15 de marzo?" |
| **Event replay** | Reconstruir vistas, arreglar bugs |
| **Compliance** | Regulaciones que requieren audit trail |

### Desventajas

| Desventaja | Descripción |
|------------|-------------|
| **Complejidad** | Más difícil de entender y mantener |
| **Event versioning** | ¿Qué pasa si cambia la estructura de un evento? |
| **Performance** | Replay de miles de eventos puede ser lento |
| **Queries complejas** | Necesitas proyecciones (vistas materializadas) |

---

## CQRS (Command Query Responsibility Segregation)

**CQRS** separa las operaciones de **lectura** (Query) y **escritura** (Command).

### Arquitectura Básica

```
┌──────────────┐
│   Client     │
└──────┬───────┘
       │
       ├─────────────────────────┬─────────────────────────┐
       │                         │                         │
       │ Command                 │ Query                   │
       ▼                         ▼                         │
┌─────────────────┐      ┌─────────────────┐             │
│  Command Bus    │      │   Query Bus     │             │
└────────┬────────┘      └────────┬────────┘             │
         │                        │                       │
         ▼                        ▼                       │
┌─────────────────┐      ┌─────────────────┐             │
│ Command Handler │      │  Query Handler  │             │
│  (Write Model)  │      │  (Read Model)   │             │
└────────┬────────┘      └────────┬────────┘             │
         │                        │                       │
         ▼                        ▼                       │
┌─────────────────┐      ┌─────────────────┐             │
│  Write DB       │─────▶│   Read DB       │             │
│  (Normalized)   │ sync │  (Denormalized) │             │
│  PostgreSQL     │      │  MongoDB, Redis │             │
└─────────────────┘      └─────────────────┘             │
                                  │                       │
                                  └───────────────────────┘
```

### Implementación

```csharp
// ──────────────────────────────────────
// WRITE SIDE (Commands)
// ──────────────────────────────────────

/// <summary>
/// Command: Intención de hacer algo
/// Naming: Imperativo (CreateOrder, UpdateCustomer, CancelOrder)
/// </summary>
public record CreateOrderCommand(
    Guid CustomerId,
    List<OrderItemDto> Items,
    Guid ShippingAddressId
) : IRequest<Result<Guid>>;

/// <summary>
/// Command Handler: Ejecuta la lógica de negocio
/// </summary>
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderRepository _repository;
    private readonly IEventBus _eventBus;

    public async Task<Result<Guid>> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        // 1. Create aggregate
        var order = Order.Create(OrderId.Create(), CustomerId.Create(command.CustomerId));

        // 2. Apply business logic
        foreach (var item in command.Items)
        {
            order.AddItem(
                ProductId.Create(item.ProductId),
                Money.Create(item.Price),
                item.Quantity);
        }

        // 3. Persist (guardando eventos)
        await _repository.SaveAsync(order);

        // 4. Publish domain events
        foreach (var domainEvent in order.GetDomainEvents())
        {
            await _eventBus.PublishAsync(domainEvent);
        }

        return Result<Guid>.Success(order.Id.Value);
    }
}

// ──────────────────────────────────────
// READ SIDE (Queries)
// ──────────────────────────────────────

/// <summary>
/// Query: Pedido de información
/// </summary>
public record GetOrderDetailsQuery(Guid OrderId) : IRequest<OrderDetailsDto>;

/// <summary>
/// Query Handler: Solo lee, no modifica
/// </summary>
public class GetOrderDetailsQueryHandler : IRequestHandler<GetOrderDetailsQuery, OrderDetailsDto>
{
    private readonly IOrderReadModelRepository _readRepository;

    public async Task<OrderDetailsDto> Handle(GetOrderDetailsQuery query, CancellationToken ct)
    {
        // Lee desde modelo de lectura optimizado
        var order = await _readRepository.GetOrderDetailsAsync(query.OrderId);

        if (order == null)
            throw new NotFoundException($"Order {query.OrderId} not found");

        return order;
    }
}

// ──────────────────────────────────────
// PROJECTION (Sincronización Read Model)
// ──────────────────────────────────────

/// <summary>
/// Projector: Actualiza Read Model cuando ocurren eventos
/// </summary>
public class OrderProjector :
    IEventHandler<OrderCreatedEvent>,
    IEventHandler<OrderConfirmedEvent>,
    IEventHandler<OrderShippedEvent>
{
    private readonly IOrderReadModelRepository _readRepository;

    public async Task Handle(OrderCreatedEvent @event)
    {
        // Crear vista de lectura
        var readModel = new OrderReadModel
        {
            Id = @event.OrderId,
            CustomerId = @event.CustomerId,
            Status = "Draft",
            CreatedAt = @event.OccurredAt,
            Items = new List<OrderItemReadModel>()
        };

        await _readRepository.InsertAsync(readModel);
    }

    public async Task Handle(OrderConfirmedEvent @event)
    {
        // Actualizar vista
        var order = await _readRepository.GetByIdAsync(@event.OrderId);
        order.Status = "Confirmed";
        order.ConfirmedAt = @event.OccurredAt;

        await _readRepository.UpdateAsync(order);
    }

    public async Task Handle(OrderShippedEvent @event)
    {
        var order = await _readRepository.GetByIdAsync(@event.OrderId);
        order.Status = "Shipped";
        order.ShippedAt = @event.OccurredAt;

        await _readRepository.UpdateAsync(order);
    }
}
```

### Bases de Datos Separadas

```csharp
// Write Model: PostgreSQL (normalizado)
public class OrderWriteModel
{
    public Guid Id { get; set; }
    public List<OrderItem> Items { get; set; }
    // Modelo normalizado
}

// Read Model: MongoDB (denormalizado para queries rápidas)
public class OrderReadModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } // Denormalizado
    public string CustomerEmail { get; set; } // Denormalizado
    public decimal Total { get; set; }
    public List<OrderItemReadModel> Items { get; set; }
    public string Status { get; set; }
    // Todo en un documento para lectura rápida
}

// Read Model: Redis (cache para queries frecuentes)
public class OrderSummaryCache
{
    public Guid OrderId { get; set; }
    public string Status { get; set; }
    public decimal Total { get; set; }
}
```

---

## Patrones de Mensajería

### 1. Publish-Subscribe (Pub/Sub)

```csharp
// Publisher
public class OrderService
{
    private readonly IEventBus _eventBus;

    public async Task ConfirmOrderAsync(Guid orderId)
    {
        var order = await _repository.GetByIdAsync(orderId);
        order.Confirm();

        // Publicar evento - múltiples suscriptores pueden recibirlo
        await _eventBus.PublishAsync(new OrderConfirmedEvent
        {
            OrderId = orderId,
            Total = order.Total
        });
    }
}

// Subscribers (pueden ser múltiples)
public class EmailNotificationSubscriber : IEventHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent @event)
    {
        await _emailService.SendOrderConfirmation(@event.OrderId);
    }
}

public class InventorySubscriber : IEventHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent @event)
    {
        await _inventoryService.ReserveStock(@event.OrderId);
    }
}

public class ShippingSubscriber : IEventHandler<OrderConfirmedEvent>
{
    public async Task Handle(OrderConfirmedEvent @event)
    {
        await _shippingService.PrepareShipment(@event.OrderId);
    }
}
```

### 2. Saga Pattern (Orquestación vs Coreografía)

**Saga**: Transacción distribuida a través de múltiples servicios.

#### Coreografía (Event-based)

```csharp
// Cada servicio reacciona a eventos y publica sus propios eventos

// Order Service
public class OrderService
{
    public async Task CreateOrderAsync(CreateOrderCommand command)
    {
        var order = Order.Create(command);
        await _repository.SaveAsync(order);

        // Evento 1
        await _eventBus.PublishAsync(new OrderCreatedEvent(order.Id));
    }
}

// Payment Service escucha OrderCreatedEvent
public class PaymentEventHandler : IEventHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent @event)
    {
        var payment = await _paymentService.ProcessPaymentAsync(@event.OrderId);

        if (payment.IsSuccessful)
        {
            // Evento 2
            await _eventBus.PublishAsync(new PaymentCompletedEvent(@event.OrderId));
        }
        else
        {
            // Evento de compensación
            await _eventBus.PublishAsync(new PaymentFailedEvent(@event.OrderId));
        }
    }
}

// Inventory Service escucha PaymentCompletedEvent
public class InventoryEventHandler : IEventHandler<PaymentCompletedEvent>
{
    public async Task Handle(PaymentCompletedEvent @event)
    {
        var result = await _inventoryService.ReserveStockAsync(@event.OrderId);

        if (result.IsSuccess)
        {
            await _eventBus.PublishAsync(new StockReservedEvent(@event.OrderId));
        }
        else
        {
            // Compensación - refund payment
            await _eventBus.PublishAsync(new StockReservationFailedEvent(@event.OrderId));
        }
    }
}

// Order Service escucha eventos de fallo para compensar
public class OrderCompensationHandler : IEventHandler<PaymentFailedEvent>
{
    public async Task Handle(PaymentFailedEvent @event)
    {
        var order = await _repository.GetByIdAsync(@event.OrderId);
        order.Cancel("Payment failed");
        await _repository.SaveAsync(order);
    }
}
```

#### Orquestación (Orchestrator)

```csharp
/// <summary>
/// Orchestrator: Coordina la saga centralmente
/// </summary>
public class OrderSagaOrchestrator
{
    private readonly IPaymentService _paymentService;
    private readonly IInventoryService _inventoryService;
    private readonly IShippingService _shippingService;
    private readonly IOrderRepository _orderRepository;

    public async Task<Result> ExecuteAsync(CreateOrderCommand command)
    {
        var order = Order.Create(command);

        try
        {
            // Step 1: Process payment
            var paymentResult = await _paymentService.ProcessPaymentAsync(order.Id, order.Total);
            if (!paymentResult.IsSuccess)
            {
                return Result.Failure("Payment failed");
            }

            // Step 2: Reserve inventory
            var inventoryResult = await _inventoryService.ReserveStockAsync(order.Items);
            if (!inventoryResult.IsSuccess)
            {
                // Compensate: Refund payment
                await _paymentService.RefundAsync(order.Id);
                return Result.Failure("Insufficient stock");
            }

            // Step 3: Create shipment
            var shippingResult = await _shippingService.CreateShipmentAsync(order.Id);
            if (!shippingResult.IsSuccess)
            {
                // Compensate: Release stock and refund payment
                await _inventoryService.ReleaseStockAsync(order.Items);
                await _paymentService.RefundAsync(order.Id);
                return Result.Failure("Shipping failed");
            }

            // Success!
            order.Confirm();
            await _orderRepository.SaveAsync(order);

            return Result.Success();
        }
        catch (Exception ex)
        {
            // Compensate all
            await CompensateAsync(order);
            throw;
        }
    }

    private async Task CompensateAsync(Order order)
    {
        await _shippingService.CancelShipmentAsync(order.Id);
        await _inventoryService.ReleaseStockAsync(order.Items);
        await _paymentService.RefundAsync(order.Id);
    }
}
```

---

## Ventajas y Desventajas

###  Ventajas

| Ventaja | Descripción |
|---------|-------------|
| **Escalabilidad** | Procesamiento paralelo y asíncrono |
| **Desacoplamiento** | Servicios no se conocen entre sí |
| **Resiliencia** | Fallos no afectan a otros componentes |
| **Flexibilidad** | Fácil agregar nuevos consumidores |
| **Auditabilidad** | Historial completo de eventos |
| **Real-time** | Reacción instantánea a cambios |

###  Desventajas

| Desventaja | Descripción |
|------------|-------------|
| **Complejidad** | Debugging distribuido es difícil |
| **Eventual Consistency** | No hay consistencia inmediata |
| **Event versioning** | Cambios en eventos requieren migración |
| **Idempotencia** | Eventos pueden duplicarse |
| **Ordenamiento** | Garantizar orden de eventos es complejo |
| **Monitoreo** | Necesitas herramientas especializadas |

---

## Casos de Uso Reales

###  Cuándo Usar EDA

1. **Sistemas de alta escalabilidad**
   - E-commerce (Black Friday, Cyber Monday)
   - Redes sociales (feeds, notificaciones)
   - Streaming (video, audio)

2. **IoT y tiempo real**
   - Smart homes
   - Vehículos autónomos
   - Monitoring industrial

3. **Microservicios desacoplados**
   - Sistemas distribuidos
   - Multi-tenant platforms

4. **Auditoría estricta**
   - Banca, finanzas
   - Healthcare
   - Compliance regulatorio

###  Cuándo NO Usar EDA

1. **CRUD simple**
   - Blog, CMS básico

2. **Consistencia fuerte requerida**
   - Si necesitas transacciones ACID inmediatas

3. **Equipo sin experiencia**
   - Curva de aprendizaje muy alta

---

## Implementación con .NET

### Tecnologías

- **MassTransit**: Abstracción sobre RabbitMQ, Azure Service Bus, Amazon SQS
- **NServiceBus**: Framework comercial para messaging
- **CAP**: Distributed transaction solution
- **EventStore**: Base de datos especializada en Event Sourcing

### Ejemplo con MassTransit

```csharp
// Configuración
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("order-created-queue", e =>
        {
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });
    });
});

// Publisher
public class OrderService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task CreateOrderAsync(CreateOrderCommand command)
    {
        var order = Order.Create(command);
        await _repository.SaveAsync(order);

        await _publishEndpoint.Publish(new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId
        });
    }
}

// Consumer
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly IEmailService _emailService;

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var @event = context.Message;

        await _emailService.SendOrderConfirmationAsync(@event.OrderId);

        _logger.LogInformation("Order confirmation sent for {OrderId}", @event.OrderId);
    }
}
```

---

## Mejores Prácticas

###  DOs

1. **Eventos son inmutables**
   ```csharp
   //  Bien - Inmutable
   public record OrderPlacedEvent(Guid OrderId, DateTime PlacedAt);

   //  Mal - Mutable
   public class OrderPlacedEvent
   {
       public Guid OrderId { get; set; }
   }
   ```

2. **Idempotencia**
   ```csharp
   public class ProcessPaymentHandler : IEventHandler<OrderConfirmedEvent>
   {
       public async Task Handle(OrderConfirmedEvent @event)
       {
           // Check if already processed
           var existing = await _repository.GetByIdempotencyKeyAsync(@event.EventId);
           if (existing != null)
           {
               _logger.LogWarning("Event {EventId} already processed", @event.EventId);
               return; // Idempotent
           }

           // Process...
           await _paymentService.ProcessAsync(@event.OrderId);

           // Mark as processed
           await _repository.SaveIdempotencyKeyAsync(@event.EventId);
       }
   }
   ```

3. **Dead Letter Queue (DLQ)**
   ```csharp
   public class RobustEventHandler : IEventHandler<OrderPlacedEvent>
   {
       private const int MaxRetries = 3;

       public async Task Handle(OrderPlacedEvent @event)
       {
           try
           {
               await ProcessEventAsync(@event);
           }
           catch (Exception ex)
           {
               if (@event.RetryCount >= MaxRetries)
               {
                   // Mover a Dead Letter Queue
                   await _deadLetterQueue.SendAsync(@event, ex);
                   _logger.LogError("Event moved to DLQ after {MaxRetries} retries", MaxRetries);
               }
               else
               {
                   // Reintentar
                   @event.RetryCount++;
                   throw; // Message broker reintentará
               }
           }
       }
   }
   ```

4. **Event Versioning**
   ```csharp
   // V1
   public record OrderPlacedEventV1(Guid OrderId, decimal Total);

   // V2 - Agregar campo
   public record OrderPlacedEventV2(Guid OrderId, decimal Total, string Currency);

   // Handler que soporta ambas versiones
   public class OrderPlacedHandler :
       IEventHandler<OrderPlacedEventV1>,
       IEventHandler<OrderPlacedEventV2>
   {
       public Task Handle(OrderPlacedEventV1 @event)
       {
           return ProcessAsync(@event.OrderId, @event.Total, "USD"); // Default currency
       }

       public Task Handle(OrderPlacedEventV2 @event)
       {
           return ProcessAsync(@event.OrderId, @event.Total, @event.Currency);
       }
   }
   ```

---

## Recursos Adicionales

### Libros
-  **Building Event-Driven Microservices** - Adam Bellemare
-  **Designing Data-Intensive Applications** - Martin Kleppmann
-  **Implementing Domain-Driven Design** - Vaughn Vernon (CQRS & Event Sourcing)

### Herramientas
-  **RabbitMQ** - Message broker
-  **Apache Kafka** - Event streaming
-  **EventStore** - Event sourcing database
-  **MassTransit** - .NET messaging framework

---

## Conclusión

Event-Driven Architecture es poderosa pero compleja. Úsala cuando:
- Necesites alta escalabilidad
- Tengas sistemas distribuidos
- Requieras desacoplamiento total

**No uses EDA para aplicaciones simples CRUD.**

---

**Happy Eventing!** 
