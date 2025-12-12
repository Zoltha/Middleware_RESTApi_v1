# Middleware REST API v1

A comprehensive ASP.NET Web API middleware solution targeting .NET Framework 4.8 for Visual Studio 2019, providing a secure gateway to Microsoft Dynamics 365 CRM.

## 📋 Overview

This middleware application serves as a REST API gateway between client applications and Microsoft Dynamics 365 CRM, offering:

- **Secure Authentication**: OAuth 2.0 client credentials flow with token caching
- **CRUD Operations**: Full Create, Read, Update, Delete operations on CRM Account entities
- **Idempotency**: Support for idempotent POST operations via `Idempotency-Key` header
- **Resiliency**: Polly retry policies for transient fault handling
- **Performance**: In-memory caching and parallel request processing
- **Logging**: Multi-sink logging with Serilog (File, SQL, Azure Blob)
- **Background Jobs**: Hangfire-based recurring CRM synchronization
- **Health Monitoring**: Comprehensive health checks for DB, CRM, and Hangfire
- **API Documentation**: Swagger/OpenAPI interface at `/swagger`

## 🏗️ Architecture

```
MiddlewareApp/
├── App_Start/
│   ├── AuthHandler.cs          # HTTP message handler for Basic authentication
│   └── SwaggerConfig.cs        # Swagger/OpenAPI configuration
├── Controllers/
│   ├── AccountsController.cs   # CRUD endpoints for CRM accounts
│   └── HealthController.cs     # Health check endpoint
├── Services/
│   ├── DynamicsCrmService.cs   # CRM REST API client with OAuth 2.0
│   └── LoggingService.cs       # Centralized Serilog logging service
├── Models/
│   ├── AccountViewModel.cs     # Account entity view model
│   └── IdempotencyRecord.cs    # Idempotency tracking entity
├── Data/
│   └── SqlDbContext.cs         # Entity Framework DbContext
├── Background/
│   └── HangfireTasks.cs        # Recurring background jobs
├── Logs/
│   └── .gitkeep                # Log files directory
└── Tests/
    ├── AuthHandlerTests.cs
    ├── DynamicsCrmServiceTests.cs
    └── AccountsControllerTests.cs
```

## 🚀 Getting Started

### Prerequisites

- Visual Studio 2019
- .NET Framework 4.8
- SQL Server LocalDB
- Microsoft Dynamics 365 CRM instance
- Azure AD application registration (for OAuth)

### Configuration

1. **Update `appsettings.json`** with your CRM credentials:

```json
{
  "DynamicsCrm": {
    "Resource": "https://yourorg.crm.dynamics.com",
    "Authority": "https://login.microsoftonline.com/your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "ApiVersion": "9.2"
  }
}
```

2. **Database Setup**:

The application uses SQL Server LocalDB for logging and idempotency tracking. The database will be created automatically on first run.

Connection String: `Server=(localdb)\MSSQLLocalDB;Database=MiddlewareLogs;Trusted_Connection=True;`

### Build and Run

1. Open `MiddlewareApp.sln` in Visual Studio 2019
2. Restore NuGet packages
3. Build the solution (Ctrl+Shift+B)
4. Run the application (F5)
5. Navigate to `https://localhost:44300/swagger` to view API documentation

## 📚 API Endpoints

### Accounts

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts` | Get all accounts |
| GET | `/api/accounts/{id}` | Get account by ID |
| POST | `/api/accounts` | Create new account (supports idempotency) |
| PUT | `/api/accounts/{id}` | Update account |
| PATCH | `/api/accounts/{id}` | Partially update account |
| DELETE | `/api/accounts/{id}` | Delete account |
| GET | `/api/accounts/batch?ids={guid1},{guid2}` | Batch retrieve accounts |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | System health check |

### Documentation

| Endpoint | Description |
|----------|-------------|
| `/swagger` | Swagger UI documentation |

## 🔒 Authentication

The API uses Basic Authentication. Include the `Authorization` header with your requests:

```
Authorization: Basic base64(username:password)
```

**Note**: In production, replace Basic Auth with proper OAuth 2.0/JWT token validation.

Public endpoints (no authentication required):
- `/api/health`
- `/swagger`

## 🔄 Idempotency

POST requests support idempotency to prevent duplicate operations. Include the `Idempotency-Key` header:

```
POST /api/accounts
Idempotency-Key: unique-key-123
```

Idempotency records are stored for 24 hours.

## 📊 Logging

The application uses Serilog with multiple sinks:

1. **File Logging**: `Logs/Middleware-{Date}.log`
2. **SQL Server**: `Logs` table in LocalDB
3. **Azure Blob Storage**: Optional, configurable

Configure logging in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": "Information",
    "EnableFileLogging": true,
    "EnableSqlLogging": true,
    "EnableBlobLogging": false
  }
}
```

## ⚡ Background Jobs

Hangfire manages recurring background jobs:

### CRM Sync Job
- **Schedule**: Hourly
- **Purpose**: Synchronize CRM accounts to local database
- **Job ID**: `sync-crm-accounts`

### Idempotency Cleanup Job
- **Schedule**: Daily
- **Purpose**: Remove expired idempotency records
- **Job ID**: `cleanup-idempotency-records`

Access Hangfire Dashboard: `/hangfire` (requires authentication)

## 🏥 Health Checks

The health endpoint provides comprehensive system status:

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "status": "Healthy",
  "database": {
    "component": "Database",
    "status": "Healthy",
    "message": "Database connection successful",
    "responseTimeMs": 45
  },
  "crm": {
    "component": "CRM",
    "status": "Healthy",
    "message": "CRM connection successful. Retrieved 150 accounts.",
    "responseTimeMs": 234
  },
  "hangfire": {
    "component": "Hangfire",
    "status": "Healthy",
    "message": "1 server(s) running, 2 recurring job(s) configured"
  }
}
```

Status values: `Healthy`, `Degraded`, `Unhealthy`

## 🧪 Testing

The solution includes comprehensive unit tests using:
- **MSTest**: Test framework
- **FakeItEasy**: Mocking library
- **FluentAssertions**: Assertion library

Run tests in Visual Studio:
- Test Explorer: View → Test Explorer
- Run All Tests: Ctrl+R, A

Test Coverage:
- `AuthHandlerTests.cs`: Authentication handler tests
- `DynamicsCrmServiceTests.cs`: CRM service and model validation tests
- `AccountsControllerTests.cs`: Controller and API endpoint tests

## 📦 Dependencies

### Core Packages
- Microsoft.AspNet.WebApi (5.2.9)
- Microsoft.EntityFramework (6.4.4)
- Newtonsoft.Json (13.0.3)

### Resiliency & Background Jobs
- Polly (7.2.4)
- Hangfire.Core (1.8.6)
- Hangfire.SqlServer (1.8.6)

### Logging
- Serilog (3.1.1)
- Serilog.Sinks.File (5.0.0)
- Serilog.Sinks.MSSqlServer (6.3.0)
- Serilog.Sinks.AzureBlobStorage (3.1.0)

### Documentation
- Swashbuckle.Core (5.6.0)

### Testing
- MSTest.TestFramework (3.1.1)
- FakeItEasy (8.0.0)
- FluentAssertions (6.12.0)

## 🔧 Configuration Files

### appsettings.json
Application settings including CRM credentials, connection strings, and logging configuration.

### Web.config
ASP.NET Web API configuration including routing, handlers, and Entity Framework settings.

### Global.asax.cs
Application startup configuration including Web API routes, Swagger, and Hangfire initialization.

## 🛡️ Security Considerations

**For Production Deployment**:

1. **Replace Basic Authentication**: Implement OAuth 2.0/JWT token validation
2. **Secure Credentials**: Use Azure Key Vault or similar for secrets
3. **HTTPS Only**: Enforce SSL/TLS for all communications
4. **Rate Limiting**: Implement API rate limiting
5. **Input Validation**: Add comprehensive input validation and sanitization
6. **SQL Injection**: Ensure parameterized queries (already using Entity Framework)
7. **CORS**: Configure appropriate CORS policies
8. **Logging**: Avoid logging sensitive information

## 📝 Best Practices

1. **Async/Await**: All operations use async/await for non-blocking I/O
2. **Retry Policies**: Polly handles transient faults with exponential backoff
3. **Caching**: MemoryCache reduces CRM API calls
4. **Parallel Processing**: Task.WhenAll for batch operations
5. **Structured Logging**: Serilog with contextual information
6. **Health Checks**: Monitor all dependencies
7. **Error Handling**: Comprehensive try-catch with logging
8. **Code Comments**: XML documentation comments throughout

## 🚧 Future Enhancements

- [ ] Implement OAuth 2.0/JWT authentication
- [ ] Add rate limiting middleware
- [ ] Implement distributed caching (Redis)
- [ ] Add Application Insights telemetry
- [ ] Implement API versioning
- [ ] Add more comprehensive integration tests
- [ ] Implement webhooks for CRM events
- [ ] Add GraphQL endpoint
- [ ] Containerize with Docker
- [ ] CI/CD pipeline setup

## 📄 License

MIT License

## 👥 Support

For issues and questions, please open an issue in the repository.

---

**Built with ❤️ using ASP.NET Web API and .NET Framework 4.8**