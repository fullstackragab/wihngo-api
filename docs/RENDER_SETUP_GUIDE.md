# Wihngo Database Setup - Complete Guide

## ? What We've Built

I've created a **complete invoice/payment system** with:
- ? 8 new tables for invoice/payment tracking
- ? PayPal, Solana, and Base payment support
- ? Automatic invoice numbering
- ? PDF receipt generation
- ? Email notifications
- ? Push notifications
- ? Refund processing
- ? Comprehensive audit logs
- ? Blockchain listeners
- ? Daily reconciliation jobs

## ??? Database Setup Options

### **Option 1: Run SQL Script (Recommended)**

1. **Open Render.com Dashboard**
   - Go to https://dashboard.render.com
   - Navigate to your database: `wihngo_kzno`
   - Click "Connect" ? "External Connection" ? "PSQL Command"

2. **Copy the Connection Command**
   ```bash
   PGPASSWORD=YOUR_DB_PASSWORD psql -h YOUR_DB_HOST -U wihngo wihngo_kzno
   ```

3. **Run the Setup Script**
   ```bash
   # If psql is installed locally:
   PGPASSWORD=YOUR_DB_PASSWORD psql -h YOUR_DB_HOST -U wihngo -d wihngo_kzno -f render-database-setup.sql
   ```

### **Option 2: Use Render Web Shell**

1. Go to Render Dashboard ? Your Database ? "Shell" tab
2. Copy the entire content from `render-database-setup.sql`
3. Paste and execute in the web shell

### **Option 3: Use pgAdmin or DBeaver**

1. Connect with these credentials:
   - Host: `YOUR_DB_HOST`
   - Port: `5432`
   - Database: `wihngo_kzno`
   - Username: `wihngo`
   - Password: `YOUR_DB_PASSWORD`
   - SSL: Required

2. Open `render-database-setup.sql` and execute

---

## ?? After Database Setup

### **1. Verify Tables Created**
```sql
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;
```

Should show **28 tables** including:
- `invoices`, `payments`, `supported_tokens`, `refund_requests`
- `users`, `birds`, `stories`, `loves`
- `notifications`, `user_devices`
- And more...

### **2. Check Seed Data**
```sql
-- Test user
SELECT * FROM users WHERE email = 'test@wihngo.com';

-- Supported tokens with YOUR addresses
SELECT token_symbol, chain, merchant_receiving_address 
FROM supported_tokens;

-- Sample bird
SELECT * FROM birds;
```

### **3. Test Login Credentials**
- **Email:** `test@wihngo.com`
- **Password:** `Test123!`

---

## ?? Start the Application

```bash
dotnet run
```

The application will:
- ? Connect to your Render database
- ? Initialize Hangfire background jobs
- ? Start blockchain listeners
- ? Enable all API endpoints

---

## ?? Test API Endpoints

### **1. Health Check**
```bash
curl http://localhost:5000/test
```

### **2. Login (Get JWT Token)**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@wihngo.com",
    "password": "Test123!"
  }'
```

### **3. Create Invoice**
```bash
curl -X POST http://localhost:5000/api/v1/invoices \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amountFiat": 10.00,
    "fiatCurrency": "USD",
    "preferredPaymentMethods": ["paypal", "solana", "base"],
    "metadata": { "purpose": "test payment" }
  }'
```

Response will include:
- Invoice ID
- Payment instructions for Solana, Base, and PayPal
- QR code data
- Expiration time

---

## ?? Configuration Needed

### **Your Crypto Addresses (Already Configured!)**
? **Solana:** `AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn`
? **Base:** `0x2e61b5d2066eAFb86FBD75F59c585468ceE51092`

### **Optional: Add PayPal Credentials**
Edit `appsettings.Development.json`:
```json
{
  "PayPal": {
    "ClientId": "YOUR_PAYPAL_CLIENT_ID",
    "ClientSecret": "YOUR_PAYPAL_CLIENT_SECRET",
    "Mode": "sandbox",
    "WebhookId": "YOUR_WEBHOOK_ID"
  }
}
```

### **Optional: Add SMTP for Email Receipts**
```json
{
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

---

## ?? Database Schema Summary

### **Invoice & Payment Tables**
1. **invoices** - Full invoice lifecycle
2. **payments** - Payment records (blockchain + PayPal)
3. **supported_tokens** - Crypto tokens config
4. **refund_requests** - Refund tracking
5. **payment_events** - Event log
6. **audit_logs** - Comprehensive audit trail
7. **webhooks_received** - PayPal webhook deduplication
8. **blockchain_cursors** - Blockchain listener state

### **Core App Tables** (Already in your DB)
- users, birds, stories, loves
- support_transactions, premium_subscriptions
- charities, charity_allocations
- notifications, user_devices
- crypto_payments, exchange_rates
- on_chain_deposits

---

## ?? What You Can Do Now

1. ? **Create invoices** with payment instructions
2. ? **Accept payments** via PayPal, Solana (USDC/EURC), Base (USDC/EURC)
3. ? **Auto-generate PDF receipts** with blockchain explorer links
4. ? **Email receipts** to users
5. ? **Send push notifications** when invoices are paid
6. ? **Process refunds** (automatic for PayPal, approval-based for crypto)
7. ? **Monitor payments** in real-time with blockchain listeners
8. ? **View Hangfire dashboard** at `/hangfire`
9. ? **Track everything** with comprehensive audit logs

---

## ?? Need Help?

### **Can't connect to database?**
- Check if SSL is enabled in connection string
- Verify Render database is "Available" in dashboard
- Test connection with: `psql "postgresql://wihngo:YOUR_DB_PASSWORD@YOUR_DB_HOST:5432/wihngo_kzno?sslmode=require"`

### **Database setup failed?**
- Run the SQL script section by section
- Check for existing tables and drop them first
- Verify you have proper permissions on Render

### **Application won't start?**
- Check `appsettings.Development.json` connection string
- Verify JWT key is set (min 32 characters)
- Run `dotnet build` to check for errors

---

## ?? Documentation

- **Full Setup Guide:** `PAYMENT_SYSTEM_README.md`
- **SQL Script:** `render-database-setup.sql`
- **API Docs:** Run app and visit `/swagger` (if enabled)

---

**?? You're all set! Once the database is created, just run `dotnet run` and start accepting payments!**
