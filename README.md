# LoginLab

A hands-on authentication and identity management lab built with **ASP.NET Core MVC** and **PostgreSQL**. The goal of this project is to implement, from scratch and without an all-in-one authentication framework (such as ASP.NET Core Identity, Auth0, or Firebase Auth), the security mechanisms that are usually hidden behind those libraries: password hashing, session management with real revocation, email verification, two-factor authentication, rate limiting, and more.

> This is a learning project. Technical decisions are documented and justified on purpose, so that every piece is a starting point for understanding **why** it's done that way, not just **how**.

## Tech stack

- **Backend**: ASP.NET Core MVC (.NET 8)
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **Password hashing**: Argon2id
- **TOTP secret encryption**: AES-256-GCM
- **Validation**: FluentValidation
- **Rate limiting**: `Microsoft.AspNetCore.RateLimiting` (built-in .NET middleware)
- **QR code generation**: QRCoder
- **Email (development)**: Mailhog (fake SMTP server)
- **Containers**: Docker Compose (PostgreSQL + Mailhog)

## Architecture

The project follows a layered architecture (inspired by Clean Architecture), designed to keep business logic isolated from infrastructure details such as EF Core, PostgreSQL, or the MVC framework itself:

```
LogInLab.sln
│
├── LogInLab              → Presentation: Controllers, Views, ViewModels (MVC)
├── LogInLab.Application       → Business logic: Services, DTOs, Interfaces, Validators
├── LogInLab.Domain            → Pure domain entities, no external dependencies
└── LogInLab.Infrastructure    → EF Core, repositories, hashing, encryption, email sending
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

### ✅ Phase 3 — Password recovery
- Forgot-password flow with single-use, hashed reset tokens (15-minute expiration, shorter than email verification given the higher sensitivity)
- Requesting a new reset token invalidates any previously issued, still-valid token for that user
- Resetting the password **revokes all active sessions** for the account — closes the door on any attacker who may have had an active session with the old, potentially compromised password
- Uniform responses on the request step (no confirmation of whether an email exists in the system)

### ✅ Phase 4 — Two-factor authentication (TOTP + backup codes)
- Custom TOTP algorithm implementation (RFC 6238 / RFC 4226): HMAC-SHA1, dynamic truncation, 30-second time step, ±1 step tolerance window for clock drift
- Custom Base32 encoder/decoder (required by the `otpauth://` standard)
- QR-based setup flow: secret shown as a scannable QR code (via QRCoder), activation confirmed only after the user enters a valid code from their authenticator app
- TOTP secret encrypted at rest with **AES-256-GCM** (reversible encryption — the server must recompute codes, unlike passwords which use one-way hashing)
- 10 single-use backup codes generated on activation, shown exactly once, hashed (SHA-256) before storage
- Two-step login flow for MFA-enabled accounts: pending identity held in `TempData` between credential validation and code verification, no session created until the second factor is confirmed
- MFA can be disabled from the user profile, removing the secret and all backup codes

### ✅ Phase 5 — Hardening
- **Account lockout**: 5 failed login attempts trigger a 15-minute temporary lock, tracked per account (`FailedLoginAttempts`, `LockedUntil`)
- **Rate limiting** (`Microsoft.AspNetCore.RateLimiting`): a global per-IP limiter for the whole app, plus stricter named policies (`AuthStrict`, `AuthModerate`) applied to login, MFA verification, registration, and password/email resend endpoints
- **Security event logging**: an `auth_events` table records login successes/failures (with reason), registrations, password resets, MFA activation/deactivation, account lockouts, and logouts — including IP address and user agent
- **Active sessions panel**: users can see all their active sessions (IP, device, creation date), identify the current one, revoke individual sessions remotely, or close all other sessions at once

### 🔜 Upcoming (optional)
- **Phase 6**: magic links (passwordless login), WebAuthn/Passkeys, Row Level Security in PostgreSQL

## Security principles applied

Several principles have been consistently applied throughout the project, documented here for quick reference:

- **Never store passwords in plain text or with reversible encryption** — always hashed with Argon2id
- **Never store single-use tokens or backup codes in plain text** — always hashed with SHA-256 before saving
- **Reversible encryption only when strictly necessary** (the TOTP secret), and even then with an authenticated cipher (AES-GCM) and a key kept out of source control
- **Generic error messages** on login, registration, and resend/reset flows, to prevent user enumeration — except where the user has already proven ownership of the account (e.g. account lockout messages, MFA prompts), where specific feedback no longer leaks anything new
- **Constant-time comparisons** (`CryptographicOperations.FixedTimeEquals`) when verifying hashes and codes, to mitigate timing attacks
- **Fail open** on non-critical external service failures (Have I Been Pwned): registration availability doesn't depend on a third party
- **Defense in depth**: validation is duplicated on the client (UX) and server (actual source of truth); account lockout and IP-based rate limiting work as complementary, overlapping layers
- **Layer separation**: business logic has no knowledge of HTTP details, EF Core, or specific external providers
- **Sessions are revoked, not just cookies cleared**, on logout, password reset, and manual session management — closing the gap that a purely self-contained cookie or stateless JWT would leave open

## Known limitations

- The MFA and reset token encryption keys currently live in `appsettings.json` for local development convenience. In a real deployment these must live in a proper secrets manager (Azure Key Vault, AWS Secrets Manager, environment variables injected by the orchestrator).
- Rate limiting is IP-based, which has known weaknesses: shared IPs (offices, NAT, universities) can be affected by another user's failed attempts, and a determined attacker can rotate IPs. This is mitigated, not eliminated, by combining it with per-account lockout.
- No CAPTCHA or equivalent challenge is implemented after repeated failures — a reasonable next step for a production-facing system.

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
   From the Package Manager Console in Visual Studio (default project: `LogInLab.Infrastructure`):
   ```powershell
   Update-Database -Project LogInLab.Infrastructure -StartupProject LogInLab
   ```

3. Run the web project (`LogInLab`) with F5 or:
   ```bash
   dotnet run --project LogInLab
   ```

4. Open `http://localhost:8025` to see the emails captured by Mailhog during testing (registration, email verification, password reset, etc.)

## Database structure (so far)

| Table | Purpose |
|---|---|
| `users` | Registered users, hashed credentials, verification status, lockout state, MFA flag |
| `sessions` | Active/revoked sessions, linked to the authentication cookie |
| `email_verification_tokens` | Single-use tokens for email verification, stored hashed |
| `password_reset_tokens` | Single-use tokens for password recovery, stored hashed |
| `mfa_secrets` | Encrypted TOTP secrets per user |
| `backup_codes` | Hashed single-use MFA recovery codes |
| `auth_events` | Security event log (logins, lockouts, password resets, MFA changes, etc.) |

## Notable design decisions

- **Cookies vs JWT**: server-side cookies with database validation on every request were chosen, prioritizing instant revocation over the raw performance of a stateless scheme. The cost (one DB query per authenticated request) is acceptable at the scale of this lab.
- **No forced password complexity**: a deliberate decision aligned with current NIST guidance, prioritizing length and breach-checking over rules like "at least one uppercase letter, one symbol, etc."
- **Result Pattern instead of exceptions for business flows**: operations like registration or login return result objects (`AuthResult`, `LoginResult`) instead of throwing exceptions for expected cases (wrong credentials, duplicate email). Exceptions are reserved for truly exceptional errors.
- **Hashing vs encryption, applied deliberately**: passwords, verification tokens, reset tokens, and backup codes are all one-way hashed, since the raw value never needs to be recovered. The TOTP secret is the one exception — it's encrypted (reversible) because the server must recompute codes from it on every validation.
- **Two layers of abuse protection**: per-account lockout and per-IP rate limiting address different attack shapes (one attacker hammering one account vs. one attacker spraying many accounts) and are intentionally kept as separate, overlapping mechanisms rather than merged into one.

## License

Personal project for educational purposes.
