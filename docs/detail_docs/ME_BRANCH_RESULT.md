# Me Branch — Kết Quả Cuối Cùng

> **Branch:** `feature/TNP-TuanNT-Me`
> **4 Commits:** P1→P2→P3→P4
> **Cập nhật:** 09/06/2026

---

## Mục Lục

1. [Tổng Quan 4 Patches](#1-tổng-quan-4-patches)
2. [Patch 1: IUserContext + v2 Breaking](#2-patch-1-iusercontext--v2-breaking)
3. [Patch 2: Dapper Read Repository](#3-patch-2-dapper-read-repository)
4. [Patch 3: Redis Cache + Policy Auth + Admin Read](#4-patch-3-redis-cache--policy-auth--admin-read)
5. [Patch 4: Write Repository + Admin Commands](#5-patch-4-write-repository--admin-commands)
6. [Flow Chi Tiết Sau Cùng](#6-flow-chi-tiết-sau-cùng)
7. [Kiến Trúc Tổng Thể](#7-kiến-trúc-tổng-thể)

---

## 1. Tổng Quan 4 Patches

```
Commit 1 ──feat(auth): IUserContext + v2 breaking──▶  (P1)
                    │
                    ▼
Commit 2 ──feat(read): Dapper read repository───────▶  (P2)
                    │
                    ▼
Commit 3 ──feat(cache): Redis caching + policy auth──▶  (P3)
                    │
                    ▼
Commit 4 ──feat(write): UserWriteRepository + admin──▶  (P4)
```

Mỗi patch là một tầng kiến trúc riêng biệt, có thể review độc lập.

| Patch | Layer | Tính năng |
|-------|-------|-----------|
| P1 | Security + API | IUserContext thay ICurrentUserService; Role→Roles[] (breaking) |
| P2 | Data Access | Dapper Read thay SqlKata; xóa code chết |
| P3 | Performance + Auth | Redis cache; Policy Authorization; Admin read endpoint |
| P4 | Write Side | Dapper Write Repository; Admin commands (update, roles, lockout) |

---

## 2. Patch 1: IUserContext + v2 Breaking

### 2.1. Vấn Đề: ICurrentUserService

**File cũ:** `AuthApi.Application/Abstractions/Interfaces/Auth/ICurrentUserService.cs`

```csharp
// ❌ TRƯỚC ĐÂY (đã xóa)
public interface ICurrentUserService
{
    string? UserId { get; }    // string? — nullable, không type-safe
    bool IsAuthenticated { get; }
    string? IpAddress { get; }  // chi tiết HttpContext — không phải ai cũng cần
}
```

**Vấn đề với ICurrentUserService:**

| Vấn đề | Giải thích |
|---------|------------|
| **`UserId` là `string?`** | Phải `Guid.Parse()` mỗi lần dùng → dễ lỗi runtime, không type-safe |
| **Property cứng (`IpAddress`)** | Không phải use case nào cũng cần IP → vi phạm Interface Segregation Principle |
| **Không test được** | Gắn chặt với `HttpContext` — không thể mock trong unit test |
| **Không dùng cho background job** | Background job không có HTTP request → không có HttpContext → crash |
| **Không có role info** | Phải gọi DB để lấy roles mỗi lần → N+1 query |

### 2.2. Giải Pháp: IUserContext

**File mới:** `AuthApi.Application/Common/Security/IUserContext.cs`

```csharp
// ✅ SAU NÀY (tạo mới)
public interface IUserContext
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }          // Guid — type-safe, không null
    string? UserName { get; }     // Chỉ những gì cần thiết
    IReadOnlyList<string> Roles { get; }  // Roles — không phải Role
    bool IsInRole(string role);           // Helper method
}
```

**Tại sao thay đổi:**

```
ICurrentUserService                    IUserContext
┌─────────────────┐                    ┌─────────────────────┐
│ UserId: string?  │  → KHÔNG dùng được  │ UserId: Guid        │  → Parse 1 lần
│ IpAddress: string│  → Chỉ HTTP cần    │ UserName: string?   │  → Chỉ cần username
│ IsAuthenticated  │  → OK              │ Roles: string[]     │  → Có sẵn không cần query
└─────────────────┘                    └─────────────────────┘
       ↓                                        ↓
  HttpContext bên trong                   Pure abstraction
  Không mock được                         Mock dễ dàng
```

### 2.3. 2 Implementations của IUserContext

```csharp
// AuthApi.Infrastructure/Services/Auth/HttpUserContext.cs
// ✅ Dùng cho Request HTTP bình thường
public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid UserId
    {
        get
        {
            // Parse từ JWT claim "sub" hoặc "nameidentifier"
            var claim = _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return claim is not null ? Guid.Parse(claim) : Guid.Empty;
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            // Lấy roles từ JWT claims (ClaimTypes.Role)
            return _accessor.HttpContext?.User
                .FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly() ?? [];
        }
    }

    public bool IsAuthenticated =>
        _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public string? UserName =>
        _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
```

```csharp
// AuthApi.Infrastructure/Services/Auth/UserContext.cs
// ✅ Dùng cho Unit Test hoặc Background Job
public class UserContext : IUserContext
{
    public static readonly IUserContext System = new UserContext
    {
        IsAuthenticated = true,
        UserId = Guid.Empty,      // System account
        UserName = "System",
        Roles = ["System"]
    };

    private UserContext() { }  // Chỉ tạo qua static hoặc factory

    // Properties được set từ bên ngoài (test hoặc background)
    public bool IsAuthenticated { get; private set; }
    public Guid UserId { get; private set; }
    public string? UserName { get; private set; }
    public IReadOnlyList<string> Roles { get; private set; } = [];
    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public static UserContext Create(Guid userId, string? userName = null, params string[] roles)
    {
        return new UserContext
        {
            IsAuthenticated = true,
            UserId = userId,
            UserName = userName,
            Roles = roles.ToList().AsReadOnly()
        };
    }
}
```

**So sánh trước-sau (IUserContext):**

| Tiêu chí | Trước (ICurrentUserService) | Sau (IUserContext) |
|----------|-----------------------------|---------------------|
| **UserId type** | `string?` → phải parse | `Guid` → sẵn sàng dùng |
| **Roles** | Không có → phải query DB | `IReadOnlyList<string>` có sẵn từ JWT |
| **Test** | Không thể mock | Mock được qua interface |
| **Background job** | Crash vì không có HttpContext | Dùng `UserContext.System` |
| **Interface** | Quá nhiều thứ (IP, UserAgent...) | Đúng những gì cần |
| **Security** | Nullable → dễ null-ref | Guid → luôn có giá trị (hoặc Empty) |

### 2.4. V2 Breaking: Role → Roles[]

**Tại sao phải breaking?**

Vì bảng `AspNetUserRoles` là **Many-to-Many**:
```
AspNetUsers ───┐
               ├── AspNetUserRoles ─── AspNetRoles
IdentityUser  ───┘
```

Một user có thể có nhiều role (vd: vừa "User" vừa "Admin").

**Trước đây (string — sai):**
```json
{
  "id": "guid",
  "email": "user@test.com",
  "role": "Admin"       // ❌ Chỉ lấy được 1 role — mất dữ liệu
}
```

**Sau này (string[] — đúng):**
```json
{
  "id": "guid",
  "email": "user@test.com",
  "roles": ["User", "Admin"]  // ✅ Lấy được tất cả roles
}
```

**Các file bị ảnh hưởng:**
- `AuthUserDto.cs` — field `role` → `roles`
- `LoginResponse.cs` — field `role` → `roles`
- `MeResponse.cs` — field `role` → `roles`
- `TokenService.cs` — JWT claims: `ClaimTypes.Role` (multiple claims)
- `RegisterCommandHandler.cs` — gán roles mảng

---

## 3. Patch 2: Dapper Read Repository

### 3.1. Vấn Đề: SqlKata QueryFactory

**Trước đây** — code dùng SqlKata:

```csharp
// ❌ TRƯỚC: AuthApi.Infrastructure/Persistence/Repositories/Users/UserQueryRepository.cs
public class UserQueryRepository : IUserQueryRepository
{
    private readonly QueryFactory _db;

    public UserQueryRepository(QueryFactory db)
    {
        _db = db;  // SqlKata QueryFactory
    }

    public async Task<UserDto> GetMeAsync(string userId)
    {
        var query = _db.Query("AspNetUsers")
            .Where("Id", userId)
            .Select("Id", "Email", "UserName");

        var user = await query.FirstOrDefaultAsync<UserDto>();

        // Query roles riêng
        if (user != null)
        {
            var roles = await _db.Query("AspNetUserRoles")
                .Join("AspNetRoles", "AspNetRoles.Id", "AspNetUserRoles.RoleId")
                .Where("AspNetUserRoles.UserId", userId)
                .Select("AspNetRoles.Name")
                .GetAsync<string>();
            user.Role = roles.FirstOrDefault() ?? "User";  // ❌ Chỉ lấy role đầu tiên!
        }

        return user;
    }
}
```

**Vấn đề với SqlKata:**

| Vấn đề | Mô tả |
|---------|-------|
| **Overhead** | SqlKata build cây AST → generate SQL → parse kết quả. Dapper chỉ Execute + Map |
| **Không debug được SQL** | Không thấy câu SQL thực tế — khó optimize |
| **Magic strings** `"AspNetUsers"`, `"Id"` | Không compile-time checking |
| **Không support RAW SQL tốt** | Phải dùng `.SelectRaw()` — mất lợi thế abstraction |
| **Dependency thứ 3** | Một package nữa phải maintain, update |
| **Performance** | SqlKata chậm hơn Dapper ~20-30% cho query đơn giản |
| **Không dùng được stored procedure** | SqlKata không hỗ trợ tốt |

### 3.2. Giải Pháp: Dapper + IDbConnectionFactory

```csharp
// ✅ SAU: AuthApi.Infrastructure/Persistence/Dapper/Repositories/UserReadRepository.cs
public class UserReadRepository : IUserReadRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserReadRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MeResponse?> GetMeAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Query 1: Lấy thông tin user
        var user = await connection.QueryFirstOrDefaultAsync<MeResponse>(
            "SELECT Id, Email, UserName, FirstName, LastName, PhoneNumber, DateOfBirth, AvatarUrl, CreatedAt, UpdatedAt " +
            "FROM AspNetUsers WHERE Id = @UserId",
            new { UserId = userId });

        if (user == null) return null;

        // Query 2: Lấy roles (batch — 1 query cho tất cả roles)
        var roles = await connection.QueryAsync<string>(
            "SELECT r.Name FROM AspNetRoles r " +
            "INNER JOIN AspNetUserRoles ur ON ur.RoleId = r.Id " +
            "WHERE ur.UserId = @UserId",
            new { UserId = userId });

        user.Roles = roles.ToList();

        return user;
    }

    public async Task<UserDetailResponse?> GetUserDetailAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var user = await connection.QueryFirstOrDefaultAsync<UserDetailResponse>(
            "SELECT Id, Email, UserName, FirstName, LastName, PhoneNumber, " +
            "DateOfBirth, AvatarUrl, CreatedAt, UpdatedAt, " +
            "LockoutEnabled, LockoutEnd, AccessFailedCount, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled " +
            "FROM AspNetUsers WHERE Id = @UserId",
            new { UserId = userId });

        if (user == null) return null;

        // Tính IsLockedOut
        user.IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        // Load roles
        var roles = await connection.QueryAsync<string>(
            "SELECT r.Name FROM AspNetRoles r " +
            "INNER JOIN AspNetUserRoles ur ON ur.RoleId = r.Id " +
            "WHERE ur.UserId = @UserId",
            new { UserId = userId });

        user.Roles = roles.ToList();

        return user;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetAllUsersAsync()
    {
        using var connection = _connectionFactory.CreateConnection();

        var users = (await connection.QueryAsync<UserListItemDto>(
            "SELECT Id, Email, UserName, CreatedAt FROM AspNetUsers " +
            "ORDER BY CreatedAt DESC"))
            .ToList();

        if (!users.Any()) return [];

        // Batch load tất cả roles cho tất cả users — 1 query duy nhất
        var userIds = users.Select(u => u.Id).ToList();
        var roleMappings = await connection.QueryAsync(
            "SELECT ur.UserId, r.Name as RoleName " +
            "FROM AspNetUserRoles ur " +
            "INNER JOIN AspNetRoles r ON r.Id = ur.RoleId " +
            "WHERE ur.UserId IN @UserIds",
            new { UserIds = userIds });

        // Group roles vào từng user
        var lookup = roleMappings.GroupBy(x => (Guid)x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => (string)x.RoleName).ToList());

        foreach (var user in users)
        {
            if (lookup.TryGetValue(user.Id, out var roles))
                user.Roles = roles;
        }

        return users.AsReadOnly();
    }
}
```

**Tại sao 2 queries thay vì 1 JOIN + STRING_AGG?**

```
Cách 1: 1 query với STRING_AGG (cách cũ)
────────────────────────────────────
SELECT u.Id, u.Email, STRING_AGG(r.Name, ',') as RolesString
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON ur.UserId = u.Id
LEFT JOIN AspNetRoles r ON r.Id = ur.RoleId
WHERE u.Id = @UserId
GROUP BY u.Id, u.Email
→ Sau đó phải Split(',') ở C# — dễ sai nếu role name có dấu phẩy

Cách 2: 2 queries riêng (cách mới)
──────────────────────────────────
Query 1: SELECT column1, column2... FROM AspNetUsers WHERE Id = @UserId
Query 2: SELECT r.Name FROM AspNetRoles r
         JOIN AspNetUserRoles ur ON ur.RoleId = r.Id WHERE ur.UserId = @UserId
→ Không cần Split(), dễ đọc, dễ bảo trì
→ 2 queries vẫn là 1 round-trip (cùng connection)
→ Dễ mở rộng (thêm role info, permission...)
```

### 3.3. IDbConnectionFactory

```csharp
// AuthApi.Infrastructure/Persistence/Dapper/DbConnectionFactory.cs
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
```

**Tại sao cần factory, không new SqlConnection trực tiếp?**

| Lý do | Giải thích |
|-------|------------|
| **DI-friendly** | Đăng ký 1 lần, inject khắp nơi |
| **Thay đổi connection string** | Chỉ cần sửa 1 chỗ |
| **Unit Test** | Mock IDbConnectionFactory → không cần DB thật |
| **Connection Pooling** | Factory không ảnh hưởng, ADO.NET pool tự quản lý |

### 3.4. Xóa Code Chết

| File cũ | Lý do xóa |
|---------|-----------|
| `Domain/Interfaces/IUserRepository.cs` | Domain layer không nên chứa interface cho infrastructure |
| `Infrastructure/Persistence/Repositories/Users/UserRepository.cs` | EF Core stub — chưa implement đầy đủ, không dùng được |
| `Infrastructure/Persistence/Repositories/Users/UserQueryRepository.cs` | SqlKata → thay bằng Dapper UserReadRepository |
| `Application/Abstractions/Interfaces/Repositories/IUserQueryRepository.cs` | Thay bằng IUserReadRepository (return type khác, Guid thay string) |
| `Application/Features/Users/DTOs/UserDto.cs` | Thay bằng UserListItemDto (sửa Role→Roles[]) |

**So sánh trước-sau (Data Access):**

| Tiêu chí | Trước (SqlKata) | Sau (Dapper) |
|----------|------------------|--------------|
| **Query style** | Builder pattern (AST) | Raw SQL |
| **Performance** | ~20-30% overhead | Gần như raw ADO.NET |
| **Debug SQL** | Không xem được SQL | Copy-paste SQL vào SSMS |
| **Parameters** | Tự động (? → @p0) | Tường minh (@param) |
| **Stored Proc** | Không hỗ trợ | Hỗ trợ đầy đủ |
| **Complex query** | JOIN phức tạp → khó đọc | SQL thuần → dễ đọc |
| **Packages** | SqlKata 4.0.1 + SqlKata.Execution | Dapper 2.x |
| **Code chết** | 5 files dead code | 🗑️ Đã xóa |

---

## 4. Patch 3: Redis Cache + Policy Auth + Admin Read

### 4.1. Vấn Đề: Không Có Cache cho /me

**Tại sao /me cần cache?**

```
Không cache:
────────────
Client → /me → Controller → MediatR → Dapper → SQL Server → Dữ liệu
            ↑                                          ↓
            └────────── (mỗi request) ──────────────────┘

Có cache:
─────────
Client → /me → Controller → MediatR → Redis ──(HIT)──→ Dữ liệu (nhanh!)
                                       │
                                    (MISS)
                                       │
                                       ▼
                                   Dapper → SQL Server → Dữ liệu → Ghi vào Redis
```

**Luồng request của /me:**
- User tải trang → gọi `/api/users/me` để lấy thông tin
- Mỗi lần F5, chuyển trang... đều gọi lại
- Dữ liệu user profile ít thay đổi (tên, email...)
- Cache giảm tải DB từ 100% xuống ~5% (nếu cache 5 phút)

### 4.2. Giải Pháp: ICacheService + Redis

```csharp
// AuthApi.Application/Abstractions/Cache/ICacheService.cs
public interface ICacheService
{
    // Generic: T có thể là bất kỳ DTO nào
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
}
```

```csharp
// AuthApi.Infrastructure/Services/Cache/RedisCacheService.cs
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var bytes = await _cache.GetAsync(key);
            if (bytes == null) return null;

            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            // Graceful degradation: Redis lỗi → vẫn query DB
            _logger.LogWarning(ex, "Redis cache get failed for key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var bytes = Encoding.UTF8.GetBytes(json);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry
            };

            await _cache.SetAsync(key, bytes, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache set failed for key {Key}", key);
            // Không throw — cache failure không block response
        }
    }

    public async Task RemoveAsync(string key)
    {
        try { await _cache.RemoveAsync(key); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis cache remove failed"); }
    }
}
```

**Graceful Degradation:**

```
         Redis OK                         Redis FAIL
    ┌──────────────┐                 ┌──────────────┐
    │ Trả về cache  │                 │ Log warning   │
    │ nhanh (1-2ms) │                 │ return null   │
    └──────┬───────┘                 └──────┬───────┘
           │                                │
           │                                ▼
           │                    Query DB bình thường
           │                    (như không có cache)
           │                                │
           │                                ▼
           │                    Trả về response OK
           │                    (chỉ chậm hơn 20-30ms)
           │
    Nếu Redis hồi phục:
    Cache MISS → Query DB → Ghi cache → Lần sau HIT
```

**Cache Key Pattern:**

```
"cache:me:{userId}"
      │      │
      │      └── Guid của user (riêng biệt cho từng user)
      │
      └── Prefix "cache:" — dễ identify trong Redis CLI
          "me" — loại cache
```

### 4.3. Cache Trong MeQueryHandler

```csharp
// AuthApi.Application/Features/Users/Queries/Me/MeQueryHandler.cs
public class MeQueryHandler : IRequestHandler<MeQuery, Result<MeResponse>>
{
    private readonly IUserContext _userContext;
    private readonly IUserReadRepository _readRepository;
    private readonly ICacheService _cache;

    // Cache key pattern: "cache:me:{userId}"
    private static string CacheKey(Guid userId) => $"cache:me:{userId}";

    public async Task<Result<MeResponse>> Handle(MeQuery request, CancellationToken ct)
    {
        // 1. Lấy userId từ context
        var userId = _userContext.UserId;
        if (userId == Guid.Empty)
            return Result<MeResponse>.Unauthorized();

        // 2. Thử cache
        var cached = await _cache.GetAsync<MeResponse>(CacheKey(userId));
        if (cached is not null)
            return Result<MeResponse>.Success(cached);

        // 3. Cache MISS → query DB
        var user = await _readRepository.GetMeAsync(userId);
        if (user is null)
            return Result<MeResponse>.NotFound();

        // 4. Ghi cache (fire-and-forget — không await)
        _ = _cache.SetAsync(CacheKey(userId), user, TimeSpan.FromMinutes(5));

        return Result<MeResponse>.Success(user);
    }
}
```

**Flow cache trong Me:**

```
MeQueryHandler.Handle()
│
├─ Step 1: IUserContext.UserId
│   → Lấy userId từ JWT
│
├─ Step 2: ICacheService.GetAsync("cache:me:{userId}")
│   │
│   ├─ [HIT]  → Deserialize JSON → trả về MeResponse
│   │          → KHÔNG chạm DB
│   │          → Thời gian: ~1-2ms
│   │
│   └─ [MISS] → cache null hoặc cache expired
│              → Tiếp tục Step 3
│              → Thời gian: ~1-2ms (phí check cache)
│
├─ Step 3: IUserReadRepository.GetMeAsync(userId) ← Dapper
│   │        → 2 queries (user info + roles)
│   │        → Thời gian: ~20-30ms
│   │
│   └─ [null] → return NotFound (404)
│
├─ Step 4: ICacheService.SetAsync("cache:me:{userId}", response, 5 phút)
│   │        → Serialize JSON → Redis SET
│   │        → Fire-and-forget (không await) — không block response
│   │        → Nếu Redis lỗi → log warning, response vẫn trả về
│
└─ Return Result<MeResponse>.Success(user)
```

### 4.4. Policy-Based Authorization

**Trước đây:**
```csharp
// ❌ Check role thủ công trong controller
[Authorize]
public async Task<IActionResult> AdminOnly()
{
    if (!User.IsInRole("Admin"))
        return Forbid();  // Hoặc quên check → lỗ hổng bảo mật
    // ... logic
}
```

**Vấn đề:**
- Dễ quên check — developer phải nhớ
- Không centralized — mỗi nơi check khác nhau
- Không test được policy riêng

**Sau này:**
```csharp
// ✅ Policy-based — khai báo 1 lần, dùng khắp nơi
// Program.cs
builder.Services.AddAuthorization(options =>
{
    // Admin-only: quản lý user, roles...
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin"));

    // User-only: thao tác cá nhân
    options.AddPolicy("RequireUser", policy =>
        policy.RequireRole("User"));

    // Hoặc kết hợp
    options.AddPolicy("RequireAdminOrUser", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("Admin") || ctx.User.IsInRole("User")));
});

// Controller — chỉ cần [Authorize(Policy = "...")]
[Authorize(Policy = "RequireAdmin")]
public async Task<IActionResult> GetUserById(Guid id)
{
    // Không cần check role — Policy đã làm
    // Nếu không phải Admin → tự động 403
}
```

**So sánh:**

| Tiêu chí | Check thủ công | Policy-based |
|----------|---------------|--------------|
| **Nơi khai báo** | Rải rác trong controller | Tập trung ở Program.cs |
| **Quên check** | Dễ xảy ra | Không thể quên (attribute) |
| **Thay đổi logic** | Sửa từng controller | Sửa 1 chỗ |
| **403 tự động** | Phải gọi Forbid() | Tự động trả 403 |
| **Unit Test policy** | Khó | Dễ (dùng AuthorizationHandlerContext) |
| **Kết hợp policy** | Phải tự viết && || | Built-in (RequireAssertion) |

### 4.5. Admin Read Endpoint

**Trước patch 3:** Không có endpoint nào cho admin xem user detail.

**Sau patch 3:** 2 endpoints mới:

| Endpoint | Mô tả | Auth |
|----------|-------|------|
| `GET /api/users/{id}` | Xem detail user (có lockout info) | RequireAdmin |
| `GET /api/users` | List all users (có roles) | AllowAnonymous (admin check trong handler) |

**UserController hoàn chỉnh sau P3:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserReadRepository _userRepo;

    public UserController(IMediator mediator, IUserReadRepository userRepo)
    {
        _mediator = mediator;
        _userRepo = userRepo;
    }

    // GET /api/users/me — User xem profile (có cache)
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await _mediator.Send(new MeQuery());
        return result.Match(Ok, this.HandleFailure);
    }

    // GET /api/users/{id} — Admin xem detail (không cache)
    [Authorize(Policy = "RequireAdmin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        return result.Match(Ok, this.HandleFailure);
    }

    // GET /api/users — List all users (không qua MediatR)
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userRepo.GetAllUsersAsync();
        return Ok(users);
    }
}
```

---

## 5. Patch 4: Write Repository + Admin Commands

### 5.1. Vấn Đề: Không Có Write Side

**Trước patch 4:** Hệ thống chỉ đọc được (Read). Các thao tác viết:

| Thao tác | Trạng thái | Giải pháp cũ |
|----------|------------|--------------|
| Đăng ký user | ✅ Có (Identity Register) | Dùng Identity Service |
| Login/Logout | ✅ Có (Identity) | Dùng Identity Service + Token Service |
| Admin update user info | ❌ Không có | Chưa implement |
| Admin assign roles | ❌ Không có | Chưa implement |
| Admin remove roles | ❌ Không có | Chưa implement |
| Admin lockout user | ❌ Không có | Chưa implement |
| Soft delete user | ❌ Không có | Chưa implement |

**Tại sao cần write repository mà không dùng Identity UserManager?**

```
Identity UserManager
┌─────────────────────────────┐
│ UserManager.FindByEmailAsync│ → Dùng EF Core internally
│ UserManager.AddToRoleAsync  │ → EF Core (Microsoft.AspNetCore.Identity.EntityFrameworkCore)
│ UserManager.SetLockoutAsync │ → Không dùng được với Dapper
│ ...                         │
└─────────────────────────────┘
        │
        ▼
Dự án dùng Dapper → UserManager không dùng được
        │
        ▼
Phải tự viết SQL → IUserWriteRepository
        │
        ▼
┌─────────────────────────────┐
│ UserWriteRepository         │
│ .UpdateUserAsync(id, dto)   │ → UPDATE AspNetUsers SET ... WHERE Id = @Id
│ .AssignRolesAsync(id, roles)│ → INSERT INTO AspNetUserRoles ...
│ .RemoveRolesAsync(id, roles)│ → DELETE FROM AspNetUserRoles ...
│ .SoftDeleteUserAsync(id)    │ → UPDATE LockoutEnd = '9999-12-31'
└─────────────────────────────┘
```

### 5.2. Từng Command Chi Tiết

#### 5.2.1. UpdateUserCommand

**Tại sao:** Admin cần sửa thông tin user (tên, số điện thoại, ...)

**Request:**
```json
PUT /api/users/{id}
{
  "firstName": "Nguyen",
  "lastName": "Van A",
  "phoneNumber": "0123456789",
  "dateOfBirth": "1990-01-01"
}
```

**Handler flow:**

```
UpdateUserCommandHandler
│
├─ 1. Validate: IValidator<UpdateUserCommand>
│   ├─ Id != Guid.Empty
│   ├─ FirstName không quá 50 ký tự (nếu có)
│   └─ PhoneNumber hợp lệ (nếu có)
│
├─ 2. IUserWriteRepository.UpdateUserAsync(id, dto)
│   │
│   ├─ SQL:
│   │   UPDATE AspNetUsers
│   │   SET
│   │       FirstName = COALESCE(@FirstName, FirstName),
│   │       LastName = COALESCE(@LastName, LastName),
│   │       PhoneNumber = COALESCE(@PhoneNumber, PhoneNumber),
│   │       DateOfBirth = COALESCE(@DateOfBirth, DateOfBirth),
│   │       UpdatedAt = GETUTCDATE()
│   │   WHERE Id = @UserId
│   │
│   ├─ rows = 0 → throw NotFoundException → Controller → 404
│   └─ rows > 0 → return Unit
│
└─ Controller → 204 No Content
```

**Tại sao dùng COALESCE?**
- Cho phép update từng field riêng lẻ
- Không cần load user trước, so sánh, rồi mới update
- 1 query duy nhất cho tất cả trường hợp

#### 5.2.2. AssignRolesCommand

**Tại sao:** Admin cần gán quyền cho user (vd: thăng user lên admin)

**Request:**
```json
POST /api/users/{id}/roles
["Admin", "Manager"]
```

**Handler flow:**

```
AssignRolesCommandHandler
│
├─ 1. Validate:
│   ├─ Id != Guid.Empty
│   ├─ Roles không empty
│   └─ Roles chỉ chứa role có trong hệ thống
│
├─ 2. IUserWriteRepository.AssignRolesAsync(id, roles)
│   │
│   ├─ SQL:
│   │   INSERT INTO AspNetUserRoles (UserId, RoleId)
│   │   SELECT @UserId, r.Id
│   │   FROM AspNetRoles r
│   │   WHERE r.Name IN @Roles
│   │     AND NOT EXISTS (
│   │         SELECT 1
│   │         FROM AspNetUserRoles ur
│   │         WHERE ur.UserId = @UserId AND ur.RoleId = r.Id
│   │     )
│   │
│   ├─ NOT EXISTS → tránh duplicate (nếu user đã có role "Admin")
│   └─ Return số role đã insert
│
└─ Controller → 204 No Content
```

**Tại sao có NOT EXISTS?**
- User gọi API nhiều lần với cùng roles → không bị lỗi duplicate key
- Idempotent: gọi 1 lần hay 10 lần đều cho kết quả giống nhau

#### 5.2.3. RemoveRolesCommand

**Tại sao:** Admin cần thu hồi quyền (vd: hạ admin xuống user)

**Request:**
```json
DELETE /api/users/{id}/roles
["Admin"]
```

**Handler flow:**

```
RemoveRolesCommandHandler
│
├─ 1. Validate: Id != Guid.Empty, Roles không empty
│
├─ 2. IUserWriteRepository.RemoveRolesAsync(id, roles)
│   │
│   ├─ SQL:
│   │   DELETE ur
│   │   FROM AspNetUserRoles ur
│   │   INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
│   │   WHERE ur.UserId = @UserId AND r.Name IN @Roles
│   │
│   └─ Chỉ xóa những role được chỉ định
│      → Nếu user không có role đó → không sao (rows = 0)
│
└─ Controller → 204 No Content
```

#### 5.2.4. SoftDeleteUser / Lockout

**Tại sao không xóa hẳn user?**
- Giữ lại dữ liệu lịch sử (orders, activities...)
- Có thể restore nếu cần
- Identity không khuyến khích xóa

**Request:**
```json
DELETE /api/users/{id}
  (không cần body)
```

```
SoftDeleteUserAsync(id)
│
├─ SQL:
│   UPDATE AspNetUsers
│   SET
│       LockoutEnabled = 1,
│       LockoutEnd = '9999-12-31 23:59:59'  -- DateTimeOffset.MaxValue
│   WHERE Id = @UserId
│
├─ rows = 0 → 404 NotFound
│   (user không tồn tại)
│
└─ rows > 0 → 204 No Content
   (user bị lock vĩnh viễn)
```

**Hậu quả của lockout:**
- User không thể login (Identity check LockoutEnd)
- API trả về lỗi khi login:
  ```json
  {
    "error": "Tài khoản đã bị khóa",
    "lockoutEnd": "9999-12-31T23:59:59"
  }
  ```
- User vẫn tồn tại trong DB — không ảnh hưởng đến dữ liệu liên quan

### 5.3. IUserWriteRepository Interface

```csharp
// AuthApi.Application/Abstractions/Repositories/IUserWriteRepository.cs
public interface IUserWriteRepository
{
    Task<int> UpdateUserAsync(Guid userId, UpdateUserDto dto);
    Task<int> AssignRolesAsync(Guid userId, IReadOnlyList<string> roles);
    Task<int> RemoveRolesAsync(Guid userId, IReadOnlyList<string> roles);
    Task<int> SoftDeleteUserAsync(Guid userId);
}
```

**Tại sao return int (số rows affected)?**
- rows = 0 → user không tồn tại → 404
- rows > 0 → thành công → 204
- Không cần check `SELECT COUNT(*)` trước → 1 query thay vì 2

---

## 6. Flow Chi Tiết Sau Cùng

### 6.1. Authentication Flow (Login)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          LOGIN FLOW                                      │
│                                                                          │
│  Client                          AuthApi                              DB │
│    │                                │                                  │
│    │  POST /api/auth/login          │                                  │
│    │  {email, password}            │                                  │
│    ├──────────────────────────────▶│                                  │
│    │                                │                                  │
│    │                                │  FindByEmailAsync(email)          │
│    │                                ├─────────────────────────────────▶│
│    │                                │◀─────────────────────────────────│
│    │                                │                                  │
│    │                                │  CheckLockoutAsync(user)          │
│    │                                ├─────────────────────────────────▶│
│    │                                │◀─────────────────────────────────│
│    │                                │                                  │
│    │                                │  CheckPasswordAsync(user, pass)   │
│    │                                ├─────────────────────────────────▶│
│    │                                │◀─────────────────────────────────│
│    │                                │                                  │
│    │     [Nếu sai password]         │  AccessFailedCount++              │
│    │     ← 401 Unauthorized        ├─────────────────────────────────▶│
│    │                                │  [Nếu >= 5 lần] → Lockout        │
│    │                                │                                  │
│    │     [Nếu đúng password]        │  ResetAccessFailedCountAsync     │
│    │                                ├─────────────────────────────────▶│
│    │                                │                                  │
│    │                                │  GetRolesAsync(user)             │
│    │                                │  → ["User", "Admin"]            │
│    │                                │                                  │
│    │                                │  GenerateTokensAsync(user, roles)│
│    │                                │  ├─ JWT: sub, email, role[]      │
│    │                                │  ├─ RefreshToken: 64-byte random │
│    │                                │  └─ Cookies: refreshToken (http) │
│    │                                │                                  │
│    │  ← 200 OK                     │                                  │
│    │  {                             │                                  │
│    │    accessToken: "jwt...",      │                                  │
│    │    roles: ["User","Admin"],    │                                  │
│    │    email: "user@test.com"      │                                  │
│    │  }                             │                                  │
│    │  Set-Cookie: refreshToken=...   │                                  │
│    │  Set-Cookie: CSRF-TOKEN=...    │                                  │
│    │◀──────────────────────────────│                                  │
└─────────────────────────────────────────────────────────────────────────┘
```

**Vai trò IUserContext trong Login:**
- `LoginCommandHandler` inject `IUserContext`
- Dùng `IUserContext.IsAuthenticated` để kiểm tra nếu user đã login → không cho login lại
- `HttpUserContext` lấy thông tin từ HttpContext (JWT đã parse)

### 6.2. Get Me Flow (Có Cache)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          GET /api/users/me FLOW                          │
│                                                                          │
│  Client                          AuthApi              Redis          DB │
│    │                                │                  │              │
│    │  GET /api/users/me            │                  │              │
│    │  Authorization: Bearer JWT   │                  │              │
│    ├──────────────────────────────▶│                  │              │
│    │                                │                  │              │
│    │                                │  JWT Middleware  │              │
│    │                                │  Parse token     │              │
│    │                                │  → ClaimsPrincipal              │
│    │                                │                  │              │
│    │                                │  [Authorize]     │              │
│    │                                │  → User.Identity.IsAuthenticated │
│    │                                │                  │              │
│    │                                │  Controller.Me()│              │
│    │                                │  → MeQuery      │              │
│    │                                │                  │              │
│    │                                │  MeQueryHandler │              │
│    │                                │                  │              │
│    │                                │  1. IUserContext │              │
│    │                                │  → userId: Guid │              │
│    │                                │  → roles: []    │              │
│    │                                │                  │              │
│    │                                │  2. Check Cache  │              │
│    │                                ├─────────────────▶│              │
│    │                                │                  │              │
│    │     ┌─── [CACHE HIT] ─────────│◀─────────────────│              │
│    │     │   ← 200 OK (cached)     │                  │              │
│    │     │   ~1-2ms                │                  │              │
│    │     │                         │                  │              │
│    │     └─── [CACHE MISS] ────────│                  │              │
│    │                                │                  │              │
│    │                                │  3. Query DB     │              │
│    │                                ├──────────────────────────────▶│
│    │                                │  Query 1: UserInfo             │
│    │                                │◀──────────────────────────────│
│    │                                │  Query 2: Roles                │
│    │                                ├──────────────────────────────▶│
│    │                                │◀──────────────────────────────│
│    │                                │                  │              │
│    │                                │  4. Set Cache    │              │
│    │                                ├─────────────────▶│              │
│    │                                │  TTL: 5 phút    │              │
│    │                                │                  │              │
│    │  ← 200 OK                     │                  │              │
│    │  {                             │                  │              │
│    │    id: "guid",                │                  │              │
│    │    email: "user@test.com",    │                  │              │
│    │    roles: ["User","Admin"],   │                  │              │
│    │    firstName: "Nguyen",       │                  │              │
│    │    lastName: "Van A"          │                  │              │
│    │  }                            │                  │              │
│    │◀──────────────────────────────│                  │              │
└─────────────────────────────────────────────────────────────────────────┘
```

### 6.3. Admin Get User Detail Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        GET /api/users/{id} FLOW                          │
│                                                                          │
│  Client (Admin)                     AuthApi                           DB │
│    │                                   │                               │
│    │  GET /api/users/123-456          │                               │
│    │  Authorization: Bearer (Admin)  │                               │
│    ├─────────────────────────────────▶│                               │
│    │                                   │                               │
│    │                                   │  JWT Middleware               │
│    │                                   │  → ClaimsPrincipal           │
│    │                                   │  Role = "Admin"              │
│    │                                   │                               │
│    │                                   │  [Authorize(Policy="RequireAdmin")]│
│    │                                   │  → User.IsInRole("Admin")    │
│    │                                   │  → Nếu không → 403 Forbidden │
│    │                                   │                               │
│    │                                   │  Controller.GetUserById(id)  │
│    │                                   │  → GetUserByIdQuery(id)      │
│    │                                   │                               │
│    │                                   │  GetUserByIdQueryHandler     │
│    │                                   │  │                            │
│    │                                   │  1. IUserContext             │
│    │                                   │  → double-check Admin role   │
│    │                                   │                               │
│    │                                   │  2. Query DB                 │
│    │                                   ├──────────────────────────────▶│
│    │                                   │  SELECT ... + Lockout info   │
│    │                                   │◀──────────────────────────────│
│    │                                   │                               │
│    │                                   │  3. Query Roles              │
│    │                                   ├──────────────────────────────▶│
│    │                                   │◀──────────────────────────────│
│    │                                   │                               │
│    │                                   │  4. Tính IsLockedOut         │
│    │                                   │  = LockoutEnd > UtcNow       │
│    │                                   │                               │
│    │  ← 200 OK                        │                               │
│    │  {                                │                               │
│    │    id: "123-456",                │                               │
│    │    email: "user@test.com",       │                               │
│    │    roles: ["User"],              │                               │
│    │    isLockedOut: true,            │                               │
│    │    lockoutEnd: "9999-12-31..."   │                               │
│    │  }                                │                               │
│    │◀─────────────────────────────────│                               │
│    │                                   │                               │
│    │  [Nếu không tồn tại]             │                               │
│    │  ← 404 NotFound                  │                               │
│    │  { "error": "User not found" }   │                               │
└──────────────────────────────────────────────────────────────────────────┘
```

### 6.4. Admin Update User Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         UPDATE USER FLOW                                 │
│                          PUT /api/users/{id}                              │
│                                                                          │
│  Client (Admin)                                                        │
│    │                                                                   │
│    │  PUT /api/users/123-456                                            │
│    │  { firstName: "Updated", lastName: "Name" }                       │
│    │  Authorization: Bearer (Admin)                                    │
│    ├──────────────────────────────▶                                    │
│    │                                │                                  │
│    │                               UserController.UpdateUser()         │
│    │                                │                                  │
│    │                               │ [Authorize(Policy = "RequireAdmin")]│
│    │                               │ → 403 nếu không phải Admin       │
│    │                                │                                  │
│    │                               │ MediatR → UpdateUserCommand      │
│    │                                │                                  │
│    │                               │ UpdateUserCommandValidator        │
│    │                               │ ├─ Id != Guid.Empty              │
│    │                               │ ├─ FirstName <= 50 (nếu có)      │
│    │                               │ └─ LastName <= 50 (nếu có)       │
│    │                               │ → Nếu lỗi → 400 Bad Request      │
│    │                                │                                  │
│    │                               │ UpdateUserCommandHandler          │
│    │                               │ UserWriteRepository.UpdateUser    │
│    │                                │                                  │
│    │                               │ SQL:                             │
│    │                               │ UPDATE AspNetUsers SET           │
│    │                               │   FirstName = COALESCE(@fn, FN)  │
│    │                               │   LastName = COALESCE(@ln, LN)   │
│    │                               │ WHERE Id = @UserId              │
│    │                                │                                  │
│    │                               ├─ rows = 0 → 404 NotFound        │
│    │                               └─ rows > 0 → 204 No Content      │
│    │                                │                                  │
│    │  [Thành công]                  │                                  │
│    │  ← 204 No Content             │                                  │
│    │                                │                                  │
│    │  [Không tìm thấy user]        │                                  │
│    │  ← 404 Not Found              │                                  │
│    │  { "error": "User not found" }│                                  │
│    │                                │                                  │
│    │  [Không phải Admin]           │                                  │
│    │  ← 403 Forbidden              │                                  │
│    │  { "error": "Forbidden" }     │                                  │
│    │◀──────────────────────────────│                                  │
└──────────────────────────────────────────────────────────────────────────┘
```

### 6.5. Error Handling Flow (ExceptionMiddleware)

```
┌──────────────────────────────────────────────────────────────────────┐
│                      EXCEPTION MIDDLEWARE FLOW                        │
│                                                                      │
│  Controller / Handler                                                │
│    │                                                                │
│    ├─ throw new NotFoundException("User not found")                 │
│    │   → ExceptionMiddleware catched                                │
│    │   → StatusCode: 404                                            │
│    │   → Response: { "error": "User not found", "code": "NOT_FOUND" }│
│    │                                                                │
│    ├─ throw new UnauthorizedException()                             │
│    │   → ExceptionMiddleware catched                                │
│    │   → StatusCode: 401                                            │
│    │   → Response: { "error": "Unauthorized" }                      │
│    │                                                                │
│    ├─ throw new ForbiddenException("Admin only")                    │
│    │   → ExceptionMiddleware catched                                │
│    │   → StatusCode: 403                                            │
│    │   → Response: { "error": "Forbidden", "message": "Admin only" }│
│    │                                                                │
│    ├─ throw new ValidationException(errors)                         │
│    │   → FluentValidationMiddleware catched                         │
│    │   → StatusCode: 400                                            │
│    │   → Response: { "errors": { "Email": ["..."] } }              │
│    │                                                                │
│    └─ throw new Exception("Something went wrong")                   │
│        → ExceptionMiddleware catched                                │
│        → StatusCode: 500                                            │
│        → Response: { "error": "Internal server error" }            │
│        → (chi tiết lỗi log, không trả về client)                   │
└──────────────────────────────────────────────────────────────────────┘
```

**So sánh trước-sau error handling:**

| Exception | Trước | Sau |
|-----------|-------|-----|
| **NotFound** | 500 (unhandled) | 404 với message |
| **Unauthorized** | 401 (mặc định) | 401 với message |
| **Forbidden** | 403 (mặc định) | 403 với message |
| **Validation** | 400 (FluentValidation) | 400 (giữ nguyên) |
| **Unhandled** | 500 (developer page) | 500 (JSON, log detail) |

---

## 7. Kiến Trúc Tổng Thể

### 7.1. Layer Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         AuthApi.WebApi                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │
│  │ AuthController│  │ UserController│  │ Exception-   │  │  Program.cs │ │
│  │               │  │              │  │ Middleware   │  │  (DI +     │ │
│  │ Login/Register│  │ Me/Detail/   │  │              │  │  Middleware)│ │
│  │ Logout/Refresh│  │ List/Update/ │  │ 401/403/404/ │  │            │ │
│  │               │  │ Roles/Lockout│  │ 500 handler  │  │            │ │
│  └──────┬───────┘  └──────┬───────┘  └──────────────┘  └────────────┘ │
│         │                 │              │                               │
└─────────┼─────────────────┼──────────────┼───────────────────────────────┘
          │                 │              │
          ▼                 ▼              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      AuthApi.Application                                 │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                      COMMANDS (Write)                            │   │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐  │   │
│  │  │ UpdateUserCommand │  │AssignRolesCommand│  │RemoveRolesCmd  │  │   │
│  │  │ +Handler         │  │ +Handler         │  │ +Handler       │  │   │
│  │  │ +Validator       │  │ +Validator       │  │ +Validator     │  │   │
│  │  └──────────────────┘  └──────────────────┘  └────────────────┘  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                      QUERIES (Read)                              │   │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐  │   │
│  │  │ MeQuery           │  │GetUserByIdQuery  │  │ (GetAllUsers   │  │   │
│  │  │ +Handler (+Cache) │  │ +Handler         │  │  direct repo)  │  │   │
│  │  └──────────────────┘  └──────────────────┘  └────────────────┘  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                    PORTS (Interfaces)                            │   │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌──────────┐  │   │
│  │  │IUserContext │  │IUserRead   │  │IUserWrite  │  │ICache    │  │   │
│  │  │(Security)  │  │Repository  │  │Repository  │  │Service   │  │   │
│  │  └────────────┘  └────────────┘  └────────────┘  └──────────┘  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    AuthApi.Infrastructure                                │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                  DAPPER REPOSITORIES                             │   │
│  │  ┌─────────────────────────────────────────────────────────────┐ │   │
│  │  │ UserReadRepository               UserWriteRepository        │ │   │
│  │  │  GetMeAsync(id) → MeResponse      UpdateUserAsync()         │ │   │
│  │  │  GetUserDetailAsync(id)           AssignRolesAsync()         │ │   │
│  │  │  GetAllUsersAsync() → List<User>  RemoveRolesAsync()         │ │   │
│  │  │                                  SoftDeleteUserAsync()       │ │   │
│  │  └─────────────────────────────────────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                  SERVICES                                        │   │
│  │  ┌────────────────┐  ┌────────────────┐  ┌─────────────────┐   │   │
│  │  │ HttpUserContext  │  │ RedisCache     │  │ IdentityService   │   │   │
│  │  │ (HTTP request)  │  │ Service        │  │ (Login/Register) │   │   │
│  │  └────────────────┘  └────────────────┘  └─────────────────┘   │   │
│  └──────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

### 7.2. File Structure Hoàn Chỉnh

```
AuthApi.Application/
├── AuthApi.Application.csproj
├── Abstractions/
│   ├── Cache/
│   │   └── ICacheService.cs                    ← MỚI: Cache abstraction
│   ├── Interfaces/
│   │   ├── Auth/
│   │   │   └── ICurrentUserService.cs          ← ĐÃ XÓA: thay bởi IUserContext
│   │   ├── Repositories/
│   │   │   ├── IUserQueryRepository.cs         ← ĐÃ XÓA: thay bởi IUserReadRepository
│   │   │   ├── IUserReadRepository.cs          ← MỚI: Dapper read interface
│   │   │   └── IUserWriteRepository.cs         ← MỚI: Dapper write interface
│   │   ├── ICategoryRepository.cs              ← Còn nhưng Không dùng
│   │   ├── IProductRepository.cs               ← Còn nhưng Không dùng
│   │   └── IWardrobeRepository.cs              ← Còn nhưng Không dùng
│   └── Pagination/
│       └── PagedResult.cs
├── Behaviors/
│   └── ValidationBehavior.cs
├── Common/
│   ├── ErrorCodes.cs                           ← SỬA: +NotFound, Forbidden, Unauthorized
│   ├── BaseResponse.cs
│   ├── NotFoundException.cs                    ← MỚI: 404 exception
│   ├── Security/
│   │   ├── IUserContext.cs                     ← MỚI: Pure abstraction
│   │   ├── UnauthorizedException.cs            ← MỚI: 401 exception
│   │   └── ForbiddenException.cs               ← MỚI: 403 exception
│   └── CurrentUserContext.cs                   ← Không còn nếu xóa
├── Features/
│   ├── Auth/
│   │   ├── Commands/
│   │   │   ├── Login/                          ← SỬA: IUserContext, Roles[]
│   │   │   ├── Logout/                         ← SỬA: IUserContext
│   │   │   ├── Register/                       ← SỬA: IUserContext, Roles[]
│   │   │   └── ForgetPassword/
│   │   └── DTOs/
│   │       ├── AuthUserDto.cs                  ← SỬA: Role→Roles[]
│   │       ├── Login/
│   │       │   ├── LoginResponse.cs             ← SỬA: role→roles[]
│   │       │   └── ...
│   │       └── .../
│   └── Users/
│       ├── Commands/
│       │   ├── AssignRoles/                    ← MỚI: full command + handler + validator
│       │   ├── RemoveRoles/                    ← MỚI: full command + handler + validator
│       │   └── UpdateUser/                     ← MỚI: full command + handler + validator
│       ├── DTOs/
│       │   ├── MeResponse.cs                   ← SỬA: Role→Roles[]
│       │   ├── UserDetailResponse.cs           ← MỚI: admin detail DTO
│       │   ├── UserListItemDto.cs              ← MỚI: list item DTO
│       │   └── UserDto.cs                      ← ĐÃ XÓA: thay bởi UserListItemDto
│       └── Queries/
│           ├── GetAllUserQuery.cs              ← SỬA: Dapper thay SqlKata
│           ├── GetAllUserQueryHandler.cs       ← SỬA: Dapper thay SqlKata
│           ├── GetUserById/
│           │   ├── GetUserByIdQuery.cs          ← MỚI: admin detail query
│           │   └── GetUserByIdQueryHandler.cs   ← MỚI: admin detail handler
│           ├── Me/
│           │   └── MeQuery.cs                   ← MỚI: me query
│           └── Me/
│               └── MeQueryHandler.cs           ← SỬA: +cache logic
└── Helpers/
    └── Validation/
        └── HandleFailureResult.cs

AuthApi.Domain/
├── AuthApi.Domain.csproj
├── Entities/
│   ├── BaseEntity.cs
│   └── ...
├── Enums/
│   └── UserRole.cs                             ← SỬA: thêm roles
├── Factories/
│   └── UsersFactory.cs                         ← SỬA: roles[]
└── Interfaces/
    └── IUserRepository.cs                      ← ĐÃ XÓA: dead code

AuthApi.Infrastructure/
├── AuthApi.Infrastructure.csproj               ← SỬA: +Dapper, -SqlKata
├── Configuration/
│   └── DependencyInjection.cs                  ← SỬA: DI cho Dapper, cache, context
├── Persistence/
│   ├── Dapper/
│   │   ├── DbConnectionFactory.cs              ← SỬA: string constructor
│   │   └── Repositories/
│   │       ├── UserReadRepository.cs           ← MỚI: Dapper read impl
│   │       └── UserWriteRepository.cs          ← MỚI: Dapper write impl
│   └── Repositories/Users/
│       ├── UserQueryRepository.cs              ← ĐÃ XÓA: SqlKata
│       └── UserRepository.cs                   ← ĐÃ XÓA: EF Core stub
├── Services/
│   ├── Auth/
│   │   ├── HttpUserContext.cs                  ← MỚI: HTTP impl của IUserContext
│   │   ├── UserContext.cs                      ← MỚI: test/background impl
│   │   ├── IdentityService.cs                  ← SỬA: IUserContext, Roles[]
│   │   └── ...
│   ├── Cache/
│   │   └── RedisCacheService.cs                ← MỚI: Redis cache impl
│   └── Token/
│       └── TokenService.cs                     ← SỬA: Roles[]
├── ...
└── ...

AuthApi.WebApi/
├── Controllers/
│   ├── AuthController.cs                       ← SỬA: v2 responses
│   └── UserController.cs                       ← SỬA: +Me, +{id}, +Admin endpoints
├── Middlewares/
│   ├── CSRFMiddleware.cs                       ← SỬA: IUserContext
│   └── ExceptionMiddleware.cs                  ← SỬA: 401, 403, 404 handling
└── Program.cs                                  ← SỬA: Auth Policies, ExceptionMiddleware
```

### 7.3. Dependency Injection Map

```
Program.cs
│
├─ Identity (ASP.NET Core Identity)
│   └─ services.AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>()
│       └─ Dùng EF Core internally cho Auth (Login/Register/Logout)
│
├─ Dapper (Data Access)
│   └─ services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(cs))
│       ├─ services.AddScoped<IUserReadRepository, UserReadRepository>()
│       └─ services.AddScoped<IUserWriteRepository, UserWriteRepository>()
│
├─ Redis Cache
│   └─ services.AddStackExchangeRedisCache(options => { ... })
│       └─ services.AddSingleton<ICacheService, RedisCacheService>()
│
├─ User Context
│   └─ services.AddScoped<IUserContext, HttpUserContext>()
│
├─ MediatR (CQRS)
│   └─ services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))
│       └─ services.AddValidatorsFromAssembly(...)
│
├─ Authorization Policies
│   ├─ "RequireAdmin" → RequireRole("Admin")
│   └─ "RequireUser" → RequireRole("User")
│
└─ Middleware Pipeline
    ├─ ExceptionMiddleware (401/403/404/500 → JSON)
    ├─ CSRFMiddleware
    └─ ...
```

### 7.4. So Sánh Tổng Quan: Trước vs Sau

| Khía cạnh | Trước (SqlKata + ICurrentUserService) | Sau (Dapper + IUserContext) | Lý do |
|-----------|----------------------------------------|-----------------------------|-------|
| **Read ORM** | SqlKata QueryFactory | Dapper + IDbConnectionFactory | SqlKata chậm, khó debug, thêm dependency |
| **Write ORM** | EF Core stub (không dùng được) | Dapper (production-ready) | EF Core stub chưa implement, cần giải pháp thực tế |
| **User Context** | ICurrentUserService (chỉ HTTP) | IUserContext (HTTP + Test + Background) | Cần test được, cần background job support |
| **Roles type** | `string` (1 role) | `string[]` (nhiều roles) | Identity schema là Many-to-Many |
| **Query pattern** | JOIN + GROUP BY + STRING_AGG | 2 queries (user info + roles batch) | Tránh STRING_AGG, dễ đọc, dễ bảo trì |
| **Caching** | Không có | Redis (TTL 5 phút, graceful degradation) | /me gọi nhiều, profile ít thay đổi |
| **Authorization** | [Authorize] + check role thủ công | Policy-based (RequireAdmin, RequireUser) | Centralized, dễ maintain, an toàn hơn |
| **Error status codes** | 400 (validation), 500 (còn lại) | 401, 403, 404, 500 đầy đủ | RESTful best practice |
| **Admin API** | Không có | GET/PUT users, POST/DELETE roles, DELETE lockout | Cần cho admin panel |
| **DTO consistency** | DTOs lộn xộn (UserDto vs UserListItemDto) | Tách biệt: MeResponse, UserDetailResponse, UserListItemDto | Mỗi use case có DTO riêng |
| **Dead code** | IUserRepository, UserRepository, ICategoryRepository... | Đã xóa 5 files | Clean code, dễ maintain |
| **Testability** | Khó (phụ thuộc HttpContext, SqlKata) | Dễ (mock IUserContext, IDbConnectionFactory) | Interface segregation + DI |
| **Packages** | SqlKata 4.0.1 + SqlKata.Execution | 🗑️ Đã xóa — chỉ Dapper 2.x | Giảm dependency, tăng performance |
