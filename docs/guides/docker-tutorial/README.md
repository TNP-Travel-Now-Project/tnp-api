# Docker Tutorial — Tổng Quan

> **Dự án:** tnp-api (ASP.NET Core 10.0)
> **Tác giả:** Nguyễn Thanh Tuấn
> **Cập nhật:** 09/06/2026 (Patch 5.4)

## 🎯 Mục tiêu

Container hóa toàn bộ ứng dụng (API + SQL Server + Redis), tự động build/test với CI/CD, kiểm tra sức khỏe (health checks), và ghi log có cấu trúc (Serilog).

## 📚 Mục Lục

| # | File | Nội dung |
|---|------|----------|
| 1 | [01-CI-CD-GitHub-Actions](./01-CI-CD-GitHub-Actions.md) | CI/CD pipeline — tự động build + test trên GitHub |
| 2 | [02-Dockerfile-MultiStage](./02-Dockerfile-MultiStage.md) | Docker multi-stage build — từ SDK đến runtime image |
| 3 | [03-Docker-Compose-Orchestration](./03-Docker-Compose-Orchestration.md) | Docker Compose — 3 container (API + SQL + Redis) |
| 4 | [04-Integration-Tests](./04-Integration-Tests.md) | Integration Tests với TestContainers |
| 5 | [05-Health-Checks-Serilog](./05-Health-Checks-Serilog.md) | Health Checks + Serilog logging |

## 🧩 Kiến Trúc Tổng Thể

```
Git Push / PR
    │
    ▼
┌──────────────────────┐
│  GitHub Actions CI   │ ──→ dotnet build + test
└──────────────────────┘
    │
    ▼
┌──────────────────────┐
│  Dockerfile          │  ──→ Multi-stage build (SDK → Runtime)
├──────────────────────┤
│  .dockerignore       │  ──→ Tối ưu build context
├──────────────────────┤
│  .env.template       │  ──→ Biến môi trường (bảo mật)
├──────────────────────┤
│  appsettings.Docker  │  ──→ Cấu hình Docker-specific
└──────────────────────┘
    │
    ▼
┌──────────────────────┐
│  docker-compose.yml  │  ──→ Orchestrate 3 services
│                      │      ├─ api      (.NET 10.0 :8080)
│                      │      ├─ sqlserver (SQL 2022 :1433)
│                      │      └─ redis     (Redis 7 :6379)
│                      │      └─ Healthchecks cho từng service
└──────────────────────┘
    │
    ▼
┌──────────────────────┐
│  Integration Tests   │  ──→ TestContainers (SQL Server thật)
├──────────────────────┤
│  Health Checks       │  ──→ /health check SQL + Redis
├──────────────────────┤
│  Serilog             │  ──→ Structured logging (console + file)
└──────────────────────┘
```

## 🔗 Liên Kết Tài Liệu Gốc

- [DOCKER_TUTORIAL.md](./DOCKER_TUTORIAL.md) — Tutorial Docker chi tiết từng dòng code
- [ME_BRANCH_RESULT.md](../../changelogs/ME_BRANCH_RESULT.md) — Nhật ký thay đổi toàn bộ dự án
