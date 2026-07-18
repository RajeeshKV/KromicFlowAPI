# Coding Standards

## MUST
- Nullable enabled
- File-scoped namespaces
- Async for all I/O
- CancellationToken on async public methods
- One public type per file
- Constructor injection
- DTOs across API boundaries
- UTC timestamps
- XML docs on public APIs

## MUST NOT
- Task.Result / Wait()
- Business logic in endpoints
- Static mutable state
- Magic strings
- Hardcoded secrets

## Checklist
- [ ] Unit tested
- [ ] Validator added
- [ ] Logging included
- [ ] CancellationToken passed
