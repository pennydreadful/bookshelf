# Bookdarr Checklist

- [ ] Add diagnostics flow: create `bookdarr-diagnostics` repo and add a UI button to collect logs/config and push a sanitized bundle.
- [ ] Publish Docker images to Docker Hub (`thashiznit2003/bookdarr`) with release tagging and compose instructions.
- [ ] Implement Hardcover fallback provider and add provider selection/priority settings.
- [ ] Add an export indexers flow (API/UI or script) for easy migration.
- [ ] Support dual-format storage (ebook + audiobook) without deleting the other file.
- [ ] Build a request UI (Overseerr-like) on top of Bookdarr search.
- [ ] Document metadata provider pros/cons in README and settings UI help.
- [ ] Add migration guidance and warnings before any change that could lose data.
