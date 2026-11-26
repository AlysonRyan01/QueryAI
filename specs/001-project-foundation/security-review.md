# Security Review: 001-project-foundation

**Date**: 2026-11-16 · **Scope**: authentication, session, CSRF, forwarded headers, user management,
secrets handling, distribution · **Gate**: constitution "Fluxo de Desenvolvimento e Quality Gates"

## Fixed during review

| # | Finding | Fix |
|---|---|---|
| 1 | **E-mail enumeration via timing**: login skipped the (slow) PBKDF2 verification for unknown/inactive e-mails. | `LoginHandler` verifies against a cached dummy hash on that path, equalizing cost. |
| 2 | **Mandatory password change enforced by an opt-in endpoint filter**: a new endpoint that forgot the filter would be reachable before the change. | Replaced by `PasswordChangedRequirement` in the default, fallback and `AdminOnly` policies; only `me`, `change-password` and `logout` opt out (`AllowPendingPasswordChange`). Covered by a sweep test over all endpoints. |
| 3 | **Session keys lost on restart / not shared across replicas** (Data Protection keys in the container filesystem): every restart logged everybody out, and `--scale api=2` broke sessions. | Keys persisted in the internal database (`data_protection_keys`). |
| 4 | `is_active` had a DB default of `true`, which EF would apply even when `false` was written explicitly. | Default removed. |

## Verified controls

- **Passwords**: PBKDF2-HMAC-SHA512 (ASP.NET Identity V3) with salt, transparent rehash; length 12–128 (upper bound prevents hashing DoS).
- **Brute force**: per-account lockout (5 failures → 15 min) and per-IP rate limit on login (10/min, configurable).
- **Session cookie**: `HttpOnly`, `SameSite=Strict`, `Secure` outside dev/evaluation mode, 8 h sliding idle expiry; re-validated against the DB on every request (active + security stamp), so deactivation, role, password change and reset revoke sessions on the next request.
- **CSRF**: `SameSite=Strict` + mandatory `X-Requested-With: QueryAI` on state-changing `/api` calls; no CORS enabled.
- **Authorization**: fallback policy requires authentication; admin endpoints require role `Admin` on the server (UI hiding is cosmetic only); sweep tests cover anonymous (401), analyst (403) and pending-password (403) access.
- **Last admin invariant**: demotion/deactivation lock all active admins (`FOR UPDATE`) inside a transaction; concurrent mutual deactivation is serialized.
- **Forwarded headers**: only trusted networks (`QUERYAI_TRUSTED_PROXIES`, loopback) may set client IP / scheme.
- **Errors**: RFC 9457 with `traceId`; unhandled exceptions never expose messages or stack traces.
- **Logs**: structured JSON; destructuring policy masks password/hash/token/secret/connection-string properties; a test scans logs and responses for every password, hash and DB secret used.
- **Secrets**: only from `.env`; startup fails on empty or `change-me` values; compose uses `${VAR:?}`; no defaults baked into images; `.env` git-ignored.
- **Containers**: API and web run as non-root; only `web` publishes a port; security headers and CSP (`default-src 'self'`, `frame-ancestors 'none'`) on the web container.
- **Open redirect**: login `redirect` parameter accepts only same-origin relative paths.

## Accepted risks / follow-ups

| Risk | Rationale / follow-up |
|---|---|
| `423 account_locked` reveals that an account exists once locked | Accepted in research R7 (better support UX; unknown e-mails never lock). |
| Logout does not revoke a stolen copy of the cookie (stateless ticket) | Exposure bounded by HttpOnly/Secure/SameSite and idle expiry. Follow-up: server-side session store or stamp rotation on "log out everywhere". |
| `change-password` is not rate limited | Requires an authenticated session; low value to an attacker. Revisit with a generic per-user limiter. |
| Data Protection key XML stored unencrypted in the internal DB | DB is internal and not exposed. Follow-up when `QUERYAI_ENCRYPTION_KEY` is introduced (connections feature): protect keys at rest. |
| Direct access to the web port can spoof `X-Forwarded-Proto` | Documented: the port must be reachable only by the HTTPS proxy (firewall). The Secure cookie is still not returned over plain HTTP. |
| Integration tests and the container build not executed in this environment | No Docker access while implementing; must run before merge (see quickstart). |
