using System;
using System.Collections.Generic;
using System.IO;
using ReaperTrayHelper;

internal static class ReaperTrayHelperTests
{
    private static int passed;

    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new Exception("FAIL: " + name);
        }

        passed++;
        Console.WriteLine("PASS: " + name);
    }

    public static void Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args[0]);
            Directory.CreateDirectory(root);
            TestSettings(root);
            TestLocalization();
            TestTrackHotkeys();
            TestGlobalHotkeyRegistration();
            TestOscBridge();
            TestStartupManager(root);
            TestProcessSelection(root);
            Console.WriteLine("Total: " + passed + " tests passed.");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL: unhandled " + exception.GetType().FullName);
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine(exception.StackTrace);
            Environment.ExitCode = 1;
        }
    }

    private static void TestSettings(string root)
    {
        string reaper = Path.Combine(root, "reaper.exe");
        string project = Path.Combine(root, "한글 project & demo.rpp");
        File.WriteAllText(reaper, "test placeholder - never launched");
        File.WriteAllText(project, "test placeholder - never opened");

        var settings = new AppSettings
        {
            ReaperPath = reaper,
            ProjectPath = project,
            StartWithWindows = true,
            ReaperOscPort = 8000,
            ReaperScriptCommandId = "_RSabc123",
            LanguageMode = UiText.English,
            TrackHotkeys = new List<TrackHotkeyBinding>
            {
                new TrackHotkeyBinding { TrackName = "마이크", Modifiers = GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT, VirtualKey = (int)System.Windows.Forms.Keys.D1 }
            }
        };
        Check(settings.ValidationError() == null, "valid settings");
        Check(settings.Arguments == "\"" + project + "\"", "quoted project argument with spaces and Unicode");

        string settingsFile = Path.Combine(root, "settings.xml");
        settings.Save(settingsFile);
        AppSettings restored = AppSettings.Load(settingsFile);
        Check(restored.ReaperPath == reaper && restored.ProjectPath == project && restored.StartWithWindows && restored.TrackHotkeys.Count == 1 && restored.TrackHotkeys[0].TrackName == "마이크" && restored.LanguageMode == UiText.English, "settings XML round trip includes track hotkeys and language");

        settings.ProjectPath = "";
        Check(settings.ValidationError() == null && settings.Arguments == "", "project is optional");
        settings.ProjectPath = reaper;
        Check(settings.ValidationError() != null, "reject non-RPP project");
        settings.ProjectPath = "";
        settings.ReaperPath = project;
        Check(settings.ValidationError() != null, "reject non-REAPER executable");
        settings.ReaperPath = reaper;

        File.WriteAllText(settingsFile, "not xml");
        Check(Throws(delegate { AppSettings.Load(settingsFile); }), "reject corrupt configuration");
        File.WriteAllText(settingsFile, "<!DOCTYPE test [<!ENTITY test SYSTEM 'file:///not-read'>]><AppSettings><ReaperPath>&test;</ReaperPath></AppSettings>");
        Check(Throws(delegate { AppSettings.Load(settingsFile); }), "reject XML external entity");

        string escapedReaper = System.Security.SecurityElement.Escape(reaper);
        File.WriteAllText(settingsFile, "<AppSettings><ReaperPath>" + escapedReaper + "</ReaperPath><ProjectPath></ProjectPath><StartWithWindows>false</StartWithWindows></AppSettings>");
        AppSettings legacy = AppSettings.Load(settingsFile);
        Check(legacy.TrackHotkeys != null && legacy.TrackHotkeys.Count == 0 && legacy.LanguageMode == UiText.Automatic && legacy.ValidationError() == null, "load existing settings without hotkey or language fields");
    }

    private static void TestLocalization()
    {
        Check(UiText.HasMatchingTranslationKeys(), "English and Korean translation keys match");
        Check(UiText.ResolveLanguage(UiText.Automatic, new System.Globalization.CultureInfo("ko-KR")) == UiText.Korean, "automatic language follows Korean Windows UI culture");
        Check(UiText.ResolveLanguage(UiText.Automatic, new System.Globalization.CultureInfo("en-US")) == UiText.English, "automatic language uses English for non-Korean culture");
        Check(UiText.ResolveLanguage(UiText.English, new System.Globalization.CultureInfo("ko-KR")) == UiText.English, "manual English overrides Windows culture");
        Check(UiText.ResolveLanguage(UiText.Korean, new System.Globalization.CultureInfo("en-US")) == UiText.Korean, "manual Korean overrides Windows culture");

        UiText.Apply(UiText.English);
        Check(UiText.Get("settings_title") == "REAPER Tray Helper Settings", "English settings title is localized");
        Check(UiText.Get("hotkey_track_required") == "Enter a REAPER track name for every shortcut.", "English validation message is localized");
        UiText.Apply(UiText.Korean);
        Check(UiText.Get("settings_title") == "REAPER 자동시작 도우미 설정", "Korean settings title is localized");
        Check(UiText.Get("hotkey_track_required") == "각 단축키에 REAPER 트랙 이름을 입력하세요.", "Korean validation message is localized");
        UiText.Apply(UiText.Automatic);
    }

    private static void TestTrackHotkeys()
    {
        var first = new TrackHotkeyBinding { TrackName = "MIC 한글", Modifiers = GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT, VirtualKey = (int)System.Windows.Forms.Keys.D1 };
        var second = new TrackHotkeyBinding { TrackName = "MUSIC", Modifiers = GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT, VirtualKey = (int)System.Windows.Forms.Keys.D2 };
        Check(TrackHotkeyBinding.ValidationError(new[] { first, second }) == null, "allow independent hotkeys for distinct tracks");
        Check(first.DisplayShortcut == "Ctrl+Alt+D1", "format configured shortcut");
        Check(TrackHotkeyBinding.ValidationError(new[] { first, new TrackHotkeyBinding { TrackName = "Other", Modifiers = first.Modifiers, VirtualKey = first.VirtualKey } }) != null, "reject duplicate key combination");
        Check(TrackHotkeyBinding.ValidationError(new[] { first, new TrackHotkeyBinding { TrackName = "MIC 한글", Modifiers = GlobalHotkeyManager.MOD_CONTROL, VirtualKey = (int)System.Windows.Forms.Keys.D2 } }) != null, "reject duplicate track assignment");
        Check(TrackHotkeyBinding.ValidationError(new[] { new TrackHotkeyBinding { TrackName = "MIC", Modifiers = 0, VirtualKey = (int)System.Windows.Forms.Keys.D1 } }) != null, "require modifier for global shortcut");
        Check(TrackHotkeyBinding.ValidationError(new[] { new TrackHotkeyBinding { TrackName = "MIC", Modifiers = GlobalHotkeyManager.MOD_CONTROL, VirtualKey = (int)System.Windows.Forms.Keys.F12 } }) != null, "reject reserved F12 shortcut");
        Check(TrackHotkeyBinding.ValidationError(new TrackHotkeyBinding[0]) == null, "allow no configured global shortcuts");
    }

    private static void TestOscBridge()
    {
        Check(ReaperOscBridge.IsValidCommandId("_RSabc123"), "accept REAPER script command ID");
        Check(!ReaperOscBridge.IsValidCommandId("123"), "reject malformed REAPER command ID");
        byte[] packet = ReaperOscBridge.BuildOscActionPacket("_RSabc123");
        Check(System.Text.Encoding.UTF8.GetString(packet, 0, 12) == "/action/str\0", "OSC packet uses ACTION string address");
        Check(Array.IndexOf(packet, (byte)',') >= 0 && Array.IndexOf(packet, (byte)'s') >= 0, "OSC packet declares string argument");
        Check(System.Text.Encoding.UTF8.GetString(packet).Contains("_RSabc123"), "OSC packet includes ReaScript command ID");
    }

    private static void TestGlobalHotkeyRegistration()
    {
        var registry = new FakeHotkeyRegistry();
        var first = new TrackHotkeyBinding { TrackName = "MIC", Modifiers = GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT, VirtualKey = (int)System.Windows.Forms.Keys.D1 };
        var second = new TrackHotkeyBinding { TrackName = "MUSIC", Modifiers = GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT, VirtualKey = (int)System.Windows.Forms.Keys.D2 };
        using (var one = new GlobalHotkeyManager(registry, new IntPtr(1)))
        using (var two = new GlobalHotkeyManager(registry, new IntPtr(2)))
        {
            Check(one.Replace(new[] { first }) == null && registry.Count == 1, "register global hotkey");
            Check(two.Replace(new[] { first }) != null && registry.Count == 1, "reject global hotkey owned by another app");
            Check(one.Replace(new TrackHotkeyBinding[0]) == null && registry.Count == 0, "unregister removed global hotkey");
            Check(two.Replace(new[] { first }) == null && registry.Count == 1, "register hotkey after prior owner releases it");
            registry.FailVirtualKey = second.VirtualKey;
            Check(two.Replace(new[] { first, second }) != null && registry.Count == 1, "restore previous hotkey when a new registration fails");
            Check(two.Replace(new TrackHotkeyBinding[0]) == null && registry.Count == 0, "release all hotkeys on removal");
        }
    }

    private static void TestStartupManager(string root)
    {
        string helper = Path.Combine(root, "ReaperTrayHelper.exe");
        File.WriteAllText(helper, "test placeholder - never launched");
        var store = new FakeShortcutStore();
        var manager = new StartupShortcutManager(store);

        Check(!manager.IsEnabled(helper), "startup initially disabled");
        manager.SetEnabled(true, helper);
        Check(store.CreateCount == 1 && store.Target == Path.GetFullPath(helper), "startup creates own shortcut target");
        Check(manager.IsEnabled(helper), "startup enabled is verified by target path");

        store.Target = Path.Combine(root, "other.exe");
        Check(!manager.IsEnabled(helper), "wrong shortcut target is not accepted");
        manager.SetEnabled(false, helper);
        Check(store.DeleteCount == 1 && !store.Exists(), "startup disable removes only helper shortcut");
        Check(Throws(delegate { manager.SetEnabled(true, Path.Combine(root, "missing.exe")); }), "reject missing helper executable");
    }

    private static void TestProcessSelection(string root)
    {
        string configured = Path.Combine(root, "reaper.exe");
        ReaperSelection launch = ReaperProcessSelector.Select(new List<RunningReaper>(), configured);
        Check(launch.Kind == ReaperSelectionKind.LaunchConfiguredProcess, "launch when configured REAPER is absent");

        ReaperSelection attach = ReaperProcessSelector.Select(
            new[] { new RunningReaper(10, configured, true) }, configured);
        Check(attach.Kind == ReaperSelectionKind.AttachToConfiguredProcess && attach.ProcessId == 10, "attach to exactly one configured REAPER");

        Check(Throws(delegate
        {
            ReaperProcessSelector.Select(
                new[] { new RunningReaper(10, configured, true), new RunningReaper(11, configured, true) }, configured);
        }), "reject multiple configured REAPER processes");

        Check(Throws(delegate
        {
            ReaperProcessSelector.Select(new[] { new RunningReaper(12, null, false) }, configured);
        }), "reject unverifiable running REAPER path");
    }

    private static bool Throws(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch
        {
            return true;
        }
    }

    private sealed class FakeShortcutStore : IShortcutStore
    {
        internal int CreateCount;
        internal int DeleteCount;
        internal string Target;
        internal string WorkingDirectory;

        public bool Exists() { return Target != null; }
        public string GetTargetPath() { return Target; }
        public void CreateOrUpdate(string targetPath, string workingDirectory, string description)
        {
            CreateCount++;
            Target = targetPath;
            WorkingDirectory = workingDirectory;
        }
        public void Delete()
        {
            DeleteCount++;
            Target = null;
        }
    }

    private sealed class FakeHotkeyRegistry : IHotkeyRegistrar
    {
        private readonly Dictionary<string, string> byChord = new Dictionary<string, string>();
        private readonly Dictionary<string, string> byOwner = new Dictionary<string, string>();
        internal int FailVirtualKey = -1;
        internal int Count { get { return byChord.Count; } }

        public bool Register(IntPtr handle, int id, uint modifiers, uint virtualKey, out int errorCode)
        {
            string chord = modifiers + ":" + virtualKey;
            string owner = handle.ToInt64() + ":" + id;
            if (byChord.ContainsKey(chord) || (int)virtualKey == FailVirtualKey)
            {
                errorCode = 1409;
                return false;
            }
            byChord.Add(chord, owner);
            byOwner.Add(owner, chord);
            errorCode = 0;
            return true;
        }

        public void Unregister(IntPtr handle, int id)
        {
            string owner = handle.ToInt64() + ":" + id;
            string chord;
            if (byOwner.TryGetValue(owner, out chord))
            {
                byOwner.Remove(owner);
                byChord.Remove(chord);
            }
        }
    }
}
