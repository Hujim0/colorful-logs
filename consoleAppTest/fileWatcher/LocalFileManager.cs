using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using consoleAppTest.structs;

namespace consoleAppTest.fileWatcher
{
    public class LocalFileManager
    {
        private readonly Dictionary<string, LocalFile> _localFiles = [];
        private readonly DataSource _dataSource;

        public LocalFileManager(FileWatcher fileWatcher, DataSource dataSource)
        {
            _dataSource = dataSource;

            fileWatcher.FileCreated += HandleFileCreated;
            fileWatcher.FileDeleted += HandleFileDeleted;
            fileWatcher.FileAppended += HandleFileAppended;
            fileWatcher.FileTruncated += HandleFileTruncated;
            fileWatcher.FileMoved += HandleFileMoved;
            fileWatcher.ErrorOccurred += HandleErrorOccurred;
        }

        private void HandleFileCreated(string path, long length)
        {
            if (!_localFiles.ContainsKey(path))
            {
                var localFile = new LocalFile
                {
                    Id = Guid.NewGuid(),
                    Path = path,
                    LastLength = length,
                    dataSource = _dataSource
                };

                _localFiles.Add(path, localFile);
                Console.WriteLine($"Created: {path} (Length: {length})");
            }
        }

        private void HandleFileDeleted(string path)
        {
            if (_localFiles.Remove(path))
            {
                Console.WriteLine($"Deleted: {path}");
            }
        }

        private void HandleFileAppended(string path, long newLength)
        {
            if (_localFiles.TryGetValue(path, out LocalFile? file))
            {
                try
                {
                    long previousLength = file.LastLength;
                    if (newLength > previousLength)
                    {
                        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                        stream.Seek(previousLength, SeekOrigin.Begin);
                        byte[] buffer = new byte[newLength - previousLength];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string newContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"Appended to {path}: {newContent}");
                    }
                    file.LastLength = newLength;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading append for {path}: {ex.Message}");
                }
            }
        }

        private void HandleFileTruncated(string path, long newLength)
        {
            if (_localFiles.TryGetValue(path, out LocalFile? file))
            {
                long previousLength = file.LastLength;
                if (newLength < previousLength)
                {
                    Console.WriteLine($"Truncated: {path} by {previousLength - newLength} bytes, now its not indexed at all");
                }
                file.LastLength = 0;
            }
        }

        private void HandleFileMoved(string oldPath, string newPath)
        {
            if (_localFiles.TryGetValue(oldPath, out LocalFile? file))
            {
                _localFiles.Remove(oldPath);
                file.Path = newPath;
                _localFiles.Add(newPath, file);
                Console.WriteLine($"Moved: {oldPath} => {newPath}");
            }
        }

        private void HandleErrorOccurred(string path, Exception ex)
        {
            Console.WriteLine($"Error [{path}]: {ex.GetType().Name} - {ex.Message}");
        }
    }
}