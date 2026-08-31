# IssueSense Web

A small React + TypeScript dashboard for exercising the IssueSense API by
hand: manage owners, pick a repository, import its issues, generate
embeddings, and check a candidate issue for duplicates — with every action
you run logged in an activity feed on the page.

This is a developer tool, not a production admin console: it has no
authentication, doesn't persist anything server-side beyond what the API
itself stores, and the activity feed only exists in the browser tab's
memory (refresh and it's gone).

## Running it

The API must be running first, on its HTTPS profile
(`dotnet run --project App/Api --launch-profile https` from the repo root —
see [docs/DEVELOPMENT.md](../../docs/DEVELOPMENT.md)). Then:

```bash
npm install
npm run dev
```

Opens on `http://localhost:5173` by default (or the next free port). Requests
to `/api/*` are proxied to `https://localhost:7094` (see `vite.config.ts`),
so no CORS configuration is needed for local development — if you change
which port/profile the API runs on, update the proxy target there to match.

## What it does

- **Owner** — a searchable dropdown of owners already known to IssueSense
  (`GET /api/owners`), with an **+ Add owner** button that opens a small
  form (`POST /api/owners`) and selects the new owner once it's created.
- **Repository** — a dropdown scoped to whichever owner is selected
  (`GET /api/repositories/{owner}`), also searchable, but unlike Owner it
  accepts free text too — you need to be able to type a repository that
  hasn't been imported yet before you can import it.
- **Import issues** — `POST /api/repositories/{owner}/{repository}/import`
- **Generate embeddings** — `POST /api/repositories/{owner}/{repository}/generate-embeddings`
- **Check duplicate** — `POST /api/repositories/{owner}/{repository}/check-duplicate`, given a title and optional body

Every action you run (success or failure) gets appended to the Activity
panel with a timestamp, duration, and a one-line summary of the result.

## Scripts

| Command | Does |
|---|---|
| `npm run dev` | Start the Vite dev server |
| `npm run build` | Type-check and build for production into `dist/` |
| `npm run preview` | Preview the production build locally |
| `npm run lint` | Run `oxlint` |
