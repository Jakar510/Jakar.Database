using Jakar.Database;


[assembly: Experimental("SqlTableBuilder")]


try
{
    "Hello World!".WriteToConsole();
    Console.WriteLine();

    // Jakar.Database.Activities.Tags.Print();
    

    // await TestDatabase.TestAsync();

    TestDatabase.PrintCreateTables();
    
    Console.WriteLine();
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }
