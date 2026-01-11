# Bookdarr

Bookdarr is a fork of [Bookshelf](https://github.com/pennydreadful/bookshelf)
and [Readarr](https://github.com/Readarr/Readarr). The goal is to provide a
stable, self-hosted book manager with improved metadata options.

Bookdarr is an ebook and audiobook collection manager for Usenet and BitTorrent
users. It can monitor multiple RSS feeds for new books from your favorite
authors and will grab, sort, and rename them. Bookdarr can keep both ebook and
audiobook files for the same book in a single instance.

## Getting Started

The container listens on port 8787 and expects a volume mounted at `/config`.
Docker images will be published under `thashiznit2003/bookdarr`. Until then,
build locally from source.

For download client integration, mount your host download folder to
`/downloads` inside the container (example: `-v /qb1/downloads:/downloads`).

### Metadata

Bookdarr supports two metadata providers:

- Google Books (default): fast, broad coverage, no API key required; shared quota
  without a key and fewer author/series extras.
- BookInfo (`bookinfo.pro`): richer author/series metadata, cover fallbacks, and
  better matching; depends on the BookInfo API and can rate-limit.

Controls:
- Set `GOOGLE_BOOKS_API_KEY` to raise Google Books quota.
- Set `METADATA_PROVIDER=bookinfo` to switch providers.
- Set `METADATA_URL` to override the BookInfo API base URL (default
  `https://api.bookinfo.pro`).

### Install Script (Source Build)

This uses Docker to build from source and logs output to `/opt/bookdarr/install.log`.
By default it only builds the image; redeploy your Portainer stack to start the container.

    sudo mkdir -p /opt/bookdarr && sudo curl -L https://raw.githubusercontent.com/thashiznit2003/Bookdarr/develop/scripts/install-bookdarr.sh -o /opt/bookdarr/install-bookdarr.sh && sudo chmod +x /opt/bookdarr/install-bookdarr.sh && sudo /opt/bookdarr/install-bookdarr.sh

### Docker Compose / Portainer Stack

Use `docker-compose.yml` for Portainer stacks or local compose deployments.
It assumes a locally built image (`bookdarr:local`) and mounts `/downloads`
inside the container for your download client.

To start or redeploy:

    sudo docker compose -f /opt/bookdarr/docker-compose.yml up -d

To rebuild only when needed:

    sudo /opt/bookdarr/install-bookdarr.sh

### Native Dev on Ubuntu (No Docker)

Use this for faster local builds on a dedicated dev VM. It installs Node 20,
Yarn 1.22.19 (via npm), and .NET SDK 10.0.101, clones the repo to
`/opt/bookdarr-dev`, and creates `/opt/bookdarr-dev/config` for AppData.

One-step setup + build + run (foreground):

    sudo curl -L https://raw.githubusercontent.com/thashiznit2003/Bookdarr/develop/scripts/dev-ubuntu.sh -o /opt/bookdarr-dev.sh && sudo bash /opt/bookdarr-dev.sh

If you only want to build (no run):

    sudo RUN_APP=false bash /opt/bookdarr-dev.sh

You can still use the individual scripts afterward:

    sudo -u joe /opt/bookdarr-dev/scripts/dev-build.sh
    sudo -u joe /opt/bookdarr-dev/scripts/dev-run.sh

### Systemd (Linux only)

Use the bundled unit file and install/uninstall steps in
`docs/LINUX_SYSTEMD.md`.

### Log Retention (Dev VM)

Update logs live in `/opt/bookdarr-dev/Logs` and are not rotated automatically.
See `docs/LOGGING.md` for a logrotate example and cleanup guidance.

## Support

This project won't use Discord for support. If you have a problem, please file
an issue or start a discussion.

## Contributors & Developers

Help is very welcome. Priority is on fixing quality of life issues

- [ ] Monitor series.
- [ ] Hardcover bookshelf import.
- [x] Support ebook and audio files in the same root.

### License

This is a derivative work of the [Readarr](https://github.com/Readarr/Readarr)
and [Prowlarr](https://github.com/Prowlarr/Prowlarr) projects which are both
licensed [GPLv3](http://www.gnu.org/licenses/gpl.html). This project is
therefore also licensed under the terms of GPLv3.

Copyright 2025
