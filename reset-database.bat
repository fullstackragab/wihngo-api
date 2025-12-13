@echo off
SETLOCAL EnableDelayedExpansion

echo.
echo ========================================
echo   Wihngo - Reset Database Script
echo ========================================
echo.
echo This script will:
echo   1. Drop all tables and data
echo   2. Recreate the schema
echo   3. Run the application to seed data
echo.
echo WARNING: This will delete ALL data!
echo.

set /p confirm="Are you sure you want to continue? (yes/no): "
if /i not "%confirm%"=="yes" (
    echo Operation cancelled.
    pause
    exit /b 0
)

echo.
echo [1/3] Dropping existing schema...
psql -h localhost -U postgres -d postgres -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"

if %ERRORLEVEL% NEQ 0 (
    echo [!] Failed to drop schema
    pause
    exit /b 1
)

echo [+] Schema dropped and recreated
echo.

echo [2/3] Building application...
dotnet build --configuration Debug

if %ERRORLEVEL% NEQ 0 (
    echo [!] Build failed
    pause
    exit /b 1
)

echo [+] Build successful
echo.

echo [3/3] Starting application to create tables and seed data...
echo.
echo Press Ctrl+C after you see "Database seeding completed successfully"
echo.

timeout /t 2 /nobreak >nul
start /wait dotnet run --no-build --launch-profile http

echo.
echo ========================================
echo   Database Reset Complete!
echo ========================================
echo.
echo Connection: Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres
echo.
echo Verify with: psql -h localhost -U postgres -d postgres
echo.
pause
