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
- Create an annotated git tag snapshot before pushing to GitHub; no local tar backups.
- When giving install commands, use sudo and chain with `&&`.
- Always put commands or code the user should run in fenced code blocks.
- Avoid adding repeated `apt-get update` steps in install/build flows.
- Changelog entries must be handoff-friendly (Summary/Why/Impact/Files/Next).

## Build/Style Notes
- StyleCop is strict. In `BookInfoProxy.cs`, keep:
  - using directives alphabetized,
  - constants before non-constant fields,
  - multiline method parameters each on their own line.
- Build uses `_output` assets; UI built via `yarn build` and server via `dotnet msbuild`.
- Install script: `scripts/install-bookdarr.sh` (logs to `/opt/bookdarr/install.log`).

## Metadata Notes
- Provider default: `MetadataProvider=googlebooks` (config/env `METADATA_PROVIDER` overrides).
- No-key Google Books calls are allowed but quota is shared/unpredictable.
- Optional per-instance key is in Settings -> Metadata -> Search Metadata.
- Quota errors (403/429) surface as user-facing messages.
- Search page shows a Google Books free-tier disclaimer.

## Open Work / Next Steps
- Docker Hub publish pipeline (GitHub Action + secrets).
- Hardcover fallback provider (research and implementation).
- Validate ebook + audiobook coexistence on real imports and check stats/progress display.
- Diagnostics repo + UI upload flow for logs/config.
- Overseerr-like request page.

## Recent Changes (since last handoff)
- Added Play/Read actions in the Files tab with audio and ebook modals plus a stream endpoint.
- Multi-part MP3 audiobook imports now preserve original filenames instead of renaming on import.
- Combine Audiobook modal now includes a rename toggle so files can be combined without renaming.
- Combine Audiobook now validates source paths, avoids overwriting part names, verifies output file size, and rolls back renamed parts if combining fails.
- Added Audiobook Combining settings (mode, chapters, delete mode) and a manual Combine Audiobook flow on the book details page.
- Combine modal supports drag-reordering MP3 parts and triggers an ffmpeg-based combine command with a top progress bar; output files are added and source parts are deleted per settings.
- Bookshelf page is removed from the sidebar and no longer routed at `/shelf`.
- Author Select no longer shows Monitor Author/Monitor New Books controls.
- Author edit modal no longer includes Monitored/Monitor New Books fields.
- Book edit label now reads “Automatically Switch Edition/Monitoring”.
- Monitoring controls were removed from Author Details, Bookshelf, and Book Editor/Edit Modal; monitoring now only changes on Book Details.
- Imports now auto-unmonitor books once both ebook + audiobook files exist (multi-file audiobook imports count as complete after import).
- Author merge modal now shows author names in the left/right boxes; buttons are plain Keep Left/Keep Right.
- Author merge modal buttons now include author names for clarity.
- Author merge now broadcasts BookUpdated events so merged books appear on the winner's author page immediately.
- Fixed Author Select blank page caused by missing merge props in AuthorIndex.
- Added Author Merge flow in Author Select mode with left/right winner choice and overwrite warning.
- Standard Book Format is always visible in Media Management; help text reminds users to enable Rename Books.
- Removed redundant using directives that caused IDE0005 build failures.
- Added media type tracking for BookFiles (ebook vs audiobook) with a DB migration to backfill existing rows.
- Upgrade/cutoff decisions now compare files within the same media type so ebooks and audiobooks can coexist.
- Import/rename logic now scopes part counts to the media type to keep multi-part audiobooks consistent.
- BookFile API resources now expose `mediaType`.
- Added a “Refresh author picture” button that forces a metadata re-fetch for images.
- Added a Wikipedia-by-name fallback (summary + thumbnail) for authors without Wikidata/Open Library art.
- Fixed a StyleCop build error in MediaCoverService.
- Author posters now use remote URLs directly when no local cover exists (avoids proxy cache misses).
- MediaCoverProxy now URL-encodes filenames so author images render even with quotes/unicode.
- Author images now render correctly when the URL is proxied (no poster-size replacement).
- Author pages show a small attribution label under the blurb when the source is Wikipedia/Open Library.
- Fixed author extras backfill build errors in the API layer.
- Author pages now backfill missing posters/blurbs/links on load and persist them to metadata.
- Author posters fall back to the media-cover proxy when no local author cover exists.
- Wikidata lookups now return Wikipedia links/blurbs even when no image is available.
- Author posters now pull from Wikidata/Wikipedia or Open Library with attribution links.
- Update modal on app reloads is disabled.
- Available Books title tooltip detection now triggers on the truncated title element.
- Available Books titles now show a mouse-following tooltip when truncated.
- Available Books: removal endpoint now returns JSON to avoid false error banners on success.
- Available Books selection is explicit via “Select Available Books”/“Done Selecting”; checkboxes and batch buttons are hidden unless selection mode is enabled.
- Book/Author selection buttons are renamed to “Book Select/Done Selecting” and “Author Select/Done Selecting.”

## Git Snapshot Convention
- Create an annotated tag in the format `snapshot-YYYYMMDD-HHMM` before each push and push the tag to GitHub.
