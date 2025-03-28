using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using colorfulLogs.fileWatcher;
using colorfulLogs.structs;

public partial class LocalFileManager
{
    public event Action<List<IndexedLine>>? OnLinesToIndex;

    private readonly Dictionary<string, LocalFile> _localFiles = [];
    private readonly DataSource _dataSource;
    public Dictionary<string, LocalFile> GetTrackedFiles() => _localFiles;


    public LocalFileManager(FileWatcher fileWatcher, DataSource dataSource, List<LocalFile>? persistedState = null)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

        // Initialize with persisted state (if any)
        _localFiles = persistedState?.ToDictionary(file => file.Path) ?? [];

        // Always reconcile with current filesystem state
        ReconcileInitialState(fileWatcher.folderPath);

        // Subscribe to events AFTER reconciliation
        fileWatcher.FileCreated += HandleFileCreated;
        fileWatcher.FileDeleted += HandleFileDeleted;
        fileWatcher.FileAppended += HandleFileAppended;
        fileWatcher.FileTruncated += HandleFileTruncated;
        fileWatcher.FileMoved += HandleFileMoved;
        fileWatcher.ErrorOccurred += HandleErrorOccurred;
    }

    private void ReconcileInitialState(string folderPath)
    {
        try
        {
            var normalizedFolder = Path.GetFullPath(folderPath);
            var currentFiles = Directory.GetFiles(normalizedFolder, "*.*", SearchOption.AllDirectories)
                .Select(Path.GetFullPath)
                .ToList();
            var currentFileSet = new HashSet<string>(currentFiles, StringComparer.OrdinalIgnoreCase);

            // Remove files that no longer exist
            foreach (var path in _localFiles.Keys.ToList())
            {
                var normalizedPath = Path.GetFullPath(path);
                if (!currentFileSet.Contains(normalizedPath))
                {
                    HandleFileDeleted(normalizedPath); // This removes from _localFiles
                }
            }

            // Add/update existing files
            foreach (var filePath in currentFiles)
            {
                var normalizedPath = Path.GetFullPath(filePath);
                var fileInfo = new FileInfo(normalizedPath);
                if (!fileInfo.Exists) continue;

                if (_localFiles.TryGetValue(normalizedPath, out LocalFile? localFile))
                {
                    // Handle size changes
                    if (fileInfo.Length > localFile.LastLength)
                    {
                        HandleFileAppended(normalizedPath, fileInfo.Length);
                    }
                    else if (fileInfo.Length < localFile.LastLength)
                    {
                        HandleFileTruncated(normalizedPath, fileInfo.Length);
                    }
                    localFile.LastLength = fileInfo.Length;
                }
                else
                {
                    HandleFileCreated(normalizedPath, fileInfo.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Reconciliation error: {ex.Message}");
        }
    }
    private void HandleFileCreated(string path, long length)
    {
        if (!_localFiles.ContainsKey(path))
        {
            var localFile = new LocalFile
            {
                Path = path,
                LastLength = length,
                dataSource = _dataSource,
                LastLineNumber = 0,
                PendingLine = ""
            };

            _localFiles.Add(path, localFile);

            if (length > 0)
            {
                ProcessFileContent(localFile, 0, length);
            }

            Console.WriteLine($"Created: {path} (Length: {length})");
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
                    ProcessFileContent(file, previousLength, newLength);
                }
                file.LastLength = newLength;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading append for {path}: {ex.Message}");
            }
        }
    }

    private void ProcessFileContent(LocalFile file, long start, long end)
    {
        using var stream = new FileStream(file.Path, FileMode.Open, FileAccess.Read);
        stream.Seek(start, SeekOrigin.Begin);

        byte[] buffer = new byte[end - start];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string newContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        // Combine with pending line from previous appends
        string fullContent = file.PendingLine + newContent;
        string[] lines = fullContent.Split('\n');

        // Determine complete lines to process
        int completeLines = lines.Length - 1;
        bool endsWithNewLine = newContent.EndsWith('\n');

        List<IndexedLine> linesToIndex = [];

        for (int i = 0; i < completeLines; i++)
        {
            file.LastLineNumber++;
            var indexedLine = new IndexedLine
            {
                Source = _dataSource,
                LineText = lines[i].TrimEnd('\r'),  // Handle CRLF
                LineNumber = (ulong)file.LastLineNumber
            };
            linesToIndex.Add(indexedLine);
        }

        OnLinesToIndex?.Invoke(linesToIndex);

        // Update pending line
        file.PendingLine = endsWithNewLine ? "" : lines.Last();
    }

    private void HandleFileTruncated(string path, long newLength)
    {
        if (_localFiles.TryGetValue(path, out LocalFile? file))
        {
            // Reset tracking state
            file.LastLength = 0;
            file.LastLineNumber = 0;
            file.PendingLine = "";

            // Process the new content from scratch
            if (newLength > 0)
            {
                ProcessFileContent(file, 0, newLength);
                file.LastLength = newLength;
            }

            Console.WriteLine($"Truncated: {path} (Reset to {newLength} bytes)");
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
    private void HandleFileDeleted(string path)
    {
        if (_localFiles.Remove(path))
        {
            Console.WriteLine($"Deleted: {path}");
        }
    }
}
