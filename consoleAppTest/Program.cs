using consoleAppTest.database;
using consoleAppTest.structs;
using Microsoft.EntityFrameworkCore.Infrastructure;

Console.WriteLine("Hello, World!");



using (DataContext context = new("UserData"))
{
    DatabaseFacade databaseFacade = new(context);
    databaseFacade.EnsureCreated();

    var dataSource = new DataSource()
    {
        Id = Guid.NewGuid(),
        Name = "test",
    };

    context.DataSources.Add(dataSource);

    context.SaveChanges();

    var allDataSources = context.DataSources.ToList();
}






