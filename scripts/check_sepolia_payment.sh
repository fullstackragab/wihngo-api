#!/bin/bash
# ====================================================
# SEPOLIA PAYMENT STATUS CHECKER
# ====================================================
# Quick script to check if your Sepolia payment completed

echo "?? CHECKING SEPOLIA PAYMENT STATUS..."
echo "======================================"
echo ""

# Configuration
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="wihngo"
DB_USER="postgres"
# DB_PASSWORD="postgres"  # Set via PGPASSWORD environment variable

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to run SQL query
query() {
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -A -c "$1"
}

# 1. Get latest Sepolia payment
echo "?? Latest Sepolia Payments:"
echo "----------------------------"
LATEST=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" << EOF
SELECT 
    id,
    status,
    transaction_hash,
    confirmations || '/' || required_confirmations AS confirmations,
    amount_crypto || ' ' || currency AS amount,
    to_char(created_at, 'HH24:MI:SS') AS time
FROM crypto_payment_requests
WHERE network = 'sepolia'
ORDER BY created_at DESC
LIMIT 5;
EOF
)

echo "$LATEST"
echo ""

# 2. Check for stuck payments
echo "??  Checking for stuck payments..."
STUCK=$(query "SELECT COUNT(*) FROM crypto_payment_requests WHERE network = 'sepolia' AND status = 'confirmed' AND completed_at IS NULL;")

if [ "$STUCK" -gt 0 ]; then
    echo -e "${YELLOW}Found $STUCK payment(s) stuck in 'confirmed' status!${NC}"
    echo "These need the PaymentMonitorJob to complete them."
    echo ""
    
    # Show stuck payments
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" << EOF
SELECT 
    id,
    status,
    confirmations || '/' || required_confirmations AS confirmations,
    to_char(NOW() - confirmed_at, 'MI:SS') AS stuck_for
FROM crypto_payment_requests
WHERE network = 'sepolia' 
  AND status = 'confirmed' 
  AND completed_at IS NULL;
EOF
    echo ""
else
    echo -e "${GREEN}? No stuck payments${NC}"
    echo ""
fi

# 3. Check for completed payments
COMPLETED=$(query "SELECT COUNT(*) FROM crypto_payment_requests WHERE network = 'sepolia' AND status = 'completed';")
echo -e "? Completed Sepolia Payments: ${GREEN}$COMPLETED${NC}"

# 4. Check for confirming payments
CONFIRMING=$(query "SELECT COUNT(*) FROM crypto_payment_requests WHERE network = 'sepolia' AND status = 'confirming';")
if [ "$CONFIRMING" -gt 0 ]; then
    echo -e "? Confirming Payments: ${YELLOW}$CONFIRMING${NC}"
fi

# 5. Check for pending payments
PENDING=$(query "SELECT COUNT(*) FROM crypto_payment_requests WHERE network = 'sepolia' AND status = 'pending';")
if [ "$PENDING" -gt 0 ]; then
    echo -e "??  Pending Payments: ${BLUE}$PENDING${NC}"
fi

echo ""
echo "======================================"
echo "?? Quick Links:"
echo "  - Hangfire Dashboard: http://localhost:5000/hangfire"
echo "  - Sepolia Etherscan: https://sepolia.etherscan.io"
echo ""

# Get latest payment ID for manual check
LATEST_ID=$(query "SELECT id FROM crypto_payment_requests WHERE network = 'sepolia' ORDER BY created_at DESC LIMIT 1;")
if [ ! -z "$LATEST_ID" ]; then
    echo "?? Latest Payment ID: $LATEST_ID"
    echo "   Manual Check API: POST /api/payments/crypto/$LATEST_ID/check-status"
    echo ""
fi

echo "?? TIP: PaymentMonitorJob runs every 30 seconds."
echo "   Wait at least 30 seconds for automatic processing."
