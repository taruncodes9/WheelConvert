using System.Windows;
using WheelConvert.Core.Models;
using WheelConvert.Core.Services;
using WheelConvert.Services;

namespace WheelConvert;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private TrayIconService? _tray;
    private WheelController? _controller;
    private Views.SettingsWindow? _settingsWindow;

    public static SettingsStore SettingsStore { get; } = new();
    public static AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(true, @"Local\WheelConvert.SingleInstance", out var isFirst);
        if (!isFirst)
        {
            System.Windows.MessageBox.Show("WheelConvert is already running (check the notification area).",
                "WheelConvert", MessageBoxButton.OK, MessageBoxImage.Information);
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            // Never crash the tray app on a stray UI-thread exception.
            System.Diagnostics.Debug.WriteLine(args.Exception);
            args.Handled = true;
        };

        Settings = SettingsStore.Load();
        _controller = new WheelController(null!); // tray initialized next
        _tray = new TrayIconService(
            onSettings: ShowSettings,
            onPauseChanged: paused => { Settings.Paused = paused; SettingsStore.Save(Settings); },
            onQuit: () => Shutdown(),
            initiallyPaused: Settings.Paused);

        // Replace controller with properly wired tray
        _controller = new WheelController(_tray);
        _controller.Start();
    }

    private void ShowSettings()
    {
        if (_controller is null) return;
        if (_settingsWindow is not null && _settingsWindow.IsLoaded)
        {
            _settingsWindow.Activate();
            if (_settingsWindow.WindowState == WindowState.Minimized)
                _settingsWindow.WindowState = WindowState.Normal;
            return;
        }

        _settingsWindow = new Views.SettingsWindow(_controller);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Dispose();
        _tray?.Dispose();
        if (_singleInstance is not null)
        {
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }
        base.OnExit(e);
    }
}
