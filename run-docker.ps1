<#
.SYNOPSIS
    Grandmaster Vision - Docker Runner
.DESCRIPTION
    Runs all services using Docker Compose with proper cleanup options.
.PARAMETER Rebuild
    Force rebuild all images without cache
.PARAMETER Clean
    Full cleanup: remove containers, volumes, and images before starting
.EXAMPLE
    .\run-docker.ps1
    .\run-docker.ps1 -Rebuild
    .\run-docker.ps1 -Clean
#>

param(
    [switch]$Rebuild,
    [switch]$Clean
)

$ErrorActionPreference = "Continue"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Grandmaster Vision - Docker Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check Docker
$dockerInstalled = Get-Command docker -ErrorAction SilentlyContinue
if (-not $dockerInstalled) {
    Write-Host "[ERROR] Docker not found. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check Docker daemon
try {
    $null = docker info 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Docker not running"
    }
} catch {
    Write-Host "[ERROR] Docker daemon is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Clean mode
if ($Clean) {
    Write-Host "[CLEAN] Performing full cleanup..." -ForegroundColor Yellow
    Write-Host "        Stopping containers..." -ForegroundColor Gray
    docker-compose down --volumes --remove-orphans 2>$null

    Write-Host "        Removing local images..." -ForegroundColor Gray
    docker-compose down --rmi local 2>$null

    Write-Host "        Pruning dangling images..." -ForegroundColor Gray
    docker image prune -f

    Write-Host "[CLEAN] Cleanup complete!" -ForegroundColor Green
    Write-Host ""
}

# Rebuild mode
if ($Rebuild) {
    Write-Host "[BUILD] Rebuilding all images (no cache)..." -ForegroundColor Yellow
    docker-compose build --no-cache
    Write-Host "[BUILD] Build complete!" -ForegroundColor Green
    Write-Host ""
}

Write-Host "[START] Starting services..." -ForegroundColor Green
Write-Host ""
Write-Host "  Options:" -ForegroundColor Gray
Write-Host "    -Rebuild  : Force rebuild images" -ForegroundColor Gray
Write-Host "    -Clean    : Full cleanup before start" -ForegroundColor Gray
Write-Host ""
Write-Host "  Access the application at:" -ForegroundColor White
Write-Host "    " -NoNewline; Write-Host "http://localhost" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Press Ctrl+C to stop all services." -ForegroundColor Cyan
Write-Host ""

# Start services
docker-compose up --build

Write-Host ""
Write-Host "[DONE] All services stopped." -ForegroundColor Green
