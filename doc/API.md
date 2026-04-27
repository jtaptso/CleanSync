# CleanSync API Reference

## Base URL

```
Development: http://localhost:5000
Production:  https://your-domain.com
```

## API Endpoints

### Business Partners

#### GET /api/businesspartners

Retrieve all business partners.

**Response** `200 OK`
```json
[
  {
    id: 1,
    cardCode: 'AMZJOHNDOE12',
    cardName: 'John Doe',
    cardType: 'cCustomer',
    federalTaxId: '12345678',
    phone1: '+1234567890',
    email: 'john.doe@example.com',
    website: null,
    address: '123 Main St',
    city: 'New York',
    country: 'US',
    zipCode: '10001',
    source: 'Shopify',
    externalId: 'sh_abc123',
    syncStatus: 'Synced',
    lastSyncedAt: '2024-01-15T10:30:00Z',
    createdAt: '2024-01-10T08:00:00Z',
    updatedAt: '2024-01-15T10:30:00Z'
  }
]
```

---

#### GET /api/businesspartners/{id}

Retrieve a business partner by ID.

**Parameters**
| Name | Type | Description |
|------|------|-------------|
| `id` | integer | Business Partner ID |

**Response** `200 OK`
```json
{
  id: 1,
  cardCode: 'AMZJOHNDOE12',
  cardName: 'John Doe',
  cardType: 'cCustomer',
  federalTaxId: '12345678',
  phone1: '+1234567890',
  email: 'john.doe@example.com',
  website: null,
  address: '123 Main St',
  city: 'New York',
  country: 'US',
  zipCode: '10001',
  source: 'Shopify',
  externalId: 'sh_abc123',
  syncStatus: 'Synced',
  lastSyncedAt: '2024-01-15T10:30:00Z',
  createdAt: '2024-01-10T08:00:00Z',
  updatedAt: '2024-01-15T10:30:00Z'
}
```

**Response** `404 Not Found`
```json
{
  statusCode: 404,
  message: 'Business Partner not found'
}
```

---

#### GET /api/businesspartners/sync-logs

Retrieve all sync operation logs.

**Response** `200 OK`
```json
[
  {
    id: 1,
    entityType: 'BusinessPartner',
    direction: 'ToSap',
    status: 'Synced',
    startedAt: '2024-01-15T10:00:00Z',
    completedAt: '2024-01-15T10:30:00Z',
    entityCount: 150,
    successCount: 148,
    failureCount: 2,
    errorMessage: 'Failed to sync customer sh_xyz789: Invalid email format; Failed to sync customer am_abc123: Duplicate CardCode'
  }
]
```

---

### Sync Operations

#### POST /api/sync/business-partners

Trigger synchronization of business partners from E-commerce to SAP.

**Request Body** None required.

**Response** `200 OK`
```json
{
  success: true,
  message: 'Synced 148 of 150 customers',
  startedAt: '2024-01-15T10:00:00Z',
  completedAt: '2024-01-15T10:30:00Z',
  totalProcessed: 150,
  successCount: 148,
  failureCount: 2,
  errors: [
    {
      entityId: 'sh_xyz789',
      errorMessage: 'Invalid email format'
    },
    {
      entityId: 'am_abc123',
      errorMessage: 'Duplicate CardCode'
    }
  ]
}
```

**Response** `500 Internal Server Error`
```json
{
  success: false,
  message: 'Sync failed: Unable to connect to SAP Service Layer',
  startedAt: '2024-01-15T10:00:00Z',
  completedAt: '2024-01-15T10:00:05Z',
  totalProcessed: 0,
  successCount: 0,
  failureCount: 0,
  errors: [
    {
      entityId: null,
      errorMessage: 'Unable to connect to SAP Service Layer'
    }
  ]
}
```

---

### Health Checks

#### GET /health

Overall health status of the application.

**Response** `200 OK` (Healthy)
```json
{
  status: 'Healthy',
  totalDuration: '00:00:00.0154682',
  entries: {
    database: {
      status: 'Healthy',
      duration: '00:00:00.0065431',
      description: 'EntityFrameworkCore.Database developer exception page'
    },
    sap: {
      status: 'Healthy',
      duration: '00:00:00.0089247'
    },
    ecommerce: {
      status: 'Healthy',
      duration: '00:00:00.0000000'
    }
  }
}
```

**Response** `503 Service Unavailable` (Unhealthy)
```json
{
  status: 'Unhealthy',
  totalDuration: '00:00:05.1234567',
  entries: {
    database: {
      status: 'Healthy'
    },
    sap: {
      status: 'Unhealthy',
      duration: '00:00:05.0000000',
      description: 'SAP Service Layer connection timeout',
      exception: 'HttpRequestException: Unable to connect to SAP'
    },
    ecommerce: {
      status: 'Healthy'
    }
  }
}
```

---

#### GET /health/ready

Readiness probe - includes database and all connection checks.

---

#### GET /health/live

Liveness probe - always returns healthy if the app is running.

---

## Data Models

### BusinessPartner

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | Primary key |
| `cardCode` | string | SAP Business Partner identifier (max 50 chars) |
| `cardName` | string | Partner name (max 100 chars) |
| `cardType` | string | Partner type (e.g., 'cCustomer') |
| `federalTaxId` | string | Tax identification number |
| `phone1` | string | Primary phone number |
| `email` | string | Email address |
| `website` | string | Website URL (nullable) |
| `address` | string | Street address |
| `city` | string | City name |
| `country` | string | Country code |
| `zipCode` | string | Postal/ZIP code |
| `source` | string | E-commerce source platform |
| `externalId` | string | External system ID |
| `syncStatus` | enum | Sync status (Pending, InProgress, Synced, Failed) |
| `lastSyncedAt` | datetime | Last sync timestamp |
| `createdAt` | datetime | Record creation timestamp |
| `updatedAt` | datetime | Record update timestamp |

### SyncLog

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | Primary key |
| `entityType` | string | Type of entity synced |
| `direction` | string | Sync direction (ToSap, FromSap) |
| `status` | enum | Operation status |
| `startedAt` | datetime | Sync start time |
| `completedAt` | datetime | Sync completion time |
| `entityCount` | integer | Total entities processed |
| `successCount` | integer | Successful syncs |
| `failureCount` | integer | Failed syncs |
| `errorMessage` | string | Error details (nullable) |

### SyncResultDto

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Overall sync success |
| `message` | string | Status message |
| `startedAt` | datetime | Sync start time |
| `completedAt` | datetime | Sync completion time |
| `totalProcessed` | integer | Total entities processed |
| `successCount` | integer | Successful syncs |
| `failureCount` | integer | Failed syncs |
| `errors` | array | List of errors |

---

## Error Responses

All error responses follow this format:

```json
{
  type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4',
  title: 'Not Found',
  status: 404,
  detail: 'Business Partner not found',
  traceId: '00-abc123...'
}
```

### HTTP Status Codes

| Code | Description |
|------|-------------|
| `200` | Success |
| `400` | Bad Request - Invalid parameters |
| `404` | Not Found - Resource doesn't exist |
| `500` | Internal Server Error |
| `503` | Service Unavailable - Dependency failure |

---

## Authentication

The current API does not require authentication in development mode. For production:

1. Configure ASP.NET Core Authentication
2. Add `[Authorize]` attribute to controllers
3. Use JWT tokens or API keys

---

## Rate Limiting

No rate limiting is currently implemented. For production:

- Consider adding rate limiting middleware
- Implement request quotas per client
- Add throttling for sync operations

---

## Swagger/OpenAPI

Interactive API documentation is available at:

```
http://localhost:5000/swagger
```

Features:
- Try It Out - Test endpoints directly from the browser
- Model documentation - View all data models
- Request/Response examples - See example payloads

---

## SDK Examples

### cURL

```bash
# Get all business partners
curl -X GET http://localhost:5000/api/businesspartners

# Trigger sync
curl -X POST http://localhost:5000/api/sync/business-partners

# Check health
curl -X GET http://localhost:5000/health
```

### .NET HttpClient

```csharp
var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

// Get all partners
var partners = await client.GetFromJsonAsync<List<BusinessPartner>>(
    '/api/businesspartners');

// Trigger sync
var result = await client.PostAsync('/api/sync/business-partners', null);
var syncResult = await result.Content.ReadFromJsonAsync<SyncResultDto>();

// Check health
var health = await client.GetFromJsonAsync<HealthReport>('/health');
```

### JavaScript/TypeScript

```typescript
const baseUrl = 'http://localhost:5000';

// Get all partners
const partners = await fetch(`${baseUrl}/api/businesspartners`)
    .then(res => res.json());

// Trigger sync
const syncResult = await fetch(`${baseUrl}/api/sync/business-partners`, {
    method: 'POST'
}).then(res => res.json());

// Check health
const health = await fetch(`${baseUrl}/health`)
    .then(res => res.json());
```