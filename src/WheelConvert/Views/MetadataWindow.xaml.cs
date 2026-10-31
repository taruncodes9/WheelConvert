using System.Windows;
using WheelConvert.Core.Services;

namespace WheelConvert.Views;

public partial class MetadataWindow : Window
{
    public MetadataWindow(string filePath)
    {
        InitializeComponent();
        FileNameText.Text = System.IO.Path.GetFileName(filePath);
        Title = $"Metadata — {System.IO.Path.GetFileName(filePath)}";

        var entries = MetadataReader.Read(filePath);
        MetadataGrid.ItemsSource = entries;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

