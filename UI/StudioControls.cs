using System.Drawing.Drawing2D;

namespace LangKinhHuKhong.Editer.UI;

/// <summary>Rounded dark panels/buttons for the TTSS-inspired studio theme.</summary>
internal static class StudioShape
{
    public static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 1 || bounds.Height <= 1) return path;
        var diameter = Math.Max(2, Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height)));
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class StudioCard : Panel
{
    public StudioCard()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        if (Width < 2 || Height < 2) return;
        using var path = StudioShape.Rounded(new Rectangle(0, 0, Width, Height), 13);
        var old = Region;
        Region = new Region(path);
        old?.Dispose();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(53, 71, 91), 1);
        using var path = StudioShape.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 13);
        e.Graphics.DrawPath(pen, path);
    }
}

internal sealed class StudioButton : Button
{
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Width < 2 || Height < 2) return;
        using var path = StudioShape.Rounded(new Rectangle(0, 0, Width, Height), 8);
        var old = Region;
        Region = new Region(path);
        old?.Dispose();
    }
}
