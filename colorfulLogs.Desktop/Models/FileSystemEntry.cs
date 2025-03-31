using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace colorfulLogs.Desktop.Models
{
    public class FileSystemEntry
    {
        public string Name { get; set; } = "";
        public bool IsDirectory { get; set; }
        public ObservableCollection<FileSystemEntry> Children { get; } = new();
        public string Icon => IsDirectory ?
            "avares://colorfulLogs.Desktop/Assets/folder.svg" :
            "avares://colorfulLogs.Desktop/Assets/file.svg";
    }
}