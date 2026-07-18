# Users

Columns
- Id (UUID)
- Email
- FullName
- Role
- TokenVersion
- IsActive
- CreatedUtc
- UpdatedUtc

Indexes
- Email UNIQUE

Rules
- Email immutable after verification.
- TokenVersion increments on logout-all.
