# ============================================
# Fix Missing Database Columns
# ============================================
# This script runs the migration to add missing columns
# that are causing the application to fail

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "FIX MISSING DATABASE COLUMNS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Database connection string
$DB_HOST = "***REMOVED***"
$DB_PORT = "5432"
$DB_NAME = "wihngo_kzno"
$DB_USER = "wihngo"
$DB_PASSWORD = "***REMOVED***"

$env:PGPASSWORD = $DB_PASSWORD

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

# Run the migration
$migrationFile = "Database\migrations\fix_missing_columns.sql"

if (-Not (Test-Path $migrationFile)) {
    Write-Host "? Migration file not found: $migrationFile" -ForegroundColor Red
    exit 1
}

# Execute the migration
psql "postgresql://$DB_USER@$DB_HOST`:$DB_PORT/$DB_NAME?sslmode=require" -f $migrationFile

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
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Check if psql is installed: psql --version" -ForegroundColor White
    Write-Host "  2. Check database connectivity" -ForegroundColor White
    Write-Host "  3. Verify credentials are correct" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Clean up
$env:PGPASSWORD = ""
