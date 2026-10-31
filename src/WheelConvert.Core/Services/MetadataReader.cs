using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileType;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Png;

namespace WheelConvert.Core.Services;

/// <summary>
/// Reads image/file metadata using MetadataExtractor (Apache 2.0).
/// Returns a flat list of key-value pairs suitable for display.
/// </summary>
public static class MetadataReader
{
    public sealed record MetadataEntry(string Category, string Name, string Value);

    public static List<MetadataEntry> Read(string filePath)
    {
        var entries = new List<MetadataEntry>();
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(filePath);
            foreach (var dir in directories)
            {
                foreach (var tag in dir.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag.Description))
                        entries.Add(new MetadataEntry(dir.Name, tag.Name, tag.Description));
                }
            }
        }
        catch (Exception ex)
        {
            entries.Add(new MetadataEntry("Error", "Could not read metadata", ex.Message));
        }
        return entries;
    }

    /// <summary>Returns a human-readable summary string for a quick balloon/popup.</summary>
    public static string GetSummary(string filePath)
    {
        var entries = Read(filePath);
        var lines = new List<string>();

        // File basics
        var fi = new FileInfo(filePath);
        lines.Add($"File: {fi.Name}");
        lines.Add($"Size: {FormatSize(fi.Length)}");

        // Image dimensions
        var dims = entries.FirstOrDefault(e => e.Name.Contains("Width", StringComparison.OrdinalIgnoreCase));
        var dimsH = entries.FirstOrDefault(e => e.Name.Contains("Height", StringComparison.OrdinalIgnoreCase));
        if (dims is not null && dimsH is not null)
            lines.Add($"Dimensions: {dims.Value} × {dimsH.Value}");

        // Camera
        var model = entries.FirstOrDefault(e => e.Name == "Model");
        if (model is not null) lines.Add($"Camera: {model.Value}");

        // Date
        var date = entries.FirstOrDefault(e => e.Name.Contains("Date/Time Original", StringComparison.OrdinalIgnoreCase));
        if (date is not null) lines.Add($"Taken: {date.Value}");

        return string.Join("\n", lines);
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
    };
}

