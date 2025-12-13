# ?? Register/Signup API - Quick Reference

## Endpoint
```
POST /api/auth/register
```

## Request
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "profileImage": "https://example.com/image.jpg",  // optional
  "bio": "Bird lover"                                // optional
}
```

## Success Response (200 OK)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "a1b2c3d4e5f6...",
  "expiresAt": "2025-01-15T11:30:00Z",
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Doe",
  "email": "john@example.com",
  "emailConfirmed": false
}
```

## Error Responses

### 400 - Validation Failed
```json
{
  "message": "Validation failed",
  "errors": { "Email": ["The Email field is required."] }
}
```

### 400 - Weak Password
```json
{
  "message": "Password does not meet security requirements",
  "errors": ["Password must be at least 8 characters long"]
}
```

### 409 - Email Exists
```json
{
  "message": "Email already registered"
}
```

### 500 - Server Error
```json
{
  "message": "Registration failed. Please try again."
}
```

## Password Rules
- ? Minimum 8 characters
- ? At least 1 uppercase letter
- ? At least 1 lowercase letter
- ? At least 1 number
- ? At least 1 special character (!@#$%^&*)

## Quick Test (cURL)
```bash
curl -X POST https://your-api.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"name":"Test User","email":"test@example.com","password":"Test1234!"}'
```

## Token Usage
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

## Token Lifetimes
- **Access Token:** 1 hour
- **Refresh Token:** 30 days
- **Email Confirmation:** 24 hours
