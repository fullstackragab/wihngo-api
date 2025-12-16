@echo off
REM =============================================
REM Seed Payment Data - Batch Script
REM =============================================

echo ======================================
echo Wihngo Payment Data Seeding
echo ======================================
echo.

set PGHOST=localhost
set PGPORT=5432
set PGDATABASE=postgres
set PGUSER=postgres
set PGPASSWORD=postgres

echo Database: %PGDATABASE%
echo Host: %PGHOST%
echo Port: %PGPORT%
echo.
echo Running payment seed script...
echo.

psql -U %PGUSER% -d %PGDATABASE% -f "Database\seed-payment-data-simple.sql"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ======================================
    echo Payment data seeded successfully!
    echo ======================================
) else (
    echo.
    echo ======================================
    echo Error seeding payment data!
    echo ======================================
    exit /b 1
)

set PGPASSWORD=
