# Log Retention and Rotation

## App Logs (`/opt/bookdarr-dev/config/logs`)
- Controlled by Settings -> Development -> Logging -> Log Rotation.
- These logs rotate within the config folder.

## Update Logs (`/opt/bookdarr-dev/Logs`)
- Update scripts append logs to `update-XX.log` files.
- Update logs are not rotated automatically.

### Logrotate example (keeps 10 compressed copies)
Create `/etc/logrotate.d/bookdarr-update`:
```
/opt/bookdarr-dev/Logs/update-*.log {
  daily
  rotate 10
  missingok
  notifempty
  compress
  delaycompress
  copytruncate
}
```

### Manual cleanup (keep newest 10)
```
ls -1t /opt/bookdarr-dev/Logs/update-*.log | tail -n +11 | xargs -r sudo rm -f
```
