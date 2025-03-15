using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace consoleAppTest.fileWatcher
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Threading;

    public class FileWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly ConcurrentDictionary<string, long> _maxFileSizes;

        public event FileSystemEventHandler? FileCreated;
        public event FileSystemEventHandler? FileDeleted;
        public event FileSystemEventHandler? FileAppended;
        public event RenamedEventHandler? FileMoved;

        public FileWatcher(string folderPath)
        {
            _maxFileSizes = new ConcurrentDictionary<string, long>();

            _watcher = new FileSystemWatcher
            {
                Path = folderPath,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
                             | NotifyFilters.Size | NotifyFilters.LastWrite,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;
            _watcher.Renamed += OnRenamed;
            _watcher.Changed += OnChanged;
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            var length = GetFileLength(e.FullPath);
            if (length != -1)
            {
                _maxFileSizes.TryAdd(e.FullPath, length);
                FileCreated?.Invoke(this, e);
            }
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            _maxFileSizes.TryRemove(e.FullPath, out _);
            FileDeleted?.Invoke(this, e);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            _maxFileSizes.TryRemove(e.OldFullPath, out _);
            var newLength = GetFileLength(e.FullPath);
            if (newLength != -1)
            {
                _maxFileSizes.TryAdd(e.FullPath, newLength);
            }
            FileMoved?.Invoke(this, e);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            long newLength = GetFileLength(e.FullPath);
            if (newLength == -1) return;

            bool isAppend = false;
            _maxFileSizes.AddOrUpdate(e.FullPath,
                newLength,
                (path, oldMax) =>
                {
                    if (newLength > oldMax)
                    {
                        isAppend = true;
                        return newLength;
                    }
                    return oldMax;
                });

            if (isAppend)
            {
                FileAppended?.Invoke(this, e);
            }
        }

        private static long GetFileLength(string path)
        {
            const int maxRetries = 3;
            const int delayMs = 100;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var fileInfo = new FileInfo(path);
                    return fileInfo.Exists ? fileInfo.Length : -1;
                }
                catch (IOException)
                {
                    Thread.Sleep(delayMs);
                }
                catch (Exception)
                {
                    return -1;
                }
            }
            return -1;
        }

        public void Dispose()
        {
            _watcher.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}