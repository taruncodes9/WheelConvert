namespace WheelConvert.Core.Services;

/// <summary>Values match Win32 MOD_ALT / MOD_CONTROL / MOD_SHIFT / MOD_WIN.</summary>
[Flags]
public enum HotkeyModifiers { None = 0, Alt = 1, Control = 2, Shift = 4, Win = 8 }

public sealed record HotkeySpec(HotkeyModifiers Modifiers, string Key);

public static class HotkeyParser
{
    public static bool TryParse(string? text, out HotkeySpec spec)
    {
        spec = new HotkeySpec(HotkeyModifiers.None, "");
        if (string.IsNullOrWhiteSpace(text)) return false;

        var mods = HotkeyModifiers.None;
        string? key = null;
        foreach (var raw in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl" or "control": mods |= HotkeyModifiers.Control; break;
                case "alt": mods |= HotkeyModifiers.Alt; break;
                case "shift": mods |= HotkeyModifiers.Shift; break;
                case "win" or "windows" or "meta": mods |= HotkeyModifiers.Win; break;
                default:
                    if (key is not null) return false;   // two non-modifier keys
                    key = raw.Length == 1 && char.IsDigit(raw[0]) ? "D" + raw : raw; // "1" -> WPF Key.D1
                    break;
            }
        }
        if (key is null || mods == HotkeyModifiers.None) return false;
        spec = new HotkeySpec(mods, key);
        return true;
    }

    public static string Format(HotkeyModifiers mods, string key)
    {
        var parts = new List<string>();
        if (mods.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (mods.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (mods.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(key);
        return string.Join("+", parts);
    }
}
