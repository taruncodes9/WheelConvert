using WheelConvert.Core.Services;
using Xunit;

namespace WheelConvert.Tests;

public class HotkeyParserTests
{
    [Fact]
    public void Parses_Default()
    {
        Assert.True(HotkeyParser.TryParse("Ctrl+Alt+Space", out var s));
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, s.Modifiers);
        Assert.Equal("Space", s.Key);
    }

    [Fact]
    public void Parses_WithSpacesAndCase_AndDigits()
    {
        Assert.True(HotkeyParser.TryParse("shift + win + 1", out var s));
        Assert.Equal(HotkeyModifiers.Shift | HotkeyModifiers.Win, s.Modifiers);
        Assert.Equal("D1", s.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Space")]        // no modifier
    [InlineData("Ctrl+Alt")]     // no key
    [InlineData("Ctrl+A+B")]     // two keys
    public void Rejects_Invalid(string? text) => Assert.False(HotkeyParser.TryParse(text, out _));

    [Fact]
    public void Format_RoundTrips()
        => Assert.Equal("Ctrl+Alt+Space", HotkeyParser.Format(HotkeyModifiers.Alt | HotkeyModifiers.Control, "Space"));
}
