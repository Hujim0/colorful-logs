using consoleAppTest.fileWatcher;
using consoleAppTest.structs;

namespace consoleAppTest.Tests
{
    public class FileWatcherTests : IDisposable
    {
        private readonly string _tempDirectory;

        public FileWatcherTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }

        public static DataSource GetDataSource() {
            return new DataSource {
                Id = Guid.NewGuid(),
                Name = "test source",  
            };
        }

        [Fact]
        public void FileCreated_WhenFileCreated_EventFires()
        {
            using var watcher = new FileWatcher(_tempDirectory, GetDataSource());
            var resetEvent = new ManualResetEventSlim();
            string createdPath = "";

            watcher.FileCreated += (s, e) =>
            {
                createdPath = e.FullPath;
                resetEvent.Set();
            };

            var testFile = Path.Combine(_tempDirectory, "test.txt");
            File.WriteAllText(testFile, "content");

            Assert.True(resetEvent.Wait(1000), "Timeout waiting for file creation");
            Assert.Equal(testFile, createdPath);
        }

        [Fact]
        public void FileDeleted_WhenFileDeleted_EventFires()
        {
            var testFile = Path.Combine(_tempDirectory, "test.txt");
            File.WriteAllText(testFile, "content");

            using var watcher = new FileWatcher(_tempDirectory, GetDataSource());
            var resetEvent = new ManualResetEventSlim();
            string deletedPath = "";

            watcher.FileDeleted += (s, e) =>
            {
                deletedPath = e.FullPath;
                resetEvent.Set();
            };

            File.Delete(testFile);

            Assert.True(resetEvent.Wait(1000), "Timeout waiting for file deletion");
            Assert.Equal(testFile, deletedPath);
        }

        [Fact]
        public void FileChangedWithoutAppend_NoAppendEvent()
        {
            var testFile = Path.Combine(_tempDirectory, "test.txt");
            File.WriteAllText(testFile, "initial content");

            using var watcher = new FileWatcher(_tempDirectory, GetDataSource());
            var resetEvent = new ManualResetEventSlim();
            bool eventFired = false;

            watcher.FileAppended += (s, e) =>
            {
                eventFired = true;
                resetEvent.Set();
            };

            File.WriteAllText(testFile, "short");

            Assert.False(resetEvent.Wait(500), "Append event fired unexpectedly");
            Assert.False(eventFired);
        }

        [Fact]
        public void FileMoved_WhenFileMoved_EventFires()
        {
            var sourceFile = Path.Combine(_tempDirectory, "source.txt");
            var destFile = Path.Combine(_tempDirectory, "dest.txt");
            File.WriteAllText(sourceFile, "content");

            using var watcher = new FileWatcher(_tempDirectory, GetDataSource());
            var resetEvent = new ManualResetEventSlim();
            string oldPath = "";
            string newPath = "";

            watcher.FileMoved += (s, e) =>
            {
                oldPath = e.OldFullPath;
                newPath = e.FullPath;
                resetEvent.Set();
            };

            File.Move(sourceFile, destFile);

            Assert.True(resetEvent.Wait(1000), "Timeout waiting for file move");
            Assert.Equal(sourceFile, oldPath);
            Assert.Equal(destFile, newPath);
        }


        [Fact]
        public void EventsNotFiredAfterDisposal()
        {
            var watcher = new FileWatcher(_tempDirectory, GetDataSource());
            var resetEvent = new ManualResetEventSlim();
            bool eventFired = false;

            watcher.FileCreated += (s, e) =>
            {
                eventFired = true;
                resetEvent.Set();
            };

            watcher.Dispose();

            var testFile = Path.Combine(_tempDirectory, "test.txt");
            File.WriteAllText(testFile, "content");

            Assert.False(resetEvent.Wait(500), "Event fired after disposal");
            Assert.False(eventFired);
        }

        [Fact]
        public void FileCreated_InSubdirectory_EventFires()
        {
            using var watcher = new FileWatcher(_tempDirectory, GetDataSource());
            var resetEvent = new ManualResetEventSlim();
            string createdPath = "";

            watcher.FileCreated += (s, e) =>
            {
                createdPath = e.FullPath;
                resetEvent.Set();
            };

            var subDir = Path.Combine(_tempDirectory, "subdir");
            Directory.CreateDirectory(subDir);
            var testFile = Path.Combine(subDir, "test.txt");
            File.WriteAllText(testFile, "content");

            Assert.True(resetEvent.Wait(1000), "Timeout waiting for subdir file creation");
            Assert.Equal(testFile, createdPath);
        }

        [Fact]
        public void FileAppended_WhenFileAppended_EventFires()
        {
            //first initialize so it adds file to hashmap
            using var watcher = new FileWatcher(_tempDirectory, GetDataSource());

            var testFile = Path.Combine(_tempDirectory, "test.txt");
            File.WriteAllText(testFile, "initial \n");

            var resetEvent = new ManualResetEventSlim();
            string appendedPath = "";

            watcher.FileAppended += (s, e) =>
            {
                appendedPath = e.FullPath;
                resetEvent.Set();
            };

            File.AppendAllText(testFile, "append");

            Assert.True(resetEvent.Wait(1000), "Timeout waiting for file append");
            Assert.Equal(testFile, appendedPath);
        }

    }
}
