using System;
using System.IO;
using System.Reflection;

namespace ReaperTrayHelper
{
    internal interface IShortcutStore
    {
        bool Exists();
        string GetTargetPath();
        void CreateOrUpdate(string targetPath, string workingDirectory, string description);
        void Delete();
    }

    internal sealed class StartupShortcutManager
    {
        internal const string ShortcutName = "REAPER Tray Helper.lnk";

        private readonly IShortcutStore store;

        internal StartupShortcutManager(IShortcutStore store)
        {
            this.store = store;
        }

        internal static StartupShortcutManager CreateDefault()
        {
            string startupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                ShortcutName);
            return new StartupShortcutManager(new WshShortcutStore(startupPath));
        }

        internal bool IsEnabled(string executablePath)
        {
            if (!store.Exists())
            {
                return false;
            }

            string target = store.GetTargetPath();
            return String.Equals(
                Path.GetFullPath(target ?? ""),
                Path.GetFullPath(executablePath),
                StringComparison.OrdinalIgnoreCase);
        }

        internal void SetEnabled(bool enabled, string executablePath)
        {
            if (!enabled)
            {
                if (store.Exists())
                {
                    store.Delete();
                }
                return;
            }

            string fullPath = Path.GetFullPath(executablePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("도우미 실행 파일을 찾을 수 없습니다.", fullPath);
            }

            store.CreateOrUpdate(
                fullPath,
                Path.GetDirectoryName(fullPath),
                "Starts REAPER Tray Helper for the current Windows account.");
        }
    }

    internal sealed class WshShortcutStore : IShortcutStore
    {
        private readonly string shortcutPath;

        internal WshShortcutStore(string shortcutPath)
        {
            this.shortcutPath = shortcutPath;
        }

        public bool Exists()
        {
            return File.Exists(shortcutPath);
        }

        public string GetTargetPath()
        {
            object shortcut = CreateShortcut();
            return (string)shortcut.GetType().InvokeMember(
                "TargetPath",
                BindingFlags.GetProperty,
                null,
                shortcut,
                null);
        }

        public void CreateOrUpdate(string targetPath, string workingDirectory, string description)
        {
            object shortcut = CreateShortcut();
            Type type = shortcut.GetType();
            SetProperty(type, shortcut, "TargetPath", targetPath);
            SetProperty(type, shortcut, "WorkingDirectory", workingDirectory);
            SetProperty(type, shortcut, "Description", description);
            SetProperty(type, shortcut, "WindowStyle", 7);
            type.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }

        public void Delete()
        {
            File.Delete(shortcutPath);
        }

        private object CreateShortcut()
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("Windows Script Host를 사용할 수 없습니다.");
            }

            object shell = Activator.CreateInstance(shellType);
            return shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                new object[] { shortcutPath });
        }

        private static void SetProperty(Type type, object instance, string name, object value)
        {
            type.InvokeMember(name, BindingFlags.SetProperty, null, instance, new[] { value });
        }
    }
}

