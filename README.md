# i4Twins Sensor Data Processing Service

A backend service for ingesting, cleaning, and aggregating sensor readings from IoT devices.

## Architecture

### Clean Architecture
The solution follows Clean Architecture principles with clear separation of concerns:
- **Domain**: Core business logic and entities (Reading, validation, identity)
- **Application**: Use cases and DTOs (ReadingService, interfaces)
- **Infrastructure**: Data persistence and external services (SQLite repository, file processing)
- **API**: HTTP endpoints and middleware

### Why Clean Architecture?
- **Testability**: Domain logic can be unit tested without infrastructure dependencies
- **Flexibility**: Easy to swap data sources (SQLite → PostgreSQL) or add new API endpoints
- **Maintainability**: Clear boundaries make it easy to locate and modify code

## Technology Stack

- **.NET 8** with C# 12
- **SQLite** with Dapper for data persistence
- **ASP.NET Core Web API** with Swagger/OpenAPI
- **xUnit** with Moq for testing

## Key Decisions & Trade-offs

### Storage: SQLite
- **Why**: No external dependencies, suitable for ~2000 records, supports transactions and indexing
- **Trade-off**: Not ideal for high-volume production, but perfect for this task

### Deduplication Strategy
- **Identity**: (DeviceId, Metric, Timestamp, Sequence) combination
- **Approach**: Check existence before insert using composite key
- **Why**: Guarantees idempotency even with out-of-order data

### Empty Buckets
- **Decision**: Omit empty buckets from response
- **Why**: Reduces payload size; client can infer empty buckets from time range

### Validation Rules
| Validation | Action |
|------------|--------|
| Empty DeviceId/Metric | Reject |
| Invalid timestamp (future, out of range) | Reject |
| Non-numeric value (NaN, Infinity) | Reject |
| Negative sequence | Reject |
| Out-of-physical-range values | Reject |
| Duplicate (same identity) | Skip |

## How to Run

### Prerequisites
- .NET 8 SDK
- Your favorite IDE (VS Code, Visual Studio, Rider)

### Steps
```bash
# Clone or extract the repository
cd i4Twins_Backend_Task

# Build the solution
dotnet build

# Run the API
dotnet run --project src/i4Twins.API

# Run tests
dotnet test