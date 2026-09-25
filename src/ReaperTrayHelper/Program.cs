using System;
using System.Windows.Forms;

namespace ReaperTrayHelper
{
    internal static class Program
    {
        internal const string ApplicationName = "REAPER Tray Helper";

        internal static string ExecutablePath
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().Location; }
        }

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, "ReaperTrayHelper_v1", out createdNew))
            {
                if (!createdNew)
                {
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                try
                {
                    var startup = StartupShortcutManager.CreateDefault();
                    using (var hotkeys = new GlobalHotkeyManager())
                    {
                        AppSettings settings = AppSettings.LoadOrConfigure(startup, ExecutablePath, hotkeys);
                        if (settings == null) return;

                        Application.Run(new ReaperTrayContext(settings, startup, ExecutablePath, hotkeys));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        UiText.Get("startup_error") + "\n\n" + ex.Message,
                        ApplicationName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }
}
