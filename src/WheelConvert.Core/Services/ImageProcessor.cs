using SkiaSharp;

namespace WheelConvert.Core.Services;

/// <summary>
/// Image processing operations using SkiaSharp (MIT license).
/// Supports: convert between formats, compress, crop, add background, resize.
/// </summary>
public static class ImageProcessor
{
    /// <summary>Convert an image file to a different format.</summary>
    public static void Convert(string sourcePath, string outputPath, string targetFormat, int quality = 90)
    {
        using var input = SKBitmap.Decode(sourcePath);
        if (input is null) throw new InvalidOperationException($"Cannot decode image: {sourcePath}");

        var format = ParseFormat(targetFormat);
        Save(input, outputPath, format, quality);
    }

    /// <summary>Compress an image by re-encoding at a lower quality.</summary>
    public static void Compress(string sourcePath, string outputPath, int quality = 70)
    {
        using var input = SKBitmap.Decode(sourcePath);
        if (input is null) throw new InvalidOperationException($"Cannot decode image: {sourcePath}");

        var ext = FileKinds.NormalizeExtension(Path.GetExtension(outputPath));
        var format = ParseFormat(ext);
        Save(input, outputPath, format, quality);
    }

    /// <summary>Crop an image to the given rectangle (in pixels).</summary>
    public static void Crop(string sourcePath, string outputPath, SKRectI cropRect, int quality = 92)
    {
        using var input = SKBitmap.Decode(sourcePath);
        if (input is null) throw new InvalidOperationException($"Cannot decode image: {sourcePath}");

        // Clamp the crop rect to image bounds
        var rect = new SKRectI(
            Math.Max(0, cropRect.Left),
            Math.Max(0, cropRect.Top),
            Math.Min(input.Width, cropRect.Right),
            Math.Min(input.Height, cropRect.Bottom));

        if (rect.Width <= 0 || rect.Height <= 0)
            throw new ArgumentException("Crop rectangle is empty or outside image bounds.");

        using var cropped = new SKBitmap(rect.Width, rect.Height);
        using (var canvas = new SKCanvas(cropped))
        {
            canvas.DrawBitmap(input, rect, new SKRect(0, 0, rect.Width, rect.Height));
        }

        var ext = FileKinds.NormalizeExtension(Path.GetExtension(outputPath));
        Save(cropped, outputPath, ParseFormat(ext), quality);
    }

    /// <summary>Replace transparent pixels with a solid background color.</summary>
    public static void AddBackground(string sourcePath, string outputPath, SKColor backgroundColor, int quality = 92)
    {
        using var input = SKBitmap.Decode(sourcePath);
        if (input is null) throw new InvalidOperationException($"Cannot decode image: {sourcePath}");

        using var result = new SKBitmap(input.Width, input.Height);
        using (var canvas = new SKCanvas(result))
        {
            canvas.Clear(backgroundColor);
            canvas.DrawBitmap(input, 0, 0);
        }

        var ext = FileKinds.NormalizeExtension(Path.GetExtension(outputPath));
        Save(result, outputPath, ParseFormat(ext), quality);
    }

    /// <summary>Get image dimensions without fully decoding.</summary>
    public static (int Width, int Height) GetDimensions(string path)
    {
        using var codec = SKCodec.Create(path);
        if (codec is null) return (0, 0);
        return (codec.Info.Width, codec.Info.Height);
    }

    /// <summary>Decode an image file to an SKBitmap for display (e.g. in crop preview).</summary>
    public static SKBitmap? Decode(string path) => SKBitmap.Decode(path);

    // ---------- internal ----------

    private static void Save(SKBitmap bitmap, string outputPath, SKEncodedImageFormat format, int quality)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "");
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality);
        if (data is null) throw new InvalidOperationException($"Failed to encode image as {format}");
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
    }

    private static SKEncodedImageFormat ParseFormat(string ext) => ext.ToLowerInvariant() switch
    {
        "jpg" or "jpeg" => SKEncodedImageFormat.Jpeg,
        "png" => SKEncodedImageFormat.Png,
        "webp" => SKEncodedImageFormat.Webp,
        "bmp" => SKEncodedImageFormat.Bmp,
        "heic" or "heif" => SKEncodedImageFormat.Heif,
        "avif" => SKEncodedImageFormat.Avif,
        _ => SKEncodedImageFormat.Png
    };
}

