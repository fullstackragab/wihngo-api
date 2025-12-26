# Mobile App API Connection Guide

## Problem
When running a React Native/Expo app on a physical device or emulator, `localhost` refers to the device itself, not your development machine where the .NET API is running.

## Solution

### 1. .NET API Configuration (COMPLETED ?)
The API has been configured to:
- Listen on all network interfaces (`0.0.0.0`)
- Display local network IP addresses on startup
- Allow CORS from any origin (for development)

### 2. Find Your API URL

When you run the .NET API (`dotnet run` or F5 in Visual Studio), look for this section in the console output:

```
?? MOBILE APP CONNECTION URLS:
   For localhost/emulator: https://localhost:7297/api/
   For mobile devices on same network:
   - https://192.168.X.X:7297/api/
     (or http://192.168.X.X:5162/api/)
```

### 3. Update Your React Native App

In your React Native/Expo app, update the API base URL in your service file:

**File: `C:\expo\wihngo\services\bird.service.ts`**

Replace:
```typescript
const API_BASE_URL = 'https://localhost:7297/api';
```

With (use the IP shown in console):
```typescript
// For Android Emulator
const API_BASE_URL = 'http://10.0.2.2:5162/api';

// OR for Physical Device (use IP from API console output)
const API_BASE_URL = 'http://192.168.8.5:5162/api';

// OR use environment variable
const API_BASE_URL = process.env.EXPO_PUBLIC_API_URL || 'http://192.168.8.5:5162/api';
```

### 4. HTTPS vs HTTP Considerations

**For Development, use HTTP** (port 5162):
- No SSL certificate issues
- Easier to debug
- Faster setup

**If you need HTTPS** (port 7297):
- Android requires trusting self-signed certificates
- iOS requires App Transport Security configuration
- More complex setup for development

### 5. Testing Different Scenarios

| Scenario | URL to Use |
|----------|------------|
| iOS Simulator | `http://localhost:5162/api` |
| Android Emulator | `http://10.0.2.2:5162/api` |
| Physical Device (same WiFi) | `http://192.168.X.X:5162/api` |
| Expo Go App | `http://192.168.X.X:5162/api` |

### 6. Firewall Configuration (Windows)

If you still can't connect, ensure Windows Firewall allows inbound connections:

1. Open Windows Defender Firewall
2. Click "Allow an app through firewall"
3. Find or add your .NET application
4. Enable both Private and Public network access

Or run this PowerShell command as Administrator:
```powershell
New-NetFirewallRule -DisplayName "ASP.NET Core Development" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5162,7297
```

### 7. Verification

Test your API is accessible:
```bash
# From your development machine
curl http://localhost:5162/api/birds

# From your mobile device (replace with actual IP)
curl http://192.168.8.5:5162/api/birds
```

### 8. Production Considerations

**This configuration is for DEVELOPMENT ONLY**

For production:
- Use a proper domain name
- Enable HTTPS with valid certificates
- Configure proper CORS policies (not AllowAll)
- Use environment-specific configuration

## Quick Fix Checklist

- [x] Update `launchSettings.json` to bind to `0.0.0.0`
- [x] Add network IP display in `Program.cs`
- [ ] Find your local IP in API console output
- [ ] Update React Native API URL to use your machine's IP
- [ ] Use HTTP (port 5162) instead of HTTPS for development
- [ ] Test connection from mobile device/emulator
- [ ] Configure Windows Firewall if needed

## Common Issues

### "Network request failed"
- API is not accessible from the mobile device
- Check that both devices are on the same WiFi network
- Verify firewall settings
- Try using HTTP instead of HTTPS

### "Connection refused"
- Wrong IP address
- API not running
- Port blocked by firewall

### "SSL Handshake failed" (HTTPS)
- Self-signed certificate not trusted
- Use HTTP for development instead
