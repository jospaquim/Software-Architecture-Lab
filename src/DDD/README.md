# Domain-Driven Design (DDD) Implementation

Implementación completa de Domain-Driven Design con C# .NET 8, enfocada en un sistema de e-commerce con múltiples bounded contexts.

## ️ Bounded Contexts

Este proyecto demuestra DDD con 3 bounded contexts:

### 1. Sales Context (CORE DOMAIN)
El contexto principal donde ocurre la lógica de negocio crítica.

**Aggregates:**
- `Order` (Aggregate Root)
  - `OrderItem` (Entity within aggregate)
  - Business Rules:
    - Order must have at least one item
    - Cannot modify confirmed orders
    - Single discount per order
    - Automatic total calculation

**Value Objects:**
- `Money` - Immutable money with currency
- `Email` - Email with validation
- `Address` - Physical address
- `CustomerId`, `OrderId`, `ProductId` (Strongly Typed IDs)

**Domain Services:**
- `PricingService` - Complex pricing calculations
- Discount policies
- Tax calculations

**Specifications:**
- `OrderByCustomerSpecification`
- `OrderByStatusSpecification`
- `OrderAboveAmountSpecification`
- Composable with And/Or/Not

### 2. Catalog Context
Gestión de productos y categorías.

**Aggregates:**
- `Product` (Aggregate Root)
- `Category`

### 3. Shipping Context
Gestión de envíos y tracking.

**Aggregates:**
- `Shipment` (Aggregate Root)

##  Estructura

```
DDD/
├── Sales/                      # Sales Bounded Context (Core Domain)
│   ├── Domain/
│   │   ├── Aggregates/
│   │   │   ├── AggregateRoot.cs
│   │   │   └── Order/
│   │   │       ├── Order.cs              # Aggregate Root
│   │   │       ├── OrderItem.cs          # Entity
│   │   │       └── OrderEvents.cs        # Domain Events
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs
│   │   │   ├── Email.cs
│   │   │   ├── Address.cs
│   │   │   └── StronglyTypedIds.cs
│   │   ├── Services/
│   │   │   └── PricingService.cs         # Domain Service
│   │   └── Specifications/
│   │       ├── Specification.cs          # Base Specification
│   │       └── OrderSpecifications.cs    # Order Specifications
│   ├── Application/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── DTOs/
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   └── Repositories/
│   └── API/
│       └── Controllers/
├── Catalog/                    # Catalog Bounded Context
└── Shipping/                   # Shipping Bounded Context
```

##  Patrones DDD Implementados

### 1. **Entities**
Objetos con identidad única que persiste en el tiempo.

```csharp
public sealed class Order : AggregateRoot
{
    public OrderId Id { get; private set; }  // Identity
    // ...
}
```

### 2. **Value Objects**
Objetos inmutables definidos por sus atributos, sin identidad.

```csharp
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money Add(Money other) { /* ... */ }
}
```

### 3. **Aggregates**
Cluster de entidades tratadas como una unidad de consistencia.

```csharp
// Order es el Aggregate Root
// OrderItem solo es accesible a través de Order
public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();

    public void AddItem(ProductId productId, Money price, int quantity)
    {
        // Business rules enforced here
    }
}
```

### 4. **Domain Services**
Lógica que no pertenece naturalmente a ninguna entidad.

```csharp
public class PricingService : IPricingService
{
    public Money CalculateOrderTotal(Order order, CustomerType type)
    {
        // Complex calculation involving multiple aggregates
    }
}
```

### 5. **Specifications**
Encapsula lógica de negocio reutilizable para queries.

```csharp
var spec = new OrderByCustomerSpecification(customerId)
    .And(new OrderAboveAmountSpecification(Money.Create(1000, Currency.USD)));

var orders = await repository.FindAsync(spec);
```

### 6. **Domain Events**
Hechos que ya ocurrieron en el dominio.

```csharp
public sealed record OrderConfirmedEvent(
    OrderId OrderId,
    Money Total
) : DomainEvent;
```

### 7. **Strongly Typed IDs**
Evita primitive obsession con IDs tipados.

```csharp
public sealed record OrderId(Guid Value) : EntityId(Value);
public sealed record CustomerId(Guid Value) : EntityId(Value);

// Type safety!
void ProcessOrder(OrderId orderId) { }
// ProcessOrder(customerId); //  Compiler error!
```

##  Reglas de Negocio Implementadas

### Order Aggregate

1. **Invariante: Orden debe tener al menos un item**
   ```csharp
   public void RemoveItem(OrderItemId itemId)
   {
       _items.Remove(item);

       if (!_items.Any())
           throw new InvalidOperationException("Order must have at least one item");
   }
   ```

2. **Regla: No se puede modificar una orden confirmada**
   ```csharp
   public void AddItem(...)
   {
       if (Status != OrderStatus.Draft)
           throw new InvalidOperationException("Cannot modify confirmed order");
   }
   ```

3. **Regla: Solo un descuento por orden**
   ```csharp
   public void ApplyDiscount(Money discount)
   {
       if (Discount.Amount > 0)
           throw new InvalidOperationException("Order already has a discount");
   }
   ```

4. **Regla: Descuento no puede exceder el subtotal**
   ```csharp
   if (discountAmount > Subtotal)
       throw new InvalidOperationException("Discount cannot exceed subtotal");
   ```

##  Ventajas de Esta Implementación

| Ventaja | Implementación |
|---------|----------------|
| **Rich Domain Model** | Lógica de negocio en entidades, no en servicios |
| **Immutability** | Value Objects son inmutables |
| **Type Safety** | Strongly Typed IDs evitan bugs |
| **Testability** | Aggregates se pueden testear sin BD |
| **Business Rules** | Encapsuladas y expresivas |
| **Reusability** | Specifications composables |

##  Testing

```csharp
[Fact]
public void Order_AddItem_ShouldIncreaseTotal()
{
    // Arrange
    var order = Order.Create(customerId, address);
    var price = Money.Create(100, Currency.USD);

    // Act
    order.AddItem(productId, "Product", price, 2);

    // Assert
    Assert.Equal(200, order.Subtotal.Amount);
}

[Fact]
public void Order_ConfirmWithoutItems_ShouldThrowException()
{
    // Arrange
    var order = Order.Create(customerId, address);

    // Act & Assert
    Assert.Throws<InvalidOperationException>(() => order.Confirm());
}
```

##  Comparación: DDD vs Clean Architecture

| Aspecto | DDD | Clean Architecture |
|---------|-----|-------------------|
| **Enfoque** | Domain-centric | Layer-centric |
| **Complejidad** | Alta | Media |
| **Value Objects** |  Extensivo |  Opcional |
| **Aggregates** |  Core concept |  No existe |
| **Bounded Contexts** |  Explícitos |  Implícitos |
| **Domain Services** |  Comunes | ️ Menos común |
| **Specifications** |  Patrón principal |  Raro |
| **Mejor para** | Dominios complejos | APIs generales |

##  Patrones DDD No Implementados (pero documentados)

Los siguientes patrones están documentados en `/docs/ddd/README.md` pero no implementados en código:

- Event Sourcing (ver EDA implementation)
- CQRS completo (ver EDA implementation)
- Sagas
- Anti-Corruption Layer
- Context Mapping entre bounded contexts

##  Referencias

- [Documentación DDD completa](../../docs/ddd/README.md)
- [Principios SOLID](../../docs/principles/SOLID.md)
- [Clean Code](../../docs/principles/CleanCode.md)

---

**Este proyecto demuestra DDD con ejemplos prácticos y código de producción.**
