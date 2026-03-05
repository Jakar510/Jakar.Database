using Jakar.Database;


[assembly: Experimental("SqlTableBuilder")]


try
{
    "Hello World!".WriteToConsole();
    Console.WriteLine();

    string           admin   = "admin";
    Guid[]           ids     = Enumerable.Range(0, 3).Select(static x => Guid.CreateVersion7()).ToArray();
    string[]         strings = Enumerable.Range(0, 3).Select(static x => Randoms.RandomString(Random.Shared.Next(3, 10))).Append(Guid.NewGuid().ToString()).ToArray();
    DateTimeOffset[] dates   = Enumerable.Range(0, 3).Select(static x => DateTimeOffset.UtcNow + TimeSpan.FromDays(Random.Shared.Next(-10, 10))).ToArray();

    SqlInterpolatedStringHandler<RoleRecord> handler = $"""
                                                        select * from {RoleRecord.TABLE_NAME}
                                                        where 
                                                        {nameof(RoleRecord.NameOfRole)} = @{admin} 
                                                        or 
                                                        {nameof(RoleRecord.NormalizedName)} = {admin}
                                                        and 
                                                        {nameof(RoleRecord.UserID)} in ({ids});

                                                        select {strings};
                                                        
                                                        select {dates};

                                                        select {default(string)};
                                                        """;


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
