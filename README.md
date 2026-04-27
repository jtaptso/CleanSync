# CleanSync

A Business Partner synchronization solution that syncs customer data between SAP Business One and E-commerce platforms.

## Overview

CleanSync is a .NET 10 solution that facilitates bidirectional synchronization of Business Partners (customers) between SAP Business One and various E-commerce platforms (Shopify, Amazon, etc.). It provides a REST API for managing synchronization operations and a Blazor Web interface for monitoring and configuration.

## Architecture

The solution follows a clean architecture pattern with four main projects:

```
CleanSync.slnx
├── src/
│   ├── CleanSync.Api/          # REST API (ASP.NET Core)
│   ├── CleanSync.Application/  # Business logic & DTOs
│   ├── CleanSync.Domain/       # Domain entities & interfaces
│   ├── CleanSync.Infrastructure/  # Data access & external services
│   └── CleanSync.Web/          # Blazor Web UI
└── tests/
    └── CleanSync.Tests/        # Unit & integration tests
```

## Features

- **SAP Business One Integration**: Syncs customer data with SAP Business One via Service Layer API
- **E-commerce Platform Support**: Interfaces for Shopify, Amazon, and other platforms (mock implementations available)
- **Bidirectional Sync**: Synchronize data from E-commerce to SAP and vice versa
- **Health Checks**: Built-in monitoring for database, SAP, and E-commerce connectivity
- **Swagger Documentation**: API documentation available at `/swagger`
- **Demo Mode**: Mock services for development and testing without live connections

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (for production) or In-Memory database (for demo mode)
- (Optional) SAP Business One Service Layer access

## Configuration

Configuration is managed via `appsettings.json` in the API project:

```json
{
  // Use in-memory database (true) or SQL Server (false)
  // UseInMemoryDb: true for development/demo
  
  // Use mock services instead of real SAP/E-commerce connections
  // DemoMode: true for development without live connections
  
  // SQL Server connection string
  // ConnectionStrings: {
  //   DefaultConnection: ...
  // }
  
  // SAP Service Layer settings
  // SapConnection: {
  //   ServiceLayerUrl: ...,
  //   CompanyDb: ...,
  //   UserName: ...,
  //   Password: ...
  // }
}
```

### Environment Variables

For production deployments, use environment variables:

| Variable | Description |
|----------|-------------|
| `DB_CONNECTION_STRING` | SQL Server connection string |
| `SAP_SERVICE_LAYER_URL` | SAP Service Layer endpoint |
| `SAP_COMPANY_DB` | SAP Company Database name |
| `SAP_USERNAME` | SAP Service Layer username |
| `SAP_PASSWORD` | SAP Service Layer password |

## Running the Application

### Development with Demo Mode

```bash
# Run the API with demo mode enabled
cd src/CleanSync.Api
dotnet run

# In a separate terminal, run the Blazor Web UI
cd src/CleanSync.Web
dotnet run
```

Or set environment variables to enable demo mode:

```bash
export ASPNETCORE_Environment=Development
export DemoMode=true
export UseInMemoryDb=true
dotnet run --project src/CleanSync.Api
```

### Production Mode

```bash
# Set required environment variables
export DB_CONNECTION_STRING='Server=localhost;Database=CleanSync;Trusted_Connection=True'
export SAP_SERVICE_LAYER_URL='https://your-sap-server:50000/b1s/v1'
export SAP_COMPANY_DB='YOURCOMPANY'
export SAP_USERNAME='manager'
export SAP_PASSWORD='your-password'

# Run the API
dotnet run --project src/CleanSync.Api
```

## API Endpoints

### Business Partners

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/partners` | List all business partners |
| `GET` | `/api/partners/{cardCode}` | Get partner by SAP CardCode |
| `POST` | `/api/partners/sync` | Trigger sync from E-commerce to SAP |

### Sync Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/sync/start` | Start synchronization job |
| `GET` | `/api/sync/status/{id}` | Get sync job status |
| `GET` | `/api/sync/history` | Get sync history |

### Health Checks

| Endpoint | Description |
|----------|-------------|
| `/health` | Overall health status |
| `/health/ready` | Readiness probe (includes DB, SAP, E-commerce) |
| `/health/live` | Liveness probe |

## Swagger Documentation

When running in Development mode, Swagger UI is available at:

- API Documentation: http://localhost:5000/swagger

## Blazor Web Interface

The Web project provides a user-friendly interface for:

- Viewing business partners
- Monitoring sync operations
- Configuring connections
- Viewing sync history

## Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collectcoverage
```

## Project Structure

### CleanSync.Domain
Contains core domain entities:
- `BusinessPartner`: Represents a customer in SAP
- `SyncLog`: Tracks synchronization operations

### CleanSync.Application
Business logic layer:
- `BusinessPartnerSyncService`: Main synchronization service
- DTOs for SAP and E-commerce data transfer

### CleanSync.Infrastructure
External integrations:
- `BusinessPartnerRepository`: Database access (EF Core)
- `SapServiceLayerBusinessPartnerService`: SAP Service Layer client
- `MockEcommerceBusinessPartnerService`: Mock for development

### CleanSync.Api
REST API:
- Controllers for business partners and sync operations
- Health checks for monitoring
- Swagger documentation

### CleanSync.Web
Blazor Server application:
- Interactive UI for monitoring and configuration

## License

MIT License