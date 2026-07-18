# Index Strategy

Unique:
- Users.Email
- Sessions.SessionGuid

Composite:
- Automation(AccountId, Enabled)
- Webhook(Status, ReceivedUtc)

Add indexes before production.
