@echo off
REM ============================================
REM QUICK FIX - Double click to run
REM ============================================
cls
echo.
echo ============================================
echo   WIHNGO DATABASE FIX
echo ============================================
echo.
echo This will add 3 missing database columns:
echo   1. crypto_payment_requests.confirmations
echo   2. token_configurations.token_address
echo   3. onchain_deposits.metadata
echo.
echo Press any key to continue or Ctrl+C to cancel...
pause >nul

REM Set password
set PGPASSWORD=YOUR_DB_PASSWORD

REM Run migration
echo.
echo Running migration...
echo.

"C:\Program Files\PostgreSQL\18\bin\psql.exe" "postgresql://wihngo@YOUR_DB_HOST:5432/wihngo_kzno?sslmode=require" -f Database\migrations\simple_fix.sql

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo   SUCCESS! COLUMNS ADDED
    echo ============================================
    echo.
    echo What to do next:
    echo   1. Restart your application
    echo   2. Try signup again - should work now!
    echo.
) else (
    echo.
    echo ============================================
    echo   FAILED - See error above
    echo ============================================
    echo.
    echo Troubleshooting:
    echo   - Check internet connection
    echo   - See TROUBLESHOOTING.md for help
    echo.
)

REM Clean up
set PGPASSWORD=

echo.
pause
