# Integration Tests với TestContainers

- **Files:** `AuthApi.IntegrationTests/` (csproj + 4 files)
- **Phụ thuộc:** `AuthApi.Infrastructure.csproj` (InternalsVisibleTo), `DbConnectionFactory` (string constructor)
- **Mục tiêu:** Test Dapper queries/writes trên SQL Server thật

---

## 🎯 Mục Tiêu

| Mục tiêu | Mô tả |
|----------|-------|
| **1. Real Database** | Test trên SQL Server thật (không mock) |
| **2. Auto Provisioning** | TestContainers tự start/stop SQL Server trong Docker |
| **3. True Coverage** | Bắt lỗi SQL mapping, transaction, data type |
| **4. 11 Tests** | 5 read + 6 write — full coverage cho UserRepository |

---

## ✅ Checklist Chi Tiết

### □ 4.1 TestContainers — Tổng Quan

**Vấn đề khi test database:**
| Cách test | Vấn đề |
|-----------|--------|
| **Mock (Moq)** | Không test được SQL syntax, mapping, transaction |
| **Local SQL Server** | Phụ thuộc môi trường, khác version → trả về kết quả khác |
| **In-memory database** | EF Core InMemory khác behavior với SQL Server thật |

**Giải pháp — TestContainers:**
```
┌─────────────────────────────────────────────────────────┐
│                    TestContainers                            │
│                                                           │
│  SqlServerFixture (IAsyncLifetime)                        │
│  ┌──────────────────────────────────────────────────┐     │
│  │ InitializeAsync()                                │     │
│  │  ├─ Tạo MsSqlContainer (Docker pull + start)     │     │
│  │  ├─ Chạy EF Core Migrate (tạo schema)            │     │
│  │  └─ Tạo DbConnectionFactory cho Dapper           │     │
│  │                                                  │     │
│  │ Tests chạy → seed data → query → assert          │     │
│  │                                                  │     │
│  │ DisposeAsync() → stop + xóa container            │     │
│  └──────────────────────────────────────────────────┘     │
│                                                           │
│  [Collection("SqlServerCollection")]                       │
│  ┌──────────────────────┐ ┌──────────────────────┐        │
│  │ UserReadRepository   │ │ UserWriteRepository  │        │
│  │ Tests (5)            │ │ Tests (6)            │        │
│  └──────────────────────┘ └──────────────────────┘        │
└─────────────────────────────────────────────────────────┘
```

### □ 4.2 SqlServerFixture — Container Lifecycle

```csharp
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;

    public SqlServerFixture()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("IntegrationTest_Pass123!")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // EF Core Migrate — tạo schema Identity + Domain tables
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;
        using var ctx = new AppDbContext(options);
        await ctx.Database.MigrateAsync();

        // DbConnectionFactory cho Dapper (dùng connection string từ container)
        DbConnectionFactory = new DbConnectionFactory(_container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

**Giải thích:**
| Thành phần | Mục đích |
|-----------|---------|
| `MsSqlBuilder` | Tạo config cho SQL Server container |
| `.WithImage(...)` | Chọn image SQL Server (giống production) |
| `.WithPassword(...)` | Set SA password cho container |
| `IAsyncLifetime` | xUnit interface — tự động gọi `InitializeAsync` trước tests, `DisposeAsync` sau |
| `Database.MigrateAsync()` | Chạy EF Core migration (tạo Identity + Domain tables) |
| `DbConnectionFactory(string)` | Constructor mới (chỉ connection string, không cần IConfiguration) |

**Nếu thiếu `Database.MigrateAsync()`:**
- Container SQL Server mới → không có table nào
- Test Dapper query → "Invalid object name 'AspNetUsers'"

**Nếu không dùng IAsyncLifetime:**
- Phải tự quản lý start/stop container trong từng test → chậm, dễ leak container

### □ 4.3 Collection Definition

```csharp
[CollectionDefinition("SqlServerCollection")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>;
```

- Tạo collection để các test class dùng chung 1 fixture (1 container cho tất cả tests)
- **Không có collection:** Mỗi test class tạo 1 container riêng → tốn RAM, chậm

### □ 4.4 DatabaseSeed — Helper

```csharp
internal static class DatabaseSeed
{
    // Seed 1 user + 1 role + gán role — trả về (UserId, RoleId)
    public static async Task<(Guid, Guid)> SeedUserWithRoleAsync(
        IDbConnection conn, string email, string userName, string roleName);

    // Seed 1 role riêng (chưa gán)
    public static async Task<Guid> SeedRoleAsync(IDbConnection conn, string roleName);
}
```

**Tại sao cần DatabaseSeed?**
- Test không thể dùng Identity's UserManager (cần DI phức tạp)
- Dùng Dapper INSERT trực tiếp vào AspNetUsers/AspNetRoles/AspNetUserRoles — nhanh, đơn giản

### □ 4.5 Integration Tests — Danh sách

#### UserReadRepositoryTests (5 tests)

| Test | Mục đích | Nếu thiếu |
|------|----------|-----------|
| `GetMeAsync` | Verify user trả về đúng fields | Không biết Dapper mapping có đúng không |
| `GetMeAsync_MultipleRoles` | User có 2 roles → trả về cả 2 | Lỗi batch roles query không phát hiện được |
| `GetMeAsync_NotFound` | ID không tồn tại → null | Lỗi exception thay vì null |
| `GetAllUsersAsync` | Seed 2 users → GetAll trả về ≥ 2 | Không biết JOIN/query có đúng không |
| `GetUserDetailAsync` | Verify IsLockedOut, roles cho Admin | Lỗi tính toán IsLockedOut |

#### UserWriteRepositoryTests (6 tests)

| Test | Mục đích | Nếu thiếu |
|------|----------|-----------|
| `UpdateUserAsync` | Update FirstName → verify DB | COALESCE query sai không phát hiện được |
| `UpdateUserAsync_NotFound` | ID không tồn tại → false | Lỗi exception thay vì false |
| `AssignRolesAsync` | Assign "Admin" → user có 2 roles | INSERT WHERE NOT EXISTS sai |
| `AssignRolesAsync_NoDuplicate` | Assign same role 2 lần → vẫn 1 row | Bug duplicate role không phát hiện được |
| `RemoveRolesAsync` | Gỡ role → chỉ còn role kia | DELETE JOIN sai — xóa nhầm role |
| `SoftDeleteUserAsync` | Lock user → LockoutEnabled = true | Query lockout sai |

### □ 4.6 DbConnectionFactory — Constructor Cho Tests

```csharp
// Trước: chỉ có constructor nhận IConfiguration
public DbConnectionFactory(IConfiguration configuration)
{
    _connectionString = configuration.GetConnectionString("Default");
}

// Sau: thêm constructor nhận string (cho integration tests)
public DbConnectionFactory(string connectionString)
{
    _connectionString = connectionString;
}
```

**Tại sao cần?**
- Test không có IConfiguration (không có appsettings.json)
- Connection string đến từ TestContainers container (dynamic port)
- **Nếu thiếu:** Phải mock IConfiguration → phức tạp, dễ sai

### □ 4.7 InternalsVisibleTo

```xml
<!-- AuthApi.Infrastructure.csproj -->
<ItemGroup>
  <InternalsVisibleTo Include="AuthApi.IntegrationTests" />
</ItemGroup>
```

- Cho phép integration test project access internal types của Infrastructure
- **Nếu thiếu:** Không thể gọi `DbConnectionFactory`, `UserReadRepository`, `UserWriteRepository`

---

## 🔄 Flow: Test Execution

```
dotnet test AuthApi.IntegrationTests
    │
    ▼
SqlServerFixture.InitializeAsync()
    │
    ├─ Docker pull mcr.microsoft.com/mssql/server:2022-latest (lần đầu ~3 phút)
    ├─ Docker run container
    ├─ EF Core Migration (tạo 20+ tables: AspNetUsers, AspNetRoles, ...)
    └─ Tạo DbConnectionFactory
    │
    ▼
11 Integration Tests chạy
    │
    ├─ Mỗi test seed data → execute Dapper query → assert result
    ├─ Dùng [Collection("SqlServerCollection")] → chung 1 container
    │
    ▼
SqlServerFixture.DisposeAsync()
    │
    └─ Docker stop + remove container
```

---

## ⚠️ Rủi Ro & Giải Pháp

| Rủi ro | Giải pháp |
|--------|-----------|
| **Docker Desktop chưa chạy** | Test fail ngay — kiểm tra `docker ps` trước |
| **Lần đầu pull image lâu** | ~3 phút (image SQL Server ~2GB), các lần sau ~30s |
| **Test không isolation** | Mỗi test clean data hoặc dùng transaction rollback |
| **Container conflict** | TestContainers random port → không conflict |
| **Docker resource** | Cần >2GB RAM cho SQL Server container |
