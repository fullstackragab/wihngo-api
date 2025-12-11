/**
 * TypeScript Type Definitions for Wihngo Crypto Payment API
 * 
 * Usage:
 * 1. Copy this file to your React Native project: src/types/cryptoPayment.ts
 * 2. Import types: import { PaymentRequest, PaymentResponse } from '@/types/cryptoPayment';
 * 3. Use with your API calls for full type safety
 */

// ============================================================================
// NETWORK & CURRENCY TYPES
// ============================================================================

export type CryptoCurrency = 'USDT' | 'USDC' | 'TRX' | 'ETH' | 'BNB' | 'BTC';

export type CryptoNetwork = 
  | 'tron' 
  | 'ethereum' 
  | 'binance-smart-chain' 
  | 'polygon' 
  | 'bitcoin';

export type PaymentStatus = 
  | 'pending' 
  | 'confirming' 
  | 'confirmed' 
  | 'completed' 
  | 'expired' 
  | 'cancelled' 
  | 'failed';

export type PaymentPurpose = 
  | 'premium_subscription' 
  | 'bird_listing' 
  | 'donation' 
  | 'other';

export type SubscriptionPlan = 
  | 'monthly' 
  | 'yearly' 
  | 'lifetime';

// ============================================================================
// API REQUEST TYPES
// ============================================================================

/**
 * Request body for creating a new crypto payment
 */
export interface CreatePaymentRequest {
  /** Amount in USD */
  amountUsd: number;
  /** Cryptocurrency to accept (e.g., 'USDT') */
  currency: CryptoCurrency;
  /** Blockchain network (e.g., 'tron', 'ethereum', 'binance-smart-chain') */
  network: CryptoNetwork;
  /** Optional: Bird ID if payment is for a specific bird */
  birdId?: string;
  /** Purpose of the payment */
  purpose: PaymentPurpose;
  /** Optional: Subscription plan if purpose is premium_subscription */
  plan?: SubscriptionPlan;
}

/**
 * Request body for verifying a payment with transaction hash
 */
export interface VerifyPaymentRequest {
  /** Blockchain transaction hash */
  transactionHash: string;
  /** Optional: User's wallet address */
  userWalletAddress?: string;
}

// ============================================================================
// API RESPONSE TYPES
// ============================================================================

/**
 * Complete payment details returned by the API
 */
export interface PaymentResponse {
  /** Unique payment request ID */
  id: string;
  /** User ID who created the payment */
  userId: string;
  /** Optional: Associated bird ID */
  birdId?: string;
  /** Amount in USD */
  amountUsd: number;
  /** Amount in cryptocurrency */
  amountCrypto: number;
  /** Cryptocurrency */
  currency: CryptoCurrency;
  /** Blockchain network */
  network: CryptoNetwork;
  /** Exchange rate used (crypto per USD) */
  exchangeRate: number;
  /** Platform wallet address to send payment to */
  walletAddress: string;
  /** Optional: User's wallet address */
  userWalletAddress?: string;
  /** QR code data (usually same as walletAddress) */
  qrCodeData: string;
  /** Payment URI for mobile wallets */
  paymentUri: string;
  /** Optional: Blockchain transaction hash */
  transactionHash?: string;
  /** Current number of confirmations */
  confirmations: number;
  /** Required number of confirmations */
  requiredConfirmations: number;
  /** Current payment status */
  status: PaymentStatus;
  /** Purpose of payment */
  purpose: PaymentPurpose;
  /** Optional: Subscription plan */
  plan?: SubscriptionPlan;
  /** Payment expiration time (ISO 8601) */
  expiresAt: string;
  /** Optional: Time when payment was confirmed (ISO 8601) */
  confirmedAt?: string;
  /** Optional: Time when payment was completed (ISO 8601) */
  completedAt?: string;
  /** Payment creation time (ISO 8601) */
  createdAt: string;
  /** Last update time (ISO 8601) */
  updatedAt: string;
}

/**
 * Response when creating a new payment
 */
export interface CreatePaymentResponse {
  paymentRequest: PaymentResponse;
  message: string;
}

/**
 * Exchange rate information
 */
export interface ExchangeRate {
  /** Currency code */
  currency: CryptoCurrency;
  /** Rate in USD (1 currency = X USD) */
  usdRate: number;
  /** Last update time (ISO 8601) */
  lastUpdated: string;
  /** Rate source (e.g., 'CoinGecko') */
  source: string;
}

/**
 * Platform wallet information
 */
export interface PlatformWallet {
  /** Currency this wallet accepts */
  currency: CryptoCurrency;
  /** Blockchain network */
  network: CryptoNetwork;
  /** Wallet address */
  address: string;
  /** QR code data */
  qrCode: string;
  /** Whether wallet is currently active */
  isActive: boolean;
}

/**
 * Payment history response
 */
export interface PaymentHistoryResponse {
  /** Array of payments */
  payments: PaymentResponse[];
  /** Current page number */
  page: number;
  /** Number of items per page */
  pageSize: number;
  /** Total number of payments */
  total: number;
}

/**
 * Cancel payment response
 */
export interface CancelPaymentResponse {
  /** Payment ID */
  id: string;
  /** New status (should be 'cancelled') */
  status: PaymentStatus;
  /** Success message */
  message: string;
}

// ============================================================================
// ERROR RESPONSE TYPES
// ============================================================================

/**
 * Standard error response from API
 */
export interface ApiErrorResponse {
  error: string;
  message?: string;
  errors?: Record<string, string[]>;
}

// ============================================================================
// NETWORK CONFIGURATION
// ============================================================================

/**
 * Configuration for each supported network
 */
export interface NetworkConfig {
  id: CryptoNetwork;
  name: string;
  displayName: string;
  decimals: number;
  requiredConfirmations: number;
  averageConfirmationTime: string;
  estimatedFee: 'low' | 'medium' | 'high';
  icon?: string;
  blockExplorer?: string;
}

/**
 * Predefined network configurations
 */
export const NETWORK_CONFIGS: Record<CryptoNetwork, NetworkConfig> = {
  tron: {
    id: 'tron',
    name: 'Tron',
    displayName: 'Tron (TRC-20)',
    decimals: 6,
    requiredConfirmations: 19,
    averageConfirmationTime: '~57 seconds',
    estimatedFee: 'low',
    blockExplorer: 'https://tronscan.org/#/transaction/',
  },
  ethereum: {
    id: 'ethereum',
    name: 'Ethereum',
    displayName: 'Ethereum (ERC-20)',
    decimals: 6,
    requiredConfirmations: 12,
    averageConfirmationTime: '~2.4 minutes',
    estimatedFee: 'high',
    blockExplorer: 'https://etherscan.io/tx/',
  },
  'binance-smart-chain': {
    id: 'binance-smart-chain',
    name: 'BSC',
    displayName: 'Binance Smart Chain (BEP-20)',
    decimals: 18, // IMPORTANT: BSC uses 18 decimals!
    requiredConfirmations: 15,
    averageConfirmationTime: '~45 seconds',
    estimatedFee: 'medium',
    blockExplorer: 'https://bscscan.com/tx/',
  },
  polygon: {
    id: 'polygon',
    name: 'Polygon',
    displayName: 'Polygon (MATIC)',
    decimals: 6,
    requiredConfirmations: 128,
    averageConfirmationTime: '~4 minutes',
    estimatedFee: 'low',
    blockExplorer: 'https://polygonscan.com/tx/',
  },
  bitcoin: {
    id: 'bitcoin',
    name: 'Bitcoin',
    displayName: 'Bitcoin (BTC)',
    decimals: 8,
    requiredConfirmations: 2,
    averageConfirmationTime: '~20 minutes',
    estimatedFee: 'high',
    blockExplorer: 'https://blockchain.com/btc/tx/',
  },
};

// ============================================================================
// UTILITY TYPES
// ============================================================================

/**
 * Helper type for payment state management
 */
export interface PaymentState {
  payment: PaymentResponse | null;
  loading: boolean;
  error: string | null;
  isPolling: boolean;
}

/**
 * Helper type for timer countdown
 */
export interface PaymentTimer {
  timeRemaining: string;
  isExpired: boolean;
  percentRemaining: number;
}

// ============================================================================
// API CLIENT CONFIGURATION
// ============================================================================

/**
 * Configuration for the crypto payment API client
 */
export interface CryptoPaymentApiConfig {
  baseUrl: string;
  authToken: string;
  timeout?: number;
}

/**
 * Options for payment polling
 */
export interface PollingOptions {
  interval?: number; // milliseconds
  maxAttempts?: number;
  onStatusChange?: (payment: PaymentResponse) => void;
  onComplete?: (payment: PaymentResponse) => void;
  onError?: (error: Error) => void;
}

// ============================================================================
// HELPER FUNCTIONS TYPE DEFINITIONS
// ============================================================================

/**
 * Format crypto amount based on network decimals
 */
export type FormatAmountFunction = (
  amount: number,
  network: CryptoNetwork
) => string;

/**
 * Calculate time remaining until expiration
 */
export type CalculateTimeRemainingFunction = (
  expiresAt: string
) => PaymentTimer;

/**
 * Validate transaction hash format
 */
export type ValidateTransactionHashFunction = (
  hash: string,
  network: CryptoNetwork
) => boolean;

/**
 * Get block explorer URL for transaction
 */
export type GetBlockExplorerUrlFunction = (
  txHash: string,
  network: CryptoNetwork
) => string;

// ============================================================================
// REACT HOOK RETURN TYPES
// ============================================================================

/**
 * Return type for useCryptoPayment hook
 */
export interface UseCryptoPaymentReturn {
  payment: PaymentResponse | null;
  loading: boolean;
  error: string | null;
  createPayment: (request: CreatePaymentRequest) => Promise<PaymentResponse>;
  verifyPayment: (paymentId: string, txHash: string, walletAddress?: string) => Promise<PaymentResponse>;
  checkStatus: (paymentId: string) => Promise<PaymentResponse>;
  cancelPayment: (paymentId: string) => Promise<CancelPaymentResponse>;
  startPolling: (paymentId: string, options?: PollingOptions) => void;
  stopPolling: () => void;
}

/**
 * Return type for usePaymentTimer hook
 */
export interface UsePaymentTimerReturn {
  timeRemaining: string;
  isExpired: boolean;
  percentRemaining: number;
}

// ============================================================================
// EXAMPLE USAGE
// ============================================================================

/**
 * Example: Type-safe API call
 * 
 * const createPayment = async (): Promise<PaymentResponse> => {
 *   const request: CreatePaymentRequest = {
 *     amountUsd: 9.99,
 *     currency: 'USDT',
 *     network: 'tron',
 *     purpose: 'premium_subscription',
 *     plan: 'monthly'
 *   };
 *   
 *   const response = await api.post<CreatePaymentResponse>('/create', request);
 *   return response.data.paymentRequest;
 * };
 */

/**
 * Example: Type-safe component props
 * 
 * interface PaymentScreenProps {
 *   payment: PaymentResponse;
 *   onVerify: (txHash: string) => void;
 *   onCancel: () => void;
 * }
 * 
 * const PaymentScreen: React.FC<PaymentScreenProps> = ({ payment, onVerify, onCancel }) => {
 *   // TypeScript will enforce correct types
 * };
 */
