# Entity Framework Cleanup Script
# This document lists all files that need the "using Microsoft.EntityFrameworkCore;" statement removed

## Files to Update

### Controllers
- Controllers\NotificationsController.cs
- Controllers\BirdsController.cs
- Controllers\CryptoPaymentController.cs
- Controllers\PaymentsController.cs
- Controllers\WebhooksController.cs
- Controllers\DevController.cs
- Controllers\StoriesController.cs
- Controllers\SupportTransactionsController.cs

### Services
- Services\EvmBlockchainMonitor.cs
- Services\CryptoPaymentService.cs
- Services\RefundService.cs
- Services\StellarBlockchainMonitor.cs
- Services\OnChainDepositBackgroundService.cs
- Services\EmailNotificationService.cs
- Services\PayPalService.cs
- Services\NotificationService.cs
- Services\PremiumSubscriptionService.cs
- Services\CharityService.cs
- Services\SolanaBlockchainMonitor.cs
- Services\EvmListenerService.cs
- Services\OnChainDepositService.cs
- Services\PushNotificationService.cs
- Services\PaymentAuditService.cs
- Services\SolanaListenerService.cs
- Services\InvoiceService.cs

### Background Jobs
- BackgroundJobs\PremiumExpiryNotificationJob.cs
- BackgroundJobs\ReconciliationJob.cs
- BackgroundJobs\NotificationCleanupJob.cs
- BackgroundJobs\CharityAllocationJob.cs
- BackgroundJobs\ExchangeRateUpdateJob.cs
- BackgroundJobs\DailyDigestJob.cs
- BackgroundJobs\PaymentMonitorJob.cs

### Database
- Database\DatabaseSeeder.cs

## Action Required

All these files need to have the line:
```csharp
using Microsoft.EntityFrameworkCore;
```

removed or commented out. The AppDbContext stub will allow compilation without EF.
