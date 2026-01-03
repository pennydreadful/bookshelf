# Bookdarr Handoff

Use this file to onboard a new Codex chat.

## Read First (in this order)
- https://github.com/thashiznit2003/Bookdarr/blob/develop/CHANGELOG.md
- https://github.com/thashiznit2003/Bookdarr/blob/develop/README.md
- https://github.com/thashiznit2003/Bookdarr/blob/develop/docs/HANDOFF.md

## Project Summary
- Bookdarr is a fork of Bookshelf/Readarr focused on improved metadata reliability and a friendlier request/search UX.
- Default metadata search uses Google Books (no API key required, shared quota); optional per-instance API key supported.

## User Preferences
- Keep changes small and methodical; avoid assumptions.
- Responses should be concise.
- Always create a backup archive before pushing to GitHub.
- When giving install commands, use sudo and chain with `&&`.
- Avoid adding repeated `apt-get update` steps in install/build flows.
- Changelog entries must be handoff-friendly (Summary/Why/Impact/Files/Next).

## Build/Style Notes
- StyleCop is strict. In `BookInfoProxy.cs`, keep:
  - using directives alphabetized,
  - constants before non-constant fields,
  - multiline method parameters each on their own line.
- Build uses `_output` assets; UI built via `yarn build` and server via `dotnet msbuild`.

## Metadata Notes
- Provider default: `MetadataProvider=googlebooks` (config/env `METADATA_PROVIDER` overrides).
- No-key Google Books calls are allowed but quota is shared/unpredictable.
- Optional per-instance key is in Settings -> Metadata -> Search Metadata.
- Quota errors (403/429) surface as user-facing messages.
- Search page shows a Google Books free-tier disclaimer.

## Open Work / Next Steps
- Docker Hub publish pipeline (GitHub Action + secrets).
- Hardcover fallback provider (research and implementation).
- Multi-file-per-book support (ebook + audiobook in same folder).
- Diagnostics repo + UI upload flow for logs/config.
- Overseerr-like request page.

## Local Backup Convention
- Backups are stored under `/Users/joe/VS Code/Bookdarr-backups/` before each push.
