using WheelConvert.Core.Models;
using WheelConvert.Core.Services;
using Xunit;

namespace WheelConvert.Tests;

public class FormatCatalogTests
{
    private static string[] Ids(FileKind k, string? ext) => FormatCatalog.TargetsFor(k, ext).Select(i => i.Id).ToArray();

    [Fact]
    public void Jpg_Offers_TheSpecifiedImageTargets()
        => Assert.Equal(new[] { "png", "webp", "heic", "tiff", "avif", "bmp", "pdf", "docx" }, Ids(FileKind.Image, "jpg"));

    [Fact]
    public void Png_Offers_Jpg_But_NotPng()
    {
        var ids = Ids(FileKind.Image, ".PNG");
        Assert.Contains("jpg", ids);
        Assert.DoesNotContain("png", ids);
        Assert.Equal(8, ids.Length);
    }

    [Fact] public void Jpeg_IsTreatedAsJpg() => Assert.DoesNotContain("jpg", Ids(FileKind.Image, "jpeg"));
    [Fact] public void Pdf_Has4Targets() => Assert.Equal(new[] { "docx", "jpg", "png", "txt" }, Ids(FileKind.Pdf, "pdf"));
    [Fact] public void Docx_Has2Targets() => Assert.Equal(new[] { "pdf", "txt" }, Ids(FileKind.Docx, "docx"));
    [Fact] public void Unknown_HasNone() => Assert.Empty(Ids(FileKind.Unknown, "xyz"));

    [Theory]
    [InlineData(@"C:\a\x.JPG", FileKind.Image)]
    [InlineData(@"C:\a\x.heic", FileKind.Image)]
    [InlineData(@"C:\a\x.pdf", FileKind.Pdf)]
    [InlineData(@"C:\a\x.docx", FileKind.Docx)]
    [InlineData(@"C:\a\x.zip", FileKind.Unknown)]
    public void FileKinds_FromPath(string path, FileKind expected) => Assert.Equal(expected, FileKinds.FromPath(path));
}
