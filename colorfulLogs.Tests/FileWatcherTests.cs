// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Threading.Tasks;
// using Xunit;

// public class FileWatcherTests : IAsyncLifetime
// {
//     private string? _tempDirectory;
//     private FileWatcher? _watcher;
//     private TaskCompletionSource<bool>? _eventTcs;
//     private string? _capturedPath;
//     private long _capturedSize;
//     private string? _capturedOldPath;
//     private string? _capturedNewPath;
//     private Exception? _capturedException;

//     const int MaxRetries = 3;
//     const int RetryDelayMs = 300;

//     public async Task InitializeAsync()
//     {
//         _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
//         Directory.CreateDirectory(_tempDirectory);
//         // Start watcher before any file operations
//         _watcher = new FileWatcher(_tempDirectory, trackInitialFiles: false);
//         await Task.Delay(100); // Allow watcher to start
//         _eventTcs = new TaskCompletionSource<bool>();
//     }

//     public async Task DisposeAsync()
//     {
//         await Task.Run(() =>
//         {
//             _watcher?.Dispose();
//             if (Directory.Exists(_tempDirectory))
//             {
//                 Directory.Delete(_tempDirectory, recursive: true);
//             }
//         });
//     }

//     private void ResetEventSources()
//     {
//         _eventTcs = new TaskCompletionSource<bool>();
//         _capturedPath = null;
//         _capturedSize = 0;
//         _capturedOldPath = null;
//         _capturedNewPath = null;
//         _capturedException = null;
//     }


//     private async Task WaitWithTimeoutForTcs(TaskCompletionSource<bool> tcs)
//     {
//         await Task.WhenAny(tcs.Task, Task.Delay(5000));
//         Assert.True(tcs.Task.IsCompleted, "Initial event not completed in time.");
//     }

//     [Fact]
//     public async Task Created_File_TriggersFileCreatedEvent()
//     {
//         var debounceDelay = TimeSpan.FromMilliseconds(100);
//         _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
//         string fileName = Path.Combine(_tempDirectory!, "test.txt");

//         var retryCount = 0;
//         var success = false;
//         Exception? lastError = null;

//         _watcher!.FileCreated += (path, size) =>
//         {
//             _capturedPath = path;
//             _capturedSize = size;
//             _eventTcs!.SetResult(true);
//         };

//         // Retry loop
//         while (retryCount < MaxRetries && !success)
//         {
//             ResetEventSources();
//             lastError = null;

//             // Set up error handler for this attempt
//             var errorTcs = new TaskCompletionSource<bool>();
//             _watcher.ErrorOccurred += HandleError;

//             try
//             {
//                 // Cleanup previous attempt
//                 if (File.Exists(fileName)) File.Delete(fileName);

//                 // Act
//                 await File.WriteAllTextAsync(fileName, "content");

//                 // Wait for either success or error
//                 var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
//                 var completedTask = await Task.WhenAny(_eventTcs!.Task, errorTcs.Task, timeoutTask);

//                 if (completedTask == _eventTcs.Task)
//                 {
//                     success = true;
//                     break;
//                 }

//                 if (completedTask == errorTcs.Task)
//                 {
//                     retryCount++;
//                     await Task.Delay(RetryDelayMs);
//                 }
//             }
//             finally
//             {
//                 _watcher.ErrorOccurred -= HandleError;
//             }

//             void HandleError(string path, Exception ex)
//             {
//                 lastError = ex;
//                 errorTcs.TrySetResult(true);
//             }
//         }

//         // Assert
//         Assert.True(success, $"Failed after {MaxRetries} retries. Last error: {lastError?.Message}");
//         Assert.Equal(fileName, _capturedPath);
//         Assert.Equal(new FileInfo(fileName).Length, _capturedSize);
//     }

//     [Fact]
//     public async Task Truncated_File_TriggersFileTruncatedEvent()
//     {
//         var debounceDelay = TimeSpan.FromMilliseconds(100);
//         _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
//         string fileName = Path.Combine(_tempDirectory!, "test.txt");

//         var retryCount = 0;
//         var success = false;
//         Exception? lastError = null;

//         // Initial file creation
//         var createTcs = new TaskCompletionSource<bool>();
//         _watcher.FileCreated += (path, size) => createTcs.SetResult(true);
//         await File.WriteAllTextAsync(fileName, "initial content");
//         await WaitWithTimeoutForTcs(createTcs);

//         while (retryCount < MaxRetries && !success)
//         {
//             ResetEventSources();
//             lastError = null;

//             _watcher.FileTruncated += (path, size) =>
//             {
//                 _capturedPath = path;
//                 _capturedSize = size;
//                 _eventTcs!.SetResult(true);
//             };

//             var errorTcs = new TaskCompletionSource<bool>();
//             _watcher.ErrorOccurred += HandleError;

//             try
//             {
//                 using (var fs = new FileStream(fileName, FileMode.Truncate, FileAccess.Write)) { }

//                 var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
//                 var completedTask = await Task.WhenAny(_eventTcs!.Task, errorTcs.Task, timeoutTask);

//                 if (completedTask == _eventTcs.Task)
//                 {
//                     success = true;
//                     break;
//                 }

//                 if (completedTask == errorTcs.Task)
//                 {
//                     retryCount++;
//                     await Task.Delay(RetryDelayMs);
//                 }
//             }
//             finally
//             {
//                 _watcher.ErrorOccurred -= HandleError;
//             }

//             void HandleError(string path, Exception ex)
//             {
//                 lastError = ex;
//                 errorTcs.TrySetResult(true);
//             }
//         }

//         Assert.True(success, $"Truncate failed after {MaxRetries} retries. Last error: {lastError?.Message}");
//         Assert.Equal(fileName, _capturedPath);
//         Assert.Equal(0, _capturedSize);
//     }
//     [Fact]
//     public async Task Deleted_File_TriggersFileDeletedEvent()
//     {
//         var debounceDelay = TimeSpan.FromMilliseconds(100);
//         _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
//         string fileName = Path.Combine(_tempDirectory!, "test.txt");

//         var retryCount = 0;
//         var success = false;
//         Exception? lastError = null;

//         // Initial setup outside retry loop
//         var createTcs = new TaskCompletionSource<bool>();
//         _watcher!.FileCreated += (path, size) => createTcs.SetResult(true);
//         await File.WriteAllTextAsync(fileName, "content");
//         await WaitWithTimeoutForTcs(createTcs);

//         while (retryCount < MaxRetries && !success)
//         {
//             ResetEventSources();
//             lastError = null;

//             var errorTcs = new TaskCompletionSource<bool>();
//             _watcher.ErrorOccurred += HandleError;

//             try
//             {
//                 _watcher.FileDeleted += (path) =>
//                 {
//                     _capturedPath = path;
//                     _eventTcs!.SetResult(true);
//                 };

//                 // Ensure file exists before deletion attempt
//                 if (!File.Exists(fileName)) await File.WriteAllTextAsync(fileName, "retry content");

//                 File.Delete(fileName);

//                 var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
//                 var completedTask = await Task.WhenAny(_eventTcs!.Task, errorTcs.Task, timeoutTask);

//                 if (completedTask == _eventTcs.Task)
//                 {
//                     success = true;
//                     break;
//                 }

//                 if (completedTask == errorTcs.Task)
//                 {
//                     retryCount++;
//                     await Task.Delay(RetryDelayMs);
//                 }
//             }
//             finally
//             {
//                 _watcher.ErrorOccurred -= HandleError;
//                 _watcher.FileDeleted -= null;
//             }

//             void HandleError(string path, Exception ex)
//             {
//                 lastError = ex;
//                 errorTcs.TrySetResult(true);
//             }
//         }

//         Assert.True(success, $"Delete failed after {MaxRetries} retries. Last error: {lastError?.Message}");
//         Assert.Equal(fileName, _capturedPath);
//     }

//     [Fact]
//     public async Task Appended_File_TriggersFileAppendedEvent()
//     {
//         var debounceDelay = TimeSpan.FromMilliseconds(100);
//         _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
//         string fileName = Path.Combine(_tempDirectory!, "test.txt");

//         var retryCount = 0;
//         var success = false;
//         Exception? lastError = null;

//         // Initial file creation
//         var createTcs = new TaskCompletionSource<bool>();
//         _watcher.FileCreated += (path, size) => createTcs.SetResult(true);
//         await File.WriteAllTextAsync(fileName, "initial");
//         await WaitWithTimeoutForTcs(createTcs);

//         while (retryCount < MaxRetries && !success)
//         {
//             ResetEventSources();
//             lastError = null;

//             var errorTcs = new TaskCompletionSource<bool>();
//             _watcher.ErrorOccurred += HandleError;

//             try
//             {
//                 _watcher.FileAppended += (path, size) =>
//                 {
//                     _capturedPath = path;
//                     _capturedSize = size;
//                     _eventTcs!.SetResult(true);
//                 };

//                 // Reset file state before append
//                 await File.WriteAllTextAsync(fileName, "initial");
//                 await File.AppendAllTextAsync(fileName, "appended");

//                 var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
//                 var completedTask = await Task.WhenAny(_eventTcs!.Task, errorTcs.Task, timeoutTask);

//                 if (completedTask == _eventTcs.Task)
//                 {
//                     success = true;
//                     break;
//                 }

//                 if (completedTask == errorTcs.Task)
//                 {
//                     retryCount++;
//                     await Task.Delay(RetryDelayMs);
//                 }
//             }
//             finally
//             {
//                 _watcher.ErrorOccurred -= HandleError;
//                 _watcher.FileAppended -= null;
//             }

//             void HandleError(string path, Exception ex)
//             {
//                 lastError = ex;
//                 errorTcs.TrySetResult(true);
//             }
//         }

//         Assert.True(success, $"Append failed after {MaxRetries} retries. Last error: {lastError?.Message}");
//         Assert.Equal(fileName, _capturedPath);
//         Assert.Equal(new FileInfo(fileName).Length, _capturedSize);
//     }


//     // [Fact]
//     // public async Task Renamed_File_TriggersFileMovedEvent()
//     // {
//     //     var debounceDelay = TimeSpan.FromMilliseconds(100);
//     //     _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
//     //     string oldFileName = Path.Combine(_tempDirectory!, "old.txt");
//     //     string newFileName = Path.Combine(_tempDirectory!, "new.txt");

//     //     var retryCount = 0;
//     //     var success = false;
//     //     Exception? lastError = null;

//     //     // Initial file creation
//     //     var createTcs = new TaskCompletionSource<bool>();
//     //     _watcher!.FileCreated += (path, size) => createTcs.SetResult(true);
//     //     await File.WriteAllTextAsync(oldFileName, "content");
//     //     await WaitWithTimeoutForTcs(createTcs);

//     //     while (retryCount < MaxRetries && !success)
//     //     {
//     //         ResetEventSources();
//     //         lastError = null;

//     //         var errorTcs = new TaskCompletionSource<bool>();
//     //         _watcher.ErrorOccurred += HandleError;
//     //         Action<string, string>? moveHandler = null;

//     //         try
//     //         {
//     //             moveHandler = (oldPath, newPath) =>
//     //             {
//     //                 _capturedOldPath = oldPath;
//     //                 _capturedNewPath = newPath;
//     //                 _eventTcs!.SetResult(true);
//     //             };
//     //             _watcher.FileMoved += moveHandler;

//     //             // Cleanup previous attempt
//     //             if (File.Exists(newFileName)) File.Delete(newFileName);
//     //             if (!File.Exists(oldFileName)) await File.WriteAllTextAsync(oldFileName, "retry content");

//     //             await PerformAtomicMove(oldFileName, newFileName);

//     //             var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
//     //             var completedTask = await Task.WhenAny(_eventTcs!.Task, errorTcs.Task, timeoutTask);

//     //             if (completedTask == _eventTcs.Task)
//     //             {
//     //                 success = true;
//     //                 break;
//     //             }

//     //             if (completedTask == errorTcs.Task)
//     //             {
//     //                 retryCount++;
//     //                 await Task.Delay(RetryDelayMs);
//     //             }
//     //         }
//     //         finally
//     //         {
//     //             _watcher.ErrorOccurred -= HandleError;
//     //             _watcher.FileMoved -= moveHandler;
//     //         }

//     //         void HandleError(string path, Exception ex)
//     //         {
//     //             lastError = ex;
//     //             errorTcs.TrySetResult(true);
//     //         }
//     //     }

//     //     Assert.True(success, $"Move failed after {MaxRetries} retries. Last error: {lastError?.Message}");
//     //     Assert.Equal(oldFileName, _capturedOldPath);
//     //     Assert.Equal(newFileName, _capturedNewPath);
//     // }


//     private async Task PerformAtomicMove(string source, string dest, CancellationToken ct = default)
//     {
//         const int moveAttempts = 3;
//         for (int i = 0; i < moveAttempts; i++)
//         {
//             try
//             {
//                 File.Move(source, dest);
//                 return;
//             }
//             catch (IOException) when (i < moveAttempts - 1)
//             {
//                 await Task.Delay(200 * (i + 1), ct);
//                 await EnsureFileUnlocked(source, 3, 100, ct);
//             }
//         }
//     }

//     private async Task EnsureFileUnlocked(string path, int maxRetries, int delayMs, CancellationToken ct)
//     {
//         for (int i = 0; i < maxRetries; i++)
//         {
//             try
//             {
//                 using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
//                 return;
//             }
//             catch (IOException) when (i < maxRetries - 1)
//             {
//                 await Task.Delay(delayMs * (i + 1), ct);
//             }
//         }
//     }

//     // [Fact]
//     // public async Task Appended_Utf8File_TriggersFileAppendedEvent()
//     // {
//     //     var debounceDelay = TimeSpan.FromMilliseconds(100);
//     //     _watcher = new FileWatcher(_tempDirectory!, debounceDelay, trackInitialFiles: false);
//     //     string fileName = Path.Combine(_tempDirectory!, "test_utf8.txt");

//     //     var retryCount = 0;
//     //     var success = false;
//     //     Exception? lastError = null;

//     //     // Initial file creation
//     //     var createTcs = new TaskCompletionSource<bool>();
//     //     _watcher.FileCreated += (path, size) => createTcs.SetResult(true);
//     //     await File.WriteAllBytesAsync(fileName, "Hello"u8.ToArray());
//     //     await WaitWithTimeoutForTcs(createTcs);

//     //     while (retryCount < MaxRetries && !success)
//     //     {
//     //         ResetEventSources();
//     //         lastError = null;

//     //         var errorTcs = new TaskCompletionSource<bool>();
//     //         _watcher.ErrorOccurred += HandleError;

//     //         try
//     //         {
//     //             _watcher.FileAppended += (path, size) =>
//     //             {
//     //                 _capturedPath = path;
//     //                 _capturedSize = size;
//     //                 _eventTcs!.SetResult(true);
//     //             };

//     //             // Reset file state
//     //             await File.WriteAllBytesAsync(fileName, "Hello"u8.ToArray());
//     //             await File.AppendAllBytesAsync(fileName, " 世界"u8.ToArray());

//     //             var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
//     //             var completedTask = await Task.WhenAny(_eventTcs!.Task, errorTcs.Task, timeoutTask);

//     //             if (completedTask == _eventTcs.Task)
//     //             {
//     //                 success = true;
//     //                 break;
//     //             }

//     //             if (completedTask == errorTcs.Task)
//     //             {
//     //                 retryCount++;
//     //                 await Task.Delay(RetryDelayMs);
//     //             }
//     //         }
//     //         finally
//     //         {
//     //             _watcher.ErrorOccurred -= HandleError;
//     //             _watcher.FileAppended -= null;
//     //         }

//     //         void HandleError(string path, Exception ex)
//     //         {
//     //             lastError = ex;
//     //             errorTcs.TrySetResult(true);
//     //         }
//     //     }

//     //     Assert.True(success, $"UTF-8 append failed after {MaxRetries} retries. Last error: {lastError?.Message}");
//     //     Assert.Equal(fileName, _capturedPath);
//     //     Assert.Equal("Hello 世界"u8.Length, _capturedSize);
//     // }
// }
