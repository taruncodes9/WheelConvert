using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using WheelConvert.Core.Models;
using WheelConvert.Core.Services;
using WheelConvert.Native;
using WheelConvert.ViewModels;
using Ellipse = System.Windows.Shapes.Ellipse;
using WpfPath = System.Windows.Shapes.Path;

namespace WheelConvert.Views;

public partial class RadialMenuWindow : Window
{
    private const double Pad = 28;            // room for shadow + scale overshoot
    private const double GapDegrees = 3;

    private readonly RadialMenuViewModel _vm;
    private readonly AppSettings _settings;
    private readonly double _outer, _inner, _size;
    private readonly List<WpfPath> _paths = new();
    private readonly DispatcherTimer _dwell = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private readonly Brush _baseBrush, _hoverBrush, _strokeBrush, _discBrush, _discStroke, _centerBrush;
    private readonly Effect _shadow = new DropShadowEffect { BlurRadius = 26, ShadowDepth = 0, Opacity = 0.45, Color = Colors.Black };
    private static readonly FontFamily IconFont = new("Segoe Fluent Icons, Segoe MDL2 Assets");
    private static readonly FontFamily TextFont = new("Segoe UI Variable Text, Segoe UI");

    private TextBlock? _centerText;
    private Storyboard? _current;
    private bool _closing;
    private int _centerPxX, _centerPxY, _radiusPx;

    public bool IsWheelVisible { get; private set; }
    public event Action? DismissRequested;

    public RadialMenuWindow(RadialMenuViewModel vm, AppSettings settings)
    {
        InitializeComponent();
        _vm = vm;
        _settings = settings;

        _outer = Math.Clamp(settings.WheelSize, 240, 640) / 2.0;
        _inner = _outer * 0.38;
        _size = _outer * 2 + Pad * 2;
        Width = Height = _size;
        WheelCanvas.Width = WheelCanvas.Height = _size;

        var accent = ParseColor(settings.AccentColor, Color.FromRgb(0xFF, 0x7A, 0x3D));
        var peach = Lerp(accent, Colors.White, 0.6);
        var hot = Lerp(accent, Color.FromRgb(0xFF, 0x3B, 0x30), 0.35);
        _baseBrush = Freeze(new SolidColorBrush(Color.FromArgb(0x66, peach.R, peach.G, peach.B)));
        _hoverBrush = Freeze(new SolidColorBrush(Color.FromArgb(0xE8, hot.R, hot.G, hot.B)));
        _strokeBrush = Freeze(new SolidColorBrush(Color.FromArgb(0x88, 255, 255, 255)));
        _discBrush = Freeze(new SolidColorBrush(Color.FromArgb(settings.UseAcrylic ? (byte)0x30 : (byte)0x9A, 0x1C, 0x1C, 0x26)));
        _discStroke = Freeze(new SolidColorBrush(Color.FromArgb(0x55, 255, 255, 255)));
        _centerBrush = Freeze(new SolidColorBrush(Color.FromArgb(0xD8, 0x14, 0x14, 0x1A)));

        _vm.PropertyChanged += OnVmChanged;
        _dwell.Tick += (_, _) => { _dwell.Stop(); if (IsConvertHovered()) _vm.OpenFormats(); };

        Root.MouseMove += OnMouseMove;
        Root.MouseLeave += (_, _) => { _dwell.Stop(); _vm.SetHovered(-1); };
        Root.MouseLeftButtonUp += OnMouseUp;
    }

    // ---------- window plumbing ----------

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;

        // Never steal focus, never show in Alt-Tab.
        var ex = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, ex | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW);

        // Optional acrylic; the translucent fill is the fallback either way.
        if (_settings.UseAcrylic)
            WindowBlur.TryEnableAcrylic(hwnd, 0x40, 0x1C, 0x1C, 0x26);
    }

    /// <summary>Is a physical-pixel screen point inside the wheel? Used by the click-outside hook.</summary>
    public bool ContainsScreenPoint(int x, int y)
    {
        var dx = x - _centerPxX; var dy = y - _centerPxY;
        return dx * dx + dy * dy <= (double)_radiusPx * _radiusPx;
    }

    /// <summary>Show the wheel centred on the cursor (physical px), clamped inside that monitor's work area.</summary>
    public void ShowAt(int cursorX, int cursorY)
    {
        _closing = false;
        _dwell.Stop();
        var hwnd = new WindowInteropHelper(this).EnsureHandle();

        var monitor = NativeMethods.MonitorFromPoint(new NativeMethods.POINT { X = cursorX, Y = cursorY }, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var work = GetWorkArea(monitor);
        var scale = GetMonitorScale(monitor);

        Rebuild();
        Show();
        Place(hwnd, cursorX, cursorY, scale, work);

        // If WPF re-scaled the window for the monitor's DPI (mixed-DPI setups), place again with its real scale.
        var actual = VisualTreeHelper.GetDpi(this).DpiScaleX;
        if (Math.Abs(actual - scale) > 0.01) Place(hwnd, cursorX, cursorY, actual, work);

        IsWheelVisible = true;
        Animate(show: true, null);
    }

    public void HideAnimated()
    {
        if (!IsWheelVisible || _closing) return;
        _closing = true;
        IsWheelVisible = false;
        _dwell.Stop();
        Animate(show: false, () => { Hide(); _closing = false; });
    }

    private void Place(IntPtr hwnd, int cursorX, int cursorY, double scale, NativeMethods.RECT work)
    {
        var sizePx = (int)Math.Round(_size * scale);
        var marginPx = (int)Math.Round(Pad * scale);   // the transparent padding may hang off-screen; the wheel itself may not

        var x = ClampRange(cursorX - sizePx / 2, work.Left - marginPx, work.Right - sizePx + marginPx);
        var y = ClampRange(cursorY - sizePx / 2, work.Top - marginPx, work.Bottom - sizePx + marginPx);

        // Scale/fade grows from the cursor point even when the window was nudged by clamping.
        Root.RenderTransformOrigin = new Point((cursorX - x) / (double)sizePx, (cursorY - y) / (double)sizePx);

        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, x, y, sizePx, sizePx,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

        _centerPxX = x + sizePx / 2;
        _centerPxY = y + sizePx / 2;
        _radiusPx = (int)Math.Round((_outer + 4) * scale);
    }

    private static int ClampRange(int v, int lo, int hi) => hi < lo ? (lo + hi) / 2 : Math.Clamp(v, lo, hi);

    private static double GetMonitorScale(IntPtr monitor)
    {
        try { if (NativeMethods.GetDpiForMonitor(monitor, 0, out var dpiX, out _) == 0 && dpiX > 0) return dpiX / 96.0; }
        catch { /* shcore missing: treat as 100% */ }
        return 1.0;
    }

    private static NativeMethods.RECT GetWorkArea(IntPtr monitor)
    {
        var mi = new NativeMethods.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        return NativeMethods.GetMonitorInfo(monitor, ref mi)
            ? mi.rcWork
            : new NativeMethods.RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
    }

    // ---------- animation ----------

    private void Animate(bool show, Action? completed)
    {
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(show ? 170 : 110);
        var sb = new Storyboard();

        void Add(string path, double from, double to)
        {
            var a = new DoubleAnimation(from, to, duration) { EasingFunction = ease };
            Storyboard.SetTarget(a, Root);
            Storyboard.SetTargetProperty(a, new PropertyPath(path));
            sb.Children.Add(a);
        }

        Add("(UIElement.RenderTransform).(ScaleTransform.ScaleX)", show ? 0.6 : 1.0, show ? 1.0 : 0.88);
        Add("(UIElement.RenderTransform).(ScaleTransform.ScaleY)", show ? 0.6 : 1.0, show ? 1.0 : 0.88);
        Add("Opacity", show ? 0.0 : 1.0, show ? 1.0 : 0.0);

        sb.Completed += (_, _) => { if (_current == sb) completed?.Invoke(); };
        _current = sb;
        sb.Begin(Root, HandoffBehavior.SnapshotAndReplace, true);
    }

    // ---------- keyboard (delivered by OverlayInputHook) ----------

    public void HandleKey(Key key)
    {
        switch (key)
        {
            case Key.Escape: DismissRequested?.Invoke(); break;
            case Key.Left or Key.Up: _vm.Move(-1); break;
            case Key.Right or Key.Down: _vm.Move(+1); break;
            case Key.Enter: _vm.ActivateHovered(); break;
            case Key.Back: if (_vm.Level == 2) _vm.Back(); else DismissRequested?.Invoke(); break;
            case Key.Tab: _vm.CycleMockKind(); break;   // step-2 test aid
        }
    }

    // ---------- mouse ----------

    private (double dx, double dy) Offset(MouseEventArgs e)
    {
        var p = e.GetPosition(WheelCanvas);
        return (p.X - _size / 2, p.Y - _size / 2);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var (dx, dy) = Offset(e);
        var idx = RadialGeometry.HitTest(dx, dy, _inner, _outer, _vm.Items.Count);
        if (idx == _vm.HoveredIndex) return;

        _vm.SetHovered(idx);
        _dwell.Stop();
        if (IsConvertHovered()) _dwell.Start();   // dwell ~400ms on Convert opens level 2
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        var (dx, dy) = Offset(e);
        if (RadialGeometry.IsInCenter(dx, dy, _inner))
        {
            if (_vm.Level == 2) _vm.Back();      // centre = "back"
            return;
        }
        var idx = RadialGeometry.HitTest(dx, dy, _inner, _outer, _vm.Items.Count);
        if (idx >= 0) _vm.Activate(idx);
    }

    private bool IsConvertHovered()
        => _vm.Level == 1 && _vm.HoveredIndex >= 0 && _vm.HoveredIndex < _vm.Items.Count && _vm.Items[_vm.HoveredIndex].Id == "convert";

    // ---------- drawing ----------

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RadialMenuViewModel.Items))
        {
            Rebuild();
            if (IsWheelVisible)   // quick fade so level changes feel intentional
                WheelCanvas.BeginAnimation(OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(140)));
        }
        else if (e.PropertyName is nameof(RadialMenuViewModel.HoveredIndex) or nameof(RadialMenuViewModel.CenterText))
            UpdateVisuals();
    }

    private void Rebuild()
    {
        WheelCanvas.Children.Clear();
        _paths.Clear();

        var items = _vm.Items;
        var n = items.Count;
        var c = _size / 2;

        // frosted-glass disc
        var disc = new Ellipse { Width = _outer * 2, Height = _outer * 2, Fill = _discBrush, Stroke = _discStroke, StrokeThickness = 1.2, Effect = _shadow };
        Canvas.SetLeft(disc, c - _outer); Canvas.SetTop(disc, c - _outer);
        WheelCanvas.Children.Add(disc);

        if (n >= 2)
        {
            var segInner = _inner + 4;
            var segOuter = _outer - 7;
            var mid = (segInner + segOuter) / 2;

            for (var i = 0; i < n; i++)
            {
                var path = new WpfPath
                {
                    Data = Geometry.Parse(RadialGeometry.SegmentPath(c, c, segInner, segOuter, i, n, GapDegrees)),
                    Fill = _baseBrush,
                    Stroke = _strokeBrush,
                    StrokeThickness = 1,
                    StrokeLineJoin = PenLineJoin.Round   // softly rounded corners
                };
                WheelCanvas.Children.Add(path);
                _paths.Add(path);

                var panel = new StackPanel { IsHitTestVisible = false };
                panel.Children.Add(new TextBlock
                {
                    Text = items[i].Glyph, FontFamily = IconFont, FontSize = n > 6 ? 21 : 26,
                    Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center
                });
                panel.Children.Add(new TextBlock
                {
                    Text = items[i].Label.ToUpperInvariant(), FontFamily = TextFont, FontSize = 9.5,
                    FontWeight = FontWeights.SemiBold, Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 3, 0, 0)
                });
                panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var p = RadialGeometry.PointAt(c, c, mid, RadialGeometry.SegmentCenterAngle(i, n));
                Canvas.SetLeft(panel, p.X - panel.DesiredSize.Width / 2);
                Canvas.SetTop(panel, p.Y - panel.DesiredSize.Height / 2);
                WheelCanvas.Children.Add(panel);
            }
        }

        // centre circle: current context / hovered option
        var cr = _inner - 4;
        _centerText = new TextBlock
        {
            FontFamily = TextFont, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
            TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(6)
        };
        var center = new Border
        {
            Width = cr * 2, Height = cr * 2, CornerRadius = new CornerRadius(cr),
            Background = _centerBrush, BorderBrush = _discStroke, BorderThickness = new Thickness(1),
            IsHitTestVisible = false, Child = _centerText
        };
        Canvas.SetLeft(center, c - cr); Canvas.SetTop(center, c - cr);
        WheelCanvas.Children.Add(center);

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        for (var i = 0; i < _paths.Count; i++)
            _paths[i].Fill = i == _vm.HoveredIndex ? _hoverBrush : _baseBrush;
        if (_centerText is not null) _centerText.Text = _vm.CenterText;
    }

    // ---------- helpers ----------

    private static T Freeze<T>(T f) where T : Freezable { f.Freeze(); return f; }

    private static Color ParseColor(string text, Color fallback)
    {
        try { return (Color)ColorConverter.ConvertFromString(text); } catch { return fallback; }
    }

    private static Color Lerp(Color a, Color b, double t)
        => Color.FromRgb((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));
}
