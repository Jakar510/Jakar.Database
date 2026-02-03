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

    /*
    string userSql             = SqlTable<UserRecord>.CreateTable();
    string addressSql          = SqlTable<AddressRecord>.CreateTable();
    string userAddressSql      = SqlTable<UserAddressRecord>.CreateTable();
    string groupSql            = SqlTable<GroupRecord>.CreateTable();
    string userGroupSql        = SqlTable<UserGroupRecord>.CreateTable();
    string roleSql             = SqlTable<RoleRecord>.CreateTable();
    string userRoleSql         = SqlTable<UserRoleRecord>.CreateTable();
    string recoveryCodeSql     = SqlTable<RecoveryCodeRecord>.CreateTable();
    string userRecoveryCodeSql = SqlTable<UserRecoveryCodeRecord>.CreateTable();
    string fileSql             = SqlTable<FileRecord>.CreateTable();
    string loginProviderSql    = SqlTable<UserLoginProviderRecord>.CreateTable();

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
    */


    Console.WriteLine();
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
