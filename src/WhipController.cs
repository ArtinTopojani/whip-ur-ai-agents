using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace WhipCursor;

public sealed class WhipController : IDisposable
{
    private static readonly TimeSpan WhipSoundDelay = TimeSpan.FromSeconds(0.6);

    private readonly WpfApplication _app;
    private readonly GlobalInputHook _inputHook;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _armedItem;
    private readonly ToolStripMenuItem _muteSfxItem;
    private readonly string _framesPath;
    private readonly string _sfxPath;
    private readonly WhipAnimation _animation;
    private readonly List<MediaPlayer> _activeSounds = [];
    private bool _disposed;

    public WhipController(WpfApplication app)
    {
        _app = app;
        _framesPath = FindAssetPath("Assets", "Video");
        _sfxPath = FindAssetPath("Assets", "Sfx", "whipsfx.mp3");
        _animation = PngAnimationLoader.Load(_framesPath);

        _armedItem = new ToolStripMenuItem("Armed", null, (_, _) => ToggleArmed())
        {
            Checked = true,
            CheckOnClick = false
        };

        _muteSfxItem = new ToolStripMenuItem("Mute SFX", null, (_, _) => ToggleSfxMuted())
        {
            Checked = false,
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
        _trayIcon.ContextMenuStrip.Items.Add(_muteSfxItem);
        _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        _trayIcon.ContextMenuStrip.Items.Add(exitItem);
        _trayIcon.DoubleClick += (_, _) => ToggleArmed();

        _inputHook = new GlobalInputHook();
        _inputHook.LeftMouseDown += HandleLeftMouseDown;
        _inputHook.F7Pressed += ToggleSfxMuted;
        _inputHook.F8Pressed += ToggleArmed;
        _inputHook.CtrlShiftQPressed += () => _app.Dispatcher.Invoke(_app.Shutdown);

        _inputHook.Start();
        _app.Exit += (_, _) => Dispose();

        _trayIcon.ShowBalloonTip(
            2500,
            "Whip Cursor is ready",
            "Left-click to whip. F7 mutes SFX. F8 arms/disarms. Ctrl+Shift+Q exits.",
            ToolTipIcon.Info);
    }

    private bool IsArmed
    {
        get => _armedItem.Checked;
        set
        {
            _armedItem.Checked = value;
            UpdateTrayText();
        }
    }

    private bool IsSfxMuted
    {
        get => _muteSfxItem.Checked;
        set
        {
            _muteSfxItem.Checked = value;
            if (value)
            {
                StopActiveSounds();
            }

            UpdateTrayText();
        }
    }

    private void ToggleArmed()
    {
        _app.Dispatcher.BeginInvoke(() => IsArmed = !IsArmed);
    }

    private void ToggleSfxMuted()
    {
        _app.Dispatcher.BeginInvoke(() => IsSfxMuted = !IsSfxMuted);
    }

    private void UpdateTrayText()
    {
        var armedState = IsArmed ? "armed" : "disarmed";
        var soundState = IsSfxMuted ? ", SFX muted" : "";
        _trayIcon.Text = $"Whip Cursor - {armedState}{soundState}";
    }

    private void HandleLeftMouseDown(ScreenPoint point)
    {
        if (!IsArmed)
        {
            return;
        }

        _app.Dispatcher.BeginInvoke(() =>
        {
            PlaySfxAfterDelay();
            var window = new WhipWindow(_animation, point);
            window.Show();
        });
    }

    private async void PlaySfxAfterDelay()
    {
        await Task.Delay(WhipSoundDelay);
        if (!_disposed && !IsSfxMuted)
        {
            PlaySfx();
        }
    }

    private void PlaySfx()
    {
        if (IsSfxMuted || !File.Exists(_sfxPath))
        {
            return;
        }

        var player = new MediaPlayer();
        player.Open(new Uri(_sfxPath, UriKind.Absolute));
        player.MediaEnded += (_, _) => CleanupSound(player);
        player.MediaFailed += (_, _) => CleanupSound(player);
        _activeSounds.Add(player);
        player.Play();
    }

    private void CleanupSound(MediaPlayer player)
    {
        player.Close();
        _activeSounds.Remove(player);
    }

    private void StopActiveSounds()
    {
        foreach (var player in _activeSounds.ToList())
        {
            CleanupSound(player);
        }
    }

    private static string FindAssetPath(params string[] parts)
    {
        var outputPath = Path.Combine([AppContext.BaseDirectory, .. parts]);
        if (Directory.Exists(outputPath) || File.Exists(outputPath))
        {
            return outputPath;
        }

        return Path.Combine([Environment.CurrentDirectory, .. parts]);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _inputHook.Dispose();
        StopActiveSounds();

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}
