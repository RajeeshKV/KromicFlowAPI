# Email Notification System — Quick Reference

> **At a glance: Email verification and subscription reminders**

---

## What Frontend Needs to Know

### 1. Email Verification

**When**: After OAuth login

**Frontend shows**:
```jsx
<EmailVerificationPrompt user={user} />
```

**User action**:
1. Clicks "Send Verification Email"
2. Receives email with link: `https://yourdomain.com/verify-email?token=ABC123`
3. Clicks link
4. Frontend submits token to backend
5. Sees "Email verified!" message

**Important**: Automations are blocked until `user.emailVerified === true`

---

### 2. Subscription Reminders (Automatic)

**When**: Background job runs hourly

**Sent at**:
- 7 days before expiry
- 3 days before expiry  
- 1 day before expiry
- On expiry day
- On renewal
- On payment failure

**Frontend shows**:
- Subscription status on dashboard
- "Renews in X days" message
- Warning if < 7 days
- Error if expired

**Important**: No frontend action needed — fully automatic!

---

## Frontend Integration Checklist

### Email Verification
- [ ] Add EmailVerificationPrompt to dashboard
- [ ] Add EmailVerificationPage (handles link from email)
- [ ] Block automation creation if `!user.emailVerified`
- [ ] Show "Email verified" success message
- [ ] Handle all error cases

### Subscription Display
- [ ] Show current plan name
- [ ] Show renewal date
- [ ] Show "Renews in X days"
- [ ] Show warning if < 7 days
- [ ] Show error if expired
- [ ] Add link to manage subscription

---

## API Endpoints

### Email Verification

**Send email**:
```
POST /api/v1/users/verify-email
Auth: Required (JWT)
Body: {}
Response: { success: true }
```

**Verify token**:
```
POST /api/v1/users/verify-email-token
Auth: Required (JWT)
Body: { token: "ABC123..." }
Response: { success: true, emailVerified: true }
```

**Get user (includes emailVerified status)**:
```
GET /api/v1/users/profile
Auth: Required (JWT)
Response: { ..., emailVerified: true, ... }
```

### Subscription

**Get status**:
```
GET /api/v1/billing/status
Auth: Required (JWT)
Response: {
  plan: "STARTER",
  status: "active",
  currentPeriodEnd: "2026-07-20T00:00:00Z"
}
```

**Calculate days remaining**:
```javascript
const expiryDate = new Date(subscription.currentPeriodEnd);
const daysLeft = Math.ceil((expiryDate - now) / (1000 * 60 * 60 * 24));
```

---

## Email Templates (Backend handles)

| Event | Template | Variables |
|-------|----------|-----------|
| Verify email | `email_verification` | userName, verificationLink |
| 7 days before | `subscription_reminder_7days` | userName, renewalDate, planName |
| 3 days before | `subscription_reminder_3days` | userName, renewalDate, planName |
| 1 day before | `subscription_reminder_1day` | userName, renewalDate, planName |
| On expiry | `subscription_expired` | userName, expiryDate, planName |
| Renewal | `subscription_charged` | userName, renewalDate, amount |
| Payment failed | `subscription_failed` | userName, failureReason, amount |

---

## React Components

### Email Verification Prompt
```jsx
<EmailVerificationPrompt user={user} />
```

Shows yellow banner if `!user.emailVerified`

### Email Verification Page
```jsx
<EmailVerificationPage />
```

Handle `/verify-email?token=ABC123` URLs

### Subscription Status Widget
```jsx
<SubscriptionStatusWidget subscription={subscription} plan={plan} />
```

Shows current plan, renewal date, warnings

---

## Key Points

✅ **Verified users can create automations**
- If `user.emailVerified === false` → Block creation, show prompt
- If `user.emailVerified === true` → Allow creation

✅ **Subscription reminders are automatic**
- No frontend action needed
- Backend sends emails automatically
- Frontend just displays status

✅ **Email link format**
- Backend sends: `https://yourdomain.com/verify-email?token=ABC123`
- Frontend extracts token from URL
- Frontend submits token to verify endpoint

✅ **Error handling**
- Expired token (>24h) → Show "Resend" button
- Invalid token → Show "Resend" button
- Network error → Show "Retry" button
- Too many requests → Show "Try again in 1 hour"

---

## Code Examples

### Check if email verified
```javascript
if (!user.emailVerified) {
  return <EmailVerificationPrompt />;
}
```

### Send verification email
```javascript
const response = await fetch('/api/v1/users/verify-email', {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}` }
});
```

### Verify token from URL
```javascript
const token = new URLSearchParams(window.location.search).get('token');
const response = await fetch('/api/v1/users/verify-email-token', {
  method: 'POST',
  headers: { 'Authorization': `Bearer ${token}` },
  body: JSON.stringify({ token })
});
```

### Show subscription status
```javascript
const daysLeft = Math.ceil((new Date(subscription.currentPeriodEnd) - new Date()) / (1000*60*60*24));
if (daysLeft < 0) return "Expired";
if (daysLeft <= 7) return `Warning: ${daysLeft} days left`;
return "Active";
```

---

## Detailed Guides

- **Email Verification**: `docs/10-Frontend/03-EmailVerificationFlow.md`
- **Subscription Reminders**: `docs/10-Frontend/04-SubscriptionExpiryNotifications.md`
- **Architecture**: `docs/10-Frontend/05-EmailNotificationArchitecture.md`

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Verification email not received | Check spam folder, ask user to resend |
| Link in email expired | Show resend button, send new email |
| Can't create automation | Check if `user.emailVerified === true` |
| Subscription reminder not shown | Check `/billing/status` endpoint |
| Token verification fails | Extract token from URL carefully |

---

## Testing Checklist

- [ ] Sign up, see verification prompt
- [ ] Send verification email
- [ ] Check email received
- [ ] Click link in email
- [ ] Token verified, automation enabled
- [ ] Can now create automations
- [ ] View subscription status
- [ ] See renewal date
- [ ] See warning if < 7 days
- [ ] See error if expired
- [ ] Test error cases

---

**Backend**: Fully implemented ✅
**Frontend**: Ready for integration 🚀

---

Last Updated: July 20, 2026
