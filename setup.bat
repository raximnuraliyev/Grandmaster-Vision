@echo off
setlocal

REM Get the directory of this script
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

echo ========================================
echo  Grandmaster Vision - Quick Start
echo ========================================
echo.

REM Check prerequisites
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found. Please install .NET SDK
    exit /b 1
)

where python >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Python not found. Please install Python 3.10+
    exit /b 1
)

echo [1/4] Setting up Python Vision Service...
cd /d "%SCRIPT_DIR%src\VisionService"
if not exist venv (
    python -m venv venv
)
call venv\Scripts\activate.bat
pip install -r requirements.txt --quiet 2>nul
echo Vision Service setup complete.

echo.
echo [2/4] Restoring .NET packages...
cd /d "%SCRIPT_DIR%"
dotnet restore GrandmasterVision.slnx --verbosity quiet
echo Packages restored.

echo.
echo [3/4] Building solution...
dotnet build GrandmasterVision.slnx --no-restore --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed. Check errors above.
    exit /b 1
)
echo Build complete.

echo.
echo ========================================
echo  Setup Complete!
echo ========================================
echo.
echo To run with Docker (RECOMMENDED):
echo    cd %SCRIPT_DIR%
echo    docker-compose up --build
echo    Then open: http://localhost
echo.
echo OR run manually (3 PowerShell terminals):
echo.
echo Terminal 1 - Vision Service:
echo    cd %SCRIPT_DIR%src\VisionService
echo    .\venv\Scripts\Activate.ps1
echo    python main.py
echo.
echo Terminal 2 - Backend API:
echo    cd %SCRIPT_DIR%src\Backend\GrandmasterVision.Api
echo    dotnet run
echo.
echo Terminal 3 - Frontend:
echo    cd %SCRIPT_DIR%src\Frontend\GrandmasterVision.Client
echo    dotnet run
echo.
echo Then open: https://localhost:5002
echo.

endlocal
