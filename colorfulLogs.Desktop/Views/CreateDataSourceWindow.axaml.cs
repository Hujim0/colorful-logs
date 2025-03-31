using Avalonia.Controls;
using Avalonia.Interactivity;
using colorfulLogs.Desktop.ViewModels;

namespace colorfulLogs.Desktop.Views;

public partial class CreateDataSourceWindow : Window
{
    public CreateDataSourceWindow()
    {
        InitializeComponent();
        DataContext = new CreateDataSourceViewModel();
    }

    private void CloseWindowClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}