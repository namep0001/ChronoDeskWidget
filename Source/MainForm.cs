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
        private VectorIconButton _ratesRefreshButton;
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

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
        private static extern IntPtr SendMessageText(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

        private const int WCA_ACCENT_POLICY = 19;
        private const int ACCENT_ENABLE_BLUR_BEHIND = 3;
        private const int ACCENT_ENABLE_ACRYLIC_BLUR_BEHIND = 4;

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int width, int height);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private bool _glassBackdropActive;

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
            Size = new Size(456, 520);
            RefreshSystemTheme(true);
            Font = GetUiFont(9F, FontStyle.Regular);
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
            // The outer glass layer is intentionally brighter than the individual widget cards.
            _themeBackground = isLight ? Color.FromArgb(246, 248, 252) : Color.FromArgb(82, 96, 116);
            _themeText = isLight ? Color.FromArgb(31, 38, 49) : Color.FromArgb(244, 247, 252);
            _themeMutedText = isLight ? Color.FromArgb(105, 115, 130) : Color.FromArgb(170, 181, 197);
            _themeAccent = accent;
            // Preserve the Windows accent hue. Only a small luminance adjustment is applied for legibility.
            _themeAccentText = GetColorLuminance(accent) > 0.72
                ? Blend(accent, Color.Black, 0.10)
                : Blend(accent, Color.White, 0.08);
            _themeSurface = isLight ? Color.FromArgb(228, 234, 243) : Color.FromArgb(31, 39, 52);
            _themeSurfaceAlt = isLight ? Color.FromArgb(237, 242, 249) : Color.FromArgb(41, 51, 67);
            _themeButton = isLight ? Color.FromArgb(216, 224, 236) : Color.FromArgb(53, 64, 82);
            _themeInput = isLight ? Color.FromArgb(249, 251, 254) : Color.FromArgb(25, 32, 44);
            _themeBorder = isLight ? Color.FromArgb(207, 216, 229) : Color.FromArgb(67, 81, 101);
            _themeSelection = accent;
            _themeSelectionText = ContrastText(accent);

            BackColor = _themeBackground;
            ForeColor = _themeText;
            if (IsHandleCreated) ApplyGlassBackdrop();
            if (_trayIcon != null && _generatedIcon != null)
            {
                Icon previousIcon = _generatedIcon;
                _generatedIcon = CreateWidgetIcon();
                _trayIcon.Icon = _generatedIcon;
                previousIcon.Dispose();
            }
            if (_content != null && !_content.IsDisposed && !_buildingContent) BuildContent();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateWindowShape();
            ApplyGlassBackdrop();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (!IsHandleCreated || IsDisposed) return;
            UpdateWindowShape();
            ApplyGlassBackdrop();
            Invalidate(true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            using (GraphicsPath path = CreateRoundedPath(bounds, 30))
            using (Pen edge = new Pen(_themeUsesLightTaskbar
                ? Color.FromArgb(130, 255, 255, 255)
                : Color.FromArgb(70, 235, 243, 255), 1F))
                e.Graphics.DrawPath(edge, path);
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_glassBackdropActive) return;

            // Fallback material for Windows builds where the acrylic composition API is unavailable.
            Rectangle bounds = ClientRectangle;
            using (LinearGradientBrush glass = new LinearGradientBrush(bounds,
                _themeUsesLightTaskbar ? Color.FromArgb(244, 247, 251) : Color.FromArgb(101, 117, 139),
                _themeUsesLightTaskbar ? Color.FromArgb(220, 228, 239) : Color.FromArgb(58, 72, 91), 90F))
                e.Graphics.FillRectangle(glass, bounds);
            using (Pen edge = new Pen(_themeUsesLightTaskbar ? Color.FromArgb(180, 255, 255, 255) : Color.FromArgb(55, 255, 255, 255), 1F))
                e.Graphics.DrawLine(edge, 18, 1, Math.Max(18, Width - 18), 1);
        }

        private void ApplyGlassBackdrop()
        {
            if (!IsHandleCreated || IsDisposed) return;

            byte alpha = (byte)Math.Max(128, Math.Min(178, 105 + (int)Math.Round(_opacityPercent * 0.72)));
            Color material = _themeUsesLightTaskbar
                ? Color.FromArgb(236, 242, 250)
                : Color.FromArgb(76, 90, 110);

            AccentPolicy policy = new AccentPolicy();
            policy.AccentState = ACCENT_ENABLE_ACRYLIC_BLUR_BEHIND;
            policy.AccentFlags = 2;
            policy.GradientColor = unchecked((uint)Color.FromArgb(alpha, material.R, material.G, material.B).ToArgb());
            IntPtr memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(AccentPolicy)));
            try
            {
                Marshal.StructureToPtr(policy, memory, false);
                WindowCompositionAttributeData data = new WindowCompositionAttributeData();
                data.Attribute = WCA_ACCENT_POLICY;
                data.Data = memory;
                data.SizeOfData = Marshal.SizeOf(typeof(AccentPolicy));
                _glassBackdropActive = SetWindowCompositionAttribute(Handle, ref data);

                if (!_glassBackdropActive)
                {
                    policy.AccentState = ACCENT_ENABLE_BLUR_BEHIND;
                    Marshal.StructureToPtr(policy, memory, false);
                    _glassBackdropActive = SetWindowCompositionAttribute(Handle, ref data);
                }
            }
            catch
            {
                _glassBackdropActive = false;
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
            Invalidate(true);
        }

        private void UpdateWindowShape()
        {
            if (!IsHandleCreated || Width < 40 || Height < 40) return;
            IntPtr region = CreateRoundRectRgn(0, 0, Width + 1, Height + 1, 32, 32);
            if (region == IntPtr.Zero) return;
            if (SetWindowRgn(Handle, region, true) == 0) DeleteObject(region);
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
                case "clock": return 146;
                case "calendar": return 184;
                case "tasks": return 152;
                case "pomodoro": return 152;
                case "notes": return 152;
                case "rates": return 152;
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
                _content.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight == 0 ? 0 : rowHeight + 7));
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
                if (rowHeight > 0) desiredHeight += rowHeight + 7;
            }

            int desiredWidth = _content.Padding.Horizontal + (_layoutColumns * 218);
            desiredHeight = Math.Max(MinimumSize.Height, desiredHeight);
            desiredWidth = Math.Max(MinimumSize.Width, desiredWidth);
            if (ClientSize.Width == desiredWidth && ClientSize.Height == desiredHeight) return;
            _resizingToContent = true;
            try
            {
                ClientSize = new Size(desiredWidth, desiredHeight);
                _content.PerformLayout();
                UpdateSectionWidths();
                ApplyGlassBackdrop();
                UpdateWindowShape();
                Invalidate(true);
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
            _content.BackColor = Color.Transparent;
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
            if (IsHandleCreated)
            {
                ApplyGlassBackdrop();
                UpdateWindowShape();
                Invalidate(true);
            }
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
            label.BackColor = _themeSurface;
            label.Height = 25;
            label.Font = GetUiFont(9.7F, FontStyle.Bold);
            label.ForeColor = _themeText;
            label.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            label.MouseDown += StartWindowDrag;
            return label;
        }

        private void AddSectionHeader(Panel panel, string iconKind, string text, int reservedRight)
        {
            VectorIconView icon = new VectorIconView(iconKind);
            icon.Size = new Size(23, 23);
            icon.ForeColor = _themeAccent;
            icon.BackColor = Color.Transparent;
            icon.Location = _language == "fa"
                ? new Point(panel.ClientSize.Width - 35, 6)
                : new Point(11, 6);
            icon.Anchor = _language == "fa" ? AnchorStyles.Top | AnchorStyles.Right : AnchorStyles.Top | AnchorStyles.Left;
            icon.MouseDown += StartWindowDrag;
            panel.Controls.Add(icon);

            Label title = NewSectionTitle(text);
            title.Location = _language == "fa" ? new Point(12, 4) : new Point(40, 4);
            title.Width = Math.Max(72, panel.ClientSize.Width - 56 - reservedRight);
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
            Panel panel = NewSection("clock", 146);
            panel.BackColor = _themeSurfaceAlt;
            AddSectionHeader(panel, "clock", T("Clock", "ساعت"), 0);

            _clockLabel = new Label();
            _clockLabel.BackColor = panel.BackColor;
            _clockLabel.Location = new Point(8, 30);
            _clockLabel.Size = new Size(panel.Width - 16, 55);
            _clockLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _clockLabel.Font = GetUiFont(31F, FontStyle.Regular);
            _clockLabel.ForeColor = _themeText;
            _clockLabel.TextAlign = ContentAlignment.MiddleCenter;
            _clockLabel.MouseDown += StartWindowDrag;
            panel.Controls.Add(_clockLabel);

            _dateEnglishLabel = new Label();
            _dateEnglishLabel.BackColor = panel.BackColor;
            _dateEnglishLabel.Location = new Point(8, 91);
            _dateEnglishLabel.Size = new Size(panel.Width - 16, 19);
            _dateEnglishLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dateEnglishLabel.Font = GetUiFont(8.2F, FontStyle.Regular);
            _dateEnglishLabel.ForeColor = _themeText;
            _dateEnglishLabel.TextAlign = ContentAlignment.MiddleCenter;
            _dateEnglishLabel.MouseDown += StartWindowDrag;
            panel.Controls.Add(_dateEnglishLabel);

            _dateSolarLabel = new Label();
            _dateSolarLabel.BackColor = panel.BackColor;
            _dateSolarLabel.Location = new Point(8, 112);
            _dateSolarLabel.Size = new Size(panel.Width - 16, 18);
            _dateSolarLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dateSolarLabel.Font = GetUiFont(8F, FontStyle.Regular);
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
            Panel panel = NewSection("calendar", 184);
            AddSectionHeader(panel, "calendar", T("Calendar", "تقویم"), 0);

            Button previous = MakeSmallButton("‹", T("Previous month", "ماه قبل"), 28);
            previous.Location = new Point(12, 31);
            previous.Click += delegate { MoveCalendarMonth(-1); };
            panel.Controls.Add(previous);

            _calendarMonthLabel = new Label();
            _calendarMonthLabel.BackColor = panel.BackColor;
            _calendarMonthLabel.Location = new Point(45, 31);
            _calendarMonthLabel.Size = new Size(panel.Width - 90, 24);
            _calendarMonthLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _calendarMonthLabel.Font = GetUiFont(8.5F, FontStyle.Bold);
            _calendarMonthLabel.ForeColor = _themeText;
            _calendarMonthLabel.TextAlign = ContentAlignment.MiddleCenter;
            panel.Controls.Add(_calendarMonthLabel);

            Button next = MakeSmallButton("›", T("Next month", "ماه بعد"), 28);
            next.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            next.Location = new Point(panel.Width - 40, 31);
            next.Click += delegate { MoveCalendarMonth(1); };
            panel.Controls.Add(next);

            _calendarModeCombo = new ComboBox();
            _calendarModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _calendarModeCombo.FlatStyle = FlatStyle.Flat;
            _calendarModeCombo.DrawMode = DrawMode.OwnerDrawFixed;
            _calendarModeCombo.ItemHeight = 20;
            _calendarModeCombo.DrawItem += delegate(object sender, DrawItemEventArgs e)
            {
                if (e.Index < 0) return;
                bool selectedItem = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color itemBack = selectedItem ? _themeButton : _themeInput;
                using (SolidBrush fill = new SolidBrush(itemBack)) e.Graphics.FillRectangle(fill, e.Bounds);
                TextRenderer.DrawText(e.Graphics, _calendarModeCombo.Items[e.Index].ToString(), _calendarModeCombo.Font,
                    e.Bounds, _themeText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            };
            _calendarModeCombo.Font = GetUiFont(8F, FontStyle.Regular);
            _calendarModeCombo.BackColor = _themeButton;
            _calendarModeCombo.ForeColor = _themeText;
            _calendarModeCombo.Items.Add(T("Solar Hijri", "هجری شمسی"));
            _calendarModeCombo.Items.Add(T("Gregorian", "میلادی"));
            _calendarModeCombo.SelectedIndex = _showSolarCalendar ? 0 : 1;
            _calendarModeCombo.Size = new Size(116, 23);
            _calendarModeCombo.Location = new Point(12, 58);
            _calendarModeCombo.SelectedIndexChanged += delegate
            {
                _showSolarCalendar = _calendarModeCombo.SelectedIndex == 0;
                _calendarAnchor = _selectedDate;
                DrawCalendar();
            };
            panel.Controls.Add(_calendarModeCombo);

            Button today = MakeSmallButton(T("Today", "امروز"), T("Jump to today", "رفتن به امروز"), 56);
            today.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            today.Location = new Point(panel.Width - 68, 58);
            today.Click += delegate
            {
                _selectedDate = DateTime.Today;
                _calendarAnchor = DateTime.Today;
                DrawCalendar();
                UpdateClock();
            };
            panel.Controls.Add(today);

            _calendarGrid = new WidgetCalendarGrid();
            _calendarGrid.Location = new Point(12, 85);
            _calendarGrid.Size = new Size(panel.Width - 24, 92);
            _calendarGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _calendarGrid.ColumnCount = 7;
            _calendarGrid.RowCount = 7;
            _calendarGrid.Margin = new Padding(0);
            _calendarGrid.Padding = new Padding(0);
            _calendarGrid.BackColor = panel.BackColor;
            _calendarGrid.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            for (int i = 0; i < 7; i++) _calendarGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7F));
            _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
            for (int i = 0; i < 6; i++) _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 13));
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
            Panel panel = NewSection("tasks", 152);
            AddSectionHeader(panel, "tasks", T("Tasks", "وظایف"), 96);

            _taskCountLabel = new Label();
            _taskCountLabel.BackColor = panel.BackColor;
            _taskCountLabel.Location = new Point(_language == "fa" ? panel.Width - 130 : panel.Width - 100, 7);
            _taskCountLabel.Size = new Size(88, 21);
            _taskCountLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _taskCountLabel.TextAlign = ContentAlignment.MiddleRight;
            _taskCountLabel.Font = GetUiFont(7.4F, FontStyle.Regular);
            _taskCountLabel.ForeColor = _themeMutedText;
            panel.Controls.Add(_taskCountLabel);

            RoundedSurfacePanel taskInputFrame = new RoundedSurfacePanel();
            taskInputFrame.Location = new Point(12, 36);
            taskInputFrame.Size = new Size(panel.Width - 106, 26);
            taskInputFrame.FillColor = _themeInput;
            taskInputFrame.BorderColor = _themeBorder;
            panel.Controls.Add(taskInputFrame);

            _taskInput = new TextBox();
            _taskInput.Location = new Point(8, 5);
            _taskInput.Size = new Size(taskInputFrame.Width - 16, 16);
            _taskInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _taskInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _taskInput.BorderStyle = BorderStyle.None;
            _taskInput.BackColor = _themeInput;
            _taskInput.ForeColor = _themeText;
            _taskInput.Font = GetUiFont(8.6F, FontStyle.Regular);
            _taskInput.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { AddTask(); e.SuppressKeyPress = true; }
            };
            taskInputFrame.Controls.Add(_taskInput);

            _addTaskButton = MakeSmallButton(T("Add", "افزودن"), T("Add task", "افزودن وظیفه"), 72);
            _addTaskButton.Location = new Point(panel.Width - 84, 35);
            _addTaskButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _addTaskButton.Height = 28;
            _addTaskButton.Click += delegate { AddTask(); };
            panel.Controls.Add(_addTaskButton);

            _taskRowsPanel = new WidgetSurfacePanel();
            _taskRowsPanel.Location = new Point(12, 68);
            _taskRowsPanel.Size = new Size(panel.Width - 24, 76);
            _taskRowsPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _taskRowsPanel.AutoScroll = true;
            _taskRowsPanel.BackColor = panel.BackColor;
            panel.Controls.Add(_taskRowsPanel);

            panel.Resize += delegate
            {
                _taskCountLabel.Left = _language == "fa" ? panel.ClientSize.Width - 130 : panel.ClientSize.Width - _taskCountLabel.Width - 12;
                taskInputFrame.Width = Math.Max(90, panel.ClientSize.Width - 106);
                _taskInput.Width = Math.Max(72, taskInputFrame.ClientSize.Width - 16);
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
            Panel panel = NewSection("pomodoro", 152);
            AddSectionHeader(panel, "pomodoro", T("Pomodoro", "پومودورو"), 0);

            _pomodoroProgressIndicator = new CircularProgressIndicator();
            _pomodoroProgressIndicator.Size = new Size(96, 96);
            _pomodoroProgressIndicator.Location = new Point((panel.Width - 96) / 2, 24);
            _pomodoroProgressIndicator.Anchor = AnchorStyles.Top;
            _pomodoroProgressIndicator.TrackColor = _themeBorder;
            _pomodoroProgressIndicator.ProgressColor = _themeAccent;
            _pomodoroProgressIndicator.TextColor = _themeText;
            _pomodoroProgressIndicator.ModeColor = _themeMutedText;
            _pomodoroProgressIndicator.ModeFont = GetUiFont(7.8F, FontStyle.Bold);
            _pomodoroProgressIndicator.TimeFont = GetUiFont(18.5F, FontStyle.Regular);
            _pomodoroProgressIndicator.FaceColor = _themeSurface;
            panel.Controls.Add(_pomodoroProgressIndicator);

            // Session state remains in the timer logic and notifications, without a redundant caption under the dial.
            _pomodoroStatusLabel = null;

            _pomodoroStartButton = new VectorIconButton("play");
            _pomodoroStartButton.Size = new Size(30, 30);
            _pomodoroStartButton.Location = new Point(0, 118);
            _pomodoroStartButton.ForeColor = ContrastText(_themeAccent);
            _pomodoroStartButton.BackColor = _themeAccent;
            _pomodoroStartButton.Cursor = Cursors.Hand;
            _pomodoroStartButton.ShowBackground = true;
            _toolTip.SetToolTip(_pomodoroStartButton, T("Start or pause timer", "شروع یا توقف زمان‌سنج"));
            _pomodoroStartButton.Click += delegate { TogglePomodoro(); };
            panel.Controls.Add(_pomodoroStartButton);

            _pomodoroResetButton = new VectorIconButton("reset");
            _pomodoroResetButton.Size = new Size(28, 28);
            _pomodoroResetButton.Location = new Point(0, 119);
            _pomodoroResetButton.ForeColor = _themeMutedText;
            _pomodoroResetButton.BackColor = _themeButton;
            _pomodoroResetButton.Cursor = Cursors.Hand;
            _pomodoroResetButton.ShowBackground = true;
            _toolTip.SetToolTip(_pomodoroResetButton, T("Reset current session", "بازنشانی نشست جاری"));
            _pomodoroResetButton.Click += delegate { ResetPomodoro(); };
            panel.Controls.Add(_pomodoroResetButton);

            _pomodoroSwitchButton = new VectorIconButton("rest");
            _pomodoroSwitchButton.Size = new Size(28, 28);
            _pomodoroSwitchButton.Location = new Point(0, 119);
            _pomodoroSwitchButton.ForeColor = _themeText;
            _pomodoroSwitchButton.BackColor = _themeButton;
            _pomodoroSwitchButton.Cursor = Cursors.Hand;
            _pomodoroSwitchButton.ShowBackground = true;
            _toolTip.SetToolTip(_pomodoroSwitchButton, T("Switch between focus and rest", "تغییر بین تمرکز و استراحت"));
            _pomodoroSwitchButton.Click += delegate { SwitchPomodoroMode(); };
            panel.Controls.Add(_pomodoroSwitchButton);

            panel.Resize += delegate
            {
                _pomodoroProgressIndicator.Left = (panel.ClientSize.Width - _pomodoroProgressIndicator.Width) / 2;
                int totalButtons = _pomodoroStartButton.Width + _pomodoroResetButton.Width + _pomodoroSwitchButton.Width + 14;
                _pomodoroStartButton.Left = (panel.ClientSize.Width - totalButtons) / 2;
                _pomodoroResetButton.Left = _pomodoroStartButton.Right + 7;
                _pomodoroSwitchButton.Left = _pomodoroResetButton.Right + 7;
            };
        }

        private void BuildNotesSection()
        {
            Panel panel = NewSection("notes", 152);
            AddSectionHeader(panel, "notes", T("Quick Note", "یادداشت سریع"), 38);

            VectorIconButton saveAsButton = new VectorIconButton("export");
            saveAsButton.Size = new Size(26, 26);
            saveAsButton.Location = new Point(_language == "fa" ? panel.Width - 70 : panel.Width - 38, 3);
            saveAsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            saveAsButton.ForeColor = _themeAccent;
            saveAsButton.BackColor = Color.Transparent;
            saveAsButton.ShowBackground = false;
            saveAsButton.Cursor = Cursors.Hand;
            _toolTip.SetToolTip(saveAsButton, T("Export note as a text file", "ذخیره یادداشت به‌صورت فایل متنی"));
            saveAsButton.Click += delegate { SaveNoteAs(); };
            panel.Controls.Add(saveAsButton);

            RoundedSurfacePanel editorFrame = new RoundedSurfacePanel();
            editorFrame.Location = new Point(12, 35);
            editorFrame.Size = new Size(panel.Width - 24, 86);
            editorFrame.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            editorFrame.FillColor = _themeInput;
            editorFrame.BorderColor = _themeBorder;
            panel.Controls.Add(editorFrame);

            _notesBox = new TextBox();
            _notesBox.Location = new Point(10, 7);
            _notesBox.Size = new Size(editorFrame.Width - 20, editorFrame.Height - 14);
            _notesBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _notesBox.Multiline = true;
            _notesBox.AcceptsReturn = true;
            _notesBox.AcceptsTab = true;
            _notesBox.ScrollBars = ScrollBars.Vertical;
            _notesBox.BorderStyle = BorderStyle.None;
            _notesBox.BackColor = _themeInput;
            _notesBox.ForeColor = _themeText;
            _notesBox.Font = GetUiFont(8.8F, FontStyle.Regular);
            _notesBox.Text = LocalStore.LoadNotes();
            _notesBox.HandleCreated += delegate
            {
                SendMessageText(_notesBox.Handle, 0x1501, IntPtr.Zero,
                    T("Ideas, reminders, or anything to remember…", "ایده‌ها، یادآوری‌ها یا هر چیزی برای به‌خاطر سپردن…"));
            };
            _notesBox.TextChanged += delegate
            {
                _notesDirty = true;
                if (_notesStatusLabel != null) _notesStatusLabel.Text = T("Saving…", "در حال ذخیره…");
            };
            _notesBox.Leave += delegate { SaveNotesIfNeeded(); };
            editorFrame.Controls.Add(_notesBox);

            VectorIconView savedIcon = new VectorIconView("save");
            savedIcon.Size = new Size(18, 18);
            savedIcon.Location = new Point(11, panel.Height - 25);
            savedIcon.ForeColor = _themeAccent;
            savedIcon.BackColor = Color.Transparent;
            panel.Controls.Add(savedIcon);

            _notesStatusLabel = new Label();
            _notesStatusLabel.Text = T("Saved locally", "ذخیره‌شده روی دستگاه");
            _notesStatusLabel.BackColor = panel.BackColor;
            _notesStatusLabel.Location = new Point(32, panel.Height - 25);
            _notesStatusLabel.Size = new Size(panel.Width - 44, 17);
            _notesStatusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _notesStatusLabel.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            _notesStatusLabel.Font = GetUiFont(7.2F, FontStyle.Regular);
            _notesStatusLabel.ForeColor = _themeMutedText;
            panel.Controls.Add(_notesStatusLabel);

            panel.Resize += delegate
            {
                saveAsButton.Left = _language == "fa" ? panel.ClientSize.Width - 70 : panel.ClientSize.Width - saveAsButton.Width - 10;
                editorFrame.Width = panel.ClientSize.Width - 24;
                editorFrame.Height = Math.Max(45, panel.ClientSize.Height - editorFrame.Top - 30);
                _notesBox.Width = editorFrame.ClientSize.Width - 20;
                _notesBox.Height = editorFrame.ClientSize.Height - 14;
                savedIcon.Top = panel.ClientSize.Height - 24;
                _notesStatusLabel.Top = panel.ClientSize.Height - 25;
                _notesStatusLabel.Width = panel.ClientSize.Width - 44;
                ApplyScrollBarTheme();
            };
        }

        private void BuildRatesSection()
        {
            Panel panel = NewSection("rates", 152);
            AddSectionHeader(panel, "rates", T("Market Rates", "نرخ بازار"), 42);

            _ratesRefreshButton = new VectorIconButton("refresh");
            _ratesRefreshButton.Size = new Size(26, 26);
            _ratesRefreshButton.Location = new Point(_language == "fa" ? panel.Width - 70 : panel.Width - 38, 3);
            _ratesRefreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _ratesRefreshButton.ForeColor = _themeAccent;
            _ratesRefreshButton.BackColor = Color.Transparent;
            _ratesRefreshButton.ShowBackground = false;
            _ratesRefreshButton.Cursor = Cursors.Hand;
            _toolTip.SetToolTip(_ratesRefreshButton, T("Refresh market rates", "به‌روزرسانی نرخ‌ها"));
            _ratesRefreshButton.Click += delegate { RefreshRates(); };
            panel.Controls.Add(_ratesRefreshButton);

            int firstY = 34;
            for (int i = 0; i < _rateDefinitions.Length; i++)
            {
                MarketRate rate = _rateDefinitions[i];
                int rowY = firstY + i * 30;
                string iconKind = rate.Key == "usd" ? "usd" : (rate.Key == "eur" ? "eur" : "gold");

                CurrencyBadgeIcon rateIcon = new CurrencyBadgeIcon(iconKind);
                rateIcon.Size = new Size(25, 25);
                rateIcon.Location = new Point(10, rowY + 1);
                rateIcon.BackColor = Color.Transparent;
                panel.Controls.Add(rateIcon);

                Label name = new Label();
                name.Text = _language == "fa" ? rate.DisplayNamePersian : rate.DisplayNameEnglish;
                name.BackColor = panel.BackColor;
                name.Location = new Point(42, rowY);
                name.Size = new Size(66, 27);
                name.ForeColor = _themeText;
                name.Font = GetUiFont(7.8F, FontStyle.Regular);
                name.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
                name.Cursor = Cursors.Hand;
                name.Tag = rate.Url;
                name.Click += OpenMarketPage;
                panel.Controls.Add(name);

                Label value = new Label();
                value.Text = "—";
                value.BackColor = panel.BackColor;
                value.Location = new Point(108, rowY);
                value.Size = new Size(panel.Width - 120, 27);
                value.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                value.ForeColor = _themeAccent;
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
                    separator.Location = new Point(42, rowY + 29);
                    separator.Size = new Size(panel.Width - 54, 1);
                    separator.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    panel.Controls.Add(separator);
                }
            }

            _ratesStatusLabel = new Label();
            _ratesStatusLabel.Text = T("Waiting for live market data…", "در انتظار داده زنده بازار…");
            _ratesStatusLabel.BackColor = panel.BackColor;
            _ratesStatusLabel.Location = new Point(12, 126);
            _ratesStatusLabel.Size = new Size(panel.Width - 24, 16);
            _ratesStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _ratesStatusLabel.Font = GetUiFont(6.8F, FontStyle.Regular);
            _ratesStatusLabel.ForeColor = _themeMutedText;
            _ratesStatusLabel.TextAlign = _language == "fa" ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft;
            panel.Controls.Add(_ratesStatusLabel);

            panel.Resize += delegate
            {
                _ratesRefreshButton.Left = _language == "fa" ? panel.ClientSize.Width - 70 : panel.ClientSize.Width - _ratesRefreshButton.Width - 12;
                foreach (Control child in panel.Controls)
                {
                    if (child is Label && child.Tag is string && child.Location.Y >= firstY && child.Location.Y < firstY + 3 * 30 && child.Location.X >= 100)
                        child.Width = panel.ClientSize.Width - child.Left - 12;
                    else if (child is Panel && child.Height == 1 && child.Location.Y >= firstY)
                        child.Width = panel.ClientSize.Width - child.Left - 12;
                }
                _ratesStatusLabel.Width = panel.ClientSize.Width - 24;
            };
        }

        private Button MakeSmallButton(string text, string tooltip, int width)
        {
            Button b = new RoundedButton();
            b.Text = text;
            b.Width = width;
            b.Height = 25;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.BorderColor = _themeBorder;
            b.BackColor = _themeButton;
            b.ForeColor = text.Length <= 2 ? _themeAccentText : _themeText;
            b.Font = GetUiFont(8.2F, FontStyle.Bold);
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
            string time = now.ToString("HH:mm", CultureInfo.InvariantCulture);
            _clockLabel.Text = _language == "fa" ? ToPersianDigits(time) : time;

            string gregorian = now.ToString("dddd, dd MMMM yyyy", english);
            string solar = GetSolarLongDate(now, _language == "fa");
            _dateEnglishLabel.Text = _language == "fa" ? "میلادی  ·  " + ToPersianDigits(now.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)) : gregorian;
            _dateSolarLabel.Text = _language == "fa" ? solar : GetSolarLongDate(now, false);

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
            _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
            for (int i = 0; i < 6; i++) _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 13));
            _calendarGrid.RowCount = 7;
            _calendarGrid.ColumnCount = 7;

            string[] weekdays = _language == "fa" ? _weekdaysFa : _weekdaysEn;
            for (int col = 0; col < 7; col++)
            {
                int index = _showSolarCalendar ? (col + 5) % 7 : col;
                Label weekday = new Label();
                weekday.Text = _language == "fa" ? weekdays[index] : weekdays[index];
                weekday.BackColor = _calendarGrid.BackColor;
                if (_showSolarCalendar && _language != "fa") weekday.Text = new string[] { "Sat", "Sun", "Mon", "Tue", "Wed", "Thu", "Fri" }[col];
                weekday.Dock = DockStyle.Fill;
                weekday.Margin = new Padding(1);
                weekday.TextAlign = ContentAlignment.MiddleCenter;
                weekday.Font = GetUiFont(_language == "fa" ? 6.8F : 7.2F, FontStyle.Bold);
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
                dayButton.Font = GetUiFont(7.6F, selected || todayDate ? FontStyle.Bold : FontStyle.Regular);
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
                empty.BackColor = _taskRowsPanel.BackColor;
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
                ? T("Rest", "استراحت")
                : T("Focus", "تمرکز");
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
            ApplyGlassBackdrop();
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
                // Custom surfaces own their background colours; keep native theme painting off them.
                if (_notesBox != null && !_notesBox.IsDisposed)
                    _notesBox.BackColor = _themeInput;
                if (_calendarModeCombo != null && !_calendarModeCombo.IsDisposed)
                    SetWindowTheme(_calendarModeCombo.Handle, _themeUsesLightTaskbar ? "Explorer" : "DarkMode_CFD", null);
            }
            catch { }
        }

        private ContextMenuStrip CreateTrayMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Font = GetUiFont(9F, FontStyle.Regular);
            menu.ForeColor = Color.FromArgb(34, 40, 49);
            menu.BackColor = Color.White;
            menu.Padding = new Padding(4);
            menu.ImageScalingSize = new Size(18, 18);
            menu.ShowImageMargin = true;
            menu.ShowCheckMargin = true;
            menu.Renderer = new ToolStripProfessionalRenderer(new WidgetColorTable(
                Color.White, Color.FromArgb(239, 243, 248), Color.FromArgb(34, 40, 49),
                Color.FromArgb(105, 113, 125), Color.FromArgb(76, 86, 100), Color.FromArgb(215, 221, 230)));

            List<Image> ownedImages = new List<Image>();
            menu.Disposed += delegate
            {
                foreach (Image image in ownedImages) image.Dispose();
                ownedImages.Clear();
            };

            ToolStripMenuItem showItem = new ToolStripMenuItem(T("Show widget", "نمایش ویجت"));
            showItem.Enabled = AnyWidgetVisible() && !Visible;
            showItem.Click += delegate { ShowFromTray(); };
            AddTrayImage(showItem, "show", ownedImages);
            menu.Items.Add(showItem);

            ToolStripMenuItem hideItem = new ToolStripMenuItem(T("Hide widget", "مخفی‌کردن ویجت"));
            hideItem.Enabled = Visible;
            hideItem.Click += delegate { HideToTray(); };
            AddTrayImage(hideItem, "hide", ownedImages);
            menu.Items.Add(hideItem);
            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem widgetsItem = new ToolStripMenuItem(T("Visible widgets", "بخش‌های قابل نمایش"));
            AddTrayImage(widgetsItem, "widgets", ownedImages);
            AddWidgetToggle(widgetsItem, "clock", T("Clock and dates", "ساعت و تاریخ"));
            AddWidgetToggle(widgetsItem, "calendar", T("Calendar", "تقویم"));
            AddWidgetToggle(widgetsItem, "tasks", T("Tasks", "وظایف"));
            AddWidgetToggle(widgetsItem, "pomodoro", T("Pomodoro timer", "زمان‌سنج پومودورو"));
            AddWidgetToggle(widgetsItem, "notes", T("Quick note", "یادداشت سریع"));
            AddWidgetToggle(widgetsItem, "rates", T("Market rates", "نرخ بازار"));
            menu.Items.Add(widgetsItem);

            ToolStripMenuItem opacityItem = new ToolStripMenuItem(T("Widget opacity", "شفافیت ویجت"));
            AddTrayImage(opacityItem, "opacity", ownedImages);
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
            AddTrayImage(themeItem, "theme", ownedImages);
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
            AddTrayImage(ratesItem, "rates", ownedImages);
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
            AddTrayImage(languageItem, "language", ownedImages);
            ToolStripMenuItem englishItem = new ToolStripMenuItem("English");
            ToolStripMenuItem persianItem = new ToolStripMenuItem("فارسی");
            AddTrayImage(englishItem, "flag-en", ownedImages);
            AddTrayImage(persianItem, "flag-fa", ownedImages);
            englishItem.Checked = _language == "en";
            persianItem.Checked = _language == "fa";
            englishItem.Click += delegate { SetLanguage("en"); };
            persianItem.Click += delegate { SetLanguage("fa"); };
            languageItem.DropDownItems.Add(englishItem);
            languageItem.DropDownItems.Add(persianItem);
            menu.Items.Add(languageItem);

            ToolStripMenuItem fontItem = new ToolStripMenuItem(T("Font", "قلم"));
            AddTrayImage(fontItem, "font", ownedImages);
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
            AddTrayImage(topItem, "pin", ownedImages);
            topItem.Checked = _keepOnTop;
            topItem.CheckOnClick = true;
            topItem.Click += delegate { SetTopmost(topItem.Checked); };
            menu.Items.Add(topItem);

            ToolStripMenuItem startupItem = new ToolStripMenuItem(T("Start with Windows", "اجرا هنگام ورود به ویندوز"));
            AddTrayImage(startupItem, "startup", ownedImages);
            startupItem.Checked = IsStartupEnabled();
            startupItem.CheckOnClick = true;
            startupItem.Click += delegate { SetStartup(startupItem.Checked); };
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem exitItem = new ToolStripMenuItem(T("Exit", "خروج"));
            AddTrayImage(exitItem, "exit", ownedImages);
            exitItem.Click += delegate { ExitApplication(); };
            menu.Items.Add(exitItem);
            return menu;
        }

        private void AddTrayImage(ToolStripMenuItem item, string kind, List<Image> ownedImages)
        {
            Image image = CreateTrayImage(kind);
            item.Image = image;
            item.ImageTransparentColor = Color.Magenta;
            ownedImages.Add(image);
        }

        private Image CreateTrayImage(string kind)
        {
            int width = kind == "flag-en" || kind == "flag-fa" ? 20 : 18;
            Bitmap bitmap = new Bitmap(width, 18);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                if (kind == "flag-en")
                {
                    Rectangle flag = new Rectangle(1, 2, 18, 14);
                    using (SolidBrush navy = new SolidBrush(Color.FromArgb(25, 45, 92))) g.FillRectangle(navy, flag);
                    using (Pen whiteDiagonal = new Pen(Color.White, 4F))
                    {
                        g.DrawLine(whiteDiagonal, 1, 2, 19, 16);
                        g.DrawLine(whiteDiagonal, 19, 2, 1, 16);
                    }
                    using (Pen redDiagonal = new Pen(Color.FromArgb(196, 48, 57), 1.6F))
                    {
                        g.DrawLine(redDiagonal, 1, 2, 19, 16);
                        g.DrawLine(redDiagonal, 19, 2, 1, 16);
                    }
                    using (SolidBrush white = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(white, 8, 2, 4, 14);
                        g.FillRectangle(white, 1, 7, 18, 4);
                    }
                    using (SolidBrush red = new SolidBrush(Color.FromArgb(196, 48, 57)))
                    {
                        g.FillRectangle(red, 9, 2, 2, 14);
                        g.FillRectangle(red, 1, 8, 18, 2);
                    }
                    using (Pen border = new Pen(Color.FromArgb(100, 255, 255, 255))) g.DrawRectangle(border, flag);
                }
                else if (kind == "flag-fa")
                {
                    Rectangle flag = new Rectangle(1, 2, 18, 14);
                    using (SolidBrush green = new SolidBrush(Color.FromArgb(35, 138, 85))) g.FillRectangle(green, 1, 2, 18, 4);
                    using (SolidBrush white = new SolidBrush(Color.White)) g.FillRectangle(white, 1, 6, 18, 5);
                    using (SolidBrush red = new SolidBrush(Color.FromArgb(205, 55, 55))) g.FillRectangle(red, 1, 11, 18, 5);
                    using (SolidBrush emblem = new SolidBrush(Color.FromArgb(205, 55, 55)))
                    {
                        PointF[] mark = { new PointF(10, 6), new PointF(12, 9), new PointF(10, 12), new PointF(8, 9) };
                        g.FillPolygon(emblem, mark);
                    }
                    using (Pen border = new Pen(Color.FromArgb(100, 255, 255, 255))) g.DrawRectangle(border, flag);
                }
                else
                {
                    Color iconInk = Color.FromArgb(48, 56, 68);
                    using (Pen pen = new Pen(iconInk, 1.8F))
                    using (SolidBrush brush = new SolidBrush(iconInk))
                    {
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        pen.LineJoin = LineJoin.Round;
                        switch (kind)
                        {
                            case "show":
                                g.DrawEllipse(pen, 2, 5, 14, 8);
                                g.FillEllipse(brush, 7, 7, 4, 4);
                                break;
                            case "hide":
                                g.DrawEllipse(pen, 2, 5, 14, 8);
                                g.DrawLine(pen, 2, 2, 16, 16);
                                break;
                            case "widgets":
                                g.DrawRectangle(pen, 2, 2, 6, 6);
                                g.DrawRectangle(pen, 10, 2, 6, 6);
                                g.DrawRectangle(pen, 2, 10, 6, 6);
                                g.DrawRectangle(pen, 10, 10, 6, 6);
                                break;
                            case "opacity":
                                g.DrawEllipse(pen, 2, 2, 14, 14);
                                g.FillPie(brush, 2, 2, 14, 14, -90, 180);
                                break;
                            case "theme":
                                g.DrawArc(pen, 2, 2, 14, 14, 40, 280);
                                g.FillEllipse(brush, 10, 2, 6, 6);
                                break;
                            case "rates":
                                g.DrawLine(pen, 2, 15, 2, 3);
                                g.DrawLine(pen, 2, 15, 16, 15);
                                g.DrawLines(pen, new Point[] { new Point(4, 12), new Point(8, 8), new Point(11, 10), new Point(15, 4) });
                                break;
                            case "language":
                                using (Font languageFont = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Pixel))
                                    g.DrawString("文", languageFont, brush, 0, 0);
                                break;
                            case "font":
                                using (Font fontGlyph = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Pixel))
                                    g.DrawString("A", fontGlyph, brush, 1, 0);
                                break;
                            case "pin":
                                g.DrawLine(pen, 6, 2, 12, 8);
                                g.DrawLine(pen, 12, 8, 9, 11);
                                g.DrawLine(pen, 9, 11, 6, 8);
                                g.DrawLine(pen, 6, 8, 6, 2);
                                g.DrawLine(pen, 9, 11, 5, 16);
                                break;
                            case "startup":
                                g.DrawRectangle(pen, 2, 3, 14, 10);
                                g.DrawLine(pen, 7, 15, 11, 15);
                                g.DrawLine(pen, 9, 13, 9, 15);
                                break;
                            case "exit":
                                g.DrawLine(pen, 2, 9, 12, 9);
                                g.DrawLine(pen, 8, 5, 12, 9);
                                g.DrawLine(pen, 8, 13, 12, 9);
                                g.DrawLine(pen, 14, 3, 14, 15);
                                break;
                        }
                    }
                }
            }
            return bitmap;
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
            float scale = Math.Min(Width, Height) / 24F;
            if (scale <= 0F) return;
            g.TranslateTransform((Width - 24F * scale) / 2F, (Height - 24F * scale) / 2F);
            g.ScaleTransform(scale, scale);
            using (Pen pen = new Pen(ForeColor, 2.35F))
            using (Pen thin = new Pen(ForeColor, 1.95F))
            using (Brush brush = new SolidBrush(ForeColor))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                thin.StartCap = LineCap.Round; thin.EndCap = LineCap.Round; thin.LineJoin = LineJoin.Round;
                switch (Kind)
                {
                    case "clock":
                        g.DrawEllipse(pen, 3.2F, 3.2F, 17.6F, 17.6F);
                        g.DrawLine(pen, 12, 6.5F, 12, 12);
                        g.DrawLine(pen, 12, 12, 16.5F, 14.5F);
                        break;
                    case "calendar":
                        using (GraphicsPath calendar = RoundedRectangle(3.5F, 5, 17, 16, 2.2F)) g.DrawPath(pen, calendar);
                        g.DrawLine(pen, 3.5F, 9, 20.5F, 9);
                        g.DrawLine(pen, 8, 3, 8, 7);
                        g.DrawLine(pen, 16, 3, 16, 7);
                        g.FillEllipse(brush, 7, 12, 2.2F, 2.2F);
                        g.FillEllipse(brush, 11, 12, 2.2F, 2.2F);
                        g.FillEllipse(brush, 15, 12, 2.2F, 2.2F);
                        g.FillEllipse(brush, 7, 16, 2.2F, 2.2F);
                        g.FillEllipse(brush, 11, 16, 2.2F, 2.2F);
                        break;
                    case "tasks":
                        using (GraphicsPath list = RoundedRectangle(3.5F, 3.5F, 17, 17, 2.2F)) g.DrawPath(pen, list);
                        g.DrawLine(pen, 6.5F, 9, 9, 11.5F);
                        g.DrawLine(pen, 9, 11.5F, 12.5F, 7.5F);
                        g.DrawLine(thin, 14.5F, 9, 18, 9);
                        g.DrawLine(pen, 6.5F, 16, 9, 18.5F);
                        g.DrawLine(pen, 9, 18.5F, 12.5F, 14.5F);
                        g.DrawLine(thin, 14.5F, 16, 18, 16);
                        break;
                    case "pomodoro":
                        g.DrawEllipse(pen, 4.5F, 7, 15, 14);
                        g.DrawLine(pen, 12, 7, 12, 4);
                        g.DrawLine(pen, 9, 3, 15, 3);
                        g.DrawLine(pen, 12, 4, 12, 7);
                        g.DrawLine(pen, 12, 11, 12, 15);
                        g.DrawLine(pen, 12, 15, 16, 17);
                        break;
                    case "notes":
                        PointF[] paper = { new PointF(5, 3), new PointF(15, 3), new PointF(19, 7), new PointF(19, 21), new PointF(5, 21) };
                        g.DrawPolygon(pen, paper);
                        g.DrawLine(pen, 15, 3, 15, 7);
                        g.DrawLine(pen, 15, 7, 19, 7);
                        g.DrawLine(thin, 8.5F, 11, 16.5F, 11);
                        g.DrawLine(thin, 8.5F, 14.5F, 16.5F, 14.5F);
                        g.DrawLine(thin, 8.5F, 18, 14.5F, 18);
                        break;
                    case "rates":
                        g.DrawLine(pen, 4, 4, 4, 20);
                        g.DrawLine(pen, 4, 20, 21, 20);
                        PointF[] chart = { new PointF(6, 16), new PointF(10, 11), new PointF(13, 14), new PointF(19, 6) };
                        g.DrawLines(pen, chart);
                        g.FillEllipse(brush, 17.5F, 4.5F, 3.5F, 3.5F);
                        break;
                    case "save":
                        using (GraphicsPath disk = RoundedRectangle(4, 3, 16, 18, 2))
                            g.FillPath(brush, disk);
                        Color surface = Parent == null ? Color.FromArgb(31, 39, 52) : Parent.BackColor;
                        using (SolidBrush cutout = new SolidBrush(surface))
                        {
                            g.FillRectangle(cutout, 7, 4, 10, 5);
                            g.FillRectangle(cutout, 7, 13, 10, 7);
                        }
                        using (Pen detail = new Pen(Color.FromArgb(175, 255, 255, 255), 1.5F))
                        {
                            detail.StartCap = LineCap.Round;
                            detail.EndCap = LineCap.Round;
                            g.DrawLine(detail, 9, 5, 15, 5);
                            g.DrawLine(detail, 9, 15, 15, 15);
                            g.DrawLine(detail, 9, 18, 15, 18);
                        }
                        break;
                    default:
                        g.DrawEllipse(pen, 4, 4, 16, 16);
                        break;
                }
            }
        }

        private static GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class VectorIconButton : Button
    {
        private string _kind;
        private bool _hovered;
        public string Kind { get { return _kind; } set { _kind = value ?? "play"; Invalidate(); } }
        public bool ShowBackground { get; set; }

        public VectorIconButton(string kind)
        {
            _kind = kind ?? "play";
            ShowBackground = true;
            Text = string.Empty;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color parent = Parent == null ? SystemColors.Control : Parent.BackColor;
            using (SolidBrush fill = new SolidBrush(parent))
                e.Graphics.FillRectangle(fill, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (ShowBackground)
            {
                Color backgroundColor = _hovered ? ControlPaint.Light(BackColor, 0.10F) : BackColor;
                using (SolidBrush background = new SolidBrush(backgroundColor))
                    g.FillEllipse(background, 1, 1, Width - 3, Height - 3);
            }
            else if (_hovered)
            {
                Color parentColor = Parent == null ? BackColor : Parent.BackColor;
                using (SolidBrush background = new SolidBrush(ControlPaint.Light(parentColor, 0.07F)))
                    g.FillEllipse(background, 1, 1, Width - 3, Height - 3);
            }

            float scale = Math.Min(Width, Height) / 34F;
            if (scale <= 0F) return;
            g.TranslateTransform((Width - 34F * scale) / 2F, (Height - 34F * scale) / 2F);
            g.ScaleTransform(scale, scale);
            using (Pen pen = new Pen(ForeColor, 2.25F))
            using (SolidBrush brush = new SolidBrush(ForeColor))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                switch (_kind)
                {
                    case "play":
                        PointF[] triangle = { new PointF(13, 8), new PointF(23, 17), new PointF(13, 26) };
                        g.FillPolygon(brush, triangle);
                        break;
                    case "pause":
                        using (GraphicsPath bars = new GraphicsPath())
                        {
                            bars.AddRectangle(new Rectangle(10, 8, 5, 18));
                            bars.AddRectangle(new Rectangle(19, 8, 5, 18));
                            g.FillPath(brush, bars);
                        }
                        break;
                    case "reset":
                        g.DrawArc(pen, 8, 8, 19, 19, 35, 295);
                        PointF[] resetArrow = { new PointF(6, 8), new PointF(16, 8), new PointF(10, 16) };
                        g.FillPolygon(brush, resetArrow);
                        break;
                    case "refresh":
                        g.DrawArc(pen, 7, 7, 20, 20, 42, 285);
                        PointF[] refreshArrow = { new PointF(23, 5), new PointF(28, 11), new PointF(20, 12) };
                        g.FillPolygon(brush, refreshArrow);
                        break;
                    case "rest":
                        g.DrawBezier(pen, new PointF(10, 5), new PointF(7, 7), new PointF(13, 8), new PointF(10, 11));
                        g.DrawBezier(pen, new PointF(17, 5), new PointF(14, 7), new PointF(20, 8), new PointF(17, 11));
                        using (GraphicsPath cup = new GraphicsPath())
                        {
                            cup.AddLine(7, 14, 23, 14);
                            cup.AddLine(23, 14, 21, 26);
                            cup.AddBezier(21, 26, 20, 28, 10, 28, 9, 26);
                            cup.AddLine(9, 26, 7, 14);
                            cup.CloseFigure();
                            g.DrawPath(pen, cup);
                        }
                        g.DrawArc(pen, 20, 16, 9, 8, 270, 180);
                        g.DrawLine(pen, 8, 30, 22, 30);
                        break;
                    case "export":
                        g.DrawLine(pen, 17, 5, 17, 20);
                        g.DrawLine(pen, 11, 14, 17, 20);
                        g.DrawLine(pen, 23, 14, 17, 20);
                        g.DrawLine(pen, 7, 25, 7, 29);
                        g.DrawLine(pen, 7, 29, 27, 29);
                        g.DrawLine(pen, 27, 29, 27, 25);
                        break;
                }
            }
        }
    }

    internal sealed class CurrencyBadgeIcon : Control
    {
        private readonly string _kind;

        public CurrencyBadgeIcon(string kind)
        {
            _kind = kind ?? "usd";
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

            if (_kind == "usd")
            {
                int diameter = Math.Min(Width, Height) - 2;
                Rectangle circle = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
                GraphicsState state = g.Save();
                using (GraphicsPath clip = new GraphicsPath())
                {
                    clip.AddEllipse(circle);
                    g.SetClip(clip);
                    using (SolidBrush white = new SolidBrush(Color.White)) g.FillRectangle(white, circle);
                    using (SolidBrush red = new SolidBrush(Color.FromArgb(190, 42, 54)))
                        for (int i = 0; i < 7; i++) g.FillRectangle(red, circle.Left, circle.Top + 1 + i * 3, diameter, 1.6F);
                    using (SolidBrush navy = new SolidBrush(Color.FromArgb(33, 54, 105)))
                        g.FillRectangle(navy, circle.Left, circle.Top, diameter * 0.55F, diameter * 0.56F);
                    using (SolidBrush stars = new SolidBrush(Color.White))
                        for (int row = 0; row < 3; row++)
                            for (int col = 0; col < 3; col++)
                                g.FillEllipse(stars, circle.Left + 2 + col * 3, circle.Top + 2 + row * 3, 1.2F, 1.2F);
                }
                g.Restore(state);
                using (Pen border = new Pen(Color.FromArgb(150, 255, 255, 255), 0.8F)) g.DrawEllipse(border, circle);
            }
            else if (_kind == "eur")
            {
                int diameter = Math.Min(Width, Height) - 2;
                Rectangle circle = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
                using (SolidBrush blue = new SolidBrush(Color.FromArgb(28, 69, 151))) g.FillEllipse(blue, circle);
                using (SolidBrush gold = new SolidBrush(Color.FromArgb(255, 207, 62)))
                {
                    float cx = circle.Left + circle.Width / 2F;
                    float cy = circle.Top + circle.Height / 2F;
                    float radius = diameter * 0.31F;
                    for (int i = 0; i < 12; i++)
                    {
                        double angle = i * Math.PI / 6.0 - Math.PI / 2.0;
                        float x = cx + (float)Math.Cos(angle) * radius - 1F;
                        float y = cy + (float)Math.Sin(angle) * radius - 1F;
                        g.FillEllipse(gold, x, y, 2F, 2F);
                    }
                }
                using (Pen border = new Pen(Color.FromArgb(150, 255, 255, 255), 0.8F)) g.DrawEllipse(border, circle);
            }
            else
            {
                // Compact bullion illustration with a broad top face and beveled lower sides.
                PointF[] body = {
                    new PointF(4, 9), new PointF(Width - 4, 9),
                    new PointF(Width - 2, 14), new PointF(Width - 6, Height - 3),
                    new PointF(6, Height - 3), new PointF(2, 14)
                };
                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(65, 0, 0, 0)))
                    g.FillEllipse(shadow, new RectangleF(3, Height - 7, Width - 6, 5));
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddPolygon(body);
                    using (LinearGradientBrush gold = new LinearGradientBrush(
                        new Rectangle(2, 8, Width - 4, Height - 10),
                        Color.FromArgb(255, 224, 126), Color.FromArgb(170, 111, 27), 90F))
                        g.FillPath(gold, path);
                    using (Pen edge = new Pen(Color.FromArgb(218, 171, 68), 0.9F))
                        g.DrawPath(edge, path);
                }
                PointF[] top = {
                    new PointF(4, 9), new PointF(Width - 4, 9),
                    new PointF(Width - 8, 14), new PointF(8, 14)
                };
                using (LinearGradientBrush facet = new LinearGradientBrush(
                    new Rectangle(4, 9, Width - 8, 6),
                    Color.FromArgb(255, 245, 190), Color.FromArgb(231, 180, 79), 90F))
                    g.FillPolygon(facet, top);
                using (Pen shine = new Pen(Color.FromArgb(210, 255, 242, 190), 1F))
                    g.DrawLine(shine, 6, 10, Width - 6, 10);
            }
        }
    }

    internal sealed class RoundedButton : Button
    {
        private bool _hovered;
        private bool _pressed;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) _pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color parentColor = Parent == null ? SystemColors.Control : Parent.BackColor;
            using (SolidBrush outside = new SolidBrush(parentColor)) g.FillRectangle(outside, ClientRectangle);
            Rectangle bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            using (GraphicsPath path = RoundedPath(bounds, 7))
            using (SolidBrush fill = new SolidBrush(_pressed ? ControlPaint.Dark(BackColor, 0.08F) :
                (_hovered ? ControlPaint.Light(BackColor, 0.08F) : BackColor)))
            using (Pen border = new Pen(FlatAppearance.BorderColor.IsEmpty ? ControlPaint.Light(BackColor, 0.12F) : FlatAppearance.BorderColor, 0.8F))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, Enabled ? ForeColor : SystemColors.GrayText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class WidgetSurfacePanel : Panel
    {
        public WidgetSurfacePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush fill = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(fill, ClientRectangle);
        }
    }

    internal sealed class WidgetCalendarGrid : TableLayoutPanel
    {
        public WidgetCalendarGrid()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (SolidBrush fill = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(fill, ClientRectangle);
        }
    }

    internal sealed class RoundedSurfacePanel : Panel
    {
        public Color FillColor { get; set; }
        public Color BorderColor { get; set; }

        public RoundedSurfacePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            FillColor = Color.FromArgb(30, 36, 48);
            BorderColor = Color.FromArgb(70, 80, 96);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color parentColor = Parent == null ? SystemColors.Control : Parent.BackColor;
            e.Graphics.Clear(parentColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedPath(bounds, 8))
            using (SolidBrush fill = new SolidBrush(FillColor))
            using (Pen border = new Pen(BorderColor, 0.8F))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class WidgetColorTable : ProfessionalColorTable
    {
        private readonly Color _surface;
        private readonly Color _surfaceAlt;
        private readonly Color _text;
        private readonly Color _muted;
        private readonly Color _accent;
        private readonly Color _border;

        public WidgetColorTable(Color surface, Color surfaceAlt, Color text, Color muted, Color accent, Color border)
        {
            _surface = surface;
            _surfaceAlt = surfaceAlt;
            _text = text;
            _muted = muted;
            _accent = accent;
            _border = border;
            UseSystemColors = false;
        }

        public override Color ToolStripDropDownBackground { get { return _surface; } }
        public override Color MenuBorder { get { return _border; } }
        public override Color MenuItemSelected { get { return _surfaceAlt; } }
        public override Color MenuItemBorder { get { return _accent; } }
        public override Color ImageMarginGradientBegin { get { return _surface; } }
        public override Color ImageMarginGradientMiddle { get { return _surface; } }
        public override Color ImageMarginGradientEnd { get { return _surface; } }
        public override Color SeparatorDark { get { return _border; } }
        public override Color SeparatorLight { get { return _surface; } }
        public override Color CheckBackground { get { return _surfaceAlt; } }
        public override Color CheckSelectedBackground { get { return _surfaceAlt; } }
        public override Color CheckPressedBackground { get { return _surfaceAlt; } }
        public override Color ButtonSelectedGradientBegin { get { return _surfaceAlt; } }
        public override Color ButtonSelectedGradientMiddle { get { return _surfaceAlt; } }
        public override Color ButtonSelectedGradientEnd { get { return _surfaceAlt; } }
        public override Color ButtonPressedGradientBegin { get { return _surfaceAlt; } }
        public override Color ButtonPressedGradientMiddle { get { return _surfaceAlt; } }
        public override Color ButtonPressedGradientEnd { get { return _surfaceAlt; } }
        public override Color GripDark { get { return _muted; } }
        public override Color GripLight { get { return _border; } }
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
            using (Pen track = new Pen(TrackColor, 5.8F))
            using (Pen glow = new Pen(Color.FromArgb(42, ProgressColor), 9F))
            using (Pen progress = new Pen(ProgressColor, 5.8F))
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
                {
                    float sweep = Math.Min(359.8F, 360F * _progress);
                    g.DrawArc(glow, bounds, -90, sweep);
                    g.DrawArc(progress, bounds, -90, sweep);
                }
                // ModeFont and TimeFont are borrowed from MainForm's shared font cache.
                // Never dispose them here: OnPaint runs repeatedly and the cache owns their lifetime.
                Font modeFont = ModeFont ?? SystemFonts.DefaultFont;
                Font timeFont = TimeFont ?? SystemFonts.DefaultFont;

                RectangleF modeBounds = new RectangleF(13, Height * 0.29F, Width - 26, 17);
                RectangleF timeBounds = new RectangleF(10, Height * 0.44F, Width - 20, 34);
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
        private const int CornerRadius = 16;

        public SectionPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = SystemColors.Control;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateCornerRegion();
        }

        private void UpdateCornerRegion()
        {
            if (Width < 4 || Height < 4) return;
            using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, Width, Height), CornerRadius))
            {
                Region old = Region;
                Region = new Region(path);
                if (old != null) old.Dispose();
            }
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
            using (SolidBrush fill = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(fill, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius))
            using (Pen border = new Pen(BorderColor.IsEmpty ? SystemColors.ControlDark : BorderColor, 0.9F))
            using (Pen highlight = new Pen(Color.FromArgb(34, 255, 255, 255), 0.8F))
            {
                e.Graphics.DrawPath(border, path);
                e.Graphics.DrawLine(highlight, 20, 1, Math.Max(20, Width - 20), 1);
            }
        }
    }
}