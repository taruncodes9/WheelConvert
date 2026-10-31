using WheelConvert.Core.Models;

namespace WheelConvert.Core.Services;

public static class FileKinds
{
    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif", ".tif", ".tiff", ".avif", ".bmp" };

    public static FileKind FromPath(string path)
    {
        var ext = Path.GetExtension(path);
        if (ImageExts.Contains(ext)) return FileKind.Image;
        if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase)) return FileKind.Pdf;
        if (ext.Equals(".docx", StringComparison.OrdinalIgnoreCase)) return FileKind.Docx;
        return FileKind.Unknown;
    }

    /// <summary>"JPEG"/".jpeg" -> "jpg", ".tif" -> "tiff", ".heif" -> "heic", ".PNG" -> "png".</summary>
    public static string NormalizeExtension(string? ext)
    {
        var e = (ext ?? "").Trim().TrimStart('.').ToLowerInvariant();
        return e switch { "jpeg" => "jpg", "tif" => "tiff", "heif" => "heic", _ => e };
    }
}
