# =============================================
# Seed Payment Data Script
# =============================================
# This script populates the Wihngo database with payment test data

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Wihngo Payment Data Seeding" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Database connection details
$env:PGHOST = "localhost"
$env:PGPORT = "5432"
$env:PGDATABASE = "postgres"
$env:PGUSER = "postgres"
$env:PGPASSWORD = "postgres"

# File path
$seedFile = "Database\seed-payment-data-simple.sql"

# Check if seed file exists
if (-not (Test-Path $seedFile)) {
    Write-Host "ERROR: Seed file not found at: $seedFile" -ForegroundColor Red
    Write-Host "Make sure you're running this script from the project root directory." -ForegroundColor Yellow
    exit 1
}

Write-Host "Database: $env:PGDATABASE" -ForegroundColor Green
Write-Host "Host: $env:PGHOST" -ForegroundColor Green
Write-Host "Port: $env:PGPORT" -ForegroundColor Green
Write-Host ""
Write-Host "Running payment seed script..." -ForegroundColor Yellow
Write-Host ""

# Execute the SQL file
try {
    psql -U $env:PGUSER -d $env:PGDATABASE -f $seedFile

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "======================================" -ForegroundColor Green
        Write-Host "Payment data seeded successfully!" -ForegroundColor Green
        Write-Host "======================================" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "======================================" -ForegroundColor Red
        Write-Host "Error seeding payment data!" -ForegroundColor Red
        Write-Host "======================================" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "ERROR: Failed to execute seed script" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Clean up environment variables
Remove-Item Env:\PGPASSWORD
