# ?? Sepolia Payment Completion Diagnostic

## Problem
You sent Sepolia test ETH and the transaction is completed on blockchain, but you're not seeing completion logs in the backend.

---

## ?? Quick Checks

### 1. Check Console Output
Look for these logs in your backend console (crypto-only logging is enabled):

**Expected logs when PaymentMonitorJob runs (every 30 seconds):**
```
[HH:mm:ss] info: === MONITORING PENDING PAYMENTS ===
[HH:mm:ss] info: Found X payments with transaction hash to monitor
[HH:mm:ss] info: [Payment payment-id] Current status: confirming, Confirmations: 6/6
[HH:mm:ss] info: [Payment payment-id] Blockchain verification: 6 confirmations
[HH:mm:ss] info: [Payment payment-id] ?? COMPLETED SUCCESSFULLY
```

**If you DON'T see these logs:**
- Backend might not be running
- Hangfire might be paused
- Payment status might be wrong

---

### 2. Check Database Directly

Run this SQL query in your PostgreSQL database:

```sql
-- Check your Sepolia payment status
SELECT 
    id,
    user_id,
    status,
    currency,
    network,
    amount_crypto,
    amount_usd,
    transaction_hash,
    confirmations,
    required_confirmations,
    created_at,
    confirmed_at,
    completed_at,
    expires_at
FROM crypto_payment_requests
WHERE network = 'sepolia'
ORDER BY created_at DESC
LIMIT 5;
```

**Check the status column:**
- ? `completed` - Payment processed successfully
- ?? `confirmed` - Confirmations met, but not completed yet (backend job needs to run)
- ? `confirming` - Transaction found, waiting for confirmations
- ?? `pending` - No transaction hash submitted yet
- ? `expired` - Payment window expired (30 minutes)
- ? `failed` - Verification failed

---

### 3. Manually Trigger Background Job

If the payment is stuck in `confirmed` status, you can manually trigger the job:

#### Option A: Via Hangfire Dashboard
1. Open browser: `http://localhost:5000/hangfire`
2. Go to "Recurring Jobs"
3. Find "monitor-payments"
4. Click "Trigger now"

#### Option B: Via SQL (Force Status Check)
```sql
-- Get the payment ID
SELECT id, status, confirmations, required_confirmations, transaction_hash
FROM crypto_payment_requests
WHERE network = 'sepolia' AND status IN ('confirming', 'confirmed')
ORDER BY created_at DESC
LIMIT 1;

-- If confirmations >= required_confirmations but status is not 'completed',
-- you can manually call the API endpoint:
-- POST /api/payments/crypto/{payment-id}/check-status
```

---

## ?? Detailed Diagnostic Steps

### Step 1: Verify Transaction on Blockchain

Visit Sepolia Etherscan:
```
https://sepolia.etherscan.io/tx/YOUR_TRANSACTION_HASH
```

**Check:**
- ? Transaction confirmed?
- ? Correct recipient address? (`0x4cc28f4cea7b440858b903b5c46685cb1478cdc4`)
- ? Correct amount sent?
- ? Number of confirmations (should be 6+)

---

### Step 2: Check Backend Status

1. **Is backend running?**
   ```sh
   # Check if process is running
   # On Windows:
   tasklist | findstr dotnet
   
   # On Linux/Mac:
   ps aux | grep dotnet
   ```

2. **Check console output**
   - Should see logs every 30 seconds: `=== MONITORING PENDING PAYMENTS ===`
   - If NO logs appear ? Hangfire might not be running

3. **Check Hangfire Dashboard**
   - Open: `http://localhost:5000/hangfire`
   - Go to "Recurring Jobs" tab
   - Find "monitor-payments"
   - Check "Last Execution" timestamp (should be within last 30 seconds)

---

### Step 3: Force Manual Check

If payment is stuck, force a manual status check via API:

**Using Postman or curl:**
```sh
curl -X POST "http://localhost:5000/api/payments/crypto/{PAYMENT_ID}/check-status" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json"
```

**Expected response if completed:**
```json
{
  "id": "payment-id",
  "status": "completed",
  "confirmations": 6,
  "requiredConfirmations": 6,
  "transactionHash": "0xabc123...",
  "completedAt": "2024-01-15T10:45:00Z"
}
```

---

## ?? Common Issues & Solutions

### Issue 1: No Logs Appearing

**Symptom:** Backend console shows no crypto payment logs at all.

**Causes:**
1. Backend not restarted after adding crypto-only logging
2. Hangfire server not started
3. PaymentMonitorJob not registered

**Fix:**
```sh
# Restart backend
dotnet run

# Wait 30 seconds and check console
# Should see: === MONITORING PENDING PAYMENTS ===
```

---

### Issue 2: Stuck in "confirming" or "confirmed"

**Symptom:** Database shows `status = 'confirmed'` but never changes to `completed`.

**Causes:**
1. PaymentMonitorJob not running
2. CompletePaymentAsync() throwing an error
3. Premium subscription service failing

**Fix:**
1. Check backend console for errors
2. Manually trigger job in Hangfire dashboard
3. Check if premium subscription exists in database:

```sql
SELECT * FROM bird_premium_subscriptions 
WHERE bird_id = (
  SELECT bird_id FROM crypto_payment_requests 
  WHERE id = 'your-payment-id'
);
```

---

### Issue 3: Transaction Not Found on Blockchain

**Symptom:** Logs show "Transaction not found on blockchain".

**Causes:**
1. Wrong transaction hash submitted
2. Transaction still pending (not mined yet)
3. Blockchain API (Infura) not responding

**Fix:**
1. Verify transaction hash on Sepolia Etherscan
2. Wait for transaction to be mined (should be ~12 seconds)
3. Check if transaction is on correct network (Sepolia, not mainnet)

---

## ?? Complete Diagnostic Workflow

### 1. Get Payment Information
```sql
SELECT id, status, transaction_hash, confirmations, required_confirmations
FROM crypto_payment_requests
WHERE network = 'sepolia'
ORDER BY created_at DESC
LIMIT 1;
```

### 2. Check Transaction on Blockchain
Visit: `https://sepolia.etherscan.io/tx/{transaction_hash}`

### 3. Check Backend Logs
Look for:
```
[HH:mm:ss] info: === MONITORING PENDING PAYMENTS ===
[HH:mm:ss] info: [Payment {id}] Current status: {status}
[HH:mm:ss] info: [Payment {id}] Blockchain verification: {confirmations} confirmations
```

### 4. If Stuck, Force Check
```sh
curl -X POST "http://localhost:5000/api/payments/crypto/{payment-id}/check-status" \
  -H "Authorization: Bearer {token}"
```

### 5. Check Result
```sql
SELECT id, status, completed_at
FROM crypto_payment_requests
WHERE id = 'your-payment-id';
```

---

## ? Expected Log Flow (Successful Payment)

```
[10:30:00] info: === MONITORING PENDING PAYMENTS ===
[10:30:00] info: Found 1 payments with transaction hash to monitor
[10:30:00] info: [Payment abc-123] Current status: confirming, Confirmations: 2/6
[10:30:00] info: [Payment abc-123] Blockchain verification: 2 confirmations
[10:30:00] info: [Payment abc-123] Still confirming - 2/6 confirmations

[10:30:30] info: === MONITORING PENDING PAYMENTS ===
[10:30:30] info: Found 1 payments with transaction hash to monitor
[10:30:30] info: [Payment abc-123] Current status: confirming, Confirmations: 4/6
[10:30:30] info: [Payment abc-123] Blockchain verification: 4 confirmations
[10:30:30] info: [Payment abc-123] Still confirming - 4/6 confirmations

[10:31:00] info: === MONITORING PENDING PAYMENTS ===
[10:31:00] info: Found 1 payments with transaction hash to monitor
[10:31:00] info: [Payment abc-123] Current status: confirming, Confirmations: 6/6
[10:31:00] info: [Payment abc-123] Blockchain verification: 6 confirmations
[10:31:00] info: [Payment abc-123] Status changed from 'confirming' to 'confirmed'
[10:31:00] info: [Payment abc-123] ?? COMPLETED SUCCESSFULLY - Status changed from 'confirmed' to 'completed'

?? SUCCESS: Payment abc-123 completed successfully!
   User: user-guid
   Amount: 0.003667 ETH ($11)
   Transaction: 0xdef456...
   Confirmations: 6/6
   Status: confirming -> completed
   Completed At: 2024-01-15T10:31:00Z
```

---

## ?? Still Not Working?

### Quick Fixes:

1. **Restart backend**
   ```sh
   # Stop and restart
   dotnet run
   ```

2. **Wait 30 seconds**
   - PaymentMonitorJob runs every 30 seconds
   - Be patient!

3. **Check Hangfire Dashboard**
   - `http://localhost:5000/hangfire`
   - See if job is running

4. **Manual API Call**
   - Force check via API endpoint
   - `/api/payments/crypto/{payment-id}/check-status`

5. **Check Database**
   - Run SQL queries above
   - Verify status and confirmations

---

## ?? Information Needed for Support

If still not working, provide:

1. **Payment ID** (from database)
2. **Transaction Hash** (from your wallet)
3. **Console Output** (last 50 lines)
4. **Database Status** (result of SQL query)
5. **Blockchain Status** (Sepolia Etherscan screenshot)
6. **Hangfire Dashboard** (screenshot of recurring jobs)

---

**Most likely cause: PaymentMonitorJob hasn't run yet. Wait 30 seconds and check logs!** ?
