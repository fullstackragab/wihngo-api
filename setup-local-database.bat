@echo off
SETLOCAL EnableDelayedExpansion

echo.
echo ========================================
echo   Wihngo - Complete Local Setup
echo ========================================
echo.

REM Check if Docker is installed
where docker >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [1/4] Docker found - checking if running...
    docker ps >nul 2>nul
    if !ERRORLEVEL! EQU 0 (
        echo [2/4] Docker is running
        goto :docker_setup
    ) else (
        echo [!] Docker is installed but not running
        echo [!] Please start Docker Desktop and run this script again
        echo.
        echo Alternatively, press any key to continue with local PostgreSQL setup...
        pause >nul
        goto :check_local_postgres
    )
) else (
    echo [1/4] Docker not found - checking for local PostgreSQL...
    goto :check_local_postgres
)

:docker_setup
echo [3/4] Setting up PostgreSQL in Docker...

REM Check if container already exists
docker ps -a --filter "name=wihngo-postgres" --format "{{.Names}}" | findstr "wihngo-postgres" >nul
if %ERRORLEVEL% EQU 0 (
    echo [i] Container wihngo-postgres already exists
    
    REM Check if it's running
    docker ps --filter "name=wihngo-postgres" --format "{{.Names}}" | findstr "wihngo-postgres" >nul
    if !ERRORLEVEL! EQU 0 (
        echo [i] Container is already running
        goto :test_connection
    ) else (
        echo [i] Starting existing container...
        docker start wihngo-postgres
        if !ERRORLEVEL! EQU 0 (
            echo [+] Container started successfully
            timeout /t 3 /nobreak >nul
            goto :test_connection
        ) else (
            echo [!] Failed to start container
            goto :error
        )
    )
) else (
    echo [i] Creating new PostgreSQL container...
    docker run -d ^
        --name wihngo-postgres ^
        -e POSTGRES_USER=postgres ^
        -e POSTGRES_PASSWORD=postgres ^
        -e POSTGRES_DB=wihngo ^
        -p 5432:5432 ^
        postgres:14
    
    if !ERRORLEVEL! EQU 0 (
        echo [+] PostgreSQL container created successfully
        echo [i] Waiting for PostgreSQL to initialize (5 seconds)...
        timeout /t 5 /nobreak >nul
        goto :test_connection
    ) else (
        echo [!] Failed to create container
        goto :error
    )
)

:check_local_postgres
echo [2/4] Checking for local PostgreSQL installation...

REM Check common PostgreSQL installation paths
set PSQL_PATH=
if exist "C:\Program Files\PostgreSQL\18\bin\psql.exe" set PSQL_PATH=C:\Program Files\PostgreSQL\18\bin\psql.exe
if exist "C:\Program Files\PostgreSQL\17\bin\psql.exe" set PSQL_PATH=C:\Program Files\PostgreSQL\17\bin\psql.exe
if exist "C:\Program Files\PostgreSQL\16\bin\psql.exe" set PSQL_PATH=C:\Program Files\PostgreSQL\16\bin\psql.exe
if exist "C:\Program Files\PostgreSQL\15\bin\psql.exe" set PSQL_PATH=C:\Program Files\PostgreSQL\15\bin\psql.exe
if exist "C:\Program Files\PostgreSQL\14\bin\psql.exe" set PSQL_PATH=C:\Program Files\PostgreSQL\14\bin\psql.exe

if not defined PSQL_PATH (
    echo [!] PostgreSQL not found
    echo.
    echo You have two options:
    echo   1. Install Docker Desktop: https://www.docker.com/products/docker-desktop/
    echo   2. Install PostgreSQL: https://www.postgresql.org/download/windows/
    echo.
    pause
    goto :error
)

echo [+] Found PostgreSQL: %PSQL_PATH%
echo [3/4] Creating database 'wihngo'...

REM Create database if it doesn't exist
set PGPASSWORD=postgres
"%PSQL_PATH%" -U postgres -h localhost -p 5432 -c "SELECT 1 FROM pg_database WHERE datname='wihngo';" | findstr "1 row" >nul
if %ERRORLEVEL% EQU 0 (
    echo [i] Database 'wihngo' already exists
) else (
    "%PSQL_PATH%" -U postgres -h localhost -p 5432 -c "CREATE DATABASE wihngo OWNER postgres;"
    if !ERRORLEVEL! EQU 0 (
        echo [+] Database 'wihngo' created successfully
    ) else (
        echo [!] Failed to create database
        echo [!] Make sure PostgreSQL service is running
        goto :error
    )
)

:test_connection
echo [4/4] Testing database connection...
echo.

REM Try to connect and show version
if defined PSQL_PATH (
    set PGPASSWORD=postgres
    "%PSQL_PATH%" -U postgres -h localhost -p 5432 -d wihngo -c "SELECT version();" >nul 2>nul
    if !ERRORLEVEL! EQU 0 (
        goto :success
    ) else (
        echo [!] Could not connect to database
        goto :error
    )
) else (
    REM Test Docker connection
    docker exec wihngo-postgres psql -U postgres -d wihngo -c "SELECT version();" >nul 2>nul
    if !ERRORLEVEL! EQU 0 (
        goto :success
    ) else (
        echo [!] Could not connect to database
        goto :error
    )
)

:success
echo ========================================
echo   SUCCESS! Database is ready
echo ========================================
echo.
echo Database Details:
echo   Host:     localhost
echo   Port:     5432
echo   Database: wihngo
echo   Username: postgres
echo   Password: postgres
echo.
echo Connection String:
echo   Host=localhost;Port=5432;Database=wihngo;Username=postgres;Password=postgres
echo.
echo Next Steps:
echo   1. Your appsettings.Development.json is already configured
echo   2. Restart your application (Stop and press F5)
echo   3. Check console for "Database connection successful"
echo.
echo Database Management Commands:
if defined PSQL_PATH (
    echo   - Connect: "%PSQL_PATH%" -U postgres -h localhost -d wihngo
) else (
    echo   - Connect: docker exec -it wihngo-postgres psql -U postgres -d wihngo
    echo   - Stop:    docker stop wihngo-postgres
    echo   - Start:   docker start wihngo-postgres
    echo   - Logs:    docker logs wihngo-postgres
)
echo.
pause
exit /b 0

:error
echo.
echo ========================================
echo   Setup Failed
echo ========================================
echo.
echo Please check:
echo   1. Docker Desktop is running (if using Docker)
echo   2. PostgreSQL service is running (if using local install)
echo   3. Port 5432 is not in use by another application
echo.
echo For detailed troubleshooting, see:
echo   - QUICK_START_LOCAL_DATABASE.md
echo   - TROUBLESHOOTING.md
echo.
pause
exit /b 1
