# ====================================================
# SEPOLIA PAYMENT STATUS CHECKER (PowerShell)
# ====================================================
# Quick script to check if your Sepolia payment completed

param(
    [string]$DbHost = "localhost",
    [string]$DbPort = "5432",
    [string]$DbName = "wihngo",
    [string]$DbUser = "postgres",
    [string]$DbPassword = "postgres"
)

Write-Host "?? CHECKING SEPOLIA PAYMENT STATUS..." -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Function to run SQL query
function Invoke-DbQuery {
    param([string]$Query)
    
    $env:PGPASSWORD = $DbPassword
    $result = & psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -t -A -c $Query 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Database connection failed!" -ForegroundColor Red
        Write-Host "Error: $result" -ForegroundColor Red
        Write-Host ""
        Write-Host "Make sure PostgreSQL is running and credentials are correct." -ForegroundColor Yellow
        return $null
    }
    
    return $result
}

# 1. Get latest Sepolia payments
Write-Host "?? Latest Sepolia Payments:" -ForegroundColor White
Write-Host "----------------------------" -ForegroundColor Gray

$latestQuery = @"
SELECT 
    id,
    status,
    SUBSTRING(transaction_hash, 1, 20) || '...' AS tx_hash,
    confirmations || '/' || required_confirmations AS confirms,
    ROUND(amount_crypto::NUMERIC, 6) || ' ' || currency AS amount,
    to_char(created_at, 'HH24:MI:SS') AS time
FROM crypto_payment_requests
WHERE network = 'sepolia'
ORDER BY created_at DESC
LIMIT 5;
"@

$env:PGPASSWORD = $DbPassword
& psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -c $latestQuery
Write-Host ""

# 2. Check for stuck payments
Write-Host "??  Checking for stuck payments..." -ForegroundColor Yellow
$stuckCount = Invoke-DbQuery "SELECT COUNT(*) FROM crypto_payment_requests WHERE network = 'sepolia' AND status = 'confirmed' AND completed_at IS NULL;"

if ($stuckCount -and [int]$stuckCount -gt 0) {
    Write-Host "Found $stuckCount payment(s) stuck in 'confirmed' status!" -ForegroundColor Yellow
    Write-Host "These need the PaymentMonitorJob to complete them." -ForegroundColor Yellow
    Write-Host ""
    
    $stuckQuery = @"
SELECT 
    id,
    status,
    confirmations || '/' || required_confirmations AS confirms,
    EXTRACT(EPOCH FROM (NOW() - confirmed_at))::INT || 's' AS stuck_for
FROM crypto_payment_requests
WHERE network = 'sepolia' 
  AND status = 'confirmed' 
  AND completed_at IS NULL;
"@
    
    & psql -h $DbHost -p $DbPort -U $DbUser -d $DbName -c $stuckQuery
    Write-Host ""
} else {
    Write-Host "? No stuck payments" -ForegroundColor Green
    Write-Host ""
}

# 3. Check payment status summary
$completed = Invoke-DbQuery "SELECT COUNT(*) FROM crypto_payment_requests WHERE network = 'sepolia' AND status = 'completed';"
$confirming = Invoke-DbQuery "SELECT COUNT(*) FROM crypto_payment_requests WHERE network = 'sepolia' AND status = 'confirming';"
$pending = Invoke-DbQuery "SELECT COUNT(*) FROM crypto_payment_requests WHERE network = 'sepolia' AND status = 'pending';"

Write-Host "?? Summary:" -ForegroundColor White
if ($completed) { Write-Host "   ? Completed: $completed" -ForegroundColor Green }
if ($confirming -and [int]$confirming -gt 0) { Write-Host "   ? Confirming: $confirming" -ForegroundColor Yellow }
if ($pending -and [int]$pending -gt 0) { Write-Host "   ??  Pending: $pending" -ForegroundColor Blue }
Write-Host ""

# 4. Get latest payment for manual check
Write-Host "======================================"
Write-Host "?? Quick Links:" -ForegroundColor White
Write-Host "   - Hangfire Dashboard: http://localhost:5000/hangfire"
Write-Host "   - Sepolia Etherscan: https://sepolia.etherscan.io"
Write-Host ""

$latestId = Invoke-DbQuery "SELECT id FROM crypto_payment_requests WHERE network = 'sepolia' ORDER BY created_at DESC LIMIT 1;"
if ($latestId) {
    Write-Host "?? Latest Payment ID: $latestId" -ForegroundColor Cyan
    Write-Host "   Manual Check API:" -ForegroundColor Gray
    Write-Host "   POST /api/payments/crypto/$latestId/check-status" -ForegroundColor White
    Write-Host ""
    
    # Show curl command
    Write-Host "   Example curl command:" -ForegroundColor Gray
    Write-Host "   curl -X POST http://localhost:5000/api/payments/crypto/$latestId/check-status \" -ForegroundColor White
    Write-Host "     -H 'Authorization: Bearer YOUR_TOKEN' \" -ForegroundColor White
    Write-Host "     -H 'Content-Type: application/json'" -ForegroundColor White
    Write-Host ""
}

Write-Host "?? TIP: PaymentMonitorJob runs every 30 seconds." -ForegroundColor Yellow
Write-Host "   Wait at least 30 seconds for automatic processing." -ForegroundColor Yellow
Write-Host ""

# 5. Check if backend is running
Write-Host "?? Checking Backend Status..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/payments/crypto/rates" -TimeoutSec 2 -ErrorAction Stop
    Write-Host "   ? Backend is running" -ForegroundColor Green
} catch {
    Write-Host "   ? Backend is NOT running or not responding" -ForegroundColor Red
    Write-Host "   Start with: dotnet run" -ForegroundColor Yellow
}
Write-Host ""

# 6. Check Hangfire status
Write-Host "?? Checking Hangfire Status..." -ForegroundColor Cyan
try {
    $hangfireResponse = Invoke-WebRequest -Uri "http://localhost:5000/hangfire" -TimeoutSec 2 -ErrorAction Stop
    Write-Host "   ? Hangfire is accessible" -ForegroundColor Green
} catch {
    Write-Host "   ??  Hangfire dashboard not accessible" -ForegroundColor Yellow
}
Write-Host ""

Write-Host "======================================"
Write-Host "? Diagnostic Complete" -ForegroundColor Green
