# Bookdarr

Bookdarr is a fork of [Bookshelf](https://github.com/pennydreadful/bookshelf)
and [Readarr](https://github.com/Readarr/Readarr). The goal is to provide a
stable, self-hosted book manager with improved metadata options.

Bookdarr is an ebook and audiobook collection manager for Usenet and BitTorrent
users. It can monitor multiple RSS feeds for new books from your favorite
authors and will grab, sort, and rename them. Note that only one type of a
given book is supported. If you want both an audiobook and ebook of a given
book you will need multiple instances.

## Getting Started

The container listens on port 8787 and expects a volume mounted at `/config`.
Docker images will be published under `thashiznit2003/bookdarr`. Until then,
build locally from source.

## Support

This project won't use Discord for support. If you have a problem, please file
an issue or start a discussion.

## Contributors & Developers

Help is very welcome. Priority is on fixing quality of life issues

- [ ] Monitor series.
- [ ] Hardcover bookshelf import.
- [ ] Support ebook and audio files in the same root.

Already done

- [x] Native support for MyAnonaMouse without Prowlarr.
- [x] Lowered default match percentage from 80% to 50%.
- [x] Metadata is no longer cached locally.
- [x] Removed servarr analytics spyware.
- [x] Supports selfhosted metadata (UI or `METADATA_URL` env var).

## Sponsors

If you ever donated to [this](https://opencollective.com/readarr) project you
should request a refund. Those people don't deserve your money.

### License

This is a derivative work of the [Readarr](https://github.com/Readarr/Readarr)
and [Prowlarr](https://github.com/Prowlarr/Prowlarr) projects which are both
licensed [GPLv3](http://www.gnu.org/licenses/gpl.html). This project is
therefore also licensed under the terms of GPLv3.

Copyright 2025
