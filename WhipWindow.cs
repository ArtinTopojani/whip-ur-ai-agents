using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace WhipCursor;

public sealed class WhipWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const double PlaybackScale = 0.8;

    private readonly WhipAnimation _animation;
    private readonly System.Windows.Controls.Image _image;
    private readonly DispatcherTimer _frameTimer;
    private readonly DispatcherTimer _followTimer;
    private int _frameIndex;

    public WhipWindow(WhipAnimation animation, ScreenPoint hitPoint)
    {
        _animation = animation;

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        Width = animation.Width * PlaybackScale;
        Height = animation.Height * PlaybackScale;
        PositionAt(hitPoint);

        _image = new System.Windows.Controls.Image
        {
            Source = animation.Frames[0],
            Stretch = Stretch.Fill,
            Width = Width,
            Height = Height
        };

        Content = _image;

        _frameTimer = new DispatcherTimer
        {
            Interval = animation.FrameDuration
        };
        _frameTimer.Tick += (_, _) => ShowNextFrame();

        _followTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(8)
        };
        _followTimer.Tick += (_, _) => FollowCursor();

        SourceInitialized += (_, _) => MakeClickThrough();
        Loaded += (_, _) =>
        {
            _frameTimer.Start();
            _followTimer.Start();
        };
        Closed += (_, _) =>
        {
            _frameTimer.Stop();
            _followTimer.Stop();
        };
    }

    private void ShowNextFrame()
    {
        _frameIndex++;
        if (_frameIndex >= _animation.Frames.Count)
        {
            Close();
            return;
        }

        _image.Source = _animation.Frames[_frameIndex];
    }

    private void FollowCursor()
    {
        var cursor = System.Windows.Forms.Cursor.Position;
        PositionAt(new ScreenPoint(cursor.X, cursor.Y));
    }

    private void PositionAt(ScreenPoint point)
    {
        Left = point.X - Width + 36;
        Top = point.Y - (Height / 2);
    }

    private void MakeClickThrough()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLong(handle, GwlExStyle);
        NativeMethods.SetWindowLong(handle, GwlExStyle, style | WsExTransparent | WsExToolWindow);
    }
}
