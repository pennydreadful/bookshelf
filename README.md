# bookshelf

This is a revival of [Readarr](https://github.com/Readarr/Readarr). The images
published are configured to use working Goodreads or Hardcover metadata out of
the box.

Bookshelf is an ebook and audiobook collection manager for Usenet and BitTorrent
users. It can monitor multiple RSS feeds for new books from your favorite
authors and will grab, sort, and rename them. Note that only one type of a
given book is supported. If you want both an audiobook and ebook of a given
book you will need multiple instances.

## Getting Started

The container listens on port 8787 and expects a volume mounted at `/config`.

    docker run -p 8787:8787 -v ~/.config/bookshelf:/config ghcr.io/pennydreadful/bookshelf:hardcover

The `softcover` tags use [Goodreads](https://www.goodreads.com) as the metadata
provider. The quality of this metadata is generally poor and contains a lot of
slop. However, it is backward-compatible with existing Readarr databases and
functionality like Goodreads list imports should continue to work normally.

The `hardcover` tags use [Hardcover](https://hardcover.app/home) as a metadata
provider. This metadata is higher quality but isn't backward-compatible; if
you're already running Readarr you'll need to redeploy this from scratch.
Goodreads list imports haven't been tested and likely don't work.

## Support

This project won't use Discord for support. If you have a problem please file
an issue or start a discussion.

## Contributors & Developers

Help is very welcome. Priority is on fixing quality of life issues

- [ ] Monitor series.
- [ ] Hardcover bookshelf import.
- [ ] Support ebook and audio files in the same root.

Already done

- [x] Lowered default match percentage from 80% to 50%.
- [x] Metadata is no longer cached locally.
- [x] Removed servarr analytics spyware.
- [x] Supports selfhosted metadata (UI or `METADATA_URL` env var).

## Sponsors

If you ever donated to [this](https://opencollective.com/readarr) project you
should request a refund. Those people don't deserve your money.

### License

* [GNU GPL v3](http://www.gnu.org/licenses/gpl.html)
* Copyright 2025
