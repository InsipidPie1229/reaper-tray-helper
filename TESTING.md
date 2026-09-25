# v1.1.0-rc.2 release-candidate verification

## Local automated checks

Run `build.cmd` from the repository root. It compiles the application and runs the test assembly before creating packages.

The current candidate passed 37 automated checks covering:

- valid, optional, corrupt, and hostile XML settings files;
- invalid REAPER and project paths, including a Unicode path with spaces;
- creating, detecting, updating, and removing only this helper's startup shortcut;
- launching REAPER when it is absent;
- attaching without hiding when exactly one configured REAPER is already running; and
- rejecting multiple configured REAPER processes or a process whose executable path cannot be read;
- settings round trip with Unicode track names and loading the previous settings format;
- independent per-track key assignments, duplicate key/name rejection, reserved-key rejection, and empty defaults; and
- OSC action packet address, string argument, and command ID validation.

The build host has REAPER 7.80 installed. On 2026-09-25, Windows Defender's command-line scanner reported no threat for the candidate EXE and both ZIP files. The PowerShell Defender status query was denied, so the current engine and signature versions could not be recorded. This is a point-in-time scan result, not a safety guarantee or an explanation for any browser warning.

## Required independent Windows test

Run this exact checklist on a different Windows 10 or Windows 11 x64 PC using the same ZIP and record the ZIP SHA-256 first.

1. Extract the ZIP to a permanent folder. Do not run it directly from the ZIP.
2. Start `ReaperTrayHelper.exe`, select that PC's installed `reaper.exe`, optionally select a test `.rpp`, enable `Windows 로그인 시 REAPER 자동시작`, and save.
3. In REAPER Preferences, add an OSC surface using `Default.ReaperOSC`, device IP `127.0.0.1`, device port `9001`, and local port `8000` (or the helper's configured port). Load the included Lua script in Actions, copy its command ID, paste it into helper settings, and confirm the connection test.
4. Confirm that the current user's Startup folder contains exactly one `REAPER Tray Helper.lnk` pointing to the extracted helper EXE.
5. Create a disposable project with uniquely named tracks `MIC 한글` and `MUSIC`. Assign `Ctrl+Alt+D1` and `Ctrl+Alt+D2`; use Notepad as the foreground app and verify each chord changes only its assigned track. Verify the tray notification matches the resulting mute state.
6. Open a second project tab with a matching track name and confirm the active tab receives the command. Test missing and duplicate target names; neither case may change any track.
7. Test a conflicting OS hotkey and a duplicate binding. Saving must show an error and preserve the previously working registration. Delete a binding and verify that it no longer fires.
8. Sign out and back in. Verify REAPER starts, hides only after it is ready, and hotkeys work while REAPER is hidden. Restore and hide with the tray menu, then exit the helper and confirm REAPER remains visible and running.
9. Disable automatic start and confirm only `REAPER Tray Helper.lnk` is removed. Test an invalid REAPER path, missing project, an already-running REAPER, and multiple REAPER processes.
10. Record Windows and REAPER versions, ZIP SHA-256, any exact browser/antivirus warning and its product, and pass/fail for each step.

## Publication gate

Do not publish a stable `v1.1.0` Release until the independent test passes and the exact prior Chrome warning has been classified as a malware detection or a reputation/download warning. A clearly labelled prerelease may be shared solely for this external test, provided that it states that it is unsigned and that the prior warning is still unclassified. If a malware detection remains, pause the stable release and use the detecting vendor's official false-positive review process. Do not bypass warnings by disabling security tools, password-protecting archives, renaming extensions, or distributing disguised files.
