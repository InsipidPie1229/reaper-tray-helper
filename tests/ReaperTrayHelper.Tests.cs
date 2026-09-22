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
        string root = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(root);
        TestSettings(root);
        TestStartupManager(root);
        TestProcessSelection(root);
        Console.WriteLine("Total: " + passed + " tests passed.");
    }

    private static void TestSettings(string root)
    {
        string reaper = Path.Combine(root, "reaper.exe");
        string project = Path.Combine(root, "한글 project & demo.rpp");
        File.WriteAllText(reaper, "test placeholder - never launched");
        File.WriteAllText(project, "test placeholder - never opened");

        var settings = new AppSettings { ReaperPath = reaper, ProjectPath = project, StartWithWindows = true };
        Check(settings.ValidationError() == null, "valid settings");
        Check(settings.Arguments == "\"" + project + "\"", "quoted project argument with spaces and Unicode");

        string settingsFile = Path.Combine(root, "settings.xml");
        settings.Save(settingsFile);
        AppSettings restored = AppSettings.Load(settingsFile);
        Check(restored.ReaperPath == reaper && restored.ProjectPath == project && restored.StartWithWindows, "settings XML round trip");

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
}

