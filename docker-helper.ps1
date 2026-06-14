<#
.SYNOPSIS
    Docker helper script cho TNP API
.DESCRIPTION
    Giản lược docker-compose thành alias ngắn gọn.
    Tên command mirror docker-compose CLI (up-d, up-d--build, ...).
.EXAMPLE
    .\docker-helper.ps1 up-d--build-api    # docker-compose up -d --build api
    .\docker-helper.ps1 logs-api           # docker-compose logs -f api
    .\docker-helper.ps1 ps                 # docker-compose ps
.NOTES
    Alias gõ tắt: dk <command>
    Setup: function dk { & "<path>\docker-helper.ps1" @args } trong $PROFILE
#>

param(
    [Parameter(Mandatory=$true, Position=0)]
    [ValidateSet(
        'up-d', 'up-d--build', 'up-d--build-api',
        'down', 'down-v',
        'logs', 'logs-api',
        'ps',
        'restart-api', 'exec-api',
        'pull', 'clean',
        'help'
    )]
    [string]$Command
)

function RunCompose {
    param([string[]]$DockerArgs)
    Push-Location $PSScriptRoot
    docker-compose @DockerArgs
    Pop-Location
}

switch ($Command) {
    'up-d' {
        Write-Host ">>> docker-compose up -d..." -ForegroundColor Green
        RunCompose @('up', '-d')
        RunCompose ps
    }
    'up-d--build' {
        Write-Host ">>> docker-compose up -d --build..." -ForegroundColor Green
        RunCompose @('up', '-d', '--build')
        RunCompose ps
    }
    'up-d--build-api' {
        Write-Host ">>> docker-compose up -d --build api..." -ForegroundColor Green
        RunCompose @('up', '-d', '--build', 'api')
        RunCompose ps
    }
    'down' {
        Write-Host ">>> docker-compose down..." -ForegroundColor Yellow
        RunCompose down
    }
    'down-v' {
        Write-Host ">>> CẢNH BÁO: docker-compose down -v (mất data)!" -ForegroundColor Red
        $confirm = Read-Host "Chắc chắn? Gõ 'y' để xóa (y/N)"
        if ($confirm -eq 'y') {
            RunCompose @('down', '-v')
        } else {
            Write-Host "Đã hủy." -ForegroundColor Yellow
        }
    }
    'logs' {
        Write-Host ">>> docker-compose logs -f..." -ForegroundColor Cyan
        RunCompose @('logs', '-f')
    }
    'logs-api' {
        Write-Host ">>> docker-compose logs -f api..." -ForegroundColor Cyan
        RunCompose @('logs', '-f', 'api')
    }
    'ps' {
        Write-Host ">>> docker-compose ps:" -ForegroundColor Cyan
        RunCompose ps
    }
    'restart-api' {
        Write-Host ">>> docker-compose restart api..." -ForegroundColor Yellow
        RunCompose restart api
        RunCompose ps
    }
    'exec-api' {
        Write-Host ">>> docker-compose exec api sh..." -ForegroundColor Cyan
        RunCompose exec api sh
    }
    'pull' {
        Write-Host ">>> docker-compose pull..." -ForegroundColor Green
        RunCompose pull
        Write-Host ">>> docker-compose up -d --build api..." -ForegroundColor Green
        RunCompose @('up', '-d', '--build', 'api')
    }
    'clean' {
        Write-Host ">>> docker system prune + docker volume prune..." -ForegroundColor Yellow
        docker system prune -f
        docker volume prune -f
    }
    'help' {
        @"

TNP API Docker Helper
=====================

Cách dùng: .\docker-helper.ps1 <command>

  up-d              docker-compose up -d
  up-d--build       docker-compose up -d --build
  up-d--build-api   docker-compose up -d --build api

  down              docker-compose down
  down-v            docker-compose down -v (kèm confirm)

  logs              docker-compose logs -f
  logs-api          docker-compose logs -f api

  ps                docker-compose ps
  restart-api       docker-compose restart api
  exec-api          docker-compose exec api sh

  pull              docker-compose pull + rebuild api

  clean             docker system prune + docker volume prune
  help              Hiển thị help này

Gõ tắt: dk <command> (setup: function dk trong `$PROFILE)
  dk up-d
  dk up-d--build-api
  dk logs-api
  dk ps

"@ | Write-Host
    }
}
