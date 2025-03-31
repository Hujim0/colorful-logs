using ReactiveUI;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Threading.Tasks;
using Avalonia;
using colorfulLogs.Desktop.Views;

namespace colorfulLogs.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private string _greeting = "Welcome to Avalonia!";

    public string Greeting
    {
        get => _greeting;
        // set => this.RaiseAndSetIfChanged(ref _greeting, value); // Use RaiseAndSetIfChanged
    }

    public ReactiveCommand<Unit, Unit> ShowCreateDataSourceDialogCommand { get; }

    public MainWindowViewModel()
    {
        ShowCreateDataSourceDialogCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var dialog = new CreateDataSourceWindow
            {
                DataContext = new CreateDataSourceViewModel()
            };

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow != null)
                {
                    try
                    {
                        await dialog.ShowDialog(desktop.MainWindow);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error showing dialog: {ex}"); // Log any exceptions
                    }
                }
                else
                {
                    Console.WriteLine("desktop.MainWindow is null!"); // Debugging: Check if MainWindow is null
                }
            }
            else
            {
                Console.WriteLine("ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime!"); // Debugging: Check Lifetime type
            }
        });

    }
}
