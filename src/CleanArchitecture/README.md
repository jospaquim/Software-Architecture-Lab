# Clean Architecture - E-Commerce API

API RESTful de ejemplo construida con Clean Architecture, implementando un sistema de e-commerce completo con gestión de clientes, productos y pedidos.

##  Quick Start

### Opción 1: Docker Compose (Recomendado)

```bash
# Construir y ejecutar
docker-compose up --build

# La API estará disponible en:
# http://localhost:5000
# Swagger UI: http://localhost:5000/swagger
```

### Opción 2: Ejecución Local

#### Prerequisitos
- .NET 8 SDK
- SQL Server o PostgreSQL

#### Pasos

1. **Configurar Connection String**

Edita `API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CleanArchitectureDB;User Id=sa;Password=TuPassword;TrustServerCertificate=True"
  },
  "DatabaseProvider": "SqlServer"  // o "PostgreSQL"
}
```

2. **Restaurar Dependencias**

```bash
dotnet restore
```

3. **Aplicar Migraciones**

```bash
cd Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../API
dotnet ef database update --startup-project ../API
```

4. **Ejecutar la API**

```bash
cd API
dotnet run
```

La API estará en `https://localhost:7001`

### Opción 3: Kubernetes

```bash
# 1. Construir imagen
docker build -t cleanarchitecture-api:latest -f Dockerfile .

# 2. Aplicar manifiestos
kubectl apply -f ../../kubernetes/clean-architecture/

# 3. Verificar
kubectl get pods
kubectl get services

# 4. Acceder (si usas port-forward)
kubectl port-forward svc/cleanarchitecture-api 5000:80
```

---

##  Estructura del Proyecto

```
CleanArchitecture/
├── Domain/                 # Entidades, interfaces, lógica de negocio
├── Application/            # Casos de uso, DTOs, validaciones
├── Infrastructure/         # EF Core, repositorios, servicios
├── API/                    # Controllers, middleware, configuración
├── Tests/                  # Tests unitarios e integración
├── Dockerfile
├── docker-compose.yml
└── README.md
```

---

##  Endpoints Principales

### Customers

- `GET /api/v1/customers` - Lista de clientes (paginado)
- `GET /api/v1/customers/{id}` - Obtener cliente por ID
- `POST /api/v1/customers` - Crear cliente
- `PUT /api/v1/customers/{id}` - Actualizar cliente
- `DELETE /api/v1/customers/{id}` - Eliminar cliente
- `POST /api/v1/customers/{id}/promote-to-vip` - Promover a VIP

### Products

- `GET /api/v1/products` - Lista de productos
- `GET /api/v1/products/{id}` - Obtener producto
- `POST /api/v1/products` - Crear producto
- `PUT /api/v1/products/{id}` - Actualizar producto

### Orders

- `GET /api/v1/orders` - Lista de pedidos
- `GET /api/v1/orders/{id}` - Obtener pedido
- `POST /api/v1/orders` - Crear pedido
- `POST /api/v1/orders/{id}/ship` - Marcar como enviado
- `POST /api/v1/orders/{id}/deliver` - Marcar como entregado

---

##  Autenticación

La API usa **JWT Bearer Token** para autenticación.

### Obtener Token

```bash
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "Admin123!"
}
```

### Usar Token

```bash
curl -H "Authorization: Bearer {token}" \
  http://localhost:5000/api/v1/customers
```

---

## ️ Configuración

### Variables de Entorno

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `DatabaseProvider` | Proveedor de BD | `SqlServer` o `PostgreSQL` |
| `ConnectionStrings__DefaultConnection` | Connection string | `Server=...` |
| `Jwt__SecretKey` | Clave secreta JWT | Mínimo 32 caracteres |
| `Jwt__Issuer` | Emisor del token | `CleanArchitectureAPI` |
| `Jwt__Audience` | Audiencia | `CleanArchitectureClient` |

### Configurar Rate Limiting

En `Program.cs`:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100; // 100 requests por minuto
    });
});
```

---

##  Testing

### Tests Unitarios

```bash
cd Tests/Unit
dotnet test
```

### Tests de Integración

```bash
cd Tests/Integration
dotnet test
```

### Cobertura de Código

```bash
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover
```

---

##  Migraciones de Base de Datos

### Crear Nueva Migración

```bash
cd Infrastructure
dotnet ef migrations add MigrationName --startup-project ../API
```

### Aplicar Migraciones

```bash
dotnet ef database update --startup-project ../API
```

### Rollback

```bash
dotnet ef database update PreviousMigrationName --startup-project ../API
```

### Generar Script SQL

```bash
dotnet ef migrations script --startup-project ../API --output migration.sql
```

---

##  Características Implementadas

### Seguridad
-  JWT Authentication
-  Rate Limiting (Fixed window, Sliding window)
-  CORS configurado
-  HTTPS redirect
-  Input validation con FluentValidation

### API
-  Swagger/OpenAPI 3.0
-  Versionamiento (`/api/v1/`)
-  Health checks (`/health`)
-  Problem Details (RFC 7807)
-  Paginación

### Base de Datos
-  Entity Framework Core 8
-  SQL Server support
-  PostgreSQL support
-  Migraciones
-  Seeding de datos

### Arquitectura
-  Clean Architecture
-  SOLID principles
-  CQRS con MediatR
-  Repository Pattern
-  Unit of Work Pattern
-  Result Pattern
-  Domain Events

### DevOps
-  Docker multi-stage build
-  Docker Compose con SQL Server y PostgreSQL
-  Kubernetes manifiestos
-  Health checks
-  Structured logging con Serilog

---

##  Troubleshooting

### Error: "Cannot connect to database"

1. Verifica que SQL Server/PostgreSQL esté corriendo:
   ```bash
   docker ps
   ```

2. Verifica el connection string en `appsettings.json`

3. Aplica las migraciones:
   ```bash
   dotnet ef database update --startup-project API
   ```

### Error: "JWT token invalid"

1. Verifica que `Jwt:SecretKey` tenga al menos 32 caracteres
2. Verifica que `Jwt:Issuer` y `Jwt:Audience` sean correctos
3. Verifica que el token no haya expirado

### Error: "Rate limit exceeded"

Has excedido el límite de requests. Espera 1 minuto o ajusta la configuración en `Program.cs`.

---

##  Documentación Adicional

- [Clean Architecture - Guía Completa](../../docs/clean-architecture/README.md)
- [Principios SOLID](../../docs/principles/SOLID.md)
- [Clean Code](../../docs/principles/CleanCode.md)

---

##  Contribuir

1. Fork el repositorio
2. Crea una rama: `git checkout -b feature/nueva-funcionalidad`
3. Commit: `git commit -m 'Agregar nueva funcionalidad'`
4. Push: `git push origin feature/nueva-funcionalidad`
5. Abre un Pull Request

---

##  Licencia

MIT License - Ver LICENSE para más detalles.

---

**Happy Coding!** 
