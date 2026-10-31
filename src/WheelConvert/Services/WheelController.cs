using System.Windows;
using WheelConvert.Core.Models;
using WheelConvert.Core.Services;
using WheelConvert.Native;
using WheelConvert.ViewModels;
using WheelConvert.Views;

namespace WheelConvert.Services;

/// <summary>Glue: hotkey -> read Explorer selection -> show wheel -> execute real file operations.</summary>
internal sealed class WheelController : IDisposable
{
    private readonly TrayIconService _tray;
    private readonly HotkeyManager _hotkey = new();
    private readonly OverlayInputHook _input = new();
    private readonly RadialMenuViewModel _vm = new();
    private RadialMenuWindow? _window;
    private FileContext _currentContext = new(Array.Empty<string>());

    public WheelController(TrayIconService tray) => _tray = tray;

    public void Start()
    {
        _window = new RadialMenuWindow(_vm, App.Settings);
        _window.DismissRequested += Dismiss;

        _vm.ActionRequested += OnAction;

        _input.KeyPressed += key => _window.HandleKey(key);
        _input.OutsideClick += Dismiss;
        _input.IsInsideWheel = (x, y) => _window.ContainsScreenPoint(x, y);

        _hotkey.HotkeyPressed += OnHotkey;
        if (!_hotkey.Register(App.Settings.Hotkey, out var error))
        {
            // Balloon tips can be silently suppressed by Focus Assist – use a message box as fallback.
            MessageBox.Show(
                error + "\n\nThe wheel will not respond until you restart and pick a different hotkey in Settings.",
                "WheelConvert – Hotkey Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    public void ReloadSettings()
    {
        if (!_hotkey.Register(App.Settings.Hotkey, out var error))
        {
            MessageBox.Show(
                error + "\n\nPlease pick a different hotkey in Settings.",
                "WheelConvert – Hotkey Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        _window?.Close();
        _window = new RadialMenuWindow(_vm, App.Settings);
        _window.DismissRequested += Dismiss;
        _input.IsInsideWheel = (x, y) => _window.ContainsScreenPoint(x, y);
    }

    private void OnHotkey()
    {
        if (_window is null || App.Settings.Paused) return;
        if (_window.IsWheelVisible) { Dismiss(); return; }

        // Step 4: Read the selected file(s) from the foreground Explorer window
        _currentContext = new FileContext(ExplorerSelectionReader.GetSelectedFiles());

        if (_currentContext.IsEmpty)
        {
            _tray.ShowBalloon("WheelConvert", "No file selected. Select a file in Explorer first, then press the hotkey.");
            return;
        }

        NativeMethods.GetCursorPos(out var p);
        _vm.Reset(_currentContext.Kind, _currentContext.Extension);
        _window.ShowAt(p.X, p.Y);
        _input.Install();
    }

    private void Dismiss()
    {
        _input.Uninstall();
        _window?.HideAnimated();
    }

    private void OnAction(WheelAction a)
    {
        Dismiss();

        if (_currentContext.IsEmpty)
        {
            _tray.ShowBalloon("WheelConvert", "No file context available.");
            return;
        }

        try
        {
            var result = FileProcessor.Execute(a, _currentContext, App.Settings.OutputFolder);

            if (result.Errors.Count > 0)
            {
                var errText = string.Join("\n", result.Errors);
                if (result.SuccessCount > 0)
                    _tray.ShowBalloon("WheelConvert", $"Done: {result.SuccessCount}/{result.TotalCount} files.\n\nErrors:\n{errText}");
                else
                    _tray.ShowBalloon("WheelConvert", $"Failed:\n{errText}");
            }
            else
            {
                var label = a.Level == 2
                    ? $"Converted {result.SuccessCount} file(s) → {a.Label.ToUpperInvariant()}"
                    : $"{a.Label}: {result.SuccessCount} file(s) done";
                _tray.ShowBalloon("WheelConvert ✓", label);
            }
        }
        catch (CropRequiredException cropEx)
        {
            // Open the interactive crop window
            var cropWin = new CropWindow(cropEx.FilePath, App.Settings.OutputFolder);
            cropWin.CropCompleted += outputPath =>
                _tray.ShowBalloon("WheelConvert ✓", $"Cropped → {System.IO.Path.GetFileName(outputPath)}");
            cropWin.Show();
        }
        catch (MetadataViewRequiredException metaEx)
        {
            // Open the metadata viewer
            var metaWin = new MetadataWindow(metaEx.FilePath);
            metaWin.Show();
        }
        catch (NotImplementedException ex)
        {
            _tray.ShowBalloon("WheelConvert", ex.Message);
        }
        catch (Exception ex)
        {
            _tray.ShowBalloon("WheelConvert – Error", ex.Message);
        }
    }

    public void Dispose()
    {
        _input.Dispose();
        _hotkey.Dispose();
        _window?.Close();
    }
}
