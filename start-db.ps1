# One-Command Database Setup
Write-Host "?? Starting PostgreSQL for Wihngo..." -ForegroundColor Cyan

# Check if container already exists
$existing = docker ps -a --filter "name=wihngo-postgres" --format "{{.Names}}"

if ($existing -eq "wihngo-postgres") {
    Write-Host "?? Container exists, starting it..." -ForegroundColor Yellow
    docker start wihngo-postgres
    Write-Host "? PostgreSQL is running!" -ForegroundColor Green
} else {
    Write-Host "?? Creating new PostgreSQL container..." -ForegroundColor Yellow
    docker run -d --name wihngo-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=wihngo -p 5432:5432 postgres:14
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? PostgreSQL created and running!" -ForegroundColor Green
        Start-Sleep -Seconds 2
    } else {
        Write-Host "? Failed! Is Docker running?" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "? Ready! Now restart your app (F5 in Visual Studio)" -ForegroundColor Green
Write-Host ""
