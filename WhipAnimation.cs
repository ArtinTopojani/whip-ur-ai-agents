using System.Windows.Media.Imaging;

namespace WhipCursor;

public sealed record WhipAnimation(
    int Width,
    int Height,
    TimeSpan FrameDuration,
    IReadOnlyList<BitmapSource> Frames);
