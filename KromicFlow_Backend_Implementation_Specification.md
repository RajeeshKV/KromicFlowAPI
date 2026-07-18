# Kromic Flow Backend Implementation Specification

> **Purpose**
>
> This document defines **what must be implemented** for the Kromic Flow
> backend.
>
> It intentionally does **not** explain **how** features should be
> implemented. All architecture, coding standards, security,
> infrastructure, and implementation details are documented in the
> `/docs` directory.

------------------------------------------------------------------------

# Mandatory Reading

Before implementing **any feature**, every developer or AI coding
assistant **must** read the relevant documentation.

``` text
docs/
├── 00-Overview
├── 01-Development
├── 02-Architecture
├── 04-Database
├── 05-Security
├── 06-Authentication
├── 07-Meta
├── 08-API
├── 09-Infrastructure
├── 10-Features
└── ADR
```

The documentation under `/docs` is the **single source of truth**.

Do **not** introduce new architectural patterns, folder structures,
coding standards, authentication flows, database conventions, or
infrastructure decisions that conflict with the documentation.

------------------------------------------------------------------------

# Project Objective

Build a production-ready backend for **Kromic Flow**, an Instagram
automation SaaS powered exclusively by **Meta's official APIs**.

The backend must be scalable, secure, testable, maintainable, and
cloud-ready.

------------------------------------------------------------------------

# Functional Modules

## Authentication

-   Meta OAuth Login
-   JWT Authentication
-   Refresh Token Rotation
-   Session Management
-   Logout
-   Logout From All Devices
-   Authorization Policies

Refer to: - `docs/06-Authentication` - `docs/05-Security`

## User Management

-   User Profile
-   User Preferences
-   Active Sessions
-   Session Revocation
-   Account Deletion
-   Email Preferences

## Instagram Integration

-   Connect Instagram Business Account
-   Disconnect Account
-   Reconnect Account
-   Refresh Tokens
-   Synchronize Profile
-   Synchronize Posts
-   Synchronize Reels
-   Synchronize Metadata

Refer to: - `docs/07-Meta`

## Automation Management

-   Create, Update, Delete
-   Enable / Disable
-   Duplicate
-   Keyword Matching
-   Public Replies
-   Private Replies
-   Cooldowns
-   Scheduling
-   Conditional Execution
-   Priority Rules

## Comment Processing Pipeline

Webhook

↓

Persist Event

↓

Validate

↓

Deduplicate

↓

Load Automation Rules

↓

Evaluate Conditions

↓

Execute Automation

↓

Persist Result

↓

Audit Log

↓

Analytics

↓

Notifications

All webhook processing must be idempotent.

## Media Synchronization

-   Initial Sync
-   Manual Sync
-   Scheduled Sync
-   Incremental Sync
-   Retry Failed Sync

## Analytics

-   Comments Processed
-   Messages Sent
-   Successful Executions
-   Failed Executions
-   Active Automations
-   Active Accounts
-   Daily Activity
-   Monthly Activity
-   Processing Latency

## Notifications

-   Email Notifications
-   Token Expiration Alerts
-   Automation Failure Alerts
-   Broadcast Emails
-   Platform Notifications

## Administration

-   User Management
-   Audit Logs
-   Broadcast Management
-   Runtime Settings
-   Feature Flags
-   Platform Metrics
-   Platform Health

------------------------------------------------------------------------

# Background Workers

-   Webhook Processing
-   Media Synchronization
-   Email Delivery
-   Retry Queue
-   Cleanup Jobs
-   Dead Letter Queue
-   Scheduled Tasks

Refer to `docs/09-Infrastructure`.

------------------------------------------------------------------------

# Database

Implement every entity defined in `docs/04-Database`.

------------------------------------------------------------------------

# API

Implement every endpoint documented in `docs/08-API`.

Every endpoint must include: - Validation - Authorization - Logging -
Error Handling - Unit Tests - Integration Tests

------------------------------------------------------------------------

# Security

Follow `docs/05-Security`.

Implement: - JWT Validation - Refresh Token Rotation - Session
Revocation - Encrypted Meta Tokens - Rate Limiting - Webhook Signature
Verification - Structured Audit Logging

------------------------------------------------------------------------

# Infrastructure

Follow `docs/09-Infrastructure`.

Implement: - Docker - Render Deployment - Health Checks -
OpenTelemetry - Serilog - Polly - Background Services

------------------------------------------------------------------------

# Testing Requirements

Every feature must include: - Unit Tests - Integration Tests -
Authorization Tests - Validation Tests - Failure Path Tests

------------------------------------------------------------------------

# Definition of Done

A feature is complete only when: - Requirements implemented -
Documentation followed - Tests pass - Validation complete -
Authorization complete - Logging complete - Audit logging implemented -
API matches documentation - No critical issues remain

------------------------------------------------------------------------

# Engineering Rule

If implementation and documentation differ, **the documentation takes
precedence** until intentionally updated.

Always consult the `/docs` directory before implementing or modifying
functionality.
