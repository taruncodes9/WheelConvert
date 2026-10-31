using System.Runtime.InteropServices;

namespace WheelConvert.Native;

internal static class WindowBlur
{
    private const int WCA_ACCENT_POLICY = 19;
    private const int ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

    /// <summary>Try acrylic blur-behind. Returns false on any failure so the caller keeps the plain translucent fallback.</summary>
    public static bool TryEnableAcrylic(IntPtr hwnd, byte a, byte r, byte g, byte b)
    {
        var ptr = IntPtr.Zero;
        try
        {
            var accent = new NativeMethods.AccentPolicy
            {
                AccentState = ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 2,
                GradientColor = (a << 24) | (b << 16) | (g << 8) | r   // AABBGGRR
            };
            var size = Marshal.SizeOf<NativeMethods.AccentPolicy>();
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(accent, ptr, false);
            var data = new NativeMethods.WindowCompositionAttribData { Attribute = WCA_ACCENT_POLICY, Data = ptr, SizeOfData = size };
            return NativeMethods.SetWindowCompositionAttribute(hwnd, ref data) != 0;
        }
        catch { return false; }
        finally { if (ptr != IntPtr.Zero) Marshal.FreeHGlobal(ptr); }
    }
}
