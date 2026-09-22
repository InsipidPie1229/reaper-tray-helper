# v1.1.0 release-candidate verification

## Local automated checks

Run `build.cmd` from the repository root. It compiles the application and runs the test assembly before creating packages.

The current candidate passed 18 checks covering:

- valid, optional, corrupt, and hostile XML settings files;
- invalid REAPER and project paths, including a Unicode path with spaces;
- creating, detecting, updating, and removing only this helper's startup shortcut;
- launching REAPER when it is absent;
- attaching without hiding when exactly one configured REAPER is already running; and
- rejecting multiple configured REAPER processes or a process whose executable path cannot be read.

The build host observed `reaper.exe` version 7.80. The Windows Defender command-line scanner reported no threat for the candidate EXE and user ZIP on 2026-09-22, using platform `4.18.26080.4-0` and signatures `1.459.330.0`. This is a point-in-time scan result, not a safety guarantee or an explanation for any browser warning.

## Required independent Windows test

Run this exact checklist on a different Windows 10 or Windows 11 x64 PC using the same ZIP and record the ZIP SHA-256 first.

1. Extract the ZIP to a permanent folder. Do not run it directly from the ZIP.
2. Start `ReaperTrayHelper.exe`, select that PC's installed `reaper.exe`, optionally select a test `.rpp`, enable `Windows 로그인 시 REAPER 자동시작`, and save.
3. Confirm that the current user's Startup folder contains exactly one `REAPER Tray Helper.lnk` pointing to the extracted helper EXE.
4. Sign out and sign back in. Verify that REAPER starts, its main window hides only after it is ready, and the helper tray icon remains available.
5. Double-click the tray icon and use the context menu to restore and hide REAPER. Close the helper and confirm that REAPER becomes visible and stays running.
6. Reopen the helper's settings, disable automatic start, save, and confirm that only `REAPER Tray Helper.lnk` is removed.
7. Test an invalid REAPER path, a missing project, an already-running REAPER, and two configured REAPER processes. The helper must show an error or attach without changing the existing REAPER window.
8. Record the Windows version, REAPER version, ZIP checksum, any exact browser or antivirus warning text, the product that displayed it, and whether each step passed.

## Publication gate

Do not publish a stable `v1.1.0` Release until the independent test passes and the exact prior Chrome warning has been classified as a malware detection or a reputation/download warning. A clearly labelled prerelease may be shared solely for this external test, provided that it states that it is unsigned and that the prior warning is still unclassified. If a malware detection remains, pause the stable release and use the detecting vendor's official false-positive review process. Do not bypass warnings by disabling security tools, password-protecting archives, renaming extensions, or distributing disguised files.
