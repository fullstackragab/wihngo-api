# Prompt for GitHub Copilot (Copy & Paste This)

## Context
Our backend has been updated to fix payment status update issues. We need to implement real-time payment status polling in our React Native Expo mobile app.

## Task
Implement a payment status polling system that checks payment confirmation status every 5 seconds and displays progress to the user.

## Requirements

### 1. Create Custom Hook: `hooks/usePaymentStatusPolling.ts`

Create a hook that:
- Accepts: `paymentId`, `authToken`, `enabled` (boolean), `onStatusChange` (callback)
- Polls the endpoint every 5 seconds: `POST /api/payments/crypto/{paymentId}/check-status`
- Returns: `status`, `confirmations`, `requiredConfirmations`, `loading`, `error`, `forceCheck()`
- Auto-stops polling when status is: confirmed, completed, expired, cancelled, or failed
- Cleans up interval on unmount
- Uses Authorization Bearer token in headers

### 2. Update Payment Screen Component

Enhance the payment waiting screen to:
- Use the `usePaymentStatusPolling` hook
- Display current status with color indicator:
  - Pending: Orange (#FFA500)
  - Confirming: Blue (#3498db)
  - Confirmed/Completed: Green (#27ae60)
  - Expired/Failed: Red (#e74c3c)
- Show confirmation progress bar when status is "confirming"
- Display: "Confirmations: X / Y" (e.g., "5 / 19")
- Include a "Check Status Now" button that calls `forceCheck()`
- Show loading indicator while checking
- Display error messages if check fails
- Show success message when payment is confirmed
- Auto-navigate or show modal on successful payment

### 3. TypeScript Types

Create/update `types/payment.ts` with:
```typescript
export interface PaymentStatus {
  id: string;
  userId: string;
  birdId?: string;
  amountUsd: number;
  amountCrypto: number;
  currency: string;
  network: string;
  exchangeRate: number;
  walletAddress: string;
  userWalletAddress?: string;
  qrCodeData: string;
  paymentUri: string;
  transactionHash?: string;
  confirmations: number;
  requiredConfirmations: number;
  status: 'pending' | 'confirming' | 'confirmed' | 'completed' | 'expired' | 'cancelled' | 'failed';
  purpose: string;
  plan?: string;
  expiresAt: string;
  confirmedAt?: string;
  completedAt?: string;
  createdAt: string;
  updatedAt: string;
}
```

## API Endpoint Details

**New Endpoint:**
```
POST /api/payments/crypto/{paymentId}/check-status
Authorization: Bearer {token}
Content-Type: application/json

Response:
{
  "id": "uuid",
  "status": "confirming",
  "confirmations": 5,
  "requiredConfirmations": 19,
  "transactionHash": "0x123...",
  "currency": "USDT",
  "network": "tron",
  "amountCrypto": 10.50,
  "walletAddress": "TXxxx...",
  ...other fields...
}
```

## Important Notes

1. **Environment Variable**: Use `process.env.EXPO_PUBLIC_API_URL` for the API base URL
2. **Polling Interval**: 5 seconds (5000ms)
3. **Terminal Statuses**: Stop polling when status is: confirmed, completed, expired, cancelled, failed
4. **Error Handling**: Show user-friendly error messages, keep polling active even if one request fails
5. **Performance**: Clean up intervals properly to prevent memory leaks
6. **User Experience**: Show immediate feedback when user taps "Check Status Now"

## User Flow

1. User completes crypto payment and submits transaction hash
2. App navigates to payment status screen
3. Hook starts polling automatically every 5 seconds
4. User sees:
   - "Waiting for payment..." (status: pending)
   - "Confirming (5/19)" with progress bar (status: confirming)
   - "Payment confirmed!" (status: confirmed)
   - Success message appears
5. Polling stops automatically
6. User can manually tap "Check Status Now" for immediate update

## Style Guidelines

- Use consistent styling with the rest of the app
- Make status indicators prominent
- Use smooth animations for progress bar
- Provide clear visual feedback for loading states
- Keep success message celebratory but professional

## Example Usage

After implementing, the screen should be used like this:

```typescript
<PaymentStatusScreen
  paymentId={route.params.paymentId}
  authToken={user.token}
  onSuccess={() => navigation.navigate('Success')}
/>
```

## Testing

Test these scenarios:
1. Payment with immediate confirmation
2. Payment with gradual confirmations (0?1?2...?19)
3. Network error during polling
4. App going to background and foreground
5. Expired payment
6. Manual "Check Status Now" button

---

Please implement this following React Native and TypeScript best practices. Use functional components with hooks, proper error handling, and clean, maintainable code.
