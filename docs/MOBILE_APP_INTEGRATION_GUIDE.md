# Mobile App Integration Guide - HD Wallet Implementation

## Overview

The backend now uses **Hierarchical Deterministic (HD) Wallets** (BIP44 standard) to generate unique payment addresses for each transaction. This is the industry-standard approach used by all major cryptocurrency exchanges and payment processors.

## What Changed?

### Before (Static Wallet)
```
User A pays 10 USDT ? Platform Wallet: TGRzh...fsgiA
User B pays 10 USDT ? Platform Wallet: TGRzh...fsgiA  ?? SAME ADDRESS!
```

**Problem:** Cannot distinguish which payment belongs to which user.

### After (HD Wallet)
```
User A pays 10 USDT ? HD Address #42: TXyz1...abc123
User B pays 10 USDT ? HD Address #43: TYzw2...def456  ? UNIQUE!
```

**Solution:** Each payment request gets a unique address derived from a master seed.

## API Changes

### Payment Request Response

The `/api/payments/crypto/create` endpoint now includes an `addressIndex` field:

```typescript
interface PaymentResponseDto {
  id: string;
  userId: string;
  birdId?: string;
  amountUsd: number;
  amountCrypto: number;
  currency: string;
  network: string;
  exchangeRate: number;
  walletAddress: string;        // ? Unique HD-derived address
  addressIndex?: number;        // ? NEW: Derivation path index
  userWalletAddress?: string;
  qrCodeData: string;
  paymentUri: string;
  transactionHash?: string;
  confirmations: number;
  requiredConfirmations: number;
  status: string;
  purpose: string;
  plan?: string;
  expiresAt: string;
  confirmedAt?: string;
  completedAt?: string;
  createdAt: string;
  updatedAt: string;
}
```

### Example Response

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "amountUsd": 9.99,
  "amountCrypto": 10.0,
  "currency": "USDT",
  "network": "tron",
  "exchangeRate": 0.999,
  "walletAddress": "TXyz1abc123def456...",
  "addressIndex": 42,
  "qrCodeData": "TXyz1abc123def456...",
  "paymentUri": "TXyz1abc123def456...",
  "confirmations": 0,
  "requiredConfirmations": 19,
  "status": "pending",
  "purpose": "premium_subscription",
  "plan": "monthly",
  "expiresAt": "2025-12-11T21:30:00Z",
  "createdAt": "2025-12-11T21:00:00Z",
  "updatedAt": "2025-12-11T21:00:00Z"
}
```

## Mobile App Implementation

### 1. Display Payment Information

**Update your payment screen to show the unique address:**

```typescript
// React Native / Expo example
const PaymentScreen = ({ payment }: { payment: PaymentResponseDto }) => {
  return (
    <View>
      <Text>Send exactly {payment.amountCrypto} {payment.currency}</Text>
      <Text>To this address:</Text>
      
      {/* Show unique payment address */}
      <Text selectable style={styles.address}>
        {payment.walletAddress}
      </Text>
      
      {/* Optional: Show HD wallet info for debugging */}
      {payment.addressIndex && (
        <Text style={styles.debug}>
          HD Index: {payment.addressIndex}
        </Text>
      )}
      
      {/* QR Code */}
      <QRCode value={payment.walletAddress} size={200} />
      
      {/* Copy button */}
      <Button 
        title="Copy Address" 
        onPress={() => {
          Clipboard.setString(payment.walletAddress);
          Alert.alert('Copied!', 'Payment address copied to clipboard');
        }}
      />
    </View>
  );
};
```

### 2. User Flow

#### Step 1: User Initiates Payment

```typescript
const createPayment = async (amountUsd: number, currency: string, network: string) => {
  const response = await fetch('https://your-api.com/api/payments/crypto/create', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      amountUsd,
      currency,
      network,
      purpose: 'premium_subscription',
      plan: 'monthly',
    }),
  });
  
  const payment: PaymentResponseDto = await response.json();
  
  // ? Each payment gets a UNIQUE address
  console.log('Payment address:', payment.walletAddress);
  console.log('HD Index:', payment.addressIndex);
  
  return payment;
};
```

#### Step 2: Display QR Code and Wait

```typescript
const PaymentWaitingScreen = ({ paymentId }: { paymentId: string }) => {
  const [payment, setPayment] = useState<PaymentResponseDto | null>(null);
  
  useEffect(() => {
    // Poll for payment status every 10 seconds
    const interval = setInterval(async () => {
      const response = await fetch(
        `https://your-api.com/api/payments/crypto/${paymentId}`,
        {
          headers: { 'Authorization': `Bearer ${token}` },
        }
      );
      
      const updatedPayment: PaymentResponseDto = await response.json();
      setPayment(updatedPayment);
      
      // Check if payment is complete
      if (updatedPayment.status === 'completed') {
        clearInterval(interval);
        navigation.navigate('PaymentSuccess');
      }
    }, 10000);
    
    return () => clearInterval(interval);
  }, [paymentId]);
  
  if (!payment) return <LoadingSpinner />;
  
  return (
    <View>
      <Text>Status: {payment.status}</Text>
      
      {payment.status === 'pending' && (
        <View>
          <Text>Send {payment.amountCrypto} {payment.currency}</Text>
          <Text>To: {payment.walletAddress}</Text>
          <QRCode value={payment.walletAddress} />
          <Text>Waiting for payment...</Text>
        </View>
      )}
      
      {payment.status === 'confirming' && (
        <View>
          <Text>Payment detected!</Text>
          <Text>
            Confirmations: {payment.confirmations}/{payment.requiredConfirmations}
          </Text>
          <ProgressBar 
            progress={payment.confirmations / payment.requiredConfirmations}
          />
        </View>
      )}
      
      {payment.status === 'confirmed' && (
        <View>
          <Text>Payment confirmed!</Text>
          <Text>Processing...</Text>
        </View>
      )}
    </View>
  );
};
```

#### Step 3: Optional - Submit Transaction Hash

Users can optionally submit their transaction hash for faster verification:

```typescript
const submitTransactionHash = async (paymentId: string, txHash: string) => {
  const response = await fetch(
    `https://your-api.com/api/payments/crypto/${paymentId}/verify`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        transactionHash: txHash,
        // Optional: user's wallet address
        userWalletAddress: 'TUser...',
      }),
    }
  );
  
  return await response.json();
};

// In your UI
const [txHash, setTxHash] = useState('');

<View>
  <Text>Enter your transaction hash (optional):</Text>
  <TextInput
    value={txHash}
    onChangeText={setTxHash}
    placeholder="0x123abc..."
  />
  <Button
    title="Verify Payment"
    onPress={() => submitTransactionHash(payment.id, txHash)}
  />
</View>
```

### 3. Status Flow

The payment goes through these statuses:

```
pending ? confirming ? confirmed ? completed
```

**Status Descriptions:**

| Status | Description | What to Show User |
|--------|-------------|-------------------|
| `pending` | Waiting for user to send crypto | QR code, payment address, "Send payment" |
| `confirming` | Transaction detected, waiting for confirmations | Progress bar, confirmations count |
| `confirmed` | Required confirmations reached | "Payment confirmed! Processing..." |
| `completed` | Payment processed, subscription activated | "Success! Premium activated" |
| `expired` | Payment window expired (30 min) | "Payment expired, create new one" |
| `cancelled` | User cancelled | "Payment cancelled" |

### 4. Error Handling

```typescript
const createPayment = async () => {
  try {
    const response = await fetch('https://your-api.com/api/payments/crypto/create', {
      // ...
    });
    
    if (!response.ok) {
      const error = await response.json();
      
      // Handle specific errors
      if (response.status === 400) {
        Alert.alert('Invalid Request', error.message);
      } else if (response.status === 401) {
        Alert.alert('Authentication Error', 'Please log in again');
      } else if (response.status === 500) {
        Alert.alert('Server Error', 'Please try again later');
      }
      
      return null;
    }
    
    return await response.json();
  } catch (error) {
    Alert.alert('Network Error', 'Check your internet connection');
    return null;
  }
};
```

## Important Notes

### 1. Each Payment = Unique Address

?? **IMPORTANT:** The `walletAddress` in the payment response is unique for each payment request. Do NOT hardcode wallet addresses in your app!

**? BAD:**
```typescript
// NEVER DO THIS!
const PAYMENT_ADDRESS = "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA";
```

**? GOOD:**
```typescript
// Always use the address from the API response
const payment = await createPayment();
const addressToUse = payment.walletAddress; // ? Unique per payment
```

### 2. Address Expiration

Each payment address expires after 30 minutes. Show a countdown timer to users:

```typescript
const PaymentTimer = ({ expiresAt }: { expiresAt: string }) => {
  const [timeLeft, setTimeLeft] = useState('');
  
  useEffect(() => {
    const interval = setInterval(() => {
      const expiry = new Date(expiresAt);
      const now = new Date();
      const diff = expiry.getTime() - now.getTime();
      
      if (diff <= 0) {
        setTimeLeft('EXPIRED');
        clearInterval(interval);
      } else {
        const minutes = Math.floor(diff / 60000);
        const seconds = Math.floor((diff % 60000) / 1000);
        setTimeLeft(`${minutes}:${seconds.toString().padStart(2, '0')}`);
      }
    }, 1000);
    
    return () => clearInterval(interval);
  }, [expiresAt]);
  
  return (
    <Text style={timeLeft === 'EXPIRED' ? styles.expired : styles.timer}>
      Time remaining: {timeLeft}
    </Text>
  );
};
```

### 3. Background Monitoring

The backend automatically monitors all pending payments every 30 seconds. You don't need to do anything special - just poll the payment status periodically.

**Backend automatically:**
- ? Scans HD-derived addresses for incoming transactions
- ? Verifies transaction amounts and confirmations
- ? Updates payment status automatically
- ? Completes payments when confirmations are reached

**Your app just needs to:**
- ? Create payment request
- ? Show QR code with the unique address
- ? Poll for status updates
- ? Show confirmation progress

### 4. Testing

For testing in development:

```typescript
// Development mode - use Sepolia testnet
const DEV_CONFIG = {
  apiUrl: 'http://localhost:5000',
  network: 'sepolia',
  currency: 'ETH',
};

// Production mode
const PROD_CONFIG = {
  apiUrl: 'https://api.wihngo.com',
  network: 'ethereum', // or 'tron'
  currency: 'USDT',
};

const config = __DEV__ ? DEV_CONFIG : PROD_CONFIG;
```

## TypeScript Types

Add these types to your mobile app:

```typescript
// types/payment.ts

export interface CreatePaymentRequestDto {
  amountUsd: number;
  currency: string;        // "USDT", "ETH", "BTC", etc.
  network: string;         // "ethereum", "tron", "sepolia", etc.
  purpose: string;         // "premium_subscription", "donation", etc.
  plan?: string;           // "monthly", "yearly", "lifetime"
  birdId?: string;         // Optional: for bird-specific payments
}

export interface PaymentResponseDto {
  id: string;
  userId: string;
  birdId?: string;
  amountUsd: number;
  amountCrypto: number;
  currency: string;
  network: string;
  exchangeRate: number;
  walletAddress: string;        // ? UNIQUE HD-derived address
  addressIndex?: number;        // ? NEW: HD wallet index
  userWalletAddress?: string;
  qrCodeData: string;
  paymentUri: string;
  transactionHash?: string;
  confirmations: number;
  requiredConfirmations: number;
  status: PaymentStatus;
  purpose: string;
  plan?: string;
  expiresAt: string;            // ISO 8601 datetime
  confirmedAt?: string;
  completedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export type PaymentStatus = 
  | 'pending' 
  | 'confirming' 
  | 'confirmed' 
  | 'completed' 
  | 'expired' 
  | 'cancelled';

export interface VerifyPaymentDto {
  transactionHash: string;
  userWalletAddress?: string;
}
```

## API Endpoints

### Create Payment Request

```
POST /api/payments/crypto/create
Authorization: Bearer {token}
Content-Type: application/json

{
  "amountUsd": 9.99,
  "currency": "USDT",
  "network": "tron",
  "purpose": "premium_subscription",
  "plan": "monthly"
}

Response: PaymentResponseDto
```

### Get Payment Status

```
GET /api/payments/crypto/{paymentId}
Authorization: Bearer {token}

Response: PaymentResponseDto
```

### Verify Payment (Optional)

```
POST /api/payments/crypto/{paymentId}/verify
Authorization: Bearer {token}
Content-Type: application/json

{
  "transactionHash": "0x123abc...",
  "userWalletAddress": "TUser..."
}

Response: PaymentResponseDto
```

### Get Payment History

```
GET /api/payments/crypto/history?page=1&pageSize=20
Authorization: Bearer {token}

Response: PaymentResponseDto[]
```

## Example: Complete Payment Flow

```typescript
import { useState, useEffect } from 'react';
import { View, Text, Button, Alert } from 'react-native';
import QRCode from 'react-native-qrcode-svg';

const PremiumUpgradeScreen = () => {
  const [payment, setPayment] = useState<PaymentResponseDto | null>(null);
  const [loading, setLoading] = useState(false);

  const handleUpgrade = async () => {
    setLoading(true);
    
    try {
      // Step 1: Create payment request
      const response = await fetch('https://api.wihngo.com/api/payments/crypto/create', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${userToken}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          amountUsd: 9.99,
          currency: 'USDT',
          network: 'tron',
          purpose: 'premium_subscription',
          plan: 'monthly',
        }),
      });
      
      if (!response.ok) {
        throw new Error('Failed to create payment');
      }
      
      const paymentData: PaymentResponseDto = await response.json();
      setPayment(paymentData);
      
      // Step 2: Start polling for status
      startPolling(paymentData.id);
      
    } catch (error) {
      Alert.alert('Error', 'Failed to create payment request');
    } finally {
      setLoading(false);
    }
  };

  const startPolling = (paymentId: string) => {
    const interval = setInterval(async () => {
      try {
        const response = await fetch(
          `https://api.wihngo.com/api/payments/crypto/${paymentId}`,
          {
            headers: { 'Authorization': `Bearer ${userToken}` },
          }
        );
        
        const updatedPayment: PaymentResponseDto = await response.json();
        setPayment(updatedPayment);
        
        // Stop polling when completed
        if (updatedPayment.status === 'completed') {
          clearInterval(interval);
          Alert.alert('Success!', 'Premium subscription activated!');
          navigation.navigate('PremiumActivated');
        }
        
        // Stop polling if expired
        if (updatedPayment.status === 'expired') {
          clearInterval(interval);
          Alert.alert('Expired', 'Payment window expired. Please create a new payment.');
        }
      } catch (error) {
        console.error('Failed to poll payment status:', error);
      }
    }, 10000); // Poll every 10 seconds
  };

  if (loading) {
    return <LoadingSpinner />;
  }

  if (!payment) {
    return (
      <View>
        <Text>Upgrade to Premium - $9.99/month</Text>
        <Button title="Pay with Crypto" onPress={handleUpgrade} />
      </View>
    );
  }

  return (
    <View>
      <Text>Send Payment</Text>
      
      {/* Payment Info */}
      <Text>Amount: {payment.amountCrypto} {payment.currency}</Text>
      <Text>Network: {payment.network}</Text>
      
      {/* UNIQUE Address */}
      <Text>To Address:</Text>
      <Text selectable>{payment.walletAddress}</Text>
      
      {/* QR Code */}
      <QRCode value={payment.walletAddress} size={200} />
      
      {/* Status */}
      <Text>Status: {payment.status}</Text>
      
      {payment.status === 'confirming' && (
        <Text>
          Confirmations: {payment.confirmations}/{payment.requiredConfirmations}
        </Text>
      )}
      
      {/* Timer */}
      <PaymentTimer expiresAt={payment.expiresAt} />
    </View>
  );
};
```

## FAQ

### Q: Do I need to change anything in the mobile app?

**A:** If you're already using the payment API correctly (using `walletAddress` from the response), you don't need to change anything! The `addressIndex` field is optional and only for informational purposes.

### Q: What if I was hardcoding the payment address?

**A:** You must update your app to use the `walletAddress` from the API response. Each payment now has a unique address.

### Q: How do I test this?

**A:** Use the Sepolia testnet for testing:
1. Set `network: "sepolia"` and `currency: "ETH"`
2. Get free test ETH from [Sepolia Faucet](https://sepoliafaucet.com/)
3. Send test payment to the address from API response
4. Backend will automatically detect and verify

### Q: What about older payments?

**A:** Older payments without `addressIndex` will continue to work. They use the static platform wallet addresses.

### Q: How long do I wait for confirmations?

**A:** Confirmation times vary by network:
- Tron: ~19 confirmations (?57 seconds)
- Ethereum: ~12 confirmations (?2.4 minutes)
- Sepolia: ~6 confirmations (?1.2 minutes)

The backend automatically handles this. Just poll for status updates.

## Support

If you encounter issues:

1. **Check the logs:** Backend logs show detailed HD wallet derivation info
2. **Verify network:** Make sure you're using the correct network (mainnet vs testnet)
3. **Check address:** Verify the address on a blockchain explorer
4. **Contact backend team:** Provide payment ID and transaction hash

## References

- [BIP44 Standard](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki)
- [TronGrid API](https://www.trongrid.io/)
- [Infura Ethereum API](https://infura.io/)
