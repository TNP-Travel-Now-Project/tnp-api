# Dockerfile — Multi-Stage Build

- **File:** `AuthApi.WebApi/Dockerfile`
- **Phụ thuộc:** `.dockerignore`, `Program.cs` (--migrate flag)
- **Mục tiêu:** Build image .NET nhỏ gọn (~117MB) để chạy trong container

---

## 🎯 Mục Tiêu

| Mục tiêu | Mô tả |
|----------|-------|
| **1. Portable** | Image chạy được trên mọi máy có Docker (Windows/Linux/Mac) |
| **2. Nhỏ gọn** | Runtime image chỉ ~117MB (thay vì ~800MB SDK) |
| **3. An toàn** | Chạy với non-root user (`$APP_UID`), không có SDK trong runtime |
| **4. Build nhanh** | Layer cache optimization — chỉ rebuild khi cần |

---

## ✅ Checklist Chi Tiết

### □ 2.1 Multi-Stage Build — 4 Stages

Dockerfile chia làm **4 stages**, mỗi stage có một vai trò riêng:

```
┌────────────────────────────────────────────────────────────┐
│                      DOCKERFILE (4 stages)                    │
│                                                              │
│  STAGE 1: base                                               │
│  ┌──────────────────────────────────────────────────┐       │
│  │ FROM aspnet:10.0                                  │       │
│  │ RUN apt-get install curl                          │       │
│  │ USER $APP_UID, WORKDIR /app, EXPOSE 8080          │       │
│  │ Kết quả: Runtime image + curl                     │       │
│  └──────────────────────────────────────────────────┘       │
│                                                              │
│  STAGE 2: build                                              │
│  ┌──────────────────────────────────────────────────┐       │
│  │ FROM sdk:10.0                                      │       │
│  │ COPY *.csproj → dotnet restore (cache layer)       │       │
│  │ COPY toàn bộ source → dotnet build → /app/build    │       │
│  └──────────────────────────────────────────────────┘       │
│                                                              │
│  STAGE 3: publish                                            │
│  ┌──────────────────────────────────────────────────┐       │
│  │ FROM build                                         │       │
│  │ dotnet publish → /app/publish (optimized output)   │       │
│  └──────────────────────────────────────────────────┘       │
│                                                              │
│  STAGE 4: final                                              │
│  ┌──────────────────────────────────────────────────┐       │
│  │ FROM base (runtime nhẹ, không SDK)               │       │
│  │ COPY --from=publish /app/publish .                │       │
│  │ ENTRYPOINT ["dotnet", "AuthApi.WebApi.dll"]       │       │
│  └──────────────────────────────────────────────────┘       │
└────────────────────────────────────────────────────────────┘
```

**Tại sao 4 stages thay vì 2?**
- `base` + `final` tách riêng: base cài curl, final dùng base + copy từ publish
- `build` + `publish` tách riêng: build compile code, publish tối ưu output (trim, tree-shake)
- Image cuối (`final`) chỉ chứa runtime + DLL, không SDK, không NuGet cache, không source code

### □ 2.2 Chi Tiết Từng Stage

#### Stage 1: `base` — Runtime + curl

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080
```

- **`RUN apt-get install curl`** — Cần cho health check trong docker-compose (`curl -f http://localhost:8080/health`). Nếu thiếu, container luôn báo unhealthy.
- **`USER $APP_UID`** — Chạy non-root user (do ASP.NET image định nghĩa, thường UID 1000). Tăng bảo mật.
- **`ENV ASPNETCORE_URLS=http://+:8080`** — Ép Kestrel listen trên port 8080 (mọi interface). Port 80/443 cần root privilege.

#### Stage 2: `build` — Compile code

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AuthApi.WebApi/AuthApi.WebApi.csproj", "AuthApi.WebApi/"]
COPY ["AuthApi.Application/AuthApi.Application.csproj", "AuthApi.Application/"]
COPY ["AuthApi.Domain/AuthApi.Domain.csproj", "AuthApi.Domain/"]
COPY ["AuthApi.Infrastructure/AuthApi.Infrastructure.csproj", "AuthApi.Infrastructure/"]
RUN dotnet restore "./AuthApi.WebApi/AuthApi.WebApi.csproj"
COPY . .
WORKDIR "/src/AuthApi.WebApi"
RUN dotnet build "./AuthApi.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build
```

- **Copy .csproj trước + restore** — Docker cache từng layer. Nếu chỉ sửa code `.cs` (không sửa `.csproj`), layer `dotnet restore` được cache → build nhanh hơn (vài giây thay vì 30s).
- **`dotnet build`** — Compile code, output ra `/app/build`.

#### Stage 3: `publish` — Tối ưu output

```dockerfile
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AuthApi.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
```

- **Kế thừa từ `build`** — Đã có source + dependencies, không cần restore/build lại.
- **`dotnet publish`** — Khác `dotnet build` ở chỗ tối ưu output:
  - Trim code chết (tree-shake)
  - Chỉ copy DLL + file runtime cần thiết (không copy `.cs`, `.pdb`, NuGet cache)
  - Output gọn hơn build
- **`/p:UseAppHost=false`** — Không tạo native executable (`.exe`), chỉ tạo DLL. Container dùng `dotnet AuthApi.WebApi.dll`.

#### Stage 4: `final` — Image chạy production

```dockerfile
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthApi.WebApi.dll"]
```

- **`FROM base`** — Dùng runtime image nhẹ (~98MB), bỏ SDK nặng (~800MB).
- **`COPY --from=publish`** — Copy DLL đã publish từ stage `publish` sang.
- **`ENTRYPOINT`** — Lệnh chạy khi container start.

### □ 2.3 Layer Cache Optimization

```dockerfile
# Layer 1: COPY chỉ .csproj files
COPY ["AuthApi.WebApi/AuthApi.WebApi.csproj", "AuthApi.WebApi/"]
COPY ["AuthApi.Application/AuthApi.Application.csproj", "AuthApi.Application/"]
COPY ["AuthApi.Domain/AuthApi.Domain.csproj", "AuthApi.Domain/"]
COPY ["AuthApi.Infrastructure/AuthApi.Infrastructure.csproj", "AuthApi.Infrastructure/"]

# Layer 2: dotnet restore (CACHE HIT nếu csproj không đổi)
RUN dotnet restore "AuthApi.WebApi/AuthApi.WebApi.csproj"

# Layer 3: COPY toàn bộ source code
COPY . .

# Layer 4: dotnet publish
RUN dotnet publish "AuthApi.WebApi/AuthApi.WebApi.csproj" -c Release -o /app
```

**Layer cache hoạt động thế nào?**
- Docker build từng layer, mỗi layer là 1 instruction
- Nếu nội dung layer không đổi → Docker dùng cache (CACHE HIT)
- Nếu layer thay đổi → Docker rebuild từ layer đó trở xuống

**Scenario 1: Chỉ sửa code .cs**
- Layer 1-2: CACHE HIT (csproj không đổi)
- Layer 3-4: Rebuild
- **Thời gian:** ~3 giây

**Scenario 2: Thêm/thay đổi NuGet package**
- Layer 1: CACHE HIT
- Layer 2: CACHE MISS (csproj thay đổi) → restore lại
- Layer 3-4: Rebuild
- **Thời gian:** ~30 giây

**Nếu không tách csproj:**
- Mỗi lần sửa code → rebuild cả restore → ~30 giây mỗi lần

### □ 2.4 Security — Non-Root User

```dockerfile
USER $APP_UID
```

- `$APP_UID` là biến môi trường do ASP.NET image định nghĩa (thường là 1000)
- Container chạy với non-root user (không phải root)
- **Nếu thiếu:** Container chạy với quyền root → nếu hacker chiếm được container, có toàn quyền trên máy host

### □ 2.5 Runtime Configuration

```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
EXPOSE 8081
ENTRYPOINT ["dotnet", "AuthApi.WebApi.dll"]
```

| Instruction | Tác dụng |
|-------------|----------|
| `ENV ASPNETCORE_URLS` | Kestrel listen trên port 8080 (mọi interface) |
| `EXPOSE 8080 / 8081` | Document port cho Docker (optional) |
| `ENTRYPOINT` | Lệnh chạy khi container start |

**Tại sao port 8080?**
- Port mặc định của ASP.NET trong container
- Port 80/443 cần root privilege
- docker-compose map `${API_PORT:-5000}:8080` → ngoài là 5000, trong là 8080

**Nếu thiếu ASPNETCORE_URLS:**
- Kestrel dùng port mặc định (5000/5001)
- Container không map được đúng port → không truy cập được API từ host

### □ 2.6 .dockerignore — Build Context

File `.dockerignore` loại bỏ file không cần thiết khỏi build context:

```
**/.git          # Git history — không cần khi build
**/bin/          # Build artifacts — không cần
**/obj/          # Object files — không cần
**/docs/         # Documentation — không cần
**/.env          # Secrets — TRÁNH LỘ
**/*.md          # Markdown — không cần
Dockerfile*      # Docker files — không cần
docker-compose*  # Docker Compose — không cần
```

**Nếu thiếu .dockerignore:**
- Build context gửi lên Docker Daemon sẽ rất lớn (cả .git, node_modules → 1GB+)
- Build chậm hơn nhiều lần
- **Nguy hiểm:** `.env` chứa mật khẩu sẽ bị copy vào image → lộ secrets

---

## 🔄 Flow Tổng Thể

```
docker build -f AuthApi.WebApi/Dockerfile -t tnp-api:latest .
    │
    ▼
Docker Daemon nhận build context (sau khi filter bởi .dockerignore)
    │
    ▼
STAGE 1: base
    ├─ FROM aspnet:10.0
    ├─ RUN apt-get install curl
    ├─ USER $APP_UID
    ├─ ENV ASPNETCORE_URLS=http://+:8080
    │
    ▼
STAGE 2: build
    ├─ FROM sdk:10.0
    ├─ WORKDIR /src
    ├─ COPY csproj → RESTORE (layer cache)
    ├─ COPY source → BUILD → /app/build
    │
    ▼
STAGE 3: publish
    ├─ FROM build
    ├─ dotnet publish → /app/publish
    │
    ▼
STAGE 4: final
    ├─ FROM base (runtime, có curl)
    ├─ COPY --from=publish /app/publish .
    └─ ENTRYPOINT ["dotnet", "AuthApi.WebApi.dll"]
    │
    ▼
Image sẵn sàng: tnp-api (117MB)
```

---

## ⚠️ Rủi Ro & Giải Pháp

| Rủi ro | Giải pháp |
|--------|-----------|
| **Image quá lớn** | Multi-stage + chỉ copy DLL cần thiết (publish) |
| **Cache miss thường xuyên** | Copy csproj trước, code sau |
| **Lộ secrets trong image** | `.dockerignore` + `.env` (gitignored) + environment variables runtime |
| **Permission denied** | `USER $APP_UID` — nếu image không hỗ trợ thì dùng root tạm |
| **Health check luôn unhealthy** | Cài `curl` trong base stage (`RUN apt-get install -y curl`) |
| **NuGet restore fail** | Kiểm tra kết nối mạng trong CI, dùng `--no-cache` nếu cần |
