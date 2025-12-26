# ?? MOBILE APP - REQUIRED CHANGES FOR UPDATED CRYPTO SUPPORT

**Date:** December 11, 2025  
**Version:** 2.0  
**Impact:** BREAKING CHANGES - App update required  
**Priority:** HIGH

---

## ?? BREAKING CHANGES SUMMARY

The backend crypto payment system has been **updated** to support only specific currencies and networks. **Old currency/network combinations will be rejected by the API.**

### ? **Removed Support:**
- Bitcoin (BTC)
- Solana (SOL)
- Polygon (MATIC)
- Native TRX (Tron)
- Sepolia testnet

### ? **Currently Supported:**
- USDT on Tron, Ethereum, BSC
- USDC on Ethereum, BSC
- ETH on Ethereum
- BNB on BSC

---

## ?? REQUIRED CHANGES CHECKLIST

### 1. ? **Update TypeScript Types/Interfaces**

**File:** `types/payment.ts` (or wherever you define types)

```typescript
// Updated payment currencies and networks
export type SupportedCurrency = 'USDT' | 'USDC' | 'ETH' | 'BNB';

export type SupportedNetwork = 'tron' | 'ethereum' | 'binance-smart-chain';

// Valid currency-network combinations
export interface CurrencyNetworkMap {
  USDT: ('tron' | 'ethereum' | 'binance-smart-chain')[];
  USDC: ('ethereum' | 'binance-smart-chain')[];
  ETH: ['ethereum'];
  BNB: ['binance-smart-chain'];
}

export const VALID_COMBINATIONS: CurrencyNetworkMap = {
  USDT: ['tron', 'ethereum', 'binance-smart-chain'],
  USDC: ['ethereum', 'binance-smart-chain'],
  ETH: ['ethereum'],
  BNB: ['binance-smart-chain']
};

// Payment response (no changes needed if you already use dynamic addresses)
export interface PaymentResponseDto {
  id: string;
  userId: string;
  birdId?: string;
  amountUsd: number;
  amountCrypto: number;
  currency: SupportedCurrency;           // ? Updated type
  network: SupportedNetwork;             // ? Updated type
  exchangeRate: number;
  walletAddress: string;                 // ? Still unique per payment
  addressIndex?: number;                 // ? HD wallet index (optional)
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

// Payment creation request
export interface CreatePaymentRequest {
  amountUsd: number;
  currency: SupportedCurrency;
  network: SupportedNetwork;
  purpose: 'premium_subscription' | 'bird_adoption' | 'donation' | 'custom';
  plan?: 'monthly' | 'yearly';
  birdId?: string;
}
```

---

### 2. ?? **Update Payment Method Selection UI**

**File:** `screens/PaymentMethodScreen.tsx` or `components/PaymentMethodSelector.tsx`

Replace your existing payment options with this configuration:

```typescript
// Payment method configuration
export const PAYMENT_METHODS: PaymentMethod[] = [
  // RECOMMENDED - Show first
  {
    id: 'usdt-tron',
    name: 'USDT (Tron)',
    currency: 'USDT',
    network: 'tron',
    icon: require('../assets/icons/tron.png'),
    badge: 'RECOMMENDED',
    badgeColor: '#00D4FF',
    estimatedFee: '~$0.01',
    estimatedTime: '~1 min',
    description: 'Cheapest and fastest option',
    advantages: ['Lowest fees', 'Very fast', 'Most popular'],
    confirmations: 19,
    enabled: true,
    sortOrder: 1
  },
  
  // Low-fee alternatives
  {
    id: 'usdt-bsc',
    name: 'USDT (BSC)',
    currency: 'USDT',
    network: 'binance-smart-chain',
    icon: require('../assets/icons/bsc.png'),
    estimatedFee: '~$0.05',
    estimatedTime: '~1 min',
    description: 'Low fee alternative',
    advantages: ['Low fees', 'Fast', 'Reliable'],
    confirmations: 15,
    enabled: true,
    sortOrder: 2
  },
  
  {
    id: 'usdc-bsc',
    name: 'USDC (BSC)',
    currency: 'USDC',
    network: 'binance-smart-chain',
    icon: require('../assets/icons/bsc-usdc.png'),
    estimatedFee: '~$0.05',
    estimatedTime: '~1 min',
    description: 'USDC on low-fee network',
    advantages: ['Regulated stablecoin', 'Low fees', 'Fast'],
    confirmations: 15,
    enabled: true,
    sortOrder: 3
  },
  
  // Ethereum options - higher fees but most trusted
  {
    id: 'usdc-ethereum',
    name: 'USDC (Ethereum)',
    currency: 'USDC',
    network: 'ethereum',
    icon: require('../assets/icons/ethereum-usdc.png'),
    badge: 'MOST TRUSTED',
    badgeColor: '#627EEA',
    estimatedFee: '$5-50',
    estimatedTime: '~3 min',
    description: 'Most trusted stablecoin',
    advantages: ['Regulated', 'FDIC-backed', 'Most secure'],
    confirmations: 12,
    enabled: true,
    sortOrder: 4
  },
  
  {
    id: 'usdt-ethereum',
    name: 'USDT (Ethereum)',
    currency: 'USDT',
    network: 'ethereum',
    icon: require('../assets/icons/ethereum-usdt.png'),
    estimatedFee: '$5-50',
    estimatedTime: '~3 min',
    description: 'USDT on Ethereum network',
    advantages: ['Most liquid', 'Widely accepted'],
    confirmations: 12,
    enabled: true,
    sortOrder: 5
  },
  
  // Native tokens - for users who hold these
  {
    id: 'eth',
    name: 'ETH',
    currency: 'ETH',
    network: 'ethereum',
    icon: require('../assets/icons/ethereum.png'),
    estimatedFee: '$5-50',
    estimatedTime: '~3 min',
    description: 'Native Ethereum',
    advantages: ['Native token', 'Widely held'],
    confirmations: 12,
    enabled: true,
    sortOrder: 6
  },
  
  {
    id: 'bnb',
    name: 'BNB',
    currency: 'BNB',
    network: 'binance-smart-chain',
    icon: require('../assets/icons/bnb.png'),
    estimatedFee: '~$0.05',
    estimatedTime: '~1 min',
    description: 'Native BSC token',
    advantages: ['Low fees', 'Fast', 'Popular'],
    confirmations: 15,
    enabled: true,
    sortOrder: 7
  }
];

// Helper function to get enabled payment methods
export const getEnabledPaymentMethods = () => 
  PAYMENT_METHODS.filter(m => m.enabled).sort((a, b) => a.sortOrder - b.sortOrder);

// Helper function to validate currency-network combination
export const isValidCombination = (currency: string, network: string): boolean => {
  const validNetworks = VALID_COMBINATIONS[currency as SupportedCurrency];
  return validNetworks?.includes(network as any) ?? false;
};
```

**Type definition for PaymentMethod:**

```typescript
interface PaymentMethod {
  id: string;
  name: string;
  currency: SupportedCurrency;
  network: SupportedNetwork;
  icon: any;
  badge?: string;
  badgeColor?: string;
  estimatedFee: string;
  estimatedTime: string;
  description: string;
  advantages: string[];
  confirmations: number;
  enabled: boolean;
  sortOrder: number;
}
```

---

### 3. ?? **Update Payment Method UI Component**

**Example React Native component:**

```tsx
import React from 'react';
import { View, Text, TouchableOpacity, Image, StyleSheet } from 'react-native';
import { PAYMENT_METHODS } from '../config/paymentMethods';

interface PaymentMethodSelectorProps {
  selectedMethodId?: string;
  onSelect: (method: PaymentMethod) => void;
}

export const PaymentMethodSelector: React.FC<PaymentMethodSelectorProps> = ({
  selectedMethodId,
  onSelect
}) => {
  const enabledMethods = PAYMENT_METHODS.filter(m => m.enabled);
  
  return (
    <View style={styles.container}>
      <Text style={styles.title}>Select Payment Method</Text>
      <Text style={styles.subtitle}>Choose how you want to pay</Text>
      
      {enabledMethods.map((method) => (
        <TouchableOpacity
          key={method.id}
          style={[
            styles.methodCard,
            selectedMethodId === method.id && styles.methodCardSelected
          ]}
          onPress={() => onSelect(method)}
        >
          <View style={styles.methodHeader}>
            <Image source={method.icon} style={styles.methodIcon} />
            <View style={styles.methodInfo}>
              <View style={styles.methodTitleRow}>
                <Text style={styles.methodName}>{method.name}</Text>
                {method.badge && (
                  <View style={[styles.badge, { backgroundColor: method.badgeColor }]}>
                    <Text style={styles.badgeText}>{method.badge}</Text>
                  </View>
                )}
              </View>
              <Text style={styles.methodDescription}>{method.description}</Text>
            </View>
          </View>
          
          <View style={styles.methodDetails}>
            <View style={styles.detailRow}>
              <Text style={styles.detailLabel}>Fee:</Text>
              <Text style={styles.detailValue}>{method.estimatedFee}</Text>
            </View>
            <View style={styles.detailRow}>
              <Text style={styles.detailLabel}>Time:</Text>
              <Text style={styles.detailValue}>{method.estimatedTime}</Text>
            </View>
            <View style={styles.detailRow}>
              <Text style={styles.detailLabel}>Confirmations:</Text>
              <Text style={styles.detailValue}>{method.confirmations} blocks</Text>
            </View>
          </View>
          
          <View style={styles.advantages}>
            {method.advantages.map((adv, idx) => (
              <View key={idx} style={styles.advantageTag}>
                <Text style={styles.advantageText}>? {adv}</Text>
              </View>
            ))}
          </View>
        </TouchableOpacity>
      ))}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    padding: 16,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 14,
    color: '#666',
    marginBottom: 20,
  },
  methodCard: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    borderWidth: 2,
    borderColor: '#e0e0e0',
  },
  methodCardSelected: {
    borderColor: '#4CAF50',
    backgroundColor: '#f1f8f4',
  },
  methodHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
  },
  methodIcon: {
    width: 48,
    height: 48,
    marginRight: 12,
  },
  methodInfo: {
    flex: 1,
  },
  methodTitleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
  },
  methodName: {
    fontSize: 18,
    fontWeight: '600',
    marginRight: 8,
  },
  badge: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 4,
  },
  badgeText: {
    color: '#fff',
    fontSize: 10,
    fontWeight: 'bold',
  },
  methodDescription: {
    fontSize: 14,
    color: '#666',
  },
  methodDetails: {
    marginTop: 12,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#f0f0f0',
  },
  detailRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 6,
  },
  detailLabel: {
    fontSize: 14,
    color: '#888',
  },
  detailValue: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
  },
  advantages: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginTop: 12,
    gap: 6,
  },
  advantageTag: {
    backgroundColor: '#e8f5e9',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 4,
  },
  advantageText: {
    fontSize: 12,
    color: '#2e7d32',
  },
});
```

---

### 4. ?? **Update API Calls**

**File:** `services/paymentService.ts` or `api/payments.ts`

```typescript
import { API_BASE_URL } from '../config/constants';
import { getAuthToken } from './authService';

// Create crypto payment
export const createCryptoPayment = async (
  request: CreatePaymentRequest
): Promise<PaymentResponseDto> => {
  // Validate combination before calling API
  if (!isValidCombination(request.currency, request.network)) {
    throw new Error(
      `Invalid combination: ${request.currency} is not supported on ${request.network}`
    );
  }
  
  const token = await getAuthToken();
  
  const response = await fetch(`${API_BASE_URL}/api/payments/crypto/create`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Failed to create payment');
  }
  
  return await response.json();
};

// Get payment status
export const getPaymentStatus = async (
  paymentId: string
): Promise<PaymentResponseDto> => {
  const token = await getAuthToken();
  
  const response = await fetch(
    `${API_BASE_URL}/api/payments/crypto/${paymentId}`,
    {
      headers: { 'Authorization': `Bearer ${token}` },
    }
  );
  
  if (!response.ok) {
    throw new Error('Failed to fetch payment status');
  }
  
  return await response.json();
};

// Submit transaction hash (optional - auto-detection works for Tron)
export const submitTransactionHash = async (
  paymentId: string,
  transactionHash: string
): Promise<PaymentResponseDto> => {
  const token = await getAuthToken();
  
  const response = await fetch(
    `${API_BASE_URL}/api/payments/crypto/${paymentId}/verify`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ transactionHash }),
    }
  );
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Failed to verify transaction');
  }
  
  return await response.json();
};

// Get exchange rates
export const getExchangeRates = async (): Promise<ExchangeRatesResponse> => {
  const response = await fetch(`${API_BASE_URL}/api/payments/crypto/rates`);
  
  if (!response.ok) {
    throw new Error('Failed to fetch exchange rates');
  }
  
  return await response.json();
};

interface ExchangeRatesResponse {
  rates: Record<string, number>;
  lastUpdated: string;
  baseCurrency: string;
}
```

---

### 5. ?? **Update Payment Flow Screen**

**Key changes in your payment screen:**

```typescript
const PaymentScreen: React.FC<PaymentScreenProps> = ({ route, navigation }) => {
  const { amountUsd, purpose, plan } = route.params;
  const [selectedMethod, setSelectedMethod] = useState<PaymentMethod>();
  const [payment, setPayment] = useState<PaymentResponseDto>();
  const [loading, setLoading] = useState(false);
  const [polling, setPolling] = useState<NodeJS.Timer>();
  
  // Step 1: User selects payment method
  const handleMethodSelect = (method: PaymentMethod) => {
    setSelectedMethod(method);
  };
  
  // Step 2: Create payment request
  const handleCreatePayment = async () => {
    if (!selectedMethod) return;
    
    setLoading(true);
    try {
      const paymentRequest: CreatePaymentRequest = {
        amountUsd,
        currency: selectedMethod.currency,
        network: selectedMethod.network,
        purpose,
        plan,
      };
      
      const createdPayment = await createCryptoPayment(paymentRequest);
      setPayment(createdPayment);
      
      // Start polling for payment status
      startPolling(createdPayment.id);
      
    } catch (error) {
      Alert.alert('Error', error.message);
    } finally {
      setLoading(false);
    }
  };
  
  // Step 3: Poll for payment status
  const startPolling = (paymentId: string) => {
    const interval = setInterval(async () => {
      try {
        const updated = await getPaymentStatus(paymentId);
        setPayment(updated);
        
        // Check if completed
        if (updated.status === 'completed') {
          clearInterval(interval);
          navigation.navigate('PaymentSuccess', { payment: updated });
        } else if (updated.status === 'expired') {
          clearInterval(interval);
          Alert.alert('Payment Expired', 'Please create a new payment');
        }
      } catch (error) {
        console.error('Polling error:', error);
      }
    }, 10000); // Poll every 10 seconds
    
    setPolling(interval);
  };
  
  // Cleanup
  useEffect(() => {
    return () => {
      if (polling) clearInterval(polling);
    };
  }, [polling]);
  
  // Render UI...
  return (
    <View>
      {!selectedMethod && (
        <PaymentMethodSelector
          onSelect={handleMethodSelect}
        />
      )}
      
      {selectedMethod && !payment && (
        <Button
          title={`Pay ${amountUsd} USD with ${selectedMethod.name}`}
          onPress={handleCreatePayment}
          loading={loading}
        />
      )}
      
      {payment && (
        <PaymentWaitingScreen
          payment={payment}
          onCancel={() => {
            if (polling) clearInterval(polling);
            navigation.goBack();
          }}
        />
      )}
    </View>
  );
};
```

---

### 6. ?? **Remove Old Code**

**Delete or comment out code for unsupported currencies:**

```typescript
// ? REMOVE THESE - No longer supported
const BITCOIN_OPTIONS = { ... };
const SOLANA_OPTIONS = { ... };
const POLYGON_OPTIONS = { ... };
const TRX_NATIVE_OPTIONS = { ... };
const SEPOLIA_TESTNET_OPTIONS = { ... };

// ? REMOVE validation for old currencies
if (currency === 'BTC') { ... }
if (network === 'solana') { ... }
if (network === 'polygon') { ... }
if (network === 'sepolia') { ... }
```

---

### 7. ?? **Testing Checklist**

Before releasing the app update:

#### ? **Test Each Payment Method**

1. **USDT on Tron** (Recommended)
   - [ ] Create payment
   - [ ] Show QR code with unique address
   - [ ] Send test payment (use testnet or small amount)
   - [ ] Verify auto-detection works
   - [ ] Confirm payment completes

2. **USDT on BSC**
   - [ ] Create payment
   - [ ] Verify address format (0x...)
   - [ ] Test transaction submission

3. **USDC on Ethereum**
   - [ ] Create payment
   - [ ] Verify high fee warning displayed
   - [ ] Test transaction

4. **USDC on BSC**
   - [ ] Create payment and verify

5. **ETH on Ethereum**
   - [ ] Create payment and verify

6. **BNB on BSC**
   - [ ] Create payment and verify

#### ? **Test Error Handling**

- [ ] Invalid currency/network combination rejected by API
- [ ] Expired payment handled correctly
- [ ] Network timeout handled
- [ ] API errors shown to user

#### ? **Test Edge Cases**

- [ ] Payment expires before completion
- [ ] User sends wrong amount
- [ ] User sends to wrong address
- [ ] User submits invalid transaction hash
- [ ] Network issues during payment creation

---

### 8. ?? **Analytics & Tracking**

**Add analytics events for new payment methods:**

```typescript
// Track payment method selection
analytics.logEvent('payment_method_selected', {
  currency: method.currency,
  network: method.network,
  method_id: method.id,
  fee_estimate: method.estimatedFee,
});

// Track payment creation
analytics.logEvent('payment_created', {
  payment_id: payment.id,
  currency: payment.currency,
  network: payment.network,
  amount_usd: payment.amountUsd,
  amount_crypto: payment.amountCrypto,
  has_hd_index: payment.addressIndex !== undefined,
});

// Track payment completion
analytics.logEvent('payment_completed', {
  payment_id: payment.id,
  currency: payment.currency,
  network: payment.network,
  time_to_complete: completionTime,
  confirmations: payment.confirmations,
});
```

---

### 9. ?? **User Communication**

**Update app store description and release notes:**

```markdown
## What's New in v2.0

### Updated Crypto Payment Support

We've streamlined our cryptocurrency payment options to provide you with the best experience:

**New Payment Options:**
? USDT on Tron (Cheapest - $0.01 fee)
? USDT on BSC (Low fee - $0.05)
? USDC on Ethereum (Most trusted)
? USDC on BSC (Low fee alternative)
? ETH (Native Ethereum)
? BNB (Native BSC)

**Removed:**
- Bitcoin (BTC)
- Solana (SOL)
- Polygon (MATIC)

**Benefits:**
- Lower fees on Tron and BSC
- Faster confirmations
- Better reliability
- Unique payment addresses for security

**Important:** If you previously used Bitcoin, Solana, or Polygon, please use one of the new supported options.
```

---

### 10. ?? **Deployment Plan**

#### **Phase 1: Beta Testing (1 week)**
- [ ] Deploy to internal testers
- [ ] Test all payment methods
- [ ] Verify analytics working
- [ ] Check error handling

#### **Phase 2: Staged Rollout**
- [ ] Release to 10% of users
- [ ] Monitor for issues
- [ ] Check payment completion rate
- [ ] Increase to 50% if stable
- [ ] Full rollout to 100%

#### **Phase 3: Monitoring**
- [ ] Monitor payment success rate
- [ ] Track most popular payment methods
- [ ] Monitor error rates
- [ ] Collect user feedback

---

## ?? **Support & Questions**

### **Backend API Contact:**
- API Documentation: See `CONFIGURATION_COMPLETE.md`
- Test Endpoint: `POST /api/payments/crypto/create`
- Status Endpoint: `GET /api/payments/crypto/{id}`

### **Technical Questions:**
Contact backend team with:
1. Which payment method failing
2. Error message from API
3. Request payload sent
4. User ID and payment ID

### **Testing:**
- Use Sepolia faucet for test ETH: https://sepoliafaucet.com/
- Use Tron testnet for test USDT: https://nileex.io/
- Small amounts recommended for first tests

---

## ?? **Success Criteria**

Before marking this implementation complete:

- [ ] All 7 payment methods selectable in UI
- [ ] API calls use correct types
- [ ] Unique HD addresses displayed
- [ ] QR codes generated correctly
- [ ] Payment polling works
- [ ] Transaction submission works
- [ ] Error handling complete
- [ ] Analytics tracking implemented
- [ ] Old code removed
- [ ] Beta testing passed
- [ ] App store listing updated

---

## ?? **Quick Reference**

### **Supported Combinations Matrix:**

| Currency | Tron | Ethereum | BSC |
|----------|:----:|:--------:|:---:|
| USDT | ? | ? | ? |
| USDC | ? | ? | ? |
| ETH | ? | ? | ? |
| BNB | ? | ? | ? |

### **Network Identifiers:**
- Tron: `"tron"`
- Ethereum: `"ethereum"`
- BSC: `"binance-smart-chain"`

### **Currency Identifiers:**
- USDT: `"USDT"`
- USDC: `"USDC"`
- ETH: `"ETH"`
- BNB: `"BNB"`

---

## ?? **Appendix: Example API Responses**

### **Successful Payment Creation:**

```json
{
  "id": "abc123-payment-id",
  "userId": "user-id",
  "amountUsd": 10.00,
  "amountCrypto": 10.0,
  "currency": "USDT",
  "network": "tron",
  "exchangeRate": 1.0,
  "walletAddress": "TXyz123abc456def789...",
  "addressIndex": 42,
  "qrCodeData": "TXyz123abc456def789...",
  "paymentUri": "TXyz123abc456def789...",
  "confirmations": 0,
  "requiredConfirmations": 19,
  "status": "pending",
  "purpose": "premium_subscription",
  "plan": "monthly",
  "expiresAt": "2025-12-11T23:00:00Z",
  "createdAt": "2025-12-11T22:30:00Z",
  "updatedAt": "2025-12-11T22:30:00Z"
}
```

### **Error Response (Invalid Combination):**

```json
{
  "error": "InvalidRequest",
  "message": "Currency ETH is not supported on network binance-smart-chain",
  "details": {
    "supportedNetworks": ["ethereum"],
    "requestedNetwork": "binance-smart-chain"
  }
}
```

---

**Document Version:** 2.0  
**Last Updated:** December 11, 2025  
**Questions?** Contact backend team or see `SUPPORTED_CURRENCIES_AND_NETWORKS.md`
