# REAPER Tray Helper

REAPER Tray Helper is an unofficial Windows companion for launching REAPER, hiding its window in the system tray, and muting individual tracks with global keyboard shortcuts.

This project is independent of Cockos and is not approved, endorsed, or supported by Cockos or REAPER. The REAPER application, its license, plug-ins, settings, and project files are not included. Install REAPER separately and use it under its own license.

## Download

The public GitHub release is available without a GitHub account. [Open the latest release page](https://github.com/InsipidPie1229/reaper-tray-helper/releases) and download `ReaperTrayHelper-1.2.0.zip` under **Assets**. Do not download `-Source.zip` unless you want the source code. Extract the ZIP before running the EXE. See [README.ko.md](README.ko.md) for Korean instructions.

## Features

- Start REAPER and optionally open a selected project.
- Hide and restore REAPER from the Windows notification area.
- Register one global shortcut for each named track in the active REAPER project.
- Choose Automatic, Korean, or English for the helper interface.

## Requirements

- Windows 10 or 11, 64-bit
- .NET Framework 4.8 or a compatible .NET Framework 4.x runtime
- A separate REAPER installation

This release is not code-signed. The user reports that Chrome showed a warning when the file was shared through a Discord DM, while downloading from the GitHub Release worked normally and the warning is no longer appearing. This supports treating the issue as resolved for the GitHub download route; it does not prove what triggered the original warning because its exact text and the tested file hash were not saved. Chrome can warn about dangerous files as well as uncommon or unfamiliar downloads ([Chrome Help](https://support.google.com/chrome/answer/6261569)); Windows SmartScreen also considers app reputation ([Microsoft Support](https://support.microsoft.com/en-us/office/protect-my-pc-from-viruses)). Do not ignore a future malware warning. SHA-256 confirms file identity only; it does not prove safety.

## Install and configure

1. Download the user ZIP from Releases and extract the entire ZIP into a folder you will keep. The folder should contain `ReaperTrayHelper.exe`, `ReaperTrayHelper_ToggleTrackMute.lua`, `README.md`, `README.ko.md`, and `LICENSE`.
2. Run `ReaperTrayHelper.exe`. Choose the installed `reaper.exe`. You may also choose a project file ending in `.rpp`; leave that field empty to use REAPER's existing startup behavior.
3. Choose **Language** in Settings. **Automatic (Windows)** uses Korean when the Windows display language is Korean and English otherwise. You can select **한국어** or **English** at any time.
4. To start REAPER when you sign in to Windows, select **Start REAPER when I sign in to Windows** and save. This creates a shortcut in the current Windows account's Startup folder.

The helper saves its settings in `%LOCALAPPDATA%\ReaperTrayHelper\settings.xml`. Changing the REAPER or project path takes effect the next time the helper starts. The language and global shortcuts take effect when you save.

## Set up global track mute shortcuts

The OSC and Lua connection is a one-time setup. Keep both REAPER and REAPER Tray Helper running during setup.

### 1. Add an OSC control surface in REAPER

1. In REAPER, open **Options → Preferences…**. Select **Control/OSC/web** from the left side, then click **Add**.
2. Choose **OSC (Open Sound Control)** and confirm.
3. In the OSC settings, set **Mode** to **Configure device IP+local port**. The top **Control surface mode** should say **OSC (Open Sound Control)**.
4. Enter these values:

   | OSC setting | Value |
   | --- | --- |
   | Pattern configuration | `Default.ReaperOSC` |
   | Device IP | `127.0.0.1` |
   | Device port | `9001` |
   | Local listen port | The helper's **REAPER OSC local listen port** (default `8000`) |

   The REAPER **Local listen port** and the helper's **REAPER OSC local listen port** must use the same number. If another OSC device already uses `8000`, choose another port from `1024` to `65535` in both places.
5. Confirm the OSC settings, then click **Apply** or **OK** in Preferences. Make sure the OSC surface remains in the control-surface list.

### 2. Load the included Lua script

1. In REAPER, open **Actions → Show action list…**. You can also press `?` to open the Action List.
2. In the Action List window, click **New action…** near the bottom and choose **Load ReaScript…**. The load command is inside the **New action…** button menu, not the main Actions menu.
3. Browse to the folder where you extracted the helper ZIP. Select `ReaperTrayHelper_ToggleTrackMute.lua`, which is in the same folder as `ReaperTrayHelper.exe`, and open it. Do not edit the Lua file.
4. The Action List should show an action named `ReaperTrayHelper_ToggleTrackMute.lua`. Search for `ToggleTrackMute` if needed.
5. Right-click that action and choose **Copy selected action command ID**. The ID normally starts with `_RS` and is copied to the clipboard.

### 3. Test the connection

1. Open the helper's Settings from its tray icon. The icon may be under the `^` hidden-icons button at the right side of the taskbar.
2. Confirm that **REAPER OSC local listen port** matches REAPER's **Local listen port**. The default is `8000`.
3. Click **ReaScript command ID** and paste the copied ID with `Ctrl+V`.
4. Click **Test OSC connection**. The message **Connected to the REAPER Lua script.** confirms that the connection works.
5. Add the track shortcuts below and click **Save**.

### 4. Assign shortcuts to tracks

1. In Settings, click **Add** below the track shortcut list.
2. Enter the track name exactly as it appears in REAPER's track panel. Spaces and Korean characters are part of the name. For example, enter `MIC` for a track named `MIC`.
3. Click the shortcut field and press the key combination you want. A shortcut must include at least one of `Ctrl`, `Alt`, or `Shift` plus a regular key. For example, press `Ctrl+Alt+1`. `F12` and some reserved keys are unavailable.
4. Confirm that the track and shortcut appear in the list, then click **Save**. Repeat for other tracks. You cannot assign the same shortcut twice or assign more than one shortcut to the same track.
5. Test with a project where the target track name appears exactly once. Pressing its shortcut toggles only that track. A notification shows whether it is muted or unmuted.

### Troubleshooting

- **The OSC connection test times out:** Make sure REAPER is running, the OSC surface is present in Preferences, and both local listen port fields use the same number.
- **The command ID is rejected:** Copy it again by right-clicking the Lua action and choosing **Copy selected action command ID**. Do not type the script name in place of the ID.
- **A shortcut cannot be saved:** Windows or another application may already use that combination. Choose another combination. If registration fails, the previously active shortcuts are kept.
- **The wrong track does not change or an error appears:** Make sure the helper is running, the intended project tab is active, and the track name matches exactly and appears only once in that project.

Shortcuts work while the helper is running, including when another application is in front or REAPER is hidden in the tray. They do not work after REAPER exits. This feature controls REAPER track mute only; it does not mute microphone signal paths outside REAPER.

## Use the tray helper

- A newly launched REAPER window is hidden about two seconds after its main window becomes ready.
- Double-click the helper's tray icon to show or hide REAPER.
- Right-click the tray icon to show REAPER, hide it, open Settings, or exit the helper.
- Exiting the helper restores a hidden REAPER window and leaves REAPER running.
- If REAPER was already running, the helper attaches to it without opening another project or hiding its window.
- To disable auto-start, clear **Start REAPER when I sign in to Windows** in Settings and save.

## Remove the helper

In Settings, delete all track shortcuts, clear the auto-start option, and save. In REAPER Preferences, remove the helper's OSC control surface; in the Action List, remove the helper Lua action. Exit the helper from its tray menu, then delete the extracted folder and `%LOCALAPPDATA%\ReaperTrayHelper`.

## Limitations

- If more than one copy of the configured REAPER is running, or its process path cannot be verified, the helper reports an error rather than choosing one at random.
- The helper does not automatically close trial, error, or save-confirmation dialogs.
- Track names must match exactly in the active project. If a name occurs more than once, no track is changed.
- The helper does not control direct microphone paths or audio routing outside REAPER. REAPER audio processing may continue while its window is hidden.

## Build and verification

On Windows, run `build.cmd` to compile the helper, run automated checks, create the user and source ZIPs, and write SHA-256 checksums under `artifacts`. See [TESTING.md](TESTING.md) for the current verification status.

## License and trademarks

The helper source and binaries are provided under the [MIT License](LICENSE). The license permits commercial use, sale, modification, and redistribution when the required copyright and license notices are kept. It does not grant rights to Cockos or REAPER trademarks.

REAPER is a trademark of Cockos Incorporated. “REAPER Tray Helper” is an independent project and does not imply endorsement or affiliation.
