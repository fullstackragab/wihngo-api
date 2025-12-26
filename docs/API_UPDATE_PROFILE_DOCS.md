# ?? Update User Profile - Request & Response

## Complete API Documentation for Mobile Team

---

## ?? Endpoints

### 1. Update Profile
```
PUT /api/users/profile
Content-Type: application/json
Authorization: Bearer {token}
```

### 2. Get Profile
```
GET /api/users/profile
Authorization: Bearer {token}
```

**Base URL:** `https://your-api-domain.com`

---

## ?? UPDATE PROFILE - Request & Response

### Endpoint
```
PUT /api/users/profile
```

### Authentication
**Required:** Yes (JWT Token in Authorization header)

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

### Request Payload

#### JSON Structure

```json
{
  "name": "John Doe Updated",
  "profileImage": "https://example.com/images/new-profile.jpg",
  "bio": "Updated bio - Bird enthusiast and photographer"
}
```

#### Field Specifications

| Field | Type | Required | Validation | Can Be Empty | Description |
|-------|------|----------|------------|--------------|-------------|
| `name` | string | ? No | Max 200 chars | No | User's display name |
| `profileImage` | string | ? No | Max 1000 chars | No | URL to profile image |
| `bio` | string | ? No | Max 2000 chars | Yes | User biography |

**Important Notes:**
- ? **At least one field must be provided** (name, profileImage, or bio)
- ? **All fields are optional** - only send fields you want to update
- ? **Partial updates supported** - unchanged fields remain as they are
- ? **Bio can be empty string** - to clear the bio, send `"bio": ""`
- ? **Null values** - don't send null, omit the field instead

#### DTO Class (C#)

```csharp
public class UserUpdateDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? ProfileImage { get; set; }

    [MaxLength(2000)]
    public string? Bio { get; set; }
}
```

---

### ? Success Response (200 OK)

#### JSON Structure

```json
{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Doe Updated",
  "email": "john.doe@example.com",
  "profileImage": "https://example.com/images/new-profile.jpg",
  "bio": "Updated bio - Bird enthusiast and photographer",
  "emailConfirmed": true,
  "createdAt": "2025-01-10T10:00:00Z",
  "updatedAt": "2025-01-15T14:30:00Z"
}
```

#### Field Specifications

| Field | Type | Description |
|-------|------|-------------|
| `userId` | uuid | Unique user identifier |
| `name` | string | Updated user's name |
| `email` | string | User's email (read-only, cannot be changed via this endpoint) |
| `profileImage` | string (nullable) | URL to profile image |
| `bio` | string (nullable) | User biography |
| `emailConfirmed` | boolean | Email verification status |
| `createdAt` | datetime | Account creation date (ISO 8601) |
| `updatedAt` | datetime | Last update timestamp (ISO 8601) |

#### DTO Class (C#)

```csharp
public class UserProfileResponseDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public string? Bio { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

---

### ? Error Responses

#### 400 Bad Request - No Fields Provided

```json
{
  "message": "At least one field (name, profileImage, or bio) must be provided"
}
```

**When it occurs:**
- Request body is empty
- All fields are null or whitespace
- No fields provided to update

---

#### 400 Bad Request - Validation Failed

```json
{
  "message": "Validation failed",
  "errors": {
    "Name": ["The field Name must be a string with a maximum length of 200."],
    "ProfileImage": ["The field ProfileImage must be a string with a maximum length of 1000."]
  }
}
```

**When it occurs:**
- Name exceeds 200 characters
- ProfileImage URL exceeds 1000 characters
- Bio exceeds 2000 characters

---

#### 401 Unauthorized - Invalid Token

```json
{
  "message": "Invalid authentication token"
}
```

**When it occurs:**
- No Authorization header provided
- Invalid JWT token
- Expired JWT token
- Malformed token

---

#### 404 Not Found - User Not Found

```json
{
  "message": "User not found"
}
```

**When it occurs:**
- User ID from token doesn't exist in database
- User account was deleted

---

#### 500 Internal Server Error

```json
{
  "message": "Failed to update profile. Please try again."
}
```

**When it occurs:**
- Database connection issues
- Unexpected server errors

---

## ?? GET PROFILE - Request & Response

### Endpoint
```
GET /api/users/profile
```

### Authentication
**Required:** Yes (JWT Token in Authorization header)

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

### Request
No request body needed. User ID is extracted from JWT token.

---

### ? Success Response (200 OK)

Same structure as UPDATE response:

```json
{
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "profileImage": "https://example.com/images/profile.jpg",
  "bio": "Bird enthusiast and nature lover",
  "emailConfirmed": true,
  "createdAt": "2025-01-10T10:00:00Z",
  "updatedAt": "2025-01-15T14:30:00Z"
}
```

---

### ? Error Responses

Same error responses as UPDATE endpoint:
- 401: Invalid authentication token
- 404: User not found
- 500: Server error

---

## ?? Example API Calls

### Update Profile - All Fields

#### cURL

```bash
curl -X PUT https://your-api-domain.com/api/users/profile \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe Updated",
    "profileImage": "https://example.com/new-image.jpg",
    "bio": "Updated bio text"
  }'
```

---

#### JavaScript (Fetch)

```javascript
const updateProfile = async (token, updates) => {
  const response = await fetch('https://your-api-domain.com/api/users/profile', {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify(updates)
  });

  if (response.ok) {
    const data = await response.json();
    console.log('Profile updated:', data);
    return data;
  } else {
    const error = await response.json();
    throw new Error(error.message);
  }
};

// Usage examples:

// Update all fields
await updateProfile(accessToken, {
  name: 'John Doe Updated',
  profileImage: 'https://example.com/new-image.jpg',
  bio: 'Updated bio'
});

// Update only name
await updateProfile(accessToken, {
  name: 'John Smith'
});

// Update only profile image
await updateProfile(accessToken, {
  profileImage: 'https://example.com/new-image.jpg'
});

// Clear bio (set to empty)
await updateProfile(accessToken, {
  bio: ''
});
```

---

#### TypeScript (Axios)

```typescript
import axios from 'axios';

interface UpdateProfileRequest {
  name?: string;
  profileImage?: string;
  bio?: string;
}

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

const updateProfile = async (
  token: string,
  updates: UpdateProfileRequest
): Promise<UserProfileResponse> => {
  try {
    const response = await axios.put<UserProfileResponse>(
      'https://your-api-domain.com/api/users/profile',
      updates,
      {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );

    console.log('Profile updated:', response.data);
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response) {
      console.error('Update failed:', error.response.data.message);
      throw new Error(error.response.data.message);
    }
    throw error;
  }
};

// Usage
const profile = await updateProfile(accessToken, {
  name: 'John Doe',
  bio: 'New bio text'
});
```

---

#### React Native Example

```typescript
import AsyncStorage from '@react-native-async-storage/async-storage';

const updateUserProfile = async (updates: {
  name?: string;
  profileImage?: string;
  bio?: string;
}) => {
  try {
    // Get token from storage
    const token = await AsyncStorage.getItem('accessToken');
    if (!token) {
      throw new Error('No authentication token found');
    }

    const response = await fetch('https://your-api-domain.com/api/users/profile', {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(updates)
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message);
    }

    const updatedProfile = await response.json();
    
    // Update local state/context with new profile
    // setUserProfile(updatedProfile);
    
    return updatedProfile;
  } catch (error) {
    console.error('Profile update error:', error);
    throw error;
  }
};

// Usage in component
const handleUpdateProfile = async () => {
  try {
    setLoading(true);
    
    const updated = await updateUserProfile({
      name: nameInput,
      bio: bioInput,
      profileImage: imageUrl
    });
    
    Alert.alert('Success', 'Profile updated successfully');
    navigation.goBack();
  } catch (error) {
    Alert.alert('Error', error.message);
  } finally {
    setLoading(false);
  }
};
```

---

### Get Profile

#### JavaScript (Fetch)

```javascript
const getProfile = async (token) => {
  const response = await fetch('https://your-api-domain.com/api/users/profile', {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  if (response.ok) {
    const profile = await response.json();
    return profile;
  } else {
    const error = await response.json();
    throw new Error(error.message);
  }
};

// Usage
const profile = await getProfile(accessToken);
console.log('User profile:', profile);
```

---

### Python (Requests)

```python
import requests

def update_profile(token, updates):
    """Update user profile"""
    url = "https://your-api-domain.com/api/users/profile"
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
    
    response = requests.put(url, json=updates, headers=headers)
    
    if response.status_code == 200:
        profile = response.json()
        print(f"Profile updated: {profile['name']}")
        return profile
    else:
        error = response.json()
        raise Exception(f"Update failed: {error['message']}")

# Usage
profile = update_profile(access_token, {
    "name": "John Doe",
    "bio": "Updated bio"
})
```

---

## ?? Update Strategies

### 1. Update Single Field

Only send the field you want to change:

```json
{
  "name": "New Name"
}
```

or

```json
{
  "profileImage": "https://example.com/new-image.jpg"
}
```

---

### 2. Update Multiple Fields

Send multiple fields at once:

```json
{
  "name": "John Doe",
  "bio": "Bird lover and photographer"
}
```

---

### 3. Clear Bio

To remove/clear the bio, send an empty string:

```json
{
  "bio": ""
}
```

---

### 4. Update All Fields

Send all three fields:

```json
{
  "name": "John Doe Updated",
  "profileImage": "https://example.com/new-image.jpg",
  "bio": "Complete new bio text"
}
```

---

## ?? Security & Best Practices

### Client-Side

? **Always include Authorization header** with valid JWT token  
? **Validate inputs locally** before sending to API  
? **Handle token expiration** - refresh if needed  
? **Sanitize user input** - especially URLs  
? **Show loading states** during updates  
? **Update local state** after successful update  
? **Handle errors gracefully** - show user-friendly messages  

### Field Validation (Client-Side)

```javascript
const validateProfileUpdate = (updates) => {
  const errors = {};
  
  if (updates.name !== undefined) {
    if (updates.name.trim().length === 0) {
      errors.name = 'Name cannot be empty';
    }
    if (updates.name.length > 200) {
      errors.name = 'Name must be 200 characters or less';
    }
  }
  
  if (updates.profileImage !== undefined) {
    if (updates.profileImage.trim().length === 0) {
      errors.profileImage = 'Profile image URL cannot be empty';
    }
    if (updates.profileImage.length > 1000) {
      errors.profileImage = 'URL must be 1000 characters or less';
    }
    // Validate URL format
    try {
      new URL(updates.profileImage);
    } catch {
      errors.profileImage = 'Invalid URL format';
    }
  }
  
  if (updates.bio !== undefined && updates.bio.length > 2000) {
    errors.bio = 'Bio must be 2000 characters or less';
  }
  
  // Check if at least one field is provided
  if (!updates.name && !updates.profileImage && updates.bio === undefined) {
    errors.general = 'At least one field must be updated';
  }
  
  return Object.keys(errors).length > 0 ? errors : null;
};
```

---

## ?? Response Status Codes

| Status Code | Meaning | When | Action |
|-------------|---------|------|--------|
| **200 OK** | Success | Profile updated successfully | Update local state with response |
| **400 Bad Request** | Validation error | Invalid input data | Show validation errors to user |
| **401 Unauthorized** | Authentication failed | Invalid/expired token | Refresh token or redirect to login |
| **404 Not Found** | User not found | User doesn't exist | Redirect to login |
| **500 Server Error** | Server issue | Unexpected error | Show error, retry option |

---

## ?? Complete User Flow Example

```typescript
// 1. User edits profile in UI
const handleSaveProfile = async () => {
  try {
    // 2. Validate locally
    const errors = validateProfileUpdate({
      name: nameInput,
      bio: bioInput,
      profileImage: imageUrl
    });
    
    if (errors) {
      setErrors(errors);
      return;
    }
    
    // 3. Show loading state
    setLoading(true);
    
    // 4. Get token
    const token = await getAccessToken();
    
    // 5. Check if token is expired
    if (isTokenExpired(token)) {
      const newToken = await refreshAccessToken();
      await saveAccessToken(newToken);
    }
    
    // 6. Build update payload (only changed fields)
    const updates = {};
    if (nameInput !== currentProfile.name) {
      updates.name = nameInput;
    }
    if (bioInput !== currentProfile.bio) {
      updates.bio = bioInput;
    }
    if (imageUrl !== currentProfile.profileImage) {
      updates.profileImage = imageUrl;
    }
    
    // 7. Call API
    const updatedProfile = await updateProfile(token, updates);
    
    // 8. Update local state/context
    setUserProfile(updatedProfile);
    
    // 9. Show success message
    showToast('Profile updated successfully');
    
    // 10. Navigate back
    navigation.goBack();
    
  } catch (error) {
    // 11. Handle errors
    if (error.message.includes('Authentication')) {
      // Token refresh failed - redirect to login
      navigation.navigate('Login');
    } else {
      // Show error message
      showToast(error.message || 'Failed to update profile');
    }
  } finally {
    // 12. Hide loading state
    setLoading(false);
  }
};
```

---

## ?? Quick Reference

### Update Profile
```
PUT /api/users/profile
Headers: Authorization: Bearer {token}
Body: { "name": "...", "profileImage": "...", "bio": "..." }
Response: UserProfileResponseDto (200 OK)
```

### Get Profile
```
GET /api/users/profile
Headers: Authorization: Bearer {token}
Response: UserProfileResponseDto (200 OK)
```

### Minimum Payload
```json
{
  "name": "New Name"
}
```

### Full Payload
```json
{
  "name": "John Doe",
  "profileImage": "https://example.com/image.jpg",
  "bio": "Bird enthusiast"
}
```

---

## ?? Related Endpoints

- `POST /api/auth/register` - Create new account
- `POST /api/auth/login` - User login
- `POST /api/auth/change-password` - Change password
- `POST /api/auth/refresh-token` - Refresh access token
- `GET /api/users/profile` - Get current user profile

---

## ?? Common Issues & Solutions

### Issue: "Invalid authentication token"
**Solution:** Check that token is:
- Present in Authorization header
- Formatted as `Bearer {token}`
- Not expired (refresh if needed)

### Issue: "At least one field must be provided"
**Solution:** Send at least one of: name, profileImage, or bio

### Issue: "Validation failed"
**Solution:** Check field lengths:
- Name ? 200 characters
- ProfileImage ? 1000 characters
- Bio ? 2000 characters

### Issue: "User not found"
**Solution:** User account may have been deleted. Redirect to login.

---

**Last Updated:** January 2025  
**API Version:** v1.0  
**Backend:** .NET 10  
**Authentication:** JWT Bearer Token
