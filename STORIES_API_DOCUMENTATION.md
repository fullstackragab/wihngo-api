# Stories API Documentation ??

## Base URL
- **Development:** `https://localhost:7297/api/stories` or `http://localhost:5162/api/stories`
- **Production:** `https://your-domain.com/api/stories`

---

## Endpoints

### 1. Get All Stories (Paginated)
Get a paginated list of all stories.

**Endpoint:** `GET /api/stories`

**Authentication:** None (Public)

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | No | 1 | Page number (starts at 1) |
| `pageSize` | int | No | 10 | Number of items per page (1-100) |

**Example Request:**
```http
GET /api/stories?page=1&pageSize=20
```

**Response:** `200 OK`
```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 156,
  "items": [
    {
      "storyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "birds": ["Ruby (Anna's Hummingbird)"],
      "mode": 1,
      "date": "December 20, 2024",
      "preview": "Today Ruby visited the feeder five times! She's becoming more confident and even chased away a larger bird. Her iridescent feathers caught...",
      "imageS3Key": "stories/image123.jpg",
      "imageUrl": "https://s3.amazonaws.com/bucket/presigned-url",
      "videoS3Key": null,
      "videoUrl": null
    }
  ]
}
```

---

### 2. Get Stories by User
Get stories created by a specific user.

**Endpoint:** `GET /api/stories/user/{userId}`

**Authentication:** None (Public)

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `userId` | GUID | Yes | User ID |

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | No | 1 | Page number |
| `pageSize` | int | No | 10 | Items per page |

**Example Request:**
```http
GET /api/stories/user/3fa85f64-5717-4562-b3fc-2c963f66afa6?page=1&pageSize=10
```

**Response:** `200 OK`
Same structure as "Get All Stories"

---

### 3. Get Current User's Stories
Get stories created by the authenticated user.

**Endpoint:** `GET /api/stories/my-stories`

**Authentication:** **Required** (Bearer Token)

**Headers:**
```http
Authorization: Bearer YOUR_JWT_TOKEN
```

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | No | 1 | Page number |
| `pageSize` | int | No | 10 | Items per page |

**Example Request:**
```http
GET /api/stories/my-stories?page=1&pageSize=10
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:** `200 OK`
Same structure as "Get All Stories"

**Error Responses:**
- `401 Unauthorized` - No valid token provided

---

### 4. Get Single Story
Get detailed information about a specific story.

**Endpoint:** `GET /api/stories/{id}`

**Authentication:** None (Public)

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Story ID |

**Example Request:**
```http
GET /api/stories/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Response:** `200 OK`
```json
{
  "storyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "content": "Today Ruby visited the feeder five times! She's becoming more confident and even chased away a larger bird. Her iridescent feathers caught the morning sunlight beautifully. I'm so proud of how far she's come since I first spotted her in April.",
  "mode": 1,
  "imageS3Key": "stories/image123.jpg",
  "imageUrl": "https://s3.amazonaws.com/bucket/presigned-url",
  "videoS3Key": null,
  "videoUrl": null,
  "createdAt": "2024-12-20T10:30:00Z",
  "birds": [
    {
      "birdId": "8fa85f64-5717-4562-b3fc-2c963f66afa7",
      "name": "Ruby",
      "species": "Anna's Hummingbird",
      "imageS3Key": "birds/ruby.jpg",
      "imageUrl": "https://s3.amazonaws.com/bucket/bird-image",
      "tagline": "The fearless little warrior",
      "lovedBy": 45,
      "supportedBy": 12,
      "ownerId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
      "isLoved": false
    }
  ],
  "author": {
    "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
    "name": "Alice Johnson"
  }
}
```

**Error Responses:**
- `404 Not Found` - Story doesn't exist

---

### 5. Create Story
Create a new story.

**Endpoint:** `POST /api/stories`

**Authentication:** **Required** (Bearer Token)

**Headers:**
```http
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Request Body:**
```json
{
  "birdId": "8fa85f64-5717-4562-b3fc-2c963f66afa7",
  "content": "Today Ruby visited the feeder five times!",
  "mode": 1,
  "imageS3Key": "stories/image123.jpg",
  "videoS3Key": null
}
```

**Field Descriptions:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `birdId` | GUID | Yes | ID of the bird this story is about |
| `content` | string | Yes | Story text content |
| `mode` | int? | No | Story mode/mood (see StoryMode enum below) |
| `imageS3Key` | string? | No | S3 key for story image (upload image first via /api/media) |
| `videoS3Key` | string? | No | S3 key for story video (upload video first via /api/media) |

**?? Important Rules:**
- You can have either an image OR a video, not both
- Image/video must be uploaded to S3 first via `/api/media/upload` endpoint
- Bird must exist in the database

**Story Mode Enum:**
```csharp
public enum StoryMode
{
    Happy = 0,
    Excited = 1,
    Calm = 2,
    Worried = 3,
    Sad = 4,
    Playful = 5
}
```

**Example Request:**
```http
POST /api/stories
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "birdId": "8fa85f64-5717-4562-b3fc-2c963f66afa7",
  "content": "Ruby had an amazing day today!",
  "mode": 1,
  "imageS3Key": "stories/ruby-feeding-2024-12-20.jpg"
}
```

**Response:** `200 OK`
Returns the complete story (same structure as "Get Single Story")

**Error Responses:**
- `400 Bad Request` - Validation errors (e.g., both image and video provided, missing content)
- `401 Unauthorized` - No valid token
- `404 Not Found` - Bird doesn't exist or media file not found in S3

---

### 6. Update Story
Update an existing story.

**Endpoint:** `PUT /api/stories/{id}`

**Authentication:** **Required** (Bearer Token - must be story author)

**Headers:**
```http
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Story ID to update |

**Request Body:**
```json
{
  "content": "Updated story content",
  "mode": 2,
  "birdId": "8fa85f64-5717-4562-b3fc-2c963f66afa7",
  "imageS3Key": "stories/new-image.jpg",
  "videoS3Key": null
}
```

**Field Descriptions:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `content` | string? | No | Updated story text (if provided, cannot be empty) |
| `mode` | int? | No | Updated story mode |
| `birdId` | GUID? | No | Change the bird this story is about |
| `imageS3Key` | string? | No | New image (set to empty string "" to remove) |
| `videoS3Key` | string? | No | New video (set to empty string "" to remove) |

**?? Update Rules:**
- Setting a new image will automatically remove any existing video
- Setting a new video will automatically remove any existing image
- Old media files are automatically deleted from S3
- Only the story author can update it

**Example Request:**
```http
PUT /api/stories/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "content": "Updated: Ruby had an even better day!",
  "mode": 1
}
```

**Response:** `204 No Content`

**Error Responses:**
- `400 Bad Request` - Validation errors (e.g., empty content, media not found in S3)
- `401 Unauthorized` - No valid token
- `403 Forbidden` - Not the story author
- `404 Not Found` - Story or bird doesn't exist

---

### 7. Delete Story
Delete a story and its associated media.

**Endpoint:** `DELETE /api/stories/{id}`

**Authentication:** **Required** (Bearer Token - must be story author)

**Headers:**
```http
Authorization: Bearer YOUR_JWT_TOKEN
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | GUID | Yes | Story ID to delete |

**Example Request:**
```http
DELETE /api/stories/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:** `204 No Content`

**?? Important:**
- Deletes the story record from the database
- Automatically deletes associated image/video from S3
- Cannot be undone

**Error Responses:**
- `401 Unauthorized` - No valid token
- `403 Forbidden` - Not the story author
- `404 Not Found` - Story doesn't exist

---

## Data Models

### PagedResult<T>
```typescript
{
  page: number;          // Current page number
  pageSize: number;      // Items per page
  totalCount: number;    // Total number of items
  items: T[];           // Array of items
}
```

### StorySummaryDto
```typescript
{
  storyId: string;       // UUID
  birds: string[];       // Array of bird names
  mode: number | null;   // Story mode (0-5, see enum)
  date: string;          // Formatted date (e.g., "December 20, 2024")
  preview: string;       // First 140 characters of content
  imageS3Key: string | null;  // S3 storage key
  imageUrl: string | null;    // Pre-signed download URL (expires in 10 min)
  videoS3Key: string | null;  // S3 storage key
  videoUrl: string | null;    // Pre-signed download URL (expires in 10 min)
}
```

### StoryReadDto
```typescript
{
  storyId: string;       // UUID
  content: string;       // Full story text
  mode: number | null;   // Story mode
  imageS3Key: string | null;
  imageUrl: string | null;    // Pre-signed URL
  videoS3Key: string | null;
  videoUrl: string | null;    // Pre-signed URL
  createdAt: string;     // ISO 8601 date
  birds: BirdSummaryDto[];
  author: UserSummaryDto;
}
```

### BirdSummaryDto
```typescript
{
  birdId: string;
  name: string;
  species: string;
  imageS3Key: string | null;
  imageUrl: string | null;
  tagline: string;
  lovedBy: number;
  supportedBy: number;
  ownerId: string;
  isLoved: boolean;
}
```

### UserSummaryDto
```typescript
{
  userId: string;
  name: string;
}
```

---

## Common Workflows

### 1. Display Stories Feed
```javascript
// Get first page of stories
const response = await fetch('https://localhost:7297/api/stories?page=1&pageSize=20');
const data = await response.json();

// Display stories
data.items.forEach(story => {
  console.log(`${story.date}: ${story.preview}`);
  if (story.imageUrl) {
    // Display image using story.imageUrl
  }
});

// Load more pages
const nextPage = await fetch(`https://localhost:7297/api/stories?page=${data.page + 1}&pageSize=20`);
```

### 2. Create a Story with Image
```javascript
// Step 1: Upload image first
const formData = new FormData();
formData.append('file', imageFile);
formData.append('folder', 'stories');

const uploadResponse = await fetch('https://localhost:7297/api/media/upload', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`
  },
  body: formData
});

const { s3Key } = await uploadResponse.json();

// Step 2: Create story with the S3 key
const createResponse = await fetch('https://localhost:7297/api/stories', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    birdId: 'your-bird-id',
    content: 'Story content here',
    mode: 1,
    imageS3Key: s3Key
  })
});

const story = await createResponse.json();
```

### 3. Update Story Content Only
```javascript
const response = await fetch(`https://localhost:7297/api/stories/${storyId}`, {
  method: 'PUT',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    content: 'Updated story text'
  })
});

// Returns 204 No Content on success
```

---

## Notes

### Media Handling
- **Pre-signed URLs** expire after 10 minutes
- Always use `imageUrl` or `videoUrl` for displaying media (not `imageS3Key` or `videoS3Key`)
- If URL expires, refetch the story to get a new pre-signed URL
- Upload media via `/api/media/upload` before creating/updating stories

### Pagination
- Default page size is 10
- Maximum page size is 100
- If `page` < 1, defaults to 1
- If `pageSize` < 1, defaults to 10

### Authentication
- Use JWT Bearer token in Authorization header
- Get token from `/api/auth/login` or `/api/auth/register`
- Token must be valid and not expired

### Notifications
- When a story is created, all users who have "loved" that bird receive a notification
- Notifications are sent asynchronously (don't block the API response)

---

## Example: Complete Story Creation Flow

```javascript
// 1. Login to get token
const loginResp = await fetch('https://localhost:7297/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password123'
  })
});
const { token } = await loginResp.json();

// 2. Upload image
const formData = new FormData();
formData.append('file', imageFile);
formData.append('folder', 'stories');

const uploadResp = await fetch('https://localhost:7297/api/media/upload', {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}` },
  body: formData
});
const { s3Key } = await uploadResp.json();

// 3. Create story
const storyResp = await fetch('https://localhost:7297/api/stories', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    birdId: 'your-bird-id-here',
    content: 'Ruby had an amazing day!',
    mode: 1,
    imageS3Key: s3Key
  })
});

const story = await storyResp.json();
console.log('Story created:', story.storyId);
```

---

## Error Handling

All endpoints may return these common errors:

```json
// 400 Bad Request
{
  "message": "Validation failed",
  "errors": {
    "Content": ["The Content field is required."]
  }
}

// 401 Unauthorized
{
  "message": "Authentication required",
  "code": "UNAUTHORIZED"
}

// 403 Forbidden
{
  "message": "Forbidden"
}

// 404 Not Found
{
  "message": "Story not found"
}

// 500 Internal Server Error
{
  "message": "An error occurred"
}
```

Always check the HTTP status code and handle errors appropriately in your application.
