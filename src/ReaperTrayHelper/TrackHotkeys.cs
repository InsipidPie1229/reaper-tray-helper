using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ReaperTrayHelper
{
    [Serializable]
    public sealed class TrackHotkeyBinding
    {
        public string TrackName { get; set; }
        public int Modifiers { get; set; }
        public int VirtualKey { get; set; }

        public string DisplayShortcut
        {
            get
            {
                var parts = new List<string>();
                if ((Modifiers & GlobalHotkeyManager.MOD_CONTROL) != 0) parts.Add("Ctrl");
                if ((Modifiers & GlobalHotkeyManager.MOD_ALT) != 0) parts.Add("Alt");
                if ((Modifiers & GlobalHotkeyManager.MOD_SHIFT) != 0) parts.Add("Shift");
                Keys key = (Keys)VirtualKey;
                parts.Add(key.ToString());
                return String.Join("+", parts);
            }
        }

        internal TrackHotkeyBinding Clone()
        {
            return new TrackHotkeyBinding { TrackName = TrackName, Modifiers = Modifiers, VirtualKey = VirtualKey };
        }

        internal static string ValidationError(IList<TrackHotkeyBinding> bindings)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (TrackHotkeyBinding binding in bindings ?? new List<TrackHotkeyBinding>())
            {
                if (binding == null || String.IsNullOrWhiteSpace(binding.TrackName))
                    return "각 단축키에 REAPER 트랙 이름을 입력하세요.";
                if (binding.Modifiers == 0 || (binding.Modifiers & ~(GlobalHotkeyManager.MOD_CONTROL | GlobalHotkeyManager.MOD_ALT | GlobalHotkeyManager.MOD_SHIFT)) != 0)
                    return "단축키에는 Ctrl, Alt, Shift 중 하나 이상이 필요합니다.";
                if (binding.VirtualKey <= 0 || binding.VirtualKey > 0xFE || binding.VirtualKey == 0x7B ||
                    binding.VirtualKey == 0x10 || binding.VirtualKey == 0x11 || binding.VirtualKey == 0x12 ||
                    binding.VirtualKey == 0x5B || binding.VirtualKey == 0x5C)
                    return "F12 또는 지원하지 않는 키는 단축키로 사용할 수 없습니다.";

                string chord = binding.Modifiers + ":" + binding.VirtualKey;
                if (!keys.Add(chord)) return "같은 단축키가 두 개 이상 지정되어 있습니다.";
                if (!names.Add(binding.TrackName)) return "같은 트랙 이름이 여러 단축키에 지정되어 있습니다. 트랙별 단축키는 하나만 지정할 수 있습니다.";
            }
            return null;
        }
    }

    internal static class TrackHotkeyDialog
    {
        internal static TrackHotkeyBinding Edit(IWin32Window owner, TrackHotkeyBinding existing)
        {
            using (var form = new Form())
            {
                form.Text = existing == null ? "트랙 단축키 추가" : "트랙 단축키 수정";
                form.ClientSize = new System.Drawing.Size(420, 165);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;

                var name = new TextBox { Left = 18, Top = 42, Width = 384, Text = existing == null ? "" : existing.TrackName };
                var chord = new TextBox
                {
                    Left = 18,
                    Top = 94,
                    Width = 250,
                    ReadOnly = true,
                    TabStop = false,
                    Text = existing == null ? "" : existing.DisplayShortcut
                };
                int modifiers = existing == null ? 0 : existing.Modifiers;
                int virtualKey = existing == null ? 0 : existing.VirtualKey;
                chord.KeyDown += delegate(object sender, KeyEventArgs e)
                {
                    if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.ShiftKey) return;
                    modifiers = (e.Control ? GlobalHotkeyManager.MOD_CONTROL : 0) |
                                (e.Alt ? GlobalHotkeyManager.MOD_ALT : 0) |
                                (e.Shift ? GlobalHotkeyManager.MOD_SHIFT : 0);
                    virtualKey = (int)e.KeyCode;
                    chord.Text = new TrackHotkeyBinding { Modifiers = modifiers, VirtualKey = virtualKey }.DisplayShortcut;
                    e.SuppressKeyPress = true;
                };

                var save = new Button { Left = 220, Top = 130, Width = 85, Text = "확인" };
                var cancel = new Button { Left = 315, Top = 130, Width = 85, Text = "취소", DialogResult = DialogResult.Cancel };
                form.Controls.AddRange(new Control[]
                {
                    new Label { Left = 18, Top = 18, Width = 380, Text = "REAPER 트랙 이름 (철자와 띄어쓰기 그대로)" },
                    name,
                    new Label { Left = 18, Top = 71, Width = 380, Text = "단축키 (Ctrl/Alt/Shift와 키를 누르세요)" },
                    chord, save, cancel
                });
                form.AcceptButton = save;
                form.CancelButton = cancel;

                TrackHotkeyBinding result = null;
                save.Click += delegate
                {
                    if (String.IsNullOrWhiteSpace(name.Text))
                    {
                        MessageBox.Show(form, "트랙 이름을 입력하세요.", Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (modifiers == 0 || virtualKey == 0)
                    {
                        MessageBox.Show(form, "Ctrl, Alt, Shift 중 하나 이상과 함께 사용할 키를 지정하세요.", Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    result = new TrackHotkeyBinding { TrackName = name.Text, Modifiers = modifiers, VirtualKey = virtualKey };
                    form.DialogResult = DialogResult.OK;
                };

                return form.ShowDialog(owner) == DialogResult.OK ? result : null;
            }
        }
    }

    internal interface IHotkeyRegistrar
    {
        bool Register(IntPtr handle, int id, uint modifiers, uint virtualKey, out int errorCode);
        void Unregister(IntPtr handle, int id);
    }

    internal sealed class GlobalHotkeyManager : NativeWindow, IDisposable
    {
        internal const int MOD_ALT = 0x0001;
        internal const int MOD_CONTROL = 0x0002;
        internal const int MOD_SHIFT = 0x0004;
        private const uint MOD_NOREPEAT = 0x4000;
        private const int WM_HOTKEY = 0x0312;

        private readonly Dictionary<int, TrackHotkeyBinding> registered = new Dictionary<int, TrackHotkeyBinding>();
        private readonly IHotkeyRegistrar registrar;
        private readonly IntPtr hostHandle;
        private int nextId = 0x4000;
        private bool disposed;
        internal event Action<TrackHotkeyBinding> Pressed;

        internal GlobalHotkeyManager()
        {
            registrar = new Win32HotkeyRegistrar();
            CreateHandle(new CreateParams { Caption = "ReaperTrayHelper.GlobalHotkey" });
            hostHandle = Handle;
        }

        internal GlobalHotkeyManager(IHotkeyRegistrar testRegistrar, IntPtr testHandle)
        {
            registrar = testRegistrar;
            hostHandle = testHandle;
        }

        internal string Replace(IEnumerable<TrackHotkeyBinding> bindings)
        {
            TrackHotkeyBinding[] requested = (bindings ?? Enumerable.Empty<TrackHotkeyBinding>()).Select(item => item.Clone()).ToArray();
            string invalid = TrackHotkeyBinding.ValidationError(requested);
            if (invalid != null) return invalid;

            TrackHotkeyBinding[] previous = registered.Values.Select(item => item.Clone()).ToArray();
            UnregisterAll();
            string error = RegisterAll(requested);
            if (error == null) return null;

            UnregisterAll();
            string restoreError = RegisterAll(previous);
            return restoreError == null
                ? error + " 기존 단축키는 유지했습니다."
                : error + " 기존 단축키를 복구하지 못했습니다. 도우미를 다시 실행하세요.";
        }

        private string RegisterAll(IEnumerable<TrackHotkeyBinding> bindings)
        {
            foreach (TrackHotkeyBinding binding in bindings)
            {
                int id = ++nextId;
                int code;
                if (!registrar.Register(hostHandle, id, (uint)binding.Modifiers | MOD_NOREPEAT, (uint)binding.VirtualKey, out code))
                {
                    return "단축키 " + binding.DisplayShortcut + "를 등록할 수 없습니다. 다른 프로그램과 충돌하거나 Windows 예약 키일 수 있습니다. (오류 " + code + ")";
                }
                registered.Add(id, binding.Clone());
            }
            return null;
        }

        private void UnregisterAll()
        {
            foreach (int id in registered.Keys.ToArray()) registrar.Unregister(hostHandle, id);
            registered.Clear();
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WM_HOTKEY)
            {
                TrackHotkeyBinding binding;
                if (registered.TryGetValue(message.WParam.ToInt32(), out binding))
                {
                    Action<TrackHotkeyBinding> handler = Pressed;
                    if (handler != null) handler(binding.Clone());
                }
            }
            base.WndProc(ref message);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            UnregisterAll();
            DestroyHandle();
        }
    }

    internal sealed class Win32HotkeyRegistrar : IHotkeyRegistrar
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint virtualKey);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr handle, int id);

        public bool Register(IntPtr handle, int id, uint modifiers, uint virtualKey, out int errorCode)
        {
            bool result = RegisterHotKey(handle, id, modifiers, virtualKey);
            errorCode = result ? 0 : System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            return result;
        }

        public void Unregister(IntPtr handle, int id)
        {
            UnregisterHotKey(handle, id);
        }
    }
}
