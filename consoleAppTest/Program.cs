using consoleAppTest.database;
using consoleAppTest.fileWatcher;
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

void addDebugHooksToWatcher(FileWatcher watcher)
{
    watcher.FileCreated += (path, size) =>
    {
        Console.WriteLine($"File was created at {path} with size {size}");
    };

    watcher.FileAppended += (path, new_size) =>
    {
        Console.WriteLine($"File was appended at {path} with new size {new_size}");
    };

    watcher.FileDeleted += (path) =>
    {
        Console.WriteLine($"File was deleted at {path}");
    };

    watcher.FileMoved += (old_path, new_path) =>
    {
        Console.WriteLine($"File was moved from {old_path} to {new_path}");
    };

    watcher.FileTruncated += (path, size) =>
    {
        Console.WriteLine($"File was truncated at {path} with new size {size}");
    };

    watcher.ErrorOccurred += (err, ex) =>
    {
        Console.WriteLine($"there was an error {err}: {ex}");
    };
}

// addDebugHooksToWatcher(watcher);

LocalFileManager manager = new(fileWatcher: watcher, new DataSource
{
    Name = "test"
});



Console.In.Read();