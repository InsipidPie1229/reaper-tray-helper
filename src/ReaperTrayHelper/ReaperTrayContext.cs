using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ReaperTrayHelper
{
    internal sealed class ReaperTrayContext : ApplicationContext
    {
        private const int SW_HIDE = 0;
        private const int SW_RESTORE = 9;

        private readonly string reaperPath;
        private readonly string projectPath;
        private readonly StartupShortcutManager startup;
        private readonly string helperPath;
        private readonly NotifyIcon trayIcon;
        private readonly Timer monitorTimer;
        private readonly HashSet<IntPtr> hiddenWindows = new HashSet<IntPtr>();
        private readonly Icon applicationIcon;
        private readonly IntPtr applicationIconHandle;

        private Process reaperProcess;
        private AppSettings settings;
        private DateTime? readySince;
        private bool autoHidePending = true;

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr handle, int command);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr handle, StringBuilder text, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr handle, StringBuilder name, int maxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr handle);

        private delegate bool EnumWindowsProc(IntPtr handle, IntPtr lParam);

        internal ReaperTrayContext(AppSettings settings, StartupShortcutManager startup, string helperPath)
        {
            this.settings = settings;
            reaperPath = settings.ReaperPath;
            projectPath = settings.ProjectPath;
            this.startup = startup;
            this.helperPath = helperPath;

            applicationIcon = CreateApplicationIcon(out applicationIconHandle);
            var menu = new ContextMenuStrip();
            menu.Items.Add("설정 (다음 실행부터 적용)", null, delegate { OpenSettings(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("REAPER 열기", null, delegate { ShowReaper(); });
            menu.Items.Add("REAPER 숨기기", null, delegate { HideReaper(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("트레이 도우미 종료", null, delegate { ExitHelper(); });

            trayIcon = new NotifyIcon
            {
                Icon = applicationIcon,
                Text = "REAPER Tray Helper - 더블 클릭으로 열기/숨기기",
                ContextMenuStrip = menu,
                Visible = true
            };
            trayIcon.DoubleClick += delegate { ToggleReaper(); };

            reaperProcess = FindOrStartReaper();
            if (reaperProcess == null)
            {
                throw new InvalidOperationException("REAPER 프로세스를 시작하지 못했습니다.");
            }

            monitorTimer = new Timer { Interval = 500 };
            monitorTimer.Tick += MonitorTimerOnTick;
            monitorTimer.Start();
        }

        private Process FindOrStartReaper()
        {
            var processMap = new Dictionary<int, Process>();
            var candidates = new List<RunningReaper>();
            foreach (Process process in Process.GetProcessesByName("reaper"))
            {
                processMap[process.Id] = process;
                string path;
                bool wasRead = TryGetProcessPath(process, out path);
                candidates.Add(new RunningReaper(process.Id, path, wasRead));
            }

            ReaperSelection selection = ReaperProcessSelector.Select(candidates, reaperPath);
            if (selection.Kind == ReaperSelectionKind.AttachToConfiguredProcess)
            {
                autoHidePending = false;
                return processMap[selection.ProcessId];
            }

            var info = new ProcessStartInfo
            {
                FileName = reaperPath,
                Arguments = String.IsNullOrWhiteSpace(projectPath) ? "" : "\"" + projectPath + "\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(String.IsNullOrWhiteSpace(projectPath) ? reaperPath : projectPath)
            };
            return Process.Start(info);
        }

        private static bool TryGetProcessPath(Process process, out string path)
        {
            path = null;
            try
            {
                path = process.MainModule.FileName;
                return !String.IsNullOrWhiteSpace(path);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        private void MonitorTimerOnTick(object sender, EventArgs e)
        {
            try
            {
                reaperProcess.Refresh();
                if (reaperProcess.HasExited)
                {
                    ExitHelperWithoutShowing();
                    return;
                }

                if (!autoHidePending)
                {
                    return;
                }

                List<WindowInfo> windows = GetProcessWindows(reaperProcess.Id);
                if (windows.Count == 0 || HasBlockingDialog(windows) || GetReaperMainWindow() == IntPtr.Zero)
                {
                    readySince = null;
                    return;
                }

                if (!readySince.HasValue)
                {
                    readySince = DateTime.UtcNow;
                    return;
                }

                if ((DateTime.UtcNow - readySince.Value).TotalSeconds >= 2)
                {
                    HideReaper();
                }
            }
            catch
            {
                // The next timer tick may recover a transient process/window state.
            }
        }

        private IntPtr GetReaperMainWindow()
        {
            foreach (WindowInfo window in GetProcessWindows(reaperProcess.Id))
            {
                if (window.Title.IndexOf(" - REAPER v", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    window.Title.StartsWith("REAPER v", StringComparison.OrdinalIgnoreCase))
                {
                    return window.Handle;
                }
            }

            reaperProcess.Refresh();
            return reaperProcess.MainWindowHandle;
        }

        private static bool HasBlockingDialog(IEnumerable<WindowInfo> windows)
        {
            foreach (WindowInfo window in windows)
            {
                var className = new StringBuilder(256);
                GetClassName(window.Handle, className, className.Capacity);
                if (className.ToString() == "#32770" || !IsWindowEnabled(window.Handle))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<WindowInfo> GetProcessWindows(int processId)
        {
            var windows = new List<WindowInfo>();
            EnumWindows(delegate(IntPtr handle, IntPtr unused)
            {
                uint ownerProcess;
                GetWindowThreadProcessId(handle, out ownerProcess);
                if (ownerProcess != (uint)processId || !IsWindowVisible(handle))
                {
                    return true;
                }

                var title = new StringBuilder(512);
                GetWindowText(handle, title, title.Capacity);
                windows.Add(new WindowInfo(handle, title.ToString()));
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        private void ToggleReaper()
        {
            if (hiddenWindows.Count > 0)
            {
                ShowReaper();
                return;
            }

            if (GetProcessWindows(reaperProcess.Id).Count > 0)
            {
                HideReaper();
            }
        }

        private void OpenSettings()
        {
            AppSettings updated = AppSettings.Configure(settings, startup, helperPath);
            if (updated != null)
            {
                settings = updated;
            }
        }

        private void ShowReaper()
        {
            foreach (IntPtr hiddenHandle in hiddenWindows.ToList())
            {
                if (IsWindow(hiddenHandle))
                {
                    ShowWindowAsync(hiddenHandle, SW_RESTORE);
                }
            }
            hiddenWindows.Clear();

            IntPtr mainWindow = GetReaperMainWindow();
            if (mainWindow != IntPtr.Zero && IsWindow(mainWindow))
            {
                ShowWindowAsync(mainWindow, SW_RESTORE);
                SetForegroundWindow(mainWindow);
            }
            autoHidePending = false;
        }

        private void HideReaper()
        {
            List<WindowInfo> windows = GetProcessWindows(reaperProcess.Id);
            if (HasBlockingDialog(windows))
            {
                return;
            }

            foreach (WindowInfo window in windows)
            {
                hiddenWindows.Add(window.Handle);
                ShowWindowAsync(window.Handle, SW_HIDE);
            }
            autoHidePending = false;
        }

        private void ExitHelper()
        {
            ShowReaper();
            ExitHelperWithoutShowing();
        }

        private void ExitHelperWithoutShowing()
        {
            if (monitorTimer != null)
            {
                monitorTimer.Stop();
            }
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (monitorTimer != null)
                {
                    monitorTimer.Dispose();
                }
                if (applicationIcon != null)
                {
                    applicationIcon.Dispose();
                }
                if (applicationIconHandle != IntPtr.Zero)
                {
                    DestroyIcon(applicationIconHandle);
                }
            }
            base.Dispose(disposing);
        }

        private static Icon CreateApplicationIcon(out IntPtr iconHandle)
        {
            using (var bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (var background = new SolidBrush(Color.FromArgb(28, 103, 128)))
            using (var foreground = new SolidBrush(Color.White))
            {
                graphics.Clear(Color.Transparent);
                graphics.FillEllipse(background, 1, 1, 30, 30);
                graphics.FillRectangle(foreground, 13, 7, 6, 12);
                graphics.FillEllipse(foreground, 10, 14, 12, 10);
                graphics.FillRectangle(foreground, 9, 22, 14, 2);
                graphics.FillRectangle(foreground, 15, 23, 2, 4);
                iconHandle = bitmap.GetHicon();
                return Icon.FromHandle(iconHandle);
            }
        }

        private sealed class WindowInfo
        {
            internal WindowInfo(IntPtr handle, string title)
            {
                Handle = handle;
                Title = title ?? "";
            }

            internal IntPtr Handle { get; private set; }
            internal string Title { get; private set; }
        }
    }
}
