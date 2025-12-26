# ?? WIHNGO - RUN THIS NOW

## ? Quick Deploy Command

```bash
psql "postgresql://wingo:Uljqr7nYUqFtPtF84NlOxwO1Ae3IkUZQ@dpg-d4qm1iu3jp1c739jpt4g-a.oregon-postgres.render.com:5432/wihngo?sslmode=require" -f Database/migrations/EXECUTE_NOW_new_wallets.sql
```

## ? Verify (should show 10 rows)

```bash
psql "postgresql://wingo:Uljqr7nYUqFtPtF84NlOxwO1Ae3IkUZQ@dpg-d4qm1iu3jp1c739jpt4g-a.oregon-postgres.render.com:5432/wihngo?sslmode=require" -c "SELECT currency, network FROM platform_wallets WHERE is_active = TRUE ORDER BY currency, network;"
```

---

## ?? Your Addresses

| Network | Address |
|---------|---------|
| Solana | `AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn` |
| ETH/Polygon/Base | `0xfcc173a7569492439ec3df467d0ec0c05c0f541c` |
| Stellar | `GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG` |

---

## ?? Monitor

- Solana: https://solscan.io/account/AE6jndedpjoX2XLt4nFYGp3JEuHTseGh8EMDGRmADacn
- Ethereum: https://etherscan.io/address/0xfcc173a7569492439ec3df467d0ec0c05c0f541c
- Polygon: https://polygonscan.com/address/0xfcc173a7569492439ec3df467d0ec0c05c0f541c
- Base: https://basescan.org/address/0xfcc173a7569492439ec3df467d0ec0c05c0f541c
- Stellar: https://stellar.expert/explorer/public/account/GDMOOMFEDZJR6UOW6O7FRF4MGKNRVFVK4Q336U5KNXNYH532TFYJC4HG

---

## ?? Test Payment

```bash
curl -X POST https://your-api.com/api/payments/crypto/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"amountUsd":10,"currency":"USDC","network":"solana","purpose":"premium_subscription","plan":"monthly"}'
```

---

## ?? What You Support

- ? **USDC** on: Solana, Ethereum, Polygon, Base, Stellar
- ? **EURC** on: Solana, Ethereum, Polygon, Base, Stellar
- ? **Total:** 10 combinations

---

## ?? Recommended for Users

1. ?? **Stellar** - Cheapest (<$0.001) + Fastest (5 sec)
2. ?? **Solana** - Very cheap ($0.001) + Fast (30 sec)
3. ?? **Polygon** - Low cost ($0.01-0.10) + Good speed (2 min)

---

**Status:** ? Ready  
**Action:** ?? Run migration command above
