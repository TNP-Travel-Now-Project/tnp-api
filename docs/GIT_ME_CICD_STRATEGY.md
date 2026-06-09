# Git Branch Strategy — Me & CICD

> **Dự án:** tnp-api (ASP.NET Core 10.0)
> **Cập nhật:** 09/06/2026

---

## Section 1: Me Branch — 4 Commits

**Branch:** `feature/TNP-TuanNT-Me` (base from `dev`)

**Mục tiêu:** Patches 1-4 — CQRS core: IUserContext, Dapper Read, Redis caching, Write Repository, Admin management.

### Commit 1 — `feat(auth): IUserContext + v2 breaking (Role→Roles[])`

**Mô tả:** Interface IUserContext thay ICurrentUserService, DTOs mới với `Roles[]` thay `Role`, cập nhật handlers.

| Action | Files | Ghi chú |
|--------|-------|---------|
| **NEW** | `Application/Common/Security/IUserContext.cs` | Pure abstraction |
| **NEW** | `Application/Common/Security/UnauthorizedException.cs` | 401 handler |
| **NEW** | `Application/Common/Security/ForbiddenException.cs` | 403 handler |
| **NEW** | `Infrastructure/Services/Auth/HttpUserContext.cs` | HTTP impl |
| **NEW** | `Infrastructure/Services/Auth/UserContext.cs` | Test/Background impl |
| **MOD** | `Application/Features/Auth/DTOs/AuthUserDto.cs` | Role→Roles[] |
| **MOD** | `Application/Features/Auth/DTOs/Login/LoginResponse.cs` | role→roles |
| **MOD** | `Application/Features/Users/DTOs/MeResponse.cs` | Role→Roles[] |
| **MOD** | `Application/Features/Auth/DTOs/ForgetPassword/NewPassResponse.cs` | v2 compat |
| **MOD** | `Application/Features/Auth/DTOs/RefreshToken/RefreshTokenResponse.cs` | v2 compat |
| **MOD** | `Application/Abstractions/Interfaces/Auth/ITokenService.cs` | v2 compat |
| **MOD** | `Application/Abstractions/Interfaces/Email/IEmailService.cs` | v2 compat |
| **MOD** | `Application/Features/Auth/Commands/Logout/LogoutCommandHandler.cs` | Use IUserContext |
| **MOD** | `Application/Features/Auth/Commands/Register/RegisterCommand.cs` | Use IUserContext |
| **MOD** | `Domain/Enums/UserRole.cs` | v2 changes |
| **MOD** | `Domain/Factories/UsersFactory.cs` | v2 changes |
| **MOD** | `Infrastructure/Services/Auth/IdentityService.cs` | Use IUserContext |
| **MOD** | `Infrastructure/Services/Token/TokenService.cs` | Roles[] handling |
| **MOD** | `Infrastructure/Persistence/Entities/SeedEntitiesData.cs` | v2 changes |
| **MOD** | `WebApi/Controllers/AuthController.cs` | v2 responses |
| **MOD** | `WebApi/Middlewares/CSRFMiddleware.cs` | Use IUserContext |
| **DEL** | `Application/Abstractions/Interfaces/Auth/ICurrentUserService.cs` | Replaced |
| **DEL** | `Infrastructure/Services/Auth/CurrentUserService.cs` | Replaced |

### Commit 2 — `feat(read): Dapper read repository (thay SqlKata)`

**Mô tả:** IUserReadRepository + Dapper implementation, xoá SqlKata, batch roles pattern.

| Action | Files | Ghi chú |
|--------|-------|---------|
| **NEW** | `Application/Abstractions/Interfaces/Repositories/IUserReadRepository.cs` | Read interface |
| **NEW** | `Infrastructure/Persistence/Dapper/Repositories/UserReadRepository.cs` | Dapper impl |
| **NEW** | `Application/Features/Users/DTOs/UserListItemDto.cs` | string[] Roles |
| **MOD** | `Application/Features/Users/Queries/GetUsers/GetAllUserQuery.cs` | Dapper |
| **MOD** | `Application/Features/Users/Queries/GetUsers/GetAllUserQueryHandler.cs` | Dapper |
| **MOD** | `Infrastructure/Configuration/DependencyInjection.cs` | DI: Dapper |
| **MOD** | `Infrastructure/AuthApi.Infrastructure.csproj` | Dapper package |
| **MOD** | `Application/AuthApi.Application.csproj` | (if needed) |
| **DEL** | `Application/Abstractions/Interfaces/Repositories/IUserQueryRepository.cs` | SqlKata |
| **DEL** | `Infrastructure/Persistence/Repositories/Users/UserQueryRepository.cs` | SqlKata |
| **DEL** | `Application/Features/Users/DTOs/UserDto.cs` | Old DTO |

### Commit 3 — `feat(cache): Redis caching + policy auth + admin read`

**Mô tả:** Cache layer cho Me, policy authorization, endpoint GET /users/{id}, ExceptionMiddleware, tests.

| Action | Files | Ghi chú |
|--------|-------|---------|
| **NEW** | `Application/Abstractions/Interfaces/Cache/ICacheService.cs` | Cache interface |
| **NEW** | `Infrastructure/Services/Cache/RedisCacheService.cs` | Redis impl |
| **NEW** | `Application/Features/Users/DTOs/UserDetailResponse.cs` | Admin DTO |
| **NEW** | `Application/Features/Users/Queries/GetUserById/GetUserByIdQuery.cs` | Query |
| **NEW** | `Application/Features/Users/Queries/GetUserById/GetUserByIdQueryHandler.cs` | Handler |
| **NEW** | `Application/Features/Users/Queries/Me/MeQuery.cs` | Query (nếu chưa có) |
| **NEW** | `AuthApi.Tests/AuthApi.Tests.csproj` | Test project |
| **NEW** | `AuthApi.Tests/Features/Users/Queries/Me/MeQueryHandlerTests.cs` | 5 tests |
| **MOD** | `Application/Common/ErrorCodes.cs` | +Unauthorized, Forbidden |
| **MOD** | `Application/Features/Users/Queries/Me/MeQueryHandler.cs` | +Cache logic |
| **MOD** | `WebApi/Middlewares/ExceptionMiddleware.cs` | 401/403/404 |
| **MOD** | `WebApi/Program.cs` | `git add -p`: Auth Policies hunk |
| **MOD** | `WebApi/Controllers/UserController.cs` | `git add -p`: GET {id} hunk |

### Commit 4 — `feat(write): UserWriteRepository + admin management`

**Mô tả:** True CQRS write side, Dapper-based, admin commands (update, assign/remove roles, soft-delete).

| Action | Files | Ghi chú |
|--------|-------|---------|
| **NEW** | `Application/Abstractions/Interfaces/Repositories/IUserWriteRepository.cs` | Write interface |
| **NEW** | `Infrastructure/Persistence/Dapper/Repositories/UserWriteRepository.cs` | Dapper writes |
| **NEW** | `Application/Features/Users/Commands/UpdateUser/UpdateUserCommand.cs` | Command |
| **NEW** | `Application/Features/Users/Commands/UpdateUser/UpdateUserCommandHandler.cs` | Handler |
| **NEW** | `Application/Features/Users/Commands/UpdateUser/UpdateUserCommandValidator.cs` | Validator |
| **NEW** | `Application/Features/Users/Commands/AssignRoles/AssignRolesCommand.cs` | Command |
| **NEW** | `Application/Features/Users/Commands/AssignRoles/AssignRolesCommandHandler.cs` | Handler |
| **NEW** | `Application/Features/Users/Commands/AssignRoles/AssignRolesCommandValidator.cs` | Validator |
| **NEW** | `Application/Features/Users/Commands/RemoveRoles/RemoveRolesCommand.cs` | Command |
| **NEW** | `Application/Features/Users/Commands/RemoveRoles/RemoveRolesCommandHandler.cs` | Handler |
| **NEW** | `Application/Features/Users/Commands/RemoveRoles/RemoveRolesCommandValidator.cs` | Validator |
| **NEW** | `Application/Features/Users/DTOs/UpdateUserRequest.cs` | DTO |
| **NEW** | `Application/Common/NotFoundException.cs` | 404 exception |
| **MOD** | `WebApi/Controllers/UserController.cs` | `git add -p`: PUT/POST/DELETE hunks |
| **DEL** | `Domain/Interfaces/IUserRepository.cs` | Dead code |
| **DEL** | `Infrastructure/Persistence/Repositories/Users/UserRepository.cs` | Dead code |

### Shared files (xử lý bằng `git add -p`)

| File | Commit 3 hunk | Commit 4 hunk |
|------|---------------|---------------|
| `Program.cs` | `#region config Authorization Policies` | — |
| `UserController.cs` | `GET {id}` endpoint | `PUT /{id}`, `POST/DELETE /{id}/roles`, `DELETE /{id}` |

---

## Section 2: CICD Branch — 1 Commit

**Branch:** `feature/TNP-TuanNT-CICD` (base from `dev`)

**Mục tiêu:** Patch 5 — CI/CD GitHub Actions, Docker hóa, Integration Tests với TestContainers, Health Checks, Serilog logging.

### Commit 5 — `feat(infra): CI/CD + Docker + integration tests + health checks + Serilog`

**Mô tả:** Toàn bộ infrastructure: tự động build/test (CI), container hóa (Docker), test database thật (TestContainers), monitoring (Health Checks), structured logging (Serilog).

#### Nhóm 5.1: GitHub Actions CI/CD

| Action | Files | Mục đích |
|--------|-------|----------|
| **NEW** | `.github/workflows/ci.yml` | Build + test trên push/PR |

#### Nhóm 5.2: Docker + Docker Compose

| Action | Files | Mục đích |
|--------|-------|----------|
| **NEW** | `AuthApi.WebApi/Dockerfile` | Multi-stage build (SDK→Runtime) |
| **NEW** | `docker-compose.yml` | 3 services: API + SQL + Redis |
| **NEW** | `.dockerignore` | Tối ưu build context |
| **NEW** | `.env.template` | Mẫu biến môi trường |
| **NEW** | `AuthApi.WebApi/appsettings.Docker.json` | Config cho môi trường Docker |
| **MOD** | `WebApi/Program.cs` | `git add -p`: `--migrate` flag hunk |

#### Nhóm 5.3: Integration Tests (TestContainers)

| Action | Files | Mục đích |
|--------|-------|----------|
| **NEW** | `AuthApi.IntegrationTests/AuthApi.IntegrationTests.csproj` | Test project |
| **NEW** | `AuthApi.IntegrationTests/Fixtures/SqlServerFixture.cs` | Container lifecycle |
| **NEW** | `AuthApi.IntegrationTests/Repositories/DatabaseSeed.cs` | Seed helper |
| **NEW** | `AuthApi.IntegrationTests/Repositories/UserReadRepositoryTests.cs` | 5 read tests |
| **NEW** | `AuthApi.IntegrationTests/Repositories/UserWriteRepositoryTests.cs` | 6 write tests |
| **MOD** | `Infrastructure/AuthApi.Infrastructure.csproj` | `git add -p`: InternalsVisibleTo |
| **MOD** | `Infrastructure/Persistence/Connections/DbConnectionFactory.cs` | +string constructor |

#### Nhóm 5.4: Health Checks + Serilog

| Action | Files | Mục đích |
|--------|-------|----------|
| **NEW** | `AuthApi.WebApi/HealthChecks/RedisHealthCheck.cs` | Redis IHealthCheck |
| **MOD** | `WebApi/Program.cs` | `git add -p`: Serilog + HealthChecks + /health hunks |
| **MOD** | `WebApi/appsettings.json` | `git add -p`: Serilog config |
| **MOD** | `AuthApi.WebApi/AuthApi.WebApi.csproj` | +HealthChecks, Serilog packages |
| **MOD** | `Infrastructure/Services/Auth/IdentityService.cs` | Console→Log (còn lại) |

#### Nhóm Tài liệu

| Action | Files | Mục đích |
|--------|-------|----------|
| **NEW** | `docs/DOCKER_TUTORIAL.md` | Docker hướng dẫn chi tiết |
| **NEW** | `docs/GIT_ME_CICD_STRATEGY.md` | File này |

---

## Appendix: Thực thi

### Me Branch (hiện tại)

```bash
# Đang ở feature/TNP-TuanNT-Me
git reset HEAD                    # Unstage all

# Commit 1 — IUserContext
git add <P1 files>
git commit -m "feat(auth): IUserContext + v2 breaking (Role→Roles[])"

# Commit 2 — Dapper
git add <P2 files>
git commit -m "feat(read): Dapper read repository (thay SqlKata)"

# Commit 3 — Caching + Policies
git add <P3 files>
git add -p WebApi/Program.cs       # Auth Policies hunk
git add -p WebApi/UserController.cs # GET {id} hunk
git commit -m "feat(cache): Redis caching + policy auth + admin read"

# Commit 4 — Write Repository
git add <P4 files>
git add -p WebApi/UserController.cs # PUT/POST/DELETE hunks
git commit -m "feat(write): UserWriteRepository + admin management"

git push origin feature/TNP-TuanNT-Me
```

### CICD Branch (sau Me)

```bash
git checkout dev
git pull origin dev
git checkout -b feature/TNP-TuanNT-CICD

# Copy files từ Me branch
git checkout feature/TNP-TuanNT-Me -- <all P5 files>

# Stage P5 hunks từ shared files
git add -p WebApi/Program.cs
git add -p WebApi/appsettings.json
git add -p Infrastructure/AuthApi.Infrastructure.csproj

# Stage remaining P5 files
git add <remaining P5 files>

git commit -m "feat(infra): CI/CD + Docker + integration tests + health checks + Serilog"
git push -u origin feature/TNP-TuanNT-CICD
```
