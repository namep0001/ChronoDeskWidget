using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
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
        private readonly string[] _fontFamilies = { "Segoe UI", "Arial", "Calibri", "Tahoma", "Verdana", "Microsoft YaHei UI" };

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
        private string _themeMode;
        private bool _keepOnTop;
        private bool _exitRequested;
        private bool _startHidden;
        private int _opacityPercent;
        private string _rateUnit;
        private string _fontFamily;
        private System.Windows.Forms.Timer _positionSaveTimer;
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

        private TableLayoutPanel _content;
        private CircularProgressIndicator _pomodoroProgressIndicator;
        private int _layoutColumns = 2;
        private bool _resizingToContent;
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
        private Label _pomodoroStatusLabel;
        private VectorIconButton _pomodoroStartButton;
        private VectorIconButton _pomodoroResetButton;
        private VectorIconButton _pomodoroSwitchButton;
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

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

        public MainForm(bool startHidden)
        {
            _startHidden = startHidden;
            _language = LocalStore.LoadSetting("language", "en");
            if (_language != "fa") _language = "en";
            _themeMode = LocalStore.LoadSetting("theme", "system").ToLowerInvariant();
            if (_themeMode != "light" && _themeMode != "dark") _themeMode = "system";
            _keepOnTop = LocalStore.LoadSetting("topmost", "1") == "1";
            _opacityPercent = 90;
            int savedOpacity;
            if (int.TryParse(LocalStore.LoadSetting("opacity", "90"), out savedOpacity))
                _opacityPercent = Math.Max(50, Math.Min(100, savedOpacity));
            _rateUnit = LocalStore.LoadSetting("rates.unit", "T");
            if (_rateUnit != "$") _rateUnit = "T";
            _fontFamily = LocalStore.LoadSetting("font", "Segoe UI");
            if (Array.IndexOf(_fontFamilies, _fontFamily) < 0) _fontFamily = "Segoe UI";

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
            MinimumSize = new Size(260, 190);
            Size = new Size(540, 720);
            RefreshSystemTheme(true);
            Font = GetUiFont(9F, FontStyle.Regular);
            Opacity = _opacityPercent / 100.0;
            TopMost = _keepOnTop;
            PlaceOnDesktop();

            _toolTip = new ToolTip();
            _positionSaveTimer = new System.Windows.Forms.Timer();
            _positionSaveTimer.Interval = 450;
            _positionSaveTimer.Tick += delegate
            {
                _positionSaveTimer.Stop();
                SaveWindowPosition();
            };
            LocationChanged += delegate
            {
                if (_positionSaveTimer == null) return;
                _positionSaveTimer.Stop();
                _positionSaveTimer.Start();
            };

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
                if (_resizingToContent) return;
                UpdateSectionWidths();
                ResizeWidgetToContent();
            };
            FormClosing += OnFormClosing;
        }

        private void PlaceOnDesktop()
        {
            int x, y;
            if (int.TryParse(LocalStore.LoadSetting("window.x", string.Empty), out x) &&
                int.TryParse(LocalStore.LoadSetting("window.y", string.Empty), out y))
            {
                Rectangle saved = new Rectangle(x, y, Width, Height);
                foreach (Screen screen in Screen.AllScreens)
                {
                    if (screen.WorkingArea.IntersectsWith(saved))
                    {
                        Location = new Point(x, y);
                        return;
                    }
                }
            }

            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(Math.Max(area.Left + 8, area.Right - Width - 16), area.Top + 8);
        }

        private void SaveWindowPosition()
        {
            if (_exitRequested || IsDisposed) return;
            try
            {
                LocalStore.SaveSetting("window.x", Location.X.ToString(CultureInfo.InvariantCulture));
                LocalStore.SaveSetting("window.y", Location.Y.ToString(CultureInfo.InvariantCulture));
            }
            catch { }
        }

        private void RefreshSystemTheme(bool force)
        {
            const string personalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string dwmPath = @"Software\Microsoft\Windows\DWM";
            const string explorerAccentPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";
            int appLight = ReadRegistryDword(personalizePath, "AppsUseLightTheme",
                ReadRegistryDword(personalizePath, "SystemUsesLightTheme", 0));
            uint rawAccent = ReadRegistryUInt(dwmPath, "AccentColor", 0u);
            if (rawAccent == 0u) rawAccent = ReadRegistryUInt(explorerAccentPath, "AccentColorMenu", 0u);
            if (rawAccent == 0u) rawAccent = ReadRegistryUInt(explorerAccentPath, "StartColorMenu", 0xFFD77800u);

            bool isLight = _themeMode == "light" || (_themeMode == "system" && appLight == 1);
            Color accent = DecodeDwmColor(rawAccent, Color.FromArgb(0, 120, 215));
            string signature = _themeMode + "|" + appLight + "|" + rawAccent.ToString(CultureInfo.InvariantCulture);
            _lastThemePoll = DateTime.Now;
            if (!force && string.Equals(signature, _themeSignature, StringComparison.Ordinal)) return;

            _themeSignature = signature;
            _themeUsesLightTaskbar = isLight;
            _themeBackground = isLight ? Color.FromArgb(241, 244, 249) : Color.FromArgb(22, 28, 38);
            _themeText = isLight ? Color.FromArgb(31, 37, 47) : Color.FromArgb(241, 245, 251);
            _themeMutedText = isLight ? Color.FromArgb(102, 111, 125) : Color.FromArgb(157, 168, 184);
            _themeAccent = accent;
            _themeAccentText = GetColorLuminance(accent) > 0.62
                ? Blend(accent, Color.Black, 0.24)
                : Blend(accent, Color.White, 0.13);
            _themeSurface = isLight ? Color.FromArgb(250, 251, 253) : Color.FromArgb(34, 42, 55);
            _themeSurfaceAlt = isLight ? Color.White : Color.FromArgb(41, 51, 66);
            _themeButton = isLight ? Color.FromArgb(235, 239, 245) : Color.FromArgb(51, 62, 79);
            _themeInput = isLight ? Color.FromArgb(255, 255, 255) : Color.FromArgb(27, 34, 46);
            _themeBorder = isLight ? Color.FromArgb(218, 224, 233) : Color.FromArgb(59, 71, 89);
            _themeSelection = accent;
            _themeSelectionText = ContrastText(accent);

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

        private int GetWidgetHeight(string key)
        {
            switch (key)
            {
                case "clock": return 188;
                case "calendar": return 234;
                case "tasks": return 222;
                case "pomodoro": return 222;
                case "notes": return 222;
                case "rates": return 222;
                default: return 0;
            }
        }

        private int GetVisibleWidgetCount()
        {
            int count = 0;
            foreach (string key in _widgetKeys)
                if (IsWidgetVisible(key)) count++;
            return count;
        }

        private int DetermineLayoutColumns()
        {
            return GetVisibleWidgetCount() > 1 ? 2 : 1;
        }

        private void ConfigureContentGrid()
        {
            if (_content == null) return;
            int rowCount = Math.Max(1, (_sections.Count + _layoutColumns - 1) / _layoutColumns);
            _content.SuspendLayout();
            _content.ColumnCount = _layoutColumns;
            _content.RowCount = rowCount;
            _content.ColumnStyles.Clear();
            for (int column = 0; column < _layoutColumns; column++)
                _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / _layoutColumns));

            _content.RowStyles.Clear();
            for (int row = 0; row < rowCount; row++)
            {
                int rowHeight = 0;
                int first = row * _layoutColumns;
                int last = Math.Min(_sections.Count - 1, first + _layoutColumns - 1);
                for (int index = first; index <= last; index++)
                    rowHeight = Math.Max(rowHeight, GetWidgetHeight(_sections[index].Tag as string));
                _content.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight == 0 ? 0 : rowHeight + 10));
            }
            _content.ResumeLayout(true);
        }

        private void ResizeWidgetToContent()
        {
            if (_content == null || _content.IsDisposed || _resizingToContent) return;
            int rowCount = Math.Max(1, (_sections.Count + _layoutColumns - 1) / _layoutColumns);
            int desiredHeight = _content.Padding.Vertical;
            for (int row = 0; row < rowCount; row++)
            {
                int rowHeight = 0;
                int first = row * _layoutColumns;
                int last = Math.Min(_sections.Count - 1, first + _layoutColumns - 1);
                for (int index = first; index <= last; index++)
                    rowHeight = Math.Max(rowHeight, GetWidgetHeight(_sections[index].Tag as string));
                if (rowHeight > 0) desiredHeight += rowHeight + 10;
            }

            int desiredWidth = _content.Padding.Horizontal + (_layoutColumns * 260);
            desiredHeight = Math.Max(MinimumSize.Height, desiredHeight);
            desiredWidth = Math.Max(MinimumSize.Width, desiredWidth);
            if (ClientSize.Width == desiredWidth && ClientSize.Height == desiredHeight) return;
            _resizingToContent = true;
            try
            {
                ClientSize = new Size(desiredWidth, desiredHeight);
                _content.PerformLayout();
                UpdateSectionWidths();
            }
            finally { _resizingToContent = false; }
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
            _pomodoroStatusLabel = null;
            _pomodoroStartButton = null;
            _pomodoroResetButton = null;
            _pomodoroSwitchButton = null;
            _pomodoroProgressIndicator = null;
            _ratesStatusLabel = null;
            _ratesRefreshButton = null;
            _sections.Clear();
            _rateValueLabels.Clear();

            _layoutColumns = DetermineLayoutColumns();
            _content = new TableLayoutPanel();
            _content.Dock = DockStyle.Fill;
            _content.ColumnCount = _layoutColumns;
            _content.RowCount = Math.Max(1, (_widgetKeys.Length + _layoutColumns - 1) / _layoutColumns);
            _content.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
            _content.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            _content.Padding = new Padding(8);
            _content.Margin = new Padding(0);
            _content.BackColor = BackColor;
            _content.RightToLeft = RightToLeft.No;
            _content.MouseDown += StartWindowDrag;
            for (int column = 0; column < _layoutColumns; column++)
                _content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / _layoutColumns));
            for (int row = 0; row < _content.RowCount; row++)
                _content.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            Controls.Add(_content);

            if (IsWidgetVisible("clock")) BuildClockSection();
            if (IsWidgetVisible("calendar")) BuildCalendarSection();
            if (IsWidgetVisible("tasks")) BuildTasksSection();
            if (IsWidgetVisible("pomodoro")) BuildPomodoroSection();
            if (IsWidgetVisible("notes")) BuildNotesSection();
            if (IsWidgetVisible("rates")) BuildRatesSection();

            ConfigureContentGrid();
            UpdateSectionWidths();
            UpdateClock();
            DrawCalendar();
            RenderTasks();
            UpdatePomodoroDisplay();
            RenderRates();
            ApplyScrollBarTheme();
            ResizeWidgetToContent();
            _buildingContent = false;
        }

        private Panel NewSection(string key, int height)
        {
            SectionPanel panel = new SectionPanel();
            panel.Tag = key;
            panel.BorderColor = _themeBorder;
            panel.MouseDown += StartWindowDrag;
            panel.Height = height;
            panel.Width = 250;
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(5);
            panel.Padding = new Padding(0);
            panel.BackColor = _themeSurface;
            panel.ForeColor = _themeText;
            _sections.Add(panel);

            int index = _sections.Count - 1;
            int row = index / _layoutColumns;
            int column = index % _layoutColumns;
            _content.Controls.Add(panel, column, row);
            if (_layoutColumns == 2 && (_sections.Count % 2) == 1 && index == GetVisibleWidgetCount() - 1)
                _content.SetColumnSpan(panel, 2);
            return panel;
        }

        private Label NewSectionTitle(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Height = 25;
            label.Font = GetUiFont(9.2F, FontStyle.Regular);
            label.ForeColor = _themeText;
            label.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            label.MouseDown += StartWindowDrag;
            return label;
        }

        private void AddSectionHeader(Panel panel, string iconKind, string text, int reservedRight)
        {
            VectorIconView icon = new VectorIconView(iconKind);
            icon.Size = new Size(21, 21);
            icon.ForeColor = _themeAccentText;
            icon.BackColor = Color.Transparent;
            icon.Location = _language == "fa"
                ? new Point(panel.ClientSize.Width - 33, 7)
                : new Point(12, 7);
            icon.Anchor = _language == "fa" ? AnchorStyles.Top | AnchorStyles.Right : AnchorStyles.Top | AnchorStyles.Left;
            icon.MouseDown += StartWindowDrag;
            panel.Controls.Add(icon);

            Label title = NewSectionTitle(text);
            title.Location = _language == "fa" ? new Point(12, 5) : new Point(40, 5);
            title.Width = Math.Max(72, panel.ClientSize.Width - 54 - reservedRight);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            title.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            panel.Controls.Add(title);

            panel.Resize += delegate
            {
                if (_language == "fa") icon.Left = panel.ClientSize.Width - icon.Width - 12;
                title.Width = Math.Max(72, panel.ClientSize.Width - 54 - reservedRight);
            };
        }

        private void BuildClockSection()
        {
            Panel panel = NewSection("clock", 188);
            panel.BackColor = _themeSurfaceAlt;
            AddSectionHeader(panel, "clock", T("Clock", "ساعت"), 0);

            _clockLabel = new Label();
            _clockLabel.Location = new Point(8, 43);
            _clockLabel.Size = new Size(panel.Width - 16, 58);
            _clockLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _clockLabel.Font = GetUiFont(30F, FontStyle.Regular);
            _clockLabel.ForeColor = _themeText;
            _clockLabel.TextAlign = ContentAlignment.MiddleCenter;
            _clockLabel.MouseDown += StartWindowDrag;
            panel.Controls.Add(_clockLabel);

            _dateEnglishLabel = new Label();
            _dateEnglishLabel.Location = new Point(8, 115);
            _dateEnglishLabel.Size = new Size(panel.Width - 16, 20);
            _dateEnglishLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dateEnglishLabel.Font = GetUiFont(7.7F, FontStyle.Regular);
            _dateEnglishLabel.ForeColor = _themeText;
            _dateEnglishLabel.TextAlign = ContentAlignment.MiddleCenter;
            _dateEnglishLabel.MouseDown += StartWindowDrag;
            panel.Controls.Add(_dateEnglishLabel);

            _dateSolarLabel = new Label();
            _dateSolarLabel.Location = new Point(8, 140);
            _dateSolarLabel.Size = new Size(panel.Width - 16, 20);
            _dateSolarLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dateSolarLabel.Font = GetUiFont(7.7F, FontStyle.Regular);
            _dateSolarLabel.ForeColor = _themeAccentText;
            _dateSolarLabel.TextAlign = ContentAlignment.MiddleCenter;
            _dateSolarLabel.MouseDown += StartWindowDrag;
            panel.Controls.Add(_dateSolarLabel);

            panel.Resize += delegate
            {
                _clockLabel.Width = panel.ClientSize.Width - 16;
                _dateEnglishLabel.Width = panel.ClientSize.Width - 16;
                _dateSolarLabel.Width = panel.ClientSize.Width - 16;
            };
        }

        private void BuildCalendarSection()
        {
            Panel panel = NewSection("calendar", 234);
            AddSectionHeader(panel, "calendar", T("Calendar", "تقویم"), 0);

            Button previous = MakeSmallButton("‹", T("Previous month", "ماه قبل"), 28);
            previous.Location = new Point(12, 34);
            previous.Click += delegate { MoveCalendarMonth(-1); };
            panel.Controls.Add(previous);

            _calendarMonthLabel = new Label();
            _calendarMonthLabel.Location = new Point(45, 34);
            _calendarMonthLabel.Size = new Size(panel.Width - 90, 25);
            _calendarMonthLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _calendarMonthLabel.Font = GetUiFont(8.1F, FontStyle.Regular);
            _calendarMonthLabel.ForeColor = _themeText;
            _calendarMonthLabel.TextAlign = ContentAlignment.MiddleCenter;
            panel.Controls.Add(_calendarMonthLabel);

            Button next = MakeSmallButton("›", T("Next month", "ماه بعد"), 28);
            next.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            next.Location = new Point(panel.Width - 40, 34);
            next.Click += delegate { MoveCalendarMonth(1); };
            panel.Controls.Add(next);

            _calendarModeCombo = new ComboBox();
            _calendarModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _calendarModeCombo.FlatStyle = FlatStyle.Flat;
            _calendarModeCombo.Font = GetUiFont(7.7F, FontStyle.Regular);
            _calendarModeCombo.BackColor = _themeButton;
            _calendarModeCombo.ForeColor = _themeText;
            _calendarModeCombo.Items.Add(T("Solar Hijri", "هجری شمسی"));
            _calendarModeCombo.Items.Add(T("Gregorian", "میلادی"));
            _calendarModeCombo.SelectedIndex = _showSolarCalendar ? 0 : 1;
            _calendarModeCombo.Size = new Size(120, 25);
            _calendarModeCombo.Location = new Point(12, 64);
            _calendarModeCombo.SelectedIndexChanged += delegate
            {
                _showSolarCalendar = _calendarModeCombo.SelectedIndex == 0;
                _calendarAnchor = _selectedDate;
                DrawCalendar();
            };
            panel.Controls.Add(_calendarModeCombo);

            Button today = MakeSmallButton(T("Today", "امروز"), T("Jump to today", "رفتن به امروز"), 56);
            today.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            today.Location = new Point(panel.Width - 68, 64);
            today.Click += delegate
            {
                _selectedDate = DateTime.Today;
                _calendarAnchor = DateTime.Today;
                DrawCalendar();
                UpdateClock();
            };
            panel.Controls.Add(today);

            _calendarGrid = new TableLayoutPanel();
            _calendarGrid.Location = new Point(12, 95);
            _calendarGrid.Size = new Size(panel.Width - 24, 132);
            _calendarGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _calendarGrid.ColumnCount = 7;
            _calendarGrid.RowCount = 7;
            _calendarGrid.Margin = new Padding(0);
            _calendarGrid.Padding = new Padding(0);
            _calendarGrid.BackColor = panel.BackColor;
            _calendarGrid.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            for (int i = 0; i < 7; i++) _calendarGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7F));
            _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 17));
            for (int i = 0; i < 6; i++) _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 19));
            panel.Controls.Add(_calendarGrid);

            panel.Resize += delegate
            {
                next.Left = panel.ClientSize.Width - next.Width - 12;
                _calendarMonthLabel.Left = previous.Right + 5;
                _calendarMonthLabel.Width = Math.Max(80, next.Left - _calendarMonthLabel.Left - 5);
                today.Left = panel.ClientSize.Width - today.Width - 12;
                _calendarGrid.Width = panel.ClientSize.Width - 24;
            };
        }

        private void BuildTasksSection()
        {
            Panel panel = NewSection("tasks", 222);
            AddSectionHeader(panel, "tasks", T("Tasks", "وظایف"), 96);

            _taskCountLabel = new Label();
            _taskCountLabel.Location = new Point(panel.Width - 100, 7);
            _taskCountLabel.Size = new Size(88, 21);
            _taskCountLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _taskCountLabel.TextAlign = ContentAlignment.MiddleRight;
            _taskCountLabel.Font = GetUiFont(7.5F, FontStyle.Regular);
            _taskCountLabel.ForeColor = _themeMutedText;
            panel.Controls.Add(_taskCountLabel);

            _taskInput = new TextBox();
            _taskInput.Location = new Point(12, 38);
            _taskInput.Size = new Size(panel.Width - 106, 27);
            _taskInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _taskInput.BorderStyle = BorderStyle.FixedSingle;
            _taskInput.BackColor = _themeInput;
            _taskInput.ForeColor = _themeText;
            _taskInput.Font = GetUiFont(8.5F, FontStyle.Regular);
            _taskInput.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { AddTask(); e.SuppressKeyPress = true; }
            };
            panel.Controls.Add(_taskInput);

            _addTaskButton = MakeSmallButton(T("Add", "افزودن"), T("Add task", "افزودن وظیفه"), 72);
            _addTaskButton.Location = new Point(panel.Width - 84, 37);
            _addTaskButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _addTaskButton.Height = 28;
            _addTaskButton.Click += delegate { AddTask(); };
            panel.Controls.Add(_addTaskButton);

            _taskRowsPanel = new Panel();
            _taskRowsPanel.Location = new Point(12, 72);
            _taskRowsPanel.Size = new Size(panel.Width - 24, 140);
            _taskRowsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _taskRowsPanel.AutoScroll = true;
            _taskRowsPanel.BackColor = panel.BackColor;
            panel.Controls.Add(_taskRowsPanel);

            panel.Resize += delegate
            {
                _taskCountLabel.Left = panel.ClientSize.Width - _taskCountLabel.Width - 12;
                _taskInput.Width = Math.Max(90, panel.ClientSize.Width - 106);
                _addTaskButton.Left = panel.ClientSize.Width - _addTaskButton.Width - 12;
                _taskRowsPanel.Width = panel.ClientSize.Width - 24;
                _taskRowsPanel.Height = panel.ClientSize.Height - _taskRowsPanel.Top - 8;
                RenderTasks();
                ApplyScrollBarTheme();
            };
            RenderTasks();
        }

        private void BuildPomodoroSection()
        {
            Panel panel = NewSection("pomodoro", 222);
            AddSectionHeader(panel, "pomodoro", T("Pomodoro", "پومودورو"), 0);

            _pomodoroProgressIndicator = new CircularProgressIndicator();
            _pomodoroProgressIndicator.Size = new Size(118, 118);
            _pomodoroProgressIndicator.Location = new Point((panel.Width - 118) / 2, 33);
            _pomodoroProgressIndicator.Anchor = AnchorStyles.Top;
            _pomodoroProgressIndicator.TrackColor = _themeBorder;
            _pomodoroProgressIndicator.ProgressColor = _themeAccentText;
            _pomodoroProgressIndicator.TextColor = _themeText;
            _pomodoroProgressIndicator.ModeColor = _themeMutedText;
            _pomodoroProgressIndicator.ModeFont = GetUiFont(7.1F, FontStyle.Regular);
            _pomodoroProgressIndicator.TimeFont = GetUiFont(19F, FontStyle.Regular);
            _pomodoroProgressIndicator.FaceColor = _themeSurface;
            panel.Controls.Add(_pomodoroProgressIndicator);

            _pomodoroStatusLabel = new Label();
            _pomodoroStatusLabel.Location = new Point(12, 151);
            _pomodoroStatusLabel.Size = new Size(panel.Width - 24, 18);
            _pomodoroStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _pomodoroStatusLabel.Font = GetUiFont(7F, FontStyle.Regular);
            _pomodoroStatusLabel.ForeColor = _themeMutedText;
            _pomodoroStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            panel.Controls.Add(_pomodoroStatusLabel);

            _pomodoroStartButton = new VectorIconButton("play");
            _pomodoroStartButton.Size = new Size(34, 34);
            _pomodoroStartButton.Location = new Point((panel.Width - 118) / 2, 179);
            _pomodoroStartButton.ForeColor = _themeAccentText;
            _pomodoroStartButton.BackColor = _themeButton;
            _pomodoroStartButton.Cursor = Cursors.Hand;
            _toolTip.SetToolTip(_pomodoroStartButton, T("Start or pause timer", "شروع یا توقف زمان‌سنج"));
            _pomodoroStartButton.Click += delegate { TogglePomodoro(); };
            panel.Controls.Add(_pomodoroStartButton);

            _pomodoroResetButton = new VectorIconButton("reset");
            _pomodoroResetButton.Size = new Size(34, 34);
            _pomodoroResetButton.Location = new Point(_pomodoroStartButton.Right + 8, 179);
            _pomodoroResetButton.ForeColor = _themeAccentText;
            _pomodoroResetButton.BackColor = _themeButton;
            _pomodoroResetButton.Cursor = Cursors.Hand;
            _toolTip.SetToolTip(_pomodoroResetButton, T("Reset current session", "بازنشانی نشست جاری"));
            _pomodoroResetButton.Click += delegate { ResetPomodoro(); };
            panel.Controls.Add(_pomodoroResetButton);

            _pomodoroSwitchButton = new VectorIconButton("rest");
            _pomodoroSwitchButton.Size = new Size(34, 34);
            _pomodoroSwitchButton.Location = new Point(_pomodoroResetButton.Right + 8, 179);
            _pomodoroSwitchButton.ForeColor = _themeAccentText;
            _pomodoroSwitchButton.BackColor = _themeButton;
            _pomodoroSwitchButton.Cursor = Cursors.Hand;
            _toolTip.SetToolTip(_pomodoroSwitchButton, T("Switch between focus and rest", "تغییر بین تمرکز و استراحت"));
            _pomodoroSwitchButton.Click += delegate { SwitchPomodoroMode(); };
            panel.Controls.Add(_pomodoroSwitchButton);

            panel.Resize += delegate
            {
                _pomodoroProgressIndicator.Left = (panel.ClientSize.Width - _pomodoroProgressIndicator.Width) / 2;
                int totalButtons = _pomodoroStartButton.Width + _pomodoroResetButton.Width + _pomodoroSwitchButton.Width + 16;
                _pomodoroStartButton.Left = (panel.ClientSize.Width - totalButtons) / 2;
                _pomodoroResetButton.Left = _pomodoroStartButton.Right + 8;
                _pomodoroSwitchButton.Left = _pomodoroResetButton.Right + 8;
                _pomodoroStatusLabel.Width = panel.ClientSize.Width - 24;
            };
        }

        private void BuildNotesSection()
        {
            Panel panel = NewSection("notes", 222);
            AddSectionHeader(panel, "notes", T("Quick Notes", "یادداشت‌های سریع"), 42);

            Button saveAsButton = MakeSmallButton("↓", T("Save note as a .txt file", "ذخیره یادداشت به‌صورت فایل متنی"), 30);
            saveAsButton.Location = new Point(panel.Width - 44, 4);
            saveAsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            saveAsButton.Click += delegate { SaveNoteAs(); };
            panel.Controls.Add(saveAsButton);

            _notesBox = new TextBox();
            _notesBox.Location = new Point(12, 38);
            _notesBox.Size = new Size(panel.Width - 24, 150);
            _notesBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _notesBox.Multiline = true;
            _notesBox.AcceptsReturn = true;
            _notesBox.AcceptsTab = true;
            _notesBox.ScrollBars = ScrollBars.Vertical;
            _notesBox.BorderStyle = BorderStyle.FixedSingle;
            _notesBox.BackColor = _themeInput;
            _notesBox.ForeColor = _themeText;
            _notesBox.Font = GetUiFont(8.5F, FontStyle.Regular);
            _notesBox.Text = LocalStore.LoadNotes();
            _notesBox.TextChanged += delegate
            {
                _notesDirty = true;
                if (_notesStatusLabel != null) _notesStatusLabel.Text = T("Saving…", "در حال ذخیره…");
            };
            _notesBox.Leave += delegate { SaveNotesIfNeeded(); };
            panel.Controls.Add(_notesBox);

            _notesStatusLabel = new Label();
            _notesStatusLabel.Text = T("Saved locally", "ذخیره‌شده روی دستگاه");
            _notesStatusLabel.Location = new Point(12, panel.Height - 25);
            _notesStatusLabel.Size = new Size(panel.Width - 24, 17);
            _notesStatusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _notesStatusLabel.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            _notesStatusLabel.Font = GetUiFont(6.8F, FontStyle.Regular);
            _notesStatusLabel.ForeColor = _themeMutedText;
            panel.Controls.Add(_notesStatusLabel);

            panel.Resize += delegate
            {
                saveAsButton.Left = panel.ClientSize.Width - saveAsButton.Width - 12;
                _notesBox.Width = panel.ClientSize.Width - 24;
                _notesBox.Height = panel.ClientSize.Height - _notesBox.Top - 34;
                _notesStatusLabel.Top = panel.ClientSize.Height - 25;
                _notesStatusLabel.Width = panel.ClientSize.Width - 24;
                ApplyScrollBarTheme();
            };
        }

        private void BuildRatesSection()
        {
            Panel panel = NewSection("rates", 222);
            AddSectionHeader(panel, "rates", T("Market Rates", "نرخ بازار"), 42);

            _ratesRefreshButton = MakeSmallButton("↻", T("Refresh market rates", "به‌روزرسانی نرخ‌ها"), 30);
            _ratesRefreshButton.Location = new Point(panel.Width - 44, 4);
            _ratesRefreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _ratesRefreshButton.Click += delegate { RefreshRates(); };
            panel.Controls.Add(_ratesRefreshButton);

            int firstY = 38;
            for (int i = 0; i < _rateDefinitions.Length; i++)
            {
                MarketRate rate = _rateDefinitions[i];
                int rowY = firstY + i * 43;
                string iconKind = rate.Key == "usd" ? "usd" : (rate.Key == "eur" ? "eur" : "gold");

                CurrencyBadgeIcon rateIcon = new CurrencyBadgeIcon(iconKind);
                rateIcon.Size = new Size(22, 22);
                rateIcon.Location = new Point(11, rowY + 5);
                rateIcon.BackColor = Color.Transparent;
                panel.Controls.Add(rateIcon);

                Label name = new Label();
                name.Text = _language == "fa" ? rate.DisplayNamePersian : rate.DisplayNameEnglish;
                name.Location = new Point(38, rowY + 3);
                name.Size = new Size(86, 26);
                name.ForeColor = _themeText;
                name.Font = GetUiFont(7.6F, FontStyle.Regular);
                name.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                name.Cursor = Cursors.Hand;
                name.Tag = rate.Url;
                name.Click += OpenMarketPage;
                panel.Controls.Add(name);

                Label value = new Label();
                value.Text = "—";
                value.Location = new Point(124, rowY + 3);
                value.Size = new Size(panel.Width - 136, 26);
                value.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                value.ForeColor = _themeAccentText;
                value.Font = GetUiFont(8.1F, FontStyle.Bold);
                value.TextAlign = _language == "fa" ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight;
                value.Tag = rate.Url;
                value.Cursor = Cursors.Hand;
                value.Click += OpenMarketPage;
                panel.Controls.Add(value);
                _rateValueLabels[rate.Key] = value;

                if (i < _rateDefinitions.Length - 1)
                {
                    Panel separator = new Panel();
                    separator.BackColor = _themeBorder;
                    separator.Location = new Point(38, rowY + 39);
                    separator.Size = new Size(panel.Width - 50, 1);
                    separator.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    panel.Controls.Add(separator);
                }
            }

            _ratesStatusLabel = new Label();
            _ratesStatusLabel.Text = T("Waiting for live market data…", "در انتظار داده زنده بازار…");
            _ratesStatusLabel.Location = new Point(12, 174);
            _ratesStatusLabel.Size = new Size(panel.Width - 24, 18);
            _ratesStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _ratesStatusLabel.Font = GetUiFont(6.5F, FontStyle.Regular);
            _ratesStatusLabel.ForeColor = _themeMutedText;
            _ratesStatusLabel.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            panel.Controls.Add(_ratesStatusLabel);

            panel.Resize += delegate
            {
                _ratesRefreshButton.Left = panel.ClientSize.Width - _ratesRefreshButton.Width - 12;
                foreach (Control child in panel.Controls)
                {
                    if (child is Label && child.Tag is string && child.Location.Y >= firstY && child.Location.Y < firstY + 3 * 43 && child.Location.X >= 100)
                        child.Width = panel.ClientSize.Width - child.Left - 12;
                    else if (child is Panel && child.Height == 1 && child.Location.Y >= firstY)
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
            b.ForeColor = text.Length <= 2 ? _themeAccentText : _themeText;
            b.Font = GetUiFont(8F, FontStyle.Regular);
            b.Cursor = Cursors.Hand;
            b.Margin = new Padding(1);
            _toolTip.SetToolTip(b, tooltip);
            return b;
        }

        private void UpdateSectionWidths()
        {
            if (_content == null) return;
            _content.PerformLayout();
            foreach (Panel panel in _sections) panel.PerformLayout();
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

        private Font GetUiFont(float size, FontStyle style = FontStyle.Regular, GraphicsUnit unit = GraphicsUnit.Point)
        {
            return GetFont(_fontFamily, size, style, unit);
        }

        private Font GetFont(string family, float size, FontStyle style = FontStyle.Regular, GraphicsUnit unit = GraphicsUnit.Point)
        {
            string key = family + "|" + size.ToString("R", CultureInfo.InvariantCulture) + "|" + ((int)style).ToString(CultureInfo.InvariantCulture) + "|" + ((int)unit).ToString(CultureInfo.InvariantCulture);
            Font font;
            if (!_fontCache.TryGetValue(key, out font))
            {
                try { font = new Font(family, size, style, unit); }
                catch { font = new Font("Segoe UI", size, style, unit); }
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
            _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 17));
            for (int i = 0; i < 6; i++) _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 19));
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
                weekday.Font = GetUiFont(_language == "fa" ? 6.7F : 7F, FontStyle.Regular);
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
                CalendarDayButton dayButton = new CalendarDayButton();
                dayButton.Dock = DockStyle.Fill;
                dayButton.Margin = new Padding(2);
                dayButton.FlatStyle = FlatStyle.Flat;
                dayButton.FlatAppearance.BorderSize = 0;
                bool currentMonth = displayYear == year && displayMonth == month;
                bool todayDate = date.Date == DateTime.Today;
                bool selected = date.Date == _selectedDate.Date;
                dayButton.Text = _language == "fa" ? ToPersianDigits(number.ToString()) : number.ToString();
                dayButton.Font = GetUiFont(7.5F, selected ? FontStyle.Bold : FontStyle.Regular);
                dayButton.Cursor = Cursors.Hand;
                dayButton.Tag = date.Date;
                dayButton.BackColor = _calendarGrid.BackColor;
                dayButton.FillColor = selected ? _themeSelection : (todayDate ? _themeButton : Color.Empty);
                dayButton.ForeColor = selected ? _themeSelectionText : (todayDate ? _themeAccentText : (currentMonth ? _themeText : _themeMutedText));
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
                empty.Size = new Size(Math.Max(160, _taskRowsPanel.ClientSize.Width - 10), 25);
                empty.ForeColor = _themeMutedText;
                empty.Font = GetUiFont(7.8F, FontStyle.Regular);
                empty.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                _taskRowsPanel.Controls.Add(empty);
                y = 29;
            }
            else
            {
                foreach (TaskItem task in _tasks)
                {
                    CheckBox check = new CheckBox();
                    check.AutoSize = false;
                    check.Location = new Point(0, y + 1);
                    check.Size = new Size(Math.Max(135, _taskRowsPanel.ClientSize.Width - 32), 24);
                    check.Text = task.Title;
                    check.Checked = task.Done;
                    check.Tag = task;
                    check.FlatStyle = FlatStyle.Flat;
                    check.BackColor = _taskRowsPanel.BackColor;
                    check.ForeColor = task.Done ? _themeMutedText : _themeText;
                    check.Font = GetUiFont(8F, task.Done ? FontStyle.Strikeout : FontStyle.Regular);
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

                    Button remove = MakeSmallButton("×", T("Remove task", "حذف وظیفه"), 25);
                    remove.Height = 22;
                    remove.Location = new Point(_taskRowsPanel.ClientSize.Width - 27, y + 1);
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
                    y += 27;
                }
            }
            _taskRowsPanel.AutoScrollMinSize = new Size(0, y + 2);
            int completed = 0;
            foreach (TaskItem item in _tasks) if (item.Done) completed++;
            if (_taskCountLabel != null)
                _taskCountLabel.Text = _language == "fa"
                    ? ToPersianDigits(completed + " از " + _tasks.Count)
                    : completed + " / " + _tasks.Count + " done";
            _taskRowsPanel.ResumeLayout();
            ApplyScrollBarTheme();
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
                if (_pomodoroStatusLabel != null) _pomodoroStatusLabel.Text = T("Paused", "متوقف شده");
            }
            else
            {
                if (_remainingSeconds <= 0) _remainingSeconds = _isBreak ? 5 * 60 : 25 * 60;
                _timerEndsAt = DateTime.Now.AddSeconds(_remainingSeconds);
                _timerRunning = true;
                if (_pomodoroStatusLabel != null) _pomodoroStatusLabel.Text = T("Session in progress", "نشست در حال اجراست");
            }
            UpdatePomodoroDisplay();
        }

        private void ResetPomodoro()
        {
            _timerRunning = false;
            _remainingSeconds = _isBreak ? 5 * 60 : 25 * 60;
            if (_pomodoroStatusLabel != null) _pomodoroStatusLabel.Text = T("Ready when you are", "آماده شروع");
            UpdatePomodoroDisplay();
        }

        private void SwitchPomodoroMode()
        {
            _timerRunning = false;
            _isBreak = !_isBreak;
            _remainingSeconds = _isBreak ? 5 * 60 : 25 * 60;
            if (_pomodoroStatusLabel != null)
                _pomodoroStatusLabel.Text = _isBreak ? T("Rest session · 5 minutes", "زمان استراحت · ۵ دقیقه") : T("Focus session · 25 minutes", "زمان تمرکز · ۲۵ دقیقه");
            UpdatePomodoroDisplay();
        }

        private void UpdatePomodoroDisplay()
        {
            if (_pomodoroProgressIndicator == null || _pomodoroProgressIndicator.IsDisposed) return;
            int seconds = _remainingSeconds;
            if (_timerRunning) seconds = Math.Max(0, (int)Math.Ceiling((_timerEndsAt - DateTime.Now).TotalSeconds));
            string display = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            if (_language == "fa") display = ToPersianDigits(display);

            int total = _isBreak ? 5 * 60 : 25 * 60;
            _pomodoroProgressIndicator.TimeText = display;
            _pomodoroProgressIndicator.ModeText = _isBreak
                ? T("Rest · 5 min", "استراحت · ۵ دقیقه")
                : T("Focus · 25 min", "تمرکز · ۲۵ دقیقه");
            _pomodoroProgressIndicator.Progress = Math.Max(0F, Math.Min(1F, seconds / (float)total));
            _pomodoroProgressIndicator.Invalidate();

            if (_pomodoroStartButton != null)
            {
                _pomodoroStartButton.Kind = _timerRunning ? "pause" : "play";
                _pomodoroStartButton.Invalidate();
            }
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
                    bool completedBreak = _isBreak;
                    _isBreak = !_isBreak;
                    _remainingSeconds = _isBreak ? 5 * 60 : 25 * 60;
                    if (_pomodoroStatusLabel != null)
                        _pomodoroStatusLabel.Text = completedBreak ? T("Rest complete — ready to focus", "استراحت پایان یافت؛ آماده تمرکز") : T("Focus complete — take a rest", "تمرکز پایان یافت؛ زمان استراحت");
                    try
                    {
                        if (_trayIcon != null)
                            _trayIcon.ShowBalloonTip(3500, AppName,
                                completedBreak ? T("Rest finished. Ready for a focus session?", "استراحت تمام شد. آماده تمرکز هستید؟") : T("Focus session finished. Time for a rest.", "زمان تمرکز تمام شد؛ استراحت کنید."),
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
            if (_ratesStatusLabel != null) _ratesStatusLabel.Text = T("Fetching market quotes…", "در حال دریافت نرخ بازار…");
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
                                        _ratesStatusLabel.Text = T("Updated ", "به‌روزرسانی ") + (_language == "fa" ? ToPersianDigits(DateTime.Now.ToString("HH:mm")) : DateTime.Now.ToString("HH:mm")) + " · " + (_rateUnit == "T" ? "Toman (T)" : "Dollar ($)");
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
            long usdRial = 0;
            bool hasUsd = _rateValues.ContainsKey("usd") &&
                long.TryParse(_rateValues["usd"], NumberStyles.Integer, CultureInfo.InvariantCulture, out usdRial) &&
                usdRial > 0;

            foreach (MarketRate definition in _rateDefinitions)
            {
                Label label;
                if (!_rateValueLabels.TryGetValue(definition.Key, out label) || label == null || label.IsDisposed) continue;
                string value;
                long parsed;
                if (_rateValues.TryGetValue(definition.Key, out value) &&
                    !string.IsNullOrEmpty(value) &&
                    long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    string formatted;
                    if (_rateUnit == "T")
                        formatted = (parsed / 10L).ToString("N0", CultureInfo.InvariantCulture) + " T";
                    else if (hasUsd)
                    {
                        decimal dollars = (decimal)parsed / (decimal)usdRial;
                        formatted = "$" + dollars.ToString("N2", CultureInfo.InvariantCulture);
                    }
                    else
                        formatted = "—";

                    if (_language == "fa") formatted = ToPersianDigits(formatted);
                    label.Text = formatted;
                    label.ForeColor = _rateFreshThisFetch[definition.Key] ? _themeAccentText : _themeMutedText;
                }
                else
                {
                    label.Text = "—";
                    label.ForeColor = _themeMutedText;
                }
            }
            if (_ratesStatusLabel != null && !_rateFetchInProgress && _lastRateRefreshLocal != DateTime.MinValue)
            {
                string unitName = _rateUnit == "T" ? "Toman (T)" : "Dollar ($)";
                _ratesStatusLabel.Text = T("Prices in ", "قیمت‌ها به ") + unitName;
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

        private void SetWidgetOpacity(int percent)
        {
            _opacityPercent = Math.Max(50, Math.Min(100, percent));
            Opacity = _opacityPercent / 100.0;
            LocalStore.SaveSetting("opacity", _opacityPercent.ToString(CultureInfo.InvariantCulture));
            BuildTrayMenu();
        }

        private void SetThemeMode(string mode)
        {
            mode = mode == "light" || mode == "dark" ? mode : "system";
            if (_themeMode == mode) return;
            _themeMode = mode;
            LocalStore.SaveSetting("theme", _themeMode);
            RefreshSystemTheme(true);
            BuildTrayMenu();
        }

        private void SetRateUnit(string unit)
        {
            _rateUnit = unit == "$" ? "$" : "T";
            LocalStore.SaveSetting("rates.unit", _rateUnit);
            RenderRates();
            BuildTrayMenu();
        }

        private void SetFontFamily(string family)
        {
            if (Array.IndexOf(_fontFamilies, family) < 0 || _fontFamily == family) return;
            SaveNotesIfNeeded();
            _fontFamily = family;
            LocalStore.SaveSetting("font", _fontFamily);
            BuildContent();
            BuildTrayMenu();
        }

        private void SaveNoteAs()
        {
            string text = _notesBox == null ? LocalStore.LoadNotes() : _notesBox.Text;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = T("Save note as text file", "ذخیره یادداشت به‌صورت فایل متنی");
                dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                dialog.DefaultExt = "txt";
                dialog.AddExtension = true;
                dialog.FileName = "ChronoDesk-Note.txt";
                dialog.OverwritePrompt = true;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dialog.FileName, text ?? string.Empty, new UTF8Encoding(true));
                    if (_notesStatusLabel != null) _notesStatusLabel.Text = T("Exported .txt", "فایل متنی ذخیره شد");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(T("Could not save the note: ", "ذخیره یادداشت ناموفق بود: ") + ex.Message,
                        AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ApplyScrollBarTheme()
        {
            try
            {
                string themeName = _themeUsesLightTaskbar ? "Explorer" : "DarkMode_Explorer";
                if (_taskRowsPanel != null && !_taskRowsPanel.IsDisposed)
                    SetWindowTheme(_taskRowsPanel.Handle, themeName, null);
                if (_notesBox != null && !_notesBox.IsDisposed)
                    SetWindowTheme(_notesBox.Handle, themeName, null);
            }
            catch { }
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

            ToolStripMenuItem widgetsItem = new ToolStripMenuItem(T("Visible widgets", "بخش‌های قابل نمایش"));
            AddWidgetToggle(widgetsItem, "clock", T("Clock and dates", "ساعت و تاریخ"));
            AddWidgetToggle(widgetsItem, "calendar", T("Calendar", "تقویم"));
            AddWidgetToggle(widgetsItem, "tasks", T("Tasks", "وظایف"));
            AddWidgetToggle(widgetsItem, "pomodoro", T("Pomodoro timer", "زمان‌سنج پومودورو"));
            AddWidgetToggle(widgetsItem, "notes", T("Quick note", "یادداشت سریع"));
            AddWidgetToggle(widgetsItem, "rates", T("Market rates", "نرخ بازار"));
            menu.Items.Add(widgetsItem);

            ToolStripMenuItem opacityItem = new ToolStripMenuItem(T("Widget opacity", "شفافیت ویجت"));
            int[] opacityOptions = { 100, 90, 80, 70, 60, 50 };
            foreach (int percent in opacityOptions)
            {
                int selectedPercent = percent;
                ToolStripMenuItem opacityOption = new ToolStripMenuItem(percent + "%");
                opacityOption.Checked = _opacityPercent == percent;
                opacityOption.Click += delegate { SetWidgetOpacity(selectedPercent); };
                opacityItem.DropDownItems.Add(opacityOption);
            }
            menu.Items.Add(opacityItem);

            ToolStripMenuItem themeItem = new ToolStripMenuItem(T("Theme", "پوسته"));
            ToolStripMenuItem systemTheme = new ToolStripMenuItem(T("System", "هماهنگ با سیستم"));
            systemTheme.Checked = _themeMode == "system";
            systemTheme.Click += delegate { SetThemeMode("system"); };
            ToolStripMenuItem lightTheme = new ToolStripMenuItem(T("Light", "روشن"));
            lightTheme.Checked = _themeMode == "light";
            lightTheme.Click += delegate { SetThemeMode("light"); };
            ToolStripMenuItem darkTheme = new ToolStripMenuItem(T("Dark", "تاریک"));
            darkTheme.Checked = _themeMode == "dark";
            darkTheme.Click += delegate { SetThemeMode("dark"); };
            themeItem.DropDownItems.Add(systemTheme);
            themeItem.DropDownItems.Add(lightTheme);
            themeItem.DropDownItems.Add(darkTheme);
            menu.Items.Add(themeItem);

            ToolStripMenuItem ratesItem = new ToolStripMenuItem(T("Market rates", "نرخ بازار"));
            ToolStripMenuItem refreshItem = new ToolStripMenuItem(T("Refresh market rates", "به‌روزرسانی نرخ‌ها"));
            refreshItem.Enabled = IsWidgetVisible("rates");
            refreshItem.Click += delegate { RefreshRates(); };
            ratesItem.DropDownItems.Add(refreshItem);
            ratesItem.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem unitDollar = new ToolStripMenuItem("Dollar ($)");
            unitDollar.Checked = _rateUnit == "$";
            unitDollar.Click += delegate { SetRateUnit("$"); };
            ToolStripMenuItem unitToman = new ToolStripMenuItem("Toman (T)");
            unitToman.Checked = _rateUnit == "T";
            unitToman.Click += delegate { SetRateUnit("T"); };
            ratesItem.DropDownItems.Add(unitDollar);
            ratesItem.DropDownItems.Add(unitToman);
            menu.Items.Add(ratesItem);

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

            ToolStripMenuItem fontItem = new ToolStripMenuItem(T("Font", "قلم"));
            foreach (string family in _fontFamilies)
            {
                string selectedFamily = family;
                ToolStripMenuItem fontOption = new ToolStripMenuItem(family);
                fontOption.Checked = string.Equals(_fontFamily, family, StringComparison.OrdinalIgnoreCase);
                fontOption.Click += delegate { SetFontFamily(selectedFamily); };
                fontItem.DropDownItems.Add(fontOption);
            }
            menu.Items.Add(fontItem);

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
            SaveWindowPosition();
            Hide();
            BuildTrayMenu();
        }

        private void ExitApplication()
        {
            SaveNotesIfNeeded();
            SaveWindowPosition();
            LocalStore.SaveTasks(_tasks);
            _exitRequested = true;
            if (_tickTimer != null) _tickTimer.Stop();
            if (_positionSaveTimer != null) _positionSaveTimer.Stop();
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
            SaveWindowPosition();
            SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
            try { LocalStore.SaveTasks(_tasks); } catch { }
            if (_trayIcon != null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
            if (_tickTimer != null) { _tickTimer.Stop(); _tickTimer.Dispose(); }
            if (_positionSaveTimer != null) { _positionSaveTimer.Stop(); _positionSaveTimer.Dispose(); }
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

    internal sealed class VectorIconView : Control
    {
        public string Kind { get; private set; }
        public VectorIconView(string kind)
        {
            Kind = kind ?? "clock";
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = false;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = Math.Min(Width, Height) / 24F;
            if (scale <= 0) return;
            g.TranslateTransform((Width - 24F * scale) / 2F, (Height - 24F * scale) / 2F);
            g.ScaleTransform(scale, scale);
            using (Pen pen = new Pen(ForeColor, 1.7F))
            using (Pen thin = new Pen(ForeColor, 1.35F))
            using (Brush brush = new SolidBrush(ForeColor))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                thin.StartCap = LineCap.Round; thin.EndCap = LineCap.Round; thin.LineJoin = LineJoin.Round;
                switch (Kind)
                {
                    case "clock":
                        g.DrawEllipse(pen, 3.5F, 3.5F, 17, 17);
                        g.DrawLine(pen, 12, 7, 12, 12); g.DrawLine(pen, 12, 12, 16, 14.5F);
                        break;
                    case "calendar":
                        using (GraphicsPath calendar = RoundedRectangle(4, 5, 16, 15, 2)) g.DrawPath(pen, calendar);
                        g.DrawLine(pen, 4, 9, 20, 9); g.DrawLine(pen, 8, 3, 8, 7); g.DrawLine(pen, 16, 3, 16, 7);
                        g.FillEllipse(brush, 7.2F, 12, 1.8F, 1.8F); g.FillEllipse(brush, 11.2F, 12, 1.8F, 1.8F);
                        g.FillEllipse(brush, 15.2F, 12, 1.8F, 1.8F); g.FillEllipse(brush, 7.2F, 16, 1.8F, 1.8F);
                        g.FillEllipse(brush, 11.2F, 16, 1.8F, 1.8F);
                        break;
                    case "tasks":
                        using (GraphicsPath list = RoundedRectangle(4, 4, 16, 16, 2)) g.DrawPath(pen, list);
                        g.DrawLine(pen, 7, 9, 9, 11); g.DrawLine(pen, 9, 11, 12, 7.5F); g.DrawLine(thin, 14, 9, 18, 9);
                        g.DrawLine(thin, 7, 16, 9, 18); g.DrawLine(thin, 9, 18, 12, 14.5F); g.DrawLine(thin, 14, 16, 18, 16);
                        break;
                    case "pomodoro":
                        g.DrawEllipse(pen, 5, 7, 14, 14);
                        g.DrawLine(pen, 12, 7, 12, 4);
                        g.DrawLine(pen, 9, 3, 15, 3);
                        g.DrawLine(pen, 12, 4, 12, 7);
                        g.DrawLine(pen, 12, 11, 12, 15);
                        g.DrawLine(pen, 12, 15, 16, 17);
                        break;
                    case "notes":
                        PointF[] paper = { new PointF(6, 3), new PointF(15, 3), new PointF(19, 7), new PointF(19, 21), new PointF(6, 21) };
                        g.DrawPolygon(pen, paper); g.DrawLine(pen, 15, 3, 15, 7); g.DrawLine(pen, 15, 7, 19, 7);
                        g.DrawLine(thin, 9, 11, 16, 11); g.DrawLine(thin, 9, 14, 16, 14); g.DrawLine(thin, 9, 17, 14, 17);
                        break;
                    case "rates":
                        g.DrawLine(pen, 4, 4, 4, 20); g.DrawLine(pen, 4, 20, 21, 20);
                        PointF[] chart = { new PointF(6, 16), new PointF(10, 12), new PointF(13, 14), new PointF(19, 7) };
                        g.DrawLines(pen, chart); g.FillEllipse(brush, 17.5F, 5.5F, 3, 3);
                        break;
                    default: g.DrawEllipse(pen, 4, 4, 16, 16); break;
                }
            }
        }
        private static GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90); path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90); path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class VectorIconButton : Button
    {
        private string _kind;
        private bool _hovered;
        public string Kind { get { return _kind; } set { _kind = value ?? "play"; Invalidate(); } }

        public VectorIconButton(string kind)
        {
            _kind = kind ?? "play";
            Text = string.Empty;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = SystemColors.Control;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush background = new SolidBrush(_hovered ? ControlPaint.Light(BackColor, 0.12F) : BackColor))
                g.FillEllipse(background, 1, 1, Width - 3, Height - 3);
            using (Pen pen = new Pen(ForeColor, 1.8F))
            using (SolidBrush brush = new SolidBrush(ForeColor))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                switch (_kind)
                {
                    case "play":
                        PointF[] triangle = { new PointF(13, 8), new PointF(22, 17), new PointF(13, 26) };
                        g.FillPolygon(brush, triangle);
                        break;
                    case "pause":
                        using (GraphicsPath bars = new GraphicsPath())
                        {
                            bars.AddRectangle(new Rectangle(10, 8, 4, 18));
                            bars.AddRectangle(new Rectangle(18, 8, 4, 18));
                            g.FillPath(brush, bars);
                        }
                        break;
                    case "reset":
                        g.DrawArc(pen, 8, 9, 17, 16, 35, 295);
                        PointF[] arrow = { new PointF(7, 8), new PointF(15, 8), new PointF(10, 15) };
                        g.FillPolygon(brush, arrow);
                        break;
                    case "rest":
                        g.DrawLine(pen, 10, 8, 10, 12);
                        g.DrawLine(pen, 18, 7, 18, 12);
                        using (GraphicsPath cup = new GraphicsPath())
                        {
                            cup.AddLine(7, 14, 19, 14);
                            cup.AddLine(19, 14, 18, 23);
                            cup.AddLine(18, 23, 9, 23);
                            cup.CloseFigure();
                            g.DrawPath(pen, cup);
                        }
                        g.DrawArc(pen, 16, 15, 7, 6, 270, 180);
                        g.DrawLine(pen, 9, 26, 20, 26);
                        break;
                }
            }
        }
    }

    internal sealed class CurrencyBadgeIcon : Control
    {
        private readonly string _kind;
        private readonly Color _badgeColor;

        public CurrencyBadgeIcon(string kind)
        {
            _kind = kind ?? "usd";
            _badgeColor = _kind == "usd" ? Color.FromArgb(39, 153, 105) :
                (_kind == "eur" ? Color.FromArgb(55, 112, 194) : Color.FromArgb(190, 143, 39));
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int diameter = Math.Min(Width, Height) - 2;
            Rectangle circle = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
            using (SolidBrush badge = new SolidBrush(_badgeColor))
            using (SolidBrush white = new SolidBrush(Color.White))
            using (Font font = new Font("Segoe UI", _kind == "gold" ? 7.1F : 10.5F, FontStyle.Bold, GraphicsUnit.Point))
            using (StringFormat centered = new StringFormat())
            {
                centered.Alignment = StringAlignment.Center;
                centered.LineAlignment = StringAlignment.Center;
                g.FillEllipse(badge, circle);
                string symbol = _kind == "usd" ? "$" : (_kind == "eur" ? "€" : "Au");
                g.DrawString(symbol, font, white, circle, centered);
            }
        }
    }

    internal sealed class CalendarDayButton : Button
    {
        public Color FillColor { get; set; }

        public CalendarDayButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush background = new SolidBrush(Parent == null ? SystemColors.Control : Parent.BackColor))
                g.FillRectangle(background, ClientRectangle);

            int diameter = Math.Min(20, Math.Min(Width - 2, Height - 1));
            if (!FillColor.IsEmpty && diameter > 0)
            {
                Rectangle circle = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
                using (SolidBrush fill = new SolidBrush(FillColor))
                    g.FillEllipse(fill, circle);
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
                TextFormatFlags.SingleLine);
        }
    }

    internal sealed class CircularProgressIndicator : Control
    {
        private float _progress;
        public float Progress { get { return _progress; } set { _progress = Math.Max(0F, Math.Min(1F, value)); Invalidate(); } }
        public Color TrackColor { get; set; }
        public Color ProgressColor { get; set; }
        public Color TextColor { get; set; }
        public Color ModeColor { get; set; }
        public Color FaceColor { get; set; }
        public string TimeText { get; set; }
        public string ModeText { get; set; }
        public Font TimeFont { get; set; }
        public Font ModeFont { get; set; }

        public CircularProgressIndicator()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TrackColor = Color.Gray;
            ProgressColor = Color.DodgerBlue;
            TextColor = Color.Black;
            ModeColor = Color.Gray;
            FaceColor = Color.White;
            TimeText = "25:00";
            ModeText = "Focus · 25 min";
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF bounds = new RectangleF(7, 7, Width - 14, Height - 14);
            using (Pen track = new Pen(TrackColor, 5.5F))
            using (Pen progress = new Pen(ProgressColor, 5.5F))
            using (SolidBrush face = new SolidBrush(FaceColor))
            using (SolidBrush textBrush = new SolidBrush(TextColor))
            using (SolidBrush modeBrush = new SolidBrush(ModeColor))
            using (StringFormat centered = new StringFormat())
            {
                centered.Alignment = StringAlignment.Center;
                centered.LineAlignment = StringAlignment.Center;
                track.StartCap = LineCap.Round;
                track.EndCap = LineCap.Round;
                progress.StartCap = LineCap.Round;
                progress.EndCap = LineCap.Round;
                g.FillEllipse(face, bounds);
                g.DrawArc(track, bounds, -90, 359.8F);
                if (_progress > 0.001F)
                    g.DrawArc(progress, bounds, -90, Math.Min(359.8F, 360F * _progress));
                // ModeFont and TimeFont are borrowed from MainForm's shared font cache.
                // Never dispose them here: OnPaint runs repeatedly and the cache owns their lifetime.
                Font modeFont = ModeFont ?? SystemFonts.DefaultFont;
                Font timeFont = TimeFont ?? SystemFonts.DefaultFont;

                RectangleF modeBounds = new RectangleF(13, Height * 0.34F, Width - 26, 18);
                RectangleF timeBounds = new RectangleF(10, Height * 0.46F, Width - 20, 32);
                if (modeBounds.Width > 0F && modeBounds.Height > 0F)
                    g.DrawString(ModeText ?? string.Empty, modeFont, modeBrush, modeBounds, centered);
                if (timeBounds.Width > 0F && timeBounds.Height > 0F)
                    g.DrawString(TimeText ?? string.Empty, timeFont, textBrush, timeBounds, centered);
            }
        }
    }

    internal sealed class SectionPanel : Panel
    {
        public Color BorderColor { get; set; }
        private const int CornerRadius = 18;

        public SectionPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = SystemColors.Control;
        }

        private GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(2, radius * 2);
            diameter = Math.Min(diameter, Math.Min(bounds.Width, bounds.Height));
            Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color outside = Parent == null ? SystemColors.Control : Parent.BackColor;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(outside);
            using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
            using (SolidBrush brush = new SolidBrush(BackColor))
                e.Graphics.FillPath(brush, path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
            using (Pen pen = new Pen(BorderColor.IsEmpty ? SystemColors.ControlDark : BorderColor, 1F))
                e.Graphics.DrawPath(pen, path);
        }
    }
}