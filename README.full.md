# Travel Now Platform (TNP) API

> Backend API cho ứng dụng quản lý tài chính cá nhân & du lịch nhóm

Backend API xây dựng bằng **C# / .NET 10** (ASP.NET Core Web API) với **SQL Server + Redis**, theo **kiến trúc Clean Architecture** (Domain – Application – Infrastructure – WebApi) kết hợp **CQRS/MediatR**. Hệ thống bao phủ: xác thực & tài khoản, quản lý người dùng, tài chính cá nhân, chuyến đi nhóm và chat realtime — kèm background jobs, logging, health checks và đóng gói Docker.

> **Chú thích trạng thái:** ✅ Đã triển khai API | 🔶 Entity & migration đã có — API đang phát triển

---

## Tính Năng Nghiệp Vụ

### Đăng nhập & Tài khoản ✅
- Đăng ký tài khoản với **xác thực email** (token có thời hạn 30 phút).
- Đăng nhập bằng email + mật khẩu, trả **JWT Access Token** + **Refresh Token** (lưu cookie).
- Tự động **refresh token** khi access token hết hạn; **logout** thu hồi refresh token.
- Quên mật khẩu: gửi **OTP** qua email → đặt lại mật khẩu mới.
- Phân quyền theo vai trò (**RBAC**): roles `Admin` / `User`, policies `RequireAdmin`, `RequireUser`, `RequireAdminOrUser`.
- Chính sách mật khẩu + **lockout** sau 5 lần sai (10 phút), bắt buộc xác nhận email.

### Quản lý người dùng ✅
- Admin xem danh sách / chi tiết người dùng, cập nhật thông tin cá nhân.
- Gán / gỡ **role** cho người dùng.
- **Khóa tài khoản** (soft delete) thay vì xóa vật lý.

### Tài chính cá nhân 🔶
- **Wallet**: nhiều ví (Tiền mặt, Ngân hàng, Thẻ tín dụng…), multi-currency; `balance` là giá trị **cache** — nguồn thật luôn tính từ `Transaction`.
- **Transaction**: mọi khoản thu/chi/chuyển tiền (`income` / `expense` / `transfer`); chuyển tiền xử lý atomic qua `transfer_group_id`.
- **Category**: danh mục hệ thống (`user_id = NULL`) + danh mục cá nhân, hỗ trợ phân cấp.
- **Budget**: giới hạn chi theo danh mục/kỳ; `spent` tính từ `Transaction`, cảnh báo khi ≥ 80% hạn mức.
- **RecurringTransaction**: thu/chi định kỳ, tự động sinh giao dịch qua background job.
- **SavingGoal**, **Debt**, **ExchangeRate** (multi-currency), **Tag / TransactionTag**, **Notification**.

### Chuyến đi nhóm (Travel) 🔶
- **Trip** là container chính; khi tạo Trip → tự động tạo **TripChatRoom**.
- **TripMember** với vai trò (Organizer / Member / Viewer) + trạng thái lời mời.
- **TripInvitation**: mời qua link/email bằng token có thời hạn.
- **TripActivity**: lịch trình theo ngày, có thứ tự sắp xếp.
- **TripExpense + TripExpenseSplit**: ghi chi tiêu nhóm, chia đều / chia tùy chỉnh.
- **TripDebt**: ma trận "ai nợ ai"; **TripSettlement**: thanh toán dứt nợ sau chuyến đi.

### Chat realtime 🔶
- Mỗi Trip có đúng 1 **TripChatRoom**; tin nhắn gồm User Message và System Message.
- **SignalR + Redis Backplane** đẩy tin nhắn realtime tới mọi thành viên.
- Hệ thống tự tạo System Message khi có sự kiện quan trọng (thêm chi phí, thêm hoạt động…).

### Hạ tầng & Vận hành 🔶
- Background jobs qua **Hangfire** (storage SQL Server + Redis).
- **Serilog** logging; **Health Checks** (`/health`: SQL Server + Redis).
- **Docker multi-stage** + **docker-compose** (API + SQL Server + Redis), tự migrate khi khởi động.

---

## Kiến Trúc & Kỹ Thuật

### Kiến trúc phân lớp (Clean Architecture)

```
┌──────────────────────────────────────────────────┐
│                     WebApi                       │  ASP.NET Core 10 — Controllers, Middlewares, Swagger
├──────────────────────────────────────────────────┤
│                    Application                   │  CQRS (MediatR) — Commands/Queries, DTOs, Validators
├──────────────────────────────────────────────────┤
│                  Infrastructure                  │  EF Core + Dapper, Identity, Redis, Hangfire, Email
├──────────────────────────────────────────────────┤
│                     Domain                        │  Entities, Enums, Value Objects, Exceptions
└──────────────────────────────────────────────────┘
                         │
               SQL Server ─── Redis
```

- **Domain** – Các entity thuần .NET, không phụ thuộc framework: `Users`, `Wallet`, `Transaction`, `Trip`, `TripExpense`, `TripMessage`… cùng enums, value objects, exceptions.
- **Application** – Tầng nghiệp vụ theo **CQRS**: mỗi Command/Query là `sealed record`, Handler mỏng; DTO, FluentValidation validator và các interface trừu tượng.
- **Infrastructure** – Hiện thực hóa: EF Core + migrations, Dapper, ASP.NET Core Identity, Redis (cache + SignalR backplane), Hangfire, dịch vụ email/token.
- **WebApi** – Controllers (chỉ inject `IMediator`), middleware (Exception, CSRF, Secure Headers), Swagger, Health Checks.

### Cấu trúc dự án

```
AuthApi.slnx
├── AuthApi.Domain/            # Entities, Enums, Value Objects, Exceptions, Factories
├── AuthApi.Application/       # CQRS (MediatR) — Features/{Module}/Commands|Queries, DTOs, Validators
├── AuthApi.Infrastructure/    # EF Core, Dapper, Identity, Redis, Hangfire, Services, Migrations
├── AuthApi.WebApi/            # Controllers, Middlewares, HealthChecks, Dockerfile
├── AuthApi.Tests/             # Unit tests
├── AuthApi.IntegrationTests/  # Integration tests (Testcontainers)
├── docs/                      # Tài liệu kỹ thuật (markdown)
└── docker-compose.yml         # API + SQL Server + Redis
```

**Dependency flow:** `Domain ← Application ← Infrastructure ← WebApi`

### Kỹ thuật chính

| Kỹ thuật | Mục đích sử dụng |
|---|---|
| .NET 10 / ASP.NET Core 10 | Nền tảng Web API |
| Entity Framework Core 10.0.5 | ORM, migrations, Identity store |
| Dapper 2.1.72 | Truy vấn read-model nhanh |
| ASP.NET Core Identity + JWT Bearer 10.0.6 | Xác thực, phân quyền RBAC |
| MediatR 12.1.1 + FluentValidation 12.1.1 | CQRS pipeline + validate đầu vào |
| StackExchange.Redis 2.12.14 | Cache + SignalR backplane |
| Hangfire 1.8.23 | Background jobs |
| SignalR 10.0.9 | Chat realtime |
| Serilog 10.0.0 | Logging có cấu trúc |
| Swashbuckle 10.1.7 | Swagger / OpenAPI |
| AspNetCore.HealthChecks.UI | Giám sát SQL Server + Redis |
| Docker / docker-compose | Đóng gói & triển khai |

### Bảo mật & Toàn vẹn dữ liệu
- **JWT Bearer** xác thực; **Refresh Token** lưu cookie, thu hồi khi logout.
- Middleware **CSRF**, **Secure Headers** và **Exception** tập trung.
- Truy vấn qua EF Core / Dapper tham số hóa → chống SQL injection.
- Chính sách mật khẩu, lockout 5 lần sai, bắt buộc xác nhận email.
- Soft delete (`deleted_at`) + audit fields (`created_at`, `created_by_id`, `updated_at`, `updated_by_id`) theo chuẩn thiết kế.

---

## Cài Đặt & Chạy

### Yêu cầu môi trường
- **.NET 10 SDK**
- **Docker** (SQL Server + Redis sẵn sàng) — hoặc cài **SQL Server** + **Redis** local
- **SMTP server** cho gửi email (OTP, xác nhận) — tuỳ chọn khi dev

### Cách 1 — Docker Compose (khuyến nghị)
1. Tạo cấu hình từ template và điền bí mật:
   ```
   copy .env.template .env
   ```
   Sửa `SA_PASSWORD` và `JWT_KEY` (tối thiểu 32 ký tự) trong `.env`.
2. Khởi động toàn bộ stack:
   ```
   docker compose up --build
   ```
3. API tự migrate database khi container khởi động.
   - API: `http://localhost:5000`
   - Swagger: `http://localhost:5000/swagger`
   - Hangfire Dashboard: `http://localhost:5000/hangfire`
   - Health check: `http://localhost:5000/health`

### Cách 2 — Chạy thủ công (dev)
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
dotnet user-secrets set "Frontend:Url" "http://localhost:3000"
dotnet user-secrets set "Email:Smtp" "smtp.gmail.com"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:From" "noreply@travelnow.com"
dotnet user-secrets set "Email:Password" "<smtp-password>"

# Apply migrations
dotnet ef database update --project AuthApi.Infrastructure --startup-project AuthApi.WebApi

# Run
dotnet run --project AuthApi.WebApi
```

### Tests
```bash
dotnet test
```
- `AuthApi.Tests` — unit tests (handlers, validators).
- `AuthApi.IntegrationTests` — integration tests (repositories, flow đăng nhập…).

---

## Tài Liệu

| File | Mục Đích |
|------|----------|
| [docs/01_BACKEND_ARCHITECTURE.md](docs/01_BACKEND_ARCHITECTURE.md) | Kiến trúc backend, request flow, auth, DI, conventions |
| [docs/02_DATABASE_SCHEMA.md](docs/02_DATABASE_SCHEMA.md) | Database schema, entities, indexes |
| [docs/03_RULES.md](docs/03_RULES.md) | Quy tắc dự án, 13-step feature pattern |
| [docs/04_AI_CONTEXT.md](docs/04_AI_CONTEXT.md) | Tóm tắt ngữ cảnh cho AI |
| [docs/BUSINESS_DOMAIN.md](docs/BUSINESS_DOMAIN.md) | Yêu cầu nghiệp vụ (Financial + Travel + Chat) |
| [docs/detail_docs/HOW_TO_ADD_FEATURE.md](docs/detail_docs/HOW_TO_ADD_FEATURE.md) | Quy trình thêm feature mới |

## Quy Tắc Phát Triển (Key)

- **Controller** chỉ inject `IMediator`, không gọi DbContext/Repository/Service trực tiếp.
- **Command/Query** là `sealed record`, Handler mỏng.
- **Service** trả `Result<T>`, không throw cho lỗi nghiệp vụ.
- **FluentValidation** auto-register qua assembly scanning, không dùng Data Annotations.
- **Soft Delete** (`deleted_at`), không DELETE vật lý.
- `Wallet.balance`, `Budget.spent`, `Trip.total_spent` là **cache** — source of truth là `Transaction` / `TripExpense`.
- **Background job** qua Hangfire, email gửi bất đồng bộ.
- **Audit fields chuẩn:** `created_at`, `created_by_id`, `updated_at`, `updated_by_id`, `deleted_at`.

Xem chi tiết tại [docs/03_RULES.md](docs/03_RULES.md) và [docs/04_AI_CONTEXT.md](docs/04_AI_CONTEXT.md).
