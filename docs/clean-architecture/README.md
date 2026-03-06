# Clean Architecture

Clean Architecture, introduced by Robert C. Martin (Uncle Bob), is a software design philosophy that promotes the separation of concerns by creating independent layers in a software system.

## Key Concepts
1. **Independent of Frameworks**: The architecture does not depend on the existence of some library of feature-laden software. This allows you to use frameworks as tools, rather than having to cram your system into their limited constraints.
2. **Testable**: The business rules can be tested without the UI, Database, Web Server, or any other external element.
3. **Independent of UI**: The UI can change easily, without changing the rest of the system.
4. **Independent of Database**: You can swap out Oracle or SQL Server, for Mongo, BigTable, CouchDB, or something else. Your business rules are not bound to the database.
5. **Independent of any external agency**: Business rules simply don't know anything at all about the outside world.

## Layers
- **Domain Layer**: Contains enterprise-wide business rules and entities.
- **Application Layer**: Contains application-specific business rules (Use Cases).
- **Presentation Layer**: Handles UI and API endpoints.
- **Infrastructure Layer**: Handles databases, external APIs, and frameworks.
