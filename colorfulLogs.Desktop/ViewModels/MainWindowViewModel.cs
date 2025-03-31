using ReactiveUI;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Threading.Tasks;
using Avalonia;
using colorfulLogs.Desktop.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using AvaloniaEdit.Document;
using colorfulLogs.Desktop.Models;

namespace colorfulLogs.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<FileSystemEntry> FileSystem { get; } = new();
    public ObservableCollection<TabItemViewModel> OpenTabs { get; } = new();

    private TabItemViewModel? _selectedTab;
    public TabItemViewModel? SelectedTab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    public TextDocument? CurrentDocument { get; set; }
    public ReactiveCommand<Unit, Unit> ShowCreateDataSourceDialogCommand { get; }
    private void CloseTab(TabItemViewModel tab)
    {
        OpenTabs.Remove(tab);
        if (OpenTabs.Count == 0)
        {
            // Add default tab if needed
        }
    }
    public MainWindowViewModel()
    {
        // Initialize sample file system
        FileSystem.Add(new FileSystemEntry
        {
            Name = "Project",
            IsDirectory = true,
            Children =
        {
            new FileSystemEntry { Name = "Main.axaml", IsDirectory = false },
            new FileSystemEntry { Name = "Main.axaml.cs", IsDirectory = false }
        }
        });

        // Initialize tabs
        OpenTabs.Add(new TabItemViewModel(CloseTab)
        {
            Header = "Welcome"
        });


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
