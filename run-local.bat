@echo off
setlocal EnableDelayedExpansion

:: ============================================================
:: Grandmaster Vision - Local Development Runner
:: Runs all services (API, Vision, Frontend) without Docker
:: Usage: run-local.bat
:: ============================================================

title Grandmaster Vision - Local Development

echo.
echo ========================================
echo   Grandmaster Vision - Local Runner
echo ========================================
echo.

:: Check .NET SDK
where dotnet >nul 2>&1
if errorlevel 1 (
    echo [ERROR] .NET SDK not found. Please install from https://dot.net
    pause
    exit /b 1
)

:: Check Python
where python >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Python not found. Please install Python 3.10+
    pause
    exit /b 1
)

cd /d "%~dp0"

echo [1/4] Setting up Python virtual environment...
if not exist "src\VisionService\venv" (
    echo       Creating virtual environment...
    python -m venv src\VisionService\venv
)

:: Activate venv and install requirements
call src\VisionService\venv\Scripts\activate.bat
pip install -q -r src\VisionService\requirements.txt

echo [2/4] Starting Vision Service on port 8000...
start "Vision Service" cmd /c "cd src\VisionService && venv\Scripts\python.exe main.py"
timeout /t 3 /nobreak >nul

echo [3/4] Starting API on port 5000...
start "API Server" cmd /c "cd src\Backend\GrandmasterVision.Api && dotnet run --urls http://localhost:5000"
timeout /t 5 /nobreak >nul

echo [4/4] Starting Frontend on port 5001...
echo.
echo ========================================
echo   Services Starting:
echo ========================================
echo   Frontend:  https://localhost:5001
echo   API:       http://localhost:5000
echo   Vision:    http://localhost:8000
echo ========================================
echo.
echo   Press Ctrl+C to stop the frontend.
echo   Close the other windows to stop all.
echo ========================================
echo.

cd src\Frontend\GrandmasterVision.Client
dotnet run --urls "https://localhost:5001;http://localhost:5002"

:: Cleanup message
echo.
echo [INFO] Frontend stopped. Close other terminal windows to stop remaining services.
pause
