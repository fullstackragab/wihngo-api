# Frontend Update: Sepolia ETH Support

## ?? Important Changes Required

### Key Update: Currency Selection

When using Sepolia network, you **must** use `ETH` as the currency, not `USDT`.

---

## Updated Code

### 1. Dynamic Currency Selection in Payment Creation

**OLD (Incorrect):**
```javascript
await createPayment({
  amountUsd,
  currency: 'USDT',  // ? Wrong for Sepolia
  network: selectedNetwork,
  // ...
});
```

**NEW (Correct):**
```javascript
await createPayment({
  amountUsd,
  currency: selectedNetwork === 'sepolia' ? 'ETH' : 'USDT',  // ? Correct
  network: selectedNetwork,
  // ...
});
```

### 2. Display Currency Dynamically

**OLD (Hardcoded):**
```javascript
<Text>{payment.amountCrypto.toFixed(6)} USDT</Text>  // ? Wrong
```

**NEW (Dynamic):**
```javascript
<Text>{payment.amountCrypto.toFixed(6)} {payment.currency}</Text>  // ? Correct
```

---

## Complete Updated Component

```javascript
const CryptoPaymentScreen = ({ route, navigation }) => {
  const { amountUsd, birdId, plan } = route.params;
  const { payment, loading, createPayment, verifyPayment, checkStatus } = useCryptoPayment();
  
  const [selectedNetwork, setSelectedNetwork] = useState('tron');
  
  // Network options with currency information
  const networks = [
    { id: 'tron', name: 'Tron (TRC-20)', fee: 'Low', currency: 'USDT' },
    { id: 'ethereum', name: 'Ethereum (ERC-20)', fee: 'High', currency: 'USDT' },
    { id: 'binance-smart-chain', name: 'BSC (BEP-20)', fee: 'Medium', currency: 'USDT' },
    { id: 'sepolia', name: 'Sepolia Testnet (ETH)', fee: 'Low (Test)', currency: 'ETH' }
  ];

  // Initialize payment with correct currency
  useEffect(() => {
    const initPayment = async () => {
      try {
        // Get currency for selected network
        const networkConfig = networks.find(n => n.id === selectedNetwork);
        const currency = networkConfig?.currency || 'USDT';
        
        await createPayment({
          amountUsd,
          currency,              // Dynamic based on network
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

  return (
    <ScrollView style={styles.container}>
      {/* ... Network Selection ... */}
      
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
            {/* ... other details ... */}
          </View>

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
```

---

## API Request Examples

### Sepolia (Testnet)
```javascript
{
  "amountUsd": 10.00,
  "currency": "ETH",      // ? Native ETH
  "network": "sepolia",   // ? Testnet
  "birdId": "bird-guid",
  "purpose": "premium_subscription",
  "plan": "monthly"
}
```

### Tron (Mainnet)
```javascript
{
  "amountUsd": 10.00,
  "currency": "USDT",     // ? TRC-20 Token
  "network": "tron",      // ? Mainnet
  "birdId": "bird-guid",
  "purpose": "premium_subscription",
  "plan": "monthly"
}
```

---

## API Response Comparison

### Sepolia Payment Response
```json
{
  "amountUsd": 10.00,
  "amountCrypto": 0.003333,              // Small amount (ETH is expensive)
  "currency": "ETH",                      // Native cryptocurrency
  "network": "sepolia",
  "exchangeRate": 3000.00,                // ~$3000 per ETH
  "requiredConfirmations": 6              // Faster (testnet)
}
```

### Tron Payment Response
```json
{
  "amountUsd": 10.00,
  "amountCrypto": 10.02,                  // Nearly 1:1
  "currency": "USDT",                     // Stablecoin token
  "network": "tron",
  "exchangeRate": 0.9978,                 // ~$1 per USDT
  "requiredConfirmations": 19             // Standard
}
```

---

## Display Logic

### Recommended Helper Function

```javascript
const getCurrencyDisplay = (network) => {
  const currencyMap = {
    'sepolia': { symbol: 'ETH', decimals: 18 },
    'tron': { symbol: 'USDT', decimals: 6 },
    'ethereum': { symbol: 'USDT', decimals: 6 },
    'binance-smart-chain': { symbol: 'USDT', decimals: 18 }
  };
  return currencyMap[network] || { symbol: 'USDT', decimals: 6 };
};

// Usage
const { symbol, decimals } = getCurrencyDisplay(selectedNetwork);
```

### Format Amount Based on Currency

```javascript
const formatCryptoAmount = (amount, currency) => {
  if (currency === 'ETH') {
    return amount.toFixed(8);  // More decimals for ETH
  }
  return amount.toFixed(6);    // Standard for stablecoins
};

// Display
<Text>{formatCryptoAmount(payment.amountCrypto, payment.currency)} {payment.currency}</Text>
```

---

## Validation

### Pre-Payment Validation

```javascript
const validatePaymentRequest = (amountUsd, currency, network) => {
  // Minimum amount
  if (amountUsd < 5) {
    throw new Error('Minimum payment amount is $5');
  }

  // Valid currency for network
  const validCombinations = {
    'sepolia': ['ETH'],
    'tron': ['USDT'],
    'ethereum': ['USDT', 'USDC'],
    'binance-smart-chain': ['USDT', 'USDC']
  };

  if (!validCombinations[network]?.includes(currency)) {
    throw new Error(`Currency ${currency} not supported on ${network}`);
  }
};
```

---

## Error Messages

### User-Friendly Messages

```javascript
const getCurrencyError = (network) => {
  const messages = {
    'sepolia': 'Please use ETH for Sepolia testnet payments',
    'tron': 'Please use USDT for Tron network payments',
    'ethereum': 'Please use USDT or USDC for Ethereum payments',
    'binance-smart-chain': 'Please use USDT or USDC for BSC payments'
  };
  return messages[network] || 'Invalid currency for selected network';
};
```

---

## Testing Steps

### 1. Development Testing (Sepolia)

```javascript
// ? Test Sepolia with ETH
const payment = await api.createPayment({
  amountUsd: 5.00,
  currency: 'ETH',      // Correct
  network: 'sepolia',
  birdId: testBirdId,
  purpose: 'premium_subscription',
  plan: 'monthly'
});

console.log('Expected:', {
  currency: 'ETH',
  amountCrypto: 0.00166,  // ~$5 at $3000/ETH
  requiredConfirmations: 6
});
```

### 2. Production Testing (Tron)

```javascript
// ? Test Tron with USDT
const payment = await api.createPayment({
  amountUsd: 9.99,
  currency: 'USDT',     // Correct
  network: 'tron',
  birdId: realBirdId,
  purpose: 'premium_subscription',
  plan: 'monthly'
});

console.log('Expected:', {
  currency: 'USDT',
  amountCrypto: 10.02,  // ~1:1 with USD
  requiredConfirmations: 19
});
```

---

## Migration Checklist

### For Existing Apps

- [ ] Update payment creation to use dynamic currency
- [ ] Replace hardcoded "USDT" displays with `payment.currency`
- [ ] Update instructions to show correct currency
- [ ] Add network-to-currency mapping
- [ ] Test Sepolia with ETH
- [ ] Test mainnet networks with USDT
- [ ] Update error messages
- [ ] Add currency validation

### For New Integrations

- [ ] Use network configuration with currency
- [ ] Always display `payment.currency` from API
- [ ] Add testnet warning for Sepolia
- [ ] Environment-based network filtering (hide Sepolia in production)

---

## Summary

| Network | Currency | Use Case | Exchange Rate | Confirmations |
|---------|----------|----------|---------------|---------------|
| **Sepolia** | **ETH** | Testing | ~$3000 | 6 (~1.2 min) |
| Tron | USDT | Production | ~$1 | 19 (~57 sec) |
| Ethereum | USDT | Production | ~$1 | 12 (~2.4 min) |
| BSC | USDT | Production | ~$1 | 15 (~45 sec) |

**Key Takeaway:** Use `ETH` for Sepolia, `USDT` for everything else!

---

**Updated:** January 2025  
**Breaking Change:** Yes (Currency selection logic)  
**Required Action:** Update payment creation and display logic
