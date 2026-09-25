# v1.2.0-rc.1 release-candidate verification

## Local automated checks

Run `build.cmd` from the repository root. It compiles the application, runs the test assembly, creates the user and source ZIPs, and writes `SHA256SUMS.txt` under `artifacts`.

The current candidate passed 46 automated checks covering:

- valid, optional, corrupt, hostile XML settings, Unicode paths, previous settings without language/hotkey fields, and language persistence;
- Automatic language selection for Korean and non-Korean Windows cultures, manual overrides, localized validation, and matching English/Korean translation keys;
- creating, detecting, updating, and removing only this helper's startup shortcut;
- independent global track shortcuts, duplicate and reserved keys, failed-registration rollback, and unregistering removed shortcuts;
- OSC action packet and ReaScript command ID validation; and
- REAPER launch/attach selection, multiple process rejection, and unreadable process paths.

The previous `v1.1.0-rc.2` EXE and ZIPs were scanned with Windows Defender on 2026-09-25 and reported no threat at that time. That scan does not cover this `v1.2.0-rc.1` candidate. The Defender engine/signature version and the original Chrome warning remain unverified, so do not describe the new files as security-scanned or as having resolved the prior warning. This is not a safety guarantee.

## Package review

The user ZIP contains only the helper EXE, Lua script, English and Korean README files, and MIT `LICENSE`. The source ZIP contains source and build/test documentation and no EXE, DLL, or installer script. The package script checks for known personal paths and licensed REAPER content. SHA-256 values identify the exact files but do not certify safety.

## Required independent Windows test

Run this checklist on a different Windows 10 or Windows 11 x64 PC using the exact user ZIP from this candidate. Record its SHA-256 first.

1. Extract the ZIP to a permanent folder. Do not run it directly from the ZIP.
2. Start `ReaperTrayHelper.exe`, select that PC's installed `reaper.exe`, optionally select a test `.rpp`, and save. Switch the Language selector among Automatic, Korean, and English; restart the helper and confirm the selected language persists.
3. In REAPER Preferences, add an OSC surface using `Default.ReaperOSC`, device IP `127.0.0.1`, device port `9001`, and local port `8000` (or the helper's configured port). Load the included Lua script in Actions, copy its command ID, paste it into helper settings, and confirm the connection test.
4. Confirm that the current user's Startup folder contains exactly one `REAPER Tray Helper.lnk` pointing to the extracted helper EXE.
5. Create a disposable project with uniquely named tracks `MIC 한글` and `MUSIC`. Assign `Ctrl+Alt+D1` and `Ctrl+Alt+D2`; use Notepad as the foreground app and verify each chord changes only its assigned track. Verify the tray notification matches the resulting mute state.
6. Open a second project tab with a matching track name and confirm the active tab receives the command. Test missing and duplicate target names; neither case may change any track.
7. Test a conflicting OS hotkey and a duplicate binding. Saving must show an error and preserve the previously working registration. Delete a binding and verify that it no longer fires.
8. Sign out and back in. Verify REAPER starts, hides only after it is ready, and hotkeys work while REAPER is hidden. Restore and hide with the tray menu, then exit the helper and confirm REAPER remains visible and running.
9. Disable automatic start and confirm only `REAPER Tray Helper.lnk` is removed. Test an invalid REAPER path, missing project, an already-running REAPER, and multiple REAPER processes.
10. Record Windows and REAPER versions, ZIP SHA-256, any exact browser/antivirus warning and its product, and pass/fail for each step.

## Publication gate

This candidate is a prerelease for review and testing, unsigned, and is not a stable release. Do not publish a stable release until the independent test passes and the exact prior Chrome warning has been classified as a malware detection or a reputation/download warning. If a malware detection remains, pause stable publication and use the detecting vendor's official review process. Do not bypass warnings by disabling security tools, password-protecting archives, renaming extensions, or disguising files.
