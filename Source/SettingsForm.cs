using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChronoDeskWidget
{
    internal sealed class SettingsForm : Form
    {
        private readonly Action<string, bool> _setWidget;
        private readonly Action<int> _setOpacity;
        private readonly Action<string> _setTheme, _setFont, _setRateUnit, _setLanguage, _setClockFormat;
        private readonly Action<bool> _setTopmost, _setStartup;
        private readonly Action _refreshRates;
        private readonly Dictionary<string, bool> _widgets;
        private readonly Dictionary<string, Button> _nav = new Dictionary<string, Button>();
        private readonly string _language, _theme, _font, _unit, _clockFormat;
        private readonly int _opacity;
        private readonly bool _topmost, _startup;
        private readonly Color _background = Color.FromArgb(15, 20, 30);
        private readonly Color _sidebar = Color.FromArgb(18, 25, 37);
        private readonly Color _surface = Color.FromArgb(24, 32, 46);
        private readonly Color _surfaceHover = Color.FromArgb(31, 42, 59);
        private readonly Color _border = Color.FromArgb(43, 56, 75);
        private readonly Color _text = Color.FromArgb(237, 242, 249);
        private readonly Color _muted = Color.FromArgb(143, 158, 178);
        private readonly Color _accent = Color.FromArgb(83, 143, 238);
        private readonly Color _accentSoft = Color.FromArgb(31, 57, 91);
        private Panel _pageHost;
        private FlowLayoutPanel _page;
        private Label _pageTitle, _pageDescription;
        private string _active = "General";
        private Point _dragOrigin;

        public SettingsForm(string language, string theme, int opacity, string font, string rateUnit,
            bool topmost, bool startup, IDictionary<string, bool> widgets,
            Action<string, bool> setWidget, Action<int> setOpacity, Action<string> setTheme,
            Action<string> setFont, Action<string> setRateUnit, Action<string> setLanguage,
            Action<bool> setTopmost, Action<bool> setStartup, Action refreshRates,
            string clockFormat, Action<string> setClockFormat)
        {
            _language = language; _theme = theme; _opacity = opacity; _font = font; _unit = rateUnit;
            _topmost = topmost; _startup = startup; _clockFormat = clockFormat;
            _setWidget = setWidget; _setOpacity = setOpacity; _setTheme = setTheme; _setFont = setFont;
            _setRateUnit = setRateUnit; _setLanguage = setLanguage; _setTopmost = setTopmost;
            _setStartup = setStartup; _refreshRates = refreshRates; _setClockFormat = setClockFormat;
            _widgets = new Dictionary<string, bool>(widgets, StringComparer.OrdinalIgnoreCase);

            Text = "Settings";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1080, 760);
            MinimumSize = new Size(920, 650);
            BackColor = _background; ForeColor = _text;
            Font = new Font("Segoe UI", 9F);
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false; MinimizeBox = false;
            DoubleBuffered = true;

            Panel topBar = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = _background, Padding = new Padding(22, 0, 14, 0) };
            Controls.Add(topBar);
            Panel settingIcon = new Panel { Location = new Point(22, 17), Size = new Size(24, 24), BackColor = Color.Transparent };
            settingIcon.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(164, 202, 255), 2.1F))
                {
                    e.Graphics.DrawEllipse(p, 7, 7, 10, 10);
                    e.Graphics.DrawEllipse(p, 10, 10, 4, 4);
                    e.Graphics.DrawLine(p, 12, 1, 12, 6); e.Graphics.DrawLine(p, 12, 18, 12, 23);
                    e.Graphics.DrawLine(p, 1, 12, 6, 12); e.Graphics.DrawLine(p, 18, 12, 23, 12);
                    e.Graphics.DrawLine(p, 4, 4, 7, 7); e.Graphics.DrawLine(p, 17, 17, 20, 20);
                    e.Graphics.DrawLine(p, 4, 20, 7, 17); e.Graphics.DrawLine(p, 17, 7, 20, 4);
                }
            };
            topBar.Controls.Add(settingIcon);
            topBar.Controls.Add(new Label { Text = "Setting", Location = new Point(57, 17), AutoSize = true, ForeColor = _text, Font = new Font("Segoe UI Semibold", 13F) });
            Button close = new Button { Text = "×", Size = new Size(34, 34), Location = new Point(Width - 52, 12), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }, BackColor = _background, ForeColor = _muted, Font = new Font("Segoe UI", 19F), TextAlign = ContentAlignment.MiddleCenter, UseVisualStyleBackColor = false, Cursor = Cursors.Hand };
            close.FlatAppearance.MouseOverBackColor = Color.FromArgb(174, 55, 65);
            close.FlatAppearance.MouseDownBackColor = Color.FromArgb(140, 42, 52);
            close.Click += delegate { Close(); };
            topBar.Controls.Add(close);
            AttachDragHandlers(topBar);
            AttachDragHandlers(settingIcon);
            topBar.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = _border });

            Panel sidebar = new Panel { Dock = DockStyle.Left, Width = 218, BackColor = _sidebar, Padding = new Padding(14, 18, 14, 14) };
            Controls.Add(sidebar);
            Panel main = new Panel { Dock = DockStyle.Fill, BackColor = _background, Padding = new Padding(30, 24, 30, 22) };
            Controls.Add(main);

            FlowLayoutPanel nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = false, BackColor = _sidebar, Padding = new Padding(0, 2, 0, 0) };
            sidebar.Controls.Add(nav);
            AddNav(nav, "General", "◉");
            AddNav(nav, "Appearance", "◐");
            AddNav(nav, "Layout", "▣");
            AddNav(nav, "Language", "文");
            AddNav(nav, "Widget", "▦");
            AddNav(nav, "Update", "↻");
            AddNav(nav, "About", "ⓘ");

            Panel heading = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = _background };
            main.Controls.Add(heading);
            _pageTitle = new Label { Text = "General", AutoSize = true, Location = new Point(0, 0), ForeColor = _text, Font = new Font("Segoe UI Semibold", 21F) };
            _pageDescription = new Label { Text = "Manage startup and desktop behavior.", AutoSize = true, Location = new Point(2, 42), ForeColor = _muted, Font = new Font("Segoe UI", 9.5F) };
            heading.Controls.Add(_pageTitle); heading.Controls.Add(_pageDescription);
            heading.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = _border });
            _pageHost = new Panel { Dock = DockStyle.Fill, BackColor = _background, Padding = new Padding(0, 18, 4, 0) };
            main.Controls.Add(_pageHost);
            main.Controls.SetChildIndex(heading, 0);
            main.Controls.SetChildIndex(_pageHost, 1);
            Controls.SetChildIndex(topBar, 0);
            Controls.SetChildIndex(sidebar, 1);
            Controls.SetChildIndex(main, 2);
            _pageHost.Resize += delegate { ResizePageItems(); };
            AttachDragHandlers(main);
            ShowPage("General");
        }


        private void AttachDragHandlers(Control root)
        {
            if (root == null) return;
            if (!(root is Button) && !(root is CheckBox) && !(root is ComboBox) &&
                !(root is TextBox) && !(root is NumericUpDown) && !(root is TrackBar))
            {
                root.MouseDown -= BeginWindowDrag;
                root.MouseDown += BeginWindowDrag;
            }

            foreach (Control child in root.Controls)
                AttachDragHandlers(child);
        }

        private void BeginWindowDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _dragOrigin = e.Location;
            if (e.Button == MouseButtons.Left)
            {
                Capture = false;
                Message m = Message.Create(Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
                WndProc(ref m);
            }
        }

        private void AddNav(FlowLayoutPanel parent, string key, string glyph)
        {
            Button b = new Button {
                Text = glyph + "     " + key, TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(188, 45), Margin = new Padding(0, 2, 0, 5),
                FlatStyle = FlatStyle.Flat, BackColor = _sidebar, ForeColor = _text,
                Font = new Font("Segoe UI", 9.5F), Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = _surfaceHover;
            b.Click += delegate { ShowPage(key); };
            parent.Controls.Add(b); _nav[key] = b;
        }

        private void ShowPage(string key)
        {
            _active = key;
            foreach (KeyValuePair<string, Button> item in _nav)
            {
                bool selected = item.Key == key;
                item.Value.BackColor = selected ? _accentSoft : _sidebar;
                item.Value.ForeColor = selected ? Color.FromArgb(164, 202, 255) : _text;
                item.Value.FlatAppearance.BorderSize = selected ? 1 : 0;
                item.Value.FlatAppearance.BorderColor = _accent;
            }

            _pageHost.Controls.Clear();
            _page = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = _background, Padding = new Padding(0, 0, 12, 16) };
            _pageHost.Controls.Add(_page);
            _pageTitle.Text = key;
            _pageDescription.Text = Subtitle(key);

            switch (key)
            {
                case "General": BuildGeneral(); break;
                case "Appearance": BuildAppearance(); break;
                case "Layout": BuildLayout(); break;
                case "Language": BuildLanguage(); break;
                case "Widget": BuildWidgets(); break;
                case "Update": BuildUpdate(); break;
                case "About": BuildAbout(); break;
            }
            AttachDragHandlers(_page);
            ResizePageItems();
        }

        private string Subtitle(string key)
        {
            switch (key)
            {
                case "General": return "Startup, behavior, and everyday preferences.";
                case "Appearance": return "Theme, transparency, typography, and visual details.";
                case "Layout": return "Position, dimensions, spacing, and desktop arrangement.";
                case "Language": return "Choose the language and regional formats.";
                case "Widget": return "Manage desktop widgets and each widget's options.";
                case "Update": return "Update preferences and installed version information.";
                default: return "Product information, support, and legal details.";
            }
        }

        private void BuildGeneral()
        {
            Section("Startup", "Choose how ChronoDesk starts and behaves.");
            CheckCard("Start with Windows", "Launch automatically when you sign in to Windows.", _startup, delegate(bool v) { _setStartup(v); });
            CheckCard("Keep on top", "Keep the widget above other application windows.", _topmost, delegate(bool v) { _setTopmost(v); });
            Section("Interaction", "Configure common desktop behavior.");
            StaticCombo("Close button behavior", "Choose what happens when the main close control is used.", "Exit application", "Exit application", "Minimize to tray");
            StaticCombo("Launch behavior", "Choose how the widget appears when the application starts.", "Restore last position", "Restore last position", "Open at default position");
            StaticToggle("Show confirmation before exit", "Ask before closing the application completely.", false);
            StaticToggle("Remember window position", "Restore the last saved desktop position.", true);
            Section("Notifications", "Control optional desktop notifications.");
            StaticToggle("Show update notifications", "Notify when a new application version is available.", true);
            StaticToggle("Show widget alerts", "Allow widgets to display relevant status messages.", true);
        }

        private void BuildAppearance()
        {
            Section("Theme", "Choose the overall visual style.");
            ComboCard("Color theme", "Follow Windows or use a fixed appearance.", new[] { "System", "Light", "Dark" }, _theme == "light" ? "Light" : _theme == "dark" ? "Dark" : "System",
                delegate(string v) { _setTheme(v.ToLowerInvariant()); });
            StaticCombo("Accent color", "Choose the highlight color used for selection and controls.", "Blue", "Blue", "Purple", "Teal", "Green", "Orange", "Custom");
            Section("Transparency & typography", "Tune contrast and readability.");
            ComboCard("Widget opacity", "Set the transparency level of the desktop widget.", new[] { "100%", "90%", "80%", "70%", "60%", "50%" }, _opacity + "%",
                delegate(string v) { _setOpacity(int.Parse(v.TrimEnd('%'))); });
            ComboCard("Font family", "Choose the typeface used by widget content.", new[] { "Segoe UI", "Arial", "Calibri", "Tahoma", "Verdana", "Microsoft YaHei UI" }, _font,
                delegate(string v) { _setFont(v); });
            StaticCombo("Font size", "Set the default text scale across widgets.", "Medium", "Small", "Medium", "Large", "Extra large");
            StaticToggle("Use compact controls", "Reduce control padding for a denser appearance.", false);
            StaticToggle("Use animations", "Enable subtle transitions when opening and updating widgets.", true);
        }

        private void BuildLayout()
        {
            Section("Widget position", "Set where the widget sits on the desktop.");
            StaticCombo("Horizontal alignment", "Choose the default horizontal anchor.", "Right", "Left", "Center", "Right");
            StaticCombo("Vertical alignment", "Choose the default vertical anchor.", "Top", "Top", "Center", "Bottom");
            StaticCombo("Screen", "Choose which display hosts the widget.", "Primary display", "Primary display", "Display 2", "Display 3", "Active display");
            StaticToggle("Remember position per display", "Keep a separate position for each monitor.", true);
            Section("Dimensions & spacing", "Control the footprint and arrangement of widget panels.");
            StaticCombo("Widget scale", "Scale widget content without changing display resolution.", "100%", "75%", "85%", "100%", "110%", "125%", "150%");
            StaticCombo("Widget spacing", "Set the gap between visible widget panels.", "12 px", "4 px", "8 px", "12 px", "16 px", "20 px");
            StaticCombo("Panel corner radius", "Choose the corner rounding used by widget panels.", "12 px", "0 px", "6 px", "10 px", "12 px", "16 px");
            StaticToggle("Snap to screen edges", "Align the widget to nearby screen edges when moving it.", true);
            StaticToggle("Keep widgets inside work area", "Avoid the taskbar and reserved desktop areas.", true);
            StaticToggle("Lock widget position", "Prevent accidental movement on the desktop.", false);
            Section("Multi-widget arrangement", "Choose how multiple widgets are organized.");
            StaticCombo("Widget arrangement", "Select the default panel layout.", "Free placement", "Free placement", "Vertical stack", "Horizontal row", "Grid");
            StaticToggle("Align widget edges", "Keep neighboring widget edges aligned.", false);
        }

        private void BuildLanguage()
        {
            Section("Interface language", "Select the language used throughout the application.");
            ComboCard("Application language", "Switch between currently supported languages.", new[] { "English", "فارسی" }, _language == "fa" ? "فارسی" : "English",
                delegate(string v) { _setLanguage(v == "فارسی" ? "fa" : "en"); });
            Section("Regional formats", "Choose how dates, numbers, and time are displayed.");
            StaticCombo("Date format", "Set the date order and separator.", "System default", "System default", "DD/MM/YYYY", "MM/DD/YYYY", "YYYY/MM/DD");
            StaticCombo("First day of week", "Choose the first day shown in calendars.", "System default", "System default", "Saturday", "Sunday", "Monday");
            StaticCombo("Number format", "Choose decimal and thousands separators.", "System default", "System default", "1,234.56", "1.234,56");
            StaticToggle("Use right-to-left layout when supported", "Apply right-to-left reading direction for supported languages.", true);
            InfoCard("Available languages", "English and Persian are currently available. Additional translations can be added later.");
        }

        private void BuildWidgets()
        {
            Section("Desktop widgets", "Enable the panels you want on the desktop. Select a widget's options to configure its display.");
            WidgetCard("Clock", "Time, date, and clock format", "clock", delegate
            {
                Section("Clock options", "Display preferences for the clock widget.");
                StaticCombo("Time format", "Choose the clock convention.", _clockFormat == "12h" ? "12-hour" : "24-hour", "24-hour", "12-hour");
                StaticToggle("Show seconds", "Display seconds alongside hours and minutes.", false);
                StaticToggle("Show date", "Display the current date below the time.", true);
                StaticCombo("Date style", "Choose the date detail shown by the clock.", "Short date", "Short date", "Long date", "Day of week");
            });
            WidgetCard("Calendar", "Monthly calendar and date navigation", "calendar", delegate
            {
                Section("Calendar options", "Configure calendar appearance and behavior.");
                StaticCombo("Calendar system", "Choose the calendar used for dates.", "System default", "Gregorian", "Solar Hijri");
                StaticCombo("Week starts on", "Choose the first weekday.", "System default", "Saturday", "Sunday", "Monday");
                StaticToggle("Show adjacent month days", "Display days from the previous and next months.", true);
                StaticToggle("Highlight current day", "Emphasize today's date in the calendar.", true);
            });
            WidgetCard("Tasks", "Personal task list and completion tracking", "tasks", delegate
            {
                Section("Task options", "Configure task list behavior.");
                StaticToggle("Show completed tasks", "Keep completed items visible in the list.", true);
                StaticToggle("Show task count", "Display the number of active tasks.", true);
                StaticCombo("Default sort order", "Choose how tasks are ordered.", "Manual", "Manual", "Due date", "Creation date", "Priority");
            });
            WidgetCard("Pomodoro", "Focus sessions and break timer", "pomodoro", delegate
            {
                Section("Pomodoro options", "Set default focus and break durations.");
                StaticCombo("Focus duration", "Length of a focus session.", "25 minutes", "15 minutes", "20 minutes", "25 minutes", "30 minutes", "45 minutes", "60 minutes");
                StaticCombo("Short break", "Length of a short break.", "5 minutes", "3 minutes", "5 minutes", "10 minutes");
                StaticCombo("Long break", "Length of a long break.", "15 minutes", "10 minutes", "15 minutes", "20 minutes", "30 minutes");
                StaticCombo("Long break interval", "Number of focus sessions before a long break.", "4 sessions", "2 sessions", "3 sessions", "4 sessions", "5 sessions");
                StaticToggle("Auto-start breaks", "Start a break automatically when focus ends.", false);
                StaticToggle("Auto-start focus sessions", "Start the next focus session after a break.", false);
            });
            WidgetCard("Quick Notes", "Lightweight notes stored on this device", "notes", delegate
            {
                Section("Notes options", "Configure the notes panel.");
                StaticCombo("Default note size", "Choose the initial note dimensions.", "Medium", "Small", "Medium", "Large");
                StaticToggle("Always on top", "Keep the notes panel above other windows.", false);
                StaticToggle("Confirm before clearing", "Ask before deleting note content.", true);
                StaticToggle("Auto-save while typing", "Save note changes automatically.", true);
            });
            WidgetCard("Market Rates", "Currency, gold, and market quote panel", "rates", delegate
            {
                Section("Market rates options", "Configure displayed quote values.");
                ComboCard("Display currency", "Choose the unit used for market values.", new[] { "Toman (T)", "Dollar ($)" }, _unit == "$" ? "Dollar ($)" : "Toman (T)",
                    delegate(string v) { _setRateUnit(v.StartsWith("Dollar", StringComparison.Ordinal) ? "$" : "T"); });
                StaticCombo("Refresh interval", "How often quotes should be refreshed.", "5 minutes", "1 minute", "5 minutes", "15 minutes", "30 minutes", "1 hour");
                StaticToggle("Show change percentage", "Display the percentage change beside each quote.", true);
                StaticToggle("Show last updated time", "Display the most recent quote update time.", true);
                ActionCard("Refresh market data", "Request the latest available market quotes.", "Refresh now", delegate { _refreshRates(); });
            });
            WidgetCard("Network Monitor", "Live upload and download activity", "network", delegate
            {
                Section("Network monitor options", "Configure network statistics.");
                StaticCombo("Speed unit", "Choose the displayed transfer unit.", "MB/s", "KB/s", "MB/s", "Mbps");
                StaticToggle("Show upload speed", "Display current upload activity.", true);
                StaticToggle("Show download speed", "Display current download activity.", true);
                StaticToggle("Show session totals", "Display uploaded and downloaded totals.", true);
            });
            WidgetCard("Calculator", "Quick arithmetic and expression input", "calculator", delegate
            {
                Section("Calculator options", "Configure calculator behavior.");
                StaticToggle("Keep calculation history", "Retain recent calculations in the panel.", true);
                StaticToggle("Use thousands separators", "Format large results for readability.", true);
                StaticCombo("Precision", "Choose decimal places for displayed results.", "Auto", "Auto", "2", "4", "6", "8");
            });
            WidgetCard("System Monitor", "CPU, memory, GPU, and storage usage", "system", delegate
            {
                Section("System monitor options", "Choose which system metrics are displayed.");
                StaticCombo("Refresh interval", "How often system metrics are sampled.", "2 seconds", "1 second", "2 seconds", "5 seconds", "10 seconds");
                StaticToggle("Show CPU usage", "Display processor utilization.", true);
                StaticToggle("Show memory usage", "Display physical memory utilization.", true);
                StaticToggle("Show GPU usage", "Display graphics processor utilization when available.", true);
                StaticToggle("Show storage usage", "Display disk utilization.", true);
                StaticToggle("Show system uptime", "Display time since the last system start.", true);
            });
        }

        private void BuildUpdate()
        {
            Section("Application updates", "Manage how ChronoDesk checks for new versions.");
            InfoCard("Current version", "Installed version information will be displayed here.");
            StaticToggle("Check for updates automatically", "Check for a newer version when the application starts.", true);
            StaticToggle("Include pre-release versions", "Allow optional beta and preview updates.", false);
            StaticCombo("Update channel", "Choose which releases are offered.", "Stable", "Stable", "Beta");
            ActionCard("Check for updates", "Check whether a newer version is available.", "Check now", delegate { });
            Section("Update installation", "Choose how available updates are installed.");
            StaticToggle("Download updates automatically", "Download update packages when available.", false);
            StaticToggle("Ask before installing", "Request confirmation before applying an update.", true);
        }

        private void BuildAbout()
        {
            Section("ChronoDesk Widget", "Application details.");
            InfoCard("ChronoDesk Widget", "A configurable desktop workspace with lightweight widgets.");
            StaticInfo("Version", "Version information will appear here.");
            StaticInfo("Developer", "Project and maintainer information.");
            StaticInfo("License", "License information.");
            Section("Resources", "Project links and support information.");
            ActionCard("Source code", "View the project repository and source files.", "Open GitHub", delegate { });
            ActionCard("Documentation", "View setup instructions and feature documentation.", "View docs", delegate { });
            StaticInfo("Privacy", "Local preferences and widget data are stored on this device unless a feature states otherwise.");
        }

        private bool IsEnabled(string key)
        {
            return _widgets.ContainsKey(key) && _widgets[key];
        }

        private void WidgetCard(string title, string description, string key, Action options)
        {
            Panel card = NewCard(92);
            CardText(card, title, description);
            CheckBox check = new CheckBox { Checked = IsEnabled(key), AutoSize = false, Size = new Size(44, 26), Location = new Point(card.Width - 62, 17), Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = _surface, Cursor = Cursors.Hand };
            check.CheckedChanged += delegate { _setWidget(key, check.Checked); };
            card.Controls.Add(check);
            Button configure = new Button { Text = "Options  ›", Size = new Size(100, 29), Location = new Point(card.Width - 122, 52), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = _surfaceHover, ForeColor = Color.FromArgb(174, 197, 226), Font = new Font("Segoe UI", 8F), Cursor = Cursors.Hand };
            configure.FlatAppearance.BorderSize = 0;
            configure.Click += delegate { options(); ResizePageItems(); };
            card.Controls.Add(configure);
            _page.Controls.Add(card);
        }

        private void StaticInfo(string title, string description)
        {
            Panel card = NewCard(66);
            card.Controls.Add(new Label { Text = title, Location = new Point(16, 11), AutoSize = true, ForeColor = _text, Font = new Font("Segoe UI Semibold", 9F) });
            card.Controls.Add(new Label { Text = description, Location = new Point(17, 34), AutoSize = true, ForeColor = _muted, Font = new Font("Segoe UI", 8.2F) });
            _page.Controls.Add(card);
        }

        private void StaticToggle(string title, string description, bool value)
        {
            Panel card = NewCard(76); CardText(card, title, description);
            CheckBox check = new CheckBox { Checked = value, AutoSize = false, Size = new Size(44, 26), Location = new Point(card.Width - 62, 24), Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = _surface, Cursor = Cursors.Hand };
            card.Controls.Add(check); _page.Controls.Add(card);
        }

        private void StaticCombo(string title, string description, string selected, params string[] values)
        {
            Panel card = NewCard(86); CardText(card, title, description);
            ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F), BackColor = _surfaceHover, ForeColor = _text, Width = 210, Height = 30, Location = new Point(card.Width - 238, 27), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            combo.Items.AddRange(values); combo.SelectedItem = selected;
            if (combo.SelectedIndex < 0 && combo.Items.Count > 0) combo.SelectedIndex = 0;
            card.Controls.Add(combo); _page.Controls.Add(card);
        }

        private void Section(string title, string description)
        {
            Panel block = new Panel { Width = Math.Max(100, _pageHost.ClientSize.Width - 42), Height = 56, BackColor = _background, Margin = new Padding(0, 0, 0, 8) };
            block.Controls.Add(new Label { Text = title, Location = new Point(1, 1), AutoSize = true, ForeColor = _text, Font = new Font("Segoe UI Semibold", 11F) });
            block.Controls.Add(new Label { Text = description, Location = new Point(2, 27), AutoSize = true, ForeColor = _muted, Font = new Font("Segoe UI", 8.5F) });
            _page.Controls.Add(block);
        }

        private Panel NewCard(int height)
        {
            Panel card = new Panel { Width = Math.Max(100, _pageHost.ClientSize.Width - 42), Height = height, BackColor = _surface, Margin = new Padding(0, 0, 0, 9), Padding = new Padding(16) };
            card.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen p = new Pen(_border)) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            return card;
        }

        private void CardText(Panel card, string title, string description)
        {
            card.Controls.Add(new Label { Text = title, Location = new Point(16, 14), AutoSize = true, ForeColor = _text, Font = new Font("Segoe UI Semibold", 9.2F) });
            card.Controls.Add(new Label { Text = description, Location = new Point(17, 39), AutoSize = true, ForeColor = _muted, Font = new Font("Segoe UI", 8.2F) });
        }

        private void CheckCard(string title, string description, bool value, Action<bool> changed)
        {
            Panel card = NewCard(76); CardText(card, title, description);
            CheckBox check = new CheckBox { Checked = value, AutoSize = false, Size = new Size(44, 26), Location = new Point(card.Width - 62, 24), Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = _surface, Cursor = Cursors.Hand };
            check.CheckedChanged += delegate { changed(check.Checked); };
            card.Controls.Add(check); _page.Controls.Add(card);
        }

        private void ComboCard(string title, string description, string[] values, string selected, Action<string> changed)
        {
            Panel card = NewCard(86); CardText(card, title, description);
            ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F), BackColor = _surfaceHover, ForeColor = _text, Width = 210, Height = 30, Location = new Point(card.Width - 238, 27), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            combo.Items.AddRange(values); combo.SelectedItem = selected;
            combo.SelectedIndexChanged += delegate { if (combo.SelectedItem != null) changed(combo.SelectedItem.ToString()); };
            card.Controls.Add(combo); _page.Controls.Add(card);
        }

        private void ActionCard(string title, string description, string buttonText, Action action)
        {
            Panel card = NewCard(86); CardText(card, title, description);
            Button button = new Button { Text = buttonText, Size = new Size(132, 34), Location = new Point(card.Width - 158, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = _surfaceHover, ForeColor = _text, Font = new Font("Segoe UI Semibold", 8.5F), Cursor = Cursors.Hand };
            button.FlatAppearance.BorderColor = _border; button.FlatAppearance.MouseOverBackColor = _accentSoft;
            button.Click += delegate { action(); };
            card.Controls.Add(button); _page.Controls.Add(card);
        }

        private void InfoCard(string title, string description)
        {
            Panel card = NewCard(82); card.BackColor = Color.FromArgb(21, 31, 45);
            card.Paint += delegate(object sender, PaintEventArgs e)
            {
                using (Pen p = new Pen(Color.FromArgb(50, 77, 110))) e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            card.Controls.Add(new Label { Text = "i", Location = new Point(17, 14), Size = new Size(22, 22), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(145, 190, 247), BackColor = _accentSoft, Font = new Font("Segoe UI Semibold", 9F) });
            card.Controls.Add(new Label { Text = title, Location = new Point(48, 13), AutoSize = true, ForeColor = _text, Font = new Font("Segoe UI Semibold", 9F) });
            card.Controls.Add(new Label { Text = description, Location = new Point(48, 39), AutoSize = true, ForeColor = _muted, Font = new Font("Segoe UI", 8.2F) });
            _page.Controls.Add(card);
        }

        private void ResizePageItems()
        {
            if (_page == null || _page.IsDisposed) return;
            int availableWidth = _page.ClientSize.Width - _page.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 8;
            int width = Math.Max(100, Math.Min(_pageHost.ClientSize.Width - 24, availableWidth));
            foreach (Control control in _page.Controls) control.Width = width;
        }
    }
}