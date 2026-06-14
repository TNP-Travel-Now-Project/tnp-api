# Hướng Dẫn: Thêm Tính Năng Mới

> **Áp dụng cho:** `feature/TNP-TuanNT-Me` branch (Clean Architecture + CQRS)
> **Cập nhật:** 09/06/2026

---

## Mục Lục

1. [Kiến Trúc Dự Án (Nhìn Nhanh)](#1-kiến-trúc-dự-án-nhìn-nhanh)
2. [Quy Tắc Bất Di Bất Dịch](#2-quy-tắc-bất-di-bất-dịch)
3. [Các Loại Công Việc](#3-các-loại-công-việc)
4. [Loại 1: Thêm Tính Năng Mới (Full Feature)](#4-loại-1-thêm-tính-năng-mới-full-feature)
5. [Loại 2: Thêm Action Method Mới Vào Controller Có Sẵn](#5-loại-2-thêm-action-method-mới-vào-controller-có-sẵn)
6. [Loại 3: Thêm Controller Mới](#6-loại-3-thêm-controller-mới)
7. [Loại 4: Thêm Query Đơn Giản (Read-Only, Không Cần Command)](#7-loại-4-thêm-query-đơn-giản-read-only-không-cần-command)
8. [Checklist Khi Tạo File Mới](#8-checklist-khi-tạo-file-mới)
9. [Ví dụ Hoàn Chỉnh: Thêm Tính Năng "User Profile Picture"](#9-ví-dụ-hoàn-chỉnh-thêm-tính-năng-user-profile-picture)

---

## 1. Kiến Trúc Dự Án (Nhìn Nhanh)

```
┌─────────────────────────────────────────────────────┐
│                 AuthApi.WebApi                       │
│  (Controllers, Middleware, Program.cs)               │
│  Chỉ gọi MediatR, không chứa business logic         │
└──────────────────┬──────────────────────────────────┘
                   │
                   ▼ MediatR (Command/Query)
┌─────────────────────────────────────────────────────┐
│               AuthApi.Application                    │
│  (CQRS, Interfaces, DTOs, Validators)               │
│  Chỉ chứa logic, không biết đến DB hay HTTP         │
└──────────────────┬──────────────────────────────────┘
                   │
                   ▼ Dependency Injection
┌─────────────────────────────────────────────────────┐
│             AuthApi.Infrastructure                   │
│  (Dapper, Redis, Identity, Http)                    │
│  Cài đặt cụ thể cho các Interface                   │
└─────────────────────────────────────────────────────┘
```

**Luồng dữ liệu bắt buộc:**

```
Controller → MediatR → Handler → Interface (Application)
                                    ↓
                               Implementation (Infrastructure)
                                    ↓
                               SQL Server / Redis / ...
```

---

## 2. Quy Tắc Bất Di Bất Dịch

### 2.1. Luật Vàng

| # | Quy tắc | Giải thích |
|---|---------|------------|
| 1 | **Controller KHÔNG chứa logic** | Chỉ gọi MediatR, map HTTP → CQRS |
| 2 | **Handler KHÔNG gọi trực tiếp DB** | Phải qua Interface (Repository/Service) |
| 3 | **Interface đặt ở Application** | Infrastructure cài đặt, Application định nghĩa |
| 4 | **DTO riêng cho từng use case** | Không dùng chung DTO cho nhiều mục đích |
| 5 | **Validator cho mọi Command** | FluentValidation, kiểm tra input đầu vào |
| 6 | **Result pattern cho response** | `Result<T>` — Success/NotFound/Unauthorized/Forbidden |
| 7 | **Dapper — không SqlKata, không EF Core** | SQL thuần, parameterized queries |
| 8 | **Roles[] — không Role** | Luôn dùng `string[]` vì user có nhiều role |
| 9 | **Authorize Policy — không check thủ công** | `[Authorize(Policy = "RequireAdmin")]`, không `if (User.IsInRole(...))` |
| 10 | **Cache chỉ cho Read, không cho Write** | Chỉ cache Query, không cache Command |

### 2.2. Quy Tắc Đặt Tên

```
File/Folder Structure:  PascalCase per folder/file
Namespace:              AuthApi.{Layer}.{Feature}.{Type}
DTO fields:             camelCase (JSON serialization)
Methods:                PascalCase
Parameters:             camelCase
Private fields:         _camelCase
```

### 2.3. Packages Được Phép Dùng

```
✅ Dapper 2.x              — Database access
✅ MediatR                 — CQRS
✅ FluentValidation        — Input validation
✅ StackExchange.Redis     — Caching (qua IDistributedCache)
✅ ASP.NET Core Identity   — Authentication

❌ SqlKata                — Đã xóa
❌ EF Core (cho queries)  — Không dùng cho đọc, viết
❌ AutoMapper             — Không có, Map thủ công
❌ Mapster                — Không có
```

---

## 3. Các Loại Công Việc

| Loại | Mô tả | Mức độ |
|------|-------|--------|
| **Full Feature** | Chức năng mới hoàn chỉnh (vd: CRUD sản phẩm) | Tốn nhiều file nhất |
| **Action method mới** | Thêm 1 endpoint vào controller có sẵn (vd: thêm GET /users/export) | Vừa |
| **Controller mới** | Tạo controller cho entity mới (vd: CategoriesController) | Nhiều |
| **Query đơn giản** | Chỉ đọc, không cần validate phức tạp | Ít |
| **Command đơn giản** | Chỉ ghi, logic đơn giản | Ít |

---

## 4. Loại 1: Thêm Tính Năng Mới (Full Feature)

### 4.1. Quy Trình 7 Bước

```
Bước 1 ── Xác định: Feature này là Read (Query) hay Write (Command)?
               │
               ├── Read  → Bước 2a: Tạo DTO + Query + Handler
               └── Write → Bước 2b: Tạo DTO + Command + Handler + Validator
               │
               ▼
Bước 3 ── Tạo Interface trong Application layer
               │
               ▼
Bước 4 ── Implement Interface trong Infrastructure layer
               │
               ▼
Bước 5 ── Đăng ký Dependency Injection
               │
               ▼
Bước 6 ── Thêm endpoint vào Controller + Authorize attribute
               │
               ▼
Bước 7 ── Kiểm tra: build, test
```

### 4.2. Chi Tiết Từng Bước

#### Bước 1: Xác Định Read Hay Write

```
┌──────────────────────────────────────────────────┐
│ Câu hỏi: "Tính năng này làm gì?"                  │
│                                                    │
├─ "Lấy thông tin..."        → QUERY (Read)         │
├─ "Tạo mới..."              → COMMAND (Write)      │
├─ "Cập nhật..."             → COMMAND (Write)      │
├─ "Xóa..."                  → COMMAND (Write)      │
├─ "Xuất danh sách..."       → QUERY (Read)         │
└──────────────────────────────────────────────────┘
```

#### Bước 2a: Tạo DTO + Query + Handler (Read)

**Vị trí file:**

```
AuthApi.Application/Features/{FeatureName}/
├── DTOs/
│   └── {FeatureName}Response.cs        ← DTO trả về
└── Queries/
    └── Get{FeatureName}/
        ├── Get{FeatureName}Query.cs     ← Query (Request)
        └── Get{FeatureName}QueryHandler.cs  ← Handler
```

**Ví dụ: GetUserProfileQuery**

```csharp
// FILE 1: AuthApi.Application/Features/UserProfile/DTOs/UserProfileResponse.cs
namespace AuthApi.Application.Features.UserProfile.DTOs;

public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public List<string> Roles { get; set; } = [];
}
```

```csharp
// FILE 2: AuthApi.Application/Features/UserProfile/Queries/GetUserProfile/GetUserProfileQuery.cs
namespace AuthApi.Application.Features.UserProfile.Queries.GetUserProfile;

public record GetUserProfileQuery(Guid UserId) : IRequest<Result<UserProfileResponse>>;
```

```csharp
// FILE 3: AuthApi.Application/Features/UserProfile/Queries/GetUserProfile/GetUserProfileQueryHandler.cs
namespace AuthApi.Application.Features.UserProfile.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileResponse>>
{
    private readonly IUserProfileRepository _repo;

    public GetUserProfileQueryHandler(IUserProfileRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<UserProfileResponse>> Handle(GetUserProfileQuery request, CancellationToken ct)
    {
        var profile = await _repo.GetProfileAsync(request.UserId);

        if (profile is null)
            return Result<UserProfileResponse>.NotFound();

        return Result<UserProfileResponse>.Success(profile);
    }
}
```

#### Bước 2b: Tạo DTO + Command + Handler + Validator (Write)

**Vị trí file:**

```
AuthApi.Application/Features/{FeatureName}/
├── DTOs/
│   └── {FeatureName}Request.cs         ← DTO nhận từ client
└── Commands/
    └── {ActionName}/
        ├── {ActionName}Command.cs       ← Command (Request)
        ├── {ActionName}CommandHandler.cs ← Handler
        └── {ActionName}CommandValidator.cs ← FluentValidation
```

**Ví dụ: UpdateUserProfileCommand**

```csharp
// FILE 1: AuthApi.Application/Features/UserProfile/DTOs/UpdateUserProfileRequest.cs
namespace AuthApi.Application.Features.UserProfile.DTOs;

public class UpdateUserProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}
```

```csharp
// FILE 2: AuthApi.Application/Features/UserProfile/Commands/UpdateUserProfile/UpdateUserProfileCommand.cs
namespace AuthApi.Application.Features.UserProfile.Commands.UpdateUserProfile;

public record UpdateUserProfileCommand(
    Guid CurrentUserId,              // Luôn lấy từ IUserContext
    string? DisplayName,
    string? Bio,
    string? AvatarUrl
) : IRequest<Result<Unit>>;
```

```csharp
// FILE 3: AuthApi.Application/Features/UserProfile/Commands/UpdateUserProfile/UpdateUserProfileCommandValidator.cs
namespace AuthApi.Application.Features.UserProfile.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.CurrentUserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100).When(x => x.DisplayName is not null)
            .WithMessage("DisplayName cannot exceed 100 characters");

        RuleFor(x => x.Bio)
            .MaximumLength(500).When(x => x.Bio is not null)
            .WithMessage("Bio cannot exceed 500 characters");
    }
}
```

```csharp
// FILE 4: AuthApi.Application/Features/UserProfile/Commands/UpdateUserProfile/UpdateUserProfileCommandHandler.cs
namespace AuthApi.Application.Features.UserProfile.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<Unit>>
{
    private readonly IUserProfileRepository _repo;

    public UpdateUserProfileCommandHandler(IUserProfileRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<Unit>> Handle(UpdateUserProfileCommand request, CancellationToken ct)
    {
        var rows = await _repo.UpdateProfileAsync(
            request.CurrentUserId,
            request.DisplayName,
            request.Bio,
            request.AvatarUrl
        );

        if (rows == 0)
            return Result<Unit>.NotFound();

        return Result<Unit>.Success(Unit.Value);
    }
}
```

#### Bước 3: Tạo Interface Trong Application Layer

**Vị trí:** `AuthApi.Application/Abstractions/Interfaces/Repositories/`

```csharp
// FILE: AuthApi.Application/Abstractions/Interfaces/Repositories/IUserProfileRepository.cs
namespace AuthApi.Application.Abstractions.Interfaces.Repositories;

public interface IUserProfileRepository
{
    // Read
    Task<UserProfileResponse?> GetProfileAsync(Guid userId);

    // Write
    Task<int> UpdateProfileAsync(Guid userId, string? displayName, string? bio, string? avatarUrl);
}
```

**Quy tắc interface:**
- Method trả về DTO (từ Application layer) — không trả về Entity
- Method write trả về `int` (số rows affected)
- Method read trả về DTO hoặc `List<DTO>`

#### Bước 4: Implement Trong Infrastructure Layer

**Vị trí:** `AuthApi.Infrastructure/Persistence/Dapper/Repositories/`

```csharp
// FILE: AuthApi.Infrastructure/Persistence/Dapper/Repositories/UserProfileRepository.cs
namespace AuthApi.Infrastructure.Persistence.Dapper.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserProfileRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var profile = await connection.QueryFirstOrDefaultAsync<UserProfileResponse>(
            "SELECT Id, Email, DisplayName, Bio, AvatarUrl " +
            "FROM UserProfiles WHERE Id = @UserId",
            new { UserId = userId });

        return profile;
    }

    public async Task<int> UpdateProfileAsync(Guid userId, string? displayName, string? bio, string? avatarUrl)
    {
        using var connection = _connectionFactory.CreateConnection();

        var rows = await connection.ExecuteAsync(
            "UPDATE UserProfiles SET " +
            "DisplayName = COALESCE(@DisplayName, DisplayName), " +
            "Bio = COALESCE(@Bio, Bio), " +
            "AvatarUrl = COALESCE(@AvatarUrl, AvatarUrl), " +
            "UpdatedAt = GETUTCDATE() " +
            "WHERE Id = @UserId",
            new
            {
                UserId = userId,
                DisplayName = displayName,
                Bio = bio,
                AvatarUrl = avatarUrl
            });

        return rows;
    }
}
```

**Mẹo Dapper:**

```
SELECT
├─ QueryAsync<T>     → Nhiều dòng
├─ QueryFirstOrDefaultAsync<T> → 1 dòng hoặc null
├─ QuerySingleAsync<T> → Exactly 1 dòng (throw nếu không)

UPDATE/INSERT/DELETE
└─ ExecuteAsync      → Trả về số rows affected

Parameters
└─ new { @param1 = value1, @param2 = value2 }
   Tên param trong SQL phải khớp tên property trong anonymous object
```

#### Bước 5: Đăng Ký Dependency Injection

**Vị trí:** `AuthApi.Infrastructure/Configuration/DependencyInjection.cs`

```csharp
// Tìm đến đoạn DI registration, thêm dòng:
services.AddScoped<IUserProfileRepository, UserProfileRepository>();
```

#### Bước 6: Thêm Endpoint Vào Controller

```csharp
// AuthApi.WebApi/Controllers/UserProfileController.cs
[ApiController]
[Route("api/[controller]")]
public class UserProfileController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserContext _userContext;

    public UserProfileController(IMediator mediator, IUserContext userContext)
    {
        _mediator = mediator;
        _userContext = userContext;
    }

    // GET /api/userprofile/me — User xem profile của mình
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var result = await _mediator.Send(new GetUserProfileQuery(_userContext.UserId));
        return result.Match(Ok, this.HandleFailure);
    }

    // PUT /api/userprofile/me — User cập nhật profile của mình
    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest request)
    {
        var command = new UpdateUserProfileCommand(
            _userContext.UserId,
            request.DisplayName,
            request.Bio,
            request.AvatarUrl
        );

        var result = await _mediator.Send(command);
        return result.Match(_ => NoContent(), this.HandleFailure);
    }
}
```

#### Bước 7: Build và Test

```bash
dotnet build
dotnet test AuthApi.Tests/AuthApi.Tests.csproj
```

---

## 5. Loại 2: Thêm Action Method Mới Vào Controller Có Sẵn

### 5.1. Khi Nào Dùng?

- Tính năng mới thuộc cùng entity với controller có sẵn
- Vd: Thêm `GET /api/users/export` vào `UserController`

### 5.2. Quy Trình 4 Bước

```
Bước 1 ── Tạo Query/Command + Handler (Application layer)
               │
               ▼
Bước 2 ── Thêm method vào Repository Interface + Implementation
               │
               ▼
Bước 3 ── Thêm action method vào Controller
               │
               ▼
Bước 4 ── Build + Test
```

### 5.3. Ví dụ: Thêm GET /api/users/export

```csharp
// Bước 1: Query
// AuthApi.Application/Features/Users/Queries/ExportUsers/ExportUsersQuery.cs
public record ExportUsersQuery : IRequest<Result<List<UserExportDto>>>;

// AuthApi.Application/Features/Users/Queries/ExportUsers/ExportUsersQueryHandler.cs
public class ExportUsersQueryHandler : IRequestHandler<ExportUsersQuery, Result<List<UserExportDto>>>
{
    private readonly IUserReadRepository _repo;
    public ExportUsersQueryHandler(IUserReadRepository repo) => _repo = repo;

    public async Task<Result<List<UserExportDto>>> Handle(ExportUsersQuery request, CancellationToken ct)
    {
        var users = await _repo.GetExportDataAsync();
        return Result<List<UserExportDto>>.Success(users.ToList());
    }
}
```

```csharp
// Bước 2: Thêm vào Interface + Implementation
// IUserReadRepository thêm:
Task<IReadOnlyList<UserExportDto>> GetExportDataAsync();

// UserReadRepository thêm:
public async Task<IReadOnlyList<UserExportDto>> GetExportDataAsync()
{
    using var connection = _connectionFactory.CreateConnection();

    var users = await connection.QueryAsync<UserExportDto>(
        "SELECT Id, Email, UserName, EmailConfirmed, CreatedAt FROM AspNetUsers");

    return users.AsList().AsReadOnly();
}
```

```csharp
// Bước 3: Thêm action vào UserController
[Authorize(Policy = "RequireAdmin")]
[HttpGet("export")]
public async Task<IActionResult> ExportUsers()
{
    var result = await _mediator.Send(new ExportUsersQuery());
    return result.Match(Ok, this.HandleFailure);
}
```

---

## 6. Loại 3: Thêm Controller Mới

### 6.1. Khi Nào?

- Entity hoàn toàn mới (vd: Categories, Products, Orders)
- Cần nhóm endpoints riêng biệt

### 6.2. Quy Trình

```
Bước 1 ── Tạo folder Feature/{EntityName} trong Application
Bước 2 ── Tạo Interface Repository trong Application
Bước 3 ── Implement Repository trong Infrastructure
Bước 4 ── Đăng ký DI
Bước 5 ── Tạo Controller trong WebApi
Bước 6 ── Thêm Route + Authorize attributes
```

### 6.3. Mẫu Controller Chuẩn

```csharp
// AuthApi.WebApi/Controllers/CategoriesController.cs
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserContext _userContext;

    public CategoriesController(IMediator mediator, IUserContext userContext)
    {
        _mediator = mediator;
        _userContext = userContext;
    }

    // GET /api/categories — Public (ai cũng xem được)
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllCategoriesQuery());
        return result.Match(Ok, this.HandleFailure);
    }

    // GET /api/categories/{id} — Public
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id));
        return result.Match(Ok, this.HandleFailure);
    }

    // POST /api/categories — Chỉ Admin
    [Authorize(Policy = "RequireAdmin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var command = new CreateCategoryCommand(
            _userContext.UserId,  // Ghi lại ai tạo
            request.Name,
            request.Description
        );
        var result = await _mediator.Send(command);
        return result.Match(
            value => CreatedAtAction(nameof(GetById), new { id = value }, value),
            this.HandleFailure
        );
    }

    // PUT /api/categories/{id} — Chỉ Admin
    [Authorize(Policy = "RequireAdmin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var command = new UpdateCategoryCommand(id, request.Name, request.Description);
        var result = await _mediator.Send(command);
        return result.Match(_ => NoContent(), this.HandleFailure);
    }

    // DELETE /api/categories/{id} — Chỉ Admin
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id));
        return result.Match(_ => NoContent(), this.HandleFailure);
    }
}
```

### 6.4. Các HTTP Status Code Cần Trả Về

| HTTP Status | Khi nào dùng | Cách trả về trong controller |
|-------------|--------------|------------------------------|
| **200 OK** | GET thành công, có data | `Ok(result.Data)` |
| **201 Created** | POST tạo mới thành công | `CreatedAtAction(nameof(GetById), new { id }, data)` |
| **204 No Content** | PUT/DELETE thành công, không return data | `NoContent()` |
| **400 Bad Request** | Validation lỗi | Tự động từ FluentValidation |
| **401 Unauthorized** | Không có token hoặc token hết hạn | Tự động từ JWT Middleware |
| **403 Forbidden** | Có token nhưng không đủ quyền | Tự động từ Authorize Policy |
| **404 Not Found** | Resource không tồn tại | `Result<T>.NotFound()` → `HandleFailure()` |

---

## 7. Loại 4: Thêm Query Đơn Giản (Read-Only, Không Cần Command)

### 7.1. Khi Nào Có Thể Bỏ Qua MediatR?

**Nên bỏ qua MediatR (gọi Repository trực tiếp):**
- Endpoint list/dashboard tổng hợp nhiều data
- Không cần validation
- Không cần business logic
- Chỉ đơn giản là SELECT + Map

**Ví dụ trong code hiện tại:**

```csharp
// UserController.GetUsers() — gọi Repository trực tiếp
[AllowAnonymous]
[HttpGet]
public async Task<IActionResult> GetUsers()
{
    var users = await _userRepo.GetAllUsersAsync();
    return Ok(users);
}
```

**Không bỏ qua MediatR khi:**
- Cần check quyền trong handler
- Cần cache (Decorator pattern với MediatR pipeline)
- Cần validation
- Cần business logic trước/sau khi query

### 7.2. Quy Tắc Quyết Định

```
┌──────────────────────────────┐
│ Bạn cần thêm 1 query đọc?    │
├──────────────┬───────────────┤
│    ĐƠN GIẢN   │   PHỨC TẠP    │
│  (SELECT 1-2  │  (cần check   │
│   bảng, không │   permission, │
│   transform)  │   cache,      │
│               │   validation) │
└──────┬───────┴──────┬────────┘
       │              │
       ▼              ▼
Gọi Repository      Dùng MediatR
trực tiếp           + Query + Handler
(Controller)        (Đầy đủ CQRS)
```

---

## 8. Checklist Khi Tạo File Mới

### 8.1. Checklist Cho Application Layer

```
☐ DTO đã được tạo trong Features/{Feature}/DTOs/?
☐ Field roles là string[] (nếu có roles)?
☐ DTO chỉ chứa field cần thiết cho use case đó?
☐ Query/Command là record?
☐ Handler inject Interface (không inject Implementation)?
☐ Handler trả về Result<T>?
☐ Validator có kiểm tra tất cả input?
☐ Folder structure đúng: Features/{Feature}/{Queries|Commands}/{Action}/
```

### 8.2. Checklist Cho Infrastructure Layer

```
☐ Interface đã tồn tại trong Application layer?
☐ File implement đặt trong Persistence/Dapper/Repositories/?
☐ Dùng IDbConnectionFactory, không new SqlConnection trực tiếp?
☐ SQL là parameterized query (không concatenate string)?
☐ Dùng COALESCE cho UPDATE (nếu là partial update)?
☐ Trả về int cho write operations?
```

### 8.3. Checklist Cho WebApi Layer

```
☐ Controller dùng [ApiController] và [Route("api/[controller]")]?
☐ Inject IMediator (nếu dùng CQRS)?
☐ Inject IUserContext (nếu cần userId)?
☐ [Authorize] đã được thêm đúng policy?
  - [Authorize] → user cần đăng nhập
  - [Authorize(Policy = "RequireAdmin")] → chỉ Admin
  - [AllowAnonymous] → public
☐ Dùng Result.Match(Ok, HandleFailure) pattern?
☐ HTTP status code đúng?
  - GET → 200
  - POST → 201 (Created)
  - PUT/DELETE → 204 (No Content)
  - Lỗi → HandleFailure tự xử lý
```

### 8.4. Checklist Tổng Thể

```
☐ dotnet build không lỗi?
☐ dotnet test không fail?
☐ Không dùng SqlKata?
☐ Không dùng EF Core cho query?
☐ Không dùng AutoMapper/Mapster?
☐ File được tạo đúng vị trí folder?
☐ Namespace đúng convention?
```

---

## 9. Ví dụ Hoàn Chỉnh: Thêm Tính Năng "User Profile Picture"

> **Mục tiêu:** Cho user upload avatar, admin xem tất cả avatar, admin xóa avatar.

### 9.1. Xác Định Read/Write

| Action | Type | Auth |
|--------|------|------|
| Xem avatar của mình | Query | [Authorize] |
| Upload avatar | Command | [Authorize] |
| Xem avatar của user khác (admin) | Query | RequireAdmin |
| Xóa avatar (admin) | Command | RequireAdmin |

### 9.2. Tạo File

**Bước 1: DTOs**

```csharp
// Application/Features/Avatar/DTOs/AvatarResponse.cs
public class AvatarResponse
{
    public Guid UserId { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

// Application/Features/Avatar/DTOs/UploadAvatarRequest.cs
public class UploadAvatarRequest
{
    public IFormFile? File { get; set; }  // IFormFile ở WebApi, không ở Application
}
```

**Bước 2: Queries**

```csharp
// Application/Features/Avatar/Queries/GetMyAvatar/GetMyAvatarQuery.cs
public record GetMyAvatarQuery : IRequest<Result<AvatarResponse>>;

// Application/Features/Avatar/Queries/GetMyAvatar/GetMyAvatarQueryHandler.cs
public class GetMyAvatarQueryHandler : IRequestHandler<GetMyAvatarQuery, Result<AvatarResponse>>
{
    private readonly IUserContext _userContext;
    private readonly IAvatarRepository _repo;

    public GetMyAvatarQueryHandler(IUserContext userContext, IAvatarRepository repo)
    {
        _userContext = userContext;
        _repo = repo;
    }

    public async Task<Result<AvatarResponse>> Handle(GetMyAvatarQuery request, CancellationToken ct)
    {
        var avatar = await _repo.GetAvatarAsync(_userContext.UserId);
        if (avatar is null)
            return Result<AvatarResponse>.NotFound();

        return Result<AvatarResponse>.Success(avatar);
    }
}
```

```csharp
// Application/Features/Avatar/Queries/GetUserAvatarByAdmin/GetUserAvatarByAdminQuery.cs
public record GetUserAvatarByAdminQuery(Guid UserId) : IRequest<Result<AvatarResponse>>;

// Application/Features/Avatar/Queries/GetUserAvatarByAdmin/GetUserAvatarByAdminQueryHandler.cs
public class GetUserAvatarByAdminQueryHandler : IRequestHandler<GetUserAvatarByAdminQuery, Result<AvatarResponse>>
{
    private readonly IAvatarRepository _repo;

    public GetUserAvatarByAdminQueryHandler(IAvatarRepository repo) => _repo = repo;

    public async Task<Result<AvatarResponse>> Handle(GetUserAvatarByAdminQuery request, CancellationToken ct)
    {
        var avatar = await _repo.GetAvatarAsync(request.UserId);
        if (avatar is null)
            return Result<AvatarResponse>.NotFound();

        return Result<AvatarResponse>.Success(avatar);
    }
}
```

**Bước 3: Commands**

```csharp
// Application/Features/Avatar/Commands/UploadAvatar/UploadAvatarCommand.cs
public record UploadAvatarCommand(Guid UserId, string AvatarUrl) : IRequest<Result<AvatarResponse>>;

// Application/Features/Avatar/Commands/UploadAvatar/UploadAvatarCommandValidator.cs
public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AvatarUrl).NotEmpty().MaximumLength(500);
    }
}

// Application/Features/Avatar/Commands/UploadAvatar/UploadAvatarCommandHandler.cs
public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, Result<AvatarResponse>>
{
    private readonly IAvatarRepository _repo;

    public UploadAvatarCommandHandler(IAvatarRepository repo) => _repo = repo;

    public async Task<Result<AvatarResponse>> Handle(UploadAvatarCommand request, CancellationToken ct)
    {
        var avatar = await _repo.UpsertAvatarAsync(request.UserId, request.AvatarUrl);
        return Result<AvatarResponse>.Success(avatar);
    }
}
```

```csharp
// Application/Features/Avatar/Commands/DeleteAvatar/DeleteAvatarCommand.cs
public record DeleteAvatarCommand(Guid UserId) : IRequest<Result<Unit>>;

// Application/Features/Avatar/Commands/DeleteAvatar/DeleteAvatarCommandValidator.cs
public class DeleteAvatarCommandValidator : AbstractValidator<DeleteAvatarCommand>
{
    public DeleteAvatarCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

// Application/Features/Avatar/Commands/DeleteAvatar/DeleteAvatarCommandHandler.cs
public class DeleteAvatarCommandHandler : IRequestHandler<DeleteAvatarCommand, Result<Unit>>
{
    private readonly IAvatarRepository _repo;

    public DeleteAvatarCommandHandler(IAvatarRepository repo) => _repo = repo;

    public async Task<Result<Unit>> Handle(DeleteAvatarCommand request, CancellationToken ct)
    {
        var rows = await _repo.DeleteAvatarAsync(request.UserId);
        if (rows == 0)
            return Result<Unit>.NotFound();

        return Result<Unit>.Success(Unit.Value);
    }
}
```

**Bước 4: Interface**

```csharp
// Application/Abstractions/Interfaces/Repositories/IAvatarRepository.cs
public interface IAvatarRepository
{
    Task<AvatarResponse?> GetAvatarAsync(Guid userId);
    Task<AvatarResponse> UpsertAvatarAsync(Guid userId, string avatarUrl);
    Task<int> DeleteAvatarAsync(Guid userId);
}
```

**Bước 5: Implementation**

```csharp
// Infrastructure/Persistence/Dapper/Repositories/AvatarRepository.cs
public class AvatarRepository : IAvatarRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AvatarRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AvatarResponse?> GetAvatarAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<AvatarResponse>(
            "SELECT UserId, AvatarUrl, UpdatedAt as UploadedAt " +
            "FROM AspNetUsers WHERE Id = @UserId AND AvatarUrl IS NOT NULL",
            new { UserId = userId });
    }

    public async Task<AvatarResponse> UpsertAvatarAsync(Guid userId, string avatarUrl)
    {
        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(
            "UPDATE AspNetUsers SET AvatarUrl = @AvatarUrl, UpdatedAt = GETUTCDATE() " +
            "WHERE Id = @UserId",
            new { UserId = userId, AvatarUrl = avatarUrl });

        return new AvatarResponse
        {
            UserId = userId,
            AvatarUrl = avatarUrl,
            UploadedAt = DateTime.UtcNow
        };
    }

    public async Task<int> DeleteAvatarAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteAsync(
            "UPDATE AspNetUsers SET AvatarUrl = NULL, UpdatedAt = GETUTCDATE() " +
            "WHERE Id = @UserId AND AvatarUrl IS NOT NULL",
            new { UserId = userId });
    }
}
```

**Bước 6: DI**

```csharp
// DependencyInjection.cs
services.AddScoped<IAvatarRepository, AvatarRepository>();
```

**Bước 7: Controller**

```csharp
// WebApi/Controllers/AvatarController.cs
[ApiController]
[Route("api/[controller]")]
public class AvatarController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserContext _userContext;

    public AvatarController(IMediator mediator, IUserContext userContext)
    {
        _mediator = mediator;
        _userContext = userContext;
    }

    // GET /api/avatar/me — User xem avatar của mình
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyAvatar()
    {
        var result = await _mediator.Send(new GetMyAvatarQuery());
        return result.Match(Ok, this.HandleFailure);
    }

    // POST /api/avatar/me — User upload avatar
    [Authorize]
    [HttpPost("me")]
    public async Task<IActionResult> UploadAvatar([FromBody] UploadAvatarRequest request)
    {
        // Upload file ở đây (lưu CDN / local), lấy URL
        var avatarUrl = $"https://cdn.example.com/avatars/{_userContext.UserId}";

        var command = new UploadAvatarCommand(_userContext.UserId, avatarUrl);
        var result = await _mediator.Send(command);

        return result.Match(
            value => CreatedAtAction(nameof(GetMyAvatar), null, value),
            this.HandleFailure
        );
    }

    // GET /api/avatar/{userId} — Admin xem avatar của user khác
    [Authorize(Policy = "RequireAdmin")]
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserAvatar(Guid userId)
    {
        var result = await _mediator.Send(new GetUserAvatarByAdminQuery(userId));
        return result.Match(Ok, this.HandleFailure);
    }

    // DELETE /api/avatar/{userId} — Admin xóa avatar user
    [Authorize(Policy = "RequireAdmin")]
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteAvatar(Guid userId)
    {
        var result = await _mediator.Send(new DeleteAvatarCommand(userId));
        return result.Match(_ => NoContent(), this.HandleFailure);
    }
}
```

### 9.3. File Tree Sau Khi Hoàn Thành

```
AuthApi.Application/
├── Abstractions/Interfaces/Repositories/
│   └── IAvatarRepository.cs              ← Interface mới
└── Features/
    └── Avatar/                             ← Feature mới
        ├── DTOs/
        │   ├── AvatarResponse.cs
        │   └── UploadAvatarRequest.cs
        ├── Queries/
        │   ├── GetMyAvatar/
        │   │   ├── GetMyAvatarQuery.cs
        │   │   └── GetMyAvatarQueryHandler.cs
        │   └── GetUserAvatarByAdmin/
        │       ├── GetUserAvatarByAdminQuery.cs
        │       └── GetUserAvatarByAdminQueryHandler.cs
        └── Commands/
            ├── UploadAvatar/
            │   ├── UploadAvatarCommand.cs
            │   ├── UploadAvatarCommandHandler.cs
            │   └── UploadAvatarCommandValidator.cs
            └── DeleteAvatar/
                ├── DeleteAvatarCommand.cs
                ├── DeleteAvatarCommandHandler.cs
                └── DeleteAvatarCommandValidator.cs

AuthApi.Infrastructure/
├── Persistence/Dapper/Repositories/
│   └── AvatarRepository.cs                ← Implementation mới
└── Configuration/
    └── DependencyInjection.cs             ← Sửa: thêm dòng DI

AuthApi.WebApi/
└── Controllers/
    └── AvatarController.cs                ← Controller mới
```

---

## Phụ Lục: Sơ Đồ Quyết Định Nhanh

```
┌──────────────────────────────────────────────┐
│ Cần thêm tính năng mới                        │
└───────────────────┬──────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────┐
│ Đã có Controller cho entity này chưa?         │
├──────────┬───────────────────┬───────────────┤
│   CHƯA   │        CÓ         │   CÓ (nhưng   │
│          │                  │   action mới   │
│          │                  │   khác hẳn)    │
└────┬─────┴──────┬───────────┴───────┬────────┘
     │            │                   │
     ▼            ▼                   ▼
Tạo Controller  Thêm action        Có thể tách
mới (Loại 3)    vào controller     controller mới
                có sẵn (Loại 2)    nếu quá dài
                                    (Loại 3)
     │            │                   │
     └────────────┼───────────────────┘
                  │
                  ▼
┌──────────────────────────────────────────────┐
│ Tính năng này là Read hay Write?              │
├──────────────────┬───────────────────────────┤
│      READ        │          WRITE             │
└────────┬─────────┴──────────┬────────────────┘
         │                    │
         ▼                    ▼
┌─────────────────┐  ┌─────────────────────────┐
│ Query đơn giản?  │  │ Tạo Command + Validator  │
├──────┬──────────┤  │ + Handler + Interface    │
│  CÓ  │  KHÔNG   │  │ + Implementation         │
│      │          │  └──────────┬──────────────┘
│ Gọi  │ Tạo      │             │
│ Repo │ Query +  │             │
│ trực │ Handler  │             │
│ tiếp │ (Loại 1) │             │
└──────┴──────────┘             │
         │                      │
         └──────────┬───────────┘
                    │
                    ▼
┌──────────────────────────────────────────────┐
│ Đã đăng ký DI?                               │
├──────────────────────────────────────────────┤
│ services.AddScoped<I{Repo}, {Repo}>();       │
└──────────────────────────────────────────────┘
                    │
                    ▼
┌──────────────────────────────────────────────┐
│ dotnet build                                  │
│ dotnet test                                   │
│ ✅ Hoàn thành                                 │
└──────────────────────────────────────────────┘
```
