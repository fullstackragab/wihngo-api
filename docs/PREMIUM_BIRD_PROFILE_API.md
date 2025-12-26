# Premium Bird Profile System - Mobile API Documentation

## Overview

The Premium Bird Profile system allows bird owners to celebrate and showcase their birds with enhanced features while contributing to bird charities. This document provides complete integration details for the mobile development team.

---

## ?? Philosophy & Features

### Core Principles
- **Love-First, Not Money-First**: Premium enhances celebration, doesn't restrict basics
- **Community-Driven**: Free users maintain full community access
- **Charity Integration**: 10-20% of premium revenue supports bird charities
- **Emotional Connection**: Focus on storytelling and memorable moments

### Premium vs Free Features

| Feature | Free Profile | Premium Profile |
|---------|-------------|-----------------|
| Basic bird page | ? | ? |
| Photos | Up to 5 | Unlimited |
| Videos | Up to 5 | Unlimited |
| Story posts | ? | ? |
| Comments & likes | ? | ? |
| Custom theme/cover | ? | ? |
| Premium badge | ? | ? ("Celebrated Bird") |
| Story highlights | ? | ? (Pin up to 5) |
| Memory collages | ? | ? |
| QR code sharing | ? | ? |
| Custom profile borders | ? | ? (Gold/Silver/Rainbow) |
| Donation tracker | Basic | Enhanced |

---

## ?? Table of Contents

1. [Authentication](#authentication)
2. [Premium Plans](#premium-plans)
3. [Subscription Management](#subscription-management)
4. [Premium Styling](#premium-styling)
5. [Charity Impact](#charity-impact)
6. [Integration Examples](#integration-examples)
7. [Error Handling](#error-handling)
8. [Testing Guide](#testing-guide)

---

## Authentication

All authenticated endpoints require a valid JWT token in the `Authorization` header:

```
Authorization: Bearer YOUR_JWT_TOKEN
```

### User Permissions
- Only bird **owners** can subscribe/cancel premium
- Only bird **owners** can update premium styles
- Anyone can view premium status and styles (public endpoints)

---

## Premium Plans

### Get Available Plans

Retrieve all available premium subscription plans.

**Endpoint:** `GET /api/premium/plans`

**Authentication:** Optional (public endpoint)

**Response:** `200 OK`

```json
[
  {
    "id": "monthly",
    "name": "Monthly Celebration",
    "price": 3.99,
    "currency": "USD",
    "interval": "month",
    "description": "Show your love & support your bird monthly",
    "charityAllocation": 10,
    "savings": null,
    "features": [
      "Custom profile theme & cover",
      "Highlighted Best Moments",
      "'Celebrated Bird' badge",
      "Unlimited photos & videos",
      "Memory collages & story albums",
      "QR code for profile sharing",
      "Pin up to 5 story highlights",
      "Donation tracker display"
    ]
  },
  {
    "id": "yearly",
    "name": "Yearly Celebration",
    "price": 39.99,
    "currency": "USD",
    "interval": "year",
    "description": "A year of love & celebration for your bird",
    "charityAllocation": 15,
    "savings": "Save $8/year - 2 months free!",
    "features": [
      "All Monthly features included",
      "Custom profile theme & cover",
      "Highlighted Best Moments",
      "'Celebrated Bird' badge",
      "Unlimited photos & videos",
      "Memory collages & story albums",
      "QR code for profile sharing",
      "Pin up to 5 story highlights",
      "Donation tracker display",
      "Priority support"
    ]
  },
  {
    "id": "lifetime",
    "name": "Lifetime Celebration",
    "price": 69.99,
    "currency": "USD",
    "interval": "lifetime",
    "description": "Eternal love & premium features for your bird",
    "charityAllocation": 20,
    "savings": "One-time payment, celebrate forever!",
    "features": [
      "All premium features forever",
      "Custom profile theme & cover",
      "Highlighted Best Moments",
      "'Celebrated Bird' badge",
      "Unlimited photos & videos",
      "Memory collages & story albums",
      "QR code for profile sharing",
      "Pin up to 5 story highlights",
      "Donation tracker display",
      "Exclusive lifetime badge",
      "Support bird charities",
      "VIP support access"
    ]
  }
]
```

### Plan Details

| Plan ID | Price | Interval | Charity % | Best For |
|---------|-------|----------|-----------|----------|
| `monthly` | $3.99 | month | 10% | Try before committing |
| `yearly` | $39.99 | year | 15% | Regular users (2 months free) |
| `lifetime` | $69.99 | one-time | 20% | Forever celebration |

---

## Subscription Management

### 1. Subscribe Bird to Premium

Create a new premium subscription for a bird.

**Endpoint:** `POST /api/premium/subscribe`

**Authentication:** Required (must be bird owner)

**Request Body:**

```json
{
  "birdId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "provider": "stripe",
  "plan": "monthly",
  "paymentMethodId": "pm_1234567890",
  "cryptoCurrency": null,
  "cryptoNetwork": null
}
```

**Field Descriptions:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `birdId` | UUID | Yes | ID of the bird to subscribe |
| `provider` | string | Yes | Payment provider: `stripe`, `apple`, `google`, `crypto` |
| `plan` | string | Yes | Plan ID: `monthly`, `yearly`, `lifetime` |
| `paymentMethodId` | string | No | Payment method ID from provider |
| `cryptoCurrency` | string | No | For crypto: `USDC`, `EURC`, etc. |
| `cryptoNetwork` | string | No | For crypto: `solana`, `base`, etc. |

**Response:** `200 OK`

```json
{
  "subscriptionId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
  "status": "active",
  "plan": "monthly",
  "currentPeriodEnd": "2024-02-15T10:30:00Z",
  "message": "Subscription activated successfully"
}
```

**Error Responses:**

- `400 Bad Request`: Invalid request data or bird doesn't exist
- `401 Unauthorized`: User is not the bird owner
- `409 Conflict`: Bird already has an active subscription

---

### 2. Get Premium Status

Check if a bird has premium status and get subscription details.

**Endpoint:** `GET /api/premium/status/{birdId}`

**Authentication:** Optional (public endpoint)

**Parameters:**
- `birdId` (path, required): UUID of the bird

**Response:** `200 OK`

**If premium:**
```json
{
  "isPremium": true,
  "subscription": {
    "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    "birdId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "ownerId": "c3d4e5f6-a7b8-9012-cdef-123456789012",
    "status": "active",
    "plan": "monthly",
    "provider": "stripe",
    "providerSubscriptionId": "sub_1234567890",
    "startedAt": "2024-01-15T10:30:00Z",
    "currentPeriodEnd": "2024-02-15T10:30:00Z",
    "canceledAt": null,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  }
}
```

**If not premium:**
```json
{
  "isPremium": false,
  "subscription": null
}
```

---

### 3. Cancel Subscription

Cancel a bird's premium subscription (ends at current period).

**Endpoint:** `POST /api/premium/cancel/{birdId}`

**Authentication:** Required (must be bird owner)

**Parameters:**
- `birdId` (path, required): UUID of the bird

**Response:** `200 OK`

```json
{
  "message": "Subscription canceled successfully"
}
```

**Behavior:**
- Subscription remains active until `currentPeriodEnd`
- After expiry, premium features are disabled
- User can resubscribe anytime

**Error Responses:**
- `401 Unauthorized`: User is not the bird owner
- `404 Not Found`: No active subscription found

---

## Premium Styling

### 1. Update Premium Style

Customize the premium visual appearance of a bird profile.

**Endpoint:** `PUT /api/premium/style/{birdId}`

**Authentication:** Required (must be bird owner with active premium)

**Parameters:**
- `birdId` (path, required): UUID of the bird

**Request Body:**

```json
{
  "frameId": "gold",
  "badgeId": "star",
  "highlightColor": "#FFD700",
  "themeId": "sunset",
  "coverImageUrl": "birds/covers/abc123.jpg"
}
```

**Field Descriptions:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `frameId` | string | No | Profile border: `gold`, `silver`, `rainbow`, `rose` |
| `badgeId` | string | No | Premium badge: `star`, `heart`, `crown`, `diamond` |
| `highlightColor` | string | No | Hex color for highlights (e.g., `#FFD700`) |
| `themeId` | string | No | Theme preset: `sunset`, `ocean`, `forest`, `lavender` |
| `coverImageUrl` | string | No | S3 key for custom cover image |

**Available Options:**

**Frame IDs:**
- `gold` - Golden border with subtle shine
- `silver` - Silver metallic border
- `rainbow` - Multi-colored gradient border
- `rose` - Rose gold elegant border

**Badge IDs:**
- `star` - Star "Celebrated Bird" badge
- `heart` - Heart "Loved Bird" badge
- `crown` - Crown "Premium Bird" badge
- `diamond` - Diamond "Lifetime Member" badge

**Theme IDs:**
- `sunset` - Warm orange/pink tones
- `ocean` - Cool blue/teal tones
- `forest` - Natural green tones
- `lavender` - Soft purple tones

**Response:** `200 OK`

```json
{
  "id": "d4e5f6a7-b8c9-0123-def4-56789abcdef0",
  "birdId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "frameId": "gold",
  "badgeId": "star",
  "highlightColor": "#FFD700",
  "themeId": "sunset",
  "coverImageUrl": "birds/covers/abc123.jpg",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T11:00:00Z"
}
```

**Error Responses:**
- `401 Unauthorized`: User is not the bird owner or bird is not premium
- `400 Bad Request`: Invalid style options

---

### 2. Get Premium Style

Retrieve the premium styling for a bird profile.

**Endpoint:** `GET /api/premium/style/{birdId}`

**Authentication:** Optional (public endpoint)

**Parameters:**
- `birdId` (path, required): UUID of the bird

**Response:** `200 OK`

```json
{
  "id": "d4e5f6a7-b8c9-0123-def4-56789abcdef0",
  "birdId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "frameId": "gold",
  "badgeId": "star",
  "highlightColor": "#FFD700",
  "themeId": "sunset",
  "coverImageUrl": "birds/covers/abc123.jpg",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T11:00:00Z"
}
```

**Error Responses:**
- `404 Not Found`: Bird has no premium style configured

---

## Charity Impact

### 1. Get Bird Charity Impact

View how much a specific premium bird has contributed to charities.

**Endpoint:** `GET /api/charity/impact/{birdId}`

**Authentication:** Optional (public endpoint)

**Parameters:**
- `birdId` (path, required): UUID of the bird

**Response:** `200 OK`

```json
{
  "totalContributed": 12.50,
  "birdsHelped": 3,
  "sheltersSupported": 2,
  "conservationProjects": 1
}
```

**Field Descriptions:**
- `totalContributed`: Total USD contributed through this bird's premium subscription
- `birdsHelped`: Estimated number of birds helped
- `sheltersSupported`: Number of shelters supported
- `conservationProjects`: Number of conservation projects funded

---

### 2. Get Global Charity Impact

View the platform-wide charity impact from all premium subscriptions.

**Endpoint:** `GET /api/charity/impact/global`

**Authentication:** Optional (public endpoint)

**Response:** `200 OK`

```json
{
  "totalContributed": 15750.00,
  "totalSubscribers": 427,
  "birdsHelped": 1250,
  "sheltersSupported": 45,
  "conservationProjects": 12
}
```

**Display Suggestions:**
- Show in app footer or about page
- Create a "Community Impact" screen
- Include in premium upsell messaging

---

### 3. Get Charity Partners

List all charity organizations supported by Wihngo.

**Endpoint:** `GET /api/charity/partners`

**Authentication:** Optional (public endpoint)

**Response:** `200 OK`

```json
[
  {
    "name": "Local Bird Shelter Network",
    "description": "Rescue and rehabilitation services",
    "website": "https://birdshelternet.org"
  },
  {
    "name": "Avian Conservation Fund",
    "description": "Species protection and habitat preservation",
    "website": "https://avianconservation.org"
  },
  {
    "name": "Wildlife Rescue Alliance",
    "description": "Emergency veterinary care for birds",
    "website": "https://wildliferescue.org"
  }
]
```

---

## Integration Examples

### Example 1: Display Premium Badge on Bird Profile

```javascript
async function loadBirdProfile(birdId) {
  // Fetch bird details
  const bird = await fetch(`/api/birds/${birdId}`).then(r => r.json());
  
  // Check premium status
  const premiumStatus = await fetch(`/api/premium/status/${birdId}`)
    .then(r => r.json());
  
  // Display premium badge if applicable
  if (premiumStatus.isPremium) {
    const style = await fetch(`/api/premium/style/${birdId}`)
      .then(r => r.json());
    
    // Apply premium styling
    applyPremiumFrame(style.frameId);
    showPremiumBadge(style.badgeId);
    applyTheme(style.themeId, style.highlightColor);
    
    // Show subscription details
    const sub = premiumStatus.subscription;
    displaySubscriptionInfo(sub.plan, sub.currentPeriodEnd);
  }
}
```

---

### Example 2: Subscribe to Premium Flow

```javascript
async function subscribeToPremium(birdId, plan) {
  try {
    // 1. Get payment method from Stripe/Apple/Google
    const paymentMethod = await getPaymentMethod();
    
    // 2. Subscribe
    const response = await fetch('/api/premium/subscribe', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({
        birdId,
        provider: 'stripe',
        plan,
        paymentMethodId: paymentMethod.id
      })
    });
    
    if (response.ok) {
      const result = await response.json();
      
      // 3. Show success message
      showSuccessMessage(`Premium activated! Valid until ${result.currentPeriodEnd}`);
      
      // 4. Update UI
      enablePremiumFeatures(birdId);
      
      // 5. Show charity impact
      const impact = await fetch(`/api/charity/impact/${birdId}`)
        .then(r => r.json());
      showCharityImpact(impact);
    }
  } catch (error) {
    handleSubscriptionError(error);
  }
}
```

---

### Example 3: Customize Premium Style

```javascript
async function updatePremiumStyle(birdId, style) {
  const response = await fetch(`/api/premium/style/${birdId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      frameId: style.frame,
      badgeId: style.badge,
      themeId: style.theme,
      highlightColor: style.color
    })
  });
  
  if (response.ok) {
    const updatedStyle = await response.json();
    
    // Refresh profile with new style
    applyPremiumStyling(updatedStyle);
    showSuccessToast('Style updated!');
  }
}
```

---

### Example 4: Display Charity Impact

```javascript
async function showCharityImpactScreen() {
  // Get global impact
  const globalImpact = await fetch('/api/charity/impact/global')
    .then(r => r.json());
  
  // Get charity partners
  const partners = await fetch('/api/charity/partners')
    .then(r => r.json());
  
  // Display impact stats
  displayImpactStats({
    totalRaised: `$${globalImpact.totalContributed.toLocaleString()}`,
    subscribersCount: globalImpact.totalSubscribers,
    birdsHelped: globalImpact.birdsHelped,
    shelters: globalImpact.sheltersSupported,
    projects: globalImpact.conservationProjects
  });
  
  // List charity partners
  displayCharityPartners(partners);
}
```

---

### Example 5: Premium Upsell Flow

```javascript
async function showPremiumUpsell(birdId) {
  // Check if already premium
  const status = await fetch(`/api/premium/status/${birdId}`)
    .then(r => r.json());
  
  if (status.isPremium) {
    // Already premium, show manage subscription
    navigateToManageSubscription(birdId);
    return;
  }
  
  // Get available plans
  const plans = await fetch('/api/premium/plans')
    .then(r => r.json());
  
  // Show upsell screen with:
  // 1. Before/after comparison
  // 2. Plan options with pricing
  // 3. Charity impact messaging
  // 4. Feature highlights
  showPremiumModal({
    bird: await getBirdDetails(birdId),
    plans: plans,
    onSelectPlan: (plan) => subscribeToPremium(birdId, plan.id)
  });
}
```

---

## Error Handling

### Error Response Format

All error responses follow this structure:

```json
{
  "message": "Descriptive error message"
}
```

### Common Error Codes

| Status Code | Meaning | Common Causes |
|-------------|---------|---------------|
| `400 Bad Request` | Invalid request data | Missing required fields, invalid plan ID |
| `401 Unauthorized` | Authentication failed | No token, expired token, not bird owner |
| `403 Forbidden` | Permission denied | Bird is not premium, owner mismatch |
| `404 Not Found` | Resource not found | Bird doesn't exist, no subscription |
| `409 Conflict` | Resource conflict | Bird already has active subscription |
| `500 Internal Server Error` | Server error | Contact support |

### Error Handling Best Practices

```javascript
async function handleApiCall(endpoint, options) {
  try {
    const response = await fetch(endpoint, options);
    
    if (!response.ok) {
      const error = await response.json();
      
      switch (response.status) {
        case 400:
          showValidationError(error.message);
          break;
        case 401:
          // Token expired, refresh or re-login
          await refreshAuthToken();
          break;
        case 403:
          showPermissionError('You must own this bird');
          break;
        case 404:
          showNotFoundError('Bird not found');
          break;
        case 409:
          showConflictError('Bird already has premium');
          break;
        default:
          showGenericError('Something went wrong');
      }
      
      return null;
    }
    
    return await response.json();
  } catch (error) {
    console.error('API Error:', error);
    showNetworkError('Please check your internet connection');
    return null;
  }
}
```

---

## Testing Guide

### Test Accounts & Data

For development/testing, you can use these test scenarios:

**Test Bird IDs:**
- Premium Bird: `test-premium-bird-uuid`
- Free Bird: `test-free-bird-uuid`

**Test Payment Methods:**
- Stripe Test Card: `4242 4242 4242 4242`
- Expiry: Any future date
- CVC: Any 3 digits

### Testing Checklist

#### Premium Subscription Flow
- [ ] View available plans
- [ ] Subscribe to monthly plan
- [ ] Subscribe to yearly plan
- [ ] Subscribe to lifetime plan
- [ ] Verify premium status active
- [ ] Cancel subscription
- [ ] Verify cancellation scheduled

#### Premium Styling
- [ ] Apply gold frame
- [ ] Apply different badges
- [ ] Change theme
- [ ] Update highlight color
- [ ] Upload custom cover image
- [ ] View style as non-owner (public)

#### Charity Impact
- [ ] View bird-specific impact
- [ ] View global impact
- [ ] View charity partners
- [ ] Verify contribution calculations

#### Edge Cases
- [ ] Try to subscribe non-owned bird (should fail)
- [ ] Try to subscribe already premium bird (should fail)
- [ ] Try to style free bird (should fail)
- [ ] View premium status of non-existent bird
- [ ] Cancel non-existent subscription

---

## UI/UX Recommendations

### Premium Badge Display

```
???????????????????????????
?  [Bird Profile Image]   ?
?  ?????????????????????  ?
?  ?  ? Celebrated    ?  ? <- Premium badge overlay
?  ?     Bird          ?  ?
?  ?????????????????????  ?
???????????????????????????
```

### Premium vs Free Comparison

Create a visual comparison table in the upgrade flow:

| Feature | Free | Premium |
|---------|------|---------|
| Photos | 5 | ?? Unlimited |
| Custom Theme | ? | ? |
| Premium Badge | ? | ? |
| Story Highlights | ? | ? 5 pins |

### Charity Impact Display

Show charity contribution prominently:

```
Your Bird Has Helped:
?? 3 birds rescued
?? 2 shelters supported  
?? 1 conservation project

Total Contributed: $12.50
```

### Pricing Display

Use friendly, love-focused copy:

```
Monthly Celebration - $3.99/month
Celebrate your bird & support charities

? Custom themes
? Premium badge
?? Unlimited memories
?? 10% to bird charities
```

---

## Platform-Specific Notes

### iOS

- Use **StoreKit 2** for in-app purchases
- Map plan IDs: `monthly` ? your iOS product ID
- Handle subscription receipts
- Display charity allocation in app store description

### Android

- Use **Google Play Billing Library 5.0+**
- Map plan IDs to SKU IDs
- Handle subscription acknowledgment
- Include charity information in Play Store listing

### Web

- Integrate **Stripe Checkout** or **Stripe Elements**
- Support credit cards and Apple/Google Pay
- Display real-time charity impact

---

## Support & Troubleshooting

### Common Issues

**Issue: "Subscription not activating"**
- Check payment provider webhook status
- Verify bird ownership
- Check for existing active subscription

**Issue: "Premium features not showing"**
- Verify `isPremium` status via API
- Check `currentPeriodEnd` hasn't expired
- Refresh premium status from server

**Issue: "Charity impact not updating"**
- Charity allocations run via background job (Hangfire)
- Updates occur daily, not real-time
- Check Hangfire dashboard for job status

### Debug Endpoints

For development, you can check:

1. Premium status: `GET /api/premium/status/{birdId}`
2. Subscription details: Check Hangfire dashboard at `/hangfire`
3. Charity allocations: `GET /api/charity/impact/{birdId}`

---

## Migration Path (For Existing Free Birds)

When upgrading existing birds to premium:

1. **Preserve existing data**: All photos, stories, comments remain
2. **Unlock premium features**: Immediately after subscription
3. **Apply default style**: Gold frame + star badge
4. **Notify owner**: "Your bird is now premium!"
5. **Show customization**: Guide to style customization

---

## Analytics & Tracking

Recommended events to track:

### Subscription Events
- `premium_plan_viewed`
- `premium_subscribe_started`
- `premium_subscribe_completed`
- `premium_subscribe_failed`
- `premium_canceled`

### Style Events
- `premium_style_viewed`
- `premium_style_updated`
- `premium_frame_changed`
- `premium_badge_changed`

### Charity Events
- `charity_impact_viewed`
- `charity_partner_clicked`

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Jan 2024 | Initial premium system release |

---

## Contact & Support

For technical questions or API issues:
- Backend Team: backend@wihngo.com
- API Documentation: https://api.wihngo.com/docs
- Status Page: https://status.wihngo.com

**Last Updated:** January 2024  
**API Version:** 1.0
