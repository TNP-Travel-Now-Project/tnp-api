# Travel Now Platform (TNP) API

> Backend API cho ứng dụng quản lý tài chính cá nhân & du lịch nhóm

## Mô Tả

`AuthApi.slnx` — .NET 10 ASP.NET Core Web API cung cấp:
- Xác thực người dùng (JWT + Refresh Token + CSRF)
- Quản lý tài chính cá nhân (ví, giao dịch, ngân sách, mục tiêu tiết kiệm)
- Quản lý chuyến đi nhóm (lịch trình, chia chi phí, ma trận nợ)
- Chat real-time trong Trip (SignalR)
- Background jobs (Hangfire) — email, recurring transactions, cleanup

## Yêu Cầu

- .NET 10 SDK
- SQL Server
- Redis
- SMTP server (cho email)

## Cấu Trúc Dự Án

```
AuthApi.slnx
├── AuthApi.Domain/          # Pure .NET — entities, value objects, enums, interfaces
├── AuthApi.Application/     # CQRS (MediatR) — commands, queries, DTOs, validators
├── AuthApi.Infrastructure/  # EF Core, Identity, Redis, Hangfire, repositories, services
└── AuthApi.WebApi/          # Controllers, middleware, Program.cs, docs/
```

**Dependency flow:** `Domain ← Application ← Infrastructure ← WebApi`

## Chạy Project

```bash
# Restore packages
dotnet restore

# Set User Secrets (chỉ lần đầu)
cd AuthApi.WebApi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Database=...;..."
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"
dotnet user-secrets set "AppSettings:JwtKey" "<32+ char key>"
dotnet user-secrets set "AppSettings:JwtIssuer" "https://api.travelnow.com"
dotnet user-secrets set "AppSettings:JwtAudience" "https://travelnow.com"
dotnet user-secrets set "Email:Smtp" "smtp.gmail.com"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:From" "noreply@travelnow.com"
dotnet user-secrets set "Email:Password" "<smtp-password>"

# Apply migrations
dotnet ef database update --project AuthApi.Infrastructure --startup-project AuthApi.WebApi

# Run
dotnet run --project AuthApi.WebApi
```

- Swagger UI: `https://localhost:xxxx/swagger`
- Hangfire Dashboard: `https://localhost:xxxx/hangfire` (Admin only)

## Tài Liệu

Tất cả docs nằm trong `AuthApi.WebApi/docs/`:

| File | Mục Đích |
|------|----------|
| [01_BACKEND_ARCHITECTURE.md](AuthApi.WebApi/docs/01_BACKEND_ARCHITECTURE.md) | Kiến trúc, request flow, auth, DI, conventions |
| [02_DATABASE_SCHEMA.md](AuthApi.WebApi/docs/02_DATABASE_SCHEMA.md) | 26 entities, 3 aggregates, source-of-truth, indexes |
| [03_RULES.md](AuthApi.WebApi/docs/03_RULES.md) | các quy tắc (80+), 13-step feature pattern |
| [04_AI_CONTEXT.md](AuthApi.WebApi/docs/04_AI_CONTEXT.md) | Tóm tắt cho AI |
| [BUSINESS_DOMAIN.md](AuthApi.WebApi/docs/BUSINESS_DOMAIN.md) | Yêu cầu nghiệp vụ (Financial + Travel + Chat) |
| [TECHNICAL_DOCUMENTATION.md](AuthApi.WebApi/docs/TECHNICAL_DOCUMENTATION.md) | Full 16-section technical doc |

## Quy Tắc Phát Triển (Key)

- **Controller** chỉ inject `IMediator`, không gọi DbContext/Repository/Service
- **Command/Query** là `sealed record`, Handler mỏng 1 dòng
- **Service** trả `Result<T>`, KHÔNG throw cho lỗi nghiệp vụ
- **FluentValidation** auto-register qua assembly scanning, không Data Annotations
- **Soft Delete** (`deleted_at IS NULL`), không DELETE vật lý
- **`Wallet.balance`, `Budget.spent`** là CACHE — source of truth là `Transaction`
- **Background job** qua Hangfire, email async
- **Audit fields chuẩn:** `created_at`, `created_by_id`, `updated_at`, `updated_by_id`, `deleted_at`

Xem chi tiết tại [03_RULES.md](AuthApi.WebApi/docs/03_RULES.md) và [04_AI_CONTEXT.md](AuthApi.WebApi/docs/04_AI_CONTEXT.md).

## Thêm Feature Mới (13 bước)

```
1. Domain Entity          → Domain/Entities/{Entity}.cs
2. Repository Interface   → Domain/Interfaces/I{Entity}Repository.cs
3. Command/Query          → Application/Features/{Module}/Commands|Queries/{Action}/{Action}Command.cs
4. Handler                → Application/Features/{Module}/Commands|Queries/{Action}/{Action}CommandHandler.cs
5. Validator              → Application/Features/{Module}/Commands/{Action}/{Action}CommandValidator.cs
6. DTO                    → Application/Features/{Module}/DTOs/{Dto}.cs
7. Service Interface      → Application/Abstractions/Interfaces/{Module}/I{Name}Service.cs
8. Service Implementation → Infrastructure/Services/{Module}/{Name}Service.cs
9. Repository Impl        → Infrastructure/Persistence/Repositories/{Entity}/{Name}Repository.cs
10. Controller            → WebApi/Controllers/{Name}Controller.cs
11. DI Registration       → Infrastructure/Configuration/DependencyInjection.cs
12. EF Config             → Infrastructure/Persistence/Entities/BuildEntities.cs
13. Migration             → dotnet ef migrations add {Name}
```

## Tech Stack

- .NET 10, ASP.NET Core 10
- Entity Framework Core 10.0.5 + Dapper + SqlKata
- ASP.NET Core Identity + JWT Bearer 10.0.6
- MediatR 12.1.1 + FluentValidation 12.1.1
- Hangfire 1.8.23 + StackExchange.Redis 2.12.14
- Swashbuckle 10.1.7 (Swagger)
- SignalR 10.0.6 (chat real-time)
