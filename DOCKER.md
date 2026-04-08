# Docker Deployment Guide

This guide explains how to run the CustomerOrder API and AuthService using Docker Compose.

## Prerequisites

- Docker Engine 20.10+
- Docker Compose V2+

## Architecture

The Docker Compose setup includes three services:

```
┌─────────────────┐
│   AuthService   │
│   Port: 7080    │
└─────────────────┘

┌─────────────────┐      ┌─────────────────┐
│ CustomerOrder   │─────▶│  MSSQL Server   │
│   Port: 8080    │      │   Port: 1433    │
└─────────────────┘      └─────────────────┘
```

### Services

1. **mssql** - SQL Server 2022 Developer Edition
   - Port: `1433`
   - Database: `CustomerOrderDb`
   - Credentials: `sa / YourStrong!Passw0rd`
   - Health checks enabled
   - Data persistence via Docker volume

2. **authservice** - JWT Token Generation Service
   - HTTP Port: `7080`
   - HTTPS Port: `7081`
   - Generates JWT tokens for API authentication

3. **customerorder** - Main API Service
   - HTTP Port: `8080`
   - HTTPS Port: `8081`
   - Depends on MSSQL (waits for health check)
   - Auto-applies migrations on startup

## Quick Start

### Using Docker Compose Directly

```bash
# Build images
docker compose build

# Start services
docker compose up -d

# View logs
docker compose logs -f

# Stop services
docker compose down
```

### Database Operations

```bash

# Access MSSQL shell
docker compose exec mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd"
```

## Service Endpoints

Once running, the services are available at:

| Service | HTTP | HTTPS | Description |
|---------|------|-------|-------------|
| AuthService | http://localhost:7080 | https://localhost:7081 | JWT token generation |
| CustomerOrder API | http://localhost:8080 | https://localhost:8081 | Main API |
| MSSQL | localhost:1433 | - | Database |

### Swagger/OpenAPI

- **CustomerOrder API**: http://localhost:8080/swagger
- **AuthService**: http://localhost:7080/swagger

## Testing the Setup

### 1. Generate JWT Token

```bash
curl -X POST http://localhost:7080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"admin123"}'
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

### 2. Use Token to Access API

```bash
# Replace {TOKEN} with the actual token from step 1
curl -X GET http://localhost:8080/api/customers \
  -H "Authorization: Bearer {TOKEN}"
```

### 3. Create a Customer

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

## Environment Variables

### MSSQL

| Variable | Default | Description |
|----------|---------|-------------|
| `SA_PASSWORD` | `YourStrong!Passw0rd` | SA user password |
| `ACCEPT_EULA` | `Y` | Accept SQL Server license |
| `MSSQL_PID` | `Developer` | SQL Server edition |

### CustomerOrder API

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | See compose file | Database connection |
| `JwtKey` | (see compose) | JWT signing key |
| `JwtIssuer` | `CustomerOrderApi` | Token issuer |
| `JwtAudience` | `CustomerOrderClient` | Token audience |
| `DataRetention__DeletedCustomersRetentionDays` | `90` | GDPR retention |
| `DataRetention__AuditLogRetentionYears` | `3` | Audit log retention |

### AuthService

| Variable | Default | Description |
|----------|---------|-------------|
| `JwtKey` | (see compose) | JWT signing key |
| `JwtIssuer` | `CustomerOrderApi` | Token issuer |
| `JwtAudience` | `CustomerOrderClient` | Token audience |
