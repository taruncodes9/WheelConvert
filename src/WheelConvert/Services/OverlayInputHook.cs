using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;
using WheelConvert.Native;

namespace WheelConvert.Services;

/// <summary>
/// Installed ONLY while the wheel is visible. The wheel window never takes focus, so:
///  - keyboard hook: delivers Esc / arrows / Enter / Tab / Backspace to the wheel (and swallows them),
///  - mouse hook: reports a click outside the wheel so it can be dismissed (the click still goes through).
/// Callbacks do almost nothing and post work to the UI dispatcher.
/// </summary>
internal sealed class OverlayInputHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13, WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100, WM_SYSKEYDOWN = 0x0104;
    private const int WM_LBUTTONDOWN = 0x0201, WM_RBUTTONDOWN = 0x0204, WM_MBUTTONDOWN = 0x0207;

    private readonly NativeMethods.HookProc _keyboardProc;   // fields keep delegates alive (GC!)
    private readonly NativeMethods.HookProc _mouseProc;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private IntPtr _keyboardHook, _mouseHook;

    public event Action<Key>? KeyPressed;
    public event Action? OutsideClick;
    public Func<int, int, bool> IsInsideWheel { get; set; } = (_, _) => true;

    public OverlayInputHook()
    {
        _keyboardProc = KeyboardProc;
        _mouseProc = MouseProc;
    }

    public void Install()
    {
        if (_keyboardHook != IntPtr.Zero) return;
        var module = NativeMethods.GetModuleHandle(null);
        _keyboardHook = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, module, 0);
        _mouseHook = NativeMethods.SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, module, 0);
    }

    public void Uninstall()
    {
        if (_keyboardHook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_keyboardHook);
        if (_mouseHook != IntPtr.Zero) NativeMethods.UnhookWindowsHookEx(_mouseHook);
        _keyboardHook = _mouseHook = IntPtr.Zero;
    }

    private static Key Map(int vk) => vk switch
    {
        0x1B => Key.Escape,
        0x0D => Key.Enter,
        0x09 => Key.Tab,
        0x08 => Key.Back,
        0x25 => Key.Left,
        0x26 => Key.Up,
        0x27 => Key.Right,
        0x28 => Key.Down,
        _ => Key.None
    };

    private IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var key = Map(Marshal.ReadInt32(lParam));   // KBDLLHOOKSTRUCT.vkCode is the first field
            if (key != Key.None)
            {
                _dispatcher.BeginInvoke(new Action(() => KeyPressed?.Invoke(key)));
                return (IntPtr)1;                       // swallow: the app underneath never sees it
            }
        }
        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    private IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN || wParam == (IntPtr)WM_MBUTTONDOWN))
        {
            var x = Marshal.ReadInt32(lParam, 0);       // MSLLHOOKSTRUCT.pt.x / pt.y (physical pixels)
            var y = Marshal.ReadInt32(lParam, 4);
            if (!IsInsideWheel(x, y))
                _dispatcher.BeginInvoke(new Action(() => OutsideClick?.Invoke()));
        }
        return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
