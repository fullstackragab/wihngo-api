# ?? Automatic Crypto Payment Detection - Frontend Integration Guide

## ?? Overview

This document explains the **automatic payment detection system** implemented in the Wihngo backend and how to integrate it with your **Expo React Native** mobile application.

---

## ??? Backend Architecture

### **Technology Stack:**
- **Framework:** ASP.NET Core (.NET 10)
- **Database:** PostgreSQL with EF Core
- **Background Jobs:** Hangfire
- **Blockchain APIs:** TronGrid (Tron), Infura (Ethereum/Sepolia)
- **Authentication:** JWT Bearer tokens

### **Repository:**
```
https://github.com/fullstackragab/wihngo-api
Branch: main
Directory: C:\.net\Wihngo\
```

---

## ?? Backend Changes Summary

### **What Changed:**

? **Automatic Wallet Scanning** - Backend now scans wallets for incoming transactions every 30 seconds  
? **No Manual Transaction Hash Required** - System detects payments automatically  
? **Real-time Payment Completion** - Payments auto-complete when confirmations are reached  
? **Comprehensive Logging** - Detailed console output for debugging  

### **Modified Files:**

#### 1. **BackgroundJobs/PaymentMonitorJob.cs**
- Added `ScanWalletForIncomingTransactionAsync()` method
- Implemented `ScanTronWalletAsync()` for TRC-20 USDT detection
- Enhanced logging with emojis for visibility
- Polls TronGrid API every 30 seconds

#### 2. **Services/BlockchainVerificationService.cs**
- Added verbose logging to `VerifyEvmTransactionAsync()`
- Enhanced ERC-20 token decoding
- Added diagnostic output for each verification step

#### 3. **Program.cs**
- Configured Hangfire with PostgreSQL storage
- Set up recurring job: `monitor-payments` (every 30 seconds)
- Enabled crypto-only logging filters

### **How It Works:**

```
1. User creates payment ? Backend generates wallet address
2. User sends crypto ? No action needed from user
3. Background job scans wallet ? Every 30 seconds via TronGrid API
4. Transaction detected ? Automatically linked to payment
5. Confirmations tracked ? Progress updated in real-time
6. Payment completes ? When required confirmations reached (19 for Tron)
```

---

## ?? API Endpoints

### **Base URL:**
```
https://horsier-maliah-semilyrical.ngrok-free.dev
```

### **1. Create Payment**

**Endpoint:**
```
POST /api/payments/crypto/create
```

**Headers:**
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "amountUsd": 10.00,
  "currency": "USDT",
  "network": "tron",
  "birdId": "uuid-here",
  "purpose": "premium_subscription",
  "plan": "monthly"
}
```

**Response (200 OK):**
```json
{
  "paymentRequest": {
    "id": "payment-uuid",
    "walletAddress": "TYour...Address",
    "amountCrypto": 10.5,
    "currency": "USDT",
    "network": "tron",
    "qrCodeData": "TYour...Address",
    "status": "pending",
    "expiresAt": "2024-01-15T10:30:00Z",
    "confirmations": 0,
    "requiredConfirmations": 19,
    "createdAt": "2024-01-15T10:00:00Z"
  },
  "message": "Payment request created successfully"
}
```

---

### **2. Check Payment Status** ? **PRIMARY ENDPOINT FOR POLLING**

**Endpoint:**
```
POST /api/payments/crypto/{paymentId}/check-status
```

**Headers:**
```
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "id": "payment-uuid",
  "userId": "user-uuid",
  "birdId": "bird-uuid",
  "status": "pending",
  "transactionHash": null,
  "confirmations": 0,
  "requiredConfirmations": 19,
  "walletAddress": "TYour...Address",
  "userWalletAddress": null,
  "amountCrypto": 10.5,
  "amountUsd": 10.00,
  "currency": "USDT",
  "network": "tron",
  "exchangeRate": 1.0,
  "qrCodeData": "TYour...Address",
  "paymentUri": "TYour...Address",
  "purpose": "premium_subscription",
  "plan": "monthly",
  "expiresAt": "2024-01-15T10:30:00Z",
  "confirmedAt": null,
  "completedAt": null,
  "createdAt": "2024-01-15T10:00:00Z",
  "updatedAt": "2024-01-15T10:00:00Z"
}
```

**Status Values:**
- `pending` - Waiting for user to send crypto
- `confirming` - Transaction detected, waiting for confirmations
- `confirmed` - Required confirmations reached, processing completion
- `completed` - Payment successful, premium activated
- `expired` - 30 minutes passed without payment
- `cancelled` - User cancelled payment

---

### **3. Get Payment Details (Alternative)**

**Endpoint:**
```
GET /api/payments/crypto/{paymentId}
```

**Headers:**
```
Authorization: Bearer {token}
```

**Response:** Same as check-status endpoint

---

### **4. Cancel Payment**

**Endpoint:**
```
POST /api/payments/crypto/{paymentId}/cancel
```

**Headers:**
```
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "id": "payment-uuid",
  "status": "cancelled",
  "message": "Payment cancelled successfully"
}
```

---

### **5. Get Exchange Rates**

**Endpoint:**
```
GET /api/payments/crypto/rates
```

**Headers:**
```
None (Anonymous endpoint)
```

**Response (200 OK):**
```json
[
  {
    "currency": "USDT",
    "usdRate": 1.0,
    "lastUpdated": "2024-01-15T10:00:00Z",
    "source": "CoinGecko"
  },
  {
    "currency": "TRX",
    "usdRate": 0.09,
    "lastUpdated": "2024-01-15T10:00:00Z",
    "source": "CoinGecko"
  }
]
```

---

### **6. Get Payment History**

**Endpoint:**
```
GET /api/payments/crypto/history?page=1&pageSize=20
```

**Headers:**
```
Authorization: Bearer {token}
```

**Query Parameters:**
- `page` (optional, default: 1) - Page number
- `pageSize` (optional, default: 20) - Items per page

**Response (200 OK):**
```json
{
  "payments": [
    {
      "id": "payment-uuid",
      "status": "completed",
      "amountCrypto": 10.5,
      "currency": "USDT",
      "createdAt": "2024-01-15T10:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 5
}
```

---

## ?? Payment Status Flow

```
pending ? confirming ? confirmed ? completed
   ?
expired (if 30 minutes passed)
   ?
cancelled (if user cancels)
```

### **Status Descriptions:**

| Status | Description | User Action | UI Display |
|--------|-------------|-------------|------------|
| `pending` | Waiting for user to send crypto | Show wallet address & QR code | Loading spinner + "Waiting for payment" |
| `confirming` | Transaction detected, waiting for confirmations | None (automatic) | Progress bar with confirmation count |
| `confirmed` | Required confirmations reached, processing | None (automatic) | "Almost done..." message |
| `completed` | Payment successful, premium activated | None (redirect) | Success screen with checkmark |
| `expired` | 30 minutes passed without payment | Create new payment | "Payment expired" message |
| `cancelled` | User cancelled payment | None | "Payment cancelled" message |

---

## ?? React Native Implementation

### **1. Install Dependencies**

```bash
npm install react-native-qrcode-svg
# or
yarn add react-native-qrcode-svg
```

### **2. Complete Payment Screen Component**

Create a new file: `screens/CryptoPaymentScreen.tsx`

```typescript
import React, { useState, useEffect } from 'react';
import { View, Text, StyleSheet, ActivityIndicator, TouchableOpacity, ScrollView } from 'react-native';
import QRCode from 'react-native-qrcode-svg';
import * as Clipboard from 'expo-clipboard';

interface PaymentScreenProps {
  paymentId: string;
  token: string;
  onComplete: () => void;
  onCancel: () => void;
}

const CryptoPaymentScreen: React.FC<PaymentScreenProps> = ({ 
  paymentId, 
  token, 
  onComplete, 
  onCancel 
}) => {
  const [payment, setPayment] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [timeLeft, setTimeLeft] = useState<number>(0);

  // Fetch initial payment details
  useEffect(() => {
    fetchPaymentStatus();
  }, []);

  // Start polling
  useEffect(() => {
    if (!payment) return;

    const shouldPoll = ['pending', 'confirming', 'confirmed'].includes(payment.status);
    
    if (!shouldPoll) {
      if (payment.status === 'completed') {
        onComplete();
      }
      return;
    }

    // Poll every 10 seconds for pending, 15 seconds for confirming
    const interval = payment.status === 'pending' ? 10000 : 15000;
    
    const pollInterval = setInterval(() => {
      fetchPaymentStatus();
    }, interval);

    return () => clearInterval(pollInterval);
  }, [payment?.status]);

  // Countdown timer
  useEffect(() => {
    if (!payment?.expiresAt) return;

    const updateTimer = () => {
      const now = new Date().getTime();
      const expiry = new Date(payment.expiresAt).getTime();
      const remaining = Math.max(0, expiry - now);
      setTimeLeft(remaining);
    };

    updateTimer();
    const timerInterval = setInterval(updateTimer, 1000);

    return () => clearInterval(timerInterval);
  }, [payment?.expiresAt]);

  const fetchPaymentStatus = async () => {
    try {
      const response = await fetch(
        `https://horsier-maliah-semilyrical.ngrok-free.dev/api/payments/crypto/${paymentId}/check-status`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          }
        }
      );

      if (!response.ok) {
        throw new Error('Failed to fetch payment status');
      }

      const data = await response.json();
      setPayment(data);
      setError(null);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const copyToClipboard = async (text: string) => {
    await Clipboard.setStringAsync(text);
    // Show toast notification
    alert('Copied to clipboard!');
  };

  const formatTime = (milliseconds: number) => {
    const minutes = Math.floor(milliseconds / 60000);
    const seconds = Math.floor((milliseconds % 60000) / 1000);
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };

  const handleCancel = async () => {
    try {
      const response = await fetch(
        `https://horsier-maliah-semilyrical.ngrok-free.dev/api/payments/crypto/${paymentId}/cancel`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          }
        }
      );

      if (response.ok) {
        onCancel();
      }
    } catch (err) {
      alert('Failed to cancel payment');
    }
  };

  if (loading && !payment) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" color="#0066FF" />
        <Text style={styles.loadingText}>Loading payment details...</Text>
      </View>
    );
  }

  if (error) {
    return (
      <View style={styles.centered}>
        <Text style={styles.errorText}>? {error}</Text>
        <TouchableOpacity style={styles.retryButton} onPress={fetchPaymentStatus}>
          <Text style={styles.retryButtonText}>Retry</Text>
        </TouchableOpacity>
      </View>
    );
  }

  // Pending - Waiting for payment
  if (payment.status === 'pending') {
    return (
      <ScrollView style={styles.container}>
        <Text style={styles.title}>?? Send Payment</Text>
        
        {/* Timer */}
        <View style={styles.timerContainer}>
          <Text style={styles.timerLabel}>?? Expires in:</Text>
          <Text style={styles.timerText}>{formatTime(timeLeft)}</Text>
        </View>

        {/* Amount */}
        <View style={styles.amountContainer}>
          <Text style={styles.amountLabel}>Amount to send:</Text>
          <Text style={styles.amountValue}>
            {payment.amountCrypto} {payment.currency}
          </Text>
          <Text style={styles.amountUsd}>
            ? ${payment.amountUsd?.toFixed(2)} USD
          </Text>
        </View>

        {/* QR Code */}
        <View style={styles.qrContainer}>
          <QRCode
            value={payment.walletAddress}
            size={200}
            backgroundColor="white"
          />
        </View>

        {/* Wallet Address */}
        <View style={styles.addressContainer}>
          <Text style={styles.addressLabel}>Wallet Address:</Text>
          <TouchableOpacity 
            style={styles.addressBox}
            onPress={() => copyToClipboard(payment.walletAddress)}
          >
            <Text style={styles.addressText}>{payment.walletAddress}</Text>
            <Text style={styles.copyHint}>Tap to copy</Text>
          </TouchableOpacity>
        </View>

        {/* Instructions */}
        <View style={styles.instructionsContainer}>
          <Text style={styles.instructionsTitle}>?? Instructions:</Text>
          <Text style={styles.instruction}>1. Open your {payment.currency} wallet</Text>
          <Text style={styles.instruction}>2. Send exactly {payment.amountCrypto} {payment.currency}</Text>
          <Text style={styles.instruction}>3. To the address above</Text>
          <Text style={styles.instruction}>4. Wait for automatic confirmation ?</Text>
        </View>

        {/* Auto-detection notice */}
        <View style={styles.autoDetectBox}>
          <ActivityIndicator size="small" color="#0066FF" />
          <Text style={styles.autoDetectText}>
            ?? Scanning blockchain for your transaction...
          </Text>
          <Text style={styles.autoDetectSubtext}>
            No need to submit transaction hash
          </Text>
        </View>

        {/* Cancel Button */}
        <TouchableOpacity 
          style={styles.cancelButton}
          onPress={handleCancel}
        >
          <Text style={styles.cancelButtonText}>Cancel Payment</Text>
        </TouchableOpacity>
      </ScrollView>
    );
  }

  // Confirming - Transaction detected
  if (payment.status === 'confirming') {
    const progress = (payment.confirmations / payment.requiredConfirmations) * 100;
    
    return (
      <View style={styles.container}>
        <Text style={styles.title}>? Payment Detected!</Text>
        
        {/* Transaction Hash */}
        <View style={styles.txHashContainer}>
          <Text style={styles.txHashLabel}>Transaction:</Text>
          <TouchableOpacity onPress={() => copyToClipboard(payment.transactionHash)}>
            <Text style={styles.txHashText}>
              {payment.transactionHash?.substring(0, 10)}...
              {payment.transactionHash?.substring(payment.transactionHash.length - 10)}
            </Text>
          </TouchableOpacity>
        </View>

        {/* Progress */}
        <View style={styles.progressContainer}>
          <Text style={styles.progressLabel}>Confirmations:</Text>
          <Text style={styles.progressValue}>
            {payment.confirmations} / {payment.requiredConfirmations}
          </Text>
          
          {/* Progress Bar */}
          <View style={styles.progressBarBackground}>
            <View style={[styles.progressBarFill, { width: `${progress}%` }]} />
          </View>
        </View>

        {/* Estimated Time */}
        <View style={styles.estimateContainer}>
          <Text style={styles.estimateText}>
            ? Estimated time: ~{(payment.requiredConfirmations - payment.confirmations) * 3} minutes
          </Text>
        </View>

        {/* Auto-update notice */}
        <View style={styles.autoDetectBox}>
          <ActivityIndicator size="small" color="#0066FF" />
          <Text style={styles.autoDetectText}>
            Waiting for network confirmations...
          </Text>
        </View>
      </View>
    );
  }

  // Completed - Success!
  if (payment.status === 'completed') {
    return (
      <View style={styles.container}>
        <Text style={styles.successIcon}>??</Text>
        <Text style={styles.successTitle}>Payment Completed!</Text>
        
        <View style={styles.successDetails}>
          <Text style={styles.successLabel}>Amount:</Text>
          <Text style={styles.successValue}>
            {payment.amountCrypto} {payment.currency}
          </Text>
          
          <Text style={styles.successLabel}>Transaction:</Text>
          <TouchableOpacity onPress={() => copyToClipboard(payment.transactionHash)}>
            <Text style={styles.txHashText}>
              {payment.transactionHash?.substring(0, 10)}...
            </Text>
          </TouchableOpacity>
        </View>

        <TouchableOpacity style={styles.doneButton} onPress={onComplete}>
          <Text style={styles.doneButtonText}>Done</Text>
        </TouchableOpacity>
      </View>
    );
  }

  // Expired
  if (payment.status === 'expired') {
    return (
      <View style={styles.container}>
        <Text style={styles.errorIcon}>?</Text>
        <Text style={styles.errorTitle}>Payment Expired</Text>
        <Text style={styles.errorMessage}>
          This payment request has expired. Please create a new payment.
        </Text>
        <TouchableOpacity style={styles.retryButton} onPress={onCancel}>
          <Text style={styles.retryButtonText}>Create New Payment</Text>
        </TouchableOpacity>
      </View>
    );
  }

  // Cancelled
  if (payment.status === 'cancelled') {
    return (
      <View style={styles.container}>
        <Text style={styles.errorIcon}>?</Text>
        <Text style={styles.errorTitle}>Payment Cancelled</Text>
        <Text style={styles.errorMessage}>
          This payment has been cancelled.
        </Text>
        <TouchableOpacity style={styles.retryButton} onPress={onCancel}>
          <Text style={styles.retryButtonText}>Go Back</Text>
        </TouchableOpacity>
      </View>
    );
  }

  return null;
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
    backgroundColor: '#F5F5F5',
  },
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    textAlign: 'center',
    marginBottom: 20,
  },
  timerContainer: {
    backgroundColor: '#FFF3CD',
    padding: 12,
    borderRadius: 8,
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 20,
  },
  timerLabel: {
    fontSize: 16,
    marginRight: 8,
  },
  timerText: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#856404',
  },
  amountContainer: {
    backgroundColor: 'white',
    padding: 20,
    borderRadius: 12,
    marginBottom: 20,
    alignItems: 'center',
  },
  amountLabel: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
  },
  amountValue: {
    fontSize: 32,
    fontWeight: 'bold',
    color: '#0066FF',
  },
  amountUsd: {
    fontSize: 16,
    color: '#999',
    marginTop: 4,
  },
  qrContainer: {
    backgroundColor: 'white',
    padding: 20,
    borderRadius: 12,
    alignItems: 'center',
    marginBottom: 20,
  },
  addressContainer: {
    marginBottom: 20,
  },
  addressLabel: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
  },
  addressBox: {
    backgroundColor: 'white',
    padding: 16,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#DDD',
  },
  addressText: {
    fontSize: 12,
    fontFamily: 'monospace',
    color: '#333',
  },
  copyHint: {
    fontSize: 12,
    color: '#0066FF',
    marginTop: 8,
    textAlign: 'center',
  },
  instructionsContainer: {
    backgroundColor: 'white',
    padding: 16,
    borderRadius: 12,
    marginBottom: 20,
  },
  instructionsTitle: {
    fontSize: 16,
    fontWeight: 'bold',
    marginBottom: 12,
  },
  instruction: {
    fontSize: 14,
    color: '#333',
    marginBottom: 6,
  },
  autoDetectBox: {
    backgroundColor: '#E3F2FD',
    padding: 16,
    borderRadius: 8,
    flexDirection: 'column',
    alignItems: 'center',
    marginBottom: 20,
  },
  autoDetectText: {
    fontSize: 14,
    color: '#0066FF',
    marginTop: 8,
    textAlign: 'center',
  },
  autoDetectSubtext: {
    fontSize: 12,
    color: '#666',
    marginTop: 4,
  },
  cancelButton: {
    backgroundColor: '#FF4444',
    padding: 16,
    borderRadius: 8,
    alignItems: 'center',
    marginBottom: 20,
  },
  cancelButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: 'bold',
  },
  txHashContainer: {
    backgroundColor: 'white',
    padding: 16,
    borderRadius: 12,
    marginBottom: 20,
  },
  txHashLabel: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
  },
  txHashText: {
    fontSize: 14,
    fontFamily: 'monospace',
    color: '#0066FF',
  },
  progressContainer: {
    backgroundColor: 'white',
    padding: 20,
    borderRadius: 12,
    marginBottom: 20,
  },
  progressLabel: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
  },
  progressValue: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#0066FF',
    marginBottom: 12,
  },
  progressBarBackground: {
    height: 8,
    backgroundColor: '#E0E0E0',
    borderRadius: 4,
    overflow: 'hidden',
  },
  progressBarFill: {
    height: '100%',
    backgroundColor: '#0066FF',
  },
  estimateContainer: {
    padding: 12,
    backgroundColor: '#F0F0F0',
    borderRadius: 8,
    marginBottom: 20,
  },
  estimateText: {
    fontSize: 14,
    color: '#666',
    textAlign: 'center',
  },
  successIcon: {
    fontSize: 80,
    textAlign: 'center',
    marginBottom: 20,
  },
  successTitle: {
    fontSize: 28,
    fontWeight: 'bold',
    textAlign: 'center',
    color: '#4CAF50',
    marginBottom: 30,
  },
  successDetails: {
    backgroundColor: 'white',
    padding: 20,
    borderRadius: 12,
    marginBottom: 30,
  },
  successLabel: {
    fontSize: 14,
    color: '#666',
    marginTop: 12,
    marginBottom: 4,
  },
  successValue: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#333',
  },
  doneButton: {
    backgroundColor: '#4CAF50',
    padding: 16,
    borderRadius: 8,
    alignItems: 'center',
  },
  doneButtonText: {
    color: 'white',
    fontSize: 18,
    fontWeight: 'bold',
  },
  errorIcon: {
    fontSize: 80,
    textAlign: 'center',
    marginBottom: 20,
  },
  errorTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    textAlign: 'center',
    color: '#FF4444',
    marginBottom: 20,
  },
  errorMessage: {
    fontSize: 16,
    color: '#666',
    textAlign: 'center',
    marginBottom: 30,
    paddingHorizontal: 20,
  },
  errorText: {
    fontSize: 16,
    color: '#FF4444',
    marginBottom: 20,
  },
  retryButton: {
    backgroundColor: '#0066FF',
    padding: 16,
    borderRadius: 8,
    alignItems: 'center',
    marginHorizontal: 20,
  },
  retryButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: 'bold',
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: '#666',
  },
});

export default CryptoPaymentScreen;
```

---

### **3. Create Payment Service**

Create a new file: `services/cryptoPaymentService.ts`

```typescript
const API_BASE_URL = 'https://horsier-maliah-semilyrical.ngrok-free.dev';

export interface CreatePaymentRequest {
  amountUsd: number;
  currency: string;
  network: string;
  birdId?: string;
  purpose: string;
  plan?: string;
}

export interface PaymentResponse {
  id: string;
  walletAddress: string;
  amountCrypto: number;
  amountUsd: number;
  currency: string;
  network: string;
  status: string;
  transactionHash?: string;
  confirmations: number;
  requiredConfirmations: number;
  expiresAt: string;
  completedAt?: string;
  qrCodeData: string;
}

export class CryptoPaymentService {
  private token: string;

  constructor(token: string) {
    this.token = token;
  }

  async createPayment(request: CreatePaymentRequest): Promise<PaymentResponse> {
    const response = await fetch(`${API_BASE_URL}/api/payments/crypto/create`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Failed to create payment');
    }

    const data = await response.json();
    return data.paymentRequest;
  }

  async checkPaymentStatus(paymentId: string): Promise<PaymentResponse> {
    const response = await fetch(
      `${API_BASE_URL}/api/payments/crypto/${paymentId}/check-status`,
      {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`,
          'Content-Type': 'application/json'
        }
      }
    );

    if (!response.ok) {
      throw new Error('Failed to check payment status');
    }

    return await response.json();
  }

  async getPayment(paymentId: string): Promise<PaymentResponse> {
    const response = await fetch(
      `${API_BASE_URL}/api/payments/crypto/${paymentId}`,
      {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${this.token}`
        }
      }
    );

    if (!response.ok) {
      throw new Error('Failed to get payment');
    }

    return await response.json();
  }

  async cancelPayment(paymentId: string): Promise<void> {
    const response = await fetch(
      `${API_BASE_URL}/api/payments/crypto/${paymentId}/cancel`,
      {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.token}`,
          'Content-Type': 'application/json'
        }
      }
    );

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Failed to cancel payment');
    }
  }

  async getExchangeRates(): Promise<Array<{ currency: string; usdRate: number }>> {
    const response = await fetch(`${API_BASE_URL}/api/payments/crypto/rates`);

    if (!response.ok) {
      throw new Error('Failed to get exchange rates');
    }

    return await response.json();
  }
}
```

---

### **4. Usage Example**

Create a new file: `screens/PremiumUpgradeScreen.tsx`

```typescript
import React, { useState } from 'react';
import { View, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { CryptoPaymentService } from '../services/cryptoPaymentService';
import CryptoPaymentScreen from './CryptoPaymentScreen';

interface PremiumUpgradeScreenProps {
  token: string;
  birdId: string;
}

const PremiumUpgradeScreen: React.FC<PremiumUpgradeScreenProps> = ({ token, birdId }) => {
  const [paymentId, setPaymentId] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const createPayment = async () => {
    setLoading(true);
    try {
      const service = new CryptoPaymentService(token);
      const payment = await service.createPayment({
        amountUsd: 9.99,
        currency: 'USDT',
        network: 'tron',
        birdId: birdId,
        purpose: 'premium_subscription',
        plan: 'monthly'
      });

      setPaymentId(payment.id);
    } catch (error: any) {
      alert(error.message);
    } finally {
      setLoading(false);
    }
  };

  const handlePaymentComplete = () => {
    // Navigate to success screen or refresh premium status
    alert('Premium activated!');
    setPaymentId(null);
  };

  const handleCancel = () => {
    setPaymentId(null);
  };

  if (paymentId) {
    return (
      <CryptoPaymentScreen
        paymentId={paymentId}
        token={token}
        onComplete={handlePaymentComplete}
        onCancel={handleCancel}
      />
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Upgrade to Premium</Text>
      <Text style={styles.description}>
        Get unlimited access to all premium features!
      </Text>

      <View style={styles.priceCard}>
        <Text style={styles.priceAmount}>$9.99 / month</Text>
        <Text style={styles.priceDescription}>Billed monthly in USDT</Text>
      </View>

      <TouchableOpacity 
        style={styles.upgradeButton}
        onPress={createPayment}
        disabled={loading}
      >
        <Text style={styles.upgradeButtonText}>
          {loading ? 'Creating Payment...' : 'Pay with Crypto'}
        </Text>
      </TouchableOpacity>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
    backgroundColor: '#F5F5F5',
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    marginBottom: 12,
  },
  description: {
    fontSize: 16,
    color: '#666',
    marginBottom: 30,
  },
  priceCard: {
    backgroundColor: 'white',
    padding: 24,
    borderRadius: 12,
    marginBottom: 30,
    alignItems: 'center',
  },
  priceAmount: {
    fontSize: 36,
    fontWeight: 'bold',
    color: '#0066FF',
  },
  priceDescription: {
    fontSize: 14,
    color: '#666',
    marginTop: 8,
  },
  upgradeButton: {
    backgroundColor: '#0066FF',
    padding: 16,
    borderRadius: 8,
    alignItems: 'center',
  },
  upgradeButtonText: {
    color: 'white',
    fontSize: 18,
    fontWeight: 'bold',
  },
});

export default PremiumUpgradeScreen;
```

---

## ?? Polling Strategy

### **Recommended Polling Intervals:**

```typescript
const POLLING_INTERVALS = {
  pending: 10000,      // 10 seconds - waiting for transaction
  confirming: 15000,   // 15 seconds - waiting for confirmations
  confirmed: 5000,     // 5 seconds - final check
  completed: 0,        // Stop polling
  expired: 0,          // Stop polling
  cancelled: 0         // Stop polling
};

function getPollingInterval(status: string): number {
  return POLLING_INTERVALS[status as keyof typeof POLLING_INTERVALS] || 0;
}
```

### **Why These Intervals?**

- **Pending (10s):** Backend scans wallets every 30 seconds, so checking every 10 seconds ensures we catch the transaction quickly
- **Confirming (15s):** Confirmations happen every ~3 minutes on Tron, so 15 seconds is frequent enough
- **Confirmed (5s):** Final state before completion, check frequently
- **Completed/Expired/Cancelled:** No need to poll, payment is in final state

---

## ?? Key Features Implemented

### ? **Automatic Detection**
- No transaction hash input needed
- Backend scans wallet every 30 seconds
- Frontend polls status every 10-15 seconds
- Average detection time: 10-60 seconds after sending crypto

### ? **Real-time Updates**
- Live confirmation counter
- Progress bar visualization
- Countdown timer for expiration
- Automatic status transitions

### ? **User Experience**
- QR code for easy scanning
- One-tap address copying
- Clear status indicators
- Auto-redirect on completion
- Persistent payment tracking

### ? **Error Handling**
- Expired payment handling
- Network error recovery
- Retry mechanisms
- User-friendly error messages

---

## ?? Security Best Practices

### **1. Token Storage**

Use `expo-secure-store` to store JWT tokens securely:

```typescript
import * as SecureStore from 'expo-secure-store';

// Save token
await SecureStore.setItemAsync('userToken', token);

// Retrieve token
const token = await SecureStore.getItemAsync('userToken');

// Delete token
await SecureStore.deleteItemAsync('userToken');
```

### **2. API Security**

```typescript
// Always use HTTPS in production
const API_BASE_URL = __DEV__ 
  ? 'https://horsier-maliah-semilyrical.ngrok-free.dev'
  : 'https://api.wihngo.com';

// Add timeout to requests
const fetchWithTimeout = async (url: string, options: any, timeout = 30000) => {
  const controller = new AbortController();
  const id = setTimeout(() => controller.abort(), timeout);

  try {
    const response = await fetch(url, {
      ...options,
      signal: controller.signal
    });
    clearTimeout(id);
    return response;
  } catch (error) {
    clearTimeout(id);
    throw error;
  }
};
```

### **3. Validate Payment Amounts**

```typescript
// Always validate amounts before displaying
const validateAmount = (amount: number): boolean => {
  return amount > 0 && amount < 1000000 && !isNaN(amount);
};

// Format currency safely
const formatCurrency = (amount: number, decimals: number = 2): string => {
  return Number(amount).toFixed(decimals);
};
```

---

## ?? Testing Checklist

### **Frontend Testing:**

- [ ] Create payment with valid amount
- [ ] Display QR code correctly
- [ ] Copy wallet address to clipboard
- [ ] Timer counts down correctly
- [ ] Polling starts automatically
- [ ] Status updates display correctly
- [ ] Progress bar animates smoothly
- [ ] Completion redirects properly
- [ ] Cancel payment works
- [ ] Expired payment handling
- [ ] Network error recovery
- [ ] App backgrounding/foregrounding
- [ ] Token expiration handling

### **Integration Testing:**

- [ ] Send test payment (testnet USDT if available)
- [ ] Verify automatic detection (10-60 seconds)
- [ ] Check confirmation progress updates
- [ ] Confirm payment completes successfully
- [ ] Test with poor network conditions
- [ ] Test with airplane mode
- [ ] Test app restart during payment
- [ ] Test multiple simultaneous payments

### **Backend Verification:**

Check console logs for:
- ?? Wallet scanning attempts
- ?? Transactions found
- ? Payment detection
- ?? Status changes
- ?? Payment completion

---

## ?? Deployment Checklist

### **Environment Configuration:**

```typescript
// config/env.ts
export const ENV = {
  development: {
    apiUrl: 'https://horsier-maliah-semilyrical.ngrok-free.dev',
    pollingInterval: 10000
  },
  production: {
    apiUrl: 'https://api.wihngo.com',
    pollingInterval: 15000
  }
};

export const getConfig = () => {
  return __DEV__ ? ENV.development : ENV.production;
};
```

### **Backend Requirements:**

- ? TronGrid API key configured in `appsettings.json`
- ? Hangfire background jobs running
- ? PostgreSQL database operational
- ? CORS enabled for your mobile app domain
- ? JWT authentication configured
- ? SSL/HTTPS enabled

### **Mobile App Requirements:**

- ? `expo-clipboard` installed
- ? `react-native-qrcode-svg` installed
- ? `expo-secure-store` configured
- ? Proper error boundaries
- ? Analytics tracking (optional)
- ? Crash reporting (Sentry, etc.)

---

## ?? Advanced Features

### **1. Deep Linking to Wallet Apps**

```typescript
import { Linking } from 'react-native';

const openWallet = (address: string, amount: number, currency: string) => {
  const urls = {
    tron: `tronlink://send?address=${address}&amount=${amount}`,
    ethereum: `ethereum:${address}?value=${amount}`,
  };

  const url = urls[currency.toLowerCase()] || address;
  
  Linking.canOpenURL(url).then(supported => {
    if (supported) {
      Linking.openURL(url);
    } else {
      // Fallback to copying address
      Clipboard.setStringAsync(address);
      alert('Wallet app not found. Address copied!');
    }
  });
};
```

### **2. Push Notifications**

```typescript
import * as Notifications from 'expo-notifications';

// Configure notifications
Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
  }),
});

// Send notification when payment detected
const notifyPaymentDetected = async () => {
  await Notifications.scheduleNotificationAsync({
    content: {
      title: '?? Payment Detected!',
      body: 'Your transaction has been found on the blockchain.',
      sound: true,
    },
    trigger: null, // Show immediately
  });
};

// Send notification when payment completed
const notifyPaymentCompleted = async () => {
  await Notifications.scheduleNotificationAsync({
    content: {
      title: '?? Payment Completed!',
      body: 'Your premium subscription is now active.',
      sound: true,
    },
    trigger: null,
  });
};
```

### **3. Persistent Payment Recovery**

```typescript
import AsyncStorage from '@react-native-async-storage/async-storage';

// Save payment ID for recovery
const savePendingPayment = async (paymentId: string) => {
  await AsyncStorage.setItem('pendingPaymentId', paymentId);
};

// Check for pending payments on app start
const checkPendingPayment = async (): Promise<string | null> => {
  return await AsyncStorage.getItem('pendingPaymentId');
};

// Clear pending payment
const clearPendingPayment = async () => {
  await AsyncStorage.removeItem('pendingPaymentId');
};

// Usage in App.tsx
useEffect(() => {
  const resumePayment = async () => {
    const pendingId = await checkPendingPayment();
    if (pendingId) {
      // Check if payment is still valid
      const service = new CryptoPaymentService(token);
      const payment = await service.getPayment(pendingId);
      
      if (['pending', 'confirming'].includes(payment.status)) {
        // Resume payment screen
        navigation.navigate('Payment', { paymentId: pendingId });
      } else {
        // Clear old payment
        await clearPendingPayment();
      }
    }
  };

  resumePayment();
}, []);
```

### **4. Background Polling**

```typescript
import * as BackgroundFetch from 'expo-background-fetch';
import * as TaskManager from 'expo-task-manager';

const PAYMENT_CHECK_TASK = 'payment-status-check';

// Define background task
TaskManager.defineTask(PAYMENT_CHECK_TASK, async () => {
  try {
    const paymentId = await AsyncStorage.getItem('pendingPaymentId');
    if (!paymentId) return BackgroundFetch.BackgroundFetchResult.NoData;

    const token = await SecureStore.getItemAsync('userToken');
    if (!token) return BackgroundFetch.BackgroundFetchResult.Failed;

    const service = new CryptoPaymentService(token);
    const payment = await service.checkPaymentStatus(paymentId);

    if (payment.status === 'completed') {
      await notifyPaymentCompleted();
      await clearPendingPayment();
      return BackgroundFetch.BackgroundFetchResult.NewData;
    }

    return BackgroundFetch.BackgroundFetchResult.NoData;
  } catch (error) {
    return BackgroundFetch.BackgroundFetchResult.Failed;
  }
});

// Register background task
const registerBackgroundTask = async () => {
  await BackgroundFetch.registerTaskAsync(PAYMENT_CHECK_TASK, {
    minimumInterval: 60, // Check every minute
    stopOnTerminate: false,
    startOnBoot: true,
  });
};
```

---

## ?? Troubleshooting

### **Common Issues:**

#### **1. Payment Not Detected**

**Symptoms:**
- Status stays on "pending" even after sending crypto
- Backend logs show "No matching transaction found"

**Solutions:**
- Verify correct wallet address was used
- Check if exact amount was sent (match within 1%)
- Confirm correct network (Tron mainnet, not testnet)
- Wait up to 60 seconds for blockchain propagation
- Check TronGrid API key is configured
- Verify backend logs show wallet scanning attempts

#### **2. Polling Not Working**

**Symptoms:**
- Status doesn't update automatically
- Must manually refresh to see changes

**Solutions:**
- Check network connectivity
- Verify JWT token is valid
- Ensure polling interval is configured
- Check console for errors
- Verify `useEffect` dependencies are correct

#### **3. QR Code Not Showing**

**Symptoms:**
- QR code component renders blank
- Error in console about QRCode

**Solutions:**
- Verify `react-native-qrcode-svg` is installed
- Check if `walletAddress` prop is valid
- Ensure SVG support is enabled in your app
- Try reinstalling dependencies

#### **4. Token Expired During Payment**

**Symptoms:**
- 401 Unauthorized errors during polling
- "Token-Expired" header in response

**Solutions:**
- Implement token refresh logic
- Store refresh token securely
- Handle 401 errors gracefully
- Redirect to login if refresh fails

```typescript
const handleTokenExpired = async () => {
  // Try to refresh token
  const refreshToken = await SecureStore.getItemAsync('refreshToken');
  if (refreshToken) {
    try {
      const newToken = await refreshAuthToken(refreshToken);
      await SecureStore.setItemAsync('userToken', newToken);
      return newToken;
    } catch (error) {
      // Refresh failed, redirect to login
      navigation.navigate('Login');
    }
  }
};
```

---

## ?? Analytics & Monitoring

### **Track Important Events:**

```typescript
import * as Analytics from 'expo-firebase-analytics';

// Payment initiated
Analytics.logEvent('payment_initiated', {
  amount_usd: 9.99,
  currency: 'USDT',
  network: 'tron'
});

// Payment detected
Analytics.logEvent('payment_detected', {
  payment_id: paymentId,
  detection_time_seconds: 45
});

// Payment completed
Analytics.logEvent('payment_completed', {
  payment_id: paymentId,
  total_time_seconds: 180,
  confirmations: 19
});

// Payment expired
Analytics.logEvent('payment_expired', {
  payment_id: paymentId,
  reason: 'timeout'
});
```

---

## ?? Summary

### **User Flow:**

1. **User taps "Upgrade to Premium"**
   - App creates payment via API
   - Displays QR code and wallet address

2. **User sends crypto from their wallet**
   - No additional action needed
   - App shows "Scanning blockchain..." message

3. **Backend detects transaction** (within 30 seconds)
   - Background job finds matching transaction
   - Links transaction to payment
   - Updates status to "confirming"

4. **App shows confirmation progress**
   - Polls every 15 seconds
   - Displays progress bar
   - Shows "5/19 confirmations" counter

5. **Payment completes automatically**
   - Backend marks as "completed"
   - Premium subscription activated
   - App redirects to success screen

### **Key Benefits:**

? **Zero Manual Steps** - Fully automatic detection  
? **Fast Detection** - 10-60 seconds average  
? **Real-time Progress** - Live confirmation tracking  
? **Reliable** - Retries and error handling  
? **User-Friendly** - Clear status indicators  

### **Technical Highlights:**

- **Backend:** Hangfire background job scanning wallets every 30 seconds
- **Frontend:** React Native polling status every 10-15 seconds
- **Blockchain:** TronGrid API for Tron, Infura for Ethereum/Sepolia
- **Security:** JWT authentication, secure token storage
- **UX:** QR codes, copy-to-clipboard, progress bars, timers

---

## ?? Additional Resources

### **Documentation:**

- **TronGrid API:** https://www.trongrid.io/
- **Infura API:** https://infura.io/docs/ethereum
- **Hangfire:** https://docs.hangfire.io/
- **Expo Documentation:** https://docs.expo.dev/

### **Blockchain Explorers:**

- **Tron:** https://tronscan.org/
- **Ethereum:** https://etherscan.io/
- **Sepolia:** https://sepolia.etherscan.io/

### **Support:**

If you encounter issues:

1. Check backend console logs (very verbose now)
2. Verify TronGrid API key is configured
3. Confirm payment exists in database
4. Check network connectivity
5. Verify JWT token is valid
6. Review Hangfire dashboard at `/hangfire`

---

## ?? Version History

- **v1.0** - Initial implementation with automatic detection
  - Tron USDT support
  - Polling every 30 seconds
  - Manual transaction hash submission (legacy)

- **v2.0** - Enhanced automatic detection (current)
  - Proactive wallet scanning
  - TronGrid API integration
  - No manual hash submission needed
  - Comprehensive logging
  - React Native component library

---

**End of Document**

For questions or support, contact the backend team or check the repository:
https://github.com/fullstackragab/wihngo-api
