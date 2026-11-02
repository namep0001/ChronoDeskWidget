using System;
using System.Collections.Generic;
using System.Drawing;
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

        public SettingsForm(string language, string theme, int opacity, string font, string rateUnit,
            bool topmost, bool startup, IDictionary<string, bool> widgets,
            Action<string, bool> setWidget, Action<int> setOpacity, Action<string> setTheme,
            Action<string> setFont, Action<string> setRateUnit, Action<string> setLanguage,
            Action<bool> setTopmost, Action<bool> setStartup, Action refreshRates,
            string clockFormat, Action<string> setClockFormat)
        {
            _setWidget=setWidget; _setOpacity=setOpacity; _setTheme=setTheme; _setFont=setFont;
            _setRateUnit=setRateUnit; _setLanguage=setLanguage; _setTopmost=setTopmost;
            _setStartup=setStartup; _refreshRates=refreshRates; _setClockFormat=setClockFormat;
            _widgets=new Dictionary<string,bool>(widgets,StringComparer.OrdinalIgnoreCase);
            Text="ChronoDesk Widget — Settings";
            StartPosition=FormStartPosition.CenterParent; Size=new Size(760,540);
            MinimumSize=new Size(680,480); BackColor=Color.FromArgb(245,247,250);
            Font=new Font("Segoe UI",9F); ShowInTaskbar=false;
            TabControl tabs=new TabControl { Dock=DockStyle.Fill, Padding=new Point(14,7), Font=new Font("Segoe UI",9F) };
            Controls.Add(tabs);
            AddTab(tabs,"⚙  General",BuildGeneral(topmost,startup));
            AddTab(tabs,"▦  Widgets",BuildWidgets());
            AddTab(tabs,"◐  Appearance",BuildAppearance(theme,opacity,font));
            AddTab(tabs,"◈  Market Rates",BuildRates(rateUnit));
            AddTab(tabs,"文  Language",BuildLanguage(language));
            AddTab(tabs,"◷  Clock",BuildClock(clockFormat));
            AddPlaceholder(tabs,"▣  Calendar","Choose two calendar systems to display.\r\nPlanned for a future update.");
            AddPlaceholder(tabs,"◴  Pomodoro","Custom focus and break durations.\r\nPlanned for a future update.");
            AddPlaceholder(tabs,"⠿  Layout","Move existing panels and create custom panels.\r\nPlanned for a future update.");
            AddPlaceholder(tabs,"⌁  Performance","Control optional internet and background activity.\r\nPlanned for a future update.");
            AddPlaceholder(tabs,"▤  Dashboard","Choose applications and websites for quick access.\r\nPlanned for a future update.");
            AddPlaceholder(tabs,"◫  Storage","Disk space analysis.\r\nPlanned for a future update.");
            AddPlaceholder(tabs,"◴  Timer","Chronometer and countdown timer options.\r\nPlanned for a future update.");
        }

        private void AddTab(TabControl tabs,string title,Control content)
        {
            TabPage page=new TabPage(title) { BackColor=Color.FromArgb(245,247,250), Padding=new Padding(18) };
            content.Dock=DockStyle.Fill; page.Controls.Add(content); tabs.TabPages.Add(page);
        }
        private Panel Page()
        {
            return new Panel { Dock=DockStyle.Fill, AutoScroll=true, Padding=new Padding(8), BackColor=Color.FromArgb(245,247,250) };
        }
        private void AddRow(Control parent,string title,Control editor,string description=null)
        {
            Panel row=new Panel { Dock=DockStyle.Top, Height=description==null?58:78, BackColor=Color.White, Padding=new Padding(14), Margin=new Padding(0,0,0,9) };
            Label heading=new Label { Text=title, AutoSize=true, Location=new Point(14,description==null?19:12), Font=new Font("Segoe UI",9.5F,FontStyle.Bold), ForeColor=Color.FromArgb(37,47,62) };
            row.Controls.Add(heading);
            if(description!=null) row.Controls.Add(new Label { Text=description, AutoSize=true, Location=new Point(14,39), ForeColor=Color.FromArgb(112,122,137) });
            editor.Anchor=AnchorStyles.Top|AnchorStyles.Right; editor.Location=new Point(390,description==null?14:24); editor.Width=250;
            row.Controls.Add(editor); parent.Controls.Add(row); row.BringToFront();
        }
        private Control BuildGeneral(bool topmost,bool startup)
        {
            Panel p=Page();
            CheckBox top=new CheckBox { Text="Keep widget above other windows",Checked=topmost,AutoSize=true };
            top.CheckedChanged+=(s,e)=>_setTopmost(top.Checked); AddRow(p,"Window behavior",top);
            CheckBox run=new CheckBox { Text="Start with Windows",Checked=startup,AutoSize=true };
            run.CheckedChanged+=(s,e)=>_setStartup(run.Checked); AddRow(p,"Startup",run,"Launch ChronoDesk Widget when you sign in to Windows.");
            return p;
        }
        private Control BuildWidgets()
        {
            Panel p=Page();
            string[] keys={"clock","calendar","tasks","pomodoro","notes","rates","network","calculator","system"};
            string[] names={"Clock and dates","Calendar","Tasks","Pomodoro timer","Quick note","Market rates","Network monitor","Calculator","System monitor"};
            for(int i=0;i<keys.Length;i++){ string key=keys[i]; CheckBox cb=new CheckBox { Text="Show this panel",Checked=_widgets.ContainsKey(key)&&_widgets[key],AutoSize=true,Tag=key }; cb.CheckedChanged+=(s,e)=>_setWidget(key,cb.Checked); AddRow(p,names[i],cb); }
            return p;
        }
        private Control BuildAppearance(string theme,int opacity,string font)
        {
            Panel p=Page();
            ComboBox themes=Combo(new[]{"System","Light","Dark"},theme=="light"?"Light":theme=="dark"?"Dark":"System");
            themes.SelectedIndexChanged+=(s,e)=>_setTheme(themes.SelectedItem.ToString().ToLowerInvariant()); AddRow(p,"Theme",themes,"Choose the appearance used by the widget.");
            ComboBox op=Combo(new[]{"100%","90%","80%","70%","60%","50%"},opacity+"%");
            op.SelectedIndexChanged+=(s,e)=>_setOpacity(int.Parse(op.SelectedItem.ToString().TrimEnd('%'))); AddRow(p,"Widget opacity",op);
            ComboBox fonts=Combo(new[]{"Segoe UI","Arial","Calibri","Tahoma","Verdana","Microsoft YaHei UI"},font);
            fonts.SelectedIndexChanged+=(s,e)=>_setFont(fonts.SelectedItem.ToString()); AddRow(p,"Font family",fonts);
            return p;
        }
        private Control BuildRates(string unit)
        {
            Panel p=Page();
            ComboBox units=Combo(new[]{"Toman (T)","Dollar ($)"},unit=="$"?"Dollar ($)":"Toman (T)");
            units.SelectedIndexChanged+=(s,e)=>_setRateUnit(units.SelectedIndex==1?"$":"T"); AddRow(p,"Display currency",units);
            Button refresh=new Button { Text="Refresh now",Width=120,Height=30 };
            refresh.Click+=(s,e)=>_refreshRates(); AddRow(p,"Market data",refresh,"Refresh the currently enabled market rates panel.");
            return p;
        }
        private Control BuildLanguage(string language)
        {
            Panel p=Page();
            ComboBox langs=Combo(new[]{"English","فارسی"},language=="fa"?"فارسی":"English");
            langs.SelectedIndexChanged+=(s,e)=>_setLanguage(langs.SelectedIndex==1?"fa":"en"); AddRow(p,"Application language",langs);
            return p;
        }
        private Control BuildClock(string format)
        {
            Panel p=Page();
            ComboBox formats=Combo(new[]{"24-hour (14:30)","12-hour (02:30 PM)"},format=="12h"?"12-hour (02:30 PM)":"24-hour (14:30)");
            formats.SelectedIndexChanged+=(s,e)=>_setClockFormat(formats.SelectedIndex==1?"12h":"24h");
            AddRow(p,"Time format",formats,"Choose how the clock displays the time.");
            return p;
        }
        private ComboBox Combo(string[] items,string selected)
        {
            ComboBox c=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList };
            c.Items.AddRange(items); c.SelectedItem=selected; return c;
        }
        private void AddPlaceholder(TabControl tabs,string title,string message)
        {
            Panel p=Page();
            Label label=new Label { Text=message,AutoSize=true,Font=new Font("Segoe UI",11F),ForeColor=Color.FromArgb(105,116,132),Location=new Point(20,24) };
            p.Controls.Add(label); AddTab(tabs,title,p);
        }
    }
}
