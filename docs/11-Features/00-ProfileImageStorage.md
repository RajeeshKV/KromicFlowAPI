# Instagram Profile Image Storage Strategy

## Current State

| Component | Current Implementation | Issue |
|-----------|------------------------|-------|
| **Fetch** | ✅ Via Meta Graph API (`/me?fields=profile_picture_url`) | Direct URL may expire or have access restrictions |
| **Storage** | ✅ Stored in `InstagramAccount.ProfilePicture` column (URL) | Only stores URL, not the actual image |
| **Sync** | ✅ During OAuth + periodic refresh via SyncInstagramAccountCommand | Works but depends on Meta API URL availability |
| **Display** | ❌ Frontend gets URL string directly | Frontend can't display if URL is dead or restricted |

---

## Problem

**Users see broken images if:**
1. Meta API changes their URL format
2. Meta access token expires for the image endpoint
3. Meta restricts access to profile images (auth required)
4. Profile picture is deleted on Instagram but we still reference old URL
5. Network latency: loading images directly from Meta on every page load

---

## Three Storage Options

### Option 1: **Meta URL Proxy** (Recommended for MVP)

**How it works:**
```
Frontend → /api/v1/instagram/{accountId}/profile-image
     ↓
Backend proxy request to Meta API
     ↓
Download image bytes
     ↓
Return to frontend (cached or streamed)
```

**Implementation:**
- New endpoint: `GET /api/v1/instagram/{accountId}/profile-image`
- Backend fetches from Meta using access token
- No database changes needed
- Simple, works immediately

**Pros:**
- ✅ No storage infrastructure needed
- ✅ Always fresh (Meta is source of truth)
- ✅ Easy to implement (1 controller + 1 service method)
- ✅ No data duplication
- ✅ Works with existing token refresh

**Cons:**
- ❌ Extra network hop (Backend → Meta on each request)
- ❌ Depends on Meta API availability
- ❌ Slower than direct URL (frontend can't cache Meta URL)
- ❌ Uses backend bandwidth

**Timeline**: 15 minutes

---

### Option 2: **Local Storage (S3 / Azure Blob / Local FS)**

**How it works:**
```
OAuth/Sync → Download image from Meta → Store in S3/Blob/FS
     ↓
Store S3 URL/path in `InstagramAccount.ProfilePictureStorageUrl`
     ↓
Frontend uses storage URL (cached, CDN-able)
```

**Implementation:**
- Add `ProfilePictureStorageUrl` column to `InstagramAccount`
- Create image storage service (S3Client, AzureBlobClient, LocalFileClient)
- On OAuth: download image → store → save URL
- On sync: download image → update in storage
- New endpoint: `GET /api/v1/instagram/{accountId}/profile-image` (serves from storage)

**Pros:**
- ✅ Persistent, can cache aggressively
- ✅ CDN-friendly (serve from edge)
- ✅ Fast (no Meta API call on every request)
- ✅ Survives Meta API changes
- ✅ Supports future processing (resize, crop, optimize)

**Cons:**
- ❌ Requires storage infrastructure (S3/Blob/local)
- ❌ Storage costs
- ❌ Need backup strategy
- ❌ Images can get out-of-sync if user changes on Instagram
- ❌ Migration: 1 database column + migration

**Timeline**: 1-2 hours (with S3), 30 mins (local FS for dev)

---

### Option 3: **Hybrid (Meta URL + Fallback to Storage)**

**How it works:**
```
Try Meta URL → If fails → Try storage URL → If fails → Placeholder image
```

**Implementation:**
- Store both `ProfilePicture` (Meta URL) and `ProfilePictureStorageUrl` (storage)
- Frontend tries Meta URL first
- On failure, frontend retries with storage URL
- Periodic batch job: re-download images to storage if Meta URL fails

**Pros:**
- ✅ Best of both worlds
- ✅ Progressive enhancement
- ✅ Resilient

**Cons:**
- ❌ Most complex to implement
- ❌ Storage costs
- ❌ Maintenance burden

**Timeline**: 2+ hours

---

## Recommendation: **Option 1 (Meta URL Proxy) for MVP**

**Reasoning:**
- Fast to implement (you need something now)
- No infrastructure needed
- Reliable (uses existing access token refresh)
- Easy to migrate to Option 2 later

**Then migrate to Option 2 (Local Storage) when:**
- You have infrastructure ready (S3)
- Performance is critical
- You want CDN caching

---

## Implementation Plan

### Quick Win: Meta URL Proxy (15 mins)

#### Step 1: Add endpoint to InstagramController

```csharp
/// <summary>
/// GET /api/v1/instagram/{accountId}/profile-image
/// Returns Instagram profile picture as image stream
/// </summary>
[HttpGet("{accountId}/profile-image")]
public async Task<IActionResult> GetProfileImage(Guid accountId, CancellationToken cancellationToken)
{
    var account = await db.InstagramAccounts
        .FirstOrDefaultAsync(x => x.Id == accountId && x.UserId == UserId, cancellationToken);
    
    if (account is null)
        return NotFound();
    
    if (string.IsNullOrEmpty(account.ProfilePicture))
        return NotFound("No profile picture available");
    
    // Option A: Redirect to Meta URL (simplest)
    return Redirect(account.ProfilePicture);
    
    // Option B: Proxy through backend (better control, see below)
    // var imageBytes = await metaApiClient.GetImageBytesAsync(account.ProfilePicture, cancellationToken);
    // return File(imageBytes, "image/jpeg");
}
```

#### Step 2 (Better): Proxy through backend

```csharp
[HttpGet("{accountId}/profile-image")]
public async Task<IActionResult> GetProfileImage(Guid accountId, CancellationToken cancellationToken)
{
    var account = await db.InstagramAccounts
        .FirstOrDefaultAsync(x => x.Id == accountId && x.UserId == UserId, cancellationToken);
    
    if (account is null)
        return NotFound();
    
    if (string.IsNullOrEmpty(account.ProfilePicture))
        return NotFound("No profile picture available");
    
    try
    {
        // Proxy the image through backend
        using var response = await httpClient.GetAsync(account.ProfilePicture, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        
        // Add cache header (Meta images don't change frequently)
        return File(imageBytes, contentType, enableRangeProcessing: true);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to fetch profile image for account {AccountId}", accountId);
        return StatusCode(502, "Failed to fetch profile image");
    }
}
```

#### Step 3: Update API documentation

```markdown
## Get Profile Image

**Endpoint**: `GET /api/v1/instagram/{accountId}/profile-image`

**Auth**: Required (JWT)

**Response**: Image stream (JPEG/PNG)

**Example**:
```html
<img src="/api/v1/instagram/{accountId}/profile-image" alt="Profile" />
```

**Error Codes**:
- `404` — Account not found or no profile picture
- `502` — Failed to fetch from Instagram
```

---

## Future: Option 2 (S3 Storage)

### When to implement:
- Performance is critical (images loading slow)
- CDN caching needed
- Want images independent of Meta API

### Migration path:
1. Add `ProfilePictureStorageUrl` to `InstagramAccount` (migration)
2. Create `IImageStorageService` (interface)
3. Implement `S3ImageStorageService` (AWS SDK)
4. During OAuth: store image in S3, save URL
5. During sync: re-upload if changed
6. Endpoint returns S3 URL instead of proxying

---

## Current Meta API Response

### Getting Profile Picture URL

**Endpoint**: `GET /me?fields=profile_picture_url`

**Response**:
```json
{
  "id": "17841405822933335",
  "user_id": "123456789",
  "username": "kromic_test",
  "profile_picture_url": "https://graph.instagram.com/me/picture?height=500&width=500&access_token=IGAA..."
}
```

**Key notes:**
- URL is temporary (expires after ~1 hour)
- Requires valid access token in URL
- Can be redirected with `?redirect=false` to get actual image bytes
- High-resolution available via `?height=` and `?width=` params

---

## Profile Image API Details

### Size/Format Options

```bash
# Default (smallest)
https://graph.instagram.com/me/picture?access_token=...

# With dimensions
https://graph.instagram.com/me/picture?height=500&width=500&access_token=...

# Get image bytes instead of redirect
https://graph.instagram.com/me/picture?redirect=false&access_token=...
```

---

## Data Model

### Current (Just URL)
```csharp
public sealed class InstagramAccount : Entity
{
    public string ProfilePicture { get; set; } = string.Empty; // Meta URL
}
```

### For Option 2 (Add storage)
```csharp
public sealed class InstagramAccount : Entity
{
    public string ProfilePicture { get; set; } = string.Empty;           // Meta URL (keep for refresh)
    public string? ProfilePictureStorageUrl { get; set; }                 // S3/Blob URL
    public DateTime? ProfilePictureLastStoredUtc { get; set; }            // Track storage freshness
    public string? ProfilePictureContentHash { get; set; }                // Detect changes
}
```

---

## Summary Table

| Aspect | Option 1 (Proxy) | Option 2 (Storage) | Option 3 (Hybrid) |
|--------|-----------------|------------------|-------------------|
| **Implementation** | 15 mins | 1-2 hours | 2+ hours |
| **Infrastructure** | None | S3/Blob | S3/Blob |
| **Cost** | Bandwidth only | Storage + bandwidth | Storage + bandwidth |
| **Latency** | High (proxy) | Low (CDN) | Low/High (fallback) |
| **Reliability** | Depends on Meta | Independent | Best |
| **Complexity** | Low | Medium | High |
| **Recommended** | ✅ MVP | ⭐ Production | ❌ Over-engineered |

---

**Next Step**: Pick Option 1 (Proxy) and implement 15-minute endpoint, or go straight to Option 2 if you have S3 infrastructure ready.
