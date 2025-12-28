# Wihngo Frontend Integration Guide
## Solana USDC Payments with Phantom Wallet

**For: Next.js PWA Frontend Team**

---

## Core Principles (Non-Negotiable)

### 1. Bird Money is Sacred
- **100% of bird support goes to bird owner**
- Wihngo NEVER deducts fees from bird money
- This must be visually clear in the UI

### 2. Wihngo Support is Optional & Additive
- Added ON TOP of bird amount (not deducted)
- Minimum: **$0.05 USDC** (if > 0)
- User can set to $0 to skip entirely
- Default suggestion: $0.05

### 3. Only Solana USDC
- No SOL payments
- No other tokens
- SOL required only for gas (user's wallet)

### 4. Phantom Wallet Required for Payments
- Not required at registration
- **Mandatory before sending or receiving money**

---

## API Endpoints

### Base URL
| Environment | URL |
|-------------|-----|
| Development | `http://localhost:5000/api` |
| Production | `https://api.wihngo.com/api` |

---

## Payment Flow: Supporting a Bird

### Step 1: Preflight Check

Before showing the payment UI, validate the user can proceed.

**`POST /api/payments/support/preflight`**

```json
{
  "birdId": "uuid",
  "birdAmount": 3.00,
  "wihngoSupportAmount": 0.05
}
```

**Response (200 OK):**
```json
{
  "canSupport": true,
  "hasWallet": true,
  "usdcBalance": 50.00,
  "solBalance": 0.05,
  "birdAmount": 3.00,
  "wihngoSupportAmount": 0.05,
  "totalUsdcRequired": 3.05,
  "solRequired": 0.001,
  "usdcMintAddress": "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
  "wihngoWalletAddress": "6GXVP...",
  "bird": {
    "birdId": "uuid",
    "name": "Sunny",
    "imageUrl": "https://..."
  },
  "recipient": {
    "userId": "uuid",
    "name": "Bird Owner",
    "walletAddress": "abc123..."
  }
}
```

**Error Codes:**
| Code | Meaning |
|------|---------|
| `BIRD_NOT_FOUND` | Bird doesn't exist |
| `CANNOT_SUPPORT_OWN_BIRD` | User owns this bird |
| `WALLET_REQUIRED` | User needs to connect Phantom |
| `RECIPIENT_NO_WALLET` | Bird owner hasn't set up wallet |
| `INSUFFICIENT_USDC` | Not enough USDC balance |
| `INSUFFICIENT_SOL` | Not enough SOL for gas |
| `WIHNGO_SUPPORT_TOO_LOW` | Wihngo support < $0.05 (if > 0) |

---

### Step 2: Create Support Intent

**`POST /api/payments/intents`**

```json
{
  "birdId": "uuid",
  "birdAmount": 3.00,
  "wihngoSupportAmount": 0.05,
  "currency": "USDC"
}
```

**Response (200 OK):**
```json
{
  "intentId": "uuid",
  "birdId": "uuid",
  "birdName": "Sunny",
  "recipientUserId": "uuid",
  "recipientName": "Bird Owner",
  "birdWalletAddress": "abc123...",
  "wihngoWalletAddress": "6GXVP...",
  "birdAmount": 3.00,
  "wihngoSupportAmount": 0.05,
  "totalAmount": 3.05,
  "currency": "USDC",
  "usdcMintAddress": "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
  "status": "awaiting_payment",
  "serializedTransaction": "base64-encoded-unsigned-tx",
  "expiresAt": "2024-01-15T12:30:00Z",
  "createdAt": "2024-01-15T12:15:00Z"
}
```

---

### Step 3: Sign Transaction in Phantom

Use Solana Wallet Adapter to sign the transaction:

```typescript
import { useWallet } from '@solana/wallet-adapter-react';
import { Transaction } from '@solana/web3.js';

const { signTransaction, publicKey } = useWallet();

async function signSupportTransaction(serializedTransaction: string) {
  // Decode the base64 transaction
  const txBuffer = Buffer.from(serializedTransaction, 'base64');
  const transaction = Transaction.from(txBuffer);

  // Sign with Phantom
  const signedTx = await signTransaction(transaction);

  // Return base64 encoded signed transaction
  return Buffer.from(signedTx.serialize()).toString('base64');
}
```

---

### Step 4: Submit Signed Transaction

**`POST /api/payments/intents/{intentId}/submit`**

```json
{
  "signedTransaction": "base64-encoded-signed-tx"
}
```

**Response (200 OK):**
```json
{
  "paymentId": "uuid",
  "solanaSignature": "tx-signature-hash",
  "status": "processing"
}
```

---

### Step 5: Check Status (Optional Polling)

**`GET /api/payments/intents/{intentId}`**

**Response:**
```json
{
  "intentId": "uuid",
  "status": "completed",
  "birdAmount": 3.00,
  "wihngoSupportAmount": 0.05,
  "totalAmount": 3.05,
  ...
}
```

**Status Values:**
| Status | Meaning |
|--------|---------|
| `pending` | Intent created |
| `awaiting_payment` | Ready for wallet signature |
| `processing` | Transaction submitted, awaiting confirmation |
| `completed` | Confirmed on-chain |
| `expired` | Intent expired (15 min) |
| `cancelled` | User cancelled |
| `failed` | Transaction failed |

---

## Payment Flow: Supporting Wihngo Independently

For users who want to support Wihngo without a bird.

**`POST /api/payments/support-wihngo`**

```json
{
  "amount": 5.00
}
```

**Response:**
```json
{
  "intentId": "uuid",
  "amount": 5.00,
  "wihngoWalletAddress": "6GXVP...",
  "usdcMintAddress": "EPjFWdd5...",
  "serializedTransaction": "base64...",
  "status": "awaiting_payment",
  "expiresAt": "2024-01-15T12:30:00Z"
}
```

Then follow steps 3-5 above.

---

## UI Requirements

### Bird Support Page

**Required Elements:**
1. Bird info card (image, name, owner)
2. Bird amount input (required)
3. Wihngo support input (optional)
   - Default: `$0.05`
   - Toggle: "Support Wihngo" (ON by default)
   - Editable amount field
4. Clear breakdown display
5. "Connect Wallet" button (if not connected)
6. "Support Bird" button (when ready)

**Breakdown Display Example:**
```
┌────────────────────────────────┐
│  Bird Support     $3.00 USDC  │  ← 100% to Sunny's owner
│  ─────────────────────────────│
│  Support Wihngo   $0.05 USDC  │  ← Optional (toggle)
│  ═════════════════════════════│
│  TOTAL            $3.05 USDC  │
└────────────────────────────────┘

[✓] Support Wihngo (helps us grow)

         [ Support Sunny ]
```

### Language Guidelines

**Use:**
- "100% goes to the bird"
- "Optional support"
- "Added on top"
- "Bird money is untouched"
- "Support Wihngo (optional)"

**Avoid:**
- "Fees"
- "Commission"
- "Platform cut"
- "Service charge"

---

## Phantom Wallet Integration

### Setup

```bash
npm install @solana/wallet-adapter-react @solana/wallet-adapter-react-ui @solana/wallet-adapter-phantom @solana/web3.js
```

### Provider Setup

```tsx
// providers/WalletProvider.tsx
import { WalletAdapterNetwork } from '@solana/wallet-adapter-base';
import { ConnectionProvider, WalletProvider } from '@solana/wallet-adapter-react';
import { WalletModalProvider } from '@solana/wallet-adapter-react-ui';
import { PhantomWalletAdapter } from '@solana/wallet-adapter-phantom';
import { clusterApiUrl } from '@solana/web3.js';

const network = WalletAdapterNetwork.Mainnet;
const endpoint = clusterApiUrl(network);

const wallets = [new PhantomWalletAdapter()];

export function SolanaWalletProvider({ children }) {
  return (
    <ConnectionProvider endpoint={endpoint}>
      <WalletProvider wallets={wallets} autoConnect>
        <WalletModalProvider>
          {children}
        </WalletModalProvider>
      </WalletProvider>
    </ConnectionProvider>
  );
}
```

### Connect Button

```tsx
import { WalletMultiButton } from '@solana/wallet-adapter-react-ui';
import '@solana/wallet-adapter-react-ui/styles.css';

function WalletConnect() {
  return <WalletMultiButton />;
}
```

### Check Wallet Status

```tsx
import { useWallet } from '@solana/wallet-adapter-react';

function useWalletStatus() {
  const { connected, publicKey } = useWallet();

  return {
    isConnected: connected,
    walletAddress: publicKey?.toBase58()
  };
}
```

---

## Complete Support Flow Example

```tsx
// components/SupportBird.tsx
import { useState } from 'react';
import { useWallet } from '@solana/wallet-adapter-react';
import { Transaction } from '@solana/web3.js';

interface SupportBirdProps {
  birdId: string;
}

export function SupportBird({ birdId }: SupportBirdProps) {
  const { connected, signTransaction } = useWallet();
  const [birdAmount, setBirdAmount] = useState(3);
  const [wihngoSupport, setWihngoSupport] = useState(0.05);
  const [includeWihngo, setIncludeWihngo] = useState(true);
  const [loading, setLoading] = useState(false);
  const [status, setStatus] = useState<string | null>(null);

  const totalAmount = birdAmount + (includeWihngo ? wihngoSupport : 0);

  async function handleSupport() {
    if (!connected) {
      alert('Please connect your Phantom wallet');
      return;
    }

    setLoading(true);
    setStatus('Creating payment intent...');

    try {
      // 1. Create intent
      const intentRes = await fetch('/api/payments/intents', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          birdId,
          birdAmount,
          wihngoSupportAmount: includeWihngo ? wihngoSupport : 0,
          currency: 'USDC'
        })
      });

      const intent = await intentRes.json();

      if (!intentRes.ok) {
        throw new Error(intent.message);
      }

      setStatus('Please sign in your wallet...');

      // 2. Sign transaction
      const txBuffer = Buffer.from(intent.serializedTransaction, 'base64');
      const transaction = Transaction.from(txBuffer);
      const signedTx = await signTransaction(transaction);
      const signedTxBase64 = Buffer.from(signedTx.serialize()).toString('base64');

      setStatus('Submitting to blockchain...');

      // 3. Submit signed transaction
      const submitRes = await fetch(`/api/payments/intents/${intent.intentId}/submit`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({ signedTransaction: signedTxBase64 })
      });

      const result = await submitRes.json();

      if (!submitRes.ok) {
        throw new Error(result.message);
      }

      setStatus('Success! Thank you for supporting this bird!');

      // Show success UI with confetti

    } catch (error) {
      setStatus(`Error: ${error.message}`);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="support-card">
      <h2>Support this Bird</h2>

      {/* Bird Amount */}
      <div className="input-group">
        <label>Bird Support (100% to owner)</label>
        <input
          type="number"
          min="0.01"
          step="0.01"
          value={birdAmount}
          onChange={(e) => setBirdAmount(parseFloat(e.target.value))}
        />
        <span>USDC</span>
      </div>

      {/* Wihngo Support Toggle */}
      <div className="toggle-group">
        <label>
          <input
            type="checkbox"
            checked={includeWihngo}
            onChange={(e) => setIncludeWihngo(e.target.checked)}
          />
          Support Wihngo (optional)
        </label>
        {includeWihngo && (
          <input
            type="number"
            min="0.05"
            step="0.01"
            value={wihngoSupport}
            onChange={(e) => setWihngoSupport(parseFloat(e.target.value))}
          />
        )}
      </div>

      {/* Breakdown */}
      <div className="breakdown">
        <div className="line">
          <span>Bird Support</span>
          <span>${birdAmount.toFixed(2)} USDC</span>
        </div>
        {includeWihngo && (
          <div className="line optional">
            <span>Wihngo Support</span>
            <span>${wihngoSupport.toFixed(2)} USDC</span>
          </div>
        )}
        <div className="line total">
          <span>Total</span>
          <span>${totalAmount.toFixed(2)} USDC</span>
        </div>
      </div>

      {/* Action Button */}
      {!connected ? (
        <WalletMultiButton />
      ) : (
        <button onClick={handleSupport} disabled={loading}>
          {loading ? status : `Support Bird ($${totalAmount.toFixed(2)})`}
        </button>
      )}

      {/* Status Message */}
      {status && <p className="status">{status}</p>}
    </div>
  );
}
```

---

## Error Handling

### Common Error Scenarios

| Error | User Message | Action |
|-------|--------------|--------|
| `WALLET_REQUIRED` | "Connect your wallet to continue" | Show connect button |
| `INSUFFICIENT_USDC` | "Not enough USDC. You need $X.XX" | Show balance, link to get USDC |
| `INSUFFICIENT_SOL` | "Need SOL for transaction fee" | Link to get SOL |
| `RECIPIENT_NO_WALLET` | "Bird owner hasn't set up payments yet" | Disable support button |
| `INTENT_EXPIRED` | "Payment expired. Please try again" | Restart flow |
| `TRANSACTION_FAILED` | "Transaction failed. Please try again" | Show retry button |

---

## Testing

### Devnet Testing

1. Set network to devnet in Phantom settings
2. Get devnet SOL from faucet: https://faucet.solana.com
3. Get devnet USDC (contact backend team for test tokens)

### Test Checklist

- [ ] Wallet connection works
- [ ] Preflight check validates correctly
- [ ] Intent creation returns transaction
- [ ] Phantom signing popup appears
- [ ] Transaction submits successfully
- [ ] Status updates to completed
- [ ] Error messages display correctly
- [ ] Wihngo support toggle works
- [ ] $0 Wihngo support works
- [ ] Mobile deep link works

---

## Environment Variables

```env
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_SOLANA_NETWORK=devnet
NEXT_PUBLIC_SOLANA_RPC_URL=https://api.devnet.solana.com
```

---

## Questions?

Contact backend team for:
- Test USDC tokens on devnet
- API authentication setup
- Webhook integration for real-time updates

---

**Remember: Birds First, Always.**
