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

    SqlResult sql = Jakar.SqlBuilder.SqlBuilder.PostgreSQL.Select<RoleRecord>(nameof(RoleRecord.NameOfRole)).From<RoleRecord>().Where<RoleRecord>(nameof(RoleRecord.NormalizedName)).In("test", "foo", "bar").Build();

    #pragma warning disable IL3050
    #pragma warning disable IL2026
    Console.WriteLine(sql.Sql);
    Console.WriteLine(sql.Dialect.ToString());
    Console.WriteLine(JsonConvert.SerializeObject(sql.Parameters, Formatting.Indented));
    #pragma warning restore IL2026
    #pragma warning restore IL3050
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }
