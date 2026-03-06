# Event-Driven Architecture (EDA)

Event-Driven Architecture is a software architecture paradigm promoting the production, detection, consumption of, and reaction to events.

## Concepts
- **Event**: A significant change in state.
- **Event Producer**: A system that emits events.
- **Event Consumer**: A system that reacts to events and performs an action.
- **Event Channel**: A messaging mechanism ensuring that events are transmitted.

## Patterns
- **Event Sourcing**: Instead of storing just the current state of a domain object, store every event that ever happened to it.
- **CQRS (Command-Query Responsibility Segregation)**: Segregate operations that read data from operations that update data by using separate interfaces.
- **Sagas**: Manage data consistency across microservices in distributed transaction scenarios.
