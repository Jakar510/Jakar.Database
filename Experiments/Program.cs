using Jakar.Database;


[assembly: Experimental("SqlTableBuilder")]


try
{
    "Hello World!".WriteToConsole();
    Console.WriteLine();
    
    // TestDatabase.PrintCreateTables();
    
    await TestDatabase.TestAsync();
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }
