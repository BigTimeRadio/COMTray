using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ComTray;

static class IconRenderer
{
    const int Size = 32;

    public static Icon Render(string text)
    {
        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            float em = text.Length >= 3 ? 17f : text.Length == 2 ? 24f : 27f;
            using var family = new FontFamily("Segoe UI");
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            using var path = new GraphicsPath();
            path.AddString(text, family, (int)FontStyle.Bold, em, new RectangleF(0, 0, Size, Size), format);

            using var outline = new Pen(Color.FromArgb(235, 0, 0, 0), 3.5f) { LineJoin = LineJoin.Round };
            g.DrawPath(outline, path);
            g.FillPath(Brushes.White, path);
        }

        IntPtr handle = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool DestroyIcon(IntPtr handle);
}
