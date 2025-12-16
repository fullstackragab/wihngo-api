# Database Reset & Seed Instructions ??

## Quick Start (Recommended)

### Option 1: Using PowerShell Script (Easiest)

1. Open PowerShell in the project directory `C:\.net\Wihngo\`
2. Run the script:
   ```powershell
   .\reset-database.ps1
   ```
3. Type `YES` when prompted to confirm
4. Done! ?

---

## Option 2: Manual Execution (If psql is not in PATH)

### Step 1: Locate psql.exe
Find your PostgreSQL installation directory. Common locations:
- `C:\Program Files\PostgreSQL\16\bin\psql.exe`
- `C:\Program Files\PostgreSQL\15\bin\psql.exe`
- `C:\PostgreSQL\bin\psql.exe`

### Step 2: Get Database Credentials
From your `appsettings.json`, note your connection details:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=wihngo_db;Username=postgres;Password=your_password"
  }
}
```

### Step 3: Execute the SQL Script

Open PowerShell in `C:\.net\Wihngo\` and run:

```powershell
# Replace with your actual psql.exe path and credentials
& "C:\Program Files\PostgreSQL\16\bin\psql.exe" -h localhost -p 5432 -U postgres -d wihngo_db -f database-reset-seed.sql
```

When prompted, enter your database password.

---

## Option 3: Using pgAdmin (GUI)

1. Open **pgAdmin 4**
2. Connect to your local PostgreSQL server
3. Right-click on your database (`wihngo_db`) ? **Query Tool**
4. Click **Open File** (folder icon) and select `database-reset-seed.sql`
5. Click **Execute** (?? play button) or press `F5`
6. Check the **Messages** tab for success confirmation

---

## Option 4: Copy-Paste in psql Terminal

1. Open Command Prompt or PowerShell
2. Connect to PostgreSQL:
   ```cmd
   psql -U postgres -d wihngo_db
   ```
3. Enter your password
4. Run the script:
   ```sql
   \i C:/.net/Wihngo/database-reset-seed.sql
   ```
   Or use forward slashes:
   ```sql
   \i 'C:/.net/Wihngo/database-reset-seed.sql'
   ```

---

## Option 5: Using VS Code PostgreSQL Extension

If you have a PostgreSQL extension installed in VS Code:

1. Open `database-reset-seed.sql` in VS Code
2. Right-click in the editor ? **Run Query**
3. Select your database connection
4. Wait for execution to complete

---

## What Gets Seeded? ??

### Users (5)
- **Alice Johnson** - alice@example.com (bird lover, community member)
- **Bob Martinez** - bob@example.com (wildlife photographer)
- **Carol Davis** - carol@example.com (new user, learning)
- **David Chen** - david@example.com (wildlife rescue volunteer)
- **Emma Wilson** - emma@example.com (teacher, educator)

**All users password:** `Password123!`

### Birds (10)
1. **Ruby** (Alice) - Anna's Hummingbird, territorial
2. **Jasper** (Alice) - Black-chinned Hummingbird
3. **Sunshine** (Bob) - American Goldfinch, premium
4. **Bella** (Bob) - House Finch
5. **Chirpy** (Carol) - House Sparrow, new
6. **Phoenix** (David) - Red-tailed Hawk, rescue
7. **Hope** (David) - American Robin, rescued
8. **Professor Hoot** (Emma) - Barred Owl, educational
9. **Flutter** (Emma) - Mourning Dove
10. **Angel** (David) - Blue Jay, **memorial bird** ???

### Stories (20)
- Various stories for each bird
- Different moods: Happy, Excited, Calm, Sad, Playful
- Some with images
- Recent stories (last 2 days) for testing

### Comments (8)
- Comments on stories
- Includes nested comment (reply)

### Relationships
- **14 Loves** - Users loving birds
- **11 Donations** - Support transactions
- **5 Premium Subscriptions** - Including 2 lifetime
- **4 Memorial Messages** - For Angel
- **9 Story Likes** - Engagement on stories
- **4 Comment Likes** - Engagement on comments

### Financial Data
- **Payout Balances** - For users with earnings
- **Supported Tokens** - USDC/EURC on Solana and Base

---

## Verification Queries

After running the script, verify the data:

```sql
-- Count records in each table
SELECT 'Users' as table_name, COUNT(*) as count FROM users
UNION ALL
SELECT 'Birds', COUNT(*) FROM birds
UNION ALL
SELECT 'Stories', COUNT(*) FROM stories
UNION ALL
SELECT 'Comments', COUNT(*) FROM comments
UNION ALL
SELECT 'Loves', COUNT(*) FROM loves
UNION ALL
SELECT 'Support Transactions', COUNT(*) FROM support_transactions
UNION ALL
SELECT 'Premium Subscriptions', COUNT(*) FROM bird_premium_subscriptions
UNION ALL
SELECT 'Memorial Messages', COUNT(*) FROM memorial_messages;
```

### Check User Data
```sql
SELECT name, email, email_confirmed, created_at 
FROM users 
ORDER BY created_at;
```

### Check Bird Data
```sql
SELECT name, species, owner_id, loved_count, supported_count, is_premium, is_memorial
FROM birds
ORDER BY created_at;
```

### Check Stories
```sql
SELECT s.content, b.name as bird_name, u.name as author_name, s.created_at
FROM stories s
JOIN birds b ON s.bird_id = b.bird_id
JOIN users u ON s.author_id = u.user_id
ORDER BY s.created_at DESC
LIMIT 10;
```

---

## Test Login in Your Application

After seeding, you can test login with:

**URL:** `POST http://localhost:5162/api/auth/login`

**Request Body:**
```json
{
  "email": "alice@example.com",
  "password": "Password123!"
}
```

**Expected Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "expiresAt": "2024-12-21T10:30:00Z",
  "userId": "11111111-1111-1111-1111-111111111111",
  "name": "Alice Johnson",
  "email": "alice@example.com",
  "emailConfirmed": true
}
```

---

## Test API Endpoints

### Get All Birds
```http
GET http://localhost:5162/api/birds
```

### Get Alice's Stories
```http
GET http://localhost:5162/api/stories/user/11111111-1111-1111-1111-111111111111
```

### Get Ruby's Profile
```http
GET http://localhost:5162/api/birds/aaaaaaaa-0001-0001-0001-000000000001
```

### Get Angel's Memorial
```http
GET http://localhost:5162/api/birds/dddddddd-0003-0003-0003-000000000003/memorial
```

---

## Troubleshooting ??

### Error: "psql: command not found"
- PostgreSQL is not in your PATH
- Use **Option 2** (manual execution with full path)
- Or use **Option 3** (pgAdmin GUI)

### Error: "password authentication failed"
- Check your password in `appsettings.json`
- Make sure PostgreSQL is running

### Error: "database does not exist"
- Create the database first:
  ```sql
  CREATE DATABASE wihngo_db;
  ```

### Error: "relation does not exist"
- Run your migrations first:
  ```bash
  dotnet ef database update
  ```
  Or apply your SQL schema before running this seed script

### Script Hangs or Takes Too Long
- The script should complete in 2-5 seconds
- If it hangs, check for connection issues
- Try running in smaller chunks

---

## Need to Reset Again?

Simply run the script again! It will:
1. ? Delete all existing data safely
2. ? Reset sequences
3. ? Insert fresh seed data

The script is **idempotent** - you can run it multiple times.

---

## Data Relationships Diagram

```
Users (5)
  ?? Owns ? Birds (10)
  ?   ?? Has ? Stories (20)
  ?   ?   ?? Has ? Comments (8)
  ?   ?   ?   ?? Has ? Comment Likes (4)
  ?   ?   ?? Has ? Story Likes (9)
  ?   ?? Has ? Support Transactions (11)
  ?   ?? Has ? Premium Subscriptions (5)
  ?   ?? Has (if memorial) ? Memorial Messages (4)
  ?? Creates ? Loves (14)
```

---

## Support

If you encounter issues:
1. Check PostgreSQL is running: `pg_ctl status`
2. Verify database exists: `psql -l`
3. Check connection string in `appsettings.json`
4. Review error messages carefully

---

**Happy Testing! ??**
