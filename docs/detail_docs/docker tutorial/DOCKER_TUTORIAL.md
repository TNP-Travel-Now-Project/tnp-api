# Docker Hóa Ứng Dụng ASP.NET Core — Tutorial Từ A → Z

> **Mục đích:** Giải thích từng dòng code trong các file Docker, cách tạo ra chúng,
> và luồng hoạt động từ lúc gõ lệnh đến lúc ứng dụng chạy trên container.

---

## Mục Lục

1. [Tổng quan — Docker là gì và tại sao cần?](#1-tổng-quan--docker-là-gì-và-tại-sao-cần)
2. [Các file Docker trong dự án](#2-các-file-docker-trong-dự-án)
3. [Dockerfile — Chi tiết từng dòng](#3-dockerfile--chi-tiết-từng-dòng)
4. [docker-compose.yml — Chi tiết từng dòng](#4-docker-composeyml--chi-tiết-từng-dòng)
5. [.dockerignore — Tối ưu build context](#5-dockerignore--tối-ưu-build-context)
6. [.env.template — Biến môi trường](#6-envtemplate--biến-môi-trường)
7. [appsettings.Docker.json — Cấu hình Docker](#7-appsettingsdockerjson--cấu-hình-docker)
8. [Program.cs — Flag --migrate](#8-programcs--flag---migrate)
9. [Luồng hoạt động — Từ lệnh đến container chạy](#9-luồng-hoạt-động--từ-lệnh-đến-container-chạy)
10. [Cách sử dụng — Từng bước](#10-cách-sử-dụng--từng-bước)
11. [Câu hỏi thường gặp](#11-câu-hỏi-thường-gặp)

---

## 1. Tổng quan — Docker là gì và tại sao cần?

### Vấn đề

Để chạy project này (tnp-api), bạn cần:

| Phần mềm | Version | Cách cài |
|----------|---------|----------|
| .NET SDK | 10.0 | Download từ microsoft.com |
| SQL Server | 2022 | Download ISO + cài đặt phức tạp |
| Redis | 7.x | Download + cài Windows Service |

Mỗi lần:
- Cài máy mới → mất 2-3 giờ
- Lỗi "cannot connect to DB" → thường do SQL Server version/port/servername
- Đồng nghiệp code xong → máy bạn chạy không được → mất thời gian debug

### Giải pháp

Docker "đóng gói" ứng dụng + database + redis vào các **container** riêng biệt:

```
┌─────────────────────────────────────────────────────────┐
│                    Máy tính của bạn                       │
│                                                         │
│  ┌──────────────────┐     ┌────────────┐               │
│  │  Container: API  │     │ Container: │               │
│  │  .NET 10.0       │◀───▶│ SQL Server │               │
│  │  Port: 5000      │     │ 2022       │               │
│  └──────────────────┘     └────────────┘               │
│                                                         │
│  ┌──────────────────┐                                   │
│  │  Container: Redis│                                   │
│  │  Port: 6379      │                                   │
│  └──────────────────┘                                   │
└─────────────────────────────────────────────────────────┘
```

Bạn chỉ cần cài **Docker Desktop** (1 lần, ~5 phút), mọi thứ khác Docker tự động kéo về.

---

## 2. Các file Docker trong dự án

Khi hoàn thành, dự án có thêm 5 file:

```
tnp-api/
├── AuthApi.WebApi/
│   ├── Dockerfile                     # Cách build image .NET
│   └── appsettings.Docker.json        # Config cho môi trường Docker
├── docker-compose.yml                 # 3 container: api + sqlserver + redis
├── .dockerignore                      # File nào không đưa vào build
└── .env.template                      # Mẫu biến môi trường
```

Ngoài ra, 1 file **được sửa**:
- `AuthApi.WebApi/Program.cs` — thêm flag `--migrate` để auto migration

---

## 3. Dockerfile — Chi tiết từng dòng

> **Đường dẫn:** `AuthApi.WebApi/Dockerfile`
>
> **Mục đích:** Hướng dẫn Docker cách build ứng dụng .NET thành image.

### Toàn bộ file

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AuthApi.WebApi/AuthApi.WebApi.csproj", "AuthApi.WebApi/"]
COPY ["AuthApi.Application/AuthApi.Application.csproj", "AuthApi.Application/"]
COPY ["AuthApi.Domain/AuthApi.Domain.csproj", "AuthApi.Domain/"]
COPY ["AuthApi.Infrastructure/AuthApi.Infrastructure.csproj", "AuthApi.Infrastructure/"]
RUN dotnet restore "AuthApi.WebApi/AuthApi.WebApi.csproj"

COPY . .
RUN dotnet publish "AuthApi.WebApi/AuthApi.WebApi.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app .

USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuthApi.WebApi.dll"]
```

### Giải thích từng phần

#### Phase 1: Build (SDK)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
```
- **`FROM`**: Chọn base image. Image `sdk:10.0` chứa đầy đủ .NET SDK + compiler.
  Dung lượng ~2.5 GB (cần cho build, nhưng không dùng cho runtime).
- **`AS build`**: Đặt tên cho stage này là `build` để stage sau copy từ nó.

```dockerfile
WORKDIR /src
```
- Tạo thư mục `/src` trong container. Mọi lệnh sau đều chạy trong thư mục này.
- Tương tự `cd /src` trong Linux, `Set-Location C:\src` trong Windows.

```dockerfile
COPY ["AuthApi.WebApi/AuthApi.WebApi.csproj", "AuthApi.WebApi/"]
COPY ["AuthApi.Application/AuthApi.Application.csproj", "AuthApi.Application/"]
COPY ["AuthApi.Domain/AuthApi.Domain.csproj", "AuthApi.Domain/"]
COPY ["AuthApi.Infrastructure/AuthApi.Infrastructure.csproj", "AuthApi.Infrastructure/"]
```
- **`COPY`**: Copy file từ máy host (dự án) vào container.
- Cú pháp: `COPY ["<source>", "<destination>"]`
- **Tại sao copy riêng csproj?** — Docker build có **layer cache**. Nếu chỉ sửa code
  (.cs files) mà không sửa csproj, Docker sẽ dùng lại cache của bước `restore`
  thay vì chạy lại từ đầu. Tiết kiệm ~30s mỗi lần build.

```dockerfile
RUN dotnet restore "AuthApi.WebApi/AuthApi.WebApi.csproj"
```
- **`RUN`**: Chạy lệnh trong container lúc build.
- `dotnet restore` tải tất cả NuGet packages từ nuget.org.
- Nếu csproj không đổi, Docker dùng cache → bước này chạy trong ~0s.

```dockerfile
COPY . .
```
- Copy toàn bộ source code còn lại (các file .cs, .json, v.v.) vào container.
- `.dockerignore` loại bỏ file không cần thiết (bin/, obj/, .git/) trước khi copy.

```dockerfile
RUN dotnet publish "AuthApi.WebApi/AuthApi.WebApi.csproj" -c Release -o /app
```
- **`dotnet publish`**: Compile code + đóng gói vào thư mục `/app`.
- `-c Release`: Build chế độ Release (tối ưu performance).
- `-o /app`: Xuất ra thư mục `/app`.

#### Phase 2: Runtime (ASP.NET)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
```
- Base image mới: **`aspnet:10.0`**, nhẹ hơn SDK rất nhiều.
- Image này CHỈ có .NET Runtime, không có SDK, compiler, tools phát triển.
- **Dung lượng ~200 MB — giảm 92% so với SDK (2.5 GB)**.

```dockerfile
WORKDIR /app
```
- Đặt thư mục làm việc là `/app`.

```dockerfile
COPY --from=build /app .
```
- **`--from=build`**: Copy file từ stage tên `build` (phase 1) sang stage hiện tại.
- Copy thư mục `/app` từ SDK stage sang runtime stage.
- Kết quả: chỉ có binary + DLLs, không có source code, không có SDK.

```dockerfile
USER $APP_UID
```
- Chuyển user từ `root` sang user không có quyền quản trị.
- `$APP_UID` là biến môi trường mặc định trong .NET image (user `app`, UID 1000).
- **Bảo mật:** Nếu container bị hack, attacker không có quyền root.

```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
```
- **`ENV`**: Đặt biến môi trường.
- `ASPNETCORE_URLS=http://+:8080` bảo ASP.NET lắng nghe trên port 8080.
- Dấu `+` nghĩa là "tất cả địa chỉ IP" (0.0.0.0) — cần thiết để Docker proxy
  có thể forwarding request từ host vào container.

```dockerfile
EXPOSE 8080
```
- **`EXPOSE`**: Khai báo port container sẽ dùng. Không thực sự mở port,
  chỉ là documentation cho người đọc Dockerfile biết app chạy port nào.
- Port thực sự được mở trong `docker-compose.yml` hoặc lệnh `docker run -p`.

```dockerfile
ENTRYPOINT ["dotnet", "AuthApi.WebApi.dll"]
```
- **`ENTRYPOINT`**: Lệnh sẽ chạy khi container start.
- Tương đương gõ `dotnet AuthApi.WebApi.dll` trong terminal.
- Đây là entry point — khi process này chết, container cũng dừng.

### Làm thế nào để tạo ra file này?

```bash
# Bước 1: Tạo file
New-Item -ItemType File -Path "AuthApi.WebApi/Dockerfile"

# Bước 2: Mở bằng editor
notepad AuthApi.WebApi/Dockerfile
# hoặc code AuthApi.WebApi/Dockerfile

# Bước 3: Viết nội dung như trên
# (Dùng tay hoặc copy từ tutorial này)

# Bước 4: Kiểm tra build thử
docker build -f AuthApi.WebApi/Dockerfile -t tnp-api:test .
```

---

## 4. docker-compose.yml — Chi tiết từng dòng

> **Đường dẫn:** `docker-compose.yml` (thư mục gốc dự án)
>
> **Mục đích:** Định nghĩa 3 container chạy cùng lúc: API + SQL Server + Redis.

### Toàn bộ file

```yaml
services:
  api:
    build:
      context: .
      dockerfile: AuthApi.WebApi/Dockerfile
    ports:
      - "${API_PORT:-5000}:8080"
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
      - Frontend__Url=${FRONTEND_URL:-https://localhost:3001}
    restart: unless-stopped

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

volumes:
  sql_data:
  redis_data:
```

### Giải thích từng phần

#### Service `api` — Ứng dụng ASP.NET Core

```yaml
api:
  build:
    context: .
    dockerfile: AuthApi.WebApi/Dockerfile
```
- **`build`**: Docker compose sẽ tự động build image từ Dockerfile.
- **`context: .`**: Thư mục gốc của dự án (nơi docker-compose.yml đặt).
  Docker sẽ gửi file trong thư mục này (và .dockerignore) vào Docker daemon.
- **`dockerfile`**: Đường dẫn đến Dockerfile (tương đối từ context).

```yaml
  ports:
    - "${API_PORT:-5000}:8080"
```
- **`ports`**: Map port từ máy host (máy bạn) vào container.
- Cú pháp: `"host:container"` — request đến `localhost:5000` → container port 8080.
- `${API_PORT:-5000}`: Lấy giá trị từ biến môi trường `API_PORT`. Nếu không có,
  dùng giá trị mặc định `5000`. Cú pháp `${TÊN:-GIÁ_TRỊ_MẶC_ĐỊNH}`.

```yaml
  depends_on:
    sqlserver:
      condition: service_healthy
    redis:
      condition: service_started
```
- **`depends_on`**: Khai báo thứ tự khởi động.
- **`condition: service_healthy`**: Chỉ start `api` khi `sqlserver` pass healthcheck.
- **`condition: service_started`**: Chỉ cần `redis` đã start (không cần healthcheck).
- **Tại sao cần?** Nếu không có, `api` có thể chạy TRƯỚC khi SQL Server sẵn sàng
  nhận kết nối → app crash ngay lập tức.

```yaml
  environment:
    - ASPNETCORE_ENVIRONMENT=Docker
    - ConnectionStrings__Default=Server=sqlserver,1433;Database=AuthDb;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True
    - ConnectionStrings__Redis=redis:6379
    - AppSettings__JwtKey=${JWT_KEY}
    - AppSettings__JwtIssuer=${JWT_ISSUER:-TravelNow}
    - AppSettings__JwtAudience=${JWT_AUDIENCE:-TravelNowApp}
    - Frontend__Url=${FRONTEND_URL:-https://localhost:3001}
```
- **`environment`**: Biến môi trường truyền vào container.
- **Dấu `__` (double underscore)**: Trong ASP.NET Core, `__` là separator cho
  cấu hình lồng nhau. Ví dụ `ConnectionStrings__Default` tương đương
  `Configuration.GetConnectionString("Default")` hoặc JSON path
  `ConnectionStrings:Default`.
- **`Server=sqlserver,1433`**: Tên host là `sqlserver` (tên service) — Docker
  tự động resolve hostname này thành IP của container SQL.
- **`TrustServerCertificate=True`**: Bỏ qua SSL certificate (trong container
  không có cert, phải bật option này).

```yaml
  restart: unless-stopped
```
- Chính sách restart: Tự động restart container nếu nó crash, trừ khi
  bạn chủ động `docker stop`.

#### Service `sqlserver` — SQL Server

```yaml
sqlserver:
  image: mcr.microsoft.com/mssql/server:2022-latest
```
- Dùng image SQL Server 2022 chính thức từ Microsoft.
- Docker tự động pull image từ registry nếu chưa có.

```yaml
  environment:
    - ACCEPT_EULA=Y
    - MSSQL_SA_PASSWORD=${SA_PASSWORD}
```
- **`ACCEPT_EULA=Y`**: Bắt buộc — đồng ý điều khoản Microsoft.
- **`MSSQL_SA_PASSWORD`**: Mật khẩu cho tài khoản `sa`.

```yaml
  volumes:
    - sql_data:/var/opt/mssql
```
- **`volumes`**: Lưu data SQL Server vào volume tên `sql_data`.
- Dù container restart hay bị xóa, data vẫn còn trên ổ cứng.
- `:/var/opt/mssql` là thư mục SQL Server lưu database files.

```yaml
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
```
- **`healthcheck`**: Kiểm tra SQL Server đã sẵn sàng chưa.
- **`test`**: Chạy lệnh SQL đơn giản `SELECT 1` qua tool `sqlcmd`.
- **`start_period: 30s`**: Docker đợi 30 giây trước khi bắt đầu check
  (SQL Server cần thời gian khởi tạo).
- **`retries: 10`**: Thử 10 lần, mỗi lần cách 10s → tổng timeout ~100s.
- Nếu SQL không kịp start, container `api` sẽ đợi tới khi healthcheck pass.

#### Service `redis` — Redis Cache

```yaml
redis:
  image: redis:7-alpine
```
- Image Redis phiên bản 7, bản `alpine` (~5MB) — siêu nhẹ.

```yaml
  healthcheck:
    test: ["CMD", "redis-cli", "ping"]
    interval: 5s
    timeout: 3s
    retries: 5
```
- Kiểm tra Redis bằng lệnh `redis-cli ping` → Redis trả về `PONG`.

#### Volumes

```yaml
volumes:
  sql_data:
  redis_data:
```
- Khai báo 2 volume để Docker quản lý.
- **Named volumes** (có tên) ≠ bind mounts (thư mục thật). Docker tự quyết định
  nơi lưu trên ổ cứng (thường ở `C:\ProgramData\Docker\volumes\`).

### Làm thế nào để tạo ra file này?

```bash
# Tạo file
New-Item -ItemType File -Path "docker-compose.yml"

# Nội dung: copy từ tutorial (hoặc soạn tay)
```

---

## 5. .dockerignore — Tối ưu build context

> **Đường dẫn:** `.dockerignore`
>
> **Mục đích:** Loại bỏ file không cần thiết khỏi build context (giảm dung lượng,
> tăng tốc build, bảo mật).

### Toàn bộ file

```
**/.classpath
**/.dockerignore
**/.env
**/.git
**/.gitignore
**/.project
**/.settings
**/.toolstarget
**/.vs
**/.vscode
**/bin/
**/changelog/
**/docs/
**/node_modules/
**/obj/
**/secrets.yaml
**/TestResults/
Dockerfile*
docker-compose*
**/*.ps1
**/*.md
```

### Giải thích từng dòng

| Pattern | Loại bỏ | Lý do |
|---------|---------|-------|
| `**/.git` | Toàn bộ lịch sử git | Không cần trong image, tránh leak thông tin |
| `**/bin/`, `**/obj/` | Thư mục build output | Docker build từ source, không cần binary cũ |
| `**/docs/` | Tài liệu | Không cần trong runtime |
| `**/.env` | File chứa mật khẩu | **Bảo mật** — không đưa secret vào image |
| `**/*.md`, `**/*.ps1` | Markdown, script | Dành cho dev, không dùng trong container |
| `Dockerfile*` | Các Dockerfile | Build context không cần copy Dockerfile vào image |

**Tác động:**
- Build context từ ~50MB (nếu có đủ docs, .git) giảm xuống ~2MB.
- Build nhanh hơn vì Docker gửi ít file hơn đến Docker daemon.

### Làm thế nào để tạo?

```bash
New-Item -ItemType File -Path ".dockerignore"
```

---

## 6. .env.template — Biến môi trường

> **Đường dẫn:** `.env.template`
>
> **Mục đích:** Mẫu cho file `.env` — nơi chứa mật khẩu và cấu hình.

### Toàn bộ file

```bash
# === SA Password cho SQL Server (bắt buộc phải thay đổi) ===
SA_PASSWORD=YourStrong!Passw0rd

# === JWT Key (bắt buộc phải thay đổi, tối thiểu 32 ký tự) ===
JWT_KEY=YourSuperSecretKeyThatIsAtLeast32CharactersLong!

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

### Giải thích

| Biến | Bắt buộc? | Mục đích |
|------|-----------|----------|
| `SA_PASSWORD` | ✅ | Mật khẩu SQL Server. Phải có ký tự đặc biệt, độ dài > 8 |
| `JWT_KEY` | ✅ | Khóa ký JWT token. Tối thiểu 32 ký tự |
| `JWT_ISSUER` | ❌ | Tên issuer (mặc định TravelNow) |
| `API_PORT` | ❌ | Port API trên máy host (mặc định 5000) |
| `SQL_PORT` | ❌ | Port SQL Server trên máy host (mặc định 1433) |

**Cách dùng:**

```bash
# Bước 1: Copy template
cp .env.template .env

# Bước 2: Edit
notepad .env
# Thay SA_PASSWORD và JWT_KEY

# Bước 3: Docker compose tự động đọc .env
docker-compose up -d
```

**Tại sao không commit `.env`?**
- File `.env` đã có trong `.gitignore`
- Mật khẩu không bao giờ được đưa vào git

### Làm thế nào để tạo?

```bash
New-Item -ItemType File -Path ".env.template"
```

---

## 7. appsettings.Docker.json — Cấu hình Docker

> **Đường dẫn:** `AuthApi.WebApi/appsettings.Docker.json`
>
> **Mục đích:** Cấu hình override khi ứng dụng chạy với environment `Docker`.

### Toàn bộ file

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

### Giải thích

ASP.NET Core có cơ chế **cấu hình theo môi trường**:

```
appsettings.json              ← Base, luôn được load
appsettings.Docker.json       ← Load khi ASPNETCORE_ENVIRONMENT = "Docker"
Environment Variables         ← Load sau cùng, override tất cả
```

Điều này có nghĩa:

| File | Có hiệu lực khi | Mục đích |
|------|----------------|----------|
| `appsettings.json` | Luôn luôn | Cấu hình mặc định |
| `appsettings.Development.json` | `ENV=Development` | Cấu hình local dev |
| `appsettings.Docker.json` | `ENV=Docker` | Cấu hình trong container |
| `appsettings.Production.json` | `ENV=Production` | Cấu hình production |

**`Cookie.Secure: false`** — Trong container, HTTPS thường không được cấu hình
(vì có reverse proxy như Nginx phía trước). Để cookie hoạt động qua HTTP,
phải set `Secure = false`.

Các connection strings và JWT key được override bằng **environment variables**
trong docker-compose.yml, KHÔNG ghi vào appsettings (tránh lộ secret).

### Làm thế nào để tạo?

```bash
New-Item -ItemType File -Path "AuthApi.WebApi/appsettings.Docker.json"
```

---

## 8. Program.cs — Flag --migrate

> **Đường dẫn:** `AuthApi.WebApi/Program.cs`
>
> **Thay đổi:** Thêm 7 dòng code xử lý EF Core migration khi start.

### Code thêm

```csharp
#region Auto migrate (Docker)
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
#endregion
```

### Giải thích từng dòng

```csharp
if (args.Contains("--migrate"))
```
- Kiểm tra xem có argument `--migrate` không.
- Đây là flag được truyền từ docker-compose: `dotnet AuthApi.WebApi.dll --migrate`.

```csharp
using var scope = app.Services.CreateScope();
```
- Tạo một scope riêng để lấy service. Trong ASP.NET Core, service có lifetime
  `Scoped` (như `AppDbContext`) chỉ có thể lấy từ scope.

```csharp
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```
- Lấy `AppDbContext` từ DI container.

```csharp
await db.Database.MigrateAsync();
```
- **`MigrateAsync()`**: Áp dụng tất cả EF Core migrations chưa chạy vào database.
- Tương đương `dotnet ef database update` nhưng chạy trong code,
  không cần EF Core CLI tool.

**Tại sao cần flag riêng?** — Không muốn migrate mỗi lần start (chỉ migrate
khi cần). Trong docker-compose, gọi 2 lần:

```yaml
entrypoint: >
  sh -c "dotnet AuthApi.WebApi.dll --migrate && dotnet AuthApi.WebApi.dll"
```

Lần 1: `--migrate` → chạy migration → exit
Lần 2: không flag → chạy app bình thường

### Cần thêm 2 using directives

```csharp
using AuthApi.Infrastructure.Persistence;    // AppDbContext
using Microsoft.EntityFrameworkCore;          // MigrateAsync()
```

---

## 9. Luồng hoạt động — Từ lệnh đến container chạy

### Toàn cảnh

```
Bạn gõ: docker-compose up -d
         │
         ▼
    ┌───────────────────────────────────────────────────────┐
    │                    Docker Compose                      │
    │                                                        │
    │  1. Đọc docker-compose.yml                             │
    │  2. Đọc .env → nạp biến môi trường                     │
    │  3. Xác định services cần chạy: api, sqlserver, redis  │
    └───────────────────────┬───────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
  ┌──────────┐      ┌──────────────┐     ┌──────────┐
  │   api    │      │  sqlserver   │     │  redis   │
  └────┬─────┘      └──────┬───────┘     └────┬─────┘
       │                   │                  │
       ▼                   ▼                  ▼
  ┌──────────────────────────────────────────────────────┐
  │              Docker Engine (Docker Desktop)            │
  │                                                        │
  │  Pull images nếu chưa có:                              │
  │    ├─ mcr.microsoft.com/dotnet/sdk:10.0 (cho build)    │
  │    ├─ mcr.microsoft.com/mssql/server:2022-latest       │
  │    └─ redis:7-alpine                                   │
  │                                                        │
  │  Build image api (từ Dockerfile):                      │
  │    ├─ Stage 1: SDK → restore → publish                  │
  │    └─ Stage 2: Runtime → copy binary                     │
  │                                                        │
  │  Create named volumes: sql_data, redis_data             │
  └────────────────────────────────────────────────────────┘
       │                   │                  │
       ▼                   ▼                  ▼
  ┌──────────┐      ┌──────────────┐     ┌──────────┐
  │ Start    │      │  Start       │     │  Start   │
  │ redis    │      │  sqlserver   │     │  api     │
  │ (nhanh)  │      │  (chậm ~30s)│     │  (chờ)   │
  └──────────┘      └──────┬───────┘     └────┬─────┘
                           │                  │
                           ▼                  │
                    healthcheck:              │
                    sqlcmd SELECT 1 ──────────┤
                    → pass sau ~30s            │
                                              ▼
                                      ┌──────────────────┐
                                      │  api start        │
                                      │  --migrate        │
                                      │  → EF Migration   │
                                      │  → Seed roles     │
                                      │  → Listen :8080   │
                                      └──────────────────┘
```

### Chi tiết từng bước

#### Bước 1: Docker Compose đọc cấu hình

Docker Compose đọc:
1. `docker-compose.yml` — định nghĩa services
2. `.env` — biến môi trường (SA_PASSWORD, JWT_KEY, ...)
3. `Dockerfile` — cách build image cho service `api`

#### Bước 2: Pull images

Docker kiểm tra local đã có image chưa:
- **Lần đầu**: Download `mssql/server:2022-latest` (~2GB), `redis:7-alpine` (~5MB),
  `dotnet/sdk:10.0` (~2.5GB), `dotnet/aspnet:10.0` (~200MB).
- **Lần sau**: Dùng cache (trừ khi image có version mới).

#### Bước 3: Build image `api`

Docker thực thi Dockerfile từng bước:

```
Step 1: FROM sdk:10.0 AS build           Cache? → Pull image
Step 2: WORKDIR /src                     Cache? → OK
Step 3: COPY *.csproj                    Cache? → Phụ thuộc file csproj
Step 4: RUN dotnet restore               Cache? → Phụ thuộc Step 3
Step 5: COPY . .                         Cache? → Phụ thuộc code
Step 6: RUN dotnet publish               Cache? → Phụ thuộc Step 5
Step 7: FROM aspnet:10.0                 Cache? → Pull image
Step 8: COPY --from=build /app .         Cache? → Phụ thuộc Step 6
...
```

Mỗi step là một **layer**. Layer được cache → nếu file không đổi, step
không chạy lại. Build từ lần 2 chỉ mất ~3-5s.

#### Bước 4: Start containers

Thứ tự do `depends_on` quyết định:

1. **redis** start → healthcheck ping → OK (2s)
2. **sqlserver** start → SQL Server khởi động → healthcheck đợi 30-60s
3. **api** CHỈ start sau khi sqlserver healthcheck PASS

#### Bước 5: api khởi động

1. `ENTRYPOINT ["dotnet", "AuthApi.WebApi.dll"]` — chạy app
2. `Program.cs` kiểm tra `args.Contains("--migrate")`:
   - Trong docker-compose, entrypoint thực tế:
     ```bash
     sh -c "dotnet AuthApi.WebApi.dll --migrate && dotnet AuthApi.WebApi.dll"
     ```
   - Lần 1: `--migrate` → chạy `db.Database.MigrateAsync()` → tạo bảng → exit
   - Lần 2: chạy app bình thường
3. Seed roles (Admin, User)
4. App listen trên `http://+:8080`
5. **Ready!** `http://localhost:5000/swagger`

---

## 10. Cách sử dụng — Từng bước

### Yêu cầu

- **Docker Desktop** (Windows/Mac) hoặc Docker Engine (Linux)
- RAM tối thiểu 2GB (khuyến nghị 4GB)
- Ổ cứng trống ~5GB (cho images + volumes)

### Cài đặt Docker Desktop (Windows)

```bash
# 1. Download từ https://www.docker.com/products/docker-desktop/
# 2. Cài đặt (next → next → finish)
# 3. Mở Docker Desktop → đợi Docker Engine start
# 4. Settings → Resources → Memory → 4096 MB
# 5. Apply & Restart
```

### Chạy ứng dụng

```bash
# 1. Clone repo (nếu chưa có)
git clone https://github.com/anomalyco/tnp-api.git
cd tnp-api

# 2. Tạo file .env từ template
cp .env.template .env

# 3. Edit .env — thay mật khẩu
#    MỞ FILE: notepad .env
#    THAY: SA_PASSWORD=MyNewPass@123
#    THAY: JWT_KEY=Your32CharacterLongSuperSecretKeyForJWT!

# 4. Build image + start containers
docker-compose up -d

# 5. Kiểm tra trạng thái
docker-compose ps

# Kết quả mong đợi:
# NAME                  STATUS
# tnp-api-api-1         Up (healthy)
# tnp-api-sqlserver-1   Up (healthy)
# tnp-api-redis-1       Up (healthy)

# 6. Xem log
docker-compose logs -f api    # Log realtime của API
docker-compose logs sqlserver # Log SQL Server (bỏ -f để xem 1 lần)

# 7. Mở browser → http://localhost:5000/swagger

# 8. Khi code thay đổi — rebuild
docker-compose up -d --build api
```

### Lệnh thường dùng

| Lệnh | Mô tả |
|------|-------|
| `docker-compose up -d` | Build + start (detached mode) |
| `docker-compose up -d --build api` | Rebuild + start chỉ service api |
| `docker-compose down` | Dừng (giữ data) |
| `docker-compose down -v` | Dừng + xóa volumes (reset DB) |
| `docker-compose logs -f` | Xem log realtime |
| `docker-compose ps` | Xem trạng thái |
| `docker-compose restart api` | Restart chỉ api |
| `docker exec -it tnp-api-api-1 sh` | SSH vào container api |

### Xử lý lỗi thường gặp

**Lỗi: "port is already allocated"**
```
Error: Port 5000 is already in use
```
→ Port 5000 trên máy bạn đã có app khác dùng. Sửa `.env`:
```
API_PORT=5001
```

**Lỗi: "Cannot connect to SQL Server"**
→ SQL Server chưa kịp start. Kiểm tra:
```bash
docker-compose logs sqlserver
```
Nếu SQL còn đang khởi tạo → đợi thêm 30s.

**Lỗi: "SA_PASSWORD does not meet complexity requirements"**
→ Mật khẩu phải có:
- Tối thiểu 8 ký tự
- Chữ hoa, chữ thường, số, ký tự đặc biệt

**Lỗi: "Docker Desktop requires WSL2"**
→ Cài WSL2 trước:
```bash
wsl --install          # PowerShell Admin
# Restart máy
# Mở Docker Desktop → Settings → WSL2
```

---

## 11. Câu hỏi thường gặp

### Hỏi: Tôi có cần cài .NET SDK trên máy không?

**Không.** Docker build có SDK riêng trong container. Máy bạn chỉ cần Docker Desktop.
Code vẫn soạn bằng Visual Studio/VS Code bình thường.

### Hỏi: Data SQL Server có bị mất khi tắt container không?

**Không.** Volume `sql_data` giữ data trên ổ cứng. Chỉ mất khi bạn chạy
`docker-compose down -v` (xóa volumes).

### Hỏi: Làm sao để kết nối SSMS vào SQL Server trong container?

Dùng: `localhost,1433` (hoặc port bạn config trong `.env`).

Username: `sa`, Password: như trong `.env`.

### Hỏi: File .env có an toàn không? Có bị commit lên git không?

`.env` đã có trong `.gitignore` — an toàn. Bạn chỉ commit `.env.template`
(không có mật khẩu thật).

### Hỏi: Tôi muốn chạy API riêng không cần Docker?

Vẫn chạy được như cũ: `dotnet run` trong thư mục `AuthApi.WebApi`.
Docker không thay đổi code hiện tại — chỉ thêm file mới.

### Hỏi: Image build có nặng không?

~200MB — chỉ gồm .NET Runtime + binary của bạn. Không có SDK, không có source code.

---

> **Tác giả:** Nguyễn Thanh Tuấn
>
> **Cập nhật:** 09/06/2026
