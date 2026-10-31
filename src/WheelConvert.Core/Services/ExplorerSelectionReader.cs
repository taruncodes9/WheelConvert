using System.Runtime.InteropServices;

namespace WheelConvert.Core.Services;

/// <summary>
/// Reads selected files from the foreground Explorer window using Shell COM automation.
/// Uses raw COM interop — no Shell32/SHDocVw type library references needed.
/// </summary>
public static class ExplorerSelectionReader
{
    public static List<string> GetSelectedFiles()
    {
        var result = new List<string>();
        try
        {
            var foreground = GetForegroundWindow();

            // Create Shell.Application COM object
            var shellAppType = Type.GetTypeFromProgID("Shell.Application");
            if (shellAppType is null) return result;
            var shell = Activator.CreateInstance(shellAppType);
            if (shell is null) return result;

            // Get the open Explorer windows collection
            var windows = Invoke(shell, "Windows");
            if (windows is null) return result;
            var count = (int)GetProperty(windows, "Count")!;

            for (int i = 0; i < count; i++)
            {
                object? win = null;
                try { win = Invoke(windows, "Item", i); } catch { continue; }
                if (win is null) continue;

                // Match by HWND
                var hwnd = (long)GetProperty(win, "HWND")!;
                var fgLong = foreground.ToInt64();
                var rootLong = GetAncestor(foreground, GA_ROOT).ToInt64();
                if (hwnd != fgLong && hwnd != rootLong) continue;

                // Get the Document (IShellFolderViewDual)
                var doc = GetProperty(win, "Document");
                if (doc is null) continue;

                // Get SelectedItems
                var selectedItems = Invoke(doc, "SelectedItems");
                if (selectedItems is null) continue;
                var selCount = (int)GetProperty(selectedItems, "Count")!;

                for (int j = 0; j < selCount; j++)
                {
                    var item = Invoke(selectedItems, "Item", j);
                    if (item is null) continue;
                    var path = GetProperty(item, "Path") as string;
                    if (!string.IsNullOrEmpty(path) && (File.Exists(path) || Directory.Exists(path)))
                        result.Add(path);
                }

                if (result.Count > 0) break;  // found the right window
            }
        }
        catch
        {
            // COM errors, Explorer not running, etc. — return empty
        }
        return result;
    }

    private static object? Invoke(object obj, string method, params object[] args)
        => obj.GetType().InvokeMember(method, System.Reflection.BindingFlags.InvokeMethod, null, obj, args);

    private static object? GetProperty(object obj, string name)
        => obj.GetType().InvokeMember(name, System.Reflection.BindingFlags.GetProperty, null, obj, null);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private const uint GA_ROOT = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
}
