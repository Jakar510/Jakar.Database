using Jakar.Database;


[assembly: Experimental("SqlTableBuilder")]


try
{
    "Hello World!".WriteToConsole();
    Console.WriteLine();

    // Jakar.Database.Activities.Tags.Print();

    MigrationManager.CreateDatabase = MigrationRecord.CreateDatabase(0, nameof(TestDatabase), "dev");
    await TestDatabase.TestAsync();

    // TestDatabase.PrintCreateTables();
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }
