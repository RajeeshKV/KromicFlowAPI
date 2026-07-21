# Frontend Integration Checklist

Complete checklist for frontend email verification integration.

---

## 📋 Pre-Integration

- [ ] Read: [00-EmailVerificationIntegration.md](./00-EmailVerificationIntegration.md)
- [ ] Understand backend handles ALL token/URL processing
- [ ] Verify backend team has deployed email verification endpoints
- [ ] Get API base URL from backend team (e.g., `https://api.example.com/api/v1`)

---

## 🎨 UI Components Needed

### Email Verification Modal
- [ ] Email input field with validation
- [ ] Send button
- [ ] Loading state while sending
- [ ] Success message after sending
- [ ] Error message display
- [ ] Resend option (disabled for 60 seconds after sending)
- [ ] Close button (optional, or force verification)

### Success Page (`/email-verification-success`)
- [ ] Checkmark or success icon
- [ ] Success message: "Email verified successfully!"
- [ ] Explanation: "You can now create automations"
- [ ] Continue button (manual redirect)
- [ ] Auto-redirect after 2-3 seconds (optional)

### Automation Creation Page
- [ ] Check before allowing automation creation
- [ ] Show modal if email not verified
- [ ] Block submit if email not verified

---

## 💻 Implementation Checklist

### Step 1: Check Verification After Login
- [ ] Call `GET /api/v1/users/profile` after successful OAuth
- [ ] Check `emailVerified` field in response
- [ ] If `false`, show email verification modal
- [ ] If `true`, allow automations

### Step 2: Email Verification Modal
- [ ] Create modal component with email input
- [ ] Add email validation (basic regex or library)
- [ ] Implement send button handler
- [ ] Call `POST /api/v1/users/verify-email`
- [ ] Send Authorization header with bearer token
- [ ] Handle success response (show "check email" message)
- [ ] Handle error responses (rate limit, already verified, etc.)
- [ ] Disable send button during request
- [ ] Show loading indicator

### Step 3: Success Page
- [ ] Create `/email-verification-success` page/route
- [ ] Display success message
- [ ] Add continue button → redirect to `/dashboard` or `/automations`
- [ ] Optional: Auto-redirect after 2-3 seconds

### Step 4: Protect Automation Creation
- [ ] Check `emailVerified` before allowing automation form submission
- [ ] Show error toast: "Please verify email first"
- [ ] Show verification modal on error
- [ ] Prevent form submission if not verified

### Step 5: Error Handling
- [ ] Handle network errors
- [ ] Handle rate limit error (show countdown)
- [ ] Handle "already verified" error (close modal, allow automations)
- [ ] Handle invalid email format
- [ ] Handle token/session expiry

---

## 🧪 Testing Checklist

### Manual Testing
- [ ] Start fresh session (logout/login)
- [ ] See email verification modal on login
- [ ] Enter valid email address
- [ ] Click send verification button
- [ ] See success message: "Email sent"
- [ ] Check test email inbox (use Brevo test email or Mailtrap)
- [ ] Verify email received with correct sender
- [ ] Verify email contains clickable verification link
- [ ] Click link in email
- [ ] Get redirected to success page
- [ ] See success page with checkmark
- [ ] Auto-redirect to dashboard (if implemented)
- [ ] Manual redirect button works
- [ ] Check profile endpoint shows `emailVerified: true`
- [ ] Try creating automation - should be allowed now
- [ ] Send 2nd verification email - should work
- [ ] Send 3rd verification email - should work
- [ ] Try 4th email within 1 hour - should see rate limit error
- [ ] Try resending with invalid email - should show error
- [ ] Try resending after token expired (24+ hours) - should see error

### Edge Cases
- [ ] Already verified user tries to access verification page
- [ ] User goes directly to verification link without email modal
- [ ] User clicks verification link after 24 hours (expired token)
- [ ] User clicks invalid/tampered link
- [ ] Network error during email send - error displays
- [ ] Network error during verification link click - error displays
- [ ] Session expires during email verification - handle gracefully

---

## 🔐 Security Checklist

- [ ] Always send Authorization header with bearer token
- [ ] Don't store tokens in URL/cookies unnecessarily
- [ ] Validate email format client-side (for UX, not security)
- [ ] Don't expose backend errors to users (show friendly messages)
- [ ] Rate limit client-side UI (60 second cooldown after send)
- [ ] Handle token expiry gracefully (ask user to request new email)

---

## 📱 Responsive Design
- [ ] Modal displays correctly on mobile
- [ ] Modal displays correctly on tablet
- [ ] Modal displays correctly on desktop
- [ ] Success page is readable on all screen sizes
- [ ] Buttons are easy to tap on mobile (minimum 44x44px)
- [ ] Input field is touch-friendly on mobile

---

## ♿ Accessibility Checklist
- [ ] Modal has proper ARIA labels
- [ ] Email input has associated label
- [ ] Button text is descriptive ("Send Verification Link" not "Submit")
- [ ] Error messages are announced to screen readers
- [ ] Success messages are announced to screen readers
- [ ] Tab navigation works correctly through modal
- [ ] Keyboard can submit form (Enter key works)
- [ ] Focus management is handled (focus moves to modal on open)

---

## 🎯 Browser Compatibility
- [ ] Works in Chrome
- [ ] Works in Firefox
- [ ] Works in Safari
- [ ] Works in Edge
- [ ] Works on iOS Safari
- [ ] Works on Android Chrome

---

## 📊 Monitoring Checklist

### Track These Metrics
- [ ] Verification email send success rate
- [ ] Verification email click-through rate
- [ ] User completion rate (from modal to verified)
- [ ] Error rates by type (network, rate limit, validation)
- [ ] Time to verification (from send to click)
- [ ] Conversion rate (verified → created automation)

### Setup Analytics Events
- [ ] Track: "Email Verification Modal Shown"
- [ ] Track: "Email Verification Requested"
- [ ] Track: "Email Verification Link Clicked"
- [ ] Track: "Email Verification Successful"
- [ ] Track: "Email Verification Failed" (with error code)
- [ ] Track: "Automation Blocked - Email Not Verified"

---

## 📚 Documentation Checklist

- [ ] Inline code comments explaining email verification logic
- [ ] API endpoint URL documented (where to call)
- [ ] Error handling documented (what each error means)
- [ ] Success flow documented (what happens after verification)
- [ ] Testing instructions documented (how to test locally)
- [ ] Known limitations documented (if any)

---

## 🚀 Deployment Checklist

- [ ] Code review completed
- [ ] All tests passing
- [ ] No console errors
- [ ] No console warnings (except pre-existing)
- [ ] Performance acceptable (email send < 2 seconds)
- [ ] No hardcoded URLs (use config/environment variables)
- [ ] Error tracking configured (Sentry, etc.)
- [ ] Backend team verified API is live
- [ ] Staging environment tested
- [ ] Production deployment tested

---

## 📞 Before Going Live

- [ ] Backend team confirmed email sending works
- [ ] Email templates configured in Brevo
- [ ] Test email received successfully
- [ ] Links in test email work
- [ ] Redirects go to correct frontend URLs
- [ ] Rate limiting works as expected
- [ ] Error messages are user-friendly
- [ ] Documentation is complete

---

## ✅ Go-Live Checklist

- [ ] All above items checked
- [ ] Frontend code deployed
- [ ] Backend API live and accessible
- [ ] Email sending configured and tested
- [ ] Monitoring and analytics configured
- [ ] Error tracking configured
- [ ] Support team trained on email verification flow
- [ ] Users can successfully complete verification flow
- [ ] Success metrics being tracked

---

## 📝 Post-Launch Monitoring

- [ ] Monitor error rates daily for first week
- [ ] Monitor verification success rate
- [ ] Monitor user feedback about email delivery
- [ ] Check email spam folder rates
- [ ] Monitor API response times
- [ ] Check for any rate limiting issues
- [ ] Monitor analytics events

---

## 🐛 Troubleshooting Guide

### "Email verification modal doesn't show"
- [ ] Check profile endpoint returns `emailVerified` field
- [ ] Verify Authorization header is being sent
- [ ] Check browser console for errors
- [ ] Verify backend API is responding

### "Email sending fails"
- [ ] Check Brevo API key in backend config
- [ ] Verify email format is valid
- [ ] Check rate limit (3/hour)
- [ ] Check network connectivity

### "Verification link doesn't work"
- [ ] Verify backend redirect URL is correct
- [ ] Check token hasn't expired (24 hours)
- [ ] Clear browser cache and cookies
- [ ] Try incognito/private mode

### "Can create automations without verification"
- [ ] Check verification check is in place
- [ ] Check `emailVerified` field is being read from profile
- [ ] Check profile is being fetched fresh (not cached)

---

## 📖 Reference Links

- Main guide: [00-EmailVerificationIntegration.md](./00-EmailVerificationIntegration.md)
- Advanced patterns: [01-EmailVerificationFlow.md](./01-EmailVerificationFlow.md)
- Email configuration: [../10-Email/](../10-Email/)
- API documentation: (Ask backend team)

