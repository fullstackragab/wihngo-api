# Wihngo API Test Script
# This script tests the main API endpoints

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Wihngo API Test Suite" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5162"

# Test 1: Health Check
Write-Host "[1/5] Testing Auth endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/auth" -UseBasicParsing -TimeoutSec 5
    Write-Host "? Auth endpoint: $($response.Content)" -ForegroundColor Green
} catch {
    Write-Host "? Auth endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Login with test user
Write-Host ""
Write-Host "[2/5] Testing Login..." -ForegroundColor Yellow
$loginBody = @{
    email = "alice@example.com"
    password = "Password123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    Write-Host "? Login successful!" -ForegroundColor Green
    Write-Host "   User: $($loginResponse.name)" -ForegroundColor Gray
    Write-Host "   Email: $($loginResponse.email)" -ForegroundColor Gray
    Write-Host "   Token: $($loginResponse.token.Substring(0, 20))..." -ForegroundColor Gray
    $token = $loginResponse.token
} catch {
    Write-Host "? Login failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Get Birds (requires authentication)
Write-Host ""
Write-Host "[3/5] Testing Get Birds..." -ForegroundColor Yellow
try {
    $headers = @{
        Authorization = "Bearer $token"
    }
    $birds = Invoke-RestMethod -Uri "$baseUrl/api/birds" -Headers $headers
    Write-Host "? Retrieved $($birds.Count) birds" -ForegroundColor Green
    if ($birds.Count -gt 0) {
        Write-Host "   First bird: $($birds[0].name) ($($birds[0].species))" -ForegroundColor Gray
    }
} catch {
    Write-Host "? Get Birds failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Get Supported Tokens
Write-Host ""
Write-Host "[4/5] Testing Supported Tokens..." -ForegroundColor Yellow
try {
    $tokens = Invoke-RestMethod -Uri "$baseUrl/api/crypto-payments/supported-tokens"
    Write-Host "? Retrieved $($tokens.Count) supported tokens" -ForegroundColor Green
    foreach ($token in $tokens) {
        Write-Host "   - $($token.tokenSymbol) on $($token.chain)" -ForegroundColor Gray
    }
} catch {
    Write-Host "? Get Tokens failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Swagger Documentation
Write-Host ""
Write-Host "[5/5] Testing Swagger UI..." -ForegroundColor Yellow
try {
    $swagger = Invoke-WebRequest -Uri "$baseUrl/swagger" -UseBasicParsing
    Write-Host "? Swagger UI is accessible" -ForegroundColor Green
} catch {
    Write-Host "? Swagger UI failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "API Endpoints:" -ForegroundColor White
Write-Host "  • Main API:    $baseUrl" -ForegroundColor Gray
Write-Host "  • Swagger UI:  $baseUrl/swagger" -ForegroundColor Gray
Write-Host "  • Hangfire:    $baseUrl/hangfire" -ForegroundColor Gray
Write-Host ""
Write-Host "Test Accounts:" -ForegroundColor White
Write-Host "  • alice@example.com (Password: Password123!)" -ForegroundColor Gray
Write-Host "  • bob@example.com (Password: Password123!)" -ForegroundColor Gray
Write-Host "  • carol@example.com (Password: Password123!)" -ForegroundColor Gray
Write-Host ""
Write-Host "Database Info:" -ForegroundColor White
Write-Host "  • Host: localhost:5432" -ForegroundColor Gray
Write-Host "  • Database: postgres" -ForegroundColor Gray
Write-Host "  • Tables: 29" -ForegroundColor Gray
Write-Host "  • Users: 5" -ForegroundColor Gray
Write-Host "  • Birds: 10" -ForegroundColor Gray
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor White
Write-Host "  1. Open browser: $baseUrl/swagger" -ForegroundColor Gray
Write-Host "  2. Try API endpoints" -ForegroundColor Gray
Write-Host "  3. View Hangfire dashboard: $baseUrl/hangfire" -ForegroundColor Gray
Write-Host ""
