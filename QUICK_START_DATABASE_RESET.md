# ?? Quick Start: Reset Your Database

I've created a comprehensive SQL script to reset your database and add realistic seed data with proper relationships.

## ? Recommended Method: Use PowerShell Script

### Step 1: Open PowerShell
```powershell
cd C:\.net\Wihngo
```

### Step 2: Run the Script
```powershell
.\reset-database.ps1
```

### Step 3: Confirm
Type `YES` when prompted

### Step 4: Done! ??

---

## ?? What You'll Get

### Users (5)
| Name | Email | Role |
|------|-------|------|
| Alice Johnson | alice@example.com | Bird lover, active |
| Bob Martinez | bob@example.com | Photographer |
| Carol Davis | carol@example.com | New user |
| David Chen | david@example.com | Rescue volunteer |
| Emma Wilson | emma@example.com | Teacher |

**Password for all:** `Password123!`

### Birds (10)
- **Ruby** - Anna's Hummingbird (Alice, premium)
- **Jasper** - Black-chinned Hummingbird (Alice)
- **Sunshine** - American Goldfinch (Bob, lifetime premium)
- **Bella** - House Finch (Bob)
- **Chirpy** - House Sparrow (Carol)
- **Phoenix** - Red-tailed Hawk (David, rescue, premium)
- **Hope** - American Robin (David, released)
- **Professor Hoot** - Barred Owl (Emma, lifetime premium)
- **Flutter** - Mourning Dove (Emma)
- **Angel** - Blue Jay (David, **memorial** ???)

### Content
- **20 Stories** - Various moods and content
- **8 Comments** - Including nested replies
- **14 Loves** - User-bird relationships
- **11 Donations** - Support transactions ($3,500 total)
- **5 Premium Subscriptions** - Mix of monthly and lifetime
- **4 Memorial Messages** - For Angel

---

## ?? Alternative Methods

### Method 2: Manual psql Command
```powershell
# Find your psql.exe location (common paths):
# C:\Program Files\PostgreSQL\16\bin\psql.exe
# C:\Program Files\PostgreSQL\15\bin\psql.exe

# Run this (replace with your actual psql path):
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5432 -U postgres -d wihngo_db -f database-reset-seed.sql
```

### Method 3: pgAdmin (GUI)
1. Open pgAdmin 4
2. Connect to your server
3. Right-click database ? Query Tool
4. Open `database-reset-seed.sql`
5. Click Execute (??)

### Method 4: Copy Script to psql
```powershell
# Connect to database
psql -U postgres -d wihngo_db

# Run the script
\i 'C:/.net/Wihngo/database-reset-seed.sql'
```

---

## ? Test It Works

### 1. Login via API
```http
POST http://localhost:5162/api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "Password123!"
}
```

### 2. Get All Birds
```http
GET http://localhost:5162/api/birds
```

### 3. Get Ruby's Stories
```http
GET http://localhost:5162/api/stories
```

### 4. Get Angel's Memorial
```http
GET http://localhost:5162/api/birds/dddddddd-0003-0003-0003-000000000003/memorial
```

---

## ?? Verify Data in Database

```sql
-- Quick summary
SELECT 'Users' as table_name, COUNT(*) FROM users
UNION ALL SELECT 'Birds', COUNT(*) FROM birds
UNION ALL SELECT 'Stories', COUNT(*) FROM stories
UNION ALL SELECT 'Comments', COUNT(*) FROM comments
UNION ALL SELECT 'Loves', COUNT(*) FROM loves;
```

Expected output:
```
table_name | count
-----------+-------
Users      |     5
Birds      |    10
Stories    |    20
Comments   |     8
Loves      |    14
```

---

## ?? Key Features of Seed Data

### Realistic Relationships
- ? Users own multiple birds
- ? Birds have multiple stories
- ? Stories have comments and likes
- ? Users love birds they don't own
- ? Premium subscriptions (monthly & lifetime)
- ? Memorial bird with messages

### Diverse Content
- Different bird species (hummingbirds, owls, hawks, songbirds)
- Various story moods (happy, excited, calm, sad, playful)
- Recent stories (last 2 days) for testing
- Donations and financial transactions
- Educational and rescue scenarios

### Test Scenarios
- New user (Carol) with minimal data
- Power user (Bob) with premium features
- Rescue volunteer (David) with memorial case
- Educator (Emma) with classroom bird

---

## ?? Need to Reset Again?

Just run the script again! It's safe to run multiple times.

The script will:
1. ? Clean all existing data (respecting foreign keys)
2. ? Reset sequences
3. ? Insert fresh data

---

## ?? Full Documentation

See **`DATABASE_RESET_INSTRUCTIONS.md`** for:
- Detailed step-by-step instructions
- Troubleshooting guide
- Verification queries
- API testing examples

---

## ?? Pro Tips

1. **Start fresh daily** - Run this script each morning for clean testing
2. **Use Alice's account** - She has the most diverse data
3. **Test memorial features** - Angel is a complete memorial bird
4. **Check relationships** - Users love birds they don't own
5. **Test premium features** - Several birds have premium subscriptions

---

## ?? Need Help?

### Error: "psql not found"
? Use pgAdmin (Method 3) or specify full psql path (Method 2)

### Error: "authentication failed"
? Check password in `appsettings.json`

### Error: "database does not exist"
? Run migrations first: `dotnet ef database update`

### Script hangs
? Check PostgreSQL is running: `pg_ctl status`

---

**Ready to start? Run:**
```powershell
.\reset-database.ps1
```

**Happy Testing! ??**
