# Environment Variables Configuration

Complete reference for email template environment variables.

## Brevo Configuration

### API Connection
```
Brevo__BaseUrl=https://api.brevo.com
Brevo__ApiKey=xkeysib-XXXXXXXXXXXXXXXXXXXX
Brevo__SenderEmail=noreply@flow.kromic.in
Brevo__SenderName=Kromic Flow
```

**Get API Key**: https://app.brevo.com/settings/keys/api

---

## Email Templates Configuration

### Development (Inline HTML - Default)

Use inline HTML templates, no Brevo template IDs needed:

```
# Disable Brevo templates (use inline HTML)
EmailTemplates__UseTemplates=false

# These don't matter in development
EmailTemplates__VerificationEmailTemplateId=0
EmailTemplates__SubscriptionExpiry7DaysTemplateId=0
EmailTemplates__SubscriptionExpiry3DaysTemplateId=0
EmailTemplates__SubscriptionExpiry1DayTemplateId=0
EmailTemplates__SubscriptionExpiredTemplateId=0
EmailTemplates__SubscriptionRenewalTemplateId=0
```

### Production (Brevo Templates)

After creating templates in Brevo, enable templates and add IDs:

```
# Enable Brevo templates
EmailTemplates__UseTemplates=true

# Template IDs from Brevo dashboard
EmailTemplates__VerificationEmailTemplateId=1
EmailTemplates__SubscriptionExpiry7DaysTemplateId=2
EmailTemplates__SubscriptionExpiry3DaysTemplateId=3
EmailTemplates__SubscriptionExpiry1DayTemplateId=4
EmailTemplates__SubscriptionExpiredTemplateId=5
EmailTemplates__SubscriptionRenewalTemplateId=6
```

Replace the numbers with your actual template IDs from Brevo.

---

## .NET Configuration Mapping

The `__` (double underscore) in environment variables maps to `:` (colon) in JSON:

```
Environment Variable:        →    appsettings.json:
EmailTemplates__UseTemplates        EmailTemplates.UseTemplates
EmailTemplates__VerificationEmailTemplateId    EmailTemplates.VerificationEmailTemplateId
```

---

## appsettings.json Equivalent

### Development
```json
{
  "Brevo": {
    "BaseUrl": "https://api.brevo.com",
    "ApiKey": "",
    "SenderEmail": "noreply@example.com",
    "SenderName": "Kromic Flow"
  },
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

### Production
```json
{
  "Brevo": {
    "BaseUrl": "https://api.brevo.com",
    "ApiKey": "xkeysib-XXXXXXXXXXXXXXXXXXXX",
    "SenderEmail": "noreply@flow.kromic.in",
    "SenderName": "Kromic Flow"
  },
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

---

## Setup Instructions

### Step 1: Copy Environment Variables

Copy `.env.example` to `.env`:

```bash
cp .env.example .env
```

### Step 2: Set Brevo Credentials

1. Get API key from https://app.brevo.com/settings/keys/api
2. Update `.env`:

```
Brevo__ApiKey=xkeysib-XXXXXXXXXXXXXXXXXXXX
Brevo__SenderEmail=noreply@flow.kromic.in
```

### Step 3: For Development

Keep default template settings (UseTemplates=false):

```
EmailTemplates__UseTemplates=false
```

✅ Ready to use with inline HTML templates

### Step 4: For Production

1. Create 6 templates in Brevo (see [Brevo Setup Guide](./05-BrevoSetupGuide.md))
2. Note template IDs
3. Update `.env` or environment variables:

```
EmailTemplates__UseTemplates=true
EmailTemplates__VerificationEmailTemplateId=1
EmailTemplates__SubscriptionExpiry7DaysTemplateId=2
EmailTemplates__SubscriptionExpiry3DaysTemplateId=3
EmailTemplates__SubscriptionExpiry1DayTemplateId=4
EmailTemplates__SubscriptionExpiredTemplateId=5
EmailTemplates__SubscriptionRenewalTemplateId=6
```

✅ Ready to use with Brevo templates

---

## Environment Variable Format

### On Windows (with `.env` file)

```
EmailTemplates__UseTemplates=true
EmailTemplates__VerificationEmailTemplateId=1
```

### On Linux/Docker (command line)

```bash
export EmailTemplates__UseTemplates=true
export EmailTemplates__VerificationEmailTemplateId=1
```

### On Render/Cloud Platform

Set as environment variables in deployment dashboard:

- `EmailTemplates__UseTemplates` = `true`
- `EmailTemplates__VerificationEmailTemplateId` = `1`
- etc.

---

## Verification

### Check Environment is Loaded

The application will log on startup:

```
Information: EmailTemplates configuration loaded
Information: UseTemplates: true/false
Information: Using Brevo templates: true/false
```

### Test Email Sending

1. Call `POST /api/v1/users/verify-email` with a test email
2. Check Brevo dashboard → Emails → Email Activity
3. Verify email was sent and template rendered correctly

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Emails not sending | Check `Brevo__ApiKey` is set correctly |
| "Template not found" | Verify template IDs match Brevo dashboard |
| Using wrong template | Check `EmailTemplates__UseTemplates` setting |
| Environment variable not loaded | Check variable name uses `__` (double underscore) |
| Inline HTML still sending | Set `EmailTemplates__UseTemplates=true` |

---

## Reference

- [.env.example](.env.example) - Example environment file
- [Configuration Guide](./01-ConfigurationGuide.md) - Detailed configuration
- [Brevo Setup Guide](./05-BrevoSetupGuide.md) - Creating templates in Brevo
- [Ready-to-Use Templates](./04-ReadyToUseTemplates.md) - HTML templates

