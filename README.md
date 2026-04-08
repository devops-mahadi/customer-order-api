# Customer Order API

A **.NET 10.0 ASP.NET Core Web API** for managing customers and orders with **GDPR compliance**, **JWT authentication**, and **comprehensive audit logging**.

## Features

- **Customer & Order Management** - Full CRUD operations with clean architecture
- **JWT Authentication** - Secure token-based authentication via AuthService
- **GDPR Compliance** - Articles 7, 17, 20, 30 implementation
  - Right to Data Portability (Export)
  - Right to Erasure (Anonymization)
  - Consent Management
  - Audit Logging
- **SQL Server Integration** - EF Core with migrations
- **Docker Support** - Complete Docker Compose setup
- **Comprehensive Testing** - Unit and integration tests with Testcontainers
- **Clean Architecture** - Domain, Application, Infrastructure, Presentation layers

## Architecture

```
CustomerOrder/
├── Domain/           # Entities, Interfaces, Constants
├── Application/      # Services, Business Logic
├── Infrastructure/   # Repositories, DbContext, Middleware
└── Presentation/     # Controllers, DTOs

AuthService/          # JWT Token Generation Service
```

## Quick Start

### Option 1: Docker (Recommended)

**Prerequisites**: Docker & Docker Compose

```bash
docker compose build
docker compose up -d
```

**Services will be available at:**
- CustomerOrder API: http://localhost:8080
- AuthService: http://localhost:7080
- MSSQL: localhost:1433

### Option 2: Local Development

**Prerequisites**: .NET 10.0 SDK, SQL Server

```bash
# Restore dependencies
dotnet restore

# Run AuthService
dotnet run --project AuthService

# Run CustomerOrder (in another terminal)
dotnet run --project CustomerOrder
```

## API Usage

### 1. Generate JWT Token

```bash
curl -X POST http://localhost:7080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","role":"Admin"}'
```

### 2. Create a Customer

```bash
curl -X POST http://localhost:8080/api/customers \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "555-1234"
  }'
```

### 3. GDPR - Export Customer Data

```bash
curl -X GET http://localhost:8080/api/customers/john.doe@example.com/export \
  -H "Authorization: Bearer {TOKEN}"
```

### 4. GDPR - Grant Consent

```bash
curl -X POST http://localhost:8080/api/customers/john.doe@example.com/consents \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "consentType": "Marketing",
    "consentVersion": "1.0"
  }'
```

**Full API documentation**: Use Swagger at http://localhost:8080/swagger

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Generate JWT token |

### Customers

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/customers` | Get all customers |
| GET | `/api/customers/{email}` | Get customer by email |
| POST | `/api/customers` | Create customer |
| PUT | `/api/customers/{email}` | Update customer |
| DELETE | `/api/customers/{email}` | Delete customer |

### GDPR Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/customers/{email}/export` | Export all customer data (Article 20) |
| POST | `/api/customers/{email}/anonymize` | Anonymize customer (Article 17) |
| POST | `/api/customers/{email}/consents` | Grant consent (Article 7) |
| GET | `/api/customers/{email}/consents` | Get all consents |
| DELETE | `/api/customers/{email}/consents/{type}` | Revoke consent |

### Orders

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/orders/{orderNumber}` | Get order by number |
| GET | `/api/orders/customer/{email}` | Get customer orders |
| GET | `/api/orders?startDate=&endDate=&status=&pageNumber=&pageSize=` | Get filtered orders |
| POST | `/api/orders` | Create order |

## Testing

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "Category=Unit"

# Run integration tests
dotnet test --filter "Category=Integration"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Technology Stack

- **.NET 10.0** - Latest .NET framework
- **ASP.NET Core Web API** - RESTful API framework
- **Entity Framework Core 10.0** - ORM for database access
- **SQL Server 2022** - Relational database
- **JWT Authentication** - Secure token-based auth
- **xUnit** - Testing framework
- **FluentAssertions** - Fluent test assertions
- **Moq** - Mocking framework
- **Testcontainers** - Integration testing with real databases
- **Docker** - Containerization
- **Swagger/OpenAPI** - API documentation
- **Jenkins/Azure-DevOps** - CI/CD pipline file

## GDPR Compliance

This API implements the following GDPR requirements:

### Article 7 - Consent
- Track customer consent for different processing purposes
- Store consent version, date, IP address, and user agent
- Allow consent withdrawal (revocation)

### Article 17 - Right to Erasure
- Anonymize customer data while preserving business records
- Soft delete with anonymization of PII
- Automated data retention policies

### Article 20 - Right to Data Portability
- Export customer data in machine-readable format (JSON)
- Include all related data (orders, consents)

### Article 30 - Record of Processing Activities
- Comprehensive audit logging via middleware
- Track all data access and modifications
- Store IP address, user agent, timestamp, changes
- Automatic retention policy (3 years default)

## Development

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName --project CustomerOrder

# Update database
dotnet ef database update --project CustomerOrder

# Rollback migration
dotnet ef migrations remove --project CustomerOrder
```

### Code Formatting

```bash
# Format code
dotnet format
```

### Run Locally

```bash
# Terminal 1 - AuthService
dotnet run --project AuthService

# Terminal 2 - CustomerOrder API
dotnet run --project CustomerOrder

# Terminal 3 - Watch mode (auto-reload)
dotnet watch --project CustomerOrder
```

## Docker Commands

### Using Docker Compose

```bash
docker compose build
docker compose up -d
docker compose logs -f
docker compose down
```

## Configuration

### Environment Variables

**CustomerOrder API**:
- `ConnectionStrings__DefaultConnection` - Database connection string
- `JwtKey` - JWT signing key
- `DataRetention__DeletedCustomersRetentionDays` - GDPR retention (days)
- `DataRetention__AuditLogRetentionYears` - Audit log retention (years)

**AuthService**:
- `JwtKey` - JWT signing key (must match CustomerOrder)
- `JwtIssuer` - Token issuer
- `JwtAudience` - Token audience

## CI/CD

The project includes complete CI/CD pipeline configurations:

### Jenkins Pipeline & Azure DevOps Pipeline

**Pipeline Features**:
1. Code checkout from Git
2. .NET dependency restoration
3. Solution build (Release configuration)
4. Unit tests execution
5. Integration tests execution
6. Code coverage report generation
7. **Docker image building** (CustomerOrder + AuthService)
8. **Docker image testing**
9. **Docker image pushing to Docker Hub**
10. Test results publishing
11. Cleanup
```

## License

This project is for educational/demonstration purposes.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Run `dotnet format`
6. Submit a pull request

---

**Built with Clean Architecture principles for maintainability, testability, and GDPR compliance.**
