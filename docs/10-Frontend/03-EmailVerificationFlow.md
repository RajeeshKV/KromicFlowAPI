# Email Verification & Subscription Expiry Notifications

> **Complete Frontend Integration Guide**  
> Base URL: `https://api.example.com/api/v1`

---

## Overview

Users must verify their email before enabling automations. The system sends:
- ✅ Welcome email with verification link (on signup)
- ✅ Subscription expiry reminders (7, 3, 1 days before + on expiry)
- ✅ Resend verification option (if needed)

---

## Table of Contents

1. [Email Verification Flow](#1-email-verification-flow)
2. [API Endpoints](#2-api-endpoints)
3. [React Components](#3-react-components)
4. [Vue Components](#4-vue-components)
5. [Error Handling](#5-error-handling)
6. [Data Models](#6-data-models)
7. [State Management](#7-state-management)
8. [UX Best Practices](#8-ux-best-practices)
9. [Testing](#9-testing)

---

## 1. Email Verification Flow

### User Journey

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User signs up via Instagram OAuth                            │
│    ↓                                                             │
│ 2. Backend creates user with EmailVerified=false                │
│    ↓                                                             │
│ 3. Frontend shows "Verify Email" prompt                         │
│    ↓                                                             │
│ 4. User clicks "Send Verification Email"                        │
│    ↓                                                             │
│ 5. Backend sends email with verification link                   │
│    ↓                                                             │
│ 6. User clicks link in email                                    │
│    ↓                                                             │
│ 7. Frontend extracts token from URL, submits to backend         │
│    ↓                                                             │
│ 8. Backend verifies token, sets EmailVerified=true              │
│    ↓                                                             │
│ 9. Frontend shows success, enables automations feature          │
│    ↓                                                             │
│ 10. User can now create automations                             │
└─────────────────────────────────────────────────────────────────┘
```

### Timeline

| Step | Duration | User Action | Backend Action |
|------|----------|-------------|-----------------|
| 1 | Immediate | OAuth callback | Create user, EmailVerified=false |
| 2 | User initiation | "Verify Email" button | Send verification email |
| 3 | ~5 min | Check email, click link | - |
| 4 | Immediate | Verification page loads | - |
| 5 | Immediate | Confirm verification | Verify token, set EmailVerified=true |
| 6 | Immediate | Success shown | User can create automations |

---

## 2. API Endpoints

### POST /api/v1/users/verify-email

**Send verification email to current user**

**Auth**: Required (JWT)

**Body**: Empty or `{}`

**Response**: `200 OK`
```json
{
  "success": true,
  "message": "Verification email sent to user@example.com. Check your inbox within 5 minutes."
}
```

**Error Responses**:
- `401` — Not authenticated
- `400` — Email not set on profile
- `429` — Too many verification requests (max 3 per hour)
- `500` — Failed to send email

---

### POST /api/v1/users/verify-email-token

**Confirm email verification with token**

**Auth**: Required (JWT)

**Body**:
```json
{
  "token": "BASE64_ENCODED_TOKEN_FROM_EMAIL_LINK"
}
```

**Response**: `200 OK`
```json
{
  "success": true,
  "message": "Email verified successfully! You can now create automations.",
  "emailVerified": true
}
```

**Error Responses**:
- `400` — Invalid or expired token
- `401` — Not authenticated
- `404` — Token not found

---

### GET /api/v1/users/profile

**Get current user profile** (includes verification status)

**Auth**: Required (JWT)

**Response**: `200 OK`
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "fullName": "John Doe",
  "role": "User",
  "planCode": "FREE",
  "isActive": true,
  "emailVerified": false,
  "marketingEmailEnabled": true,
  "marketingPushEnabled": true
}
```

---

### POST /api/v1/users/resend-verification-email (Admin)

**Resend verification email to a user** (admin only)

**Auth**: Required (JWT + Admin role)

**Body**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response**: `200 OK`

---

## 3. React Components

### 3.1 Email Verification Prompt

```jsx
import React, { useState } from 'react';
import { useAuth } from './context/AuthContext';

export function EmailVerificationPrompt() {
  const { user, sendVerificationEmail } = useAuth();
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  if (!user || user.emailVerified) {
    return null; // Don't show if verified
  }

  const handleSendEmail = async () => {
    setLoading(true);
    setError('');
    setMessage('');

    try {
      const response = await fetch('/api/v1/users/verify-email', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        setMessage('✓ Verification email sent! Check your inbox.');
      } else {
        const data = await response.json();
        setError(data.message || 'Failed to send verification email');
      }
    } catch (err) {
      setError('Network error. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="email-verification-banner bg-yellow-50 border-l-4 border-yellow-400 p-4">
      <div className="flex items-start">
        <div className="flex-shrink-0">
          <svg className="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
          </svg>
        </div>
        <div className="ml-3 flex-1">
          <h3 className="text-sm font-medium text-yellow-800">
            Verify your email to activate automations
          </h3>
          <p className="mt-2 text-sm text-yellow-700">
            We need to verify your email address before you can enable automations. 
            This helps us send you important subscription and billing notifications.
          </p>
          <div className="mt-4">
            {error && <p className="text-red-600 text-sm mb-2">{error}</p>}
            {message && <p className="text-green-600 text-sm mb-2">{message}</p>}
            <button
              onClick={handleSendEmail}
              disabled={loading}
              className="px-4 py-2 bg-yellow-600 text-white rounded hover:bg-yellow-700 disabled:opacity-50"
            >
              {loading ? 'Sending...' : 'Send Verification Email'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
```

### 3.2 Email Verification Page

```jsx
import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';

export function EmailVerificationPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState('verifying'); // verifying, success, error
  const [message, setMessage] = useState('');

  useEffect(() => {
    const verifyEmail = async () => {
      const token = searchParams.get('token');

      if (!token) {
        setStatus('error');
        setMessage('No verification token found in URL');
        return;
      }

      try {
        const response = await fetch('/api/v1/users/verify-email-token', {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({ token })
        });

        if (response.ok) {
          setStatus('success');
          setMessage('✓ Email verified successfully!');
          
          // Redirect to dashboard after 2 seconds
          setTimeout(() => {
            navigate('/dashboard');
          }, 2000);
        } else {
          const data = await response.json();
          setStatus('error');
          setMessage(data.message || 'Verification failed. Token may have expired.');
        }
      } catch (err) {
        setStatus('error');
        setMessage('Network error. Please try again.');
      }
    };

    verifyEmail();
  }, [searchParams, navigate]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        {status === 'verifying' && (
          <div className="text-center">
            <div className="animate-spin inline-flex h-8 w-8 border-4 border-blue-500 border-t-transparent rounded-full"></div>
            <p className="mt-4 text-gray-600">Verifying your email...</p>
          </div>
        )}

        {status === 'success' && (
          <div className="text-center">
            <div className="text-green-500 text-4xl mb-4">✓</div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Email Verified!</h2>
            <p className="text-gray-600">{message}</p>
            <p className="text-sm text-gray-500 mt-4">Redirecting to dashboard...</p>
          </div>
        )}

        {status === 'error' && (
          <div className="text-center">
            <div className="text-red-500 text-4xl mb-4">✕</div>
            <h2 className="text-2xl font-bold text-gray-900 mb-2">Verification Failed</h2>
            <p className="text-gray-600 mb-6">{message}</p>
            <button
              onClick={() => window.location.href = '/'}
              className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
            >
              Go to Dashboard
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
```

### 3.3 Automation Creation Guard

```jsx
import React from 'react';
import { useAuth } from './context/AuthContext';
import { EmailVerificationPrompt } from './EmailVerificationPrompt';

export function CreateAutomationPage() {
  const { user } = useAuth();

  if (!user?.emailVerified) {
    return (
      <div className="max-w-7xl mx-auto py-12 px-4">
        <EmailVerificationPrompt />
        <div className="mt-8 bg-gray-100 p-8 rounded-lg text-center">
          <p className="text-gray-600">
            Please verify your email to create automations.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div>
      {/* Rest of automation creation UI */}
    </div>
  );
}
```

---

## 4. Vue Components

### 4.1 Email Verification Prompt (Vue 3)

```vue
<template>
  <div v-if="!user?.emailVerified" class="email-verification-banner">
    <div class="banner-content">
      <div class="icon">⚠️</div>
      <div class="message">
        <h3>Verify your email to activate automations</h3>
        <p>We need to verify your email address before you can enable automations.</p>
        
        <div v-if="error" class="error">{{ error }}</div>
        <div v-if="message" class="success">{{ message }}</div>
        
        <button 
          @click="handleSendEmail" 
          :disabled="loading"
          class="btn btn-primary"
        >
          {{ loading ? 'Sending...' : 'Send Verification Email' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useAuth } from '@/composables/useAuth';

const { user } = useAuth();
const loading = ref(false);
const message = ref('');
const error = ref('');

const handleSendEmail = async () => {
  loading.value = true;
  error.value = '';
  message.value = '';

  try {
    const response = await fetch('/api/v1/users/verify-email', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
        'Content-Type': 'application/json'
      }
    });

    if (response.ok) {
      message.value = '✓ Verification email sent! Check your inbox.';
    } else {
      const data = await response.json();
      error.value = data.message || 'Failed to send verification email';
    }
  } catch (err) {
    error.value = 'Network error. Please try again.';
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.email-verification-banner {
  background-color: #fef3c7;
  border-left: 4px solid #f59e0b;
  padding: 1rem;
  margin-bottom: 2rem;
  border-radius: 0.5rem;
}

.banner-content {
  display: flex;
  gap: 1rem;
}

.icon {
  font-size: 1.5rem;
  flex-shrink: 0;
}

.message h3 {
  margin: 0 0 0.5rem 0;
  font-weight: 600;
}

.message p {
  color: #92400e;
  margin: 0 0 1rem 0;
}

.error {
  color: #dc2626;
  margin-bottom: 0.5rem;
  font-size: 0.875rem;
}

.success {
  color: #059669;
  margin-bottom: 0.5rem;
  font-size: 0.875rem;
}

.btn {
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 0.375rem;
  cursor: pointer;
  font-weight: 500;
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
```

---

## 5. Error Handling

### Common Errors & Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| "Email not set" | User has no email | Frontend should ask for email before showing verification |
| "Too many requests" | >3 emails/hour | Show rate limit message, offer retry button in 1 hour |
| "Token expired" | >24 hours since send | Show "Resend Email" option |
| "Invalid token" | Token tampered | Show "Resend Email" option |
| "Network error" | Backend unreachable | Show retry button |

### Error Handling Code

```javascript
const handleVerificationError = (error) => {
  const errorMap = {
    'INVALID_TOKEN': 'The link has expired. Please request a new verification email.',
    'TOKEN_EXPIRED': 'The link has expired. Please request a new verification email.',
    'EMAIL_NOT_SET': 'Please add your email to your profile first.',
    'TOO_MANY_REQUESTS': 'Too many requests. Please try again in 1 hour.',
    'NETWORK_ERROR': 'Network connection error. Please check your internet.',
  };

  return errorMap[error.code] || error.message || 'An unexpected error occurred.';
};
```

---

## 6. Data Models

### User Profile (with verification)

```typescript
interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  role: 'User' | 'Admin';
  planCode: 'FREE' | 'STARTER' | 'PRO';
  isActive: boolean;
  emailVerified: boolean;           // ← New field
  marketingEmailEnabled: boolean;
  marketingPushEnabled: boolean;
}
```

### Verification Request

```typescript
interface VerifyEmailRequest {
  token: string;  // From email link: ?token=ABC123
}
```

### Verification Response

```typescript
interface VerifyEmailResponse {
  success: boolean;
  message: string;
  emailVerified: boolean;
}
```

---

## 7. State Management

### Redux / Zustand Pattern

```javascript
// Auth store
const authStore = create((set) => ({
  user: null,
  emailVerified: false,
  
  setUser: (user) => set({ user, emailVerified: user.emailVerified }),
  
  setEmailVerified: () => set({ emailVerified: true }),
  
  sendVerificationEmail: async () => {
    const response = await fetch('/api/v1/users/verify-email', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    return response.json();
  },
  
  verifyEmailToken: async (token) => {
    const response = await fetch('/api/v1/users/verify-email-token', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${getToken()}` },
      body: JSON.stringify({ token })
    });
    
    if (response.ok) {
      set({ emailVerified: true });
    }
    return response.json();
  }
}));
```

---

## 8. UX Best Practices

### 8.1 When to Show Verification Prompt

```javascript
// Show after OAuth callback
function showVerificationIfNeeded(user) {
  if (!user.emailVerified) {
    showBanner(); // Show yellow banner at top
    blockAutomationCreation(); // Disable automation feature
  }
}
```

### 8.2 Email Link Format

**Email should contain link like**:
```
https://yourdomain.com/verify-email?token=ABC123DEF456...
```

**Frontend extracts token**:
```javascript
const params = new URLSearchParams(window.location.search);
const token = params.get('token');
```

### 8.3 User Guidance

**On Dashboard**:
- Show yellow banner with verification status
- "Verify Email" button
- Link to resend if needed

**In Automation Create**:
- Block form if email not verified
- Show reason: "Email verification required"
- Quick link to verify

**After Verification**:
- Show success message
- Auto-enable automations feature
- Redirect to dashboard or automation creation

### 8.4 Email Content

**Subject**: `Verify your KromicFlow email`

**Body**:
```
Hi [Name],

Thank you for signing up for KromicFlow!

To activate your automations, please verify your email by clicking the link below:

[VERIFICATION_LINK]

This link expires in 24 hours.

If you didn't sign up, you can ignore this email.

Best regards,
KromicFlow Team
```

---

## 9. Testing

### Manual Testing Checklist

- [ ] New user sees verification prompt after OAuth
- [ ] Clicking "Send Verification Email" triggers email
- [ ] Email contains verification link with token
- [ ] Clicking link opens verification page
- [ ] Token verified successfully, EmailVerified set to true
- [ ] Automations feature now enabled
- [ ] Trying to create automation without verification shows error (403)
- [ ] "Resend Email" button works
- [ ] Expired token (>24h) shows error
- [ ] Invalid token shows error
- [ ] Rate limiting works (>3 per hour blocked)

### Unit Tests

```javascript
describe('EmailVerification', () => {
  it('should show verification prompt for unverified users', () => {
    const user = { emailVerified: false };
    render(<EmailVerificationPrompt user={user} />);
    expect(screen.getByText(/Verify your email/i)).toBeInTheDocument();
  });

  it('should hide verification prompt for verified users', () => {
    const user = { emailVerified: true };
    const { container } = render(<EmailVerificationPrompt user={user} />);
    expect(container.firstChild).toBeNull();
  });

  it('should send verification email on button click', async () => {
    render(<EmailVerificationPrompt />);
    const button = screen.getByText(/Send Verification Email/i);
    
    fireEvent.click(button);
    await waitFor(() => {
      expect(screen.getByText(/Verification email sent/i)).toBeInTheDocument();
    });
  });

  it('should verify email with token', async () => {
    render(<EmailVerificationPage />);
    await waitFor(() => {
      expect(screen.getByText(/Email verified successfully/i)).toBeInTheDocument();
    });
  });
});
```

---

## Summary

**Frontend handles**:
- ✅ Show/hide verification prompt based on user status
- ✅ Send verification email on button click
- ✅ Extract token from email link
- ✅ Verify token with backend
- ✅ Show success/error messages
- ✅ Block automations until verified
- ✅ Handle all error states

**Backend handles** (already implemented):
- ✅ Generate secure tokens
- ✅ Send emails via Brevo
- ✅ Validate tokens
- ✅ Rate limiting
- ✅ Token expiration

**Integration steps**:
1. Add these components to your React/Vue app
2. Hook into your auth flow after login
3. Update automation create page to check `user.emailVerified`
4. Test with real email
5. Deploy!

---

**Last Updated**: July 20, 2026  
**Status**: Ready for frontend integration
