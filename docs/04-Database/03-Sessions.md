# Sessions

Tracks every login.

Columns:
- Id
- SessionGuid
- UserId
- RefreshTokenHash
- DeviceName
- Browser
- OS
- IPAddress
- LastSeenUtc
- ExpiresUtc
- RevokedUtc

Indexes:
- SessionGuid UNIQUE
- UserId
