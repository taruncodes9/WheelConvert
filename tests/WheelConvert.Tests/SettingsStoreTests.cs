using WheelConvert.Core.Models;
using WheelConvert.Core.Services;
using Xunit;

namespace WheelConvert.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void RoundTrip_And_CorruptFileFallsBackToDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "", "settings.json");
        var store = new SettingsStore(path);
        Assert.Equal("Ctrl+Shift+Space", store.Load().Hotkey);

        store.Save(new AppSettings { WheelSize = 420 });
        Assert.Equal(420, store.Load().WheelSize);

        File.WriteAllText(path, "{ not json");
        Assert.Equal(360, store.Load().WheelSize);
    }
}
