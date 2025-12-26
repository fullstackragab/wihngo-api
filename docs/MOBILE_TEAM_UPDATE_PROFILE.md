# ? User Profile Update Endpoint - Mobile Team Summary

## ?? What Was Created

A new RESTful API endpoint for users to update their profile information (name, profile image, and bio).

---

## ?? Endpoints

### 1. **Update Profile**
```
PUT /api/users/profile
```
- **Auth Required:** Yes (JWT Bearer token)
- **Purpose:** Update user's name, profile image, or bio
- **Partial updates:** Supported (send only fields to update)

### 2. **Get Profile**
```
GET /api/users/profile
```
- **Auth Required:** Yes (JWT Bearer token)
- **Purpose:** Get current user's profile information

---

## ?? Request Payload (UPDATE)

```json
{
  "name": "John Doe",
  "profileImage": "https://example.com/image.jpg",
  "bio": "Bird enthusiast and nature lover"
}
```

### Field Specifications

| Field | Type | Required | Max Length | Can Be Empty |
|-------|------|----------|------------|--------------|
| `name` | string | ? No | 200 chars | No |
| `profileImage` | string | ? No | 1000 chars | No |
| `bio` | string | ? No | 2000 chars | Yes |

**Rules:**
- ? All fields are optional
- ? At least ONE field must be provided
- ? Only send fields you want to update
- ? Other fields remain unchanged

---

## ? Success Response (200 OK)

```json
{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "profileImage": "https://example.com/image.jpg",
  "bio": "Bird enthusiast and nature lover",
  "emailConfirmed": true,
  "createdAt": "2025-01-10T10:00:00Z",
  "updatedAt": "2025-01-15T14:30:00Z"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `userId` | uuid | User's unique ID |
| `name` | string | User's display name |
| `email` | string | User's email (read-only) |
| `profileImage` | string? | Profile image URL |
| `bio` | string? | User biography |
| `emailConfirmed` | boolean | Email verification status |
| `createdAt` | datetime | Account creation date |
| `updatedAt` | datetime | Last update timestamp |

---

## ? Error Responses

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

### 404 - User Not Found
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

## ?? Example Code

### JavaScript (Fetch)

```javascript
// Update profile
const updateProfile = async (token, updates) => {
  const response = await fetch('https://your-api.com/api/users/profile', {
    method: 'PUT',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(updates)
  });
  
  if (response.ok) {
    return await response.json();
  } else {
    const error = await response.json();
    throw new Error(error.message);
  }
};

// Usage examples:

// Update only name
await updateProfile(token, { name: 'John Smith' });

// Update name and bio
await updateProfile(token, {
  name: 'John Smith',
  bio: 'New bio text'
});

// Update all fields
await updateProfile(token, {
  name: 'John Smith',
  profileImage: 'https://example.com/new-image.jpg',
  bio: 'Complete new bio'
});

// Clear bio
await updateProfile(token, { bio: '' });
```

### TypeScript Types

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

// Usage
const updateProfile = async (
  token: string,
  updates: UpdateProfileRequest
): Promise<UserProfileResponse> => {
  // implementation
};
```

### React Native Example

```typescript
const handleUpdateProfile = async () => {
  try {
    setLoading(true);
    
    const token = await AsyncStorage.getItem('accessToken');
    const updates: UpdateProfileRequest = {
      name: nameInput,
      bio: bioInput,
      profileImage: imageUrl
    };
    
    const response = await fetch('https://your-api.com/api/users/profile', {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(updates)
    });
    
    if (response.ok) {
      const updatedProfile = await response.json();
      setUserProfile(updatedProfile);
      Alert.alert('Success', 'Profile updated!');
      navigation.goBack();
    } else {
      const error = await response.json();
      Alert.alert('Error', error.message);
    }
  } catch (error) {
    Alert.alert('Error', 'Failed to update profile');
  } finally {
    setLoading(false);
  }
};
```

---

## ?? Common Use Cases

### 1. Update Only Name
```json
{ "name": "New Name" }
```

### 2. Update Only Profile Image
```json
{ "profileImage": "https://example.com/new-image.jpg" }
```

### 3. Update Only Bio
```json
{ "bio": "New bio text" }
```

### 4. Update Multiple Fields
```json
{
  "name": "John Doe",
  "bio": "Updated bio"
}
```

### 5. Clear Bio
```json
{ "bio": "" }
```

---

## ?? Authentication

**Required:** Yes

Include JWT token in Authorization header:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**User ID** is extracted from the token automatically - don't send it in the request.

---

## ?? Important Notes

1. **Email cannot be changed** via this endpoint
2. **Password cannot be changed** via this endpoint (use `/api/auth/change-password`)
3. **User ID** is extracted from JWT token (no need to send it)
4. **Partial updates** are fully supported
5. **Token expiry** - refresh token if expired before calling
6. **Validation** - validate inputs on client before sending

---

## ?? Status Codes

| Code | Meaning | What to Do |
|------|---------|------------|
| 200 | Success | Update local state with response |
| 400 | Bad request | Show validation errors to user |
| 401 | Unauthorized | Refresh token or redirect to login |
| 404 | Not found | Redirect to login (user deleted) |
| 500 | Server error | Show error message, offer retry |

---

## ?? Testing

### Test Scenarios

1. ? Update only name
2. ? Update only profile image
3. ? Update only bio
4. ? Update all three fields
5. ? Clear bio (empty string)
6. ? Validation - exceed max lengths
7. ? Authorization - missing token
8. ? Authorization - expired token
9. ? Authorization - invalid token

### Test URLs

**Local:**
```
PUT http://localhost:5000/api/users/profile
GET http://localhost:5000/api/users/profile
```

**Production:**
```
PUT https://your-api-domain.com/api/users/profile
GET https://your-api-domain.com/api/users/profile
```

---

## ?? Documentation Files

| File | Purpose |
|------|---------|
| `API_UPDATE_PROFILE_DOCS.md` | Complete documentation with examples |
| `API_UPDATE_PROFILE_QUICK_REF.md` | Quick reference guide |
| `MOBILE_TEAM_UPDATE_PROFILE.md` | This summary document |

---

## ?? Files Created/Modified

### New Files

1. **Dtos/UserUpdateDto.cs** - Request DTO
2. **Dtos/UserProfileResponseDto.cs** - Response DTO

### Modified Files

1. **Controllers/UsersController.cs** - Added two new endpoints:
   - `PUT /api/users/profile` - Update profile
   - `GET /api/users/profile` - Get profile

---

## ? Ready to Integrate

The endpoints are:
- ? **Fully implemented**
- ? **Built and tested**
- ? **Documented**
- ? **Security validated**
- ? **Ready for mobile integration**

---

## ?? Next Steps

1. **Read** `API_UPDATE_PROFILE_DOCS.md` for complete details
2. **Implement** profile update UI in mobile app
3. **Test** with all scenarios listed above
4. **Handle** error cases gracefully
5. **Validate** inputs on client side
6. **Update** local state after successful update

---

## ?? Integration Tips

1. **Optimize API calls** - Only send fields that changed
2. **Show loading states** - Better UX during updates
3. **Validate locally first** - Catch errors before API call
4. **Handle token expiry** - Refresh token if needed
5. **Update UI immediately** - Better perceived performance
6. **Cache profile data** - Reduce unnecessary API calls

---

## ?? Support

If you have questions about:
- API behavior
- Error handling
- Security concerns
- Integration issues

Contact the backend team or refer to the detailed documentation files.

---

**Created:** January 2025  
**Backend:** .NET 10  
**Authentication:** JWT Bearer Token  
**Status:** ? Ready for Integration
