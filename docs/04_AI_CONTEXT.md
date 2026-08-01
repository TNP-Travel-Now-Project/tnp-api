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

**Token expiry inconsistency:** `ITokenService` interface default `expiredDay = 15`, nhưng `TokenService` implementation default `expiredDay = 30`. Thực tế dùng **30 ngày** (implementation wins). Khi implement tính năng mới liên quan, cần đồng bộ.
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

---

## Feature Flows (Layer-by-Layer)

### MODULE: AUTH — 7 endpoints

---

#### 1. POST /api/auth/login

**Controller** (`AuthController.Login`)
→ nhận `LoginCommand(Email, Password, RememberMe)`
→ `mediator.Send(command)`
→ kiểm tra `result.IsSuccess` → `Ok(LoginResponse)` / `BadRequest(ApiErrorResponse)`

**Validator** (`LoginCommandValidator`)
→ Email required + valid format
→ Password 8-20 ký tự, uppercase, lowercase, digit, special

**Handler** (`LoginCommandHandler`)
→ `_identities.LoginAsync(request)` — 1 dòng

**Service** (`IdentityService.LoginAsync`)
1. `_userManager.FindByEmailAsync(email)` — tìm user
2. `_userManager.IsLockedOutAsync(user)` — check lockout (5 lần sai → 10 phút)
3. Check `user.EmailConfirmed` — phải confirm mới login được
4. `_userManager.CheckPasswordAsync(user, password)` — verify password
5. `_userManager.ResetAccessFailedCountAsync(user)` — reset fail count
6. `_userManager.GetRolesAsync(user)` — lấy roles
7. `_tokenService.GenerateTokensAsync(userDto, roles)` — tạo JWT + refresh token

→ Return `Result<LoginResponse>`

**TokenService.GenerateTokensAsync**
1. `GeneralJwtToken()` — tạo JWT (HMAC-SHA256, 15 phút, claims: nameid, email, unique_name, role[])
2. `GenerateRefreshTokenString()` — 64-byte random → Base64
3. Lưu `RefreshToken` entity vào DB (UserId, Token, ExpiresAt, IsRevoked=false)
4. `_tokenHandler.SetRefreshToken()` — set HttpOnly cookie
5. `_tokenHandler.SetCSRFToken()` — set non-HttpOnly cookie
6. Return `AuthResponse(AccessToken, RefreshToken=null, AccessTokenExpiresAt)`

**DTO Response:** `LoginResponse(accessToken, refreshToken=null, expired, userId, email, roles[])`

---

#### 2. POST /api/auth/register

**Controller** (`AuthController.Register`)
→ nhận `RegisterCommand(Email, FirstName, LastName, UserName, PhoneNumber, DateOfBirth, Password, ConfirmPassword)`
→ `mediator.Send(command)`
→ `Created(...)` / `BadRequest(ApiErrorResponse)`

**Validator** (`RegisterCommandValidator`)
→ FirstName, LastName required + max 50
→ UserName required + max 256
→ Email required + valid
→ PhoneNumber required + đúng 10 số
→ DateOfBirth: 18-25 tuổi
→ Password: 8-20, uppercase, lowercase, digit, special
→ ConfirmPassword == Password

**Handler** (`RegisterCommandHandler`)
→ `Email.Create(request.Email)` — validate email domain (value object)
→ rebuild request với email đã validate
→ `_identities.RegisterAsync(newRes)`

**Service** (`IdentityService.RegisterAsync`)
1. Tạo `ApplicationUser` (extends IdentityUser: FirstName, LastName, DOB, CreatedAt)
2. `_userManager.CreateAsync(user, password)` — tạo user trong DB
3. `_userManager.AddToRoleAsync(user, "User")` — gán role mặc định
4. `_userManager.GenerateEmailConfirmationTokenAsync(user)` — tạo token
5. Build confirm link: `FrontendUrl/api/auth/verify-email?userId={id}&token={encoded}`
6. `_jobClient.Enqueue(() => _emailService.SendEmailAsync(...))` — gửi email confirm (Hangfire)
7. `_jobClient.Schedule<EmailCleanupJob>(p => p.DeleteUnverifiedUser(user.Id), 2h)` — auto-delete nếu không confirm

→ Return `Result<RegisterResponse>`

**DTO Response:** `RegisterResponse(UserId, FirstName, LastName, UserName, Email, CreatedAt)`

---

#### 3. POST /api/auth/verify-email

**Controller** (`AuthController.VerifyEmail`)
→ `[FromQuery] Guid userId, [FromQuery] string token`
→ `mediator.Send(new VerifyEmailCommand(userId, token))`
→ `Ok()` / `BadRequest(ApiErrorResponse)`

**Handler** (`VerifyEmailCommandHander`)
→ `_identities.VerifyEmailAsync(request.UserId, request.Token)`

**Service** (`IdentityService.VerifyEmailAsync`)
1. `_userManager.FindByIdAsync(userId)` — tìm user
2. `_userManager.ConfirmEmailAsync(user, token)` — confirm email
3. Nếu fail → return error với detail từ Identity errors

→ Return `Result<bool>`

---

#### 4. POST /api/auth/send-otp

**Controller** (`AuthController.SendOTPByEmail`)
→ `[FromQuery] string email`
→ `mediator.Send(new SendOTPCommand(email))`
→ `Ok(OtpResponse)` / `BadRequest(ApiErrorResponse)`

**Handler** (`SendOTPCommandHandler`)
→ `Email.Create(request.Email)` — validate email domain
→ `_identities.SendOTPAsync(email.Value)`

**Service** (`IdentityService.SendOTPAsync`)
1. `_userManager.FindByEmailAsync(email)` — kiểm tra email tồn tại trong hệ thống
2. `_emailChecker.IsValidAsync(email)` — kiểm tra MX record (DnsClient)
3. Tạo OTP 4 số: `RandomNumberGenerator.GetInt32(1000, 9999)`
4. Lưu OTP vào Redis: `StringSetAsync("otp:{email}", otp, 5 phút)`
5. `_jobClient.Enqueue(() => _emailService.SendEmailAsync(...))` — gửi OTP qua email

→ Return `Result<OtpResponse>`

**DTO Response:** `OtpResponse { Email, Expired }`

---

#### 5. POST /api/auth/reset-password

**Controller** (`AuthController.ResetPassword`)
→ nhận `ResetPasswordCommand(Email, Otp, NewPass)`
→ `mediator.Send(command)`
→ `Ok(NewPassResponse)` / `BadRequest(ApiErrorResponse)`

**Validator** (`ResetPassCommandValidator`)
→ Email required + valid
→ OTP required + đúng 4 số
→ NewPass: 8-20, uppercase, lowercase, digit, special

**Handler** (`ResetPassCommandHandler`)
→ `Email.Create(request.Email)` — validate domain
→ `_identities.SetNewPassAsync(newRequest)`

**Service** (`IdentityService.SetNewPassAsync`)
1. Redis: `StringGetAsync("otp:{email}")` — lấy OTP từ Redis
2. Nếu key không tồn tại → `"OTP expired or not found"`
3. Nếu OTP không khớp → `"OTP incorrect"`
4. `db.KeyDeleteAsync(key)` — xóa OTP sau khi dùng
5. `_userManager.FindByEmailAsync(email)` — tìm user
6. `_userManager.GeneratePasswordResetTokenAsync(user)` — tạo reset token
7. `_userManager.ResetPasswordAsync(user, token, newPass)` — đặt mật khẩu mới
8. Update `user.UpdatedAt`

→ Return `Result<NewPassResponse>`

**DTO Response:** `NewPassResponse { Email, NewPassword }`

---

#### 6. POST /api/auth/refresh-token

**Controller** (`AuthController.RefreshToken`)
→ (không có body — đọc từ cookie)
→ `mediator.Send(new RefreshTokenCommand())`
→ `Ok(RefreshTokenResponse)` / `BadRequest(ApiErrorResponse)`

**Handler** (`RefeshTokenCommandHandler`)
→ `_identities.RefeshTokenAsync(request)`

**Service** (`IdentityService.RefeshTokenAsync`)
→ `_tokenService.RefreshTokenAsync()`

**TokenService.RefreshTokenAsync**
1. `_tokenHandler.GetRefreshToken()` — đọc refresh token từ HttpOnly cookie
2. Query DB: tìm RefreshToken còn hạn, chưa revoked
3. `_userManager.FindByIdAsync(userId)` — lấy user
4. `_userManager.GetRolesAsync(user)` — lấy roles
5. **Revoke token cũ:** `refreshTokenEntity.IsRevoked = true`
6. `GenerateTokensAsync(user, roles)` — tạo JWT + refresh token mới
7. Set cookies mới (refreshToken + CSRF-TOKEN)

→ Return `Result<RefreshTokenResponse>`

**DTO Response:** `RefreshTokenResponse(accessToken, refreshtoken=null, expiredAt)`

---

#### 7. POST /api/auth/logout

**Controller** (`AuthController.Logout`)
→ (không có body)
→ `mediator.Send(new LogoutCommand())`
→ `Ok(LogoutResponse)` / `BadRequest(ApiErrorResponse)`

**Handler** (`LogoutCommandHandler`)
→ `_identities.LogoutAsync(request)` — 1 dòng

**Service** (`IdentityService.LogoutAsync`)
1. `_tokenService.RevokeRefreshTokenAsync()` — revoke refresh token trong DB
2. `_tokenHandler.ClearTokens()` — xóa cookies (refreshToken, CSRF-TOKEN)

**TokenService.RevokeRefreshTokenAsync**
→ Đọc refresh token từ cookie → tìm entity → set `IsRevoked = true` → save

**AuthCookieService.ClearTokens**
→ Xóa cookies: `refreshToken`, `CSRF-TOKEN`

→ Return `Result<LogoutResponse>`

**DTO Response:** `LogoutResponse(Message: "Logout successful")`

---

### MODULE: USER — 7 endpoints

---

#### 8. GET /api/users

**Controller** (`UserController.GetUsers`)
→ `[AllowAnonymous]`
→ **Không qua MediatR** (gọi thẳng repository)
→ `_userRepo.GetAllUsersAsync()`
→ `Ok(List<UserListItemDto>)`

**Repository** (`UserReadRepository.GetAllUsersAsync`)
1. Query 1: `SELECT Id, Email, UserName, FirstName, LastName, CreatedAt FROM AspNetUsers`
2. Query 2: `SELECT ur.UserId, r.Name AS Role FROM AspNetUserRoles ur JOIN AspNetRoles r`
3. Map roles vào từng user bằng `roleLookup` dictionary

**DTO Response:** `List<UserListItemDto { Id, Email, UserName, FirstName, LastName, Roles[], CreatedAt }>`

---
        
#### 9. GET /api/users/me

**Controller** (`UserController.Me`)
→ `[Authorize]`
→ `mediator.Send(new MeQuery())`
→ check `ErrorCodes.UserNotFound` → 404
→ `Ok(MeResponse)` / `Unauthorized(ApiErrorResponse)`

**Handler** (`MeQueryHandler`)
1. Check `_context.IsAuthenticated` — nếu không → fail
2. `_callCache.TryGetCachedAsync(cacheKey)` — check Redis cache trước
3. Nếu cache hit → return luôn
4. `_userRepo.GetMeAsync(userId)` — cache miss → query DB
5. `_callCache.TrySetCacheAsync(cacheKey, user, 5 phút)` — set cache
→ Return `Result<MeResponse>`

**Repository** (`UserReadRepository.GetMeAsync`)
1. `QueryMultipleAsync` — 2 queries:
   - `SELECT ... FROM AspNetUsers WHERE Id = @UserId`
   - `SELECT r.Name FROM AspNetRoles r JOIN AspNetUserRoles ur WHERE ur.UserId = @UserId`
2. Map roles vào user response

**DTO Response:** `MeResponse { Id, Email, UserName, FirstName, LastName, DateOfBirth, PhoneNumber, EmailConfirmed, Roles[], CreatedAt, UpdatedAt }`

---

#### 10. GET /api/users/{id:guid}

**Controller** (`UserController.GetUserById`)
→ `[Authorize(Policy = "RequireAdmin")]`
→ `mediator.Send(new GetUserByIdQuery(id))`
→ check error codes: NotFound → 404, Forbidden → 403, Unauthorized → 401

**Handler** (`GetUserByIdQueryHandler`)
1. Check `_context.IsAuthenticated` — nếu không → fail Unauthorized
2. Check `_context.IsInRole("Admin")` — nếu không → fail Forbidden
3. `_userRepo.GetUserDetailAsync(request.UserId)` — query DB
→ Return `Result<UserDetailResponse>`

**Repository** (`UserReadRepository.GetUserDetailAsync`)
1. `QueryMultipleAsync`:
   - `SELECT ... (Id, Email, UserName, ..., LockoutEnd, AccessFailedCount, ...) FROM AspNetUsers`
   - `SELECT r.Name FROM AspNetRoles r JOIN AspNetUserRoles ur WHERE ur.UserId = @UserId`
2. Tính `IsLockedOut = LockoutEnabled && LockoutEnd > UtcNow`

**DTO Response:** `UserDetailResponse { Id, Email, UserName, FirstName, LastName, DateOfBirth, PhoneNumber, EmailConfirmed, IsLockedOut, LockoutEnabled, Roles[], LockoutEnd, CreatedAt, UpdatedAt }`

---

#### 11. PUT /api/users/{id:guid}

**Controller** (`UserController.UpdateUser`)
→ `[Authorize(Policy = "RequireAdmin")]`
→ nhận `UpdateUserRequest { FirstName?, LastName?, PhoneNumber?, DateOfBirth? }`
→ `mediator.Send(new UpdateUserCommand(id, ...))`
→ `NoContent()`

**Handler** (`UpdateUserCommandHandler`)
→ Map `UpdateUserCommand` → `UpdateUserRequest`
→ `_userRepo.UpdateUserAsync(userId, requestDto)`
→ Nếu `!updated` → throw `NotFoundException` (sẽ được ExceptionMiddleware bắt → 404)

**Repository** (`UserWriteRepository.UpdateUserAsync`)
```sql
UPDATE AspNetUsers
SET FirstName = COALESCE(@FirstName, FirstName),
    LastName = COALESCE(@LastName, LastName),
    PhoneNumber = COALESCE(@PhoneNumber, PhoneNumber),
    DOB = COALESCE(@DateOfBirth, DOB),
    UpdatedAt = @Now
WHERE Id = @UserId
```
→ Dùng `COALESCE` — chỉ update field được cung cấp

---

#### 12. POST /api/users/{id:guid}/roles

**Controller** (`UserController.AssignRoles`)
→ `[Authorize(Policy = "RequireAdmin")]`
→ body: `string[] roles`
→ `mediator.Send(new AssignRolesCommand(id, roles))`
→ `NoContent()`

**Validator** (`AssignRolesCommandValidator`)
→ UserId required
→ Roles not empty

**Handler** (`AssignRolesCommandHandler`)
→ `_userRepo.AssignRolesAsync(request.UserId, request.Roles)`

**Repository** (`UserWriteRepository.AssignRolesAsync`)
```sql
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT @UserId, r.Id
FROM AspNetRoles r
WHERE r.Name IN @Roles
  AND NOT EXISTS (
      SELECT 1 FROM AspNetUserRoles ur
      WHERE ur.UserId = @UserId AND ur.RoleId = r.Id
  )
```
→ Chỉ insert roles chưa tồn tại (tránh duplicate)

---

#### 13. DELETE /api/users/{id:guid}/roles

**Controller** (`UserController.RemoveRoles`)
→ `[Authorize(Policy = "RequireAdmin")]`
→ body: `string[] roles`
→ `mediator.Send(new RemoveRolesCommand(id, roles))`
→ `NoContent()`

**Validator** (`RemoveRolesCommandValidator`)
→ UserId required
→ Roles not empty

**Handler** (`RemoveRolesCommandHandler`)
→ `_userRepo.RemoveRolesAsync(request.UserId, request.Roles)`

**Repository** (`UserWriteRepository.RemoveRolesAsync`)
```sql
DELETE ur
FROM AspNetUserRoles ur
INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
WHERE ur.UserId = @UserId AND r.Name IN @Roles
```

---

#### 14. DELETE /api/users/{id:guid}

**Controller** (`UserController.LockoutUser`)
→ `[Authorize(Policy = "RequireAdmin")]`
→ **Không qua MediatR** (gọi thẳng repository)
→ `_userWriteRepo.SoftDeleteUserAsync(id)`
→ Nếu `!updated` → 404
→ `NoContent()`

**Repository** (`UserWriteRepository.SoftDeleteUserAsync`)
```sql
UPDATE AspNetUsers
SET LockoutEnabled = 1,
    LockoutEnd = @LockoutEnd,    -- DateTimeOffset.MaxValue (khóa vĩnh viễn)
    UpdatedAt = @Now
WHERE Id = @UserId
```
→ Soft delete = lockout vĩnh viễn, không xóa vật lý

---

## Coding Convention (PHẢI TUÂN THEO)

| Rule | Ví dụ |
|------|-------|
| Controller inject `IMediator` + `IUserContext` | `UserController(IMediator mediator, IUserContext ctx)` |
| Controller dùng `result.IsSuccess` + `result.Match` | AuthController: `result.IsSuccess ? Ok() : BadRequest()` — UserController: trộn cả 2 pattern |
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
11. **Mọi đọc/ghi DB đều qua Dapper** — EF Core chỉ dùng nội bộ cho Identity.
12. **UserWriteRepository dùng COALESCE cho partial update** — chỉ update field được cung cấp.
13. **ExceptionMiddleware tự động bắt `NotFoundException` → 404** — handler có thể throw thay vì return Result.
14. **MeQuery có Redis cache (5 phút)** — cache key: `cache:me:{userId}`.

## Known Bugs Cần Tránh

1. ~~**SqlKata dùng `SqlServerCompiler` nhưng DB là PostgreSQL**~~ — **SqlKata đã xóa** (Me branch P2).
2. **HSTS bật ở dev, tắt ở prod** — `if (isdev)` phải là `if (!isdev)` (SecureHeadersMiddleware.cs:40).
3. ~~**AuthController chỉ còn login endpoint active**~~ — **Đã implement lại UserController** (Me, Detail, List, Update, Roles, Lockout).
4. ~~**UserRepository trả stub data**~~ — **Đã xóa, thay bằng UserWriteRepository + UserReadRepository** (Dapper).
5. **Validation đăng ký ở cả Application và Infrastructure** — duplicate.
6. **RefreshToken không có FK cascade** — orphaned records nếu user bị xóa.
7. **Admin password hardcoded** — `"Admin@123"` trong `RoleSeeder.cs`.
8. **ITokenService vs TokenService default expiry khác nhau** — interface: 15 ngày, implementation: 30 ngày. Cần đồng bộ.

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
9. Repository Impl      → Infrastructure/Persistence/Dapper/Repositories/{Entity}/{Name}Repository.cs
10. Controller          → WebApi/Controllers/{Name}Controller.cs
11. DI Registration     → Infrastructure/Configuration/DependencyInjection.cs
12. EF Config           → Infrastructure/Persistence/Entities/BuildEntities.cs
13. Migration           → dotnet ef migrations add {Name}
```

## DI Reference

| Interface | Implementation | Lifetime | Layer |
|-----------|---------------|----------|-------|
| `IMediator` | Mediator | Scoped | Application |
| `IIdentityService` | IdentityService | Scoped | Infrastructure |
| `ITokenService` | TokenService | Scoped | Infrastructure |
| `IAuthCookieService` | AuthCookieService | Scoped | Infrastructure |
| `IUserReadRepository` | UserReadRepository | Scoped | Infrastructure |
| `IUserWriteRepository` | UserWriteRepository | Scoped | Infrastructure |
| `IUserContext` | HttpUserContext | Scoped | Infrastructure |
| `ICallCacheService` | CallCacheService | Scoped | Infrastructure |
| `ICacheService` | RedisCacheService | Singleton | Infrastructure |
| `IEmailService` | EmailService | Scoped | Infrastructure |
| `IEmailChecker` | EmailChecker | Scoped | Infrastructure |
| `IConnectionMultiplexer` | ConnectionMultiplexer | Singleton | Infrastructure |
| `IDbConnectionFactory` | DbConnectionFactory | Singleton | Infrastructure |
| Hangfire Server | — | Singleton | Infrastructure |

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
ErrorCodes.Forbidden             // "FORBIDDEN"
ErrorCodes.Unauthorized          // "UNAUTHORIZED"
ErrorCodes.NotFound              // "NOT_FOUND"
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
