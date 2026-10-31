using WheelConvert.Core.Models;

namespace WheelConvert.Core.Services;

public static class MenuDefinitions
{
    /// <summary>Level-1 wheel, clockwise from 12 o'clock.</summary>
    public static readonly IReadOnlyList<WheelItem> MainItems = new[]
    {
        new WheelItem("convert",  "Convert",  "\uE8AB"),
        new WheelItem("compress", "Compress", "\uE73F"),
        new WheelItem("metadata", "Metadata", "\uE946"),
        new WheelItem("edit",     "Edit",     "\uE70F"),
        new WheelItem("addbg",    "Add BG",   "\uE790"),
        new WheelItem("crop",     "Crop",     "\uE7A8"),
        new WheelItem("redact",   "Redact",   "\uED1A"),
        new WheelItem("annotate", "Annotate", "\uE90A"),
    };
}
