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


    SqlCommand.GetRandom<RoleRecord>().WriteToConsole();
    SqlCommand.GetRandom<RoleRecord>(userID).WriteToConsole();
    SqlCommand.WherePaged<RoleRecord>(parameters, 0, 10).WriteToConsole();
    SqlCommand.WherePaged<RoleRecord>(userID,     0, 10).WriteToConsole();
    SqlCommand.WherePaged<RoleRecord>(0,          10).WriteToConsole();
    SqlCommand.WherePaged<RoleRecord>(date,       0, 10).WriteToConsole();

    SqlCommand.Where<RoleRecord, string>(nameof(RoleRecord.NameOfRole), ADMIN).WriteToConsole();

    SqlCommand.Get(id).WriteToConsole();
    SqlCommand.Get<RoleRecord>([id, RecordID<RoleRecord>.New()]).WriteToConsole();
    SqlCommand.Get<RoleRecord>(parameters).WriteToConsole();
    SqlCommand.GetAll<RoleRecord>().WriteToConsole();
    SqlCommand.GetFirst<RoleRecord>().WriteToConsole();
    SqlCommand.GetLast<RoleRecord>().WriteToConsole();
    SqlCommand.GetCount<RoleRecord>().WriteToConsole();
    SqlCommand.GetSortedID<RoleRecord>().WriteToConsole();
    SqlCommand.GetExists<RoleRecord>(parameters).WriteToConsole();

    SqlCommand.GetDelete<RoleRecord>(parameters).WriteToConsole();
    SqlCommand.GetDelete(id).WriteToConsole();
    SqlCommand.GetDeleteAll<RoleRecord>().WriteToConsole();

    SqlCommand.GetNext(pair).WriteToConsole();
    SqlCommand.GetNextID(pair).WriteToConsole();

    SqlCommand.GetCopy<RoleRecord>().WriteToConsole();
    SqlCommand.GetInsert(record).WriteToConsole();
    SqlCommand.GetInsert<RoleRecord>([record, record]).WriteToConsole();

    SqlCommand.GetUpdate(record).WriteToConsole();
    SqlCommand.GetTryInsert(record, parameters).WriteToConsole();
    SqlCommand.InsertOrUpdate(record, parameters).WriteToConsole();

    // await TestDatabase.TestAsync();
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }
