using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Watchdog.App;

internal static class IconFactory
{
    private static readonly string DogImagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "watchdog-dog.png");

    public static Icon CreateDogIcon()
    {
        using var bitmap = CreateDogImage(64);
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

    public static Bitmap CreateDogImage(int size)
    {
        if (File.Exists(DogImagePath))
        {
            using var source = Image.FromFile(DogImagePath);
            var bitmap = new Bitmap(size, size);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.Clear(Color.Transparent);

            var ratio = Math.Min((float)size / source.Width, (float)size / source.Height);
            var width = (int)(source.Width * ratio);
            var height = (int)(source.Height * ratio);
            var x = (size - width) / 2;
            var y = (size - height) / 2;
            graphics.DrawImage(source, x, y, width, height);

            return bitmap;
        }

        return CreateDogBitmap(size);
    }

    public static Bitmap CreateDogBitmap(int size)
    {
        var bitmap = new Bitmap(size, size);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var scale = size / 64f;
        graphics.ScaleTransform(scale, scale);

        using var shadowBrush = new SolidBrush(Color.FromArgb(55, 0, 0, 0));
        graphics.FillEllipse(shadowBrush, 14, 52, 36, 6);

        using var earBrush = new SolidBrush(Color.FromArgb(126, 87, 48));
        graphics.FillEllipse(earBrush, 9, 18, 17, 27);
        graphics.FillEllipse(earBrush, 38, 18, 17, 27);

        using var faceBrush = new SolidBrush(Color.FromArgb(222, 170, 101));
        graphics.FillEllipse(faceBrush, 15, 13, 34, 37);

        using var muzzleBrush = new SolidBrush(Color.FromArgb(248, 227, 188));
        graphics.FillEllipse(muzzleBrush, 22, 29, 20, 16);

        using var eyeBrush = new SolidBrush(Color.FromArgb(35, 31, 32));
        graphics.FillEllipse(eyeBrush, 24, 25, 5, 6);
        graphics.FillEllipse(eyeBrush, 35, 25, 5, 6);
        graphics.FillEllipse(eyeBrush, 30, 33, 5, 4);

        using var nosePen = new Pen(Color.FromArgb(35, 31, 32), 2)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawLine(nosePen, 32, 37, 32, 40);
        graphics.DrawArc(nosePen, 27, 37, 10, 8, 15, 150);

        using var badgeBrush = new SolidBrush(Color.FromArgb(44, 123, 229));
        graphics.FillEllipse(badgeBrush, 42, 42, 13, 13);

        using var badgePen = new Pen(Color.White, 2);
        graphics.DrawLine(badgePen, 45, 48, 48, 51);
        graphics.DrawLine(badgePen, 48, 51, 53, 45);

        return bitmap;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
