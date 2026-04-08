# Docker Deployment Guide

This guide explains how to run the CustomerOrder API and AuthService using Docker Compose.

## Prerequisites

- Docker Engine 20.10+
- Docker Compose V2+
- Make (optional, for convenience commands)

## Architecture

The Docker Compose setup includes three services:

```
┌─────────────────┐
│   AuthService   │
│   Port: 5001    │
└─────────────────┘

┌─────────────────┐      ┌─────────────────┐
│ CustomerOrder   │─────▶│  MSSQL Server   │
│   Port: 5000    │      │   Port: 1433    │
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
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","role":"Admin"}'
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
curl -X GET http://localhost:5000/api/customers \
  -H "Authorization: Bearer {TOKEN}"
```

### 3. Create a Customer

```bash
curl -X POST http://localhost:5000/api/customers \
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

## Volumes

### mssql-data

Persists MSSQL database files across container restarts.

```bash
# View volume
docker volume ls | grep mssql

# Remove volume (WARNING: deletes all data)
docker volume rm customer-order-api_mssql-data
```

## Networking

All services communicate via the `customerorder-network` bridge network.

```bash
# Inspect network
docker network inspect customer-order-api_customerorder-network
```

## Troubleshooting

### Service won't start

```bash
# Check logs
make logs

# Check specific service
docker compose logs customerorder
```

### Database connection issues

```bash
# Check MSSQL health
docker compose ps mssql

# Test connection
docker compose exec mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong!Passw0rd" -Q "SELECT 1"
```

### Port already in use

```bash
# Find process using port
lsof -i :5000

# Or kill the process
kill -9 $(lsof -t -i:5000)
```

### Reset everything

```bash
# Complete reset
make clean
docker system prune -a
make build
make up
```

## Production Considerations

⚠️ **This setup is for development only. For production:**

1. **Security**
   - Change default passwords
   - Use secrets management (Docker Secrets, Azure Key Vault)
   - Enable HTTPS with proper certificates
   - Use environment-specific configuration files

2. **Performance**
   - Use production SQL Server edition
   - Configure connection pooling
   - Set up load balancing
   - Enable caching (Redis)

3. **Monitoring**
   - Add health check endpoints
   - Configure logging (ELK stack, Application Insights)
   - Set up monitoring (Prometheus, Grafana)
   - Enable distributed tracing

4. **Scaling**
   - Use Docker Swarm or Kubernetes
   - Set up database replication
   - Configure auto-scaling
   - Use managed database services (Azure SQL)

## Development Workflow

### Update Code

```bash
# After code changes, rebuild and restart
make rebuild

# Or rebuild specific service
docker compose build customerorder
docker compose up -d customerorder
```

### Database Migrations

```bash
# Add migration (outside Docker)
dotnet ef migrations add MigrationName --project CustomerOrder

# Apply migration
make migrate
```

### Debugging

```bash
# Access container shell
make shell-api

# View real-time logs
make logs-api
```

## Clean Up

```bash
# Stop services (keep volumes)
make down

# Remove everything
make clean

# Remove unused Docker resources
docker system prune -a --volumes
```

## Makefile Commands Reference

```bash
make help          # Show all available commands
make build         # Build Docker images
make up            # Start services
make down          # Stop services
make restart       # Restart services
make logs          # View all logs
make logs-api      # CustomerOrder logs
make logs-auth     # AuthService logs
make logs-db       # MSSQL logs
make clean         # Remove everything
make migrate       # Run migrations
make test          # Run tests
make status        # Show service status
make rebuild       # Clean rebuild
make shell-api     # Access API container
make shell-auth    # Access Auth container
make shell-db      # Access DB container
```

## Support

For issues or questions:
- Check logs: `make logs`
- Check service status: `make status`
- Reset environment: `make clean && make build && make up`
