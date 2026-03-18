@echo off
setlocal EnableDelayedExpansion

:: ============================================================
:: Grandmaster Vision - Docker Runner
:: Runs all services using Docker Compose
:: Usage: run-docker.bat [--rebuild] [--clean]
:: ============================================================

title Grandmaster Vision - Docker

echo.
echo ========================================
echo   Grandmaster Vision - Docker Runner
echo ========================================
echo.

cd /d "%~dp0"

:: Parse arguments
set REBUILD=0
set CLEAN=0

:parse_args
if "%~1"=="" goto after_args
if /i "%~1"=="--rebuild" set REBUILD=1
if /i "%~1"=="--clean" set CLEAN=1
if /i "%~1"=="-r" set REBUILD=1
if /i "%~1"=="-c" set CLEAN=1
shift
goto parse_args
:after_args

:: Check Docker
where docker >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker not found. Please install Docker Desktop.
    pause
    exit /b 1
)

:: Check if Docker daemon is running
docker info >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker daemon is not running. Please start Docker Desktop.
    pause
    exit /b 1
)

:: Clean mode - full cleanup
if %CLEAN%==1 (
    echo [CLEAN] Stopping and removing all containers...
    docker-compose down --volumes --remove-orphans 2>nul
    echo [CLEAN] Removing images...
    docker-compose down --rmi local 2>nul
    echo [CLEAN] Pruning dangling images...
    docker image prune -f
    echo [CLEAN] Cleanup complete!
    echo.
)

:: Rebuild mode
if %REBUILD%==1 (
    echo [BUILD] Rebuilding all images (no cache)...
    docker-compose build --no-cache
    echo.
)

echo [START] Starting services...
echo.
echo   Options:
echo     --rebuild, -r  : Force rebuild images
echo     --clean, -c    : Full cleanup before start
echo.
echo   Access at: http://localhost
echo.
echo   Press Ctrl+C to stop all services.
echo.

docker-compose up --build

echo.
echo [INFO] Services stopped.
pause
