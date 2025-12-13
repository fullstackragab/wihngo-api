@echo off
REM ============================================
REM QUICK AUTH CHECK
REM ============================================
cls
echo.
echo ============================================
echo   QUICK AUTHENTICATION CHECK
echo ============================================
echo.

REM Set password
set PGPASSWORD=adMdetBUaXezA4UQoOfq0Dd6NjCb7XUx

REM Run query
"C:\Program Files\PostgreSQL\18\bin\psql.exe" "postgresql://wihngo@dpg-d4u4tb7gi27c73e5nui0-a.oregon-postgres.render.com:5432/wihngo_kzno?sslmode=require" -c "SELECT user_id, name, email, email_confirmed, created_at, last_login_at, failed_login_attempts, is_account_locked FROM users ORDER BY created_at DESC;"

REM Clean up
set PGPASSWORD=

echo.
pause
