# Implementation Status & Migration Tasks

## ? Completed
- AppDbContext stub structure (compilation only, no real functionality)
- Raw SQL + Dapper helper utilities in `Helpers/SqlQueryHelper.cs`
- StoriesController Get/GetUserStories/GetMyStories/Get(id) methods using raw SQL
- Comprehensive SQL examples documentation in `SQL_EXAMPLES.md`
- Migration guide in `MIGRATION_GUIDE.md`

## ?? Remaining Work - Files Using LINQ (Need Migration to Raw SQL)

### High Priority Controllers (User-Facing)
1. **Controllers/BirdsController.cs**
   - Lines 70, 89, 532-533, 723: Uses LINQ Select/ToListAsync
   - Line 562: Uses _db.Add() 
   - **Action**: Convert all queries to raw SQL with Dapper

2. **Controllers/StoriesController.cs**
   - Lines 550, 1021: Uses LINQ Select.ToListAsync
   - Line 974: Uses LINQ AnyAsync with predicate
   - **Action**: Replace with raw SQL EXISTS or COUNT queries

3. **Controllers/NotificationsController.cs**
   - Line 265: Uses LINQ Select.ToListAsync
   - **Action**: Use raw SQL SELECT with Dapper

4. **Controllers/PaymentsController.cs**
   - Already migrated or uses FindAsync which works

5. **Controllers/DevController.cs**
   - Line 221: Uses ThenByDescending
   - **Action**: Use raw SQL ORDER BY with multiple columns

### Services
6. **Services/NotificationService.cs**
   - Lines 73, 130, 152, 192, 213, 257, 407, 419, 476, 526, 535: Multiple LINQ operations
   - **Action**: Rewrite all methods using raw SQL

7. **Services/PushNotificationService.cs**
   - Line 47: LINQ ToListAsync
   - **Action**: Raw SQL SELECT

8. **Services/EmailNotificationService.cs**
   - Line 88: LINQ ToListAsync
   - **Action**: Raw SQL SELECT

### Background Jobs
9. **BackgroundJobs/NotificationCleanupJob.cs**
   - Lines 33, 60: LINQ ToListAsync
   - **Action**: Raw SQL SELECT with WHERE

10. **BackgroundJobs/DailyDigestJob.cs**
    - Line 40: LINQ Select.ToListAsync
    - **Action**: Raw SQL SELECT

11. **BackgroundJobs/ReconciliationJob.cs**
    - Line 73: LINQ GroupBy.ToListAsync
    - Line 147: LINQ SumAsync
    - **Action**: Use raw SQL with GROUP BY and SUM()

12. **BackgroundJobs/PaymentMonitorJob.cs**
    - Line 465: ExecuteUpdateAsync (EF Core bulk update)
    - **Action**: Raw SQL UPDATE statement

13. **BackgroundJobs/PremiumExpiryNotificationJob.cs**
    - Line 45: Include() on IQueryable
    - **Action**: Raw SQL with JOIN

## ?? Quick Fix Template

For any file with LINQ errors, use this pattern:

### Before (LINQ):
```csharp
var items = await _db.Items
    .Where(x => x.UserId == userId)
    .Select(x => new ItemDto { Id = x.Id, Name = x.Name })
    .ToListAsync();
```

### After (Raw SQL):
```csharp
var sql = @"
    SELECT 
        item_id as Id,
        name as Name
    FROM items
    WHERE user_id = @UserId";

var items = await _dbFactory.QueryListAsync<ItemDto>(sql, new { UserId = userId });
```

## ?? Migration Checklist Per File

For each file above:
1. ? Identify all LINQ operations (Where, Select, Include, OrderBy, etc.)
2. ? Replace with raw SQL query
3. ? Use `_dbFactory.QueryListAsync<T>()` or `QuerySingleOrDefaultAsync<T>()`
4. ? Add proper parameters for WHERE clauses
5. ? Test the endpoint/method
6. ? Remove any `using` statements for EF Core

## ?? Priority Order

1. **BirdsController** - Most used endpoint
2. **NotificationService** - Core functionality
3. **StoriesController** remaining methods
4. **Background Jobs** - Can run with errors temporarily
5. **Dev/Admin Controllers** - Lowest priority

## ?? Key Patterns to Remember

- **No LINQ queries** - Everything is raw SQL
- **Use IDbConnectionFactory** - Not AppDbContext
- **Dapper for mapping** - Query results to POCOs
- **Snake_case** - Database columns
- **PascalCase** - C# properties
- **Parameterized queries** - Always use @Parameters

## ?? Need Help?

See `SQL_EXAMPLES.md` for complete working examples of:
- SELECT with WHERE
- JOINs (1-to-many, many-to-many)
- Pagination
- Transactions
- GROUP BY, aggregations
- CTEs, window functions
