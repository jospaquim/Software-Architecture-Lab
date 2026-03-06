# Software Architecture Boilerplates

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)
![Kubernetes](https://img.shields.io/badge/Kubernetes-Ready-326CE5?logo=kubernetes)

Repositorio de arquitecturas de software empresariales implementadas como boilerplates listos para producción. Este repositorio es tu guía completa para entender, comparar e implementar diferentes patrones arquitectónicos en proyectos reales.

##  Arquitecturas Incluidas

### 1. [Clean Architecture](./docs/clean-architecture/README.md)
Arquitectura basada en la separación de responsabilidades en capas concéntricas, priorizando la independencia de frameworks y bases de datos.

**Casos de uso ideales:**
- APIs RESTful empresariales
- Microservicios con lógica de negocio compleja
- Aplicaciones que requieren alta testabilidad
- Proyectos de larga duración con equipos grandes

### 2. [Domain-Driven Design (DDD)](./docs/ddd/README.md)
Arquitectura centrada en el dominio del negocio, usando patrones tácticos y estratégicos para modelar sistemas complejos.

**Casos de uso ideales:**
- Sistemas con lógica de negocio muy compleja
- E-commerce, finanzas, seguros
- Aplicaciones con múltiples bounded contexts
- Sistemas que evolucionan constantemente

### 3. [Event-Driven Architecture (EDA)](./docs/eda/README.md)
Arquitectura basada en eventos para sistemas distribuidos y asíncronos de alta escalabilidad.

**Casos de uso ideales:**
- Sistemas de procesamiento en tiempo real
- Microservicios desacoplados
- Aplicaciones IoT
- Sistemas de notificaciones y mensajería

## ️ Estructura del Repositorio

```
SoftwareArchitecture/
├── docs/                           # Documentación detallada
│   ├── clean-architecture/         # Guía Clean Architecture
│   ├── ddd/                        # Guía DDD
│   ├── eda/                        # Guía EDA
│   └── principles/                 # SOLID, Clean Code
├── src/                            # Implementaciones
│   ├── CleanArchitecture/          # Proyecto Clean Architecture
│   │   ├── API/                    # Capa de presentación
│   │   ├── Application/            # Casos de uso
│   │   ├── Domain/                 # Entidades y lógica de negocio
│   │   ├── Infrastructure/         # Implementaciones externas
│   │   └── Tests/                  # Tests unitarios e integración
│   ├── DDD/                        # Proyecto DDD
│   │   ├── API/
│   │   ├── Application/
│   │   ├── Domain/
│   │   ├── Infrastructure/
│   │   └── Tests/
│   └── EDA/                        # Proyecto EDA
│       ├── API/
│       ├── EventHandlers/
│       ├── Domain/
│       ├── Infrastructure/
│       └── Tests/
├── frontend/                       # Ejemplos frontend
│   ├── angular-example/            # Cliente Angular
│   └── nextjs-example/             # Cliente Next.js
├── docker/                         # Dockerfiles
└── kubernetes/                     # Manifiestos K8s
```

##  Características Principales

###  Seguridad
- **Autenticación JWT** con refresh tokens
- **Integración Keycloak** para SSO empresarial
- **Auth0** como alternativa cloud
- **Rate Limiting** para protección contra abuso
- **CORS** configurado correctamente
- **HTTPS** obligatorio en producción
- **Secrets management** con variables de entorno

###  API
- **Swagger/OpenAPI 3.0** con documentación completa
- **Versionamiento** de APIs (v1, v2)
- **Paginación, filtrado y ordenamiento** estándar
- **Validación** con FluentValidation
- **Manejo de errores** centralizado con Problem Details (RFC 7807)
- **Health checks** para monitoreo

###  Bases de Datos
- **SQL Server** con Entity Framework Core
- **PostgreSQL** con soporte completo
- **Migraciones** automatizadas
- **Repository Pattern** implementado
- **Unit of Work** para transacciones
- **Database seeding** para desarrollo

###  Cloud Ready
- **Docker** multi-stage builds optimizados
- **Kubernetes** manifiestos (Deployments, Services, ConfigMaps, Secrets)
- **Health checks** para orquestadores
- **Logs estructurados** con Serilog
- **Métricas** con Prometheus
- **Tracing distribuido** con OpenTelemetry

###  Principios y Patrones
- **SOLID** principles aplicados
- **Clean Code** en toda la base de código
- **Design Patterns** (Repository, Factory, Strategy, CQRS, Mediator)
- **Dependency Injection** nativo de .NET
- **Async/Await** para operaciones I/O

###  Testing
- **Unit Tests** con xUnit
- **Integration Tests** con WebApplicationFactory
- **Test Containers** para bases de datos
- **Mocking** con Moq
- **Cobertura de código** > 80%

## ️ Stack Tecnológico

### Backend
- **.NET 8.0** (LTS)
- **C# 12**
- **ASP.NET Core Web API**
- **Entity Framework Core 8**
- **MediatR** (CQRS/Mediator pattern)
- **AutoMapper**
- **FluentValidation**
- **Serilog**

### Frontend
- **Angular 17+** (TypeScript)
- **Next.js 14+** (React, TypeScript)
- **Tailwind CSS**
- **Axios/Fetch** para llamadas API

### Bases de Datos
- **SQL Server 2022**
- **PostgreSQL 16**

### Mensajería (EDA)
- **RabbitMQ**
- **MassTransit** (.NET)

### Autenticación
- **JWT (JSON Web Tokens)**
- **Keycloak** (Open Source)
- **Auth0** (SaaS)

### DevOps
- **Docker & Docker Compose**
- **Kubernetes**
- **GitHub Actions** (CI/CD)

##  Quick Start

### Prerequisitos
```bash
# Instalar .NET 8 SDK
dotnet --version  # Debe ser 8.0+

# Instalar Docker
docker --version

# Instalar Docker Compose
docker-compose --version
```

### Opción 1: Ejecutar con Docker Compose (Recomendado)

```bash
# Clean Architecture
cd src/CleanArchitecture
docker-compose up

# DDD
cd src/DDD
docker-compose up

# EDA
cd src/EDA
docker-compose up
```

La API estará disponible en `http://localhost:5000` y Swagger en `http://localhost:5000/swagger`

### Opción 2: Ejecutar Localmente

```bash
# 1. Clonar el repositorio
git clone <repository-url>
cd SoftwareArchitecture

# 2. Elegir una arquitectura (ejemplo: Clean Architecture)
cd src/CleanArchitecture/API

# 3. Restaurar dependencias
dotnet restore

# 4. Configurar connection string en appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CleanArchDB;User Id=sa;Password=YourPassword123;TrustServerCertificate=True"
  }
}

# 5. Aplicar migraciones
dotnet ef database update --project ../Infrastructure

# 6. Ejecutar la aplicación
dotnet run

# La API estará en https://localhost:7001
```

### Opción 3: Ejecutar en Kubernetes

```bash
# 1. Construir imágenes Docker
docker build -t clean-architecture-api:latest -f docker/CleanArchitecture.Dockerfile .

# 2. Aplicar manifiestos de Kubernetes
kubectl apply -f kubernetes/clean-architecture/

# 3. Verificar pods
kubectl get pods

# 4. Acceder a la API
kubectl port-forward svc/clean-architecture-api 5000:80
```

##  Guías de Arquitectura

### [Clean Architecture](./docs/clean-architecture/README.md)
-  Ventajas y desventajas
-  Comparación con otras arquitecturas
-  Casos de uso reales
-  Mejores prácticas
-  Implementación paso a paso

### [Domain-Driven Design](./docs/ddd/README.md)
-  Patrones tácticos (Entities, Value Objects, Aggregates)
- ️ Patrones estratégicos (Bounded Contexts, Context Mapping)
-  Ubiquitous Language
- ️ Arquitectura hexagonal
-  Ejemplos de e-commerce

### [Event-Driven Architecture](./docs/eda/README.md)
-  Event Sourcing
-  CQRS (Command Query Responsibility Segregation)
-  Mensajería con RabbitMQ
-  Sagas y orquestación
-  Event Storming

##  Seguridad

### Autenticación JWT
```csharp
// Configuración básica incluida
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* ... */ });
```

### Rate Limiting
```csharp
// Protección contra abuso
services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("fixed", options => {
        options.Window = TimeSpan.FromMinutes(1);
        options.PermitLimit = 100;
    });
});
```

### Keycloak Integration
Ver documentación completa en [docs/authentication/keycloak.md](./docs/authentication/keycloak.md)

##  Testing

```bash
# Ejecutar todos los tests
dotnet test

# Ejecutar con cobertura
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Tests de integración (requiere Docker)
dotnet test --filter Category=Integration
```

##  Comparación de Arquitecturas

| Característica | Clean Architecture | DDD | EDA |
|---------------|-------------------|-----|-----|
| **Complejidad** | Media | Alta | Alta |
| **Curva de aprendizaje** | Media | Alta | Alta |
| **Escalabilidad** | Alta | Alta | Muy Alta |
| **Testabilidad** | Muy Alta | Alta | Media |
| **Mantenibilidad** | Muy Alta | Alta | Media |
| **Performance** | Alta | Alta | Muy Alta |
| **Mejor para** | APIs, Microservicios | Dominios complejos | Sistemas distribuidos |
| **Tamaño de equipo** | Pequeño-Grande | Mediano-Grande | Grande |

##  Recursos de Aprendizaje

### Libros Recomendados
- **Clean Architecture** - Robert C. Martin (Uncle Bob)
- **Domain-Driven Design** - Eric Evans
- **Implementing Domain-Driven Design** - Vaughn Vernon
- **Building Event-Driven Microservices** - Adam Bellemare

### Principios SOLID
Ver guía completa en [docs/principles/SOLID.md](./docs/principles/SOLID.md)

### Clean Code
Ver guía completa en [docs/principles/CleanCode.md](./docs/principles/CleanCode.md)

##  Contribuciones

Las contribuciones son bienvenidas! Por favor:
1. Fork el repositorio
2. Crea una rama con tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

##  Licencia

Este proyecto está bajo la licencia MIT. Ver `LICENSE` para más detalles.

##  Contacto

Para preguntas, sugerencias o problemas, por favor abre un issue en GitHub.

---

**Happy Coding!** 
