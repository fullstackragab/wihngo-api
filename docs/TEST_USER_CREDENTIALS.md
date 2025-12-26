# ?? Test User Credentials & Statistics

## Quick Access Endpoints

### Get All Users with Statistics
```
GET http://localhost:5162/api/dev/users
GET http://192.168.8.5:5162/api/dev/users
```

### Get Specific User Details
```
GET http://localhost:5162/api/dev/users/alice@example.com
```

### Get Quick Credentials Summary
```
GET http://localhost:5162/api/dev/test-credentials
```

---

## ?? Recommended Test User

**Email:** `alice@example.com`  
**Password:** `Password123!`

**Why Alice?** She has the most complete data set for testing:
- ? Email confirmed
- ?? Owns multiple birds
- ?? Has written stories
- ?? Has loved other birds
- ?? Has made support transactions

---

## ?? All Test Users (Based on Seeded Data)

### 1. **Alice Johnson**
**Email:** `alice@example.com`  
**Password:** `Password123!`

**Approximate Stats:**
- ?? Birds Owned: 2 (Sunny, Flash)
- ?? Stories Written: 2-3
- ?? Birds Loved: 3-7
- ?? Support Given: 4-6 transactions

**Her Birds:**
- **Sunny** - Anna's Hummingbird - "A vibrant backyard regular who guards her favorite feeder"
- **Flash** - Ruby-throated Hummingbird - "Named for his incredible speed and iridescent throat"

---

### 2. **Bob Smith**
**Email:** `bob@example.com`  
**Password:** `Password123!`

**Approximate Stats:**
- ?? Birds Owned: 2 (Bella, Spike)
- ?? Stories Written: 2-3
- ?? Birds Loved: 3-7
- ?? Support Given: 4-6 transactions

**His Birds:**
- **Bella** - Black-chinned Hummingbird - "A gentle soul who shares feeders with everyone"
- **Spike** - Allen's Hummingbird - "Territorial and fierce, but beautiful to watch"

---

### 3. **Carol Williams**
**Email:** `carol@example.com`  
**Password:** `Password123!`

**Approximate Stats:**
- ?? Birds Owned: 2 (Luna, Zippy)
- ?? Stories Written: 1-3
- ?? Birds Loved: 3-7
- ?? Support Given: 4-6 transactions

**Her Birds:**
- **Luna** - Calliope Hummingbird - "Our smallest visitor with the biggest personality"
- **Zippy** - Rufous Hummingbird - "Travels 3,000 miles for migration - a true warrior"

---

### 4. **David Brown**
**Email:** `david@example.com`  
**Password:** `Password123!`

**Approximate Stats:**
- ?? Birds Owned: 2 (Jewel, Blaze)
- ?? Stories Written: 1-2
- ?? Birds Loved: 3-7
- ?? Support Given: 4-6 transactions

**His Birds:**
- **Jewel** - Costa's Hummingbird - "Desert beauty with a purple crown"
- **Blaze** - Broad-tailed Hummingbird - "His wings whistle like a cricket"

---

### 5. **Eve Davis**
**Email:** `eve@example.com`  
**Password:** `Password123!`

**Approximate Stats:**
- ?? Birds Owned: 2 (Misty, Emerald)
- ?? Stories Written: 0-1
- ?? Birds Loved: 3-7
- ?? Support Given: 4-6 transactions

**Her Birds:**
- **Misty** - Buff-bellied Hummingbird - "Rare visitor from Mexico who stole our hearts"
- **Emerald** - Magnificent Hummingbird - "Living up to his name every single day"

---

## ?? How to Login

### Option 1: Using the New Dev Endpoint
```bash
# Get all users with stats
curl http://localhost:5162/api/dev/test-credentials
```

### Option 2: Login via API
```bash
curl -X POST http://localhost:5162/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "alice@example.com",
    "password": "Password123!"
  }'
```

### Option 3: Using Postman/Insomnia
```
POST http://localhost:5162/api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "Password123!"
}
```

### Option 4: Mobile App (React Native)
Make sure your API URL is set to:
- Android Emulator: `http://10.0.2.2:5162/api`
- Physical Device: `http://192.168.8.5:5162/api`

Then use:
```javascript
{
  email: "alice@example.com",
  password: "Password123!"
}
```

---

## ?? Get Detailed User Stats

### Via Dev Endpoint (Development Only)
```bash
# Get complete stats for Alice
curl http://localhost:5162/api/dev/users/alice@example.com

# Response includes:
# - Login credentials
# - User information
# - Statistics (birds, stories, loves, donations)
# - List of birds owned with details
# - List of birds loved
# - Support transactions given
```

---

## ?? Important Notes

1. **All test users have the same password:** `Password123!`
2. **All emails are pre-confirmed** - no email verification needed
3. **These endpoints are DEVELOPMENT ONLY** - they return 404 in production
4. **No passwords are exposed** - the endpoint shows "Password123!" as a hint, not actual hashes
5. **Data is randomly seeded** - exact counts may vary slightly

---

## ?? Features to Test with Alice

After logging in as Alice, you can test:

? **View Bird List** - See all birds in the system  
? **View My Birds** - See Alice's birds (Sunny and Flash)  
? **Read Stories** - View stories about birds  
? **Love Birds** - Like/unlike birds  
? **Make Donations** - Support other birds  
? **View Notifications** - See Alice's notifications  
? **View Profile** - See Alice's profile and stats  

---

## ?? Reset Test Data

If you need to reset and reseed the database:

1. Stop the API
2. Drop the database or delete all tables
3. Restart the API - it will automatically:
   - Create all tables
   - Seed development data (in Development environment)

---

## ?? Need More Info?

Use the development endpoints while the app is running:

```bash
# Quick credentials
GET http://localhost:5162/api/dev/test-credentials

# All users with stats
GET http://localhost:5162/api/dev/users

# Specific user details
GET http://localhost:5162/api/dev/users/{email}
```
