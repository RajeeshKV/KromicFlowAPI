# Email Verification Flow - Frontend Implementation Guide

Complete step-by-step guide for implementing email verification on the frontend.

---

## 📋 Overview

User flow:
```
1. User receives email with link: https://flow.kromic.in/verify-email?token=abc123xyz
2. User clicks link → Browser navigates to /verify-email page
3. Frontend extracts token from URL
4. Frontend calls backend to verify token
5. Backend validates and marks email as verified
6. Frontend shows success page
7. User can now create automations
```

---

## 🔑 Key Points

### Does It Require JWT?

**YES - but here's how:**

1. **Token in URL** - This is the EMAIL verification token (from Brevo email link)
   - Format: `?token=Ii4nTBUTFhcnfpCuaXgibgmnTBOZu307z_KZVhJ--zs`
   - Purpose: Verify which email to activate
   - No JWT needed here

2. **API Call** - When calling backend, you DO need JWT
   - Header: `Authorization: Bearer {JWT_TOKEN}`
   - Purpose: Authenticate which user is verifying
   - Why: Backend needs to know which user account to update

### So the Flow is:

```
Email Link (no auth needed to click)
    ↓
Frontend extracts token from URL
    ↓
Frontend calls backend API WITH JWT header
    ↓
Backend validates token + updates user's email as verified
    ↓
Frontend shows success
```

### JWT Is Already Stored Where?

- **localStorage**: `const jwt = localStorage.getItem('access_token')`
- **sessionStorage**: `const jwt = sessionStorage.getItem('access_token')`
- **Cookies**: `const jwt = document.cookie.split('; ').find(row => row.startsWith('auth'))`
- **Zustand/Redux Store**: `const jwt = useAuthStore().token`

---

## 🛠️ Implementation Steps

### Step 1: Create /verify-email Page

**File: `src/pages/VerifyEmail.jsx` or `src/components/VerifyEmailPage.tsx`**

```jsx
import { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';

export function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState('loading'); // loading, success, error
  const [message, setMessage] = useState('');
  const token = searchParams.get('token');

  useEffect(() => {
    if (!token) {
      setStatus('error');
      setMessage('No verification token found in URL');
      return;
    }

    verifyEmail(token);
  }, [token]);

  async function verifyEmail(token) {
    try {
      setStatus('loading');
      setMessage('Verifying your email...');

      const response = await fetch('/api/v1/users/verify-email-token', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${getAuthToken()}` // ← JWT here
        },
        body: JSON.stringify({ token })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Verification failed');
      }

      const data = await response.json();
      setStatus('success');
      setMessage(data.message);

      // Redirect to dashboard after 2 seconds
      setTimeout(() => {
        navigate('/dashboard');
      }, 2000);

    } catch (error) {
      console.error('Verification error:', error);
      setStatus('error');
      setMessage(error.message);
    }
  }

  return (
    <div className="verify-email-container">
      {status === 'loading' && (
        <div className="loading">
          <div className="spinner"></div>
          <p>{message}</p>
        </div>
      )}

      {status === 'success' && (
        <div className="success">
          <div className="checkmark">✅</div>
          <h1>Email Verified!</h1>
          <p>{message}</p>
          <p className="redirect">Redirecting to dashboard...</p>
        </div>
      )}

      {status === 'error' && (
        <div className="error">
          <div className="errorIcon">❌</div>
          <h1>Verification Failed</h1>
          <p>{message}</p>
          <button onClick={() => navigate('/settings')}>
            Request New Link
          </button>
        </div>
      )}
    </div>
  );
}

// Helper function to get JWT token
function getAuthToken() {
  return localStorage.getItem('access_token') || 
         sessionStorage.getItem('access_token') ||
         // or from cookies if using cookie-based auth
         document.cookie
           .split('; ')
           .find(row => row.startsWith('auth='))
           ?.split('=')[1];
}
```

### Step 2: Add Route

**File: `src/App.jsx` or `src/router.tsx`**

```jsx
import { VerifyEmailPage } from './pages/VerifyEmail';

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public route - no authentication required to VISIT the page */}
        <Route path="/verify-email" element={<VerifyEmailPage />} />
        
        {/* Protected routes */}
        <Route path="/dashboard" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
        {/* ... other routes */}
      </Routes>
    </BrowserRouter>
  );
}
```

### Step 3: Show Email Verification Modal (After Login)

**File: `src/pages/Dashboard.jsx` or main app entry**

```jsx
import { useEffect, useState } from 'react';
import { EmailVerificationModal } from './components/EmailVerificationModal';

export function Dashboard() {
  const [showEmailModal, setShowEmailModal] = useState(false);
  const [profile, setProfile] = useState(null);

  useEffect(() => {
    // Fetch user profile after login
    fetchProfile();
  }, []);

  async function fetchProfile() {
    try {
      const response = await fetch('/api/v1/users/profile', {
        headers: {
          'Authorization': `Bearer ${getAuthToken()}`
        }
      });

      if (response.ok) {
        const data = await response.json();
        setProfile(data);

        // Show email verification modal if not verified
        if (!data.emailVerified) {
          setShowEmailModal(true);
        }
      }
    } catch (error) {
      console.error('Failed to fetch profile:', error);
    }
  }

  return (
    <div className="dashboard">
      {/* Show modal if email not verified */}
      {showEmailModal && (
        <EmailVerificationModal 
          onSuccess={() => {
            setShowEmailModal(false);
            fetchProfile(); // Refresh profile
          }}
        />
      )}

      {/* Rest of dashboard */}
      <h1>Welcome {profile?.fullName}</h1>
      {/* ... */}
    </div>
  );
}
```

### Step 4: Email Verification Modal Component

**File: `src/components/EmailVerificationModal.jsx`**

```jsx
import { useState } from 'react';

export function EmailVerificationModal({ onSuccess }) {
  const [email, setEmail] = useState('');
  const [status, setStatus] = useState('input'); // input, sending, sent, error
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  async function handleSubmit(e) {
    e.preventDefault();

    if (!email || !email.includes('@')) {
      setError('Please enter a valid email address');
      return;
    }

    try {
      setStatus('sending');
      setError('');

      const response = await fetch('/api/v1/users/verify-email', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${getAuthToken()}` // ← JWT here
        },
        body: JSON.stringify({ email })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.error || 'Failed to send verification email');
      }

      const data = await response.json();
      setStatus('sent');
      setMessage(data.message);

    } catch (err) {
      console.error('Error:', err);
      setStatus('error');
      setError(err.message);
    }
  }

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <h2>Verify Your Email</h2>
        <p className="subtitle">
          We need to verify your email before you can create automations.
        </p>

        {status === 'input' && (
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="email">Email Address</label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Enter your email"
                required
              />
            </div>

            {error && <div className="error-message">{error}</div>}

            <button type="submit" className="btn-primary">
              Send Verification Link
            </button>
          </form>
        )}

        {status === 'sending' && (
          <div className="sending">
            <div className="spinner"></div>
            <p>Sending verification email...</p>
          </div>
        )}

        {status === 'sent' && (
          <div className="success">
            <div className="checkmark">✅</div>
            <p>{message}</p>
            <p className="sub-message">
              Check your email for the verification link. It expires in 24 hours.
            </p>
            <button onClick={() => setStatus('input')} className="btn-secondary">
              Use Different Email
            </button>
          </div>
        )}

        {status === 'error' && (
          <div className="error">
            <p className="error-text">{error}</p>
            <button onClick={() => setStatus('input')} className="btn-primary">
              Try Again
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

function getAuthToken() {
  return localStorage.getItem('access_token');
}
```

### Step 5: Block Automation Creation Until Verified

**File: `src/components/CreateAutomationButton.jsx`**

```jsx
import { useState, useEffect } from 'react';

export function CreateAutomationButton() {
  const [profile, setProfile] = useState(null);
  const [showVerificationModal, setShowVerificationModal] = useState(false);

  useEffect(() => {
    fetchProfile();
  }, []);

  async function fetchProfile() {
    const response = await fetch('/api/v1/users/profile', {
      headers: { 'Authorization': `Bearer ${getAuthToken()}` }
    });
    const data = await response.json();
    setProfile(data);
  }

  function handleCreateAutomation() {
    if (!profile?.emailVerified) {
      // Show error message
      showToast('error', 'Please verify your email first');
      // Show verification modal
      setShowVerificationModal(true);
      return;
    }

    // Proceed with automation creation
    navigateTo('/create-automation');
  }

  return (
    <>
      <button 
        onClick={handleCreateAutomation}
        className="btn-create-automation"
        disabled={!profile}
      >
        + Create Automation
      </button>

      {showVerificationModal && (
        <EmailVerificationModal 
          onSuccess={() => {
            setShowVerificationModal(false);
            fetchProfile(); // Refresh to check emailVerified again
          }}
        />
      )}
    </>
  );
}
```

---

## 🔄 Complete Flow Diagram

```
User receives email with link
↓
https://flow.kromic.in/verify-email?token=xyz123abc
↓
User clicks link
↓
Browser navigates to /verify-email page
↓
Frontend extracts token from URL: const token = searchParams.get('token')
↓
Frontend calls backend:
  POST /api/v1/users/verify-email-token
  Header: Authorization: Bearer {JWT_TOKEN}
  Body: { "token": "xyz123abc" }
↓
Backend:
  1. Validates JWT (knows which user)
  2. Looks up user by JWT
  3. Finds token in database
  4. Checks token expiry (not expired = OK)
  5. Updates User.EmailVerified = true
  6. Clears token from database
  7. Returns success
↓
Frontend receives success response
↓
Shows "✅ Email Verified!" page
↓
Auto-redirects to dashboard after 2 seconds
↓
User can now create automations
```

---

## 🔐 Authentication Details

### JWT Token Storage

```javascript
// How to store JWT after login
localStorage.setItem('access_token', response.data.accessToken);

// How to retrieve for API calls
const token = localStorage.getItem('access_token');

// How to send in headers
headers: {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
}
```

### Why JWT is Needed

1. **Verify user identity** - Backend knows which user account to update
2. **Security** - Token can't be used without user's JWT
3. **Authorization** - Only authenticated users can verify emails
4. **Rate limiting** - Backend applies 3 emails/hour per user

### JWT Not in URL Because

- JWT is sensitive (contains user info)
- Should be stored securely (localStorage/cookies)
- Email verification token is ONE-TIME use, JWT is long-lived
- Email tokens are meant to be sent to email (one-time)
- JWTs are kept in browser storage

---

## 📱 Styling (CSS/Tailwind)

### Loading State

```css
.verify-email-container .loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  text-align: center;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #e5e7eb;
  border-top-color: #3b82f6;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
```

### Success State

```css
.verify-email-container .success {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  text-align: center;
}

.checkmark {
  font-size: 60px;
  margin-bottom: 20px;
}

.success h1 {
  color: #10b981;
  margin-bottom: 10px;
}
```

### Error State

```css
.verify-email-container .error {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  text-align: center;
}

.errorIcon {
  font-size: 60px;
  margin-bottom: 20px;
}

.error h1 {
  color: #ef4444;
  margin-bottom: 10px;
}
```

### Tailwind Version

```jsx
<div className="flex flex-col items-center justify-center min-h-screen">
  {status === 'loading' && (
    <>
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      <p className="mt-4 text-gray-600">{message}</p>
    </>
  )}

  {status === 'success' && (
    <>
      <div className="text-6xl mb-4">✅</div>
      <h1 className="text-2xl font-bold text-green-600">Email Verified!</h1>
      <p className="text-gray-600 mt-2">{message}</p>
    </>
  )}

  {status === 'error' && (
    <>
      <div className="text-6xl mb-4">❌</div>
      <h1 className="text-2xl font-bold text-red-600">Verification Failed</h1>
      <p className="text-gray-600 mt-2">{message}</p>
    </>
  )}
</div>
```

---

## 🧪 Testing Checklist

- [ ] User clicks email link
- [ ] Page loads at `/verify-email?token=xyz`
- [ ] Shows loading spinner
- [ ] Backend called with correct JWT header
- [ ] Success message displays after 1-2 seconds
- [ ] Auto-redirects to dashboard after 2 seconds
- [ ] Can click "Use Different Email" to try again
- [ ] Error message displays if token invalid
- [ ] Error message displays if token expired
- [ ] Calling with wrong token shows error
- [ ] Can't verify twice (second attempt fails gracefully)

---

## 🚀 Error Scenarios

### Scenario 1: Token Not in URL

```javascript
const token = searchParams.get('token');
if (!token) {
  setStatus('error');
  setMessage('No verification token found');
  return;
}
```

### Scenario 2: Token Invalid

**Backend returns 400:**
```json
{ "error": "Invalid verification token" }
```

**Frontend handles:**
```javascript
if (!response.ok) {
  const data = await response.json();
  setStatus('error');
  setMessage(data.error);
}
```

### Scenario 3: Token Expired

**Backend returns 400:**
```json
{ "error": "Verification token has expired. Please request a new verification email" }
```

**Frontend shows:**
```jsx
<p>{message}</p>
<button onClick={() => navigate('/settings')}>
  Request New Link
</button>
```

### Scenario 4: User Already Verified

**Backend returns 400:**
```json
{ "error": "Email is already verified" }
```

**Frontend handles:**
```jsx
if (data.error?.includes("already verified")) {
  setMessage('Your email is already verified. Redirecting...');
  setTimeout(() => navigate('/dashboard'), 1500);
}
```

### Scenario 5: No JWT Token

**Frontend checks:**
```javascript
const jwt = getAuthToken();
if (!jwt) {
  setStatus('error');
  setMessage('Please log in first');
  redirectToLogin();
}
```

---

## 📚 Summary Table

| Component | Purpose | JWT Needed | Where |
|-----------|---------|-----------|-------|
| Email Link | User receives this | NO | Email body |
| /verify-email Page | Shows loading/success | YES (for API) | Frontend route |
| Token in URL | Query parameter | NO | URL: `?token=xyz` |
| API Call | Verify token | YES | Authorization header |
| Backend Verification | Validate & update DB | YES | Middleware |
| Success Page | User sees this | N/A | Frontend component |

---

## 🎯 Key Takeaways

1. **Email link has no JWT** - It's one-time use token from Brevo
2. **API call needs JWT** - Backend needs to know which user
3. **Frontend extracts token from URL** - Use `URLSearchParams`
4. **Frontend calls backend with JWT** - In `Authorization` header
5. **Backend updates database** - Marks email as verified
6. **Frontend shows success** - Then redirects
7. **User can now create automations** - Email is verified

---

## 🔗 Related Documentation

- [Email Verification Backend](./00-EmailVerificationIntegration.md)
- [Environment Variables](../10-Email/06-EnvironmentVariables.md)
- [Quick Start](../10-Email/QUICKSTART.md)

