using CommunityToolkit.Mvvm.ComponentModel;
using WheelConvert.Core.Models;
using WheelConvert.Core.Services;

namespace WheelConvert.ViewModels;

public sealed partial class RadialMenuViewModel : ObservableObject
{
    [ObservableProperty] private int _level = 1;
    [ObservableProperty] private FileKind _kind = FileKind.Image;
    [ObservableProperty] private IReadOnlyList<WheelItem> _items = MenuDefinitions.MainItems;
    [ObservableProperty] private int _hoveredIndex = -1;
    [ObservableProperty] private string _centerText = "";

    public string? SourceExtension { get; private set; }

    public event Action<WheelAction>? ActionRequested;

    public void Reset(FileKind kind, string? sourceExtension)
    {
        Kind = kind;
        SourceExtension = sourceExtension;
        HoveredIndex = -1;
        Level = 1;
        Items = MenuDefinitions.MainItems;
        UpdateCenter();
    }

    public void SetHovered(int index) => HoveredIndex = index;

    public void OpenFormats()
    {
        var targets = FormatCatalog.TargetsFor(Kind, SourceExtension);
        if (targets.Count == 0) return;
        HoveredIndex = -1;
        Level = 2;
        Items = targets;
        UpdateCenter();
    }

    public void Back()
    {
        HoveredIndex = -1;
        Level = 1;
        Items = MenuDefinitions.MainItems;
        UpdateCenter();
    }

    /// <summary>Keyboard navigation: wraps around.</summary>
    public void Move(int delta)
    {
        var n = Items.Count;
        if (n == 0) return;
        HoveredIndex = HoveredIndex < 0 ? (delta > 0 ? 0 : n - 1) : (HoveredIndex + delta + n) % n;
    }

    public void ActivateHovered() => Activate(HoveredIndex);

    public void Activate(int index)
    {
        if (index < 0 || index >= Items.Count) return;
        var item = Items[index];
        if (Level == 1 && item.Id == "convert") { OpenFormats(); return; }
        ActionRequested?.Invoke(new WheelAction(Level, item.Id, item.Label, Kind, SourceExtension));
    }

    /// <summary>STEP-2 TEST AID: Tab cycles the pretend input file type (Image -> PDF -> DOCX). Removed once real selection is read (step 4).</summary>
    public void CycleMockKind()
    {
        var (next, ext) = Kind switch
        {
            FileKind.Image => (FileKind.Pdf, "pdf"),
            FileKind.Pdf => (FileKind.Docx, "docx"),
            _ => (FileKind.Image, "jpg")
        };
        Reset(next, ext);
    }

    partial void OnHoveredIndexChanged(int value) => UpdateCenter();

    private void UpdateCenter()
    {
        CenterText = HoveredIndex >= 0 && HoveredIndex < Items.Count
            ? Items[HoveredIndex].Label.ToUpperInvariant()
            : Kind switch { FileKind.Image => "IMAGE", FileKind.Pdf => "PDF", FileKind.Docx => "DOCX", _ => "FILE" };
    }
}
