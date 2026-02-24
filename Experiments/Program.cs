using Jakar.Database;


[assembly: Experimental("SqlTableBuilder")]


try
{
    "Hello World!".WriteToConsole();
    Console.WriteLine();

    // Jakar.Database.Activities.Tags.Print();

    // MigrationManager.CreateDatabase = MigrationRecord.CreateDatabase(0, nameof(TestDatabase), "dev");

    // TestDatabase.PrintCreateTables();

    await TestDatabase.TestAsync();
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }
