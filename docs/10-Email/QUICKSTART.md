# Quick Start Guide - Email Templates

Get up and running in 5 minutes.

---

## 🚀 For Development (Right Now)

No setup needed! Emails work immediately with inline HTML templates.

### Test It

1. **Start your app**:
   ```bash
   dotnet run
   ```

2. **Send a test email**:
   ```bash
   curl -X POST https://localhost:5000/api/v1/users/verify-email \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -d '{"email": "test@example.com"}'
   ```

3. **Check Brevo dashboard**:
   - Go to https://app.brevo.com
   - Click **Emails** → **Email Activity**
   - You should see your test email sent

✅ **Done!** Emails are working with inline HTML templates.

---

## 🏭 For Production (After Templates Created)

### Step 1: Create Templates in Brevo (10 minutes)

1. Open https://app.brevo.com/login
2. Go to **Templates** → **Transactional**
3. Click **Create a template** (6 times)

For each template:
- Click **Design** editor
- Click **HTML** button
- Copy HTML from [04-ReadyToUseTemplates.md](./04-ReadyToUseTemplates.md)
- Set the **Subject** line
- Click **Save & Name**
- **Note the Template ID**

**Template IDs to record:**
```
Template 1 (Email Verification): ___
Template 2 (Expiry - 7 Days): ___
Template 3 (Expiry - 3 Days): ___
Template 4 (Expiry - 1 Day): ___
Template 5 (Subscription Expired): ___
Template 6 (Renewal Confirmation): ___
```

### Step 2: Update Environment Variables (2 minutes)

Add to your `.env` or deployment environment:

```
EmailTemplates__UseTemplates=true
EmailTemplates__VerificationEmailTemplateId=TEMPLATE_ID_1
EmailTemplates__SubscriptionExpiry7DaysTemplateId=TEMPLATE_ID_2
EmailTemplates__SubscriptionExpiry3DaysTemplateId=TEMPLATE_ID_3
EmailTemplates__SubscriptionExpiry1DayTemplateId=TEMPLATE_ID_4
EmailTemplates__SubscriptionExpiredTemplateId=TEMPLATE_ID_5
EmailTemplates__SubscriptionRenewalTemplateId=TEMPLATE_ID_6
```

Replace `TEMPLATE_ID_X` with actual IDs from Brevo.

### Step 3: Deploy (1 minute)

```bash
git add .
git commit -m "feat: enable Brevo email templates"
git push
```

✅ **Done!** Emails now use Brevo templates.

---

## 📋 Environment Variables Quick Reference

### Development (Default)
```
Brevo__ApiKey=YOUR_API_KEY
Brevo__SenderEmail=noreply@flow.kromic.in
Brevo__SenderName=Kromic Flow
EmailTemplates__UseTemplates=false
```

### Production (After Creating Templates)
```
Brevo__ApiKey=YOUR_API_KEY
Brevo__SenderEmail=noreply@flow.kromic.in
Brevo__SenderName=Kromic Flow
EmailTemplates__UseTemplates=true
EmailTemplates__VerificationEmailTemplateId=1
EmailTemplates__SubscriptionExpiry7DaysTemplateId=2
EmailTemplates__SubscriptionExpiry3DaysTemplateId=3
EmailTemplates__SubscriptionExpiry1DayTemplateId=4
EmailTemplates__SubscriptionExpiredTemplateId=5
EmailTemplates__SubscriptionRenewalTemplateId=6
```

---

## 📚 API Endpoints

### Send Verification Email
```bash
POST /api/v1/users/verify-email

Request:
{
  "email": "user@example.com"
}

Response:
{
  "success": true,
  "message": "Verification email sent to user@example.com"
}
```

### Verify Email Token
```bash
POST /api/v1/users/verify-email-token

Request:
{
  "token": "verification_token_here"
}

Response:
{
  "success": true,
  "message": "Email verified successfully!",
  "emailVerified": true
}
```

### Get User Profile (Check Verification Status)
```bash
GET /api/v1/users/profile

Response:
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "emailVerified": true,
  ...
}
```

---

## 🔐 Environment Variable Locations

### Local Development
- **File**: `.env` (in project root)
- **Format**: `KEY=VALUE`

### Docker / Linux
- **Command**: `export KEY=VALUE`
- **Format**: `EXPORT KEY=VALUE`

### Render / Heroku / Cloud Platforms
- **Location**: Dashboard → Environment Variables
- **Format**: `KEY = VALUE`

### Windows Server / IIS
- **Location**: System Environment Variables
- **Format**: `KEY = VALUE`

---

## ✅ Verification Checklist

- [ ] Brevo API key set in environment
- [ ] Sender email configured
- [ ] Development mode working with inline templates
- [ ] 6 templates created in Brevo (if going to production)
- [ ] Template IDs recorded
- [ ] `EmailTemplates__UseTemplates=true` (if using templates)
- [ ] All template IDs configured in environment
- [ ] Deployed successfully
- [ ] Test email sent and received
- [ ] Email contains correct content and parameters

---

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| Emails not sending | Set `Brevo__ApiKey` with correct value from https://app.brevo.com/settings/keys/api |
| "Template not found" | Verify template IDs are correct numbers from Brevo dashboard |
| Variables showing as `{{variable}}` | Set `EmailTemplates__UseTemplates=false` or verify variable names in template |
| Inline HTML still using | Set `EmailTemplates__UseTemplates=true` if you created templates |

---

## 📞 Quick Links

- **Brevo Dashboard**: https://app.brevo.com
- **Get API Key**: https://app.brevo.com/settings/keys/api
- **Detailed Setup**: [05-BrevoSetupGuide.md](./05-BrevoSetupGuide.md)
- **HTML Templates**: [04-ReadyToUseTemplates.md](./04-ReadyToUseTemplates.md)
- **Env Variables**: [06-EnvironmentVariables.md](./06-EnvironmentVariables.md)

---

## 🎯 What's Next

1. ✅ Backend code ready
2. ⏳ Create templates in Brevo (10 min)
3. ⏳ Update environment variables (2 min)
4. ⏳ Test email sending (2 min)
5. ⏳ Deploy to production (1 min)

**Total time: ~15 minutes**

