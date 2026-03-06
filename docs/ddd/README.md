# Domain-Driven Design (DDD) - Guía Completa

![DDD](https://martinfowler.com/bliki/images/boundedContext/sketch.png)

##  Índice

1. [¿Qué es DDD?](#qué-es-ddd)
2. [Conceptos Fundamentales](#conceptos-fundamentales)
3. [Patrones Tácticos](#patrones-tácticos)
4. [Patrones Estratégicos](#patrones-estratégicos)
5. [Ventajas y Desventajas](#ventajas-y-desventajas)
6. [Casos de Uso Reales](#casos-de-uso-reales)
7. [Arquitectura Hexagonal con DDD](#arquitectura-hexagonal-con-ddd)
8. [Ejemplo Práctico: E-Commerce](#ejemplo-práctico-e-commerce)
9. [Mejores Prácticas](#mejores-prácticas)

---

## ¿Qué es DDD?

**Domain-Driven Design** es un enfoque para el desarrollo de software complejo propuesto por **Eric Evans** en 2003, que enfatiza:

-  **Enfoque en el dominio del negocio** sobre la tecnología
-  **Lenguaje ubicuo** compartido entre técnicos y expertos del dominio
- ️ **Modelado estratégico** del dominio en bounded contexts
-  **Patrones tácticos** para implementar el modelo

### Filosofía Central

> "El software debe reflejar el modelo mental del dominio del negocio"

DDD no es sobre tecnología, es sobre **entender profundamente el negocio** y expresarlo en código.

---

## Conceptos Fundamentales

### 1. Ubiquitous Language (Lenguaje Ubicuo)

Un lenguaje compartido entre desarrolladores y expertos del dominio.

####  Sin Lenguaje Ubicuo

```csharp
// Desarrolladores: "Usuario"
public class User
{
    public void MakePurchase() { }
}

// Negocio: "Cliente"
//  Desconexión entre código y negocio
```

####  Con Lenguaje Ubicuo

```csharp
// Todos dicen "Cliente"
public class Customer
{
    public void PlaceOrder() { } // En lugar de "MakePurchase"
}

// Todos dicen "Pedido" en lugar de "Transaction"
public class Order { }
```

### 2. Bounded Context (Contexto Delimitado)

División lógica del dominio donde ciertos términos tienen significados específicos.

```
┌─────────────────────────┐  ┌─────────────────────────┐
│   Sales Context         │  │   Shipping Context      │
│                         │  │                         │
│   Customer              │  │   Recipient             │
│   - Name                │  │   - Name                │
│   - Email               │  │   - DeliveryAddress     │
│   - CreditLimit         │  │   - PhoneNumber         │
│                         │  │                         │
│   Order                 │  │   Shipment              │
│   - OrderDate           │  │   - TrackingNumber      │
│   - Items               │  │   - Status              │
│   - Total               │  │   - EstimatedDelivery   │
└─────────────────────────┘  └─────────────────────────┘

      Mismo "Customer" pero diferente significado
```

### 3. Core Domain, Supporting Domains, Generic Domains

```
┌─────────────────────────────────────────────┐
│         E-Commerce System                   │
├─────────────────────────────────────────────┤
│                                             │
│   CORE DOMAIN (Ventaja competitiva)      │
│     - Pricing Engine (precios dinámicos)   │
│     - Recommendation System                │
│                                             │
│   SUPPORTING DOMAINS (Apoyo)             │
│     - Inventory Management                 │
│     - Order Processing                     │
│                                             │
│   GENERIC DOMAINS (Comprar/Reutilizar)   │
│     - Authentication (Auth0, Keycloak)     │
│     - Payment Processing (Stripe)          │
│     - Email Service (SendGrid)             │
└─────────────────────────────────────────────┘
```

---

## Patrones Tácticos

### 1. Entity (Entidad)

Objeto con **identidad única** que persiste a través del tiempo.

```csharp
/// <summary>
/// Entity: Tiene identidad única (Id)
/// Dos customers con el mismo nombre son DIFERENTES si tienen IDs diferentes
/// </summary>
public class Customer : Entity
{
    public CustomerId Id { get; private set; } // Identity
    public string Name { get; private set; }
    public Email Email { get; private set; } // Value Object

    private readonly List<Order> _orders = new();
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    // Constructor privado - solo se puede crear vía factory method
    private Customer(CustomerId id, string name, Email email)
    {
        Id = id;
        Name = name;
        Email = email;
    }

    // Factory method
    public static Customer Create(string name, string email)
    {
        var customerId = CustomerId.Create();
        var emailVo = Email.Create(email);

        return new Customer(customerId, name, emailVo);
    }

    // Domain logic
    public void ChangeName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Name cannot be empty");

        Name = newName;
        AddDomainEvent(new CustomerNameChangedEvent(Id, newName));
    }

    public void PlaceOrder(Order order)
    {
        // Business rule
        if (_orders.Any(o => o.Status == OrderStatus.Pending))
            throw new DomainException("Cannot place new order while having pending orders");

        _orders.Add(order);
        AddDomainEvent(new OrderPlacedEvent(Id, order.Id));
    }
}
```

### 2. Value Object (Objeto de Valor)

Objeto **sin identidad**, definido solo por sus atributos. Inmutable.

```csharp
/// <summary>
/// Value Object: Sin identidad, inmutable
/// Dos emails con el mismo valor son IGUALES
/// </summary>
public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");

        if (!IsValidEmail(email))
            throw new DomainException($"Invalid email: {email}");

        return new Email(email.ToLowerInvariant());
    }

    private static bool IsValidEmail(string email)
    {
        // Validation logic
        return email.Contains("@") && email.Contains(".");
    }

    // Value Objects must override equality
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
}

// Uso
var email1 = Email.Create("john@example.com");
var email2 = Email.Create("john@example.com");

// email1 == email2 (mismo valor)

// Otros ejemplos de Value Objects
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Cannot add money with different currencies");

        return new Money(Amount + other.Amount, Currency);
    }
}

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string Country { get; }
    public string ZipCode { get; }

    // Value object completo
}
```

### 3. Aggregate y Aggregate Root

**Aggregate**: Cluster de entidades y value objects que se tratan como una unidad.
**Aggregate Root**: Entidad principal que controla el acceso al aggregate.

```csharp
/// <summary>
/// Order es el Aggregate Root
/// OrderItem solo es accesible a través de Order
/// </summary>
public class Order : AggregateRoot
{
    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }

    // Colección privada - no se puede modificar desde fuera
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Core

    public static Order Create(CustomerId customerId)
    {
        var order = new Order
        {
            Id = OrderId.Create(),
            CustomerId = customerId,
            Status = OrderStatus.Draft,
            Total = Money.Zero(Currency.USD)
        };

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerId));
        return order;
    }

    /// <summary>
    /// Invariante: Un pedido debe tener al menos un item
    /// </summary>
    public void AddItem(ProductId productId, Money unitPrice, int quantity)
    {
        // Business rule
        if (Status != OrderStatus.Draft)
            throw new DomainException("Cannot add items to a confirmed order");

        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.IncreaseQuantity(quantity);
        }
        else
        {
            var orderItem = OrderItem.Create(Id, productId, unitPrice, quantity);
            _items.Add(orderItem);
        }

        RecalculateTotal();
    }

    public void RemoveItem(OrderItemId itemId)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Cannot remove items from a confirmed order");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new DomainException($"Item {itemId} not found");

        _items.Remove(item);
        RecalculateTotal();

        // Invariante
        if (!_items.Any())
            throw new DomainException("Order must have at least one item");
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException($"Cannot confirm order in {Status} status");

        if (!_items.Any())
            throw new DomainException("Cannot confirm empty order");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id, Total));
    }

    private void RecalculateTotal()
    {
        Total = _items
            .Select(i => i.GetTotal())
            .Aggregate(Money.Zero(Currency.USD), (acc, money) => acc.Add(money));
    }
}

/// <summary>
/// OrderItem NO es un Aggregate Root
/// Solo existe dentro de Order
/// </summary>
public class OrderItem : Entity
{
    public OrderItemId Id { get; private set; }
    public OrderId OrderId { get; private set; }
    public ProductId ProductId { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    private OrderItem() { }

    internal static OrderItem Create(OrderId orderId, ProductId productId, Money unitPrice, int quantity)
    {
        return new OrderItem
        {
            Id = OrderItemId.Create(),
            OrderId = orderId,
            ProductId = productId,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }

    internal void IncreaseQuantity(int amount)
    {
        Quantity += amount;
    }

    public Money GetTotal() => UnitPrice.Multiply(Quantity);
}
```

**Reglas de Aggregates**:
1.  Las referencias externas solo pueden apuntar al Aggregate Root
2.  Las transacciones no deben abarcar múltiples Aggregates
3.  Usar identificadores para referenciar otros Aggregates

```csharp
//  Mal - Referencia directa a entidad interna
public class Customer
{
    public List<OrderItem> FavoriteItems { get; set; }
}

//  Bien - Referencia al Aggregate Root
public class Customer
{
    public List<OrderId> PastOrders { get; set; } // Solo IDs
}
```

### 4. Domain Service (Servicio de Dominio)

Operaciones que **no pertenecen naturalmente a una entidad**.

```csharp
/// <summary>
/// Domain Service: Lógica que involucra múltiples aggregates
/// o que no pertenece claramente a ninguna entidad
/// </summary>
public interface IPricingService
{
    Money CalculateOrderTotal(Order order, Customer customer, Promotion? promotion);
}

public class PricingService : IPricingService
{
    public Money CalculateOrderTotal(Order order, Customer customer, Promotion? promotion)
    {
        var subtotal = order.Items
            .Select(i => i.GetTotal())
            .Aggregate(Money.Zero(Currency.USD), (acc, m) => acc.Add(m));

        // Descuento VIP
        if (customer.IsVip())
        {
            subtotal = subtotal.ApplyDiscount(0.10m); // 10% off
        }

        // Descuento por promoción
        if (promotion != null && promotion.IsActive())
        {
            subtotal = promotion.ApplyTo(subtotal);
        }

        // Impuestos
        var tax = subtotal.Multiply(0.15m);

        return subtotal.Add(tax);
    }
}

// Uso en Application Layer
public class ConfirmOrderCommandHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPricingService _pricingService; // Domain Service

    public async Task<Result> Handle(ConfirmOrderCommand command)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId);
        var customer = await _customerRepository.GetByIdAsync(order.CustomerId);

        var total = _pricingService.CalculateOrderTotal(order, customer, null);

        order.SetTotal(total);
        order.Confirm();

        await _orderRepository.UpdateAsync(order);
        return Result.Success();
    }
}
```

### 5. Domain Events

Eventos que representan algo que **ya sucedió** en el dominio.

```csharp
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class OrderPlacedEvent : DomainEvent
{
    public OrderId OrderId { get; }
    public CustomerId CustomerId { get; }
    public Money Total { get; }

    public OrderPlacedEvent(OrderId orderId, CustomerId customerId, Money total)
    {
        OrderId = orderId;
        CustomerId = customerId;
        Total = total;
    }
}

// En la entidad
public class Order : AggregateRoot
{
    public void Confirm()
    {
        // ... lógica

        // Levantar evento
        AddDomainEvent(new OrderPlacedEvent(Id, CustomerId, Total));
    }
}

// Event Handler
public class OrderPlacedEventHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly IEmailService _emailService;

    public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        // Enviar email de confirmación
        await _emailService.SendOrderConfirmationAsync(notification.CustomerId, notification.OrderId);

        // Decrementar stock
        // ...

        // Iniciar proceso de facturación
        // ...
    }
}
```

### 6. Repository (Repositorio)

Abstracción para **persistir y recuperar Aggregates**.

```csharp
/// <summary>
/// Repository para Order Aggregate
/// Solo expone operaciones relevantes para el dominio
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<Order>> GetCustomerOrdersAsync(CustomerId customerId);

    Task AddAsync(Order order);
    Task UpdateAsync(Order order);

    // NO hay Delete - usamos soft delete en el aggregate
    // NO exponemos IQueryable - eso es responsabilidad de queries
}

// Implementación en Infrastructure
public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<Order?> GetByIdAsync(OrderId id)
    {
        return await _context.Orders
            .Include(o => o.Items) // Cargar aggregate completo
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        // Eventos se procesan en SaveChanges
    }
}
```

---

## Patrones Estratégicos

### 1. Context Mapping (Mapeo de Contextos)

Define las **relaciones entre Bounded Contexts**.

```
┌─────────────────────────┐         ┌─────────────────────────┐
│   Sales Context         │         │   Inventory Context     │
│   (Upstream)            │────────▶│   (Downstream)          │
│                         │  ACL    │                         │
│   Order                 │         │   StockItem             │
│   - OrderId             │         │   - SKU                 │
│   - Items (ProductId)   │         │   - Quantity            │
└─────────────────────────┘         └─────────────────────────┘

ACL = Anti-Corruption Layer (protege el downstream)
```

**Tipos de relaciones**:

1. **Shared Kernel**: Código compartido (usar con precaución)
2. **Customer-Supplier**: Downstream depende de upstream
3. **Conformist**: Downstream acepta modelo del upstream sin cambios
4. **Anti-Corruption Layer (ACL)**: Traduce entre contextos
5. **Open Host Service**: API pública del contexto
6. **Published Language**: Formato estándar (JSON, XML)

### 2. Anti-Corruption Layer (ACL)

```csharp
// Contexto externo (Legacy System)
public class LegacyCustomerDto
{
    public int cust_id { get; set; }
    public string cust_name { get; set; }
    public string addr_line1 { get; set; }
    // ... horrible modelo legacy
}

// ACL: Traduce legacy a nuestro modelo
public interface ILegacyCustomerAdapter
{
    Task<Customer> GetCustomerAsync(int legacyId);
}

public class LegacyCustomerAdapter : ILegacyCustomerAdapter
{
    private readonly ILegacySystemClient _legacyClient;

    public async Task<Customer> GetCustomerAsync(int legacyId)
    {
        var legacyCustomer = await _legacyClient.GetCustomerAsync(legacyId);

        // Traducción
        return Customer.Create(
            name: legacyCustomer.cust_name,
            email: ExtractEmail(legacyCustomer),
            address: new Address(
                street: legacyCustomer.addr_line1,
                // ...
            )
        );
    }
}
```

---

## Ventajas y Desventajas

###  Ventajas

| Ventaja | Descripción |
|---------|-------------|
| **Alineación con el negocio** | El código refleja el modelo mental del dominio |
| **Lenguaje ubicuo** | Comunicación fluida entre técnicos y negocio |
| **Mantenibilidad** | Lógica de negocio centralizada y expresiva |
| **Testabilidad** | Aggregates con lógica rica son fáciles de testear |
| **Escalabilidad** | Bounded Contexts permiten escalar independientemente |
| **Flexibilidad** | Cambios en un contexto no afectan a otros |

###  Desventajas

| Desventaja | Descripción |
|------------|-------------|
| **Curva de aprendizaje muy alta** | Requiere entender muchos conceptos |
| **Overhead inicial** | Más tiempo en diseño y modelado |
| **Requiere expertos del dominio** | Difícil sin acceso a stakeholders |
| **Complejidad** | Puede ser excesivo para dominios simples |
| **No apto para CRUD** | Demasiado para aplicaciones simples |

---

## Casos de Uso Reales

###  Cuándo Usar DDD

1. **Dominios complejos con lógica de negocio rica**
   - Sistemas financieros (banca, seguros, inversiones)
   - E-commerce con reglas de precios complejas
   - Healthcare (historiales médicos, recetas)
   - Logística y supply chain

2. **Proyectos de larga duración**
   - Sistemas legacy que evolucionan constantemente
   - Plataformas empresariales (10+ años de vida)

3. **Múltiples subdominios**
   - ERP con ventas, inventario, contabilidad, RH
   - Sistemas multi-tenant con diferentes reglas por tenant

###  Cuándo NO Usar DDD

1. **Aplicaciones CRUD simples**
   - Blog, CMS básico
   - Aplicaciones de catálogo

2. **Proyectos con timeline corto**
   - MVPs, prototipos
   - Proyectos de 3-6 meses

3. **Sin acceso a expertos del dominio**
   - Si no puedes hablar con quien entiende el negocio

---

## Ejemplo Práctico: E-Commerce

### Identificación de Bounded Contexts

```
E-Commerce System
│
├── Sales Context (Core Domain)
│   ├── Customer
│   ├── Order
│   ├── Product Catalog
│   └── Pricing
│
├── Inventory Context (Supporting)
│   ├── StockItem
│   ├── Warehouse
│   └── Supplier
│
├── Shipping Context (Supporting)
│   ├── Shipment
│   ├── Carrier
│   └── Tracking
│
└── Billing Context (Supporting)
    ├── Invoice
    ├── Payment
    └── Receipt
```

### Implementación del Core Domain

Ver código de ejemplo en `/src/DDD/`

---

## Mejores Prácticas

###  DOs

1. **Empieza con Event Storming**
   - Reúne a expertos del dominio
   - Identifica eventos del dominio
   - Descubre Aggregates y Bounded Contexts

2. **Mantén Aggregates pequeños**
   ```csharp
   //  Bien - Aggregate pequeño
   public class Order : AggregateRoot
   {
       public List<OrderItemId> ItemIds { get; set; } // Solo IDs
   }

   //  Mal - Aggregate gigante
   public class Order : AggregateRoot
   {
       public List<OrderItem> Items { get; set; }
       public Customer Customer { get; set; } // Otro aggregate!
       public List<Shipment> Shipments { get; set; }
       public List<Payment> Payments { get; set; }
   }
   ```

3. **Usa Value Objects agresivamente**
   ```csharp
   //  Bien
   public class Customer
   {
       public Email Email { get; set; }
       public Money CreditLimit { get; set; }
       public Address ShippingAddress { get; set; }
   }

   //  Mal - Primitive obsession
   public class Customer
   {
       public string Email { get; set; }
       public decimal CreditLimit { get; set; }
       public string StreetAddress { get; set; }
   }
   ```

4. **Modela explícitamente las reglas de negocio**
   ```csharp
   public class Order
   {
       public void ApplyDiscount(Discount discount)
       {
           // Regla: No se pueden aplicar múltiples descuentos
           if (_appliedDiscounts.Any())
               throw new DomainException("Only one discount per order");

           // Regla: Descuento solo en pedidos confirmados
           if (Status != OrderStatus.Confirmed)
               throw new DomainException("Can only apply discount to confirmed orders");

           _appliedDiscounts.Add(discount);
       }
   }
   ```

###  DON'Ts

1. **No uses DDD para todo**
   - No necesitas Value Objects para un TODO app

2. **No cruces límites de Aggregates en transacciones**
   ```csharp
   //  Mal
   public async Task ProcessOrder(OrderId orderId)
   {
       using var transaction = await _dbContext.Database.BeginTransactionAsync();

       var order = await _orderRepository.GetByIdAsync(orderId);
       var customer = await _customerRepository.GetByIdAsync(order.CustomerId);
       var inventory = await _inventoryRepository.GetByProductAsync(order.ProductId);

       // Modificando 3 aggregates en una transacción
       order.Confirm();
       customer.DecrementCredit(order.Total);
       inventory.ReserveStock(order.Quantity);

       await transaction.CommitAsync();
   }

   //  Bien - Eventual consistency
   public async Task ProcessOrder(OrderId orderId)
   {
       var order = await _orderRepository.GetByIdAsync(orderId);
       order.Confirm(); // Solo modifica un aggregate

       // Levanta evento
       order.AddDomainEvent(new OrderConfirmedEvent(orderId));

       await _orderRepository.UpdateAsync(order);

       // Otros aggregates se actualizan vía event handlers
   }
   ```

---

## Recursos Adicionales

### Libros Esenciales
-  **Domain-Driven Design** - Eric Evans (El libro azul)
-  **Implementing Domain-Driven Design** - Vaughn Vernon (El libro rojo)
-  **Domain-Driven Design Distilled** - Vaughn Vernon (Versión corta)

### Cursos
-  **Domain-Driven Design Fundamentals** - Julie Lerman & Steve Smith (Pluralsight)
-  **DDD Europe Conference** - YouTube

### Herramientas
-  **EventStorming** - Técnica de modelado colaborativo
-  **Context Mapper** - DSL para modelar bounded contexts

---

## Conclusión

DDD no es solo sobre código, es sobre **entender profundamente el negocio** y reflejarlo en software mantenible.

**Recuerda**:
- Empieza pequeño, no necesitas implementar todo DDD de una vez
- El lenguaje ubicuo es más importante que los patrones tácticos
- DDD es una inversión - solo úsalo si el dominio lo justifica

---

**Happy Domain Modeling!** 
