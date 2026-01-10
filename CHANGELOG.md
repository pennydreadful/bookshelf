# Changelog

## 1.2.197
- Summary: surface diagnostics push failures in the UI response.
- Why: the diagnostics button needs actionable error feedback when GitHub push fails.
- Impact: diagnostics push returns a sanitized error message instead of a generic failure.
- Files: src/NzbDrone.Core/Diagnostics/DiagnosticsPushService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm the Diagnostics page shows detailed failures.

## 1.2.196
- Summary: fix IDE0005 in diagnostics push service.
- Why: the .NET 10 build fails on unused using directives.
- Impact: diagnostics push service builds cleanly in net10.0.
- Files: src/NzbDrone.Core/Diagnostics/DiagnosticsPushService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm diagnostics build completes.

## 1.2.195
- Summary: add a develop-only diagnostics push flow with UI event capture and GitHub export.
- Why: we need a one-click way to bundle logs and UI interactions for debugging recurring issues.
- Impact: a sidebar Diagnostics page pushes log bundles to a dedicated repo and records UI clicks/requests/messages.
- Files: src/NzbDrone.Core/Diagnostics/DiagnosticsPushService.cs, src/NzbDrone.Core/Configuration/ConfigFileProvider.cs, src/Readarr.Api.V1/Diagnostics/DiagnosticsController.cs, src/Readarr.Api.V1/Diagnostics/DiagnosticsStatusResource.cs, src/Readarr.Api.V1/Diagnostics/DiagnosticsPushResultResource.cs, frontend/src/Diagnostics/diagnosticsEvents.js, frontend/src/Utilities/createAjaxRequest.js, frontend/src/System/Diagnostics/Diagnostics.js, frontend/src/App/AppRoutes.js, frontend/src/Components/Page/Sidebar/PageSidebar.js, frontend/src/bootstrap.tsx, src/NzbDrone.Core/Localization/Core/en.json, docs/HANDOFF.md, src/Directory.Build.props, CHANGELOG.md.
- Next: set DiagnosticsRepo/DiagnosticsToken in config.xml and test a diagnostics push end-to-end.

## 1.2.194
- Summary: force RestResource payloads to bind from the request body on POST/PUT/PATCH.
- Why: .NET 10 stopped inferring body binding for these resources, leaving defaults and breaking settings saves.
- Impact: config save endpoints (including Media Management naming) receive the JSON payload and validate correctly.
- Files: src/Readarr.Http/REST/RestResourceBodyBindingConvention.cs, src/NzbDrone.Host/Startup.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm Media Management saves without validation errors.

## 1.2.193
- Summary: capture the GitHub-link refresh/read rule in the handoff doc.
- Why: new chats need to auto-refresh and read linked diagnostics without re-explaining the rule.
- Impact: handoff now instructs to pull and read GitHub-linked files immediately.
- Files: docs/HANDOFF.md, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm update logs still increment from update-01.log.

## 1.2.192
- Summary: remove unused using that tripped IDE0005 in the .NET 10 build.
- Why: the style analyzer fails the build when unused usings remain.
- Impact: VersionedApiControllerAttribute builds cleanly in net10.0.
- Files: src/Readarr.Http/VersionedApiControllerAttribute.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm the build clears IDE0005.

## 1.2.191
- Summary: clear repo log files and reset the update log numbering.
- Why: log artifacts are no longer needed in version control and we want to restart log sequencing at 01.
- Impact: `Logs/` is cleaned out and the handoff now starts update logs at `update-01.log`.
- Files: Logs/*.log, Logs/*.txt, docs/HANDOFF.md, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm future logs start at update-01.log.

## 1.2.190
- Summary: ensure API controllers bind JSON bodies correctly after the .NET 10 upgrade.
- Why: PUT requests were validating default values because the body was not bound to resources.
- Impact: Media Management naming saves now receive the full payload and pass validation.
- Files: src/Readarr.Http/VersionedApiControllerAttribute.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm the Media Management settings save clears pending changes.

## 1.2.189
- Summary: make Standard Book Format validation tolerant of PartNumber token variants.
- Why: valid formats with part tokens were being rejected, preventing saves.
- Impact: format validation now recognizes PartNumber/PartCount tokens in common patterns.
- Files: src/NzbDrone.Core/Organizer/FileNameValidation.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm settings save clears pending changes.

## 1.2.188
- Summary: fix missing HttpContext import in the UI auth redirect.
- Why: build failed with CS0246 after adding the login redirect.
- Impact: StaticResourceController compiles with the new redirect logic.
- Files: src/Readarr.Http/Frontend/StaticResourceController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm the login redirect works.

## 1.2.187
- Summary: redirect unauthenticated UI requests to the login page.
- Why: Forms auth returns a 401 for UI endpoints, so the browser never reaches the login page.
- Impact: root UI routes now redirect to `/login` when auth is required and the user is not authenticated.
- Files: src/Readarr.Http/Frontend/StaticResourceController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm the login redirect works.

## 1.2.186
- Summary: fix .NET 10 deprecations for forwarded headers and cert loading.
- Why: ASPDEPR005 and SYSLIB0057 are treated as errors in the .NET 10 build.
- Impact: forwarded header configuration uses KnownIPNetworks and SSL loading uses X509CertificateLoader.
- Files: src/NzbDrone.Host/Startup.cs, src/NzbDrone.Host/Bootstrap.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm restore/build succeeds on the VM.

## 1.2.185
- Summary: replace obsolete X509Certificate2 loading API for .NET 10.
- Why: SYSLIB0057 blocks builds when loading certificates via deprecated constructors.
- Impact: SSL certificate validation now uses X509CertificateLoader.
- Files: src/Readarr.Api.V1/Config/HostConfigController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm restore/build succeeds on the VM.

## 1.2.184
- Summary: address .NET 10 ASP.NET analyzer errors in authentication and middleware.
- Why: ISystemClock is obsolete and header additions now trigger ASP0019.
- Impact: authentication handlers use the new base ctor and headers are set safely.
- Files: src/Readarr.Http/Authentication/ApiKeyAuthenticationHandler.cs, src/Readarr.Http/Authentication/BasicAuthenticationHandler.cs, src/Readarr.Http/Authentication/NoAuthenticationHandler.cs, src/Readarr.Http/Middleware/VersionMiddleware.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm restore/build succeeds on the VM.

## 1.2.183
- Summary: fix remaining StyleCop SA1508 warnings in exception types.
- Why: analyzer warnings are treated as errors in the .NET 10 build.
- Impact: extra blank lines removed in three exception classes.
- Files: src/NzbDrone.Core/MediaFiles/AzwTag/AzwTagException.cs, src/NzbDrone.Core/MediaFiles/BookImport/RecycleBinException.cs, src/NzbDrone.Core/MediaFiles/BookImport/RootFolderNotFoundException.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm restore/build succeeds on the VM.

## 1.2.182
- Summary: fix .NET 10 analyzers for CA2022 and SYSLIB0051.
- Why: analyzer warnings are treated as errors in the .NET 10 build.
- Impact: file reads use ReadExactly and exception types no longer use formatter serialization.
- Files: src/NzbDrone.Mono/Disk/DiskProvider.cs, src/NzbDrone.Core/Housekeeping/Housekeepers/DeleteBadMediaCovers.cs, src/NzbDrone.Core/MediaFiles/BookImport/RootFolderNotFoundException.cs, src/NzbDrone.Core/MediaFiles/BookImport/RecycleBinException.cs, src/NzbDrone.Core/MediaFiles/AzwTag/AzwTagException.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm restore/build succeeds on the VM.

## 1.2.181
- Summary: fix StyleCop warning introduced by the .NET 10 exception cleanup.
- Why: SA1508 reports a blank line before a closing brace.
- Impact: DestinationAlreadyExistsException formatting now passes analyzers.
- Files: src/NzbDrone.Common/Disk/DestinationAlreadyExistsException.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm restore/build succeeds on the VM.

## 1.2.180
- Summary: remove obsolete .NET APIs flagged as errors under .NET 10.
- Why: SYSLIB0051 and SYSLIB0014 warnings are treated as errors in the .NET 10 build.
- Impact: custom exception no longer uses formatter-based serialization and HttpClient avoids ServicePointManager.
- Files: src/NzbDrone.Common/Disk/DestinationAlreadyExistsException.cs, src/NzbDrone.Common/Http/HttpClient.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm restore/build succeeds on the VM.

## 1.2.179
- Summary: pin System.Drawing.Common to a .NET 10 package to resolve NU1904.
- Why: restore failed due to the vulnerable 4.7.0 transitive version being treated as an error.
- Impact: System.Drawing.Common resolves to 10.0.1 across projects with central package pinning.
- Files: src/Directory.Packages.props, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm restore/build succeeds on the VM.

## 1.2.178
- Summary: align platform packages with .NET 10 and pin transitive versions.
- Why: .NET 10 builds still resolved 6.x platform packages and flagged vulnerable IdentityModel dependencies.
- Impact: Microsoft.Extensions/System packages now target 10.0.1, and transitive pins apply across projects.
- Files: src/Directory.Packages.props, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm restore/build succeeds on the VM.

## 1.2.177
- Summary: fix .NET 10 restore failures by pinning IdentityModel packages and removing redundant framework references.
- Why: NU1902 vulnerability warnings and NU1510 pruning warnings were treated as errors on the net10 restore.
- Impact: restore succeeds with IdentityModel 6.36.0 pinned, and framework packages are no longer explicitly referenced in net10 projects.
- Files: src/Directory.Packages.props, src/NzbDrone.Common/Readarr.Common.csproj, src/NzbDrone.Core/Readarr.Core.csproj, src/NzbDrone.Host/Readarr.Host.csproj, src/NzbDrone.Core.Test/Readarr.Core.Test.csproj, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging and confirm the build completes on the VM.

## 1.2.176
- Summary: align .NET 10 SDK pinning with the 10.0.1 runtime.
- Why: SDKs use 10.0.101 for the 10.0.1 runtime, and the VM couldn’t find 10.0.1 on the SDK feeds.
- Impact: global.json, scripts, and CI now target SDK 10.0.101 so installs resolve cleanly.
- Files: global.json, scripts/dev-ubuntu.sh, scripts/dev-setup-ubuntu.sh, azure-pipelines.yml, mise.toml, docs/NET10_UPGRADE.md, src/Directory.Build.props, CHANGELOG.md.
- Next: install the 10.0.101 SDK, run update-dev.sh with logging, and verify the UI shows v1.2.176.

## 1.2.175
- Summary: upgrade Bookdarr to target .NET 10 and pin the SDK.
- Why: align the build/runtime with the .NET 10.0.1 release and keep output paths consistent.
- Impact: projects now compile for net10.0, scripts/CI build with the 10.0.1 SDK, and dev/test paths use net10.0 output folders.
- Files: src/**/*.csproj, global.json, scripts/dev-ubuntu.sh, scripts/dev-setup-ubuntu.sh, scripts/dev-build.sh, scripts/update-dev.sh, scripts/run-bookdarr.sh, build.sh, docs.sh, azure-pipelines.yml, mise.toml, src/NzbDrone.Test.Common/NzbDroneRunner.cs, docs/NET10_UPGRADE.md, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh with logging, confirm the app starts, and verify the UI shows v1.2.175.

## 1.2.174
- Summary: update README/handoff guidance and restore the sidebar width.
- Why: documentation needed to reflect current behavior and the sidebar should match the original layout.
- Impact: README clarifies dual-format support and metadata defaults; handoff adds push/version/logging reminders; the sidebar width returns to 210px.
- Files: README.md, docs/HANDOFF.md, frontend/src/Styles/Variables/dimensions.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, confirm the sidebar width looks right, and verify the UI shows v1.2.174.

## 1.2.116
- Summary: add a rescan action for book files after edition changes.
- Why: files can become unlinked when switching editions and need a manual recovery path.
- Impact: the book toolbar now offers a rescan that re-links files from the author folder.
- Files: frontend/src/Book/Details/BookDetails.js, frontend/src/Book/Details/BookDetailsConnector.js, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, click Rescan book files on a book, and confirm files reappear.

## 1.2.115
- Summary: relink book files and refresh the books list after edition changes.
- Why: switching editions changed the slug and orphaned file mappings, leading to a 404 page and empty file lists.
- Impact: the UI updates the books collection after edition selection and reattaches existing files to the new edition.
- Files: frontend/src/Components/Form/BookEditionSelectInputConnector.js, src/Readarr.Api.V1/Books/BookController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, switch editions, and confirm no 404 plus files remain listed.

## 1.2.114
- Summary: keep the edit-modal edition switch from navigating to a 404 page.
- Why: changing editions can alter the book slug, leaving the current route stale.
- Impact: after selecting an edition, the UI updates the book state and redirects to the new slug if needed.
- Files: frontend/src/Components/Form/BookEditionSelectInputConnector.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, change an edition, and confirm the book page stays on the updated title.

## 1.2.113
- Summary: fix using-directive ordering after adding edition lookup endpoints.
- Why: style analysis failed the build.
- Impact: Readarr.Api.V1 builds cleanly again.
- Files: src/Readarr.Api.V1/Books/BookController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and retry the build on the VM.

## 1.2.112
- Summary: enable edition lookup from the edit modal and allow switching to a newly searched edition.
- Why: users can get stuck with a wrong-language edition and need a way to re-search metadata.
- Impact: the edition dropdown always opens, fetches edition lookup results from the metadata provider, and applies selected editions directly.
- Files: frontend/src/Book/Edit/EditBookModalContent.js, frontend/src/Components/Form/BookEditionSelectInputConnector.js, frontend/src/Components/Form/SelectInput.js, src/Readarr.Api.V1/Books/BookController.cs, src/Readarr.Api.V1/Books/EditionLookupResource.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open Edit on a book, click the Edition dropdown, and choose an English edition from the lookup list.

## 1.2.111
- Summary: fix using-directive ordering in the book controller and bump the app version.
- Why: style analysis failed the build.
- Impact: build passes style checks for Readarr.Api.V1.
- Files: src/Readarr.Api.V1/Books/BookController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and retry the build on the VM.

## 1.2.110
- Summary: prefer a UI-language edition when refreshing book metadata.
- Why: some books return non-English descriptions unless the edition language is switched.
- Impact: refresh book cover/description now attempts to select an edition matching the UI language before updating covers.
- Files: src/Readarr.Api.V1/Books/BookController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, refresh a book, and confirm the overview switches to English when an English edition exists.

## 1.2.109
- Summary: fix book metadata refresh build error and bump the app version.
- Why: API controller used a RefreshBookInfo overload that was missing from the interface.
- Impact: build succeeds and metadata refresh endpoint compiles cleanly.
- Files: src/NzbDrone.Core/Books/Services/RefreshBookService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and retry the build/refresh flow on the VM.

## 1.2.108
- Summary: add a book metadata refresh that re-downloads covers and uses the UI language for metadata requests.
- Why: book cover/description refresh needed to pull updated metadata and respect the system UI language.
- Impact: book details can refresh cover/overview on demand, and metadata requests include the UI language header.
- Files: frontend/src/Book/Details/BookDetails.js, frontend/src/Book/Details/BookDetailsConnector.js, frontend/src/Store/Actions/bookActions.js, src/NzbDrone.Core/Localization/Core/en.json, src/NzbDrone.Core/MediaCover/MediaCoverService.cs, src/NzbDrone.Core/MetadataSource/MetadataRequestBuilder.cs, src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, src/Readarr.Api.V1/Books/BookController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, refresh a book from the toolbar, and confirm the cover/overview update in your UI language.

## 1.2.107
- Summary: apply light/dark text styling inside the EPUB reader and bump the app version.
- Why: EPUB content was rendering dark text on a dark background in dark mode.
- Impact: dark mode uses light text on a dark background, light mode uses dark text on a light background, and the UI reports 1.2.107.
- Files: frontend/src/BookFile/BookFileReaderModal.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open an EPUB, and confirm text colors match the active theme.

## 1.2.106
- Summary: move EPUB navigation arrows to a modal overlay and bump the app version.
- Why: the in-reader buttons were not visible over the cover.
- Impact: EPUB modals now show prominent left/right arrow buttons for page turns, and the UI reports 1.2.106.
- Files: frontend/src/BookFile/BookFileReaderModal.js, frontend/src/BookFile/BookFileReaderModal.css, frontend/src/BookFile/BookFileReaderModal.css.d.ts, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open an EPUB, and confirm the arrows appear and page turns work.

## 1.2.105
- Summary: make EPUB navigation arrows larger and ensure they layer above the reader.
- Why: the buttons were not visible over the cover in some readers.
- Impact: navigation arrows display more clearly on top of the EPUB content, and the UI reports 1.2.105.
- Files: frontend/src/BookFile/BookFileReaderModal.js, frontend/src/BookFile/BookFileReaderModal.css, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open an EPUB, and confirm the arrows are visible and page turns work.

## 1.2.104
- Summary: always show EPUB navigation arrows and bump the app version.
- Why: navigation buttons were hidden until the reader signaled readiness, so they never appeared for some EPUBs.
- Impact: EPUB modals show left/right arrows immediately for page turning, and the UI reports 1.2.104.
- Files: frontend/src/BookFile/BookFileReaderModal.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open an EPUB, and confirm the arrows appear and page turns work.

## 1.2.103
- Summary: add EPUB page navigation controls and bump the app version.
- Why: the reader showed only the cover without navigation controls.
- Impact: EPUB reader now includes previous/next arrow buttons to turn pages, and the UI reports 1.2.103.
- Files: frontend/src/BookFile/BookFileReaderModal.js, frontend/src/BookFile/BookFileReaderModal.css, frontend/src/BookFile/BookFileReaderModal.css.d.ts, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open an EPUB, and confirm page turns using the left/right arrows.

## 1.2.102
- Summary: improve ebook reader resilience, handle unsupported formats, and bump the app version.
- Why: the reader could fail silently for unsupported or flaky loads, and the UI version needed to reflect updates.
- Impact: unsupported ebook types show a clear message, EPUB render attempts are more defensive, and the UI reports 1.2.102.
- Files: frontend/src/BookFile/BookFileReaderModal.js, frontend/src/BookFile/Editor/BookFileActionsCell.js, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open an EPUB, and confirm the modal renders while the version shows 1.2.102.

## 1.2.101
- Summary: load JSZip before the EPUB reader so ebook rendering works.
- Why: epub.js depends on JSZip, and missing it can result in a blank reader.
- Impact: EPUBs render in the modal once JSZip loads with the reader script.
- Files: frontend/src/BookFile/BookFileReaderModal.js, frontend/src/Content/Scripts/jszip.min.js, CHANGELOG.md.
- Next: run update-dev.sh, open an EPUB from the Files tab, and confirm the reader displays.

## 1.2.100
- Summary: fix EPUB reader rendering in the modal and use an open-book icon for Read actions.
- Why: the reader modal body used a scrolling wrapper that collapsed the reader, and the icon should better match the action.
- Impact: EPUBs render inside the modal instead of a blank panel, and the Files tab shows an open-book Read icon.
- Files: frontend/src/BookFile/BookFileReaderModal.js, frontend/src/BookFile/Editor/BookFileActionsCell.js, frontend/src/Helpers/Props/icons.js, CHANGELOG.md.
- Next: run update-dev.sh, open an EPUB from the Files tab, and confirm the reader renders with the open-book icon.

## 1.2.99
- Summary: harden audiobook detection and load EPUBs via a blob URL.
- Why: some M4B rows still hid Play, and the EPUB modal could render blank despite a valid stream.
- Impact: Play actions show for more audiobook metadata/extension variants, and EPUBs render reliably through the reader modal.
- Files: frontend/src/BookFile/Editor/BookFileActionsCell.js, frontend/src/BookFile/BookFileReaderModal.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, confirm the Play icon appears for M4B files, and verify EPUB content renders in the reader modal.

## 1.2.98
- Summary: export the Play icon so audiobook rows can render the Play button.
- Why: the Play icon constant was missing, so M4B rows couldn’t render the button even when detected.
- Impact: Play now appears for audiobook files once detection flags them as audio.
- Files: frontend/src/Helpers/Props/icons.js, CHANGELOG.md.
- Next: run update-dev.sh and confirm M4B rows show the Play icon.

## 1.2.97
- Summary: show Play actions for audiobook files using media type/quality detection.
- Why: some audiobook rows weren’t showing the Play icon despite M4B files being present.
- Impact: Play now appears for M4B/MP3 files even when extension detection fails.
- Files: frontend/src/BookFile/Editor/BookFileActionsCell.js, frontend/src/BookFile/Editor/BookFileEditorRow.js, CHANGELOG.md.
- Next: run update-dev.sh and confirm M4B rows show the Play icon.

## 1.2.96
- Summary: load the EPUB reader script with API key auth and default combine renaming to off.
- Why: authenticated static assets blocked the EPUB viewer, and renaming should be opt-in.
- Impact: Read ebook now surfaces load failures and works with API key auth; Combine Audiobook defaults to keeping original filenames; stream endpoint allows HEAD requests.
- Files: frontend/src/BookFile/BookFileReaderModal.js, frontend/src/BookFile/BookFileReaderModal.css, frontend/src/BookFile/BookFileReaderModal.css.d.ts, frontend/src/BookFile/Editor/BookFileActionsCell.js, frontend/src/Book/Combine/CombineAudiobookModalContent.js, src/Readarr.Api.V1/BookFiles/BookFileController.cs, src/NzbDrone.Core/Localization/Core/en.json, CHANGELOG.md.
- Next: run update-dev.sh, confirm Play/Read buttons appear in Files, and verify EPUB opens without a blank modal.

## 1.2.95
- Summary: fix missing ebook icon in the Files tab.
- Why: the book reader icon was referenced but not exported.
- Impact: Read ebook buttons now render correctly for EPUB/PDF files.
- Files: frontend/src/Helpers/Props/icons.js, CHANGELOG.md.
- Next: run update-dev.sh and confirm Play/Read buttons appear in the Files tab.

## 1.2.94
- Summary: add in-app audio playback and ebook viewing from the Files tab.
- Why: Bookdarr needed lightweight playback for M4B/MP3 and viewing for EPUB/PDF.
- Impact: Files rows now include Play/Read actions, a streaming endpoint, and a modal audio player/ebook reader.
- Files: frontend/src/BookFile/BookFileAudioModal.js, frontend/src/BookFile/BookFileAudioModal.css, frontend/src/BookFile/BookFileAudioModal.css.d.ts, frontend/src/BookFile/BookFileReaderModal.js, frontend/src/BookFile/BookFileReaderModal.css, frontend/src/BookFile/BookFileReaderModal.css.d.ts, frontend/src/BookFile/Editor/BookFileActionsCell.js, frontend/src/BookFile/Editor/BookFileActionsCell.css, frontend/src/Content/Scripts/epub.min.js, frontend/build/webpack.config.js, src/Readarr.Api.V1/BookFiles/BookFileController.cs, src/NzbDrone.Core/Localization/Core/en.json, CHANGELOG.md.
- Next: run update-dev.sh, open a book with M4B/EPUB files, and verify Play/Read works from the Files tab.

## 1.2.93
- Summary: keep original filenames for multi-part MP3 audiobook imports.
- Why: multi-file audiobooks already have meaningful part names and ordering.
- Impact: when importing multiple MP3s for an audiobook, Bookdarr preserves original base filenames while still moving into the book folder.
- Files: src/NzbDrone.Core/MediaFiles/BookFileMovingService.cs, CHANGELOG.md.
- Next: run update-dev.sh, import a multi-part MP3 audiobook, and confirm the parts keep their original names.

## 1.2.92
- Summary: allow Combine Audiobook to skip renaming when files are already ordered correctly.
- Why: many audiobook parts already follow the desired naming, so renaming can be unnecessary.
- Impact: combine modal now includes a rename toggle; backend respects it and only renames when enabled.
- Files: frontend/src/Book/Combine/CombineAudiobookModalContent.js, frontend/src/Book/Combine/CombineAudiobookModalContent.css, frontend/src/Book/Combine/CombineAudiobookModalContent.css.d.ts, frontend/src/Book/Details/BookDetails.js, frontend/src/Book/Details/BookDetailsConnector.js, frontend/src/Commands/Command.ts, src/NzbDrone.Core/MediaFiles/Commands/CombineAudiobookCommand.cs, src/NzbDrone.Core/MediaFiles/CombineAudiobookService.cs, src/NzbDrone.Core/Localization/Core/en.json, CHANGELOG.md.
- Next: run update-dev.sh, try combine with rename unchecked, and verify parts keep their original names.

## 1.2.91
- Summary: make Combine Audiobook safer when renaming or ffmpeg fails.
- Why: missing source files or name collisions caused ffmpeg failures and left renamed parts without clear recovery.
- Impact: combine validates source/target paths, avoids overwriting part names, verifies output size, and rolls back renamed parts when combining fails.
- Files: src/NzbDrone.Core/MediaFiles/CombineAudiobookService.cs, CHANGELOG.md.
- Next: run update-dev.sh, set delete mode to Keep, retry Combine Audiobook, and confirm files remain if ffmpeg fails.

## 1.2.90
- Summary: add a manual audiobook combine tool with reorder modal and progress bar.
- Why: multi-part MP3 audiobooks need to be merged into a single MP3/M4B with optional chapters.
- Impact: Media Management adds Audiobook Combining settings; Book Details adds a Combine Audiobook button, a drag-order modal, and a top progress bar; backend combines parts via ffmpeg and optionally deletes source files.
- Files: frontend/src/Book/Combine/CombineAudiobookModal.js, frontend/src/Book/Combine/CombineAudiobookModalContent.js, frontend/src/Book/Combine/CombineAudiobookModalContent.css, frontend/src/Book/Combine/CombineAudiobookModalContent.css.d.ts, frontend/src/Book/Details/BookDetails.js, frontend/src/Book/Details/BookDetails.css, frontend/src/Book/Details/BookDetails.css.d.ts, frontend/src/Book/Details/BookDetailsConnector.js, frontend/src/Book/Details/CombineAudiobookProgress.js, frontend/src/Book/Details/CombineAudiobookProgress.css, frontend/src/Book/Details/CombineAudiobookProgress.css.d.ts, frontend/src/Commands/commandNames.js, frontend/src/Components/Page/Toolbar/PageToolbarButton.js, frontend/src/Commands/Command.ts, frontend/src/Settings/MediaManagement/MediaManagement.js, src/NzbDrone.Core/MediaFiles/CombineAudiobookService.cs, src/NzbDrone.Core/MediaFiles/CombineAudiobookMode.cs, src/NzbDrone.Core/MediaFiles/CombineAudiobookDeleteMode.cs, src/NzbDrone.Core/MediaFiles/Commands/CombineAudiobookCommand.cs, src/NzbDrone.Core/Configuration/ConfigService.cs, src/NzbDrone.Core/Configuration/IConfigService.cs, src/Readarr.Api.V1/Config/MediaManagementConfigResource.cs, src/NzbDrone.Core/Localization/Core/en.json, CHANGELOG.md.
- Next: run update-dev.sh, open a book with multiple audiobook MP3s, combine them, and verify the progress bar plus the new output file in the Files tab.

## 1.2.89
- Summary: remove the Bookshelf page from navigation and routing.
- Why: the page is non-functional and should be hidden/disabled.
- Impact: the Bookshelf link is gone from the sidebar and `/shelf` no longer routes to the page.
- Files: frontend/src/App/AppRoutes.js, frontend/src/Components/Page/Sidebar/PageSidebar.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, confirm the Bookshelf link is gone, and verify `/shelf` is no longer accessible.

## 1.2.88
- Summary: remove author-level monitoring controls in Author Select and Edit, and clarify the book edit label.
- Why: monitoring should be managed at the book level only; the author-level controls were still visible.
- Impact: Author Select no longer shows Monitor Author/Monitor New Books; author edit modal no longer includes the Monitored/Monitor New Books fields; book edit label now reads “Automatically Switch Edition/Monitoring”.
- Files: frontend/src/Author/Editor/AuthorEditorFooter.js, frontend/src/Author/Edit/EditAuthorModalContent.js, frontend/src/Author/Edit/EditAuthorModalContentConnector.js, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, confirm Author Select and Edit no longer show monitoring fields, and verify the book edit label text.

## 1.2.87
- Summary: remove author/bookshelf/bulk monitoring controls and auto-unmonitor books once both ebook and audiobook files exist.
- Why: monitoring should only be changed on the book details page, and completed media sets should stop monitoring automatically.
- Impact: author details and Bookshelf no longer expose monitor toggles; book edit/bulk edit no longer offer monitoring changes; after import, any book with both media types is auto-unmonitored (multi-file audiobooks still count once imported).
- Files: frontend/src/Author/Details/AuthorDetails.js, frontend/src/Author/Details/AuthorDetailsConnector.js, frontend/src/Author/Details/AuthorDetailsHeader.js, frontend/src/Author/Details/AuthorDetailsHeaderConnector.js, frontend/src/Author/Details/AuthorDetailsSeason.js, frontend/src/Author/Details/AuthorDetailsSeasonConnector.js, frontend/src/Author/Details/AuthorDetailsSeries.js, frontend/src/Author/Details/AuthorDetailsSeriesConnector.js, frontend/src/Author/Details/BookRow.js, frontend/src/Author/Details/BookRowConnector.js, frontend/src/Book/Edit/EditBookModalContent.js, frontend/src/Book/Edit/EditBookModalContentConnector.js, frontend/src/Book/Editor/BookEditorFooter.js, frontend/src/Bookshelf/Bookshelf.js, frontend/src/Bookshelf/BookshelfBook.js, frontend/src/Bookshelf/BookshelfRow.js, frontend/src/Bookshelf/BookshelfRowConnector.js, src/NzbDrone.Core/MediaFiles/BookImport/ImportApprovedBooks.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, confirm author/bookshelf monitoring controls are gone, and verify a book with both ebook + audiobook files auto-unmonitors after import.

## 1.2.86
- Summary: show author names in merge selection boxes and keep buttons simple.
- Why: author names were missing due to a trimmed selector, which made merge choices unclear.
- Impact: left/right boxes now show author names; buttons read “Keep Left/Keep Right”.
- Files: frontend/src/Store/Selectors/createAuthorClientSideCollectionItemsSelector.js, frontend/src/Author/Editor/Merge/MergeAuthorModalContent.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and confirm the merge modal shows author names in the selection boxes.

## 1.2.85
- Summary: make author merge choices clearer by showing names on the buttons.
- Why: the left/right labels were easy to confuse without context.
- Impact: merge modal buttons now include the author names for each side.
- Files: frontend/src/Author/Editor/Merge/MergeAuthorModalContent.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and confirm the merge modal clearly shows which author you are keeping.

## 1.2.84
- Summary: refresh book resources after author merge so author pages show merged books.
- Why: merged books updated in the database but the UI list was stale because book update events were not broadcast.
- Impact: after merging, book updates are broadcast and author pages show all merged books without a manual refresh.
- Files: src/NzbDrone.Core/Books/Services/AuthorMergeService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, merge two authors, and confirm merged books appear immediately on the winner's author page.

## 1.2.83
- Summary: fix Author Select view going blank after enabling merge UI.
- Why: the AuthorIndex render referenced merge props that were not destructured, causing a runtime error.
- Impact: Author Select no longer blanks the page; merge modal can be opened as expected.
- Files: frontend/src/Author/Index/AuthorIndex.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, enter Author Select, and confirm the page stays visible.

## 1.2.82
- Summary: add author merge flow to resolve duplicate authors.
- Why: near-duplicate author names (e.g., spacing differences) can create duplicate author entries.
- Impact: Author Select mode offers a Merge Authors action with a left/right winner choice, warning about overwriting files; merge moves loser files into the winner folder, reassigns books, and deletes the loser.
- Files: frontend/src/Author/Editor/AuthorEditorFooter.js, frontend/src/Author/Editor/AuthorEditorFooter.css, frontend/src/Author/Editor/Merge/MergeAuthorModal.js, frontend/src/Author/Editor/Merge/MergeAuthorModalContent.js, frontend/src/Author/Editor/Merge/MergeAuthorModalContent.css, frontend/src/Author/Index/AuthorIndex.js, frontend/src/Author/Index/AuthorIndexConnector.js, frontend/src/Store/Actions/authorIndexActions.js, src/Readarr.Api.V1/Author/AuthorController.cs, src/Readarr.Api.V1/Author/MergeAuthorsResource.cs, src/NzbDrone.Core/Books/Services/AuthorMergeService.cs, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, select exactly two authors in Author Select mode, merge, and confirm books/files move into the winning author folder.

## 1.2.81
- Summary: switch backup policy to git tag snapshots only.
- Why: tags allow quick reverts without creating large local archives.
- Impact: handoff now directs tags (`snapshot-YYYYMMDD-HHMM`) and no local tar backups.
- Files: docs/HANDOFF.md, src/Directory.Build.props, CHANGELOG.md.
- Next: create and push a snapshot tag before the next GitHub push.

## 1.2.80
- Summary: restore the Standard Book Format field even when Rename Books is disabled.
- Why: users still need to see and set the global naming pattern while toggling rename behavior.
- Impact: the field stays visible with a help reminder to enable Rename Books; naming rules remain unchanged.
- Files: frontend/src/Settings/MediaManagement/Naming/Naming.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and confirm Standard Book Format is visible and editable.

## 1.2.79
- Summary: fix IDE0005 build errors by removing unnecessary using directives.
- Why: the dev build failed with StyleCop/IDE warnings treated as errors.
- Impact: update-dev.sh completes without the IDE0005 errors in import/identification code.
- Files: src/NzbDrone.Core/MediaFiles/BookImport/Identification/IdentificationService.cs, src/NzbDrone.Core/MediaFiles/BookImport/ImportApprovedBooks.cs, src/NzbDrone.Core/MediaFiles/BookImport/ImportDecisionMaker.cs, src/NzbDrone.Core/MediaFiles/BookImport/Manual/ManualImportService.cs, src/NzbDrone.Core/MediaFiles/BookImport/Specifications/UpgradeSpecification.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh again to confirm the build passes.

## 1.2.78
- Summary: support ebook + audiobook files per book with media-type-aware upgrades.
- Why: importing one format should not delete the other, and quality comparisons must stay within the same media type.
- Impact: BookFiles store a media type, upgrade/cutoff checks compare only matching types, part counts are scoped by media type, and the API exposes mediaType.
- Files: src/NzbDrone.Core/MediaFiles/BookFileMediaType.cs, src/NzbDrone.Core/MediaFiles/MediaFileExtensions.cs, src/NzbDrone.Core/MediaFiles/BookFile.cs, src/NzbDrone.Core/Parser/Model/LocalBook.cs, src/NzbDrone.Core/Datastore/Migration/041_add_bookfile_media_type.cs, src/NzbDrone.Core/MediaFiles/BookImport/ImportDecisionMaker.cs, src/NzbDrone.Core/MediaFiles/BookImport/Identification/IdentificationService.cs, src/NzbDrone.Core/MediaFiles/BookImport/Manual/ManualImportService.cs, src/NzbDrone.Core/MediaFiles/DiskScanService.cs, src/NzbDrone.Core/MediaFiles/BookImport/ImportApprovedBooks.cs, src/NzbDrone.Core/MediaFiles/UpgradeMediaFileService.cs, src/NzbDrone.Core/MediaFiles/RenameBookFileService.cs, src/NzbDrone.Core/MediaFiles/BookImport/Specifications/UpgradeSpecification.cs, src/NzbDrone.Core/DecisionEngine/Specifications/UpgradeAllowedSpecification.cs, src/NzbDrone.Core/DecisionEngine/Specifications/CutoffSpecification.cs, src/NzbDrone.Core/DecisionEngine/Specifications/UpgradeDiskSpecification.cs, src/Readarr.Api.V1/BookFiles/BookFileResource.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, import an ebook and audiobook for the same book, and confirm both files remain listed under the book files table.

## 1.2.77
- Summary: add an author picture refresh button that forces a metadata re-fetch.
- Why: users need a quick way to fix incorrect or missing author photos without a full refresh.
- Impact: author pages include a “Refresh author picture” button; it refreshes metadata images and updates the author record.
- Files: src/NzbDrone.Core/MetadataSource/AuthorExtraMetadata.cs, src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, src/Readarr.Api.V1/Author/AuthorController.cs, frontend/src/Author/Details/AuthorDetails.js, frontend/src/Author/Details/AuthorDetailsConnector.js, frontend/src/Store/Actions/authorActions.js, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and use the new button on an author page to confirm the poster updates.

## 1.2.76
- Summary: add Wikipedia direct lookup fallback for author images/blurbs.
- Why: some authors (e.g., Dave Ramsey) lack Wikidata image data but do have Wikipedia summaries.
- Impact: when Wikidata/Open Library fail, Bookdarr pulls a Wikipedia summary and thumbnail (if available) by author name, skipping disambiguation pages.
- Files: src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open the Dave Ramsey author page, and confirm the blurb/image are present if Wikipedia provides them.

## 1.2.75
- Summary: fix build style error in MediaCoverService.
- Why: StyleCop rejected a missing blank line, which stopped builds.
- Impact: build passes without the StyleCop error.
- Files: src/NzbDrone.Core/MediaCover/MediaCoverService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and confirm the build completes.

## 1.2.74
- Summary: serve author posters directly from remote URLs when no local cover exists.
- Why: MediaCoverProxy entries are in-memory and can be missing after restarts, causing author images to fail.
- Impact: author images load directly from Wikimedia/Open Library without relying on the proxy cache.
- Files: src/NzbDrone.Core/MediaCover/MediaCoverService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and confirm author posters display (Andy Weir, Stephen King).

## 1.2.73
- Summary: URL-encode proxied cover filenames.
- Why: author images with quotes/unicode in filenames were not rendering through the proxy.
- Impact: MediaCoverProxy links now work reliably for Wikimedia/Open Library images with special characters.
- Files: src/NzbDrone.Core/MediaCover/MediaCoverProxy.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and reload an author page (Andy Weir should show a poster).

## 1.2.72
- Summary: show remote author images correctly and add attribution under the author blurb.
- Why: author posters were not rendering when served via the proxy, and attribution was requested in the UI.
- Impact: author images display reliably even when they are remote/proxied; author pages show a small “Source: Wikipedia/Open Library” label under the blurb when applicable.
- Files: frontend/src/Author/AuthorImage.js, frontend/src/Author/Details/AuthorDetailsHeader.js, frontend/src/Author/Details/AuthorDetailsHeader.css, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open an author page, and confirm the poster and attribution label are visible.

## 1.2.71
- Summary: fix author extras build errors in the API layer.
- Why: a type mismatch in the author metadata backfill prevented compilation.
- Impact: Bookdarr builds cleanly while still backfilling author images/blurbs/links.
- Files: src/Readarr.Api.V1/Author/AuthorController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and confirm the build completes.

## 1.2.70
- Summary: backfill author photos/blurbs/links and serve remote author images when local covers are missing.
- Why: author images and Wikipedia blurbs were not appearing for existing or newly added authors.
- Impact: author pages now populate missing posters and overviews from Wikidata/Wikipedia or Open Library, include Wikipedia links when available, and display remote author images via the cover proxy if no local file exists.
- Files: src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, src/NzbDrone.Core/MetadataSource/AuthorExtraMetadata.cs, src/NzbDrone.Core/MediaCover/MediaCoverService.cs, src/Readarr.Api.V1/Author/AuthorController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open an author page with missing art, and confirm the poster, blurb, and Wikipedia link appear.

## 1.2.69
- Summary: add Wikidata/Open Library author photos with attribution and disable the update modal.
- Why: author pages need real photos with clear source links, and the update modal was outdated/noisy.
- Impact: author posters are populated from Wikidata/Wikipedia or Open Library with attribution links; the update modal no longer appears after updates.
- Files: src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, frontend/src/Components/Page/Page.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, refresh an author page to verify a poster appears with source links, and confirm no update popup appears.

## 1.2.68
- Summary: fix Available Books title tooltips so they trigger on truncated text.
- Why: the hover detection was bound to an inner element and never detected truncation.
- Impact: hovering a truncated Available Books title now shows the full title tooltip reliably.
- Files: frontend/src/Author/Details/AuthorDetailsAvailableBooks.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and hover a truncated Available Books title to confirm the tooltip appears.

## 1.2.67
- Summary: show a mouse-following tooltip for truncated available-book titles.
- Why: long titles were clipped with ellipses and the full text was not accessible.
- Impact: hovering a truncated title shows the full title in a floating tooltip that follows the cursor.
- Files: frontend/src/Author/Details/AuthorDetailsAvailableBooks.js, frontend/src/Author/Details/AuthorDetailsAvailableBooks.css, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and hover a truncated Available Books title to confirm the tooltip appears.

## 1.2.66
- Summary: fix false removal errors, add a selection toggle for available books, and rename select mode buttons.
- Why: removing available books showed an error despite success, and selection mode should be explicit and clearer across library pages.
- Impact: available book removals return JSON to avoid error banners; selection is enabled via “Select Available Books” and hidden when done; Book/Author buttons now read “Select” and “Done Selecting.”
- Files: src/Readarr.Api.V1/Author/AuthorBooksController.cs, frontend/src/Author/Details/AuthorDetailsAvailableBooks.js, frontend/src/Author/Details/AuthorDetailsAvailableBooks.css, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, remove an available book via the X and batch remove, and confirm no error banner appears.

## 1.2.65
- Summary: add available-book exclusions with confirmations and batch actions, and stop author adds from auto-importing books.
- Why: you need to hide unwanted books from the available list and avoid auto-adding an author’s entire catalog.
- Impact: available books can be selected, added, or excluded; excluded books stay hidden after refresh; adding an author only adds the author.
- Files: frontend/src/Author/Details/AuthorDetailsAvailableBooks.js, frontend/src/Author/Details/AuthorDetailsAvailableBooks.css, frontend/src/Author/Details/AuthorDetailsAvailableBooksConnector.js, frontend/src/Store/Actions/authorAvailableBooksActions.js, frontend/src/Search/Author/AddNewAuthorModalContentConnector.js, frontend/src/Utilities/Author/getNewAuthor.js, src/Readarr.Api.V1/Author/AuthorBooksController.cs, src/Readarr.Api.V1/Author/AuthorBooksExcludeResource.cs, src/Readarr.Api.V1/Author/AuthorController.cs, src/Readarr.Api.V1/Author/AuthorResource.cs, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, verify single/batch remove confirmations, and confirm adding an author does not add books.

## 1.2.64
- Summary: make author refresh reload available books only, relabel the Books tab, and add available books as monitored.
- Why: the author refresh action was adding all books unintentionally, the tab label was ambiguous, and available-book adds were coming in unmonitored.
- Impact: refresh now re-fetches available books, the Books tab reads “Books added to Bookdarr,” and author-page adds are monitored by default.
- Files: frontend/src/Author/Details/AuthorDetailsConnector.js, frontend/src/Author/Details/AuthorDetails.js, src/Readarr.Api.V1/Author/AuthorBooksController.cs, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, verify refresh only reloads Available Books, confirm tab label change, and add a book to verify it is monitored.

## 1.2.63
- Summary: refine the Available Books grid layout to be compact and readable.
- Why: the initial grid was overly tall with cramped text, making titles hard to read.
- Impact: cards are shorter with fixed cover sizing, improved text wrapping, and cleaner metadata spacing.
- Files: frontend/src/Author/Details/AuthorDetailsAvailableBooks.css, frontend/src/Author/Details/AuthorDetailsAvailableBooks.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open an author page, and confirm Available Books renders with compact cards and readable titles.

## 1.2.62
- Summary: show available author books on the author page and add a “Request All Books” action in the edit modal.
- Why: the author page only listed existing library books, so discovery and bulk adds were missing.
- Impact: author pages now list addable books with a + button; edit modal can request all available books at once.
- Files: src/Readarr.Api.V1/Author/AuthorBooksAddResource.cs, src/Readarr.Api.V1/Author/AuthorBooksController.cs, frontend/src/Store/Actions/authorAvailableBooksActions.js, frontend/src/Store/Actions/index.js, frontend/src/Author/Details/AuthorDetailsAvailableBooks.js, frontend/src/Author/Details/AuthorDetailsAvailableBooks.css, frontend/src/Author/Details/AuthorDetailsAvailableBooksConnector.js, frontend/src/Author/Details/AuthorDetails.js, frontend/src/Author/Edit/EditAuthorModalContent.js, frontend/src/Author/Edit/EditAuthorModalContent.css, frontend/src/Author/Edit/EditAuthorModalContentConnector.js, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh, open an author page, confirm Available Books renders with + buttons, and verify “Request All Books” in Edit.

## 1.2.61
- Summary: avoid page crash when a deleted book is still referenced during selection updates.
- Why: the book author selector assumed a book always exists, which threw after a delete and triggered the error page.
- Impact: deleting books from Book Editor no longer shows the error page.
- Files: frontend/src/Store/Selectors/createBookAuthorSelector.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and delete a book from Book Editor to confirm no error page appears.

## 1.2.60
- Summary: make book covers edition-independent by preferring the local book cover file.
- Why: cover art was tied to the monitored edition, so toggling monitored could drop images and show placeholders.
- Impact: if a local book cover exists, it is always used for display regardless of edition state.
- Files: src/NzbDrone.Core/MediaCover/MediaCoverService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and toggle monitored to confirm covers stay unchanged.

## 1.2.59
- Summary: keep book covers on save by reloading editions before returning the updated resource.
- Why: the edit save response could omit editions, which wiped images in the UI even though nothing about covers changed.
- Impact: edit saves retain existing cover art and no longer replace it with placeholders.
- Files: src/Readarr.Api.V1/Books/BookController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and toggle monitored in the Edit Book modal to confirm covers stay.

## 1.2.58
- Summary: keep book covers by selecting an edition with images when available.
- Why: when no edition is marked monitored, the fallback could pick an edition without images and show placeholders.
- Impact: monitor/unmonitor toggles keep covers visible as long as any edition has artwork.
- Files: src/Readarr.Api.V1/Books/BookResource.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and toggle a book’s monitored state to confirm covers stay.

## 1.2.57
- Summary: keep book covers when toggling monitored state.
- Why: the edit flow could return a resource without a monitored edition, resulting in empty images and placeholder art.
- Impact: book art remains visible after monitor/unmonitor changes by falling back to the first edition.
- Files: src/Readarr.Api.V1/Books/BookResource.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and toggle monitored on a book to verify covers stay.

## 1.2.56
- Summary: prevent edit saves from failing when editions are missing.
- Why: PUT /api/v1/book could throw when editions or links were null, and UpdateMany was called with a null list.
- Impact: Book edit saves no longer throw 500s or break BookEditedEvent broadcasts when editions are omitted.
- Files: src/Readarr.Api.V1/Books/BookResource.cs, src/Readarr.Api.V1/Books/BookController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and confirm the Edit Book modal saves and closes.

## 1.2.55
- Summary: avoid crashing Book save when edit payload omits editions.
- Why: the Book Details edit modal triggers a PUT without editions, which caused a 500 error and made Save appear to do nothing.
- Impact: edit modal saves no longer error when editions are missing from the payload; monitoring changes still handled separately.
- Files: src/Readarr.Api.V1/Books/BookResource.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and confirm Book Details edit Save updates without errors.

## 1.2.54
- Summary: force monitored updates from the edit modal via the monitor endpoint.
- Why: the Book Details edit modal was not reliably applying monitor/unmonitor changes.
- Impact: toggling Monitored in the edit modal now updates the book immediately (including on the Book Details page).
- Files: frontend/src/Book/Edit/EditBookModalContentConnector.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and verify edit modal monitoring on the Book Details page.

## 1.2.53
- Summary: return updated book data on save and expose selection checkboxes in Book Editor mode.
- Why: unmonitor changes from the edit modal were not reflected in the UI, and bulk unmonitor was hard to access.
- Impact: book edit saves update the local list immediately; Book Editor shows the select column so bulk unmonitoring is available in table view.
- Files: src/Readarr.Api.V1/Books/BookController.cs, frontend/src/Book/Index/Table/BookIndexTable.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and verify unmonitoring from Edit Book and bulk unmonitor in Book Editor.

## 1.2.44
- Summary: default author add to "All Books" when the add author modal opens.
- Why: avoid only adding a single book when prior defaults were set to a single-book option.
- Impact: author add starts with "All Books" monitoring unless you change it before confirming.
- Files: frontend/src/Search/Author/AddNewAuthorModalContentConnector.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and verify author adds now pull the full catalog.

## 1.2.52
- Summary: fix stylecop spacing in MetadataProfileService.
- Why: build failed due to a missing blank line after a closing brace.
- Impact: update-dev.sh completes without SA1513.
- Files: src/NzbDrone.Core/Profiles/Metadata/MetadataProfileService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and retry adding authors.

## 1.2.51
- Summary: relax metadata profile filters for Google Books and stop using book covers as author posters.
- Why: Google Books doesn’t provide popularity scores or author photos; those filters were removing all books and the poster fallback was misleading.
- Impact: author adds from Google Books should now populate books; author posters will show the default placeholder unless a real author image exists.
- Files: src/NzbDrone.Core/Profiles/Metadata/MetadataProfileService.cs, src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and re-test adding J. K. Rowling and author images.

## 1.2.50
- Summary: fix stylecop ordering error from the AddBookService import change.
- Why: builds fail when using directives are out of order.
- Impact: update-dev.sh completes without the SA1210 error.
- Files: src/NzbDrone.Core/Books/Services/AddBookService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and re-test author add and covers.

## 1.2.49
- Summary: improve Google Books author results and cover handling.
- Why: author adds were still too small and cover images were inconsistent or missing.
- Impact: author adds try multiple Google Books queries, author posters fall back to the first book cover, Google thumbnails are forced to HTTPS, and manual book adds download their cover without triggering author refresh.
- Files: src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, src/NzbDrone.Core/Books/Services/AddBookService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and re-test J. K. Rowling and book/author covers.

## 1.2.48
- Summary: fix Google Books paging build error.
- Why: `HttpRequest` doesn’t expose `AddQueryParam`; paging must be added before request build.
- Impact: update-dev.sh builds again with Google Books paging enabled.
- Files: src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and retry the author add flow.

## 1.2.47
- Summary: page Google Books author results and stop manual book adds from triggering author refresh.
- Why: author adds were returning too few books and manual book adds were pulling extra books from author refreshes.
- Impact: author add fetches up to 200 Google Books results; adding a single book no longer auto-adds other books.
- Files: src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, src/NzbDrone.Core/Books/Services/AddBookService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and re-test adding a book and adding J. K. Rowling as an author.

## 1.2.46
- Summary: fix build error in Google Books author search.
- Why: avoid variable shadowing that caused the dev build to fail.
- Impact: update-dev.sh completes successfully again.
- Files: src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and retry adding J. K. Rowling as an author.

## 1.2.45
- Summary: use `inauthor:` for Google Books author searches.
- Why: generic Google Books queries can return unrelated authors and lead to partial catalogs.
- Impact: author search results and author adds should map to the correct author when using Google Books metadata.
- Files: src/NzbDrone.Core/MetadataSource/BookInfo/BookInfoProxy.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and retry adding J. K. Rowling as an author.

## 1.2.43
- Summary: default book adds to a single book and warn before adding an author.
- Why: choosing a book should not add the entire author catalog; adding an author should be explicit.
- Impact: book add defaults to “Only This Book” and existing defaults migrate away from “All Books”; adding an author now shows a confirmation warning.
- Files: frontend/src/Store/Actions/searchActions.js, frontend/src/Store/Migrators/migrateAddBookDefaults.js, frontend/src/Store/Migrators/migrate.js, frontend/src/Search/Author/AddNewAuthorModalContent.js, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and verify book vs author add behavior.

## 1.2.42
- Summary: return JSON from the create-folder API to avoid false UI errors.
- Why: the file browser expects JSON; empty responses were treated as errors.
- Impact: folder creation no longer shows a failure banner when it succeeds.
- Files: src/Readarr.Api.V1/FileSystem/FileSystemController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and re-test create folder.

## 1.2.41
- Summary: add missing System import for the new filesystem folder API.
- Why: fixes the build error in FileSystemController.
- Impact: build completes again after update-dev.sh.
- Files: src/Readarr.Api.V1/FileSystem/FileSystemController.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh to rebuild.

## 1.2.40
- Summary: fix duplicate import in FormInputGroup.
- Why: resolves the webpack build failure after the permissions tip update.
- Impact: frontend build succeeds again.
- Files: frontend/src/Components/Form/FormInputGroup.js, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh to rebuild.

## 1.2.39
- Summary: add create-folder support in the file browser and show a permissions tip for unwritable paths.
- Why: let users create folders from the UI and fix common permission errors faster.
- Impact: file browser has a “Create Folder” row and the API adds `/filesystem/folder`; path fields show a chmod/chown tip when the folder isn’t writable.
- Files: src/Readarr.Api.V1/FileSystem/FileSystemController.cs, frontend/src/Components/FileBrowser/FileBrowserModalContent.js, frontend/src/Components/FileBrowser/FileBrowserModalContent.css, frontend/src/Components/FileBrowser/FileBrowserModalContentConnector.js, frontend/src/Components/Form/FormInputGroup.js, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and verify folder creation + permission tip.

## 1.2.38
- Summary: add a downloads folder fallback for remote path mapping and hide device UUID labels in disk space.
- Why: avoid false remote path mapping warnings and show cleaner disk space labels.
- Impact: when no remote path mapping exists, `/downloads` remaps to the configured downloads folder if it exists; disk space labels no longer show `/dev/disk/by-uuid/...`.
- Files: src/NzbDrone.Core/RemotePathMappings/RemotePathMappingService.cs, src/NzbDrone.Common/Disk/DiskProviderBase.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and confirm the qBittorrent warning is gone and disk space shows paths only.

## 1.2.37
- Summary: default recycle bin to Bookdarr home when available.
- Why: keep the recycle folder at the app root (e.g., `/opt/bookdarr-dev/recycle`) instead of under config.
- Impact: if `BOOKDARR_HOME` is set, recycle bin defaults to `BOOKDARR_HOME/recycle`; otherwise uses the app data path.
- Files: src/NzbDrone.Core/MediaFiles/RecycleBinDefaults.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and verify the recycle bin path.

## 1.2.36
- Summary: fix build failure from missing OsInfo using.
- Why: ConfigService now uses OsInfo for default download path on Linux.
- Impact: builds succeed after adding the EnvironmentInfo import.
- Files: src/NzbDrone.Core/Configuration/ConfigService.cs, src/Directory.Build.props, CHANGELOG.md.
- Next: run update-dev.sh and confirm the app builds and starts.

## 1.2.35
- Summary: add configurable downloads folder and default recycle bin path.
- Why: allow changing the download path and avoid missing recycle bin folders.
- Impact: Download Clients options include a downloads folder setting; new remote path mappings default to it; recycle bin defaults to appdata/recycle.
- Files: src/NzbDrone.Core/Configuration/IConfigService.cs, src/NzbDrone.Core/Configuration/ConfigService.cs, src/Readarr.Api.V1/Config/DownloadClientConfigResource.cs, frontend/src/Settings/DownloadClients/Options/DownloadClientOptions.js, frontend/src/Settings/DownloadClients/RemotePathMappings/EditRemotePathMappingModalContentConnector.js, src/NzbDrone.Core/MediaFiles/RecycleBinDefaults.cs, src/NzbDrone.Core/Localization/Core/en.json, src/Directory.Build.props, CHANGELOG.md.
- Next: rebuild and confirm download folder + recycle bin appear in settings and on disk.

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
