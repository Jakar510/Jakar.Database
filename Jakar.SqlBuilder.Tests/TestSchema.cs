// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 06/27/2026

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Jakar.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary> Pins the PostgreSQL identifier folder so PG output is deterministic regardless of test order. </summary>
[SetUpFixture]
public sealed class GlobalTestSetup
{
    [OneTimeSetUp] public void Init() => SqlDialects.PostgresIdentifierFolder = TestSchema.SnakeLower;
}



/// <summary> Shared helpers + a deterministic snake_case folder mirroring the builder's default. </summary>
public static class TestSchema
{
    /// <summary> snake_case + lower, matching <c>SqlDialects.DefaultSnakeCaseLower</c> (which is internal). </summary>
    public static string SnakeLower( string name )
    {
        if ( string.IsNullOrEmpty(name) ) { return name; }

        System.Text.StringBuilder builder = new(name.Length + 8);

        for ( int i = 0; i < name.Length; i++ )
        {
            char c = name[i];

            if ( char.IsUpper(c) )
            {
                if ( i > 0 && name[i - 1] is not '_' ) { builder.Append('_'); }

                builder.Append(char.ToLowerInvariant(c));
            }
            else { builder.Append(c); }
        }

        return builder.ToString();
    }
}



/// <summary> A minimal <see cref="ISqlColumn"/> for tests. </summary>
public sealed class TestColumn( string propertyName, string columnName, Type clrType, bool isNullable = false, bool isPrimaryKey = false, bool isIdentity = false ) : ISqlColumn
{
    public string PropertyName                          => propertyName;
    public string ColumnName                            => columnName;
    public Type   ClrType                               => clrType;
    public bool   IsNullable                            => isNullable;
    public bool   IsPrimaryKey                          => isPrimaryKey;
    public bool   IsIdentity                            => isIdentity;
    public string GetTypeName( SqlDialectKind dialect ) => clrType.Name;
}



/// <summary> Test table record: columns stored as canonical lowercase names. </summary>
public sealed class User : ISqlTable<User>
{
    public int    ID     { get; init; }
    public string Name   { get; init; } = string.Empty;
    public string Email  { get; init; } = string.Empty;
    public bool   Active { get; init; }

    private static readonly ISqlColumn[]                   __columns = [new TestColumn(nameof(ID), "id", typeof(int), isPrimaryKey: true, isIdentity: true), new TestColumn(nameof(Name), "name", typeof(string)), new TestColumn(nameof(Email), "email", typeof(string), isNullable: true), new TestColumn(nameof(Active), "active", typeof(bool))];
    private static readonly Dictionary<string, ISqlColumn> __byName  = __columns.ToDictionary(static c => c.PropertyName);

    public static string                    SqlTableName                                                => "users";
    public static IReadOnlyList<ISqlColumn> SqlColumns                                                  => __columns;
    public static bool                      TrySqlColumn( string propertyName, out ISqlColumn? column ) => __byName.TryGetValue(propertyName, out column);
}



/// <summary> Second test table record (for joins / multi-table cases). </summary>
public sealed class Order : ISqlTable<Order>
{
    public int    ID     { get; init; }
    public int    UserID { get; init; }
    public string Name   { get; init; } = string.Empty;

    private static readonly ISqlColumn[]                   __columns = [new TestColumn(nameof(ID), "id", typeof(int), isPrimaryKey: true, isIdentity: true), new TestColumn(nameof(UserID), "user_id", typeof(int)), new TestColumn(nameof(Name), "name", typeof(string))];
    private static readonly Dictionary<string, ISqlColumn> __byName  = __columns.ToDictionary(static c => c.PropertyName);

    public static string                    SqlTableName                                                => "orders";
    public static IReadOnlyList<ISqlColumn> SqlColumns                                                  => __columns;
    public static bool                      TrySqlColumn( string propertyName, out ISqlColumn? column ) => __byName.TryGetValue(propertyName, out column);
}
