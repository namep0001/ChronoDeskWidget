using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace ChronoDeskWidget
{
    internal sealed class VectorIconView : Control
    {
        private string _kind;
        public string Kind { get { return _kind; } set { _kind = value ?? "clock"; Invalidate(); } }

        public VectorIconView(string kind)
        {
            _kind = kind ?? "clock";
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
                    case "play":
                        PointF[] play = { new PointF(8.3F, 5), new PointF(19.3F, 12), new PointF(8.3F, 19) };
                        g.FillPolygon(brush, play);
                        break;
                    case "pause":
                        using (GraphicsPath pause = new GraphicsPath())
                        {
                            pause.AddRectangle(new RectangleF(7, 5, 4, 14));
                            pause.AddRectangle(new RectangleF(14, 5, 4, 14));
                            g.FillPath(brush, pause);
                        }
                        break;
                    case "reset":
                        g.DrawArc(pen, 5, 5, 14, 14, 42, 278);
                        PointF[] resetArrow = { new PointF(4, 5), new PointF(12, 5), new PointF(6, 12) };
                        g.FillPolygon(brush, resetArrow);
                        break;
                    case "refresh":
                        g.DrawArc(pen, 5, 5, 14, 14, 48, 278);
                        PointF[] refreshArrow = { new PointF(17, 3), new PointF(21, 8), new PointF(14, 9) };
                        g.FillPolygon(brush, refreshArrow);
                        break;
                    case "coffee":
                    case "rest":
                        GraphicsState cupState = g.Save();
                        g.TranslateTransform(-1.5F, -3F);
                        using (GraphicsPath cup = new GraphicsPath())
                        {
                            cup.AddLine(5, 8, 17, 8);
                            cup.AddLine(17, 8, 15, 18);
                            cup.AddBezier(15, 18, 14, 21, 8, 21, 7, 18);
                            cup.AddLine(7, 18, 5, 8);
                            cup.CloseFigure();
                            g.DrawPath(pen, cup);
                        }
                        g.DrawArc(pen, 16, 10, 6, 6, 270, 180);
                        g.DrawLine(pen, 7, 21, 17, 21);
                        g.Restore(cupState);
                        break;
                    case "export":
                        g.DrawLine(pen, 12, 3, 12, 15);
                        g.DrawLine(pen, 7, 10, 12, 15);
                        g.DrawLine(pen, 17, 10, 12, 15);
                        g.DrawLine(pen, 5, 17, 5, 21);
                        g.DrawLine(pen, 5, 21, 19, 21);
                        g.DrawLine(pen, 19, 21, 19, 17);
                        break;
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
                    case "network":
                        g.DrawArc(pen, 3.5F, 4, 17, 15, 205, 130);
                        g.DrawArc(pen, 7, 8, 10, 9, 205, 130);
                        g.DrawArc(pen, 10.5F, 12, 3, 3, 205, 130);
                        g.FillEllipse(brush, 10.2F, 18, 3.6F, 3.6F);
                        break;
                    case "calculator":
                        using (GraphicsPath calc = RoundedRectangle(3.5F, 2.5F, 17, 19, 2F)) g.DrawPath(pen, calc);
                        g.DrawLine(pen, 6.5F, 7, 17.5F, 7);
                        for (int row = 0; row < 3; row++)
                            for (int col = 0; col < 3; col++)
                                g.FillEllipse(brush, 6.5F + col * 5.3F, 10 + row * 4F, 2.2F, 2.2F);
                        break;
                    case "system":
                        g.DrawLine(pen, 3, 20, 21, 20);
                        g.DrawLine(pen, 5, 17, 5, 11);
                        g.DrawLine(pen, 10, 17, 10, 6);
                        g.DrawLine(pen, 15, 17, 15, 9);
                        g.DrawLine(pen, 20, 17, 20, 3);
                        g.DrawLine(pen, 3, 17, 21, 17);
                        break;
                    case "save":
                        using (Pen disk = new Pen(ForeColor, 2F))
                        using (GraphicsPath shape = new GraphicsPath())
                        {
                            disk.StartCap = LineCap.Round; disk.EndCap = LineCap.Round; disk.LineJoin = LineJoin.Round;
                            shape.AddLine(5, 3, 17, 3); shape.AddLine(17, 3, 21, 7); shape.AddLine(21, 7, 21, 21);
                            shape.AddLine(21, 21, 3, 21); shape.AddLine(3, 21, 3, 3); shape.CloseFigure();
                            g.DrawPath(disk, shape);
                            g.DrawRectangle(disk, 7, 4, 9, 6);
                            g.DrawRectangle(disk, 7, 13, 10, 6);
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

    internal sealed class ThemedProgressBar : Control
    {
        private int _value;
        public int Value
        {
            get { return _value; }
            set { _value = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }
        public Color TrackColor { get; set; }
        public Color FillColor { get; set; }

        public ThemedProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = 13;
            TrackColor = Color.FromArgb(225, 232, 241);
            FillColor = Color.DodgerBlue;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 1, Math.Max(0, Width - 1), Math.Max(1, Height - 2));
            if (bounds.Width <= 0) return;
            using (GraphicsPath track = RoundedRectangle(bounds, Math.Max(2, bounds.Height / 2)))
            using (SolidBrush trackBrush = new SolidBrush(TrackColor))
                e.Graphics.FillPath(trackBrush, track);
            int fillWidth = (int)Math.Round(bounds.Width * _value / 100.0);
            if (fillWidth > 0)
            {
                Rectangle fillBounds = new Rectangle(bounds.X, bounds.Y, Math.Min(bounds.Width, fillWidth), bounds.Height);
                using (GraphicsPath fill = RoundedRectangle(fillBounds, Math.Max(2, bounds.Height / 2)))
                using (SolidBrush fillBrush = new SolidBrush(FillColor))
                    e.Graphics.FillPath(fillBrush, fill);
            }
        }

        private static GraphicsPath RoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = Math.Min(radius * 2F, Math.Min(rect.Width, rect.Height));
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class CircularIconButton : Control
    {
        private readonly VectorIconView _icon;
        private bool _hovered;
        private bool _pressed;

        public string IconKind { get; private set; }
        public string Kind { get { return IconKind; } set { IconKind = value ?? "play"; if (_icon != null) _icon.Kind = IconKind; } }
        public bool ShowBackground { get; set; }

        public CircularIconButton(string iconKind)
        {
            IconKind = iconKind ?? "play";
            ShowBackground = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = SystemColors.Control;
            Cursor = Cursors.Hand;
            TabStop = false;

            _icon = new VectorIconView(IconKind);
            _icon.Enabled = false;
            _icon.BackColor = Color.Transparent;
            _icon.TabStop = false;
            Controls.Add(_icon);
            UpdateIconLayout();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateIconLayout();
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            if (_icon != null) _icon.ForeColor = ForeColor;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) _pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color parentColor = Parent == null ? SystemColors.Control : Parent.BackColor;
            using (SolidBrush background = new SolidBrush(parentColor))
                e.Graphics.FillRectangle(background, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            if (ShowBackground || _hovered)
            {
                Color fill = _pressed ? ControlPaint.Dark(BackColor, 0.10F) :
                    (_hovered ? ControlPaint.Light(BackColor, 0.12F) : BackColor);
                using (SolidBrush brush = new SolidBrush(fill))
                    g.FillEllipse(brush, 0.8F, 0.8F, Math.Max(0, Width - 1.6F), Math.Max(0, Height - 1.6F));
            }
        }

        private void UpdateIconLayout()
        {
            if (_icon == null) return;
            int iconSize = Math.Max(12, Math.Min(Width, Height) - 4);
            _icon.Size = new Size(iconSize, iconSize);
            _icon.Location = new Point((Width - iconSize) / 2, (Height - iconSize) / 2);
            _icon.ForeColor = ForeColor;
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
                int diameter = Math.Min(Width, Height) - 2;
                Rectangle circle = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
                using (LinearGradientBrush badge = new LinearGradientBrush(circle,
                    Color.FromArgb(255, 226, 128), Color.FromArgb(153, 91, 25), 135F))
                    g.FillEllipse(badge, circle);

                float cx = circle.Left + circle.Width / 2F;
                float cy = circle.Top + circle.Height / 2F;
                float unit = diameter / 24F;
                PointF[] barPoints = {
                    new PointF(cx - 7.5F * unit, cy - 2.0F * unit),
                    new PointF(cx - 4.5F * unit, cy - 5.0F * unit),
                    new PointF(cx + 4.5F * unit, cy - 5.0F * unit),
                    new PointF(cx + 7.5F * unit, cy - 2.0F * unit),
                    new PointF(cx + 5.5F * unit, cy + 4.5F * unit),
                    new PointF(cx - 5.5F * unit, cy + 4.5F * unit)
                };
                using (GraphicsPath body = new GraphicsPath())
                using (LinearGradientBrush gold = new LinearGradientBrush(circle,
                    Color.FromArgb(255, 249, 190), Color.FromArgb(190, 116, 25), 90F))
                using (Pen edge = new Pen(Color.FromArgb(145, 83, 18), Math.Max(0.7F, unit)))
                {
                    body.AddPolygon(barPoints);
                    g.FillPath(gold, body);
                    g.DrawPath(edge, body);
                }
                using (Pen shine = new Pen(Color.FromArgb(255, 255, 224), Math.Max(0.7F, unit * 0.9F)))
                using (Pen shade = new Pen(Color.FromArgb(177, 105, 24), Math.Max(0.6F, unit * 0.75F)))
                {
                    g.DrawLine(shine, cx - 4.2F * unit, cy - 4.0F * unit, cx + 4.2F * unit, cy - 4.0F * unit);
                    g.DrawLine(shade, cx - 5.0F * unit, cy - 1.0F * unit, cx + 5.0F * unit, cy - 1.0F * unit);
                    g.DrawLine(shade, cx - 4.0F * unit, cy + 1.8F * unit, cx + 4.0F * unit, cy + 1.8F * unit);
                }
                using (Pen border = new Pen(Color.FromArgb(150, 255, 255, 255), 0.8F))
                    g.DrawEllipse(border, circle);
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

    internal sealed class RoundedCardPanel : Panel
    {
        public RoundedCardPanel() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); using (GraphicsPath p = Path(new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)))) Region = new Region(p); }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (GraphicsPath path = Path(bounds))
            using (LinearGradientBrush fill = new LinearGradientBrush(bounds,
                ControlPaint.Light(BackColor, 0.045F), BackColor, LinearGradientMode.Vertical))
            using (Pen border = new Pen(Parent == null ? Color.Gray : ControlPaint.Light(Parent.BackColor, 0.12F), 1F))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
        }
        private static GraphicsPath Path(Rectangle r) { GraphicsPath p = new GraphicsPath(); int d = Math.Min(24, Math.Min(r.Width, r.Height)); p.AddArc(r.Left,r.Top,d,d,180,90); p.AddArc(r.Right-d,r.Top,d,d,270,90); p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.Left,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p; }
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
        public bool IsToday { get; set; }

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

            int diameter = Math.Min(28, Math.Min(Width - 2, Height - 2));
            if (!FillColor.IsEmpty && diameter > 0)
            {
                Rectangle circle = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
                using (SolidBrush fill = new SolidBrush(FillColor))
                    g.FillEllipse(fill, circle);
                if (IsToday)
                {
                    using (Pen todayRing = new Pen(ForeColor, 1.6F))
                        g.DrawEllipse(todayRing, circle);
                }
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

                RectangleF timeBounds = new RectangleF(8, Height * 0.31F, Width - 16, 36);
                RectangleF modeBounds = new RectangleF(12, Height * 0.68F, Width - 24, 15);
                if (modeBounds.Width > 0F && modeBounds.Height > 0F)
                    g.DrawString(ModeText ?? string.Empty, modeFont, modeBrush, modeBounds, centered);
                if (timeBounds.Width > 0F && timeBounds.Height > 0F)
                    g.DrawString(TimeText ?? string.Empty, timeFont, textBrush, timeBounds, centered);
            }
        }
    }
}
