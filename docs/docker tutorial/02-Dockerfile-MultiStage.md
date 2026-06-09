# Dockerfile — Multi-Stage Build

- **File:** `AuthApi.WebApi/Dockerfile`
- **Phụ thuộc:** `.dockerignore`, `Program.cs` (--migrate flag)
- **Mục tiêu:** Build image .NET nhỏ gọn (~200MB) để chạy trong container

---

## 🎯 Mục Tiêu

| Mục tiêu | Mô tả |
|----------|-------|
| **1. Portable** | Image chạy được trên mọi máy có Docker (Windows/Linux/Mac) |
| **2. Nhỏ gọn** | Runtime image chỉ ~200MB (thay vì 2.5GB SDK) |
| **3. An toàn** | Chạy với non-root user (`$APP_UID`), không có SDK trong runtime |
| **4. Build nhanh** | Layer cache optimization — chỉ rebuild khi cần |

---

## ✅ Checklist Chi Tiết

### □ 2.1 Multi-Stage Build — Tại Sao?

```
┌─────────────────────────────────────────────────────┐
│                  DOCKERFILE                            │
│                                                       │
│  STAGE 1: BUILD (SDK)                                 │
│  ┌─────────────────────────────────────────────┐     │
│  │ Base: mcr.microsoft.com/dotnet/sdk:10.0     │     │
│  │ Size: ~2.5 GB                               │     │
│  │ Công cụ: dotnet restore, build, publish     │     │
│  │ Kết quả: /app (published DLLs)              │     │
│  └─────────────────────────────────────────────┘     │
│                        │                              │
│                        ▼ COPY --from=build /app .     │
│                        │                              │
│  STAGE 2: RUNTIME (ASP.NET)                           │
│  ┌─────────────────────────────────────────────┐     │
│  │ Base: mcr.microsoft.com/dotnet/aspnet:10.0  │     │
│  │ Size: ~200 MB                               │     │
│  │ Chỉ có: runtime, không có SDK               │     │
│  │ Bảo mật: Non-root user, ít attack surface   │     │
│  └─────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────┘
```

**Nếu không dùng multi-stage:**
- Image sẽ chứa SDK (~2.5GB) → chậm khi pull/push/deploy
- Chứa compiler, header files → tăng attack surface cho hacker
- Tốn disk space, băng thông

### □ 2.2 Layer Cache Optimization

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

### □ 2.3 Security — Non-Root User

```dockerfile
USER $APP_UID
```

- `$APP_UID` là biến môi trường do ASP.NET image định nghĩa (thường là 1000)
- Container chạy với non-root user (không phải root)
- **Nếu thiếu:** Container chạy với quyền root → nếu hacker chiếm được container, có toàn quyền trên máy host

### □ 2.4 Runtime Configuration

```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AuthApi.WebApi.dll"]
```

| Instruction | Tác dụng |
|-------------|----------|
| `ENV ASPNETCORE_URLS` | Kestrel listen trên port 8080 (mọi interface) |
| `EXPOSE 8080` | Document port cho Docker (optional) |
| `ENTRYPOINT` | Lệnh chạy khi container start |

**Tại sao port 8080?**
- Port mặc định của ASP.NET trong container
- Port 80/443 cần root privilege
- docker-compose map `${API_PORT:-5000}:8080` → ngoài là 5000, trong là 8080

**Nếu thiếu ASPNETCORE_URLS:**
- Kestrel dùng port mặc định (5000/5001)
- Container không map được đúng port → không truy cập được API từ host

### □ 2.5 .dockerignore — Build Context

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
docker build -t tnp-api:latest .
    │
    ▼
Docker Daemon nhận build context (sau khi filter bởi .dockerignore)
    │
    ▼
STAGE 1: BUILD
    │
    ├─ FROM sdk:10.0
    ├─ WORKDIR /src
    ├─ COPY csproj → RESTORE (layer cache)
    ├─ COPY source → PUBLISH → /app
    │
    ▼
STAGE 2: RUNTIME
    │
    ├─ FROM aspnet:10.0
    ├─ COPY --from=build /app .
    ├─ USER $APP_UID (non-root)
    ├─ ENV ASPNETCORE_URLS=http://+:8080
    └─ ENTRYPOINT ["dotnet", "AuthApi.WebApi.dll"]
    │
    ▼
Image sẵn sàng: tnp-api (200MB)
```

---

## ⚠️ Rủi Ro & Giải Pháp

| Rủi ro | Giải pháp |
|--------|-----------|
| **Image quá lớn** | Multi-stage + Alpine (nếu cần tối ưu thêm) |
| **Cache miss thường xuyên** | Copy csproj trước, code sau |
| **Lộ secrets trong image** | `.dockerignore` + `.env` (gitignored) + environment variables runtime |
| **Permission denied** | `USER $APP_UID` — nếu image không hỗ trợ thì dùng root tạm |
| **NuGet restore fail** | Kiểm tra kết nối mạng trong CI, dùng `--no-cache` nếu cần |
