# Quick Setup Guide: Creating Templates in Brevo

Step-by-step guide to create all 6 email templates in Brevo console.

## Login to Brevo

Go to: https://app.brevo.com/login

---

## Create Template 1: Email Verification

1. Click **Templates** (left sidebar)
2. Click **Transactional** 
3. Click **Create a template**
4. Choose **Design** editor
5. In the editor, look for **HTML** option at the bottom
6. Click **HTML** 
7. Clear any existing HTML and paste this:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f9fafb; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }
        .header { background-color: #3b82f6; color: #ffffff; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .content h2 { color: #1f2937; font-size: 20px; margin-top: 0; margin-bottom: 16px; }
        .content p { margin: 0 0 16px 0; color: #4b5563; line-height: 1.6; }
        .button { display: inline-block; background-color: #3b82f6; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; text-align: center; }
        .button:hover { background-color: #2563eb; }
        .link-text { color: #3b82f6; word-break: break-all; font-size: 13px; margin: 16px 0; }
        .footer { background-color: #f3f4f6; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; }
        .footer p { margin: 0 0 8px 0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🔐 Verify Your Email</h1>
        </div>
        <div class="content">
            <h2>Hi {{fullName}},</h2>
            <p>Thank you for signing up for <strong>KromicFlow</strong>!</p>
            <p>To activate your automations and start managing your Instagram account, please verify your email by clicking the button below:</p>
            <div style="text-align: center;">
                <a href="{{verificationLink}}" class="button">✓ Verify Email</a>
            </div>
            <p>Or copy and paste this link in your browser:</p>
            <div class="link-text">{{verificationLink}}</div>
            <p style="color: #9ca3af; font-size: 13px;">⏱️ This link expires in <strong>24 hours</strong>.</p>
            <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;">
            <p style="color: #9ca3af; font-size: 13px;">If you didn't create this account, you can safely ignore this email.</p>
        </div>
        <div class="footer">
            <p><strong>KromicFlow</strong></p>
            <p>Automate your Instagram business effortlessly</p>
            <p style="margin-top: 16px; color: #9ca3af;">© 2026 KromicFlow. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
```

8. Set **Subject**: `Verify your KromicFlow email`
9. Click **Save & Name**
10. Name it: `Email Verification`
11. Click **Save**
12. **Note the Template ID** (shown on the right side of the screen)

---

## Create Template 2: Subscription Expiry - 7 Days

1. Click **Create a template** again
2. Choose **Design**
3. Click **HTML**
4. Paste HTML from [docs/10-Email/04-ReadyToUseTemplates.md](./04-ReadyToUseTemplates.md#template-2-subscription-expiry---7-days)
5. Set **Subject**: `Your KromicFlow subscription expires in 7 days`
6. Click **Save & Name**
7. Name it: `Subscription Expiry - 7 Days`
8. Click **Save**
9. **Note the Template ID**

---

## Create Template 3: Subscription Expiry - 3 Days

1. Click **Create a template** again
2. Choose **Design**
3. Click **HTML**
4. Paste HTML from [docs/10-Email/04-ReadyToUseTemplates.md](./04-ReadyToUseTemplates.md#template-3-subscription-expiry---3-days)
5. Set **Subject**: `⚠️ Your KromicFlow subscription expires in 3 days`
6. Click **Save & Name**
7. Name it: `Subscription Expiry - 3 Days`
8. Click **Save**
9. **Note the Template ID**

---

## Create Template 4: Subscription Expiry - 1 Day

1. Click **Create a template** again
2. Choose **Design**
3. Click **HTML**
4. Paste HTML from [docs/10-Email/04-ReadyToUseTemplates.md](./04-ReadyToUseTemplates.md#template-4-subscription-expiry---1-day)
5. Set **Subject**: `🚨 Your KromicFlow subscription expires TOMORROW`
6. Click **Save & Name**
7. Name it: `Subscription Expiry - 1 Day`
8. Click **Save**
9. **Note the Template ID**

---

## Create Template 5: Subscription Expired

1. Click **Create a template** again
2. Choose **Design**
3. Click **HTML**
4. Paste HTML from [docs/10-Email/04-ReadyToUseTemplates.md](./04-ReadyToUseTemplates.md#template-5-subscription-expired)
5. Set **Subject**: `🔴 Your KromicFlow subscription has expired`
6. Click **Save & Name**
7. Name it: `Subscription Expired`
8. Click **Save**
9. **Note the Template ID**

---

## Create Template 6: Subscription Renewal Confirmation

1. Click **Create a template** again
2. Choose **Design**
3. Click **HTML**
4. Paste HTML from [docs/10-Email/04-ReadyToUseTemplates.md](./04-ReadyToUseTemplates.md#template-6-subscription-renewal-confirmation)
5. Set **Subject**: `✅ Your KromicFlow subscription has been renewed`
6. Click **Save & Name**
7. Name it: `Subscription Renewal`
8. Click **Save**
9. **Note the Template ID**

---

## Configuration

Once all 6 templates are created, update your backend configuration.

### Step 1: Record Template IDs

Create a file or document with your template IDs:

```
Template 1 (Email Verification): ___
Template 2 (Expiry - 7 Days): ___
Template 3 (Expiry - 3 Days): ___
Template 4 (Expiry - 1 Day): ___
Template 5 (Subscription Expired): ___
Template 6 (Renewal Confirmation): ___
```

### Step 2: Update appsettings.Production.json

Add this section to your `appsettings.Production.json`:

```json
{
  "EmailTemplates": {
    "UseTemplates": true,
    "VerificationEmailTemplateId": YOUR_TEMPLATE_1_ID,
    "SubscriptionExpiry7DaysTemplateId": YOUR_TEMPLATE_2_ID,
    "SubscriptionExpiry3DaysTemplateId": YOUR_TEMPLATE_3_ID,
    "SubscriptionExpiry1DayTemplateId": YOUR_TEMPLATE_4_ID,
    "SubscriptionExpiredTemplateId": YOUR_TEMPLATE_5_ID,
    "SubscriptionRenewalTemplateId": YOUR_TEMPLATE_6_ID
  }
}
```

**Example:**
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

### Step 3: Deploy

Deploy your application with the updated configuration. Emails will now be sent using Brevo templates!

---

## Verify Templates Are Working

1. Test email verification:
   - Call `POST /api/v1/users/verify-email`
   - User should receive email from Brevo template

2. Check Brevo Dashboard:
   - Go to **Emails** → **Transactional** → **Email Activity**
   - You should see sent emails with your template names

3. Verify parameters rendered correctly:
   - Check if `{{fullName}}` shows user's name
   - Check if `{{verificationLink}}` shows actual link
   - Check if colors and styling display properly

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Template not sending | Check `Brevo__ApiKey` in configuration |
| Variables showing as `{{variable}}` | Check variable names match exactly |
| Wrong template sending | Verify Template IDs in config are correct |
| Email styling broken | Check HTML pastes correctly (no truncation) |
| "Transactional" not visible in sidebar | Click on "Templates" first, then "Transactional" |

---

## Need Help?

- **Brevo Docs**: https://help.brevo.com
- **Template Editor Help**: https://help.brevo.com/hc/en-us/articles/209467485
- **API Docs**: https://developers.brevo.com

