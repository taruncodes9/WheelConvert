using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WheelConvert.Services;

/// <summary>Notification-area icon + context menu (Settings / Pause / Quit).</summary>
public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Icon _appIcon;
    private readonly ToolStripMenuItem _pauseItem;

    public TrayIconService(Action onSettings, Action<bool> onPauseChanged, Action onQuit, bool initiallyPaused)
    {
        _pauseItem = new ToolStripMenuItem("Pause") { CheckOnClick = true, Checked = initiallyPaused };
        _pauseItem.CheckedChanged += (_, _) =>
        {
            UpdateTooltip();
            onPauseChanged(_pauseItem.Checked);
        };

        var menu = new ContextMenuStrip();
        var settings = new ToolStripMenuItem("Settings…");
        settings.Click += (_, _) => onSettings();
        var quit = new ToolStripMenuItem("Quit");
        quit.Click += (_, _) => onQuit();
        menu.Items.AddRange(new ToolStripItem[] { settings, _pauseItem, new ToolStripSeparator(), quit });

        _appIcon = CreateIcon();
        _icon = new NotifyIcon { Icon = _appIcon, ContextMenuStrip = menu, Visible = true };
        _icon.DoubleClick += (_, _) => onSettings();
        UpdateTooltip();
    }

    public bool IsPaused => _pauseItem.Checked;

    public void ShowBalloon(string title, string text)
        => _icon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);

    private void UpdateTooltip() => _icon.Text = _pauseItem.Checked ? "WheelConvert (paused)" : "WheelConvert";

    /// <summary>Draws a small orange pie-wheel icon at runtime so no binary asset is needed.</summary>
    private static Icon CreateIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var orange = new SolidBrush(Color.FromArgb(255, 255, 122, 61));
            using var light = new SolidBrush(Color.FromArgb(255, 255, 190, 150));
            using var hole = new SolidBrush(Color.FromArgb(255, 40, 40, 48));
            var rect = new Rectangle(2, 2, 28, 28);
            for (var i = 0; i < 6; i++)
                g.FillPie(i == 0 ? orange : light, rect, i * 60 + 2, 56);
            g.FillEllipse(hole, 10, 10, 12, 12);
        }
        var hIcon = bmp.GetHicon();
        try { using var tmp = Icon.FromHandle(hIcon); return (Icon)tmp.Clone(); }
        finally { DestroyIcon(hIcon); }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _appIcon.Dispose();
    }
}
