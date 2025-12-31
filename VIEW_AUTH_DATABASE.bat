@echo off
REM ============================================
REM VIEW AUTHENTICATION DATABASE
REM ============================================
cls
echo.
echo ============================================
echo   AUTHENTICATION DATABASE VIEWER
echo ============================================
echo.
echo This will display:
echo   - Users table schema
echo   - All user accounts
echo   - Authentication status
echo   - Locked accounts
echo   - Unconfirmed emails
echo   - Active tokens
echo   - Recent logins
echo   - Statistics
echo.
echo Press any key to continue or Ctrl+C to cancel...
pause >nul

REM Set password
set PGPASSWORD=YOUR_DB_PASSWORD

REM Run query
echo.
echo Connecting to database...
echo.

"C:\Program Files\PostgreSQL\18\bin\psql.exe" "postgresql://wihngo@YOUR_DB_HOST:5432/wihngo_kzno?sslmode=require" -f Database\queries\view_auth_database.sql

REM Clean up
set PGPASSWORD=

echo.
echo ============================================
echo   REPORT COMPLETE
echo ============================================
echo.
pause
