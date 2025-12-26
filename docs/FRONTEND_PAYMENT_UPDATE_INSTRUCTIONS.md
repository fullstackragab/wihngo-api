# Frontend Payment Status Update Instructions

## Overview
The backend has been updated to fix the payment status update issue. Money arriving at the destination wallet will now be detected and processed faster.

## Backend Changes Summary

### 1. Faster Background Monitoring
- Payment monitoring job now runs every **30 seconds** (was 60 seconds)
- Reduces maximum detection delay by 50%

### 2. New Real-Time Status Check Endpoint
- **Endpoint**: `POST /api/payments/crypto/{paymentId}/check-status`
- **Purpose**: Immediately check blockchain for payment confirmation
- **Use Case**: Call this while user is waiting on payment screen

### 3. Enhanced Logging
- Better error tracking for debugging payment issues
- Detailed confirmation counts logged

---

## Required Frontend Changes for React Native (Expo)

### Implementation Steps

#### 1. Add Real-Time Payment Status Polling

Create or update your payment monitoring hook:

```typescript
// hooks/usePaymentStatusPolling.ts
import { useState, useEffect, useCallback, useRef } from 'react';
import { PaymentStatus } from '../types/payment';

interface UsePaymentStatusPollingProps {
  paymentId: string;
  authToken: string;
  onStatusChange?: (status: PaymentStatus) => void;
  enabled?: boolean;
}

export const usePaymentStatusPolling = ({
  paymentId,
  authToken,
  onStatusChange,
  enabled = true
}: UsePaymentStatusPollingProps) => {
  const [status, setStatus] = useState<string>('pending');
  const [confirmations, setConfirmations] = useState<number>(0);
  const [requiredConfirmations, setRequiredConfirmations] = useState<number>(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const checkPaymentStatus = useCallback(async () => {
    if (!enabled || !paymentId) return;

    try {
      setLoading(true);
      setError(null);

      const response = await fetch(
        `${process.env.EXPO_PUBLIC_API_URL}/api/payments/crypto/${paymentId}/check-status`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${authToken}`,
            'Content-Type': 'application/json',
          },
        }
      );

      if (!response.ok) {
        throw new Error('Failed to check payment status');
      }

      const payment = await response.json();

      setStatus(payment.status);
      setConfirmations(payment.confirmations || 0);
      setRequiredConfirmations(payment.requiredConfirmations || 0);

      onStatusChange?.(payment);

      // Stop polling if payment is in a terminal state
      if (['confirmed', 'completed', 'expired', 'cancelled', 'failed'].includes(payment.status)) {
        if (intervalRef.current) {
          clearInterval(intervalRef.current);
          intervalRef.current = null;
        }
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
      console.error('Payment status check error:', err);
    } finally {
      setLoading(false);
    }
  }, [paymentId, authToken, enabled, onStatusChange]);

  useEffect(() => {
    if (!enabled) {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
      return;
    }

    // Check immediately
    checkPaymentStatus();

    // Then poll every 5 seconds
    intervalRef.current = setInterval(checkPaymentStatus, 5000);

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [enabled, checkPaymentStatus]);

  const forceCheck = useCallback(() => {
    checkPaymentStatus();
  }, [checkPaymentStatus]);

  return {
    status,
    confirmations,
    requiredConfirmations,
    loading,
    error,
    forceCheck,
  };
};
```

#### 2. Update Payment Screen Component

```typescript
// screens/PaymentScreen.tsx
import React, { useState, useEffect } from 'react';
import { View, Text, StyleSheet, ActivityIndicator, TouchableOpacity } from 'react-native';
import { usePaymentStatusPolling } from '../hooks/usePaymentStatusPolling';

interface PaymentScreenProps {
  paymentId: string;
  authToken: string;
}

export const PaymentScreen: React.FC<PaymentScreenProps> = ({ paymentId, authToken }) => {
  const [showSuccess, setShowSuccess] = useState(false);

  const {
    status,
    confirmations,
    requiredConfirmations,
    loading,
    error,
    forceCheck
  } = usePaymentStatusPolling({
    paymentId,
    authToken,
    enabled: true,
    onStatusChange: (payment) => {
      console.log('Payment status updated:', payment);
      
      if (payment.status === 'confirmed' || payment.status === 'completed') {
        setShowSuccess(true);
        // Navigate to success screen or show success modal
      }
    }
  });

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'pending':
        return '#FFA500';
      case 'confirming':
        return '#3498db';
      case 'confirmed':
      case 'completed':
        return '#27ae60';
      case 'expired':
      case 'failed':
        return '#e74c3c';
      default:
        return '#95a5a6';
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case 'pending':
        return 'Waiting for payment...';
      case 'confirming':
        return `Confirming (${confirmations}/${requiredConfirmations})`;
      case 'confirmed':
        return 'Payment confirmed!';
      case 'completed':
        return 'Payment completed!';
      case 'expired':
        return 'Payment expired';
      case 'failed':
        return 'Payment failed';
      default:
        return status;
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.statusContainer}>
        <View style={[styles.statusIndicator, { backgroundColor: getStatusColor(status) }]} />
        <Text style={styles.statusText}>{getStatusText(status)}</Text>
      </View>

      {status === 'confirming' && (
        <View style={styles.progressContainer}>
          <Text style={styles.progressText}>
            Confirmations: {confirmations} / {requiredConfirmations}
          </Text>
          <View style={styles.progressBar}>
            <View
              style={[
                styles.progressFill,
                {
                  width: `${(confirmations / requiredConfirmations) * 100}%`,
                },
              ]}
            />
          </View>
        </View>
      )}

      {loading && <ActivityIndicator size="large" color="#3498db" style={styles.loader} />}

      {error && (
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>{error}</Text>
        </View>
      )}

      <TouchableOpacity
        style={styles.refreshButton}
        onPress={forceCheck}
        disabled={loading}
      >
        <Text style={styles.refreshButtonText}>
          {loading ? 'Checking...' : 'Check Status Now'}
        </Text>
      </TouchableOpacity>

      {showSuccess && (
        <View style={styles.successContainer}>
          <Text style={styles.successText}>? Payment Successful!</Text>
          <Text style={styles.successSubtext}>Your premium features are now active</Text>
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
    backgroundColor: '#fff',
  },
  statusContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 20,
  },
  statusIndicator: {
    width: 12,
    height: 12,
    borderRadius: 6,
    marginRight: 10,
  },
  statusText: {
    fontSize: 18,
    fontWeight: '600',
  },
  progressContainer: {
    marginBottom: 20,
  },
  progressText: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
  },
  progressBar: {
    height: 8,
    backgroundColor: '#e0e0e0',
    borderRadius: 4,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    backgroundColor: '#3498db',
    borderRadius: 4,
  },
  loader: {
    marginVertical: 20,
  },
  errorContainer: {
    backgroundColor: '#ffebee',
    padding: 12,
    borderRadius: 8,
    marginBottom: 20,
  },
  errorText: {
    color: '#c62828',
    fontSize: 14,
  },
  refreshButton: {
    backgroundColor: '#3498db',
    padding: 16,
    borderRadius: 8,
    alignItems: 'center',
    marginTop: 20,
  },
  refreshButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  successContainer: {
    backgroundColor: '#e8f5e9',
    padding: 20,
    borderRadius: 12,
    alignItems: 'center',
    marginTop: 20,
  },
  successText: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#27ae60',
    marginBottom: 8,
  },
  successSubtext: {
    fontSize: 14,
    color: '#666',
  },
});
```

#### 3. Update Payment Types

```typescript
// types/payment.ts
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

#### 4. Environment Configuration

Add to your `.env` file:

```env
EXPO_PUBLIC_API_URL=https://your-api-domain.com
```

Or for local development:

```env
EXPO_PUBLIC_API_URL=http://localhost:5000
```

---

## Usage Flow

### After User Submits Transaction Hash

1. User completes the crypto transfer
2. User enters transaction hash via the verify endpoint:
   ```typescript
   await fetch(`${API_URL}/api/payments/crypto/${paymentId}/verify`, {
     method: 'POST',
     headers: {
       'Authorization': `Bearer ${token}`,
       'Content-Type': 'application/json',
     },
     body: JSON.stringify({
       transactionHash: userEnteredHash,
       userWalletAddress: userWalletAddress // optional
     })
   });
   ```

3. Navigate to payment status screen
4. The `usePaymentStatusPolling` hook automatically:
   - Checks status immediately
   - Polls every 5 seconds
   - Updates UI with confirmations progress
   - Stops polling when payment is confirmed/completed
   - Shows success message

### Manual Refresh

Users can also tap "Check Status Now" button to force an immediate check without waiting for the next poll interval.

---

## Testing

### Test Scenarios

1. **Happy Path**
   - Submit transaction hash
   - Watch confirmations increase: 0/19 ? 1/19 ? ... ? 19/19
   - Status changes: pending ? confirming ? confirmed ? completed
   - Success message appears

2. **Already Confirmed**
   - If payment already has enough confirmations
   - Status immediately shows "confirmed"
   - No polling needed

3. **Expired Payment**
   - Wait past expiration time
   - Status shows "expired"
   - Polling stops

4. **Network Error**
   - Disconnect internet
   - Error message appears
   - Polling continues when reconnected

---

## Performance Considerations

- **Polling Interval**: 5 seconds is optimal for user experience without overloading the server
- **Auto-Stop**: Polling automatically stops when payment reaches terminal state
- **Background Handling**: Stop polling when app goes to background to save battery:

```typescript
import { AppState } from 'react-native';

// Add to your component
useEffect(() => {
  const subscription = AppState.addEventListener('change', nextAppState => {
    // Pause polling when app is in background
    if (nextAppState === 'active') {
      // Resume polling
      forceCheck();
    }
  });

  return () => subscription.remove();
}, [forceCheck]);
```

---

## Benefits of This Implementation

? **Real-time Updates**: 5-second polling provides near-instant feedback
? **Progress Tracking**: Users see confirmation count increasing
? **Auto-detection**: Works even if user refreshes the page
? **Battery Efficient**: Stops polling when payment is complete
? **User Control**: Manual refresh button for impatient users
? **Error Handling**: Clear error messages with retry capability

---

## API Endpoints Reference

### Check Payment Status (New)
```
POST /api/payments/crypto/{paymentId}/check-status
Authorization: Bearer {token}

Response:
{
  "id": "uuid",
  "status": "confirming",
  "confirmations": 5,
  "requiredConfirmations": 19,
  "transactionHash": "0x...",
  ...
}
```

### Verify Payment (Existing)
```
POST /api/payments/crypto/{paymentId}/verify
Authorization: Bearer {token}
Content-Type: application/json

Body:
{
  "transactionHash": "0x...",
  "userWalletAddress": "0x..." // optional
}
```

### Get Payment Status (Existing)
```
GET /api/payments/crypto/{paymentId}
Authorization: Bearer {token}

Response: Same as check-status
```

---

## Troubleshooting

### Payment stays "pending" forever
- Ensure user submitted transaction hash via `/verify` endpoint
- Check if transaction actually exists on blockchain
- Verify wallet address matches

### Confirmations not increasing
- Check blockchain explorer to verify transaction is being confirmed
- Ensure backend job is running (every 30 seconds)
- Check Hangfire dashboard at `/hangfire`

### Polling not working
- Verify API URL in environment config
- Check auth token is valid
- Ensure component is mounted and enabled prop is true

---

## Migration from Old Implementation

If you had polling the old endpoint (`GET /api/payments/crypto/{paymentId}`), simply:

1. Change from GET to POST
2. Update endpoint path to include `/check-status`
3. Everything else stays the same

Old:
```typescript
GET /api/payments/crypto/{paymentId}
```

New (better):
```typescript
POST /api/payments/crypto/{paymentId}/check-status
```

---

## Questions?

If you encounter any issues:
1. Check backend logs for errors
2. Check Hangfire dashboard at `https://your-api.com/hangfire`
3. Verify payment exists in database with correct status
4. Test blockchain verification manually using transaction hash

Backend monitoring runs every 30 seconds, but frontend polling every 5 seconds ensures users get immediate feedback.
