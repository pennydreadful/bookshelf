# .NET 10 Upgrade Plan (Bookdarr)

This plan targets the Linux/Ubuntu VM install (no Docker yet) and focuses on reducing update-time failures. It is written to be executed later when you are ready.

## Scope
- Linux/Ubuntu VM only.
- Update flow uses `sudo /opt/bookdarr-dev/scripts/update-dev.sh`.
- No Docker changes required yet (but Docker items are listed for later follow-up).

## Preflight
1. Confirm current version and branch.
2. Create a snapshot tag before any upgrade changes.
3. Record update output to a log file for review.

Example update command (always capture logs):
```
sudo /opt/bookdarr-dev/scripts/update-dev.sh 2>&1 | tee /opt/bookdarr-dev/Logs/update-0XX.log
```

## Plan (high level)
1. Update targets to .NET 10 across the codebase.
2. Pin the SDK via `global.json`.
3. Update install/build/update scripts to install .NET 10.0.1.
4. Update CI configuration to use .NET 10.
5. Run the full build and update the VM with logging.
6. Validate the app starts and status is healthy.

## Step-by-step (detailed)
1. Tag a snapshot (required before changes):
   - `git tag snapshot-YYYYMMDD-HHMM`
   - `git push origin snapshot-YYYYMMDD-HHMM`

2. Update project targets to .NET 10:
   - Change `TargetFramework` / `TargetFrameworks` in `src/**/*.csproj` from `net6.0` to `net10.0`.
   - If any projects use a `net6.0`-specific condition, update the condition accordingly.
   - If `RuntimeIdentifiers` or publish settings are tied to `net6.0`, update them to `net10.0`.

3. Add/Update `global.json`:
   - Pin SDK version to `10.0.1`.
   - Use `rollForward` set to `latestPatch` (or `latestFeature` if you want automatic minor roll-forward).
   - Example:
     ```
     {
       "sdk": {
         "version": "10.0.1",
         "rollForward": "latestPatch"
       }
     }
     ```

4. Update VM install/build/update scripts:
   - `scripts/dev-ubuntu.sh`
   - `scripts/dev-setup-ubuntu.sh`
   - `scripts/dev-build.sh`
   - `scripts/update-dev.sh`
   - Replace any .NET 6 installs with .NET 10.0.1.
   - Ensure `/usr/share/dotnet` is on PATH (if the script sets PATH explicitly).

5. Update CI configs:
   - `azure-pipelines.yml`: ensure the .NET 10 SDK is installed and used.
   - Any GitHub Actions workflows (if present): update `setup-dotnet` to `10.0.1`.

6. Update Docker files (for later use):
   - `docker/*` and `docker-compose.yml` should reference .NET 10 base images when Docker migration happens.
   - This is optional now, but should be part of the upgrade branch before Docker rollout.

7. Full build (StyleCop check):
   - Run `scripts/dev-build.sh` and fix any SA/IDE warnings that surface.

8. Update on VM and capture log:
   - Run the update command with `tee` to create `update-0XX.log`.

9. Validate service start:
   - `curl http://localhost:8787/api/v1/system/status`

## Update-time failure points and fixes
- "SDK not found" or "No .NET SDKs were found":
  - Install .NET 10.0.1 SDK on the VM.
  - Verify with `dotnet --list-sdks`.

- "The framework 'Microsoft.NETCore.App', version '10.0.0' was not found":
  - Install the .NET 10 runtime on the VM.
  - Confirm `dotnet --list-runtimes`.

- Build errors after framework change:
  - Update NuGet packages for .NET 10 compatibility.
  - Re-run `scripts/dev-build.sh` to confirm StyleCop and build success.

## Rollback (using snapshots)
- Use the snapshot tag created before the upgrade:
  - `git fetch --tags`
  - `git checkout snapshot-YYYYMMDD-HHMM`
  - Run the update script to rebuild and restart.

## Notes
- Keep the update log for each attempt and share it if a build fails.
- Keep one change at a time (framework targets first, then scripts, then CI).
