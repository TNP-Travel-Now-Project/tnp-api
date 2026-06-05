# Quy Tắc Dự Án — TNP API (Implicit Conventions)

Tài liệu này tổng hợp tất cả **quy tắc ngầm (implicit conventions)** được suy luận từ source code. Đây là những quy tắc mà team đang áp dụng nhưng không được document chính thức. Mọi development mới PHẢI tuân theo các quy tắc này.

---

## 1. Architecture Rules

### 1.1 Luồng phụ thuộc một chiều
```
Domain ← Application ← Infrastructure ← WebApi
```
**Inferred from:** Solution structure + `.csproj` project references.
- `Domain` KHÔNG được phép tham chiếu bất kỳ project nào khác.
- `Application` CHỈ được phép tham chiếu `Domain`.
- `Infrastructure` được phép tham chiếu `Application` + `Domain`.
- `WebApi` được phép tham chiếu `Application` + `Infrastructure`.
- **Nghiêm cấm** tham chiếu ngược (ví dụ: Domain không được biết đến Application).

### 1.2 Domain là pure .NET
**Inferred from:** `AuthApi.Domain.csproj` không có bất kỳ PackageReference nào.
- Domain layer KHÔNG được cài đặt NuGet packages.
- Domain chỉ chứa: entities, value objects, enums, interfaces, exceptions.
- Domain KHÔNG được phụ thuộc vào EF Core, Identity, hay bất kỳ framework nào.

### 1.3 Application layer chỉ điều phối, không chứa logic nghiệp vụ phức tạp
**Inferred from:** Tất cả Command Handlers đều 1-2 dòng, ủy quyền cho service ngay lập tức.
- Handler không chứa business logic — chỉ `mediator.Send() → service.Method()`.
- Logic nghiệp vụ phải nằm trong Domain entities (factory methods, behavior methods) hoặc Infrastructure services.

### 1.4 CQRS: ghi dùng Command, đọc dùng Query
**Inferred from:** Cấu trúc `Features/{Module}/Commands/` và `Features/{Module}/Queries/`.
- Thao tác ghi (tạo, sửa, xóa) luôn đi qua Command → Handler → Service.
- Thao tác đọc (lấy danh sách, chi tiết) luôn đi qua Query → Handler → QueryRepository.
- Command không bao giờ trả về dữ liệu danh sách. Query không bao giờ thay đổi dữ liệu.

### 1.5 WebApi layer phải mỏng
**Inferred from:** `AuthController.cs` và `UserController.cs` — controller không chứa logic.
- Controller chỉ làm 3 việc: nhận request → gọi MediatR → trả về response.
- Controller KHÔNG được gọi DbContext, Repository, hoặc Service trực tiếp.
- Controller KHÔNG được chứa logic xử lý.

---

## 2. Coding Rules

### 2.1 Luôn dùng primary constructor (C# 12)
**Inferred from:** Toàn bộ controllers, handlers, services, validators đều dùng primary constructor.
```csharp
public class Service(IDependency dep) : IService  // ✅ Đúng
public class Service : IService                    // ❌ Sai — phải dùng primary constructor
{
    private readonly IDependency _dep;
    public Service(IDependency dep) => _dep = dep; // ❌ Sai — không dùng cách cũ
}
```

### 2.2 Field naming: underscore prefix
**Inferred from:** `IdentityService.cs`, `TokenService.cs`, `AuthCookieService.cs`.
```csharp
public class Service(IDep dep1, IDep2 dep2) : IService
{
    // dep1 và dep2 là primary constructor parameters — dùng trực tiếp, không gán field
}
```
**Lưu ý:** Dự án dùng primary constructor parameters trực tiếp, không gán vào private fields. Các service như `IdentityService` dùng tham số trực tiếp (VD: `_tokenService`, `_userManager`).

### 2.3 Commands là sealed records
**Inferred from:** `LoginCommand.cs`, `RegisterCommand.cs`, `LogoutCommand.cs`,...
```csharp
public sealed record LoginCommand(
    string Email,
    string Password
) : ICommand<Result<LoginResponse>>;  // ✅ Đúng
```
- Command phải là `sealed record`.
- Command phải implement `ICommand<Result<TResponse>>`.
- Command phải immutable (dùng record, không class).

### 2.4 DTOs là sealed records hoặc POCO
**Inferred from:** `LoginResponse.cs` (record), `UserDto.cs` (record), `AuthUserDto.cs` (record).
```csharp
public sealed record LoginResponse(string accessToken, string expired, ...);  // ✅ Đúng
public sealed record UserDto { public Guid Id { get; init; } ... }            // ✅ Đúng (POCO-style)
```

### 2.5 Handlers phải mỏng (thin handler pattern)
**Inferred from:** `LoginCommandHandler.cs` (1 dòng), `LogoutCommandHandler.cs` (1 dòng),...
```csharp
public class LoginCommandHandler(IIdentityService _identities) 
    : ICommandHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken ct)
        => await _identities.LoginAsync(request);  // ✅ Chỉ 1 dòng, ủy quyền cho service
}
```
- Handler KHÔNG được chứa business logic.
- Handler KHÔNG được gọi DbContext hay Repository.
- Handler CHỈ được gọi 1 phương thức service duy nhất.

### 2.6 Async suffix cho mọi async method
**Inferred from:** `LoginAsync`, `RegisterAsync`, `SendOTPAsync`, `VerifyEmailAsync`,...
- Mọi async method đều phải kết thúc bằng `Async`.

### 2.7 File-scoped namespaces
**Inferred from:** Tất cả file đều dùng `namespace X.Y.Z;` (không dùng block namespace).
```csharp
namespace AuthApi.Application.Features.Auth.Commands.Login;  // ✅ Đúng
```

### 2.8 Nullable enabled + ImplicitUsings enabled
**Inferred from:** Tất cả `.csproj` đều có `<Nullable>enable</Nullable>` và `<ImplicitUsings>enable</ImplicitUsings>`.

---

## 3. Service Rules

### 3.1 Service luôn trả về Result\<T\>
**Inferred from:** Mọi method của `IIdentityService`, `ITokenService` đều trả về `Result<T>`.
```csharp
Task<Result<LoginResponse>> LoginAsync(LoginCommand request);     // ✅ Đúng
Task<LoginResponse> LoginAsync(LoginCommand request);              // ❌ Sai — thiếu Result<T>
```
- Service KHÔNG BAO GIỜ throw exception cho lỗi nghiệp vụ.
- Exception chỉ dùng cho lỗi hệ thống thực sự (sẽ được ExceptionMiddleware bắt).

### 3.2 Service không gọi service khác trực tiếp
**Inferred from:** `IdentityService` inject nhiều dependencies và điều phối chúng, nhưng mỗi service chỉ gọi dependencies của nó.
- Service orchestration được thực hiện bởi `IIdentityService` (facade pattern).
- Service A không được gọi Service B trực tiếp nếu không thông qua Interface.
- **Ngoại lệ:** `IdentityService` là facade gọi `TokenService`, `AuthCookieService`, `EmailService`,... — đây là orchestration, không phải coupling.

### 3.3 Service interface đặt trong Application, implementation trong Infrastructure
**Inferred from:** `IIdentityService` ở `Application/Abstractions/Interfaces/`, `IdentityService` ở `Infrastructure/Services/`.
```csharp
// Application/Abstractions/Interfaces/Auth/IIdentityService.cs  — Interface
// Infrastructure/Services/Auth/IdentityService.cs               — Implementation
```

### 3.4 Mỗi service đăng ký Scoped trừ khi có lý do đặc biệt
**Inferred from:** `DependencyInjection.cs` — tất cả service đều Scoped, chỉ `ConnectionMultiplexer` là Singleton.
```csharp
services.AddScoped<IIdentityService, IdentityService>();   // ✅ Scoped
services.AddSingleton<IConnectionMultiplexer>(redis);       // ✅ Singleton (có lý do: Redis connection pool)
```

### 3.5 Service không lưu state
**Inferred from:** Tất cả service đều stateless — state được lưu trong Database, Redis, hoặc Cookie.
- Service không có private mutable fields.
- Service không có static mutable state.

---

## 4. Repository Rules

### 4.1 Repository chỉ thao tác database thuần túy
**Inferred from:** `UserRepository.cs` và `UserQueryRepository.cs`.
- Repository không chứa business logic.
- Repository không gọi service khác.
- Repository không gửi email, không enqueue job, không ghi log.

### 4.2 Repository interface ở Domain, implementation ở Infrastructure
**Inferred from:** `IUserRepository` ở `Domain/Interfaces/`, `UserRepository` ở `Infrastructure/Persistence/Repositories/`.
```csharp
// Domain/Interfaces/IUserRepository.cs
public interface IUserRepository
{
    Task<Users> AddAsync(Users user);
    Task<Users?> GetUserByIdAsync(Guid id);
    Task<int> CommitAsync();
}
```
- Domain định nghĩa contract (interface).
- Infrastructure implement contract.

### 4.3 Query repository dùng SqlKata/Dapper, Command repository dùng EF Core
**Inferred from:** `UserQueryRepository` dùng `QueryFactory` (SqlKata), `UserRepository` dùng `AppDbContext` (EF Core).
- **CQRS Read side:** Dùng SqlKata + Dapper (hiệu năng cao, đọc nhanh).
- **CQRS Write side:** Dùng EF Core (change tracking, transaction, identity).

### 4.4 Repository phương thức trả về Task, không async void
**Inferred from:** Tất cả repository methods đều trả về `Task<T>` hoặc `Task`.

---

## 5. Entity Rules

### 5.1 Entity phải kế thừa BaseEntity
**Inferred from:** `Users : BaseEntity`, `Category : BaseEntity`.
```csharp
public class Users : BaseEntity, IAggregateRoot  // ✅ Đúng
public class Wallet : BaseEntity                   // ✅ Đúng
```
- `BaseEntity` cung cấp `Guid Id`.
- Id được khởi tạo bằng `Guid.CreateVersion7()`.

### 5.2 Sinh ID bằng Guid.CreateVersion7()
**Inferred from:** `Users.Create()`, `RefreshToken.cs`.
```csharp
var id = Guid.CreateVersion7();  // ✅ Time-ordered UUID, tốt cho index
```
- Không dùng `Guid.NewGuid()`.
- Không dùng `new Guid()`.
- Không dùng auto-generate của database.

### 5.3 Value Object dùng factory method
**Inferred from:** `Email.cs`.
```csharp
public class Email : IValueObject
{
    public string Value { get; }
    private Email(string value) => Value = value;          // Constructor private
    
    public static Email Create(string email) { ... }       // Factory method
}
```
- Constructor của Value Object phải private.
- Chỉ expose factory method để tạo instance (validation trong factory).

### 5.4 Marker interfaces cho DDD tactical patterns
**Inferred from:** `IEntity.cs`, `IAggregateRoot.cs`, `IValueObject.cs`.
```csharp
public interface IEntity;              // Đánh dấu entity
public interface IAggregateRoot;       // Đánh dấu aggregate root (internal)
public interface IValueObject;         // Đánh dấu value object
```

### 5.5 Domain entity tách biệt với Identity entity
**Inferred from:** `AuthApi.Domain.Entities.Users` (domain) khác với `AuthApi.Infrastructure.Identities.ApplicationUser` (Identity).
- Domain entity (`Users`) dùng cho business logic thuần.
- Identity entity (`ApplicationUser`) dùng cho authentication.
- Hai entity này đại diện cùng một concept nhưng disconnected — cần mapping.

### 5.6 Domain exception cho business rule violations
**Inferred from:** `DomainException.cs`, `UserAgeNotValid.cs`.
```csharp
public class UserAgeNotValid : DomainException
{
    public UserAgeNotValid(int age) : base($"User age {age} is not valid.") { }
}
```
- Dùng Domain exception class riêng (không dùng `ArgumentException` hay `InvalidOperationException` cho business rules).

### 5.7 Entity dùng factory method thay vì constructor public
**Inferred from:** `Users.Create()`.
```csharp
public static Users Create(int age, UserRole role, string name)
{
    // Validate...
    return new Users(id, role, name.Trim());
}
```
- Constructor thường là private hoặc protected.
- Factory method chứa validation logic.

---

## 6. API Rules

### 6.1 Controller chỉ gọi IMediator
**Inferred from:** `AuthController.cs`.
```csharp
[Route("api/auth"), ApiController]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginCommand command)
    {
        var result = await mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new ApiErrorResponse(result.Error!));
    }
}
```
- Controller KHÔNG inject service, repository, hoặc DbContext.
- Controller CHỈ inject `IMediator`.

### 6.2 Mọi API response theo pattern Result\<T\>
**Inferred from:** Tất cả controller actions.
```csharp
if (result.IsSuccess) return Ok(result.Value);
return BadRequest(new ApiErrorResponse(result.Error!));
```
- Success → `Ok(value)` (200).
- Failure → `BadRequest(apiErrorResponse)` (400).
- Không dùng try-catch trong controller (ExceptionMiddleware xử lý).

### 6.3 Route convention: api/{controller}
**Inferred from:** `[Route("api/auth")]`, `[Route("api/users")]`.
```csharp
[Route("api/auth")]        // ✅ /api/auth/login, /api/auth/register
[Route("api/users")]       // ✅ /api/users
```

### 6.4 ApiController attribute bắt buộc
**Inferred from:** Tất cả controllers đều có `[ApiController]`.

### 6.5 ProducesResponseType cho document
**Inferred from:** `AuthController.Login()`.
```csharp
[ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
```

### 6.6 [AllowAnonymous] hoặc [Authorize] ở class level
**Inferred from:** `AuthController` có `[AllowAnonymous]` ở class level.

### 6.7 ApiErrorResponse cho mọi response lỗi
**Inferred from:** `ApiErrorResponse.cs`, `ExceptionMiddleware.cs`.
```json
{
  "code": "VALIDATION_ERROR",
  "message": "Validation failed",
  "errors": { "Email": ["Email is required"] }
}
```

---

## 7. Validation Rules

### 7.1 Validation chỉ dùng FluentValidation
**Inferred from:** `RegisterCommandValidator.cs`, `LoginCommandValidator.cs`, không có `[Required]` attribute trong controllers.
- KHÔNG dùng Data Annotations (`[Required]`, `[EmailAddress]`) trong Controller actions.
- KHÔNG tự validate trong code (if-else throw).
- Chỉ dùng `AbstractValidator<T>` từ FluentValidation.

### 7.2 Validator tự động đăng ký qua assembly scanning
**Inferred from:** `DependencyInjection.cs`.
```csharp
services.AddValidatorsFromAssemblyContaining<ApplicationAssembly>();  // Application layer
services.AddValidatorsFromAssemblyContaining<RegisterCommandValidator>();  // Infrastructure layer
```
- Không cần đăng ký từng validator thủ công.
- Validator được MediatR pipeline `ValidationBehavior` tự động gọi.

### 7.3 Validator cùng thư mục với Command
**Inferred from:** `LoginCommandValidator.cs` ở cùng folder với `LoginCommand.cs`.
```
Features/Auth/Commands/Login/
├── LoginCommand.cs
├── LoginCommandHandler.cs
└── LoginCommandValidator.cs
```

### 7.4 ValidationException được ExceptionMiddleware bắt tự động
**Inferred from:** `ExceptionMiddleware.cs`.
```csharp
catch (ValidationException ex)
{
    context.Response.StatusCode = 400;
    // Tự động parse errors thành dictionary
    var errors = ex.Errors.GroupBy(e => e.PropertyName)
                          .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
```
- Không cần try-catch FluentValidation errors trong controller.
- ExceptionMiddleware tự động chuyển thành 400 response.

---

## 8. Authentication Rules

### 8.1 Access token qua Bearer header, không qua cookie
**Inferred from:** `Program.cs` — JWT config, `OnMessageReceived` event.
```csharp
OnMessageReceived = context =>
{
    var authorization = context.Request.Headers.Authorization.FirstOrDefault();
    if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
    {
        context.Token = authorization.Substring("Bearer ".Length).Trim();
    }
};
```
- Access token gửi qua header `Authorization: Bearer <token>`.
- Code lưu access token vào cookie đã bị comment (`//var token = context.Request.Cookies["accessToken"]`).

### 8.2 Refresh token qua HttpOnly cookie
**Inferred from:** `AuthCookieService.cs`, `TokenService.cs`.
```csharp
private static readonly CookieOptions _refreshTokenOptions = new()
{
    Secure = true,
    HttpOnly = true,
    SameSite = SameSiteMode.None,
    Path = "/"
};
```
- Refresh token không bao giờ gửi qua header hay body.
- Client không thể đọc refresh token bằng JavaScript (HttpOnly).

### 8.3 Token rotation bắt buộc
**Inferred from:** `TokenService.RefreshTokenAsync()`.
```csharp
refreshTokenEntity.IsRevoked = true;        // Thu hồi token cũ
_dbContext.RefreshToken.Update(refreshTokenEntity);
await _dbContext.SaveChangesAsync();
return await GenerateTokensAsync(...);       // Cấp token mới
```
- Mỗi lần refresh: token cũ bị revoke, token mới được cấp.
- Không cho phép dùng lại refresh token cũ.

### 8.4 Auth endpoints bypass CSRF
**Inferred from:** `CSRFMiddleware.cs`.
```csharp
if (path.StartsWith($"{apiAuth}/login") || path.StartsWith($"{apiAuth}/register")
    || path.StartsWith($"{apiAuth}/refresh-token") || path.StartsWith($"{apiAuth}/logout"))
{
    await _next(_context);
    return;  // Bỏ qua CSRF check
}
```

### 8.5 Email confirmation bắt buộc trước khi đăng nhập
**Inferred from:** `IdentityService.LoginAsync()`, Identity config.
```csharp
if (!user.EmailConfirmed)
    return Result<LoginResponse>.Fail(new Error(ErrorCodes.EmailNotConfirmed, "Email is not confirmed"));
```
- User chưa confirm email không thể đăng nhập.
- User không confirm trong 2h bị auto-delete (Hangfire job `EmailCleanupJob`).

### 8.6 Lockout policy: 5 lần sai → 10 phút
**Inferred from:** `IdentityService.LoginAsync()`, `DependencyInjection.cs`.
```csharp
if (await _userManager.IsLockedOutAsync(user))
    return Result<LoginResponse>.Fail(new Error(ErrorCodes.UserLockedOut, "User is locked"));

await _userManager.AccessFailedAsync(user);  // Tăng fail count
```

---

## 9. Database Rules

### 9.1 Soft Delete thay vì Delete vật lý
**Inferred from:** BUSINESS_DOMAIN.md — tất cả business entities đều có `deleted_at`.
```csharp
// Thay vì DELETE FROM Transaction WHERE id = '...'
// Làm: UPDATE Transaction SET deleted_at = NOW() WHERE id = '...'
```
- Không dùng `DELETE FROM` cho business data.
- Tất cả query phải filter `WHERE deleted_at IS NULL`.

### 9.2 Wallet.balance là cache — tuyệt đối không trust
**Inferred from:** BUSINESS_DOMAIN.md — nhấn mạnh nhiều lần.
```
Wallet.balance CHỈ LÀ CACHE.
Source of truth: Transaction table.
Công thức: SUM(Transaction.amount) WHERE wallet_id = ? AND deleted_at IS NULL
```
- Khi cần số dư chính xác (báo cáo, tổng kết): query Transaction.
- Wallet.balance chỉ dùng cho hiển thị dashboard nhanh.

### 9.3 Transaction là Source of Truth duy nhất cho tài chính
**Inferred from:** BUSINESS_DOMAIN.md.
- Mọi số dư cache: `Wallet.balance`, `Budget.spent`, `SavingGoal.current_amount`, `Debt.current_balance` đều tính từ Transaction.

### 9.4 timestamptz cho mọi DateTime trong database
**Inferred from:** Migration file — tất cả DateTime columns đều là `timestamp with time zone`.

### 9.5 Composite indexes cho Transaction
**Inferred from:** BUSINESS_DOMAIN.md — khuyến nghị rõ ràng.
- `(user_id, transaction_date)` — báo cáo tháng/năm.
- `(user_id, wallet_id)` — tính balance.
- `(user_id, category_id, transaction_date)` — báo cáo danh mục.

### 9.6 Audit fields cho mọi business table
**Inferred from:** BUSINESS_DOMAIN.md — liệt kê field chuẩn.
```
created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
created_by_id    UUID REFERENCES AspNetUsers(id)
updated_at       TIMESTAMPTZ
updated_by_id    UUID REFERENCES AspNetUsers(id)
deleted_at       TIMESTAMPTZ   -- NULL = active
```

### 9.7 RefreshToken không có FK cascade
**Inferred from:** Migration file — RefreshToken.UserId không có ON DELETE CASCADE.
```
RefreshToken.UserId → AspNetUsers.Id (KHÔNG CÓ CASCADE)
```
**Đây là lỗi tiềm ẩn:** Khi user bị xóa, refresh token không tự động bị xóa → orphaned records.

---

## 10. Feature Development Rules

### 10.1 Mỗi feature là một vertical slice
**Inferred from:** Cấu trúc thư mục `Features/{Module}/`.
```
Features/Auth/Commands/Login/     ← Login feature
Features/Auth/Commands/Register/   ← Register feature
Features/Users/Queries/GetUsers/   ← GetUsers feature
```

### 10.2 13 bước chuẩn khi thêm feature mới
**Inferred from:** Phân tích toàn bộ codebase convention.

| Step | Action | Location |
|------|--------|----------|
| 1 | Tạo Domain Entity | `Domain/Entities/{Entity}.cs` |
| 2 | Tạo Domain Repository Interface | `Domain/Interfaces/I{Entity}Repository.cs` |
| 3 | Tạo Command/Query | `Application/Features/{Module}/Commands|Queries/{Action}/{Action}Command.cs` |
| 4 | Tạo Handler | `Application/Features/{Module}/Commands|Queries/{Action}/{Action}CommandHandler.cs` |
| 5 | Tạo Validator | `Application/Features/{Module}/Commands/{Action}/{Action}CommandValidator.cs` |
| 6 | Tạo DTO | `Application/Features/{Module}/DTOs/{Dto}.cs` |
| 7 | Tạo Service Interface | `Application/Abstractions/Interfaces/{Module}/I{Name}Service.cs` |
| 8 | Tạo Service Implementation | `Infrastructure/Services/{Module}/{Name}Service.cs` |
| 9 | Tạo Repository Implementation | `Infrastructure/Persistence/Repositories/{Entity}/{Name}Repository.cs` |
| 10 | Tạo Controller | `WebApi/Controllers/{Name}Controller.cs` |
| 11 | Đăng ký DI | `Infrastructure/Configuration/DependencyInjection.cs` |
| 12 | Cấu hình EF Entity | `Infrastructure/Persistence/Entities/BuildEntities.cs` |
| 13 | Migration | `dotnet ef migrations add {Name}` |

### 10.3 Command/Query là sealed record implement ICommand\<Result\<T\>\> hoặc IQuery\<T\>
**Inferred from:** Mọi command/query trong dự án.
```csharp
public sealed record LoginCommand(...) : ICommand<Result<LoginResponse>>;
public sealed record GetAllUserQuery : IQuery<List<UserDto>>;
```

### 10.4 Tên file theo pattern {Action}{Type}
**Inferred from:**
```
LoginCommand.cs
LoginCommandHandler.cs
LoginCommandValidator.cs
LoginResponse.cs
RegisterCommand.cs
```

### 10.5 Namespace theo đường dẫn folder
**Inferred from:**
```csharp
namespace AuthApi.Application.Features.Auth.Commands.Login;     // Features/Auth/Commands/Login/
namespace AuthApi.Infrastructure.Services.Auth;                  // Infrastructure/Services/Auth/
```

### 10.6 Không dùng Mapster (dù đã cài)
**Inferred from:** Mapster có trong `.csproj` nhưng không được dùng ở bất kỳ đâu trong code.
- Mapping thủ công (manual) là convention hiện tại.
- Nếu muốn dùng Mapster, phải đồng bộ toàn bộ project.

---

## 11. Error Handling Rules

### 11.1 Result\<T\> cho mọi response (không throw)
**Inferred from:** Mọi method trong `IIdentityService`, mọi Command Handler.
```csharp
// ✅ Đúng: dùng Result<T>
return Result<LoginResponse>.Fail(new Error(ErrorCodes.InvalidCredentials, "Invalid credentials"));

// ❌ Sai: throw exception cho business logic
throw new UnauthorizedAccessException("Invalid credentials");
```

### 11.2 Error codes là constants trong ErrorCodes
**Inferred from:** `ErrorCodes.cs`.
```csharp
ErrorCodes.InvalidCredentials      // "INVALID_CREDENTIALS"
ErrorCodes.UserLockedOut           // "USER_LOCKED_OUT"
ErrorCodes.EmailNotConfirmed       // "EMAIL_NOT_CONFIRMED"
ErrorCodes.OtpExpired              // "OTP_EXPIRED"
```

### 11.3 ExceptionMiddleware là global error handler duy nhất
**Inferred from:** `ExceptionMiddleware.cs`, không có try-catch trong controller.
- Controller không có try-catch.
- Middleware bắt tất cả exception.
- FluentValidation error → 400.
- Exception khác → 500 (generic message, không leak detail).

### 11.4 ValidationException từ FluentValidation được xử lý riêng
**Inferred from:** `ExceptionMiddleware.cs`.
```csharp
catch (ValidationException ex)
{
    // Trả về 400 với field-level errors
}

catch (Exception)
{
    // Trả về 500 với message chung
}
```

---

## 12. Configuration Rules

### 12.1 Connection strings KHÔNG trong appsettings.json
**Inferred from:** `appsettings.json` chỉ có Logging + AllowedHosts.
- `ConnectionStrings:Default` (PostgreSQL) — từ User Secrets (dev) hoặc env (prod).
- `ConnectionStrings:Redis` (Redis) — từ User Secrets (dev) hoặc env (prod).

### 12.2 JWT key KHÔNG trong appsettings.json
**Inferred from:** `Program.cs` đọc từ `AppSettings` section, nhưng không có trong appsettings.json.
- `AppSettings:JwtKey`, `AppSettings:JwtIssuer`, `AppSettings:JwtAudience` — từ User Secrets/env.

### 12.3 Email config KHÔNG trong appsettings.json
**Inferred from:** `EmailService.cs` đọc từ `IConfiguration["Email:Smtp"]`.
- `Email:Smtp`, `Email:Port`, `Email:From`, `Email:Password` — từ User Secrets/env.

### 12.4 Options Pattern cho AppSettings
**Inferred from:** `AppSettings.cs` + `Program.cs`.
```csharp
services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
```

---

## 13. Middleware Rules

### 13.1 Thứ tự middleware KHÔNG được thay đổi
**Inferred from:** `Program.cs` — thứ tự được comment rõ ràng.
```
1. ExceptionMiddleware
2. HangfireDashboard
3. CORS
4. HttpsRedirection
5. SecureHeadersMiddleware
6. CookiePolicy
7. Authentication
8. CSRFMiddleware
9. Authorization
10. MapControllers
```

### 13.2 CSRF bỏ qua GET, HEAD, OPTIONS và auth endpoints
**Inferred from:** `CSRFMiddleware.cs`.

### 13.3 Hàngfire chỉ cho phép Admin
**Inferred from:** `HangfireAuthFilter.cs`.
```csharp
return httpContext.User.Identity?.IsAuthenticated == true
       && httpContext.User.IsInRole("Admin");
```

---

## 14. Redis Rules

### 14.1 OTP lưu trong Redis với TTL 5 phút
**Inferred from:** `IdentityService.SendOTPAsync()`.
```csharp
var key = $"otp:{email.ToLower()}";
var expired = TimeSpan.FromMinutes(5);
var otp = RandomNumberGenerator.GetInt32(1000, 9999).ToString();
await db.StringSetAsync(key, otp, expired);
```

### 14.2 Redis connection là Singleton
**Inferred from:** `DependencyInjection.cs`.
```csharp
var redis = ConnectionMultiplexer.Connect(redisOptions);
services.AddSingleton<IConnectionMultiplexer>(redis);
```

---

## 15. Hangfire Rules

### 15.1 Background jobs qua Hangfire, không dùng Task.Run
**Inferred from:** `IdentityService.cs` dùng `IBackgroundJobClient`.
```csharp
_jobClient.Enqueue(() => _emailService.SendEmailAsync(...));
_jobClient.Schedule<EmailCleanupJob>(p => p.DeleteUnverifiedUser(user.Id), TimeSpan.FromHours(2));
```

### 15.2 Email sending luôn qua Hangfire
**Inferred from:** `IdentityService.cs` — mọi SendEmail đều enqueue job, không gọi trực tiếp.

---

## 16. Password Rules

### 16.1 Password rules bị duplicate (lỗi)
**Inferred from:** `LoginCommandValidator.cs` và `RegisterCommandValidator.cs` có cùng rules.
- Password: 8-20 ký tự, 1 uppercase, 1 lowercase, 1 number, 1 special character.
- **Quy tắc đúng:** Cần extract thành shared validator, nhưng chưa được làm.

### 16.2 Identity config yêu cầu yếu hơn FluentValidation
**Inferred from:** `DependencyInjection.cs` — Identity config.
```csharp
options.Password.RequireDigit = true;
options.Password.RequireUppercase = true;
options.Password.RequiredLength = 6;   // Identity: 6 ký tự
```
- Nhưng FluentValidation yêu cầu: 8-20 ký tự.
- FluentValidation rules là chính xác (stricter).

---

## 17. Background Job Rules

### 17.1 Unverified user bị auto-delete sau 2 giờ
**Inferred from:** `IdentityService.RegisterAsync()`.
```csharp
_jobClient.Schedule<EmailCleanupJob>(p =>
    p.DeleteUnverifiedUser(user.Id), TimeSpan.FromHours(2));
```

### 17.2 Cleanup job kiểm tra EmailConfirmed trước khi xóa
**Inferred from:** `EmailCleanupJob.DeleteUnverifiedUser()`.
```csharp
if (user != null && !user.EmailConfirmed)
{
    await _userManager.DeleteAsync(user);  // Chỉ xóa nếu chưa confirm
}
```

---

## 18. Bug Rules (Anti-Patterns cần tránh)

### 18.1 SqlKata dùng sai SQL Compiler
**Inferred from:** `DependencyInjection.cs:44`.
```csharp
var compiler = new SqlServerCompiler();  // ❌ SAI: DB là PostgreSQL
// Phải là: var compiler = new PostgresCompiler();
```

### 18.2 HSTS bật sai môi trường
**Inferred from:** `SecureHeadersMiddleware.cs:40`.
```csharp
if (isdev)  // ❌ SAI: HSTS chỉ bật ở dev, tắt ở prod
// Phải là: if (!isdev)
```

### 18.3 Primary constructor param names không nhất quán
**Inferred from:** `IdentityService.cs` dùng `_tokenService`, `TokenService.cs` dùng `_dbContext`.
- `IdentityService`: `_tokenService`, `_userManager`, `_emailChecker`, `_emailService`, `_jobClient`, `_appSetting`, `_redis`, `_tokenHandler`
- `TokenService`: `_dbContext`, `_userManager`, `_appSetting`, `_tokenHandler`
- `EmailService`: `_config`
- Nên chuẩn hóa: luôn dùng `_` prefix hoặc không.
