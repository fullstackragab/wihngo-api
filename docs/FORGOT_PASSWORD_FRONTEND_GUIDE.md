# Forgot Password - Frontend Integration Guide

This document provides instructions for implementing the forgot password flow in the frontend application.

## Overview

The forgot password feature allows users to reset their password via email. The flow consists of two steps:

1. **Request Reset** - User submits their email address
2. **Reset Password** - User clicks the email link and sets a new password

---

## API Endpoints

### 1. Request Password Reset

**Endpoint:** `POST /api/auth/forgot-password`

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Response (200 OK):**
```json
{
  "message": "If the email exists, a password reset link has been sent."
}
```

> **Note:** This endpoint always returns the same success message regardless of whether the email exists. This is a security measure to prevent email enumeration attacks.

**Validation:**
- `email` - Required, must be valid email format, max 255 characters

---

### 2. Reset Password

**Endpoint:** `POST /api/auth/reset-password`

**Request Body:**
```json
{
  "email": "user@example.com",
  "token": "base64-encoded-token-from-email",
  "newPassword": "NewSecureP@ss123",
  "confirmPassword": "NewSecureP@ss123"
}
```

**Response (200 OK):**
```json
{
  "message": "Password reset successfully"
}
```

**Error Responses:**

| Status | Response | Meaning |
|--------|----------|---------|
| 400 | `{ "message": "Invalid or expired reset token" }` | Token is invalid, already used, or expired (1-hour limit) |
| 400 | `{ "message": "Password does not meet security requirements", "errors": [...] }` | Password validation failed |
| 400 | `{ "message": "Validation failed", "errors": {...} }` | Passwords don't match or other validation error |

---

## Password Requirements

The new password must meet all of the following criteria:

| Requirement | Rule |
|-------------|------|
| Length | 8-128 characters |
| Uppercase | At least one uppercase letter (A-Z) |
| Lowercase | At least one lowercase letter (a-z) |
| Digit | At least one number (0-9) |
| Special Character | At least one of: `!@#$%^&*()_+-=[]{}';:"\|,.<>/?` |
| No Sequences | Cannot contain sequential characters like `123`, `abc`, `xyz` |
| Not Common | Cannot be a common password (password, 123456, qwerty, etc.) |

---

## Frontend Implementation

### Required Routes

Create two frontend routes:

```
/auth/forgot-password          - Email submission form
/auth/reset-password           - New password form (with query params)
```

### Route: Forgot Password Page

**URL:** `/auth/forgot-password`

**UI Elements:**
- Email input field
- Submit button
- Link to login page
- Success/error message display

**Flow:**
1. User enters email address
2. Frontend calls `POST /api/auth/forgot-password`
3. Show success message: "If an account with that email exists, we've sent a password reset link."
4. Optionally redirect to login page after a few seconds

**Example (React):**
```tsx
const handleForgotPassword = async (email: string) => {
  try {
    setLoading(true);
    const response = await fetch('/api/auth/forgot-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email })
    });

    if (response.ok) {
      setSuccess(true);
      setMessage('If an account with that email exists, we\'ve sent a password reset link.');
    }
  } catch (error) {
    setError('Something went wrong. Please try again.');
  } finally {
    setLoading(false);
  }
};
```

---

### Route: Reset Password Page

**URL:** `/auth/reset-password?email={email}&token={token}`

**Query Parameters:**
| Parameter | Description |
|-----------|-------------|
| `email` | User's email address (URL-encoded) |
| `token` | Base64-encoded reset token (URL-encoded) |

**UI Elements:**
- New password input field
- Confirm password input field
- Password strength indicator (recommended)
- Password requirements list
- Submit button
- Error message display

**Flow:**
1. Parse `email` and `token` from URL query parameters
2. User enters new password and confirms it
3. Frontend validates password matches requirements
4. Frontend calls `POST /api/auth/reset-password`
5. On success, show message and redirect to login page
6. On error, show appropriate error message

**Example (React):**
```tsx
import { useSearchParams, useNavigate } from 'react-router-dom';

const ResetPasswordPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const email = searchParams.get('email') || '';
  const token = searchParams.get('token') || '';

  const handleResetPassword = async (newPassword: string, confirmPassword: string) => {
    try {
      setLoading(true);
      setError(null);

      const response = await fetch('/api/auth/reset-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email,
          token,
          newPassword,
          confirmPassword
        })
      });

      const data = await response.json();

      if (response.ok) {
        setSuccess(true);
        // Redirect to login after 3 seconds
        setTimeout(() => navigate('/auth/login'), 3000);
      } else {
        if (data.errors) {
          // Handle password validation errors
          setError(data.errors.join(', '));
        } else {
          setError(data.message || 'Password reset failed.');
        }
      }
    } catch (error) {
      setError('Something went wrong. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  // Check for missing params
  if (!email || !token) {
    return <div>Invalid reset link. Please request a new password reset.</div>;
  }

  return (
    // Your form JSX here
  );
};
```

---

## Email Template

Users will receive an email with a link in this format:

```
https://wihngo.com/auth/reset-password?email={encoded-email}&token={encoded-token}
```

The email includes:
- Personalized greeting with user's name
- "Reset Password" button linking to the reset page
- Warning that the link expires in 1 hour
- Plain text fallback URL
- Security notice about ignoring if not requested

---

## Security Considerations

### For Frontend Developers

1. **Token Expiry:** Reset tokens expire after 1 hour. If a user clicks an expired link, show a friendly message and link back to the forgot password page.

2. **Don't Cache Tokens:** Never store reset tokens in localStorage or cookies.

3. **HTTPS Only:** Ensure all password reset requests are made over HTTPS.

4. **Rate Limiting:** Consider adding client-side rate limiting to prevent abuse (e.g., disable submit button for 30 seconds after submission).

5. **Clear Form on Success:** After successful reset, clear all password fields before redirecting.

6. **Password Visibility Toggle:** Consider adding a show/hide password button for better UX.

### What Happens on Reset

When a password is successfully reset:
- All existing sessions are invalidated (refresh tokens cleared)
- Any account lockout is cleared
- User must log in again with new password
- A security alert email is sent to the user

---

## Error Handling

### Forgot Password Errors

| Scenario | Handling |
|----------|----------|
| Network error | Show generic error, allow retry |
| Server error (500) | Show "Please try again later" |
| Invalid email format | Client-side validation before submit |

### Reset Password Errors

| Error Message | User Action |
|---------------|-------------|
| "Invalid or expired reset token" | Link to request new reset |
| "Password does not meet security requirements" | Show requirements, highlight issues |
| "Passwords do not match" | Clear confirm field, show error |

---

## Testing Checklist

- [ ] Forgot password form submits correctly
- [ ] Success message shows after submission
- [ ] Reset password page parses URL parameters correctly
- [ ] Password validation shows requirements clearly
- [ ] Confirm password mismatch shows error
- [ ] Successful reset redirects to login
- [ ] Expired/invalid token shows helpful error
- [ ] Works on mobile devices
- [ ] Accessible (keyboard navigation, screen readers)

---

## API Base URL

Configure your API base URL based on environment:

| Environment | Base URL |
|-------------|----------|
| Development | `http://localhost:5000` |
| Staging | `https://api-staging.wihngo.com` |
| Production | `https://api.wihngo.com` |

---

## Questions?

Contact the backend team or check the API documentation for additional details.
