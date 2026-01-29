using System.Runtime.CompilerServices;
using Jakar.Database;


[assembly: Experimental("SqlTableBuilder")]


try
{
    "Hello World!".WriteToConsole();
    Console.WriteLine();
    /*
#pragma warning disable OpenTelemetry // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    Jakar.Database.Activities.Tags.Print();
#pragma warning restore OpenTelemetry // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    */

    // TODO: TestDatabase.TestAsync();


    string userSql             = Migrations.CreateTableSql<UserRecord>();
    string addressSql          = Migrations.CreateTableSql<AddressRecord>();
    string userAddressSql      = Migrations.CreateTableSql<UserAddressRecord>();
    string groupSql            = Migrations.CreateTableSql<GroupRecord>();
    string userGroupSql        = Migrations.CreateTableSql<UserGroupRecord>();
    string roleSql             = Migrations.CreateTableSql<RoleRecord>();
    string userRoleSql         = Migrations.CreateTableSql<UserRoleRecord>();
    string recoveryCodeSql     = Migrations.CreateTableSql<RecoveryCodeRecord>();
    string userRecoveryCodeSql = Migrations.CreateTableSql<UserRecoveryCodeRecord>();
    string fileSql             = Migrations.CreateTableSql<FileRecord>();
    string loginProviderSql    = Migrations.CreateTableSql<UserLoginProviderRecord>();

    printSql(userSql);
    printSql(groupSql);
    printSql(userGroupSql);
    printSql(addressSql);
    printSql(userAddressSql);
    printSql(roleSql);
    printSql(userRoleSql);
    printSql(recoveryCodeSql);
    printSql(userRecoveryCodeSql);
    printSql(fileSql);
    printSql(loginProviderSql);
}
catch ( Exception e ) { e.WriteToConsole(); }
finally { "Bye".WriteToConsole(); }

return;

static void printSql( string sql, [CallerArgumentExpression(nameof(sql))] string variableName = EMPTY )
{
    const string BOUNDARY = "================================";
    Console.WriteLine(BOUNDARY);
    Console.WriteLine();
    Console.WriteLine(variableName);
    Console.WriteLine();
    Console.WriteLine(sql);
    Console.WriteLine();
    Console.WriteLine(BOUNDARY);
}
