# Clean Code - Guía Práctica

"Any fool can write code that a computer can understand. Good programmers write code that humans can understand." - Martin Fowler

##  Índice

1. [Nombres Significativos](#nombres-significativos)
2. [Funciones](#funciones)
3. [Comentarios](#comentarios)
4. [Formateo](#formateo)
5. [Manejo de Errores](#manejo-de-errores)
6. [Clases](#clases)
7. [Tests](#tests)

---

## 1. Nombres Significativos

###  Mal
```csharp
int d; // elapsed time in days
List<int> lst1;
string s;
var x = GetData();

public class DtaRcrd102
{
    private DateTime genymdhms;
    private DateTime modymdhms;
}
```

###  Bien
```csharp
int elapsedTimeInDays;
List<Customer> activeCustomers;
string firstName;
var customerOrders = GetCustomerOrders();

public class Customer
{
    private DateTime createdAt;
    private DateTime modifiedAt;
}
```

### Reglas de Oro

1. **Usa nombres que revelen intención**
```csharp
// Mal
if (x == 1)

// Bien
if (userRole == UserRole.Admin)
```

2. **Evita abreviaciones**
```csharp
// Mal
var custRepo = new CustRepo();

// Bien
var customerRepository = new CustomerRepository();
```

3. **Usa nombres pronunciables**
```csharp
// Mal
DateTime genymdhms;

// Bien
DateTime generationTimestamp;
```

4. **Una palabra por concepto**
```csharp
// Inconsistente (Mal)
customerController.Fetch();
orderController.Retrieve();
productController.Get();

// Consistente (Bien)
customerController.Get();
orderController.Get();
productController.Get();
```

5. **Nombres de clases: sustantivos**
```csharp
public class Customer { }
public class Account { }
public class OrderProcessor { }
```

6. **Nombres de métodos: verbos**
```csharp
public void SaveCustomer() { }
public Customer GetCustomerById(int id) { }
public bool IsValidEmail(string email) { }
```

---

## 2. Funciones

### Principio: Funciones Pequeñas

**Una función debe hacer una cosa, hacerla bien y solo hacer eso.**

####  Mal - Función que hace demasiado
```csharp
public void ProcessOrder(Order order)
{
    // Validar
    if (order == null)
        throw new ArgumentNullException(nameof(order));

    if (order.Items.Count == 0)
        throw new InvalidOperationException("Order must have items");

    // Calcular total
    decimal total = 0;
    foreach (var item in order.Items)
    {
        total += item.Price * item.Quantity;
    }

    // Aplicar descuento
    if (order.Customer.IsVip)
        total *= 0.9m;

    // Guardar en base de datos
    using var connection = new SqlConnection(_connectionString);
    connection.Open();
    var command = new SqlCommand("INSERT INTO Orders...", connection);
    command.Parameters.AddWithValue("@Total", total);
    command.ExecuteNonQuery();

    // Enviar email
    var smtp = new SmtpClient();
    smtp.Send(new MailMessage("from@email.com", order.Customer.Email, "Order Confirmed", $"Total: {total}"));

    // Actualizar inventario
    foreach (var item in order.Items)
    {
        var product = GetProductById(item.ProductId);
        product.Stock -= item.Quantity;
        UpdateProduct(product);
    }

    // Log
    File.AppendAllText("log.txt", $"Order {order.Id} processed");
}
```

####  Bien - Funciones pequeñas y enfocadas
```csharp
public async Task<Result<Order>> ProcessOrderAsync(Order order)
{
    ValidateOrder(order);

    var total = CalculateOrderTotal(order);
    var discountedTotal = ApplyDiscount(total, order.Customer);

    order.Total = discountedTotal;

    await SaveOrderAsync(order);
    await UpdateInventoryAsync(order.Items);
    await SendConfirmationEmailAsync(order);

    _logger.LogInformation("Order {OrderId} processed successfully", order.Id);

    return Result<Order>.Success(order);
}

private void ValidateOrder(Order order)
{
    if (order == null)
        throw new ArgumentNullException(nameof(order));

    if (!order.Items.Any())
        throw new InvalidOperationException("Order must have at least one item");
}

private decimal CalculateOrderTotal(Order order)
{
    return order.Items.Sum(item => item.Price * item.Quantity);
}

private decimal ApplyDiscount(decimal total, Customer customer)
{
    if (customer.IsVip)
        return total * 0.9m;

    return total;
}

private async Task SaveOrderAsync(Order order)
{
    await _orderRepository.CreateAsync(order);
}

private async Task UpdateInventoryAsync(IEnumerable<OrderItem> items)
{
    foreach (var item in items)
    {
        await _inventoryService.DecrementStockAsync(item.ProductId, item.Quantity);
    }
}

private async Task SendConfirmationEmailAsync(Order order)
{
    var message = new EmailMessage
    {
        To = order.Customer.Email,
        Subject = "Order Confirmed",
        Body = $"Your order total is: {order.Total:C}"
    };

    await _emailService.SendAsync(message);
}
```

### Reglas para Funciones

1. **Pequeñas** - Idealmente 10-15 líneas, máximo 20
2. **Hacer una cosa** - Single Responsibility
3. **Un nivel de abstracción** - No mezclar alto y bajo nivel
4. **Pocos parámetros** - Idealmente 0-2, máximo 3
5. **Sin efectos secundarios** - No modificar estado global

### Número de Parámetros

####  Mal - Demasiados parámetros
```csharp
public void CreateUser(
    string firstName,
    string lastName,
    string email,
    string phone,
    string address,
    string city,
    string country,
    string zipCode,
    DateTime birthDate,
    bool isActive)
{
    // ...
}
```

####  Bien - Objeto de parámetros
```csharp
public class CreateUserRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public Address Address { get; set; }
    public DateTime BirthDate { get; set; }
    public bool IsActive { get; set; }
}

public void CreateUser(CreateUserRequest request)
{
    // ...
}
```

### Evitar Flag Arguments

####  Mal
```csharp
public void SaveDocument(Document doc, bool isPublic)
{
    if (isPublic)
    {
        // Guardar como público
    }
    else
    {
        // Guardar como privado
    }
}
```

####  Bien
```csharp
public void SavePublicDocument(Document doc) { }
public void SavePrivateDocument(Document doc) { }

// O mejor aún
public void SaveDocument(Document doc, DocumentVisibility visibility) { }

public enum DocumentVisibility
{
    Public,
    Private
}
```

---

## 3. Comentarios

**"No comentes código malo, reescríbelo" - Brian Kernighan**

###  Comentarios Malos

```csharp
// Comentarios obvios
int price; // el precio

// Comentarios redundantes
/// <summary>
/// Gets or sets the customer ID
/// </summary>
public int CustomerId { get; set; }

// Comentarios que deberían ser código
// Check to see if employee is eligible for full benefits
if ((employee.Flags & HOURLY_FLAG) && (employee.Age > 65))

// Código comentado (NUNCA hagas esto, usa control de versiones)
// public void OldMethod()
// {
//     // ...
// }

// Comentarios engañosos
// Returns the user (realmente devuelve un DTO)
public User GetUser(int id)
```

###  Comentarios Buenos

```csharp
// 1. Comentarios legales
// Copyright (C) 2024 by Company Inc. All rights reserved.

// 2. Comentarios informativos cuando no se puede expresar en código
// Format matched: yyyy-MM-dd HH:mm:ss
var regex = new Regex(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}");

// 3. Explicar intención
// We use a hash to speed up lookups. The array won't work
// for large datasets and the overhead of a HashMap is acceptable.
var cache = new Dictionary<int, Customer>();

// 4. Aclaración de código que no se puede cambiar
// This is required by the legacy API
var response = await CallLegacyApiAsync(obscureParameter);

// 5. Advertencias
// WARNING: This operation is expensive. Use caching for production.
public List<Customer> GetAllCustomersWithFullHistory() { }

// 6. TODO comments (pero mejor usar herramientas)
// TODO: Implement retry logic with exponential backoff

// 7. Amplificación
// The trim is REALLY important. Without it, the parser fails.
var cleanedInput = input.Trim();
```

### Mejor que Comentarios: Código Auto-Explicativo

####  Mal
```csharp
// Check if user has admin privileges
if (user.Role == 1 && user.Permissions.Contains("admin"))
{
    // ...
}
```

####  Bien
```csharp
if (user.IsAdmin())
{
    // ...
}

// En la clase User
public bool IsAdmin()
{
    return Role == UserRole.Admin && Permissions.Contains("admin");
}
```

---

## 4. Formateo

### Orden Vertical

```csharp
public class OrderService
{
    // 1. Constantes
    private const int MAX_ITEMS = 100;

    // 2. Campos privados
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;

    // 3. Constructor
    public OrderService(
        IOrderRepository orderRepository,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    // 4. Propiedades públicas
    public int MaxRetries { get; set; } = 3;

    // 5. Métodos públicos
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        ValidateRequest(request);
        var order = MapToOrder(request);
        return await _orderRepository.CreateAsync(order);
    }

    public async Task<Order> GetOrderAsync(int id)
    {
        return await _orderRepository.GetByIdAsync(id);
    }

    // 6. Métodos privados (cerca de donde se usan)
    private void ValidateRequest(CreateOrderRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
    }

    private Order MapToOrder(CreateOrderRequest request)
    {
        return new Order
        {
            CustomerId = request.CustomerId,
            Items = request.Items
        };
    }
}
```

### Espaciado

```csharp
// Agrupar conceptos relacionados
public class Customer
{
    // Información básica
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    // Información de contacto
    public string Email { get; set; }
    public string Phone { get; set; }

    // Información de cuenta
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

// Separar conceptos no relacionados
public void ProcessOrder(Order order)
{
    ValidateOrder(order);

    var total = CalculateTotal(order);

    SaveOrder(order);
}
```

### Límite de Línea

```csharp
// Mal - Línea muy larga
var customer = new Customer { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Phone = "+1234567890", Address = "123 Main St", City = "New York" };

// Bien - Formateado
var customer = new Customer
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john.doe@example.com",
    Phone = "+1234567890",
    Address = "123 Main St",
    City = "New York"
};
```

---

## 5. Manejo de Errores

### Usar Excepciones, No Códigos de Error

####  Mal
```csharp
public int SaveCustomer(Customer customer)
{
    if (customer == null)
        return -1; // Error code

    if (string.IsNullOrEmpty(customer.Email))
        return -2; // Another error code

    // ...
    return customerId; // Success
}

// Uso
var result = SaveCustomer(customer);
if (result == -1)
{
    // Handle null customer
}
else if (result == -2)
{
    // Handle invalid email
}
```

####  Bien
```csharp
public async Task<Customer> SaveCustomerAsync(Customer customer)
{
    if (customer == null)
        throw new ArgumentNullException(nameof(customer));

    if (string.IsNullOrEmpty(customer.Email))
        throw new ValidationException("Email is required");

    return await _repository.CreateAsync(customer);
}

// Uso
try
{
    var customer = await SaveCustomerAsync(newCustomer);
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed");
    return BadRequest(ex.Message);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500);
}
```

### Usar Result Pattern para Errores de Negocio

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }

    private Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}

// Uso
public async Task<Result<Order>> CreateOrderAsync(CreateOrderCommand command)
{
    if (!command.Items.Any())
        return Result<Order>.Failure("Order must have at least one item");

    if (command.Total < 0)
        return Result<Order>.Failure("Total cannot be negative");

    var order = await _repository.CreateAsync(command.ToOrder());
    return Result<Order>.Success(order);
}

// En el controller
var result = await _orderService.CreateOrderAsync(command);

if (!result.IsSuccess)
    return BadRequest(result.Error);

return Ok(result.Value);
```

### Try-Catch-Finally

```csharp
// Mal - Try-Catch muy amplio
try
{
    var customer = await GetCustomerAsync(id);
    var orders = await GetOrdersAsync(customer.Id);
    var total = CalculateTotal(orders);
    await UpdateCustomerAsync(customer);
    await SendEmailAsync(customer);
    return total;
}
catch (Exception ex)
{
    // ¿Qué falló exactamente?
    _logger.LogError(ex, "Something failed");
    throw;
}

// Bien - Try-Catch específico
var customer = await GetCustomerAsync(id);
var orders = await GetOrdersAsync(customer.Id);
var total = CalculateTotal(orders);

try
{
    await UpdateCustomerAsync(customer);
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Failed to update customer {CustomerId}", customer.Id);
    throw new ApplicationException("Failed to update customer", ex);
}

try
{
    await SendEmailAsync(customer);
}
catch (SmtpException ex)
{
    // No crítico, solo log
    _logger.LogWarning(ex, "Failed to send email to {Email}", customer.Email);
}

return total;
```

---

## 6. Clases

### Single Responsibility Principle

```csharp
// Mal - Clase que hace demasiado
public class UserManager
{
    public void CreateUser(User user) { }
    public void SendWelcomeEmail(User user) { }
    public void LogAction(string action) { }
    public void ValidateUser(User user) { }
    public void SaveToDatabase(User user) { }
}

// Bien - Clases con responsabilidad única
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly IUserValidator _validator;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;

    public async Task<User> CreateUserAsync(User user)
    {
        _validator.Validate(user);
        var createdUser = await _repository.CreateAsync(user);
        await _emailService.SendWelcomeEmailAsync(user);
        _logger.LogInformation("User created: {UserId}", user.Id);
        return createdUser;
    }
}
```

### Cohesión

```csharp
// Alta cohesión - Todos los métodos usan la mayoría de los campos
public class Calculator
{
    private readonly decimal _taxRate;
    private readonly decimal _discountRate;

    public Calculator(decimal taxRate, decimal discountRate)
    {
        _taxRate = taxRate;
        _discountRate = discountRate;
    }

    public decimal CalculateTotal(decimal amount)
    {
        var discounted = ApplyDiscount(amount);
        return ApplyTax(discounted);
    }

    private decimal ApplyDiscount(decimal amount)
    {
        return amount * (1 - _discountRate);
    }

    private decimal ApplyTax(decimal amount)
    {
        return amount * (1 + _taxRate);
    }
}
```

---

## 7. Tests

### Tests Limpios

```csharp
// Patrón AAA: Arrange, Act, Assert
[Fact]
public async Task CreateOrder_WithValidData_ShouldReturnOrder()
{
    // Arrange
    var mockRepository = new Mock<IOrderRepository>();
    var service = new OrderService(mockRepository.Object);
    var command = new CreateOrderCommand
    {
        CustomerId = 1,
        Items = new List<OrderItem>
        {
            new() { ProductId = 1, Quantity = 2 }
        }
    };

    mockRepository
        .Setup(r => r.CreateAsync(It.IsAny<Order>()))
        .ReturnsAsync(new Order { Id = 1 });

    // Act
    var result = await service.CreateOrderAsync(command);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(1, result.Id);
    mockRepository.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Once);
}

// Un concepto por test
[Fact]
public async Task CreateOrder_WithNoItems_ShouldThrowException()
{
    // Arrange
    var service = new OrderService(Mock.Of<IOrderRepository>());
    var command = new CreateOrderCommand { CustomerId = 1, Items = new List<OrderItem>() };

    // Act & Assert
    await Assert.ThrowsAsync<ValidationException>(() => service.CreateOrderAsync(command));
}
```

### Nombres de Tests Descriptivos

```csharp
// Mal
[Fact]
public void Test1() { }

// Bien - Indica qué se prueba, en qué condición, y qué se espera
[Fact]
public void CreateUser_WithNullEmail_ShouldThrowArgumentNullException() { }

[Fact]
public void CalculateDiscount_ForVipCustomer_ShouldReturn10PercentOff() { }

[Fact]
public void ProcessPayment_WhenInsufficientFunds_ShouldReturnFailureResult() { }
```

---

##  Checklist de Clean Code

### Nombres
- [ ] ¿Los nombres son descriptivos y revelan intención?
- [ ] ¿Evito abreviaciones confusas?
- [ ] ¿Uso sustantivos para clases y verbos para métodos?

### Funciones
- [ ] ¿Mis funciones son pequeñas (menos de 20 líneas)?
- [ ] ¿Cada función hace solo una cosa?
- [ ] ¿Tengo máximo 3 parámetros por función?
- [ ] ¿Evito efectos secundarios?

### Comentarios
- [ ] ¿El código es auto-explicativo sin comentarios?
- [ ] ¿Los comentarios explican el "por qué", no el "qué"?
- [ ] ¿Evito código comentado?

### Formateo
- [ ] ¿Uso espaciado consistente?
- [ ] ¿Agrupo conceptos relacionados?
- [ ] ¿Las líneas tienen menos de 120 caracteres?

### Manejo de Errores
- [ ] ¿Uso excepciones en lugar de códigos de error?
- [ ] ¿Mis excepciones son específicas?
- [ ] ¿Manejo errores cerca de donde ocurren?

### Clases
- [ ] ¿Cada clase tiene una sola responsabilidad?
- [ ] ¿Mis clases son cohesivas?
- [ ] ¿Evito clases "God"?

### Tests
- [ ] ¿Uso el patrón AAA (Arrange, Act, Assert)?
- [ ] ¿Cada test prueba un solo concepto?
- [ ] ¿Los nombres de tests son descriptivos?
- [ ] ¿Tengo buena cobertura de código?

---

##  Recursos Recomendados

1. **Clean Code** - Robert C. Martin (Uncle Bob)
2. **The Pragmatic Programmer** - Andrew Hunt, David Thomas
3. **Refactoring** - Martin Fowler
4. **Code Complete** - Steve McConnell

---

**Recuerda:** El código se lee muchas más veces de las que se escribe. Escribe código para humanos, no para computadoras.
