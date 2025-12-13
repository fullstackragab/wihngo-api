@echo off
SETLOCAL EnableDelayedExpansion

echo.
echo ========================================
echo   Wihngo - Database Setup for Docker
echo ========================================
echo.

REM Check if Docker container is running
docker ps --filter "name=postgres" --format "{{.Names}}" | findstr "postgres" >nul
if %ERRORLEVEL% NEQ 0 (
    echo [!] PostgreSQL Docker container is not running
    echo.
    echo Starting the container...
    docker start postgres
    
    if !ERRORLEVEL! NEQ 0 (
        echo [!] Container not found. Creating it now...
        docker run --name postgres ^
            -e POSTGRES_PASSWORD=postgres ^
            -p 5432:5432 ^
            -v postgres_data:/var/lib/postgresql/data ^
            -d postgres
        
        if !ERRORLEVEL! EQU 0 (
            echo [+] PostgreSQL container created successfully
            echo [i] Waiting for PostgreSQL to initialize (8 seconds)...
            timeout /t 8 /nobreak >nul
        ) else (
            echo [!] Failed to create container
            goto :error
        )
    ) else (
        echo [+] Container started
        timeout /t 3 /nobreak >nul
    )
)

echo [1/3] PostgreSQL Docker container is running
echo.

REM Create wihngo database if it doesn't exist
echo [2/3] Creating 'wihngo' database...
docker exec postgres psql -U postgres -c "SELECT 1 FROM pg_database WHERE datname='wihngo';" | findstr "1 row" >nul
if %ERRORLEVEL% EQU 0 (
    echo [i] Database 'wihngo' already exists
) else (
    docker exec postgres psql -U postgres -c "CREATE DATABASE wihngo OWNER postgres;"
    if !ERRORLEVEL! EQU 0 (
        echo [+] Database 'wihngo' created successfully
    ) else (
        echo [!] Failed to create database
        goto :error
    )
)

echo.
echo [3/3] Testing connection...
docker exec postgres psql -U postgres -d wihngo -c "SELECT version();" >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    goto :success
) else (
    echo [!] Could not connect to database
    goto :error
)

:success
echo.
echo ========================================
echo   SUCCESS! Database is ready
echo ========================================
echo.
echo Docker Container:
echo   Name:     postgres
echo   Port:     5432
echo   Volume:   postgres_data
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
echo Your appsettings.Development.json has been updated with this connection.
echo.
echo Next Steps:
echo   1. Restart your application (Stop and press F5)
echo   2. The app will automatically:
echo      - Create all database tables
echo      - Seed initial data (supported tokens)
echo   3. Check console for "Database connection successful"
echo.
echo Docker Management Commands:
echo   - Stop:    docker stop postgres
echo   - Start:   docker start postgres
echo   - Logs:    docker logs postgres
echo   - Connect: docker exec -it postgres psql -U postgres -d wihngo
echo   - Remove:  docker stop postgres ^&^& docker rm postgres
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
echo   1. Docker Desktop is running
echo   2. Port 5432 is not in use by another application
echo   3. Docker has sufficient resources
echo.
echo To manually start the container:
echo   docker run --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -v postgres_data:/var/lib/postgresql/data -d postgres
echo.
pause
exit /b 1
