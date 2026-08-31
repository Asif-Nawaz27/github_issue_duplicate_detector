# Security Policy

## Supported versions

There aren't tagged releases yet — everything happens on `main`, and
security fixes go there too. No older version to worry about maintaining in
parallel, at least for now.

## Found a vulnerability? Here's what to do.

**Please don't open a public GitHub issue for it.** That's the one case
where we'd genuinely rather you email us first.

Send the details to **asif.nawaz8787@gmail.com**:

- What the vulnerability is and what it could let someone do
- Steps to reproduce it, if you can put them together
- Any logs, requests, or code pointers that'd help us find it faster

You should hear back within a few days. This is a single-maintainer project
right now, so there's no formal SLA, but security reports jump the queue
ahead of everything else.

If it checks out, we'll fix it and note it in the commit/release that does.
We'll coordinate with you on when and how the details go public — you
won't see us disclose it unilaterally before you've had a chance to weigh
in.

## Where to look, if you're the kind of person who looks

A few spots worth extra scrutiny if you're auditing this project:

- **`POST /api/webhooks/github`** — the one endpoint that's intentionally
  open to the internet without auth, because GitHub has no way to send it a
  bearer token. It's protected entirely by HMAC-SHA256 signature
  verification (`X-Hub-Signature-256`) against `GitHub:WebhookSecret`,
  computed over the raw request body with a constant-time comparison. If
  you find a way around or through that check, that's a high-priority
  report — please send it our way.
- **How credentials are handled** — `GitHub:AccessToken` and
  `GitHub:WebhookSecret` are supposed to live in `dotnet user-secrets` or an
  environment variable, never in a file that gets committed. If you spot a
  code path that logs, echoes, or otherwise leaks either one, we'd want to
  know.
- **The rest of the API has no authentication at all right now** — that's a
  known, documented limitation (it's in the README), not something you need
  to report on its own. But if you find something that goes further than
  "an unauthenticated caller can hit this endpoint" — injection, path
  traversal, SSRF, that sort of thing — that's absolutely in scope.

## Dependencies

Package versions here are pinned, and we've already had to resolve at least
one known high-severity transitive vulnerability (`Microsoft.OpenApi`) by
pinning to a patched version. If a dependency scanner flags something in
this repo, feel free to just open a PR bumping the affected package
(green `dotnet test` included) instead of going through the private-report
process — a version bump on its own isn't sensitive.
