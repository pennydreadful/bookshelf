# Changelog

## 1.2.3
- Summary: document the download client mount path as `/downloads`.
- Why: standardize the container-side path for client integration.
- Impact: documentation-only; no runtime changes.
- Files: README.md, src/Directory.Build.props, CHANGELOG.md.
- Next: keep install/run instructions consistent with `/downloads`.

## 1.2.2
- Summary: default proxy cover images to JPEG content type when the filename has no extension.
- Why: Google Books thumbnails often omit file extensions and were not rendering.
- Impact: search results should display covers reliably.
- Files: src/Readarr.Http/Frontend/Mappers/MediaCoverProxyMapper.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: verify covers render on the VM after rebuild.

## 1.2.1
- Summary: prevent search results UI crashes when external links are missing.
- Why: Google Books results may not include author/book links.
- Impact: search page renders without errors even when links are absent.
- Files: frontend/src/Search/Author/AddNewAuthorSearchResult.js, frontend/src/Search/Book/AddNewBookSearchResult.js, src/Directory.Build.props, CHANGELOG.md.
- Next: confirm search page behavior on the VM after rebuild.

## 1.2.0
- Summary: add a source-build install script with error logging.
- Why: make VM installs repeatable and capture clear errors for support.
- Impact: new script writes `/opt/bookdarr/install.log` and builds a local image.
- Files: scripts/install-bookdarr.sh, README.md, docs/HANDOFF.md, src/Directory.Build.props, CHANGELOG.md.
- Next: wire diagnostics upload and Docker Hub image publishing.

## 1.1.3
- Summary: add a handoff guide to help a new Codex chat take over.
- Why: preserve context, preferences, and next steps for continuity.
- Impact: documentation-only; no runtime changes.
- Files: docs/HANDOFF.md, CHANGELOG.md, src/Directory.Build.props.
- Next: keep using this changelog format for new entries.

## 1.1.2
- Fix build by aligning BookInfoProxy formatting to style rules.

## 1.1.1
- Add tooltip instructions for creating a Google Books API key.

## 1.1.0
- Add a Google Books API key field in metadata settings.

## 1.0.2
- Show Google Books free-tier quota warning on the search page.
- Surface a user-friendly error when the Google Books quota is exceeded.

## 1.0.1
- Add Google Books search support with optional API key.

## 1.0.0
- Initial Bookdarr rebrand from Bookshelf/Readarr fork.
