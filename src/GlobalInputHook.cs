using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WhipCursor;

public readonly record struct ScreenPoint(int X, int Y);

public sealed class GlobalInputHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WhKeyboardLl = 13;
    private const int WmLButtonDown = 0x0201;
    private const int WmKeyDown = 0x0100;
    private const int VkF7 = 0x76;
    private const int VkF8 = 0x77;
    private const int VkQ = 0x51;
    private const int VkControl = 0x11;
    private const int VkShift = 0x10;

    private readonly NativeMethods.HookProc _mouseProc;
    private readonly NativeMethods.HookProc _keyboardProc;
    private IntPtr _mouseHook;
    private IntPtr _keyboardHook;
    private bool _disposed;

    public GlobalInputHook()
    {
        _mouseProc = MouseHookCallback;
        _keyboardProc = KeyboardHookCallback;
    }

    public event Action<ScreenPoint>? LeftMouseDown;
    public event Action? F7Pressed;
    public event Action? F8Pressed;
    public event Action? CtrlShiftQPressed;

    public void Start()
    {
        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        var moduleHandle = NativeMethods.GetModuleHandle(currentModule?.ModuleName);

        _mouseHook = NativeMethods.SetWindowsHookEx(WhMouseLl, _mouseProc, moduleHandle, 0);
        _keyboardHook = NativeMethods.SetWindowsHookEx(WhKeyboardLl, _keyboardProc, moduleHandle, 0);

        if (_mouseHook == IntPtr.Zero || _keyboardHook == IntPtr.Zero)
        {
            throw new InvalidOperationException("Could not start the global input hooks.");
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WmLButtonDown)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.Msllhookstruct>(lParam);
            LeftMouseDown?.Invoke(new ScreenPoint(hookStruct.Point.X, hookStruct.Point.Y));
        }

        return NativeMethods.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == WmKeyDown)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.Kbdllhookstruct>(lParam);

            switch (hookStruct.VirtualKeyCode)
            {
                case VkF7:
                    F7Pressed?.Invoke();
                    break;
                case VkF8:
                    F8Pressed?.Invoke();
                    break;
                case VkQ when IsKeyDown(VkControl) && IsKeyDown(VkShift):
                    CtrlShiftQPressed?.Invoke();
                    break;
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private static bool IsKeyDown(int virtualKey)
    {
        return (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }

        if (_keyboardHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
    }
}
