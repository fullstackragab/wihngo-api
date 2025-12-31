@echo off
REM ============================================
REM Fix Missing Database Columns - Using Full Path
REM ============================================
echo.
echo ========================================
echo FIX MISSING DATABASE COLUMNS
echo ========================================
echo.

REM Full path to psql
set PSQL_PATH="C:\Program Files\PostgreSQL\18\bin\psql.exe"

REM Check if psql exists
if not exist %PSQL_PATH% (
    echo ERROR: psql not found at %PSQL_PATH%
    echo.
    pause
    exit /b 1
)

echo Found psql at %PSQL_PATH%
echo.

REM Database connection details
set DB_HOST=YOUR_DB_HOST
set DB_PORT=5432
set DB_NAME=wihngo_kzno
set DB_USER=wihngo
set PGPASSWORD=YOUR_DB_PASSWORD

echo Target Database: %DB_NAME%
echo Host: %DB_HOST%
echo.
echo Errors to be fixed:
echo   - column c.confirmations does not exist
echo   - column t.token_address does not exist
echo   - column o.metadata does not exist
echo.

REM Migration file path
set MIGRATION_FILE=Database\migrations\simple_fix.sql

if not exist %MIGRATION_FILE% (
    echo ERROR: Migration file not found: %MIGRATION_FILE%
    echo Current directory: %CD%
    echo.
    pause
    exit /b 1
)

echo Found migration file: %MIGRATION_FILE%
echo.
echo Press any key to run migration, or Ctrl+C to cancel...
pause >nul

echo.
echo Connecting to database...
echo.

REM Build connection string and execute
%PSQL_PATH% "postgresql://%DB_USER%@%DB_HOST%:%DB_PORT%/%DB_NAME%?sslmode=require" -f %MIGRATION_FILE%

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo SUCCESS - MIGRATION COMPLETED
    echo ========================================
    echo.
    echo Next steps:
    echo   1. Restart your application
    echo   2. Try registering a user again
    echo   3. Background jobs should now work
    echo.
) else (
    echo.
    echo ========================================
    echo ERROR - MIGRATION FAILED
    echo ========================================
    echo.
    echo Exit code: %ERRORLEVEL%
    echo.
)

REM Clean up
set PGPASSWORD=

echo.
echo Press any key to exit...
pause >nul
