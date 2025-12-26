# Raw SQL Implementation Examples

This file contains working examples of common database operations using Dapper + Raw SQL.
Copy and adapt these patterns to replace EF Core code throughout the application.

## Table of Contents
1. [Basic CRUD Operations](#basic-crud-operations)
2. [Querying with Joins](#querying-with-joins)
3. [Pagination](#pagination)
4. [Transactions](#transactions)
5. [Complex Queries](#complex-queries)

---

## Basic CRUD Operations

### Select All Records

```csharp
// Get all stories
public async Task<List<Story>> GetAllStories()
{
    var sql = "SELECT * FROM stories ORDER BY created_at DESC";
    return await _dbFactory.QueryListAsync<Story>(sql);
}
```

### Select Single Record by ID

```csharp
// Get story by ID
public async Task<Story?> GetStoryById(Guid storyId)
{
    var sql = "SELECT * FROM stories WHERE story_id = @StoryId";
    return await _dbFactory.QuerySingleOrDefaultAsync<Story>(sql, new { StoryId = storyId });
}
```

### Select with WHERE Clause

```csharp
// Get stories by author
public async Task<List<Story>> GetStoriesByAuthor(Guid authorId)
{
    var sql = @"
        SELECT * FROM stories 
        WHERE author_id = @AuthorId 
        ORDER BY created_at DESC";
    
    return await _dbFactory.QueryListAsync<Story>(sql, new { AuthorId = authorId });
}
```

### Count Records

```csharp
// Count all stories
public async Task<int> CountStories()
{
    return await _dbFactory.QueryCountAsync("stories");
}

// Count with WHERE
public async Task<int> CountStoriesByAuthor(Guid authorId)
{
    return await _dbFactory.QueryCountAsync(
        "stories", 
        "author_id = @AuthorId", 
        new { AuthorId = authorId });
}
```

### Check if Record Exists

```csharp
// Check if story exists
public async Task<bool> StoryExists(Guid storyId)
{
    return await _dbFactory.QueryExistsAsync(
        "stories",
        "story_id = @StoryId",
        new { StoryId = storyId });
}
```

### Insert Record

```csharp
// Insert new story
public async Task<Guid> CreateStory(Story story)
{
    var sql = @"
        INSERT INTO stories (
            story_id, author_id, content, mode, 
            image_url, video_url, created_at
        )
        VALUES (
            @StoryId, @AuthorId, @Content, @Mode,
            @ImageUrl, @VideoUrl, @CreatedAt
        )";
    
    await _dbFactory.ExecuteAsync(sql, new
    {
        StoryId = story.StoryId,
        AuthorId = story.AuthorId,
        Content = story.Content,
        Mode = story.Mode?.ToString(), // Enum to string
        ImageUrl = story.ImageUrl,
        VideoUrl = story.VideoUrl,
        CreatedAt = story.CreatedAt
    });
    
    return story.StoryId;
}
```

### Update Record

```csharp
// Update story
public async Task<bool> UpdateStory(Story story)
{
    var sql = @"
        UPDATE stories 
        SET 
            content = @Content,
            mode = @Mode,
            image_url = @ImageUrl,
            video_url = @VideoUrl
        WHERE story_id = @StoryId";
    
    var rowsAffected = await _dbFactory.ExecuteAsync(sql, new
    {
        StoryId = story.StoryId,
        Content = story.Content,
        Mode = story.Mode?.ToString(),
        ImageUrl = story.ImageUrl,
        VideoUrl = story.VideoUrl
    });
    
    return rowsAffected > 0;
}
```

### Delete Record

```csharp
// Delete story
public async Task<bool> DeleteStory(Guid storyId)
{
    var sql = "DELETE FROM stories WHERE story_id = @StoryId";
    var rowsAffected = await _dbFactory.ExecuteAsync(sql, new { StoryId = storyId });
    return rowsAffected > 0;
}
```

---

## Querying with Joins

### One-to-Many with Manual Mapping

```csharp
// Get story with its birds
public async Task<Story?> GetStoryWithBirds(Guid storyId)
{
    var connection = await _dbFactory.CreateOpenConnectionAsync();
    try
    {
        var sql = @"
            SELECT 
                s.*,
                sb.story_bird_id, sb.bird_id, sb.created_at as story_bird_created_at,
                b.bird_id as bird_id, b.name as bird_name, b.species, b.image_url as bird_image_url
            FROM stories s
            LEFT JOIN story_birds sb ON s.story_id = sb.story_id
            LEFT JOIN birds b ON sb.bird_id = b.bird_id
            WHERE s.story_id = @StoryId";

        Story? story = null;
        
        await connection.QueryAsync<Story, StoryBird, Bird, Story>(
            sql,
            (s, storyBird, bird) =>
            {
                if (story == null)
                {
                    story = s;
                    story.StoryBirds = new List<StoryBird>();
                }

                if (storyBird != null && bird != null)
                {
                    storyBird.Bird = bird;
                    if (!story.StoryBirds.Any(sb => sb.StoryBirdId == storyBird.StoryBirdId))
                    {
                        story.StoryBirds.Add(storyBird);
                    }
                }

                return story;
            },
            new { StoryId = storyId },
            splitOn: "story_bird_id,bird_id");

        return story;
    }
    finally
    {
        await connection.DisposeAsync();
    }
}
```

### Many-to-One (Include Author)

```csharp
// Get story with author
public async Task<Story?> GetStoryWithAuthor(Guid storyId)
{
    var sql = @"
        SELECT 
            s.*,
            u.user_id as author_user_id, u.name as author_name, u.email as author_email
        FROM stories s
        LEFT JOIN users u ON s.author_id = u.user_id
        WHERE s.story_id = @StoryId";

    var connection = await _dbFactory.CreateOpenConnectionAsync();
    try
    {
        var result = await connection.QueryAsync<Story, User, Story>(
            sql,
            (story, author) =>
            {
                story.Author = author;
                return story;
            },
            new { StoryId = storyId },
            splitOn: "author_user_id");

        return result.FirstOrDefault();
    }
    finally
    {
        await connection.DisposeAsync();
    }
}
```

### Complex Multi-Level Joins

```csharp
// Get story with author and birds (complete graph)
public async Task<Story?> GetCompleteStory(Guid storyId)
{
    var connection = await _dbFactory.CreateOpenConnectionAsync();
    try
    {
        var sql = @"
            SELECT 
                s.story_id, s.content, s.mode, s.image_url, s.video_url, s.created_at,
                u.user_id, u.name, u.email,
                sb.story_bird_id, sb.bird_id, sb.created_at as sb_created_at,
                b.bird_id, b.name, b.species, b.image_url, b.video_url, 
                b.tagline, b.loved_count, b.supported_count, b.owner_id
            FROM stories s
            LEFT JOIN users u ON s.author_id = u.user_id
            LEFT JOIN story_birds sb ON s.story_id = sb.story_id
            LEFT JOIN birds b ON sb.bird_id = b.bird_id
            WHERE s.story_id = @StoryId";

        Story? story = null;
        
        await connection.QueryAsync<Story, User, StoryBird, Bird, Story>(
            sql,
            (s, author, storyBird, bird) =>
            {
                if (story == null)
                {
                    story = s;
                    story.Author = author;
                    story.StoryBirds = new List<StoryBird>();
                }

                if (storyBird != null && bird != null)
                {
                    storyBird.Bird = bird;
                    if (!story.StoryBirds.Any(sb => sb.StoryBirdId == storyBird.StoryBirdId))
                    {
                        story.StoryBirds.Add(storyBird);
                    }
                }

                return story;
            },
            new { StoryId = storyId },
            splitOn: "user_id,story_bird_id,bird_id");

        return story;
    }
    finally
    {
        await connection.DisposeAsync();
    }
}
```

---

## Pagination

### Basic Pagination

```csharp
// Get paginated stories
public async Task<(List<Story> items, int total)> GetStoriesPaginated(int page, int pageSize)
{
    // Get total count
    var total = await _dbFactory.QueryCountAsync("stories");

    // Get paginated items
    var offset = (page - 1) * pageSize;
    var sql = @"
        SELECT * FROM stories 
        ORDER BY created_at DESC 
        OFFSET @Offset LIMIT @Limit";
    
    var items = await _dbFactory.QueryListAsync<Story>(sql, new { Offset = offset, Limit = pageSize });

    return (items, total);
}
```

### Pagination with Filter

```csharp
// Get paginated stories by author
public async Task<(List<Story> items, int total)> GetStoriesByAuthorPaginated(
    Guid authorId, 
    int page, 
    int pageSize)
{
    // Get total count for this author
    var total = await _dbFactory.QueryCountAsync(
        "stories", 
        "author_id = @AuthorId", 
        new { AuthorId = authorId });

    // Get paginated items
    var offset = (page - 1) * pageSize;
    var sql = @"
        SELECT * FROM stories 
        WHERE author_id = @AuthorId
        ORDER BY created_at DESC 
        OFFSET @Offset LIMIT @Limit";
    
    var items = await _dbFactory.QueryListAsync<Story>(sql, new 
    { 
        AuthorId = authorId, 
        Offset = offset, 
        Limit = pageSize 
    });

    return (items, total);
}
```

---

## Transactions

### Simple Transaction

```csharp
// Create story with story-bird relationships in a transaction
public async Task<Story> CreateStoryWithBirds(Story story, List<Guid> birdIds)
{
    var connection = await _dbFactory.CreateOpenConnectionAsync();
    using var transaction = await connection.BeginTransactionAsync();
    
    try
    {
        // Insert story
        var storySql = @"
            INSERT INTO stories (story_id, author_id, content, mode, image_url, video_url, created_at)
            VALUES (@StoryId, @AuthorId, @Content, @Mode, @ImageUrl, @VideoUrl, @CreatedAt)";
        
        await connection.ExecuteAsync(storySql, new
        {
            StoryId = story.StoryId,
            AuthorId = story.AuthorId,
            Content = story.Content,
            Mode = story.Mode?.ToString(),
            ImageUrl = story.ImageUrl,
            VideoUrl = story.VideoUrl,
            CreatedAt = story.CreatedAt
        }, transaction);

        // Insert story-bird relationships
        var storyBirdSql = @"
            INSERT INTO story_birds (story_bird_id, story_id, bird_id, created_at)
            VALUES (@StoryBirdId, @StoryId, @BirdId, @CreatedAt)";
        
        foreach (var birdId in birdIds)
        {
            await connection.ExecuteAsync(storyBirdSql, new
            {
                StoryBirdId = Guid.NewGuid(),
                StoryId = story.StoryId,
                BirdId = birdId,
                CreatedAt = DateTime.UtcNow
            }, transaction);
        }

        await transaction.CommitAsync();
        return story;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
    finally
    {
        await connection.DisposeAsync();
    }
}
```

---

## Complex Queries

### Aggregate with GROUP BY

```csharp
// Get bird story counts
public async Task<List<(Guid BirdId, string BirdName, int StoryCount)>> GetBirdStoryCounts()
{
    var sql = @"
        SELECT 
            b.bird_id,
            b.name,
            COUNT(DISTINCT s.story_id) as story_count
        FROM birds b
        LEFT JOIN story_birds sb ON b.bird_id = sb.bird_id
        LEFT JOIN stories s ON sb.story_id = s.story_id
        GROUP BY b.bird_id, b.name
        ORDER BY story_count DESC";

    var connection = await _dbFactory.CreateOpenConnectionAsync();
    try
    {
        var results = await connection.QueryAsync<(Guid BirdId, string Name, int StoryCount)>(sql);
        return results.ToList();
    }
    finally
    {
        await connection.DisposeAsync();
    }
}
```

### Subquery

```csharp
// Get users with more than 5 stories
public async Task<List<User>> GetActiveAuthors()
{
    var sql = @"
        SELECT u.*
        FROM users u
        WHERE (
            SELECT COUNT(*) 
            FROM stories s 
            WHERE s.author_id = u.user_id
        ) > 5
        ORDER BY u.name";

    return await _dbFactory.QueryListAsync<User>(sql);
}
```

### CTE (Common Table Expression)

```csharp
// Get birds with their latest story
public async Task<List<dynamic>> GetBirdsWithLatestStory()
{
    var sql = @"
        WITH latest_stories AS (
            SELECT DISTINCT ON (sb.bird_id)
                sb.bird_id,
                s.story_id,
                s.content,
                s.created_at
            FROM story_birds sb
            JOIN stories s ON sb.story_id = s.story_id
            ORDER BY sb.bird_id, s.created_at DESC
        )
        SELECT 
            b.bird_id,
            b.name,
            ls.content as latest_story_content,
            ls.created_at as latest_story_date
        FROM birds b
        LEFT JOIN latest_stories ls ON b.bird_id = ls.bird_id
        ORDER BY b.name";

    return await _dbFactory.QueryListAsync<dynamic>(sql);
}
```

### Window Functions

```csharp
// Get stories with row numbers
public async Task<List<dynamic>> GetStoriesWithRanking()
{
    var sql = @"
        SELECT 
            story_id,
            author_id,
            content,
            created_at,
            ROW_NUMBER() OVER (PARTITION BY author_id ORDER BY created_at DESC) as story_rank,
            COUNT(*) OVER (PARTITION BY author_id) as author_total_stories
        FROM stories
        ORDER BY author_id, created_at DESC";

    return await _dbFactory.QueryListAsync<dynamic>(sql);
}
```

### CASE Statements

```csharp
// Get stories with categorization
public async Task<List<dynamic>> GetCategorizedStories()
{
    var sql = @"
        SELECT 
            story_id,
            content,
            created_at,
            CASE 
                WHEN created_at >= CURRENT_DATE - INTERVAL '7 days' THEN 'Recent'
                WHEN created_at >= CURRENT_DATE - INTERVAL '30 days' THEN 'This Month'
                ELSE 'Older'
            END as time_category
        FROM stories
        ORDER BY created_at DESC";

    return await _dbFactory.QueryListAsync<dynamic>(sql);
}
```

---

## Performance Tips

1. **Use Indexes**: Ensure WHERE clause columns have indexes
   ```sql
   CREATE INDEX idx_stories_author_id ON stories(author_id);
   CREATE INDEX idx_stories_created_at ON stories(created_at DESC);
   ```

2. **Limit SELECT columns**: Only select what you need
   ```csharp
   var sql = "SELECT story_id, content FROM stories"; // Not SELECT *
   ```

3. **Use EXPLAIN ANALYZE**: Test query performance
   ```sql
   EXPLAIN ANALYZE 
   SELECT * FROM stories WHERE author_id = 'some-guid';
   ```

4. **Batch inserts**: Use table-valued parameters for multiple inserts
   ```csharp
   await connection.ExecuteAsync(sql, collectionOfObjects);
   ```

5. **Connection pooling**: Already handled by Npgsql - just dispose connections properly

---

## Common Patterns Summary

| Operation | Pattern |
|-----------|---------|
| Get One | `QuerySingleOrDefaultAsync<T>` with WHERE |
| Get Many | `QueryListAsync<T>` |
| Count | `QueryCountAsync` or `ExecuteScalarAsync<int>` |
| Exists | `QueryExistsAsync` |
| Insert | `ExecuteAsync` with INSERT |
| Update | `ExecuteAsync` with UPDATE |
| Delete | `ExecuteAsync` with DELETE |
| Join (1-to-many) | Multi-mapping with dictionary deduplication |
| Pagination | OFFSET + LIMIT with separate COUNT |
| Transaction | `BeginTransactionAsync`, pass to Execute, Commit/Rollback |
