#  Tests - Software Architecture Repository

Este directorio contiene todos los tests para las tres arquitecturas del repositorio.

##  Estructura

```
tests/
├── CleanArchitecture.Domain.Tests/         # Tests de Domain Layer
│   ├── Entities/
│   │   ├── OrderTests.cs                    # Tests de Order entity
│   │   └── Auth/
│   │       └── UserTests.cs                 # Tests de User entity
│   └── CleanArchitecture.Domain.Tests.csproj
│
├── CleanArchitecture.Application.Tests/     # Tests de Application Layer (TODO)
│   ├── Commands/
│   ├── Queries/
│   └── Validators/
│
├── DDD.Sales.Domain.Tests/                  # Tests de DDD Domain
│   ├── ValueObjects/
│   │   └── MoneyTests.cs                    # Tests de Money Value Object
│   ├── Aggregates/                          # (TODO)
│   └── DDD.Sales.Domain.Tests.csproj
│
└── EDA.Tests/                               # Tests de Event Sourcing
    ├── WriteModel/
    │   └── OrderAggregateTests.cs           # Tests de Event Sourcing
    ├── Projections/                         # (TODO)
    └── EDA.Tests.csproj
```

##  Cómo ejecutar los tests

### Ejecutar todos los tests

```bash
# Desde la raíz del repositorio
dotnet test

# Con coverage
dotnet test --collect:"XPlat Code Coverage"

# Con output verbose
dotnet test --verbosity normal
```

### Ejecutar tests de un proyecto específico

```bash
# Clean Architecture Domain tests
dotnet test tests/CleanArchitecture.Domain.Tests/

# DDD Domain tests
dotnet test tests/DDD.Sales.Domain.Tests/

# EDA tests
dotnet test tests/EDA.Tests/
```

### Ejecutar un test específico

```bash
# Por nombre de clase
dotnet test --filter OrderTests

# Por nombre de test
dotnet test --filter "FullyQualifiedName~Create_ShouldCreateOrderWithPendingStatus"
```

##  Coverage

### Generar reporte de coverage

```bash
# Ejecutar tests con coverage
dotnet test --collect:"XPlat Code Coverage"

# Instalar ReportGenerator (solo una vez)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generar reporte HTML
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html

# Ver reporte
# Abrir coverage-report/index.html en el navegador
```

### Objetivo de coverage

- **Domain Layer**: 90%+
- **Application Layer**: 80%+
- **Infrastructure Layer**: 70%+
- **API Layer**: 60%+

##  Frameworks y librerías

- **xUnit**: Framework de testing
- **FluentAssertions**: Assertions legibles
- **Moq**: Mocking framework (TODO: agregar para Application tests)
- **Coverlet**: Code coverage
- **ReportGenerator**: Generación de reportes HTML

##  Convenciones

### Naming de tests

Usamos el patrón **MethodName_Scenario_ExpectedResult**:

```csharp
//  Bueno
Create_ShouldCreateOrderWithPendingStatus()
Create_WithEmptyOrderNumber_ShouldThrowArgumentException()
AddItem_WithNegativeQuantity_ShouldThrowArgumentException()

//  Malo
Test1()
CreateOrder()
ValidateOrder()
```

### Estructura AAA (Arrange-Act-Assert)

```csharp
[Fact]
public void AddItem_ShouldAddItemAndCalculateTotal()
{
    // Arrange - Preparar datos
    var order = Order.Create(Guid.NewGuid(), "ORD-001");
    var productId = Guid.NewGuid();
    var quantity = 2;
    var unitPrice = 50.00m;

    // Act - Ejecutar acción
    order.AddItem(productId, "Product", quantity, unitPrice);

    // Assert - Verificar resultado
    order.Items.Should().HaveCount(1);
    order.TotalAmount.Should().Be(100.00m);
}
```

### FluentAssertions

Preferir FluentAssertions sobre Assert de xUnit:

```csharp
//  Preferido (FluentAssertions)
order.Should().NotBeNull();
order.Status.Should().Be(OrderStatus.Pending);
order.Items.Should().HaveCount(1);
order.TotalAmount.Should().Be(100.00m);

//  Evitar (xUnit Assert)
Assert.NotNull(order);
Assert.Equal(OrderStatus.Pending, order.Status);
Assert.Single(order.Items);
Assert.Equal(100.00m, order.TotalAmount);
```

##  Tipos de tests implementados

### 1. Tests de Entities (Domain)

**Clean Architecture - OrderTests.cs**
-  Creación de orden
-  Agregar items
-  Cálculo de total
-  Validaciones de negocio
-  Transiciones de estado
-  Confirmación y envío
-  Cancelación

**Clean Architecture - UserTests.cs**
-  Creación de usuario
-  Validación de email
-  Confirmación de email
-  Activación/desactivación
-  Refresh tokens
-  Gestión de roles
-  Actualización de perfil

### 2. Tests de Value Objects (DDD)

**DDD - MoneyTests.cs**
-  Creación de Money
-  Validaciones
-  Igualdad por valor
-  Operaciones (Add, Subtract, Multiply)
-  Diferentes monedas
-  Hash code

### 3. Tests de Event Sourcing (EDA)

**EDA - OrderAggregateTests.cs**
-  Generación de eventos
-  Reconstrucción desde eventos
-  Versionado de eventos
-  Estado del aggregate
-  Uncommitted events
-  Event replay

##  Tests pendientes (TODO)

### Clean Architecture

- [ ] **Application.Tests**
  - [ ] CreateCustomerCommandHandlerTests
  - [ ] GetCustomerByIdQueryHandlerTests
  - [ ] CreateCustomerCommandValidatorTests

- [ ] **Infrastructure.Tests**
  - [ ] CustomerRepositoryTests (con InMemory DB)
  - [ ] UnitOfWorkTests

- [ ] **API.Tests** (Integration)
  - [ ] CustomersControllerTests
  - [ ] AuthControllerTests

### DDD

- [ ] **Domain.Tests**
  - [ ] OrderAggregateTests
  - [ ] EmailTests
  - [ ] AddressTests
  - [ ] PricingServiceTests
  - [ ] SpecificationTests

- [ ] **Application.Tests**
  - [ ] CreateOrderCommandHandlerTests
  - [ ] AddItemToOrderCommandHandlerTests

- [ ] **Infrastructure.Tests**
  - [ ] OrderRepositoryTests
  - [ ] SalesDbContextTests

### EDA

- [ ] **EventStore.Tests**
  - [ ] InMemoryEventStoreTests
  - [ ] KafkaEventStoreTests

- [ ] **Projections.Tests**
  - [ ] OrderProjectionTests

- [ ] **ReadModel.Tests**
  - [ ] OrderReadModelRepositoryTests

##  Configuración de CI/CD

```yaml
# Ejemplo de GitHub Actions
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

    - name: Upload coverage
      uses: codecov/codecov-action@v3
```

##  Recursos

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

---

**Estadísticas actuales:**
- Tests implementados: 27
- Cobertura: Domain ~60%, Application 0%, Infrastructure 0%
- Frameworks: xUnit, FluentAssertions, Coverlet
