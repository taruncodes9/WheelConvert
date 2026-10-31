namespace WheelConvert.Core.Models;

public enum FileKind { Unknown, Image, Pdf, Docx }

/// <summary>One wheel segment. Glyph is a Segoe Fluent Icons / MDL2 character.</summary>
public sealed record WheelItem(string Id, string Label, string Glyph);

/// <summary>Raised when the user picks a segment. Level 1 = main wheel, 2 = format wheel.</summary>
public sealed record WheelAction(int Level, string Id, string Label, FileKind Kind, string? SourceExtension);

/// <summary>The file(s) the wheel is operating on, read from Explorer or drag-drop.</summary>
public sealed class FileContext
{
    public IReadOnlyList<string> Paths { get; }
    public FileKind Kind { get; }
    public string? Extension { get; }

    public FileContext(IReadOnlyList<string> paths)
    {
        Paths = paths;
        if (paths.Count > 0)
        {
            var first = paths[0];
            Kind = Services.FileKinds.FromPath(first);
            Extension = Services.FileKinds.NormalizeExtension(System.IO.Path.GetExtension(first));
        }
        else
        {
            Kind = FileKind.Unknown;
            Extension = null;
        }
    }

    public bool IsEmpty => Paths.Count == 0;
    public string FirstPath => Paths.Count > 0 ? Paths[0] : "";
}
