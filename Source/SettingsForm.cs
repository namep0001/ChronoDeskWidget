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
        private Panel _pageHost, _header;
        private FlowLayoutPanel _page;
        private Label _pageTitle, _pageDescription;
        private string _active = "General";

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

            Text = "ChronoDesk Widget | Settings";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1060, 720);
            MinimumSize = new Size(900, 620);
            BackColor = _background; ForeColor = _text;
            Font = new Font("Segoe UI", 9F);
            ShowInTaskbar = false; FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false; MinimizeBox = false;
            DoubleBuffered = true;

            Panel shell = new Panel { Dock = DockStyle.Fill, BackColor = _background };
            Controls.Add(shell);

            Panel sidebar = new Panel { Dock = DockStyle.Left, Width = 224, BackColor = _sidebar, Padding = new Padding(14, 18, 14, 14) };
            shell.Controls.Add(sidebar);
            BuildSidebar(sidebar);

            Panel main = new Panel { Dock = DockStyle.Fill, BackColor = _background, Padding = new Padding(30, 24, 30, 18) };
            shell.Controls.Add(main);

            Panel footer = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = _background };
            main.Controls.Add(footer);
            Label footerNote = new Label { Text = "Changes are saved automatically", AutoSize = true, ForeColor = _muted, Font = new Font("Segoe UI", 8.5F), Location = new Point(2, 21) };
            footer.Controls.Add(footerNote);
            Button close = new Button { Text = "Done", Size = new Size(104, 36), FlatStyle = FlatStyle.Flat, BackColor = _accent, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 9F), Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            close.FlatAppearance.BorderSize = 0;
            close.FlatAppearance.MouseOverBackColor = Color.FromArgb(105, 160, 245);
            close.Click += delegate { Close(); };
            footer.Controls.Add(close);
            footer.Resize += delegate { close.Location = new Point(footer.ClientSize.Width - close.Width, 10); };

            _header = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = _background };
            main.Controls.Add(_header);
            _pageTitle = new Label { Text = "General", AutoSize = true, Location = new Point(0, 3), ForeColor = _text, Font = new Font("Segoe UI Semibold", 21F) };
            _pageDescription = new Label { Text = "Manage startup and desktop behavior.", AutoSize = true, Location = new Point(2, 43), ForeColor = _muted, Font = new Font("Segoe UI", 9.5F) };
            _header.Controls.Add(_pageTitle); _header.Controls.Add(_pageDescription);
            Panel rule = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = _border };
            _header.Controls.Add(rule);

            _pageHost = new Panel { Dock = DockStyle.Fill, BackColor = _background, Padding = new Padding(0, 18, 4, 0) };
            main.Controls.Add(_pageHost);
            main.Controls.SetChildIndex(footer, 0);
            main.Controls.SetChildIndex(_header, 1);
            main.Controls.SetChildIndex(_pageHost, 2);
            shell.Controls.SetChildIndex(sidebar, 0);
            shell.Controls.SetChildIndex(main, 1);
            _pageHost.Resize += delegate { ResizePageItems(); };
            ShowPage("General");
        }

        private void BuildSidebar(Panel host)
        {
            FlowLayoutPanel nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = _sidebar, Padding = new Padding(0, 7, 0, 5) };
            host.Controls.Add(nav);
            Label group = new Label { Text = "PREFERENCES", Width = 194, Height = 28, TextAlign = ContentAlignment.BottomLeft, ForeColor = Color.FromArgb(112, 130, 153), Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), Padding = new Padding(7, 0, 0, 5), Margin = new Padding(0, 0, 0, 3) };
            nav.Controls.Add(group);
            AddNav(nav, "General", "⌂");
            AddNav(nav, "Appearance", "◐");
            AddNav(nav, "Language", "文");
            AddNav(nav, "Widgets", "▦");
            Label widgetGroup = new Label { Text = "WIDGET SETTINGS", Width = 194, Height = 34, TextAlign = ContentAlignment.BottomLeft, ForeColor = Color.FromArgb(112, 130, 153), Font = new Font("Segoe UI", 7.5F, FontStyle.Bold), Padding = new Padding(7, 0, 0, 6), Margin = new Padding(0, 5, 0, 3) };
            nav.Controls.Add(widgetGroup);
            AddNav(nav, "Clock", "◷", true);
            AddNav(nav, "Calendar", "▦", true);
            AddNav(nav, "Pomodoro", "◴", true);
            AddNav(nav, "Tasks", "☷", true);
            AddNav(nav, "Quick Notes", "▤", true);
            AddNav(nav, "Market Rates", "↗", true);
            AddNav(nav, "Network Monitor", "⌁", true);
            AddNav(nav, "Calculator", "＋", true);
            AddNav(nav, "System Monitor", "▥", true);

            Panel brand = new Panel { Dock = DockStyle.Top, Height = 86, BackColor = _sidebar };
            Panel mark = new Panel { Location = new Point(2, 3), Size = new Size(42, 42), BackColor = _accentSoft };
            mark.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(137, 184, 255), 2.2F))
                {
                    e.Graphics.DrawEllipse(p, 13, 13, 16, 16);
                    e.Graphics.DrawLine(p, 21, 4, 21, 13); e.Graphics.DrawLine(p, 21, 29, 21, 38);
                    e.Graphics.DrawLine(p, 4, 21, 13, 21); e.Graphics.DrawLine(p, 29, 21, 38, 21);
                }
            };
            brand.Controls.Add(mark);
            brand.Controls.Add(new Label { Text = "ChronoDesk", Location = new Point(54, 5), AutoSize = true, ForeColor = _text, Font = new Font("Segoe UI Semibold", 11F) });
            brand.Controls.Add(new Label { Text = "WIDGET SETTINGS", Location = new Point(55, 28), AutoSize = true, ForeColor = _muted, Font = new Font("Segoe UI", 7.5F, FontStyle.Bold) });
            host.Controls.Add(brand);

            Panel bottom = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = _sidebar };
            bottom.Paint += delegate(object sender, PaintEventArgs e) { using (Pen p = new Pen(_border)) e.Graphics.DrawLine(p, 0, 0, bottom.Width, 0); };
            bottom.Controls.Add(new Label { Text = "●  APPLICATION", Location = new Point(4, 11), AutoSize = true, ForeColor = Color.FromArgb(105, 190, 145), Font = new Font("Segoe UI", 7.5F, FontStyle.Bold) });
            bottom.Controls.Add(new Label { Text = "ChronoDesk Widget", Location = new Point(4, 28), AutoSize = true, ForeColor = _muted, Font = new Font("Segoe UI", 8F) });
            host.Controls.Add(bottom);
        }

        private void AddNav(FlowLayoutPanel parent, string key, string glyph, bool child = false)
        {
            Button b = new Button {
                Text = glyph + "     " + key, TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(194, child ? 34 : 39), Margin = new Padding(0, 1, 0, 3),
                FlatStyle = FlatStyle.Flat, BackColor = _sidebar,
                ForeColor = child ? Color.FromArgb(166, 180, 199) : _text,
                Font = new Font("Segoe UI", child ? 8.5F : 9F, child ? FontStyle.Regular : FontStyle.Bold),
                Cursor = Cursors.Hand, Padding = new Padding(child ? 13 : 8, 0, 0, 0)
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
                item.Value.ForeColor = selected ? Color.FromArgb(164, 202, 255) : (IsWidgetPage(item.Key) ? Color.FromArgb(166, 180, 199) : _text);
                item.Value.FlatAppearance.BorderSize = selected ? 1 : 0;
                item.Value.FlatAppearance.BorderColor = _accent;
            }

            _pageHost.Controls.Clear();
            _page = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = _background, Padding = new Padding(0, 0, 12, 16) };
            _pageHost.Controls.Add(_page);
            _pageTitle.Text = key;
            _pageDescription.Text = Subtitle(key);

            if (key == "General") BuildGeneral();
            else if (key == "Appearance") BuildAppearance();
            else if (key == "Language") BuildLanguage();
            else if (key == "Widgets") BuildWidgets();
            else if (key == "Clock") BuildClock();
            else if (key == "Market Rates") BuildRates();
            else
            {
                string widgetKey = WidgetKey(key);
                Section(key, WidgetDescription(key));
                CheckCard("Show " + key, "Display this panel on your desktop.", IsEnabled(widgetKey), delegate(bool value) { _setWidget(widgetKey, value); });
                if (key == "Calendar") InfoCard("Calendar formats", "The desktop calendar supports Gregorian and Solar Hijri dates.");
                else if (key == "Pomodoro") InfoCard("Focus timer", "Use the timer controls on the widget to start, pause, and reset a session.");
                else if (key == "Tasks") InfoCard("Task list", "Tasks are stored locally on this device.");
                else if (key == "Quick Notes") InfoCard("Local notes", "Your notes are saved on this device and can be exported as a text file.");
                else if (key == "Network Monitor") InfoCard("Network activity", "Usage totals are collected while ChronoDesk is running.");
                else if (key == "Calculator") InfoCard("Calculator", "Use the calculator panel for quick arithmetic.");
                else if (key == "System Monitor") InfoCard("System metrics", "CPU, memory, GPU, storage, and uptime are shown when available.");
            }
        }

        private bool IsWidgetPage(string key) { return key != "General" && key != "Appearance" && key != "Language" && key != "Widgets"; }
        private bool IsEnabled(string key) { return _widgets.ContainsKey(key) && _widgets[key]; }
        private string WidgetKey(string name)
        {
            switch (name)
            {
                case "Quick Notes": return "notes";
                case "Market Rates": return "rates";
                case "Network Monitor": return "network";
                case "System Monitor": return "system";
                default: return name.ToLowerInvariant();
            }
        }
        private string WidgetDescription(string key)
        {
            switch (key)
            {
                case "Clock": return "Control the clock panel and its time display.";
                case "Calendar": return "Control the calendar panel on your desktop.";
                case "Pomodoro": return "Control the focus timer panel.";
                case "Tasks": return "Control your local task list panel.";
                case "Quick Notes": return "Control your local notes panel.";
                case "Market Rates": return "Control live market quotes and their display unit.";
                case "Network Monitor": return "Control live network statistics.";
                case "Calculator": return "Control the built-in calculator panel.";
                default: return "Control system usage statistics.";
            }
        }
        private string Subtitle(string key)
        {
            switch (key)
            {
                case "General": return "Manage startup and desktop behavior.";
                case "Appearance": return "Set the visual style, transparency, and typeface.";
                case "Language": return "Choose the language used throughout the widget.";
                case "Widgets": return "Choose which panels are visible on your desktop.";
                case "Clock": return "Configure the clock display.";
                case "Calendar": return "Calendar visibility and supported date systems.";
                case "Pomodoro": return "Focus timer visibility and usage.";
                case "Tasks": return "Task panel visibility and local storage.";
                case "Quick Notes": return "Notes panel visibility and local storage.";
                case "Market Rates": return "Market quote visibility and display currency.";
                case "Network Monitor": return "Network panel visibility and usage information.";
                case "Calculator": return "Calculator panel visibility.";
                default: return "System monitoring panel visibility.";
            }
        }

        private void BuildGeneral()
        {
            Section("Startup & desktop", "Control how ChronoDesk behaves when you sign in.");
            CheckCard("Start with Windows", "Launch ChronoDesk automatically when you sign in to Windows.", _startup, delegate(bool v) { _setStartup(v); });
            CheckCard("Keep on top", "Keep the desktop widget above other application windows.", _topmost, delegate(bool v) { _setTopmost(v); });
            Section("Application", "Your preferences are stored locally on this device.");
            InfoCard("Local settings", "Changes are saved as soon as you make them. Your tasks and notes remain on this device.");
        }

        private void BuildAppearance()
        {
            Section("Color theme", "Choose how the desktop widget looks.");
            ComboCard("Theme", "Follow Windows appearance or select a fixed theme.", new[] { "System", "Light", "Dark" }, _theme == "light" ? "Light" : _theme == "dark" ? "Dark" : "System",
                delegate(string v) { _setTheme(v.ToLowerInvariant()); });
            Section("Transparency & typography", "Adjust the desktop widget's readability.");
            ComboCard("Widget opacity", "Set the transparency level of the desktop widget.", new[] { "100%", "90%", "80%", "70%", "60%", "50%" }, _opacity + "%",
                delegate(string v) { _setOpacity(int.Parse(v.TrimEnd('%'))); });
            ComboCard("Font family", "Choose the typeface used by widget content.", new[] { "Segoe UI", "Arial", "Calibri", "Tahoma", "Verdana", "Microsoft YaHei UI" }, _font,
                delegate(string v) { _setFont(v); });
            InfoCard("System theme", "The System option follows your Windows app theme and accent color.");
        }

        private void BuildLanguage()
        {
            Section("Interface language", "Select the language used by ChronoDesk.");
            ComboCard("Application language", "Switch between the currently supported languages.", new[] { "English", "فارسی" }, _language == "fa" ? "فارسی" : "English",
                delegate(string v) { _setLanguage(v == "فارسی" ? "fa" : "en"); });
            InfoCard("Supported languages", "English and Persian are currently available.");
        }

        private void BuildWidgets()
        {
            Section("Desktop panels", "Turn panels on or off. Open a panel's section for its available options.");
            string[] keys = { "clock", "calendar", "tasks", "pomodoro", "notes", "rates", "network", "calculator", "system" };
            string[] names = { "Clock", "Calendar", "Tasks", "Pomodoro", "Quick Notes", "Market Rates", "Network Monitor", "Calculator", "System Monitor" };
            string[] descriptions = { "Time and date", "Gregorian and Solar Hijri calendar", "Local task list", "Focus and break timer", "Local text notes", "USD, EUR, and gold quotes", "Live network usage", "Basic arithmetic", "Hardware usage statistics" };
            for (int i = 0; i < keys.Length; i++)
            {
                string k = keys[i];
                CheckCard(names[i], descriptions[i], IsEnabled(k), delegate(bool v) { _setWidget(k, v); });
            }
        }

        private void BuildClock()
        {
            Section("Clock display", "Choose the time format shown in the desktop clock.");
            CheckCard("Show clock", "Show or hide the clock and date panel.", IsEnabled("clock"), delegate(bool v) { _setWidget("clock", v); });
            ComboCard("Time format", "Choose between a 24-hour and 12-hour clock.", new[] { "24-hour  ·  14:30", "12-hour  ·  02:30 PM" },
                _clockFormat == "12h" ? "12-hour  ·  02:30 PM" : "24-hour  ·  14:30",
                delegate(string v) { _setClockFormat(v.StartsWith("12-hour", StringComparison.Ordinal) ? "12h" : "24h"); });
        }

        private void BuildRates()
        {
            Section("Market data", "Configure the live quote panel.");
            CheckCard("Show Market Rates", "Show or hide the market rates panel.", IsEnabled("rates"), delegate(bool v) { _setWidget("rates", v); });
            ComboCard("Display currency", "Choose the unit used for market values.", new[] { "Toman (T)", "Dollar ($)" }, _unit == "$" ? "Dollar ($)" : "Toman (T)",
                delegate(string v) { _setRateUnit(v.StartsWith("Dollar", StringComparison.Ordinal) ? "$" : "T"); });
            ActionCard("Refresh market data", "Request the latest available market quotes.", "Refresh now", delegate { _refreshRates(); });
        }

        private void ResizePageItems()
        {
            if (_page == null || _page.IsDisposed) return;
            int width = Math.Max(500, _pageHost.ClientSize.Width - 42);
            foreach (Control control in _page.Controls) control.Width = width;
        }

        private void Section(string title, string description)
        {
            Panel block = new Panel { Width = Math.Max(500, _pageHost.ClientSize.Width - 42), Height = 56, BackColor = _background, Margin = new Padding(0, 0, 0, 8) };
            block.Controls.Add(new Label { Text = title, Location = new Point(1, 1), AutoSize = true, ForeColor = _text, Font = new Font("Segoe UI Semibold", 11F) });
            block.Controls.Add(new Label { Text = description, Location = new Point(2, 27), AutoSize = true, ForeColor = _muted, Font = new Font("Segoe UI", 8.5F) });
            _page.Controls.Add(block);
        }

        private Panel NewCard(int height)
        {
            Panel card = new Panel { Width = Math.Max(500, _pageHost.ClientSize.Width - 42), Height = height, BackColor = _surface, Margin = new Padding(0, 0, 0, 9), Padding = new Padding(16) };
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
            card.Controls.Add(check);
            _page.Controls.Add(card);
        }

        private void ComboCard(string title, string description, string[] values, string selected, Action<string> changed)
        {
            Panel card = NewCard(86); CardText(card, title, description);
            ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5F), BackColor = _surfaceHover, ForeColor = _text, Width = 210, Height = 30, Location = new Point(card.Width - 238, 27), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            combo.Items.AddRange(values); combo.SelectedItem = selected;
            combo.SelectedIndexChanged += delegate { if (combo.SelectedItem != null) changed(combo.SelectedItem.ToString()); };
            card.Controls.Add(combo);
            _page.Controls.Add(card);
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
    }
}
