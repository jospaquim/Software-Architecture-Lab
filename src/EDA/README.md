# Event-Driven Architecture (EDA) Project

This project implements a system based on Event-Driven Architecture, decoupling microservices and focusing on scalability.

## Architecture
- **Producer Microservices**: Publish events when state changes.
- **Consumer Microservices**: Subscribe to relevant events and update local state or trigger actions.

## Patterns Included
- CQRS (Command Query Responsibility Segregation)
- Event Sourcing (optional implementation using Kafka)
- Saga Pattern for distributed transactions.

## Setup Instructions
Please refer to the docker-compose setup instructions in the main repository README.
