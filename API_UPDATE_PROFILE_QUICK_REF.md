# ?? Update Profile API - Quick Reference for Mobile Team

## Endpoints

### Update Profile
```
PUT /api/users/profile
Authorization: Bearer {token}
```

### Get Profile
```
GET /api/users/profile
Authorization: Bearer {token}
```

---

## Request (UPDATE)

### Payload
```json
{
  "name": "John Doe",
  "profileImage": "https://example.com/image.jpg",
  "bio": "Bird lover"
}
```

### Rules
- ? All fields are **optional**
- ? Send only fields you want to update
- ? **At least ONE field required**
- ? Bio can be empty string `""` to clear it

### Field Limits
- `name`: Max 200 characters
- `profileImage`: Max 1000 characters (URL)
- `bio`: Max 2000 characters

---

## Success Response (200 OK)

```json
{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Doe",
  "email": "john@example.com",
  "profileImage": "https://example.com/image.jpg",
  "bio": "Bird lover",
  "emailConfirmed": true,
  "createdAt": "2025-01-10T10:00:00Z",
  "updatedAt": "2025-01-15T14:30:00Z"
}
```

---

## Error Responses

### 400 - No Fields Provided
```json
{
  "message": "At least one field (name, profileImage, or bio) must be provided"
}
```

### 400 - Validation Failed
```json
{
  "message": "Validation failed",
  "errors": {
    "Name": ["The field Name must be a string with a maximum length of 200."]
  }
}
```

### 401 - Unauthorized
```json
{
  "message": "Invalid authentication token"
}
```

### 404 - Not Found
```json
{
  "message": "User not found"
}
```

### 500 - Server Error
```json
{
  "message": "Failed to update profile. Please try again."
}
```

---

## Usage Examples

### Update Only Name
```javascript
await fetch('/api/users/profile', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    name: 'John Smith'
  })
});
```

### Update Name + Bio
```javascript
await fetch('/api/users/profile', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    name: 'John Smith',
    bio: 'Updated bio text'
  })
});
```

### Clear Bio
```javascript
await fetch('/api/users/profile', {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    bio: ''
  })
});
```

### Get Profile
```javascript
const response = await fetch('/api/users/profile', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
const profile = await response.json();
```

---

## TypeScript Types

```typescript
// Request
interface UpdateProfileRequest {
  name?: string;
  profileImage?: string;
  bio?: string;
}

// Response
interface UserProfileResponse {
  userId: string;
  name: string;
  email: string;
  profileImage?: string;
  bio?: string;
  emailConfirmed: boolean;
  createdAt: string;
  updatedAt: string;
}
```

---

## Status Codes

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | Update local state |
| 400 | Bad request | Show validation errors |
| 401 | Unauthorized | Refresh token or login |
| 404 | Not found | Redirect to login |
| 500 | Server error | Show error, retry |

---

## Important Notes

1. **User ID** is extracted from JWT token (don't send it)
2. **Email** cannot be changed via this endpoint
3. **Password** cannot be changed via this endpoint (use `/api/auth/change-password`)
4. **Partial updates** are supported
5. **Token expiry**: Check and refresh if needed before calling

---

## Complete Documentation

See `API_UPDATE_PROFILE_DOCS.md` for:
- Detailed error handling
- React Native examples
- Validation logic
- Security best practices
- Complete workflows
