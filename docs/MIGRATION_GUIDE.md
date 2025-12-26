# EF Core to Raw SQL + Dapper Migration Guide

## Overview

This project is migrating from Entity Framework Core to **raw SQL with Dapper** for all database operations. This provides:
- ? Full control over SQL queries
- ? Better performance
- ? No hidden query generation
- ? Support for any complex query scenario
- ? Explicit and maintainable code

## Quick Start

### 1. Basic Query (SELECT)

```csharp
// OLD: EF Core
var stories = await _db.Stories
    .Where(s => s.AuthorId == userId)
    .OrderByDescending(s => s.CreatedAt)
    .ToListAsync();

// NEW: Raw SQL + Dapper
var connection = await _dbFactory.CreateOpenConnectionAsync();
try
{
    var sql = @"
        SELECT * FROM stories 
        WHERE author_id = @UserId 
        ORDER BY created_at DESC";
    
    var stories = await connection.QueryAsync<Story>(sql, new { UserId = userId });
    return stories.ToList();
}
finally
{
    await connection.DisposeAsync();
}
```

### 2. Query with Joins (Include/ThenInclude)

```csharp
// OLD: EF Core
var story = await _db.Stories
    .Include(s => s.StoryBirds)
        .ThenInclude(sb => sb.Bird)
    .Include(s => s.Author)
    .FirstOrDefaultAsync(s => s.StoryId == id);

// NEW: Raw SQL + Dapper Multi-Mapping
var connection = await _dbFactory.CreateOpenConnectionAsync();
try
{
    var sql = @"
        SELECT s.*, 
               u.user_id as Author_UserId, u.name as Author_Name,
               sb.story_bird_id, sb.bird_id,
               b.bird_id as Bird_BirdId, b.name as Bird_Name, b.species as Bird_Species
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
                story.StoryBirds.Add(storyBird);
            }

            return story;
        },
        new { StoryId = id },
        splitOn: "Author_UserId,story_bird_id,Bird_BirdId");

    return story;
}
finally
{
    await connection.DisposeAsync();
}
```

### 3. Insert

```csharp
// OLD: EF Core
_db.Stories.Add(story);
await _db.SaveChangesAsync();

// NEW: Raw SQL + Dapper
var connection = await _dbFactory.CreateOpenConnectionAsync();
try
{
    var sql = @"
        INSERT INTO stories (story_id, author_id, content, mode, image_url, video_url, created_at)
        VALUES (@StoryId, @AuthorId, @Content, @Mode, @ImageUrl, @VideoUrl, @CreatedAt)";
    
    await connection.ExecuteAsync(sql, new
    {
        StoryId = story.StoryId,
        AuthorId = story.AuthorId,
        Content = story.Content,
        Mode = story.Mode?.ToString(), // Enum to string
        ImageUrl = story.ImageUrl,
        VideoUrl = story.VideoUrl,
        CreatedAt = story.CreatedAt
    });
}
finally
{
    await connection.DisposeAsync();
}
```

### 4. Update

```csharp
// OLD: EF Core
story.Content = "Updated content";
await _db.SaveChangesAsync();

// NEW: Raw SQL + Dapper
var connection = await _dbFactory.CreateOpenConnectionAsync();
try
{
    var sql = @"
        UPDATE stories 
        SET content = @Content, mode = @Mode, image_url = @ImageUrl
        WHERE story_id = @StoryId";
    
    var rowsAffected = await connection.ExecuteAsync(sql, new
    {
        Content = story.Content,
        Mode = story.Mode?.ToString(),
        ImageUrl = story.ImageUrl,
        StoryId = story.StoryId
    });
    
    if (rowsAffected == 0)
    {
        // Story not found
    }
}
finally
{
    await connection.DisposeAsync();
}
```

### 5. Delete

```csharp
// OLD: EF Core
_db.Stories.Remove(story);
await _db.SaveChangesAsync();

// NEW: Raw SQL + Dapper
var connection = await _dbFactory.CreateOpenConnectionAsync();
try
{
    var sql = "DELETE FROM stories WHERE story_id = @StoryId";
    await connection.ExecuteAsync(sql, new { StoryId = story.StoryId });
}
finally
{
    await connection.DisposeAsync();
}
```

### 6. Count

```csharp
// OLD: EF Core
var count = await _db.Stories.CountAsync();

// NEW: Raw SQL + Dapper
var connection = await _dbFactory.CreateOpenConnectionAsync();
try
{
    var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM stories");
}
finally
{
    await connection.DisposeAsync();
}
```

### 7. Exists Check

```csharp
// OLD: EF Core
var exists = await _db.Stories.AnyAsync(s => s.StoryId == id);

// NEW: Raw SQL + Dapper
var connection = await _dbFactory.CreateOpenConnectionAsync();
try
{
    var count = await connection.ExecuteScalarAsync<int>(
        "SELECT COUNT(*) FROM stories WHERE story_id = @Id LIMIT 1",
        new { Id = id });
    var exists = count > 0;
}
finally
{
    await connection.DisposeAsync();
}
```

## Column Name Mapping

Dapper is configured to automatically map `snake_case` database columns to `PascalCase` C# properties:

- Database: `created_at` ? C# Property: `CreatedAt`
- Database: `author_id` ? C# Property: `AuthorId`
- Database: `story_id` ? C# Property: `StoryId`

This is configured in `DapperQueryExtensions.cs`:
```csharp
DefaultTypeMap.MatchNamesWithUnderscores = true;
```

## Multi-Mapping (Joins)

When you have relationships, use Dapper's multi-mapping:

```csharp
// For one-to-one or many-to-one:
await connection.QueryAsync<Story, User, Story>(
    sql,
    (story, author) =>
    {
        story.Author = author;
        return story;
    },
    splitOn: "user_id" // Column that starts the User entity
);

// For one-to-many (with deduplication):
var storyDict = new Dictionary<Guid, Story>();

await connection.QueryAsync<Story, Bird, Story>(
    sql,
    (story, bird) =>
    {
        if (!storyDict.TryGetValue(story.StoryId, out var storyEntry))
        {
            storyEntry = story;
            storyEntry.Birds = new List<Bird>();
            storyDict.Add(story.StoryId, storyEntry);
        }
        
        if (bird != null)
        {
            storyEntry.Birds.Add(bird);
        }
        
        return storyEntry;
    },
    splitOn: "bird_id"
);

var stories = storyDict.Values.ToList();
```

## Best Practices

1. **Always use parameterized queries** - Never string interpolation for user input
2. **Dispose connections** - Use `try/finally` with `DisposeAsync()`
3. **Use transactions for multiple operations**:
   ```csharp
   using var connection = await _dbFactory.CreateOpenConnectionAsync();
   using var transaction = await connection.BeginTransactionAsync();
   try
   {
       await connection.ExecuteAsync(sql1, param1, transaction);
       await connection.ExecuteAsync(sql2, param2, transaction);
       await transaction.CommitAsync();
   }
   catch
   {
       await transaction.RollbackAsync();
       throw;
   }
   ```

4. **Log your SQL queries** for debugging
5. **Test queries directly in psql** before putting them in code

## Enum Handling

Enums should be converted to/from strings:

```csharp
// To database (Insert/Update):
Mode = story.Mode?.ToString()

// From database (requires explicit cast in some cases):
var modeStr = reader["mode"] as string;
story.Mode = !string.IsNullOrEmpty(modeStr) ? Enum.Parse<StoryMode>(modeStr) : null;
```

## Complex Queries

For complex queries with CTEs, window functions, or aggregations, write the full SQL:

```csharp
var sql = @"
    WITH bird_stats AS (
        SELECT 
            bird_id,
            COUNT(*) as story_count,
            MAX(created_at) as last_story_at
        FROM stories s
        JOIN story_birds sb ON s.story_id = sb.story_id
        GROUP BY bird_id
    )
    SELECT b.*, bs.story_count, bs.last_story_at
    FROM birds b
    LEFT JOIN bird_stats bs ON b.bird_id = bs.bird_id
    WHERE b.owner_id = @OwnerId
    ORDER BY bs.story_count DESC NULLS LAST";

var results = await connection.QueryAsync<BirdWithStats>(sql, new { OwnerId = userId });
```

## Compatibility Stubs

The `AppDbContext` and extension methods are kept as **compilation stubs only**. They:
- Allow old code to compile
- Return no-op or throw `NotImplementedException`
- Should be replaced with raw SQL as you migrate each controller/service

## Example: Complete Controller Method

See `Controllers/StoriesController.cs` methods `Get()` and `GetUserStories()` for complete examples of:
- Multi-table joins
- Pagination
- DTO mapping
- Error handling

## Resources

- [Dapper Documentation](https://github.com/DapperLib/Dapper)
- [Dapper Multi-Mapping](https://github.com/DapperLib/Dapper#multi-mapping)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/current/)
