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
    using consoleAppTest.structs;

    public class FileWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly ConcurrentDictionary<string, LocalFile> _Files;

        private DataSource source;
        public event FileSystemEventHandler? FileCreated;
        public event FileSystemEventHandler? FileDeleted;
        public event FileSystemEventHandler? FileAppended;
        public event RenamedEventHandler? FileMoved;

        public FileWatcher(string folderPath, DataSource source)
        {
            _Files = new ConcurrentDictionary<string, LocalFile>();
            this.source = source;

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
                var file = new LocalFile
                {
                    Id = Guid.NewGuid(),
                    Path = e.FullPath,
                    dataSource = source,
                    LastLength = length,
                };

                _Files.TryAdd(e.FullPath, file);
                FileCreated?.Invoke(this, e);
            }
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            _Files.TryRemove(e.FullPath, out _);
            FileDeleted?.Invoke(this, e);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            bool res = _Files.TryGetValue(e.OldFullPath, out LocalFile? file);
            var newLength = GetFileLength(e.FullPath);
            if (newLength != -1 && res && file != null)
            {
                file.Path = e.FullPath;
                file.LastLength = newLength;
            }
            FileMoved?.Invoke(this, e);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            long newLength = GetFileLength(e.FullPath);
            if (newLength == -1) return;

            bool isAppend = false;
            _Files.AddOrUpdate(e.FullPath,
                new LocalFile { Path = e.FullPath, LastLength = newLength, dataSource = source },
                (path, file) =>
                {
                    var oldMax = file.LastLength;

                    if (newLength > oldMax)
                    {
                        isAppend = true;
                        file.LastLength = newLength;
                        return file;
                    }
                    return file;
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