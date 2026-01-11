# Bookdarr Systemd (Linux Only)

This guide is for Linux installs that run Bookdarr directly (no Docker).

## Install
1. Create a service user and folders:
   - `sudo useradd --system --home /opt/bookdarr-dev --shell /usr/sbin/nologin bookdarr`
   - `sudo mkdir -p /opt/bookdarr-dev/config`
   - `sudo chown -R bookdarr:bookdarr /opt/bookdarr-dev`
2. Build Bookdarr so the binary exists under `/opt/bookdarr-dev/_output`.
3. Install the unit file:
   - `sudo install -m 644 /opt/bookdarr-dev/systemd/bookdarr.service /etc/systemd/system/bookdarr.service`
4. Optional overrides in `/etc/default/bookdarr`:
   - `BOOKDARR_HOME=/opt/bookdarr-dev`
   - `BOOKDARR_DATA_DIR=/opt/bookdarr-dev/config`
   - `BOOKDARR_BIN=/opt/bookdarr-dev/_output/net10.0/linux-x64/Readarr`
   - `RID=linux-x64`
5. Enable and start:
   - `sudo systemctl daemon-reload`
   - `sudo systemctl enable --now bookdarr`

## Update
1. Stop the service:
   - `sudo systemctl stop bookdarr`
2. Run the update script and capture logs.
3. Start the service:
   - `sudo systemctl start bookdarr`

## Uninstall
1. Stop and disable:
   - `sudo systemctl disable --now bookdarr`
2. Remove the unit file and reload:
   - `sudo rm -f /etc/systemd/system/bookdarr.service`
   - `sudo systemctl daemon-reload`

## Logs
- View service logs: `journalctl -u bookdarr -f`
