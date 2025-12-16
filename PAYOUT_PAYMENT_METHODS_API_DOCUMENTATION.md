# Payout Payment Methods API Documentation ??

## Base URL
- **Development:** `https://localhost:7297/api/payouts` or `http://localhost:5162/api/payouts`
- **Production:** `https://your-domain.com/api/payouts`

---

## ?? Authentication
**All endpoints require authentication** using JWT Bearer token in the Authorization header.

```http
Authorization: Bearer YOUR_JWT_TOKEN
```

Get your token from `/api/auth/login` or `/api/auth/register`.

---

## Endpoints

### 1. Add New Payment Method
Add a new payout method for receiving payments.

**Endpoint:** `POST /api/payouts/methods`

**Authentication:** Required

**Request Body:**
```json
{
  "methodType": "string",
  "isDefault": boolean,
  // Fields vary based on methodType
}
```

---

## Payment Method Types

### ?? Method Type 1: Bank Transfer (IBAN/SEPA)
**`methodType: "BankTransfer"`** or **`"Wise"`**

**Required Fields:**
```json
{
  "methodType": "BankTransfer",
  "isDefault": false,
  "accountHolderName": "John Doe",
  "iban": "DE89370400440532013000",
  "bic": "COBADEFFXXX",
  "bankName": "Deutsche Bank"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `methodType` | string | ? Yes | "BankTransfer" or "Wise" |
| `accountHolderName` | string | ? Yes | Full name on bank account |
| `iban` | string | ? Yes | International Bank Account Number |
| `bic` | string | ?? Sometimes | Bank Identifier Code (required for some countries) |
| `bankName` | string | ? No | Bank name (for reference) |
| `isDefault` | boolean | ? No | Set as default method (default: false) |

**Example Request:**
```http
POST /api/payouts/methods
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "methodType": "BankTransfer",
  "accountHolderName": "Alice Johnson",
  "iban": "GB82WEST12345698765432",
  "bic": "NWBKGB2L",
  "bankName": "NatWest",
  "isDefault": true
}
```

**Response:** `201 Created`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
  "methodType": "BankTransfer",
  "isDefault": true,
  "isVerified": false,
  "createdAt": "2024-12-20T10:30:00Z",
  "message": "Payout method added successfully"
}
```

---

### ?? Method Type 2: PayPal
**`methodType: "PayPal"`**

**Required Fields:**
```json
{
  "methodType": "PayPal",
  "isDefault": false,
  "payPalEmail": "user@example.com"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `methodType` | string | ? Yes | "PayPal" |
| `payPalEmail` | string | ? Yes | PayPal account email |
| `isDefault` | boolean | ? No | Set as default method (default: false) |

**Example Request:**
```http
POST /api/payouts/methods
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "methodType": "PayPal",
  "payPalEmail": "alice.johnson@example.com",
  "isDefault": false
}
```

**Response:** `201 Created`
```json
{
  "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
  "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
  "methodType": "PayPal",
  "isDefault": false,
  "isVerified": false,
  "createdAt": "2024-12-20T10:35:00Z",
  "message": "Payout method added successfully"
}
```

---

### ?? Method Type 3: Cryptocurrency (Solana/Base)
**`methodType: "Crypto"` or `"Solana"` or `"Base"`**

**Required Fields:**
```json
{
  "methodType": "Solana",
  "isDefault": false,
  "walletAddress": "7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU",
  "network": "solana-mainnet",
  "currency": "USDC"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `methodType` | string | ? Yes | "Solana" or "Base" or "Crypto" |
| `walletAddress` | string | ? Yes | Blockchain wallet address |
| `network` | string | ? Yes | Network identifier (e.g., "solana-mainnet", "base-mainnet") |
| `currency` | string | ? Yes | Cryptocurrency (e.g., "USDC", "EURC") |
| `isDefault` | boolean | ? No | Set as default method (default: false) |

**Supported Networks:**
- **Solana:** `"solana-mainnet"`, `"solana-devnet"`
- **Base:** `"base-mainnet"`, `"base-testnet"`

**Supported Currencies:**
- `"USDC"` - USD Coin
- `"EURC"` - Euro Coin
- `"SOL"` - Solana (native)
- `"ETH"` - Ethereum (on Base)

**Example Request (Solana):**
```http
POST /api/payouts/methods
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "methodType": "Solana",
  "walletAddress": "7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU",
  "network": "solana-mainnet",
  "currency": "USDC",
  "isDefault": false
}
```

**Example Request (Base):**
```http
POST /api/payouts/methods
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "methodType": "Base",
  "walletAddress": "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb",
  "network": "base-mainnet",
  "currency": "USDC",
  "isDefault": true
}
```

**Response:** `201 Created`
```json
{
  "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
  "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
  "methodType": "Solana",
  "isDefault": false,
  "isVerified": false,
  "createdAt": "2024-12-20T10:40:00Z",
  "message": "Payout method added successfully"
}
```

---

## Other Endpoints

### 2. Get All Payment Methods
Retrieve all payment methods for the authenticated user.

**Endpoint:** `GET /api/payouts/methods`

**Authentication:** Required

**Example Request:**
```http
GET /api/payouts/methods
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:** `200 OK`
```json
{
  "methods": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
      "methodType": "BankTransfer",
      "isDefault": true,
      "isVerified": true,
      "createdAt": "2024-12-15T10:30:00Z",
      "updatedAt": "2024-12-15T10:30:00Z",
      "accountHolderName": "Alice Johnson",
      "iban": "GB82WEST12345698******",
      "bic": "NWBKGB2L",
      "bankName": "NatWest"
    },
    {
      "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
      "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
      "methodType": "PayPal",
      "isDefault": false,
      "isVerified": true,
      "createdAt": "2024-12-18T14:20:00Z",
      "updatedAt": "2024-12-18T14:20:00Z",
      "payPalEmail": "alice.******@example.com"
    },
    {
      "id": "5fa85f64-5717-4562-b3fc-2c963f66afa8",
      "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
      "methodType": "Solana",
      "isDefault": false,
      "isVerified": false,
      "createdAt": "2024-12-20T10:40:00Z",
      "updatedAt": "2024-12-20T10:40:00Z",
      "walletAddress": "7xKXtg2...JosgAsU",
      "network": "solana-mainnet",
      "currency": "USDC"
    }
  ]
}
```

**Note:** Sensitive data is masked:
- IBAN: Last 6 digits hidden
- PayPal Email: Partially hidden
- Wallet Address: Shortened for display

---

### 3. Get Single Payment Method
Retrieve details of a specific payment method.

**Endpoint:** `GET /api/payouts/methods/{methodId}`

**Authentication:** Required

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `methodId` | GUID | Yes | Payment method ID |

**Example Request:**
```http
GET /api/payouts/methods/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:** `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
  "methodType": "BankTransfer",
  "isDefault": true,
  "isVerified": true,
  "createdAt": "2024-12-15T10:30:00Z",
  "updatedAt": "2024-12-15T10:30:00Z",
  "accountHolderName": "Alice Johnson",
  "iban": "GB82WEST12345698******",
  "bic": "NWBKGB2L",
  "bankName": "NatWest"
}
```

**Error Responses:**
- `404 Not Found` - Payment method doesn't exist or doesn't belong to user

---

### 4. Update Payment Method
Update a payment method (primarily to set as default).

**Endpoint:** `PATCH /api/payouts/methods/{methodId}`

**Authentication:** Required

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `methodId` | GUID | Yes | Payment method ID |

**Request Body:**
```json
{
  "isDefault": true
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `isDefault` | boolean? | No | Set as default payment method |

**Example Request:**
```http
PATCH /api/payouts/methods/4fa85f64-5717-4562-b3fc-2c963f66afa7
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "isDefault": true
}
```

**Response:** `200 OK`
```json
{
  "id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
  "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
  "methodType": "PayPal",
  "isDefault": true,
  "isVerified": true,
  "updatedAt": "2024-12-20T11:00:00Z",
  "message": "Payout method updated successfully"
}
```

**?? Note:** Setting a payment method as default will automatically unset any other default method.

---

### 5. Delete Payment Method
Remove a payment method.

**Endpoint:** `DELETE /api/payouts/methods/{methodId}`

**Authentication:** Required

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `methodId` | GUID | Yes | Payment method ID |

**Example Request:**
```http
DELETE /api/payouts/methods/5fa85f64-5717-4562-b3fc-2c963f66afa8
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:** `200 OK`
```json
{
  "message": "Payout method deleted successfully"
}
```

**Error Responses:**
- `404 Not Found` - Payment method doesn't exist
- `400 Bad Request` - Cannot delete (e.g., pending payouts using this method)

---

### 6. Get Payout Balance
Get your current payout balance and earnings summary.

**Endpoint:** `GET /api/payouts/balance`

**Authentication:** Required

**Example Request:**
```http
GET /api/payouts/balance
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:** `200 OK`
```json
{
  "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
  "availableBalance": 125.50,
  "pendingBalance": 50.00,
  "totalEarned": 500.00,
  "totalPaidOut": 324.50,
  "currency": "USD",
  "minimumPayout": 25.00,
  "nextPayoutDate": "2024-12-31T00:00:00Z",
  "lastPayoutDate": "2024-11-30T15:30:00Z",
  "lastPayoutAmount": 200.00
}
```

---

### 7. Get Payout History
Get your payout transaction history.

**Endpoint:** `GET /api/payouts/history`

**Authentication:** Required

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `page` | int | No | 1 | Page number |
| `pageSize` | int | No | 20 | Items per page (max: 100) |
| `status` | string? | No | null | Filter by status: "pending", "processing", "completed", "failed" |

**Example Request:**
```http
GET /api/payouts/history?page=1&pageSize=10&status=completed
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:** `200 OK`
```json
{
  "page": 1,
  "pageSize": 10,
  "totalCount": 25,
  "items": [
    {
      "id": "7fa85f64-5717-4562-b3fc-2c963f66afa9",
      "userId": "1fa85f64-5717-4562-b3fc-2c963f66afa8",
      "amount": 200.00,
      "currency": "USD",
      "status": "completed",
      "methodType": "BankTransfer",
      "requestedAt": "2024-11-28T10:00:00Z",
      "processedAt": "2024-11-30T15:30:00Z",
      "completedAt": "2024-12-01T09:00:00Z",
      "providerTransactionId": "WISE-12345678"
    }
  ]
}
```

---

## Validation Rules

### Bank Transfer (IBAN/SEPA)
- ? IBAN must be valid format (checked with validator)
- ? Account holder name: 2-100 characters
- ? BIC/SWIFT: 8 or 11 characters (if provided)

### PayPal
- ? Email must be valid format
- ? Email must be a PayPal-registered account

### Cryptocurrency
- ? Wallet address must be valid for the specified network
- ? Solana addresses: Base58, 32-44 characters
- ? Base/Ethereum addresses: 0x + 40 hex characters
- ? Network must match wallet address type
- ? Currency must be supported on the network

---

## Error Responses

### 400 Bad Request
```json
{
  "message": "Invalid IBAN format"
}
```

### 401 Unauthorized
```json
{
  "message": "Authentication required",
  "code": "UNAUTHORIZED"
}
```

### 404 Not Found
```json
{
  "message": "Payout method not found"
}
```

### 500 Internal Server Error
```json
{
  "message": "Internal server error"
}
```

---

## Complete Workflow Example

### Add Payment Method and Check Balance

```javascript
// 1. Login to get token
const loginResp = await fetch('https://localhost:7297/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password123'
  })
});
const { token } = await loginResp.json();

// 2. Check current balance
const balanceResp = await fetch('https://localhost:7297/api/payouts/balance', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const balance = await balanceResp.json();
console.log('Available balance:', balance.availableBalance);

// 3. Add bank transfer method
const methodResp = await fetch('https://localhost:7297/api/payouts/methods', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    methodType: 'BankTransfer',
    accountHolderName: 'John Doe',
    iban: 'GB82WEST12345698765432',
    bic: 'NWBKGB2L',
    bankName: 'NatWest',
    isDefault: true
  })
});
const method = await methodResp.json();
console.log('Method added:', method.id);

// 4. Get all payment methods
const methodsResp = await fetch('https://localhost:7297/api/payouts/methods', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const { methods } = await methodsResp.json();
console.log('Total methods:', methods.length);

// 5. Get payout history
const historyResp = await fetch('https://localhost:7297/api/payouts/history?page=1&pageSize=10', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const history = await historyResp.json();
console.log('Total payouts:', history.totalCount);
```

---

## Security Notes

?? **Data Protection:**
- Sensitive fields (IBAN, wallet addresses) are masked in responses
- Payment credentials are encrypted in the database
- SSL/TLS required for all requests

?? **Verification:**
- New payment methods require verification before first payout
- Verification may involve micro-deposits or email confirmation
- Status shown in `isVerified` field

?? **Payout Rules:**
- Minimum payout threshold: $25 (configurable)
- Payouts processed monthly (typically last day of month)
- Default payment method used if specified, otherwise first verified method

---

## Payment Method Type Enum

```csharp
public enum PayoutMethodType
{
    BankTransfer,  // IBAN/SEPA
    PayPal,        // PayPal email
    Wise,          // Wise (TransferWise)
    Solana,        // Solana blockchain
    Base,          // Base/Ethereum L2
    Crypto         // Generic crypto
}
```

Use the string representation (e.g., `"BankTransfer"`) in API requests.

---

## FAQ

**Q: Can I have multiple payment methods?**  
A: Yes, you can add multiple methods of the same or different types.

**Q: Which method will be used for payouts?**  
A: The default method (where `isDefault: true`) is used. If no default is set, the first verified method is used.

**Q: How long does verification take?**  
A: Bank transfers: 1-2 business days, PayPal: instant if account exists, Crypto: instant (address validation).

**Q: Can I change my default payment method?**  
A: Yes, use `PATCH /api/payouts/methods/{methodId}` with `isDefault: true`.

**Q: What happens if a payout fails?**  
A: The amount returns to your available balance and you'll receive a notification. Check your payment method details.

---

*For support or questions, contact support@wihngo.com*
