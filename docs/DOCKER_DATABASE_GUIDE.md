# ?? Docker PostgreSQL Setup Guide

## ? Connection String Created

Your application is now configured to connect to your PostgreSQL Docker container:

```
Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres
```

This connection string has been added to `appsettings.Development.json`.

---

## ?? Quick Start (3 Steps)

### Step 1: Run Setup Script
Double-click:
```
setup-docker-database.bat
```

This will:
- Start your Docker container (if not running)
- Create `wihngo` database
- Test the connection

### Step 2: Restart Your Application
- Stop debugger: `Shift+F5`
- Start debugger: `F5`

### Step 3: Watch the Magic! ?

Your console will show:
```
? Database connection successful!
? Database created successfully!
?? Seeding development data...
? Seeded 5 users
? Seeded 10 birds
? Seeded 35 loves
? Seeded 25 support transactions
? Seeded 12 stories
? Seeded 6 notifications
? Seeded 5 invoices
? Seeded 8 crypto payment requests
? Database seeding completed successfully!
```

---

## ?? What Gets Seeded

### 1. **5 Test Users**
- Alice Johnson (alice@example.com)
- Bob Smith (bob@example.com)
- Carol Williams (carol@example.com)
- David Brown (david@example.com)
- Eve Davis (eve@example.com)

**Password for all:** `Password123!`

### 2. **10 Hummingbirds**
- Sunny (Anna's Hummingbird)
- Flash (Ruby-throated Hummingbird)
- Bella (Black-chinned Hummingbird)
- Spike (Allen's Hummingbird)
- Luna (Calliope Hummingbird)
- Zippy (Rufous Hummingbird)
- Jewel (Costa's Hummingbird)
- Blaze (Broad-tailed Hummingbird)
- Misty (Buff-bellied Hummingbird)
- Emerald (Magnificent Hummingbird)

Each bird has:
- Name, species, and description
- Love counts (10-150)
- Donation amounts ($1-$500)
- Associated owner

### 3. **35 Loves**
Users loving various birds (3-7 birds per user)

### 4. **25 Support Transactions**
Donations ranging from $5-$100 with optional messages

### 5. **12 Stories**
Bird updates like "First Sighting!", "Bath Time", "Nest Building"

### 6. **6 Notifications**
Mix of "Bird Loved" and "Bird Supported" notifications

### 7. **5 Invoices**
Sample invoices ($10-$200) with mixed paid/pending status

### 8. **8 Crypto Payment Requests**
USDC/EURC payments on Solana/Base ($10-$100)

### 9. **4 Supported Tokens**
- USDC on Solana & Base
- EURC on Solana & Base

---

## ?? Test Your Data

### Login as a Test User
```bash
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "Password123!"
}
```

### View All Birds
```bash
GET http://localhost:5000/api/birds
```

You should see 10 birds with realistic data!

### View Specific Bird
```bash
GET http://localhost:5000/api/birds/{bird-id}
```

---

## ?? Docker Commands

### View Your Container
```bash
docker ps
```

### Connect to Database
```bash
docker exec -it postgres psql -U postgres -d wihngo
```

### View Tables
```sql
\dt
```

### Check Seeded Data
```sql
-- Count users
SELECT COUNT(*) FROM users;

-- Count birds
SELECT COUNT(*) FROM birds;

-- View birds with love counts
SELECT name, species, loved_count, donation_cents 
FROM birds 
ORDER BY loved_count DESC;

-- View support transactions
SELECT b.name, s.amount, s.message, s.created_at
FROM support_transactions s
JOIN birds b ON s.bird_id = b.bird_id
ORDER BY s.created_at DESC;
```

### Stop Database
```bash
docker stop postgres
```

### Start Database
```bash
docker start postgres
```

### Remove Container (WARNING: Deletes data!)
```bash
docker stop postgres
docker rm postgres
```

---

## ?? Reset Database with Fresh Data

To completely reset and reseed:

### Option 1: Using Script
```bash
# Stop and remove container
docker stop postgres
docker rm postgres

# Run setup script again
setup-docker-database.bat

# Restart your app - fresh data will be seeded!
```

### Option 2: Manual
```bash
# Connect to database
docker exec -it postgres psql -U postgres

# Drop and recreate database
DROP DATABASE IF EXISTS wihngo;
CREATE DATABASE wihngo OWNER postgres;
\q

# Restart your app - it will recreate tables and seed data
```

---

## ?? Project Structure

```
Wihngo/
??? Database/
?   ??? DatabaseSeeder.cs          # Comprehensive seed data
??? appsettings.Development.json   # Connection string
??? setup-docker-database.bat      # Setup script
??? Program.cs                     # Calls seeder on startup
```

---

## ?? Benefits

? **Realistic Data** - 10 birds, 5 users, transactions, stories  
? **Ready to Test** - Login and explore immediately  
? **Consistent** - Seeded with fixed random seed for reproducibility  
? **Comprehensive** - All major entities populated  
? **Fast** - Seeds in < 2 seconds  

---

## ?? Customization

Want different seed data? Edit `Database/DatabaseSeeder.cs`:

```csharp
private static async Task<List<User>> SeedUsersAsync(...)
{
    var users = new List<User>
    {
        new User
        {
            Name = "Your Name",
            Email = "your@email.com",
            // ... customize ...
        }
    };
    // ...
}
```

Then restart your app!

---

## ?? Important Notes

1. **Development Only** - Seeding only runs in Development environment
2. **One-Time** - Seeds only when database is freshly created
3. **Safe** - Won't duplicate data if tables already have records
4. **Password** - All test users use `Password123!`

---

## ?? Troubleshooting

### "Database not seeding"
- Make sure you're in Development environment
- Delete the database and let it recreate
- Check console logs for errors

### "Cannot connect to Docker"
```bash
# Make sure Docker Desktop is running
docker ps

# If not, start Docker Desktop
```

### "Port 5432 already in use"
```bash
# Find what's using it
netstat -ano | findstr :5432

# Stop other PostgreSQL or change port in docker command
```

---

## ? What's Next?

1. **? Database is ready** with dummy data
2. **? Login with test users** (alice@example.com / Password123!)
3. **? Explore birds, loves, transactions** via API
4. **? Test all endpoints** with real-looking data
5. **? Develop new features** with confidence!

---

**Your Docker PostgreSQL database is all set with comprehensive seed data!** ??

Run `setup-docker-database.bat` and restart your app to get started!
