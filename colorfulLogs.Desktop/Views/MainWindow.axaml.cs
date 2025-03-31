using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using colorfulLogs.Desktop.ViewModels;

namespace colorfulLogs.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Disable the Loaded event so the the diaglogue does not reopen
            this.Loaded -= MainWindow_Loaded;
            await viewModel.ShowCreateDataSourceDialogCommand.Execute();
        }
    }
}
