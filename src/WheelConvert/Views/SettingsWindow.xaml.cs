using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using WheelConvert.Core.Models;
using WheelConvert.Core.Services;
using WheelConvert.Services;

namespace WheelConvert.Views;

public partial class SettingsWindow : Window
{
    private readonly WheelController _controller;
    private string _selectedAccent;

    internal SettingsWindow(WheelController controller)
    {
        InitializeComponent();
        _controller = controller;
        _selectedAccent = App.Settings.AccentColor;

        LoadCurrentSettings();
    }

    private void LoadCurrentSettings()
    {
        var s = App.Settings;
        HotkeyBox.Text = s.Hotkey;

        if (string.IsNullOrWhiteSpace(s.OutputFolder))
        {
            SameFolderRadio.IsChecked = true;
            OutputFolderBox.Text = "";
            BrowseFolderBtn.IsEnabled = false;
        }
        else
        {
            CustomFolderRadio.IsChecked = true;
            OutputFolderBox.Text = s.OutputFolder;
            BrowseFolderBtn.IsEnabled = true;
        }

        WheelSizeSlider.Value = Math.Clamp(s.WheelSize, 240, 500);
        WheelSizeLabel.Text = $"{(int)WheelSizeSlider.Value} px";

        StartWithWindowsCheck.IsChecked = s.StartWithWindows;
        UseAcrylicCheck.IsChecked = s.UseAcrylic;
    }

    private void PresetHotkey_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (HotkeyBox is null) return;
        if (PresetHotkeyCombo.SelectedItem is ComboBoxItem item)
        {
            var raw = item.Content.ToString() ?? "";
            var key = raw.Split(' ')[0];
            HotkeyBox.Text = key;
        }
    }

    private void DestRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (BrowseFolderBtn is null) return;
        BrowseFolderBtn.IsEnabled = CustomFolderRadio.IsChecked == true;
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Select Default Output Folder",
            InitialDirectory = Directory.Exists(OutputFolderBox.Text) ? OutputFolderBox.Text : ""
        };

        if (dlg.ShowDialog() == true)
        {
            OutputFolderBox.Text = dlg.FolderName;
        }
    }

    private void WheelSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WheelSizeLabel is not null)
            WheelSizeLabel.Text = $"{(int)e.NewValue} px";
    }

    private void AccentColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            _selectedAccent = hex;
            // Visual feedback: outline the clicked button
            if (btn.Parent is StackPanel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Button b)
                    {
                        b.BorderBrush = b == btn ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5A));
                        b.BorderThickness = b == btn ? new Thickness(2) : new Thickness(1);
                    }
                }
            }
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var hotkeyText = HotkeyBox.Text.Trim();
        if (!HotkeyParser.TryParse(hotkeyText, out _))
        {
            MessageBox.Show(
                $"“{hotkeyText}” is not a recognized hotkey.\n\nExamples of valid shortcuts:\nCtrl+Shift+Space\nCtrl+Shift+C\nAlt+Shift+Space\nWin+Shift+C",
                "Invalid Hotkey",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var s = App.Settings;
        s.Hotkey = hotkeyText;
        s.OutputFolder = (CustomFolderRadio.IsChecked == true && !string.IsNullOrWhiteSpace(OutputFolderBox.Text))
            ? OutputFolderBox.Text.Trim()
            : null;
        s.WheelSize = (int)WheelSizeSlider.Value;
        s.AccentColor = _selectedAccent;
        s.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
        s.UseAcrylic = UseAcrylicCheck.IsChecked == true;

        // Apply startup setting
        SetWindowsStartup(s.StartWithWindows);

        // Save to disk
        App.SettingsStore.Save(s);

        // Live apply to running wheel controller
        _controller.ReloadSettings();

        MessageBox.Show("Settings saved and applied successfully!", "WheelConvert", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private static void SetWindowsStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key is not null)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    if (enable)
                        key.SetValue("WheelConvert", $"\"{exePath}\"");
                    else
                        key.DeleteValue("WheelConvert", false);
                }
            }
        }
        catch { /* Ignore registry permission errors */ }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
