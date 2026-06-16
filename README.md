# Favilonia — Multi-Tenant SaaS for Educational Institutions

REST API backend for a multi-tenant SaaS platform serving schools and educational organizations. Each tenant (organization) has full data isolation; a single backend instance serves multiple organizations simultaneously.

Built with **ASP.NET Core 8**, **PostgreSQL**, **Entity Framework Core**, **Docker**.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 Web API |
| Database | PostgreSQL 16 + Entity Framework Core 8 |
| Auth | JWT Bearer + Rotated Refresh Tokens |
| Containerization | Docker + Docker Compose |
| ORM | EF Core with Code-First migrations |
| Validation | FluentValidation |

---

## Architecture Highlights

### Multi-Tenancy
Every tenant-scoped endpoint is routed under `/api/organizations/{organizationId}/...` and protected by a custom `SameOrganization` authorization policy that compares the route parameter against the `organizationId` claim embedded in the JWT. Controllers additionally filter every query by `OrganizationId` as defense-in-depth.

```
Route:  /api/organizations/{organizationId:guid}/[controller]
Policy: SameOrganization  →  OrganizationAuthorizationHandler
Query:  .Where(x => x.OrganizationId == organizationId)
```

### Auth Flow
- Login requires `organizationId + email + password` — composite key isolates tenants
- Access token: ~60 min lifetime, embeds `organizationId` and `role` claims
- Refresh token: 7-day lifetime, persisted in DB, **rotated on every use**
- Password reset: time-limited one-time token, delivered via `IEmailService` (console stub included; swap for real SMTP)

### Role-Based Access
| Role | Access |
|---|---|
| `SuperAdmin` | All organizations |
| `Admin` | Own organization — full CRUD |
| `User` (student) | Own organization — own data only (grades, attendance) |

Student isolation pattern — forcibly overrides `studentId` from JWT on shared endpoints so students cannot read each other's data:
```csharp
if (User.IsInRole(Roles.User))
    studentId = User.GetUserId();
```

### Data Layer
- `BaseEntity` provides `Id / CreatedAt / UpdatedAt`; timestamps set automatically in `SaveChanges`
- Soft-delete via global query filters (`IsDeleted`) on Organization, News, Schedule, Page, Subject, Group, Period
- Two FKs to the same table (e.g. `Grade.StudentId` + `Grade.TeacherId`) → `DeleteBehavior.Restrict` on both to avoid EF cascade conflict
- Error handling centralized in `ApiExceptionHandlerMiddleware` + custom validation response factory

---

## Project Structure

```
Backend/
├── Favilonia.API/
│   ├── Authorization/        # SameOrganization, AdminOnly, SuperAdmin policies
│   ├── Controllers/          # REST endpoints
│   ├── Dtos/                 # Request / response models
│   ├── Extensions/           # IServiceCollection, ClaimsPrincipal helpers
│   ├── Middleware/           # Global exception handler
│   ├── Services/             # JwtTokenGenerator, RefreshTokenService, EmailService
│   └── Validation/           # FluentValidation validators
├── Favilonia.Domain/
│   └── Entities/             # Domain models (Organization, User, Grade, Attendance …)
└── Favilonia.Infrastructure/
    ├── Data/
    │   ├── AppDbContext.cs
    │   ├── Migrations/
    │   └── Seed/             # Demo org + users seeded in Development
    └── Favilonia.Infrastructure.csproj
```

---

## API Overview

### Public (anonymous)
```
GET  /api/public/{domain}              — org info by domain
GET  /api/public/{domain}/news         — published news
GET  /api/public/{domain}/schedule     — upcoming events
GET  /api/public/{domain}/pages/{slug} — page by slug
```

### Auth
```
POST /api/auth/login            — returns access + refresh token
POST /api/auth/refresh          — rotated refresh token
POST /api/auth/logout           — revoke refresh token
POST /api/auth/forgot-password  — generate reset token (logged to console)
POST /api/auth/reset-password   — validate token, change password
```

### SaaS Onboarding
```
POST /api/organizations/register — create org + first admin, returns tokens
```

### Tenant-Scoped (require SameOrganization policy)
```
/api/organizations/{orgId}/users
/api/organizations/{orgId}/news
/api/organizations/{orgId}/schedule
/api/organizations/{orgId}/groups
/api/organizations/{orgId}/subjects
/api/organizations/{orgId}/periods
/api/organizations/{orgId}/grades          + GET /summary
/api/organizations/{orgId}/attendance      + POST /bulk
/api/organizations/{orgId}/final-grades
/api/organizations/{orgId}/pages
/api/organizations/{orgId}/feedback
```

Full Swagger UI available at `/swagger` when running in Development.

---

## Running Locally

### Docker (recommended — includes PostgreSQL)

```bash
cp .env.example .env
# Edit .env: set POSTGRES_PASSWORD and JWT_KEY
docker compose up --build
```

API: http://localhost:5011  
Swagger: http://localhost:5011/swagger

### Without Docker

Requires PostgreSQL on `localhost:5432`. Set connection string and JWT key:

```bash
# appsettings.json or environment variables:
# ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=favilonia_db;Username=postgres;Password=yourpassword"
# Jwt__Key = "your-secret-key-at-least-16-chars"

dotnet run --project Backend/Favilonia.API
```

The database is created and seeded automatically on first run (Development environment).

### Add EF Migration

```bash
dotnet ef migrations add <Name> \
  --project Backend/Favilonia.Infrastructure \
  --startup-project Backend/Favilonia.API
```

---

## Demo Credentials

Seeded automatically in Development:

| Role | Email | Password |
|---|---|---|
| Admin | admin@demo-school.local | Admin@123456 |
| Student | ivanov@demo-school.local | User@123456 |
| Student | petrova@demo-school.local | User@123456 |

Organization domain: `demo-school`  
Organization ID: `12345678-1234-1234-1234-123456789012`

---

## Configuration

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | JWT signing key (min 16 chars) |
| `Jwt__ExpirationMinutes` | Access token lifetime (default: 60) |
| `Jwt__RefreshTokenExpirationDays` | Refresh token lifetime (default: 7) |

Set via environment variables, `appsettings.json`, or `.env` (Docker).

---

## License

MIT
