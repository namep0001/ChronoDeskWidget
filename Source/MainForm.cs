using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ChronoDeskWidget
{
    internal sealed class MainForm : Form
    {
        private const string AppName = "ChronoDesk Widget";
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "ChronoDeskWidget";

        private readonly string[] _solarMonthsEn = { "Farvardin", "Ordibehesht", "Khordad", "Tir", "Mordad", "Shahrivar", "Mehr", "Aban", "Azar", "Dey", "Bahman", "Esfand" };
        private readonly string[] _solarMonthsFa = { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
        private readonly string[] _weekdaysEn = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        private readonly string[] _weekdaysFa = { "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنجشنبه", "جمعه", "شنبه", "یکشنبه" };
        private readonly PersianCalendar _persianCalendar = new PersianCalendar();
        private readonly List<TaskItem> _tasks;
        private readonly List<Panel> _sections = new List<Panel>();
        private readonly MarketRate[] _rateDefinitions = TgjuClient.Definitions();
        private readonly Dictionary<string, Label> _rateValueLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, string> _rateValues = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _rateErrors = new Dictionary<string, string>();
        private readonly Dictionary<string, bool> _rateFreshThisFetch = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _widgetVisibility = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly string[] _widgetKeys = { "clock", "calendar", "tasks", "pomodoro", "notes", "rates" };

        private Color _themeBackground;
        private Color _themeSurface;
        private Color _themeSurfaceAlt;
        private Color _themeInput;
        private Color _themeButton;
        private Color _themeText;
        private Color _themeMutedText;
        private Color _themeAccent;
        private Color _themeAccentText;
        private Color _themeBorder;
        private Color _themeSelection;
        private Color _themeSelectionText;
        private bool _themeUsesLightTaskbar;
        private string _themeSignature = string.Empty;
        private DateTime _lastThemePoll = DateTime.MinValue;

        private string _language;
        private bool _keepOnTop;
        private bool _exitRequested;
        private bool _startHidden;
        private bool _notesDirty;
        private bool _buildingContent;
        private bool _rateFetchInProgress;
        private int _remainingRateFetches;
        private DateTime _lastRateRefreshLocal = DateTime.MinValue;
        private DateTime _selectedDate = DateTime.Today;
        private DateTime _calendarAnchor = DateTime.Today;
        private bool _showSolarCalendar = true;
        private bool _timerRunning;
        private bool _isBreak;
        private int _remainingSeconds = 25 * 60;
        private DateTime _timerEndsAt;

        private FlowLayoutPanel _content;
        private Label _clockLabel;
        private Label _dateEnglishLabel;
        private Label _dateSolarLabel;
        private Label _clockHintLabel;
        private Label _calendarMonthLabel;
        private ComboBox _calendarModeCombo;
        private TableLayoutPanel _calendarGrid;
        private Panel _taskRowsPanel;
        private TextBox _taskInput;
        private Button _addTaskButton;
        private Label _taskCountLabel;
        private TextBox _notesBox;
        private Label _notesStatusLabel;
        private Label _pomodoroModeLabel;
        private Label _pomodoroTimeLabel;
        private Label _pomodoroStatusLabel;
        private Button _pomodoroStartButton;
        private Button _pomodoroResetButton;
        private Button _pomodoroSwitchButton;
        private Label _ratesStatusLabel;
        private Button _ratesRefreshButton;
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private System.Windows.Forms.Timer _tickTimer;
        private Icon _generatedIcon;
        private ToolTip _toolTip;
        private readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>(StringComparer.Ordinal);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;

        public MainForm(bool startHidden)
        {
            _startHidden = startHidden;
            _language = LocalStore.LoadSetting("language", "en");
            if (_language != "fa") _language = "en";
            _keepOnTop = LocalStore.LoadSetting("topmost", "1") == "1";
            foreach (string widgetKey in _widgetKeys)
                _widgetVisibility[widgetKey] = LocalStore.LoadSetting("widget." + widgetKey, "1") != "0";
            _tasks = LocalStore.LoadTasks();
            foreach (MarketRate rate in _rateDefinitions)
            {
                _rateValues[rate.Key] = string.Empty;
                _rateErrors[rate.Key] = string.Empty;
                _rateFreshThisFetch[rate.Key] = false;
            }

            Text = AppName;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(390, 90);
            Size = new Size(450, Math.Min(860, Math.Max(120, Screen.PrimaryScreen.WorkingArea.Height - 16)));
            RefreshSystemTheme(true);
            Font = GetFont("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Opacity = 0.90;
            TopMost = _keepOnTop;
            PlaceOnDesktop();

            _toolTip = new ToolTip();

            BuildContent();
            BuildTrayIcon();
            SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;

            _tickTimer = new System.Windows.Forms.Timer();
            _tickTimer.Interval = 1000;
            _tickTimer.Tick += OnTick;
            _tickTimer.Start();
            UpdateClock();
            UpdatePomodoroDisplay();

            Shown += delegate
            {
                if (_startHidden || !AnyWidgetVisible()) Hide();
                if (IsWidgetVisible("rates")) RefreshRates();
            };
            Resize += delegate
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    Hide();
                    WindowState = FormWindowState.Normal;
                }
                UpdateSectionWidths();
            };
            FormClosing += OnFormClosing;
        }

        private void PlaceOnDesktop()
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(Math.Max(area.Left + 8, area.Right - Width - 16), area.Top + 8);
        }

        private void RefreshSystemTheme(bool force)
        {
            const string personalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string dwmPath = @"Software\Microsoft\Windows\DWM";
            const string explorerAccentPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";
            int systemLight = ReadRegistryDword(personalizePath, "SystemUsesLightTheme", 0);
            int accentOnTaskbar = ReadRegistryDword(personalizePath, "ColorPrevalence", 0);
            uint rawAccent = ReadRegistryUInt(dwmPath, "AccentColor", 0u);
            if (rawAccent == 0u) rawAccent = ReadRegistryUInt(explorerAccentPath, "AccentColorMenu", 0u);
            if (rawAccent == 0u) rawAccent = ReadRegistryUInt(explorerAccentPath, "StartColorMenu", 0xFFD77800u);
            bool isLight = systemLight == 1;
            bool useAccent = accentOnTaskbar > 0 && !isLight;
            Color accent = DecodeDwmColor(rawAccent, Color.FromArgb(0, 120, 215));
            Color background = useAccent ? accent : (isLight ? Color.FromArgb(243, 243, 243) : Color.FromArgb(30, 30, 30));
            string signature = systemLight + "|" + accentOnTaskbar + "|" + rawAccent.ToString(CultureInfo.InvariantCulture);
            _lastThemePoll = DateTime.Now;
            if (!force && string.Equals(signature, _themeSignature, StringComparison.Ordinal)) return;

            _themeSignature = signature;
            _themeUsesLightTaskbar = GetColorLuminance(background) >= 0.55;
            _themeBackground = background;
            _themeText = ContrastText(background);
            _themeMutedText = Blend(_themeText, background, 0.34);
            _themeAccent = accent;
            _themeAccentText = Blend(accent, _themeText, 0.48);
            _themeSurface = Blend(background, _themeText, useAccent ? 0.12 : 0.055);
            _themeSurfaceAlt = Blend(background, _themeText, useAccent ? 0.20 : 0.09);
            _themeButton = Blend(background, _themeText, useAccent ? 0.24 : 0.12);
            _themeInput = Blend(_themeSurface, _themeUsesLightTaskbar ? Color.White : Color.Black, 0.12);
            _themeBorder = Blend(background, _themeText, useAccent ? 0.31 : 0.22);
            if (useAccent)
            {
                _themeSelection = Blend(background, _themeText, 0.29);
                _themeSelectionText = ContrastText(_themeSelection);
            }
            else
            {
                _themeSelection = accent;
                _themeSelectionText = ContrastText(accent);
            }

            BackColor = _themeBackground;
            ForeColor = _themeText;
            if (_trayIcon != null && _generatedIcon != null)
            {
                Icon previousIcon = _generatedIcon;
                _generatedIcon = CreateWidgetIcon();
                _trayIcon.Icon = _generatedIcon;
                previousIcon.Dispose();
            }
            if (_content != null && !_content.IsDisposed && !_buildingContent) BuildContent();
        }

        private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.Color && e.Category != UserPreferenceCategory.General) return;
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke((MethodInvoker)delegate { RefreshSystemTheme(false); }); }
            catch { }
        }

        private int ReadRegistryDword(string path, string name, int fallback)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path, false))
                {
                    object value = key == null ? null : key.GetValue(name);
                    return value == null ? fallback : Convert.ToInt32(value, CultureInfo.InvariantCulture);
                }
            }
            catch { return fallback; }
        }

        private uint ReadRegistryUInt(string path, string name, uint fallback)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path, false))
                {
                    object value = key == null ? null : key.GetValue(name);
                    return value == null ? fallback : unchecked((uint)Convert.ToInt64(value, CultureInfo.InvariantCulture));
                }
            }
            catch { return fallback; }
        }

        private Color DecodeDwmColor(uint raw, Color fallback)
        {
            if (raw == 0) return fallback;
            int red = (int)(raw & 0xFF);
            int green = (int)((raw >> 8) & 0xFF);
            int blue = (int)((raw >> 16) & 0xFF);
            Color decoded = Color.FromArgb(red, green, blue);
            return decoded == Color.Black ? fallback : decoded;
        }

        private double GetColorLuminance(Color color)
        {
            return (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;
        }

        private Color ContrastText(Color background)
        {
            return GetColorLuminance(background) >= 0.55 ? Color.FromArgb(28, 30, 34) : Color.FromArgb(246, 248, 252);
        }

        private Color Blend(Color source, Color target, double targetAmount)
        {
            double amount = Math.Max(0.0, Math.Min(1.0, targetAmount));
            return Color.FromArgb(
                (int)Math.Round(source.R * (1 - amount) + target.R * amount),
                (int)Math.Round(source.G * (1 - amount) + target.G * amount),
                (int)Math.Round(source.B * (1 - amount) + target.B * amount));
        }

        private void ResizeWidgetToContent()
        {
            if (_content == null || _content.IsDisposed) return;
            int desiredHeight = _content.Padding.Vertical;
            foreach (Panel section in _sections)
                desiredHeight += section.Height + section.Margin.Vertical;
            desiredHeight += 2;
            int maxHeight = Math.Max(90, Math.Min(860, Screen.FromControl(this).WorkingArea.Height - 16));
            desiredHeight = Math.Max(90, Math.Min(maxHeight, desiredHeight));
            if (Height != desiredHeight) Height = desiredHeight;
        }

        private void StartWindowDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void BuildContent()
        {
            _buildingContent = true;
            if (_notesBox != null)
            {
                _notesDirty = true;
                SaveNotesIfNeeded();
            }
            if (_content != null)
            {
                Controls.Remove(_content);
                _content.Dispose();
                _content = null;
            }
            _clockLabel = null;
            _dateEnglishLabel = null;
            _dateSolarLabel = null;
            _clockHintLabel = null;
            _calendarMonthLabel = null;
            _calendarModeCombo = null;
            _calendarGrid = null;
            _taskRowsPanel = null;
            _taskInput = null;
            _addTaskButton = null;
            _taskCountLabel = null;
            _notesBox = null;
            _notesStatusLabel = null;
            _pomodoroModeLabel = null;
            _pomodoroTimeLabel = null;
            _pomodoroStatusLabel = null;
            _pomodoroStartButton = null;
            _pomodoroResetButton = null;
            _pomodoroSwitchButton = null;
            _ratesStatusLabel = null;
            _ratesRefreshButton = null;
            _sections.Clear();
            _rateValueLabels.Clear();

            _content = new FlowLayoutPanel();
            _content.Dock = DockStyle.Fill;
            _content.FlowDirection = FlowDirection.TopDown;
            _content.WrapContents = false;
            _content.AutoScroll = true;
            _content.Padding = new Padding(8, 8, 8, 8);
            _content.Margin = new Padding(0);
            _content.BackColor = BackColor;
            _content.RightToLeft = _language == "fa" ? RightToLeft.Yes : RightToLeft.No;
            _content.MouseDown += StartWindowDrag;
            Controls.Add(_content);

            if (IsWidgetVisible("clock")) BuildClockSection();
            if (IsWidgetVisible("calendar")) BuildCalendarSection();
            if (IsWidgetVisible("tasks")) BuildTasksSection();
            if (IsWidgetVisible("pomodoro")) BuildPomodoroSection();
            if (IsWidgetVisible("notes")) BuildNotesSection();
            if (IsWidgetVisible("rates")) BuildRatesSection();
            UpdateSectionWidths();
            UpdateClock();
            DrawCalendar();
            RenderTasks();
            UpdatePomodoroDisplay();
            RenderRates();
            ResizeWidgetToContent();
            _buildingContent = false;
        }

        private Panel NewSection(int height)
        {
            SectionPanel panel = new SectionPanel();
            panel.BorderColor = _themeBorder;
            panel.MouseDown += StartWindowDrag;
            panel.Height = height;
            panel.Width = 400;
            panel.Margin = new Padding(0, 0, 0, 9);
            panel.Padding = new Padding(12, 10, 12, 10);
            panel.BackColor = _themeSurface;
            panel.ForeColor = ForeColor;
            _sections.Add(panel);
            _content.Controls.Add(panel);
            return panel;
        }

        private Label NewSectionTitle(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Top;
            label.Height = 25;
            label.Font = GetFont("Segoe UI Semibold", 10F, FontStyle.Bold);
            label.ForeColor = _themeText;
            label.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            label.MouseDown += StartWindowDrag;
            return label;
        }

        private void BuildClockSection()
        {
            Panel panel = NewSection(115);
            panel.BackColor = _themeSurfaceAlt;

            _clockLabel = new Label();
            _clockLabel.Dock = DockStyle.None;
            _clockLabel.Location = new Point(10, 8);
            _clockLabel.Size = new Size(panel.Width - 20, 43);
            _clockLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _clockLabel.Font = GetFont("Segoe UI Light", 31F, FontStyle.Regular, GraphicsUnit.Point);
            _clockLabel.ForeColor = _themeText;
            _clockLabel.TextAlign = ContentAlignment.MiddleCenter;
            _clockLabel.MouseDown += StartWindowDrag;
            panel.Controls.Add(_clockLabel);

            _dateEnglishLabel = new Label();
            _dateEnglishLabel.Dock = DockStyle.None;
            _dateEnglishLabel.Location = new Point(10, 51);
            _dateEnglishLabel.Size = new Size(panel.Width - 20, 21);
            _dateEnglishLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dateEnglishLabel.Font = GetFont("Segoe UI", 9F, FontStyle.Regular);
            _dateEnglishLabel.ForeColor = _themeText;
            _dateEnglishLabel.TextAlign = ContentAlignment.MiddleCenter;
            _dateEnglishLabel.MouseDown += StartWindowDrag;
            panel.Controls.Add(_dateEnglishLabel);

            _dateSolarLabel = new Label();
            _dateSolarLabel.Dock = DockStyle.None;
            _dateSolarLabel.Location = new Point(10, 72);
            _dateSolarLabel.Size = new Size(panel.Width - 20, 21);
            _dateSolarLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dateSolarLabel.Font = GetFont("Segoe UI", 9F, FontStyle.Regular);
            _dateSolarLabel.ForeColor = _themeAccentText;
            _dateSolarLabel.TextAlign = ContentAlignment.MiddleCenter;
            _dateSolarLabel.MouseDown += StartWindowDrag;
            panel.Controls.Add(_dateSolarLabel);

            _clockHintLabel = new Label();
            _clockHintLabel.Dock = DockStyle.None;
            _clockHintLabel.Location = new Point(10, 93);
            _clockHintLabel.Size = new Size(panel.Width - 20, 16);
            _clockHintLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _clockHintLabel.Font = GetFont("Segoe UI", 8F, FontStyle.Regular);
            _clockHintLabel.ForeColor = _themeMutedText;
            _clockHintLabel.TextAlign = ContentAlignment.MiddleCenter;
            _clockHintLabel.MouseDown += StartWindowDrag;
            panel.Controls.Add(_clockHintLabel);

            panel.Resize += delegate
            {
                _clockLabel.Width = panel.ClientSize.Width - 20;
                _dateEnglishLabel.Width = panel.ClientSize.Width - 20;
                _dateSolarLabel.Width = panel.ClientSize.Width - 20;
                _clockHintLabel.Width = panel.ClientSize.Width - 20;
            };
        }

        private void BuildCalendarSection()
        {
            Panel panel = NewSection(282);
            _calendarMonthLabel = NewSectionTitle(T("CALENDAR", "تقویم"));
            _calendarMonthLabel.Width = 190;
            _calendarMonthLabel.Dock = DockStyle.None;
            _calendarMonthLabel.Location = new Point(12, 8);
            _calendarMonthLabel.Height = 26;
            panel.Controls.Add(_calendarMonthLabel);

            Button previous = MakeSmallButton("‹", T("Previous month", "ماه قبل"), 30);
            previous.Location = new Point(12, 36);
            previous.Click += delegate { MoveCalendarMonth(-1); };
            panel.Controls.Add(previous);

            Button next = MakeSmallButton("›", T("Next month", "ماه بعد"), 30);
            next.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            next.Location = new Point(panel.Width - 42, 36);
            next.Click += delegate { MoveCalendarMonth(1); };
            panel.Controls.Add(next);

            Button today = MakeSmallButton(T("Today", "امروز"), T("Jump to today", "رفتن به امروز"), 60);
            today.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            today.Location = new Point(panel.Width - 108, 36);
            today.Click += delegate
            {
                _selectedDate = DateTime.Today;
                _calendarAnchor = DateTime.Today;
                DrawCalendar();
                UpdateClock();
            };
            panel.Controls.Add(today);

            _calendarModeCombo = new ComboBox();
            _calendarModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _calendarModeCombo.FlatStyle = FlatStyle.Flat;
            _calendarModeCombo.Font = GetFont("Segoe UI", 8.5F);
            _calendarModeCombo.BackColor = _themeButton;
            _calendarModeCombo.ForeColor = ForeColor;
            _calendarModeCombo.Items.Add(T("Solar Hijri", "هجری شمسی"));
            _calendarModeCombo.Items.Add(T("Gregorian", "میلادی"));
            _calendarModeCombo.SelectedIndex = _showSolarCalendar ? 0 : 1;
            _calendarModeCombo.Size = new Size(126, 26);
            _calendarModeCombo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _calendarModeCombo.Location = new Point(panel.Width - 245, 36);
            _calendarModeCombo.SelectedIndexChanged += delegate
            {
                _showSolarCalendar = _calendarModeCombo.SelectedIndex == 0;
                _calendarAnchor = _selectedDate;
                DrawCalendar();
            };
            panel.Controls.Add(_calendarModeCombo);

            _calendarGrid = new TableLayoutPanel();
            _calendarGrid.Location = new Point(12, 68);
            _calendarGrid.Size = new Size(panel.Width - 24, 204);
            _calendarGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _calendarGrid.ColumnCount = 7;
            _calendarGrid.RowCount = 7;
            _calendarGrid.Margin = new Padding(0);
            _calendarGrid.Padding = new Padding(0);
            _calendarGrid.BackColor = panel.BackColor;
            _calendarGrid.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            for (int i = 0; i < 7; i++) _calendarGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7F));
            _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            for (int i = 0; i < 6; i++) _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.Controls.Add(_calendarGrid);
            panel.Resize += delegate
            {
                next.Left = panel.ClientSize.Width - next.Width - 12;
                today.Left = next.Left - today.Width - 5;
                _calendarModeCombo.Left = today.Left - _calendarModeCombo.Width - 5;
                _calendarGrid.Width = panel.ClientSize.Width - 24;
            };
        }

        private void BuildTasksSection()
        {
            Panel panel = NewSection(135);
            Label title = NewSectionTitle(T("TASKS", "وظایف"));
            title.Location = new Point(12, 8);
            title.Height = 23;
            title.Dock = DockStyle.None;
            panel.Controls.Add(title);

            _taskCountLabel = new Label();
            _taskCountLabel.Location = new Point(panel.Width - 95, 9);
            _taskCountLabel.Size = new Size(80, 21);
            _taskCountLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _taskCountLabel.TextAlign = ContentAlignment.MiddleRight;
            _taskCountLabel.Font = GetFont("Segoe UI", 8F);
            _taskCountLabel.ForeColor = _themeMutedText;
            panel.Controls.Add(_taskCountLabel);

            _taskInput = new TextBox();
            _taskInput.Location = new Point(12, 36);
            _taskInput.Size = new Size(panel.Width - 100, 27);
            _taskInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _taskInput.BorderStyle = BorderStyle.FixedSingle;
            _taskInput.BackColor = _themeInput;
            _taskInput.ForeColor = ForeColor;
            _taskInput.Font = GetFont("Segoe UI", 9F);
            _taskInput.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { AddTask(); e.SuppressKeyPress = true; }
            };
            panel.Controls.Add(_taskInput);

            _addTaskButton = MakeSmallButton(T("Add", "افزودن"), T("Add task", "افزودن وظیفه"), 72);
            _addTaskButton.Location = new Point(panel.Width - 84, 35);
            _addTaskButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _addTaskButton.Height = 29;
            _addTaskButton.Click += delegate { AddTask(); };
            panel.Controls.Add(_addTaskButton);

            _taskRowsPanel = new Panel();
            _taskRowsPanel.Location = new Point(12, 70);
            _taskRowsPanel.Size = new Size(panel.Width - 24, 54);
            _taskRowsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _taskRowsPanel.AutoScroll = true;
            _taskRowsPanel.BackColor = panel.BackColor;
            panel.Controls.Add(_taskRowsPanel);

            panel.Resize += delegate
            {
                _taskCountLabel.Left = panel.ClientSize.Width - _taskCountLabel.Width - 12;
                _taskInput.Width = Math.Max(100, panel.ClientSize.Width - 112);
                _addTaskButton.Left = panel.ClientSize.Width - _addTaskButton.Width - 12;
                _taskRowsPanel.Width = panel.ClientSize.Width - 24;
                RenderTasks();
            };
            RenderTasks();
        }

        private void BuildPomodoroSection()
        {
            Panel panel = NewSection(132);
            _pomodoroModeLabel = NewSectionTitle("");
            panel.Controls.Add(_pomodoroModeLabel);

            _pomodoroTimeLabel = new Label();
            _pomodoroTimeLabel.Location = new Point(12, 34);
            _pomodoroTimeLabel.Size = new Size(175, 45);
            _pomodoroTimeLabel.Font = GetFont("Segoe UI Light", 27F, FontStyle.Regular);
            _pomodoroTimeLabel.ForeColor = _themeText;
            _pomodoroTimeLabel.TextAlign = ContentAlignment.MiddleLeft;
            panel.Controls.Add(_pomodoroTimeLabel);

            _pomodoroStartButton = MakeSmallButton(T("Start", "شروع"), T("Start or pause timer", "شروع یا توقف زمان‌سنج"), 77);
            _pomodoroStartButton.Location = new Point(12, 82);
            _pomodoroStartButton.Click += delegate { TogglePomodoro(); };
            panel.Controls.Add(_pomodoroStartButton);

            _pomodoroResetButton = MakeSmallButton(T("Reset", "بازنشانی"), T("Reset current session", "بازنشانی نشست جاری"), 75);
            _pomodoroResetButton.Location = new Point(96, 82);
            _pomodoroResetButton.Click += delegate { ResetPomodoro(); };
            panel.Controls.Add(_pomodoroResetButton);

            _pomodoroSwitchButton = MakeSmallButton(T("Switch mode", "تغییر حالت"), T("Switch between focus and break", "تغییر بین تمرکز و استراحت"), 100);
            _pomodoroSwitchButton.Location = new Point(178, 82);
            _pomodoroSwitchButton.Click += delegate { SwitchPomodoroMode(); };
            panel.Controls.Add(_pomodoroSwitchButton);

            _pomodoroStatusLabel = new Label();
            _pomodoroStatusLabel.Location = new Point(198, 42);
            _pomodoroStatusLabel.Size = new Size(panel.Width - 214, 35);
            _pomodoroStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _pomodoroStatusLabel.Font = GetFont("Segoe UI", 8F);
            _pomodoroStatusLabel.ForeColor = _themeMutedText;
            _pomodoroStatusLabel.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            panel.Controls.Add(_pomodoroStatusLabel);
            panel.Resize += delegate { _pomodoroStatusLabel.Width = Math.Max(70, panel.ClientSize.Width - 214); };
        }

        private void BuildNotesSection()
        {
            Panel panel = NewSection(164);
            Label title = NewSectionTitle(T("QUICK NOTE", "یادداشت سریع"));
            title.Location = new Point(12, 8);
            title.Height = 23;
            title.Dock = DockStyle.None;
            panel.Controls.Add(title);

            _notesStatusLabel = new Label();
            _notesStatusLabel.Text = T("Saved locally", "ذخیره‌شده روی دستگاه");
            _notesStatusLabel.Location = new Point(panel.Width - 145, 9);
            _notesStatusLabel.Size = new Size(132, 21);
            _notesStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _notesStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            _notesStatusLabel.Font = GetFont("Segoe UI", 8F);
            _notesStatusLabel.ForeColor = _themeMutedText;
            panel.Controls.Add(_notesStatusLabel);

            _notesBox = new TextBox();
            _notesBox.Location = new Point(12, 36);
            _notesBox.Size = new Size(panel.Width - 24, 116);
            _notesBox.Multiline = true;
            _notesBox.AcceptsReturn = true;
            _notesBox.AcceptsTab = true;
            _notesBox.ScrollBars = ScrollBars.Vertical;
            _notesBox.BorderStyle = BorderStyle.FixedSingle;
            _notesBox.BackColor = _themeInput;
            _notesBox.ForeColor = _themeText;
            _notesBox.Font = GetFont("Segoe UI", 9F);
            _notesBox.Text = LocalStore.LoadNotes();
            _notesBox.TextChanged += delegate
            {
                _notesDirty = true;
                if (_notesStatusLabel != null) _notesStatusLabel.Text = T("Unsaved changes", "تغییرات ذخیره‌نشده");
            };
            _notesBox.Leave += delegate { SaveNotesIfNeeded(); };
            panel.Controls.Add(_notesBox);
            panel.Resize += delegate
            {
                _notesStatusLabel.Left = panel.ClientSize.Width - _notesStatusLabel.Width - 12;
                _notesBox.Width = panel.ClientSize.Width - 24;
            };
        }

        private void BuildRatesSection()
        {
            Panel panel = NewSection(145);
            Label title = NewSectionTitle(T("LIVE MARKET RATES  ·  TGJU", "نرخ لحظه‌ای بازار  ·  TGJU"));
            title.Location = new Point(12, 8);
            title.Width = 270;
            title.Height = 23;
            title.Dock = DockStyle.None;
            panel.Controls.Add(title);

            _ratesRefreshButton = MakeSmallButton("↻", T("Refresh market rates", "به‌روزرسانی نرخ‌ها"), 30);
            _ratesRefreshButton.Location = new Point(panel.Width - 42, 7);
            _ratesRefreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _ratesRefreshButton.Click += delegate { RefreshRates(); };
            panel.Controls.Add(_ratesRefreshButton);

            int y = 36;
            for (int i = 0; i < _rateDefinitions.Length; i++)
            {
                MarketRate rate = _rateDefinitions[i];
                Label name = new Label();
                name.Text = _language == "fa" ? rate.DisplayNamePersian : rate.DisplayNameEnglish;
                name.Location = new Point(12, y);
                name.Size = new Size(145, 23);
                name.ForeColor = _themeText;
                name.Font = GetFont("Segoe UI", 9F);
                name.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                name.Cursor = Cursors.Hand;
                name.Tag = rate.Url;
                name.Click += OpenMarketPage;
                panel.Controls.Add(name);

                Label value = new Label();
                value.Text = "—";
                value.Location = new Point(158, y);
                value.Size = new Size(panel.Width - 174, 23);
                value.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                value.ForeColor = _themeAccentText;
                value.Font = GetFont("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                value.TextAlign = _language == "fa" ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight;
                value.Tag = rate.Url;
                value.Cursor = Cursors.Hand;
                value.Click += OpenMarketPage;
                panel.Controls.Add(value);
                _rateValueLabels[rate.Key] = value;
                y += 24;
            }

            LinkLabel source = new LinkLabel();
            source.Text = T("Open TGJU source pages", "بازکردن صفحات منبع TGJU");
            source.Location = new Point(12, 108);
            source.Size = new Size(210, 18);
            source.LinkColor = _themeAccentText;
            source.ActiveLinkColor = _themeAccentText;
            source.VisitedLinkColor = _themeAccentText;
            source.Font = GetFont("Segoe UI", 8F);
            source.LinkClicked += delegate { OpenUrl("https://www.tgju.org/"); };
            panel.Controls.Add(source);

            _ratesStatusLabel = new Label();
            _ratesStatusLabel.Text = T("Waiting for live data…", "در انتظار داده زنده…");
            _ratesStatusLabel.Location = new Point(12, 124);
            _ratesStatusLabel.Size = new Size(panel.Width - 24, 15);
            _ratesStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _ratesStatusLabel.Font = GetFont("Segoe UI", 7.5F);
            _ratesStatusLabel.ForeColor = _themeMutedText;
            _ratesStatusLabel.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            panel.Controls.Add(_ratesStatusLabel);
            panel.Resize += delegate
            {
                _ratesRefreshButton.Left = panel.ClientSize.Width - _ratesRefreshButton.Width - 12;
                foreach (Control child in panel.Controls)
                {
                    if (child is Label && child.Tag is string && child.Location.Y >= 36 && child.Location.Y < 108 && child.Location.X >= 150)
                        child.Width = panel.ClientSize.Width - child.Left - 12;
                }
                _ratesStatusLabel.Width = panel.ClientSize.Width - 24;
            };
        }

        private Button MakeSmallButton(string text, string tooltip, int width)
        {
            Button b = new Button();
            b.Text = text;
            b.Width = width;
            b.Height = 25;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = _themeBorder;
            b.BackColor = _themeButton;
            b.ForeColor = _themeText;
            b.Font = GetFont("Segoe UI", 8.5F, FontStyle.Regular);
            b.Cursor = Cursors.Hand;
            b.Margin = new Padding(1);
            _toolTip.SetToolTip(b, tooltip);
            return b;
        }

        private void UpdateSectionWidths()
        {
            if (_content == null) return;
            int width = Math.Max(330, _content.ClientSize.Width - _content.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 2);
            foreach (Panel p in _sections)
            {
                p.Width = width;
                foreach (Control c in p.Controls)
                {
                    if (c is Label && c.Dock == DockStyle.Top && c.Name != "") continue;
                }
            }
            if (_calendarGrid != null) _calendarGrid.Width = width - 24;
            if (_taskInput != null) _taskInput.Width = width - 112;
            if (_taskRowsPanel != null) _taskRowsPanel.Width = width - 24;
            if (_notesBox != null) _notesBox.Width = width - 24;
        }

        private void UpdateClock()
        {
            if (_clockLabel == null || IsDisposed) return;
            DateTime now = DateTime.Now;
            CultureInfo english = CultureInfo.GetCultureInfo("en-US");
            string time = now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            _clockLabel.Text = _language == "fa" ? ToPersianDigits(time) : time;

            string gregorian = now.ToString("dddd, dd MMMM yyyy", english);
            string solar = GetSolarLongDate(now, _language == "fa");
            _dateEnglishLabel.Text = _language == "fa" ? "میلادی  ·  " + ToPersianDigits(now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)) : gregorian;
            _dateSolarLabel.Text = _language == "fa" ? solar : "Solar Hijri  ·  " + GetSolarLongDate(now, false);
            _clockHintLabel.Text = _language == "fa" ? "زمان محلی سیستم" : "Local system time";

            if (now.Date != _calendarAnchor.Date && _calendarAnchor.Date == _selectedDate.Date && _calendarAnchor.Date == now.Date.AddDays(-1))
            {
                _calendarAnchor = DateTime.Today;
                _selectedDate = DateTime.Today;
                DrawCalendar();
            }
        }

        private string GetSolarLongDate(DateTime date, bool persianDigits)
        {
            int year = _persianCalendar.GetYear(date);
            int month = _persianCalendar.GetMonth(date);
            int day = _persianCalendar.GetDayOfMonth(date);
            string monthName = persianDigits ? _solarMonthsFa[month - 1] : _solarMonthsEn[month - 1];
            string value = day + " " + monthName + " " + year;
            return persianDigits ? ToPersianDigits(value) : value;
        }

        private Font GetFont(string family, float size, FontStyle style = FontStyle.Regular, GraphicsUnit unit = GraphicsUnit.Point)
        {
            string key = family + "|" + size.ToString("R", CultureInfo.InvariantCulture) + "|" + ((int)style).ToString(CultureInfo.InvariantCulture) + "|" + ((int)unit).ToString(CultureInfo.InvariantCulture);
            Font font;
            if (!_fontCache.TryGetValue(key, out font))
            {
                font = new Font(family, size, style, unit);
                _fontCache.Add(key, font);
            }
            return font;
        }

        private static void ClearAndDisposeControls(Control parent)
        {
            while (parent.Controls.Count > 0)
            {
                Control child = parent.Controls[0];
                parent.Controls.RemoveAt(0);
                child.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Let WinForms dispose controls before releasing the shared Font instances they reference.
                base.Dispose(true);
                if (_toolTip != null)
                {
                    _toolTip.Dispose();
                    _toolTip = null;
                }
                foreach (Font font in _fontCache.Values) font.Dispose();
                _fontCache.Clear();
                return;
            }
            base.Dispose(false);
        }

        private void DrawCalendar()
        {
            if (_calendarGrid == null || _calendarGrid.IsDisposed) return;
            _calendarGrid.SuspendLayout();
            ClearAndDisposeControls(_calendarGrid);
            _calendarGrid.ColumnStyles.Clear();
            _calendarGrid.RowStyles.Clear();
            for (int i = 0; i < 7; i++) _calendarGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7F));
            _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            for (int i = 0; i < 6; i++) _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            _calendarGrid.RowCount = 7;
            _calendarGrid.ColumnCount = 7;

            string[] weekdays = _language == "fa" ? _weekdaysFa : _weekdaysEn;
            for (int col = 0; col < 7; col++)
            {
                int index = _showSolarCalendar ? (col + 5) % 7 : col;
                Label weekday = new Label();
                weekday.Text = _language == "fa" ? weekdays[index] : weekdays[index];
                if (_showSolarCalendar && _language != "fa") weekday.Text = new string[] { "Sat", "Sun", "Mon", "Tue", "Wed", "Thu", "Fri" }[col];
                weekday.Dock = DockStyle.Fill;
                weekday.Margin = new Padding(1);
                weekday.TextAlign = ContentAlignment.MiddleCenter;
                weekday.Font = GetFont("Segoe UI", _language == "fa" ? 7.1F : 7.5F, FontStyle.Regular);
                weekday.ForeColor = _themeMutedText;
                _calendarGrid.Controls.Add(weekday, col, 0);
            }

            int year, month;
            if (_showSolarCalendar)
            {
                year = _persianCalendar.GetYear(_calendarAnchor);
                month = _persianCalendar.GetMonth(_calendarAnchor);
            }
            else
            {
                year = _calendarAnchor.Year;
                month = _calendarAnchor.Month;
            }
            DateTime first;
            string monthText;
            if (_showSolarCalendar)
            {
                first = _persianCalendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
                monthText = (_language == "fa" ? _solarMonthsFa[month - 1] : _solarMonthsEn[month - 1]) + "  " + (_language == "fa" ? ToPersianDigits(year.ToString()) : year.ToString());
            }
            else
            {
                first = new DateTime(year, month, 1);
                if (_language == "fa")
                {
                    string[] gregorianMonthsFa = { "ژانویه", "فوریه", "مارس", "آوریل", "مه", "ژوئن", "ژوئیه", "اوت", "سپتامبر", "اکتبر", "نوامبر", "دسامبر" };
                    monthText = gregorianMonthsFa[month - 1] + " " + ToPersianDigits(year.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    monthText = first.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-US"));
                }
            }
            _calendarMonthLabel.Text = monthText;
            int offset = _showSolarCalendar ? ((int)first.DayOfWeek + 1) % 7 : ((int)first.DayOfWeek + 6) % 7;
            for (int slot = 0; slot < 42; slot++)
            {
                DateTime date = first.AddDays(slot - offset);
                int number = _showSolarCalendar ? _persianCalendar.GetDayOfMonth(date) : date.Day;
                int displayYear = _showSolarCalendar ? _persianCalendar.GetYear(date) : date.Year;
                int displayMonth = _showSolarCalendar ? _persianCalendar.GetMonth(date) : date.Month;
                Button dayButton = new Button();
                dayButton.Dock = DockStyle.Fill;
                dayButton.Margin = new Padding(1);
                dayButton.FlatStyle = FlatStyle.Flat;
                dayButton.FlatAppearance.BorderSize = 0;
                bool currentMonth = displayYear == year && displayMonth == month;
                bool todayDate = date.Date == DateTime.Today;
                bool selected = date.Date == _selectedDate.Date;
                dayButton.Text = _language == "fa" ? ToPersianDigits(number.ToString()) : number.ToString();
                dayButton.Font = GetFont("Segoe UI", 8.2F, selected ? FontStyle.Bold : FontStyle.Regular);
                dayButton.Cursor = Cursors.Hand;
                dayButton.Tag = date.Date;
                if (selected)
                {
                    dayButton.BackColor = _themeSelection;
                    dayButton.ForeColor = _themeSelectionText;
                    dayButton.FlatAppearance.BorderSize = 1;
                    dayButton.FlatAppearance.BorderColor = _themeAccentText;
                }
                else if (todayDate)
                {
                    dayButton.BackColor = _themeButton;
                    dayButton.ForeColor = _themeAccentText;
                }
                else
                {
                    dayButton.BackColor = currentMonth ? _themeSurfaceAlt : _themeSurface;
                    dayButton.ForeColor = _themeText;
                }
                dayButton.Click += delegate(object sender, EventArgs e)
                {
                    Button clicked = (Button)sender;
                    _selectedDate = ((DateTime)clicked.Tag).Date;
                    _calendarAnchor = _selectedDate;
                    DrawCalendar();
                };
                _calendarGrid.Controls.Add(dayButton, slot % 7, 1 + slot / 7);
            }
            _calendarGrid.ResumeLayout();
        }

        private void MoveCalendarMonth(int delta)
        {
            try
            {
                if (_showSolarCalendar)
                    _calendarAnchor = _persianCalendar.AddMonths(_calendarAnchor, delta);
                else
                    _calendarAnchor = _calendarAnchor.AddMonths(delta);
                DrawCalendar();
            }
            catch { }
        }

        private void AddTask()
        {
            if (_taskInput == null) return;
            string text = (_taskInput.Text ?? string.Empty).Trim();
            if (text.Length == 0) return;
            _tasks.Add(new TaskItem { Id = Guid.NewGuid().ToString("N"), Title = text, Done = false });
            LocalStore.SaveTasks(_tasks);
            _taskInput.Clear();
            RenderTasks();
            _taskInput.Focus();
        }

        private void RenderTasks()
        {
            if (_taskRowsPanel == null || _taskRowsPanel.IsDisposed) return;
            _taskRowsPanel.SuspendLayout();
            ClearAndDisposeControls(_taskRowsPanel);
            int y = 0;
            if (_tasks.Count == 0)
            {
                Label empty = new Label();
                empty.Text = T("No tasks yet — add one above.", "هنوز وظیفه‌ای ندارید؛ یک مورد اضافه کنید.");
                empty.Location = new Point(2, 3);
                empty.Size = new Size(Math.Max(220, _taskRowsPanel.ClientSize.Width - 10), 26);
                empty.ForeColor = _themeMutedText;
                empty.Font = GetFont("Segoe UI", 8.5F);
                empty.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                _taskRowsPanel.Controls.Add(empty);
                y = 30;
            }
            else
            {
                foreach (TaskItem task in _tasks)
                {
                    CheckBox check = new CheckBox();
                    check.AutoSize = false;
                    check.Location = new Point(0, y + 2);
                    check.Size = new Size(Math.Max(180, _taskRowsPanel.ClientSize.Width - 36), 25);
                    check.Text = task.Title;
                    check.Checked = task.Done;
                    check.Tag = task;
                    check.FlatStyle = FlatStyle.Flat;
                    check.ForeColor = task.Done ? _themeMutedText : _themeText;
                    check.Font = GetFont("Segoe UI", 8.8F, task.Done ? FontStyle.Strikeout : FontStyle.Regular);
                    check.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                    check.CheckAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                    check.Padding = _language == "fa" ? new Padding(0, 0, 5, 0) : new Padding(5, 0, 0, 0);
                    check.CheckedChanged += delegate(object sender, EventArgs e)
                    {
                        CheckBox changed = (CheckBox)sender;
                        TaskItem item = (TaskItem)changed.Tag;
                        item.Done = changed.Checked;
                        LocalStore.SaveTasks(_tasks);
                        RenderTasks();
                    };
                    _taskRowsPanel.Controls.Add(check);

                    Button remove = MakeSmallButton("×", T("Remove task", "حذف وظیفه"), 26);
                    remove.Height = 23;
                    remove.Location = new Point(_taskRowsPanel.ClientSize.Width - 27, y + 2);
                    remove.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                    remove.Tag = task;
                    remove.Click += delegate(object sender, EventArgs e)
                    {
                        TaskItem item = (TaskItem)((Button)sender).Tag;
                        _tasks.Remove(item);
                        LocalStore.SaveTasks(_tasks);
                        RenderTasks();
                    };
                    _taskRowsPanel.Controls.Add(remove);
                    y += 29;
                }
            }
            _taskRowsPanel.AutoScrollMinSize = new Size(0, y);
            int visibleRows = Math.Min(Math.Max(_tasks.Count, 1), 5);
            _taskRowsPanel.Height = Math.Max(31, visibleRows * 29 + 2);
            Panel tasksSection = FindSectionContaining(_taskRowsPanel);
            if (tasksSection != null) tasksSection.Height = 78 + _taskRowsPanel.Height + 8;
            int completed = 0;
            foreach (TaskItem item in _tasks) if (item.Done) completed++;
            if (_taskCountLabel != null) _taskCountLabel.Text = _language == "fa" ? ToPersianDigits(completed + " از " + _tasks.Count) : completed + " / " + _tasks.Count + " done";
            _taskRowsPanel.ResumeLayout();
            if (_content != null) _content.PerformLayout();
            if (!_buildingContent) ResizeWidgetToContent();
        }

        private Panel FindSectionContaining(Control child)
        {
            Control parent = child.Parent;
            return parent as Panel;
        }

        private void TogglePomodoro()
        {
            if (_timerRunning)
            {
                _remainingSeconds = Math.Max(0, (int)Math.Ceiling((_timerEndsAt - DateTime.Now).TotalSeconds));
                _timerRunning = false;
                _pomodoroStatusLabel.Text = T("Paused", "متوقف شده");
            }
            else
            {
                if (_remainingSeconds <= 0) _remainingSeconds = _isBreak ? 5 * 60 : 25 * 60;
                _timerEndsAt = DateTime.Now.AddSeconds(_remainingSeconds);
                _timerRunning = true;
                _pomodoroStatusLabel.Text = T("Session in progress", "نشست در حال اجراست");
            }
            UpdatePomodoroDisplay();
        }

        private void ResetPomodoro()
        {
            _timerRunning = false;
            _remainingSeconds = _isBreak ? 5 * 60 : 25 * 60;
            _pomodoroStatusLabel.Text = T("Ready when you are", "آماده شروع");
            UpdatePomodoroDisplay();
        }

        private void SwitchPomodoroMode()
        {
            _timerRunning = false;
            _isBreak = !_isBreak;
            _remainingSeconds = _isBreak ? 5 * 60 : 25 * 60;
            _pomodoroStatusLabel.Text = _isBreak ? T("Rest, then refocus", "استراحت و سپس تمرکز") : T("Ready when you are", "آماده شروع");
            UpdatePomodoroDisplay();
        }

        private void UpdatePomodoroDisplay()
        {
            if (_pomodoroTimeLabel == null || _pomodoroTimeLabel.IsDisposed) return;
            int seconds = _remainingSeconds;
            if (_timerRunning) seconds = Math.Max(0, (int)Math.Ceiling((_timerEndsAt - DateTime.Now).TotalSeconds));
            string display = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            if (_language == "fa") display = ToPersianDigits(display);
            _pomodoroTimeLabel.Text = display;
            _pomodoroModeLabel.Text = _isBreak ? T("POMODORO  ·  BREAK", "پومودورو  ·  استراحت") : T("POMODORO  ·  FOCUS", "پومودورو  ·  تمرکز");
            _pomodoroStartButton.Text = _timerRunning ? T("Pause", "توقف") : T("Start", "شروع");
            if (_pomodoroStatusLabel != null && !_timerRunning && string.IsNullOrEmpty(_pomodoroStatusLabel.Text))
                _pomodoroStatusLabel.Text = T("Ready when you are", "آماده شروع");
        }

        private void OnTick(object sender, EventArgs e)
        {
            UpdateClock();
            if ((DateTime.Now - _lastThemePoll).TotalSeconds >= 10) RefreshSystemTheme(false);
            if (_timerRunning)
            {
                int seconds = (int)Math.Ceiling((_timerEndsAt - DateTime.Now).TotalSeconds);
                if (seconds <= 0)
                {
                    _timerRunning = false;
                    _remainingSeconds = 0;
                    bool completedBreak = _isBreak;
                    _isBreak = !_isBreak;
                    _remainingSeconds = _isBreak ? 5 * 60 : 25 * 60;
                    _pomodoroStatusLabel.Text = completedBreak ? T("Break complete — ready to focus", "استراحت پایان یافت؛ آماده تمرکز") : T("Focus complete — take a break", "تمرکز پایان یافت؛ کمی استراحت کنید");
                    try
                    {
                        _trayIcon.ShowBalloonTip(3500, AppName,
                            completedBreak ? T("Break finished. Ready for a focus session?", "استراحت تمام شد. آماده تمرکز هستید؟") : T("Focus session finished. Time for a break.", "نشست تمرکز تمام شد؛ زمان استراحت است."),
                            ToolTipIcon.Info);
                        System.Media.SystemSounds.Asterisk.Play();
                    }
                    catch { }
                }
            }
            UpdatePomodoroDisplay();
            if (_notesDirty) SaveNotesIfNeeded();
            if (IsWidgetVisible("rates") && !_rateFetchInProgress && (DateTime.Now - _lastRateRefreshLocal).TotalMinutes >= 5)
                RefreshRates();
        }

        private void RefreshRates()
        {
            if (_rateFetchInProgress || IsDisposed) return;
            _rateFetchInProgress = true;
            _remainingRateFetches = _rateDefinitions.Length;
            foreach (MarketRate item in _rateDefinitions) _rateFreshThisFetch[item.Key] = false;
            RenderRates();
            if (_ratesStatusLabel != null) _ratesStatusLabel.Text = T("Fetching TGJU quotes…", "در حال دریافت نرخ از TGJU…");
            if (_ratesRefreshButton != null) _ratesRefreshButton.Enabled = false;
            foreach (MarketRate definition in _rateDefinitions)
            {
                MarketRate def = definition;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    MarketRate result = TgjuClient.Fetch(def);
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (IsDisposed) return;
                            if (!string.IsNullOrEmpty(result.Value))
                            {
                                _rateValues[result.Key] = result.Value;
                                _rateErrors[result.Key] = string.Empty;
                                _rateFreshThisFetch[result.Key] = true;
                            }
                            else
                            {
                                _rateErrors[result.Key] = result.Error ?? T("Unknown error", "خطای نامشخص");
                                _rateFreshThisFetch[result.Key] = false;
                            }
                            RenderRates();
                            if (Interlocked.Decrement(ref _remainingRateFetches) == 0)
                            {
                                _rateFetchInProgress = false;
                                _lastRateRefreshLocal = DateTime.Now;
                                if (_ratesRefreshButton != null) _ratesRefreshButton.Enabled = true;
                                int succeeded = 0;
                                foreach (MarketRate item in _rateDefinitions) if (_rateFreshThisFetch[item.Key]) succeeded++;
                                if (_ratesStatusLabel != null)
                                {
                                    if (succeeded == _rateDefinitions.Length)
                                        _ratesStatusLabel.Text = T("Updated ", "به‌روزرسانی ") + (_language == "fa" ? ToPersianDigits(DateTime.Now.ToString("HH:mm")) : DateTime.Now.ToString("HH:mm")) + T(" · TGJU · values in Toman", " · TGJU · قیمت‌ها به تومان");
                                    else if (succeeded > 0)
                                        _ratesStatusLabel.Text = T("Partially updated · check connection", "به‌روزرسانی ناقص · اتصال را بررسی کنید");
                                    else
                                        _ratesStatusLabel.Text = T("Could not load live data · tap refresh", "داده زنده دریافت نشد · دوباره تلاش کنید");
                                }
                            }
                        });
                    }
                    catch { }
                });
            }
        }

        private void RenderRates()
        {
            foreach (MarketRate definition in _rateDefinitions)
            {
                Label label;
                if (!_rateValueLabels.TryGetValue(definition.Key, out label) || label == null || label.IsDisposed) continue;
                string value;
                if (_rateValues.TryGetValue(definition.Key, out value) && !string.IsNullOrEmpty(value))
                {
                    string formatted;
                    long parsed;
                    if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                        formatted = (parsed / 10L).ToString("N0", CultureInfo.InvariantCulture);
                    else formatted = value;
                    if (_language == "fa") formatted = ToPersianDigits(formatted) + " تومان (T)";
                    else formatted += " T";
                    label.Text = formatted;
                    label.ForeColor = _rateFreshThisFetch[definition.Key] ? _themeAccentText : _themeMutedText;
                }
                else
                {
                    label.Text = "—";
                    label.ForeColor = _themeMutedText;
                }
            }
        }

        private void BuildTrayIcon()
        {
            _generatedIcon = CreateWidgetIcon();
            _trayMenu = CreateTrayMenu();
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = _generatedIcon;
            _trayIcon.Text = AppName;
            _trayIcon.ContextMenuStrip = _trayMenu;
            _trayIcon.Visible = true;
            _trayIcon.MouseDoubleClick += delegate { ShowFromTray(); };
            _trayIcon.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) ShowFromTray();
            };
        }

        private ContextMenuStrip CreateTrayMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem showItem = new ToolStripMenuItem(T("Show widget", "نمایش ویجت"));
            showItem.Enabled = AnyWidgetVisible() && !Visible;
            showItem.Click += delegate { ShowFromTray(); };
            menu.Items.Add(showItem);

            ToolStripMenuItem hideItem = new ToolStripMenuItem(T("Hide widget", "مخفی‌کردن ویجت"));
            hideItem.Enabled = Visible;
            hideItem.Click += delegate { HideToTray(); };
            menu.Items.Add(hideItem);
            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem widgetsItem = new ToolStripMenuItem(T("Visible widgets", "ویجت‌های قابل نمایش"));
            AddWidgetToggle(widgetsItem, "clock", T("Clock and dates", "ساعت و تاریخ"));
            AddWidgetToggle(widgetsItem, "calendar", T("Calendar", "تقویم"));
            AddWidgetToggle(widgetsItem, "tasks", T("Tasks", "وظایف"));
            AddWidgetToggle(widgetsItem, "pomodoro", T("Pomodoro timer", "زمان‌سنج پومودورو"));
            AddWidgetToggle(widgetsItem, "notes", T("Quick note", "یادداشت سریع"));
            AddWidgetToggle(widgetsItem, "rates", T("Live market rates", "نرخ لحظه‌ای بازار"));
            menu.Items.Add(widgetsItem);
            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem refreshItem = new ToolStripMenuItem(T("Refresh market rates", "به‌روزرسانی نرخ‌ها"));
            refreshItem.Enabled = IsWidgetVisible("rates");
            refreshItem.Click += delegate { RefreshRates(); };
            menu.Items.Add(refreshItem);

            ToolStripMenuItem languageItem = new ToolStripMenuItem(T("Language", "زبان"));
            ToolStripMenuItem englishItem = new ToolStripMenuItem("English");
            ToolStripMenuItem persianItem = new ToolStripMenuItem("فارسی");
            englishItem.Checked = _language == "en";
            persianItem.Checked = _language == "fa";
            englishItem.Click += delegate { SetLanguage("en"); };
            persianItem.Click += delegate { SetLanguage("fa"); };
            languageItem.DropDownItems.Add(englishItem);
            languageItem.DropDownItems.Add(persianItem);
            menu.Items.Add(languageItem);

            ToolStripMenuItem topItem = new ToolStripMenuItem(T("Keep on top", "همیشه روی پنجره‌ها"));
            topItem.Checked = _keepOnTop;
            topItem.CheckOnClick = true;
            topItem.Click += delegate { SetTopmost(topItem.Checked); };
            menu.Items.Add(topItem);

            ToolStripMenuItem startupItem = new ToolStripMenuItem(T("Start with Windows", "اجرا هنگام ورود به ویندوز"));
            startupItem.Checked = IsStartupEnabled();
            startupItem.CheckOnClick = true;
            startupItem.Click += delegate { SetStartup(startupItem.Checked); };
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem exitItem = new ToolStripMenuItem(T("Exit", "خروج"));
            exitItem.Click += delegate { ExitApplication(); };
            menu.Items.Add(exitItem);
            return menu;
        }

        private void AddWidgetToggle(ToolStripMenuItem parent, string key, string label)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(label);
            item.Checked = IsWidgetVisible(key);
            item.CheckOnClick = true;
            item.Click += delegate { SetWidgetVisibility(key, item.Checked); };
            parent.DropDownItems.Add(item);
        }

        private bool IsWidgetVisible(string key)
        {
            bool visible;
            return _widgetVisibility.TryGetValue(key, out visible) && visible;
        }

        private bool AnyWidgetVisible()
        {
            foreach (KeyValuePair<string, bool> pair in _widgetVisibility)
                if (pair.Value) return true;
            return false;
        }

        private void SetWidgetVisibility(string key, bool visible)
        {
            if (!_widgetVisibility.ContainsKey(key) || _widgetVisibility[key] == visible) return;
            SaveNotesIfNeeded();
            _widgetVisibility[key] = visible;
            LocalStore.SaveSetting("widget." + key, visible ? "1" : "0");
            BuildContent();
            BuildTrayMenu();
            if (!AnyWidgetVisible()) Hide();
            else if (visible)
            {
                ShowFromTray();
                if (key == "rates") RefreshRates();
            }
        }

        private Icon CreateWidgetIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Brush b = new SolidBrush(_themeBackground)) g.FillEllipse(b, 1, 1, 30, 30);
                using (Pen p = new Pen(_themeAccentText, 2.2F)) g.DrawEllipse(p, 4, 4, 24, 24);
                using (Pen p = new Pen(_themeText, 2.4F))
                {
                    g.DrawLine(p, 16, 8, 16, 16);
                    g.DrawLine(p, 16, 16, 22, 19);
                }
            }
            IntPtr hIcon = bmp.GetHicon();
            Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
            DestroyIcon(hIcon);
            bmp.Dispose();
            return icon;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private void ToggleTopmost()
        {
            SetTopmost(!_keepOnTop);
        }

        private void SetTopmost(bool enabled)
        {
            _keepOnTop = enabled;
            TopMost = enabled;
            LocalStore.SaveSetting("topmost", enabled ? "1" : "0");
            BuildTrayMenu();
        }

        private void BuildTrayMenu()
        {
            if (_trayIcon == null || IsDisposed) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (_trayIcon == null || IsDisposed || _exitRequested) return;
                    ContextMenuStrip oldMenu = _trayIcon.ContextMenuStrip;
                    BuildTrayIconMenuOnly();
                    if (oldMenu != null && oldMenu != _trayIcon.ContextMenuStrip) oldMenu.Dispose();
                });
            }
            catch { }
        }

        private void BuildTrayIconMenuOnly()
        {
            if (_trayIcon == null) return;
            ContextMenuStrip menu = CreateTrayMenu();
            _trayIcon.ContextMenuStrip = menu;
            _trayMenu = menu;
        }

        private void SetLanguage(string language)
        {
            if (language != "fa") language = "en";
            if (_language == language) return;
            SaveNotesIfNeeded();
            _language = language;
            LocalStore.SaveSetting("language", _language);
            BuildContent();
            BuildTrayMenu();
        }

        private bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    object value = key.GetValue(RunValueName);
                    return value != null && value.ToString().IndexOf(Path.GetFileName(Application.ExecutablePath), StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch { return false; }
        }

        private void SetStartup(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null) throw new InvalidOperationException("Could not access the current-user Run registry key.");
                    if (enabled) key.SetValue(RunValueName, "\"" + Application.ExecutablePath + "\"");
                    else key.DeleteValue(RunValueName, false);
                }
                BuildTrayMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show(T("Could not change startup setting: ", "تغییر تنظیم شروع ویندوز ناموفق بود: ") + ex.Message,
                    AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowFromTray()
        {
            if (IsDisposed || !AnyWidgetVisible()) return;
            Show();
            WindowState = FormWindowState.Normal;
            TopMost = _keepOnTop;
            Activate();
            BringToFront();
            BuildTrayMenu();
        }

        private void HideToTray()
        {
            SaveNotesIfNeeded();
            Hide();
            BuildTrayMenu();
        }

        private void ExitApplication()
        {
            SaveNotesIfNeeded();
            LocalStore.SaveTasks(_tasks);
            _exitRequested = true;
            if (_tickTimer != null) _tickTimer.Stop();
            if (_trayIcon != null) _trayIcon.Visible = false;
            Close();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
            SaveNotesIfNeeded();
            SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
            try { LocalStore.SaveTasks(_tasks); } catch { }
            if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
            if (_tickTimer != null) { _tickTimer.Stop(); _tickTimer.Dispose(); }
            if (_trayMenu != null) _trayMenu.Dispose();
            if (_generatedIcon != null) _generatedIcon.Dispose();
        }

        private void SaveNotesIfNeeded()
        {
            if (!_notesDirty || _notesBox == null || _notesBox.IsDisposed) return;
            try
            {
                LocalStore.SaveNotes(_notesBox.Text);
                _notesDirty = false;
                if (_notesStatusLabel != null) _notesStatusLabel.Text = T("Saved locally", "ذخیره‌شده روی دستگاه");
            }
            catch
            {
                if (_notesStatusLabel != null) _notesStatusLabel.Text = T("Save failed", "ذخیره ناموفق بود");
            }
        }

        private void OpenMarketPage(object sender, EventArgs e)
        {
            Control control = sender as Control;
            if (control != null && control.Tag is string) OpenUrl((string)control.Tag);
        }

        private void OpenUrl(string url)
        {
            try { Process.Start(url); }
            catch (Exception ex) { MessageBox.Show(T("Could not open the link: ", "بازکردن پیوند ناموفق بود: ") + ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }

        private string T(string english, string persian)
        {
            return _language == "fa" ? persian : english;
        }

        private string ToPersianDigits(string input)
        {
            if (input == null) return string.Empty;
            char[] chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (chars[i] >= '0' && chars[i] <= '9') chars[i] = (char)('۰' + chars[i] - '0');
            return new string(chars);
        }
    }

    internal sealed class SectionPanel : Panel
    {
        public Color BorderColor { get; set; }

        public SectionPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(BorderColor.IsEmpty ? SystemColors.ControlDark : BorderColor, 1F))
            {
                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                e.Graphics.DrawRectangle(p, r);
            }
        }
    }
}
