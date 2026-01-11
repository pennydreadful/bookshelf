# Bookdarr Checklist

- [x] Add diagnostics flow: create `bookdarr-diagnostics` repo and add a UI button to collect logs/config and push a sanitized bundle.
- [ ] Add an export indexers flow (API/UI or script) for easy migration.
- [ ] Build a request UI (Overseerr-like) on top of Bookdarr search.
- [ ] Explore multi-user support (auth, permissions, per-user views).
- [x] Document metadata provider pros/cons in README and settings UI help.
- [x] Add .NET upgrade plan: move to `net8.0` LTS now and `net10.0` when available; update Docker/base images, CI, and VM scripts accordingly.
- [x] Add `global.json` to pin the .NET SDK version used in dev/CI.
- [x] Refresh dependency versions for net8+ compatibility (NuGet + Node/Yarn toolchain).
- [ ] Add dependency security automation (Dependabot/Renovate) and code scanning (CodeQL or equivalent).
- [ ] Add Linux CI job that runs the same build scripts as the VM (`scripts/dev-build.sh`) to catch update failures early.
- [x] Add systemd unit file and install/uninstall docs for Linux-only deployments.
- [x] Add log retention/rotation guidance for `/opt/bookdarr-dev/Logs`.
- [ ] Publish Docker images to Docker Hub (`thashiznit2003/bookdarr`) with release tagging and compose instructions.
