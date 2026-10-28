// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/26/2026

using System;
using NUnit.Framework;
using Jakar.Database;
using Jakar.SqlBuilder;
using Jakar.SqlBuilder.Tests.Execution;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary>
///     <c>In</c> / <c>NotIn</c> overloads: integers (<c>long</c>) and other numbers, <see cref="Guid"/>s and dates are written inline (they can't carry SQL); strings, <see cref="char"/>s and
///     types with no SQL literal are bound as parameters. Under StrictTypes (the default) every value is checked against the column.
/// </summary>
[TestFixture]
public sealed class InListTests
{
    private const SqlDialectKind PG  = SqlDialectKind.PostgreSql;
    private const SqlDialectKind MS  = SqlDialectKind.SqlServer;
    private const SqlDialectKind ORA = SqlDialectKind.Oracle;

    private static readonly SqlBuilderOptions __strict = SqlBuilderOptions.Default with { StrictTypes = true };

    private static SqlRoot For( SqlDialectKind dialect, SqlBuilderOptions? options = null ) => SqlBuilder.For(dialect, options);


    // ---- inline literals ----

    [Test] public void Integer_Literals_Stay_On_The_Long_Overload()
    {
        SqlResult result = For(PG).Select("*").From("t").Where("id").In(1, 2, 3).Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,              Is.EqualTo("SELECT *\nFROM t\nWHERE id IN (1, 2, 3);"));
                            Assert.That(result.Parameters.Count, Is.Zero);
                        });
    }

    [Test] public void Numbers_Are_Inline()
        => Assert.Multiple(() =>
                           {
                               Assert.That(For(PG).Select("*").From("t").Where("x").In(1.5, 2.25, -3d).Build().Sql,     Is.EqualTo("SELECT *\nFROM t\nWHERE x IN (1.5, 2.25, -3);"));
                               Assert.That(For(PG).Select("*").From("t").Where("x").In(1.5m, 2m).Build().Sql,           Is.EqualTo("SELECT *\nFROM t\nWHERE x IN (1.5, 2);"));
                               Assert.That(For(PG).Select("*").From("t").Where("x").In(0.5f).Build().Sql,               Is.EqualTo("SELECT *\nFROM t\nWHERE x IN (0.5);"));
                               Assert.That(For(PG).Select("*").From("t").Where("x").In(ulong.MaxValue).Build().Sql,     Is.EqualTo("SELECT *\nFROM t\nWHERE x IN (18446744073709551615);"));
                               Assert.That(For(PG).Select("*").From("t").Where("x").In(Int128.MaxValue).Build().Sql,    Does.Contain("IN (170141183460469231731687303715884105727)"));
                               Assert.That(For(PG).Select("*").From("t").Where("x").In(1.5, 2.25).Build().Parameters.Count, Is.Zero);
                           });

    [Test] public void Mixed_Number_Lists_Use_The_Double_Overload_And_Exact_Types_Keep_Their_Value()
        => Assert.Multiple(() =>
                           {
                               Assert.That(For(PG).Select("*").From("t").Where("x").In(1, 2.5).Build().Sql,                      Is.EqualTo("SELECT *\nFROM t\nWHERE x IN (1, 2.5);"));
                               Assert.That(For(PG).Select("*").From("t").Where("x").NotIn(1, 2.5).Build().Sql,                   Is.EqualTo("SELECT *\nFROM t\nWHERE x NOT IN (1, 2.5);"));
                               Assert.That(For(PG).Select("*").From("t").Where("x").NotIn(ulong.MaxValue).Build().Sql,           Is.EqualTo("SELECT *\nFROM t\nWHERE x NOT IN (18446744073709551615);"));
                               Assert.That(For(PG).Select("*").From("t").Where("x").In(0.1f).Build().Sql,                        Is.EqualTo("SELECT *\nFROM t\nWHERE x IN (0.1);"), "float isn't widened to double (0.10000000149011612)");
                               Assert.That(For(PG).Select("*").From("t").Where("x").In(1, 2).Build().Sql,                        Is.EqualTo("SELECT *\nFROM t\nWHERE x IN (1, 2);"));
                           });

    [Test] public void Non_Finite_Doubles_Are_Bound()
    {
        SqlResult result = For(PG).Select("*").From("t").Where("x").In(1.5, double.NaN).Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,                 Is.EqualTo("SELECT *\nFROM t\nWHERE x IN (1.5, $1);"), "SQL has no NaN literal");
                            Assert.That(result.Parameters[0].Value is double value && double.IsNaN(value), Is.True);
                        });
    }

    [TestCase(PG,  "IN ('019f1ef9-49ca-7a30-b254-eab2321f9e51', '019f1ef9-49ca-7a30-b254-eab2321f9e52')")]
    [TestCase(MS,  "IN ('019f1ef9-49ca-7a30-b254-eab2321f9e51', '019f1ef9-49ca-7a30-b254-eab2321f9e52')")]
    [TestCase(ORA, "IN (HEXTORAW('")]
    public void Guids_Are_Inline_In_The_Dialects_Syntax( SqlDialectKind dialect, string expected )
    {
        SqlResult result = For(dialect).Select("*").From("t").Where("id").In(Guid.Parse("019f1ef9-49ca-7a30-b254-eab2321f9e51"), Guid.Parse("019f1ef9-49ca-7a30-b254-eab2321f9e52")).Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,              Does.Contain(expected));
                            Assert.That(result.Parameters.Count, Is.Zero);
                        });
    }

    [Test] public void Dates_Are_Inline_In_The_Dialects_Syntax()
    {
        DateTimeOffset when = new(2026, 10, 26, 13, 15, 50, TimeSpan.Zero);
        DateOnly       day  = new(2026, 10, 26);

        Assert.Multiple(() =>
                        {
                            Assert.That(For(PG).Select("*").From("t").Where("at").In(when).Build().Sql,                 Does.EndWith("IN ('2026-10-26 13:15:50.000000+00:00');"));
                            Assert.That(For(ORA).Select("*").From("t").Where("at").In(when).Build().Sql,                Does.EndWith("IN (TIMESTAMP '2026-10-26 13:15:50.000000 +00:00')"));
                            Assert.That(For(MS).Select("*").From("t").Where("at").In(when.UtcDateTime).Build().Sql,     Does.EndWith("IN ('2026-10-26T13:15:50.0000000');"));
                            Assert.That(For(PG).Select("*").From("t").Where("d").In(day).Build().Sql,                   Does.EndWith("IN (DATE '2026-10-26');"));
                            Assert.That(For(PG).Select("*").From("t").Where("at").In(when).Build().Parameters.Count, Is.Zero);
                        });
    }


    // ---- bound values ----

    [Test] public void Strings_Are_Bound()
    {
        SqlResult result = For(PG).Select<RoleRecord>(nameof(RoleRecord.NameOfRole)).From<RoleRecord>().Where<RoleRecord>(nameof(RoleRecord.NormalizedName)).In("ADMIN", "O'BRIEN", "").Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Sql,                 Is.EqualTo("SELECT name_of_role\nFROM roles\nWHERE normalized_name IN ($1, $2, $3);"));
                            Assert.That(result.Parameters.Count,    Is.EqualTo(3));
                            Assert.That(result.Parameters[1].Value, Is.EqualTo("O'BRIEN"), "the quote travels as data, never as SQL");
                            Assert.That(result.Parameters[2].Value, Is.EqualTo(string.Empty));
                        });
    }

    [Test] public void Chars_And_Types_Without_A_Literal_Are_Bound()
    {
        SqlResult chars = For(MS).Select("*").From("t").Where("c").In('a', '\'').Build();
        SqlResult spans = For(MS).Select("*").From("t").Where("d").In(TimeSpan.FromMinutes(5)).Build();
        SqlResult enums = For(MS).Select("*").From("t").Where("k").In(DayOfWeek.Monday).Build();

        Assert.Multiple(() =>
                        {
                            Assert.That(chars.Sql,                 Does.EndWith("IN (@p0, @p1);"));
                            Assert.That(chars.Parameters[1].Value, Is.EqualTo('\''));
                            Assert.That(spans.Sql,                 Does.EndWith("IN (@p0);"));
                            Assert.That(spans.Parameters[0].Value, Is.EqualTo(TimeSpan.FromMinutes(5)));
                            Assert.That(enums.Parameters[0].Value, Is.EqualTo(DayOfWeek.Monday));
                        });
    }

    [Test] public void Not_In_Mirrors_In()
        => Assert.Multiple(() =>
                           {
                               Assert.That(For(PG).Select("*").From("t").Where("id").NotIn(1, 2).Build().Sql,       Is.EqualTo("SELECT *\nFROM t\nWHERE id NOT IN (1, 2);"));
                               Assert.That(For(PG).Select("*").From("t").Where("x").NotIn(1.5).Build().Sql,         Is.EqualTo("SELECT *\nFROM t\nWHERE x NOT IN (1.5);"));
                               Assert.That(For(PG).Select("*").From("t").Where("name").NotIn("a", "b").Build().Sql, Is.EqualTo("SELECT *\nFROM t\nWHERE name NOT IN ($1, $2);"));
                           });

    [Test] public void An_Empty_List_Is_Rejected()
        => Assert.Multiple(() =>
                           {
                               Assert.That(() => For(PG).Select("*").From("t").Where("x").In(ReadOnlySpan<double>.Empty).Build(), Throws.InstanceOf<Exception>());
                               Assert.That(() => For(PG).Select("*").From("t").Where("x").In(ReadOnlySpan<string?>.Empty).Build(), Throws.InstanceOf<Exception>());
                           });


    // ---- StrictTypes ----

    [Test] public void Strict_Types_Check_Every_Value()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(() => For(MS, __strict).Select("*").From<User>().Where<User>(nameof(User.Name)).In(1.5, 2.5).Build(), Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.TypeMismatch));
                            Assert.That(() => For(MS, __strict).Select("*").From<User>().Where<User>(nameof(User.Name)).In(1, 2).Build(),     Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.TypeMismatch), "the long overload checks too");
                            Assert.That(() => For(MS, __strict).Select("*").From<User>().Where<User>(nameof(User.ID)).In("a", "b").Build(),   Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.TypeMismatch));
                            Assert.That(() => For(MS, __strict).Select("*").From<User>().Where<User>(nameof(User.ID)).NotIn(1.5).Build(),     Throws.Nothing, "a double compares with a number column");
                            Assert.That(For(MS, __strict).Select("*").From<User>().Where<User>(nameof(User.Name)).In("Ada", "Bob").Build().Sql, Is.EqualTo("SELECT *\nFROM [users]\nWHERE [name] IN (@p0, @p1);"));
                            Assert.That(For(MS, __strict).Select("*").From<User>().Where<User>(nameof(User.ID)).In(1, 2).Build().Sql,           Is.EqualTo("SELECT *\nFROM [users]\nWHERE [id] IN (1, 2);"));
                        });
    }

    [Test] public void Without_Strict_Types_Values_Are_Not_Checked()
        => Assert.That(() => For(MS, SqlBuilderOptions.Default with { StrictTypes = false }).Select("*").From<User>().Where<User>(nameof(User.Name)).In(1.5).Build(), Throws.Nothing);

    [Test] public void Strict_Types_Are_On_By_Default()
        => Assert.That(() => SqlBuilder.PostgreSQL.Select<RoleRecord>(nameof(RoleRecord.NameOfRole)).From<RoleRecord>().Where<RoleRecord>(nameof(RoleRecord.NormalizedName)).In(1, 2, 3).Build(),
                       Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.TypeMismatch));

    [Test] public void Record_Id_Columns_Compare_With_Their_Guid()
    {
        Guid id = Guid.Parse("019f1ef9-49ca-7a30-b254-eab2321f9e51");

        Assert.Multiple(() =>
                        {
                            Assert.That(() => SqlBuilder.PostgreSQL.Select("*").From<RoleRecord>().Where<RoleRecord>(nameof(RoleRecord.ID)).EqualTo(id).Build(),                        Throws.Nothing);
                            Assert.That(() => SqlBuilder.PostgreSQL.Select("*").From<RoleRecord>().Where<RoleRecord>(nameof(RoleRecord.ID)).EqualTo(RecordID<RoleRecord>.Create(id)).Build(), Throws.Nothing);
                            Assert.That(() => SqlBuilder.PostgreSQL.Select("*").From<RoleRecord>().Where<RoleRecord>(nameof(RoleRecord.ID)).In(id).Build(),                             Throws.Nothing);
                            Assert.That(() => SqlBuilder.PostgreSQL.Select("*").From<RoleRecord>().Where<RoleRecord>(nameof(RoleRecord.ID)).EqualTo("oops").Build(),
                                        Throws.TypeOf<SqlBuildException>().With.Property(nameof(SqlBuildException.Reason)).EqualTo(SqlBuildError.TypeMismatch));
                        });
    }
}
