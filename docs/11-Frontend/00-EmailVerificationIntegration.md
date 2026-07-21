# Email Verification - Frontend Integration Guide

**Complete Frontend Integration Guide for Email Verification Workflow**

> **⚠️ Important**: Backend handles ALL token generation, email sending, URL generation, and token validation. Frontend only displays UI and handles redirects.

---

## Overview

Users must verify their email before enabling automations.

**What Backend Does**:
- ✅ Generates secure tokens
- ✅ Sends emails via Brevo
- ✅ Creates verification links with embedded tokens
- ✅ Validates tokens
- ✅ Redirects users to frontend success page
- ✅ Enforces rate limiting (3 emails/hour)

**What Frontend Does**:
- ✅ Check `emailVerified` field from profile
- ✅ Show email input modal/form
- ✅ Call API to send verification email
- ✅ Display success page when backend redirects
- ✅ Block automations until verified

---

## Email Verification Link Flow

**This is completely handled by the backend:**

```
User receives email with link:
  https://yourdomain.com/verify-email?token=abc123xyz

User clicks link
  ↓
Backend intercepts request:
  - Extracts token from URL
  - Validates token in database
  - Updates EmailVerified = true
  - Redirects to: https://yourdomain.com/email-verification-success

Frontend receives redirect automatically
  ↓
Frontend displays success page
  ↓
User can now create automations
```

**Frontend does NOT handle**:
- Token parsing from URL ✗
- Token validation ✗
- Database updates ✗
- Email sending ✗

---

## API Endpoints

### 1. Get User Profile (Check Verification Status)

**Endpoint**: `GET /api/v1/users/profile`

**When to call**: After user logs in

**Response**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "fullName": "John Doe",
  "emailVerified": false,  // ← Check this!
  ...
}
```

**What to do**:
- If `emailVerified: true` → User can create automations
- If `emailVerified: false` → Show email verification modal

---

### 2. Send Verification Email

**Endpoint**: `POST /api/v1/users/verify-email`

**When to call**: When user submits email in modal

**Request**:
```json
{
  "email": "user@example.com"
}
```

**Success Response (200)**:
```json
{
  "success": true,
  "message": "Verification email sent to user@example.com. Check your inbox within 5 minutes."
}
```

**Error Responses**:
- `"Email is already verified"` - User already verified
- `"Too many verification requests. Please try again in 1 hour"` - Rate limited (3/hour)

---

## Frontend Implementation

### Step 1: Check Email Status After Login

```javascript
async function checkEmailVerification() {
  try {
    const response = await fetch('/api/v1/users/profile', {
      headers: {
        'Authorization': `Bearer ${getAccessToken()}`
      }
    });
    
    const profile = await response.json();
    
    // Check if email is verified
    if (!profile.emailVerified) {
      showEmailVerificationModal();
      return false;
    }
    
    return true;
    
  } catch (error) {
    console.error('Error:', error);
    return false;
  }
}

// After successful OAuth login
const isVerified = await checkEmailVerification();
if (!isVerified) {
  // Block access to automations, show modal
}
```

### Step 2: Create Email Input Modal

```html
<div id="emailVerificationModal" class="modal" style="display: none;">
  <div class="modal-content">
    <h2>Verify Your Email</h2>
    <p>We need to verify your email to activate automations.</p>
    
    <input 
      type="email" 
      id="emailInput" 
      placeholder="Enter your email"
      required
    />
    
    <button id="sendBtn" onclick="sendVerificationEmail()">
      Send Verification Link
    </button>
    
    <p id="message"></p>
  </div>
</div>
```

### Step 3: Send Verification Email

```javascript
async function sendVerificationEmail() {
  const email = document.getElementById('emailInput').value;
  const sendBtn = document.getElementById('sendBtn');
  
  // Validate email
  if (!isValidEmail(email)) {
    showMessage('Please enter a valid email address', 'error');
    return;
  }
  
  // Disable button
  sendBtn.disabled = true;
  
  try {
    const response = await fetch('/api/v1/users/verify-email', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${getAccessToken()}`
      },
      body: JSON.stringify({ email })
    });
    
    const data = await response.json();
    
    if (!response.ok) {
      showMessage(data.error, 'error');
      sendBtn.disabled = false;
      return;
    }
    
    // Success - show "check your email" message
    showMessage(data.message, 'success');
    document.getElementById('emailInput').disabled = true;
    
  } catch (error) {
    showMessage('Failed to send verification email', 'error');
    sendBtn.disabled = false;
  }
}

function showMessage(text, type) {
  const msg = document.getElementById('message');
  msg.textContent = text;
  msg.className = type; // 'success' or 'error'
}

function isValidEmail(email) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function getAccessToken() {
  return localStorage.getItem('accessToken');
}
```

### Step 4: Create Success Page

When user clicks the email link, backend redirects to this page:

**Path**: `/email-verification-success` or similar

```html
<div class="success-page">
  <div class="success-card">
    <h1>✅ Email Verified!</h1>
    <p>Your email has been verified successfully.</p>
    <p>You can now create and manage automations.</p>
    
    <button onclick="goToDashboard()">
      Go to Dashboard
    </button>
  </div>
</div>

<script>
  function goToDashboard() {
    window.location.href = '/dashboard';
  }
  
  // Auto-redirect after 2 seconds (optional)
  setTimeout(() => {
    goToDashboard();
  }, 2000);
</script>
```

### Step 5: Block Automations Until Verified

```javascript
async function createAutomation() {
  // Check if email is verified
  try {
    const response = await fetch('/api/v1/users/profile', {
      headers: {
        'Authorization': `Bearer ${getAccessToken()}`
      }
    });
    
    const profile = await response.json();
    
    if (!profile.emailVerified) {
      showToast('error', 'Please verify your email before creating automations');
      showEmailVerificationModal();
      return;
    }
    
    // Proceed with automation creation
    proceedWithCreation();
    
  } catch (error) {
    showToast('error', 'Error checking email verification');
  }
}

// Add event listener to create automation button
document.getElementById('createAutomationBtn').addEventListener('click', createAutomation);
```

---

## Complete Integration Example

```javascript
class EmailVerificationManager {
  constructor() {
    this.isVerified = false;
  }
  
  // Step 1: After login, check email verification status
  async checkVerification() {
    try {
      const response = await fetch('/api/v1/users/profile', {
        headers: { 'Authorization': `Bearer ${this.getToken()}` }
      });
      
      const profile = await response.json();
      this.isVerified = profile.emailVerified;
      
      if (!this.isVerified) {
        this.showVerificationModal();
      }
      return this.isVerified;
    } catch (error) {
      console.error('Error:', error);
      return false;
    }
  }
  
  // Step 2: User submits email
  async sendEmail(email) {
    if (!this.isValidEmail(email)) {
      throw new Error('Invalid email address');
    }
    
    const response = await fetch('/api/v1/users/verify-email', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getToken()}`
      },
      body: JSON.stringify({ email })
    });
    
    const data = await response.json();
    if (!response.ok) throw new Error(data.error);
    return data;
  }
  
  // Step 3: Handle success (called when backend redirects)
  handleVerificationSuccess() {
    this.isVerified = true;
    this.closeModal();
    window.location.href = '/dashboard';
  }
  
  // Step 4: Block automations if not verified
  canCreateAutomation() {
    if (!this.isVerified) {
      this.showVerificationModal();
      return false;
    }
    return true;
  }
  
  showVerificationModal() {
    document.getElementById('emailVerificationModal').style.display = 'block';
  }
  
  closeModal() {
    document.getElementById('emailVerificationModal').style.display = 'none';
  }
  
  isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }
  
  getToken() {
    return localStorage.getItem('accessToken');
  }
}

// Usage
const emailManager = new EmailVerificationManager();

// After OAuth login
await emailManager.checkVerification();

// When user clicks send button
document.getElementById('sendBtn').addEventListener('click', async () => {
  try {
    const email = document.getElementById('emailInput').value;
    const result = await emailManager.sendEmail(email);
    showToast('success', result.message);
  } catch (error) {
    showToast('error', error.message);
  }
});

// Before creating automation
if (emailManager.canCreateAutomation()) {
  createAutomation();
}
```

---

## UI/UX Recommendations

### Email Verification Modal

```
┌──────────────────────────────────┐
│  Verify Your Email               │
├──────────────────────────────────┤
│                                  │
│  We need to verify your email    │
│  to activate automations.        │
│                                  │
│  [user@example.com     ]         │
│  [Send Verification Link]        │
│                                  │
│  ✓ Verification email sent!      │
│  Check your inbox within 5 min   │
│                                  │
│  [Resend] (after 60 seconds)     │
│                                  │
└──────────────────────────────────┘
```

### Success Page (After Clicking Email Link)

```
┌──────────────────────────────────┐
│                                  │
│           ✅ Email Verified!     │
│                                  │
│  Your email has been verified    │
│  successfully.                   │
│                                  │
│  You can now create automations  │
│                                  │
│  [Go to Dashboard]               │
│                                  │
│  Redirecting in 2 seconds...     │
│                                  │
└──────────────────────────────────┘
```

### Toast Notifications

- ✅ "Verification email sent!"
- ❌ "Email is already verified"
- ❌ "Too many requests. Try again in 1 hour"
- ✅ "Email verified successfully!"
- ❌ "Please verify your email before creating automations"

---

## Frontend Pages Needed

1. **Email Verification Modal** (Overlay on current page)
   - Email input field
   - Send button
   - Status messages
   - Resend option

2. **Email Verification Success Page** (`/email-verification-success`)
   - Success message
   - Checkmark icon
   - Auto-redirect button

3. **Block Automations Page** (When accessing automations without verification)
   - Message: "Please verify your email"
   - Show verification modal

---

## Testing Checklist

- [ ] After login with `emailVerified: false`, modal appears
- [ ] Enter email and click send
- [ ] See success message: "Verification email sent"
- [ ] Receive email in inbox (test with Brevo dashboard)
- [ ] Click link in email
- [ ] Backend redirects to success page
- [ ] Success page shows checkmark
- [ ] Page auto-redirects to dashboard after 2 seconds
- [ ] Check profile endpoint shows `emailVerified: true`
- [ ] Try creating automation - should work now
- [ ] Try sending email again - should work (3/hour limit)
- [ ] Send 4th email within 1 hour - should see rate limit error

---

## Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Modal doesn't show after login | Check `emailVerified` field is in profile response |
| Email never arrives | Check Brevo API key in backend config |
| Link in email doesn't work | Backend redirect URL must match your domain |
| Success page doesn't show | Check redirect URL in backend config |
| Can create automations without verification | Check if `emailVerified` check is in place |

---

## Key Points

✅ **Backend handles all**: Tokens, emails, validation, redirects
✅ **Frontend displays**: Modal, success page, messages
✅ **Email link**: Automatically redirects to frontend success page
✅ **Rate limit**: 3 emails per hour (backend enforces)
✅ **Token expiry**: 24 hours (backend enforces)

❌ **Frontend should NOT**: Parse tokens, validate tokens, send emails, call token validation endpoint

