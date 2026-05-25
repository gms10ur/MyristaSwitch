using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MyristaSwitch.App;

internal static class BrandAssets
{
    public static readonly Color Primary = Color.FromArgb(13, 148, 136);
    public static readonly Color PrimaryDark = Color.FromArgb(15, 118, 110);
    public static readonly Color Accent = Color.FromArgb(79, 70, 229);
    public static readonly Color Warn = Color.FromArgb(217, 119, 6);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);

    public static ThemePalette CurrentTheme => IsDarkMode()
        ? new ThemePalette(
            BackColor: Color.FromArgb(32, 32, 32),
            SurfaceColor: Color.FromArgb(43, 43, 43),
            SurfaceAltColor: Color.FromArgb(52, 52, 52),
            TextColor: Color.FromArgb(245, 245, 245),
            MutedTextColor: Color.FromArgb(176, 176, 176),
            BorderColor: Color.FromArgb(72, 72, 72),
            ButtonColor: Color.FromArgb(62, 62, 62),
            ButtonTextColor: Color.White)
        : new ThemePalette(
            BackColor: SystemColors.Control,
            SurfaceColor: SystemColors.Window,
            SurfaceAltColor: Color.FromArgb(245, 247, 250),
            TextColor: SystemColors.ControlText,
            MutedTextColor: SystemColors.GrayText,
            BorderColor: Color.FromArgb(218, 220, 224),
            ButtonColor: SystemColors.ControlLight,
            ButtonTextColor: SystemColors.ControlText);

    public static Icon CreateIcon(int size = 64)
    {
        using var bitmap = CreateLogoBitmap(size);
        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    public static Bitmap CreateLogoBitmap(int size)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var padding = Math.Max(3, size / 12);
        var rect = new Rectangle(padding, padding, size - padding * 2, size - padding * 2);
        using var background = new LinearGradientBrush(rect, Primary, Accent, 35F);
        using var path = RoundedRectangle(rect, size / 5);
        graphics.FillPath(background, path);

        using var whitePen = new Pen(Color.White, Math.Max(3, size / 14))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        var y1 = size * 0.34F;
        var y2 = size * 0.66F;
        graphics.DrawLine(whitePen, size * 0.25F, y1, size * 0.74F, y1);
        graphics.DrawLine(whitePen, size * 0.26F, y2, size * 0.75F, y2);

        using var knobBrush = new SolidBrush(Color.White);
        graphics.FillEllipse(knobBrush, size * 0.58F, y1 - size * 0.11F, size * 0.22F, size * 0.22F);
        graphics.FillEllipse(knobBrush, size * 0.20F, y2 - size * 0.11F, size * 0.22F, size * 0.22F);

        return bitmap;
    }

    public static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static bool IsDarkMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}

internal sealed record ThemePalette(
    Color BackColor,
    Color SurfaceColor,
    Color SurfaceAltColor,
    Color TextColor,
    Color MutedTextColor,
    Color BorderColor,
    Color ButtonColor,
    Color ButtonTextColor);
