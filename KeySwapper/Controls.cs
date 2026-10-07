using System.Drawing.Drawing2D;
using static KeySwapper.Theme;

namespace KeySwapper;

/// <summary>Rounded white panel on the gray window background.</summary>
sealed class Card : Panel
{
    public Card()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Back;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundRect(new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), D(8));
        using var fill = new SolidBrush(Theme.Card);
        using var pen = new Pen(Theme.Border);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);
    }
}

/// <summary>A keyboard-key looking label.</summary>
sealed class KeyCap : Control
{
    bool dimmed;

    public KeyCap()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Font = new Font(MonoFont, 11f);
        BackColor = Theme.Card;
        Height = D(30);
    }

    public bool Dimmed
    {
        get => dimmed;
        set { dimmed = value; Invalidate(); }
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Width = Measure(Text, Font);
    }

    public static int Measure(string text, Font font) =>
        Math.Max(D(34), TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding).Width + D(20));

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        Draw(e.Graphics, ClientRectangle, Text, Font, dimmed);
    }

    public static void Draw(Graphics g, Rectangle bounds, string text, Font font, bool dimmed = false)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float r = D(6);
        var outer = new RectangleF(bounds.X + 0.5f, bounds.Y + 0.5f, bounds.Width - 1.5f, bounds.Height - 1.5f);
        var inner = new RectangleF(outer.X + 1, outer.Y + 1, outer.Width - 2, outer.Height - 2 - D(2));
        using (var path = RoundRect(outer, r))
        using (var brush = new SolidBrush(KeyBorder))
            g.FillPath(brush, path);
        using (var path = RoundRect(inner, r - 1))
        using (var brush = new SolidBrush(KeyFill))
            g.FillPath(brush, path);

        TextRenderer.DrawText(g, text, font, Rectangle.Round(inner), dimmed ? Muted : Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

/// <summary>Windows 11 style on/off switch.</summary>
sealed class ToggleSwitch : Control
{
    bool isOn;
    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        Size = new Size(D(44), D(22));
    }

    public bool Checked
    {
        get => isOn;
        set
        {
            if (isOn == value) return;
            isOn = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Focus();
        Checked = !Checked;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space) Checked = !Checked;
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var track = new RectangleF(1, 1, Width - 3, Height - 3);
        float radius = track.Height / 2;
        using (var path = RoundRect(track, radius))
        {
            if (isOn)
            {
                using var fill = new SolidBrush(Accent);
                g.FillPath(fill, path);
            }
            else
            {
                using var pen = new Pen(SubText, 1.2f);
                g.DrawPath(pen, path);
            }
        }

        float knob = isOn ? track.Height - D(8) : track.Height - D(10);
        float x = isOn ? track.Right - D(4) - knob : track.X + D(5);
        float y = track.Y + (track.Height - knob) / 2;
        using (var brush = new SolidBrush(isOn ? OnAccent : SubText))
            g.FillEllipse(brush, x, y, knob, knob);

        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(g, ClientRectangle);
    }
}

/// <summary>Click it, press a key, and it remembers which key (and whether Shift was held).</summary>
sealed class KeyCaptureBox : Control
{
    readonly Font hintFont = new(UiFont, 9.75f);
    string display = "";

    public int Vk { get; private set; }
    public bool Shift { get; private set; }
    public string Display => display;

    public event Action? KeyCaptured;
    public event Action? Rejected;

    public KeyCaptureBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        Font = new Font(MonoFont, 11f);
        BackColor = Theme.Card;
    }

    // Keep Tab and Esc working for dialog navigation; capture everything else.
    protected override bool IsInputKey(Keys keyData) =>
        keyData is not (Keys.Tab or Keys.Escape or (Keys.Tab | Keys.Shift));

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        if (e.KeyCode is Keys.ShiftKey or Keys.ControlKey or Keys.Menu or Keys.LWin or Keys.RWin or Keys.Capital)
            return;
        if (e.Control || e.Alt)
        {
            Rejected?.Invoke();
            return;
        }

        Vk = (int)e.KeyCode;
        Shift = e.Shift;
        display = KeyNames.Describe(Vk, Shift);
        Invalidate();
        KeyCaptured?.Invoke();
    }

    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); Focus(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float w = Focused ? 2f : 1f;
        using (var path = RoundRect(new RectangleF(w / 2, w / 2, Width - w - 1, Height - w - 1), D(6)))
        using (var pen = new Pen(Focused ? Accent : KeyBorder, w))
            g.DrawPath(pen, path);

        int x = D(8);
        string hint;
        if (display.Length > 0)
        {
            int keyWidth = KeyCap.Measure(display, Font);
            KeyCap.Draw(g, new Rectangle(x, (Height - D(30)) / 2, keyWidth, D(30)), display, Font);
            x += keyWidth + D(10);
            hint = Focused ? "press another key to change" : "";
        }
        else
        {
            x += D(4);
            hint = Focused ? "Press any key…" : "Click here, then press a key";
        }

        TextRenderer.DrawText(g, hint, hintFont, new Rectangle(x, 0, Width - x - D(8), Height), Muted,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) hintFont.Dispose();
        base.Dispose(disposing);
    }
}
