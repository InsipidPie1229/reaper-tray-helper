using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace ReaperTrayHelper
{
    public sealed class AppSettings
    {
        public string ReaperPath { get; set; }
        public string ProjectPath { get; set; }
        public bool StartWithWindows { get; set; }
        public int ReaperOscPort { get; set; }
        public string ReaperScriptCommandId { get; set; }
        public List<TrackHotkeyBinding> TrackHotkeys { get; set; }

        public static string SettingsFile
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ReaperTrayHelper",
                    "settings.xml");
            }
        }

        public string Arguments
        {
            get
            {
                return String.IsNullOrWhiteSpace(ProjectPath) ? "" : "\"" + ProjectPath + "\"";
            }
        }

        public string ValidationError()
        {
            if (String.IsNullOrWhiteSpace(ReaperPath) ||
                !File.Exists(ReaperPath) ||
                !String.Equals(Path.GetFileName(ReaperPath), "reaper.exe", StringComparison.OrdinalIgnoreCase))
            {
                return "설치된 REAPER의 reaper.exe 파일을 선택하세요.";
            }

            if (!String.IsNullOrWhiteSpace(ProjectPath) &&
                (!File.Exists(ProjectPath) ||
                 !String.Equals(Path.GetExtension(ProjectPath), ".rpp", StringComparison.OrdinalIgnoreCase)))
            {
                return "사용할 .rpp 프로젝트를 선택하거나 프로젝트 칸을 비워 두세요.";
            }

            string hotkeyError = TrackHotkeyBinding.ValidationError(TrackHotkeys);
            if (hotkeyError != null) return hotkeyError;
            if (TrackHotkeys != null && TrackHotkeys.Count > 0)
            {
                if (ReaperOscPort < 1024 || ReaperOscPort > 65535)
                    return "REAPER OSC 수신 포트는 1024~65535 사이여야 합니다.";
                if (!ReaperOscBridge.IsValidCommandId(ReaperScriptCommandId))
                    return "트랙 단축키를 사용하려면 ReaScript 명령 ID를 입력하세요.";
            }

            return null;
        }

        public static AppSettings Load(string file)
        {
            var options = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using (var reader = XmlReader.Create(file, options))
            {
                return (AppSettings)new XmlSerializer(typeof(AppSettings)).Deserialize(reader);
            }
        }

        public void Save(string file)
        {
            string fullPath = Path.GetFullPath(file);
            string directory = Path.GetDirectoryName(fullPath);
            string temporaryPath = fullPath + ".tmp";

            Directory.CreateDirectory(directory);
            using (var writer = new StreamWriter(temporaryPath, false, new UTF8Encoding(true)))
            {
                new XmlSerializer(typeof(AppSettings)).Serialize(writer, this);
            }

            if (File.Exists(fullPath))
            {
                File.Replace(temporaryPath, fullPath, null);
            }
            else
            {
                File.Move(temporaryPath, fullPath);
            }
        }

        internal static AppSettings LoadOrConfigure(StartupShortcutManager startup, string executablePath, GlobalHotkeyManager hotkeys = null)
        {
            AppSettings settings = null;
            if (File.Exists(SettingsFile))
            {
                try
                {
                    settings = Load(SettingsFile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "기존 설정 파일을 읽지 못해 다시 설정합니다.\n\n" + ex.Message,
                        Program.ApplicationName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            if (settings != null && settings.ValidationError() == null)
            {
                settings.StartWithWindows = startup.IsEnabled(executablePath);
                if (hotkeys == null || hotkeys.Replace(settings.TrackHotkeys) == null) return settings;
                MessageBox.Show(
                    "저장된 전역 단축키를 등록하지 못했습니다. 설정을 열어 충돌하는 키를 수정하세요.",
                    Program.ApplicationName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            settings = settings ?? new AppSettings();
            if (String.IsNullOrWhiteSpace(settings.ReaperPath))
            {
                settings.ReaperPath = FindDefaultReaperPath();
            }

            settings.StartWithWindows = startup.IsEnabled(executablePath);
            return Configure(settings, startup, executablePath, hotkeys);
        }

        internal static AppSettings Configure(AppSettings current, StartupShortcutManager startup, string executablePath, GlobalHotkeyManager hotkeys = null)
        {
            using (var form = new Form())
            {
                form.Text = "REAPER 자동시작 도우미 설정";
                form.ClientSize = new System.Drawing.Size(760, 570);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var reaperPath = new TextBox { Left = 20, Top = 43, Width = 500, Text = current.ReaperPath ?? "" };
                var projectPath = new TextBox { Left = 20, Top = 113, Width = 500, Text = current.ProjectPath ?? "" };
                var reaperBrowse = new Button { Left = 530, Top = 41, Width = 85, Text = "찾아보기" };
                var projectBrowse = new Button { Left = 530, Top = 111, Width = 85, Text = "찾아보기" };
                var startupCheck = new CheckBox
                {
                    Left = 20,
                    Top = 157,
                    Width = 360,
                    Text = "Windows 로그인 시 REAPER 자동시작",
                    Checked = current.StartWithWindows
                };
                var note = new Label
                {
                    Left = 20,
                    Top = 187,
                    Width = 595,
                    Height = 42,
                    Text = "자동시작은 현재 Windows 계정의 시작프로그램 바로가기로 등록됩니다.\n프로젝트 경로를 비워 두면 REAPER의 기존 시작 설정을 사용합니다."
                };
                var hotkeyLabel = new Label { Left = 20, Top = 238, Width = 500, Text = "트랙별 전역 음소거 단축키 (현재 활성 REAPER 프로젝트)" };
                var hotkeyList = new ListView
                {
                    Left = 20,
                    Top = 260,
                    Width = 520,
                    Height = 170,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    MultiSelect = false
                };
                hotkeyList.Columns.Add("트랙 이름", 320);
                hotkeyList.Columns.Add("단축키", 170);
                var hotkeyAdd = new Button { Left = 550, Top = 260, Width = 190, Text = "추가" };
                var hotkeyEdit = new Button { Left = 550, Top = 296, Width = 190, Text = "수정" };
                var hotkeyDelete = new Button { Left = 550, Top = 332, Width = 190, Text = "삭제" };
                var oscPort = new NumericUpDown { Left = 550, Top = 405, Width = 90, Minimum = 1024, Maximum = 65535, Value = current.ReaperOscPort >= 1024 && current.ReaperOscPort <= 65535 ? current.ReaperOscPort : 8000 };
                var commandId = new TextBox { Left = 20, Top = 486, Width = 520, Text = current.ReaperScriptCommandId ?? "" };
                var connectionTest = new Button { Left = 550, Top = 484, Width = 190, Text = "OSC 연결 시험" };
                var setupNote = new Label
                {
                    Left = 20,
                    Top = 386,
                    Width = 720,
                    Height = 20,
                    Text = "REAPER OSC 로컬 수신 포트 (Default.ReaperOSC, 장치 IP 127.0.0.1, 장치 포트 9001)"
                };
                var guideNote = new Label
                {
                    Left = 20,
                    Top = 438,
                    Width = 720,
                    Height = 24,
                    Text = "Actions에서 동봉 Lua 스크립트를 불러온 뒤 명령 ID를 복사해 입력하세요."
                };
                var commandLabel = new Label { Left = 20, Top = 464, Width = 520, Text = "ReaScript 명령 ID" };
                var save = new Button { Left = 550, Top = 535, Width = 90, Text = "저장" };
                var cancel = new Button { Left = 650, Top = 535, Width = 90, Text = "취소", DialogResult = DialogResult.Cancel };

                form.Controls.Add(new Label { Left = 20, Top = 18, Width = 560, Text = "REAPER 실행 파일 (reaper.exe)" });
                form.Controls.Add(new Label { Left = 20, Top = 88, Width = 560, Text = "시작할 프로젝트 (.rpp) — 선택 사항" });
                form.Controls.AddRange(new Control[] { reaperPath, projectPath, reaperBrowse, projectBrowse, startupCheck, note, hotkeyLabel, hotkeyList, hotkeyAdd, hotkeyEdit, hotkeyDelete, oscPort, setupNote, commandLabel, commandId, connectionTest, guideNote, save, cancel });
                form.AcceptButton = save;
                form.CancelButton = cancel;

                var bindings = current.TrackHotkeys == null
                    ? new List<TrackHotkeyBinding>()
                    : new List<TrackHotkeyBinding>(current.TrackHotkeys);
                Action refreshBindings = delegate
                {
                    hotkeyList.Items.Clear();
                    foreach (TrackHotkeyBinding binding in bindings)
                    {
                        var item = new ListViewItem(binding.TrackName);
                        item.SubItems.Add(binding.DisplayShortcut);
                        hotkeyList.Items.Add(item);
                    }
                };
                refreshBindings();

                hotkeyAdd.Click += delegate
                {
                    TrackHotkeyBinding binding = TrackHotkeyDialog.Edit(form, null);
                    if (binding != null)
                    {
                        bindings.Add(binding);
                        refreshBindings();
                    }
                };
                hotkeyEdit.Click += delegate
                {
                    if (hotkeyList.SelectedIndices.Count == 0) return;
                    int index = hotkeyList.SelectedIndices[0];
                    TrackHotkeyBinding binding = TrackHotkeyDialog.Edit(form, bindings[index]);
                    if (binding != null)
                    {
                        bindings[index] = binding;
                        refreshBindings();
                    }
                };
                hotkeyDelete.Click += delegate
                {
                    if (hotkeyList.SelectedIndices.Count == 0) return;
                    bindings.RemoveAt(hotkeyList.SelectedIndices[0]);
                    refreshBindings();
                };
                connectionTest.Click += delegate
                {
                    string id = commandId.Text.Trim();
                    if (!ReaperOscBridge.IsValidCommandId(id))
                    {
                        MessageBox.Show(form, "Actions 목록에서 복사한 ReaScript 명령 ID를 입력하세요.", Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    try
                    {
                        ReaperOscBridge.Test((int)oscPort.Value, id);
                        MessageBox.Show(form, "REAPER Lua 스크립트와 연결되었습니다.", Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(form, "연결하지 못했습니다. REAPER OSC 포트와 ReaScript 명령 ID를 확인하세요.\n\n" + ex.Message, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                reaperBrowse.Click += delegate
                {
                    using (var dialog = new OpenFileDialog { Filter = "REAPER|reaper.exe", Title = "REAPER 실행 파일 선택" })
                    {
                        if (dialog.ShowDialog(form) == DialogResult.OK)
                        {
                            reaperPath.Text = dialog.FileName;
                        }
                    }
                };

                projectBrowse.Click += delegate
                {
                    using (var dialog = new OpenFileDialog { Filter = "REAPER 프로젝트|*.rpp", Title = "시작할 프로젝트 선택" })
                    {
                        if (dialog.ShowDialog(form) == DialogResult.OK)
                        {
                            projectPath.Text = dialog.FileName;
                        }
                    }
                };

                AppSettings result = null;
                save.Click += delegate
                {
                    var candidate = new AppSettings
                    {
                        ReaperPath = reaperPath.Text.Trim().Trim('\"'),
                        ProjectPath = projectPath.Text.Trim().Trim('\"'),
                        StartWithWindows = startupCheck.Checked,
                        ReaperOscPort = (int)oscPort.Value,
                        ReaperScriptCommandId = commandId.Text.Trim(),
                        TrackHotkeys = bindings
                    };

                    string error = candidate.ValidationError();
                    if (error != null)
                    {
                        MessageBox.Show(form, error, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        if (hotkeys != null)
                        {
                            string hotkeyError = hotkeys.Replace(candidate.TrackHotkeys);
                            if (hotkeyError != null)
                            {
                                MessageBox.Show(form, hotkeyError, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                        }

                        startup.SetEnabled(candidate.StartWithWindows, executablePath);
                        candidate.StartWithWindows = startup.IsEnabled(executablePath);
                        if (candidate.StartWithWindows != startupCheck.Checked)
                        {
                            throw new InvalidOperationException("자동시작 바로가기를 확인하지 못했습니다.");
                        }

                        candidate.Save(SettingsFile);
                    }
                    catch (Exception ex)
                    {
                        if (hotkeys != null) hotkeys.Replace(current.TrackHotkeys);
                        MessageBox.Show(
                            form,
                            "설정을 저장하지 못했습니다.\n\n" + ex.Message,
                            Program.ApplicationName,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    result = candidate;
                    form.DialogResult = DialogResult.OK;
                };

                return form.ShowDialog() == DialogResult.OK ? result : null;
            }
        }

        private static string FindDefaultReaperPath()
        {
            string[] roots =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            foreach (string root in roots)
            {
                foreach (string folder in new[] { "REAPER (x64)", "REAPER" })
                {
                    string candidate = Path.Combine(root, folder, "reaper.exe");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return "";
        }
    }
}
