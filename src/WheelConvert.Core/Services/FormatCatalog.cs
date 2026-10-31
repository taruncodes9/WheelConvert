using WheelConvert.Core.Models;

namespace WheelConvert.Core.Services;

/// <summary>Valid target formats per input type (drives the level-2 wheel). Segment count adapts to the list length.</summary>
public static class FormatCatalog
{
    private static readonly string[] ImageTargets = { "jpg", "png", "webp", "heic", "tiff", "avif", "bmp", "pdf", "docx" };
    private static readonly string[] PdfTargets = { "docx", "jpg", "png", "txt" };
    private static readonly string[] DocxTargets = { "pdf", "txt" };

    public static IReadOnlyList<WheelItem> TargetsFor(FileKind kind, string? sourceExtension = null)
    {
        var src = FileKinds.NormalizeExtension(sourceExtension);
        IEnumerable<string> ids = kind switch
        {
            FileKind.Image => ImageTargets,
            FileKind.Pdf => PdfTargets,
            FileKind.Docx => DocxTargets,
            _ => Array.Empty<string>()
        };
        // never offer "convert to what it already is"
        return ids.Where(id => id != src)
                  .Select(id => new WheelItem(id, id.ToUpperInvariant(), GlyphFor(id)))
                  .ToList();
    }

    private static string GlyphFor(string id) => id switch
    {
        "pdf" or "docx" or "txt" => "\uE8A5",   // page
        _ => "\uEB9F"                            // photo
    };
}
