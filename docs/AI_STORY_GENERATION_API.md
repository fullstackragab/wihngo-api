# AI Story Generation API

## Endpoint

```
POST /api/stories/generate
```

## Authentication

Requires JWT Bearer token in Authorization header.

## Request Body

```json
{
  "birdId": "aaaaaaaa-0001-0001-0001-000000000001",
  "mode": "LoveAndBond",
  "imageS3Key": "users/stories/user-id/story-id/image.png",
  "videoS3Key": "users/videos/user-id/video.mp4",
  "language": "en"
}
```

### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `birdId` | `string` (UUID) | **Yes** | The ID of the bird to generate a story about |
| `mode` | `string` (enum) | No | The mood/tone of the story. See valid values below |
| `imageS3Key` | `string` | No | S3 key of uploaded image for context (future feature) |
| `videoS3Key` | `string` | No | S3 key of uploaded video for context (future feature) |
| `language` | `string` | No | Language code (default: "en") |

### Valid `mode` Values

**IMPORTANT**: The `mode` field must be **exactly** one of these values (case-sensitive):

- `LoveAndBond` - Stories about love, bonding, and affection
- `NewBeginning` - Stories about new beginnings, arrivals, or starting fresh
- `ProgressAndWins` - Stories about achievements, milestones, and celebrations
- `FunnyMoment` - Funny, humorous, or entertaining moments
- `PeacefulMoment` - Calm, peaceful, and serene moments
- `LossAndMemory` - Stories about loss, remembrance, and grief (for memorial birds)
- `CareAndHealth` - Stories about health, medical care, and wellbeing
- `DailyLife` - Everyday life, routine activities, and daily observations

**Note**: `mode` is optional. If not provided, the AI will use a default tone.

### Valid `language` Values

- `en` - English (default)
- `es` - Spanish
- `fr` - French
- `de` - German
- `it` - Italian
- `pt` - Portuguese
- `ja` - Japanese
- `zh` - Chinese

## Response

### Success Response (200 OK)

```json
{
  "generatedContent": "Today was one of those magical moments with Ruby. As the morning sun streamed through the window, my little hummingbird decided it was the perfect time to show off her acrobatic skills...",
  "tokensUsed": 150,
  "generationId": "abc123xyz"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `generatedContent` | `string` | The AI-generated story content (100-500 words) |
| `tokensUsed` | `number` | Number of OpenAI tokens consumed (optional) |
| `generationId` | `string` | Unique ID for this generation (optional, for tracking) |

## Error Responses

### 400 Bad Request - Validation Error

**Missing required field:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "BirdId": ["The birdId field is required."]
  }
}
```

**Invalid mode value:**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "$.mode": ["The JSON value could not be converted to StoryMode. Valid values: LoveAndBond, NewBeginning, ProgressAndWins, FunnyMoment, PeacefulMoment, LossAndMemory, CareAndHealth, DailyLife"]
  }
}
```

### 401 Unauthorized

```json
{
  "error": "Unauthorized",
  "message": "Invalid or expired token"
}
```

### 403 Forbidden

```json
{
  "error": "Forbidden",
  "message": "You do not own this bird"
}
```

### 404 Not Found

```json
{
  "error": "NotFound",
  "message": "Bird not found"
}
```

### 429 Too Many Requests

```json
{
  "error": "TooManyRequests",
  "message": "AI generation limit exceeded. Please try again later.",
  "retryAfter": 3600
}
```

**Rate Limits:**
- 10 generations per hour per user
- 5 generations per hour per bird
- 30 generations per day per user

### 500 Internal Server Error

```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred while generating the story"
}
```

### 503 Service Unavailable

```json
{
  "error": "ServiceUnavailable",
  "message": "OpenAI API key is not configured"
}
```

## Example Request (cURL)

```bash
curl -X POST https://api.wihngo.com/api/stories/generate \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "birdId": "aaaaaaaa-0001-0001-0001-000000000001",
    "mode": "LoveAndBond",
    "language": "en"
  }'
```

## Example Request (JavaScript/TypeScript)

```typescript
const response = await fetch('https://api.wihngo.com/api/stories/generate', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    birdId: 'aaaaaaaa-0001-0001-0001-000000000001',
    mode: 'LoveAndBond', // Must be exact enum value
    language: 'en',
  }),
});

if (!response.ok) {
  const error = await response.json();
  console.error('Error:', error);
  throw new Error(`API Error: ${response.status}`);
}

const data = await response.json();
console.log('Generated story:', data.generatedContent);
```

## Common Issues

### Issue 1: "The request field is required"

**Problem**: Mobile app is sending an incorrect request structure.

**Solution**: Ensure you're sending the fields directly in the JSON body, not nested under a `request` object.

**❌ Wrong:**
```json
{
  "request": {
    "birdId": "...",
    "mode": "LoveAndBond"
  }
}
```

**✅ Correct:**
```json
{
  "birdId": "...",
  "mode": "LoveAndBond"
}
```

### Issue 2: "The JSON value could not be converted to StoryMode"

**Problem**: The `mode` value doesn't match the exact enum values (case-sensitive).

**Solution**: Use exact enum values from the list above.

**❌ Wrong:**
```json
{
  "mode": "love_and_bond"  // Wrong: snake_case
}
```

**❌ Wrong:**
```json
{
  "mode": "loveandBond"  // Wrong: camelCase
}
```

**✅ Correct:**
```json
{
  "mode": "LoveAndBond"  // Correct: PascalCase
}
```

### Issue 3: Optional vs Required

**Remember:**
- **Required**: `birdId` only
- **Optional**: `mode`, `imageS3Key`, `videoS3Key`, `language`

You can omit optional fields entirely:
```json
{
  "birdId": "aaaaaaaa-0001-0001-0001-000000000001"
}
```

## Notes

- The AI generates stories in first-person perspective from the bird owner's point of view
- Story length is typically 100-500 words
- Content will not exceed 5000 characters
- Memorial birds (isMemorial = true) receive sensitive, respectful content
- Image/video analysis is prepared but not yet implemented (Phase 2 feature)
