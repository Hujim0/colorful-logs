using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class FileWatcherTests : IAsyncLifetime
{
    private string? _tempDirectory;
    private FileWatcher? _watcher;
    private TaskCompletionSource<bool>? _eventTcs;
    private string? _capturedPath;
    private long _capturedSize;
    private string? _capturedOldPath;
    private string? _capturedNewPath;
    private Exception? _capturedException;

    public async Task InitializeAsync()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
        // Start watcher before any file operations
        _watcher = new FileWatcher(_tempDirectory, trackInitialFiles: false);
        await Task.Delay(100); // Allow watcher to start
        _eventTcs = new TaskCompletionSource<bool>();
    }

    public async Task DisposeAsync()
    {
        _watcher?.Dispose();
        // Retry deletion to handle any lingering file locks
        await RetryHelper.RetryAsync(() =>
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }, maxRetries: 3, delayMs: 100);
    }

    private void ResetEventSources()
    {
        _eventTcs = new TaskCompletionSource<bool>();
        _capturedPath = null;
        _capturedSize = 0;
        _capturedOldPath = null;
        _capturedNewPath = null;
        _capturedException = null;
    }

    private async Task WaitWithTimeout(TimeSpan timeout)
    {
        await Task.WhenAny(_eventTcs!.Task, Task.Delay(timeout));
        Assert.True(_eventTcs.Task.IsCompleted, "Event was not triggered within the expected time.");
    }

    private async Task WaitWithTimeoutForTcs(TaskCompletionSource<bool> tcs)
    {
        await Task.WhenAny(tcs.Task, Task.Delay(5000));
        Assert.True(tcs.Task.IsCompleted, "Initial event not completed in time.");
    }

    [Fact]
    public async Task Created_File_TriggersFileCreatedEvent()
    {
        var debounceDelay = TimeSpan.FromMilliseconds(100);
        _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
        string fileName = Path.Combine(_tempDirectory!, "test.txt");
        ResetEventSources();

        _watcher!.FileCreated += (path, size) =>
        {
            _capturedPath = path;
            _capturedSize = size;
            _eventTcs!.SetResult(true);
        };

        // Act
        await File.WriteAllTextAsync(fileName, "content");
        await WaitWithTimeout(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(fileName, _capturedPath);
        Assert.Equal(new FileInfo(fileName).Length, _capturedSize);
    }

    [Fact]
    public async Task Deleted_File_TriggersFileDeletedEvent()
    {
        var debounceDelay = TimeSpan.FromMilliseconds(100);
        _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
        string fileName = Path.Combine(_tempDirectory!, "test.txt");

        // Create file and wait for creation to complete
        var createTcs = new TaskCompletionSource<bool>();
        _watcher!.FileCreated += (path, size) => createTcs.SetResult(true);
        await File.WriteAllTextAsync(fileName, "content");
        await WaitWithTimeoutForTcs(createTcs);

        ResetEventSources();

        _watcher.FileDeleted += (path) =>
        {
            _capturedPath = path;
            _eventTcs!.SetResult(true);
        };

        // Act
        File.Delete(fileName);
        await WaitWithTimeout(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(fileName, _capturedPath);
    }

    [Fact]
    public async Task Appended_File_TriggersFileAppendedEvent()
    {
        // Arrange
        var debounceDelay = TimeSpan.FromMilliseconds(100);
        _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
        string fileName = Path.Combine(_tempDirectory!, "test.txt");

        // Create file and wait for creation
        var createTcs = new TaskCompletionSource<bool>();
        _watcher.FileCreated += (path, size) => createTcs.SetResult(true);
        await File.WriteAllTextAsync(fileName, "initial");
        await WaitWithTimeoutForTcs(createTcs);

        ResetEventSources();

        _watcher.FileAppended += (path, size) =>
        {
            _capturedPath = path;
            _capturedSize = size;
            _eventTcs!.SetResult(true);
        };

        // Act
        await File.AppendAllTextAsync(fileName, "appended");
        await WaitWithTimeout(debounceDelay.Add(TimeSpan.FromMilliseconds(5000))); // Increased buffer

        // Assert
        Assert.Equal(fileName, _capturedPath);
        Assert.Equal(new FileInfo(fileName).Length, _capturedSize);
    }

    [Fact]
    public async Task Truncated_File_TriggersFileTruncatedEvent()
    {
        // Arrange
        var debounceDelay = TimeSpan.FromMilliseconds(100);
        _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
        string fileName = Path.Combine(_tempDirectory!, "test.txt");

        // Create file and wait for creation
        var createTcs = new TaskCompletionSource<bool>();
        _watcher.FileCreated += (path, size) => createTcs.SetResult(true);
        await File.WriteAllTextAsync(fileName, "initial content");
        await WaitWithTimeoutForTcs(createTcs);

        ResetEventSources();

        _watcher.FileTruncated += (path, size) =>
        {
            _capturedPath = path;
            _capturedSize = size;
            _eventTcs!.SetResult(true);
        };

        // Act
        using (var fs = new FileStream(fileName, FileMode.Truncate, FileAccess.Write)) { }
        await WaitWithTimeout(debounceDelay.Add(TimeSpan.FromMilliseconds(5000)));

        // Assert
        Assert.Equal(fileName, _capturedPath);
        Assert.Equal(0, _capturedSize);
    }

    [Fact]
    public async Task Renamed_File_TriggersFileMovedEvent()
    {
        // Arrange
        var debounceDelay = TimeSpan.FromMilliseconds(100);
        _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
        string oldFileName = Path.Combine(_tempDirectory!, "old.txt");
        string newFileName = Path.Combine(_tempDirectory!, "new.txt");

        // Create file and wait for creation
        var createTcs = new TaskCompletionSource<bool>();
        _watcher!.FileCreated += (path, size) => createTcs.SetResult(true);
        await File.WriteAllTextAsync(oldFileName, "content");
        await WaitWithTimeoutForTcs(createTcs);

        ResetEventSources();

        _watcher.FileMoved += (oldPath, newPath) =>
        {
            _capturedOldPath = oldPath;
            _capturedNewPath = newPath;
            _eventTcs!.SetResult(true);
        };

        // Act
        File.Move(oldFileName, newFileName);
        await WaitWithTimeout(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(oldFileName, _capturedOldPath);
        Assert.Equal(newFileName, _capturedNewPath);
    }

    [Fact]
    public async Task ErrorOccurred_WhenWatchedDirectoryDeleted()
    {
        // Arrange
        var debounceDelay = TimeSpan.FromMilliseconds(100);
        _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
        ResetEventSources();

        _watcher!.ErrorOccurred += (path, ex) =>
        {
            _capturedException = ex;
            _eventTcs!.SetResult(true);
        };

        // Act
        Directory.Delete(_tempDirectory!, recursive: true);
        await WaitWithTimeout(TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(_capturedException);
    }

    [Fact]
    public async Task Created_Utf8File_TriggersFileCreatedEvent()
    {
        // Arrange
        var debounceDelay = TimeSpan.FromMilliseconds(100);
        _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
        string fileName = Path.Combine(_tempDirectory!, "test_utf8.txt");
        var content = "你好世界"u8.ToArray(); // UTF-8 bytes for "Hello World" in Chinese
        ResetEventSources();

        _watcher!.FileCreated += (path, size) =>
        {
            _capturedPath = path;
            _capturedSize = size;
            _eventTcs!.SetResult(true);
        };

        // Act
        await File.WriteAllBytesAsync(fileName, content);
        await WaitWithTimeout(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(fileName, _capturedPath);
        Assert.Equal("你好世界"u8.Length, _capturedSize);
    }

    [Fact]
    public async Task Appended_Utf8File_TriggersFileAppendedEvent()
    {
        // Arrange
        var debounceDelay = TimeSpan.FromMilliseconds(100);
        _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
        string fileName = Path.Combine(_tempDirectory!, "test_utf8.txt");
        var initialContent = "Hello"u8.ToArray();
        var appendContent = " 世界"u8.ToArray(); // " World" in Chinese

        // Create file and wait for creation
        var createTcs = new TaskCompletionSource<bool>();
        _watcher.FileCreated += (path, size) => createTcs.SetResult(true);
        await File.WriteAllBytesAsync(fileName, initialContent);
        await WaitWithTimeoutForTcs(createTcs);

        ResetEventSources();

        _watcher.FileAppended += (path, size) =>
        {
            _capturedPath = path;
            _capturedSize = size;
            _eventTcs!.SetResult(true);
        };

        // Act
        await File.AppendAllBytesAsync(fileName, appendContent);
        await WaitWithTimeout(debounceDelay.Add(TimeSpan.FromMilliseconds(500)));

        // Assert
        Assert.Equal(fileName, _capturedPath);
        Assert.Equal((initialContent.Length + appendContent.Length), _capturedSize);
    }

    [Fact]
    public async Task Truncated_Utf8File_TriggersFileTruncatedEvent()
    {
        // Arrange
        var debounceDelay = TimeSpan.FromMilliseconds(100);
        _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
        string fileName = Path.Combine(_tempDirectory!, "test_utf8.txt");
        var content = "🍕🚀🎉"u8.ToArray(); // UTF-8 emojis

        // Create file and wait for creation
        var createTcs = new TaskCompletionSource<bool>();
        _watcher.FileCreated += (path, size) => createTcs.SetResult(true);
        await File.WriteAllBytesAsync(fileName, content);
        await WaitWithTimeoutForTcs(createTcs);

        ResetEventSources();

        _watcher.FileTruncated += (path, size) =>
        {
            _capturedPath = path;
            _capturedSize = size;
            _eventTcs!.SetResult(true);
        };

        // Act
        using (var fs = new FileStream(fileName, FileMode.Truncate, FileAccess.Write)) { }
        await WaitWithTimeout(debounceDelay.Add(TimeSpan.FromMilliseconds(500)));

        // Assert
        Assert.Equal(fileName, _capturedPath);
        Assert.Equal(0, _capturedSize);
    }

}

public static class RetryHelper
{
    public static async Task RetryAsync(Action action, int maxRetries = 3, int delayMs = 100)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                action();
                return;
            }
            catch
            {
                if (i == maxRetries - 1) throw;
                await Task.Delay(delayMs);
            }
        }
    }
}