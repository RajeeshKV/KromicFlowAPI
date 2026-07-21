# Instagram Profile Image Integration Guide

## Overview

Profile pictures from Instagram accounts are now available via a dedicated API endpoint that proxies the image through the KromicFlow backend.

**Base URL**: `https://api.example.com/api/v1/instagram/{accountId}/profile-image`

---

## Quick Start

### React Component

```jsx
import { useEffect, useState } from 'react';

export function InstagramAccountCard({ account, jwtToken }) {
  const [imageError, setImageError] = useState(false);

  const profileImageUrl = `/api/v1/instagram/${account.id}/profile-image`;

  return (
    <div className="account-card">
      <div className="profile-picture">
        {imageError ? (
          <div className="placeholder">No Image</div>
        ) : (
          <img
            src={profileImageUrl}
            alt={account.username}
            onError={() => setImageError(true)}
            className="rounded-full"
          />
        )}
      </div>
      <div className="account-info">
        <h3>{account.displayName}</h3>
        <p>@{account.username}</p>
      </div>
    </div>
  );
}
```

### HTML (Simple)

```html
<img 
  src="/api/v1/instagram/{accountId}/profile-image" 
  alt="Instagram Profile"
  class="profile-picture"
  onerror="this.src='/images/placeholder.png'"
/>
```

---

## Endpoint Details

### GET /api/v1/instagram/{accountId}/profile-image

**Authentication**: Required (JWT Bearer token)

**Response Type**: Image stream (image/jpeg or image/png)

**Status Codes**:
- `200` — Image returned successfully
- `401` — Unauthorized (missing or invalid JWT)
- `404` — Account not found or no profile picture available
- `502` — Failed to fetch image from Instagram (temporary)
- `504` — Request timeout

**Response Headers**:
```
Content-Type: image/jpeg
Content-Length: <size>
Accept-Ranges: bytes
```

---

## Frontend Implementation Examples

### 1. React with Error Handling

```jsx
import React, { useState, useEffect } from 'react';

export function ProfileImage({ accountId, jwtToken, className = '' }) {
  const [imageUrl, setImageUrl] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    // Since we're using an <img> tag, headers are automatic
    const url = `/api/v1/instagram/${accountId}/profile-image`;
    setImageUrl(url);
    setLoading(false);
  }, [accountId]);

  if (loading) {
    return <div className={`${className} skeleton-loader`} />;
  }

  if (error) {
    return (
      <div className={`${className} placeholder`}>
        <span>No Image</span>
      </div>
    );
  }

  return (
    <img
      src={imageUrl}
      alt="Instagram Profile"
      className={className}
      onError={() => setError(true)}
      onLoad={() => setError(null)}
    />
  );
}
```

### 2. Vue Component

```vue
<template>
  <div class="profile-image-container">
    <img
      v-if="!error"
      :src="`/api/v1/instagram/${accountId}/profile-image`"
      :alt="`${username}'s profile`"
      class="profile-picture"
      @error="error = true"
      @load="onImageLoad"
    />
    <div v-else class="image-placeholder">
      {{ username }}
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';

defineProps({
  accountId: String,
  username: String
});

const error = ref(false);

function onImageLoad() {
  error.value = false;
}
</script>
```

### 3. TypeScript Service

```typescript
export class InstagramService {
  constructor(private http: HttpClient) {}

  /**
   * Get profile image URL for an Instagram account
   * Note: The image is fetched directly from the endpoint
   * Frontend should handle auth via interceptor
   */
  getProfileImageUrl(accountId: string): string {
    return `/api/v1/instagram/${accountId}/profile-image`;
  }

  /**
   * Download image as blob (for saving, processing, etc.)
   */
  async downloadProfileImage(accountId: string): Promise<Blob> {
    return this.http
      .get(`/api/v1/instagram/${accountId}/profile-image`, {
        responseType: 'blob'
      })
      .toPromise()
      .then(blob => blob || new Blob());
  }

  /**
   * Download and convert to data URL
   */
  async getProfileImageAsDataUrl(accountId: string): Promise<string> {
    const blob = await this.downloadProfileImage(accountId);
    return new Promise((resolve) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.readAsDataURL(blob);
    });
  }
}

// Usage in component
export class AccountListComponent {
  constructor(private instagramService: InstagramService) {}

  getProfileImageUrl(accountId: string): string {
    return this.instagramService.getProfileImageUrl(accountId);
  }
}
```

### 4. Displaying in a List

```jsx
export function InstagramAccountsList({ accounts, jwtToken }) {
  return (
    <div className="accounts-list">
      {accounts.map(account => (
        <div key={account.id} className="account-item">
          <img
            src={`/api/v1/instagram/${account.id}/profile-image`}
            alt={account.username}
            className="w-16 h-16 rounded-full"
            onError={(e) => {
              e.target.src = '/images/default-avatar.png';
            }}
          />
          <div className="account-details">
            <h4>{account.displayName}</h4>
            <p className="text-sm text-gray-500">@{account.username}</p>
          </div>
        </div>
      ))}
    </div>
  );
}
```

---

## Authentication

### Automatic (with HTTP Interceptor)

If your frontend has an HTTP interceptor that adds the JWT token to all requests, the image will load automatically:

```typescript
// HTTP Interceptor (e.g., in Angular)
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.authService.getToken();
    if (token) {
      req = req.clone({
        setHeaders: { Authorization: `Bearer ${token}` }
      });
    }
    return next.handle(req);
  }
}
```

### Manual (if needed)

In rare cases, if you need to download the image programmatically:

```typescript
async function downloadImage(accountId: string, jwtToken: string): Promise<Blob> {
  const response = await fetch(`/api/v1/instagram/${accountId}/profile-image`, {
    method: 'GET',
    headers: {
      'Authorization': `Bearer ${jwtToken}`
    }
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch image: ${response.statusCode}`);
  }

  return response.blob();
}
```

---

## Error Handling

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| `404` | Account not found | Verify account ID exists |
| `401` | Not authenticated | Ensure JWT token is sent |
| `502` | Instagram API error | Temporary issue, retry after 5s |
| `504` | Timeout | Image is large/network slow, retry |
| `<img>` shows broken | HTTP error | Add `onerror` handler with fallback |

### Implementation

```jsx
export function RobustProfileImage({ accountId, className = '' }) {
  const [src, setSrc] = useState(
    `/api/v1/instagram/${accountId}/profile-image`
  );

  const handleError = () => {
    // Fallback to default image on any error
    setSrc('/images/default-profile.png');
  };

  return (
    <img
      src={src}
      alt="Profile"
      className={className}
      onError={handleError}
    />
  );
}
```

---

## Performance Optimization

### 1. Caching in Browser

The browser will cache the image automatically. To control caching:

```jsx
// Force refresh (bypasses cache)
const refreshImageUrl = () => {
  const timestamp = new Date().getTime();
  return `/api/v1/instagram/${accountId}/profile-image?t=${timestamp}`;
};
```

### 2. Lazy Loading

```jsx
<img
  src={`/api/v1/instagram/${accountId}/profile-image`}
  alt="Profile"
  loading="lazy"
  className="w-16 h-16"
/>
```

### 3. Image Optimization (CSS)

```css
.profile-image {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  object-fit: cover;
  background-color: #e0e0e0;
}
```

### 4. Next.js Image Component

```jsx
import Image from 'next/image';

export function ProfileImage({ accountId }) {
  return (
    <Image
      src={`/api/v1/instagram/${accountId}/profile-image`}
      alt="Instagram Profile"
      width={64}
      height={64}
      className="rounded-full"
      priority={false}
    />
  );
}
```

---

## Data Format

**Request**:
```
GET /api/v1/instagram/{accountId}/profile-image HTTP/1.1
Host: api.example.com
Authorization: Bearer <jwt_token>
```

**Response** (Success - 200):
```
HTTP/1.1 200 OK
Content-Type: image/jpeg
Content-Length: 8192
Accept-Ranges: bytes

[Binary image data]
```

**Response** (Not Found - 404):
```
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "error": "Account not found"
}
```

**Response** (Unauthorized - 401):
```
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{
  "error": "Unauthorized"
}
```

---

## Troubleshooting

### Image Won't Load

1. **Check JWT token is being sent**
   - Open DevTools → Network tab
   - Click the image request
   - Verify `Authorization: Bearer ...` header is present

2. **Check account ID is correct**
   - Get account ID from `/api/v1/instagram/accounts` endpoint
   - Verify in URL: `/api/v1/instagram/{id}/profile-image`

3. **Check account has profile picture**
   - Call `/api/v1/instagram/accounts` endpoint
   - Look for account with `profilePicture` not empty

4. **Check network connectivity**
   - Verify backend can reach Instagram API
   - Check if Instagram API is having issues

### Image Shows as 404

- Account doesn't exist
- Account was disconnected
- User isn't authenticated

### Image Shows Broken (after loading worked)

- Instagram changed their API URL (rare)
- Access token expired
- Backend temporarily can't reach Instagram

**Solution**: Add error handler with fallback:
```jsx
onError={() => setSrc('/default-avatar.png')}
```

---

## Attributes to Use

```jsx
// Always include these for best results
<img
  src={`/api/v1/instagram/${accountId}/profile-image`}
  alt={`${username}'s profile picture`}           // For accessibility
  loading="lazy"                                   // Performance
  className="rounded-full object-cover"            // Styling
  onError={(e) => e.target.src = '/fallback.png'} // Error handling
  width={64}                                       // Size hint
  height={64}
/>
```

---

## CORS & Security

- The endpoint respects JWT authentication
- CORS is configured to allow requests from your frontend domain
- Images are returned with appropriate cache headers
- No sensitive data is exposed in responses

---

## Future Enhancements

### Coming Soon (Optional)

- Image resizing endpoint: `/api/v1/instagram/{id}/profile-image?width=200&height=200`
- WebP format support for modern browsers
- CDN integration for faster delivery
- Local storage option to cache images on backend

---

## See Also

- API Documentation: `docs/08-API/02-InstagramAccountsAPI.md`
- Instagram Integration: `docs/07-Meta/05-MediaSync.md`
- Profile Storage Guide: `docs/11-Features/00-ProfileImageStorage.md`

---

**Last Updated**: July 20, 2026  
**Status**: ✅ Ready for production
