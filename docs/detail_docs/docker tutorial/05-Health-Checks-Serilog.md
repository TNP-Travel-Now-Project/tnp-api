# Health Checks + Serilog

- **Files:** `AuthApi.WebApi/HealthChecks/RedisHealthCheck.cs`, `Program.cs`, `appsettings.json`
- **Phụ thuộc:** `docker-compose.yml` (healthcheck api service), `appsettings.json` (Serilog config)
- **Mục tiêu:** Health Check endpoint + structured logging

---

## 🎯 Mục Tiêu

| Mục tiêu | Mô tả |
|----------|-------|
| **1. Health Check** | Endpoint `/health` cho Docker/Load Balancer biết app còn sống không |
| **2. Redis Check** | Custom health check kiểm tra Redis connectivity |
| **3. Serilog** | Thay `Console.WriteLine()` bằng structured logging |
| **4. File Logging** | Rolling file log (14 ngày) cho troubleshooting |
| **5. Log Levels** | INFO cho app, WARNING cho framework (giảm noise) |

---

## ✅ Checklist Chi Tiết

### □ 5.1 Tại Sao Cần Health Checks?

**Vấn đề nếu không có health check:**
```
┌──────────────────────────────────────────┐
│            Docker Compose                  │
│                                           │
│  api (ASP.NET) ── SQL Server (healthy)    │
│                                           │
│  ● SQL Server khởi động xong              │
│  ● `depends_on: condition: service_healthy`│
│  ● Docker thấy SQL Server healthy         │
│  ● Docker START api container             │
│                                           │
│  ⚠ NHƯNG chưa biết api có chạy không!    │
│                                           │
│  ● API migration chưa xong → crash        │
│  ● API kết nối Redis thất bại → crash     │
│  ● Load balancer gửi request → 502        │
└──────────────────────────────────────────┘
```

**Health check giải quyết:**
```
GET /health → 200 OK
{
  "status": "Healthy",
  "entries": {
    "sqlserver": { "status": "Healthy" },
    "redis": { "status": "Healthy" }
  }
}
```

- Docker gọi `/health` mỗi 15 giây
- Chỉ khi trả về 200 → container marked "healthy"
- Load balancer chỉ gửi request đến container healthy

### □ 5.2 RedisHealthCheck — Custom Check

```csharp
public sealed class RedisHealthCheck(IConnectionMultiplexer _redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable", ex);
        }
    }
}
```

**Primary constructor** (`(IConnectionMultiplexer _redis)`):
- Inject `IConnectionMultiplexer` từ DI (đã register trong Infrastructure)
- Không cần field riêng — C# 12 primary constructor tự tạo field

**Tại sao dùng PING?**
- Ping là lệnh nhẹ nhất (O(1)), không ảnh hưởng performance
- Nếu Redis không reachable → exception → Unhealthy

**Graceful degradation:**
- Redis health check fail → API vẫn chạy (chỉ mất cache, không crash)
- Nhưng Docker/Load Balancer biết Redis đang có vấn đề

**Nếu không có RedisHealthCheck:**
- Health check chỉ check SQL Server (qua DbContext)
- Redis có thể chết mà không ai biết → cache miss liên tục → DB load tăng đột biến

### □ 5.3 Đăng Ký Health Checks (Program.cs)

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "sqlserver",
        tags: ["db", "sql"])
    .AddCheck<RedisHealthCheck>(
        name: "redis",
        tags: ["cache", "redis"]);
```

- `AddDbContextCheck<AppDbContext>()` — EF Core tự động check kết nối SQL Server
- `AddCheck<RedisHealthCheck>()` — custom check cho Redis

**Endpoint mapping:**
```csharp
app.MapHealthChecks("/health", new()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

- Dùng `UIResponseWriter` từ package `AspNetCore.HealthChecks.UI.Client`
- Trả về JSON chi tiết: status, từng check, duration

**Nếu không dùng `UIResponseWriter`:**
- Mặc định ASP.NET trả về text `Healthy`/`Unhealthy` — không có chi tiết từng check
- Khó debug khi health check fail

### □ 5.4 Serilog — Tại Sao?

**Vấn đề với `Console.WriteLine()`:**
```
// ❌ Cũ:
Console.WriteLine($"Authentication failed: {context.Exception.Message}");

// Không có: timestamp, log level, structured data
// Không thể: filter, search, alert
// Mất: exception stack trace
```

**Serilog giải quyết:**
```
// ✅ Mới:
Log.Warning(context.Exception, "Authentication failed");

// Output:
// [14:23:45 WRN] Authentication failed
// System.Security.SecurityTokenExpiredException: ...
//    at ...
```

- Có timestamp, log level, exception details
- Structured logging — có thể query bằng Serilog.Sinks.Seq, Elasticsearch, v.v.

### □ 5.5 Cấu Hình Serilog (Program.cs)

```csharp
// Đầu Main — cấu hình Serilog trước khi build WebApplication
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting application");
    await BuildAndRun(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Trong BuildAndRun — Serilog cho toàn bộ host
builder.Host.UseSerilog();
```

**Giải thích từng bước:**

| Bước | Mục đích |
|------|----------|
| `.ReadFrom.Configuration(...)` | Đọc cấu hình từ appsettings.json + env |
| `Log.Logger = ...` | Global static logger — dùng được ở mọi nơi |
| `try/catch/finally` | Bắt lỗi startup, đảm bảo log được flush |
| `Log.Information("Starting")` | Ghi nhận thời điểm start |
| `Log.Fatal(ex, "...")` | Ghi nhận crash với full stack trace |
| `Log.CloseAndFlushAsync()` | Đảm bảo log entries không bị mất |
| `builder.Host.UseSerilog()` | Serilog làm logging provider cho ASP.NET |

**Nếu không có try/catch:**
- Lỗi startup bị nuốt mất → không biết app crash vì lý do gì
- `CloseAndFlushAsync` không được gọi → mất log cuối

### □ 5.6 Cấu Hình Serilog (appsettings.json)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Hangfire": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/tnp-api-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 14,
          "fileSizeLimitBytes": 104857600
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

**MinimumLevel.Override:**
- `Microsoft.AspNetCore: Warning` — bỏ qua log DEBUG/INFO từ ASP.NET (request info, etc.)
- `Microsoft.EntityFrameworkCore: Warning` — bỏ qua SQL queries log
- `Hangfire: Warning` — bỏ qua background job info

**Nếu không override:**
- Console tràn ngập log từ ASP.NET (`Request started`, `Request finished`) → khó đọc log thật
- EF Core log mỗi SQL query → hàng nghìn dòng/giây

**WriteTo sinks:**
| Sink | Tác dụng | Nếu thiếu |
|------|---------|-----------|
| Console | Xem log realtime khi docker-compose logs | Không thấy log khi chạy container |
| File | Rolling file 14 ngày, 100MB/file | Mất log sau khi container restart |

**File rolling config:**
- `path: "logs/tnp-api-.log"` → tạo file: `tnp-api-20260609.log`
- `rollingInterval: Day` → mỗi ngày 1 file mới
- `retainedFileCountLimit: 14` → giữ 14 ngày, tự động xóa cũ
- `fileSizeLimitBytes: 100MB` → nếu file >100MB, tạo file mới

**Enrichers:**
- `FromLogContext` — thêm properties từ LogContext (ví dụ: UserId, RequestId)
- `WithMachineName` — thêm tên máy (hữu ích khi nhiều server)
- `WithThreadId` — thêm Thread ID (debug concurrent issues)

### □ 5.7 Docker Healthcheck Update

```yaml
# docker-compose.yml — api service
api:
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
    interval: 15s
    timeout: 5s
    retries: 3
    start_period: 60s
```

**Trước đây:** Không có healthcheck cho API (chỉ SQL Server có healthcheck)
**Sau:** API tự kiểm tra sức khỏe qua endpoint `/health` (kiểm tra cả SQL + Redis)

**Tại sao dùng `curl` thay vì `sqlcmd`?**
- `sqlcmd` chỉ check SQL Server — không biết API có chạy không
- `/health` check cả SQL Server + Redis — toàn diện hơn
- Không cần cài `mssql-tools` trong API image → image nhỏ hơn

**Lưu ý:** Image base `mcr.microsoft.com/dotnet/aspnet:10.0` không có `curl` mặc định. `curl` được cài trong Dockerfile stage `base`:
```dockerfile
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
```
Nếu thiếu `curl`, health check luôn fail → container báo `unhealthy`.

---

## 📦 NuGet Packages Added

| Package | Version | Mục đích |
|---------|---------|----------|
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | 10.0.6 | `AddDbContextCheck<AppDbContext>()` |
| `AspNetCore.HealthChecks.UI.Client` | 9.0.0 | JSON response writer cho `/health` |
| `Serilog.AspNetCore` | 10.0.0 | Host logging + DI tích hợp |
| `Serilog.Sinks.File` | (transitive) | Ghi log ra file (rolling daily) |

---

## 🔄 Flow: Application Startup với Serilog

```
Program.Main()
    │
    ├─ Log.Logger = new LoggerConfiguration()...
    ├─ .ReadFrom.Configuration(appsettings.json + env)
    │
    ├─ try
    │   ├─ Log.Information("Starting application")
    │   ├─ WebApplication.Build() + Run()
    │   └─ Log.Information("Application started")
    │
    ├─ catch (Exception ex)
    │   └─ Log.Fatal(ex, "Application terminated")
    │
    └─ finally
        └─ Log.CloseAndFlushAsync()
```

```
Application running
    │
    ├─ GET /health → Kiểm tra SQL + Redis → 200 OK + JSON
    ├─ GET /api/... → Log tự động từ ASP.NET (Warning level)
    ├─ Authentication fail → Log.Warning("Authentication failed")
    └─ Exception → Log.Error / Log.Fatal (tự động capture)
```

---

## ⚠️ Rủi Ro & Giải Pháp

| Rủi ro | Giải pháp |
|--------|-----------|
| **Log file đầy disk** | `retainedFileCountLimit: 14` + `fileSizeLimitBytes: 100MB` |
| **Performance impact từ logging** | Async sinks (mặc định), không block request |
| **Secrets trong log** | Serilog filter (có sẵn `Destructure`) — không log password |
| **Health check false positive** | Cấu hình retries + timeout phù hợp |
| **Serilog không flush kịp** | `CloseAndFlushAsync()` trong `finally` block |
