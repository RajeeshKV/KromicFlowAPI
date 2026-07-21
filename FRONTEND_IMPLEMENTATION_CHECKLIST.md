# Frontend Implementation Checklist

> **Step-by-step guide to integrate email verification and subscription reminders**

---

## 📋 Pre-Implementation

- [ ] Read `EMAIL_NOTIFICATION_QUICK_REFERENCE.md` (5 min)
- [ ] Read `docs/10-Frontend/03-EmailVerificationFlow.md` (15 min)
- [ ] Read `docs/10-Frontend/04-SubscriptionExpiryNotifications.md` (15 min)
- [ ] Understand the two main flows:
  - [ ] Email verification (manual, on demand)
  - [ ] Subscription reminders (automatic, hourly)
- [ ] Get backend API endpoints ready (ask backend team for status)
- [ ] Confirm Brevo templates are created (ask backend/devops)

---

## 🔐 Email Verification Implementation

### Components to Create

- [ ] **EmailVerificationPrompt.jsx/tsx**
  - [ ] Yellow banner component
  - [ ] "Send Verification Email" button
  - [ ] Show loading state
  - [ ] Show success message ("Email sent")
  - [ ] Show error message
  - [ ] Only show if `user.emailVerified === false`

- [ ] **EmailVerificationPage.jsx/tsx**
  - [ ] Parse token from URL query param
  - [ ] Extract token: `URLSearchParams.get('token')`
  - [ ] Show loading spinner
  - [ ] Call `/api/v1/users/verify-email-token` with token
  - [ ] Show success message
  - [ ] Show error message (expired/invalid)
  - [ ] Redirect to dashboard on success

- [ ] **VerificationGuard Component** (Optional, for routes)
  - [ ] Redirect to dashboard if already verified
  - [ ] Show prompt if not verified

### Integration Points

- [ ] **After OAuth Login**
  ```javascript
  if (user && !user.emailVerified) {
    showEmailVerificationPrompt();
  }
  ```

- [ ] **Dashboard**
  ```javascript
  <EmailVerificationPrompt />
  ```

- [ ] **Email Link Handler**
  ```javascript
  // Route: /verify-email?token=ABC123
  <Route path="/verify-email" element={<EmailVerificationPage />} />
  ```

### API Calls

- [ ] Implement: `POST /api/v1/users/verify-email`
  ```javascript
  const response = await fetch('/api/v1/users/verify-email', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` }
  });
  ```

- [ ] Implement: `POST /api/v1/users/verify-email-token`
  ```javascript
  const response = await fetch('/api/v1/users/verify-email-token', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ token: urlToken })
  });
  ```

- [ ] Implement: `GET /api/v1/users/profile`
  ```javascript
  // Already exists, use for checking emailVerified status
  ```

### Error Handling

- [ ] Handle "Email not set" error (400)
  - [ ] Show message: "Please add your email to your profile"
- [ ] Handle "Too many requests" error (429)
  - [ ] Show message: "Please wait 1 hour before trying again"
- [ ] Handle "Token expired" error (400)
  - [ ] Show "Resend Email" button
- [ ] Handle "Invalid token" error (400)
  - [ ] Show "Resend Email" button
- [ ] Handle network errors
  - [ ] Show retry button

### Testing

- [ ] Sign up new user → see verification prompt
- [ ] Click "Send Verification Email" → email sent
- [ ] Check email received → link works
- [ ] Click link → verification page shows
- [ ] Token verified → redirected to dashboard
- [ ] `user.emailVerified` now true
- [ ] Can now create automations
- [ ] Test expired token (modify time in DB)
- [ ] Test invalid token
- [ ] Test rate limiting (>3 emails)

---

## 🔴 Block Automations Until Verified

### Automation Create Page

- [ ] Import EmailVerificationPrompt
- [ ] Add check:
  ```javascript
  if (!user?.emailVerified) {
    return (
      <div>
        <EmailVerificationPrompt />
        <p>Please verify email to create automations</p>
      </div>
    );
  }
  ```

- [ ] Disable form if email not verified
- [ ] Show warning message
- [ ] Test: Cannot create automation without verification

### Automation List Page

- [ ] Show indicator: "Email verification required" if not verified
- [ ] Disable "Create" button if not verified
- [ ] Link to verification page

---

## 💳 Subscription Status Implementation

### Components to Create

- [ ] **SubscriptionCard.jsx/tsx**
  - [ ] Show plan name
  - [ ] Show renewal date
  - [ ] Show status (Active/Expired)
  - [ ] Calculate days until expiry
  - [ ] Color code based on days left:
    - [ ] Green: > 7 days
    - [ ] Yellow: 1-7 days
    - [ ] Red: < 0 days (expired)
  - [ ] Show message:
    - [ ] "Renews in X days"
    - [ ] "Expires today"
    - [ ] "Expired - Downgraded to Free"
  - [ ] Button to manage subscription

- [ ] **SubscriptionExpiryWidget.jsx/tsx**
  - [ ] Show on dashboard top
  - [ ] Show warning if < 7 days
  - [ ] Show error if expired
  - [ ] Link to billing page
  - [ ] Hide if > 7 days away

- [ ] **SubscriptionBillingPage.jsx/tsx**
  - [ ] Show current plan details
  - [ ] Show renewal date
  - [ ] Show price
  - [ ] Show features included
  - [ ] Button to change plan
  - [ ] Button to cancel subscription

### Integration Points

- [ ] **Dashboard**
  ```javascript
  <SubscriptionExpiryWidget subscription={sub} plan={plan} />
  ```

- [ ] **Sidebar/Header**
  ```javascript
  // Show "⚠️ Renews in 3 days" indicator
  ```

- [ ] **Settings Page**
  ```javascript
  <SubscriptionCard subscription={sub} plan={plan} />
  ```

### API Calls

- [ ] Implement: `GET /api/v1/billing/status`
  ```javascript
  const sub = await fetch('/api/v1/billing/status', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  ```

- [ ] Implement: `GET /api/v1/billing/plans`
  ```javascript
  const plans = await fetch('/api/v1/billing/plans');
  ```

### Utility Functions

- [ ] Create helper to calculate days until expiry:
  ```javascript
  function calculateDaysUntilExpiry(expiryDate) {
    const now = new Date();
    const expiry = new Date(expiryDate);
    const diffTime = expiry - now;
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }
  ```

- [ ] Create helper to get status message:
  ```javascript
  function getSubscriptionMessage(daysLeft) {
    if (daysLeft < 0) return 'Expired';
    if (daysLeft === 0) return 'Expires today';
    return `Renews in ${daysLeft} days`;
  }
  ```

- [ ] Create helper to get status color:
  ```javascript
  function getStatusColor(daysLeft) {
    if (daysLeft < 0) return 'bg-red-50';
    if (daysLeft <= 7) return 'bg-yellow-50';
    return 'bg-green-50';
  }
  ```

### Testing

- [ ] Get subscription status (call API)
- [ ] Display renewal date correctly
- [ ] Show countdown (days until expiry)
- [ ] Show warning if < 7 days
- [ ] Show error if expired
- [ ] Update every time user visits page
- [ ] Handle subscription not found (free plan)
- [ ] Handle API errors gracefully

---

## 📱 State Management

### If Using React Context

- [ ] Add to AuthContext:
  ```javascript
  const [user, setUser] = useState(null);
  // user.emailVerified
  // user.subscription
  // user.plan
  ```

- [ ] Add methods:
  ```javascript
  const sendVerificationEmail = async () => { ... }
  const verifyEmailToken = async (token) => { ... }
  const refreshSubscription = async () => { ... }
  ```

### If Using Redux/Zustand

- [ ] Add slices:
  ```javascript
  auth: { user, emailVerified, loading, error }
  subscription: { current, plan, expiresAt, loading, error }
  ```

- [ ] Add thunks/actions:
  ```javascript
  sendVerificationEmail()
  verifyEmailToken(token)
  getSubscriptionStatus()
  ```

---

## 🧪 Testing Checklist

### Unit Tests

- [ ] EmailVerificationPrompt
  - [ ] Shows when not verified
  - [ ] Hides when verified
  - [ ] Shows loading state
  - [ ] Shows error message
  - [ ] Calls API on button click

- [ ] EmailVerificationPage
  - [ ] Extracts token from URL
  - [ ] Calls verification endpoint
  - [ ] Shows success message
  - [ ] Shows error message
  - [ ] Redirects on success

- [ ] SubscriptionCard
  - [ ] Displays plan name
  - [ ] Shows renewal date
  - [ ] Calculates days correctly
  - [ ] Shows appropriate message
  - [ ] Shows correct color

### Integration Tests

- [ ] Sign up → See verification prompt
- [ ] Send email → Email endpoint called
- [ ] Verify token → Verification page works
- [ ] Check status → emailVerified updated
- [ ] Create automation → Now allowed

### E2E Tests

- [ ] Complete sign-up flow
- [ ] Email verification from inbox
- [ ] Automation creation after verification
- [ ] Subscription status display
- [ ] Renewal countdown updates

### Manual Testing

- [ ] Create new account
- [ ] Should see verification prompt
- [ ] Click "Send Email"
- [ ] Check Brevo dashboard for email
- [ ] Copy verification link
- [ ] Paste in browser
- [ ] Token should verify
- [ ] Should redirect to dashboard
- [ ] Should show success message
- [ ] Should be able to create automation
- [ ] Should see subscription status
- [ ] Should see renewal date

---

## 🎨 UI/UX Guidelines

### Email Verification Banner

```jsx
<div className="bg-yellow-50 border-l-4 border-yellow-400 p-4">
  <h3 className="font-semibold text-yellow-800">
    Verify your email to activate automations
  </h3>
  <p className="text-sm text-yellow-700 mt-2">
    We need to verify your email before you can create automations.
  </p>
  <button className="mt-4 px-4 py-2 bg-yellow-600 text-white rounded">
    Send Verification Email
  </button>
</div>
```

### Subscription Status Widget

```jsx
<div className="bg-green-50 border-l-4 border-green-400 p-4">
  <p className="text-sm font-semibold text-green-800">
    Your Starter plan renews on July 25, 2026
  </p>
  <p className="text-xs text-green-700 mt-1">
    7 days remaining
  </p>
  <a href="/billing" className="text-green-600 underline text-sm mt-2">
    Manage Subscription →
  </a>
</div>
```

---

## 📊 Progress Tracking

### Week 1
- [ ] Day 1: Components created
- [ ] Day 2: Integration started
- [ ] Day 3: Email verification working
- [ ] Day 4: Subscription display working
- [ ] Day 5: Testing & fixes

### Week 2
- [ ] Day 1: E2E testing
- [ ] Day 2: Error handling review
- [ ] Day 3: Performance testing
- [ ] Day 4: Documentation review
- [ ] Day 5: Ready for production

---

## 🚀 Deployment Checklist

- [ ] All components created
- [ ] All tests passing
- [ ] Email verification working E2E
- [ ] Subscription status displaying
- [ ] Error handling complete
- [ ] Loading states added
- [ ] Success messages added
- [ ] Accessibility checked (alt text, aria labels)
- [ ] Mobile responsive tested
- [ ] Browser compatibility tested
- [ ] Performance acceptable
- [ ] Code review passed
- [ ] Ready to merge to main

---

## 📚 Reference Documents

| Document | Use When |
|----------|----------|
| `EMAIL_NOTIFICATION_QUICK_REFERENCE.md` | Quick lookup |
| `docs/10-Frontend/03-EmailVerificationFlow.md` | Implementing verification |
| `docs/10-Frontend/04-SubscriptionExpiryNotifications.md` | Implementing subscription UI |
| `docs/10-Frontend/05-EmailNotificationArchitecture.md` | Understanding system design |
| This checklist | Tracking progress |

---

## ✅ Sign-Off

- [ ] All tasks completed
- [ ] All tests passing
- [ ] All documentation read
- [ ] Deployed to production
- [ ] Monitoring in place
- [ ] Ready for users

---

**Created**: July 20, 2026  
**Last Updated**: July 20, 2026  
**Status**: Ready for implementation
