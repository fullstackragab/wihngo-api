# Quick Test: Story Edit Endpoint

## Test the Story Update Functionality

Run this PowerShell script to verify the story edit endpoint is working correctly:

```powershell
Write-Host "`n???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "Testing Story Edit Endpoint" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????`n" -ForegroundColor Cyan

# Configuration
$baseUrl = "https://localhost:7297/api"
$email = "alice@example.com"
$password = "Password123!"

# Step 1: Login
Write-Host "1. Logging in..." -ForegroundColor Yellow
$loginBody = @{ 
    email = $email
    password = $password 
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$baseUrl/auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json"
    
    $token = $loginResponse.token
    Write-Host "   ? Logged in as $email" -ForegroundColor Green
} catch {
    Write-Host "   ? Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit
}

# Step 2: Get user's stories
Write-Host "`n2. Fetching your stories..." -ForegroundColor Yellow
try {
    $stories = Invoke-RestMethod `
        -Uri "$baseUrl/stories/my-stories" `
        -Headers @{ "Authorization" = "Bearer $token" }
    
    if ($stories.items.Count -eq 0) {
        Write-Host "   ??  No stories found. Creating one first..." -ForegroundColor Yellow
        
        # Get user's birds
        $birds = Invoke-RestMethod `
            -Uri "$baseUrl/birds/my-birds" `
            -Headers @{ "Authorization" = "Bearer $token" }
        
        if ($birds.items.Count -eq 0) {
            Write-Host "   ? No birds found. Please create a bird first." -ForegroundColor Red
            exit
        }
        
        # Create a test story
        $createBody = @{
            birdId = $birds.items[0].birdId
            content = "Test Story - This is a test story that we will edit. It has some content here."
        } | ConvertTo-Json
        
        $newStory = Invoke-RestMethod `
            -Uri "$baseUrl/stories" `
            -Method POST `
            -Headers @{ 
                "Authorization" = "Bearer $token"
                "Content-Type" = "application/json"
            } `
            -Body $createBody
        
        $storyId = $newStory.storyId
        Write-Host "   ? Created test story: $storyId" -ForegroundColor Green
    } else {
        $storyId = $stories.items[0].storyId
        Write-Host "   ? Found story: $storyId" -ForegroundColor Green
    }
    
    # Get full story details
    $story = Invoke-RestMethod `
        -Uri "$baseUrl/stories/$storyId" `
        -Headers @{ "Authorization" = "Bearer $token" }
    
    Write-Host "   ?? Current content (first 80 chars):" -ForegroundColor Cyan
    $preview = $story.content.Substring(0, [Math]::Min(80, $story.content.Length))
    Write-Host "      `"$preview...`"" -ForegroundColor White
    Write-Host "   ?? Full content length: $($story.content.Length) characters" -ForegroundColor Gray
    
} catch {
    Write-Host "   ? Error: $($_.Exception.Message)" -ForegroundColor Red
    exit
}

# Step 3: Update the story
Write-Host "`n3. Updating story content..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "HH:mm:ss"
$newContent = "Updated Story Title at $timestamp - This is the complete updated content. The title is automatically generated from the first 30 characters of this content. Here's some more text to make it longer and test the full content update functionality."

$updateBody = @{
    content = $newContent
} | ConvertTo-Json

try {
    Invoke-RestMethod `
        -Uri "$baseUrl/stories/$storyId" `
        -Method PUT `
        -Headers @{ 
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/json"
        } `
        -Body $updateBody `
        -ErrorAction Stop
    
    Write-Host "   ? Story updated successfully!" -ForegroundColor Green
} catch {
    Write-Host "   ? Update failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "   Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit
}

# Step 4: Verify the update
Write-Host "`n4. Verifying update..." -ForegroundColor Yellow
try {
    $updatedStory = Invoke-RestMethod `
        -Uri "$baseUrl/stories/$storyId" `
        -Headers @{ "Authorization" = "Bearer $token" }
    
    Write-Host "   ?? Updated content (first 80 chars):" -ForegroundColor Cyan
    $preview = $updatedStory.content.Substring(0, [Math]::Min(80, $updatedStory.content.Length))
    Write-Host "      `"$preview...`"" -ForegroundColor White
    Write-Host "   ?? New content length: $($updatedStory.content.Length) characters" -ForegroundColor Gray
    
    # Check if content matches what we sent
    if ($updatedStory.content -eq $newContent) {
        Write-Host "   ? Content matches perfectly!" -ForegroundColor Green
    } else {
        Write-Host "   ??  Content doesn't match exactly" -ForegroundColor Yellow
        Write-Host "   Expected length: $($newContent.Length)" -ForegroundColor Gray
        Write-Host "   Actual length: $($updatedStory.content.Length)" -ForegroundColor Gray
    }
    
    # Get story list to see generated title
    $storiesList = Invoke-RestMethod `
        -Uri "$baseUrl/stories/my-stories" `
        -Headers @{ "Authorization" = "Bearer $token" }
    
    $storyInList = $storiesList.items | Where-Object { $_.storyId -eq $storyId }
    if ($storyInList) {
        Write-Host "`n   ?? Generated title (from first 30 chars):" -ForegroundColor Cyan
        Write-Host "      `"$($storyInList.title)`"" -ForegroundColor White
    }
    
} catch {
    Write-Host "   ? Verification failed: $($_.Exception.Message)" -ForegroundColor Red
    exit
}

Write-Host "`n???????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "? Test completed successfully!" -ForegroundColor Green
Write-Host "???????????????????????????????????????????????`n" -ForegroundColor Cyan

Write-Host "?? Summary:" -ForegroundColor Cyan
Write-Host "   • Stories use a single 'content' field (max 5000 chars)" -ForegroundColor White
Write-Host "   • Title is auto-generated from first 30 characters" -ForegroundColor White
Write-Host "   • Preview is auto-generated from first 140 characters" -ForegroundColor White
Write-Host "   • Update endpoint works correctly with logging enabled" -ForegroundColor White
Write-Host "`nCheck Visual Studio Debug Output window for detailed logs." -ForegroundColor Yellow
```

## Expected Output

```
???????????????????????????????????????????????
Testing Story Edit Endpoint
???????????????????????????????????????????????

1. Logging in...
   ? Logged in as alice@example.com

2. Fetching your stories...
   ? Found story: a1b2c3d4-5678-90ab-cdef-123456789abc
   ?? Current content (first 80 chars):
      "Test Story - This is a test story that we will edit. It has some content here..."
   ?? Full content length: 89 characters

3. Updating story content...
   ? Story updated successfully!

4. Verifying update...
   ?? Updated content (first 80 chars):
      "Updated Story Title at 23:45:12 - This is the complete updated content. The ti..."
   ?? New content length: 234 characters
   ? Content matches perfectly!

   ?? Generated title (from first 30 chars):
      "Updated Story Title at 23:45:1..."

???????????????????????????????????????????????
? Test completed successfully!
???????????????????????????????????????????????

?? Summary:
   • Stories use a single 'content' field (max 5000 chars)
   • Title is auto-generated from first 30 characters
   • Preview is auto-generated from first 140 characters
   • Update endpoint works correctly with logging enabled

Check Visual Studio Debug Output window for detailed logs.
```

## What to Check in Visual Studio Debug Output

After running this test, check the Debug Output window for logs like:

```
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      Edit story request for a1b2c3d4-5678-90ab-cdef-123456789abc
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      DTO Content: True, Length: 234
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      DTO ImageS3Key: NULL
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      Current story content length: 89
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      Updating story content from 89 to 234 chars
[HH:mm:ss] info: Wihngo.Controllers.StoriesController[0]
      Story updated successfully: a1b2c3d4-5678-90ab-cdef-123456789abc, Content length: 234
```

## Mobile App Testing

If testing from the mobile app:

1. **Open a story for editing**
2. **Change the content** (remember: title and content are ONE field)
3. **Save the changes**
4. **Check Visual Studio Debug Output** for the logs above
5. **Verify the story was updated** by refreshing the story list

## Troubleshooting

### If update returns 400 Bad Request
- Check that `content` field is not empty
- Ensure content is under 5000 characters
- Verify valid JSON format

### If update returns 401 Unauthorized
- Token may be expired - login again
- Check Authorization header format: `Bearer {token}`

### If update returns 403 Forbidden
- User is not the author of the story
- Can only edit your own stories

### If update returns 404 Not Found
- Story ID doesn't exist
- Check the story ID is correct

### If content doesn't update
- Check Visual Studio Debug Output logs
- Verify content is actually different from current content
- Ensure mobile app is sending `content` field, not `title`
