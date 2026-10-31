using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Interop;
using WheelConvert.Core.Services;
using WheelConvert.Native;

namespace WheelConvert.Services;

/// <summary>Global hotkey via RegisterHotKey on a hidden message-only window.</summary>
internal sealed class HotkeyManager : IDisposable
{
    private const int HotkeyId = 0x5743;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private readonly HwndSource _source;
    private bool _registered;

    public event Action? HotkeyPressed;

    public HotkeyManager()
    {
        var p = new HwndSourceParameters("WheelConvert.HotkeyWindow")
        {
            ParentWindow = HWND_MESSAGE,
            WindowStyle = 0,
            Width = 0,
            Height = 0
        };
        _source = new HwndSource(p);
        _source.AddHook(WndProc);
    }

    public bool Register(string hotkeyText, out string error)
    {
        Unregister();
        error = "";

        if (!HotkeyParser.TryParse(hotkeyText, out var spec) ||
            !Enum.TryParse<Key>(spec.Key, ignoreCase: true, out var key) || key == Key.None)
        {
            error = $"“{hotkeyText}” is not a valid hotkey. Open Settings to choose another.";
            return false;
        }

        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (!NativeMethods.RegisterHotKey(_source.Handle, HotkeyId, (uint)spec.Modifiers | NativeMethods.MOD_NOREPEAT, vk))
        {
            var code = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            error = code == 1409
                ? $"The hotkey {hotkeyText} is already used by another program. Pick a different one in Settings."
                : $"Could not register {hotkeyText}: {new Win32Exception(code).Message}";
            return false;
        }
        _registered = true;
        return true;
    }

    public void Unregister()
    {
        if (!_registered) return;
        NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
        _registered = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            HotkeyPressed?.Invoke();
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
