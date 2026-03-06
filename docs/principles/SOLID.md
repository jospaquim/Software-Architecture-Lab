# Principios SOLID

Los principios SOLID son cinco principios fundamentales de diseño orientado a objetos que hacen que el software sea más mantenible, flexible y escalable.

##  Los 5 Principios

### 1. Single Responsibility Principle (SRP)
**"Una clase debe tener una, y solo una, razón para cambiar"**

####  Mal Ejemplo
```csharp
public class UserService
{
    public void CreateUser(User user)
    {
        // Validación
        if (string.IsNullOrEmpty(user.Email))
            throw new ArgumentException("Email es requerido");

        // Guardar en base de datos
        using var connection = new SqlConnection(_connectionString);
        connection.Execute("INSERT INTO Users...");

        // Enviar email de bienvenida
        var smtpClient = new SmtpClient("smtp.server.com");
        smtpClient.Send(new MailMessage(...));

        // Logging
        File.AppendAllText("log.txt", $"Usuario creado: {user.Email}");
    }
}
```

**Problemas:**
- La clase hace demasiadas cosas: validación, persistencia, envío de emails, logging
- Múltiples razones para cambiar
- Difícil de testear
- Alta acoplamiento

####  Buen Ejemplo
```csharp
// Responsabilidad: Validación
public class UserValidator
{
    public ValidationResult Validate(User user)
    {
        if (string.IsNullOrEmpty(user.Email))
            return ValidationResult.Failure("Email es requerido");

        if (!IsValidEmail(user.Email))
            return ValidationResult.Failure("Email inválido");

        return ValidationResult.Success();
    }
}

// Responsabilidad: Persistencia
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
}

// Responsabilidad: Notificaciones
public class EmailNotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;

    public async Task SendWelcomeEmailAsync(User user)
    {
        var message = new EmailMessage
        {
            To = user.Email,
            Subject = "Bienvenido!",
            Body = $"Hola {user.Name}, bienvenido a nuestra plataforma"
        };

        await _emailSender.SendAsync(message);
    }
}

// Responsabilidad: Orquestación (Application Layer)
public class CreateUserUseCase
{
    private readonly IUserValidator _validator;
    private readonly IUserRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CreateUserUseCase> _logger;

    public async Task<Result<User>> ExecuteAsync(CreateUserCommand command)
    {
        // Validar
        var validationResult = _validator.Validate(command.User);
        if (!validationResult.IsSuccess)
            return Result<User>.Failure(validationResult.Errors);

        // Persistir
        var user = await _repository.CreateAsync(command.User);

        // Notificar
        await _notificationService.SendWelcomeEmailAsync(user);

        // Log
        _logger.LogInformation("Usuario creado: {Email}", user.Email);

        return Result<User>.Success(user);
    }
}
```

**Beneficios:**
- Cada clase tiene una sola responsabilidad
- Fácil de testear (mocking de dependencias)
- Cambios aislados (cambiar email no afecta la base de datos)
- Reutilizable

---

### 2. Open/Closed Principle (OCP)
**"Las entidades de software deben estar abiertas para extensión, pero cerradas para modificación"**

####  Mal Ejemplo
```csharp
public class PaymentProcessor
{
    public void ProcessPayment(Order order, string paymentMethod)
    {
        if (paymentMethod == "CreditCard")
        {
            // Procesar con tarjeta de crédito
            Console.WriteLine("Procesando pago con tarjeta...");
        }
        else if (paymentMethod == "PayPal")
        {
            // Procesar con PayPal
            Console.WriteLine("Procesando pago con PayPal...");
        }
        else if (paymentMethod == "Bitcoin")
        {
            // Procesar con Bitcoin
            Console.WriteLine("Procesando pago con Bitcoin...");
        }
        // Cada nuevo método de pago requiere modificar esta clase
    }
}
```

**Problemas:**
- Cada nuevo método de pago requiere modificar la clase existente
- Riesgo de romper funcionalidad existente
- Viola el principio de cerrado para modificación

####  Buen Ejemplo
```csharp
// Abstracción
public interface IPaymentMethod
{
    Task<PaymentResult> ProcessAsync(Order order);
    string Name { get; }
}

// Implementaciones concretas (extensiones)
public class CreditCardPayment : IPaymentMethod
{
    public string Name => "CreditCard";

    public async Task<PaymentResult> ProcessAsync(Order order)
    {
        // Lógica específica de tarjeta de crédito
        Console.WriteLine($"Procesando ${order.Total} con tarjeta...");
        // Integración con payment gateway
        return PaymentResult.Success();
    }
}

public class PayPalPayment : IPaymentMethod
{
    public string Name => "PayPal";

    public async Task<PaymentResult> ProcessAsync(Order order)
    {
        // Lógica específica de PayPal
        Console.WriteLine($"Procesando ${order.Total} con PayPal...");
        return PaymentResult.Success();
    }
}

public class BitcoinPayment : IPaymentMethod
{
    public string Name => "Bitcoin";

    public async Task<PaymentResult> ProcessAsync(Order order)
    {
        // Lógica específica de Bitcoin
        Console.WriteLine($"Procesando ${order.Total} con Bitcoin...");
        return PaymentResult.Success();
    }
}

// Procesador (cerrado para modificación, abierto para extensión)
public class PaymentProcessor
{
    private readonly IEnumerable<IPaymentMethod> _paymentMethods;

    public PaymentProcessor(IEnumerable<IPaymentMethod> paymentMethods)
    {
        _paymentMethods = paymentMethods;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(Order order, string methodName)
    {
        var method = _paymentMethods.FirstOrDefault(m => m.Name == methodName);

        if (method == null)
            return PaymentResult.Failure($"Método de pago '{methodName}' no soportado");

        return await method.ProcessAsync(order);
    }
}

// Registro en DI Container
services.AddTransient<IPaymentMethod, CreditCardPayment>();
services.AddTransient<IPaymentMethod, PayPalPayment>();
services.AddTransient<IPaymentMethod, BitcoinPayment>();
// Agregar nuevos métodos sin modificar código existente
```

**Beneficios:**
- Agregar nuevos métodos de pago sin modificar código existente
- Reduce el riesgo de bugs
- Facilita pruebas unitarias

---

### 3. Liskov Substitution Principle (LSP)
**"Los objetos de una clase derivada deben poder reemplazar objetos de la clase base sin alterar el comportamiento del programa"**

####  Mal Ejemplo
```csharp
public class Rectangle
{
    public virtual int Width { get; set; }
    public virtual int Height { get; set; }

    public int GetArea() => Width * Height;
}

public class Square : Rectangle
{
    public override int Width
    {
        get => base.Width;
        set
        {
            base.Width = value;
            base.Height = value; // Rompe la expectativa
        }
    }

    public override int Height
    {
        get => base.Height;
        set
        {
            base.Width = value; // Rompe la expectativa
            base.Height = value;
        }
    }
}

// Uso
void TestArea(Rectangle rectangle)
{
    rectangle.Width = 5;
    rectangle.Height = 4;

    // Esperamos 20, pero si es Square obtenemos 16
    Assert.Equal(20, rectangle.GetArea()); // FALLA con Square!
}
```

**Problemas:**
- Square no puede sustituir a Rectangle sin romper el comportamiento
- Viola las expectativas del cliente

####  Buen Ejemplo
```csharp
// Abstracción base
public abstract class Shape
{
    public abstract int GetArea();
}

public class Rectangle : Shape
{
    public int Width { get; set; }
    public int Height { get; set; }

    public override int GetArea() => Width * Height;
}

public class Square : Shape
{
    public int Side { get; set; }

    public override int GetArea() => Side * Side;
}

// Uso
void TestArea(Shape shape)
{
    // No hacemos asunciones sobre propiedades específicas
    int area = shape.GetArea();
    Console.WriteLine($"Área: {area}");
}

// Ejemplo más realista con comportamiento consistente
public interface IReadOnlyRepository<T>
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
}

public interface IRepository<T> : IReadOnlyRepository<T>
{
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

// Implementación que respeta el contrato
public class UserRepository : IRepository<User>
{
    private readonly DbContext _context;

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
```

---

### 4. Interface Segregation Principle (ISP)
**"Los clientes no deben verse obligados a depender de interfaces que no utilizan"**

####  Mal Ejemplo
```csharp
// Interface "gorda" con demasiadas responsabilidades
public interface IWorker
{
    void Work();
    void Eat();
    void Sleep();
    void GetPaid();
}

// Un robot no come ni duerme!
public class Robot : IWorker
{
    public void Work()
    {
        Console.WriteLine("Robot trabajando...");
    }

    public void Eat()
    {
        throw new NotImplementedException(); // Problema!
    }

    public void Sleep()
    {
        throw new NotImplementedException(); // Problema!
    }

    public void GetPaid()
    {
        Console.WriteLine("Mantenimiento programado");
    }
}
```

**Problemas:**
- Robot se ve obligado a implementar métodos que no necesita
- Implementaciones vacías o excepciones

####  Buen Ejemplo
```csharp
// Interfaces segregadas
public interface IWorkable
{
    void Work();
}

public interface IFeedable
{
    void Eat();
}

public interface ISleepable
{
    void Sleep();
}

public interface IPayable
{
    void GetPaid();
}

// Humano implementa todas
public class Human : IWorkable, IFeedable, ISleepable, IPayable
{
    public void Work() => Console.WriteLine("Humano trabajando...");
    public void Eat() => Console.WriteLine("Humano comiendo...");
    public void Sleep() => Console.WriteLine("Humano durmiendo...");
    public void GetPaid() => Console.WriteLine("Humano recibiendo salario");
}

// Robot solo implementa lo que necesita
public class Robot : IWorkable, IPayable
{
    public void Work() => Console.WriteLine("Robot trabajando...");
    public void GetPaid() => Console.WriteLine("Mantenimiento programado");
}

// Ejemplo realista con repositorios
public interface IReadRepository<T>
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
}

public interface IWriteRepository<T>
{
    Task<T> CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

// Los clientes pueden depender solo de lo que necesitan
public class ReportService
{
    private readonly IReadRepository<Order> _orderRepository;

    // Solo necesita lectura, no escritura
    public ReportService(IReadRepository<Order> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderReport> GenerateReportAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return new OrderReport(orders);
    }
}
```

---

### 5. Dependency Inversion Principle (DIP)
**"Depende de abstracciones, no de concreciones"**

####  Mal Ejemplo
```csharp
// Clase concreta de bajo nivel
public class SqlServerDatabase
{
    public void Save(string data)
    {
        Console.WriteLine($"Guardando en SQL Server: {data}");
    }
}

// Clase de alto nivel depende de implementación concreta
public class UserService
{
    private readonly SqlServerDatabase _database;

    public UserService()
    {
        _database = new SqlServerDatabase(); // Acoplamiento fuerte!
    }

    public void CreateUser(User user)
    {
        _database.Save(user.ToString());
    }
}
```

**Problemas:**
- UserService está acoplado a SqlServerDatabase
- Imposible cambiar a otra base de datos sin modificar UserService
- Difícil de testear (no se puede mockear)

####  Buen Ejemplo
```csharp
// Abstracción (alto nivel)
public interface IDatabase
{
    Task SaveAsync(string data);
}

// Implementaciones concretas (bajo nivel)
public class SqlServerDatabase : IDatabase
{
    private readonly string _connectionString;

    public SqlServerDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveAsync(string data)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync("INSERT INTO...", new { data });
    }
}

public class PostgreSqlDatabase : IDatabase
{
    private readonly string _connectionString;

    public PostgreSqlDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveAsync(string data)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("INSERT INTO...", new { data });
    }
}

public class MongoDatabase : IDatabase
{
    private readonly IMongoClient _client;

    public MongoDatabase(IMongoClient client)
    {
        _client = client;
    }

    public async Task SaveAsync(string data)
    {
        var database = _client.GetDatabase("mydb");
        var collection = database.GetCollection<BsonDocument>("users");
        await collection.InsertOneAsync(new BsonDocument("data", data));
    }
}

// Servicio de alto nivel depende de abstracción
public class UserService
{
    private readonly IDatabase _database;
    private readonly ILogger<UserService> _logger;

    // Inyección de dependencias
    public UserService(IDatabase database, ILogger<UserService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task CreateUserAsync(User user)
    {
        await _database.SaveAsync(user.ToString());
        _logger.LogInformation("Usuario creado: {UserId}", user.Id);
    }
}

// Configuración en Startup/Program.cs
services.AddScoped<IDatabase, SqlServerDatabase>(); // Fácil cambiar a PostgreSql!
services.AddScoped<UserService>();

// Testing
public class UserServiceTests
{
    [Fact]
    public async Task CreateUser_ShouldSaveToDatabase()
    {
        // Arrange
        var mockDatabase = new Mock<IDatabase>();
        var mockLogger = new Mock<ILogger<UserService>>();
        var service = new UserService(mockDatabase.Object, mockLogger.Object);
        var user = new User { Id = 1, Name = "Test" };

        // Act
        await service.CreateUserAsync(user);

        // Assert
        mockDatabase.Verify(db => db.SaveAsync(It.IsAny<string>()), Times.Once);
    }
}
```

---

##  Aplicación de SOLID en Arquitecturas

### En Clean Architecture
```csharp
// Domain Layer (abstracciones)
public interface IUserRepository { }

// Application Layer (casos de uso)
public class CreateUserUseCase
{
    private readonly IUserRepository _repository;
    // DIP: Depende de abstracción, no de implementación concreta
}

// Infrastructure Layer (implementaciones)
public class SqlUserRepository : IUserRepository
{
    // Implementación concreta
}
```

### En DDD
```csharp
// Aggregate Root (SRP)
public class Order : AggregateRoot
{
    // Solo responsable de lógica de pedidos
    public void AddItem(OrderItem item) { }
    public void RemoveItem(int itemId) { }
}

// Value Object (Inmutable, SRP)
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    // Solo responsable de operaciones monetarias
    public Money Add(Money other) { }
}

// Domain Service (cuando la lógica no pertenece a una entidad)
public class PriceCalculator
{
    public Money Calculate(Order order, DiscountPolicy policy) { }
}
```

---

##  Beneficios de SOLID

| Principio | Beneficio Principal |
|-----------|-------------------|
| **SRP** | Código más mantenible y cohesivo |
| **OCP** | Extensibilidad sin modificar código existente |
| **LSP** | Polimorfismo seguro y predecible |
| **ISP** | Interfaces pequeñas y específicas |
| **DIP** | Bajo acoplamiento, alta testabilidad |

##  Checklist para Revisar tu Código

- [ ] ¿Cada clase tiene una sola razón para cambiar? (SRP)
- [ ] ¿Puedo agregar nuevas funcionalidades sin modificar código existente? (OCP)
- [ ] ¿Las clases derivadas pueden sustituir a las base sin romper el comportamiento? (LSP)
- [ ] ¿Las interfaces son pequeñas y específicas? (ISP)
- [ ] ¿Mis clases dependen de abstracciones en lugar de concreciones? (DIP)

##  Recursos Adicionales

- **Clean Architecture** - Robert C. Martin
- **Agile Software Development, Principles, Patterns, and Practices** - Robert C. Martin
- **Head First Design Patterns** - Freeman, Robson, Sierra, Bates

---

**Recuerda:** SOLID no es dogma. Son principios guía. Úsalos con sentido común y adapta según el contexto de tu proyecto.
