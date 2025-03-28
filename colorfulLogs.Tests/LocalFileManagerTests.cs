using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using colorfulLogs.fileWatcher;
using colorfulLogs.structs;

namespace colorfulLogs.Tests
{
    public class LocalFileManagerTests : IDisposable
    {
        private readonly string _tempFolder;
        private readonly DataSource _testDataSource = new DataSource { Id = Guid.NewGuid(), Name = "name" };

        public LocalFileManagerTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempFolder);
        }

        public void Dispose()
        {
            Directory.Delete(_tempFolder, recursive: true);
        }

        [Fact]
        public async Task InitialReconciliation_DetectsNewFile()
        {
            // Arrange
            var filePath = Path.Combine(_tempFolder, "test.txt");
            File.WriteAllText(filePath, "content"); // 7 bytes

            using var watcher = new FileWatcher(_tempFolder, trackInitialFiles: false);
            var manager = new LocalFileManager(watcher, _testDataSource);
            await Task.Delay(500); // Allow reconciliation

            // Assert
            var normalizedPath = Path.GetFullPath(filePath);
            Assert.True(manager.GetTrackedFiles().ContainsKey(normalizedPath));
            Assert.Equal(7, manager.GetTrackedFiles()[normalizedPath].LastLength);
        }

        [Fact]
        public async Task FileAppend_TriggersProcessing()
        {
            // Arrange
            var filePath = Path.Combine(_tempFolder, "live.txt");

            using var watcher = new FileWatcher(_tempFolder, debounceDelay: TimeSpan.FromMilliseconds(50));
            var manager = new LocalFileManager(watcher, _testDataSource);
            var linesProcessed = 0;
            manager.OnLinesToIndex += lines => linesProcessed += lines.Count;

            // Create file after manager starts
            File.WriteAllText(filePath, "line1\n");
            await Task.Delay(500); // Allow creation event

            // Append new content
            File.AppendAllText(filePath, "line2\n");
            await Task.Delay(500); // Allow append event

            // Assert
            var normalizedPath = Path.GetFullPath(filePath);
            Assert.Equal(12, manager.GetTrackedFiles()[normalizedPath].LastLength);
            Assert.Equal(2, linesProcessed); // Both lines processed
        }

        [Fact]
        public async Task FileTruncate_ResetsState()
        {
            // Arrange
            var filePath = Path.Combine(_tempFolder, "truncate.txt");
            File.WriteAllText(filePath, "initial content"); // 14 bytes

            using var watcher = new FileWatcher(_tempFolder, debounceDelay: TimeSpan.FromMilliseconds(50));
            var manager = new LocalFileManager(watcher, _testDataSource);
            await Task.Delay(500); // Allow initial processing

            // Act: Truncate without deleting the file
            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                fs.SetLength(5); // Truncate to 5 bytes ("initi")
            }
            await Task.Delay(500); // Allow debounce and processing

            // Assert
            var normalizedPath = Path.GetFullPath(filePath);
            Assert.True(manager.GetTrackedFiles().ContainsKey(normalizedPath));
            var trackedFile = manager.GetTrackedFiles()[normalizedPath];
            Assert.Equal(5, trackedFile.LastLength);
            Assert.Equal(0, trackedFile.LastLineNumber); // Reset on truncate
        }

        // Helper method to access internal state
        public static class TestHelpers
        {
            public static Dictionary<string, LocalFile> GetTrackedFiles(LocalFileManager manager) => manager.GetTrackedFiles();
        }
    }
}