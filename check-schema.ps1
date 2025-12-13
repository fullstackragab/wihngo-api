# ============================================
# Check Database Schema
# ============================================
# Quick check to see if missing columns exist

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DATABASE SCHEMA CHECKER" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Database connection string
$DB_HOST = "***REMOVED***"
$DB_PORT = "5432"
$DB_NAME = "wihngo_kzno"
$DB_USER = "wihngo"
$DB_PASSWORD = "***REMOVED***"

$env:PGPASSWORD = $DB_PASSWORD

Write-Host "? Checking database: $DB_NAME" -ForegroundColor Yellow
Write-Host ""

# Run verification
$verifyFile = "Database\migrations\verify_schema.sql"

if (-Not (Test-Path $verifyFile)) {
    Write-Host "? Verification file not found: $verifyFile" -ForegroundColor Red
    exit 1
}

psql "postgresql://$DB_USER@$DB_HOST`:$DB_PORT/$DB_NAME?sslmode=require" -f $verifyFile

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "To fix any missing columns, run:" -ForegroundColor Yellow
    Write-Host "  .\run-fix-migration.ps1" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "? Could not connect to database" -ForegroundColor Red
    Write-Host ""
}

# Clean up
$env:PGPASSWORD = ""
