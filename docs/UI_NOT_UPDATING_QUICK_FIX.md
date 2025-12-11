# ?? UI Not Updating - Quick Fix Guide

## Problem
Payment completed on blockchain, but mobile app UI doesn't show completion.

---

## ?? Most Common Causes & Fixes

### 1?? Polling Stopped Prematurely

**Symptom:** Console shows polling started, but no more poll messages appear.

**Fix:**
```javascript
// Ensure polling doesn't stop on error
pollIntervalRef.current = setInterval(async () => {
  try {
    const status = await checkStatus(payment.id);
    setPayment(status); // ? Always update state
  } catch (error) {
    console.error('Poll error:', error);
    // ? Don't stop polling on error!
  }
}, 5000);
```

---

### 2?? State Not Updating

**Symptom:** API returns completed status, but UI shows old status.

**Fix:**
```javascript
const checkStatus = useCallback(async (paymentId) => {
  try {
    const result = await api.checkPaymentStatus(paymentId);
    
    // ? CRITICAL: Update state even if same status
    setPayment(result);
    
    // ? Also return the result
    return result;
  } catch (err) {
    console.error('Failed to check status:', err);
    return null;
  }
}, [api]);
```

---

### 3?? Component Unmounted

**Symptom:** User navigated away or app backgrounded.

**Fix:**
```javascript
// Keep reference to navigation and prevent background issues
useEffect(() => {
  return () => {
    if (pollIntervalRef.current) {
      clearInterval(pollIntervalRef.current);
    }
  };
}, []);

// Also add app state listener
useEffect(() => {
  const subscription = AppState.addEventListener('change', nextAppState => {
    if (nextAppState === 'active' && payment?.id && payment.status !== 'completed') {
      console.log('App foregrounded - refreshing status');
      checkStatus(payment.id);
    }
  });

  return () => subscription.remove();
}, [payment?.id, payment?.status]);
```

---

### 4?? Backend Hasn't Updated Yet

**Symptom:** Transaction confirmed on blockchain, but backend status still "confirming".

**Fix:** Wait for background job to run (runs every 30 seconds) or manually trigger:

```javascript
// Add a "Force Refresh" button
<TouchableOpacity onPress={async () => {
  const status = await api.checkPaymentStatus(payment.id);
  if (status.status === 'completed') {
    Alert.alert('? Completed!', 'Premium activated');
  } else {
    Alert.alert('Status', `${status.status} - ${status.confirmations}/${status.requiredConfirmations}`);
  }
}}>
  <Text>?? Force Refresh</Text>
</TouchableOpacity>
```

---

## ?? Quick Diagnostic

Add this to your component:

```javascript
// At top of component
useEffect(() => {
  console.log('?? DIAGNOSTIC:', {
    hasPayment: !!payment,
    paymentId: payment?.id,
    status: payment?.status,
    confirmations: `${payment?.confirmations}/${payment?.requiredConfirmations}`,
    isPolling: isPolling,
    hasInterval: !!pollIntervalRef.current
  });
}, [payment, isPolling]);
```

**Look for:**
- ? `isPolling: true` - Polling active
- ? `hasInterval: true` - Interval running
- ? `status: 'completed'` - Backend updated
- ? If all ? but UI not updating ? React state issue

---

## ?? Complete Fixed Implementation

```javascript
import { AppState } from 'react-native';

const CryptoPaymentScreen = ({ route, navigation }) => {
  const { payment, loading, createPayment, verifyPayment, checkStatus, api } = useCryptoPayment();
  const [isPolling, setIsPolling] = useState(false);
  const pollIntervalRef = useRef(null);

  // ? Monitor app state
  useEffect(() => {
    const subscription = AppState.addEventListener('change', nextAppState => {
      if (nextAppState === 'active' && payment?.id && payment.status !== 'completed') {
        checkStatus(payment.id);
      }
    });
    return () => subscription.remove();
  }, [payment?.id, payment?.status]);

  // ? Enhanced polling with error handling
  const startPolling = useCallback(() => {
    if (!payment?.id || isPolling) return;

    setIsPolling(true);
    pollIntervalRef.current = setInterval(async () => {
      try {
        const status = await checkStatus(payment.id);
        
        // ? Always update state
        if (status) {
          setPayment(status);
          
          // ? Check completion
          if (status.status === 'completed') {
            stopPolling();
            Alert.alert(
              'Payment Successful! ??',
              'Your premium subscription has been activated.',
              [{ text: 'OK', onPress: () => navigation.goBack() }]
            );
          }
        }
      } catch (error) {
        console.error('Poll error:', error);
        // ? Continue polling even on error
      }
    }, 5000);
  }, [payment?.id, isPolling, checkStatus, navigation]);

  // ? Cleanup on unmount
  useEffect(() => {
    return () => {
      if (pollIntervalRef.current) {
        clearInterval(pollIntervalRef.current);
      }
    };
  }, []);

  // ? Manual refresh function
  const forceRefresh = useCallback(async () => {
    if (!payment?.id) return;
    
    try {
      const status = await api.checkPaymentStatus(payment.id);
      setPayment(status);
      
      Alert.alert(
        'Status Updated',
        `Status: ${status.status}\nConfirmations: ${status.confirmations}/${status.requiredConfirmations}`
      );
    } catch (error) {
      Alert.alert('Error', 'Failed to refresh status');
    }
  }, [payment?.id, api]);

  return (
    <ScrollView>
      {/* Your existing UI */}
      
      {/* ? Add manual refresh button */}
      {payment && payment.status !== 'completed' && (
        <TouchableOpacity
          style={styles.refreshButton}
          onPress={forceRefresh}
        >
          <Text style={styles.refreshButtonText}>?? Refresh Status</Text>
        </TouchableOpacity>
      )}
    </ScrollView>
  );
};
```

---

## ?? Expected Console Output

When working correctly, you should see:

```
? Starting polling: payment-abc123
?? Poll #1 - Checking payment status...
?? Status: confirming 2/6
?? Poll #2 - Checking payment status...
?? Status: confirming 4/6
?? Poll #3 - Checking payment status...
?? Status: confirming 6/6
?? Poll #4 - Checking payment status...
?? Status: confirmed 6/6
?? Poll #5 - Checking payment status...
?? Status: completed 6/6
?? COMPLETED!
```

---

## ? Checklist

Before asking for help, verify:

- [ ] Console shows polling started
- [ ] Console shows poll attempts (every 5 seconds)
- [ ] API returns updated status (check backend logs)
- [ ] `setPayment()` is called with new status
- [ ] Component is still mounted (not navigated away)
- [ ] No JavaScript errors in console
- [ ] Payment ID exists and is correct

---

## ?? Emergency Fix

If nothing works, add this **temporary** button:

```javascript
<TouchableOpacity onPress={async () => {
  // Direct API call
  const response = await fetch(
    `https://your-api.com/api/payments/crypto/${payment.id}`,
    { headers: { 'Authorization': `Bearer ${token}` } }
  );
  const data = await response.json();
  
  console.log('Direct check:', data);
  setPayment(data);  // Force update state
  
  if (data.status === 'completed') {
    Alert.alert('? Completed!', 'Premium activated!');
  }
}}>
  <Text>?? Emergency Check</Text>
</TouchableOpacity>
```

---

## ?? Still Not Working?

1. **Check backend logs** - Payment status might not be updated
2. **Check database** - `SELECT * FROM crypto_payment_requests WHERE id = 'your-id'`
3. **Run background job manually** - PaymentMonitorJob might not be running
4. **Use diagnostic tools** - See `utils/cryptoPaymentDiagnostics.js`

---

**Most issues are fixed by ensuring `setPayment()` is always called!** ??
