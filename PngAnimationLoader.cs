using System.IO;
using System.Windows.Media.Imaging;

namespace WhipCursor;

public static class PngAnimationLoader
{
    private static readonly TimeSpan DefaultFrameDuration = TimeSpan.FromMilliseconds(1000.0 / 30.0);

    public static WhipAnimation Load(string framesFolder)
    {
        if (!Directory.Exists(framesFolder))
        {
            throw new DirectoryNotFoundException($"Could not find the PNG sequence folder: {framesFolder}");
        }

        var framePaths = Directory
            .EnumerateFiles(framesFolder, "*.png")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (framePaths.Count == 0)
        {
            throw new InvalidDataException($"No PNG frames were found in: {framesFolder}");
        }

        var frames = new List<BitmapSource>(framePaths.Count);
        foreach (var framePath in framePaths)
        {
            frames.Add(LoadFrame(framePath));
        }

        return new WhipAnimation(
            frames[0].PixelWidth,
            frames[0].PixelHeight,
            DefaultFrameDuration,
            frames);
    }

    private static BitmapSource LoadFrame(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
