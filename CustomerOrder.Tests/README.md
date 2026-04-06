# CustomerOrder.Tests

Unit and integration tests for the Customer Order API.

## Quick Start

### Run All Tests
```bash
dotnet test
```

### Run Unit Tests (Fast, No Dependencies)
```bash
dotnet test --filter "FullyQualifiedName~Unit"
```

### Run Integration Tests - InMemory (Fast, No Docker)
```bash
dotnet test --filter "FullyQualifiedName~Integration.InMemory"
```

### Run Integration Tests - Container (Slow, Requires Docker)
```bash
dotnet test --filter "FullyQualifiedName~Integration.Container"
```

## Prerequisites

- .NET 10.0 SDK
- Docker Desktop (for Container integration tests only)

## Test Details

See [TestDesign.md](TestDesign.md) for detailed testing strategy, architecture, and troubleshooting.
