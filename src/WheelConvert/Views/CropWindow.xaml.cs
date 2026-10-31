using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using WheelConvert.Core.Services;

namespace WheelConvert.Views;

public partial class CropWindow : Window
{
    private readonly string _filePath;
    private readonly string? _outputFolder;
    private int _imgWidth, _imgHeight;

    // Selection state (in canvas coordinates)
    private Point _start;
    private bool _dragging;
    private double _imageLeft, _imageTop, _imageScale;

    /// <summary>Raised when crop is complete, with the output path.</summary>
    public event Action<string>? CropCompleted;

    public CropWindow(string filePath, string? outputFolder)
    {
        InitializeComponent();
        _filePath = filePath;
        _outputFolder = outputFolder;
        Title = $"Crop — {System.IO.Path.GetFileName(filePath)}";

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var (w, h) = ImageProcessor.GetDimensions(_filePath);
            _imgWidth = w;
            _imgHeight = h;
            DimensionsText.Text = $"({w} × {h})";

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(_filePath, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            Preview.Source = bmp;

            // Fit image in canvas
            FitImage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Cannot load image:\n{ex.Message}", "Crop Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void FitImage()
    {
        var canvasW = ImageCanvas.ActualWidth;
        var canvasH = ImageCanvas.ActualHeight;
        if (canvasW <= 0 || canvasH <= 0 || _imgWidth <= 0 || _imgHeight <= 0) return;

        _imageScale = Math.Min(canvasW / _imgWidth, canvasH / _imgHeight);
        var dispW = _imgWidth * _imageScale;
        var dispH = _imgHeight * _imageScale;
        _imageLeft = (canvasW - dispW) / 2;
        _imageTop = (canvasH - dispH) / 2;

        Canvas.SetLeft(Preview, _imageLeft);
        Canvas.SetTop(Preview, _imageTop);
        Preview.Width = dispW;
        Preview.Height = dispH;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        FitImage();
    }

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(ImageCanvas);
        _dragging = true;
        SelectionRect.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRect, _start.X);
        Canvas.SetTop(SelectionRect, _start.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;
        CropButton.IsEnabled = false;
        ImageCanvas.CaptureMouse();
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var pos = e.GetPosition(ImageCanvas);
        var x = Math.Min(_start.X, pos.X);
        var y = Math.Min(_start.Y, pos.Y);
        var w = Math.Abs(pos.X - _start.X);
        var h = Math.Abs(pos.Y - _start.Y);

        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;

        // Show pixel dimensions of selection
        var (px, py, pw, ph) = ToImagePixels(x, y, w, h);
        DimensionsText.Text = $"Selection: {pw} × {ph} px (of {_imgWidth} × {_imgHeight})";
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        ImageCanvas.ReleaseMouseCapture();

        var x = Canvas.GetLeft(SelectionRect);
        var y = Canvas.GetTop(SelectionRect);
        var w = SelectionRect.Width;
        var h = SelectionRect.Height;
        var (_, _, pw, ph) = ToImagePixels(x, y, w, h);
        CropButton.IsEnabled = pw > 1 && ph > 1;
    }

    private (int x, int y, int w, int h) ToImagePixels(double cx, double cy, double cw, double ch)
    {
        if (_imageScale <= 0) return (0, 0, 0, 0);
        var px = (int)Math.Round((cx - _imageLeft) / _imageScale);
        var py = (int)Math.Round((cy - _imageTop) / _imageScale);
        var pw = (int)Math.Round(cw / _imageScale);
        var ph = (int)Math.Round(ch / _imageScale);
        // Clamp
        px = Math.Clamp(px, 0, _imgWidth);
        py = Math.Clamp(py, 0, _imgHeight);
        pw = Math.Clamp(pw, 0, _imgWidth - px);
        ph = Math.Clamp(ph, 0, _imgHeight - py);
        return (px, py, pw, ph);
    }

    private void Crop_Click(object sender, RoutedEventArgs e)
    {
        var x = Canvas.GetLeft(SelectionRect);
        var y = Canvas.GetTop(SelectionRect);
        var w = SelectionRect.Width;
        var h = SelectionRect.Height;
        var (px, py, pw, ph) = ToImagePixels(x, y, w, h);

        if (pw <= 0 || ph <= 0)
        {
            MessageBox.Show("Selection is too small.", "Crop", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var ext = FileKinds.NormalizeExtension(System.IO.Path.GetExtension(_filePath));
            var suffix = "_cropped";
            var baseName = System.IO.Path.GetFileNameWithoutExtension(_filePath) + suffix;
            var dir = string.IsNullOrWhiteSpace(_outputFolder)
                ? System.IO.Path.GetDirectoryName(_filePath) ?? ""
                : _outputFolder;
            var desired = System.IO.Path.Combine(dir, baseName + "." + ext);
            var output = FileNamer.GetUniquePath(desired);

            ImageProcessor.Crop(_filePath, output, new SKRectI(px, py, px + pw, py + ph));
            CropCompleted?.Invoke(output);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Crop failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}

