# Docker Compose — Orchestration

- **File:** `docker-compose.yml`, `.env.template`, `appsettings.Docker.json`
- **Phụ thuộc:** `Dockerfile`, `Program.cs` (--migrate flag)
- **Mục tiêu:** Chạy 3 container (API + SQL Server + Redis) phối hợp với nhau

---

## 🎯 Mục Tiêu

| Mục tiêu | Mô tả |
|----------|-------|
| **1. One-Command Setup** | `docker-compose up -d` → cả app chạy |
| **2. Isolation** | Mỗi service 1 container riêng, không xung đột với máy host |
| **3. Data Persistence** | Volume lưu data SQL + Redis sau restart |
| **4. Startup Order** | API chỉ start khi SQL Server sẵn sàng |
| **5. Security** | Secrets qua `.env` file, không hardcode |

---

## ✅ Checklist Chi Tiết

### □ 3.1 Kiến Trúc Docker Compose

```yaml
services:
  api:         # ASP.NET Core :8080 (build từ Dockerfile)
  sqlserver:   # SQL Server 2022 :1433 (image có sẵn)
  redis:       # Redis 7 Alpine :6379 (image có sẵn)
```

```
┌─────────────────────────────────────────────────────────────┐
│                      docker-compose.yml                       │
│                                                             │
│  ┌─────────────────────┐    ┌──────────────────┐           │
│  │      api            │    │    sqlserver      │           │
│  │  (ASP.NET :8080)    │◀──▶│  (SQL Server      │           │
│  │                     │    │   :1433)          │           │
│  │  depends_on:        │    │                   │           │
│  │   sqlserver (health)│    │  Volume: sql_data │           │
│  │   redis (started)   │    └──────────────────┘           │
│  │                     │                                   │
│  │  Volume: none       │    ┌──────────────────┐           │
│  │  (stateless)        │◀──▶│      redis        │           │
│  │                     │    │  (Redis 7 :6379)  │           │
│  └─────────────────────┘    │                   │           │
│                             │  Volume: redis_data│           │
│                             └──────────────────┘           │
│                                                             │
│  NETWORK: tnp-api_default (bridge) tự động tạo              │
└─────────────────────────────────────────────────────────────┘
```

### □ 3.2 API Service

```yaml
api:
  build:
    context: .
    dockerfile: AuthApi.WebApi/Dockerfile
  ports:
    - "${API_PORT:-5000}:8080"
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
    interval: 15s
    timeout: 5s
    retries: 3
    start_period: 60s
  depends_on:
    sqlserver:
      condition: service_healthy
    redis:
      condition: service_started
  environment:
    - ASPNETCORE_ENVIRONMENT=Docker
    - ConnectionStrings__Default=Server=sqlserver,1433;Database=AuthDb;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True
    - ConnectionStrings__Redis=redis:6379
    - AppSettings__JwtKey=${JWT_KEY}
    - AppSettings__JwtIssuer=${JWT_ISSUER:-TravelNow}
    - AppSettings__JwtAudience=${JWT_AUDIENCE:-TravelNowApp}
    - Frontend__Url=${FRONTEND_URL:-http://localhost:3000}
  restart: unless-stopped
```

#### Giải thích từng phần:

**`build.context` và `build.dockerfile`:**
- Context là thư mục gốc (`.`), Dockerfile nằm ở `AuthApi.WebApi/Dockerfile`
- **Nếu sai context:** Docker không tìm thấy file .csprop vì đường dẫn tương đối

**`ports` mapping:**
- `${API_PORT:-5000}:8080` → Host port (lấy từ .env, mặc định 5000) map vào container port 8080
- **Nếu thiếu:** API chạy trong container nhưng không truy cập được từ trình duyệt

**`healthcheck`:**
- Dùng `curl` gọi `http://localhost:8080/health` mỗi 15 giây
- `start_period: 60s` — cho API thời gian khởi động (migrate + seed)
- **Nếu thiếu:** Docker không biết API đã sẵn sàng hay chưa; load balancer có thể gửi request vào container chưa ready

**`depends_on`:**
- `sqlserver` có `condition: service_healthy` → API chỉ start sau khi SQL Server health check pass
- `redis` có `condition: service_started` → Redis chỉ cần start (không cần health)
- **Nếu thiếu `depends_on`:** API start trước SQL Server → crash vì không connect được DB → restart loop

**`environment` variables:**
- Dùng cú pháp `${VAR}` để lấy từ `.env` file
- `ConnectionStrings__Default` — dấu `__` là cách ASP.NET đọc nested config từ env (tương đương `ConnectionStrings:Default`)
- **Nếu thiếu `ConnectionStrings__Default`:** API không biết kết nối đến database nào → crash
- **Nếu thiếu `AppSettings__JwtKey`:** JWT không sign được → 401 mọi request

### □ 3.3 SQL Server Service

```yaml
sqlserver:
  image: mcr.microsoft.com/mssql/server:2022-latest
  environment:
    - ACCEPT_EULA=Y
    - MSSQL_SA_PASSWORD=${SA_PASSWORD}
  ports:
    - "${SQL_PORT:-1433}:1433"
  volumes:
    - sql_data:/var/opt/mssql
  healthcheck:
    test:
      ["CMD", "/opt/mssql-tools18/bin/sqlcmd",
       "-C", "-S", "localhost",
       "-U", "sa", "-P", "${SA_PASSWORD}",
       "-Q", "SELECT 1"]
    interval: 10s
    timeout: 5s
    retries: 10
    start_period: 30s
  restart: unless-stopped
```

**`image`:** Image chính thức từ Microsoft
- **Nếu dùng image khác (như `mcr.microsoft.com/mssql/server:2019-latest`):** Có thể thiếu tính năng (T-SQL, performance)

**`ACCEPT_EULA=Y`:** Bắt buộc để chấp nhận license SQL Server
- **Nếu thiếu:** Container start → crash ngay vì chưa accept EULA

**`sql_data` volume:**
- Lưu file .mdf/.ldf vào Docker volume → không mất data khi restart container
- **Nếu thiếu:** Mỗi lần restart container → mất toàn bộ data (user, roles, transactions)

**`sqlcmd` healthcheck:**
- Dùng SQLCMD kiểm tra SQL Server sẵn sàng nhận kết nối
- Cờ `-C` (trust certificate) — cần vì SQL Server container dùng self-signed cert
- `start_period: 30s` — SQL Server cần ~20-30s để khởi động lần đầu
- **Nếu thiếu:** API không biết SQL Server đã sẵn sàng → crash khi connect

### □ 3.4 Redis Service

```yaml
redis:
  image: redis:7-alpine
  ports:
    - "${REDIS_PORT:-6379}:6379"
  volumes:
    - redis_data:/data
  healthcheck:
    test: ["CMD", "redis-cli", "ping"]
    interval: 5s
    timeout: 3s
    retries: 5
  restart: unless-stopped
```

- **`redis:7-alpine`:** Nhẹ (~5MB), an toàn, performance tốt
- **`redis_data` volume:** Lưu cache data (nếu Redis persistence được bật)
- **`redis-cli ping` healthcheck:** Đơn giản, nhanh—Redis ping/pong

### □ 3.5 Volume Definitions

```yaml
volumes:
  sql_data:
  redis_data:
```

- Tạo 2 named volumes để Docker quản lý
- **Nếu dùng bind mount (`./data:/var/opt/mssql`):** Không portable—phụ thuộc vào OS
- **Named volumes:** Docker quản lý, portable, backup dễ

### □ 3.6 `.env.template` — Biến Môi Trường

```bash
# === SA Password cho SQL Server (bắt buộc phải thay đổi) ===
SA_PASSWORD=Nhonaovay@1

# === JWT Key (bắt buộc phải thay đổi, tối thiểu 32 ký tự) ===
JWT_KEY=NguyenThanhTuanKrp1PhuTucKrongPaGiaLaiNhoNhaovay@1

# === JWT Issuer & Audience (tùy chọn) ===
JWT_ISSUER=TravelNow
JWT_AUDIENCE=TravelNowApp

# === Port mapping (tùy chọn) ===
API_PORT=5000
SQL_PORT=1433
REDIS_PORT=6379

# === Frontend URL (tùy chọn) ===
FRONTEND_URL=https://localhost:3001
```

**Tại sao cần `.env` riêng?**
- **Không hardcode secrets trong docker-compose.yml:** Tránh commit password lên Git
- **`.gitignore` có sẵn `.env`:** Đảm bảo không lộ mật khẩu
- **Dễ thay đổi:** Chỉ cần sửa 1 file, không cần sửa docker-compose

**Nếu dùng trực tiếp trong docker-compose:**
```yaml
# Không làm thế này — password lộ trong Git history
SA_PASSWORD=MyRealPassword
```

### □ 3.7 `appsettings.Docker.json` — Cấu Hình Docker

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Frontend": {
    "Url": "https://localhost:3001"
  },
  "Cookie": {
    "Secure": false
  }
}
```

- `ASPNETCORE_ENVIRONMENT=Docker` → ASP.NET load `appsettings.Docker.json` (sau `appsettings.json`)
- **`Cookie.Secure = false`:** Trong container HTTP (không HTTPS), cookie secure=true sẽ không gửi được
- **Nếu thiếu `Cookie.Secure = false`:** Cookie không được set → authentication không hoạt động trong Docker

### □ 3.8 Migration Tự Động (Program.cs --migrate)

```csharp
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
```

- Docker Compose gọi: `dotnet AuthApi.WebApi.dll --migrate`
- Chạy EF Core migration + seed data (roles) khi container start
- **Nếu thiếu:** Database schema không được tạo → API trả về lỗi "Invalid object name 'AspNetUsers'"

---

## 🔄 Flow: docker-compose up

```
User runs: docker-compose up -d
    │
    ▼
Step 1: Pull images (nếu chưa có)
    ├─ mcr.microsoft.com/mssql/server:2022-latest
    ├─ redis:7-alpine
    └─ build api locally (Dockerfile)
    │
    ▼
Step 2: Create volumes
    ├─ sql_data
    └─ redis_data
    │
    ▼
Step 3: Start sqlserver + redis (song song)
    ├─ sqlserver (healthcheck chờ ~30s)
    └─ redis (healthcheck ~2s)
    │
    ▼
Step 4: Start api (chờ sqlserver healthy)
    ├─ dotnet AuthApi.WebApi.dll --migrate (EF Core schema)
    ├─ Seed roles (Admin, User)
    └─ Listen on http://+:8080
    │
    ▼
Step 5: Ready!
    http://localhost:5000/swagger
```

---

## ⚠️ Rủi Ro & Giải Pháp

| Rủi ro | Giải pháp |
|--------|-----------|
| **SQL Server tốn RAM** | Docker Desktop → Settings → Memory > 2GB |
| **Container restart mất data** | Volumes `sql_data`, `redis_data` (persistent) |
| **SA_PASSWORD leak** | `.env` file (đã .gitignore) |
| **Certificate SSL** | `TrustServerCertificate=True` trong connection string |
| **Healthcheck timeout** | `start_period: 30s` cho sqlserver, `60s` cho api |
| **Container không connect được sqlserver** | `depends_on: condition: service_healthy` |
| **Migration fail** | Docker compose dùng `entrypoint` chạy migrate trước khi start server |
