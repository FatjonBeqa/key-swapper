using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace KeySwapper;

static class Theme
{
    public static float Scale { get; set; } = 1f;
    public static int D(int px) => (int)Math.Round(px * Scale);

    static bool Dark => Application.IsDarkModeEnabled;
    static Color Pick(int light, int dark) => Color.FromArgb(unchecked((int)0xFF000000) | (Dark ? dark : light));

    public static Color Back => Pick(0xF3F3F3, 0x202020);
    public static Color Card => Pick(0xFFFFFF, 0x2B2B2B);
    public static Color Border => Pick(0xE5E5E5, 0x3A3A3A);
    public static Color KeyBorder => Pick(0xC8C8C8, 0x555555);
    public static Color KeyFill => Pick(0xF9F9F9, 0x373737);
    public static Color Text => Pick(0x1A1A1A, 0xFFFFFF);
    public static Color SubText => Pick(0x5F5F5F, 0xC5C5C5);
    public static Color Muted => Pick(0x8A8A8A, 0x969696);
    public static Color Accent => Pick(0x0067C0, 0x4CC2FF);
    public static Color OnAccent => Pick(0xFFFFFF, 0x000000);
    public static Color Danger => Pick(0xC42B1C, 0xFF99A4);

    public static readonly string UiFont = "Segoe UI";
    public static readonly string MonoFont =
        FontFamily.Families.Any(f => f.Name == "Cascadia Mono") ? "Cascadia Mono" : "Consolas";

    public static GraphicsPath RoundRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

static class Icons
{
    static readonly string Family =
        FontFamily.Families.Any(f => f.Name == "Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets";

    public static Font Font(float size) => new(Family, size);

    public const string Delete = "";
    public const string Forward = "";
    public const string Info = "";
}

static class IconFactory
{
    /// <summary>Tray/window icon: an "ë" keycap, blue when on and gray when off.</summary>
    public static Icon Create(bool on)
    {
        using var bmp = CreateBitmap(32, on);
        return Icon.FromHandle(bmp.GetHicon());
    }

    public static Bitmap CreateBitmap(int size, bool on)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var fill = new SolidBrush(on ? Color.FromArgb(0, 103, 192) : Color.FromArgb(120, 120, 120));
        using var path = Theme.RoundRect(new RectangleF(1, 1, size - 2, size - 2), size * 0.22f);
        g.FillPath(fill, path);
        using var font = new Font(Theme.UiFont, size * 0.62f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("ë", font, Brushes.White, new RectangleF(0, -size * 0.03f, size, size), format);
        return bmp;
    }
}
