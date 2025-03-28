using System.CommandLine;
using colorfulLogs.database;
using colorfulLogs.fileWatcher;
using colorfulLogs.indexTask;
using colorfulLogs.Lib.indexTask;
using colorfulLogs.patterns;
using colorfulLogs.structs;
using Microsoft.EntityFrameworkCore;

class Program
{
    static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand("Log processing CLI tool with file watching capabilities");

        var dataSourceOption = new Option<string>(
            name: "--datasource",
            description: "Name of the data source to use",
            getDefaultValue: () => "data_source");
        dataSourceOption.AddAlias("-d");

        var watchDirOption = new Option<string>(
            name: "--watch-dir",
            description: "Directory to watch for log files",
            getDefaultValue: () => "./");
        watchDirOption.AddAlias("-w");

        var dbOption = new Option<string>(
            name: "--db",
            description: "Database name/connection string",
            getDefaultValue: () => "UserData");
        dbOption.AddAlias("-b");

        rootCommand.AddOption(dataSourceOption);
        rootCommand.AddOption(watchDirOption);
        rootCommand.AddOption(dbOption);

        rootCommand.SetHandler(RunLogProcessor, dataSourceOption, watchDirOption, dbOption);

        await rootCommand.InvokeAsync(args);
    }

    static async Task RunLogProcessor(string dataSourceName, string watchDirectory, string dbName)
    {
        // Initialize and seed the database
        using (var context = new DataContext(dbName))
        {
            context.Database.EnsureCreated();

            if (!context.Patterns.Any())
            {
                var defaultPatterns = DefaultPatterns.GetLogPatterns();
                context.Patterns.AddRange(defaultPatterns);
                await context.SaveChangesAsync();
            }

            var dataSource = await context.DataSources.FirstOrDefaultAsync(ds => ds.Name == dataSourceName);
            if (dataSource == null)
            {
                dataSource = new DataSource { Name = dataSourceName };
                context.DataSources.Add(dataSource);
                await context.SaveChangesAsync();
            }
        }

        // Load patterns from database
        List<Pattern> dbPatterns;
        using (var context = new DataContext(dbName))
        {
            dbPatterns = await context.Patterns
                .Include(p => p.Components)
                .ToListAsync();
        }

        var patternMatcher = new PatternMatcher(dbPatterns, new PatternCompiler());

        // Setup file watcher and manager
        DataSource source;
        List<LocalFile> loadedLocalFiles;

        using (var context = new DataContext(dbName))
        {
            source = await context.DataSources.FirstAsync(ds => ds.Name == dataSourceName);
            loadedLocalFiles = await context.LocalFiles.ToListAsync();
        }

        var debounceDelay = TimeSpan.FromMilliseconds(100);
        var watcher = new FileWatcher(watchDirectory, debounceDelay, trackInitialFiles: false);

        var manager = new LocalFileManager(
            fileWatcher: watcher,
            dataSource: source,
            persistedState: loadedLocalFiles
        );

        int processorCount = Environment.ProcessorCount;
        var taskScheduler = new PriorityTaskScheduler(
            maxHighPriorityThreads: processorCount,
            maxLowPriorityThreads: 0
        );

        manager.OnLinesToIndex += indexedLines =>
        {
            taskScheduler.QueueTask(
                IndexTask.CreateDefault(
                    TaskPriority.High,
                    indexedLines,
                    patternMatcher
                )
            );
        };

        Console.WriteLine($"Started watching directory: {watchDirectory}");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}