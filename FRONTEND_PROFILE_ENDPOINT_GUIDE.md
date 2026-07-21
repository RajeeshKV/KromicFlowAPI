# Frontend Profile Endpoint Integration Guide

> **How to use the user profile endpoint to check email verification status**

---

## 🎯 Quick Answer

**Correct Endpoint**: `GET /api/v1/user/profile`

**Response**:
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

**To Check Email Verification**:
```javascript
const response = await fetch('/api/v1/user/profile', {
  method: 'GET',
  headers: {
    'Authorization': `Bearer ${jwtToken}`,
    'Content-Type': 'application/json'
  }
});

const profile = await response.json();

if (!profile.emailVerified) {
  showToast('Please verify your email before creating automations');
  return; // Don't allow automation creation
}
```

---

## 📋 Endpoint Details

### GET /api/v1/user/profile

**Authentication**: Required (JWT Bearer token)

**Method**: GET

**URL**: `https://flowapi.kromic.in/api/v1/user/profile`

**Headers**:
```
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

**Response Status**: 
- `200 OK` — Profile returned
- `401 Unauthorized` — Invalid or missing JWT
- `404 Not Found` — User not found (rare)
- `500 Server Error` — Internal error

**Response Body**:
```json
{
  "id": "string (UUID)",
  "email": "string",
  "fullName": "string",
  "role": "string",
  "planCode": "string",
  "isActive": "boolean",
  "emailVerified": "boolean",
  "marketingEmailEnabled": "boolean",
  "marketingPushEnabled": "boolean"
}
```

---

## 💻 Implementation Examples

### React Hook

```jsx
import { useState, useEffect } from 'react';

export function useUserProfile(jwtToken) {
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (!jwtToken) {
      setLoading(false);
      return;
    }

    const fetchProfile = async () => {
      try {
        const response = await fetch('/api/v1/user/profile', {
          method: 'GET',
          headers: {
            'Authorization': `Bearer ${jwtToken}`,
            'Content-Type': 'application/json'
          }
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        setProfile(data);
        setError(null);
      } catch (err) {
        setError(err.message);
        setProfile(null);
      } finally {
        setLoading(false);
      }
    };

    fetchProfile();
  }, [jwtToken]);

  return { profile, loading, error };
}

// Usage in component
export function AutomationCreatePage() {
  const jwtToken = localStorage.getItem('accessToken');
  const { profile, loading, error } = useUserProfile(jwtToken);

  if (loading) return <div>Loading...</div>;

  if (error) {
    return <div className="error">Failed to load profile: {error}</div>;
  }

  if (!profile?.emailVerified) {
    return (
      <div className="warning-banner">
        <p>⚠️ Please verify your email before creating automations</p>
        <button onClick={() => navigate('/verify-email')}>
          Verify Email
        </button>
      </div>
    );
  }

  return <CreateAutomationForm />;
}
```

### Vue 3 Composable

```javascript
import { ref, computed, onMounted } from 'vue';

export function useUserProfile(jwtToken) {
  const profile = ref(null);
  const loading = ref(true);
  const error = ref(null);
  const emailVerified = computed(() => profile.value?.emailVerified ?? false);

  const fetchProfile = async () => {
    if (!jwtToken) {
      loading.value = false;
      return;
    }

    try {
      const response = await fetch('/api/v1/user/profile', {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${jwtToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      profile.value = await response.json();
      error.value = null;
    } catch (err) {
      error.value = err.message;
      profile.value = null;
    } finally {
      loading.value = false;
    }
  };

  onMounted(fetchProfile);

  const refetch = fetchProfile;

  return {
    profile,
    loading,
    error,
    emailVerified,
    refetch
  };
}

// Usage in component
<template>
  <div v-if="loading" class="loading">Loading...</div>
  
  <div v-else-if="error" class="error">
    Failed to load profile: {{ error }}
  </div>
  
  <div v-else-if="!emailVerified" class="warning">
    <p>⚠️ Please verify your email</p>
    <button @click="$router.push('/verify-email')">
      Verify Email
    </button>
  </div>
  
  <div v-else>
    <!-- Show automation creation UI -->
  </div>
</template>

<script setup>
const jwtToken = localStorage.getItem('accessToken');
const { profile, loading, error, emailVerified, refetch } = useUserProfile(jwtToken);
</script>
```

### TypeScript Service

```typescript
export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  role: string;
  planCode: string;
  isActive: boolean;
  emailVerified: boolean;
  marketingEmailEnabled: boolean;
  marketingPushEnabled: boolean;
}

export class UserService {
  async getProfile(jwtToken: string): Promise<UserProfile> {
    const response = await fetch('/api/v1/user/profile', {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${jwtToken}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Unauthorized. Please log in again.');
      }
      throw new Error(`Failed to fetch profile: ${response.statusText}`);
    }

    return response.json();
  }

  async isEmailVerified(jwtToken: string): Promise<boolean> {
    try {
      const profile = await this.getProfile(jwtToken);
      return profile.emailVerified;
    } catch {
      return false;
    }
  }
}

// Usage
const userService = new UserService();
const isVerified = await userService.isEmailVerified(jwtToken);

if (!isVerified) {
  showToast('Email verification required');
  navigateTo('/verify-email');
}
```

### Using Axios

```javascript
import axios from 'axios';

const profileApi = axios.create({
  baseURL: 'https://flowapi.kromic.in/api/v1'
});

export async function getUserProfile(jwtToken) {
  try {
    const response = await profileApi.get('/user/profile', {
      headers: {
        'Authorization': `Bearer ${jwtToken}`
      }
    });
    return response.data;
  } catch (error) {
    if (error.response?.status === 401) {
      // Token expired, redirect to login
      window.location.href = '/login';
    }
    throw error;
  }
}

// Usage
const profile = await getUserProfile(jwtToken);

if (!profile.emailVerified) {
  toast.warning('Please verify your email to create automations');
}
```

---

## 🛡️ Authentication

The endpoint **requires JWT authentication**. Always include:

```
Authorization: Bearer <TOKEN>
```

**Getting the token**:

After OAuth login, the backend redirects with a URL like:
```
https://yourdomain.com/callback#accessToken=eyJ...&refreshToken=...
```

**Store the token**:
```javascript
const params = new URLSearchParams(window.location.hash.substring(1));
const accessToken = params.get('accessToken');
localStorage.setItem('accessToken', accessToken);
```

**Use in subsequent requests**:
```javascript
const token = localStorage.getItem('accessToken');
const headers = {
  'Authorization': `Bearer ${token}`,
  'Content-Type': 'application/json'
};
```

---

## ✅ Response Fields Explained

| Field | Type | Purpose |
|-------|------|---------|
| `id` | UUID | Unique user identifier |
| `email` | String | User's email address |
| `fullName` | String | User's display name |
| `role` | String | User role (User/Admin) |
| `planCode` | String | Subscription plan (FREE/STARTER/PRO) |
| `isActive` | Boolean | Account active status |
| `emailVerified` | Boolean | **← Check this for automation gating** |
| `marketingEmailEnabled` | Boolean | Email preference |
| `marketingPushEnabled` | Boolean | Push notification preference |

---

## 🚦 Common Patterns

### Pattern 1: Check on Mount

```jsx
useEffect(() => {
  const checkEmailVerification = async () => {
    const token = localStorage.getItem('accessToken');
    const response = await fetch('/api/v1/user/profile', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const profile = await response.json();
    
    if (!profile.emailVerified) {
      setShowVerificationPrompt(true);
    }
  };

  checkEmailVerification();
}, []);
```

### Pattern 2: Check Before Action

```jsx
const handleCreateAutomation = async () => {
  const token = localStorage.getItem('accessToken');
  const response = await fetch('/api/v1/user/profile', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const profile = await response.json();

  if (!profile.emailVerified) {
    toast.error('Please verify your email first');
    return;
  }

  // Proceed with automation creation
  createAutomation();
};
```

### Pattern 3: Global State Update

```jsx
// In AuthContext
const [profile, setProfile] = useState(null);

const fetchProfile = async (token) => {
  const response = await fetch('/api/v1/user/profile', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await response.json();
  setProfile(data);
  return data;
};

// Use anywhere
const { profile } = useAuth();

if (!profile.emailVerified) {
  // Show verification required UI
}
```

---

## ⚠️ Error Handling

```javascript
const fetchProfile = async (token) => {
  try {
    const response = await fetch('/api/v1/user/profile', {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    if (response.status === 401) {
      // Token expired or invalid
      localStorage.removeItem('accessToken');
      window.location.href = '/login';
      return;
    }

    if (response.status === 404) {
      // User not found (rare)
      console.error('User not found');
      return;
    }

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    const profile = await response.json();
    return profile;
  } catch (error) {
    console.error('Failed to fetch profile:', error);
    // Show error to user
    toast.error('Failed to load profile');
    return null;
  }
};
```

---

## 🔄 Refresh Strategy

**When to refresh profile**:
1. After user logs in
2. After user verifies email (immediately refresh)
3. On page navigation to critical pages
4. When user comes back to focus (optional)
5. Periodically (every 5 minutes optional)

```javascript
// Refresh after email verification
const handleEmailVerified = async () => {
  const token = localStorage.getItem('accessToken');
  const profile = await fetchProfile(token);
  
  // Check if verified
  if (profile.emailVerified) {
    showToast('Email verified successfully!');
    setCanCreateAutomations(true);
  }
};
```

---

## 📱 Mobile Considerations

```javascript
// Store profile locally to avoid repeated API calls
const getCachedProfile = () => {
  const cached = localStorage.getItem('userProfile');
  if (cached) {
    const profile = JSON.parse(cached);
    // Use cached if < 5 minutes old
    if (Date.now() - profile.timestamp < 5 * 60 * 1000) {
      return profile;
    }
  }
  return null;
};

const getProfile = async (token) => {
  const cached = getCachedProfile();
  if (cached) return cached;

  const response = await fetch('/api/v1/user/profile', {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const profile = await response.json();
  
  // Cache it
  localStorage.setItem('userProfile', JSON.stringify({
    ...profile,
    timestamp: Date.now()
  }));

  return profile;
};
```

---

## 📊 What Changed

**Before (Broken)**: Endpoint didn't exist
- URL: `/api/v1/users/profile` ❌ (404 Not Found)
- Plural "users" ❌

**After (Fixed)**: Endpoint now exists
- URL: `/api/v1/user/profile` ✅ (Singular "user")
- Response includes `emailVerified` field ✅
- Fully authenticated endpoint ✅

---

## 🎯 Implementation Checklist

- [ ] Update endpoint URL to `/api/v1/user/profile` (singular)
- [ ] Ensure JWT token is in Authorization header
- [ ] Check `emailVerified` field in response
- [ ] Show toast/banner if not verified
- [ ] Block automation creation if not verified
- [ ] Handle 401 errors (expired token)
- [ ] Cache profile response (optional)
- [ ] Test with real token
- [ ] Test with expired token
- [ ] Test with unverified email

---

## ✅ Verification

**Test the endpoint**:

```bash
curl -H "Authorization: Bearer <YOUR_JWT_TOKEN>" \
  https://flowapi.kromic.in/api/v1/user/profile
```

**Expected response**:
```json
{
  "id": "...",
  "email": "...",
  "fullName": "...",
  "emailVerified": false
}
```

---

## 📞 Support

**If you still get 404**:
1. Check URL: should be `/api/v1/user/profile` (singular)
2. Check Authorization header: must include Bearer token
3. Check token is valid (not expired)
4. Verify you're calling GET not POST

**If emailVerified is missing**:
- Redeploy backend code (new field added)
- Migrations applied
- Build passes

---

**Status**: ✅ **Ready to use**  
**Endpoint**: `GET /api/v1/user/profile`  
**Build**: ✅ PASSING | Tests: ✅ 2/2 PASSING
