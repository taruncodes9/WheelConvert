namespace WheelConvert.Core.Services;

/// <summary>Builds output paths and never overwrites: adds " (2)", " (3)"... on collision.</summary>
public static class FileNamer
{
    public static string GetUniquePath(string desiredPath, Func<string, bool>? exists = null)
    {
        exists ??= p => File.Exists(p) || Directory.Exists(p);
        if (!exists(desiredPath)) return desiredPath;

        var dir = Path.GetDirectoryName(desiredPath) ?? "";
        var name = Path.GetFileNameWithoutExtension(desiredPath);
        var ext = Path.GetExtension(desiredPath);
        for (var i = 2; ; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!exists(candidate)) return candidate;
        }
    }

    /// <summary>Same folder (or outputFolder) as source, new extension. Extension may be given with or without dot.</summary>
    public static string GetOutputPath(string sourcePath, string newExtension, string? outputFolder = null,
                                       Func<string, bool>? exists = null)
    {
        if (!newExtension.StartsWith('.')) newExtension = "." + newExtension;
        var dir = string.IsNullOrWhiteSpace(outputFolder) ? Path.GetDirectoryName(sourcePath) ?? "" : outputFolder;
        var desired = Path.Combine(dir, Path.GetFileNameWithoutExtension(sourcePath) + newExtension);
        return GetUniquePath(desired, exists);
    }
}
