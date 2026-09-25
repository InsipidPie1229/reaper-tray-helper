using System;
using System.Collections.Generic;
using System.Globalization;

namespace ReaperTrayHelper
{
    internal static class UiText
    {
        internal const string Automatic = "auto";
        internal const string Korean = "ko";
        internal const string English = "en";

        private static readonly Dictionary<string, string> EnglishText = new Dictionary<string, string>
        {
            { "settings_title", "REAPER Tray Helper Settings" },
            { "settings_menu", "Settings (path changes apply after restart)" },
            { "open_reaper", "Show REAPER" },
            { "hide_reaper", "Hide REAPER" },
            { "exit_helper", "Exit tray helper" },
            { "tray_tooltip", "REAPER Tray Helper - double-click to show or hide" },
            { "startup_shortcut_description", "Starts REAPER Tray Helper for the current Windows account." },
            { "start_failed", "Could not start REAPER." },
            { "process_path_unreadable", "Could not verify the path of a running REAPER process. Make sure REAPER and the helper run with the same permissions." },
            { "multiple_processes", "More than one copy of the selected REAPER is running. Close the extra copy and try again." },
            { "helper_file_missing", "The helper executable could not be found." },
            { "wsh_unavailable", "Windows Script Host is not available." },
            { "invalid_reaper_path", "Select the installed REAPER executable (reaper.exe)." },
            { "invalid_project_path", "Select a valid REAPER project (.rpp), or leave the project field empty." },
            { "invalid_osc_port", "The REAPER OSC port must be between 1024 and 65535." },
            { "missing_command_id", "Enter the ReaScript command ID to use track hotkeys." },
            { "settings_corrupt", "The existing settings file could not be read. The helper will ask you to set it up again." },
            { "stored_hotkey_conflict", "A saved global hotkey could not be registered. Open Settings and choose a different shortcut." },
            { "startup_note", "Auto-start creates a shortcut in this Windows account's Startup folder.\nLeave the project field empty to use REAPER's existing startup behavior." },
            { "hotkeys_heading", "Global track mute shortcuts (active REAPER project)" },
            { "column_track", "Track name" },
            { "column_shortcut", "Shortcut" },
            { "add", "Add" },
            { "edit", "Edit" },
            { "delete", "Delete" },
            { "osc_port_label", "OSC local listen port (Default.ReaperOSC, device IP 127.0.0.1, device port 9001)" },
            { "command_id_label", "ReaScript command ID" },
            { "command_id_hint", "Load the included Lua script in Actions, then copy its command ID here." },
            { "connection_test", "Test OSC connection" },
            { "save", "Save" },
            { "cancel", "Cancel" },
            { "reaper_path_label", "REAPER application (reaper.exe)" },
            { "project_path_label", "Project to open (.rpp) - optional" },
            { "browse", "Browse..." },
            { "auto_start", "Start REAPER when I sign in to Windows" },
            { "hotkey_track_label", "REAPER track name (match spelling and spaces exactly)" },
            { "hotkey_chord_label", "Shortcut (press Ctrl/Alt/Shift with a key)" },
            { "confirm", "OK" },
            { "enter_track_name", "Enter a REAPER track name." },
            { "enter_shortcut", "Choose a key combination that includes Ctrl, Alt, or Shift." },
            { "hotkey_track_required", "Enter a REAPER track name for every shortcut." },
            { "hotkey_modifier_required", "Each shortcut must include Ctrl, Alt, or Shift." },
            { "hotkey_unsupported", "F12 and unsupported keys cannot be used for shortcuts." },
            { "duplicate_shortcut", "The same shortcut is assigned more than once." },
            { "duplicate_track", "A track name can only have one shortcut. Remove the duplicate assignment." },
            { "registration_failed", "Could not register shortcut {0}. Another app may already use it, or Windows may reserve it. (Error {1})" },
            { "keep_old_hotkeys", "The previous shortcuts are still active." },
            { "restore_hotkeys_failed", "The previous shortcuts could not be restored. Restart the helper." },
            { "copy_command_id", "Copy the ReaScript command ID from the Actions list and enter it here." },
            { "osc_connected", "Connected to the REAPER Lua script." },
            { "osc_failed", "Could not connect. Check the REAPER OSC port and ReaScript command ID." },
            { "choose_reaper", "Select REAPER executable" },
            { "choose_project", "Select REAPER project" },
            { "project_filter", "REAPER project|*.rpp" },
            { "startup_verify_failed", "Could not verify the auto-start shortcut." },
            { "settings_save_failed", "Could not save settings." },
            { "hotkey_setup_first", "Set up the REAPER Lua script command ID and OSC connection first." },
            { "muted", "Track muted" },
            { "unmuted", "Track unmuted" },
            { "no_active_project", "There is no active REAPER project." },
            { "track_not_found", "Track '{0}' was not found in the active project." },
            { "track_name_duplicated", "The active project has more than one track named '{0}'. Give the track a unique name." },
            { "reaper_timeout", "REAPER did not respond. Check the OSC port and script command ID." },
            { "toggle_failed", "Could not toggle the REAPER track. ({0})" },
            { "hotkey_error", "REAPER mute shortcut error: {0}" },
            { "startup_error", "REAPER Tray Helper could not start." },
            { "script_response", "Script response: {0}" },
            { "invalid_command_id", "Invalid ReaScript command ID." },
            { "language_label", "Language" },
            { "language_auto", "Automatic (Windows)" },
            { "language_korean", "한국어" },
            { "language_english", "English" }
        };

        private static readonly Dictionary<string, string> KoreanText = new Dictionary<string, string>
        {
            { "settings_title", "REAPER 자동시작 도우미 설정" },
            { "settings_menu", "설정 (경로 변경은 다음 실행부터 적용)" },
            { "open_reaper", "REAPER 열기" },
            { "hide_reaper", "REAPER 숨기기" },
            { "exit_helper", "트레이 도우미 종료" },
            { "tray_tooltip", "REAPER Tray Helper - 더블 클릭으로 열기/숨기기" },
            { "startup_shortcut_description", "현재 Windows 계정에서 REAPER Tray Helper를 시작합니다." },
            { "start_failed", "REAPER 프로세스를 시작하지 못했습니다." },
            { "process_path_unreadable", "실행 중인 REAPER의 경로를 확인할 수 없습니다. 같은 권한으로 실행 중인지 확인하세요." },
            { "multiple_processes", "설정한 REAPER가 여러 개 실행 중입니다. 하나만 남긴 뒤 다시 실행하세요." },
            { "helper_file_missing", "도우미 실행 파일을 찾을 수 없습니다." },
            { "wsh_unavailable", "Windows Script Host를 사용할 수 없습니다." },
            { "invalid_reaper_path", "설치된 REAPER의 reaper.exe 파일을 선택하세요." },
            { "invalid_project_path", "사용할 .rpp 프로젝트를 선택하거나 프로젝트 칸을 비워 두세요." },
            { "invalid_osc_port", "REAPER OSC 수신 포트는 1024~65535 사이여야 합니다." },
            { "missing_command_id", "트랙 단축키를 사용하려면 ReaScript 명령 ID를 입력하세요." },
            { "settings_corrupt", "기존 설정 파일을 읽지 못해 다시 설정합니다." },
            { "stored_hotkey_conflict", "저장된 전역 단축키를 등록하지 못했습니다. 설정을 열어 충돌하는 키를 수정하세요." },
            { "startup_note", "자동시작은 현재 Windows 계정의 시작프로그램 바로가기로 등록됩니다.\n프로젝트 경로를 비워 두면 REAPER의 기존 시작 설정을 사용합니다." },
            { "hotkeys_heading", "트랙별 전역 음소거 단축키 (현재 활성 REAPER 프로젝트)" },
            { "column_track", "트랙 이름" },
            { "column_shortcut", "단축키" },
            { "add", "추가" },
            { "edit", "수정" },
            { "delete", "삭제" },
            { "osc_port_label", "REAPER OSC 로컬 수신 포트 (Default.ReaperOSC, 장치 IP 127.0.0.1, 장치 포트 9001)" },
            { "command_id_label", "ReaScript 명령 ID" },
            { "command_id_hint", "Actions에서 동봉 Lua 스크립트를 불러온 뒤 명령 ID를 복사해 입력하세요." },
            { "connection_test", "OSC 연결 시험" },
            { "save", "저장" },
            { "cancel", "취소" },
            { "reaper_path_label", "REAPER 실행 파일 (reaper.exe)" },
            { "project_path_label", "시작할 프로젝트 (.rpp) — 선택 사항" },
            { "browse", "찾아보기" },
            { "auto_start", "Windows 로그인 시 REAPER 자동시작" },
            { "hotkey_track_label", "REAPER 트랙 이름 (철자와 띄어쓰기 그대로)" },
            { "hotkey_chord_label", "단축키 (Ctrl/Alt/Shift와 키를 누르세요)" },
            { "confirm", "확인" },
            { "enter_track_name", "트랙 이름을 입력하세요." },
            { "enter_shortcut", "Ctrl, Alt, Shift 중 하나 이상과 함께 사용할 키를 지정하세요." },
            { "hotkey_track_required", "각 단축키에 REAPER 트랙 이름을 입력하세요." },
            { "hotkey_modifier_required", "단축키에는 Ctrl, Alt, Shift 중 하나 이상이 필요합니다." },
            { "hotkey_unsupported", "F12 또는 지원하지 않는 키는 단축키로 사용할 수 없습니다." },
            { "duplicate_shortcut", "같은 단축키가 두 개 이상 지정되어 있습니다." },
            { "duplicate_track", "같은 트랙 이름이 여러 단축키에 지정되어 있습니다. 트랙별 단축키는 하나만 지정할 수 있습니다." },
            { "registration_failed", "단축키 {0}를 등록할 수 없습니다. 다른 프로그램과 충돌하거나 Windows 예약 키일 수 있습니다. (오류 {1})" },
            { "keep_old_hotkeys", "기존 단축키는 유지했습니다." },
            { "restore_hotkeys_failed", "기존 단축키를 복구하지 못했습니다. 도우미를 다시 실행하세요." },
            { "copy_command_id", "Actions 목록에서 복사한 ReaScript 명령 ID를 입력하세요." },
            { "osc_connected", "REAPER Lua 스크립트와 연결되었습니다." },
            { "osc_failed", "연결하지 못했습니다. REAPER OSC 포트와 ReaScript 명령 ID를 확인하세요." },
            { "choose_reaper", "REAPER 실행 파일 선택" },
            { "choose_project", "시작할 프로젝트 선택" },
            { "project_filter", "REAPER 프로젝트|*.rpp" },
            { "startup_verify_failed", "자동시작 바로가기를 확인하지 못했습니다." },
            { "settings_save_failed", "설정을 저장하지 못했습니다." },
            { "hotkey_setup_first", "REAPER Lua 스크립트 명령 ID와 OSC 연결을 먼저 설정하세요." },
            { "muted", "트랙 음소거" },
            { "unmuted", "트랙 음소거 해제" },
            { "no_active_project", "활성 REAPER 프로젝트가 없습니다." },
            { "track_not_found", "현재 프로젝트에서 '{0}' 트랙을 찾지 못했습니다." },
            { "track_name_duplicated", "현재 프로젝트에 '{0}' 이름의 트랙이 여러 개 있습니다. 이름을 고유하게 바꾸세요." },
            { "reaper_timeout", "REAPER가 응답하지 않습니다. REAPER OSC 포트와 스크립트 명령 ID를 확인하세요." },
            { "toggle_failed", "REAPER 트랙을 전환하지 못했습니다. ({0})" },
            { "hotkey_error", "REAPER 음소거 단축키 오류: {0}" },
            { "startup_error", "REAPER 자동시작 도우미를 시작하지 못했습니다." },
            { "script_response", "스크립트 응답: {0}" },
            { "invalid_command_id", "잘못된 ReaScript 명령 ID입니다." },
            { "language_label", "표시 언어" },
            { "language_auto", "자동 (Windows)" },
            { "language_korean", "한국어" },
            { "language_english", "English" }
        };

        private static string activeMode = Automatic;
        private static bool isEnglish = ResolveLanguage(Automatic, CultureInfo.CurrentUICulture) == English;

        internal static string ActiveMode { get { return activeMode; } }
        internal static bool IsEnglish { get { return isEnglish; } }

        internal static void Apply(string mode)
        {
            activeMode = Normalize(mode);
            isEnglish = ResolveLanguage(activeMode, CultureInfo.CurrentUICulture) == English;
        }

        internal static string Normalize(string mode)
        {
            if (String.Equals(mode, Korean, StringComparison.OrdinalIgnoreCase)) return Korean;
            if (String.Equals(mode, English, StringComparison.OrdinalIgnoreCase)) return English;
            return Automatic;
        }

        internal static string ResolveLanguage(string mode, CultureInfo culture)
        {
            string normalized = Normalize(mode);
            if (normalized == Korean || normalized == English) return normalized;
            string language = culture == null ? "en" : culture.TwoLetterISOLanguageName;
            return String.Equals(language, "ko", StringComparison.OrdinalIgnoreCase) ? Korean : English;
        }

        internal static string Get(string key)
        {
            string value;
            Dictionary<string, string> source = isEnglish ? EnglishText : KoreanText;
            return source.TryGetValue(key, out value) ? value : key;
        }

        internal static string Format(string key, params object[] values)
        {
            return String.Format(CultureInfo.CurrentCulture, Get(key), values);
        }

        internal static int ModeIndex(string mode)
        {
            switch (Normalize(mode))
            {
                case Korean: return 1;
                case English: return 2;
                default: return 0;
            }
        }

        internal static bool HasMatchingTranslationKeys()
        {
            if (EnglishText.Count != KoreanText.Count) return false;
            foreach (string key in EnglishText.Keys)
            {
                if (!KoreanText.ContainsKey(key)) return false;
            }
            return true;
        }
    }
}
