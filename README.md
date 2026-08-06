# LoginLab

A hands-on authentication and identity management lab built with **ASP.NET Core MVC** and **PostgreSQL**. The goal of this project is to implement, from scratch and without an all-in-one authentication framework (such as ASP.NET Core Identity, Auth0, or Firebase Auth), the security mechanisms that are usually hidden behind those libraries: password hashing, session management with real revocation, email verification, second-factor authentication, and more.

> This is a learning project. Technical decisions are documented and justified on purpose, so that every piece is a starting point for understanding **why** it's done that way, not just **how**.

## Tech stack

- **Backend**: ASP.NET Core MVC (.NET 8)
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **Password hashing**: Argon2id
- **Validation**: FluentValidation
- **Email (development)**: Mailhog (fake SMTP server)
- **Containers**: Docker Compose (PostgreSQL + Mailhog)

## Architecture

The project follows a layered architecture (inspired by Clean Architecture), designed to keep business logic isolated from infrastructure details such as EF Core, PostgreSQL, or the MVC framework itself:

```
LoginLab.sln
│
├── LoginLab.Web              → Presentation: Controllers, Views, ViewModels (MVC)
├── LoginLab.Application       → Business logic: Services, DTOs, Interfaces, Validators
├── LoginLab.Domain            → Pure domain entities, no external dependencies
└── LoginLab.Infrastructure    → EF Core, repositories, hashing, email sending
```

**Dependency rule**: `Domain` depends on nothing. `Application` only depends on `Domain`. `Infrastructure` implements the interfaces defined in `Application`. `Web` orchestrates, but contains no business logic.

This separation makes it possible, for example, to test all authentication logic without spinning up an HTTP server or a real database, and to swap out pieces (like the email provider or the database engine) without touching the rest of the system.

## Implemented features

### ✅ Phase 1 — Basic registration and login
- User registration with password hashing via **Argon2id** (random salt, configurable cost parameters stored alongside the hash)
- Login with credential verification and generic error messages (user enumeration mitigation)
- **Hybrid** session management: ASP.NET Core authentication cookies + a `sessions` table in PostgreSQL, validated on every request via the `OnValidatePrincipal` event. This enables **real, instant session revocation** (unlike a self-contained cookie or a JWT without a blacklist)
- `[Authorize]` middleware protecting private routes
- Logout that revokes the session in the database, not just the browser cookie
- Specific handling of race conditions during registration (PostgreSQL unique constraint violation `23505`, translated into a dedicated domain exception)

### ✅ Phase 2 — Password policies and email verification
- Password validation aligned with **NIST 800-63B**: minimum length (12 characters) instead of forced complexity rules, which are discouraged for leading to predictable patterns
- Breached password check against the **Have I Been Pwned** API, using the **k-anonymity** protocol (only a 5-character prefix of the SHA-1 hash is sent, never the password or the full hash)
- Email verification flow with single-use tokens: generated with `RandomNumberGenerator` (256 bits of entropy), stored **hashed** (SHA-256) in the database, with a 30-minute expiration
- Login blocked for accounts with an unverified email
- Resend verification email with a cooldown (2 minutes) and uniform responses that don't reveal whether an account exists, is already verified, or is in cooldown

### 🔜 Upcoming phases
- **Phase 3**: password recovery
- **Phase 4**: two-factor authentication (TOTP + backup codes)
- **Phase 5**: rate limiting, account lockout, security event logging, active sessions panel
- **Phase 6** (optional): magic links, WebAuthn/Passkeys, Row Level Security in PostgreSQL

## Security principles applied

Several principles have been consistently applied throughout the project, documented here for quick reference:

- **Never store passwords in plain text or with reversible encryption** — always hashed with Argon2id
- **Never store tokens (verification, reset) in plain text in the database** — always hashed with SHA-256 before saving
- **Generic error messages** on login, registration, and resend verification, to prevent user enumeration
- **Constant-time comparisons** (`CryptographicOperations.FixedTimeEquals`) when verifying hashes, to mitigate timing attacks
- **Fail open** on non-critical external service failures (Have I Been Pwned): registration availability doesn't depend on a third party
- **Defense in depth**: validation is duplicated on the client (UX) and server (actual source of truth), never client-side only
- **Layer separation**: business logic has no knowledge of HTTP details, EF Core, or specific external providers

## Prerequisites

- .NET 8 SDK
- Docker Desktop (for PostgreSQL and Mailhog)
- Visual Studio 2022 (or your preferred IDE)

## Getting started

1. Start the infrastructure services:
   ```bash
   docker compose up -d
   ```
   This starts PostgreSQL (port `5432`) and Mailhog (SMTP on `1025`, web UI on `8025`).

2. Restore dependencies and apply migrations:
   ```bash
   dotnet restore
   ```
   From the Package Manager Console in Visual Studio (default project: `LoginLab.Infrastructure`):
   ```powershell
   Update-Database -Project LoginLab.Infrastructure -StartupProject LoginLab.Web
   ```

3. Run the web project (`LoginLab.Web`) with F5 or:
   ```bash
   dotnet run --project LoginLab.Web
   ```

4. Open `http://localhost:8025` to see the emails captured by Mailhog during testing (registration, email verification, etc.)

## Database structure (so far)

| Table | Purpose |
|---|---|
| `users` | Registered users, hashed credentials, verification status |
| `sessions` | Active/revoked sessions, linked to the authentication cookie |
| `email_verification_tokens` | Single-use tokens for email verification, stored hashed |

## Notable design decisions

- **Cookies vs JWT**: server-side cookies with database validation on every request were chosen, prioritizing instant revocation over the raw performance of a stateless scheme. The cost (one DB query per authenticated request) is acceptable at the scale of this lab.
- **No forced password complexity**: a deliberate decision aligned with current NIST guidance, prioritizing length and breach-checking over rules like "at least one uppercase letter, one symbol, etc."
- **Result Pattern instead of exceptions for business flows**: operations like registration or login return result objects (`AuthResult`, `LoginResult`) instead of throwing exceptions for expected cases (wrong credentials, duplicate email). Exceptions are reserved for truly exceptional errors.

## License

Personal project for educational purposes.
