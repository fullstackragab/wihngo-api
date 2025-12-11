# ?? Quick Start: Sepolia Testnet Payments

## For Frontend Developers

### Step 1: Get Test ETH (5 minutes)

1. Visit: https://sepoliafaucet.com/
2. Enter your wallet address
3. Complete verification (may need Twitter/social media)
4. Receive 0.5 Sepolia ETH (enough for many tests)

### Step 2: Configure MetaMask

**Automatic (Recommended):**
- MetaMask usually detects Sepolia automatically
- Just switch network dropdown ? "Sepolia"

**Manual Setup:**
```
Network Name: Sepolia
RPC URL: https://sepolia.infura.io/v3/YOUR_KEY
Chain ID: 11155111
Currency Symbol: ETH
Block Explorer: https://sepolia.etherscan.io
```

### Step 3: Test Payment Code

```javascript
import CryptoPaymentAPI from './services/cryptoPaymentApi';

// Initialize API with your auth token
const api = new CryptoPaymentAPI(userAuthToken);

// Create payment request
const payment = await api.createPayment({
  amountUsd: 10.00,          // $10 test payment
  currency: 'ETH',           // Native Sepolia ETH
  network: 'sepolia',        // Testnet
  birdId: 'test-bird-id',
  purpose: 'premium_subscription',
  plan: 'monthly'
});

console.log('Send ETH to:', payment.walletAddress);
console.log('Amount:', payment.amountCrypto, 'ETH');
console.log('Confirmations needed:', payment.requiredConfirmations); // 6
```

### Step 4: Send Test Transaction

1. Open MetaMask
2. Send ETH to: `0x4cc28f4cea7b440858b903b5c46685cb1478cdc4`
3. Amount: Use exact `payment.amountCrypto` from response
4. Confirm transaction
5. Copy transaction hash (e.g., `0xabc123...`)

### Step 5: Verify Payment

```javascript
// After sending transaction
const result = await api.verifyPayment(
  payment.id,
  '0xYOUR_TRANSACTION_HASH',  // From MetaMask
  '0xYOUR_WALLET_ADDRESS'      // Your wallet
);

console.log('Status:', result.status); // 'confirming'
```

### Step 6: Poll for Completion

```javascript
// Check status every 5 seconds
const pollStatus = setInterval(async () => {
  const status = await api.checkPaymentStatus(payment.id);
  
  console.log(`Status: ${status.status}`);
  console.log(`Confirmations: ${status.confirmations}/${status.requiredConfirmations}`);
  
  if (status.status === 'completed') {
    clearInterval(pollStatus);
    console.log('? Payment successful! Premium activated.');
  }
}, 5000);
```

---

## ?? UI Implementation Example

```javascript
// Add to your network picker
const networks = [
  { id: 'sepolia', name: 'Sepolia Testnet (ETH)', fee: 'Low (Test)', icon: '??' }
];

// Show testnet warning
{selectedNetwork === 'sepolia' && (
  <View style={styles.warning}>
    <Text>?? Testnet Mode - No real money</Text>
  </View>
)}
```

---

## ?? Verify Transaction

Visit: https://sepolia.etherscan.io/tx/YOUR_TX_HASH

Check:
- ? Status: Success
- ? To: `0x4cc28f4cea7b440858b903b5c46685cb1478cdc4`
- ? Value: Matches `payment.amountCrypto`
- ? Confirmations: Increasing (need 6)

---

## ?? Expected Timeline

| Event | Time |
|-------|------|
| Transaction sent | Instant |
| First confirmation | ~12 seconds |
| Status: `confirming` | After 1st confirmation |
| 6 confirmations | ~1.2 minutes |
| Status: `completed` | After 6th confirmation |
| Premium activated | Immediately after completion |

---

## ?? Troubleshooting

**Transaction not found:**
- Wait ~15 seconds for blockchain propagation
- Verify transaction hash is correct
- Check Sepolia Etherscan for status

**Amount mismatch error:**
- Must send exact amount from `payment.amountCrypto`
- Check network fees didn't reduce amount
- Use "exact amount" feature in wallet

**Payment expired:**
- Create new payment request
- Payments expire after 30 minutes

**Wrong network error:**
- Make sure MetaMask is on Sepolia network
- Don't send mainnet ETH to test address

---

## ?? Pro Tips

1. **Small Amounts:** Test with $5-10 first
2. **Save Hash:** Always save transaction hash
3. **Monitor Logs:** Check API responses for errors
4. **Block Explorer:** Use Sepolia Etherscan to debug
5. **Rate Limits:** Exchange rates update every 5 minutes
6. **Test Thoroughly:** Try all payment states before production

---

## ?? Production Migration

When ready for production:

```javascript
// Change these two lines:
currency: 'USDT',          // Stable value (instead of ETH)
network: 'tron',           // Low fees (instead of sepolia)

// Or for mainnet ETH:
currency: 'ETH',
network: 'ethereum',       // Mainnet (instead of sepolia)
```

?? **Important:** Remove Sepolia option from production builds!

---

## ?? Need Help?

- **Faucet Issues:** Try alternative faucets (Alchemy, Infura)
- **MetaMask:** https://support.metamask.io/
- **API Errors:** Check backend logs at `/hangfire`
- **Documentation:** See `CRYPTO_PAYMENT_FRONTEND_INTEGRATION.md`

---

## ? Testing Checklist

- [ ] Got test ETH from faucet
- [ ] MetaMask configured for Sepolia
- [ ] Created payment request successfully
- [ ] Sent exact amount to correct address
- [ ] Transaction hash verified on Etherscan
- [ ] Payment confirmed after 6 blocks
- [ ] Premium subscription activated
- [ ] Tested payment expiration (30 min)
- [ ] Tested payment cancellation
- [ ] Ready for production migration

---

**Happy Testing! ??**

Remember: Sepolia ETH has no real value - test freely!
