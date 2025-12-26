# Quick Migration Guide - Next Steps

## Current Status ?

**Phase 1 Complete:**
- ? Core infrastructure (IDbConnectionFactory, NpgsqlConnectionFactory)
- ? AuthController fully migrated (9 endpoints)
- ? UsersController fully migrated (7 endpoints)
- ? Project compiles successfully
- ? Stub AppDbContext for backward compatibility

**What Works Now:**
- All authentication features (register, login, password reset, etc.)
- All user management features (profile, CRUD, push tokens, etc.)

## Migration Strategy - Choose Your Approach

### Option A: Incremental Migration (Recommended)
Migrate controllers one at a time as you need them. The stub AppDbContext will throw clear errors when unmigrated features are accessed.

**Priority Order:**
1. BirdsController (core feature)
2. StoriesController (core feature)  
3. NotificationsController (user experience)
4. SupportTransactionsController (revenue)
5. Others as needed

### Option B: Feature-by-Feature
Migrate by feature area rather than by controller:
1. Bird management (BirdsController + related services)
2. Story management (StoriesController + related services)
3. Payments (all payment-related controllers + services)
4. Notifications (NotificationsController + NotificationService + jobs)

### Option C: Wait Until Needed
Keep the current setup. The application works fine for Auth and Users. Other features will fail gracefully with NotImplementedException until you're ready to migrate them.

## Step-by-Step Migration Example

### Example: Migrating a Simple Controller Method

**Before (Entity Framework):**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<BirdDto>> Get(Guid id)
{
    var bird = await _db.Birds
        .Include(b => b.Owner)
        .FirstOrDefaultAsync(b => b.BirdId == id);
    
    if (bird == null) return NotFound();
    
    return Ok(_mapper.Map<BirdDto>(bird));
}
```

**After (Raw SQL):**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<BirdDto>> Get(Guid id)
{
    using var connection = await _dbFactory.CreateOpenConnectionAsync();
    
    // Get bird
    using var birdCmd = connection.CreateCommand();
    birdCmd.CommandText = @"
        SELECT b.bird_id, b.owner_id, b.name, b.species, b.created_at,
               u.user_id as owner_user_id, u.name as owner_name, u.email as owner_email
        FROM birds b
        LEFT JOIN users u ON b.owner_id = u.user_id
        WHERE b.bird_id = @id
    ";
    birdCmd.Parameters.AddWithValue("id", id);
    
    Bird? bird = null;
    using var reader = await birdCmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        bird = new Bird
        {
            BirdId = reader.GetGuid(0),
            OwnerId = reader.GetGuid(1),
            Name = reader.GetString(2),
            Species = reader.GetString(3),
            CreatedAt = reader.GetDateTime(4),
            Owner = new User
            {
                UserId = reader.GetGuid(5),
                Name = reader.GetString(6),
                Email = reader.GetString(7)
            }
        };
    }
    
    if (bird == null) return NotFound();
    
    return Ok(_mapper.Map<BirdDto>(bird));
}
```

## Common Patterns Reference

### Pattern 1: Simple SELECT
```csharp
using var connection = await _dbFactory.CreateOpenConnectionAsync();
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT id, name FROM table WHERE id = @id";
cmd.Parameters.AddWithValue("id", someId);

using var reader = await cmd.ExecuteReaderAsync();
if (await reader.ReadAsync())
{
    // Read data
    var id = reader.GetGuid(0);
    var name = reader.GetString(1);
}
```

### Pattern 2: INSERT
```csharp
using var connection = await _dbFactory.CreateOpenConnectionAsync();
using var cmd = connection.CreateCommand();
cmd.CommandText = @"
    INSERT INTO table (id, name, created_at)
    VALUES (@id, @name, @created_at)
";
cmd.Parameters.AddWithValue("id", Guid.NewGuid());
cmd.Parameters.AddWithValue("name", "Some Name");
cmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
await cmd.ExecuteNonQueryAsync();
```

### Pattern 3: UPDATE
```csharp
using var connection = await _dbFactory.CreateOpenConnectionAsync();
using var cmd = connection.CreateCommand();
cmd.CommandText = @"
    UPDATE table 
    SET name = @name, updated_at = @updated_at
    WHERE id = @id
";
cmd.Parameters.AddWithValue("name", "New Name");
cmd.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
cmd.Parameters.AddWithValue("id", existingId);
var rowsAffected = await cmd.ExecuteNonQueryAsync();
if (rowsAffected == 0) return NotFound();
```

### Pattern 4: DELETE
```csharp
using var connection = await _dbFactory.CreateOpenConnectionAsync();
using var cmd = connection.CreateCommand();
cmd.CommandText = "DELETE FROM table WHERE id = @id";
cmd.Parameters.AddWithValue("id", idToDelete);
var rowsAffected = await cmd.ExecuteNonQueryAsync();
if (rowsAffected == 0) return NotFound();
```

### Pattern 5: COUNT/Aggregate
```csharp
using var connection = await _dbFactory.CreateOpenConnectionAsync();
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT COUNT(*) FROM table WHERE status = @status";
cmd.Parameters.AddWithValue("status", "active");
var count = (long)await cmd.ExecuteScalarAsync();
```

### Pattern 6: Multiple Results (List)
```csharp
using var connection = await _dbFactory.CreateOpenConnectionAsync();
using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT id, name FROM table WHERE active = true";

var items = new List<Item>();
using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    items.Add(new Item
    {
        Id = reader.GetGuid(0),
        Name = reader.GetString(1)
    });
}
```

### Pattern 7: Handling Nulls
```csharp
// When reading nullable columns
var bio = reader.IsDBNull(3) ? null : reader.GetString(3);

// When inserting nullable values
cmd.Parameters.AddWithValue("bio", (object?)entity.Bio ?? DBNull.Value);
```

### Pattern 8: Transactions
```csharp
using var connection = await _dbFactory.CreateOpenConnectionAsync();
using var transaction = await connection.BeginTransactionAsync();

try
{
    // Multiple commands
    using var cmd1 = connection.CreateCommand();
    cmd1.Transaction = transaction;
    cmd1.CommandText = "INSERT INTO...";
    await cmd1.ExecuteNonQueryAsync();
    
    using var cmd2 = connection.CreateCommand();
    cmd2.Transaction = transaction;
    cmd2.CommandText = "UPDATE...";
    await cmd2.ExecuteNonQueryAsync();
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## EF to SQL Translation Cheat Sheet

| Entity Framework | Raw SQL |
|-----------------|---------|
| `.Where(x => x.Status == "active")` | `WHERE status = @status` |
| `.Include(x => x.Owner)` | `LEFT JOIN users ON...` |
| `.OrderBy(x => x.CreatedAt)` | `ORDER BY created_at ASC` |
| `.OrderByDescending(x => x.CreatedAt)` | `ORDER BY created_at DESC` |
| `.Take(10)` | `LIMIT 10` |
| `.Skip(20)` | `OFFSET 20` |
| `.FirstOrDefaultAsync()` | Read first row or return null |
| `.ToListAsync()` | Loop through all rows |
| `.AnyAsync()` | `SELECT EXISTS(...)` or `SELECT COUNT(*) > 0` |
| `.CountAsync()` | `SELECT COUNT(*)` |
| `.Add(entity)` | `INSERT INTO...` |
| `.Remove(entity)` | `DELETE FROM...` |
| `.SaveChangesAsync()` | `ExecuteNonQueryAsync()` |

## Quick Wins - Easy Controllers to Migrate

These controllers have simpler logic and would be good practice:

1. **DevController** - Development utilities, simple queries
2. **SupportTransactionsController** - CRUD operations
3. **MediaController** - If it exists, usually simple

## Tools & Tips

### VS Code Search & Replace
Remove all EF using statements:
1. Press `Ctrl+Shift+H` (Replace in Files)
2. Find: `using Microsoft.EntityFrameworkCore;`
3. Replace: *(empty)*
4. Replace All

### Testing After Migration
```bash
# Run build
dotnet build

# Run specific tests
dotnet test --filter FullyQualifiedName~BirdsController

# Run application
dotnet run
```

### Debugging SQL
Add logging to see executed SQL:
```csharp
_logger.LogDebug("Executing: {Sql}", cmd.CommandText);
```

## Common Gotchas

1. **Column Names**: PostgreSQL uses `snake_case` (e.g., `user_id`, not `UserId`)
2. **Parameter Names**: Use `@paramName` syntax
3. **NULL Handling**: Always check `IsDBNull()` before reading
4. **Dispose**: Always use `using` statements for connections, commands, readers
5. **Async**: Always use `ExecuteReaderAsync()`, not synchronous versions
6. **Transactions**: Don't forget to set `cmd.Transaction = transaction`

## Need Help?

- **Examples**: Look at `AuthController.cs` and `UsersController.cs`
- **Documentation**: `MIGRATION_SUMMARY.md`
- **Entity Models**: Check Models folder for property names
- **Database Schema**: Look at SQL migration files in Database folder

## Status Tracking

Create a checklist to track your progress:

### Controllers
- [x] AuthController
- [x] UsersController
- [ ] BirdsController
- [ ] StoriesController
- [ ] NotificationsController
- [ ] SupportTransactionsController
- [ ] DevController
- [ ] CryptoPaymentController
- [ ] PaymentsController
- [ ] WebhooksController
- [ ] InvoicesController
- [ ] CharityController
- [ ] PremiumSubscriptionController
- [ ] OnChainDepositController
- [ ] MediaController

### Services
- [ ] NotificationService
- [ ] CryptoPaymentService
- [ ] PaymentService
- [ ] PremiumSubscriptionService
- [ ] Others...

## Remember

? **The application works right now** for Auth and User features
? **You can migrate incrementally** - no rush to do everything at once
? **Clear error messages** will tell you what needs migration when you hit unmigrated code
? **The patterns are consistent** - once you've done a few, the rest are easy

Good luck with your migration! ??
