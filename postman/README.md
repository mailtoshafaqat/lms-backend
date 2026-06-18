# Postman / API testing export

OpenAPI 3 spec and Postman environment for the White-Label LMS API.

## Files

| File | Purpose |
|------|---------|
| `openapi.json` | Full API spec (all endpoints, schemas, JWT security) |
| `White-Label-LMS.local.postman_environment.json` | Local variables (`baseUrl`, `tenantSlug`, demo logins) |
| `export-openapi.ps1` | Re-download spec from a running API |

## Prerequisites

1. SQL Server: `docker compose up -d` (from `backend/`)
2. API running: `dotnet run --project src/Lms.Api`
3. Swagger UI: http://localhost:5237/swagger

## Import into Postman

### Option A — OpenAPI (recommended)

1. Postman → **Import** → **File** → select `openapi.json`
2. Postman creates a collection with all routes grouped by controller tag
3. **Import** → `White-Label-LMS.local.postman_environment.json`
4. Select environment **White-Label LMS — Local** in the top-right dropdown

### Option B — Live URL

1. Start the API
2. Postman → **Import** → **Link** → `http://localhost:5237/swagger/v1/swagger.json`

## Auth in Postman

### Institute users (admin, teacher, student)

1. `POST {{baseUrl}}/api/v1/auth/login`
2. Headers: `X-Tenant-Slug: {{tenantSlug}}`
3. Body (JSON):

```json
{
  "email": "{{adminEmail}}",
  "password": "{{adminPassword}}"
}
```

4. Copy `accessToken` from the response into the environment variable `accessToken`
5. Other requests: **Authorization** → Bearer Token → `{{accessToken}}`  
   (OpenAPI import should already attach Bearer auth; set the token once.)

### SuperAdmin / Support

- Login **without** `X-Tenant-Slug` (or use platform login flow)
- Use `superAdminEmail` / `superAdminPassword` from the environment

## Tenant header

Most institute-scoped calls need:

```
X-Tenant-Slug: demo
```

Set `tenantSlug` in the environment and add a collection-level header in Postman if needed.

## Refresh the export

After API changes, re-export:

```powershell
cd backend/postman
powershell -File export-openapi.ps1
```

Or with a custom URL:

```powershell
powershell -File export-openapi.ps1 -BaseUrl http://localhost:5237
```

## Quick smoke requests

| Request | Method | Path |
|---------|--------|------|
| Login (admin) | POST | `/api/v1/auth/login` |
| Bundles | GET | `/api/v1/bundles` |
| Admin storage | GET | `/api/v1/admin/storage` |
| Subject catalog | GET | `/api/v1/admin/subject-definitions` |
| Swagger | GET | `/swagger/v1/swagger.json` |
