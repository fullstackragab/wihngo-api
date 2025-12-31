# ============================================
# Run Database Setup on Render.com
# ============================================

# Set password as environment variable to avoid prompt
$env:PGPASSWORD="YOUR_DB_PASSWORD"

Write-Host "?? Connecting to Render.com database..." -ForegroundColor Green
Write-Host "   Host: YOUR_DB_HOST" -ForegroundColor Cyan
Write-Host "   Database: wihngo_kzno" -ForegroundColor Cyan
Write-Host ""

# Run the setup script
psql -h YOUR_DB_HOST `
     -U wihngo `
     -d wihngo_kzno `
     -f render-database-setup.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "? Database setup completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? Test Credentials:" -ForegroundColor Yellow
    Write-Host "   Email: test@wihngo.com" -ForegroundColor White
    Write-Host "   Password: Test123!" -ForegroundColor White
    Write-Host ""
    Write-Host "?? Your Crypto Addresses:" -ForegroundColor Yellow
    Write-Host "   Solana: AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn" -ForegroundColor White
    Write-Host "   Base: 0x2e61b5d2066eAFb86FBD75F59c585468ceE51092" -ForegroundColor White
    Write-Host ""
    Write-Host "?? Next Steps:" -ForegroundColor Yellow
    Write-Host "   1. Run 'dotnet run' to start the application" -ForegroundColor White
    Write-Host "   2. Test the /test endpoint" -ForegroundColor White
    Write-Host "   3. Login with test@wihngo.com to get JWT token" -ForegroundColor White
    Write-Host "   4. Create your first invoice!" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "? Database setup failed. Check errors above." -ForegroundColor Red
}

# Clean up environment variable
Remove-Item Env:PGPASSWORD
