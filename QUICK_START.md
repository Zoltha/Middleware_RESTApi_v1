# Quick Start Guide

## Prerequisites
- Visual Studio 2019
- .NET Framework 4.8
- SQL Server LocalDB

## Setup (5 minutes)

### 1. Configure CRM Credentials
Edit `MiddlewareApp/appsettings.json`:

```json
{
  "DynamicsCrm": {
    "Resource": "https://yourorg.crm.dynamics.com",
    "Authority": "https://login.microsoftonline.com/your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

### 2. Build & Run
```bash
# Open solution in Visual Studio
# Or use command line:
dotnet build MiddlewareApp.sln
```

### 3. Access API
- Swagger UI: `https://localhost:44300/swagger`
- Health Check: `https://localhost:44300/api/health`

## Testing the API

### Using Swagger
1. Navigate to `https://localhost:44300/swagger`
2. Expand an endpoint
3. Click "Try it out"
4. Enter credentials in Authorization header: `Basic dXNlcm5hbWU6cGFzc3dvcmQ=`
5. Execute request

### Using PowerShell
```powershell
# Encode credentials
$credentials = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("username:password"))

# Test health endpoint (no auth required)
Invoke-RestMethod -Uri "https://localhost:44300/api/health"

# Get all accounts
Invoke-RestMethod -Uri "https://localhost:44300/api/accounts" `
  -Headers @{ Authorization = "Basic $credentials" }

# Create account with idempotency
$body = @{
  Name = "Test Company"
  Email = "test@company.com"
  Phone = "555-1234"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:44300/api/accounts" `
  -Method POST `
  -Headers @{ 
    Authorization = "Basic $credentials"
    "Idempotency-Key" = "unique-key-123"
    "Content-Type" = "application/json"
  } `
  -Body $body
```

### Using cURL
```bash
# Test health endpoint
curl https://localhost:44300/api/health

# Get all accounts
curl https://localhost:44300/api/accounts \
  -H "Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ="

# Create account
curl https://localhost:44300/api/accounts \
  -X POST \
  -H "Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=" \
  -H "Idempotency-Key: unique-key-123" \
  -H "Content-Type: application/json" \
  -d '{"Name":"Test Company","Email":"test@company.com"}'

# Get specific account
curl https://localhost:44300/api/accounts/{guid} \
  -H "Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ="

# Update account
curl https://localhost:44300/api/accounts/{guid} \
  -X PUT \
  -H "Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=" \
  -H "Content-Type: application/json" \
  -d '{"Name":"Updated Company Name"}'

# Delete account
curl https://localhost:44300/api/accounts/{guid} \
  -X DELETE \
  -H "Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ="
```

## Run Tests
```bash
# In Visual Studio: Test Explorer (Ctrl+E, T)
# Or command line:
dotnet test MiddlewareApp.Tests/MiddlewareApp.Tests.csproj
```

## Monitor Background Jobs
Access Hangfire Dashboard (requires authentication):
`https://localhost:44300/hangfire`

## View Logs

### File Logs
```
MiddlewareApp/Logs/Middleware-{Date}.log
```

### SQL Logs
```sql
USE MiddlewareLogs
SELECT * FROM Logs ORDER BY Timestamp DESC
```

## API Endpoints Quick Reference

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/health` | No | System health check |
| GET | `/swagger` | No | API documentation |
| GET | `/api/accounts` | Yes | List all accounts |
| GET | `/api/accounts/{id}` | Yes | Get account by ID |
| POST | `/api/accounts` | Yes | Create new account |
| PUT | `/api/accounts/{id}` | Yes | Update account |
| PATCH | `/api/accounts/{id}` | Yes | Partial update |
| DELETE | `/api/accounts/{id}` | Yes | Delete account |
| GET | `/api/accounts/batch?ids=...` | Yes | Batch retrieve |

## Troubleshooting

### Database Issues
```sql
-- Check if database exists
SELECT name FROM sys.databases WHERE name = 'MiddlewareLogs'

-- Recreate database
DROP DATABASE IF EXISTS MiddlewareLogs
-- Restart application (auto-creates)
```

### CRM Connection Issues
1. Verify credentials in appsettings.json
2. Check Azure AD app registration
3. Ensure CRM URL is correct
4. Check firewall/network settings
5. Review logs in `Logs/` directory

### Authentication Issues
- Ensure Authorization header format: `Basic {base64(username:password)}`
- Minimum username length: 3 characters
- Minimum password length: 6 characters
- Use online tool to generate Base64: `btoa("username:password")`

### Port Already in Use
Change port in `MiddlewareApp.csproj`:
```xml
<IISExpressSSLPort>44301</IISExpressSSLPort>
```

## Performance Tips

1. **Token Caching**: OAuth tokens cached for ~1 hour
2. **Data Caching**: Account data cached for 30 minutes
3. **Batch Operations**: Use `/api/accounts/batch` for multiple accounts
4. **Parallel Requests**: Service uses Task.WhenAll automatically
5. **Retry Logic**: Automatic retry on transient failures

## Security Reminders

⚠️ **For Production**:
1. Replace Basic Auth with OAuth 2.0/JWT
2. Store secrets in Azure Key Vault
3. Enable HTTPS only
4. Implement rate limiting
5. Add CORS policies
6. Enable Application Insights
7. Review and sanitize logs

## Support

- Check `README.md` for detailed documentation
- Review `IMPLEMENTATION_SUMMARY.md` for technical details
- Open issue on GitHub repository
- Check logs in `Logs/` directory

---

**Happy coding! 🚀**
