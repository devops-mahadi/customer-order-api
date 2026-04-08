# CustomerOrder.Tests

Unit and integration tests for the Customer Order API.

## Quick Start

### Run All Tests
```bash
dotnet test
```

### Run Unit Tests (Fast, No Dependencies)
```bash
dotnet test --filter "Category=Unit"
# OR
dotnet test --filter "FullyQualifiedName~Unit"
```

### Run Integration Tests - Container (Slow, Requires Docker)
```bash
dotnet test --filter "FullyQualifiedName~Integration.Container"
```

### Run with Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Prerequisites

- .NET 10.0 SDK
- Docker Desktop (for Container integration tests only)

## Test Categories

Tests use xUnit traits for categorization in CI/CD pipelines:

### Unit Tests
- **Trait**: `[Trait("Category", "Unit")]`
- **Purpose**: Test components in isolation with mocked dependencies
- **Database**: No database (mocked repositories)

### Integration Tests
- **Trait**: `[Trait("Category", "Integration")]`
- **Purpose**: Test components working together
- **Database**: Docker container MSSQL database

### Example Usage

```csharp
using Xunit;
using Moq;
using FluentAssertions;

namespace CustomerOrder.Tests.Unit.Services;

[Trait("Category", "Unit")]
public class CustomerServiceTests
{
    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsCustomer()
    {
        // Test implementation
    }
}
```

## CI/CD Integration

Jenkins automatically filter tests by category:
- **Unit Tests**: Run on every build
- **Integration Tests**: Run based on pipeline parameter
