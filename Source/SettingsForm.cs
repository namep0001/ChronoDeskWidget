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
        private readonly Panel _content;
        private readonly Label _title, _subtitle;
        private readonly Dictionary<string, Button> _nav = new Dictionary<string, Button>();
        private readonly Color bg = Color.FromArgb(16, 23, 39), side = Color.FromArgb(13, 19, 33);
        private readonly Color surface = Color.FromArgb(27, 36, 56), surface2 = Color.FromArgb(32, 43, 66);
        private readonly Color border = Color.FromArgb(48, 62, 91), fg = Color.FromArgb(239, 243, 255);
        private readonly Color muted = Color.FromArgb(159, 174, 204), accent = Color.FromArgb(111, 91, 255);
        private readonly string _language, _theme, _font, _unit, _clockFormat;
        private readonly int _opacity;
        private readonly bool _topmost, _startup;

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

            Text="ChronoDesk Widget — Settings"; StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1120,760); MinimumSize=new Size(900,620); BackColor=bg; ForeColor=fg;
            Font=new Font("Segoe UI",9F); ShowInTaskbar=false; FormBorderStyle=FormBorderStyle.Sizable; MaximizeBox=false;

            Panel root=new Panel { Dock=DockStyle.Fill, BackColor=bg, Padding=new Padding(1) }; Controls.Add(root);
            Panel sidebar=new Panel { Dock=DockStyle.Left, Width=248, BackColor=side, Padding=new Padding(13,14,12,12) }; root.Controls.Add(sidebar);
            Panel brand=new Panel { Dock=DockStyle.Top, Height=78, BackColor=side }; sidebar.Controls.Add(brand);
            Panel logo=new Panel { Location=new Point(3,5), Size=new Size(44,44), BackColor=Color.FromArgb(34,43,76) };
            logo.Paint+=delegate(object s,PaintEventArgs e) {
                e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(Pen pen=new Pen(Color.FromArgb(173,183,255),3F)) {
                    e.Graphics.DrawEllipse(pen,12,12,20,20); e.Graphics.DrawLine(pen,22,4,22,12);
                    e.Graphics.DrawLine(pen,22,32,22,40); e.Graphics.DrawLine(pen,4,22,12,22); e.Graphics.DrawLine(pen,32,22,40,22);
                }
            }; brand.Controls.Add(logo);
            brand.Controls.Add(new Label { Text="ChronoDesk Widget",Location=new Point(57,7),AutoSize=true,Font=new Font("Segoe UI Semibold",11.5F),ForeColor=fg });
            brand.Controls.Add(new Label { Text="SETTINGS",Location=new Point(58,34),AutoSize=true,Font=new Font("Segoe UI",8F,FontStyle.Bold),ForeColor=muted });

            FlowLayoutPanel nav=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=side,Padding=new Padding(0,4,0,0) };
            sidebar.Controls.Add(nav);
            AddNav(nav,"General","⌂",false); AddNav(nav,"Language","◎",false); AddNav(nav,"Appearance","◉",false);
            AddNav(nav,"Widgets","⬡",false,true);
            AddNav(nav,"Clock","◷",true); AddNav(nav,"Calendar","▦",true); AddNav(nav,"Pomodoro","◴",true);
            AddNav(nav,"Tasks","☷",true); AddNav(nav,"Notes","▤",true); AddNav(nav,"Market Rates","↗",true);
            AddNav(nav,"Network","⌁",true); AddNav(nav,"Calculator","＋",true); AddNav(nav,"System Monitor","▥",true);
            AddNav(nav,"Custom Panels","＋",true,false,true); AddNav(nav,"Performance","ϟ",false,false,true);
            AddNav(nav,"Dashboard","▦",false,false,true); AddNav(nav,"Storage","▰",false,false,true);

            Panel version=new Panel { Dock=DockStyle.Bottom,Height=60,BackColor=Color.FromArgb(23,30,50),Padding=new Padding(9) };
            version.Paint+=delegate(object s,PaintEventArgs e){using(Pen pen=new Pen(border))e.Graphics.DrawRectangle(pen,0,0,version.Width-1,version.Height-1);};
            version.Controls.Add(new Label { Text="●  ChronoDesk Widget",Location=new Point(9,7),AutoSize=true,ForeColor=muted,Font=new Font("Segoe UI",8.5F) });
            version.Controls.Add(new Label { Text="Settings  •  Build 21116",Location=new Point(9,29),AutoSize=true,ForeColor=muted,Font=new Font("Segoe UI",8F) });
            sidebar.Controls.Add(version);

            Panel right=new Panel { Dock=DockStyle.Fill,BackColor=bg,Padding=new Padding(20,17,18,12) }; root.Controls.Add(right);
            Panel top=new Panel { Dock=DockStyle.Top,Height=83,BackColor=bg }; right.Controls.Add(top);
            _title=new Label { Text="General Settings",Location=new Point(3,5),AutoSize=true,Font=new Font("Segoe UI Semibold",19F),ForeColor=fg };
            _subtitle=new Label { Text="Configure the basic behavior of ChronoDesk Widget.",Location=new Point(5,43),AutoSize=true,Font=new Font("Segoe UI",9.5F),ForeColor=muted };
            top.Controls.Add(_title); top.Controls.Add(_subtitle);
            Panel line=new Panel { Dock=DockStyle.Bottom,Height=2 };
            line.Paint+=delegate(object s,PaintEventArgs e){using(LinearGradientBrush b=new LinearGradientBrush(line.ClientRectangle,accent,Color.FromArgb(39,163,255),0F))e.Graphics.FillRectangle(b,line.ClientRectangle);};
            top.Controls.Add(line);
            Panel footer=new Panel { Dock=DockStyle.Bottom,Height=48,BackColor=bg }; right.Controls.Add(footer);
            footer.Controls.Add(new Label { Text="Changes apply immediately",Location=new Point(3,15),AutoSize=true,ForeColor=muted,Font=new Font("Segoe UI",8.5F) });
            Button done=new Button { Text="Done",Width=112,Height=32,FlatStyle=FlatStyle.Flat,BackColor=accent,ForeColor=Color.White,Cursor=Cursors.Hand,Anchor=AnchorStyles.Top|AnchorStyles.Right,Location=new Point(footer.Width-116,7) };
            done.FlatAppearance.BorderSize=0; done.Click+=delegate{Close();}; footer.Controls.Add(done);
            footer.Resize+=delegate{done.Left=footer.ClientSize.Width-done.Width-2;};
            _content=new Panel { Dock=DockStyle.Fill,BackColor=bg,Padding=new Padding(0,13,4,8),AutoScroll=true }; right.Controls.Add(_content);
            ShowPage("General");
        }

        private void AddNav(FlowLayoutPanel parent,string key,string glyph,bool sub,bool main=false,bool planned=false)
        {
            Panel wrap=new Panel { Width=215,Height=sub?39:50,Margin=new Padding(0,0,0,3),BackColor=side };
            Button b=new Button { Text=(sub?"    ":"  ")+glyph+"   "+key+(planned?"   ·":""),TextAlign=ContentAlignment.MiddleLeft,Dock=DockStyle.Fill,
                FlatStyle=FlatStyle.Flat,BackColor=side,ForeColor=planned?Color.FromArgb(129,141,171):(sub?muted:fg),
                Font=new Font("Segoe UI",sub?9F:9.3F,main?FontStyle.Bold:FontStyle.Regular),Cursor=Cursors.Hand,Padding=new Padding(2,0,0,0) };
            b.FlatAppearance.BorderSize=0; b.FlatAppearance.MouseOverBackColor=Color.FromArgb(28,39,64);
            b.Click+=delegate{ShowPage(key);}; wrap.Controls.Add(b); parent.Controls.Add(wrap); _nav[key]=b;
        }

        private void ShowPage(string key)
        {
            foreach(KeyValuePair<string,Button> item in _nav) {
                bool selected=item.Key==key || (key=="Widgets"&&item.Key=="Widgets");
                item.Value.BackColor=selected?Color.FromArgb(37,48,81):side;
                item.Value.ForeColor=selected?Color.FromArgb(195,190,255):(item.Key=="Custom Panels"||item.Key=="Performance"||item.Key=="Dashboard"||item.Key=="Storage"?Color.FromArgb(129,141,171):fg);
            }
            _content.Controls.Clear();
            _title.Text=key=="Market Rates"||key=="System Monitor"?key:key+((key=="General"||key=="Language"||key=="Appearance"||key=="Widgets")?" Settings":"");
            _subtitle.Text=Subtitle(key);
            Panel p=new Panel { Dock=DockStyle.Fill,AutoScroll=true,BackColor=bg,Padding=new Padding(0,0,4,8) }; _content.Controls.Add(p);
            if(key=="General") BuildGeneral(p);
            else if(key=="Language") BuildLanguage(p);
            else if(key=="Appearance") BuildAppearance(p);
            else if(key=="Widgets") BuildWidgets(p);
            else if(key=="Clock") BuildClock(p);
            else if(key=="Market Rates") BuildRates(p);
            else if(key=="Calendar") BuildWidget(p,"calendar","Calendar","The existing Gregorian and Solar Hijri calendar panel.","Calendar system selection is planned.");
            else if(key=="Pomodoro") BuildWidget(p,"pomodoro","Pomodoro","The existing focus and break timer panel.","Custom focus and break durations are planned.");
            else if(key=="Tasks") BuildWidget(p,"tasks","Tasks","The existing task list panel.",null);
            else if(key=="Notes") BuildWidget(p,"notes","Quick Notes","The existing notes panel.",null);
            else if(key=="Network") BuildWidget(p,"network","Network Monitor","The existing network usage panel.","Fine-grained background activity controls are planned.");
            else if(key=="Calculator") BuildWidget(p,"calculator","Calculator","The existing calculator panel.",null);
            else if(key=="System Monitor") BuildWidget(p,"system","System Monitor","The existing system statistics panel.",null);
            else if(key=="Custom Panels") Planned(p,"Custom Panels","Move panels freely and create your own widget panels.");
            else if(key=="Performance") Planned(p,"Performance","Control optional internet features, polling, and background resource usage.");
            else if(key=="Dashboard") Planned(p,"Dashboard","Create one-click shortcuts for applications and websites.");
            else if(key=="Storage") Planned(p,"Storage","Explore disk usage with a visual storage analyzer.");
        }

        private string Subtitle(string key)
        {
            switch(key) {
                case "General":return "Configure the basic behavior of ChronoDesk Widget.";
                case "Language":return "Choose the interface language and reading direction.";
                case "Appearance":return "Tune the theme, transparency, and typography.";
                case "Widgets":return "Manage the panels currently available in the widget.";
                case "Clock":return "Configure the clock panel currently available.";
                case "Calendar":return "Calendar visibility and future calendar options.";
                case "Pomodoro":return "The current timer panel and planned focus controls.";
                case "Market Rates":return "Configure the existing market data panel.";
                case "Custom Panels":return "Arrange and extend your desktop workspace.";
                case "Performance":return "Manage background activity and resource usage.";
                case "Dashboard":return "Keep favorite applications and websites within one click.";
                case "Storage":return "Review disk usage and large folders.";
                default:return "Configure the "+key.ToLowerInvariant()+" panel.";
            }
        }

        private void BuildGeneral(Panel p)
        {
            Section(p,"Startup & Behavior","Basic application behavior");
            CheckCard(p,"Start with Windows","Launch ChronoDesk Widget automatically when you sign in.",_startup,delegate(bool v){_setStartup(v);});
            CheckCard(p,"Keep on Top","Keep the widget visible above other windows.",_topmost,delegate(bool v){_setTopmost(v);});
            Section(p,"Settings","Current options are connected to the application");
            InfoCard(p,"Instant changes","Existing settings apply immediately. Planned controls are visual placeholders only.");
        }
        private void BuildLanguage(Panel p)
        {
            Section(p,"Language & Direction","Interface localization");
            ComboBox c=Combo(new[]{"English","فارسی"},_language=="fa"?"فارسی":"English");
            c.SelectedIndexChanged+=delegate{_setLanguage(c.SelectedIndex==1?"fa":"en");};
            ControlCard(p,"Application Language","Choose the currently supported interface language.",c);
            InfoCard(p,"More languages","Additional popular languages and automatic right-to-left layout support are planned.");
        }
        private void BuildAppearance(Panel p)
        {
            Section(p,"Theme & Visual Style","Personalize the widget");
            ComboBox themes=Combo(new[]{"System","Light","Dark"},_theme=="light"?"Light":_theme=="dark"?"Dark":"System");
            themes.SelectedIndexChanged+=delegate{_setTheme(themes.SelectedItem.ToString().ToLowerInvariant());};
            ControlCard(p,"Theme","Follow Windows or choose a theme.",themes);
            ComboBox opacity=Combo(new[]{"100%","90%","80%","70%","60%","50%"},_opacity+"%");
            opacity.SelectedIndexChanged+=delegate{_setOpacity(int.Parse(opacity.SelectedItem.ToString().TrimEnd('%')));};
            ControlCard(p,"Widget Opacity","Adjust the transparency of the desktop widget.",opacity);
            ComboBox fonts=Combo(new[]{"Segoe UI","Arial","Calibri","Tahoma","Verdana","Microsoft YaHei UI"},_font);
            fonts.SelectedIndexChanged+=delegate{_setFont(fonts.SelectedItem.ToString());};
            ControlCard(p,"Font Family","Choose the typeface used by the widget.",fonts);
            InfoCard(p,"Visual refinements","More theme tuning and improved light-theme styling are planned.");
        }
        private void BuildWidgets(Panel p)
        {
            Section(p,"Widget Panels","Select a panel in the sidebar or change its visibility here");
            string[] keys={"clock","calendar","tasks","pomodoro","notes","rates","network","calculator","system"};
            string[] names={"Clock","Calendar","Tasks","Pomodoro","Quick Notes","Market Rates","Network Monitor","Calculator","System Monitor"};
            string[] desc={"Time and date display","Gregorian and Solar Hijri calendar","Simple task list","Focus and break timer","Quick notes","Market prices and rates","Network usage statistics","Built-in calculator","System usage statistics"};
            for(int i=0;i<keys.Length;i++){string k=keys[i];CheckCard(p,names[i],desc[i],_widgets.ContainsKey(k)&&_widgets[k],delegate(bool v){_setWidget(k,v);});}
            InfoCard(p,"Future panels","Custom panels, movable layout, dashboard shortcuts, and storage analysis are planned.");
        }
        private void BuildWidget(Panel p,string key,string name,string desc,string planned)
        {
            Section(p,name,desc);
            CheckCard(p,"Show "+name,"Show or hide this panel on the desktop widget.",_widgets.ContainsKey(key)&&_widgets[key],delegate(bool v){_setWidget(key,v);});
            if(!String.IsNullOrEmpty(planned))PlannedCard(p,planned);
        }
        private void BuildClock(Panel p)
        {
            Section(p,"Clock Display","Configure the existing clock panel");
            CheckCard(p,"Show Clock","Show or hide the clock and date panel.",_widgets.ContainsKey("clock")&&_widgets["clock"],delegate(bool v){_setWidget("clock",v);});
            ComboBox c=Combo(new[]{"24-hour  ·  14:30","12-hour  ·  02:30 PM"},_clockFormat=="12h"?"12-hour  ·  02:30 PM":"24-hour  ·  14:30");
            c.SelectedIndexChanged+=delegate{_setClockFormat(c.SelectedIndex==1?"12h":"24h");};
            ControlCard(p,"Time Format","Choose how time is displayed.",c);
            PlannedCard(p,"Select a city, use a custom time, or display local time.");
        }
        private void BuildRates(Panel p)
        {
            Section(p,"Market Rates","Configure the existing market data panel");
            CheckCard(p,"Show Market Rates","Show or hide the market rates panel.",_widgets.ContainsKey("rates")&&_widgets["rates"],delegate(bool v){_setWidget("rates",v);});
            ComboBox units=Combo(new[]{"Toman (T)","Dollar ($)"},_unit=="$"?"Dollar ($)":"Toman (T)");
            units.SelectedIndexChanged+=delegate{_setRateUnit(units.SelectedIndex==1?"$":"T");};
            ControlCard(p,"Display Currency","Choose the unit used by the current rates panel.",units);
            Button refresh=new Button { Text="↻   Refresh now",Width=145,Height=34,FlatStyle=FlatStyle.Flat,BackColor=surface2,ForeColor=fg,Cursor=Cursors.Hand };
            refresh.FlatAppearance.BorderColor=border; refresh.Click+=delegate{_refreshRates();};
            ControlCard(p,"Market Data","Fetch the latest available values.",refresh);
            PlannedCard(p,"Choose up to three market rates from an expanded list.");
        }
        private void Planned(Panel p,string name,string desc)
        {
            Section(p,name,desc); PlannedCard(p,desc);
            InfoCard(p,"Planned section","This is a design placeholder and does not add behavior or start background activity.");
        }
        private void Section(Panel p,string title,string desc)
        {
            Panel h=new Panel { Dock=DockStyle.Top,Height=59,BackColor=bg };
            h.Controls.Add(new Label { Text=title,Location=new Point(4,2),AutoSize=true,Font=new Font("Segoe UI Semibold",12F),ForeColor=fg });
            h.Controls.Add(new Label { Text=desc,Location=new Point(5,29),AutoSize=true,Font=new Font("Segoe UI",8.5F),ForeColor=muted });
            p.Controls.Add(h); h.BringToFront();
        }
        private Panel Card(int height)
        {
            Panel c=new Panel { Dock=DockStyle.Top,Height=height,BackColor=surface,Padding=new Padding(10),Margin=new Padding(0,0,0,9) };
            c.Paint+=delegate(object s,PaintEventArgs e){using(Pen pen=new Pen(border))e.Graphics.DrawRectangle(pen,0,0,c.Width-1,c.Height-1);using(Pen pen=new Pen(Color.FromArgb(45,accent)))e.Graphics.DrawLine(pen,1,1,c.Width-2,1);};
            return c;
        }
        private void CheckCard(Panel p,string title,string desc,bool value,Action<bool> changed)
        {
            Panel c=Card(70);
            c.Controls.Add(new Label { Text=title,Location=new Point(16,12),AutoSize=true,Font=new Font("Segoe UI Semibold",9.5F),ForeColor=fg });
            c.Controls.Add(new Label { Text=desc,Location=new Point(17,37),AutoSize=true,Font=new Font("Segoe UI",8.3F),ForeColor=muted });
            CheckBox check=new CheckBox { Checked=value,Text="",AutoSize=false,Size=new Size(24,24),Location=new Point(c.Width-45,22),Anchor=AnchorStyles.Top|AnchorStyles.Right,BackColor=surface };
            check.CheckedChanged+=delegate{changed(check.Checked);}; c.Controls.Add(check); c.Resize+=delegate{check.Left=c.ClientSize.Width-44;};
            p.Controls.Add(c); c.BringToFront();
        }
        private void ControlCard(Panel p,string title,string desc,Control control)
        {
            Panel c=Card(82);
            c.Controls.Add(new Label { Text=title,Location=new Point(16,13),AutoSize=true,Font=new Font("Segoe UI Semibold",9.5F),ForeColor=fg });
            c.Controls.Add(new Label { Text=desc,Location=new Point(17,40),AutoSize=true,Font=new Font("Segoe UI",8.3F),ForeColor=muted });
            control.Anchor=AnchorStyles.Top|AnchorStyles.Right; control.Location=new Point(c.Width-235,22); control.Width=205;
            if(control is ComboBox){((ComboBox)control).FlatStyle=FlatStyle.Flat;((ComboBox)control).BackColor=surface2;((ComboBox)control).ForeColor=fg;}
            c.Controls.Add(control); c.Resize+=delegate{control.Left=c.ClientSize.Width-control.Width-18;};
            p.Controls.Add(c); c.BringToFront();
        }
        private void InfoCard(Panel p,string title,string desc)
        {
            Panel c=Card(76); c.BackColor=Color.FromArgb(25,34,54);
            c.Controls.Add(new Label { Text="ⓘ   "+title,Location=new Point(16,12),AutoSize=true,Font=new Font("Segoe UI Semibold",9F),ForeColor=Color.FromArgb(182,187,255) });
            c.Controls.Add(new Label { Text=desc,Location=new Point(18,39),AutoSize=true,ForeColor=muted,Font=new Font("Segoe UI",8.3F) });
            p.Controls.Add(c); c.BringToFront();
        }
        private void PlannedCard(Panel p,string desc)
        {
            Panel c=Card(78); c.BackColor=Color.FromArgb(25,32,51);
            c.Controls.Add(new Label { Text="PLANNED",Location=new Point(15,11),AutoSize=true,ForeColor=Color.FromArgb(187,177,255),Font=new Font("Segoe UI",7.5F,FontStyle.Bold),BackColor=Color.FromArgb(49,43,86),Padding=new Padding(7,4,7,4) });
            c.Controls.Add(new Label { Text=desc,Location=new Point(17,43),AutoSize=true,ForeColor=muted,Font=new Font("Segoe UI",8.3F) });
            p.Controls.Add(c); c.BringToFront();
        }
        private ComboBox Combo(string[] items,string selected)
        {
            ComboBox c=new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList }; c.Items.AddRange(items); c.SelectedItem=selected; return c;
        }
    }
}