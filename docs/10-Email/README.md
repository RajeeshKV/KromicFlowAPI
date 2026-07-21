# Email System Documentation

Complete documentation for KromicFlow's email template and notification system.

## 🚀 Quick Links

- ⭐ **[Ready-to-Use HTML Templates](./04-ReadyToUseTemplates.md)** - Copy-paste into Brevo
- ⭐ **[Brevo Setup Guide](./05-BrevoSetupGuide.md)** - Step-by-step creation instructions
- 📋 **[Summary](./SUMMARY.md)** - Complete overview and architecture

---

## 📖 Full Documentation

### Getting Started
1. **[Summary](./SUMMARY.md)** - Overview of everything that's implemented
2. **[Ready-to-Use Templates](./04-ReadyToUseTemplates.md)** - Copy HTML templates into Brevo
3. **[Brevo Setup Guide](./05-BrevoSetupGuide.md)** - Step-by-step instructions
4. **[Environment Variables](./06-EnvironmentVariables.md)** - How to configure

### Reference
5. **[Configuration Guide](./01-ConfigurationGuide.md)** - Configuration properties and options
6. **[Template Specifications](./02-TemplateSpecifications.md)** - Detailed template specs
7. **[Code Examples](./03-CodeExamples.md)** - Implementation examples
8. **[Brevo Overview](./00-BrevoTemplates.md)** - System architecture

---

## Overview

KromicFlow uses Brevo for transactional email delivery with a flexible template system that supports:

1. **Inline HTML Rendering** (Default) - Templates are hardcoded in the application
2. **Brevo Template System** (Optional) - Uses Brevo's template rendering engine

## Email Templates Supported

| # | Template | When Sent | Variables |
|---|----------|-----------|-----------|
| 1 | **Email Verification** | User requests email verification | `fullName`, `verificationLink` |
| 2 | **Expiry - 7 Days** | 7 days before subscription expiry | `fullName`, `planName`, `expiryDate`, `renewalLink` |
| 3 | **Expiry - 3 Days** | 3 days before subscription expiry | `fullName`, `planName`, `expiryDate`, `renewalLink` |
| 4 | **Expiry - 1 Day** | 1 day before subscription expiry | `fullName`, `planName`, `expiryDate`, `renewalLink` |
| 5 | **Subscription Expired** | On subscription expiry date | `fullName`, `planName`, `renewalLink` |
| 6 | **Renewal Confirmation** | After successful renewal | `fullName`, `planName`, `newExpiryDate`, `amount`, `transactionId` |

## Quick Start

### Option 1: Use Default (Inline HTML)
No configuration needed. Templates work immediately.

```json
{
  "EmailTemplates": {
    "UseTemplates": false
  }
}
```

### Option 2: Use Brevo Templates

1. Read: [Ready-to-Use Templates](./04-ReadyToUseTemplates.md)
2. Follow: [Brevo Setup Guide](./05-BrevoSetupGuide.md)
3. Update configuration with template IDs
4. Deploy!

## Endpoints

### Email Verification
- **POST** `/api/v1/users/verify-email` - Send verification email
- **POST** `/api/v1/users/verify-email-token` - Verify email with token
- **GET** `/api/v1/users/profile` - Check `emailVerified` status

## Configuration

### Development (Default - Inline HTML)
```json
{
  "EmailTemplates": {
    "UseTemplates": false
  }
}
```

### Production (Brevo Templates)
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

## For UI/Design Team

To create email templates in Brevo:

1. **Read**: [Ready-to-Use Templates](./04-ReadyToUseTemplates.md) - All HTML ready to copy
2. **Follow**: [Brevo Setup Guide](./05-BrevoSetupGuide.md) - Step-by-step instructions
3. **Login**: https://app.brevo.com
4. **Create**: 6 templates using provided HTML
5. **Share**: Template IDs with backend team

No technical knowledge required!

## For Backend Team

To implement new email templates:

1. Review: [Code Examples](./03-CodeExamples.md)
2. Add template type to `EmailTemplateType` enum
3. Add configuration property to `EmailTemplateOptions`
4. Implement render methods in `EmailTemplateService`
5. Use in handlers via `IEmailTemplateService`

## Architecture

```
API Controllers
    ↓
Command Handlers
    ↓
IEmailTemplateService (Abstractions)
    ↓
EmailTemplateService (Infrastructure)
    ├─ Renders subjects/bodies
    ├─ Gets template IDs
    └─ Checks if templates enabled
    ↓
INotificationSender (Abstractions)
    ↓
BrevoNotificationSender (Infrastructure)
    └─ Sends via Brevo API
    ↓
Brevo SMTP API
```

## Features

✅ 6 email templates defined with all parameters  
✅ Flexible inline HTML + Brevo template system  
✅ Configuration-based switching (no code changes)  
✅ Secure tokens with rate limiting  
✅ Professional email designs  
✅ Ready-to-use HTML templates  
✅ Comprehensive documentation  

## Status

- ✅ Backend implementation complete
- ✅ Email verification endpoints ready
- ✅ Template system ready
- ✅ Brevo integration ready
- ✅ Documentation complete
- ✅ Build passing: 0 errors
- ✅ Tests passing: 2/2

## Next Steps

1. **Create templates** in Brevo (see [Brevo Setup Guide](./05-BrevoSetupGuide.md))
2. **Record template IDs**
3. **Update configuration**
4. **Deploy** - That's it!

---

## References

- [Brevo API Docs](https://developers.brevo.com/)
- [Brevo Template System](https://help.brevo.com/hc/en-us/articles/209467485)
- [Configuration Guide](./01-ConfigurationGuide.md)
- [Template Specifications](./02-TemplateSpecifications.md)
- [Code Examples](./03-CodeExamples.md)
