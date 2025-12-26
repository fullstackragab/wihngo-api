# BirdsController Dapper Migration - Required Fixes

## Issue
The BirdsController is still using AppDbContext stub methods (`.Add()`, `.Remove()`, `SaveChangesAsync()`) which throw `NotSupportedException`. These need to be migrated to use Dapper with raw SQL.

## Methods That Need Fixing

### 1. ? **POST /api/birds** - FIXED
Already migrated to Dapper in previous fix.

### 2. **PUT /api/birds/{id}** - Line 354-396
Update bird information
- Uses: `FindAsync()`, `SaveChangesAsync()`
- **Action:** Convert to Dapper UPDATE

### 3. **DELETE /api/birds/{id}** - Line 398-426
Delete bird
- Uses: `FindAsync()`, `Remove()`, `SaveChangesAsync()`
- **Action:** Convert to Dapper DELETE

### 4. **POST /api/birds/{id}/love** - Line 428-498
Love a bird
- Uses: `FindAsync()` (3x), `Add()`, `ExecuteSqlInterpolatedAsync()`, `SaveChangesAsync()`, `AsNoTracking().FirstOrDefaultAsync()`
- **Action:** Convert to Dapper INSERT + UPDATE

### 5. **POST /api/birds/{id}/unlove** - Line 500-515
Unlove a bird
- Uses: `FindAsync()`, `ExecuteSqlInterpolatedAsync()`, `Remove()`, `SaveChangesAsync()`
- **Action:** Convert to Dapper DELETE + UPDATE

### 6. **DELETE /api/birds/{id}/love** - Line 517-536
Unlove a bird (alternate endpoint)
- Uses: `FindAsync()` (2x), `ExecuteSqlInterpolatedAsync()`, `Remove()`, `SaveChangesAsync()`
- **Action:** Convert to Dapper DELETE + UPDATE

### 7. **POST /api/birds/{id}/premium/subscribe** - Line 599-643
Subscribe to premium
- Uses: `FindAsync()`, `Add()`, `SaveChangesAsync()`
- **Action:** Convert to Dapper INSERT + UPDATE

### 8. **POST /api/birds/{id}/premium/subscribe/lifetime** - Line 645-689
Purchase lifetime subscription
- Uses: `FindAsync()`, `Add()`, `SaveChangesAsync()`
- **Action:** Convert to Dapper INSERT + UPDATE

### 9. **PATCH /api/birds/{id}/premium/style** - Line 691-712
Update premium style
- Uses: `FindAsync()`, `SaveChangesAsync()`
- **Action:** Convert to Dapper UPDATE

### 10. **PATCH /api/birds/{id}/premium/qr** - Line 714-729
Update QR code
- Uses: `FindAsync()`, `SaveChangesAsync()`
- **Action:** Convert to Dapper UPDATE

### 11. **POST /api/birds/{id}/donate** - Line 731-821
Donate to bird
- Uses: `FindAsync()` (2x), `Add()`, `SaveChangesAsync()`
- **Action:** Convert to Dapper INSERT + UPDATE

## Migration Pattern

### From AppDbContext:
```csharp
var bird = await _db.Birds.FindAsync(id);
bird.Name = "New Name";
await _db.SaveChangesAsync();
```

### To Dapper:
```csharp
using var connection = await _dbFactory.CreateOpenConnectionAsync();
await connection.ExecuteAsync(@"
    UPDATE birds 
    SET name = @Name 
    WHERE bird_id = @BirdId",
    new { Name = "New Name", BirdId = id });
```

## Priority
**HIGH** - These methods are throwing `NotSupportedException` and breaking the application at runtime.

## Next Steps
1. Fix all methods in order
2. Test each endpoint after migration
3. Remove AppDbContext dependency once all migrations complete
