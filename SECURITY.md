# Security Policy

## Supported versions

IssueSense doesn't have tagged releases yet — it's developed on `main`.
Security fixes are applied to `main`; there's no older version being
maintained in parallel.

## Reporting a vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

Instead, report it privately by emailing **asif.nawaz8787@gmail.com** with:

- A description of the vulnerability and its potential impact
- Steps to reproduce it, if possible
- Any relevant logs, requests, or code pointers

You should get an acknowledgment within a few days. There's no formal SLA
(this is a single-maintainer project at this stage), but security reports
are prioritized over other work.

If the report is confirmed, a fix will be prepared and a note added to the
release/commit that fixes it. Public disclosure of the vulnerability
details will be coordinated with you rather than published unilaterally.

## Scope and known-sensitive areas

Things particularly worth a careful look if you're reviewing this project
for security issues:

- **`POST /api/webhooks/github`** — the only unauthenticated public
  endpoint by design (GitHub can't send a bearer token). It relies entirely
  on HMAC-SHA256 signature verification (`X-Hub-Signature-256`) against
  `GitHub:WebhookSecret`, computed over the raw request body using a
  constant-time comparison. If you find a way to bypass or weaken that
  check, that's a high-priority report.
- **Credential handling** — `GitHub:AccessToken` and `GitHub:WebhookSecret`
  are meant to live in `dotnet user-secrets` or environment variables, never
  in a committed file. If you spot a code path that logs, echoes, or
  otherwise leaks either value, please report it.
- **The rest of the API currently has no authentication** — this is a known,
  documented limitation (see the README), not something to separately
  report, but exploits that go beyond "an unauthenticated caller can call
  this endpoint" (e.g. injection, path traversal, SSRF) are in scope.

## Dependencies

This project pins package versions and has previously resolved a known
high-severity transitive vulnerability (`Microsoft.OpenApi`) by pinning to a
patched version. If a dependency scan flags something, a PR bumping the
affected package (with `dotnet test` passing) is welcome alongside or
instead of a private report — dependency version bumps aren't sensitive.
