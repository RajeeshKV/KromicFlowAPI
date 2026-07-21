# Email Template Configuration Guide

## Overview

KromicFlow uses a flexible email template system that supports both:
1. **Inline HTML rendering** - Default mode, templates hardcoded in the application
2. **Brevo Template System** - Optional, uses Brevo's template rendering engine

## Configuration

### In `appsettings.json`

```json
{
  "EmailTemplates": {
    "UseTemplates": false,
    "VerificationEmailTemplateId": 0,
    "SubscriptionExpiry7DaysTemplateId": 0,
    "SubscriptionExpiry3DaysTemplateId": 0,
    "SubscriptionExpiry1DayTemplateId": 0,
    "SubscriptionExpiredTemplateId": 0,
    "SubscriptionRenewalTemplateId": 0
  }
}
```

### In `appsettings.Production.json`

```json
{
  "EmailTemplates": {
    "UseTemplates": true,
    "VerificationEmailTemplateId": 1,
    "SubscriptionExpiry7DaysTemplateId": 2,
    "SubscriptionExpiry3DaysTemplateId": 3,
    "SubscriptionExpiry1DayTemplateId": 4,
    "SubscriptionExpiredTemplateId": 5,
    "SubscriptionRenewalTemplateId": 6
  }
}
```

## Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `UseTemplates` | boolean | `false` | Enable/disable Brevo template system. If `false`, inline HTML is used. If `true`, template IDs must be set in Brevo. |
| `VerificationEmailTemplateId` | int | `0` | Brevo template ID for email verification emails |
| `SubscriptionExpiry7DaysTemplateId` | int | `0` | Brevo template ID for 7-day expiry reminder |
| `SubscriptionExpiry3DaysTemplateId` | int | `0` | Brevo template ID for 3-day expiry reminder |
| `SubscriptionExpiry1DayTemplateId` | int | `0` | Brevo template ID for 1-day expiry reminder |
| `SubscriptionExpiredTemplateId` | int | `0` | Brevo template ID for subscription expired notification |
| `SubscriptionRenewalTemplateId` | int | `0` | Brevo template ID for subscription renewal confirmation |

## Email Template Types

The system supports 6 email template types:

### 1. VerificationEmail
**When sent**: User requests email verification
**Template parameters**:
- `fullName` - User's full name
- `verificationLink` - Email verification URL with token

### 2. SubscriptionExpiry7Days
**When sent**: 7 days before subscription expiry
**Template parameters**:
- `fullName` - User's full name
- `planName` - Plan name (e.g., "Starter", "Pro")
- `expiryDate` - Subscription expiry date
- `renewalLink` - Link to renewal page

### 3. SubscriptionExpiry3Days
**When sent**: 3 days before subscription expiry
**Template parameters**:
- `fullName` - User's full name
- `planName` - Plan name
- `expiryDate` - Subscription expiry date
- `renewalLink` - Link to renewal page

### 4. SubscriptionExpiry1Day
**When sent**: 1 day before subscription expiry
**Template parameters**:
- `fullName` - User's full name
- `planName` - Plan name
- `expiryDate` - Subscription expiry date
- `renewalLink` - Link to renewal page

### 5. SubscriptionExpired
**When sent**: On subscription expiry date
**Template parameters**:
- `fullName` - User's full name
- `planName` - Plan name
- `renewalLink` - Link to renewal page

### 6. SubscriptionRenewal
**When sent**: After successful subscription renewal
**Template parameters**:
- `fullName` - User's full name
- `planName` - Plan name
- `newExpiryDate` - New subscription expiry date
- `amount` - Renewal amount paid
- `transactionId` - Razorpay transaction ID (optional)

## How to Set Up Brevo Templates

### Step 1: Create Templates in Brevo

1. Log in to [Brevo Console](https://app.brevo.com)
2. Go to **Templates** → **Transactional** → **Create a template**
3. For each of the 6 template types above:
   - Click **Design** (drag-and-drop editor)
   - Build your email template
   - Use template variables: `{{fullName}}`, `{{verificationLink}}`, `{{planName}}`, etc.
   - Save the template
   - Note the **Template ID** (visible in the template list)

### Step 2: Update Configuration

Add the template IDs to `appsettings.Production.json`:

```json
{
  "EmailTemplates": {
    "UseTemplates": true,
    "VerificationEmailTemplateId": 1,
    "SubscriptionExpiry7DaysTemplateId": 2,
    "SubscriptionExpiry3DaysTemplateId": 3,
    "SubscriptionExpiry1DayTemplateId": 4,
    "SubscriptionExpiredTemplateId": 5,
    "SubscriptionRenewalTemplateId": 6
  }
}
```

### Step 3: Update Email Sender Code

When `UseTemplates` is enabled, the code will:
1. Get the template ID from configuration
2. Pass template parameters to Brevo
3. Brevo renders the template with the parameters
4. Send the rendered email

No code changes needed - the system handles this automatically!

## Template Parameter Examples

### Email Verification
```csharp
var templateParams = new Dictionary<string, string>
{
    { "fullName", "John Doe" },
    { "verificationLink", "https://yourdomain.com/verify-email?token=abc123..." }
};
```

### Subscription Expiry (7 days)
```csharp
var templateParams = new Dictionary<string, string>
{
    { "fullName", "Jane Smith" },
    { "planName", "Pro" },
    { "expiryDate", "2026-08-20" },
    { "renewalLink", "https://yourdomain.com/billing/renew" }
};
```

### Subscription Renewal
```csharp
var templateParams = new Dictionary<string, string>
{
    { "fullName", "Bob Johnson" },
    { "planName", "Starter" },
    { "newExpiryDate", "2027-01-20" },
    { "amount", "₹999" },
    { "transactionId", "rzp_1234567890" }
};
```

## Default Inline Templates

If `UseTemplates` is `false` (default), the system uses inline HTML templates. You can view these in:
- `src/KromicFlow.Infrastructure/Services/EmailTemplateService.cs` → `#region Email Templates`

These templates are pre-built with:
- Professional styling
- Color-coded urgency (green for 7-day warning, orange for 3-day, red for 1-day and expired)
- Call-to-action buttons
- Legal language

## Switching Between Modes

### Mode 1: Inline HTML (Development/Testing)
```json
"UseTemplates": false
```

### Mode 2: Brevo Templates (Production)
```json
"UseTemplates": true,
"VerificationEmailTemplateId": 1,
"SubscriptionExpiry7DaysTemplateId": 2,
...
```

The code automatically detects which mode is active and sends emails accordingly. No restart required!

## Testing Templates

### Test Inline Templates (Default)
1. Ensure `"UseTemplates": false` in `appsettings.json`
2. Trigger an email event (e.g., send verification email)
3. Check Brevo dashboard → **Emails** → **Transactional** → **Email Activity**
4. View the rendered HTML email

### Test Brevo Templates
1. Create templates in Brevo console
2. Update `appsettings.json` with template IDs
3. Set `"UseTemplates": true`
4. Trigger an email event
5. Check Brevo dashboard to verify template rendering

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Emails not sending | `UseTemplates: true` but no template ID | Set template IDs in config or use `UseTemplates: false` |
| Template variables not rendering | Wrong parameter names | Check parameter names match template variable names |
| Old email format sent | Still using inline templates | Update config and verify `UseTemplates: true` |
| Test email not received | Brevo API key invalid | Verify API key in `Brevo.ApiKey` config |

## API Reference

- [Brevo Transactional Emails API](https://developers.brevo.com/reference/sendtransacemail)
- [Brevo Template System Docs](https://help.brevo.com/hc/en-us/articles/209467485-Create-and-manage-transactional-email-templates)

