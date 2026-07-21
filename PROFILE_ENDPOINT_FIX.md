# Profile Endpoint Fix — Quick Reference

> **Fix for 404 error on /api/v1/users/profile**

---

## ❌ The Problem

Frontend was calling:
```
GET https://flowapi.kromic.in/api/v1/users/profile  ❌ 404 NOT FOUND
```

But endpoint didn't exist!

---

## ✅ The Solution

**Endpoint now exists**:
```
GET https://flowapi.kromic.in/api/v1/user/profile  ✅ 200 OK
```

**Note**: Singular `/user/profile` not plural `/users/profile`

---

## 📦 What Was Added

### Backend Changes
1. ✅ Created `GetUserProfileQuery` handler
2. ✅ Created `UserController.GetProfile()` endpoint  
3. ✅ Added `emailVerified` field to `UserProfileDto`
4. ✅ Build: **0 errors** | Tests: **2/2 passing**

### Files Modified
- `src/KromicFlow.Api/Controllers/UserController.cs` — Added GET /user/profile endpoint
- `src/KromicFlow.Application/DTOs/UserProfileDto.cs` — Added emailVerified field
- `src/KromicFlow.Application/Features/Auth/MetaCallback/MetaCallbackCommandHandler.cs` — Updated DTO instantiation
- Created: `src/KromicFlow.Application/Features/User/GetUserProfile/GetUserProfileQuery.cs`
- Created: `src/KromicFlow.Application/Features/User/GetUserProfile/GetUserProfileQueryHandler.cs`

---

## 🔧 Frontend Fix Required

Change this:
```javascript
// ❌ OLD (404 Not Found)
fetch('https://flowapi.kromic.in/api/v1/users/profile', {
  headers: { 'Authorization': `Bearer ${token}` }
})
```

To this:
```javascript
// ✅ NEW (200 OK)
fetch('https://flowapi.kromic.in/api/v1/user/profile', {
  headers: { 'Authorization': `Bearer ${token}` }
})
```

---

## 📋 Response Format

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

## ✨ New Fields

- ✅ `emailVerified` — Check this to gate automation creation

**Use it**:
```javascript
const profile = await response.json();

if (!profile.emailVerified) {
  showToast('Please verify your email before creating automations');
  return; // Block automation
}
```

---

## 🎯 Implementation Steps

1. **Update endpoint URL** in your frontend code
   - Change from: `/api/v1/users/profile`
   - Change to: `/api/v1/user/profile`

2. **Check the `emailVerified` field** in response
   ```javascript
   if (!profile.emailVerified) {
     // Show verification prompt
     // Block automation creation
   }
   ```

3. **Handle errors**
   ```javascript
   if (response.status === 401) {
     // Token expired, redirect to login
   }
   ```

---

## ✅ Testing

Test with curl:
```bash
curl -H "Authorization: Bearer <YOUR_TOKEN>" \
  https://flowapi.kromic.in/api/v1/user/profile
```

Should return:
```json
{
  "id": "...",
  "emailVerified": true
}
```

---

## 📊 Status

| Item | Status |
|------|--------|
| Endpoint created | ✅ |
| emailVerified field added | ✅ |
| Build passing | ✅ |
| Tests passing | ✅ |
| Ready for production | ✅ |

---

## 🚀 Deploy

1. Pull latest code
2. No database migrations needed
3. Build: `dotnet build`
4. Tests: `dotnet test`
5. Deploy as usual

---

**Done!** Frontend can now use `/api/v1/user/profile` to check email verification status.

For detailed integration guide, see: `FRONTEND_PROFILE_ENDPOINT_GUIDE.md`
