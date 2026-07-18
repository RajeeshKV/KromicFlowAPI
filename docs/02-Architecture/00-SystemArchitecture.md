# System Architecture

Presentation
↓
Application (CQRS)
↓
Domain
↓
Infrastructure

Dependency rule:
Presentation -> Application -> Domain
Infrastructure depends on Domain/Application only through interfaces.
