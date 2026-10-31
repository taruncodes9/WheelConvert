using WheelConvert.Core.Services;
using Xunit;

namespace WheelConvert.Tests;

public class FileNamerTests
{
    [Fact]
    public void NoCollision_ReturnsDesired()
        => Assert.Equal(@"C:\a\Page 001.jpg", FileNamer.GetUniquePath(@"C:\a\Page 001.jpg", _ => false));

    [Fact]
    public void OneCollision_Adds2()
    {
        var taken = new HashSet<string> { Path.Combine("C:\\a", "x.jpg") };
        Assert.Equal(Path.Combine("C:\\a", "x (2).jpg"),
            FileNamer.GetUniquePath(Path.Combine("C:\\a", "x.jpg"), taken.Contains));
    }

    [Fact]
    public void MultipleCollisions_Adds3()
    {
        var taken = new HashSet<string>
        {
            Path.Combine("C:\\a", "x.jpg"), Path.Combine("C:\\a", "x (2).jpg")
        };
        Assert.Equal(Path.Combine("C:\\a", "x (3).jpg"),
            FileNamer.GetUniquePath(Path.Combine("C:\\a", "x.jpg"), taken.Contains));
    }

    [Theory]
    [InlineData("jpg")]
    [InlineData(".jpg")]
    public void OutputPath_AcceptsExtensionWithOrWithoutDot(string ext)
    {
        var src = Path.Combine("C:\\a", "Page 001.pdf");
        Assert.Equal(Path.Combine("C:\\a", "Page 001.jpg"), FileNamer.GetOutputPath(src, ext, null, _ => false));
    }

    [Fact]
    public void OutputPath_UsesCustomFolder()
    {
        var src = Path.Combine("C:\\a", "doc.pdf");
        Assert.Equal(Path.Combine("D:\\out", "doc.txt"), FileNamer.GetOutputPath(src, "txt", "D:\\out", _ => false));
    }
}
