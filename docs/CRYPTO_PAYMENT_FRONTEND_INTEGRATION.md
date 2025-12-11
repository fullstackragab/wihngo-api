# Crypto Payment Frontend Integration Guide
## Expo React Native Mobile App

This guide provides complete instructions for integrating the crypto payment system into your Expo React Native mobile application.

---

## ?? Table of Contents

1. [Overview](#overview)
2. [Supported Cryptocurrencies](#supported-cryptocurrencies)
3. [API Endpoints](#api-endpoints)
4. [Payment Flow](#payment-flow)
5. [Implementation Steps](#implementation-steps)
6. [Code Examples](#code-examples)
7. [Error Handling](#error-handling)
8. [Testing](#testing)

---

## ?? Overview

The crypto payment system supports multiple cryptocurrencies and networks. Users can pay for premium subscriptions or other services using cryptocurrency wallets.

### Key Features
- ? Multiple network support (Tron, Ethereum, BSC)
- ? Real-time payment verification
- ? QR code payment integration
- ? Automatic confirmation tracking
- ? Payment expiration (30 minutes)
- ? Payment history

---

## ?? Supported Cryptocurrencies

| Currency | Network | Token Type | Decimals | Network Identifier |
|----------|---------|------------|----------|-------------------|
| **USDT** | Tron | TRC-20 | 6 | `tron` |
| **USDT** | Ethereum | ERC-20 | 6 | `ethereum` |
| **USDT** | BSC | BEP-20 | **18** | `binance-smart-chain` |
| **ETH** | Sepolia (Testnet) | Native | 18 | `sepolia` |
| USDC | Ethereum | ERC-20 | 6 | `ethereum` |
| USDC | BSC | BEP-20 | 18 | `binance-smart-chain` |

**?? IMPORTANT:** 
- BEP-20 USDT on Binance Smart Chain uses **18 decimals**, not 6!
- Sepolia is a **testnet** for development and testing only. Use mainnet networks for production.

---

## ?? API Endpoints

### Base URL
```
https://api.wihngo.com/api/payments/crypto
```

### Authentication
All endpoints (except rates) require JWT Bearer token:
```
Authorization: Bearer <your_jwt_token>
```

### Available Endpoints

#### 1. **Get Exchange Rates** (Public)
```http
GET /rates
```
Returns current exchange rates for all supported cryptocurrencies.

**Response:**
```json
[
  {
    "currency": "USDT",
    "usdRate": 0.9998,
    "lastUpdated": "2024-01-15T10:30:00Z",
    "source": "CoinGecko"
  }
]
```

#### 2. **Get Specific Rate** (Public)
```http
GET /rates/{currency}
```
Example: `GET /rates/USDT`

#### 3. **Get Platform Wallet** (Public)
```http
GET /wallet/{currency}/{network}
```
Example: `GET /wallet/USDT/tron`

**Response:**
```json
{
  "currency": "USDT",
  "network": "tron",
  "address": "TXYZa1b2c3d4e5f6g7h8i9j0...",
  "qrCode": "TXYZa1b2c3d4e5f6g7h8i9j0...",
  "isActive": true
}
```

#### 4. **Create Payment Request** (Authenticated)
```http
POST /create
```

**Request Body:**
```json
{
  "amountUsd": 9.99,
  "currency": "USDT",
  "network": "tron",
  "birdId": "123e4567-e89b-12d3-a456-426614174000",
  "purpose": "premium_subscription",
  "plan": "monthly"
}
```

**Response:**
```json
{
  "paymentRequest": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "userId": "user-guid",
    "birdId": "bird-guid",
    "amountUsd": 9.99,
    "amountCrypto": 10.02,
    "currency": "USDT",
    "network": "tron",
    "exchangeRate": 0.9978,
    "walletAddress": "TXYZa1b2c3d4e5f6g7h8i9j0...",
    "qrCodeData": "TXYZa1b2c3d4e5f6g7h8i9j0...",
    "paymentUri": "TXYZa1b2c3d4e5f6g7h8i9j0...",
    "requiredConfirmations": 19,
    "status": "pending",
    "purpose": "premium_subscription",
    "plan": "monthly",
    "expiresAt": "2024-01-15T11:00:00Z",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  },
  "message": "Payment request created successfully"
}
```

#### 5. **Verify Payment** (Authenticated)
```http
POST /{paymentId}/verify
```

**Request Body:**
```json
{
  "transactionHash": "0xabc123...",
  "userWalletAddress": "TXYZuser123..."
}
```

**Response:** Returns updated `PaymentResponseDto` with status `confirming` or `confirmed`

#### 6. **Get Payment Status** (Authenticated)
```http
GET /{paymentId}
```

Automatically checks blockchain and updates status in real-time.

**Response:** Returns `PaymentResponseDto`

#### 7. **Check Payment Status** (Authenticated)
```http
POST /{paymentId}/check-status
```

Force immediate blockchain verification (use for polling).

#### 8. **Get Payment History** (Authenticated)
```http
GET /history?page=1&pageSize=20
```

#### 9. **Cancel Payment** (Authenticated)
```http
POST /{paymentId}/cancel
```

---

## ?? Payment Flow

### Complete User Journey

```
1. User selects premium plan
   ?
2. App calls POST /create ? Get payment details
   ?
3. Display payment screen with:
   - Amount to pay (amountCrypto)
   - QR code (qrCodeData)
   - Wallet address (walletAddress)
   - Timer (30 min expiration)
   - Network selection
   ?
4. User opens crypto wallet and sends payment
   ?
5. User submits transaction hash
   ?
6. App calls POST /{paymentId}/verify ? Backend verifies
   ?
7. Poll GET /{paymentId} or POST /{paymentId}/check-status
   ?
8. Show confirmation status:
   - confirming: X/Y confirmations
   - confirmed: Waiting for completion
   - completed: Success! Premium activated
```

### Payment States

| Status | Description | User Action |
|--------|-------------|-------------|
| `pending` | Waiting for transaction | Send crypto & submit hash |
| `confirming` | Transaction found, waiting for confirmations | Wait |
| `confirmed` | Sufficient confirmations, processing | Wait |
| `completed` | Payment successful, premium activated | Done! |
| `expired` | Payment window (30 min) expired | Create new payment |
| `cancelled` | User cancelled | Create new payment |
| `failed` | Verification failed | Contact support |

---

## ??? Implementation Steps

### Step 1: Install Dependencies

```bash
npm install @react-native-community/clipboard react-native-qrcode-svg
# or
expo install expo-clipboard expo-qrcode
```

### Step 2: Create API Service

Create `services/cryptoPaymentApi.js`:

```javascript
import axios from 'axios';

const API_BASE_URL = 'https://api.wihngo.com/api/payments/crypto';

class CryptoPaymentAPI {
  constructor(authToken) {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Authorization': `Bearer ${authToken}`,
        'Content-Type': 'application/json'
      }
    });
  }

  // Get all exchange rates
  async getExchangeRates() {
    const response = await axios.get(`${API_BASE_URL}/rates`);
    return response.data;
  }

  // Create payment request
  async createPayment(paymentData) {
    const response = await this.client.post('/create', paymentData);
    return response.data;
  }

  // Verify payment with transaction hash
  async verifyPayment(paymentId, transactionHash, userWalletAddress) {
    const response = await this.client.post(`/${paymentId}/verify`, {
      transactionHash,
      userWalletAddress
    });
    return response.data;
  }

  // Get payment status (with auto-check)
  async getPaymentStatus(paymentId) {
    const response = await this.client.get(`/${paymentId}`);
    return response.data;
  }

  // Force check payment status
  async checkPaymentStatus(paymentId) {
    const response = await this.client.post(`/${paymentId}/check-status`);
    return response.data;
  }

  // Cancel payment
  async cancelPayment(paymentId) {
    const response = await this.client.post(`/${paymentId}/cancel`);
    return response.data;
  }

  // Get payment history
  async getPaymentHistory(page = 1, pageSize = 20) {
    const response = await this.client.get(`/history?page=${page}&pageSize=${pageSize}`);
    return response.data;
  }
}

export default CryptoPaymentAPI;
```

### Step 3: Create Payment Hook

Create `hooks/useCryptoPayment.js`:

```javascript
import { useState, useEffect, useCallback } from 'react';
import CryptoPaymentAPI from '../services/cryptoPaymentApi';

export const useCryptoPayment = (authToken) => {
  const [api] = useState(() => new CryptoPaymentAPI(authToken));
  const [payment, setPayment] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  // Create new payment
  const createPayment = useCallback(async (paymentData) => {
    setLoading(true);
    setError(null);
    try {
      const result = await api.createPayment(paymentData);
      setPayment(result.paymentRequest);
      return result.paymentRequest;
    } catch (err) {
      setError(err.response?.data?.error || 'Failed to create payment');
      throw err;
    } finally {
      setLoading(false);
    }
  }, [api]);

  // Verify payment
  const verifyPayment = useCallback(async (paymentId, txHash, walletAddress) => {
    setLoading(true);
    setError(null);
    try {
      const result = await api.verifyPayment(paymentId, txHash, walletAddress);
      setPayment(result);
      return result;
    } catch (err) {
      setError(err.response?.data?.error || 'Failed to verify payment');
      throw err;
    } finally {
      setLoading(false);
    }
  }, [api]);

  // Check status
  const checkStatus = useCallback(async (paymentId) => {
    try {
      const result = await api.checkPaymentStatus(paymentId);
      setPayment(result);
      return result;
    } catch (err) {
      console.error('Failed to check status:', err);
      return null;
    }
  }, [api]);

  return {
    payment,
    loading,
    error,
    createPayment,
    verifyPayment,
    checkStatus,
    api
  };
};
```

### Step 4: Create Payment Screen Component

Create `screens/CryptoPaymentScreen.js`:

```javascript
import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  StyleSheet,
  ScrollView
} from 'react-native';
import * as Clipboard from 'expo-clipboard';
import QRCode from 'react-native-qrcode-svg';
import { useCryptoPayment } from '../hooks/useCryptoPayment';

const CryptoPaymentScreen = ({ route, navigation }) => {
  const { amountUsd, birdId, plan } = route.params;
  const { payment, loading, createPayment, verifyPayment, checkStatus } = useCryptoPayment();
  
  const [selectedNetwork, setSelectedNetwork] = useState('tron');
  const [transactionHash, setTransactionHash] = useState('');
  const [timeRemaining, setTimeRemaining] = useState(null);
  const [isPolling, setIsPolling] = useState(false);
  
  const pollIntervalRef = useRef(null);

  // Network options
  const networks = [
    { id: 'tron', name: 'Tron (TRC-20)', fee: 'Low' },
    { id: 'ethereum', name: 'Ethereum (ERC-20)', fee: 'High' },
    { id: 'binance-smart-chain', name: 'BSC (BEP-20)', fee: 'Medium' },
    { id: 'sepolia', name: 'Sepolia Testnet (ETH)', fee: 'Low (Test)' }
  ];

  // Initialize payment
  useEffect(() => {
    const initPayment = async () => {
      try {
        // Determine currency based on network
        const currency = selectedNetwork === 'sepolia' ? 'ETH' : 'USDT';
        
        await createPayment({
          amountUsd,
          currency,
          network: selectedNetwork,
          birdId,
          purpose: 'premium_subscription',
          plan
        });
      } catch (err) {
        Alert.alert('Error', 'Failed to create payment request');
      }
    };
    initPayment();
  }, [selectedNetwork]);

  // Timer countdown
  useEffect(() => {
    if (!payment?.expiresAt) return;

    const updateTimer = () => {
      const now = new Date().getTime();
      const expiry = new Date(payment.expiresAt).getTime();
      const diff = expiry - now;

      if (diff <= 0) {
        setTimeRemaining('Expired');
        return;
      }

      const minutes = Math.floor(diff / 60000);
      const seconds = Math.floor((diff % 60000) / 1000);
      setTimeRemaining(`${minutes}:${seconds.toString().padStart(2, '0')}`);
    };

    updateTimer();
    const interval = setInterval(updateTimer, 1000);
    return () => clearInterval(interval);
  }, [payment?.expiresAt]);

  // Poll for payment status
  const startPolling = useCallback(() => {
    if (!payment?.id || isPolling) return;

    setIsPolling(true);
    pollIntervalRef.current = setInterval(async () => {
      const status = await checkStatus(payment.id);
      
      if (status?.status === 'completed') {
        stopPolling();
        Alert.alert(
          'Payment Successful!',
          'Your premium subscription has been activated.',
          [{ text: 'OK', onPress: () => navigation.goBack() }]
        );
      } else if (status?.status === 'failed') {
        stopPolling();
        Alert.alert('Payment Failed', 'Please contact support.');
      }
    }, 5000); // Poll every 5 seconds
  }, [payment?.id, isPolling, checkStatus]);

  const stopPolling = () => {
    if (pollIntervalRef.current) {
      clearInterval(pollIntervalRef.current);
      pollIntervalRef.current = null;
    }
    setIsPolling(false);
  };

  useEffect(() => {
    return () => stopPolling();
  }, []);

  // Handle payment submission
  const handleSubmitPayment = async () => {
    if (!transactionHash.trim()) {
      Alert.alert('Error', 'Please enter transaction hash');
      return;
    }

    try {
      await verifyPayment(payment.id, transactionHash.trim(), null);
      Alert.alert(
        'Transaction Submitted',
        'Waiting for blockchain confirmations...'
      );
      startPolling();
    } catch (err) {
      Alert.alert(
        'Verification Failed',
        err.response?.data?.error || 'Invalid transaction'
      );
    }
  };

  // Copy to clipboard
  const copyToClipboard = async (text) => {
    await Clipboard.setStringAsync(text);
    Alert.alert('Copied', 'Address copied to clipboard');
  };

  if (loading && !payment) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#007AFF" />
        <Text style={styles.loadingText}>Creating payment request...</Text>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container}>
      {/* Network Selection */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Select Network</Text>
        {networks.map((network) => (
          <TouchableOpacity
            key={network.id}
            style={[
              styles.networkButton,
              selectedNetwork === network.id && styles.networkButtonActive
            ]}
            onPress={() => setSelectedNetwork(network.id)}
            disabled={payment?.status !== 'pending'}
          >
            <Text style={styles.networkName}>{network.name}</Text>
            <Text style={styles.networkFee}>Fee: {network.fee}</Text>
          </TouchableOpacity>
        ))}
      </View>

      {payment && (
        <>
          {/* Payment Details */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Payment Details</Text>
            <View style={styles.detailRow}>
              <Text style={styles.detailLabel}>Amount to Pay:</Text>
              <Text style={styles.detailValue}>
                {payment.amountCrypto.toFixed(6)} {payment.currency}
              </Text>
            </View>
            <View style={styles.detailRow}>
              <Text style={styles.detailLabel}>USD Value:</Text>
              <Text style={styles.detailValue}>${payment.amountUsd}</Text>
            </View>
            <View style={styles.detailRow}>
              <Text style={styles.detailLabel}>Network:</Text>
              <Text style={styles.detailValue}>
                {networks.find(n => n.id === payment.network)?.name}
              </Text>
            </View>
            <View style={styles.detailRow}>
              <Text style={styles.detailLabel}>Time Remaining:</Text>
              <Text style={[styles.detailValue, styles.timer]}>
                {timeRemaining}
              </Text>
            </View>
          </View>

          {/* QR Code */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Scan to Pay</Text>
            <View style={styles.qrContainer}>
              <QRCode
                value={payment.walletAddress}
                size={200}
                backgroundColor="white"
              />
            </View>
          </View>

          {/* Wallet Address */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Or Copy Address</Text>
            <TouchableOpacity
              style={styles.addressContainer}
              onPress={() => copyToClipboard(payment.walletAddress)}
            >
              <Text style={styles.addressText} numberOfLines={1}>
                {payment.walletAddress}
              </Text>
              <Text style={styles.copyText}>?? Copy</Text>
            </TouchableOpacity>
          </View>

          {/* Transaction Hash Input */}
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Submit Transaction Hash</Text>
            <TextInput
              style={styles.input}
              placeholder="Enter transaction hash (0x...)"
              value={transactionHash}
              onChangeText={setTransactionHash}
              autoCapitalize="none"
              autoCorrect={false}
            />
            <TouchableOpacity
              style={[
                styles.submitButton,
                (!transactionHash.trim() || loading) && styles.submitButtonDisabled
              ]}
              onPress={handleSubmitPayment}
              disabled={!transactionHash.trim() || loading}
            >
              {loading ? (
                <ActivityIndicator color="white" />
              ) : (
                <Text style={styles.submitButtonText}>Verify Payment</Text>
              )}
            </TouchableOpacity>
          </View>

          {/* Status */}
          {payment.status !== 'pending' && (
            <View style={styles.section}>
              <Text style={styles.sectionTitle}>Payment Status</Text>
              <View style={styles.statusContainer}>
                <Text style={styles.statusText}>
                  {payment.status.toUpperCase()}
                </Text>
                {payment.confirmations !== undefined && (
                  <Text style={styles.confirmationsText}>
                    Confirmations: {payment.confirmations}/{payment.requiredConfirmations}
                  </Text>
                )}
              </View>
            </View>
          )}

          {/* Instructions */}
          <View style={styles.section}>
            <Text style={styles.instructionsTitle}>Instructions:</Text>
            <Text style={styles.instructionsText}>
              1. Open your crypto wallet (Trust Wallet, MetaMask, etc.)
              {'\n'}2. Scan QR code or copy address
              {'\n'}3. Send exactly {payment.amountCrypto.toFixed(6)} {payment.currency}
              {'\n'}4. Copy transaction hash from wallet
              {'\n'}5. Paste hash above and submit
              {'\n'}6. Wait for confirmations ({payment.requiredConfirmations} required)
            </Text>
          </View>
        </>
      )}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F5F5F5',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: '#666',
  },
  section: {
    backgroundColor: 'white',
    marginVertical: 8,
    marginHorizontal: 16,
    padding: 16,
    borderRadius: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    marginBottom: 12,
    color: '#333',
  },
  networkButton: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    padding: 16,
    borderWidth: 2,
    borderColor: '#E0E0E0',
    borderRadius: 8,
    marginBottom: 8,
  },
  networkButtonActive: {
    borderColor: '#007AFF',
    backgroundColor: '#E3F2FD',
  },
  networkName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
  },
  networkFee: {
    fontSize: 14,
    color: '#666',
  },
  detailRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderBottomColor: '#F0F0F0',
  },
  detailLabel: {
    fontSize: 15,
    color: '#666',
  },
  detailValue: {
    fontSize: 15,
    fontWeight: '600',
    color: '#333',
  },
  timer: {
    color: '#FF6B6B',
    fontWeight: 'bold',
  },
  qrContainer: {
    alignItems: 'center',
    paddingVertical: 16,
  },
  addressContainer: {
    flexDirection: 'row',
    backgroundColor: '#F8F8F8',
    padding: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  addressText: {
    flex: 1,
    fontSize: 14,
    color: '#333',
    fontFamily: 'monospace',
  },
  copyText: {
    fontSize: 14,
    color: '#007AFF',
    marginLeft: 8,
  },
  input: {
    borderWidth: 1,
    borderColor: '#E0E0E0',
    borderRadius: 8,
    padding: 12,
    fontSize: 14,
    fontFamily: 'monospace',
    marginBottom: 12,
  },
  submitButton: {
    backgroundColor: '#007AFF',
    padding: 16,
    borderRadius: 8,
    alignItems: 'center',
  },
  submitButtonDisabled: {
    backgroundColor: '#CCCCCC',
  },
  submitButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: 'bold',
  },
  statusContainer: {
    alignItems: 'center',
    paddingVertical: 16,
  },
  statusText: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#4CAF50',
    marginBottom: 8,
  },
  confirmationsText: {
    fontSize: 16,
    color: '#666',
  },
  instructionsTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    marginBottom: 8,
    color: '#333',
  },
  instructionsText: {
    fontSize: 14,
    lineHeight: 22,
    color: '#666',
  },
});

export default CryptoPaymentScreen;
```

---

## ?? Error Handling

### Common Errors and Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| `"Minimum payment amount is $5"` | Amount too low | Enforce minimum $5 |
| `"Exchange rate not available"` | Currency not supported | Check supported currencies |
| `"No wallet configured"` | Network not set up | Contact admin |
| `"Transaction not found"` | Invalid tx hash or not confirmed yet | Wait and retry |
| `"Incorrect amount"` | Amount mismatch | User sent wrong amount |
| `"Incorrect recipient address"` | Sent to wrong address | User error - refund needed |
| `"Payment already {status}"` | Duplicate verification | Check current status |
| `401 Unauthorized` | Invalid/expired token | Re-authenticate user |

### Error Handling Example

```javascript
try {
  const payment = await api.createPayment(data);
} catch (error) {
  if (error.response?.status === 400) {
    // Validation error
    Alert.alert('Invalid Request', error.response.data.error);
  } else if (error.response?.status === 401) {
    // Authentication error
    // Redirect to login
  } else if (error.response?.status === 404) {
    // Not found
    Alert.alert('Not Found', 'Payment request not found');
  } else {
    // Server error
    Alert.alert('Error', 'Something went wrong. Please try again.');
  }
}
```

---

## ?? Testing

### Test Scenarios

1. **Create Payment**
   - Test with different networks
   - Test with minimum amount ($5)
   - Test with invalid amount

2. **Network Switching**
   - Change network before payment
   - Verify correct wallet address
   - Verify correct decimal places

3. **Payment Verification**
   - Submit valid transaction hash
   - Submit invalid transaction hash
   - Submit duplicate hash

4. **Status Updates**
   - Monitor confirmation progress
   - Test payment completion
   - Test payment expiration (wait 30 min)

5. **Edge Cases**
   - Network errors during polling
   - App backgrounded during payment
   - Token expiration during payment

### Test Networks (Testnet)

For testing, you can request testnet support from backend team:
- Tron Shasta Testnet
- Ethereum Sepolia Testnet
- BSC Testnet

---

## ?? UI/UX Best Practices

### Payment Screen Requirements

1. **Clear Amount Display**
   - Show exact crypto amount (e.g., "10.023456 USDT")
   - Show USD equivalent
   - Highlight network fees

2. **Timer Display**
   - Show countdown (30 minutes)
   - Warn when < 5 minutes remaining
   - Auto-refresh on expiration

3. **QR Code**
   - Large, scannable QR code
   - Include wallet address in QR
   - Provide manual copy option

4. **Status Indicators**
   - Pending: Yellow/Orange
   - Confirming: Blue with progress
   - Completed: Green with checkmark
   - Failed/Expired: Red with error

5. **User Guidance**
   - Step-by-step instructions
   - Link to wallet tutorials
   - Support contact option

### Recommended Libraries

```json
{
  "dependencies": {
    "axios": "^1.6.0",
    "expo-clipboard": "~5.0.0",
    "react-native-qrcode-svg": "^6.2.0",
    "react-native-svg": "^14.0.0"
  }
}
```

---

## ?? Security Considerations

1. **Never Store Private Keys** - Only handle public addresses and tx hashes
2. **Validate Inputs** - Always validate transaction hashes format
3. **Use HTTPS** - All API calls must use HTTPS
4. **Token Security** - Store JWT securely (Secure Store)
5. **Amount Verification** - Always show exact amounts, no rounding

---

## ?? Support

For backend API issues or questions:
- Check logs in server console
- Review Hangfire dashboard at `/hangfire`
- Contact backend team

For frontend integration help:
- Review this documentation
- Check React Native docs
- Test with small amounts first

---

## ?? Notes

- **BEP-20 USDT uses 18 decimals** (different from TRC-20 and ERC-20!)
- **Sepolia is a testnet** - Only use for development and testing. Requires testnet ETH (no real value).
- Payment expiration is 30 minutes
- Confirmations required:
  - Tron: 19 blocks (~57 seconds)
  - Ethereum: 12 blocks (~2.4 minutes)
  - Sepolia: 6 blocks (~1.2 minutes) - **Testnet only**
  - BSC: 15 blocks (~45 seconds)
- Always show exact crypto amounts (don't round)
- Polling interval: 5 seconds recommended
- Maximum payment history: 20 items per page

---

## ?? Testing with Sepolia Testnet

### Setup for Development

1. **Get Sepolia Test ETH**
   - Visit [Sepolia Faucet](https://sepoliafaucet.com/)
   - Alternative faucets:
     - [Alchemy Sepolia Faucet](https://sepoliafaucet.com/)
     - [Infura Sepolia Faucet](https://www.infura.io/faucet/sepolia)
   - Request test ETH (may require social media account verification)
   - Test ETH has **no real monetary value** - safe for testing!

2. **Configure MetaMask for Sepolia**
   - Open MetaMask browser extension or mobile app
   - Click network dropdown at the top
   - Select "Show test networks" in settings if not visible
   - Choose "Sepolia" from the network list
   - Or manually add Sepolia network:
     - Network Name: Sepolia
     - RPC URL: https://sepolia.infura.io/v3/YOUR_INFURA_KEY
     - Chain ID: 11155111
     - Currency Symbol: ETH
     - Block Explorer: https://sepolia.etherscan.io

3. **Platform Receive Address (Testnet)**
   ```
   0x4cc28f4cea7b440858b903b5c46685cb1478cdc4
   ```
   This is the address where your test payments will be sent.

4. **Test Payment Flow Example**
   ```javascript
   // Create test payment for Sepolia
   const payment = await api.createPayment({
     amountUsd: 10.00,
     currency: 'ETH',        // Native Sepolia ETH
     network: 'sepolia',     // Testnet network
     birdId: 'your-bird-id',
     purpose: 'premium_subscription',
     plan: 'monthly'
   });

   // The response will include:
   // - walletAddress: "0x4cc28f4cea7b440858b903b5c46685cb1478cdc4"
   // - amountCrypto: Calculated ETH amount based on exchange rate
   // - requiredConfirmations: 6 (faster than mainnet)

   // Send test ETH from your MetaMask to the walletAddress
   // Then verify the transaction
   await api.verifyPayment(
     payment.id, 
     'your-transaction-hash-from-metamask',
     'your-wallet-address'
   );

   // Poll for status updates
   setInterval(async () => {
     const status = await api.checkPaymentStatus(payment.id);
     console.log(`Status: ${status.status}, Confirmations: ${status.confirmations}/6`);
   }, 5000);
   ```

5. **Important Testing Notes**
   - ? Sepolia ETH has **no real value** - safe for testing
   - ? Block times are ~12 seconds (similar to mainnet)
   - ? Requires only **6 confirmations** (~1.2 minutes vs 2.4 minutes on mainnet)
   - ? Use Sepolia for integration testing before mainnet deployment
   - ?? **Never use real money on testnet**
   - ?? **Always switch to mainnet networks for production**

6. **Verify Your Test Transaction**
   - Visit [Sepolia Etherscan](https://sepolia.etherscan.io)
   - Paste your transaction hash to view details
   - Check confirmations, amount, and recipient address

### Switching to Production

When ready for production:

1. **Update network in your payment creation:**
   ```javascript
   // Change from:
   network: 'sepolia'  // Testnet
   
   // To:
   network: 'ethereum'  // Mainnet (for real ETH)
   // or
   network: 'binance-smart-chain'  // BSC mainnet
   // or
   network: 'tron'  // Tron mainnet
   ```

2. **Update currency if using stablecoins:**
   ```javascript
   // For production stablecoins:
   currency: 'USDT'  // or 'USDC'
   ```

3. **Test thoroughly on testnet first!**
   - Verify all payment flows
   - Test error handling
   - Confirm notifications work
   - Check premium activation

---
