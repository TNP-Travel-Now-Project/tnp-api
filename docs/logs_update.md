# Update Log — 14/06/2026

> Cập nhật cấu hình Docker và tài liệu liên quan đến connection string, health check, config flow.

---

## Mục Lục

| STT | File | Loại | Trạng thái |
|-----|------|------|------------|
| 1 | `AuthApi.WebApi/Dockerfile` | Code | ✅ Đã sửa |
| 2 | `AuthApi.WebApi/appsettings.Docker.json` | Config | ✅ Đã sửa |
| 3 | `AuthApi.WebApi/Properties/launchSettings.json` | Config | ✅ Đã sửa |
| 4 | `AuthApi.WebApi/appsettings.Development.json` | Config | ✅ Đã revert |
| 5 | User Secrets (dotnet user-secrets) | Config | ✅ Đã thêm |
| 6 | `docs/01_BACKEND_ARCHITECTURE.md` | Doc | ✅ Đã sửa |
| 7 | `docs/README.md` | Doc | ✅ Đã sửa |
| 8 | `docs/detail_docs/docker tutorial/02-Dockerfile-MultiStage.md` | Doc | ✅ Đã viết lại |
| 9 | `docs/detail_docs/docker tutorial/03-Docker-Compose-Orchestration.md` | Doc | ✅ Đã sửa |
| 10 | `docs/detail_docs/docker tutorial/05-Health-Checks-Serilog.md` | Doc | ✅ Đã sửa |
| 11 | `docs/guides/docker-tutorial/02-Dockerfile-MultiStage.md` | Doc | ✅ Đồng bộ |
| 12 | `docs/guides/docker-tutorial/03-Docker-Compose-Orchestration.md` | Doc | ✅ Đồng bộ |
| 13 | `docs/guides/docker-tutorial/05-Health-Checks-Serilog.md` | Doc | ✅ Đồng bộ |

---

## Chi Tiết Từng File

### 1. `AuthApi.WebApi/Dockerfile` — Thêm curl

- **Trạng thái:** ✅ Đã sửa
- **Lý do:** Container health check trong docker-compose dùng `curl -f http://localhost:8080/health`. Image base `aspnet:10.0` không có curl mặc định → health check luôn fail → container báo unhealthy.
- **Thay đổi:** Thêm dòng `RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*` vào stage `base`.
- **Liên quan:** docker-compose.yml healthcheck, docs health checks.

---

### 2. `AuthApi.WebApi/appsettings.Docker.json` — Thêm ConnectionStrings

- **Trạng thái:** ✅ Đã sửa
- **Lý do:** Khi chạy container với `ASPNETCORE_ENVIRONMENT=Docker` (standalone, không docker-compose), không có env var `ConnectionStrings__Default` → connectionString = null → app crash với lỗi "Connectstring invalid".
- **Thay đổi:** Thêm section `ConnectionStrings.Default` và `ConnectionStrings.Redis` với giá trị fallback.
- **Lưu ý:** Khi chạy docker-compose, env vars ghi đè giá trị file này (ưu tiên cao hơn).

---

### 3. `AuthApi.WebApi/Properties/launchSettings.json` — Thêm ConnectionStrings env vars cho Docker profile

- **Trạng thái:** ✅ Đã sửa
- **Lý do:** Khi F5 Docker trong VS, container không có connection string → crash. Các env var từ launchSettings.json được inject vào container khi `docker run`.
- **Thay đổi:** Thêm `ConnectionStrings__Default` (dùng `host.docker.internal`) và `ConnectionStrings__Redis` vào profile "Container (Dockerfile)".
- **Lưu ý:** `host.docker.internal` là DNS do Docker tạo, trỏ từ container ra máy Windows host. Bắt buộc vì container API không nằm cùng network docker-compose với SQL/Redis.

---

### 4. `AuthApi.WebApi/appsettings.Development.json` — Không thay đổi (giữ nguyên)

- **Trạng thái:** ✅ Đã revert về trạng thái ban đầu
- **Lý do:** Ban đầu thêm ConnectionStrings vào file này, sau đó quyết định dùng User Secrets cho local dev để tránh commit password lên git.
- **Kết luận:** File sạch, không chứa secret.

---

### 5. User Secrets — Thêm ConnectionStrings cho local dev

- **Trạng thái:** ✅ Đã thêm
- **Lý do:** Cần connection string cho `dotnet run` local (Development). User Secrets không bị commit lên git, an toàn cho secret.
- **Giá trị đã set:**
  - `ConnectionStrings:Default` — `Server=localhost,1433;Database=AuthDb;User Id=sa;Password=Nhonaovay@1;TrustServerCertificate=True`
  - `ConnectionStrings:Redis` — `localhost:6379`
- **Lệnh chạy:**
  ```bash
  dotnet user-secrets set "ConnectionStrings:Default" "..." --project AuthApi.WebApi
  dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379" --project AuthApi.WebApi
  ```

---

### 6. `docs/01_BACKEND_ARCHITECTURE.md` — Cập nhật cấu hình

- **Trạng thái:** ✅ Đã sửa
- **Lý do:** Tài liệu thiếu `appsettings.Docker.json` trong file tree, bảng nguồn cấu hình chưa đầy đủ thứ tự ưu tiên.
- **Thay đổi:**
  - Thêm `appsettings.Docker.json` + chú thích vào file tree WebApi
  - Thêm chú thích cho `launchSettings.json`
  - Mở rộng bảng nguồn cấu hình: thêm thứ tự ưu tiên (thấp→cao), thêm `launchSettings.json` env vars, thêm `appsettings.Docker.json`
  - Thêm bảng flow lấy ConnectionString theo từng cách chạy

---

### 7. `docs/README.md` — Fix link + thêm section

- **Trạng thái:** ✅ Đã sửa
- **Lý do:** Link `changelogs/CHANGELOG_FLOW.md` không tồn tại (file ở `detail_docs/`). Thiếu section Detail Docs.
- **Thay đổi:**
  - Thêm section "Detail Docs" với links đúng
  - Sửa link CHANGELOG_FLOW.md đúng path
  - Xóa link dead

---

### 8. `docs/detail_docs/docker tutorial/02-Dockerfile-MultiStage.md` — Viết lại

- **Trạng thái:** ✅ Đã viết lại
- **Lý do:** Tài liệu cũ chỉ mô tả 2 stages (build + runtime), nhưng Dockerfile thực tế có 4 stages (base, build, publish, final). Thiếu curl, thiếu giải thích stage publish, kích thước image sai (~200MB → ~117MB).
- **Thay đổi:**
  - Sửa diagram từ 2 stages thành 4 stages
  - Thêm giải thích chi tiết từng stage
  - Thêm curl installation vào stage base
  - Cập nhật kích thước image
  - Cập nhật flow tổng thể, rủi ro & giải pháp

---

### 9. `docs/detail_docs/docker tutorial/03-Docker-Compose-Orchestration.md` — Mở rộng

- **Trạng thái:** ✅ Đã sửa
- **Lý do:** Tài liệu thiếu các section về config flow, launchSettings, user-secrets. Phần appsettings.Docker.json cũ chưa có ConnectionStrings.
- **Thay đổi:**
  - Thêm ConnectionStrings vào section appsettings.Docker.json
  - Thêm section 3.9: "Các Chế Độ Chạy Khác Nhau" — so sánh 3 flow lấy connection string
  - Thêm section 3.10: "launchSettings.json — VS Docker Profile"
  - Thêm section 3.11: "User Secrets — Cho Local Dev"
  - Cập nhật rủi ro & giải pháp

---

### 10. `docs/detail_docs/docker tutorial/05-Health-Checks-Serilog.md` — Bổ sung

- **Trạng thái:** ✅ Đã sửa
- **Lý do:** Thiếu lưu ý rằng curl cần được cài trong Dockerfile để health check hoạt động.
- **Thay đổi:** Thêm note + code snippet về curl installation trong Dockerfile.

---

### 11-13. `docs/guides/docker-tutorial/*` — Đồng bộ

- **Trạng thái:** ✅ Đồng bộ từ `docs/detail_docs/docker tutorial/`
- **Lý do:** Các file này là bản sao của `detail_docs/docker tutorial/`, cần update để không bị lệch.
- **Thay đổi:** Copy 3 file đã sửa từ `detail_docs/docker tutorial/` sang.

---

## Tổng Kết

| Loại thay đổi | Số file |
|---------------|---------|
| Code (Dockerfile) | 1 |
| Config (appsettings, launchSettings) | 3 |
| User Secrets (máy local) | 1 |
| Documentation | 9 |
| **Tổng** | **13** |
