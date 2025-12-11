# ?? MOBILE APP - HD WALLET INTEGRATION INSTRUCTIONS

## ?? CRITICAL: READ THIS FIRST

The backend now uses **HD Wallets** - each payment gets a **UNIQUE address**. 

### ?? BREAKING CHANGE IF:
- You hardcoded wallet addresses in your app ? **MUST UPDATE**
- You use `walletAddress` from API response ? **NO CHANGES NEEDED** ?

---

## ?? Quick Check: Do You Need to Update?

Run this check in your mobile app code:

```typescript
// ? BAD - If you have this, YOU MUST UPDATE
const PAYMENT_ADDRESS = "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA"; // HARDCODED!

// ? GOOD - If you do this, NO CHANGES NEEDED
const payment = await createPayment();
const address = payment.walletAddress; // From API response
```

**Search your codebase for:**
- `"TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA"` (Tron USDT)
- `"0x4cc28f4cea7b440858b903b5c46685cb1478cdc4"` (Ethereum USDT)
- `"0x83675000ac9915614afff618906421a2baea0020"` (BSC USDT)

If found ? **UPDATE REQUIRED** ??

---

## ?? REQUIRED CHANGES

### 1. Update TypeScript Types

Add `addressIndex` to your payment response type:

```typescript
// types/payment.ts or wherever you define types

export interface PaymentResponseDto {
  id: string;
  userId: string;
  birdId?: string;
  amountUsd: number;
  amountCrypto: number;
  currency: string;
  network: string;
  exchangeRate: number;
  walletAddress: string;        // ? Each payment gets UNIQUE address now
  addressIndex?: number;         // ? NEW: HD wallet derivation index
  userWalletAddress?: string;
  qrCodeData: string;
  paymentUri: string;
  transactionHash?: string;
  confirmations: number;
  requiredConfirmations: number;
  status: PaymentStatus;
  purpose: string;
  plan?: string;
  expiresAt: string;
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
```

### 2. Verify You Use Dynamic Addresses

**? Correct Implementation:**

```typescript
// Payment creation
const createPayment = async (amountUsd: number) => {
  const response = await fetch('https://api.wihngo.com/api/payments/crypto/create', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      amountUsd,
      currency: 'USDT',
      network: 'tron',
      purpose: 'premium_subscription',
      plan: 'monthly',
    }),
  });
  
  const payment: PaymentResponseDto = await response.json();
  
  // ? Use the UNIQUE address from API response
  return payment;
};

// Display payment screen
const PaymentScreen = ({ payment }: { payment: PaymentResponseDto }) => {
  return (
    <View>
      <Text>Send exactly {payment.amountCrypto} {payment.currency}</Text>
      
      {/* ? Show UNIQUE address from API */}
      <Text>To this address:</Text>
      <Text selectable style={styles.address}>
        {payment.walletAddress}
      </Text>
      
      {/* ? QR code with UNIQUE address */}
      <QRCode value={payment.walletAddress} size={200} />
      
      {/* Optional: Show HD wallet index for debugging */}
      {payment.addressIndex !== undefined && (
        <Text style={styles.debugInfo}>
          Payment Address #{payment.addressIndex}
        </Text>
      )}
      
      {/* Copy button */}
      <TouchableOpacity onPress={() => {
        Clipboard.setString(payment.walletAddress);
        Alert.alert('Copied!', 'Payment address copied to clipboard');
      }}>
        <Text>Copy Address</Text>
      </TouchableOpacity>
    </View>
  );
};
```

### 3. Remove Hardcoded Addresses (If Any)

**? REMOVE THIS:**

```typescript
// DON'T DO THIS!
const WALLET_ADDRESSES = {
  TRON_USDT: "TGRzhw2kwBW5PzncWfKCnqsvkrBezfsgiA",
  ETH_USDT: "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4",
  BSC_USDT: "0x83675000ac9915614afff618906421a2baea0020",
};

// DON'T DO THIS!
<QRCode value={WALLET_ADDRESSES.TRON_USDT} />
```

**? REPLACE WITH THIS:**

```typescript
// ALWAYS use address from API response
const payment = await createPayment();
<QRCode value={payment.walletAddress} />
```

---

## ?? COMPLETE PAYMENT FLOW EXAMPLE

Here's the complete, correct implementation:

```typescript
import React, { useState, useEffect } from 'react';
import { View, Text, Button, Alert, ActivityIndicator } from 'react-native';
import QRCode from 'react-native-qrcode-svg';
import * as Clipboard from 'expo-clipboard';

interface PaymentFlowProps {
  userId: string;
  token: string;
  amountUsd: number;
  onComplete: () => void;
}

export const PaymentFlow: React.FC<PaymentFlowProps> = ({
  userId,
  token,
  amountUsd,
  onComplete,
}) => {
  const [payment, setPayment] = useState<PaymentResponseDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [polling, setPolling] = useState<NodeJS.Timer | null>(null);

  // Step 1: Create payment request
  const createPaymentRequest = async () => {
    setLoading(true);
    
    try {
      const response = await fetch('https://api.wihngo.com/api/payments/crypto/create', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          amountUsd,
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
      
      // Start polling for payment status
      startPolling(paymentData.id);
      
    } catch (error) {
      Alert.alert('Error', 'Failed to create payment request');
      console.error(error);
    } finally {
      setLoading(false);
    }
  };

  // Step 2: Poll for payment status
  const startPolling = (paymentId: string) => {
    const interval = setInterval(async () => {
      try {
        const response = await fetch(
          `https://api.wihngo.com/api/payments/crypto/${paymentId}`,
          {
            headers: { 'Authorization': `Bearer ${token}` },
          }
        );

        const updatedPayment: PaymentResponseDto = await response.json();
        setPayment(updatedPayment);

        // Check status
        if (updatedPayment.status === 'completed') {
          clearInterval(interval);
          setPolling(null);
          Alert.alert('Success!', 'Premium subscription activated!');
          onComplete();
        } else if (updatedPayment.status === 'expired') {
          clearInterval(interval);
          setPolling(null);
          Alert.alert('Expired', 'Payment window expired. Please create a new payment.');
        }
      } catch (error) {
        console.error('Failed to poll payment status:', error);
      }
    }, 10000); // Poll every 10 seconds

    setPolling(interval);
  };

  // Cleanup polling on unmount
  useEffect(() => {
    return () => {
      if (polling) {
        clearInterval(polling);
      }
    };
  }, [polling]);

  // Copy address to clipboard
  const copyAddress = async () => {
    if (payment?.walletAddress) {
      await Clipboard.setStringAsync(payment.walletAddress);
      Alert.alert('Copied!', 'Payment address copied to clipboard');
    }
  };

  // Render loading state
  if (loading) {
    return (
      <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
        <ActivityIndicator size="large" />
        <Text>Creating payment request...</Text>
      </View>
    );
  }

  // Render initial state (before payment created)
  if (!payment) {
    return (
      <View style={{ padding: 20 }}>
        <Text style={{ fontSize: 24, marginBottom: 20 }}>
          Upgrade to Premium
        </Text>
        <Text style={{ fontSize: 18, marginBottom: 20 }}>
          ${amountUsd.toFixed(2)}/month
        </Text>
        <Button title="Pay with Crypto" onPress={createPaymentRequest} />
      </View>
    );
  }

  // Render payment waiting screen
  return (
    <View style={{ padding: 20 }}>
      <Text style={{ fontSize: 24, marginBottom: 20 }}>Send Payment</Text>

      {/* Status */}
      <Text style={{ fontSize: 16, marginBottom: 10 }}>
        Status: {payment.status.toUpperCase()}
      </Text>

      {/* Payment amount */}
      <Text style={{ fontSize: 18, marginBottom: 20 }}>
        Send exactly: {payment.amountCrypto} {payment.currency}
      </Text>

      {/* UNIQUE payment address */}
      <Text style={{ fontWeight: 'bold', marginBottom: 5 }}>
        To this address:
      </Text>
      <Text
        selectable
        style={{
          fontSize: 12,
          backgroundColor: '#f0f0f0',
          padding: 10,
          marginBottom: 10,
        }}
      >
        {payment.walletAddress}
      </Text>

      {/* Optional: Show HD index for debugging */}
      {payment.addressIndex !== undefined && __DEV__ && (
        <Text style={{ fontSize: 12, color: '#666', marginBottom: 10 }}>
          HD Index: {payment.addressIndex}
        </Text>
      )}

      {/* QR Code with UNIQUE address */}
      <View style={{ alignItems: 'center', marginVertical: 20 }}>
        <QRCode value={payment.walletAddress} size={200} />
      </View>

      {/* Copy button */}
      <Button title="Copy Address" onPress={copyAddress} />

      {/* Confirmation progress */}
      {payment.status === 'confirming' && (
        <View style={{ marginTop: 20 }}>
          <Text style={{ fontSize: 16, marginBottom: 10 }}>
            Transaction detected! Waiting for confirmations...
          </Text>
          <Text style={{ fontSize: 14 }}>
            Confirmations: {payment.confirmations}/{payment.requiredConfirmations}
          </Text>
          <View
            style={{
              height: 10,
              backgroundColor: '#e0e0e0',
              marginTop: 10,
              borderRadius: 5,
              overflow: 'hidden',
            }}
          >
            <View
              style={{
                height: '100%',
                backgroundColor: '#4CAF50',
                width: `${(payment.confirmations / payment.requiredConfirmations) * 100}%`,
              }}
            />
          </View>
        </View>
      )}

      {/* Timer */}
      <PaymentTimer expiresAt={payment.expiresAt} />
    </View>
  );
};

// Timer component
const PaymentTimer: React.FC<{ expiresAt: string }> = ({ expiresAt }) => {
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
    <Text
      style={{
        marginTop: 20,
        fontSize: 14,
        color: timeLeft === 'EXPIRED' ? 'red' : '#666',
      }}
    >
      Time remaining: {timeLeft}
    </Text>
  );
};
```

---

## ?? TESTING INSTRUCTIONS

### Test 1: Verify Unique Addresses

```typescript
// Create 3 payments and verify each gets a different address
const payment1 = await createPayment(10);
const payment2 = await createPayment(10);
const payment3 = await createPayment(10);

console.log('Payment 1:', payment1.walletAddress, 'Index:', payment1.addressIndex);
console.log('Payment 2:', payment2.walletAddress, 'Index:', payment2.addressIndex);
console.log('Payment 3:', payment3.walletAddress, 'Index:', payment3.addressIndex);

// ? All 3 should have DIFFERENT addresses
// ? Indexes should be sequential (0, 1, 2 or similar)
```

### Test 2: Verify QR Code Shows Correct Address

```typescript
// Create payment
const payment = await createPayment(10);

// QR code should contain the UNIQUE address
console.log('QR Code Value:', payment.walletAddress);
console.log('Should match wallet address:', payment.walletAddress === payment.qrCodeData);

// ? Should be true
```

### Test 3: Test with Sepolia (Testnet)

For safe testing without real money:

```typescript
const createTestPayment = async () => {
  const response = await fetch('https://api.wihngo.com/api/payments/crypto/create', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      amountUsd: 0.01,  // Small amount
      currency: 'ETH',
      network: 'sepolia',  // Testnet!
      purpose: 'test',
    }),
  });
  
  const payment = await response.json();
  console.log('Test payment address:', payment.walletAddress);
  
  // Get free test ETH from: https://sepoliafaucet.com/
  // Send to payment.walletAddress
};
```

---

## ? FAQ

### Q: Do I need to change anything if I'm already using `payment.walletAddress`?

**A:** No! If you already use the address from the API response, you're good to go. Just add the optional `addressIndex` field to your TypeScript types.

### Q: What if I was hardcoding the wallet address?

**A:** You MUST update your code to use `payment.walletAddress` from the API response. Each payment now gets a unique address.

### Q: What is `addressIndex` and do I need to use it?

**A:** It's the HD wallet derivation index (like a sequence number). You don't need to use it, but it's useful for:
- Debugging
- Customer support (helps identify specific payments)
- Optional display: "Payment #42"

### Q: How do I test without spending real crypto?

**A:** Use Sepolia testnet:
```typescript
{
  currency: 'ETH',
  network: 'sepolia',
  amountUsd: 0.01
}
```
Get free test ETH from https://sepoliafaucet.com/

### Q: What if the backend is still using static addresses?

**A:** The backend admin needs to:
1. Run the database migration
2. Configure the HD mnemonic
3. Restart the application

Check with backend team if migration was run.

### Q: Can multiple payments use the same address?

**A:** No! Each payment request creates a NEW unique address. This is the whole point of HD wallets.

---

## ?? CHECKLIST

Before deploying your mobile app update:

- [ ] Added `addressIndex?: number` to `PaymentResponseDto` type
- [ ] Removed any hardcoded wallet addresses
- [ ] Verified all QR codes use `payment.walletAddress` from API
- [ ] Verified all address displays use `payment.walletAddress` from API
- [ ] Tested creating multiple payments (each should have different address)
- [ ] Tested with Sepolia testnet
- [ ] Updated any payment-related documentation

---

## ?? SUPPORT

If you encounter issues:

1. **Verify backend migration ran:** Check with backend team
2. **Check API response:** Does it include `addressIndex`?
3. **Test with Sepolia:** Use testnet to avoid spending real money
4. **Check logs:** Backend logs show HD wallet derivation details
5. **Contact backend team:** Provide payment ID and screenshots

---

## ?? Additional Resources

- See `MOBILE_APP_INTEGRATION_GUIDE.md` for more detailed examples
- See `QUICK_REFERENCE.md` for implementation overview
- Backend API documentation: Check with your backend team

---

## ? SUMMARY

**What Changed:**
- Each payment now gets a UNIQUE HD-derived address
- API response includes `addressIndex` field

**What You Need to Do:**
1. Add `addressIndex?: number` to your TypeScript types
2. Verify you use `payment.walletAddress` from API (not hardcoded)
3. Test that each payment gets a different address

**What You Don't Need to Do:**
- If you already use dynamic addresses from API ? No code changes needed!
- `addressIndex` is optional, you don't have to display it

That's it! ??
