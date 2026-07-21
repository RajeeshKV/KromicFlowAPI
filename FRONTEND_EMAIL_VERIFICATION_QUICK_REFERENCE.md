# Frontend Email Verification - Quick Reference

Simplified guide for frontend developers.

---

## 🎯 The User Flow

```
User in email → Clicks verification link
                ↓
             Browser opens: https://flow.kromic.in/verify-email?token=abc123xyz
                ↓
             Frontend page loads
                ↓
             Frontend extracts token from URL
                ↓
             Frontend calls backend API (with JWT in header)
                ↓
             Backend verifies token and marks email as verified
                ↓
             Frontend shows success page
                ↓
             User can now create automations
```

---

## ✅ What Frontend Needs to Do

### 1. Create Route
```jsx
<Route path="/verify-email" element={<VerifyEmailPage />} />
```

### 2. Extract Token from URL
```javascript
import { useSearchParams } from 'react-router-dom';

const [searchParams] = useSearchParams();
const token = searchParams.get('token'); // Gets 'abc123xyz' from ?token=abc123xyz
```

### 3. Call Backend to Verify
```javascript
const response = await fetch('/api/v1/users/verify-email-token', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${localStorage.getItem('access_token')}` // ← JWT HERE
  },
  body: JSON.stringify({ token })
});
```

### 4. Show Success/Error
```javascript
if (response.ok) {
  showSuccess("✅ Email Verified!");
  redirect('/dashboard');
} else {
  showError("❌ Verification Failed");
}
```

---

## 🔑 Key Point: JWT is Required

**Question:** Does frontend need JWT to open the /verify-email page?
**Answer:** NO - user can click email link and open page without login

**Question:** Does frontend need JWT to verify the token?
**Answer:** YES - backend needs to know which user is verifying

**So:**
```
Opening page: NO JWT needed
Calling API: YES JWT needed in Authorization header
```

---

## 📡 API Endpoint Reference

### Endpoint
```
POST /api/v1/users/verify-email-token
```

### Headers
```
Authorization: Bearer {JWT_TOKEN}
Content-Type: application/json
```

### Request Body
```json
{
  "token": "Ii4nTBUTFhcnfpCuaXgibgmnTBOZu307z_KZVhJ--zs"
}
```

### Success Response (200)
```json
{
  "success": true,
  "message": "Email verified successfully! You can now create automations",
  "emailVerified": true
}
```

### Error Response (400)
```json
{
  "error": "Invalid verification token"
}
```

OR

```json
{
  "error": "Verification token has expired. Please request a new verification email"
}
```

---

## 💡 Complete Code Example

```jsx
import { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';

export function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState('loading');
  const [message, setMessage] = useState('');

  const token = searchParams.get('token');

  useEffect(() => {
    if (!token) {
      setStatus('error');
      setMessage('No token in URL');
      return;
    }

    verifyEmail(token);
  }, [token]);

  async function verifyEmail(token) {
    try {
      const jwt = localStorage.getItem('access_token');
      if (!jwt) {
        setStatus('error');
        setMessage('Please log in first');
        return;
      }

      const response = await fetch('/api/v1/users/verify-email-token', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${jwt}`
        },
        body: JSON.stringify({ token })
      });

      const data = await response.json();

      if (!response.ok) {
        setStatus('error');
        setMessage(data.error);
        return;
      }

      setStatus('success');
      setMessage(data.message);

      setTimeout(() => navigate('/dashboard'), 2000);

    } catch (error) {
      setStatus('error');
      setMessage('Something went wrong');
    }
  }

  return (
    <div className="verify-container">
      {status === 'loading' && <div>⏳ Verifying...</div>}
      {status === 'success' && <div>✅ {message}</div>}
      {status === 'error' && <div>❌ {message}</div>}
    </div>
  );
}
```

---

## 🛑 Common Mistakes to Avoid

### ❌ Wrong: Not including JWT
```javascript
// WRONG - will get 401 Unauthorized
const response = await fetch('/api/v1/users/verify-email-token', {
  method: 'POST',
  body: JSON.stringify({ token })
});
```

### ✅ Right: Include JWT in header
```javascript
// RIGHT - backend knows which user
const response = await fetch('/api/v1/users/verify-email-token', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${getJWT()}`  // ← JWT HERE
  },
  body: JSON.stringify({ token })
});
```

### ❌ Wrong: Putting JWT in request body
```javascript
// WRONG - backend looks in header, not body
body: JSON.stringify({ token, jwt })
```

### ✅ Right: JWT in Authorization header
```javascript
// RIGHT - standard way
headers: {
  'Authorization': `Bearer ${jwt}`,
  'Content-Type': 'application/json'
}
```

---

## 🧪 How to Test

### Step 1: Send Verification Email
```bash
curl -X POST http://localhost:5000/api/v1/users/verify-email \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com"}'

# Response:
# {
#   "success": true,
#   "message": "Verification email sent..."
# }
```

### Step 2: Check Email for Link
Look for email from noreply@flow.kromic.in with link:
```
https://flow.kromic.in/verify-email?token=xyz123abc
```

### Step 3: Click Link
Opens /verify-email page with token in URL

### Step 4: Frontend Calls Backend
```javascript
// Frontend automatically calls:
// POST /api/v1/users/verify-email-token
// With token from URL
// With JWT from localStorage
```

### Step 5: See Success
Shows "✅ Email Verified!" and redirects

---

## 📍 Where to Put VerifyEmailPage Component

**Option 1: Separate Page**
```
src/
├── pages/
│   ├── Dashboard.jsx
│   ├── Login.jsx
│   └── VerifyEmailPage.jsx ← HERE
├── App.jsx
```

**Option 2: As Modal**
```
src/
├── components/
│   ├── EmailVerificationModal.jsx
│   └── VerifyEmailPage.jsx ← HERE
```

**Route Configuration:**
```jsx
<Route path="/verify-email" element={<VerifyEmailPage />} />
```

---

## 🔄 Related User Flows

### After Login - Show Email Modal

```jsx
useEffect(() => {
  fetchProfile();
}, []);

async function fetchProfile() {
  const response = await fetch('/api/v1/users/profile', {
    headers: { 'Authorization': `Bearer ${jwt}` }
  });
  const profile = await response.json();
  
  if (!profile.emailVerified) {
    showEmailVerificationModal();
  }
}
```

### Before Creating Automation - Check Email

```jsx
function handleCreateAutomation() {
  if (!profile.emailVerified) {
    showToast('Please verify email first');
    showEmailVerificationModal();
    return;
  }
  
  // Create automation
}
```

### Resend Verification Email

```jsx
async function resendEmail() {
  const response = await fetch('/api/v1/users/verify-email', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${jwt}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ email })
  });
}
```

---

## 📊 API Endpoints Summary

| Endpoint | Method | Purpose | JWT |
|----------|--------|---------|-----|
| `/api/v1/users/verify-email` | POST | Send verification email | YES |
| `/api/v1/users/verify-email-token` | POST | Verify token from email | YES |
| `/api/v1/users/profile` | GET | Check if emailVerified | YES |

---

## 🎯 Checklist for Implementation

- [ ] Create `/verify-email` route
- [ ] Extract token from URL using `useSearchParams`
- [ ] Get JWT from localStorage/sessionStorage/cookies
- [ ] Call backend with JWT in Authorization header
- [ ] Handle success response (show success page, redirect)
- [ ] Handle error response (show error message)
- [ ] Show verification modal after login if not verified
- [ ] Block automation creation until email verified
- [ ] Test with actual email link
- [ ] Test error scenarios (invalid token, expired token)

---

## 🚀 That's It!

You now have everything needed to implement email verification on the frontend.

**Next Step:** Read the detailed guide in `docs/11-Frontend/02-EmailVerificationFlow.md`

