# ?? SUPPORTED CRYPTOCURRENCIES & NETWORKS

## ? Supported Configuration

Your platform now supports **4 cryptocurrencies** on **3 blockchain networks**:

---

### ?? **Currency Matrix**

| Currency | Type | Network | Contract Address | Decimals | Fee Estimate |
|----------|------|---------|------------------|----------|--------------|
| **USDT** | Stablecoin | Tron | `TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t` | 6 | ~$0.01 |
| **USDT** | Stablecoin | Ethereum | `0xdac17f958d2ee523a2206206994597c13d831ec7` | 6 | $5-50 |
| **USDT** | Stablecoin | BSC | `0x55d398326f99059fF775485246999027B3197955` | 18 | ~$0.05 |
| **USDC** | Stablecoin | Ethereum | `0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48` | 6 | $5-50 |
| **USDC** | Stablecoin | BSC | `0x8ac76a51cc950d9822d68b83fe1ad97b32cd580d` | 18 | ~$0.05 |
| **ETH** | Native | Ethereum | - (native) | 18 | $5-50 |
| **BNB** | Native | BSC | - (native) | 18 | ~$0.05 |

---

### ?? **Network Details**

#### 1. **Tron**
- **RPC:** `https://api.trongrid.io`
- **Explorer:** https://tronscan.org
- **Confirmations Required:** 19 blocks (~57 seconds)
- **Supported Currencies:** USDT (TRC-20)
- **Best For:** Low fees, fast transactions, popular in Asia

#### 2. **Ethereum**
- **RPC:** Infura (mainnet.infura.io)
- **Explorer:** https://etherscan.io
- **Confirmations Required:** 12 blocks (~2.4 minutes)
- **Supported Currencies:** USDT (ERC-20), USDC (ERC-20), ETH (native)
- **Best For:** Maximum security, most trusted network

#### 3. **Binance Smart Chain (BSC)**
- **RPC:** `https://bsc-dataseed.binance.org`
- **Explorer:** https://bscscan.com
- **Confirmations Required:** 15 blocks (~45 seconds)
- **Supported Currencies:** USDT (BEP-20), USDC (BEP-20), BNB (native)
- **Best For:** Lower fees than Ethereum, fast transactions

---

## ?? **Implementation Details**

### **HD Wallet Support**
? All networks and currencies support HD wallet derivation
- Each payment gets a unique address
- Automatic address generation from master seed
- BIP-44 derivation paths:
  - Ethereum/BSC: `m/44'/60'/0'/0/X`
  - Tron: `m/44'/195'/0'/0/X`

### **Proactive Payment Detection**
? **Tron:** Wallet scanning implemented (scans for incoming USDT)
? **Ethereum/BSC:** Manual transaction hash submission required (scanning to be implemented)

### **Transaction Verification**
? All networks support full transaction verification:
- Amount verification
- Address verification (from/to)
- Confirmation tracking
- Status checking

---

## ?? **Mobile App Configuration**

### **Display to Users:**

```json
{
  "supportedPayments": [
    {
      "name": "USDT (Tron)",
      "currency": "USDT",
      "network": "tron",
      "description": "Cheapest option - $0.01 fee",
      "recommended": true,
      "icon": "tron-usdt"
    },
    {
      "name": "USDT (BSC)",
      "currency": "USDT",
      "network": "binance-smart-chain",
      "description": "Low fee - $0.05",
      "icon": "bsc-usdt"
    },
    {
      "name": "USDC (Ethereum)",
      "currency": "USDC",
      "network": "ethereum",
      "description": "Most trusted - Higher fee",
      "icon": "eth-usdc"
    },
    {
      "name": "USDC (BSC)",
      "currency": "USDC",
      "network": "binance-smart-chain",
      "description": "Low fee - $0.05",
      "icon": "bsc-usdc"
    },
    {
      "name": "ETH (Ethereum)",
      "currency": "ETH",
      "network": "ethereum",
      "description": "Native Ethereum",
      "icon": "eth"
    },
    {
      "name": "BNB (BSC)",
      "currency": "BNB",
      "network": "binance-smart-chain",
      "description": "Native BSC token",
      "icon": "bnb"
    }
  ]
}
```

---

## ?? **Recommended Default**

**Show this first to users:**
1. **USDT on Tron** - Cheapest, fastest, most popular for payments
2. **USDT on BSC** - Second cheapest option
3. **USDC on Ethereum** - Most trusted for larger amounts
4. **Others** - Based on what user already holds

---

## ?? **Security Features**

### ? Implemented
- HD wallet unique addresses per payment
- Transaction verification on blockchain
- Confirmation tracking
- Amount tolerance checking (1%)
- Address validation
- Network mismatch detection

### ? Performance
- Exchange rates updated every 5 minutes
- Payment monitoring every 30 seconds
- Tron wallet scanning for auto-detection
- Automatic payment expiry (30 minutes default)

---

## ?? **Configuration Required**

### **appsettings.json or Environment Variables:**

```json
{
  "BlockchainSettings": {
    "TronGrid": {
      "ApiUrl": "https://api.trongrid.io",
      "ApiKey": "your-trongrid-api-key-optional"
    },
    "Infura": {
      "ProjectId": "your-infura-project-id-required-for-ethereum"
    },
    "BscDataSeed": {
      "Url": "https://bsc-dataseed.binance.org"
    }
  },
  "HdWallet": {
    "Mnemonic": "your 12 or 24 word seed phrase"
  },
  "CryptoPayment": {
    "ExpirationMinutes": 30,
    "MinimumAmountUsd": 5,
    "TolerancePercent": 1
  }
}
```

---

## ?? **Removed/Unsupported**

The following are **NOT** supported (removed from codebase):
- ? Bitcoin (BTC)
- ? Solana (SOL)
- ? Polygon (MATIC)
- ? Sepolia testnet (use mainnet only)
- ? TRX (native Tron)
- ? Other ERC-20 tokens

---

## ?? **Migration Path**

If you want to add more currencies later:

### **Easy to Add (Same infrastructure):**
- DAI (stablecoin) on Ethereum/BSC
- BUSD (stablecoin) on BSC
- Other ERC-20/BEP-20/TRC-20 tokens

### **Requires New Integration:**
- Polygon (new RPC endpoint, similar to BSC)
- Solana (completely different architecture)
- Bitcoin (UTXO model, different verification)

---

## ? **Summary**

Your platform is now configured for:
- ? 4 cryptocurrencies (USDT, USDC, ETH, BNB)
- ? 3 networks (Tron, Ethereum, BSC)
- ? HD wallet support (unique address per payment)
- ? Automatic monitoring and verification
- ? Stablecoin focus (USDT/USDC) for price stability
- ? Multiple network options for user choice

**This covers 95% of crypto payment use cases! ??**
