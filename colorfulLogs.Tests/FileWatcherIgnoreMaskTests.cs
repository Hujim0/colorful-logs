using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace colorfulLogs.fileWatcher.Tests
{
    public class FileWatcherIgnoreMaskTests : IDisposable
    {
        private readonly List<IDisposable> _disposables = new();
        private readonly string _tempDir;

        public FileWatcherIgnoreMaskTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }

            try { Directory.Delete(_tempDir, true); }
            catch { /* Ignore cleanup errors */ }
        }

        private FileWatcher CreateWatcher(
            IEnumerable<string>? ignoreMasks = null,
            bool trackInitialFiles = false,
            TimeSpan? debounceDelay = null)
        {
            var watcher = new FileWatcher(
                _tempDir,
                debounceDelay: debounceDelay ?? TimeSpan.FromMilliseconds(10),
                trackInitialFiles: trackInitialFiles,
                ignoreFileMasks: ignoreMasks);

            _disposables.Add(watcher);
            return watcher;
        }

        [Fact]
        public async Task IgnoredFile_DoesNotTriggerEvents()
        {
            // Arrange
            var events = new List<string>();
            var watcher = CreateWatcher(new[] { "*.tmp" });

            watcher.FileCreated += (p, _) => events.Add("created");
            watcher.FileAppended += (p, _) => events.Add("appended");
            watcher.FileDeleted += _ => events.Add("deleted");

            // Act
            var filePath = Path.Combine(_tempDir, "test.tmp");
            await File.WriteAllTextAsync(filePath, "content");
            await Task.Delay(100);

            await File.AppendAllTextAsync(filePath, "more content");
            await Task.Delay(100);

            File.Delete(filePath);
            await Task.Delay(100);

            // Assert
            Assert.Empty(events);
        }

        [Fact]
        public async Task NonIgnoredFile_TriggersEvents()
        {
            // Arrange
            var events = new List<string>();
            var watcher = CreateWatcher(new[] { "*.tmp" });

            watcher.FileCreated += (p, _) => events.Add("created");
            watcher.FileAppended += (p, _) => events.Add("appended");

            // Act
            var filePath = Path.Combine(_tempDir, "test.log");
            await File.WriteAllTextAsync(filePath, "content");
            await Task.Delay(100);

            await File.AppendAllTextAsync(filePath, "more content");
            await Task.Delay(100);

            // Assert
            Assert.Equal(new[] { "created", "appended" }, events);
        }

        [Fact]
        public async Task RenameToIgnored_RemovesFromTracking()
        {
            // Arrange
            var createdEvents = new List<string>();
            var deletedEvents = new List<string>();
            var watcher = CreateWatcher(new[] { "*.tmp" });

            watcher.FileCreated += (p, _) => createdEvents.Add(Path.GetFileName(p));
            watcher.FileDeleted += p => deletedEvents.Add(Path.GetFileName(p));

            // Act
            var originalPath = Path.Combine(_tempDir, "file.log");
            await File.WriteAllTextAsync(originalPath, "content");
            await Task.Delay(100);

            var newPath = Path.Combine(_tempDir, "file.tmp");
            File.Move(originalPath, newPath);
            await Task.Delay(100);

            // Assert
            Assert.Equal(new[] { "file.log" }, createdEvents);
            Assert.Equal(new[] { "file.log" }, deletedEvents);
        }

        [Fact]
        public async Task RenameFromIgnored_AddsToTracking()
        {
            // Arrange
            var createdEvents = new List<string>();
            var watcher = CreateWatcher(new[] { "*.tmp" });

            watcher.FileCreated += (p, _) => createdEvents.Add(Path.GetFileName(p));

            // Act
            var originalPath = Path.Combine(_tempDir, "file.tmp");
            await File.WriteAllTextAsync(originalPath, "content");
            await Task.Delay(100);

            var newPath = Path.Combine(_tempDir, "file.log");
            File.Move(originalPath, newPath);
            await Task.Delay(100);

            // Assert
            Assert.Equal(new[] { "file.log" }, createdEvents);
        }

        [Fact]
        public void Initialization_IgnoresMatchingFiles()
        {
            // Arrange
            var filePath = Path.Combine(_tempDir, "ignore.me");
            File.WriteAllText(filePath, "content");

            // Act
            var events = new List<string>();
            var watcher = CreateWatcher(new[] { "*.me" }, trackInitialFiles: true);
            watcher.FileCreated += (p, _) => events.Add(p);

            // Assert
            Assert.Empty(events);
        }

        [Fact]
        public void CaseInsensitive_Matching()
        {
            // Arrange
            var events = new List<string>();
            var watcher = CreateWatcher(new[] { "*.TMP" });
            watcher.FileCreated += (p, _) => events.Add(p);

            // Act
            var filePath = Path.Combine(_tempDir, "FILE.tmp");
            File.WriteAllText(filePath, "content");
            Thread.Sleep(100);

            // Assert
            Assert.Empty(events);
        }

        [Fact]
        public async Task Subdirectory_IgnoredPattern()
        {
            // Arrange
            var subDir = Path.Combine(_tempDir, "logs");
            Directory.CreateDirectory(subDir);

            var events = new List<string>();
            // Change pattern to match any .tmp in logs directory
            var watcher = CreateWatcher(new[] { "logs/*.tmp" });
            watcher.FileCreated += (p, _) => events.Add(p);

            // Act
            var filePath = Path.Combine(subDir, "file.tmp");
            await File.WriteAllTextAsync(filePath, "content");
            await Task.Delay(100);

            // Assert
            Assert.Empty(events);
        }

        [Fact]
        public async Task MultipleMasks_AreRespected()
        {
            // Arrange
            var events = new List<string>();
            var watcher = CreateWatcher(new[] { "*.tmp", "backup.*" });
            watcher.FileCreated += (p, _) => events.Add(p);

            // Act
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "test.tmp"), "content");
            await File.WriteAllTextAsync(Path.Combine(_tempDir, "backup.log"), "content");
            await Task.Delay(100);

            // Assert
            Assert.Empty(events);
        }
    }
}