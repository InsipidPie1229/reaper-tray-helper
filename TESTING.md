# v1.2.0-rc.1 release-candidate verification

## External availability and user report

The repository and `v1.2.0-rc.1` release are public; anyone can view and download the assets without a GitHub account. The release remains marked as a prerelease.

The user reports that the helper worked stably on a separate Windows PC and that the previously seen Chrome warning no longer appears. The Windows and REAPER versions, exact ZIP SHA-256 used for that test, detailed checklist results, and original Chrome warning text were not recorded. Treat this as a positive user report, not as a fully reproducible test record or a security scan. The current files are unsigned.

## Local automated checks

Run `build.cmd` from the repository root. It compiles the application, runs the test assembly, creates the user and source ZIPs, and writes `SHA256SUMS.txt` under `artifacts`.

The current candidate passed 46 automated checks covering:

- valid, optional, corrupt, hostile XML settings, Unicode paths, previous settings without language/hotkey fields, and language persistence;
- Automatic language selection for Korean and non-Korean Windows cultures, manual overrides, localized validation, and matching English/Korean translation keys;
- creating, detecting, updating, and removing only this helper's startup shortcut;
- independent global track shortcuts, duplicate and reserved keys, failed-registration rollback, and unregistering removed shortcuts;
- OSC action packet and ReaScript command ID validation; and
- REAPER launch/attach selection, multiple process rejection, and unreadable process paths.

The previous `v1.1.0-rc.2` EXE and ZIPs were scanned with Windows Defender on 2026-09-25 and reported no threat at that time. That scan does not cover this `v1.2.0-rc.1` candidate. The Defender engine/signature version and original Chrome warning text remain unverified. The user says the warning is no longer appearing, but no new security scan or vendor classification is available. Do not describe these files as security-scanned or as certified safe. This is not a safety guarantee.

## Package review

The user ZIP contains only the helper EXE, Lua script, English and Korean README files, and MIT `LICENSE`. The source ZIP contains source and build/test documentation and no EXE, DLL, or installer script. The package script checks for known personal paths and licensed REAPER content. SHA-256 values identify the exact files but do not certify safety.

## Independent Windows test checklist

Use this checklist when recording a future repeat test. The user has reported a successful separate-PC use, but the details below were not individually recorded.

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

## Stable-release status

Keep this candidate marked as a prerelease until a release-grade record identifies the exact tested asset by SHA-256, records the Windows/REAPER versions and key checklist results, and captures enough information about the original Chrome warning to establish whether it was a reputation warning or a malware detection. If a malware detection is reported, pause stable publication and use the detecting vendor's official review process. Do not bypass warnings by disabling security tools, password-protecting archives, renaming extensions, or disguising files.
