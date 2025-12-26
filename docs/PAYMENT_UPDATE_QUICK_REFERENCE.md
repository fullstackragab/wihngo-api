# Payment Status Update - Quick Reference

## What Changed (Backend)

### 1. Faster Monitoring
- Payment check: **60s ? 30s**
- File: `Program.cs` line 165

### 2. New Endpoint
```
POST /api/payments/crypto/{paymentId}/check-status
```
- Instantly checks blockchain
- Returns current payment status
- Use for real-time polling

### 3. Better Logging
- Enhanced `PaymentMonitorJob.cs`
- Tracks payments without transaction hash
- Detailed confirmation counts

---

## What to Do (Frontend - React Native)

### Quick Implementation

```typescript
// 1. Create hook for polling
const { status, confirmations, requiredConfirmations } = usePaymentStatusPolling({
  paymentId,
  authToken,
  enabled: true,
  onStatusChange: (payment) => {
    if (payment.status === 'confirmed') {
      showSuccess();
    }
  }
});

// 2. Poll every 5 seconds automatically
// Hook handles this internally

// 3. Show progress
{status === 'confirming' && (
  <Text>Confirmations: {confirmations}/{requiredConfirmations}</Text>
)}
```

### API Call
```typescript
const response = await fetch(
  `${API_URL}/api/payments/crypto/${paymentId}/check-status`,
  {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` }
  }
);
const payment = await response.json();
```

---

## Full Flow

1. User sends crypto to wallet ?
2. User submits transaction hash via `/verify` endpoint ?
3. Frontend polls `/check-status` every 5 seconds ?
4. Backend checks blockchain every 30 seconds ?
5. Status updates: pending ? confirming ? confirmed ? completed ?
6. Frontend shows success ?

---

## Files to Give to GitHub Copilot

Give these instructions to GitHub Copilot for your React Native app:

**Prompt for Copilot:**
```
Please implement real-time payment status polling for crypto payments:

1. Create a custom hook `usePaymentStatusPolling` that:
   - Calls POST /api/payments/crypto/{paymentId}/check-status every 5 seconds
   - Stops polling when status is terminal (confirmed, completed, expired, cancelled, failed)
   - Provides forceCheck() method for manual refresh
   - Accepts onStatusChange callback

2. Update the PaymentScreen to:
   - Use the polling hook
   - Show confirmation progress (e.g., "5/19 confirmations")
   - Display status with color coding
   - Show success message when confirmed
   - Include "Check Status Now" button

3. Add TypeScript types for PaymentStatus

Reference the detailed implementation in FRONTEND_PAYMENT_UPDATE_INSTRUCTIONS.md
```

---

## Test It

1. **Create payment** ? Get payment ID
2. **Send crypto** ? Submit transaction hash
3. **Watch screen** ? Should update within 5 seconds
4. **See confirmations** ? Increase: 0 ? 1 ? 2 ? ... ? 19
5. **Success!** ? Payment completed

---

## Timing

- **Frontend polling**: Every 5 seconds
- **Backend monitoring**: Every 30 seconds
- **Max delay**: 5 seconds (frontend triggers immediate check)
- **Old delay**: Up to 60 seconds

**Result**: 12x faster user feedback! ??
