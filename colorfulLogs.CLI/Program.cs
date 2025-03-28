using colorfulLogs.database;
using colorfulLogs.fileWatcher;
using colorfulLogs.indexTask;
using colorfulLogs.Lib.indexTask;
using colorfulLogs.patterns;
using colorfulLogs.structs;
using Microsoft.EntityFrameworkCore;

// Initialize and seed the database
using (var context = new DataContext("UserData"))
{
    context.Database.EnsureCreated();

    // Seed patterns if none exist
    if (!context.Patterns.Any())
    {
        var defaultPatterns = DefaultPatterns.GetLogPatterns();
        context.Patterns.AddRange(defaultPatterns);
        context.SaveChanges();
    }

    // Ensure DataSource exists
    var dataSource = context.DataSources.FirstOrDefault(ds => ds.Name == "test");
    if (dataSource == null)
    {
        dataSource = new DataSource { Name = "test" };
        context.DataSources.Add(dataSource);
        context.SaveChanges();
    }
}

// Load patterns from the database
List<Pattern> dbPatterns;
using (var context = new DataContext("UserData"))
{
    dbPatterns = context.Patterns.Include(p => p.Components).ToList();
}

PatternMatcher patternMatcher = new(dbPatterns, new PatternCompiler());

// Retrieve the DataSource from the database
DataSource source;
List<LocalFile> loadedLocalFiles;

using (var context = new DataContext("UserData"))
{
    source = context.DataSources.First(ds => ds.Name == "test");

    loadedLocalFiles = context.LocalFiles.ToList();
}

var debounceDelay = TimeSpan.FromMilliseconds(100);
var watcher = new FileWatcher("watcher_folder/", debounceDelay, trackInitialFiles: false);

LocalFileManager manager = new(fileWatcher: watcher, dataSource: source, persistedState: loadedLocalFiles);

int processorCount = Environment.ProcessorCount;
PriorityTaskScheduler taskScheduler = new(maxHighPriorityThreads: processorCount, maxLowPriorityThreads: 0);

manager.OnLinesToIndex += indexedLines =>
{
    taskScheduler.QueueTask(IndexTask.CreateDefault(TaskPriority.High, indexedLines, patternMatcher));
};

Console.Read();