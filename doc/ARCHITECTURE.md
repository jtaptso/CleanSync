# CleanSync Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CleanSync Solution                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐       │
│  │  CleanSync.Web  │     │  CleanSync.Api  │     │  CleanSync.Tests │       │
│  │   (Blazor UI)   │────▶│   (REST API)    │────▶│  (Unit Tests)   │       │
│  └─────────────────┘     └────────┬────────┘     └─────────────────┘       │
│                                  │                                          │
│                         ┌────────▼────────┐                                │
│                         │     src/        │                                │
│                         │                 │                                │
│                         │  ┌─────────────┐│                                │
│                         │  │Application │ │                                │
│                         │  │  Services   ││                                │
│                         │  └──────┬──────┘│                                │
│                         │         │       │                                │
│                         │  ┌──────▼──────┐│                                │
│                         │  │  Domain     │ │                                │
│                         │  │  Entities   │ │                                │
│                         │  └──────┬──────┘│                                │
│                         │         │       │                                │
│                         │  ┌──────▼──────┐│                                │
│                         │  │Infrastructure││                                │
│                         │  │  Services   │ │                                │
│                         │  └─────────────┘│                                │
│                         └─────────────────┘                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Clean Architecture Layers

```
┌────────────────────────────────────────────────────────┐
│                   Presentation Layer                    │
│                  (CleanSync.Api, Web)                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ Controllers  │  │   Pages/     │  │Health Checks│  │
│  │              │  │Components    │  │             │  │
│  └──────┬───────┘  └──────┬───────┘  └──────────────┘  │
└─────────┼─────────────────┼────────────────────────────┘
          │                 │
          ▼                 ▼
┌────────────────────────────────────────────────────────┐
│                  Application Layer                      │
│                  (CleanSync.Application)                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   Services   │  │     DTOs     │  │  Interfaces  │  │
│  │ BusinessSync │  │ EcommerceDTO │  │ ISapService  │  │
│  └──────┬───────┘  └──────────────┘  └──────────────┘  │
└─────────┼──────────────────────────────────────────────┘
          │
          ▼
┌────────────────────────────────────────────────────────┐
│                    Domain Layer                         │
│                  (CleanSync.Domain)                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  Entities    │  │  Interfaces  │  │   Enums      │  │
│  │BusinessPartner│ │IBusinessRepo │  │ SyncStatus   │  │
│  │   SyncLog    │  │ISyncLogRepo  │  │              │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└────────────────────────────────────────────────────────┘
          │
          ▼
┌────────────────────────────────────────────────────────┐
│               Infrastructure Layer                      │
│              (CleanSync.Infrastructure)                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ Repositories │  │   Services   │  │     Data     │  │
│  │BusinessRepo  │  │SAPService    │  │ DbContext    │  │
│  │ SyncLogRepo  │  │EcommerceMock │  │              │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└────────────────────────────────────────────────────────┘
```

## Data Flow Diagram

### Sync Flow: E-commerce → SAP

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐     ┌─────────────┐
│ E-commerce  │────▶│  Application │────▶│   Domain     │────▶│    SAP      │
│   Platform  │     │   Service    │     │   Entities   │     │Service Layer│
│(Shopify/Amazon)   │ BusinessSync │     │   Repository │     │   API       │
└─────────────┘     └──────┬───────┘     └──────────────┘     └─────────────┘
                           │
                           │ On Success:
                           ▼
                    ┌──────────────┐
                    │   SyncLog    │
                    │   Updated    │
                    └──────────────┘
```

### Sync Flow Details

```
1. BusinessPartnerSyncService.SyncFromEcommerceToSapAsync() is called
           │
           ▼
2. Fetch customers from E-commerce platform
   └─ MockEcommerceBusinessPartnerService.GetCustomersAsync()
           │
           ▼
3. For each customer:
   a. Check if BusinessPartner exists in DB (by ExternalId)
      └─ IBusinessPartnerRepository.GetByExternalIdAsync()
           │
           ├─ If exists: Update SAP via SAPServiceLayerBusinessPartnerService
           │   └─ ISapBusinessPartnerService.UpdateAsync()
           │
           └─ If new: Create in SAP
               └─ ISapBusinessPartnerService.CreateAsync()
               └─ IBusinessPartnerRepository.AddAsync()
           │
           ▼
4. Record sync results in SyncLog
   └─ ISyncLogRepository.AddAsync() / UpdateAsync()
```

## Component Architecture

### CleanSync.Api (REST API)

```
Controllers/
├── BusinessPartnersController.cs    # CRUD for BusinessPartners
│   ├── GET /api/partners            # List all partners
│   ├── GET /api/partners/{id}       # Get partner by ID
│   └── GET /api/partners/sync-logs  # Get sync history
│
└── SyncController.cs                # Sync operations
    └── POST /api/sync/business-partners  # Trigger sync

HealthChecks/
├── SapConnectionHealthCheck.cs      # SAP connectivity monitor
└── EcommerceConnectionHealthCheck.cs # E-commerce connectivity monitor
```

### CleanSync.Application (Business Logic)

```
Services/
└── BusinessPartnerSyncService.cs
    ├── SyncFromEcommerceToSapAsync()  # Main sync orchestrator
    ├── GenerateUniqueCardCodeAsync()  # Generate SAP CardCodes
    └── MapToSapDto()                  # Map EcommerceCustomer to SAP DTO

DTOs/
├── EcommerceCustomerDto.cs          # E-commerce customer model
├── SapBusinessPartnerDto.cs         # SAP Business Partner model
├── SapConnectionSettings.cs         # SAP connection config
└── SyncResultDto.cs                 # Sync operation result
```

### CleanSync.Domain (Core Business)

```
Entities/
├── BusinessPartner.cs               # Core business partner entity
│   ├── CardCode (SAP identifier)
│   ├── ExternalId (e-commerce ID)
│   ├── SyncStatus
│   └── LastSyncedAt
│
└── SyncLog.cs                       # Sync operation audit log
    ├── Direction (ToSap/FromSap)
    ├── Status (Pending/Synced/Failed)
    └── SuccessCount/FailureCount

Interfaces/
├── IBusinessPartnerRepository.cs    # Data access contract
└── ISyncLogRepository.cs            # Audit log access contract
```

### CleanSync.Infrastructure (External Integrations)

```
Data/
└── CleanSyncDbContext.cs            # EF Core DbContext

Repositories/
├── BusinessPartnerRepository.cs     # SQL Server / InMemory implementation
└── SyncLogRepository.cs             # Sync log persistence

Services/
├── SapServiceLayerBusinessPartnerService.cs
│   ├── Session management (B1SESSION cookie)
│   ├── Login/Logout automation
│   └── CRUD operations via SAP Service Layer
│
├── MockSapBusinessPartnerService.cs  # Mock for testing
└── MockEcommerceBusinessPartnerService.cs # Mock with sample data
```

## Dependency Injection Configuration

```csharp
// In CleanSync.Api/Program.cs

// Database
if (useInMemory)
    services.AddDbContext<CleanSyncDbContext>(options => 
        options.UseInMemoryDatabase());
else
    services.AddDbContext<CleanSyncDbContext>(options => 
        options.UseSqlServer(connectionString));

// Repositories
services.AddScoped<IBusinessPartnerRepository, BusinessPartnerRepository>();
services.AddScoped<ISyncLogRepository, SyncLogRepository>();

// External Services (based on DemoMode)
if (demoMode)
{
    services.AddScoped<ISapBusinessPartnerService, MockSapBusinessPartnerService>();
    services.AddScoped<IEcommerceBusinessPartnerService, MockEcommerceBusinessPartnerService>();
}
else
{
    services.AddScoped<ISapBusinessPartnerService, SapServiceLayerBusinessPartnerService>();
    services.AddScoped<IEcommerceBusinessPartnerService, MockEcommerceBusinessPartnerService>();
}

// Application Services
services.AddScoped<BusinessPartnerSyncService>();
```

## SAP Service Layer Integration

### Authentication Flow

```
┌─────────────┐    POST /Login     ┌─────────────┐
│   Client    │───────────────────▶│   SAP SL    │
│             │   { CompanyDB,     │             │
│             │    UserName,       │   B1SESSION │
│             │    Password }      │   cookie    │
└─────────────┘◀──────────────────┘─────────────┘
```

### Session Management

- Sessions expire after `SessionTimeoutMinutes` (default: 30 min)
- Automatic logout on `Dispose()` to prevent orphan sessions
- Thread-safe session management using `SemaphoreSlim`
- Double-check locking pattern for concurrent requests

### API Operations

| Operation | Method | Endpoint | Description |
|-----------|--------|----------|-------------|
| Create | POST | `/BusinessPartners` | Create new Business Partner |
| Read | GET | `/BusinessPartners('{CardCode}')` | Get by CardCode |
| Update | PATCH | `/BusinessPartners('{CardCode}')` | Partial update |
| List | GET | `/BusinessPartners?$filter=...` | Query customers |

## Configuration Reference

```json
{
  // Database Configuration
  ConnectionStrings: {
    DefaultConnection: // SQL Server connection string
  },
  UseInMemoryDb: true/false,
  
  // SAP Connection
  SapConnection: {
    ServiceLayerUrl: // e.g., https://sap-server:50000/b1s/v1
    CompanyDb: // SAP Company Database name
    UserName: // Service Layer username
    Password: // Service Layer password
    SessionTimeoutMinutes: 30
  },
  
  // Demo Mode (uses mock services)
  DemoMode: true/false,
  
  // Health Checks
  HealthChecks: {
    Enabled: true,
    DetailedErrors: true,
    ReadinessChecks: [
      // Checks to include in /health/ready
    ]
  }
}
```

## Error Handling Strategy

1. **Sync Failures**: Individual customer sync failures are logged but don't halt the entire sync
2. **Connection Errors**: Health checks expose connectivity issues via `/health` endpoints
3. **Authentication Failures**: Session management handles re-authentication automatically
4. **Validation Errors**: API returns structured error responses with details

## Future Extension Points

- Add more E-commerce platform adapters (WooCommerce, Magento)
- Implement bidirectional sync (SAP → E-commerce)
- Add retry policies with Polly
- Implement event-driven sync triggers
- Add WebSocket support for real-time sync status