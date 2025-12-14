# Project Status: Raw SQL + Dapper Migration

## ?? Current State

### ? What's Working
- **Build Status**: Compiles with 32 LINQ-related errors (expected, requires migration)
- **Core Infrastructure**: Complete and ready
  - `IDbConnectionFactory` - Database connection management
  - `SqlQueryHelper` - Utility methods for common patterns
  - `DapperQueryExtensions` - Minimal compatibility stubs
  - `AppDbContext` - Compilation stub only (no functionality)

- **Reference Implementations**: Complete
  - **StoriesController** - Get(), GetUserStories(), GetMyStories(), Get(id)
    - Demonstrates complex multi-table joins
    - Shows Dapper multi-mapping
    - Includes pagination patterns
    - Full DTO mapping with S3 URL generation

### ?? What Needs Migration (32 Errors)
All remaining errors are LINQ operations that need conversion to raw SQL.
See `MIGRATION_BLUEPRINT.md` for exact replacements.

| File | Errors | Priority | Estimated Time |
|------|--------|----------|----------------|
| BirdsController.cs | 8 | High | 30 min |
| NotificationService.cs | 11 | High | 45 min |
| StoriesController.cs | 3 | Medium | 15 min |
| NotificationCleanupJob.cs | 2 | Low | 10 min |
| DailyDigestJob.cs | 1 | Low | 5 min |
| ReconciliationJob.cs | 2 | Low | 20 min |
| PaymentMonitorJob.cs | 1 | Low | 5 min |
| PremiumExpiryNotificationJob.cs | 1 | Low | 10 min |
| PushNotificationService.cs | 1 | Medium | 5 min |
| EmailNotificationService.cs | 1 | Medium | 5 min |
| NotificationsController.cs | 1 | Medium | 5 min |
| DevController.cs | 1 | Low | 10 min |

**Total Estimated Time**: ~2.5 hours

---

## ?? Architecture

### Database Access Pattern
```
Controller/Service
    ?
IDbConnectionFactory.CreateOpenConnectionAsync()
    ?
Raw SQL Query (string)
    ?
Dapper Query/Execute Methods
    ?
POCO Mapping (automatic via Dapper)
    ?
Results returned
```

### No More:
- ? Entity Framework Core
- ? DbContext SaveChanges()
- ? LINQ to SQL
- ? Include()/ThenInclude()
- ? .Where().Select().ToListAsync()
- ? Change tracking
- ? Lazy loading
- ? Navigation property automatic loading

### Yes To:
- ? Raw SQL strings
- ? Explicit queries
- ? Dapper mapping only
- ? Manual JOINs
- ? @Parameterized queries
- ? Connection disposal
- ? Explicit transactions

---

## ?? Documentation

### For Developers
1. **`MIGRATION_GUIDE.md`** - Conceptual guide, explains the why and how
2. **`SQL_EXAMPLES.md`** - Complete working examples with explanations
3. **`MIGRATION_BLUEPRINT.md`** - Exact code replacements for all 32 errors
4. **`IMPLEMENTATION_STATUS.md`** - Task tracking and priorities

### For Reference
5. **`Helpers/SqlQueryHelper.cs`** - Reusable query helpers
6. **`Controllers/StoriesController.cs`** - Working examples of complex queries
7. **`GlobalUsings.cs`** - Project-wide imports

---

## ?? Quick Start for New Code

### 1. Select Query
```csharp
public async Task<List<Story>> GetStories(Guid authorId)
{
    var sql = @"
        SELECT * FROM stories 
        WHERE author_id = @AuthorId 
        ORDER BY created_at DESC";
    
    return await _dbFactory.QueryListAsync<Story>(sql, new { AuthorId = authorId });
}
```

### 2. Insert Query
```csharp
public async Task<Guid> CreateStory(Story story)
{
    var sql = @"
        INSERT INTO stories (story_id, author_id, content, created_at)
        VALUES (@StoryId, @AuthorId, @Content, @CreatedAt)";
    
    await _dbFactory.ExecuteAsync(sql, new
    {
        StoryId = story.StoryId,
        AuthorId = story.AuthorId,
        Content = story.Content,
        CreatedAt = story.CreatedAt
    });
    
    return story.StoryId;
}
```

### 3. Query with JOIN
```csharp
public async Task<Story?> GetStoryWithAuthor(Guid storyId)
{
    var sql = @"
        SELECT 
            s.*,
            u.user_id, u.name, u.email
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
            splitOn: "user_id");

        return result.FirstOrDefault();
    }
    finally
    {
        await connection.DisposeAsync();
    }
}
```

### 4. Check Exists
```csharp
public async Task<bool> StoryExists(Guid storyId)
{
    var sql = "SELECT EXISTS(SELECT 1 FROM stories WHERE story_id = @StoryId)";
    return await _dbFactory.ExecuteScalarAsync<bool>(sql, new { StoryId = storyId });
}
```

### 5. Count with Filter
```csharp
public async Task<int> CountStoriesByAuthor(Guid authorId)
{
    return await _dbFactory.QueryCountAsync(
        "stories", 
        "author_id = @AuthorId", 
        new { AuthorId = authorId });
}
```

---

## ?? Helper Methods Available

From `Helpers/SqlQueryHelper.cs`:

```csharp
// Query methods
await _dbFactory.QueryListAsync<T>(sql, parameters)
await _dbFactory.QuerySingleOrDefaultAsync<T>(sql, parameters)
await _dbFactory.QueryCountAsync(tableName, whereClause, parameters)
await _dbFactory.QueryExistsAsync(tableName, whereClause, parameters)

// Execute methods
await _dbFactory.ExecuteAsync(sql, parameters)
await _dbFactory.ExecuteScalarAsync<T>(sql, parameters)

// SQL building
SqlQueryHelper.BuildInsertSql(tableName, columns)
SqlQueryHelper.BuildUpdateSql(tableName, keyColumn, columns)
SqlQueryHelper.BuildDeleteSql(tableName, keyColumn)
SqlQueryHelper.ToSnakeCase(propertyName)
```

---

## ?? Learning Path

### Day 1: Understanding
1. Read `MIGRATION_GUIDE.md`
2. Review `SQL_EXAMPLES.md`
3. Study `Controllers/StoriesController.cs` Get methods

### Day 2: Practice
1. Pick 1-2 files from `MIGRATION_BLUEPRINT.md`
2. Implement the replacements
3. Test the endpoints
4. Mark complete in `IMPLEMENTATION_STATUS.md`

### Day 3: Advanced
1. Implement complex queries with multiple JOINs
2. Handle transactions
3. Optimize with indexes
4. Add error handling

---

## ?? Debugging Tips

### Query Not Returning Data?
1. Test SQL directly in `psql` or pgAdmin
2. Check column name mapping (snake_case ? PascalCase)
3. Verify parameters are passed correctly
4. Check for NULL handling

### Performance Issues?
1. Add indexes on WHERE clause columns
2. Use EXPLAIN ANALYZE to check query plan
3. Avoid N+1 queries - use JOINs
4. Consider pagination for large datasets

### Mapping Issues?
1. Ensure column names match property names (or use aliases)
2. Check for nullable types
3. Handle enum conversions explicitly
4. Use `splitOn` correctly in multi-mapping

---

## ?? Support

### When You're Stuck
1. Check the relevant .md file for your scenario
2. Look at `StoriesController.cs` for working examples
3. Test your SQL query directly in the database
4. Use Dapper's `QueryAsync<dynamic>` to see what's actually returned
5. Check the Dapper documentation: https://github.com/DapperLib/Dapper

### Common Questions
**Q: Can I still use LINQ?**
A: No. All LINQ operations on database queries must be converted to SQL.

**Q: What about complex filters?**
A: Build them in SQL with WHERE clauses and parameters.

**Q: How do I handle optional filters?**
A: Build SQL dynamically or use CASE statements.

**Q: What about transactions?**
A: Use `connection.BeginTransactionAsync()` and pass to Dapper methods.

**Q: Can I use stored procedures?**
A: Yes! Just execute them with Dapper like any SQL.

---

## ? Definition of Done

A file/feature is "migrated" when:
- [ ] No LINQ query operations remain
- [ ] All database access uses `IDbConnectionFactory`
- [ ] All queries are raw SQL strings
- [ ] All queries use parameterized @Parameters
- [ ] Code compiles without errors
- [ ] Endpoints/methods return correct data
- [ ] Tests pass (if applicable)
- [ ] Marked complete in `IMPLEMENTATION_STATUS.md`

---

## ?? Next Steps

1. **Review the documentation** - Understand the patterns
2. **Start with BirdsController** - High priority, clear examples
3. **Use MIGRATION_BLUEPRINT.md** - Copy-paste solutions
4. **Test as you go** - Verify each method works
5. **Mark progress** - Update IMPLEMENTATION_STATUS.md

---

## ?? Progress Tracking

Current: **32 errors remaining** (all LINQ-related, expected)

Goal: **0 errors** - Pure raw SQL + Dapper implementation

ETA: **~2.5 hours** of focused development time

---

*Last Updated: [Current Date]*
*Status: Ready for development*
*Next Action: Begin BirdsController migration*
