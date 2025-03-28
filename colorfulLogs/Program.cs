using colorfulLogs.database;
using colorfulLogs.fileWatcher;
using colorfulLogs.indexTask;
using colorfulLogs.patterns;
using colorfulLogs.structs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Linq;

Console.WriteLine("Hello, World!");

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
using (var context = new DataContext("UserData"))
{
    source = context.DataSources.First(ds => ds.Name == "test");
}

var debounceDelay = TimeSpan.FromMilliseconds(100);
var watcher = new FileWatcher("watcher_folder/", debounceDelay, trackInitialFiles: false);

LocalFileManager manager = new(fileWatcher: watcher, source);

int processorCount = Environment.ProcessorCount;
PriorityTaskScheduler taskScheduler = new(processorCount / 2, processorCount / 2);


manager.OnLinesToIndex += indexedLines =>
{
    taskScheduler.QueueTask(new IndexTask()
    {
        Work = () =>
        {
            lock (DataContext.DbLock) // Serialize writes
            {
                // Save IndexedLines first

                using var lineContext = new DataContext("UserData");
                lineContext.DataSources.Attach(source);

                lineContext.IndexedLines.AddRange(indexedLines);
                lineContext.SaveChanges();
            }

            List<IndexedValue> allValues = [];
            List<TagInstance> allTagInstances = [];

            foreach (var indexedLine in indexedLines)
            {
                Console.WriteLine($"Line {indexedLine.LineNumber}: {indexedLine.LineText}");

                var values = patternMatcher.ProcessLine(indexedLine);
                allValues.AddRange(values);

                foreach (var value in values)
                {
                    allTagInstances.AddRange(value.TagInstances);
                }
            }

            // Save IndexedValues
            if (allValues.Count > 0 && allTagInstances.Count > 0)
            {
                lock (DataContext.DbLock) // Serialize writes
                {
                    using var valueContext = new DataContext("UserData");
                    valueContext.DataSources.Attach(source);

                    var uniquePatterns = allValues.Select(v => v.Pattern).DistinctBy(p => p.Id).ToList();

                    foreach (var pattern in uniquePatterns)
                    {
                        valueContext.Attach(pattern);
                        valueContext.Entry(pattern).State = EntityState.Unchanged;
                    }
                    valueContext.IndexedValues.AddRange(allValues);

                    foreach (var tagInstance in allTagInstances)
                    {
                        valueContext.Attach(tagInstance);
                    }

                    valueContext.TagInstances.AddRange(allTagInstances);
                    valueContext.SaveChanges();
                }
            }
        }
    });
};

Console.Read();