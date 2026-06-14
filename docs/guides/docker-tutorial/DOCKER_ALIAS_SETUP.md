# Hướng Dẫn Tạo Alias Docker Cho Dự Án TNP API

## Vấn Đề

Các lệnh Docker Compose hàng ngày rất dài và khó nhớ:

```powershell
docker-compose up -d --build api
docker-compose logs -f api
docker-compose ps
docker-compose down
```

## Giải Pháp

Tạo **2 bước**: file helper `.ps1` + alias trong PowerShell profile.

---

## Bước 1: Tạo File Helper (`docker-helper.ps1`)

**File:** `docker-helper.ps1` (đặt tại thư mục gốc dự án)

### Nội dung cốt lõi:

```powershell
param(
    [Parameter(Mandatory=$true, Position=0)]
    [ValidateSet('up', 'up-build', 'up-build-api', 'down', 'down-v',
                 'logs', 'logs-api', 'ps', 'restart-api', 'exec-api',
                 'pull', 'clean', 'help')]
    [string]$Command
)

$composeFile = "$PSScriptRoot\docker-compose.yml"

function RunCompose {
    param([string[]]$Args)
    docker-compose -f $composeFile @Args
}

switch ($Command) {
    'up-build-api' { RunCompose up -d --build api; RunCompose ps }
    'logs-api'     { RunCompose logs -f api }
    'ps'           { RunCompose ps }
    'down'         { RunCompose down }
    # ... các command khác
}
```

### Giải thích:

| Thành phần | Mục đích |
|------------|----------|
| `param([ValidateSet])` | Chỉ cho phép nhập đúng tên command, tự động gợi ý |
| `$composeFile` | Đường dẫn tuyệt đối tới `docker-compose.yml` |
| `RunCompose` | Wrapper gọi `docker-compose -f <file>` tránh lặp |
| `switch ($Command)` | Map tên ngắn → lệnh docker-compose thật |

### Cách dùng (trực tiếp):

```powershell
.\docker-helper.ps1 up-build-api
.\docker-helper.ps1 logs-api
```

---

## Bước 2: Tạo Alias Trong PowerShell Profile

### 2.1. Xác định vị trí profile

```powershell
echo $PROFILE
# Output: C:\Users\<User>\OneDrive\Documents\PowerShell\Microsoft.PowerShell_profile.ps1
```

### 2.2. Kiểm tra nội dung hiện tại

```powershell
Get-Content $PROFILE
# Output: oh-my-posh init pwsh ...
```

### 2.3. Thêm alias

Thêm dòng sau vào cuối file `$PROFILE`:

```powershell
function dk { & "C:\Users\<User>\source\repos\tnp-api\docker-helper.ps1" @args }
```

### 2.4. Kích hoạt alias

```powershell
. $PROFILE
```

**Lưu ý:** Chỉ cần làm 1 lần. Mỗi lần mở terminal mới, alias tự động có sẵn.

---

## Kết Quả

| Lệnh gốc | Lệnh mới |
|----------|----------|
| `docker-compose up -d --build api` | `dk up-build-api` |
| `docker-compose logs -f api` | `dk logs-api` |
| `docker-compose ps` | `dk ps` |
| `docker-compose down` | `dk down` |
| `docker-compose down -v` | `dk down-v` |
| `docker-compose exec api sh` | `dk exec-api` |

---

## Cấu Trúc Code & Luồng Thực Thi

### File structure:

```
tnp-api/
├── docker-helper.ps1          # Helper script (gọi docker-compose)
├── docker-compose.yml         # Docker Compose config
└── .env                       # Environment variables

PowerShell Profile:
└── Microsoft.PowerShell_profile.ps1   # Chứa alias 'dk'
```

### Luồng từ gõ lệnh đến container:

```
User gõ: dk up-build-api
         │
         ▼
PowerShell profile tìm function 'dk'
         │
         ▼
Gọi: .\docker-helper.ps1 up-build-api
         │
         ▼
docker-helper.ps1:
  param($Command = 'up-build-api')
         │
         ▼
  switch: 'up-build-api'
         │
         ▼
  RunCompose up -d --build api
         │
         ▼
docker-compose up -d --build api
         │
         ├─ Build image từ Dockerfile
         ├─ Start api container
         └─ docker-compose ps (kiểm tra status)
```

---

## Lợi Ích

1. **Gõ nhanh hơn** - `dk up-build-api` thay vì `docker-compose up -d --build api`
2. **Không sợ sai** - `ValidateSet` chỉ cho phép đúng command, có auto-complete
3. **Bảo vệ dữ liệu** - `down-v` có confirm trước khi xóa volume
4. **Nhất quán** - Cả team dùng chung 1 file helper
5. **Dễ mở rộng** - Thêm command mới chỉ cần sửa `docker-helper.ps1`

---

## Lưu Ý

- **Windows**: Dùng PowerShell 7+ (pwsh) cho `ValidateSet` hoạt động tốt
- **Profile path**: `$PROFILE` trỏ đến `Documents\PowerShell\Microsoft.PowerShell_profile.ps1`
- **Tự động có**: Alias tồn tại vĩnh viễn, không cần setup lại khi mở terminal mới
- **Multi-project**: Có thể tạo nhiều alias `dk-auth`, `dk-blog`,... cho các project khác
