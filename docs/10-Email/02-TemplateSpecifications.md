# Email Template Specifications for UI/Design Team

This document specifies the exact email templates needed for Brevo. Use this to create templates in the Brevo console.

## ⚠️ Important: URLs Are Handled by Backend

**All URLs in email templates (verification links, renewal links, etc.) are generated and managed by the backend API.** 

The backend will:
1. Generate all URLs with proper tokens and parameters
2. Send emails with complete, clickable links
3. **Redirect users to the correct Frontend URL** when they click the link

**Frontend does NOT need to handle URL generation or token parameters** - just display the pages that the backend redirects to.

---

## Template 1: Email Verification

**Template ID**: *(You will assign this in Brevo)*
**Template Name**: `Email Verification - KromicFlow`
**Subject Line**: `Verify your KromicFlow email`

### Template Variables

Use these exact variable names in Brevo:
- `{{fullName}}` - User's full name
- `{{verificationLink}}` - **Full email verification URL** (backend generates this with token)

### Subject Line
```
Verify your KromicFlow email
```

### HTML Body
```html
<h2>Verify Your Email</h2>
<p>Hi {{fullName}},</p>
<p>Thank you for signing up for KromicFlow!</p>
<p>To activate your automations, please verify your email by clicking the link below:</p>
<p>
  <a href="{{verificationLink}}" 
     style="background-color: #3b82f6; color: white; padding: 10px 20px; 
            text-decoration: none; border-radius: 4px; display: inline-block;">
    Verify Email
  </a>
</p>
<p>Or copy and paste this link: {{verificationLink}}</p>
<p>This link expires in 24 hours.</p>
<p>If you didn't sign up, you can ignore this email.</p>
<p>Best regards,<br>KromicFlow Team</p>
```

### Design Notes
- Primary button color: **#3b82f6** (Blue)
- Use professional tone, welcoming
- Include link twice (button + text) for email clients that don't render links properly

---

## Template 2: Subscription Expiry Reminder - 7 Days

**Template ID**: *(You will assign this in Brevo)*
**Template Name**: `Subscription Expiry - 7 Days - KromicFlow`
**Subject Line**: `Your KromicFlow subscription expires in 7 days`

### Template Variables
- `{{fullName}}` - User's full name
- `{{planName}}` - Subscription plan name (e.g., "Starter", "Pro")
- `{{expiryDate}}` - Subscription expiry date (format: YYYY-MM-DD)
- `{{renewalLink}}` - Link to subscription renewal page

### Subject Line
```
Your KromicFlow subscription expires in 7 days
```

### HTML Body
```html
<h2>Your Subscription Expires Soon</h2>
<p>Hi {{fullName}},</p>
<p>Your {{planName}} subscription will expire on <strong>{{expiryDate}}</strong> (in 7 days).</p>
<p>To continue using KromicFlow and keep your automations running, please renew your subscription:</p>
<p>
  <a href="{{renewalLink}}" 
     style="background-color: #10b981; color: white; padding: 10px 20px; 
            text-decoration: none; border-radius: 4px; display: inline-block;">
    Renew Subscription
  </a>
</p>
<p>Without renewal, your automations will be disabled.</p>
<p>Best regards,<br>KromicFlow Team</p>
```

### Design Notes
- Button color: **#10b981** (Green) - Informational tone
- Emphasize the action is optional but recommended
- Include clear date of expiry

---

## Template 3: Subscription Expiry Reminder - 3 Days

**Template ID**: *(You will assign this in Brevo)*
**Template Name**: `Subscription Expiry - 3 Days - KromicFlow`
**Subject Line**: `Your KromicFlow subscription expires in 3 days`

### Template Variables
- `{{fullName}}` - User's full name
- `{{planName}}` - Subscription plan name
- `{{expiryDate}}` - Subscription expiry date
- `{{renewalLink}}` - Link to subscription renewal page

### Subject Line
```
Your KromicFlow subscription expires in 3 days
```

### HTML Body
```html
<h2>Your Subscription Expires in 3 Days</h2>
<p>Hi {{fullName}},</p>
<p><strong>Urgent:</strong> Your {{planName}} subscription will expire on <strong>{{expiryDate}}</strong> (in 3 days).</p>
<p>Please renew your subscription to avoid losing access to your automations:</p>
<p>
  <a href="{{renewalLink}}" 
     style="background-color: #f59e0b; color: white; padding: 10px 20px; 
            text-decoration: none; border-radius: 4px; display: inline-block;">
    Renew Now
  </a>
</p>
<p>Your automations will be automatically disabled if your subscription expires.</p>
<p>Best regards,<br>KromicFlow Team</p>
```

### Design Notes
- Button color: **#f59e0b** (Amber/Orange) - Urgent tone
- Use "Urgent" label to convey importance
- Emphasize consequence: "automations will be disabled"

---

## Template 4: Subscription Expiry Reminder - 1 Day

**Template ID**: *(You will assign this in Brevo)*
**Template Name**: `Subscription Expiry - 1 Day - KromicFlow`
**Subject Line**: `Your KromicFlow subscription expires tomorrow`

### Template Variables
- `{{fullName}}` - User's full name
- `{{planName}}` - Subscription plan name
- `{{expiryDate}}` - Subscription expiry date
- `{{renewalLink}}` - Link to subscription renewal page

### Subject Line
```
Your KromicFlow subscription expires tomorrow
```

### HTML Body
```html
<h2>Your Subscription Expires Tomorrow</h2>
<p>Hi {{fullName}},</p>
<p><strong style="color: #dc2626;">URGENT:</strong> Your {{planName}} subscription will expire on <strong>{{expiryDate}}</strong> (tomorrow).</p>
<p>Renew your subscription immediately to keep your automations active:</p>
<p>
  <a href="{{renewalLink}}" 
     style="background-color: #dc2626; color: white; padding: 10px 20px; 
            text-decoration: none; border-radius: 4px; display: inline-block;">
    Renew Immediately
  </a>
</p>
<p>All automations will be disabled starting tomorrow.</p>
<p>Best regards,<br>KromicFlow Team</p>
```

### Design Notes
- Button color: **#dc2626** (Red) - Critical/urgent action required
- Use "URGENT" in caps for maximum visibility
- Emphasize "starting tomorrow" to drive action

---

## Template 5: Subscription Expired

**Template ID**: *(You will assign this in Brevo)*
**Template Name**: `Subscription Expired - KromicFlow`
**Subject Line**: `Your KromicFlow subscription has expired`

### Template Variables
- `{{fullName}}` - User's full name
- `{{planName}}` - Subscription plan name
- `{{renewalLink}}` - Link to subscription renewal page

### Subject Line
```
Your KromicFlow subscription has expired
```

### HTML Body
```html
<h2>Your Subscription Has Expired</h2>
<p>Hi {{fullName}},</p>
<p>Your {{planName}} subscription has expired.</p>
<p><strong>Your automations are now disabled.</strong> To resume your automations and continue using KromicFlow, please renew your subscription:</p>
<p>
  <a href="{{renewalLink}}" 
     style="background-color: #dc2626; color: white; padding: 10px 20px; 
            text-decoration: none; border-radius: 4px; display: inline-block;">
    Renew Subscription
  </a>
</p>
<p>Don't lose your automation setup. Renew now to get back online.</p>
<p>Best regards,<br>KromicFlow Team</p>
```

### Design Notes
- Button color: **#dc2626** (Red) - Critical action required
- Use "Your automations are now disabled" in bold to emphasize consequence
- Motivate with "Don't lose your automation setup"

---

## Template 6: Subscription Renewal Confirmation

**Template ID**: *(You will assign this in Brevo)*
**Template Name**: `Subscription Renewal - KromicFlow`
**Subject Line**: `Your KromicFlow subscription has been renewed`

### Template Variables
- `{{fullName}}` - User's full name
- `{{planName}}` - Subscription plan name
- `{{newExpiryDate}}` - New subscription expiry date (format: YYYY-MM-DD)
- `{{amount}}` - Renewal amount (e.g., "₹999" or "$12.99")
- `{{transactionId}}` - Razorpay transaction ID (e.g., "rzp_1234567890")

### Subject Line
```
Your KromicFlow subscription has been renewed
```

### HTML Body
```html
<h2>Subscription Renewed Successfully</h2>
<p>Hi {{fullName}},</p>
<p>Thank you for renewing your {{planName}} subscription!</p>
<p><strong>Renewal Details:</strong></p>
<ul>
  <li>Plan: {{planName}}</li>
  <li>Amount: {{amount}}</li>
  <li>New Expiry Date: {{newExpiryDate}}</li>
  <li>Transaction ID: {{transactionId}}</li>
</ul>
<p>Your automations are now active and will continue running without interruption.</p>
<p>If you have any questions or need assistance, feel free to reach out to us.</p>
<p>Best regards,<br>KromicFlow Team</p>
```

### Design Notes
- Use list format for transaction details
- Reassure with "automations are now active"
- Include transaction ID for customer reference
- Professional, celebratory tone

---

## Summary Table

| Template # | Name | Subject | Key Variable | Button Color | Tone |
|-----------|------|---------|--------------|--------------|------|
| 1 | Email Verification | "Verify your KromicFlow email" | `verificationLink` | Blue (#3b82f6) | Welcoming |
| 2 | Expiry - 7 Days | "expires in 7 days" | `renewalLink` | Green (#10b981) | Informational |
| 3 | Expiry - 3 Days | "expires in 3 days" | `renewalLink` | Amber (#f59e0b) | Urgent |
| 4 | Expiry - 1 Day | "expires tomorrow" | `renewalLink` | Red (#dc2626) | Critical |
| 5 | Expired | "has expired" | `renewalLink` | Red (#dc2626) | Critical |
| 6 | Renewal | "has been renewed" | `transactionId` | N/A (Confirmation) | Celebratory |

---

## Steps to Create in Brevo Console

1. **Log in** to [Brevo Console](https://app.brevo.com)
2. **Navigate** to **Templates** → **Transactional**
3. **Click** "Create a template"
4. **Choose** "Design" (not "Code")
5. **Build template** using drag-and-drop editor:
   - Add heading (h2) with title
   - Add paragraphs with content above
   - Add button with link variable `{{renewalLink}}` or `{{verificationLink}}`
   - Add color-coded button (use colors from table above)
6. **Save** the template
7. **Note** the Template ID shown in the template list
8. **Repeat** for all 6 templates

---

## Template ID Recording

Once you create all templates in Brevo, record the template IDs and update `appsettings.Production.json`:

```json
{
  "EmailTemplates": {
    "UseTemplates": true,
    "VerificationEmailTemplateId": [TEMPLATE_ID_1],
    "SubscriptionExpiry7DaysTemplateId": [TEMPLATE_ID_2],
    "SubscriptionExpiry3DaysTemplateId": [TEMPLATE_ID_3],
    "SubscriptionExpiry1DayTemplateId": [TEMPLATE_ID_4],
    "SubscriptionExpiredTemplateId": [TEMPLATE_ID_5],
    "SubscriptionRenewalTemplateId": [TEMPLATE_ID_6]
  }
}
```

Replace `[TEMPLATE_ID_X]` with the actual template IDs from Brevo.

---

## Testing Templates

After creating templates in Brevo:

1. **Test Email Verification**:
   - Call `POST /api/v1/users/verify-email` with an email
   - Check Brevo dashboard → Emails → Email Activity
   - Verify email renders correctly

2. **Test Subscription Emails** (Backend Job):
   - Wait for scheduled job to run (or manually trigger in code)
   - Check Brevo Email Activity dashboard
   - Verify parameters render correctly

3. **Preview in Brevo**:
   - Go to template → Click "Preview"
   - Fill in sample variables
   - Review rendering

---

## Support

- **Brevo Docs**: https://developers.brevo.com/
- **Template Editor Guide**: https://help.brevo.com/hc/en-us/articles/209467485
- **Questions?** Contact the development team

