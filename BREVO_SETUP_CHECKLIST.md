# 🚀 Brevo Setup Checklist

Complete checklist to get email system working with Brevo.

---

## ✅ Step 1: Get Brevo API Key (5 minutes)

- [ ] Go to https://app.brevo.com/login
- [ ] Log in with your Brevo account
- [ ] Click **Settings** (bottom left)
- [ ] Click **SMTP & API**
- [ ] Click **API Keys**
- [ ] Copy your API key (should start with `xkeysib-`)
- [ ] **Save it somewhere safe** - you'll need it

**Your API Key:** `xkeysib-_______________________`

---

## ✅ Step 2: Configure Environment Variables

### For Development (.env file)

1. Open `.env` file in project root
2. Find the `Brevo` section
3. Update with your credentials:

```
Brevo__BaseUrl=https://api.brevo.com/v3
Brevo__ApiKey=xkeysib-YOUR_KEY_FROM_ABOVE
Brevo__SenderEmail=noreply@flow.kromic.in
Brevo__SenderName=Kromic Flow

# Email verification link (where users click from email)
Platform__EmailVerificationRedirectUrl=http://localhost:3000/verify-email
```

4. Save file
5. Restart application

### For Production (Render/Heroku/Docker)

1. Go to your deployment dashboard (e.g., Render, Heroku)
2. Find **Environment Variables** section
3. Add these variables:

```
Brevo__BaseUrl              = https://api.brevo.com/v3
Brevo__ApiKey               = xkeysib-YOUR_KEY_FROM_ABOVE
Brevo__SenderEmail          = noreply@flow.kromic.in
Brevo__SenderName           = Kromic Flow
Platform__EmailVerificationRedirectUrl = https://flow.kromic.in/verify-email
EmailTemplates__UseTemplates = false (for now)
```

4. Restart application

---

## ✅ Step 3: Verify Email in Brevo (5 minutes)

1. Go to https://app.brevo.com
2. Click **Senders** (left sidebar)
3. Check if `noreply@flow.kromic.in` is listed
4. If NOT listed:
   - Click **Add a sender**
   - Enter email: `noreply@flow.kromic.in`
   - Click **Submit**
   - Check your email for verification link
   - Click link to verify
5. Once verified, status should show "Verified"

✅ **Sender email verified**

---

## ✅ Step 4: Test Email Sending

### Test via API Endpoint

1. **Get access token** from your application
2. **Call email endpoint:**

```bash
curl -X POST https://flowapi.kromic.in/api/v1/users/verify-email \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "email": "kromic.build@gmail.com"
  }'
```

3. **Check response:**
   - ✅ Success: `{ "success": true, "message": "Verification email sent..." }`
   - ❌ Error: See troubleshooting section

4. **Check email inbox:**
   - You should receive verification email
   - Subject: "Verify your KromicFlow email"
   - Contains verification link

### Test via Brevo Dashboard

1. Go to https://app.brevo.com
2. Click **Emails** → **Email Activity**
3. Look for recent sent emails
4. Check status (should be "Sent" or "Delivered")

---

## ✅ Step 5: Create Email Templates (Optional - for Production)

Skip this for development. For production:

1. Follow: [docs/10-Email/05-BrevoSetupGuide.md](docs/10-Email/05-BrevoSetupGuide.md)
2. Create 6 templates
3. Record template IDs
4. Update environment variables:

```
EmailTemplates__UseTemplates=true
EmailTemplates__VerificationEmailTemplateId=1
EmailTemplates__SubscriptionExpiry7DaysTemplateId=2
... etc
```

---

## 🔍 Verification

| Checklist | Status |
|-----------|--------|
| Brevo account created | ✅ or ⏳ |
| API key obtained | ✅ or ⏳ |
| API key configured in environment | ✅ or ⏳ |
| Sender email configured | ✅ or ⏳ |
| Sender email verified in Brevo | ✅ or ⏳ |
| Test email sends successfully | ✅ or ⏳ |
| Email received in inbox | ✅ or ⏳ |

---

## 🐛 If Something Goes Wrong

### Email not sending

**Error:** `401 Unauthorized`

**Solution:**
1. Check API key is correct: `xkeysib-...`
2. Go to https://app.brevo.com/settings/keys/api
3. Verify key matches what you set
4. If different, update environment variable
5. Restart application

**Debug:**
```bash
curl -X POST https://api.brevo.com/v3/smtp/email \
  -H "api-key: xkeysib-YOUR_KEY" \
  -H "Content-Type: application/json" \
  -d '{"to":[{"email":"test@example.com"}],"sender":{"email":"noreply@flow.kromic.in"},"subject":"Test","htmlContent":"<p>Test</p>"}'
```

### Email sending but not received

1. Check **Brevo Email Activity** dashboard
2. If shows "Sent" but not received:
   - Check spam folder
   - Check email address is correct
3. If not in Brevo activity:
   - Email didn't actually send
   - Check configuration again

### "api-key header missing"

1. Verify `Brevo__ApiKey` is set
2. Restart application
3. Check logs for configuration load message

---

## 📋 Configuration Summary

### Minimal (Development)

```env
Brevo__BaseUrl=https://api.brevo.com/v3
Brevo__ApiKey=xkeysib-YOUR_API_KEY
Brevo__SenderEmail=noreply@flow.kromic.in
Brevo__SenderName=Kromic Flow
Platform__EmailVerificationRedirectUrl=http://localhost:3000/verify-email
EmailTemplates__UseTemplates=false
```

### Full (Production with Templates)

```env
Brevo__BaseUrl=https://api.brevo.com/v3
Brevo__ApiKey=xkeysib-YOUR_API_KEY
Brevo__SenderEmail=noreply@flow.kromic.in
Brevo__SenderName=Kromic Flow
Platform__EmailVerificationRedirectUrl=https://flow.kromic.in/verify-email
EmailTemplates__UseTemplates=true
EmailTemplates__VerificationEmailTemplateId=1
EmailTemplates__SubscriptionExpiry7DaysTemplateId=2
EmailTemplates__SubscriptionExpiry3DaysTemplateId=3
EmailTemplates__SubscriptionExpiry1DayTemplateId=4
EmailTemplates__SubscriptionExpiredTemplateId=5
EmailTemplates__SubscriptionRenewalTemplateId=6
```

---

## 🎯 Next Steps

1. ✅ Complete steps 1-4 above
2. ⏳ Test email system works
3. ⏳ (Optional) Create templates in Brevo for production
4. ⏳ Deploy to production
5. ⏳ Enable template switching if templates created

---

## 📞 Need Help?

- **Configuration**: [docs/10-Email/01-ConfigurationGuide.md](docs/10-Email/01-ConfigurationGuide.md)
- **Environment Vars**: [docs/10-Email/06-EnvironmentVariables.md](docs/10-Email/06-EnvironmentVariables.md)
- **Troubleshooting**: [docs/10-Email/07-Troubleshooting.md](docs/10-Email/07-Troubleshooting.md)
- **Quick Start**: [docs/10-Email/QUICKSTART.md](docs/10-Email/QUICKSTART.md)

---

## ✨ You're Set!

Once you complete all steps, email verification will work:
- ✅ Users can send verification emails
- ✅ Backend generates secure tokens
- ✅ Frontend receives emails with verification links
- ✅ Users can verify and unlock automations

🎉 **Ready to go!**

