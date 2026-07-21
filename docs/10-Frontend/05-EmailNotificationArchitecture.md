# Email Notification Architecture

> **Complete system design for email verification and subscription reminders**

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│  Frontend                                                       │
│  • Email verification flow                                     │
│  • Subscription status display                                 │
│  • Blocks automations until verified                           │
└────────────────┬────────────────────────────────────────────────┘
                 │
                 ↓
┌─────────────────────────────────────────────────────────────────┐
│  Backend API                                                    │
│  • POST /verify-email (send token)                             │
│  • POST /verify-email-token (confirm)                          │
│  • GET /billing/status (subscription status)                   │
└────────────────┬────────────────────────────────────────────────┘
                 │
        ┌────────┴───────────┐
        │                    │
        ↓                    ↓
┌──────────────────┐  ┌──────────────────────────┐
│ Email Services   │  │ Background Services      │
│                  │  │                          │
│ • Brevo API      │  │ • Email Verification     │
│ • Template        │  │ • Subscription Expiry    │
│   Engine         │  │ • Outbox Event Processor │
└────────────────┬─┘  └──────────┬───────────────┘
                 │               │
                 └───────┬───────┘
                         ↓
                  ┌──────────────────┐
                  │  Database        │
                  │  • Users         │
                  │  • Subscriptions │
                  │  • Notifications │
                  └──────────────────┘
```

---

## Email Verification Flow

### Components

**1. User Entity** (Domain)
```csharp
public sealed class User : Entity
{
    public string? Email { get; set; }
    public bool EmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiresUtc { get; set; }
}
```

**2. Email Verification Service** (Infrastructure)
```csharp
public interface IEmailVerificationService
{
    string GenerateToken();  // 256-bit crypto random token
    DateTime GetTokenExpirationTime();  // 24 hours from now
}
```

**3. Endpoints** (API)
- `POST /api/v1/users/verify-email` — Send verification email
- `POST /api/v1/users/verify-email-token` — Confirm verification
- `GET /api/v1/users/profile` — Get status (includes EmailVerified)

**4. Email Template** (Brevo)
- Template: `email_verification`
- Variables: `{{verificationLink}}`, `{{userName}}`

**5. Frontend Components** (UI)
- `<EmailVerificationPrompt />` — Yellow banner
- `<EmailVerificationPage />` — Verification link handler
- Automation create guard

---

## Subscription Expiry Notification Flow

### Components

**1. User Subscription Entity** (Domain)
```csharp
public sealed class UserSubscription : Entity
{
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; }  // active, halted, cancelled, expired
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? LastReminderSentAtUtc { get; set; }
    public int RemindersSent { get; set; }  // 0: none, 1: 7day, 2: 3day, 3: 1day, 4: expired
}
```

**2. Background Service** (Infrastructure)
```csharp
public sealed class SubscriptionExpiryNotificationBackgroundService 
    : BackgroundService
{
    // Runs every hour
    // Checks all active subscriptions
    // Sends reminders at: 7 days, 3 days, 1 day, expiry
}
```

**3. Notification Templates** (Brevo)
- `subscription_reminder_7days`
- `subscription_reminder_3days`
- `subscription_reminder_1day`
- `subscription_expired`
- `subscription_charged` (webhook)
- `subscription_failed` (webhook)

**4. Template Engine** (Application)
```csharp
public interface IEmailTemplateEngine
{
    string Render(string templateName, Dictionary<string, object> variables);
}
```

Variables:
- `{{userName}}`
- `{{planName}}`
- `{{renewalDate}}`
- `{{daysUntilExpiry}}`
- `{{maxAccounts}}`
- `{{maxAutomations}}`
- `{{monthlyRuns}}`
- etc.

**5. Frontend Components** (UI)
- Subscription status widget
- Renewal date display
- Expiry warning banner
- Manage subscription button

---

## Data Models

### User Profile Response
```json
{
  "id": "guid",
  "email": "user@example.com",
  "fullName": "John Doe",
  "emailVerified": true,
  "planCode": "STARTER"
}
```

### Email Verification
```json
{
  "token": "BASE64_TOKEN_FROM_EMAIL"
}
```

### Subscription Status
```json
{
  "plan": "STARTER",
  "status": "active",
  "currentPeriodEnd": "2026-07-20T00:00:00Z",
  "razorpaySubscriptionId": "sub_123"
}
```

---

## Email Templates Reference

### Template: email_verification

**Brevo Template ID**: (set via admin)

**Variables**:
- `verificationLink` — Full URL with token
- `userName` — User's name
- `expiresAt` — When token expires

**HTML**:
```html
Hi {{userName}},

Please verify your email by clicking the link below:
<a href="{{verificationLink}}">Verify Email</a>

This link expires at {{expiresAt}}.
```

### Template: subscription_reminder_7days

**Variables**:
- `userName`
- `planName`
- `renewalDate`
- `maxAccounts`, `maxAutomations`, `monthlyRuns`
- `dashboardUrl`

### Template: subscription_reminder_3days

Similar to 7 days

### Template: subscription_reminder_1day

Similar to 7 days

### Template: subscription_expired

**Variables**:
- `userName`
- `planName`
- `expiryDate`
- `freePlanFeatures`
- `upgradeUrl`

### Template: subscription_charged

**Variables**:
- `userName`
- `planName`
- `amount`, `currency`
- `renewalDate`
- `invoiceUrl`

### Template: subscription_failed

**Variables**:
- `userName`
- `planName`
- `failureReason`
- `amount`, `currency`
- `paymentUrl`

---

## Database Schema

### users table
```sql
CREATE TABLE users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) UNIQUE,
    full_name VARCHAR(255),
    email_verified BOOLEAN DEFAULT FALSE,
    email_verification_token VARCHAR(255),
    email_verification_token_expires_utc TIMESTAMP,
    -- other fields...
);
```

### user_subscriptions table
```sql
CREATE TABLE user_subscriptions (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    plan_id UUID REFERENCES plans(id),
    razorpay_subscription_id VARCHAR(255) UNIQUE,
    status VARCHAR(50),  -- active, halted, cancelled, expired
    current_period_start TIMESTAMP,
    current_period_end TIMESTAMP,
    last_reminder_sent_at_utc TIMESTAMP,
    reminders_sent INT DEFAULT 0,
    created_utc TIMESTAMP,
    updated_utc TIMESTAMP
);
```

### notification_messages table
```sql
CREATE TABLE notification_messages (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    template_name VARCHAR(100),
    subject VARCHAR(255),
    body TEXT,
    status VARCHAR(50),  -- pending, sent, failed
    provider_message_id VARCHAR(255),  -- Brevo message ID
    sent_utc TIMESTAMP,
    created_utc TIMESTAMP
);
```

---

## Implementation Checklist

### Backend (API)
- [ ] Add EmailVerified, EmailVerificationToken, EmailVerificationTokenExpiresUtc to User entity
- [ ] Create migration for new fields
- [ ] Implement IEmailVerificationService
- [ ] Create SendVerificationEmailCommand handler
- [ ] Create VerifyEmailTokenCommand handler
- [ ] Add endpoints to UserController
- [ ] Implement SubscriptionExpiryNotificationBackgroundService
- [ ] Create IEmailTemplateEngine service
- [ ] Register all services in DI
- [ ] Create Brevo email templates
- [ ] Add rate limiting to email endpoints (3/hour max)
- [ ] Add email verification check to CreateAutomationCommand (403 if not verified)

### Frontend (UI)
- [ ] Create EmailVerificationPrompt component
- [ ] Create EmailVerificationPage component
- [ ] Add verification check to automation create page
- [ ] Create SubscriptionStatusWidget component
- [ ] Add expiry warning banner
- [ ] Create subscription management UI
- [ ] Handle all error states
- [ ] Add success messages
- [ ] Test with real email
- [ ] Test link in email
- [ ] Test token verification

### Testing
- [ ] Unit test token generation
- [ ] Unit test background service logic
- [ ] Integration test email sending
- [ ] Integration test token verification
- [ ] E2E test complete flow
- [ ] Test rate limiting
- [ ] Test error handling
- [ ] Test with expired tokens
- [ ] Manual test with real email

---

## Environment Variables

Add to `.env`:

```env
# Email Verification
EMAIL_VERIFICATION_TOKEN_EXPIRY_HOURS=24
EMAIL_VERIFICATION_MAX_REQUESTS_PER_HOUR=3

# Subscription Reminders
SUBSCRIPTION_REMINDER_DAYS=7,3,1
SUBSCRIPTION_CHECK_INTERVAL_MINUTES=60

# Brevo Templates (create in Brevo dashboard)
BREVO_TEMPLATE_EMAIL_VERIFICATION=1
BREVO_TEMPLATE_SUBSCRIPTION_REMINDER_7DAYS=2
BREVO_TEMPLATE_SUBSCRIPTION_REMINDER_3DAYS=3
BREVO_TEMPLATE_SUBSCRIPTION_REMINDER_1DAY=4
BREVO_TEMPLATE_SUBSCRIPTION_EXPIRED=5
BREVO_TEMPLATE_SUBSCRIPTION_CHARGED=6
BREVO_TEMPLATE_SUBSCRIPTION_FAILED=7
```

---

## Error Handling

### Email Verification Errors

| Error | Status | Solution |
|-------|--------|----------|
| No email set | 400 | Ask user to update profile |
| Email already verified | 400 | Skip to dashboard |
| Too many requests | 429 | Show rate limit message |
| Brevo error | 500 | Retry with exponential backoff |
| Token expired | 400 | Show resend button |
| Token invalid | 400 | Show resend button |

### Subscription Reminder Errors

| Error | Handling |
|-------|----------|
| Brevo API down | Log and retry in 1 hour |
| Subscription not found | Log and skip |
| User email empty | Log and skip |
| Template not found | Log and skip |

---

## Security Considerations

### Email Verification Token
- ✅ Generated using cryptographically secure random (256 bits)
- ✅ Not reversible (one-way)
- ✅ Expires in 24 hours
- ✅ Stored hashed in database (optional, for added security)
- ✅ Can only be used once

### Subscription Data
- ✅ User can only see own subscription
- ✅ Only admin can send manual emails
- ✅ All emails logged for audit
- ✅ No sensitive data in email body
- ✅ HMAC verification on webhooks

### Rate Limiting
- ✅ Max 3 verification emails per user per hour
- ✅ Prevents email flooding
- ✅ Returns 429 Too Many Requests

---

## Monitoring & Observability

### Key Metrics to Track

1. **Email Verification**
   - Emails sent (daily)
   - Verification success rate (%)
   - Time to verify (avg hours)
   - Failed verifications (daily)

2. **Subscription Reminders**
   - Emails sent per reminder type (daily)
   - Delivery failures (daily)
   - User engagement (opens, clicks)

3. **Errors**
   - Failed email sends (per type)
   - Rate limit hits (daily)
   - Expired tokens (daily)

### Logging

```csharp
logger.LogInformation("Email verification sent to {Email} with token expiry {ExpiryTime}", 
    user.Email, expiryTime);

logger.LogError("Brevo email send failed: {ErrorMessage}", ex.Message);

logger.LogInformation("Subscription reminder sent: User={UserId}, DaysUntilExpiry={Days}, TemplateId={Template}",
    userId, daysLeft, templateId);
```

---

## Deployment Checklist

- [ ] Database migrations applied
- [ ] Brevo email templates created
- [ ] Environment variables set
- [ ] API endpoints tested
- [ ] Frontend integrated
- [ ] Email verification flow tested E2E
- [ ] Subscription reminders tested
- [ ] Rate limiting verified
- [ ] Error handling verified
- [ ] Monitoring set up
- [ ] Documentation reviewed
- [ ] Production deployment approved

---

## Future Enhancements

1. **SMS Reminders** (in addition to email)
   - Send SMS 1 day before expiry
   - Alternative channel for critical notifications

2. **Email Unsubscribe**
   - Allow users to opt-out of specific emails
   - While keeping critical alerts (e.g., payment failed)

3. **Notification Preferences**
   - User can choose reminder days (instead of 7/3/1)
   - User can choose email frequency (daily digest vs immediate)

4. **Scheduled Email Sending**
   - Batch send emails at off-peak hours
   - Reduce database load

5. **A/B Testing**
   - Test different email subject lines
   - Track engagement metrics

---

## Summary

**This system provides**:
- ✅ Email verification before automations enabled
- ✅ Automatic subscription renewal reminders
- ✅ Failure notifications
- ✅ Complete audit trail
- ✅ Security and rate limiting
- ✅ Extensible template system
- ✅ Frontend integration guides

**Frontend developers will**:
- Show verification prompt post-signup
- Display subscription status on dashboard
- Handle verification link from email
- Block automation creation until verified

**No manual intervention needed** - everything is automated!

---

**Last Updated**: July 20, 2026  
**Status**: Ready for implementation
