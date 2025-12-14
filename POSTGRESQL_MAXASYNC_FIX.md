# PostgreSQL MaxAsync Error Fix

## ?? Issue

**Error:** 
```
Npgsql.PostgresException: 42809: WITHIN GROUP is required for ordered-set aggregate mode
```

**Location:** `StoriesController.cs` - `ToggleHighlight` method, line ~987

## ?? Root Cause

PostgreSQL was throwing an error when using `MaxAsync()` directly on a nullable integer property in Entity Framework:

```csharp
// ? This caused the error
int? birdMax = await _db.Stories
    .Where(...)
    .MaxAsync(s => (int?)s.HighlightOrder);
```

The issue occurs because PostgreSQL's aggregate functions have different behavior with nullable types compared to SQL Server.

## ? Solution

Changed to use `Select().ToListAsync()` pattern then apply `Max()` in memory:

```csharp
// ? Fixed version
var highlightOrders = await _db.Stories
    .Where(s => s.StoryBirds.Any(sb => sb.BirdId == birdId) && 
                s.IsHighlighted && 
                s.HighlightOrder != null)
    .Select(s => s.HighlightOrder!.Value)  // Select non-nullable values
    .ToListAsync();
    
if (highlightOrders.Any())
{
    var birdMax = highlightOrders.Max();  // Max() in memory
    if (birdMax > maxOrder)
    {
        maxOrder = birdMax;
    }
}
```

## ?? Changes Made

**File:** `Controllers\StoriesController.cs`

**Method:** `ToggleHighlight` (around line 987)

**Change:**
- Replaced `MaxAsync(s => (int?)s.HighlightOrder)` 
- With `Select(s => s.HighlightOrder!.Value).ToListAsync()` followed by in-memory `Max()`
- Added null check with `highlightOrders.Any()`

## ?? Why This Works

1. **Select non-nullable values:** `s.HighlightOrder!.Value` extracts the int from the nullable int
2. **ToListAsync():** Brings the data into memory
3. **Max() in memory:** LINQ to Objects handles Max() correctly
4. **Null safety:** Check `Any()` before calling `Max()` to avoid empty sequence error

## ?? Testing

The fix should allow the `ToggleHighlight` endpoint to work correctly when:
- Setting story highlight order
- Finding the maximum highlight order for a bird
- Handling cases where no highlighted stories exist

## ?? Performance Note

This fix brings the data into memory before finding the max. Since we're only dealing with a small number of highlighted stories (max 3 per bird), the performance impact is negligible.

---

**Status:** ? Fixed  
**Build Status:** ? Compiles successfully  
**Testing Required:** Test highlight toggle functionality
