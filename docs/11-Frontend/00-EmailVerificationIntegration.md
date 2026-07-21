# Frontend Integration Guide: Email Verification

## Overview

Users must verify their email before enabling automations. This guide covers the complete integration flow.

## API Endpoints

### 1. Get User Profile (Check Verification Status)

**Endpoint**: `GET /api/v1/users/profile`

**Authentication**: Required (Bearer token)

**Response**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "fullName": "John Doe",
  "role": "User",
  "planCode": "starter",
  "isActive": true,
  "emailVerified": false,
  "marketingEmailEnabled": false,
  "marketingPushEnabled": false
}
```

**Key Field**: `emailVerified` (boolean)
- `true`: Email is verified, user can create automations
- `false`: Email not verified, user must verify before creating automations

---

### 2. Send Verification Email

**Endpoint**: `POST /api/v1/users/verify-email`

**Authentication**: Required (Bearer token)

**Request Body**:
```json
{
  "email": "user@example.com"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "Verification email sent to user@example.com. Check your inbox within 5 minutes."
}
```

**Response (Error - 400)**:
```json
{
  "error": "Email is already verified"
}
```

or

```json
{
  "error": "Too many verification requests. Please try again in 1 hour"
}
```

**Rate Limit**: 3 requests per hour per user

**Token Expiry**: 24 hours

---

### 3. Verify Email Token

**Endpoint**: `POST /api/v1/users/verify-email-token`

**Authentication**: Required (Bearer token)

**Request Body**:
```json
{
  "token": "verification-token-from-email-link"
}
```

**Response (Success - 200)**:
```json
{
  "success": true,
  "message": "Email verified successfully! You can now create automations",
  "emailVerified": true
}
```

**Response (Error - 400)**:
```json
{
  "error": "Verification token has expired. Please request a new verification email"
}
```

or

```json
{
  "error": "Invalid verification token"
}
```

---

## Integration Flow

### Step 1: Post-Login Check

After user logs in via Instagram OAuth, check their verification status:

```javascript
async function checkEmailVerification() {
  const response = await fetch('/api/v1/users/profile', {
    headers: {
      'Authorization': `Bearer ${accessToken}`
    }
  });
  
  const profile = await response.json();
  
  if (!profile.emailVerified) {
    showEmailVerificationFlow();
  }
}
```

### Step 2: Display Email Verification Modal

Show a modal asking user to enter email:

```html
<div id="emailVerificationModal">
  <h2>Verify Your Email</h2>
  <p>We need to verify your email to activate automations.</p>
  
  <input 
    type="email" 
    id="emailInput" 
    placeholder="Enter your email"
    required
  />
  
  <button onclick="sendVerificationEmail()">Send Verification Link</button>
  <p id="message"></p>
</div>
```

### Step 3: Send Verification Email

```javascript
async function sendVerificationEmail() {
  const email = document.getElementById('emailInput').value;
  const message = document.getElementById('message');
  
  if (!email) {
    message.textContent = 'Please enter an email address';
    message.style.color = 'red';
    return;
  }
  
  try {
    const response = await fetch('/api/v1/users/verify-email', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`
      },
      body: JSON.stringify({ email })
    });
    
    const data = await response.json();
    
    if (!response.ok) {
      message.textContent = data.error;
      message.style.color = 'red';
      return;
    }
    
    message.textContent = data.message;
    message.style.color = 'green';
    
    // Show instruction to check email
    document.getElementById('emailInput').disabled = true;
    showTokenVerificationStep();
    
  } catch (error) {
    console.error('Error:', error);
    message.textContent = 'Failed to send verification email';
    message.style.color = 'red';
  }
}
```

### Step 4: Handle Verification Link Click

When user clicks link in email, extract token and verify:

```javascript
// From URL: https://yourdomain.com/verify-email?token=abc123xyz
const urlParams = new URLSearchParams(window.location.search);
const token = urlParams.get('token');

if (token) {
  verifyEmailToken(token);
}

async function verifyEmailToken(token) {
  try {
    const response = await fetch('/api/v1/users/verify-email-token', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`
      },
      body: JSON.stringify({ token })
    });
    
    const data = await response.json();
    
    if (!response.ok) {
      showError(data.error);
      return;
    }
    
    // Success! Email verified
    showSuccessMessage("Email verified successfully!");
    
    // Redirect to automations page after 2 seconds
    setTimeout(() => {
      window.location.href = '/automations';
    }, 2000);
    
  } catch (error) {
    console.error('Error:', error);
    showError('Failed to verify email');
  }
}
```

### Step 5: Block Automation Creation If Not Verified

Before allowing users to create automations, check verification status:

```javascript
async function canCreateAutomation() {
  const response = await fetch('/api/v1/users/profile', {
    headers: {
      'Authorization': `Bearer ${accessToken}`
    }
  });
  
  const profile = await response.json();
  
  if (!profile.emailVerified) {
    showToast('error', 'Please verify your email before creating automations');
    return false;
  }
  
  return true;
}

// In automation creation form
document.getElementById('createAutomationBtn').onclick = async (e) => {
  e.preventDefault();
  
  if (!(await canCreateAutomation())) {
    return;
  }
  
  // Proceed with automation creation
  createAutomation();
};
```

---

## UI/UX Recommendations

### 1. Toast Notifications

Show user-friendly messages for key events:

- ✅ "Verification email sent! Check your inbox"
- ❌ "Email already verified"
- ❌ "Too many attempts. Try again in 1 hour"
- ✅ "Email verified successfully!"
- ❌ "Verification link expired. Request a new one"

### 2. Resend Option

Allow users to resend verification email:

```javascript
async function resendVerificationEmail() {
  // Call the same /api/v1/users/verify-email endpoint again
  // Rate limit is 3/hour, so show appropriate message if exceeded
  await sendVerificationEmail();
}
```

### 3. Email Entry Validation

- Validate email format before sending
- Show inline validation errors
- Disable button during request

### 4. Verification State Flow

```
Initial State
    ↓
User Not Verified → Show Modal
    ↓
User Enters Email → Send Request
    ↓
Rate Limit Check → Success or Error
    ↓
Email Sent → Show "Check Email" Message
    ↓
User Clicks Link → Extract Token
    ↓
Verify Token → Update emailVerified = true
    ↓
Success → Redirect to Automations
    ↓
User Can Create Automations
```

---

## Error Handling

### Scenarios and Responses

| Scenario | HTTP Status | Response | Action |
|----------|------------|----------|--------|
| First time verification | 200 | Confirmation message | Show "check email" message |
| Already verified | 400 | "Email is already verified" | Show success state, allow automations |
| Rate limited | 400 | "Too many requests" | Show countdown timer (1 hour) |
| Invalid token | 400 | "Invalid verification token" | Show "Request new email" link |
| Token expired | 400 | "Token has expired" | Show "Request new email" link |
| Invalid email format | 400 | "Email is required" | Show validation error |

---

## Code Example: Complete Component

```javascript
class EmailVerificationFlow {
  constructor(accessToken) {
    this.accessToken = accessToken;
    this.isVerified = false;
  }
  
  async checkVerificationStatus() {
    const response = await fetch('/api/v1/users/profile', {
      headers: { 'Authorization': `Bearer ${this.accessToken}` }
    });
    const profile = await response.json();
    this.isVerified = profile.emailVerified;
    return this.isVerified;
  }
  
  async sendVerificationEmail(email) {
    const response = await fetch('/api/v1/users/verify-email', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.accessToken}`
      },
      body: JSON.stringify({ email })
    });
    
    const data = await response.json();
    
    if (!response.ok) {
      throw new Error(data.error);
    }
    
    return data;
  }
  
  async verifyToken(token) {
    const response = await fetch('/api/v1/users/verify-email-token', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.accessToken}`
      },
      body: JSON.stringify({ token })
    });
    
    const data = await response.json();
    
    if (!response.ok) {
      throw new Error(data.error);
    }
    
    this.isVerified = true;
    return data;
  }
  
  async blockAutomationCreation() {
    if (!this.isVerified) {
      const verified = await this.checkVerificationStatus();
      if (!verified) {
        throw new Error('Email verification required before creating automations');
      }
    }
  }
}

// Usage
const emailFlow = new EmailVerificationFlow(accessToken);

// After login
if (!(await emailFlow.checkVerificationStatus())) {
  showEmailVerificationModal();
}

// When user clicks verify button
try {
  await emailFlow.sendVerificationEmail(userEmail);
  showToast('Verification email sent!');
} catch (error) {
  showToast('Error: ' + error.message);
}

// From email verification page/link
try {
  await emailFlow.verifyToken(tokenFromUrl);
  showToast('Email verified!');
  redirectToAutomations();
} catch (error) {
  showToast('Error: ' + error.message);
}

// Before creating automation
try {
  await emailFlow.blockAutomationCreation();
  createAutomation();
} catch (error) {
  showToast('Error: ' + error.message);
}
```

---

## Important Notes

1. **HTTPS Required**: Verification links should only work over HTTPS
2. **Token in URL**: Keep verification token in URL params, not in body (easier for users clicking from email)
3. **Token One-Time Use**: Once verified, token is immediately cleared from DB
4. **24-Hour Expiry**: Tokens expire after 24 hours
5. **Rate Limiting**: 3 emails per hour per user prevents abuse
6. **Store Email**: Email is persisted to User entity for future communication (subscription reminders, support)

