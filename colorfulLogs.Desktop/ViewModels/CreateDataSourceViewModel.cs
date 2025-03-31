using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;

namespace colorfulLogs.Desktop.ViewModels
{
    public class CreateDataSourceViewModel : INotifyPropertyChanged
    {
        private int _selectedTypeIndex;
        private string _path = string.Empty;
        private string _ignoreMasks = string.Empty;
        private int _port = 8080;

        public int SelectedTypeIndex
        {
            get => _selectedTypeIndex;
            set => SetField(ref _selectedTypeIndex, value);
        }

        public string Path
        {
            get => _path;
            set => SetField(ref _path, value);
        }

        public string IgnoreMasks
        {
            get => _ignoreMasks;
            set => SetField(ref _ignoreMasks, value);
        }

        public int Port
        {
            get => _port;
            set => SetField(ref _port, value);
        }

        public ReactiveCommand<Window, Unit> SelectFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

        public CreateDataSourceViewModel()
        {
            SelectFolderCommand = ReactiveCommand.CreateFromTask<Window>(async window =>
            {
                var storageProvider = window.StorageProvider;
                var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Folder",
                    AllowMultiple = false
                });

                if (result.Count > 0 && result[0].TryGetLocalPath() is string localPath)
                {
                    Path = localPath;
                }
            });

            ConfirmCommand = ReactiveCommand.Create(() =>
            {
                // Handle confirmation logic based on selected tab
                if (SelectedTypeIndex == 0) // Files tab
                {
                    if (string.IsNullOrWhiteSpace(Path))
                    {
                        // Show error message
                        return;
                    }
                    // Process file data source creation
                }
                else // HTTP tab
                {
                    if (Port < 1 || Port > 65535)
                    {
                        // Show error message
                        return;
                    }
                    // Process HTTP server creation
                }

                // Close window or notify parent viewmodel
            }, this.WhenAnyValue(x => x.Path, path => !string.IsNullOrEmpty(path))
            .ObserveOn(RxApp.MainThreadScheduler));
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}
