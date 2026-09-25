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
        public string LanguageMode { get; set; }

        public AppSettings()
        {
            LanguageMode = UiText.Automatic;
        }

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
                return UiText.Get("invalid_reaper_path");
            }

            if (!String.IsNullOrWhiteSpace(ProjectPath) &&
                (!File.Exists(ProjectPath) ||
                 !String.Equals(Path.GetExtension(ProjectPath), ".rpp", StringComparison.OrdinalIgnoreCase)))
            {
                return UiText.Get("invalid_project_path");
            }

            string hotkeyError = TrackHotkeyBinding.ValidationError(TrackHotkeys);
            if (hotkeyError != null) return hotkeyError;
            if (TrackHotkeys != null && TrackHotkeys.Count > 0)
            {
                if (ReaperOscPort < 1024 || ReaperOscPort > 65535)
                    return UiText.Get("invalid_osc_port");
                if (!ReaperOscBridge.IsValidCommandId(ReaperScriptCommandId))
                    return UiText.Get("missing_command_id");
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
            UiText.Apply(UiText.Automatic);
            AppSettings settings = null;
            if (File.Exists(SettingsFile))
            {
                try
                {
                    settings = Load(SettingsFile);
                    settings.LanguageMode = UiText.Normalize(settings.LanguageMode);
                    UiText.Apply(settings.LanguageMode);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        UiText.Get("settings_corrupt") + "\n\n" + ex.Message,
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
                    UiText.Get("stored_hotkey_conflict"),
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
            UiText.Apply(current.LanguageMode);
            using (var form = new Form())
            {
                form.Text = UiText.Get("settings_title");
                form.ClientSize = new System.Drawing.Size(760, 570);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var languageLabel = new Label { Left = 620, Top = 18, Width = 120, Text = UiText.Get("language_label") };
                var languageChoice = new ComboBox
                {
                    Left = 620,
                    Top = 40,
                    Width = 120,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                languageChoice.Items.AddRange(new object[]
                {
                    UiText.Get("language_auto"),
                    UiText.Get("language_korean"),
                    UiText.Get("language_english")
                });
                languageChoice.SelectedIndex = UiText.ModeIndex(current.LanguageMode);

                var reaperPath = new TextBox { Left = 20, Top = 43, Width = 500, Text = current.ReaperPath ?? "" };
                var projectPath = new TextBox { Left = 20, Top = 113, Width = 500, Text = current.ProjectPath ?? "" };
                var reaperBrowse = new Button { Left = 530, Top = 41, Width = 85, Text = UiText.Get("browse") };
                var projectBrowse = new Button { Left = 530, Top = 111, Width = 85, Text = UiText.Get("browse") };
                var startupCheck = new CheckBox
                {
                    Left = 20,
                    Top = 157,
                    Width = 360,
                    Text = UiText.Get("auto_start"),
                    Checked = current.StartWithWindows
                };
                var note = new Label
                {
                    Left = 20,
                    Top = 187,
                    Width = 595,
                    Height = 42,
                    Text = UiText.Get("startup_note")
                };
                var hotkeyLabel = new Label { Left = 20, Top = 238, Width = 500, Text = UiText.Get("hotkeys_heading") };
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
                hotkeyList.Columns.Add(UiText.Get("column_track"), 320);
                hotkeyList.Columns.Add(UiText.Get("column_shortcut"), 170);
                var hotkeyAdd = new Button { Left = 550, Top = 260, Width = 190, Text = UiText.Get("add") };
                var hotkeyEdit = new Button { Left = 550, Top = 296, Width = 190, Text = UiText.Get("edit") };
                var hotkeyDelete = new Button { Left = 550, Top = 332, Width = 190, Text = UiText.Get("delete") };
                var oscPort = new NumericUpDown { Left = 550, Top = 405, Width = 90, Minimum = 1024, Maximum = 65535, Value = current.ReaperOscPort >= 1024 && current.ReaperOscPort <= 65535 ? current.ReaperOscPort : 8000 };
                var commandId = new TextBox { Left = 20, Top = 486, Width = 520, Text = current.ReaperScriptCommandId ?? "" };
                var connectionTest = new Button { Left = 550, Top = 484, Width = 190, Text = UiText.Get("connection_test") };
                var setupNote = new Label
                {
                    Left = 20,
                    Top = 386,
                    Width = 720,
                    Height = 20,
                    Text = UiText.Get("osc_port_label")
                };
                var guideNote = new Label
                {
                    Left = 20,
                    Top = 438,
                    Width = 720,
                    Height = 24,
                    Text = UiText.Get("command_id_hint")
                };
                var commandLabel = new Label { Left = 20, Top = 464, Width = 520, Text = UiText.Get("command_id_label") };
                var save = new Button { Left = 550, Top = 535, Width = 90, Text = UiText.Get("save") };
                var cancel = new Button { Left = 650, Top = 535, Width = 90, Text = UiText.Get("cancel"), DialogResult = DialogResult.Cancel };

                form.Controls.Add(new Label { Left = 20, Top = 18, Width = 560, Text = UiText.Get("reaper_path_label") });
                form.Controls.Add(new Label { Left = 20, Top = 88, Width = 560, Text = UiText.Get("project_path_label") });
                form.Controls.AddRange(new Control[] { languageLabel, languageChoice, reaperPath, projectPath, reaperBrowse, projectBrowse, startupCheck, note, hotkeyLabel, hotkeyList, hotkeyAdd, hotkeyEdit, hotkeyDelete, oscPort, setupNote, commandLabel, commandId, connectionTest, guideNote, save, cancel });
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
                        MessageBox.Show(form, UiText.Get("copy_command_id"), Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    try
                    {
                        ReaperOscBridge.Test((int)oscPort.Value, id);
                        MessageBox.Show(form, UiText.Get("osc_connected"), Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(form, UiText.Get("osc_failed") + "\n\n" + ex.Message, Program.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                reaperBrowse.Click += delegate
                {
                    using (var dialog = new OpenFileDialog { Filter = "REAPER|reaper.exe", Title = UiText.Get("choose_reaper") })
                    {
                        if (dialog.ShowDialog(form) == DialogResult.OK)
                        {
                            reaperPath.Text = dialog.FileName;
                        }
                    }
                };

                projectBrowse.Click += delegate
                {
                    using (var dialog = new OpenFileDialog { Filter = UiText.Get("project_filter"), Title = UiText.Get("choose_project") })
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
                        TrackHotkeys = bindings,
                        LanguageMode = languageChoice.SelectedIndex == 1 ? UiText.Korean : languageChoice.SelectedIndex == 2 ? UiText.English : UiText.Automatic
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
                            throw new InvalidOperationException(UiText.Get("startup_verify_failed"));
                        }

                        candidate.Save(SettingsFile);
                    }
                    catch (Exception ex)
                    {
                        if (hotkeys != null) hotkeys.Replace(current.TrackHotkeys);
                        MessageBox.Show(
                            form,
                            UiText.Get("settings_save_failed") + "\n\n" + ex.Message,
                            Program.ApplicationName,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    UiText.Apply(candidate.LanguageMode);
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
