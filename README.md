# Software Architecture Boilerplates

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)
![Kubernetes](https://img.shields.io/badge/Kubernetes-Ready-326CE5?logo=kubernetes)

Enterprise software architecture repository implemented as production-ready boilerplates. This repository is your comprehensive guide to understanding, comparing, and implementing different architectural patterns in real-world projects.

## Included Architectures

### 1. [Clean Architecture](./docs/clean-architecture/README.md)
Architecture based on the separation of concerns in concentric layers, prioritizing independence from frameworks and databases.

**Ideal use cases:**
- Enterprise RESTful APIs
- Microservices with complex business logic
- Applications requiring high testability
- Long-term projects with large teams

### 2. [Domain-Driven Design (DDD)](./docs/ddd/README.md)
Architecture centered on the business domain, using tactical and strategic patterns to model complex systems.

**Ideal use cases:**
- Systems with highly complex business logic
- E-commerce, finance, insurance
- Applications with multiple bounded contexts
- Constantly evolving systems

### 3. [Event-Driven Architecture (EDA)](./docs/eda/README.md)
Event-based architecture for highly scalable distributed and asynchronous systems.

**Ideal use cases:**
- Real-time processing systems
- Decoupled microservices
- IoT applications
- Notification and messaging systems

## Repository Structure

```
SoftwareArchitecture/
├── docs/                           # Detailed documentation
│   ├── clean-architecture/         # Clean Architecture Guide
│   ├── ddd/                        # DDD Guide
│   ├── eda/                        # EDA Guide
│   └── principles/                 # SOLID, Clean Code
├── src/                            # Implementations
│   ├── CleanArchitecture/          # Clean Architecture Project
│   │   ├── API/                    # Presentation layer
│   │   ├── Application/            # Use cases
│   │   ├── Domain/                 # Entities and business logic
│   │   ├── Infrastructure/         # External implementations
│   │   └── Tests/                  # Unit and integration tests
│   ├── DDD/                        # DDD Project
│   │   ├── API/
│   │   ├── Application/
│   │   ├── Domain/
│   │   ├── Infrastructure/
│   │   └── Tests/
│   └── EDA/                        # EDA Project
│       ├── API/
│       ├── EventHandlers/
│       ├── Domain/
│       ├── Infrastructure/
│       └── Tests/
├── frontend/                       # Frontend examples
│   ├── angular-example/            # Angular client
│   └── nextjs-example/             # Next.js client
├── docker/                         # Dockerfiles
└── kubernetes/                     # K8s Manifests
```

## Core Features

### Security
- **JWT Authentication** with refresh tokens
- **Keycloak Integration** for enterprise SSO
- **Auth0** as a cloud alternative
- **Rate Limiting** for abuse protection
- **CORS** properly configured
- **HTTPS** enforced in production
- **Secrets management** with environment variables

### API
- **Swagger/OpenAPI 3.0** with complete documentation
- **API Versioning** (v1, v2)
- **Standard pagination, filtering, and sorting**
- **Validation** using FluentValidation
- **Centralized error handling** with Problem Details (RFC 7807)
- **Health checks** for monitoring

### Databases
- **SQL Server** with Entity Framework Core
- **PostgreSQL** with full support
- **Automated migrations**
- **Repository Pattern** implemented
- **Unit of Work** for transactions
- **Database seeding** for development

### Cloud Ready
- **Docker** optimized multi-stage builds
- **Kubernetes** manifests (Deployments, Services, ConfigMaps, Secrets)
- **Health checks** for orchestrators
- **Structured logging** with Serilog
- **Metrics** with Prometheus
- **Distributed tracing** with OpenTelemetry

### Principles and Patterns
- **SOLID** principles applied
- **Clean Code** throughout the codebase
- **Design Patterns** (Repository, Factory, Strategy, CQRS, Mediator)
- **Dependency Injection** native to .NET
- **Async/Await** for I/O operations

### Testing
- **Unit Tests** with xUnit
- **Integration Tests** with WebApplicationFactory
- **Test Containers** for databases
- **Mocking** with Moq
- **Code Coverage** > 80%

## Tech Stack

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
- **Axios/Fetch** for API calls

### Databases
- **SQL Server 2022**
- **PostgreSQL 16**

### Messaging (EDA)
- **RabbitMQ**
- **MassTransit** (.NET)

### Authentication
- **JWT (JSON Web Tokens)**
- **Keycloak** (Open Source)
- **Auth0** (SaaS)

### DevOps
- **Docker & Docker Compose**
- **Kubernetes**
- **GitHub Actions** (CI/CD)

## Quick Start

### Prerequisites
```bash
# Install .NET 8 SDK
dotnet --version  # Must be 8.0+

# Install Docker
docker --version

# Install Docker Compose
docker-compose --version
```

### Option 1: Run with Docker Compose (Recommended)

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

The API will be available at `http://localhost:5000` and Swagger at `http://localhost:5000/swagger`

### Option 2: Run Locally

```bash
# 1. Clone the repository
git clone <repository-url>
cd Software-Architecture-Lab

# 2. Choose an architecture (example: Clean Architecture)
cd src/CleanArchitecture/API

# 3. Restore dependencies
dotnet restore

# 4. Configure connection string in appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CleanArchDB;User Id=sa;Password=YourPassword123;TrustServerCertificate=True"
  }
}

# 5. Apply migrations
dotnet ef database update --project ../Infrastructure

# 6. Run the application
dotnet run

# The API will be at https://localhost:7001
```

### Option 3: Run in Kubernetes

```bash
# 1. Build Docker images
docker build -t clean-architecture-api:latest -f docker/CleanArchitecture.Dockerfile .

# 2. Apply Kubernetes manifests
kubectl apply -f kubernetes/clean-architecture/

# 3. Verify pods
kubectl get pods

# 4. Access the API
kubectl port-forward svc/clean-architecture-api 5000:80
```

## Architecture Guides

### [Clean Architecture](./docs/clean-architecture/README.md)
- Pros and cons
- Comparison with other architectures
- Real-world use cases
- Best practices
- Step-by-step implementation

### [Domain-Driven Design](./docs/ddd/README.md)
- Tactical patterns (Entities, Value Objects, Aggregates)
- Strategic patterns (Bounded Contexts, Context Mapping)
- Ubiquitous Language
- Hexagonal architecture
- E-commerce examples

### [Event-Driven Architecture](./docs/eda/README.md)
- Event Sourcing
- CQRS (Command Query Responsibility Segregation)
- Messaging with RabbitMQ
- Sagas and orchestration
- Event Storming

## Security

### JWT Authentication
```csharp
// Basic configuration included
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* ... */ });
```

### Rate Limiting
```csharp
// Protection against abuse
services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("fixed", options => {
        options.Window = TimeSpan.FromMinutes(1);
        options.PermitLimit = 100;
    });
});
```

### Keycloak Integration
See full documentation in [docs/authentication/keycloak.md](./docs/authentication/keycloak.md)

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Integration tests (requires Docker)
dotnet test --filter Category=Integration
```

## Architecture Comparison

| Feature | Clean Architecture | DDD | EDA |
|---------|-------------------|-----|-----|
| **Complexity** | Medium | High | High |
| **Learning Curve** | Medium | High | High |
| **Scalability** | High | High | Very High |
| **Testability** | Very High | High | Medium |
| **Maintainability**| Very High | High | Medium |
| **Performance** | High | High | Very High |
| **Best For** | APIs, Microservices | Complex domains | Distributed systems |
| **Team Size** | Small-Large | Medium-Large | Large |

## Learning Resources

### Recommended Books
- **Clean Architecture** - Robert C. Martin (Uncle Bob)
- **Domain-Driven Design** - Eric Evans
- **Implementing Domain-Driven Design** - Vaughn Vernon
- **Building Event-Driven Microservices** - Adam Bellemare

### SOLID Principles
See full guide in [docs/principles/SOLID.md](./docs/principles/SOLID.md)

### Clean Code
See full guide in [docs/principles/CleanCode.md](./docs/principles/CleanCode.md)

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License. See `LICENSE` for more details.

## Contact

For questions, suggestions or issues, please open an issue on GitHub.

---

**Happy Coding!**
