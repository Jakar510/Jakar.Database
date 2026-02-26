using Jakar.Database;


[assembly: Experimental("SqlTableBuilder")]


try
{
    "Hello World!".WriteToConsole();
    Console.WriteLine();

    // Jakar.Database.Activities.Tags.Print();

    // MigrationManager.CreateDatabase = MigrationRecord.CreateDatabase(0, nameof(TestDatabase), "dev");

    // TestDatabase.PrintCreateTables();

    Console.WriteLine();
    Console.WriteLine(MigrationRecord.TryCreateSql);
    Console.WriteLine();
    Console.WriteLine(MigrationRecord.ApplySql);
    Console.WriteLine();
    Console.WriteLine(MigrationRecord.SelectSql);
    Console.WriteLine();

    await TestDatabase.TestAsync();
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }
