# Frontend Integration Guide - Instagram Media & Automation Scopes

## Overview

This guide covers the new Instagram media management and automation scope features. Users can now view their Instagram posts, select specific posts for automation, and define automation scopes (Specific Posts, Existing Posts, Future Posts, All Posts).

## Key Concepts

### Automation Scopes

- **SpecificPosts** (Default): Automation applies only to selected posts
- **ExistingPosts**: Automation applies to all posts created before the automation
- **FuturePosts**: Automation applies to all posts created after the automation
- **AllPosts**: Automation applies to all posts (existing + future)

### Media Types

- **Image**: Regular photo posts
- **Video**: Video posts
- **Carousel**: Multi-image carousel posts
- **Reel**: Instagram Reels

---

## API Endpoints

### 1. List Instagram Media

**Endpoint:** `GET /api/v1/instagram/{accountId}/media`

**Description:** Retrieve paginated list of Instagram media for an account.

**Query Parameters:**
- `page` (int, optional): Page number (default: 1)
- `pageSize` (int, optional): Items per page (default: 20)
- `mediaType` (string, optional): Filter by media type (Image, Video, Carousel, Reel)
- `search` (string, optional): Search by caption text

**Response:**
```json
{
  "items": [
    {
      "id": "guid",
      "instagramMediaId": "instagram-media-id",
      "mediaType": 0,
      "caption": "Post caption text",
      "thumbnailUrl": "https://cdn.instagram.com/...",
      "mediaUrl": "https://cdn.instagram.com/...",
      "permalink": "https://instagram.com/p/...",
      "postedAtUtc": "2024-01-15T10:30:00Z",
      "likeCount": 150,
      "commentsCount": 25,
      "lastSyncedAtUtc": "2024-01-20T08:00:00Z"
    }
  ],
  "total": 120
}
```

**Example Usage:**
```javascript
// Get first page of media
const response = await fetch('/api/v1/instagram/{accountId}/media?page=1&pageSize=20');

// Filter by media type
const response = await fetch('/api/v1/instagram/{accountId}/media?mediaType=Image');

// Search by caption
const response = await fetch('/api/v1/instagram/{accountId}/media?search=summer');
```

---

### 2. Sync Instagram Account (with Media)

**Endpoint:** `POST /api/v1/instagram/{accountId}/sync`

**Description:** Sync Instagram account and fetch latest media from Instagram API.

**Response:** Success/Failure result

**Example Usage:**
```javascript
const response = await fetch(`/api/v1/instagram/${accountId}/sync`, {
  method: 'POST'
});
```

**Note:** This endpoint now syncs media along with account profile. Call this before displaying media to ensure data is fresh.

---

### 3. Create Automation with Scope

**Endpoint:** `POST /api/v1/automations`

**Description:** Create a new automation with specified scope and media selection.

**Request Body:**
```json
{
  "instagramAccountId": "guid",
  "name": "Reply to comments",
  "scope": 0,
  "triggerType": "CommentKeyword",
  "keywords": ["hello", "hi", "hey"],
  "publicReply": "Thanks for commenting!",
  "privateReply": null,
  "cooldownSeconds": 60,
  "priority": 1,
  "selectedMediaIds": ["guid-1", "guid-2", "guid-3"]
}
```

**Scope Values:**
- `0` = SpecificPosts
- `1` = ExistingPosts
- `2` = FuturePosts
- `3` = AllPosts

**Validation Rules:**
- **SpecificPosts**: Must have at least 1 media in `selectedMediaIds`
- **ExistingPosts**: `selectedMediaIds` must be empty
- **FuturePosts**: `selectedMediaIds` must be empty
- **AllPosts**: `selectedMediaIds` must be empty

**Example Usage:**
```javascript
const automation = {
  instagramAccountId: accountId,
  name: "Reply to summer posts",
  scope: 0, // SpecificPosts
  triggerType: "CommentKeyword",
  keywords: ["summer", "vacation"],
  publicReply: "Thanks for your comment!",
  cooldownSeconds: 60,
  priority: 1,
  selectedMediaIds: selectedMedia.map(m => m.id)
};

const response = await fetch('/api/v1/automations', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(automation)
});
```

---

### 4. Update Automation

**Endpoint:** `PUT /api/v1/automations/{id}`

**Description:** Update existing automation with new scope and media selection.

**Request Body:** Same as Create Automation

**Example Usage:**
```javascript
const response = await fetch(`/api/v1/automations/${automationId}`, {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(updatedAutomation)
});
```

---

### 5. List Automations

**Endpoint:** `GET /api/v1/automations`

**Description:** Get all automations for current user with scope and selected media.

**Response:**
```json
[
  {
    "id": "guid",
    "instagramAccountId": "guid",
    "name": "Reply to comments",
    "scope": 0,
    "triggerType": "CommentKeyword",
    "keywords": ["hello", "hi"],
    "publicReply": "Thanks!",
    "privateReply": null,
    "enabled": true,
    "cooldownSeconds": 60,
    "priority": 1,
    "selectedMedia": [
      {
        "id": "guid",
        "instagramMediaId": "ig-media-id",
        "caption": "Post caption",
        "thumbnailUrl": "https://...",
        "postedAtUtc": "2024-01-15T10:30:00Z"
      }
    ]
  }
]
```

---

## Frontend Implementation Guide

### Step 1: Display Instagram Media

```javascript
// Fetch media for an account
async function loadInstagramMedia(accountId, page = 1, filters = {}) {
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: '20',
    ...filters
  });
  
  const response = await fetch(`/api/v1/instagram/${accountId}/media?${params}`);
  const data = await response.json();
  
  return data;
}

// Display media in a grid
function renderMediaGrid(mediaItems) {
  return mediaItems.map(media => `
    <div class="media-item" data-id="${media.id}">
      <img src="${media.thumbnailUrl}" alt="${media.caption}" />
      <div class="media-info">
        <p>${media.caption}</p>
        <span>${new Date(media.postedAtUtc).toLocaleDateString()}</span>
        <span>❤️ ${media.likeCount} 💬 ${media.commentsCount}</span>
      </div>
    </div>
  `).join('');
}
```

### Step 2: Media Selection UI

```javascript
// Handle media selection for automation
let selectedMediaIds = new Set();

function toggleMediaSelection(mediaId) {
  if (selectedMediaIds.has(mediaId)) {
    selectedMediaIds.delete(mediaId);
  } else {
    selectedMediaIds.add(mediaId);
  }
  updateSelectionUI();
}

function updateSelectionUI() {
  // Update visual selection state
  document.querySelectorAll('.media-item').forEach(item => {
    const id = item.dataset.id;
    item.classList.toggle('selected', selectedMediaIds.has(id));
  });
  
  // Update count display
  document.getElementById('selected-count').textContent = selectedMediaIds.size;
}
```

### Step 3: Automation Scope Selection

```javascript
// Scope selection dropdown
const scopeOptions = [
  { value: 0, label: 'Specific Posts', description: 'Apply to selected posts only' },
  { value: 1, label: 'Existing Posts', description: 'Apply to posts created before automation' },
  { value: 2, label: 'Future Posts', description: 'Apply to posts created after automation' },
  { value: 3, label: 'All Posts', description: 'Apply to all posts (existing + future)' }
];

function renderScopeSelector() {
  return scopeOptions.map(scope => `
    <div class="scope-option" data-value="${scope.value}">
      <input type="radio" name="scope" value="${scope.value}" id="scope-${scope.value}" />
      <label for="scope-${scope.value}">
        <strong>${scope.label}</strong>
        <p>${scope.description}</p>
      </label>
    </div>
  `).join('');
}

// Handle scope change
function onScopeChange(scopeValue) {
  const mediaSelector = document.getElementById('media-selector');
  
  // Show/hide media selector based on scope
  if (scopeValue === 0) { // SpecificPosts
    mediaSelector.style.display = 'block';
  } else {
    mediaSelector.style.display = 'none';
    selectedMediaIds.clear();
    updateSelectionUI();
  }
}
```

### Step 4: Create Automation with Scope

```javascript
async function createAutomation(formData) {
  const payload = {
    instagramAccountId: formData.accountId,
    name: formData.name,
    scope: parseInt(formData.scope),
    triggerType: formData.triggerType,
    keywords: formData.keywords.split(',').map(k => k.trim()),
    publicReply: formData.publicReply,
    privateReply: formData.privateReply,
    cooldownSeconds: parseInt(formData.cooldownSeconds),
    priority: parseInt(formData.priority),
    selectedMediaIds: Array.from(selectedMediaIds)
  };
  
  const response = await fetch('/api/v1/automations', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || 'Failed to create automation');
  }
  
  return await response.json();
}
```

### Step 5: Complete Workflow Example

```javascript
// Complete automation creation workflow
async function createAutomationWorkflow(accountId) {
  try {
    // Step 1: Sync account to get fresh media
    await fetch(`/api/v1/instagram/${accountId}/sync`, { method: 'POST' });
    
    // Step 2: Load media
    const mediaData = await loadInstagramMedia(accountId);
    
    // Step 3: Display media for selection
    renderMediaGrid(mediaData.items);
    
    // Step 4: User selects scope and media
    // (UI interaction handled by event listeners)
    
    // Step 5: Create automation
    const result = await createAutomation({
      accountId,
      name: 'My Automation',
      scope: selectedScope,
      triggerType: 'CommentKeyword',
      keywords: ['hello', 'hi'],
      publicReply: 'Thanks!',
      cooldownSeconds: 60,
      priority: 1
    });
    
    // Step 6: Show success
    alert('Automation created successfully!');
    
  } catch (error) {
    console.error('Error:', error);
    alert('Failed to create automation: ' + error.message);
  }
}
```

---

## Best Practices

### 1. Sync Before Displaying Media

Always sync the account before displaying media to ensure data is fresh:

```javascript
async function ensureMediaSynced(accountId) {
  await fetch(`/api/v1/instagram/${accountId}/sync`, { method: 'POST' });
}
```

### 2. Handle Pagination

Implement pagination for accounts with many posts:

```javascript
async function loadAllMedia(accountId) {
  let allMedia = [];
  let page = 1;
  let hasMore = true;
  
  while (hasMore) {
    const data = await loadInstagramMedia(accountId, page);
    allMedia = [...allMedia, ...data.items];
    hasMore = allMedia.length < data.total;
    page++;
  }
  
  return allMedia;
}
```

### 3. Validate Scope Selection

Validate scope and media selection before submission:

```javascript
function validateAutomationForm(formData) {
  if (formData.scope === 0 && selectedMediaIds.size === 0) {
    throw new Error('Please select at least one post for Specific Posts scope');
  }
  
  if (formData.scope !== 0 && selectedMediaIds.size > 0) {
    throw new Error('Media selection is only available for Specific Posts scope');
  }
  
  if (formData.triggerType === 'CommentKeyword' && formData.keywords.length === 0) {
    throw new Error('Please add at least one keyword for keyword trigger');
  }
  
  return true;
}
```

### 4. Display Media Thumbnails Efficiently

Use lazy loading for media thumbnails:

```html
<img 
  src="${media.thumbnailUrl}" 
  alt="${media.caption}" 
  loading="lazy"
  class="media-thumbnail"
/>
```

### 5. Show Media Type Indicators

Display media type icons for better UX:

```javascript
function getMediaTypeIcon(mediaType) {
  const icons = {
    0: '📷', // Image
    1: '🎥', // Video
    2: '🎠', // Carousel
    3: '🎬'  // Reel
  };
  return icons[mediaType] || '📷';
}
```

---

## Error Handling

### Common Errors

1. **Invalid Scope Configuration**
   - Error: "Invalid automation scope configuration"
   - Cause: Media selection doesn't match scope
   - Solution: Ensure media selection matches scope rules

2. **Media Not Found**
   - Error: "Instagram account not found"
   - Cause: Account ID is invalid or user doesn't own account
   - Solution: Verify account ID and user permissions

3. **Plan Limit Reached**
   - Error: "Plan automation limit reached"
   - Cause: User has exceeded their plan's automation limit
   - Solution: Prompt user to upgrade plan or delete existing automations

---

## Testing Checklist

- [ ] Media loads correctly for connected accounts
- [ ] Pagination works for accounts with many posts
- [ ] Media type filtering works
- [ ] Search by caption works
- [ ] Media selection UI updates correctly
- [ ] Scope selection shows/hides media selector
- [ ] Validation prevents invalid scope/media combinations
- [ ] Automation creation succeeds with valid data
- [ ] Automation update preserves existing media mappings
- [ ] Automation list displays selected media correctly
- [ ] Sync endpoint refreshes media data

---

## Future Enhancements

The system is designed to support future features:

- **Reels Support**: Media type enum already includes Reels
- **Stories Support**: Can be added to MediaType enum without schema changes
- **AI Automations**: Scope system supports AI-based automation selection
- **Multiple Account Selection**: Architecture supports multi-account automations
- **Analytics**: Media-level execution tracking can be added

---

## Support

For questions or issues, refer to the main API documentation or contact the development team.
