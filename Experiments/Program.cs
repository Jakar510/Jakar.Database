using System.Runtime.CompilerServices;
using InterpolatedSql.SqlBuilders;
using Jakar.Database;
using Jakar.SqlBuilder;
using Newtonsoft.Json;


[assembly: Experimental("SqlTableBuilder")]


try
{
    "Hello World!".WriteToConsole();
    Console.WriteLine();

    // TestDatabase.TestSQL();

    // await TestDatabase.TestAsync();

    SqlResult sql = Jakar.SqlBuilder.SqlBuilder.PostgreSQL.CreateTable<RoleRecord>().Build();

    #pragma warning disable IL3050
    #pragma warning disable IL2026
    Console.WriteLine(JsonConvert.SerializeObject(sql, Formatting.Indented));
    #pragma warning restore IL2026
    #pragma warning restore IL3050
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }
