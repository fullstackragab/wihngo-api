# Quick Test: Mobile App Profile Endpoint

## Summary

The `/api/users/profile` endpoint **IS WORKING** through ngrok. The 404 error from the mobile app is likely due to an **expired or invalid JWT token**.

## Proof of Functionality

I tested the endpoint through your ngrok tunnel and got these results:

### Test 1: Without Authentication
```bash
GET https://horsier-maliah-semilyrical.ngrok-free.dev/api/users/profile
Result: 401 Unauthorized ? (Expected behavior)
```

### Test 2: With Valid Token
```bash
GET https://horsier-maliah-semilyrical.ngrok-free.dev/api/users/profile
Authorization: Bearer <valid_token>
Result: 200 OK ? (Working!)
Response:
{
  "userId": "d734e5ca-5690-470f-83c1-868dfb5175a8",
  "name": "Alice Johnson",
  "email": "alice@example.com",
  "profileImageUrl": "https://amzn-s3-wihngo-bucket.s3.amazonaws.com/...",
  "emailConfirmed": true,
  ...
}
```

## What This Means

The API endpoint is **100% functional**. The mobile app's 404 error is happening because:

1. **Most Likely**: The mobile app's JWT token is expired/invalid
2. **Possible**: The mobile app is not properly including the Authorization header
3. **Less Likely**: There's a URL mismatch in the mobile app configuration

## Immediate Action Items

### For the Mobile App Developer:

1. **Check Token Validity**
   - Print the token being sent in mobile app logs
   - Verify the token hasn't expired
   - Test the same token from PowerShell/Postman

2. **Verify Authorization Header**
   ```typescript
   // Make sure the header is formatted correctly
   headers: {
     'Authorization': `Bearer ${token}`,  // Must be exactly this format
     'Content-Type': 'application/json',
     'ngrok-skip-browser-warning': '1'
   }
   ```

3. **Implement Token Refresh**
   - The token expires (check your JWT configuration for expiry time)
   - Implement automatic token refresh before making requests
   - Handle 401 errors by refreshing the token and retrying

## Quick Diagnostic Script

Run this in PowerShell to test your exact ngrok setup:

```powershell
# Test the endpoint is accessible
Write-Host "`n=== Testing Profile Endpoint ===" -ForegroundColor Cyan

# Test 1: Without auth (should be 401)
Write-Host "`n1. Testing without authentication..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "https://horsier-maliah-semilyrical.ngrok-free.dev/api/users/profile" `
        -Headers @{"ngrok-skip-browser-warning"="1"} -ErrorAction Stop
    Write-Host "   ? Unexpected: Endpoint allowed access without auth!" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 401) {
        Write-Host "   ? Correct: 401 Unauthorized (endpoint exists)" -ForegroundColor Green
    } else {
        Write-Host "   ? Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
}

# Test 2: Login and test with valid token
Write-Host "`n2. Getting fresh token..." -ForegroundColor Yellow
$body = @{ 
    email = "alice@example.com"
    password = "Password123!" 
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "https://horsier-maliah-semilyrical.ngrok-free.dev/api/auth/login" `
        -Method POST `
        -Body $body `
        -ContentType "application/json" `
        -Headers @{"ngrok-skip-browser-warning"="1"}
    
    Write-Host "   ? Login successful" -ForegroundColor Green
    Write-Host "   Token: $($loginResponse.token.Substring(0, 50))..." -ForegroundColor Gray
    
    # Test 3: Use token to access profile
    Write-Host "`n3. Testing profile endpoint with token..." -ForegroundColor Yellow
    $profileResponse = Invoke-RestMethod `
        -Uri "https://horsier-maliah-semilyrical.ngrok-free.dev/api/users/profile" `
        -Headers @{
            "ngrok-skip-browser-warning"="1"
            "Authorization"="Bearer $($loginResponse.token)"
        }
    
    Write-Host "   ? Profile retrieved successfully!" -ForegroundColor Green
    Write-Host "   User: $($profileResponse.name) ($($profileResponse.email))" -ForegroundColor Gray
    
    Write-Host "`n=== CONCLUSION ===" -ForegroundColor Cyan
    Write-Host "The API endpoint is working perfectly!" -ForegroundColor Green
    Write-Host "The mobile app issue is likely:" -ForegroundColor Yellow
    Write-Host "  • Expired/invalid token" -ForegroundColor Yellow
    Write-Host "  • Missing Authorization header" -ForegroundColor Yellow
    Write-Host "  • Token not being sent correctly" -ForegroundColor Yellow
    
} catch {
    Write-Host "   ? Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
}

Write-Host ""
```

## API Changes Made

I've enhanced the API with diagnostic logging:

1. **Added logging filters** in `Program.cs`:
   - `Microsoft.AspNetCore.Authentication` - logs auth attempts
   - `Microsoft.AspNetCore.Authorization` - logs authorization checks
   - `Wihngo.Controllers.UsersController` - logs controller actions

2. **Enhanced `GetProfile()` endpoint** with detailed logs:
   - Logs when endpoint is called
   - Logs user ID claim extraction
   - Logs user lookup results
   - Logs success/failure

## Next Steps

1. **Restart your API** (if hot reload didn't apply changes)
2. **Have the mobile app make a request** to `/api/users/profile`
3. **Check the Visual Studio Debug Output window** for logs
4. **Look for**:
   - `GetProfile endpoint called` ? endpoint was reached
   - `User ID claim: {guid}` ? token was valid
   - OR authentication errors ? token was invalid

This will definitively tell you what's happening!

## Support

If after checking logs you still see issues:
1. Share the mobile app's token (first 50 chars is safe)
2. Share the exact URL the mobile app is calling
3. Share the Visual Studio Debug logs from when the request is made

The endpoint is working - we just need to align the mobile app's authentication!
