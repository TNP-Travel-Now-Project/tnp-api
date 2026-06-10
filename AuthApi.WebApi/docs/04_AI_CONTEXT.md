# TNP API — AI Context Summary

Đọc file này trong 30 giây để hiểu toàn bộ dự án và có thể phát triển tiếp.

---

## Project Identity

- **Name:** Travel Now Platform (TNP) API — `AuthApi.slnx`
- **Target:** .NET 10.0, SQL Server + Redis
- **Auth:** JWT Bearer (HMAC-SHA256, 15 phút) + Refresh Token (30 ngày, HttpOnly cookie) + CSRF Token (non-HttpOnly cookie)
- **Solution:** 4 projects — Domain (pure) ← Application (MediatR CQRS) ← Infrastructure (EF Core, Identity, Redis, Hangfire) ← WebApi (Controllers, Middleware)

---

## Architecture

- **Clean Architecture + CQRS + DDD hybrid**
- Domain là pure .NET (zero NuGet). Application chỉ tham chiếu Domain. Infrastructure tham chiếu Application + Domain. WebApi tham chiếu Application + Infrastructure.
- **CQRS:** Cả đọc và ghi đều dùng **Dapper** (SqlKata đã xóa). Đọc: Query → Handler → ReadRepository. Ghi: Command → Handler → WriteRepository.
- **WebApi mỏng:** Controller chỉ gọi `IMediator.Send()` → kiểm tra `result.IsSuccess` → `Ok()` hoặc `BadRequest(apiError)`.

## Request Flow

```
Request → ExceptionMiddleware → CORS → HttpsRedirection → SecureHeadersMiddleware → CookiePolicy
 → Authentication (JWT) → CSRFMiddleware → Authorization → Controller
 → MediatR.ValidationBehavior → Handler → Service → DB/Redis → Response
```

Thứ tự middleware KHÔNG được thay đổi. ExceptionMiddleware bắt `ValidationException` → 400, các exception khác → 500. Auth endpoints (login/register/refresh/logout) bypass CSRF.

## Authentication Model

- **Login:** email/password → UserManager → JWT (15m, body) + RefreshToken (30d, HttpOnly cookie) + CSRF-TOKEN cookie
- **Refresh:** Cookie `refreshToken` → validate DB → revoke cũ → issue cặp mới (token rotation bắt buộc)
- **CSRF:** Cookie `CSRF-TOKEN` vs header `X-CSRF-TOKEN` cho POST/PUT/PATCH/DELETE
- **Claims:** nameid (userId), email, unique_name (userName), role[]
- **Email confirmation bắt buộc** để đăng nhập. User không confirm trong 2h bị auto-delete (Hangfire job).

## Database Model

**SQL Server** qua `Microsoft.EntityFrameworkCore.SqlServer` (`Microsoft.Data.SqlClient`). Migration hiện tại chỉ có Identity tables + `RefreshToken`.

| Bảng | Vai trò |
|------|---------|
| `AspNetUsers` | User + Identity (FirstName, LastName, DOB) |
| `AspNetRoles` | User, Admin, Guest (seeded) |
| `RefreshToken` | Token + UserId + ExpiresAt + IsRevoked |

**Soft Delete:** Dùng `deleted_at TIMESTAMPTZ NULL`. Mọi query mặc định filter `WHERE deleted_at IS NULL`.

**Audit fields chuẩn:** `created_at`, `created_by_id`, `updated_at`, `updated_by_id`, `deleted_at` — cho mọi business table.

**Source of Truth vs Cache:**
- `Transaction` là source of truth duy nhất cho tài chính.
- `Wallet.balance`, `Budget.spent`, `SavingGoal.current_amount`, `Debt.current_balance`, `Trip.total_spent`, `TripMember.balance` — TẤT CẢ đều là cache. KHÔNG trust cache khi cần chính xác.

## Business Aggregates

### Aggregate 1: User (Financial Module)
```
User → Wallet, Category, Budget, RecurringTransaction, SavingGoal, Debt, Tag, Transaction, Notification
Transaction → TransactionSplit, TransactionTag, TransactionAttachment
```
- Mỗi user có nhiều Wallet. Wallet.Type: Cash, Bank, Credit, Ewallet, Savings.
- Category hỗ trợ phân cấp (ParentCategoryId). user_id=NULL = system default.
- Transfer tạo 2 Transaction records qua `transfer_group_id` (xử lý atomic).
- Budget period: Monthly, Weekly, Yearly. Alert ở 80% spent.

### Aggregate 2: Trip (Travel Module)
```
Trip → TripMember, TripActivity, TripExpense, TripSettlement, TripChatRoom
TripExpense → TripExpenseSplit
TripChatRoom → TripMessage
```
- Trip là container cho mọi hoạt động nhóm. Trip.status: Planning, Ongoing, Completed, Cancelled.
- TripMember.role: Organizer, Member, Viewer. invitation_status: Pending, Accepted, Declined.
- TripExpenseSplit: tổng share_amount = expense amount.
- TripMessage.message_type: Text, System, ActivityCard, ExpenseCard, PlaceCard. metadata dùng JSONB.
- TripChatRoom: 1-1 với Trip.

## Coding Convention (PHẢI TUÂN THEO)

| Rule | Ví dụ |
|------|-------|
| Controller inject `IMediator` + `IUserContext` | `UserController(IMediator mediator, IUserContext ctx)` |
| Controller dùng `result.Match(Ok, HandleFailure)` | Không còn `result.IsSuccess` thủ công |
| Command là `sealed record` | `sealed record LoginCommand(...) : ICommand<Result<LoginResponse>>` |
| Handler mỏng (1 dòng) | `=> await service.MethodAsync(request)` |
| Service trả về `Result<T>` | `Result<T>.Success(value)` / `Result<T>.Fail(error)` |
| Entity kế thừa `BaseEntity` | `Guid Id` = `Guid.CreateVersion7()` |
| Primary constructor C#12 | `class Service(IDep dep) : IService` |
| Validator: FluentValidation + auto-register | `AbstractValidator<T>` — không cần DI thủ công |
| Route: `api/{controller}` | `[Route("api/auth")]` |
| Namespace = folder path | `Features/Auth/Commands/Login/` |
| DTO: `sealed record` | `LoginResponse(string accessToken, ...)` |
| **Roles = string[] (breaking)** | **`role` → `roles` trong mọi DTO** |

## Hidden Rules (Quan trọng)

1. **Không throw cho lỗi nghiệp vụ** — luôn dùng `Result<T>.Fail()`.
2. **Repository không chứa business logic** — chỉ thao tác DB.
3. **Controller không gọi DbContext/Repository/Service** — chỉ gọi MediatR.
4. **FluentValidation validators KHÔNG đăng ký thủ công** — auto scan assembly.
5. **Soft Delete = `deleted_at IS NOT NULL`** — không dùng DELETE.
6. **Background job qua Hangfire** — không dùng `Task.Run()` hay `Thread`.
7. **Email luôn gửi qua Hangfire** — không gọi SMTP trực tiếp từ controller/handler.
8. **OTP lưu trong Redis, TTL 5 phút** — key pattern: `otp:{email}`.
9. **Token rotation bắt buộc** — refresh revokes old token.
10. **Cookie: refreshToken (HttpOnly=true), CSRF-TOKEN (HttpOnly=false)**.

## Known Bugs Cần Tránh

1. ~~**SqlKata dùng `SqlServerCompiler` nhưng DB là PostgreSQL**~~ — **SqlKata đã xóa** (Me branch P2).
2. **HSTS bật ở dev, tắt ở prod** — `if (isdev)` phải là `if (!isdev)` (SecureHeadersMiddleware.cs:40).
3. ~~**AuthController chỉ còn login endpoint active**~~ — **Đã implement lại UserController** (Me, Detail, List, Update, Roles, Lockout).
4. ~~**UserRepository trả stub data**~~ — **Đã xóa, thay bằng UserWriteRepository + UserReadRepository** (Dapper).
5. **Validation đăng ký ở cả Application và Infrastructure** — duplicate.
6. **RefreshToken không có FK cascade** — orphaned records nếu user bị xóa.
7. **Admin password hardcoded** — `"Admin@123"` trong `RoleSeeder.cs`.

## Feature Development Pattern (13 bước)

```
1. Domain Entity        → Domain/Entities/{Entity}.cs
2. Repository Interface → Domain/Interfaces/I{Entity}Repository.cs
3. Command/Query        → Application/Features/{Module}/Commands|Queries/{Action}/{Action}Command.cs
4. Handler              → Application/Features/{Module}/Commands|Queries/{Action}/{Action}CommandHandler.cs
5. Validator            → Application/Features/{Module}/Commands/{Action}/{Action}CommandValidator.cs
6. DTO                  → Application/Features/{Module}/DTOs/{Dto}.cs
7. Service Interface    → Application/Abstractions/Interfaces/{Module}/I{Name}Service.cs
8. Service Impl         → Infrastructure/Services/{Module}/{Name}Service.cs
9. Repository Impl      → Infrastructure/Persistence/Repositories/{Entity}/{Name}Repository.cs
10. Controller          → WebApi/Controllers/{Name}Controller.cs
11. DI Registration     → Infrastructure/Configuration/DependencyInjection.cs
12. EF Config           → Infrastructure/Persistence/Entities/BuildEntities.cs
13. Migration           → dotnet ef migrations add {Name}
```

## DI Reference

| Interface | Implementation | Lifetime |
|-----------|---------------|----------|
| `IMediator` | Mediator | Scoped |
| `IIdentityService` | IdentityService | Scoped |
| `ITokenService` | TokenService | Scoped |
| `IAuthCookieService` | AuthCookieService | Scoped |
| `IUserReadRepository` | UserReadRepository | Scoped |
| `IUserWriteRepository` | UserWriteRepository | Scoped |
| `IUserContext` | HttpUserContext | Scoped |
| `ICacheService` | RedisCacheService | Singleton |
| `IEmailService` | EmailService | Scoped |
| `IEmailChecker` | EmailChecker | Scoped |
| `IConnectionMultiplexer` | ConnectionMultiplexer | Singleton |
| Hangfire Server | — | Singleton |

## Error Codes

```csharp
ErrorCodes.InvalidCredentials    // "INVALID_CREDENTIALS"
ErrorCodes.UserLockedOut         // "USER_LOCKED_OUT"
ErrorCodes.EmailNotConfirmed     // "EMAIL_NOT_CONFIRMED"
ErrorCodes.UserNotFound          // "USER_NOT_FOUND"
ErrorCodes.OtpExpired            // "OTP_EXPIRED"
ErrorCodes.OtpIncorrect          // "OTP_INCORRECT"
ErrorCodes.ValidationError       // "VALIDATION_ERROR"
ErrorCodes.GeneralError          // "GENERAL_ERROR"
```

## Middleware Order (cố định)

```
ExceptionMiddleware → HangfireDashboard → CORS → HttpsRedirection
→ SecureHeadersMiddleware (CSP, HSTS, X-Frame-Options)
→ CookiePolicy (SameSite=None, Secure)
→ Authentication (JWT Bearer)
→ CSRFMiddleware
→ Authorization
→ MapControllers
```
