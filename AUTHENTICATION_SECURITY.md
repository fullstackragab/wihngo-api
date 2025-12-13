# Authentication & Security System Documentation

## Overview
The Wihngo authentication system has been enhanced with comprehensive security features including email confirmation, password reset, refresh tokens, account lockout protection, rate limiting, and strong password policies.

## Security Features

### 1. Password Security
- **Minimum Requirements:**
  - At least 8 characters long
  - Contains uppercase letter (A-Z)
  - Contains lowercase letter (a-z)
  - Contains digit (0-9)
  - Contains special character (!@#$%^&* etc.)
  - Not a common password
  - No sequential characters (123, abc, etc.)

- **Hashing:** BCrypt with work factor 12
- **Maximum Length:** 128 characters

### 2. Account Lockout Protection
- **Failed Login Attempts:** Account locks after 5 consecutive failed attempts
- **Lockout Duration:** 30 minutes
- **Auto-Unlock:** Account automatically unlocks when lockout period expires
- **Counter Reset:** Failed attempts counter resets on successful login

### 3. Rate Limiting
- **Login/Register Endpoints:** 5 attempts per 15 minutes per IP address
- **General API Endpoints:** 100 requests per minute per IP address
- **Implementation:** In-memory tracking with automatic cleanup
- **Response Code:** 429 (Too Many Requests) when limit exceeded

### 4. JWT Token Management
- **Access Token:**
  - Expiry: 24 hours (configurable)
  - Contains: UserId, Name, Email, EmailConfirmed status
  - Algorithm: HS256
  - No clock skew grace period

- **Refresh Token:**
  - Expiry: 30 days (configurable)
  - SHA256 hashed storage
  - One-time use (rotates on each refresh)
  - Revoked on logout and password change

### 5. Email Confirmation
- **Token Expiry:** 24 hours
- **Purpose:** Verify user email ownership
- **Security:** Token stored hashed, expires automatically
- **Status:** Tracked in `email_confirmed` field

### 6. Password Reset
- **Token Expiry:** 1 hour
- **Security:** 
  - Tokens are single-use
  - No user existence disclosure
  - All refresh tokens revoked on reset
  - Account lockout cleared on successful reset
- **Flow:** Request ? Email ? Verify Token ? Reset Password

## API Endpoints

### Authentication Endpoints

#### POST /api/auth/register
Register a new user account.
```json
Request:
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecureP@ss123",
  "profileImage": "optional-url",
  "bio": "optional-bio"
}

Response:
{
  "token": "jwt-access-token",
  "refreshToken": "refresh-token",
  "expiresAt": "2024-01-01T00:00:00Z",
  "userId": "guid",
  "name": "John Doe",
  "email": "john@example.com",
  "emailConfirmed": false
}
```

#### POST /api/auth/login
Login with email and password.
```json
Request:
{
  "email": "john@example.com",
  "password": "SecureP@ss123"
}

Response:
{
  "token": "jwt-access-token",
  "refreshToken": "refresh-token",
  "expiresAt": "2024-01-01T00:00:00Z",
  "userId": "guid",
  "name": "John Doe",
  "email": "john@example.com",
  "emailConfirmed": false
}
```

#### POST /api/auth/refresh-token
Get new access token using refresh token.
```json
Request:
{
  "refreshToken": "your-refresh-token"
}

Response:
{
  "token": "new-jwt-access-token",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2024-01-01T00:00:00Z"
}
```

#### POST /api/auth/logout
Logout and revoke refresh token (requires authentication).
```json
Response:
{
  "message": "Logged out successfully"
}
```

#### GET /api/auth/validate
Validate current JWT token (requires authentication).
```json
Response:
{
  "valid": true,
  "userId": "guid",
  "email": "john@example.com",
  "name": "John Doe",
  "emailConfirmed": false
}
```

#### POST /api/auth/confirm-email
Confirm user email address.
```json
Request:
{
  "email": "john@example.com",
  "token": "email-confirmation-token"
}

Response:
{
  "message": "Email confirmed successfully"
}
```

#### POST /api/auth/forgot-password
Request password reset.
```json
Request:
{
  "email": "john@example.com"
}

Response:
{
  "message": "If the email exists, a password reset link has been sent."
}
```

#### POST /api/auth/reset-password
Reset password with token.
```json
Request:
{
  "email": "john@example.com",
  "token": "reset-token",
  "newPassword": "NewSecureP@ss456",
  "confirmPassword": "NewSecureP@ss456"
}

Response:
{
  "message": "Password reset successfully"
}
```

#### POST /api/auth/change-password
Change password (requires authentication).
```json
Request:
{
  "currentPassword": "OldSecureP@ss123",
  "newPassword": "NewSecureP@ss456",
  "confirmPassword": "NewSecureP@ss456"
}

Response:
{
  "message": "Password changed successfully. Please login again."
}
```

## Error Responses

### 400 Bad Request
```json
{
  "message": "Validation failed",
  "errors": ["Password must contain at least one uppercase letter"]
}
```

### 401 Unauthorized
```json
{
  "message": "Invalid email or password"
}
```

### 409 Conflict
```json
{
  "message": "Email already registered"
}
```

### 429 Too Many Requests
```json
{
  "message": "Too many login attempts. Please try again later.",
  "code": "RATE_LIMIT_EXCEEDED",
  "retryAfter": 900
}
```

### Account Locked
```json
{
  "message": "Account is locked due to too many failed login attempts. Try again in 25 minutes.",
  "code": "ACCOUNT_LOCKED",
  "lockoutEnd": "2024-01-01T00:30:00Z"
}
```

## Database Schema

### Users Table Security Fields
```sql
-- Email Confirmation
email_confirmed BOOLEAN DEFAULT FALSE
email_confirmation_token VARCHAR(500)
email_confirmation_token_expiry TIMESTAMP

-- Account Lockout
is_account_locked BOOLEAN DEFAULT FALSE
failed_login_attempts INTEGER DEFAULT 0
lockout_end TIMESTAMP

-- Session Management
last_login_at TIMESTAMP
refresh_token_hash VARCHAR(500)
refresh_token_expiry TIMESTAMP

-- Password Management
password_reset_token VARCHAR(500)
password_reset_token_expiry TIMESTAMP
last_password_change_at TIMESTAMP
```

### Indexes
- `idx_users_email_confirmation_token` - Fast email confirmation lookups
- `idx_users_password_reset_token` - Fast password reset lookups
- `idx_users_refresh_token_hash` - Fast refresh token validation
- `idx_users_lockout_end` - Efficient lockout status checks

## Configuration

### appsettings.json
```json
{
  "Jwt": {
    "Key": "your-secret-key-at-least-32-characters-long",
    "Issuer": "https://wihngo.com",
    "Audience": "https://wihngo.com",
    "ExpiryHours": "24"
  },
  "Security": {
    "PasswordPolicy": {
      "MinimumLength": 8,
      "MaximumLength": 128,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigit": true,
      "RequireSpecialCharacter": true,
      "CheckCommonPasswords": true,
      "CheckSequentialCharacters": true,
      "BCryptWorkFactor": 12
    },
    "AccountLockout": {
      "MaxFailedAttempts": 5,
      "LockoutDurationMinutes": 30,
      "EnableLockout": true
    },
    "TokenSettings": {
      "AccessTokenExpiryHours": 24,
      "RefreshTokenExpiryDays": 30,
      "EmailConfirmationTokenExpiryHours": 24,
      "PasswordResetTokenExpiryHours": 1
    },
    "RateLimit": {
      "MaxLoginAttemptsPerWindow": 5,
      "LoginWindowMinutes": 15,
      "MaxApiRequestsPerWindow": 100,
      "ApiWindowMinutes": 1,
      "EnableRateLimiting": true
    }
  }
}
```

## Security Best Practices

### 1. Token Handling
- Store JWT in secure HTTP-only cookies or secure storage
- Never expose tokens in URLs or logs
- Implement token refresh before expiry
- Revoke refresh tokens on logout

### 2. Password Management
- Never log passwords or password hashes
- Use strong BCrypt work factors (12+)
- Enforce password complexity requirements
- Implement password history (future enhancement)

### 3. Rate Limiting
- Monitor for distributed attacks
- Implement IP whitelist for trusted sources
- Consider adding CAPTCHA for repeated failures
- Log suspicious activity

### 4. Email Verification
- Send confirmation emails asynchronously
- Implement email sending service (TODO)
- Use branded email templates
- Include unsubscribe links

### 5. Monitoring & Logging
- Log all authentication attempts
- Track failed login patterns
- Monitor for brute force attacks
- Alert on suspicious activity

## Future Enhancements

1. **Multi-Factor Authentication (MFA)**
   - TOTP (Time-based One-Time Password)
   - SMS verification
   - Email verification codes

2. **OAuth/Social Login**
   - Google Sign-In
   - Apple Sign-In
   - Facebook Login

3. **Session Management**
   - Device tracking
   - Active session list
   - Remote logout capability

4. **Security Analytics**
   - Login location tracking
   - Device fingerprinting
   - Anomaly detection

5. **Password History**
   - Prevent password reuse
   - Track last N passwords

6. **Account Recovery**
   - Security questions
   - Backup email
   - Phone number verification

## Migration Guide

### Step 1: Run Database Migration
```bash
psql -h your-host -U your-user -d your-database -f Database/migrations/add_user_security_fields.sql
```

### Step 2: Update Environment Variables
Ensure JWT configuration is properly set in your environment.

### Step 3: Test Authentication Flow
1. Register new user
2. Attempt login with wrong password (test lockout)
3. Test refresh token
4. Test password change
5. Test rate limiting

### Step 4: Monitor Logs
Watch for authentication events and rate limiting triggers.

## Support & Maintenance

### Common Issues

1. **Token Expired**
   - Use refresh token endpoint
   - Re-authenticate if refresh token expired

2. **Account Locked**
   - Wait for lockout period to expire
   - Contact support for manual unlock

3. **Rate Limited**
   - Wait for window to reset
   - Check for automated scripts
   - Verify IP address

4. **Email Not Received**
   - Check spam folder
   - Verify email configuration
   - Check email service logs

## Security Checklist

- [x] Strong password requirements enforced
- [x] Account lockout after failed attempts
- [x] Rate limiting on auth endpoints
- [x] JWT with secure configuration
- [x] Refresh token rotation
- [x] Email confirmation flow
- [x] Password reset flow
- [x] Secure password hashing (BCrypt)
- [x] Token revocation on logout
- [x] Comprehensive logging
- [ ] Email service integration (TODO)
- [ ] Multi-factor authentication (TODO)
- [ ] OAuth integration (TODO)

## Contact

For security concerns or vulnerabilities, please contact: security@wihngo.com

---

**Last Updated:** 2024
**Version:** 1.0
