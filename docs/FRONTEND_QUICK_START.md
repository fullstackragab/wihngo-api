# ?? Quick Start Guide - Crypto Payments
**For Frontend Developers using Expo React Native**

## ? TL;DR

```bash
# 1. Install dependencies
npm install axios expo-clipboard react-native-qrcode-svg

# 2. Create payment
POST /api/payments/crypto/create
{
  "amountUsd": 9.99,
  "currency": "USDT",
  "network": "tron",  // or "ethereum" or "binance-smart-chain"
  "birdId": "uuid",
  "purpose": "premium_subscription",
  "plan": "monthly"
}

# 3. Show QR code with walletAddress
# 4. User sends crypto
# 5. User submits transaction hash

POST /api/payments/crypto/{paymentId}/verify
{
  "transactionHash": "0xabc...",
  "userWalletAddress": "optional"
}

# 6. Poll for status every 5 seconds
GET /api/payments/crypto/{paymentId}
```

## ?? Critical Information

### Networks & Decimals (IMPORTANT!)
```javascript
const NETWORK_CONFIG = {
  'tron': { 
    name: 'Tron (TRC-20)', 
    decimals: 6,  // ? 6 decimals
    confirmations: 19,
    avgTime: '~1 min'
  },
  'ethereum': { 
    name: 'Ethereum (ERC-20)', 
    decimals: 6,  // ? 6 decimals
    confirmations: 12,
    avgTime: '~2.4 min'
  },
  'binance-smart-chain': { 
    name: 'BSC (BEP-20)', 
    decimals: 18, // ?? 18 decimals (different!)
    confirmations: 15,
    avgTime: '~45 sec'
  }
};
```

### API Endpoints
```
Base URL: https://api.wihngo.com/api/payments/crypto

? Public (no auth):
  GET  /rates
  GET  /rates/{currency}
  GET  /wallet/{currency}/{network}

?? Authenticated (Bearer token):
  POST /create
  POST /{paymentId}/verify
  GET  /{paymentId}
  POST /{paymentId}/check-status
  GET  /history
  POST /{paymentId}/cancel
```

## ?? Minimal Implementation

### 1. API Service (10 lines)
```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'https://api.wihngo.com/api/payments/crypto',
  headers: { 'Authorization': `Bearer ${token}` }
});

export const createPayment = (data) => api.post('/create', data);
export const verifyPayment = (id, txHash) => 
  api.post(`/${id}/verify`, { transactionHash: txHash });
export const getStatus = (id) => api.get(`/${id}`);
```

### 2. Payment Screen (minimal)
```javascript
import React, { useState, useEffect } from 'react';
import { View, Text, TextInput, Button } from 'react-native';
import QRCode from 'react-native-qrcode-svg';
import * as Clipboard from 'expo-clipboard';

function PaymentScreen({ amount, birdId }) {
  const [payment, setPayment] = useState(null);
  const [txHash, setTxHash] = useState('');

  // 1. Create payment
  useEffect(() => {
    createPayment({
      amountUsd: amount,
      currency: 'USDT',
      network: 'tron',
      birdId,
      purpose: 'premium_subscription',
      plan: 'monthly'
    }).then(res => setPayment(res.data.paymentRequest));
  }, []);

  // 2. Submit tx hash
  const handleVerify = async () => {
    await verifyPayment(payment.id, txHash);
    // Start polling...
  };

  if (!payment) return <Text>Loading...</Text>;

  return (
    <View>
      {/* Amount */}
      <Text>Pay: {payment.amountCrypto} USDT</Text>
      
      {/* QR Code */}
      <QRCode value={payment.walletAddress} size={200} />
      
      {/* Address */}
      <Text>{payment.walletAddress}</Text>
      <Button 
        title="Copy" 
        onPress={() => Clipboard.setStringAsync(payment.walletAddress)} 
      />
      
      {/* TX Hash Input */}
      <TextInput 
        placeholder="Transaction Hash"
        value={txHash}
        onChangeText={setTxHash}
      />
      <Button title="Verify Payment" onPress={handleVerify} />
    </View>
  );
}
```

### 3. Status Polling
```javascript
useEffect(() => {
  if (!payment?.id) return;
  
  const interval = setInterval(async () => {
    const status = await getStatus(payment.id);
    
    if (status.data.status === 'completed') {
      alert('Payment successful!');
      clearInterval(interval);
    }
  }, 5000); // Every 5 seconds
  
  return () => clearInterval(interval);
}, [payment?.id]);
```

## ?? UI Components Needed

```
? Network Selector (Tron/Ethereum/BSC)
? Amount Display (crypto + USD)
? QR Code (200x200px)
? Address Display + Copy Button
? TX Hash Input Field
? Submit Button
? Status Badge (pending/confirming/completed)
? Confirmation Counter (X/Y)
? Timer (30 min countdown)
? Instructions/Help Text
```

## ?? Common Pitfalls

? **Wrong Decimal Places**
```javascript
// DON'T use hardcoded decimals
amount = wei / 1_000_000; // Wrong for BSC!

// DO use network-specific decimals
const decimals = network === 'binance-smart-chain' ? 18 : 6;
amount = wei / Math.pow(10, decimals);
```

? **Not Polling for Status**
```javascript
// DON'T just verify once
await verifyPayment(id, txHash);
// Payment might still be confirming!

// DO poll until completed
const interval = setInterval(() => checkStatus(), 5000);
```

? **Ignoring Expiration**
```javascript
// DON'T forget to check expiration
// Payments expire after 30 minutes

// DO show timer and handle expiration
if (new Date() > new Date(payment.expiresAt)) {
  alert('Payment expired. Please create a new one.');
}
```

## ?? Testing Checklist

```
? Create payment with Tron network
? Create payment with Ethereum network
? Create payment with BSC network
? Scan QR code with wallet
? Copy address manually
? Submit valid transaction hash
? Submit invalid transaction hash
? Watch confirmation progress
? Wait for payment completion
? Test payment expiration (30 min)
? Test network errors during polling
? Test app background/foreground
? Test with minimum amount ($5)
```

## ?? Example User Flow

```
User Journey (90 seconds):
?? 0s:  Select premium plan ? Choose payment method (Crypto)
?? 2s:  Select network (Tron/Eth/BSC) ? Create payment
?? 5s:  Show QR code + address
?? 10s: User opens wallet app
?? 20s: User scans QR or pastes address
?? 30s: User sends USDT
?? 35s: User copies transaction hash
?? 40s: User pastes hash ? Submit
?? 45s: Backend verifies transaction
?? 50s: Show "Confirming..." with progress
?? 90s: Payment completed ? Premium activated ?
```

## ?? Key Links

- **Full Documentation**: `docs/CRYPTO_PAYMENT_FRONTEND_INTEGRATION.md`
- **API Base URL**: `https://api.wihngo.com/api/payments/crypto`
- **Hangfire Dashboard**: `https://api.wihngo.com/hangfire`

## ?? Pro Tips

1. **Pre-fetch exchange rates** on app start for instant quotes
2. **Cache wallet addresses** per network to avoid repeated API calls
3. **Show network fees** to help users choose (Tron is cheapest)
4. **Add "What is transaction hash?"** help button
5. **Support deep linking** from wallet apps back to your app
6. **Show estimated confirmation time** per network
7. **Add loading skeleton** while creating payment
8. **Implement retry logic** for failed API calls
9. **Log all payment events** for debugging
10. **Test on real devices** with small amounts first

## ?? Support

**Backend Issues?** ? Check server logs or Hangfire  
**Frontend Issues?** ? Review full documentation  
**Integration Help?** ? Ping backend team

---

**Made with ?? for Wihngo**  
*Last Updated: January 2024*
