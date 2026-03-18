<#
.SYNOPSIS
    Grandmaster Vision - Local Development Runner
.DESCRIPTION
    Runs all services (Vision, API, Frontend) locally without Docker.
    Similar to 'npm run dev:all' in Node.js projects.
.EXAMPLE
    .\run-local.ps1
#>

$ErrorActionPreference = "Continue"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Grandmaster Vision - Local Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
$dotnetInstalled = Get-Command dotnet -ErrorAction SilentlyContinue
$pythonInstalled = Get-Command python -ErrorAction SilentlyContinue

if (-not $dotnetInstalled) {
    Write-Host "[ERROR] .NET SDK not found. Install from https://dot.net" -ForegroundColor Red
    exit 1
}

if (-not $pythonInstalled) {
    Write-Host "[ERROR] Python not found. Install Python 3.10+" -ForegroundColor Red
    exit 1
}

# Track started processes
$processes = @()

try {
    # Setup Python virtual environment
    Write-Host "[1/4] Setting up Python environment..." -ForegroundColor Green
    $venvPath = Join-Path $ScriptDir "src\VisionService\venv"
    if (-not (Test-Path $venvPath)) {
        Write-Host "       Creating virtual environment..." -ForegroundColor Yellow
        python -m venv $venvPath
    }

    # Install requirements
    & "$venvPath\Scripts\pip.exe" install -q -r "src\VisionService\requirements.txt" 2>$null

    # Start Vision Service (port 8000)
    Write-Host "[2/4] Starting Vision Service (port 8000)..." -ForegroundColor Green
    $visionProcess = Start-Process -FilePath "$venvPath\Scripts\python.exe" `
        -ArgumentList "main.py" `
        -WorkingDirectory "src\VisionService" `
        -PassThru -WindowStyle Minimized
    $processes += $visionProcess
    Start-Sleep -Seconds 2

    # Start API (port 5000)
    Write-Host "[3/4] Starting API Service (port 5000)..." -ForegroundColor Green
    $apiProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run --urls http://localhost:5000" `
        -WorkingDirectory "src\Backend\GrandmasterVision.Api" `
        -PassThru -WindowStyle Minimized
    $processes += $apiProcess
    Start-Sleep -Seconds 4

    # Start Frontend (port 5001)
    Write-Host "[4/4] Starting Frontend (port 5001)..." -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  All Services Running!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Frontend:  " -NoNewline; Write-Host "https://localhost:5001" -ForegroundColor Yellow
    Write-Host "  API:       " -NoNewline; Write-Host "http://localhost:5000" -ForegroundColor Yellow
    Write-Host "  Vision:    " -NoNewline; Write-Host "http://localhost:8000" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Press Ctrl+C to stop all services." -ForegroundColor Cyan
    Write-Host ""

    # Run frontend in foreground (blocking)
    Set-Location "src\Frontend\GrandmasterVision.Client"
    dotnet run --urls "https://localhost:5001;http://localhost:5002"
}
finally {
    # Cleanup: stop all background processes
    Write-Host ""
    Write-Host "[CLEANUP] Stopping background services..." -ForegroundColor Yellow
    foreach ($proc in $processes) {
        if (-not $proc.HasExited) {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
    }
    Write-Host "[DONE] All services stopped." -ForegroundColor Green
}
