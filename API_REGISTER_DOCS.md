# ?? User Registration/Signup - Request & Response

## Complete API Documentation

---

## ?? Endpoint

```
POST /api/auth/register
Content-Type: application/json
```

**Base URL:** `https://your-api-domain.com`  
**Full URL:** `https://your-api-domain.com/api/auth/register`

---

## ?? Request Payload

### JSON Structure

```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "password": "SecurePassword123!",
  "profileImage": "https://example.com/images/profile.jpg",
  "bio": "Bird enthusiast and nature lover"
}
```

### Field Specifications

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `name` | string | ? Yes | Max 200 chars | User's full name |
| `email` | string | ? Yes | Valid email format, Max 255 chars, Unique | Email address (used for login) |
| `password` | string | ? Yes | Min 6 chars, Max 128 chars | Password (will be hashed with BCrypt) |
| `profileImage` | string | ? No | Max 1000 chars | URL to profile image |
| `bio` | string | ? No | Max 2000 chars | User biography/description |

### DTO Class (C#)

```csharp
public class UserCreateDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ProfileImage { get; set; }

    [MaxLength(2000)]
    public string? Bio { get; set; }
}
```

---

## ? Success Response (200 OK)

### JSON Structure

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
  "refreshToken": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6",
  "expiresAt": "2025-01-15T11:30:00Z",
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "emailConfirmed": false
}
```

### Field Specifications

| Field | Type | Description |
|-------|------|-------------|
| `token` | string | JWT access token (expires in 1 hour) |
| `refreshToken` | string | Refresh token for getting new access tokens (expires in 30 days) |
| `expiresAt` | datetime | When the access token expires (ISO 8601 format) |
| `userId` | uuid | Unique identifier for the user |
| `name` | string | User's full name |
| `email` | string | User's email address |
| `emailConfirmed` | boolean | Email confirmation status (false after registration) |

### DTO Class (C#)

```csharp
public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
}
```

---

## ? Error Responses

### 400 Bad Request - Validation Failed

```json
{
  "message": "Validation failed",
  "errors": {
    "Email": ["The Email field is required."],
    "Password": ["The Password field is required."]
  }
}
```

**When it occurs:**
- Required fields missing
- Invalid email format
- Password too short (< 6 characters)
- Field exceeds maximum length

---

### 400 Bad Request - Weak Password

```json
{
  "message": "Password does not meet security requirements",
  "errors": [
    "Password must be at least 8 characters long",
    "Password must contain at least one uppercase letter",
    "Password must contain at least one number"
  ]
}
```

**When it occurs:**
- Password doesn't meet security requirements
- See password validation rules below

---

### 409 Conflict - Email Already Exists

```json
{
  "message": "Email already registered"
}
```

**When it occurs:**
- Email address is already in use by another user

---

### 500 Internal Server Error

```json
{
  "message": "Registration failed. Please try again."
}
```

**When it occurs:**
- Database connection issues
- Unexpected server errors
- Email service failures

---

## ?? Password Validation Rules

The password must meet these requirements:

| Rule | Requirement |
|------|-------------|
| **Length** | Minimum 8 characters |
| **Uppercase** | At least 1 uppercase letter (A-Z) |
| **Lowercase** | At least 1 lowercase letter (a-z) |
| **Digit** | At least 1 number (0-9) |
| **Special Character** | At least 1 special character (!@#$%^&*) |

### Examples

? **Valid Passwords:**
- `SecurePass123!`
- `MyP@ssw0rd`
- `Test1234!@#$`

? **Invalid Passwords:**
- `password` (no uppercase, no digit, no special char)
- `PASSWORD` (no lowercase, no digit, no special char)
- `Pass123` (no special character)
- `Short1!` (too short - only 7 characters)

---

## ?? Post-Registration Process

After successful registration:

1. **User is created** in the database
2. **Email confirmation token** is generated (expires in 24 hours)
3. **Confirmation email** is sent to the user's email address
4. **User receives:**
   - JWT access token (1-hour expiration)
   - Refresh token (30-day expiration)
5. **User can login** immediately but certain features may require email confirmation

### Email Confirmation

The confirmation email contains:
- Link format: `https://wihngo.com/auth/confirm-email?email={email}&token={token}`
- 24-hour expiration
- One-time use only

**To confirm email:**
```
POST /api/auth/confirm-email
{
  "email": "john.doe@example.com",
  "token": "confirmation-token-from-email"
}
```

---

## ?? Using the Tokens

### Access Token (JWT)

**Usage:**
```http
GET /api/some-protected-endpoint
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Lifetime:** 1 hour  
**Purpose:** Access protected API endpoints

### Refresh Token

**Usage:**
```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6"
}
```

**Lifetime:** 30 days  
**Purpose:** Get new access token when expired

---

## ?? Example API Calls

### cURL

```bash
curl -X POST https://your-api-domain.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "email": "john.doe@example.com",
    "password": "SecurePass123!",
    "profileImage": "https://example.com/profile.jpg",
    "bio": "Bird lover"
  }'
```

### JavaScript (Fetch)

```javascript
const response = await fetch('https://your-api-domain.com/api/auth/register', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    name: 'John Doe',
    email: 'john.doe@example.com',
    password: 'SecurePass123!',
    profileImage: 'https://example.com/profile.jpg',
    bio: 'Bird lover'
  })
});

const data = await response.json();

if (response.ok) {
  console.log('Registration successful!');
  console.log('Token:', data.token);
  console.log('User ID:', data.userId);
  
  // Store tokens securely
  localStorage.setItem('accessToken', data.token);
  localStorage.setItem('refreshToken', data.refreshToken);
} else {
  console.error('Registration failed:', data.message);
}
```

### TypeScript (Axios)

```typescript
import axios from 'axios';

interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  profileImage?: string;
  bio?: string;
}

interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  userId: string;
  name: string;
  email: string;
  emailConfirmed: boolean;
}

try {
  const response = await axios.post<AuthResponse>(
    'https://your-api-domain.com/api/auth/register',
    {
      name: 'John Doe',
      email: 'john.doe@example.com',
      password: 'SecurePass123!',
      profileImage: 'https://example.com/profile.jpg',
      bio: 'Bird lover'
    }
  );

  const { token, refreshToken, userId, emailConfirmed } = response.data;
  
  // Store tokens
  localStorage.setItem('accessToken', token);
  localStorage.setItem('refreshToken', refreshToken);
  
  console.log('User registered:', userId);
  console.log('Email confirmed:', emailConfirmed);
  
} catch (error) {
  if (axios.isAxiosError(error) && error.response) {
    console.error('Registration error:', error.response.data.message);
  }
}
```

### C# (HttpClient)

```csharp
using System.Net.Http.Json;

public class UserCreateDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string? ProfileImage { get; set; }
    public string? Bio { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public bool EmailConfirmed { get; set; }
}

// Usage
var httpClient = new HttpClient();
var request = new UserCreateDto
{
    Name = "John Doe",
    Email = "john.doe@example.com",
    Password = "SecurePass123!",
    ProfileImage = "https://example.com/profile.jpg",
    Bio = "Bird lover"
};

var response = await httpClient.PostAsJsonAsync(
    "https://your-api-domain.com/api/auth/register", 
    request
);

if (response.IsSuccessStatusCode)
{
    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
    Console.WriteLine($"User registered: {authResponse.UserId}");
    Console.WriteLine($"Token: {authResponse.Token}");
}
else
{
    var error = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"Registration failed: {error}");
}
```

### Python (Requests)

```python
import requests

url = "https://your-api-domain.com/api/auth/register"
payload = {
    "name": "John Doe",
    "email": "john.doe@example.com",
    "password": "SecurePass123!",
    "profileImage": "https://example.com/profile.jpg",
    "bio": "Bird lover"
}

response = requests.post(url, json=payload)

if response.status_code == 200:
    data = response.json()
    print(f"User registered: {data['userId']}")
    print(f"Token: {data['token']}")
    
    # Store tokens securely
    access_token = data['token']
    refresh_token = data['refreshToken']
else:
    error = response.json()
    print(f"Registration failed: {error['message']}")
```

---

## ?? Security Features

### Implemented on Backend

? **Password Hashing** - BCrypt with work factor 12  
? **Email Uniqueness** - Prevents duplicate accounts  
? **Token Generation** - Secure random tokens  
? **JWT Signing** - HMAC SHA-256 signature  
? **Token Expiration** - Access token (1h), Refresh token (30d)  
? **Email Verification** - Confirmation required for full access  
? **Rate Limiting** - Prevents abuse  
? **Input Validation** - Server-side validation  

### What to Implement on Client

? **HTTPS Only** - Always use secure connections  
? **Secure Storage** - Store tokens securely (not localStorage for sensitive apps)  
? **Token Refresh** - Implement automatic token refresh  
? **Password Validation** - Client-side validation before submit  
? **Error Handling** - Handle all error cases gracefully  

---

## ?? Database Changes

After successful registration, a new record is created:

```sql
INSERT INTO users (
  user_id,
  name,
  email,
  password_hash,
  email_confirmed,
  email_confirmation_token,
  email_confirmation_token_expiry,
  refresh_token_hash,
  refresh_token_expiry,
  last_login_at,
  last_password_change_at,
  created_at,
  updated_at,
  failed_login_attempts,
  is_account_locked,
  profile_image,
  bio
) VALUES (
  'generated-uuid',
  'John Doe',
  'john.doe@example.com',
  '$2a$12$hashed_password...',
  false,
  'confirmation-token',
  '2025-01-16 10:00:00',
  '$2a$12$hashed_refresh_token...',
  '2025-02-14 10:00:00',
  '2025-01-15 10:00:00',
  '2025-01-15 10:00:00',
  '2025-01-15 10:00:00',
  '2025-01-15 10:00:00',
  0,
  false,
  'https://example.com/profile.jpg',
  'Bird lover'
);
```

---

## ?? Quick Reference

### Minimum Required Payload

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

### Full Payload (All Fields)

```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "profileImage": "https://example.com/profile.jpg",
  "bio": "Bird enthusiast"
}
```

### Success Response

```json
{
  "token": "jwt-token",
  "refreshToken": "refresh-token",
  "expiresAt": "2025-01-15T11:30:00Z",
  "userId": "uuid",
  "name": "John Doe",
  "email": "john@example.com",
  "emailConfirmed": false
}
```

---

## ?? Next Steps After Registration

1. ? **Store tokens securely**
2. ? **Show "Check your email" message**
3. ? **Implement email confirmation flow**
4. ? **Handle token refresh**
5. ? **Redirect to main app (if allowed pre-confirmation)**

---

## ?? Related Endpoints

- `POST /api/auth/login` - User login
- `POST /api/auth/confirm-email` - Confirm email address
- `POST /api/auth/resend-confirmation` - Resend confirmation email
- `POST /api/auth/refresh-token` - Refresh access token
- `GET /api/auth/validate` - Validate current token

---

**Last Updated:** January 2025  
**API Version:** v1.0  
**Backend:** .NET 10
