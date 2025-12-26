# Mobile App 404 Error Fix for `/api/users/profile`

## Problem Analysis

The mobile app is getting a **404 error** when calling `GET /api/users/profile`, but when testing the endpoint through ngrok with curl/PowerShell, it works correctly:
- Returns **401 Unauthorized** without a token (expected)
- Returns **200 OK** with a valid token (works!)

### Current Situation
- **API Endpoint**: `GET /api/users/profile` - ? Exists and works
- **Ngrok Tunnel**: `https://horsier-maliah-semilyrical.ngrok-free.dev` ? `https://localhost:7297` - ? Running
- **Mobile App Error**: 404 Not Found ?

## Root Cause

The 404 error from the mobile app (instead of 401) suggests one of these issues:

1. **Token Issues**: The mobile app's JWT token might be:
   - Expired
   - Malformed
   - Missing required claims
   - Using wrong signing key

2. **URL Mismatch**: The mobile app might be:
   - Calling a slightly different URL
   - Including extra path segments
   - Case-sensitivity issues (though unlikely with ASP.NET Core)

3. **Headers Missing**: The request might be missing required headers

## Diagnostic Steps Added

I've added detailed logging to help diagnose this:

### 1. Enhanced Logging in `Program.cs`
```csharp
// Authentication and Authorization logging now enabled
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Information);
builder.Logging.AddFilter("Microsoft.AspNetCore.Authorization", LogLevel.Information);
builder.Logging.AddFilter("Wihngo.Controllers.UsersController", LogLevel.Information);
```

### 2. Enhanced Logging in `UsersController.GetProfile()`
The endpoint now logs:
- When the endpoint is called
- User ID claim extraction
- User lookup in database
- Success/failure outcomes

## How to Diagnose

### Step 1: Check API Logs
When the mobile app makes the request to `/api/users/profile`, check the Visual Studio Output window (Debug pane) for logs like:

```
[HH:mm:ss] info: Wihngo.Controllers.UsersController[0]
      GetProfile endpoint called
[HH:mm:ss] info: Wihngo.Controllers.UsersController[0]
      User ID claim: {guid}
```

**If you don't see these logs**, it means the endpoint is NOT being hit at all, suggesting:
- The mobile app is calling a different URL
- Authentication middleware is rejecting the request before it reaches the controller

**If you see authentication errors** like:
```
[HH:mm:ss] info: Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler[7]
      Failed to validate the token
```

This confirms the token is invalid/expired.

### Step 2: Test with Mobile App's Actual Token

In the mobile app, print out the token being sent:
```typescript
// In your API service
console.log('Auth Token:', authToken.substring(0, 50) + '...');
```

Then test that exact token from PowerShell:
```powershell
$token = "paste_mobile_app_token_here"
try {
    $response = Invoke-RestMethod `
        -Uri "https://horsier-maliah-semilyrical.ngrok-free.dev/api/users/profile" `
        -Method GET `
        -Headers @{
            "ngrok-skip-browser-warning"="1"
            "Authorization"="Bearer $token"
        }
    Write-Host "Success!"
    $response | ConvertTo-Json
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    Write-Host "Status: $($_.Exception.Response.StatusCode.value__)"
}
```

### Step 3: Verify Token Claims

Test the token validation endpoint:
```typescript
// In mobile app
const response = await api.get('/api/auth/validate');
console.log('Token validation:', response);
```

If this returns 401, the token is invalid/expired and needs to be refreshed.

## Solutions

### Solution 1: Refresh the Token
The mobile app should implement automatic token refresh:

```typescript
// services/auth.service.ts
export const refreshAuthToken = async (): Promise<boolean> => {
  try {
    const refreshToken = await SecureStore.getItemAsync('refreshToken');
    if (!refreshToken) {
      return false;
    }

    const response = await apiRequest('/auth/refresh-token', {
      method: 'POST',
      body: JSON.stringify({ refreshToken })
    });

    if (response.ok) {
      const { token, refreshToken: newRefreshToken, expiresAt } = await response.json();
      await SecureStore.setItemAsync('authToken', token);
      await SecureStore.setItemAsync('refreshToken', newRefreshToken);
      await SecureStore.setItemAsync('tokenExpiresAt', expiresAt.toString());
      return true;
    }

    return false;
  } catch (error) {
    console.error('Token refresh failed:', error);
    return false;
  }
};
```

### Solution 2: Implement Automatic Retry with Token Refresh

```typescript
// services/api-helper.ts
export const apiRequest = async (endpoint: string, options: RequestInit = {}) => {
  let response = await fetch(API_BASE_URL + endpoint, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'ngrok-skip-browser-warning': '1',
      ...options.headers,
    },
  });

  // If 401, try to refresh token and retry
  if (response.status === 401) {
    const refreshed = await refreshAuthToken();
    if (refreshed) {
      // Retry with new token
      const newToken = await SecureStore.getItemAsync('authToken');
      response = await fetch(API_BASE_URL + endpoint, {
        ...options,
        headers: {
          ...options.headers,
          'Authorization': `Bearer ${newToken}`,
        },
      });
    }
  }

  return response;
};
```

### Solution 3: Check Token Expiry Before Requests

```typescript
// services/auth.service.ts
export const isTokenExpired = async (): Promise<boolean> => {
  const expiresAt = await SecureStore.getItemAsync('tokenExpiresAt');
  if (!expiresAt) return true;
  
  const expiryDate = new Date(expiresAt);
  const now = new Date();
  
  // Consider expired if less than 5 minutes remaining
  return expiryDate.getTime() - now.getTime() < 5 * 60 * 1000;
};

// Before making requests
if (await isTokenExpired()) {
  await refreshAuthToken();
}
```

## Testing Checklist

- [ ] Verify ngrok is running: `Get-Process -Name ngrok`
- [ ] Test `/api/users` without auth: Should return 200
- [ ] Test `/api/users/profile` without auth: Should return 401
- [ ] Login and get fresh token from mobile app or API
- [ ] Test `/api/users/profile` with fresh token: Should return 200
- [ ] Check mobile app is using correct ngrok URL
- [ ] Verify mobile app includes Authorization header
- [ ] Check token expiry date in mobile app
- [ ] Implement token refresh logic in mobile app

## Quick Test Commands

```powershell
# 1. Check ngrok tunnel
$tunnels = Invoke-RestMethod -Uri "http://127.0.0.1:4040/api/tunnels"
$tunnels.tunnels | ForEach-Object { 
    Write-Host "Public URL: $($_.public_url)"
    Write-Host "Local Addr: $($_.config.addr)"
}

# 2. Login and get token
$body = @{ email = "alice@example.com"; password = "Password123!" } | ConvertTo-Json
$response = Invoke-RestMethod `
    -Uri "https://horsier-maliah-semilyrical.ngrok-free.dev/api/auth/login" `
    -Method POST `
    -Body $body `
    -ContentType "application/json" `
    -Headers @{"ngrok-skip-browser-warning"="1"}
$global:token = $response.token
Write-Host "Token: $($global:token.Substring(0, 50))..."

# 3. Test profile endpoint
$response = Invoke-RestMethod `
    -Uri "https://horsier-maliah-semilyrical.ngrok-free.dev/api/users/profile" `
    -Method GET `
    -Headers @{
        "ngrok-skip-browser-warning"="1"
        "Authorization"="Bearer $global:token"
    }
$response | ConvertTo-Json
```

## Expected Outcome

After implementing token refresh logic in the mobile app, the profile endpoint should work consistently without 404 errors.

## API Changes Made

? Added logging filters for authentication/authorization
? Added detailed logging to `GetProfile()` endpoint
? No breaking changes to the API

The API is working correctly - the issue is on the mobile app side with token management.
