# Changelog

## 1.2.34
- Summary: add a one-command update script for native dev installs.
- Why: avoid build failures from a running process and simplify updates.
- Impact: `scripts/update-dev.sh` stops, updates, builds, and restarts Bookdarr.
- Files: scripts/update-dev.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: use update-dev.sh for future native updates.

## 1.2.33
- Summary: add a Settings link for Development and update the System status links to Bookdarr.
- Why: surface the hidden Development page and point “More info” at the correct repo.
- Impact: Settings page now includes Development; System -> Status links point to Bookdarr.
- Files: frontend/src/Settings/Settings.js, frontend/src/System/Status/MoreInfo/MoreInfo.js, src/Directory.Build.props, CHANGELOG.md.
- Next: rebuild UI and confirm the new link and repo URLs.

## 1.2.32
- Summary: record command formatting preference in the handoff doc.
- Why: ensure future chats use fenced code blocks for commands.
- Impact: HANDOFF.md now mandates code blocks for user-run commands.
- Files: docs/HANDOFF.md, src/Directory.Build.props, CHANGELOG.md.
- Next: keep command output consistently in code blocks going forward.

## 1.2.31
- Summary: add a shared run script for Bookdarr and route dev runs through it.
- Why: keep dev and Docker launch behavior consistent.
- Impact: new `scripts/run-bookdarr.sh`; `dev-run.sh` now calls it.
- Files: scripts/run-bookdarr.sh, scripts/dev-run.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: use run-bookdarr.sh as the Docker entrypoint later.

## 1.2.30
- Summary: add an indexer export item to the development checklist.
- Why: track the request to support exporting indexers for migration.
- Impact: checklist now includes indexer export work.
- Files: checklist.md, src/Directory.Build.props, CHANGELOG.md.
- Next: plan the export flow (API/UI or script) and implement it.

## 1.2.29
- Summary: allow import script to read connection details from a local env file.
- Why: make it possible to run the import without retyping keys each time.
- Impact: import-indexers.sh loads `/opt/bookdarr-dev/import-indexers.env` if present.
- Files: scripts/import-indexers.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: add the env file on the VM and rerun the import script.

## 1.2.28
- Summary: add a recovery prompt when the target indexer API fails.
- Why: a corrupted indexer table returns HTTP 500 and blocks imports.
- Impact: import-indexers.sh can optionally reset the local indexers table and proceed.
- Files: scripts/import-indexers.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download import-indexers.sh and run it on the Bookdarr VM.

## 1.2.27
- Summary: force visible API key input for the import script prompts.
- Why: terminals can keep echo disabled from previous commands, hiding input.
- Impact: import-indexers.sh ensures echo is on before API key prompts.
- Files: scripts/import-indexers.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download import-indexers.sh and run it on the Bookdarr VM.

## 1.2.26
- Summary: show API key input in the import script prompts.
- Why: allow copy/paste visibility when entering keys interactively.
- Impact: import-indexers.sh no longer hides API key input.
- Files: scripts/import-indexers.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download import-indexers.sh and run it on the Bookdarr VM.

## 1.2.25
- Summary: make indexer imports replace existing entries by default.
- Why: simplify migration by updating matching indexers automatically.
- Impact: import-indexers.sh now replaces existing indexers unless overridden.
- Files: scripts/import-indexers.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download import-indexers.sh and run it on the Bookdarr VM.

## 1.2.24
- Summary: prompt for Readarr and Bookdarr connection details in the import script.
- Why: avoid passing API keys on the command line.
- Impact: import-indexers.sh now asks for source host/port/key and target key interactively.
- Files: scripts/import-indexers.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download import-indexers.sh and run it on the Bookdarr VM.

## 1.2.23
- Summary: add a script to import indexers from another Readarr instance via API.
- Why: avoid manual SQL edits and reduce migration errors.
- Impact: `scripts/import-indexers.sh` pulls from a source Readarr and posts to Bookdarr.
- Files: scripts/import-indexers.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: run import-indexers.sh with the source host/port/API key.

## 1.2.22
- Summary: make dev scripts executable in git to avoid permission errors on fresh clones.
- Why: some setups were cloning scripts without execute bits, causing dev-build.sh to fail.
- Impact: scripts can run directly after checkout; dev-ubuntu.sh still chmods as a safety net.
- Files: scripts/dev-build.sh, scripts/dev-run.sh, scripts/dev-setup-ubuntu.sh, scripts/dev-ubuntu.sh, scripts/install-bookdarr.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download dev-ubuntu.sh and rerun it.

## 1.2.21
- Summary: enforce repo ownership and readable permissions before running dev builds.
- Why: avoid "Permission denied" when executing dev-build.sh as the `joe` user.
- Impact: dev-ubuntu.sh now chowns the repo and ensures scripts are readable.
- Files: scripts/dev-ubuntu.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download dev-ubuntu.sh and rerun it.

## 1.2.20
- Summary: run dev-build.sh via bash and ensure scripts are executable.
- Why: avoid permission errors on some filesystems after git update.
- Impact: dev-ubuntu.sh no longer fails on dev-build.sh execution.
- Files: scripts/dev-ubuntu.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download the dev-ubuntu.sh script and rerun it.

## 1.2.19
- Summary: ensure dev scripts are executable after clone/update.
- Why: avoid "Permission denied" when running dev-build.sh.
- Impact: dev-ubuntu.sh now chmods scripts before building.
- Files: scripts/dev-ubuntu.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: rerun dev-ubuntu.sh on the VM.

## 1.2.18
- Summary: copy built UI assets into the runtime output for native dev runs.
- Why: the dev binary expects UI under `_output/net6.0/<rid>/UI`.
- Impact: native dev runs will load the UI without missing index.html warnings.
- Files: scripts/dev-build.sh, scripts/dev-ubuntu.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: rerun dev-build.sh or dev-ubuntu.sh on the VM, then start dev-run.sh.

## 1.2.17
- Summary: add a single Ubuntu dev script that sets up, builds, and optionally runs Bookdarr.
- Why: simplify dev setup to one command on the VM.
- Impact: `scripts/dev-ubuntu.sh` replaces the multi-step flow; README updated.
- Files: scripts/dev-ubuntu.sh, README.md, src/Directory.Build.props, CHANGELOG.md.
- Next: use dev-ubuntu.sh for future dev installs.

## 1.2.16
- Summary: add a dotnet-install.sh fallback when dotnet-sdk-6.0 isn't in apt.
- Why: Ubuntu 24.04 doesn't provide dotnet-sdk-6.0 packages.
- Impact: dev setup can install .NET 6 via Microsoft script on newer Ubuntu.
- Files: scripts/dev-setup-ubuntu.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download the dev setup script and rerun it.

## 1.2.15
- Summary: install Yarn via npm to avoid corepack permission errors on Ubuntu.
- Why: corepack enable was failing to create global symlinks in /usr/bin.
- Impact: dev setup now uses npm to install Yarn; dev build checks for yarn.
- Files: scripts/dev-setup-ubuntu.sh, scripts/dev-build.sh, README.md, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download the dev setup script and rerun it.

## 1.2.14
- Summary: run `corepack enable` with sudo during Ubuntu dev setup.
- Why: corepack needs root to create global symlinks on Ubuntu.
- Impact: dev setup no longer fails with EACCES on corepack enable.
- Files: scripts/dev-setup-ubuntu.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download the dev setup script and rerun it.

## 1.2.13
- Summary: make the Ubuntu dev setup script work when run with sudo.
- Why: avoid the "-E: command not found" error and support running as root.
- Impact: dev setup now uses sudo when available and runs user commands safely.
- Files: scripts/dev-setup-ubuntu.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: re-download the dev setup script and rerun it.

## 1.2.12
- Summary: fix NodeSource setup execution when running dev setup as root.
- Why: avoid the "-E: command not found" error on Ubuntu.
- Impact: dev setup script now handles sudo vs root properly.
- Files: scripts/dev-setup-ubuntu.sh, src/Directory.Build.props, CHANGELOG.md.
- Next: rerun dev-setup-ubuntu.sh on the VM.

## 1.2.11
- Summary: add native Ubuntu dev scripts (setup/build/run) to avoid Docker during development.
- Why: speed up iteration on the dev VM and defer Docker builds to release time.
- Impact: new scripts for local builds and a README section describing the flow.
- Files: scripts/dev-setup-ubuntu.sh, scripts/dev-build.sh, scripts/dev-run.sh, README.md, src/Directory.Build.props, CHANGELOG.md.
- Next: run dev-setup-ubuntu.sh on the VM and validate a native launch.

## 1.2.10
- Summary: allow the update modal to close even if reload fails.
- Why: prevent the UI from getting stuck on the update dialog.
- Impact: closing the modal clears the update flag before reloading.
- Files: frontend/src/App/AppUpdatedModalConnector.js, src/Directory.Build.props, CHANGELOG.md.
- Next: rebuild and verify the modal can be dismissed.

## 1.2.9
- Summary: normalize SignalR version messages to major.minor.patch.
- Why: prevent the update modal from reappearing due to build-number mismatches.
- Impact: the update modal should dismiss normally after reload.
- Files: frontend/src/Components/SignalRConnector.js, src/Directory.Build.props, CHANGELOG.md.
- Next: rebuild and confirm the update modal can be closed.

## 1.2.8
- Summary: trim the displayed version to major.minor.patch in the UI.
- Why: hide the auto-generated build suffix (e.g., `.40745`) in the header.
- Impact: UI shows clean semantic version while keeping internal build info.
- Files: src/Readarr.Http/Frontend/InitializeJsonController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: rebuild and confirm header shows `v1.2.8`.

## 1.2.7
- Summary: make the install script build only and skip container start by default.
- Why: support Portainer stack redeploys without container name conflicts.
- Impact: install script no longer starts Bookdarr unless `START_CONTAINER=true`.
- Files: scripts/install-bookdarr.sh, README.md, src/Directory.Build.props, CHANGELOG.md.
- Next: confirm stack redeploy works cleanly after rebuilds.

## 1.2.6
- Summary: move the Bookdarr name/version into the left logo area.
- Why: keep the header branding closer to the icon, as requested.
- Impact: header layout shift only; no runtime behavior changes.
- Files: frontend/src/Components/Page/Header/PageHeader.js, frontend/src/Components/Page/Header/PageHeader.css, src/Directory.Build.props, CHANGELOG.md.
- Next: verify alignment looks good in the sidebar header on the VM.

## 1.2.5
- Summary: show Bookdarr name/version in the header and fix proxy covers without file extensions.
- Why: make it easy to confirm the running version and render Google Books covers reliably.
- Impact: new header text; proxy image URLs are now extension-safe.
- Files: frontend/src/Components/Page/Header/PageHeader.js, frontend/src/Components/Page/Header/PageHeader.css, src/NzbDrone.Core/MediaCover/MediaCoverProxy.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: verify cover images appear on the search page after rebuild.

## 1.2.4
- Summary: add a compose file for Portainer stacks and document redeploy flow.
- Why: avoid full rebuilds unless the image actually changes.
- Impact: compose-driven deployments can restart quickly without rebuilding.
- Files: docker-compose.yml, README.md, src/Directory.Build.props, CHANGELOG.md.
- Next: keep stack instructions aligned with `/downloads` and `/config` mounts.

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
