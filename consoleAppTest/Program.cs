using consoleAppTest.fileWatcher;
using consoleAppTest.patterns;
using consoleAppTest.structs;
using Microsoft.EntityFrameworkCore.Infrastructure;

Console.WriteLine("Hello, World!");

// using (DataContext context = new("UserData"))
// {
//     DatabaseFacade databaseFacade = new(context);
//     databaseFacade.EnsureCreated();

//     var dataSource = new DataSource()
//     {
//         Id = Guid.NewGuid(),
//         Name = "test",
//     };

//     context.DataSources.Add(dataSource);

//     context.SaveChanges();

//     var allDataSources = context.DataSources.ToList();
// }


var debounceDelay = TimeSpan.FromMilliseconds(100);
var watcher = new FileWatcher("watcher_folder/", debounceDelay, trackInitialFiles: false);


// addDebugHooksToWatcher(watcher);

PatternMatcher patternMatcher = new(DefaultPatterns.GetLogPatterns(), new PatternCompiler());

DataSource source = new()
{
    Name = "test"
};

LocalFileManager manager = new(fileWatcher: watcher, source);

manager.OnLineIndexed += indexedLine =>
{
    // Send to other systems or store in database
    Console.WriteLine($"Line {indexedLine.LineNumber}: {indexedLine.LineText}");

    var values = patternMatcher.ProcessLine(indexedLine);

    foreach (IndexedValue value in values)
    {
        Console.WriteLine($"new value: {value.Value} from {value.Pattern.PatternName}");
    }
};

Console.In.Read();