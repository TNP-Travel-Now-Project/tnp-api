# CI/CD với GitHub Actions

- **File:** `.github/workflows/ci.yml`
- **Mục tiêu:** Tự động build + test mỗi khi có push hoặc pull request, đảm bảo code không bị lỗi trước khi merge.

---

## 🎯 Mục Tiêu

| Mục tiêu | Mô tả |
|----------|-------|
| **1. Quality Gate** | Không cho merge nếu build/test fail |
| **2. Self-documenting** | Pipeline là tài liệu sống cho team |
| **3. Trust** | Dev biết chắc code mình không phá vỡ hệ thống |

---

## ✅ Checklist Chi Tiết

### □ 1.1 Tạo file `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [dev]
  pull_request:
    branches: [dev, main]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - run: dotnet restore

      - run: dotnet build --no-restore -c Release

      - run: dotnet test --no-build -c Release --logger trx

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: "**/TestResults/**"
```

---

### □ 1.2 Giải Thích Từng Bước

#### Step: `actions/checkout@v4`
- **Tác dụng:** Lấy code từ repository về GitHub Actions runner
- **Nếu thiếu:** Runner không có code để build

#### Step: `actions/setup-dotnet@v4`
- **Tác dụng:** Cài .NET SDK 10.0.x lên runner
- **Nếu thiếu:** Lệnh `dotnet build` không chạy được
- **Lưu ý:** GitHub Actions runner mặc định không có .NET SDK

#### Step: `dotnet restore`
- **Tác dụng:** Khôi phục NuGet packages
- **Nếu thiếu:** Build sẽ lỗi vì thiếu dependencies

#### Step: `dotnet build --no-restore -c Release`
- **Tác dụng:** Build toàn bộ solution ở chế độ Release
- **Cờ `--no-restore`:** Tiết kiệm thời gian (đã restore ở bước trước)
- **Nếu thiếu:** Không phát hiện lỗi compile trước khi merge

#### Step: `dotnet test --no-build -c Release --logger trx`
- **Tác dụng:** Chạy unit tests (5 tests hiện tại)
- **Cờ `--trx`:** Xuất kết quả test ra file XML để upload artifact
- **Nếu thiếu:** Code sai logic nhưng vẫn merge được
- **Lưu ý:** Chỉ chạy unit test (Moq) — không cần SQL Server hay Redis trong CI

#### Step: `actions/upload-artifact@v4`
- **Tác dụng:** Upload file `.trx` (test results) lên GitHub
- **Cờ `if: always()`:** Upload kể cả khi test fail (để phân tích)
- **Nếu thiếu:** Không xem được chi tiết test fail trên GitHub UI

---

### □ 1.3 Trigger Events

| Event | Branches | Mục đích |
|-------|----------|----------|
| `push` | `dev` | Build mỗi khi dev push code |
| `pull_request` | `dev`, `main` | Kiểm tra PR trước khi merge |

**Tại sao không push thẳng vào `main`?**
- Main là production branch — chỉ merge qua PR sau khi CI pass
- Tránh deploy code lỗi lên production

---

## 🔄 Toàn Bộ Flow

```
Dev push code lên GitHub
    │
    ▼
GitHub Actions trigger
    │
    ▼
runs-on: ubuntu-latest
    │
    ├─ Checkout code            (actions/checkout@v4)
    ├─ Setup .NET 10.0 SDK      (actions/setup-dotnet@v4)
    ├─ dotnet restore           (khôi phục packages)
    ├─ dotnet build Release     (0 errors ✅)
    ├─ dotnet test Release      (5/5 passed ✅)
    └─ Upload test results      (.trx artifact)
    │
    ▼
Kết quả hiện trên GitHub:
  ✅ Build passed
  ✅ Tests passed
  → Sẵn sàng merge PR
```

---

## ⚠️ Rủi Ro & Giải Pháp

| Rủi ro | Giải pháp |
|--------|-----------|
| **Chi phí GitHub Actions** | Free tier đủ cho solo project (2000 phút/tháng) |
| **Integration tests không chạy được** | Chỉ chạy unit test (Moq); integration test cần Docker riêng |
| **.NET 10.0 SDK chưa có sẵn** | `setup-dotnet@v4` tự động cài |
| **Test flaky (không ổn định)** | Unit test dùng Moq → deterministic |
