using System;
using System.IO;
using System.Text;
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

        internal static AppSettings LoadOrConfigure(StartupShortcutManager startup, string executablePath)
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
                return settings;
            }

            settings = settings ?? new AppSettings();
            if (String.IsNullOrWhiteSpace(settings.ReaperPath))
            {
                settings.ReaperPath = FindDefaultReaperPath();
            }

            settings.StartWithWindows = startup.IsEnabled(executablePath);
            return Configure(settings, startup, executablePath);
        }

        internal static AppSettings Configure(AppSettings current, StartupShortcutManager startup, string executablePath)
        {
            using (var form = new Form())
            {
                form.Text = "REAPER 자동시작 도우미 설정";
                form.ClientSize = new System.Drawing.Size(635, 295);
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
                var save = new Button { Left = 425, Top = 247, Width = 90, Text = "저장" };
                var cancel = new Button { Left = 525, Top = 247, Width = 90, Text = "취소", DialogResult = DialogResult.Cancel };

                form.Controls.Add(new Label { Left = 20, Top = 18, Width = 560, Text = "REAPER 실행 파일 (reaper.exe)" });
                form.Controls.Add(new Label { Left = 20, Top = 88, Width = 560, Text = "시작할 프로젝트 (.rpp) — 선택 사항" });
                form.Controls.AddRange(new Control[] { reaperPath, projectPath, reaperBrowse, projectBrowse, startupCheck, note, save, cancel });
                form.AcceptButton = save;
                form.CancelButton = cancel;

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
                        StartWithWindows = startupCheck.Checked
                    };

                    string error = candidate.ValidationError();
                    if (error != null)
                    {
                        MessageBox.Show(form, error, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
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
