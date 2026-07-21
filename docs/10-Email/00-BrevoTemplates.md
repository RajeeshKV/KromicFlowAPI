# Brevo Email Templates

## Overview

KromicFlow uses Brevo (formerly Sendinblue) for transactional email delivery. Email templates are currently generated dynamically (inline HTML in handlers) rather than using Brevo's template system.

## Current Implementation

All emails are sent via `INotificationSender.SendEmailAsync()` which accepts:
- `toEmail`: recipient email address
- `subject`: email subject
- `body`: HTML email body (rendered inline)
- `cancellationToken`: cancellation token

Example:
```csharp
await notificationSender.SendEmailAsync(
    request.Email,
    "Subject Here",
    htmlBody,
    cancellationToken
);
```

## Email Templates

### 1. Email Verification Template

**File**: `src/KromicFlow.Application/Features/Users/SendVerificationEmail/SendVerificationEmailCommandHandler.cs`

**Purpose**: Sent when user requests email verification

**Subject**: `Verify your KromicFlow email`

**HTML Template**:
```html
<h2>Verify Your Email</h2>
<p>Hi {FullName},</p>
<p>Thank you for signing up for KromicFlow!</p>
<p>To activate your automations, please verify your email by clicking the link below:</p>
<p><a href="{verificationLink}" style="background-color: #3b82f6; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;">Verify Email</a></p>
<p>Or copy and paste this link: {verificationLink}</p>
<p>This link expires in 24 hours.</p>
<p>If you didn't sign up, you can ignore this email.</p>
<p>Best regards,<br>KromicFlow Team</p>
```

**Variables**:
- `{FullName}`: User's full name
- `{verificationLink}`: Verification URL with token (format: `https://yourdomain.com/verify-email?token={token}`)

**Rate Limiting**: 3 verification emails per hour per user

---

## Future: Brevo Template System

To use Brevo's built-in template system, follow these steps:

### 1. Create Template in Brevo Dashboard

1. Log in to [Brevo Console](https://app.brevo.com)
2. Go to **Templates** → **Transactional** → **Create a template**
3. Choose **Design** (drag-and-drop editor)
4. Build your email template
5. Add dynamic variables using `{{variable_name}}`
6. Save and note the Template ID

### 2. Update Code to Use Templates

Instead of inline HTML, use Brevo's template ID:

```csharp
// Current approach (inline HTML)
await notificationSender.SendEmailAsync(email, subject, htmlBody, ct);

// Future approach (template-based)
var payload = new {
    to = new[] { new { email = toEmail } },
    templateId = 123, // Brevo template ID
    params = new {
        fullName = user.FullName,
        verificationLink = verificationLink,
        // ... other template variables
    }
};
```

### 3. Update BrevoNotificationSender

Extend to support both inline HTML and template-based emails:

```csharp
public async Task<string?> SendEmailWithTemplateAsync(
    string toEmail, 
    int templateId, 
    Dictionary<string, string> templateVariables, 
    CancellationToken cancellationToken)
{
    var payload = new {
        to = new[] { new { email = toEmail } },
        templateId,
        @params = templateVariables
    };
    
    var response = await httpClient.PostAsJsonAsync("/v3/smtp/email", payload, cancellationToken);
    response.EnsureSuccessStatusCode();
    return response.Headers.TryGetValues("x-message-id", out var values) ? values.FirstOrDefault() : null;
}
```

---

## Brevo API Reference

- **API Docs**: https://developers.brevo.com/reference/sendtransacemail
- **Endpoint**: `POST /v3/smtp/email`
- **Authentication**: API key in `api-key` header
- **Rate Limits**: Check Brevo account plan

## Configuration

In `appsettings.json`:
```json
{
  "Brevo": {
    "ApiKey": "your-api-key-here",
    "SenderEmail": "noreply@example.com",
    "SenderName": "KromicFlow",
    "BaseUrl": "https://api.brevo.com/v3"
  }
}
```

## Testing

To test email sending:

1. Get a test email (e.g., Mailtrap, Ethereal Email)
2. Update Brevo recipient in test
3. Send and verify delivery

---

## Templates to Implement (Future)

1. ✅ **Email Verification** - Done (inline)
2. 🔄 **Subscription Expiry Reminders** - 7, 3, 1 days before + on expiry
3. 🔄 **Subscription Renewal** - Confirmation on successful renewal
4. 🔄 **Welcome Email** - Sent after OAuth signup
5. 🔄 **Password Reset** - Reset link (if added later)
6. 🔄 **Account Alerts** - Automation failures, plan limits reached

