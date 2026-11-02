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
        private readonly Panel _content, _sidebar;
        private readonly Label _title, _subtitle;
        private readonly Dictionary<string, Button> _nav = new Dictionary<string, Button>();
        private readonly Color bg = Color.FromArgb(17, 22, 39);
        private readonly Color sidebarColor = Color.FromArgb(13, 18, 33);
        private readonly Color cardColor = Color.FromArgb(27, 35, 57);
        private readonly Color cardAlt = Color.FromArgb(32, 42, 68);
        private readonly Color lineColor = Color.FromArgb(49, 62, 94);
        private readonly Color fg = Color.FromArgb(240, 243, 255);
        private readonly Color muted = Color.FromArgb(164, 177, 207);
        private readonly Color accent = Color.FromArgb(111, 79, 255);
        private readonly Color cyan = Color.FromArgb(49, 163, 255);
        private readonly string _language, _theme, _font, _unit, _clockFormat;
        private readonly int _opacity;
        private readonly bool _topmost, _startup;
        private string _active = "General";

        public SettingsForm(string language, string theme, int opacity, string font, string rateUnit,
            bool topmost, bool startup, IDictionary<string, bool> widgets,
            Action<string, bool> setWidget, Action<int> setOpacity, Action<string> setTheme,
            Action<string> setFont, Action<string> setRateUnit, Action<string> setLanguage,
            Action<bool> setTopmost, Action<bool> setStartup, Action refreshRates,
            string clockFormat, Action<string> setClockFormat)
        {
            _language=language; _theme=theme; _opacity=opacity; _font=font; _unit=rateUnit;
            _topmost=topmost; _startup=startup; _clockFormat=clockFormat;
            _setWidget=setWidget; _setOpacity=setOpacity; _setTheme=setTheme; _setFont=setFont;
            _setRateUnit=setRateUnit; _setLanguage=setLanguage; _setTopmost=setTopmost;
            _setStartup=setStartup; _refreshRates=refreshRates; _setClockFormat=setClockFormat;
            _widgets=new Dictionary<string,bool>(widgets,StringComparer.OrdinalIgnoreCase);

            Text="ChronoDesk Widget — Settings";
            StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1160,780); MinimumSize=new Size(940,640);
            BackColor=bg; ForeColor=fg; Font=new Font("Segoe UI",9F);
            ShowInTaskbar=false; FormBorderStyle=FormBorderStyle.Sizable; MaximizeBox=false;

            Panel root=new Panel { Dock=DockStyle.Fill, BackColor=bg, Padding=new Padding(1) };
            Controls.Add(root);

            _sidebar=new Panel { Dock=DockStyle.Left, Width=258, BackColor=sidebarColor, Padding=new Padding(14,15,12,12) };
            root.Controls.Add(_sidebar);
            BuildSidebar();

            Panel right=new Panel { Dock=DockStyle.Fill, BackColor=bg, Padding=new Padding(22,18,20,12) };
            root.Controls.Add(right);

            Panel header=new Panel { Dock=DockStyle.Top, Height=112, BackColor=bg };
            right.Controls.Add(header);
            Panel iconTile=new Panel { Location=new Point(2,5), Size=new Size(66,66), BackColor=Color.FromArgb(35,43,78) };
            iconTile.Paint+=delegate(object s,PaintEventArgs e)
            {
                e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(LinearGradientBrush b=new LinearGradientBrush(iconTile.ClientRectangle,Color.FromArgb(41,75,132),Color.FromArgb(72,44,150),45F)) e.Graphics.FillRectangle(b,iconTile.ClientRectangle);
                using(Pen pen=new Pen(Color.FromArgb(137,177,255),2.5F))
                {
                    e.Graphics.DrawEllipse(pen,22,20,23,23);
                    e.Graphics.DrawLine(pen,33,8,33,20); e.Graphics.DrawLine(pen,33,43,33,55);
                    e.Graphics.DrawLine(pen,9,31,22,31); e.Graphics.DrawLine(pen,45,31,57,31);
                }
            };
            header.Controls.Add(iconTile);
            _title=new Label { Text="General",Location=new Point(84,6),AutoSize=true,Font=new Font("Segoe UI Semibold",21F),ForeColor=fg };
            _subtitle=new Label { Text="Configure the way ChronoDesk Widget works.",Location=new Point(86,44),AutoSize=true,Font=new Font("Segoe UI",10F),ForeColor=muted };
            header.Controls.Add(_title); header.Controls.Add(_subtitle);
            Panel accentLine=new Panel { Dock=DockStyle.Bottom,Height=2 };
            accentLine.Paint+=delegate(object s,PaintEventArgs e){using(LinearGradientBrush b=new LinearGradientBrush(accentLine.ClientRectangle,accent,cyan,0F))e.Graphics.FillRectangle(b,accentLine.ClientRectangle);};
            header.Controls.Add(accentLine);

            Panel footer=new Panel { Dock=DockStyle.Bottom,Height=52,BackColor=bg };
            right.Controls.Add(footer);
            Label hint=new Label { Text="Changes apply immediately",Location=new Point(3,17),AutoSize=true,ForeColor=muted,Font=new Font("Segoe UI",8.5F) };
            footer.Controls.Add(hint);
            Button done=new Button { Text="Done   ✓",Width=122,Height=36,Anchor=AnchorStyles.Top|AnchorStyles.Right,Location=new Point(footer.Width-126,7),FlatStyle=FlatStyle.Flat,BackColor=accent,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",9F),Cursor=Cursors.Hand };
            done.FlatAppearance.BorderSize=0; done.Click+=delegate{Close();}; footer.Controls.Add(done);
            footer.Resize+=delegate{done.Left=footer.ClientSize.Width-done.Width-2;};

            _content=new Panel { Dock=DockStyle.Fill,BackColor=bg,Padding=new Padding(0,14,3,6),AutoScroll=true };
            right.Controls.Add(_content);
            ShowPage("General");
        }

        private void BuildSidebar()
        {
            Panel brand=new Panel { Dock=DockStyle.Top,Height=90,BackColor=sidebarColor };
            _sidebar.Controls.Add(brand);
            Panel logo=new Panel { Location=new Point(3,7),Size=new Size(49,49),BackColor=Color.FromArgb(35,43,77) };
            logo.Paint+=delegate(object s,PaintEventArgs e)
            {
                e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(Pen pen=new Pen(Color.FromArgb(116,151,255),2.5F))
                {
                    e.Graphics.DrawEllipse(pen,14,14,21,21); e.Graphics.DrawLine(pen,24,4,24,14);
                    e.Graphics.DrawLine(pen,24,35,24,45); e.Graphics.DrawLine(pen,4,24,14,24); e.Graphics.DrawLine(pen,35,24,45,24);
                }
            };
            brand.Controls.Add(logo);
            brand.Controls.Add(new Label { Text="ChronoDesk Widget",Location=new Point(63,9),AutoSize=true,Font=new Font("Segoe UI Semibold",11F),ForeColor=fg });
            brand.Controls.Add(new Label { Text="SETTINGS",Location=new Point(64,36),AutoSize=true,Font=new Font("Segoe UI",8F,FontStyle.Bold),ForeColor=muted });

            FlowLayoutPanel nav=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=sidebarColor,Padding=new Padding(0,4,0,4) };
            _sidebar.Controls.Add(nav);
            AddNav(nav,"General","⌂","",false);
            AddNav(nav,"Language","◎","",false);
            AddNav(nav,"Appearance","◉","",false);
            AddNav(nav,"Widgets","⬡","",false);
            AddNav(nav,"Clock","◷","",true);
            AddNav(nav,"Calendar","▦","",true);
            AddNav(nav,"Pomodoro","◴","",true);
            AddNav(nav,"Tasks","☷","",true);
            AddNav(nav,"Quick Notes","▤","",true);
            AddNav(nav,"Market Rates","↗","",true);
            AddNav(nav,"Network Monitor","⌁","",true);
            AddNav(nav,"Calculator","＋","",true);
            AddNav(nav,"System Monitor","▥","",true);
            AddNav(nav,"Custom Panels","＋","planned",true);
            AddNav(nav,"Performance","ϟ","planned",false);
            AddNav(nav,"Dashboard","▦","planned",false);
            AddNav(nav,"Storage","▰","planned",false);

            Panel build=new Panel { Dock=DockStyle.Bottom,Height=57,BackColor=Color.FromArgb(21,28,47),Padding=new Padding(10) };
            build.Paint+=delegate(object s,PaintEventArgs e){using(Pen pen=new Pen(lineColor))e.Graphics.DrawRectangle(pen,0,0,build.Width-1,build.Height-1);};
            build.Controls.Add(new Label { Text="●  ChronoDesk Widget",Location=new Point(10,6),AutoSize=true,ForeColor=muted,Font=new Font("Segoe UI",8.5F) });
            build.Controls.Add(new Label { Text="Build 21116  ·  Settings",Location=new Point(10,29),AutoSize=true,ForeColor=muted,Font=new Font("Segoe UI",8F) });
            _sidebar.Controls.Add(build);
        }

        private void AddNav(FlowLayoutPanel parent,string key,string glyph,string status,bool sub)
        {
            Panel row=new Panel { Width=224,Height=sub?35:48,Margin=new Padding(0,0,0,3),BackColor=sidebarColor };
            Button b=new Button {
                Text=(sub?"      •   ":"  ")+glyph+"   "+key+(status=="planned"?"   ·":""),
                TextAlign=ContentAlignment.MiddleLeft,Dock=DockStyle.Fill,FlatStyle=FlatStyle.Flat,
                BackColor=sidebarColor,ForeColor=status=="planned"?Color.FromArgb(130,143,174):(sub?muted:fg),
                Font=new Font("Segoe UI",sub?8.8F:9.4F,(key=="Widgets"||!sub)?FontStyle.Regular:FontStyle.Regular),
                Cursor=Cursors.Hand,Padding=new Padding(2,0,0,0)
            };
            b.FlatAppearance.BorderSize=0;
            b.FlatAppearance.MouseOverBackColor=Color.FromArgb(28,38,62);
            b.Click+=delegate{ShowPage(key);};
            row.Controls.Add(b); parent.Controls.Add(row); _nav[key]=b;
        }

        private void ShowPage(string key)
        {
            _active=key;
            foreach(KeyValuePair<string,Button> item in _nav)
            {
                bool selected=item.Key==key || (key=="Widgets"&&item.Key=="Widgets");
                item.Value.BackColor=selected?Color.FromArgb(45,48,100):sidebarColor;
                item.Value.ForeColor=selected?Color.FromArgb(204,199,255):(item.Key=="Custom Panels"||item.Key=="Performance"||item.Key=="Dashboard"||item.Key=="Storage"?Color.FromArgb(130,143,174):(IsWidgetPage(item.Key)?muted:fg));
            }
            _content.Controls.Clear();
            _title.Text=key=="Quick Notes"?"Quick Notes":key;
            _subtitle.Text=Subtitle(key);
            Panel p=new Panel { Dock=DockStyle.Fill,AutoScroll=true,BackColor=bg,Padding=new Padding(0,0,5,8) };
            _content.Controls.Add(p);
            if(key=="General") BuildGeneral(p);
            else if(key=="Language") BuildLanguage(p);
            else if(key=="Appearance") BuildAppearance(p);
            else if(key=="Widgets") BuildWidgets(p);
            else if(key=="Clock") BuildClock(p);
            else if(key=="Market Rates") BuildRates(p);
            else if(key=="Calendar") BuildWidget(p,"calendar","Calendar","Existing Gregorian and Solar Hijri calendar panel.","Choose the calendar systems to display.");
            else if(key=="Pomodoro") BuildWidget(p,"pomodoro","Pomodoro","Existing focus and break timer panel.","Set custom focus, short-break, and long-break durations.");
            else if(key=="Tasks") BuildWidget(p,"tasks","Tasks","Existing task list panel.",null);
            else if(key=="Quick Notes") BuildWidget(p,"notes","Quick Notes","Existing quick notes panel.",null);
            else if(key=="Network Monitor") BuildWidget(p,"network","Network Monitor","Existing network usage panel.","Control polling and background activity.");
            else if(key=="Calculator") BuildWidget(p,"calculator","Calculator","Existing calculator panel.",null);
            else if(key=="System Monitor") BuildWidget(p,"system","System Monitor","Existing system statistics panel.",null);
            else if(key=="Custom Panels") Planned(p,"Custom Panels","Move panels freely and create custom widget panels.");
            else if(key=="Performance") Planned(p,"Performance","Control optional internet activity, polling, and resource usage.");
            else if(key=="Dashboard") Planned(p,"Dashboard","Create one-click shortcuts for applications and websites.");
            else if(key=="Storage") Planned(p,"Storage","Explore disk usage with a visual storage analyzer.");
        }

        private bool IsWidgetPage(string key)
        {
            return key=="Clock"||key=="Calendar"||key=="Pomodoro"||key=="Tasks"||key=="Quick Notes"||key=="Market Rates"||key=="Network Monitor"||key=="Calculator"||key=="System Monitor"||key=="Custom Panels";
        }
        private string Subtitle(string key)
        {
            switch(key)
            {
                case "General":return "Configure startup and basic application behavior.";
                case "Language":return "Choose the interface language and reading direction.";
                case "Appearance":return "Personalize colors, transparency, and typography.";
                case "Widgets":return "Manage the desktop panels available in ChronoDesk Widget.";
                case "Clock":return "Time display and clock preferences.";
                case "Calendar":return "Calendar display and calendar-system preferences.";
                case "Pomodoro":return "Configure your focus and break timer.";
                case "Market Rates":return "Manage the market data panel and displayed rates.";
                case "Performance":return "Manage background activity and resource usage.";
                case "Dashboard":return "Keep favorite applications and websites one click away.";
                case "Storage":return "Inspect disk usage and discover large folders.";
                default:return "Configure the "+key.ToLowerInvariant()+" panel.";
            }
        }

        private void BuildGeneral(Panel p)
        {
            Section(p,"Startup & Behavior","How the widget behaves on your desktop");
            CheckCard(p,"Start with Windows","Launch ChronoDesk Widget automatically when you sign in.",_startup,delegate(bool v){_setStartup(v);});
            CheckCard(p,"Keep on Top","Keep the widget above other windows.",_topmost,delegate(bool v){_setTopmost(v);});
            Section(p,"Settings","Changes are applied immediately");
            InfoCard(p,"You're in control","Only existing options are active. Future settings are shown as design placeholders.");
        }
        private void BuildLanguage(Panel p)
        {
            Section(p,"Language & Direction","Interface localization");
            ComboBox c=Combo(new[]{"English","فارسی"},_language=="fa"?"فارسی":"English");
            c.SelectedIndexChanged+=delegate{_setLanguage(c.SelectedIndex==1?"fa":"en");};
            ControlCard(p,"Application Language","Choose a currently supported language.",c);
            InfoCard(p,"More languages planned","Additional popular languages and automatic right-to-left layout support will be added later.");
        }
        private void BuildAppearance(Panel p)
        {
            Section(p,"Theme","Choose a visual style");
            ComboBox themes=Combo(new[]{"System","Light","Dark"},_theme=="light"?"Light":_theme=="dark"?"Dark":"System");
            themes.SelectedIndexChanged+=delegate{_setTheme(themes.SelectedItem.ToString().ToLowerInvariant());};
            ControlCard(p,"Color Theme","Follow Windows or choose Light or Dark.",themes);
            ComboBox op=Combo(new[]{"100%","90%","80%","70%","60%","50%"},_opacity+"%");
            op.SelectedIndexChanged+=delegate{_setOpacity(int.Parse(op.SelectedItem.ToString().TrimEnd('%')));};
            ControlCard(p,"Widget Opacity","Adjust desktop widget transparency.",op);
            ComboBox fonts=Combo(new[]{"Segoe UI","Arial","Calibri","Tahoma","Verdana","Microsoft YaHei UI"},_font);
            fonts.SelectedIndexChanged+=delegate{_setFont(fonts.SelectedItem.ToString());};
            ControlCard(p,"Font Family","Choose the typeface used by the widget.",fonts);
            Section(p,"Coming later","More visual customization");
            PlannedCard(p,"Fine-grained accent colors, improved light theme, and additional visual controls.");
        }
        private void BuildWidgets(Panel p)
        {
            Section(p,"Your Widgets","Toggle a panel or select its name in the sidebar for more options");
            string[] keys={"clock","calendar","tasks","pomodoro","notes","rates","network","calculator","system"};
            string[] names={"Clock","Calendar","Tasks","Pomodoro","Quick Notes","Market Rates","Network Monitor","Calculator","System Monitor"};
            string[] desc={"Time and date display","Gregorian and Solar Hijri calendar","Simple task list","Focus and break timer","Quick notes","Market prices and rates","Network usage statistics","Built-in calculator","System usage statistics"};
            for(int i=0;i<keys.Length;i++){string k=keys[i];CheckCard(p,names[i],desc[i],_widgets.ContainsKey(k)&&_widgets[k],delegate(bool v){_setWidget(k,v);});}
            Section(p,"Planned","More control over your workspace");
            PlannedCard(p,"Movable panels, custom widgets, and per-widget layout settings.");
        }
        private void BuildWidget(Panel p,string key,string name,string desc,string planned)
        {
            Section(p,name,desc);
            CheckCard(p,"Show "+name,"Show or hide this panel on the desktop.",_widgets.ContainsKey(key)&&_widgets[key],delegate(bool v){_setWidget(key,v);});
            if(!String.IsNullOrEmpty(planned)){Section(p,"Planned settings","These controls are design placeholders");PlannedCard(p,planned);}
        }
        private void BuildClock(Panel p)
        {
            Section(p,"Clock Display","Configure the clock panel currently available");
            CheckCard(p,"Show Clock","Show or hide the clock and date panel.",_widgets.ContainsKey("clock")&&_widgets["clock"],delegate(bool v){_setWidget("clock",v);});
            ComboBox c=Combo(new[]{"24-hour  ·  14:30","12-hour  ·  02:30 PM"},_clockFormat=="12h"?"12-hour  ·  02:30 PM":"24-hour  ·  14:30");
            c.SelectedIndexChanged+=delegate{_setClockFormat(c.SelectedIndex==1?"12h":"24h");};
            ControlCard(p,"Time Format","Choose how time is displayed.",c);
            Section(p,"Planned settings","Additional clock sources");
            PlannedCard(p,"Select a city, use a custom time, or display local time.");
        }
        private void BuildRates(Panel p)
        {
            Section(p,"Market Rates","Configure the existing market data panel");
            CheckCard(p,"Show Market Rates","Show or hide the market rates panel.",_widgets.ContainsKey("rates")&&_widgets["rates"],delegate(bool v){_setWidget("rates",v);});
            ComboBox units=Combo(new[]{"Toman (T)","Dollar ($)"},_unit=="$"?"Dollar ($)":"Toman (T)");
            units.SelectedIndexChanged+=delegate{_setRateUnit(units.SelectedIndex==1?"$":"T");};
            ControlCard(p,"Display Currency","Choose the unit used by the current rates panel.",units);
            Button refresh=new Button { Text="↻   Refresh now",Width=150,Height=34,FlatStyle=FlatStyle.Flat,BackColor=cardAlt,ForeColor=fg,Cursor=Cursors.Hand };
            refresh.FlatAppearance.BorderColor=lineColor; refresh.Click+=delegate{_refreshRates();};
            ControlCard(p,"Market Data","Fetch the latest available values.",refresh);
            Section(p,"Planned settings","Choose what appears in the panel");
            PlannedCard(p,"Expanded market list with up to three selected rates.");
        }
        private void Planned(Panel p,string title,string desc)
        {
            Section(p,title,desc); PlannedCard(p,desc);
            InfoCard(p,"Design placeholder","This section has no active controls and starts no background activity.");
        }
        private void Section(Panel p,string title,string desc)
        {
            Panel h=new Panel { Dock=DockStyle.Top,Height=62,BackColor=bg };
            h.Controls.Add(new Label { Text=title,Location=new Point(4,3),AutoSize=true,Font=new Font("Segoe UI Semibold",12F),ForeColor=fg });
            h.Controls.Add(new Label { Text=desc,Location=new Point(5,31),AutoSize=true,Font=new Font("Segoe UI",8.5F),ForeColor=muted });
            p.Controls.Add(h); h.BringToFront();
        }
        private Panel Card(int height)
        {
            Panel c=new Panel { Dock=DockStyle.Top,Height=height,BackColor=cardColor,Padding=new Padding(10),Margin=new Padding(0,0,0,10) };
            c.Paint+=delegate(object s,PaintEventArgs e)
            {
                using(Pen pen=new Pen(lineColor))e.Graphics.DrawRectangle(pen,0,0,c.Width-1,c.Height-1);
                using(Pen pen=new Pen(Color.FromArgb(45,accent)))e.Graphics.DrawLine(pen,1,1,c.Width-2,1);
            };
            return c;
        }
        private void CheckCard(Panel p,string title,string desc,bool value,Action<bool> changed)
        {
            Panel c=Card(76);
            c.Controls.Add(new Label { Text=title,Location=new Point(17,13),AutoSize=true,Font=new Font("Segoe UI Semibold",9.5F),ForeColor=fg });
            c.Controls.Add(new Label { Text=desc,Location=new Point(18,39),AutoSize=true,Font=new Font("Segoe UI",8.5F),ForeColor=muted });
            CheckBox check=new CheckBox { Checked=value,Text="",AutoSize=false,Size=new Size(25,25),Location=new Point(c.Width-48,25),Anchor=AnchorStyles.Top|AnchorStyles.Right,BackColor=cardColor };
            check.CheckedChanged+=delegate{changed(check.Checked);}; c.Controls.Add(check); c.Resize+=delegate{check.Left=c.ClientSize.Width-46;};
            p.Controls.Add(c); c.BringToFront();
        }
        private void ControlCard(Panel p,string title,string desc,Control control)
        {
            Panel c=Card(88);
            c.Controls.Add(new Label { Text=title,Location=new Point(17,14),AutoSize=true,Font=new Font("Segoe UI Semibold",9.5F),ForeColor=fg });
            c.Controls.Add(new Label { Text=desc,Location=new Point(18,43),AutoSize=true,Font=new Font("Segoe UI",8.5F),ForeColor=muted });
            control.Anchor=AnchorStyles.Top|AnchorStyles.Right; control.Location=new Point(c.Width-245,25); control.Width=215;
            if(control is ComboBox){((ComboBox)control).FlatStyle=FlatStyle.Flat;((ComboBox)control).BackColor=cardAlt;((ComboBox)control).ForeColor=fg;}
            c.Controls.Add(control); c.Resize+=delegate{control.Left=c.ClientSize.Width-control.Width-18;};
            p.Controls.Add(c); c.BringToFront();
        }
        private void InfoCard(Panel p,string title,string desc)
        {
            Panel c=Card(82); c.BackColor=Color.FromArgb(24,32,52);
            c.Controls.Add(new Label { Text="ⓘ   "+title,Location=new Point(17,13),AutoSize=true,Font=new Font("Segoe UI Semibold",9.2F),ForeColor=Color.FromArgb(183,180,255) });
            c.Controls.Add(new Label { Text=desc,Location=new Point(19,42),AutoSize=true,ForeColor=muted,Font=new Font("Segoe UI",8.5F) });
            p.Controls.Add(c); c.BringToFront();
        }
        private void PlannedCard(Panel p,string desc)
        {
            Panel c=Card(82); c.BackColor=Color.FromArgb(24,31,50);
            c.Controls.Add(new Label { Text="PLANNED",Location=new Point(16,10),AutoSize=true,ForeColor=Color.FromArgb(195,185,255),Font=new Font("Segoe UI",7.5F,FontStyle.Bold),BackColor=Color.FromArgb(48,42,82),Padding=new Padding(7,4,7,4) });
            c.Controls.Add(new Label { Text=desc,Location=new Point(18,44),AutoSize=true,ForeColor=muted,Font=new Font("Segoe UI",8.5F) });
            p.Controls.Add(c); c.BringToFront();
        }
        private ComboBox Combo(string[] items,string selected)
        {
            ComboBox c=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList };
            c.Items.AddRange(items); c.SelectedItem=selected; return c;
        }
    }
}