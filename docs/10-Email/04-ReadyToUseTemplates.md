# Ready-to-Use Email Templates for Brevo

Copy-paste these HTML templates directly into Brevo console. Each template includes all variables and styling.

---

## Template 1: Email Verification

**Subject:** `Verify your KromicFlow email`

**HTML Body:**
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

---

## Template 2: Subscription Expiry - 7 Days

**Subject:** `Your KromicFlow subscription expires in 7 days`

**HTML Body:**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f9fafb; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }
        .header { background-color: #10b981; color: #ffffff; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .content h2 { color: #1f2937; font-size: 20px; margin-top: 0; margin-bottom: 16px; }
        .content p { margin: 0 0 16px 0; color: #4b5563; line-height: 1.6; }
        .button { display: inline-block; background-color: #10b981; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; text-align: center; }
        .button:hover { background-color: #059669; }
        .info-box { background-color: #ecfdf5; border-left: 4px solid #10b981; padding: 16px; margin: 20px 0; border-radius: 4px; }
        .info-box p { margin: 8px 0; color: #047857; }
        .footer { background-color: #f3f4f6; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; }
        .footer p { margin: 0 0 8px 0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>⏰ Subscription Expiring Soon</h1>
        </div>
        <div class="content">
            <h2>Hi {{fullName}},</h2>
            <p>Your <strong>{{planName}}</strong> subscription will expire on <strong>{{expiryDate}}</strong> (in 7 days).</p>
            <p>To continue using KromicFlow and keep your automations running without interruption, please renew your subscription:</p>
            <div style="text-align: center;">
                <a href="{{renewalLink}}" class="button">💳 Renew Subscription</a>
            </div>
            <div class="info-box">
                <p><strong>What happens if I don't renew?</strong></p>
                <p>Your automations will be automatically disabled after your subscription expires. You can always re-enable them by renewing.</p>
            </div>
            <p style="color: #9ca3af; font-size: 13px;">Need help? Contact our support team.</p>
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

---

## Template 3: Subscription Expiry - 3 Days

**Subject:** `⚠️ Your KromicFlow subscription expires in 3 days`

**HTML Body:**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f9fafb; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }
        .header { background-color: #f59e0b; color: #ffffff; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .content h2 { color: #1f2937; font-size: 20px; margin-top: 0; margin-bottom: 16px; }
        .content p { margin: 0 0 16px 0; color: #4b5563; line-height: 1.6; }
        .urgent { color: #d97706; font-weight: 600; }
        .button { display: inline-block; background-color: #f59e0b; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; text-align: center; }
        .button:hover { background-color: #d97706; }
        .warning-box { background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 16px; margin: 20px 0; border-radius: 4px; }
        .warning-box p { margin: 8px 0; color: #92400e; }
        .footer { background-color: #f3f4f6; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; }
        .footer p { margin: 0 0 8px 0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>⏰ Subscription Expiring in 3 Days</h1>
        </div>
        <div class="content">
            <h2>Hi {{fullName}},</h2>
            <p class="urgent">⚠️ URGENT: Your {{planName}} subscription will expire on {{expiryDate}} (in just 3 days).</p>
            <p>Please renew your subscription now to avoid losing access to your automations:</p>
            <div style="text-align: center;">
                <a href="{{renewalLink}}" class="button">🔄 Renew Now</a>
            </div>
            <div class="warning-box">
                <p><strong>⚡ Important:</strong> Your automations will be disabled in 3 days if you don't renew.</p>
                <p>Don't lose your automation setup. Renewing takes less than 2 minutes.</p>
            </div>
            <p style="color: #9ca3af; font-size: 13px;">Questions? Contact our support team immediately.</p>
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

---

## Template 4: Subscription Expiry - 1 Day

**Subject:** `🚨 Your KromicFlow subscription expires TOMORROW`

**HTML Body:**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f9fafb; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }
        .header { background-color: #dc2626; color: #ffffff; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .content h2 { color: #1f2937; font-size: 20px; margin-top: 0; margin-bottom: 16px; }
        .content p { margin: 0 0 16px 0; color: #4b5563; line-height: 1.6; }
        .critical { color: #dc2626; font-weight: 700; font-size: 16px; }
        .button { display: inline-block; background-color: #dc2626; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; text-align: center; font-size: 16px; }
        .button:hover { background-color: #b91c1c; }
        .critical-box { background-color: #fee2e2; border-left: 4px solid #dc2626; padding: 16px; margin: 20px 0; border-radius: 4px; }
        .critical-box p { margin: 8px 0; color: #7f1d1d; }
        .footer { background-color: #f3f4f6; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; }
        .footer p { margin: 0 0 8px 0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🚨 CRITICAL: Expires Tomorrow</h1>
        </div>
        <div class="content">
            <h2>Hi {{fullName}},</h2>
            <p class="critical">⛔ CRITICAL: Your {{planName}} subscription expires on {{expiryDate}} (TOMORROW).</p>
            <p>Renew your subscription immediately to keep your automations active:</p>
            <div style="text-align: center;">
                <a href="{{renewalLink}}" class="button">⚡ RENEW IMMEDIATELY</a>
            </div>
            <div class="critical-box">
                <p><strong>🛑 All automations will be disabled starting tomorrow.</strong></p>
                <p>Don't lose your automation setup and customer data. Renew now in less than 2 minutes.</p>
                <p><strong>This is your last chance to renew before expiry.</strong></p>
            </div>
            <p style="color: #9ca3af; font-size: 13px;">Questions? Contact support immediately.</p>
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

---

## Template 5: Subscription Expired

**Subject:** `🔴 Your KromicFlow subscription has expired`

**HTML Body:**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f9fafb; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }
        .header { background-color: #dc2626; color: #ffffff; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .content h2 { color: #1f2937; font-size: 20px; margin-top: 0; margin-bottom: 16px; }
        .content p { margin: 0 0 16px 0; color: #4b5563; line-height: 1.6; }
        .disabled { background-color: #fecaca; color: #dc2626; padding: 12px; border-radius: 6px; font-weight: 600; margin: 16px 0; text-align: center; }
        .button { display: inline-block; background-color: #dc2626; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; text-align: center; font-size: 16px; }
        .button:hover { background-color: #b91c1c; }
        .info-box { background-color: #fee2e2; border-left: 4px solid #dc2626; padding: 16px; margin: 20px 0; border-radius: 4px; }
        .info-box p { margin: 8px 0; color: #7f1d1d; }
        .footer { background-color: #f3f4f6; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; }
        .footer p { margin: 0 0 8px 0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🔴 Subscription Expired</h1>
        </div>
        <div class="content">
            <h2>Hi {{fullName}},</h2>
            <p>Your {{planName}} subscription has expired.</p>
            <div class="disabled">
                ❌ Your automations are now DISABLED
            </div>
            <p>To resume your automations and continue using KromicFlow, please renew your subscription now:</p>
            <div style="text-align: center;">
                <a href="{{renewalLink}}" class="button">🔓 Renew Subscription Now</a>
            </div>
            <div class="info-box">
                <p><strong>💡 Don't lose your automation setup!</strong></p>
                <p>Renew now to:</p>
                <p>✓ Re-activate all your automations<br>✓ Keep your configuration & data<br>✓ Resume comment management & replies</p>
            </div>
            <p style="text-align: center; margin-top: 24px;">
                <strong style="color: #dc2626;">Renew in less than 2 minutes</strong>
            </p>
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

---

## Template 6: Subscription Renewal Confirmation

**Subject:** `✅ Your KromicFlow subscription has been renewed`

**HTML Body:**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f9fafb; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); overflow: hidden; }
        .header { background-color: #10b981; color: #ffffff; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .content h2 { color: #1f2937; font-size: 20px; margin-top: 0; margin-bottom: 16px; }
        .content p { margin: 0 0 16px 0; color: #4b5563; line-height: 1.6; }
        .success { background-color: #ecfdf5; border-left: 4px solid #10b981; padding: 16px; margin: 20px 0; border-radius: 4px; }
        .success p { margin: 8px 0; color: #047857; }
        .details-box { background-color: #f0fdf4; border: 1px solid #dcfce7; padding: 20px; margin: 20px 0; border-radius: 6px; }
        .detail-row { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #dcfce7; }
        .detail-row:last-child { border-bottom: none; }
        .detail-label { font-weight: 600; color: #047857; }
        .detail-value { color: #065f46; text-align: right; }
        .button { display: inline-block; background-color: #10b981; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: 600; margin: 20px 0; text-align: center; }
        .button:hover { background-color: #059669; }
        .footer { background-color: #f3f4f6; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; }
        .footer p { margin: 0 0 8px 0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>✅ Renewal Successful!</h1>
        </div>
        <div class="content">
            <h2>Hi {{fullName}},</h2>
            <p>Thank you for renewing your KromicFlow subscription! 🎉</p>
            <div class="success">
                <p><strong>✓ Your automations are now active and running</strong></p>
                <p>Your subscription has been renewed successfully. You can now continue managing your Instagram automations without interruption.</p>
            </div>
            <p><strong>Renewal Details:</strong></p>
            <div class="details-box">
                <div class="detail-row">
                    <span class="detail-label">Plan:</span>
                    <span class="detail-value">{{planName}}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Amount Paid:</span>
                    <span class="detail-value">{{amount}}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">New Expiry Date:</span>
                    <span class="detail-value">{{newExpiryDate}}</span>
                </div>
                <div class="detail-row">
                    <span class="detail-label">Transaction ID:</span>
                    <span class="detail-value">{{transactionId}}</span>
                </div>
            </div>
            <p>If you have any questions or need assistance, don't hesitate to contact our support team.</p>
            <div style="text-align: center;">
                <a href="https://flow.kromic.in/dashboard" class="button">📊 Go to Dashboard</a>
            </div>
            <p style="color: #9ca3af; font-size: 13px; text-align: center; margin-top: 24px;">Thank you for choosing KromicFlow!</p>
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

---

## Template Variables Summary

| Template | Variables |
|----------|-----------|
| **Template 1: Email Verification** | `{{fullName}}`, `{{verificationLink}}` |
| **Template 2: Expiry - 7 Days** | `{{fullName}}`, `{{planName}}`, `{{expiryDate}}`, `{{renewalLink}}` |
| **Template 3: Expiry - 3 Days** | `{{fullName}}`, `{{planName}}`, `{{expiryDate}}`, `{{renewalLink}}` |
| **Template 4: Expiry - 1 Day** | `{{fullName}}`, `{{planName}}`, `{{expiryDate}}`, `{{renewalLink}}` |
| **Template 5: Subscription Expired** | `{{fullName}}`, `{{planName}}`, `{{renewalLink}}` |
| **Template 6: Renewal Confirmation** | `{{fullName}}`, `{{planName}}`, `{{newExpiryDate}}`, `{{amount}}`, `{{transactionId}}` |

---

## Steps to Create in Brevo

1. Log in to https://app.brevo.com
2. Go to **Templates** → **Transactional** → **Create a template**
3. Choose **Design** (not Code)
4. Click **HTML** in the design editor
5. Copy the entire HTML from above and paste it
6. Set the **Subject line** (copy from above)
7. Click **Save & Name**
8. Note the **Template ID** (you'll need this for configuration)

---

## Configuration After Creating Templates

Once you create all 6 templates in Brevo, update your `appsettings.Production.json`:

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

Replace the numbers with your actual template IDs from Brevo.

