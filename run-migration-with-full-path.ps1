# ============================================
# Fix Missing Database Columns - Enhanced Version
# ============================================
# This script uses the full path to psql to avoid PATH issues

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "FIX MISSING DATABASE COLUMNS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Full path to psql
$psqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

# Check if psql exists
if (-Not (Test-Path $psqlPath)) {
    Write-Host "? psql not found at: $psqlPath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please update the path in this script to match your PostgreSQL installation" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "? Found psql at: $psqlPath" -ForegroundColor Green
Write-Host ""

# Database connection details
$DB_HOST = "YOUR_DB_HOST"
$DB_PORT = "5432"
$DB_NAME = "wihngo_kzno"
$DB_USER = "wihngo"
$DB_PASSWORD = "YOUR_DB_PASSWORD"

Write-Host "? Target Database: $DB_NAME" -ForegroundColor Yellow
Write-Host "? Host: $DB_HOST" -ForegroundColor Yellow
Write-Host ""
Write-Host "Errors to be fixed:" -ForegroundColor Red
Write-Host "  ? column c.confirmations does not exist" -ForegroundColor Red
Write-Host "  ? column t.token_address does not exist" -ForegroundColor Red
Write-Host "  ? column o.metadata does not exist" -ForegroundColor Red
Write-Host ""

# Confirm before proceeding
Write-Host "Press Enter to run migration, or Ctrl+C to cancel..." -ForegroundColor Yellow
Read-Host

Write-Host ""
Write-Host "? Running migration..." -ForegroundColor Cyan
Write-Host ""

# Migration file path
$migrationFile = Join-Path $PSScriptRoot "Database\migrations\fix_missing_columns.sql"

if (-Not (Test-Path $migrationFile)) {
    Write-Host "? Migration file not found: $migrationFile" -ForegroundColor Red
    Write-Host ""
    Write-Host "Current directory: $PSScriptRoot" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "? Found migration file: $migrationFile" -ForegroundColor Green
Write-Host ""

# Set password environment variable
$env:PGPASSWORD = $DB_PASSWORD

# Build connection string
$connectionString = "postgresql://$DB_USER@$DB_HOST`:$DB_PORT/$DB_NAME?sslmode=require"

Write-Host "? Connecting to database..." -ForegroundColor Cyan

# Execute the migration
try {
    & $psqlPath $connectionString -f $migrationFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "? MIGRATION COMPLETED SUCCESSFULLY" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Restart your application" -ForegroundColor White
        Write-Host "  2. Try registering a user again" -ForegroundColor White
        Write-Host "  3. Background jobs should now work without errors" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "? MIGRATION FAILED" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host ""
        Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Yellow
        Write-Host ""
    }
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "? ERROR OCCURRED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}
finally {
    # Clean up password
    $env:PGPASSWORD = ""
}

Write-Host ""
Write-Host "Press Enter to exit..." -ForegroundColor Gray
Read-Host
