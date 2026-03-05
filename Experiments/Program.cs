using Jakar.Database;


[assembly: Experimental("SqlTableBuilder")]


try
{
    "Hello World!".WriteToConsole();
    Console.WriteLine();

    string admin = "admin";
    Guid[] ids = Enumerable.Range(0, 10).Select(static x => Guid.CreateVersion7()).ToArray();

    SqlInterpolatedStringHandler<RoleRecord> handler = $"select * from {RoleRecord.TABLE_NAME} where {nameof(RoleRecord.NameOfRole)} = @{admin} and {nameof(RoleRecord.UserID)} in ({ids})";


    Console.WriteLine(handler.ToString());


    // TestDatabase.PrintCreateTables();

    // Console.WriteLine();
    // Console.WriteLine(MigrationRecord.TryCreateSql);
    // Console.WriteLine();
    // Console.WriteLine(MigrationRecord.ApplySql);
    // Console.WriteLine();
    // Console.WriteLine(MigrationRecord.SelectSql);
    // Console.WriteLine();
    //
    // await TestDatabase.TestAsync();
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }
