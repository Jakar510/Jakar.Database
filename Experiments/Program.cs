using System.Runtime.CompilerServices;
using Jakar.Database;


[assembly: Experimental("SqlTableBuilder")]


try
{
    "Hello World!".WriteToConsole();
    Console.WriteLine();

    // TestDatabase.PrintCreateTables();
    const string           ADMIN      = "Admin";
    DateTimeOffset         date       = DateTimeOffset.UtcNow - TimeSpan.FromDays(5);
    RecordID<UserRecord>   userID     = RecordID<UserRecord>.New();
    RecordID<RoleRecord>   id         = RecordID<RoleRecord>.New();
    RecordPair<RoleRecord> pair       = new(id, date);
    RoleRecord             record     = new(ADMIN, ADMIN, Randoms.RandomString(10), new UserRights(""), id, userID, date);
    PostgresParameters     parameters = PostgresParameters.Create<RoleRecord>();
    parameters.Add(nameof(RoleRecord.NameOfRole),     ADMIN);
    parameters.Add(nameof(RoleRecord.NormalizedName), "Admin");


    writeLine(SqlCommand.GetRandom<RoleRecord>().ToString());

    writeLine(SqlCommand.GetRandom<RoleRecord>(userID).ToString());
    writeLine(SqlCommand.WherePaged<RoleRecord>(parameters, 0, 10).ToString());
    writeLine(SqlCommand.WherePaged<RoleRecord>(userID,     0, 10).ToString());
    writeLine(SqlCommand.WherePaged<RoleRecord>(0,          10).ToString());
    writeLine(SqlCommand.WherePaged<RoleRecord>(date,       0, 10).ToString());

    writeLine(SqlCommand.Where<RoleRecord, string>(nameof(RoleRecord.NameOfRole), ADMIN).ToString());

    writeLine(SqlCommand.Get(id).ToString());
    writeLine(SqlCommand.Get<RoleRecord>([id, RecordID<RoleRecord>.New()]).ToString());
    writeLine(SqlCommand.Get<RoleRecord>(parameters).ToString());
    writeLine(SqlCommand.GetAll<RoleRecord>().ToString());
    writeLine(SqlCommand.GetFirst<RoleRecord>().ToString());
    writeLine(SqlCommand.GetLast<RoleRecord>().ToString());
    writeLine(SqlCommand.GetCount<RoleRecord>().ToString());
    writeLine(SqlCommand.GetSortedID<RoleRecord>().ToString());
    writeLine(SqlCommand.GetExists<RoleRecord>(parameters).ToString());

    writeLine(SqlCommand.GetDelete<RoleRecord>(parameters).ToString());
    writeLine(SqlCommand.GetDelete(id).ToString());
    writeLine(SqlCommand.GetDelete([id, RecordID<RoleRecord>.New()]).ToString());
    writeLine(SqlCommand.GetDeleteAll<RoleRecord>().ToString());

    writeLine(SqlCommand.GetNext(pair).ToString());
    writeLine(SqlCommand.GetNextID(pair).ToString());

    writeLine(SqlCommand.GetCopy<RoleRecord>().ToString());
    writeLine(SqlCommand.GetInsert(record).ToString());
    writeLine(SqlCommand.GetInsert<RoleRecord>([record, record]).ToString());

    writeLine(SqlCommand.GetUpdate(record).ToString());
    writeLine(SqlCommand.GetTryInsert(record, parameters).ToString());
    writeLine(SqlCommand.InsertOrUpdate(record, parameters).ToString());

    // await TestDatabase.TestAsync();
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }

return;

static void writeLine( string line, [CallerArgumentExpression(nameof(line))] string paramName = EMPTY )
{
    string header = new('=', paramName.Length + 20);
    Console.WriteLine();
    Console.WriteLine(header);
    Console.WriteLine(paramName.PadLeft(header.Length - 10).PadRight(header.Length));
    Console.WriteLine(header);
    Console.WriteLine();
    Console.WriteLine(line);
    Console.WriteLine();
}
