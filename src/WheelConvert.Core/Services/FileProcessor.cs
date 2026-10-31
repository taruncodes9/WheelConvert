using SkiaSharp;
using WheelConvert.Core.Models;

namespace WheelConvert.Core.Services;

/// <summary>
/// Dispatches a WheelAction to the appropriate processor for each selected file.
/// Returns (success count, error messages) for notification.
/// </summary>
public static class FileProcessor
{
    public sealed record ProcessResult(int SuccessCount, int TotalCount, List<string> Errors);

    /// <summary>Process all files in the context for the given action.</summary>
    public static ProcessResult Execute(WheelAction action, FileContext context, string? outputFolder = null)
    {
        var errors = new List<string>();
        int success = 0;

        foreach (var path in context.Paths)
        {
            try
            {
                if (action.Level == 2)
                {
                    // Level 2 = format conversion (action.Id is the target extension)
                    ConvertFile(path, action.Id, outputFolder);
                }
                else
                {
                    // Level 1 actions
                    switch (action.Id)
                    {
                        case "compress":
                            CompressFile(path, outputFolder);
                            break;
                        case "addbg":
                            AddBackground(path, outputFolder);
                            break;
                        case "crop":
                            if (FileKinds.FromPath(path) != FileKind.Image)
                                throw new NotSupportedException("Crop is only available for image files.");
                            // Crop requires user interaction (rectangle selection) —
                            // return a special marker so the controller can open the crop UI.
                            throw new CropRequiredException(path);
                        case "metadata":
                            // Metadata is view-only — handled by the controller opening a window.
                            throw new MetadataViewRequiredException(path);
                        case "redact":
                        case "annotate":
                        case "edit":
                            throw new NotImplementedException($"{action.Label} is not yet implemented (coming in a future update).");
                        default:
                            throw new NotSupportedException($"Unknown action: {action.Id}");
                    }
                }
                success++;
            }
            catch (CropRequiredException) { throw; }
            catch (MetadataViewRequiredException) { throw; }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(path)}: {ex.Message}");
            }
        }
        return new ProcessResult(success, context.Paths.Count, errors);
    }

    private static void ConvertFile(string sourcePath, string targetExt, string? outputFolder)
    {
        var kind = FileKinds.FromPath(sourcePath);
        var target = FileKinds.NormalizeExtension(targetExt);

        if (kind == FileKind.Image)
        {
            if (target == "pdf")
            {
                var output = FileNamer.GetOutputPath(sourcePath, "pdf", outputFolder);
                DocumentProcessor.ImageToPdf(sourcePath, output);
            }
            else if (target == "docx")
            {
                var output = FileNamer.GetOutputPath(sourcePath, "docx", outputFolder);
                DocumentProcessor.ImageToDocx(sourcePath, output);
            }
            else
            {
                var output = FileNamer.GetOutputPath(sourcePath, target, outputFolder);
                ImageProcessor.Convert(sourcePath, output, target);
            }
        }
        else if (kind == FileKind.Pdf)
        {
            if (target is "jpg" or "jpeg" or "png")
            {
                var output = FileNamer.GetOutputPath(sourcePath, target, outputFolder);
                DocumentProcessor.PdfToImages(sourcePath, output, target);
            }
            else if (target == "docx")
            {
                var output = FileNamer.GetOutputPath(sourcePath, "docx", outputFolder);
                DocumentProcessor.PdfToDocx(sourcePath, output);
            }
            else if (target == "txt")
            {
                var output = FileNamer.GetOutputPath(sourcePath, "txt", outputFolder);
                DocumentProcessor.PdfToTxtFile(sourcePath, output);
            }
            else
            {
                throw new NotSupportedException($"Converting PDF to {target.ToUpperInvariant()} is not supported.");
            }
        }
        else if (kind == FileKind.Docx)
        {
            if (target == "pdf")
            {
                var output = FileNamer.GetOutputPath(sourcePath, "pdf", outputFolder);
                DocumentProcessor.DocxToPdf(sourcePath, output);
            }
            else if (target == "txt")
            {
                var output = FileNamer.GetOutputPath(sourcePath, "txt", outputFolder);
                DocumentProcessor.DocxToTxtFile(sourcePath, output);
            }
            else
            {
                throw new NotSupportedException($"Converting DOCX to {target.ToUpperInvariant()} is not supported.");
            }
        }
        else
        {
            throw new NotSupportedException($"Unsupported file type: {Path.GetExtension(sourcePath)}");
        }
    }

    private static void CompressFile(string sourcePath, string? outputFolder)
    {
        var kind = FileKinds.FromPath(sourcePath);
        if (kind != FileKind.Image)
            throw new NotSupportedException("Compress currently works on images only.");

        var ext = FileKinds.NormalizeExtension(Path.GetExtension(sourcePath));
        // For lossless formats, convert to JPEG for compression; for lossy, re-encode at lower quality
        var targetExt = ext is "png" or "bmp" or "tiff" ? "jpg" : ext;
        var suffix = "_compressed";
        var baseName = Path.GetFileNameWithoutExtension(sourcePath) + suffix;
        var dir = string.IsNullOrWhiteSpace(outputFolder) ? Path.GetDirectoryName(sourcePath) ?? "" : outputFolder;
        var desired = Path.Combine(dir, baseName + "." + targetExt);
        var output = FileNamer.GetUniquePath(desired);
        ImageProcessor.Compress(sourcePath, output, quality: 65);
    }

    private static void AddBackground(string sourcePath, string? outputFolder)
    {
        var kind = FileKinds.FromPath(sourcePath);
        if (kind != FileKind.Image)
            throw new NotSupportedException("Add Background currently works on images only.");

        var ext = FileKinds.NormalizeExtension(Path.GetExtension(sourcePath));
        var suffix = "_bg";
        var baseName = Path.GetFileNameWithoutExtension(sourcePath) + suffix;
        var dir = string.IsNullOrWhiteSpace(outputFolder) ? Path.GetDirectoryName(sourcePath) ?? "" : outputFolder;
        var desired = Path.Combine(dir, baseName + "." + ext);
        var output = FileNamer.GetUniquePath(desired);
        ImageProcessor.AddBackground(sourcePath, output, SKColors.White);
    }
}

/// <summary>Signals that the crop action needs a UI for rectangle selection.</summary>
public sealed class CropRequiredException : Exception
{
    public string FilePath { get; }
    public CropRequiredException(string filePath) : base("Crop requires UI") => FilePath = filePath;
}

/// <summary>Signals that the metadata action needs a read-only viewer window.</summary>
public sealed class MetadataViewRequiredException : Exception
{
    public string FilePath { get; }
    public MetadataViewRequiredException(string filePath) : base("Metadata view") => FilePath = filePath;
}

