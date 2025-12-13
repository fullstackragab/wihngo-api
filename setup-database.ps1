# Quick PostgreSQL Setup Script for Wihngo
# Run this with: .\setup-database.ps1

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Wihngo - PostgreSQL Setup Script" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is installed
Write-Host "Checking for Docker..." -ForegroundColor Yellow
$dockerInstalled = Get-Command docker -ErrorAction SilentlyContinue

if (-not $dockerInstalled) {
    Write-Host "? Docker is not installed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Docker Desktop from:" -ForegroundColor Yellow
    Write-Host "https://www.docker.com/products/docker-desktop/" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "After installation, restart your computer and run this script again." -ForegroundColor Yellow
    exit 1
}

Write-Host "? Docker is installed" -ForegroundColor Green

# Check if Docker is running
Write-Host "Checking if Docker is running..." -ForegroundColor Yellow
docker ps > $null 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Docker is not running!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please start Docker Desktop and try again." -ForegroundColor Yellow
    exit 1
}

Write-Host "? Docker is running" -ForegroundColor Green
Write-Host ""

# Check if container already exists
Write-Host "Checking for existing PostgreSQL container..." -ForegroundColor Yellow
$existingContainer = docker ps -a --filter "name=wihngo-postgres" --format "{{.Names}}"

if ($existingContainer -eq "wihngo-postgres") {
    Write-Host "??  Container 'wihngo-postgres' already exists" -ForegroundColor Yellow
    Write-Host ""
    
    # Check if it's running
    $runningContainer = docker ps --filter "name=wihngo-postgres" --format "{{.Names}}"
    
    if ($runningContainer -eq "wihngo-postgres") {
        Write-Host "? Container is already running!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Database is ready at:" -ForegroundColor Cyan
        Write-Host "  Host: localhost" -ForegroundColor White
        Write-Host "  Port: 5432" -ForegroundColor White
        Write-Host "  Database: wihngo" -ForegroundColor White
        Write-Host "  Username: postgres" -ForegroundColor White
        Write-Host "  Password: postgres" -ForegroundColor White
    } else {
        Write-Host "Starting existing container..." -ForegroundColor Yellow
        docker start wihngo-postgres
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Container started successfully!" -ForegroundColor Green
        } else {
            Write-Host "? Failed to start container" -ForegroundColor Red
            exit 1
        }
    }
} else {
    Write-Host "Creating new PostgreSQL container..." -ForegroundColor Yellow
    Write-Host ""
    
    docker run -d `
        --name wihngo-postgres `
        -e POSTGRES_PASSWORD=postgres `
        -e POSTGRES_DB=wihngo `
        -p 5432:5432 `
        postgres:14
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? PostgreSQL container created successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Waiting for PostgreSQL to start..." -ForegroundColor Yellow
        Start-Sleep -Seconds 3
    } else {
        Write-Host "? Failed to create container" -ForegroundColor Red
        Write-Host ""
        Write-Host "Common issues:" -ForegroundColor Yellow
        Write-Host "  1. Port 5432 is already in use" -ForegroundColor White
        Write-Host "  2. Docker doesn't have enough resources" -ForegroundColor White
        Write-Host ""
        exit 1
    }
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  ? PostgreSQL is Ready!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Connection details:" -ForegroundColor Cyan
Write-Host "  Host:     localhost" -ForegroundColor White
Write-Host "  Port:     5432" -ForegroundColor White
Write-Host "  Database: wihngo" -ForegroundColor White
Write-Host "  Username: postgres" -ForegroundColor White
Write-Host "  Password: postgres" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Your appsettings.Development.json is already configured" -ForegroundColor White
Write-Host "  2. Run your application with: dotnet run" -ForegroundColor White
Write-Host "  3. You should see '? Database connection successful!'" -ForegroundColor White
Write-Host ""
Write-Host "Useful Docker commands:" -ForegroundColor Cyan
Write-Host "  Stop database:    docker stop wihngo-postgres" -ForegroundColor White
Write-Host "  Start database:   docker start wihngo-postgres" -ForegroundColor White
Write-Host "  View logs:        docker logs wihngo-postgres" -ForegroundColor White
Write-Host "  Remove database:  docker stop wihngo-postgres && docker rm wihngo-postgres" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
