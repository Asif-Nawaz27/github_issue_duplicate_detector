# IssueSense Web

A small React + TypeScript dashboard for exercising the IssueSense API by
hand: import a repository's issues, generate embeddings, and check a
candidate issue for duplicates — with every action you run logged in an
activity feed on the page.

This is a developer tool, not a production admin console: it has no
authentication, doesn't persist anything server-side, and the activity feed
only exists in the browser tab's memory (refresh and it's gone).

## Running it

The API must be running first (`dotnet run --project App/Api` from the repo
root — see [docs/DEVELOPMENT.md](../../docs/DEVELOPMENT.md)). Then:

```bash
npm install
npm run dev
```

Opens on `http://localhost:5173` by default. Requests to `/api/*` are
proxied to the API at `http://localhost:5100` (see `vite.config.ts`), so no
CORS configuration is needed on the API side for local development.

## What it does

Three actions, each calling the corresponding IssueSense API endpoint for
the owner/repository you enter:

- **Import issues** — `POST /api/repositories/{owner}/{repository}/import`
- **Generate embeddings** — `POST /api/repositories/{owner}/{repository}/generate-embeddings`
- **Check duplicate** — `POST /api/repositories/{owner}/{repository}/check-duplicate`, given a title and optional body

Every call (success or failure) is appended to the Activity panel with a
timestamp, duration, and a one-line summary of the result.

## Scripts

| Command | Does |
|---|---|
| `npm run dev` | Start the Vite dev server |
| `npm run build` | Type-check and build for production into `dist/` |
| `npm run preview` | Preview the production build locally |
| `npm run lint` | Run `oxlint` |
