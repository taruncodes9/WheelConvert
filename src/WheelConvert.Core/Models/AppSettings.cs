namespace WheelConvert.Core.Models;

public sealed class AppSettings
{
    public string Hotkey { get; set; } = "Ctrl+Shift+Space";
    public string DragModifier { get; set; } = "Shift";      // Shift | Ctrl | Alt
    public string? OutputFolder { get; set; }                  // null = same as original
    public bool StartWithWindows { get; set; }
    public int WheelSize { get; set; } = 360;                  // diameter in DIPs
    public string AccentColor { get; set; } = "#FF7A3D";
    public bool Paused { get; set; }

    /// <summary>
    /// Try real acrylic via SetWindowCompositionAttribute. OFF by default: on a per-pixel-transparent
    /// (AllowsTransparency) WPF window the blur is either ignored or covers the whole square, so the
    /// default look is a translucent "glass" fill that always works. Flip to true to experiment.
    /// </summary>
    public bool UseAcrylic { get; set; }
}
