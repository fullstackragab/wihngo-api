# =============================================
# Execute Database Reset & Seed Script
# =============================================
# This script executes the database-reset-seed.sql file
# on your local PostgreSQL database

Write-Host ""
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "   Wihngo Database Reset & Seed Tool" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Read connection string from appsettings.json
$appsettings = Get-Content -Path "appsettings.json" | ConvertFrom-Json
$connectionString = $appsettings.ConnectionStrings.DefaultConnection

if (-not $connectionString) {
    Write-Host "? Could not read connection string from appsettings.json" -ForegroundColor Red
    exit 1
}

# Parse connection string
$connParts = @{}
$connectionString -split ';' | ForEach-Object {
    if ($_ -match '(.+?)=(.+)') {
        $connParts[$matches[1].Trim()] = $matches[2].Trim()
    }
}

$host_addr = $connParts['Host']
$port = $connParts['Port']
$database = $connParts['Database']
$username = $connParts['Username']
$password = $connParts['Password']

Write-Host "?? Connection Details:" -ForegroundColor Yellow
Write-Host "   Host: $host_addr" -ForegroundColor White
Write-Host "   Port: $port" -ForegroundColor White
Write-Host "   Database: $database" -ForegroundColor White
Write-Host "   User: $username" -ForegroundColor White
Write-Host ""

# Confirm action
Write-Host "??  WARNING: This will DELETE ALL DATA in the database!" -ForegroundColor Red
Write-Host "??  All existing birds, stories, users, etc. will be removed!" -ForegroundColor Red
Write-Host ""
$confirmation = Read-Host "Type 'YES' to continue or 'N' to cancel"

if ($confirmation -ne 'YES') {
    Write-Host "? Operation cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "?? Executing database reset and seed script..." -ForegroundColor Cyan

# Set environment variable for password (psql reads it automatically)
$env:PGPASSWORD = $password

# Execute the SQL script
$scriptPath = Join-Path $PSScriptRoot "database-reset-seed.sql"

try {
    # Run psql command
    $output = & psql -h $host_addr -p $port -U $username -d $database -f $scriptPath 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "? Database reset and seed completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
        Write-Host "   Seeded Data Summary" -ForegroundColor Cyan
        Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
        Write-Host "?? Users: 5" -ForegroundColor White
        Write-Host "?? Birds: 10 (including 1 memorial bird)" -ForegroundColor White
        Write-Host "?? Stories: 20" -ForegroundColor White
        Write-Host "?? Comments: 8" -ForegroundColor White
        Write-Host "??  Loves: 14" -ForegroundColor White
        Write-Host "?? Support Transactions: 11" -ForegroundColor White
        Write-Host "? Premium Subscriptions: 5" -ForegroundColor White
        Write-Host "???  Memorial Messages: 4" -ForegroundColor White
        Write-Host ""
        Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
        Write-Host "   Test User Credentials" -ForegroundColor Cyan
        Write-Host "???????????????????????????????????????????????" -ForegroundColor Cyan
        Write-Host "All users have the same password: Password123!" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "?? alice@example.com  - Bird lover, active community member" -ForegroundColor White
        Write-Host "?? bob@example.com    - Wildlife photographer" -ForegroundColor White
        Write-Host "?? carol@example.com  - New user, learning" -ForegroundColor White
        Write-Host "?? david@example.com  - Wildlife rescue volunteer" -ForegroundColor White
        Write-Host "?? emma@example.com   - Teacher, using birds for education" -ForegroundColor White
        Write-Host ""
        Write-Host "?? Featured Birds:" -ForegroundColor Cyan
        Write-Host "   • Ruby (Alice) - Anna's Hummingbird, territorial and fearless" -ForegroundColor White
        Write-Host "   • Sunshine (Bob) - American Goldfinch, bright and cheerful" -ForegroundColor White
        Write-Host "   • Phoenix (David) - Red-tailed Hawk, rescue/rehabilitation" -ForegroundColor White
        Write-Host "   • Professor Hoot (Emma) - Barred Owl, classroom favorite" -ForegroundColor White
        Write-Host "   • Angel (David) - Blue Jay, memorial bird ???" -ForegroundColor White
        Write-Host ""
        Write-Host "?? Tip: Use alice@example.com / Password123! to login and explore!" -ForegroundColor Yellow
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "? Error executing script!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Error details:" -ForegroundColor Yellow
        Write-Host $output
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "? Failed to execute database script!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure:" -ForegroundColor Yellow
    Write-Host "  1. PostgreSQL is installed and in PATH" -ForegroundColor White
    Write-Host "  2. psql command is available" -ForegroundColor White
    Write-Host "  3. Database credentials are correct" -ForegroundColor White
    Write-Host "  4. Database server is running" -ForegroundColor White
    exit 1
} finally {
    # Clear password from environment
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}
