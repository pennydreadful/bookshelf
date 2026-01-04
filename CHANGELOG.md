# Changelog

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
