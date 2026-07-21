# Troubleshooting Guide - Email System

Solutions for common email configuration and sending issues.

---

## 🔴 Error: 401 Unauthorized

**Log Message:**
```
System.Net.Http.HttpRequestException: Response status code does not indicate success: 401 (Unauthorized)
Failed to send verification email
```

**Cause:** Brevo API key is missing, invalid, or incorrectly configured.

### Solutions

#### 1. Check API Key is Set

**On Linux/Docker:**
```bash
echo $Brevo__ApiKey
```

**On Windows:**
```powershell
$env:Brevo__ApiKey
```

**In .env file:**
```
Brevo__ApiKey=xkeysib-XXXXXXXXXXXXXXXXXXXX
```

#### 2. Verify API Key Format

API keys should start with `xkeysib-`:
```
❌ WRONG:   12345678
❌ WRONG:   YOUR_API_KEY
✅ CORRECT: xkeysib-1234567890abcdef1234567890abcdef
```

#### 3. Get Valid API Key

1. Log in to https://app.brevo.com
2. Go to **Settings** (bottom left)
3. Click **SMTP & API**
4. Click **API Keys**
5. Copy your API key (or create new one)
6. It should start with `xkeysib-`

#### 4. Update Environment Variable

**Development (.env file):**
```
Brevo__ApiKey=xkeysib-YOUR_ACTUAL_KEY_HERE
```

**Production (Render/Heroku/Docker):**
- Set in deployment dashboard environment variables
- Restart application

#### 5. Verify Configuration Loaded

Check logs on startup:
```
[INF] Brevo configuration loaded
[INF] Sender email: noreply@flow.kromic.in
```

If you don't see this, configuration is not loaded.

---

## 🔴 Error: "api-key header missing"

**Cause:** API key header not being sent to Brevo.

### Solution

The code automatically adds the header. If this error appears:

1. Verify `Brevo__ApiKey` is NOT empty
2. Restart application
3. Check logs for configuration load message

---

## 🔴 Error: "Invalid sender email"

**Cause:** Sender email not configured or invalid.

### Solution

#### 1. Set Sender Email

**Development (.env file):**
```
Brevo__SenderEmail=noreply@flow.kromic.in
```

**Production:**
- Set in deployment dashboard
- Format: `noreply@yourdomain.com` or real email

#### 2. Verify Email is Valid

Email should be:
- ✅ Valid email format: `name@domain.com`
- ✅ Real domain
- ✅ Configured in Brevo sender list

#### 3. Add Sender Email in Brevo

1. Log in to https://app.brevo.com
2. Go to **Senders** (left sidebar)
3. Click **Add a sender**
4. Enter email address
5. Verify the email (check inbox for confirmation link)

---

## 🔴 Error: "Template not found"

**Cause:** Brevo template ID is invalid or template doesn't exist.

### Solution

#### 1. Check Template IDs

In your environment:
```
EmailTemplates__VerificationEmailTemplateId=1
```

This should be a **number**, not a string.

#### 2. Verify Template Exists in Brevo

1. Log in to https://app.brevo.com
2. Go to **Templates** → **Transactional**
3. Look for template with ID matching your config
4. If not found, create template or update ID

#### 3. Update Configuration

If template IDs are wrong:
```
❌ WRONG: EmailTemplates__VerificationEmailTemplateId=abc
✅ CORRECT: EmailTemplates__VerificationEmailTemplateId=1
```

---

## 🔴 Error: "UseTemplates is true but no template ID set"

**Cause:** Configuration mismatch.

### Solution

If `UseTemplates=true`, all 6 template IDs must be set:

```
EmailTemplates__UseTemplates=true
EmailTemplates__VerificationEmailTemplateId=1          ← REQUIRED
EmailTemplates__SubscriptionExpiry7DaysTemplateId=2    ← REQUIRED
EmailTemplates__SubscriptionExpiry3DaysTemplateId=3    ← REQUIRED
EmailTemplates__SubscriptionExpiry1DayTemplateId=4     ← REQUIRED
EmailTemplates__SubscriptionExpiredTemplateId=5        ← REQUIRED
EmailTemplates__SubscriptionRenewalTemplateId=6        ← REQUIRED
```

Or disable templates:
```
EmailTemplates__UseTemplates=false
```

---

## 🟡 Warning: "Email sending returned null"

**Cause:** Not critical, but email may not have sent properly.

### Solution

1. Check Brevo dashboard for email activity
2. If email shows as sent there, it's OK
3. If not sent, check error logs for more details

---

## 🔴 Error: "Failed to send verification email"

**Cause:** Generic error, multiple possible causes.

### Debug Steps

1. **Check logs for exact error:**
   ```
   System.Net.Http.HttpRequestException: ...
   ```

2. **Check each component:**
   - [ ] Brevo API key set? `Brevo__ApiKey`
   - [ ] Sender email set? `Brevo__SenderEmail`
   - [ ] Email address valid? Format: `user@domain.com`
   - [ ] Brevo account active? https://app.brevo.com

3. **Test with curl:**
   ```bash
   curl -X POST https://api.brevo.com/v3/smtp/email \
     -H "api-key: xkeysib-YOUR_KEY" \
     -H "Content-Type: application/json" \
     -d '{
       "sender": {"email": "noreply@flow.kromic.in", "name": "KromicFlow"},
       "to": [{"email": "test@example.com"}],
       "subject": "Test",
       "htmlContent": "<p>Test</p>"
     }'
   ```

4. **If curl works but API doesn't:**
   - Restart application
   - Check environment variables reloaded

---

## 🔴 Email Not Received

**Cause:** Email sent but recipient didn't receive it.

### Solutions

1. **Check Brevo Email Activity:**
   - Go to https://app.brevo.com
   - Click **Emails** → **Email Activity**
   - Look for email in list
   - Check delivery status

2. **Check Spam Folder:**
   - Email may be in spam/junk
   - Add `noreply@flow.kromic.in` to contacts

3. **Check Email Address:**
   - Typo in email? Double-check
   - Is email format valid? `name@domain.com`

4. **Check Brevo Settings:**
   - Go to **Senders**
   - Verify sender email is confirmed
   - Check daily sending limit not exceeded

---

## 🔴 Error: "Unauthorized" on Test Email

**Cause:** API key invalid or expired.

### Solution

1. **Regenerate API Key:**
   - Log in to https://app.brevo.com
   - Go to **Settings** → **SMTP & API**
   - Delete old key
   - Create new key
   - Copy new key: `xkeysib-...`

2. **Update Environment:**
   - Set `Brevo__ApiKey` to new key
   - Restart application

3. **Wait 1 minute:**
   - Sometimes API takes time to recognize new keys

---

## 🟡 Slow Email Sending

**Cause:** Network latency or Brevo API slow.

### Normal Performance
- Email sending takes 200-700ms
- If consistently >2 seconds, something is wrong

### Solutions

1. **Check Network:**
   - Can you reach `https://api.brevo.com`?
   - Is internet connection stable?

2. **Check Brevo Status:**
   - Go to https://status.brevo.com
   - Check if any incidents reported

3. **Increase Timeout:**
   - In `DependencyInjection.cs`:
   ```csharp
   services.AddHttpClient<INotificationSender, BrevoNotificationSender>(client =>
   {
       client.Timeout = TimeSpan.FromSeconds(60); // Increase from 30s
   });
   ```

---

## ✅ Verification Checklist

Before going to production, verify:

- [ ] `Brevo__ApiKey` is set and starts with `xkeysib-`
- [ ] `Brevo__SenderEmail` is set and is a valid email
- [ ] Sender email is verified in Brevo console
- [ ] Test email sends successfully
- [ ] Email received in inbox
- [ ] Email content displays correctly
- [ ] Variables render (not `{{variable}}`)
- [ ] Links work (click verification link)

---

## 🧪 Quick Test

### Send Test Email via cURL

```bash
curl -X POST https://api.brevo.com/v3/smtp/email \
  -H "api-key: xkeysib-YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "sender": {
      "email": "noreply@flow.kromic.in",
      "name": "KromicFlow Test"
    },
    "to": [{
      "email": "your-email@example.com"
    }],
    "subject": "Test Email",
    "htmlContent": "<h1>Test</h1><p>If you see this, Brevo is working!</p>"
  }'
```

**Expected response (success):**
```json
{
  "messageId": "<xyz@brevo.com>"
}
```

**If you see error:**
```json
{
  "code": "invalid_parameter",
  "message": "Invalid api-key"
}
```

---

## 📞 Still Having Issues?

1. **Check logs** for exact error message
2. **Review this guide** for matching error
3. **Verify each component** with checklist above
4. **Test with curl** to isolate issue
5. **Check Brevo status** https://status.brevo.com
6. **Contact Brevo support** if API issues

---

## 📚 Related Documentation

- **[Configuration Guide](./01-ConfigurationGuide.md)** - Configuration reference
- **[Environment Variables](./06-EnvironmentVariables.md)** - How to set env vars
- **[Quick Start](./QUICKSTART.md)** - Getting started
- **[Brevo Setup](./05-BrevoSetupGuide.md)** - Create templates

