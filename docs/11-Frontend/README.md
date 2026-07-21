# Frontend Integration Documentation

Complete frontend integration guides for KromicFlow.

## Email Verification

### Quick Start for Frontend Developers

**Start here**: [00-EmailVerificationIntegration.md](./00-EmailVerificationIntegration.md)

This guide covers:
- ✅ What backend handles (token generation, email sending, validation, redirects)
- ✅ What frontend needs to do (display UI, handle redirects)
- ✅ API endpoints to call
- ✅ Step-by-step implementation
- ✅ Complete code examples
- ✅ Testing checklist

**Key Points**:
- Backend generates ALL URLs with tokens
- Backend handles ALL token validation
- Backend redirects users to frontend success page
- Frontend only displays UI and handles the redirect

### Advanced Patterns

**See**: [01-EmailVerificationFlow.md](./01-EmailVerificationFlow.md)

Advanced implementation patterns:
- React hooks for email verification
- State management approaches
- Rate limiting client-side
- Protected routes
- Error boundaries
- Jest testing examples

---

## What Backend Handles

✅ Token generation (cryptographically secure)
✅ Email sending via Brevo
✅ URL generation with embedded tokens
✅ Token validation when user clicks email link
✅ Redirects to frontend success page
✅ Rate limiting (3 emails/hour)
✅ Database updates

---

## What Frontend Handles

✅ Check `emailVerified` field from `/api/v1/users/profile`
✅ Display email input modal/form
✅ Call `/api/v1/users/verify-email` to request verification
✅ Show "check your email" message
✅ Display success page when redirected
✅ Block automation creation until verified

---

## Email Verification Flow (Simplified)

```
User Signs Up (OAuth)
    ↓
Frontend: GET /api/v1/users/profile
    ↓
If emailVerified=false → Show modal
    ↓
User enters email → POST /api/v1/users/verify-email
    ↓
Backend sends email with verification link
    ↓
User clicks link in email
    ↓
Backend validates token & redirects to success page
    ↓
Frontend shows success page
    ↓
User can now create automations
```

---

## Documentation Structure

### For Frontend Teams

1. **Main Guide** - [00-EmailVerificationIntegration.md](./00-EmailVerificationIntegration.md)
   - API endpoints
   - Frontend implementation steps
   - Complete integration example
   - Error handling
   - Testing checklist

2. **Advanced Patterns** - [01-EmailVerificationFlow.md](./01-EmailVerificationFlow.md)
   - React hooks
   - Component patterns
   - Rate limiting
   - Protected routes
   - Jest testing

### For Backend/DevOps Teams

- **Email Configuration** - See `docs/10-Email/`
- **Template Specifications** - See `docs/10-Email/02-TemplateSpecifications.md`

---

## Key Files

| File | Purpose | Audience |
|------|---------|----------|
| `00-EmailVerificationIntegration.md` | Main FE guide | Frontend developers |
| `01-EmailVerificationFlow.md` | Advanced patterns | Frontend developers |
| `../10-Email/README.md` | Email system overview | Backend developers |
| `../10-Email/02-TemplateSpecifications.md` | Email template specs | UI/Design team |
| `../10-Email/01-ConfigurationGuide.md` | Backend config | Backend/DevOps |

---

## Pages Needed in Frontend

1. **Email Verification Modal**
   - Email input field
   - Send button
   - Status messages
   - Resend option (after cooldown)

2. **Email Verification Success Page** (`/email-verification-success`)
   - Success message with checkmark
   - "You can now create automations" message
   - Auto-redirect button (optional)

3. **Block Automations**
   - When accessing `/automations` without verified email
   - Show verification modal

---

## API Endpoints (Frontend Calls These)

### 1. Check Email Status
```
GET /api/v1/users/profile
Response: { emailVerified: boolean, ... }
```

### 2. Send Verification Email
```
POST /api/v1/users/verify-email
Body: { email: "user@example.com" }
Response: { success: true, message: "..." }
```

**That's it!** Backend handles the rest.

---

## Email Link Handling

**Important**: When user clicks the email verification link:

1. User receives email with link: `https://yourdomain.com/verify-email?token=abc123`
2. User clicks link
3. **Backend intercepts the request** (not frontend)
4. Backend validates token
5. Backend redirects to: `https://yourdomain.com/email-verification-success`
6. **Frontend receives the redirect automatically**
7. Frontend displays success page

**Frontend does NOT need to**:
- Parse the token from URL
- Make any API calls
- Handle token validation

---

## Testing

### Manual Testing
- [ ] Sign up with OAuth
- [ ] See email verification modal
- [ ] Enter email and send
- [ ] Check test email inbox
- [ ] Click verification link
- [ ] See success page and redirect
- [ ] Verify can now create automations

### Automated Testing
- See [01-EmailVerificationFlow.md](./01-EmailVerificationFlow.md#testing-examples)

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Modal doesn't show after login | Check profile endpoint returns `emailVerified` field |
| Email never arrives | Check Brevo API key in backend config |
| Link doesn't work | Verify backend redirect URL matches your domain |
| Can create automation without verification | Check if validation is in place before creation |

---

## Questions?

- Backend configuration: See [10-Email Documentation](../10-Email/README.md)
- Email templates: See [Template Specifications](../10-Email/02-TemplateSpecifications.md)
- Advanced patterns: See [Advanced Frontend Guide](./01-EmailVerificationFlow.md)

