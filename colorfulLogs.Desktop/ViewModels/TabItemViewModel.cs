using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace colorfulLogs.Desktop.ViewModels
{
    public class TabItemViewModel : ViewModelBase
    {
        public string Header { get; set; } = string.Empty;
        public ICommand CloseTabCommand { get; }

        public TabItemViewModel(Action<TabItemViewModel> closeAction)
        {
            CloseTabCommand = new RelayCommand(() => closeAction(this));
        }
    }
}