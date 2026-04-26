using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace WhipCursor;

public sealed class WhipController : IDisposable
{
    private readonly WpfApplication _app;
    private readonly GlobalInputHook _inputHook;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _armedItem;
    private readonly string _videoPath;
    private readonly WhipAnimation _animation;
    private bool _disposed;

    public WhipController(WpfApplication app)
    {
        _app = app;
        _videoPath = FindVideoPath();
        _animation = QuickTimeAnimationDecoder.Load(_videoPath);

        _armedItem = new ToolStripMenuItem("Armed", null, (_, _) => ToggleArmed())
        {
            Checked = true,
            CheckOnClick = false
        };

        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => _app.Shutdown());

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Whip Cursor",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        _trayIcon.ContextMenuStrip.Items.Add(_armedItem);
        _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        _trayIcon.ContextMenuStrip.Items.Add(exitItem);
        _trayIcon.DoubleClick += (_, _) => ToggleArmed();

        _inputHook = new GlobalInputHook();
        _inputHook.LeftMouseDown += HandleLeftMouseDown;
        _inputHook.F8Pressed += ToggleArmed;
        _inputHook.CtrlShiftQPressed += () => _app.Dispatcher.Invoke(_app.Shutdown);

        _inputHook.Start();
        _app.Exit += (_, _) => Dispose();

        _trayIcon.ShowBalloonTip(
            2500,
            "Whip Cursor is ready",
            "Left-click to whip. F8 arms/disarms. Ctrl+Shift+Q exits.",
            ToolTipIcon.Info);
    }

    private bool IsArmed
    {
        get => _armedItem.Checked;
        set
        {
            _armedItem.Checked = value;
            _trayIcon.Text = value ? "Whip Cursor - armed" : "Whip Cursor - disarmed";
        }
    }

    private void ToggleArmed()
    {
        _app.Dispatcher.BeginInvoke(() => IsArmed = !IsArmed);
    }

    private void HandleLeftMouseDown(ScreenPoint point)
    {
        if (!IsArmed || !File.Exists(_videoPath))
        {
            return;
        }

        _app.Dispatcher.BeginInvoke(() =>
        {
            var window = new WhipWindow(_animation, point);
            window.Show();
        });
    }

    private static string FindVideoPath()
    {
        var basePath = Path.Combine(AppContext.BaseDirectory, "whip.mov");
        if (File.Exists(basePath))
        {
            return basePath;
        }

        return Path.Combine(Environment.CurrentDirectory, "whip.mov");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _inputHook.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}
