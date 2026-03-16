# Grandmaster Vision - Start All Services
# Run: .\start.ps1 from the GrandmasterVision directory

$ErrorActionPreference = "Stop"
$scriptPath = $PSScriptRoot
if (-not $scriptPath) { $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path }
Set-Location $scriptPath

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Grandmaster Vision - Starting Services" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is available and running
$dockerAvailable = $false
try {
    $null = docker info 2>&1
    if ($LASTEXITCODE -eq 0) { $dockerAvailable = $true }
} catch { }

if ($dockerAvailable) {
    Write-Host "Docker detected. Starting with Docker Compose..." -ForegroundColor Green
    Write-Host "Open http://localhost when ready" -ForegroundColor Yellow
    Write-Host ""
    docker-compose up --build
}
else {
    Write-Host "Docker not running. Starting services manually..." -ForegroundColor Yellow
    Write-Host ""

    # Start Vision Service
    Write-Host "[1/3] Starting Vision Service on port 8000..." -ForegroundColor Green
    $visionPath = Join-Path $scriptPath "src\VisionService"
    $visionProcess = Start-Process -FilePath "cmd.exe" -ArgumentList "/c cd /d `"$visionPath`" && .\venv\Scripts\python.exe main.py" -PassThru -WindowStyle Minimized

    Start-Sleep -Seconds 2

    # Start API
    Write-Host "[2/3] Starting API on port 5000..." -ForegroundColor Green
    $apiPath = Join-Path $scriptPath "src\Backend\GrandmasterVision.Api"
    $apiProcess = Start-Process -FilePath "cmd.exe" -ArgumentList "/c cd /d `"$apiPath`" && dotnet run --urls http://localhost:5000" -PassThru -WindowStyle Minimized

    Start-Sleep -Seconds 5

    # Start Frontend
    Write-Host "[3/3] Starting Frontend on port 5001..." -ForegroundColor Green
    $frontendPath = Join-Path $scriptPath "src\Frontend\GrandmasterVision.Client"

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host " All services starting!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Frontend: https://localhost:5001" -ForegroundColor Yellow
    Write-Host "API:      http://localhost:5000" -ForegroundColor Yellow
    Write-Host "Vision:   http://localhost:8000" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press Ctrl+C to stop, then close other windows" -ForegroundColor Cyan
    Write-Host ""

    Set-Location $frontendPath
    dotnet run --urls "https://localhost:5001;http://localhost:5002"

    # Cleanup when frontend stops
    Write-Host "Stopping services..." -ForegroundColor Yellow
    if ($visionProcess -and !$visionProcess.HasExited) { Stop-Process -Id $visionProcess.Id -Force -ErrorAction SilentlyContinue }
    if ($apiProcess -and !$apiProcess.HasExited) { Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue }
    Write-Host "Done." -ForegroundColor Green
}
