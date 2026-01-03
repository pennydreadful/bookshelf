# Changelog

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
