using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace consoleAppTest.fileWatcher
{
    public class FileWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly ConcurrentDictionary<string, FileState> _fileStates;
        private readonly TimeSpan _debounceDelay;
        private readonly SemaphoreSlim _debounceSemaphore = new(1, 1);
        private readonly bool _trackInitialFiles;

        public event Action<string, long>? FileCreated;
        public event Action<string>? FileDeleted;
        public event Action<string, long>? FileAppended;
        public event Action<string, long>? FileTruncated;
        public event Action<string, string>? FileMoved;
        public event Action<string, Exception>? ErrorOccurred;

        public FileWatcher(string folderPath, TimeSpan? debounceDelay = null, bool trackInitialFiles = true)
        {
            _debounceDelay = debounceDelay ?? TimeSpan.FromMilliseconds(500);
            _trackInitialFiles = trackInitialFiles;
            _fileStates = new ConcurrentDictionary<string, FileState>();

            _watcher = new FileSystemWatcher
            {
                Path = folderPath,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                             NotifyFilters.Size | NotifyFilters.LastWrite,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                InternalBufferSize = 65536 // Max size for Windows
            };

            _watcher.Created += OnCreated;
            _watcher.Deleted += OnDeleted;
            _watcher.Renamed += OnRenamed;
            _watcher.Changed += OnChanged;
            _watcher.Error += OnWatcherError;

            if (_trackInitialFiles)
            {
                InitializeExistingFiles();
            }
        }

        private void InitializeExistingFiles()
        {
            try
            {
                foreach (var filePath in Directory.GetFiles(_watcher.Path, "*.*", SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(filePath);
                    _fileStates.TryAdd(filePath, new FileState { Length = fileInfo.Length, LastWriteTime = fileInfo.LastWriteTimeUtc });
                    FileCreated?.Invoke(filePath, fileInfo.Length);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(_watcher.Path, ex);
            }
        }

        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                await _debounceSemaphore.WaitAsync();
                await ProcessChangeEventAsync(e.FullPath);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(e.FullPath, ex);
            }
            finally
            {
                try
                {
                    _debounceSemaphore?.Release();
                }
                catch (ObjectDisposedException)
                {
                    // Semaphore was disposed; no action needed
                }
            }
        }

        private async Task ProcessChangeEventAsync(string fullPath)
        {
            var lastWriteTime = File.GetLastWriteTimeUtc(fullPath);
            if (_fileStates.TryGetValue(fullPath, out var currentState) &&
                currentState.LastWriteTime == lastWriteTime)
            {
                return;
            }

            await Task.Delay(_debounceDelay);

            var newLastWriteTime = File.GetLastWriteTimeUtc(fullPath);
            if (newLastWriteTime != lastWriteTime)
            {
                return;
            }

            ProcessFileChange(fullPath);
        }

        private void ProcessFileChange(string fullPath)
        {
            try
            {
                var fileInfo = new FileInfo(fullPath);
                if (!fileInfo.Exists)
                    return;

                var newLength = fileInfo.Length;
                var newLastWriteTime = fileInfo.LastWriteTimeUtc;

                _fileStates.AddOrUpdate(fullPath,
                    _ =>
                    {
                        // This is a new file entry, trigger FileCreated
                        FileCreated?.Invoke(fullPath, newLength);
                        return new FileState { Length = newLength, LastWriteTime = newLastWriteTime };
                    },
                    (path, oldState) =>
                    {
                        if (newLength > oldState.Length)
                        {
                            FileAppended?.Invoke(path, newLength);
                        }
                        else if (newLength < oldState.Length)
                        {
                            FileTruncated?.Invoke(path, newLength);
                        }
                        return new FileState { Length = newLength, LastWriteTime = newLastWriteTime };
                    });
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(fullPath, ex);
            }
        }

        private async void OnCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                await WaitForFileRelease(e.FullPath);
                ProcessFileChange(e.FullPath);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(e.FullPath, ex);
            }
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                _fileStates.TryRemove(e.FullPath, out _);
                FileDeleted?.Invoke(e.FullPath);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(e.FullPath, ex);
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                if (!IsPathInWatchedDirectory(e.FullPath))
                {
                    OnDeleted(sender, new FileSystemEventArgs(WatcherChangeTypes.Deleted,
                        Path.GetDirectoryName(e.OldFullPath)!,
                        Path.GetFileName(e.OldFullPath)));
                    return;
                }

                _fileStates.TryRemove(e.OldFullPath, out _);
                ProcessFileChange(e.FullPath);
                FileMoved?.Invoke(e.OldFullPath, e.FullPath);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(e.OldFullPath, ex);
            }
        }

        private bool IsPathInWatchedDirectory(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                var watchedPath = Path.GetFullPath(_watcher.Path);
                return fullPath.StartsWith(watchedPath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private async Task WaitForFileRelease(string path, int timeout = 2000)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TimeSpan.FromMilliseconds(timeout))
            {
                try
                {
                    using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    return; // File is available
                }
                catch (IOException) when (stopwatch.Elapsed < TimeSpan.FromMilliseconds(timeout))
                {
                    await Task.Delay(50);
                }
            }
            throw new IOException($"File {path} remained locked after {timeout}ms");
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(_watcher.Path, e.GetException());
        }
        public void Dispose()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnCreated;
            _watcher.Deleted -= OnDeleted;
            _watcher.Renamed -= OnRenamed;
            _watcher.Changed -= OnChanged;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _debounceSemaphore.Dispose();
        }

        private class FileState
        {
            public long Length { get; set; }
            public DateTime LastWriteTime { get; set; }
        }
    }
}