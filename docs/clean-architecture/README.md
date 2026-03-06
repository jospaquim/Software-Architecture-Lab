# Clean Architecture - Guía Completa

![Clean Architecture Diagram](https://blog.cleancoder.com/uncle-bob/images/2012-08-13-the-clean-architecture/CleanArchitecture.jpg)

##  Índice

1. [¿Qué es Clean Architecture?](#qué-es-clean-architecture)
2. [Principios Fundamentales](#principios-fundamentales)
3. [Capas de la Arquitectura](#capas-de-la-arquitectura)
4. [Ventajas y Desventajas](#ventajas-y-desventajas)
5. [Casos de Uso Reales](#casos-de-uso-reales)
6. [Estructura del Proyecto](#estructura-del-proyecto)
7. [Flujo de Datos](#flujo-de-datos)
8. [Patrones Implementados](#patrones-implementados)
9. [Guía de Implementación](#guía-de-implementación)
10. [Mejores Prácticas](#mejores-prácticas)

---

## ¿Qué es Clean Architecture?

Clean Architecture es un patrón arquitectónico propuesto por **Robert C. Martin (Uncle Bob)** que enfatiza la **separación de responsabilidades** y la **independencia de frameworks, UI y bases de datos**.

### Objetivos Principales

-  **Independencia de Frameworks**: La arquitectura no depende de bibliotecas específicas
-  **Testeable**: La lógica de negocio puede probarse sin UI, base de datos o servicios externos
-  **Independencia de UI**: La UI puede cambiar sin afectar la lógica de negocio
-  **Independencia de Base de Datos**: Puedes cambiar de SQL Server a PostgreSQL sin cambiar lógica
-  **Independencia de Agentes Externos**: La lógica de negocio no conoce nada del mundo exterior

---

## Principios Fundamentales

### 1. Dependency Rule (Regla de Dependencia)

**"Las dependencias del código fuente deben apuntar solo hacia adentro, hacia políticas de alto nivel"**

```
┌─────────────────────────────────────┐
│         Frameworks & Drivers        │  ← Capa externa
├─────────────────────────────────────┤
│     Interface Adapters (API)        │
├─────────────────────────────────────┤
│   Application Business Rules        │
├─────────────────────────────────────┤
│   Enterprise Business Rules (Core)  │  ← Núcleo (independiente)
└─────────────────────────────────────┘

Dependencies flow: → → → (hacia adentro)
```

### 2. SOLID Principles

Todos los principios SOLID se aplican en Clean Architecture:

- **S**ingle Responsibility Principle
- **O**pen/Closed Principle
- **L**iskov Substitution Principle
- **I**nterface Segregation Principle
- **D**ependency Inversion Principle

---

## Capas de la Arquitectura

###  1. Domain Layer (Capa de Dominio)

**Responsabilidad**: Lógica de negocio fundamental, entidades y reglas de negocio.

**Características**:
-  No tiene dependencias externas
-  Solo código .NET puro
-  Entidades con lógica de negocio
-  Interfaces de repositorios
-  Domain Events
-  Value Objects
-  Enumeraciones

**Ejemplo**:
```csharp
// src/CleanArchitecture/Domain/Entities/Order.cs
public class Order : BaseEntity
{
    public string OrderNumber { get; set; }
    public decimal Total { get; private set; }

    // Business rule
    public void AddItem(Product product, int quantity)
    {
        if (Status >= OrderStatus.Shipped)
            throw new InvalidOperationException("Cannot modify shipped order");

        // Logic...
    }
}
```

**Archivos**:
- `Domain/Entities/` - Entidades de negocio
- `Domain/Interfaces/` - Abstracciones de repositorios
- `Domain/Common/` - Clases base
- `Domain/Enums/` - Enumeraciones

---

###  2. Application Layer (Capa de Aplicación)

**Responsabilidad**: Casos de uso, orquestación, DTOs, validaciones.

**Características**:
-  Depende solo de Domain
-  Implementa CQRS con MediatR
-  Contiene Commands y Queries
-  Validaciones con FluentValidation
-  Mapping con AutoMapper
-  No conoce detalles de infraestructura

**Ejemplo**:
```csharp
// Command
public record CreateOrderCommand(
    int CustomerId,
    List<OrderItemDto> Items
) : IRequest<Result<OrderDto>>;

// Handler
public class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, Result<OrderDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<OrderDto>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        // Orchestrate business logic
        var order = new Order { /* ... */ };
        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return Result<OrderDto>.Success(orderDto);
    }
}
```

**Archivos**:
- `Application/UseCases/` - Commands y Queries
- `Application/DTOs/` - Data Transfer Objects
- `Application/Validation/` - Validadores
- `Application/Mapping/` - Perfiles de AutoMapper

---

###  3. Infrastructure Layer (Capa de Infraestructura)

**Responsabilidad**: Implementaciones concretas de abstracciones del dominio.

**Características**:
-  Depende de Domain y Application
-  Entity Framework Core
-  Repositorios concretos
-  Servicios externos (Email, SMS, etc.)
-  Configuración de base de datos

**Ejemplo**:
```csharp
// Infrastructure/Repositories/OrderRepository.cs
public class OrderRepository : Repository<Order>, IOrderRepository
{
    public async Task<Order?> GetOrderWithItemsAsync(int orderId)
    {
        return await _dbSet
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }
}

// Infrastructure/Persistence/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();
}
```

**Archivos**:
- `Infrastructure/Persistence/` - DbContext, Configurations
- `Infrastructure/Repositories/` - Implementaciones de repositorios
- `Infrastructure/Services/` - Servicios externos

---

###  4. API/Presentation Layer (Capa de Presentación)

**Responsabilidad**: Exposición de la API, controllers, middleware, autenticación.

**Características**:
-  ASP.NET Core Web API
-  Controllers
-  Middleware
-  Authentication (JWT)
-  Rate Limiting
-  Swagger/OpenAPI
-  CORS

**Ejemplo**:
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrdersController : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var command = new CreateOrderCommand(dto.CustomerId, dto.Items);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }
}
```

**Archivos**:
- `API/Controllers/` - REST API Controllers
- `API/Middleware/` - Middleware personalizado
- `API/Filters/` - Filtros de acción

---

## Ventajas y Desventajas

###  Ventajas

| Ventaja | Descripción |
|---------|-------------|
| **Alta Testabilidad** | Cada capa puede probarse independientemente con mocks |
| **Mantenibilidad** | Código organizado y fácil de mantener |
| **Flexibilidad** | Cambiar de framework, UI o BD es sencillo |
| **Escalabilidad** | Fácil agregar nuevas funcionalidades sin romper código existente |
| **Separación de Responsabilidades** | Cada capa tiene un propósito claro |
| **Independencia Tecnológica** | El core no depende de librerías externas |
| **Equipos Grandes** | Diferentes equipos pueden trabajar en diferentes capas |

###  Desventajas

| Desventaja | Descripción |
|------------|-------------|
| **Complejidad Inicial** | Curva de aprendizaje pronunciada |
| **Más Código** | Más archivos, clases e interfaces |
| **Overhead** | Para proyectos pequeños puede ser excesivo |
| **Performance** | Múltiples capas pueden agregar overhead (mínimo) |
| **Mapeo Tedioso** | Mapear entre entidades y DTOs requiere esfuerzo |

---

## Casos de Uso Reales

###  Cuándo Usar Clean Architecture

1. **Aplicaciones Empresariales de Larga Duración**
   - ERP, CRM, sistemas financieros
   - Proyectos que vivirán 5+ años
   - Equipos grandes (5+ desarrolladores)

2. **Microservicios con Lógica Compleja**
   - Servicios con lógica de negocio rica
   - Necesidad de alta testabilidad
   - Múltiples integraciones externas

3. **Sistemas que Requieren Flexibilidad**
   - Posibilidad de cambiar de base de datos
   - Múltiples clientes (Web, Mobile, Desktop)
   - Necesidad de versionar la API

###  Cuándo NO Usar Clean Architecture

1. **Prototipos y MVPs**
   - Necesitas validar una idea rápidamente
   - El proyecto puede no continuar

2. **Aplicaciones CRUD Simples**
   - Sin lógica de negocio compleja
   - Solo operaciones básicas de BD

3. **Proyectos Pequeños**
   - 1-2 desarrolladores
   - Duración estimada < 6 meses

---

## Estructura del Proyecto

```
CleanArchitecture/
├── Domain/                          # ← Núcleo (sin dependencias)
│   ├── Entities/
│   │   ├── Customer.cs
│   │   ├── Product.cs
│   │   └── Order.cs
│   ├── Interfaces/
│   │   ├── IRepository.cs
│   │   ├── ICustomerRepository.cs
│   │   └── IUnitOfWork.cs
│   ├── Common/
│   │   └── BaseEntity.cs
│   └── Enums/
│       └── OrderStatus.cs
│
├── Application/                     # ← Casos de uso
│   ├── UseCases/
│   │   ├── Customers/
│   │   │   ├── Commands/
│   │   │   │   └── CreateCustomer/
│   │   │   │       ├── CreateCustomerCommand.cs
│   │   │   │       ├── CreateCustomerCommandHandler.cs
│   │   │   │       └── CreateCustomerCommandValidator.cs
│   │   │   └── Queries/
│   │   │       └── GetCustomerById/
│   │   │           ├── GetCustomerByIdQuery.cs
│   │   │           └── GetCustomerByIdQueryHandler.cs
│   │   └── Orders/
│   ├── DTOs/
│   │   ├── CustomerDto.cs
│   │   └── OrderDto.cs
│   ├── Mapping/
│   │   └── MappingProfile.cs
│   └── Common/
│       └── Result.cs
│
├── Infrastructure/                  # ← Implementaciones
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   └── Configurations/
│   │       ├── CustomerConfiguration.cs
│   │       └── OrderConfiguration.cs
│   ├── Repositories/
│   │   ├── Repository.cs
│   │   ├── CustomerRepository.cs
│   │   └── UnitOfWork.cs
│   └── Services/
│       └── EmailService.cs
│
└── API/                            # ← Presentación
    ├── Controllers/
    │   ├── BaseApiController.cs
    │   ├── CustomersController.cs
    │   └── OrdersController.cs
    ├── Middleware/
    │   └── ExceptionHandlingMiddleware.cs
    ├── Program.cs
    ├── appsettings.json
    └── Dockerfile
```

---

## Flujo de Datos

### Request Flow (de afuera hacia adentro)

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ HTTP Request
       ▼
┌─────────────────────────┐
│   API Controller        │  ← Validate, Authorize
└───────────┬─────────────┘
            │ MediatR Command/Query
            ▼
┌─────────────────────────┐
│   Application Handler   │  ← Orchestrate
└───────────┬─────────────┘
            │ Domain entities
            ▼
┌─────────────────────────┐
│   Domain Layer          │  ← Business Rules
└───────────┬─────────────┘
            │ Repository Interface
            ▼
┌─────────────────────────┐
│   Infrastructure Repo   │  ← Data Access
└───────────┬─────────────┘
            │ EF Core
            ▼
┌─────────────────────────┐
│   Database              │
└─────────────────────────┘
```

### Response Flow (de adentro hacia afuera)

```
Database → Infrastructure → Domain → Application → API → Client
```

---

## Patrones Implementados

### 1. CQRS (Command Query Responsibility Segregation)

Separación entre operaciones de **lectura** (Queries) y **escritura** (Commands).

```csharp
// Command (Write)
public record CreateOrderCommand(...) : IRequest<Result<OrderDto>>;

// Query (Read)
public record GetOrderByIdQuery(int Id) : IRequest<Result<OrderDto>>;
```

### 2. Mediator Pattern

Desacopla la comunicación entre componentes usando MediatR.

```csharp
// Controller no conoce el handler directamente
var command = new CreateOrderCommand(...);
var result = await _mediator.Send(command);
```

### 3. Repository Pattern

Abstrae el acceso a datos.

```csharp
public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetOrderWithItemsAsync(int orderId);
}
```

### 4. Unit of Work Pattern

Coordina el trabajo de múltiples repositorios en una transacción.

```csharp
await _unitOfWork.BeginTransactionAsync();
await _unitOfWork.Orders.AddAsync(order);
await _unitOfWork.Customers.UpdateAsync(customer);
await _unitOfWork.CommitTransactionAsync();
```

### 5. Result Pattern

Manejo de errores sin excepciones para lógica de negocio.

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string Error { get; }
}
```

### 6. Dependency Injection

Inyección de dependencias nativa de .NET.

```csharp
public class CreateOrderCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
}
```

---

## Guía de Implementación

### Paso 1: Crear Entidades del Dominio

```csharp
// Domain/Entities/Product.cs
public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }

    // Business logic
    public void DecreaseStock(int quantity)
    {
        if (quantity > Stock)
            throw new InvalidOperationException("Insufficient stock");

        Stock -= quantity;
    }
}
```

### Paso 2: Definir Interfaces de Repositorio

```csharp
// Domain/Interfaces/IProductRepository.cs
public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySkuAsync(string sku);
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
}
```

### Paso 3: Crear DTOs

```csharp
// Application/DTOs/ProductDto.cs
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
```

### Paso 4: Implementar Casos de Uso

```csharp
// Application/UseCases/Products/Commands/CreateProduct/CreateProductCommand.cs
public record CreateProductCommand(
    string Name,
    decimal Price,
    int Stock
) : IRequest<Result<ProductDto>>;

// Handler
public class CreateProductCommandHandler
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public async Task<Result<ProductDto>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            Stock = request.Stock
        };

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<ProductDto>(product);
        return Result<ProductDto>.Success(dto);
    }
}
```

### Paso 5: Implementar Repositorio

```csharp
// Infrastructure/Repositories/ProductRepository.cs
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Sku == sku);
    }
}
```

### Paso 6: Crear Controller

```csharp
// API/Controllers/ProductsController.cs
[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : BaseApiController
{
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
    {
        var command = new CreateProductCommand(dto.Name, dto.Price, dto.Stock);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }
}
```

---

## Mejores Prácticas

###  DOs

1. **Mantén el Domain puro**
   ```csharp
   //  Bien - Solo lógica de negocio
   public class Order
   {
       public void AddItem(Product product, int quantity)
       {
           // Business rules
       }
   }
   ```

2. **Usa validadores explícitos**
   ```csharp
   public class CreateProductValidator : AbstractValidator<CreateProductCommand>
   {
       public CreateProductValidator()
       {
           RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
           RuleFor(x => x.Price).GreaterThan(0);
       }
   }
   ```

3. **Retorna DTOs, nunca entidades**
   ```csharp
   //  Bien
   public async Task<ProductDto> GetProduct(int id)

   //  Mal
   public async Task<Product> GetProduct(int id)
   ```

4. **Usa CancellationToken**
   ```csharp
   public async Task<Product> GetByIdAsync(
       int id,
       CancellationToken cancellationToken = default)
   ```

###  DON'Ts

1. **No pongas lógica de negocio en Controllers**
   ```csharp
   //  Mal
   [HttpPost]
   public async Task<IActionResult> CreateOrder([FromBody] OrderDto dto)
   {
       var order = new Order();
       order.Total = dto.Items.Sum(i => i.Price * i.Quantity);
       // ...
   }
   ```

2. **No hagas referencia a Infrastructure desde Application**
   ```csharp
   //  Mal - Application no debe conocer EF Core
   public class OrderService
   {
       private readonly ApplicationDbContext _context;
   }
   ```

3. **No uses Entity Framework en Domain**
   ```csharp
   //  Mal
   using Microsoft.EntityFrameworkCore;

   namespace CleanArchitecture.Domain.Entities;
   ```

---

## Recursos Adicionales

### Libros
-  **Clean Architecture** - Robert C. Martin
-  **Domain-Driven Design** - Eric Evans
-  **Implementing Domain-Driven Design** - Vaughn Vernon

### Videos
-  [Clean Architecture with ASP.NET Core](https://www.youtube.com/jasontaylor)
-  [SOLID Principles](https://www.pluralsight.com)

### Repositorios de Ejemplo
-  [Jason Taylor's Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)
-  [Microsoft eShopOnWeb](https://github.com/dotnet-architecture/eShopOnWeb)

---

## Conclusión

Clean Architecture es una inversión a largo plazo. Aunque requiere más esfuerzo inicial, los beneficios en **mantenibilidad, testabilidad y flexibilidad** son invaluables para proyectos de mediano a largo plazo.

**Recuerda**: La arquitectura no es dogma. Adáptala a las necesidades de tu proyecto y equipo.

---

**Happy Coding!** 
