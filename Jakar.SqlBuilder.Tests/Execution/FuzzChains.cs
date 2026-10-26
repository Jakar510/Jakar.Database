// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Jakar.SqlBuilder;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests.Execution;


/// <summary>
///     SPECIFICATION §12's property / fuzz layer: random but valid chains, reproducible from their seed. A seed builds the same chain in every dialect, using only what every dialect can
///     express, so every engine must return the same number of rows; in-process SQLite is the reference.
/// </summary>
public static class FuzzChains
{
    public const int COUNT = 400;

    private static SqlRoot For( SqlDialectKind dialect ) => SqlBuilder.For(dialect);


    public static SqlResult Build( int seed, SqlDialectKind dialect )
    {
        Random random = new(seed);

        return random.Next(5) switch
               {
                   0 => Filtered(random, dialect),
                   1 => Grouped(random, dialect),
                   2 => Joined(random, dialect),
                   3 => SetOperation(random, dialect),
                   _ => Projected(random, dialect)
               };
    }


    private static SqlResult Filtered( Random random, SqlDialectKind dialect )
    {
        bool             distinct = random.Next(3) is 0;
        string           first    = distinct ? "active" : "id";
        SelectWhereStage where    = ( distinct ? For(dialect).SelectDistinct("active") : For(dialect).Select("id", "name") ).From("users").Where(p => Predicate(p, random, "users.", 2));

        return random.Next(3) switch
               {
                   0 => where.Build(),
                   1 => where.OrderBy(first).Build(),
                   _ => where.OrderBy(first).Limit(random.Next(1, 4)).Build()
               };
    }

    private static SqlResult Grouped( Random random, SqlDialectKind dialect )
    {
        int          minimum = random.Next(0, 3);
        GroupByStage group   = For(dialect).Select("active").Count("*", "n").From("users").Where(p => Predicate(p, random, "users.", 1)).GroupBy("active");

        return random.Next(3) switch
               {
                   0 => group.Build(),
                   1 => group.Having(h => h.Count().GreaterThan(minimum)).Build(),
                   _ => group.Having(h => h.Count().GreaterOrEqual(minimum).Or().Max("age").LessThan(40)).OrderBy("active").Build()
               };
    }

    private static SqlResult Joined( Random random, SqlDialectKind dialect )
    {
        SelectStage select = For(dialect).Select("u.id", "o.total");
        FromStage   from   = select.From("users", "u");

        return random.Next(2) is 0
                   ? from.InnerJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").Where(p => Predicate(p, random, "u.", 2)).Build()
                   : from.LeftJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").And("o.total").GreaterThan(random.Next(0, 15)).Where(p => Predicate(p, random, "u.", 1)).Build();
    }

    private static SqlResult SetOperation( Random random, SqlDialectKind dialect )
    {
        SelectWhereStage left  = For(dialect).Select("id").From("users").Where(p => Predicate(p, random, "users.", 1));
        int              total = random.Next(0, 25);

        return random.Next(2) is 0
                   ? left.Union().Select("user_id").From("orders").Where("total").GreaterThan(total).Build()
                   : left.UnionAll().Select("user_id").From("orders").Where("total").LessThan(total).Build();
    }

    private static SqlResult Projected( Random random, SqlDialectKind dialect )
    {
        int k = random.Next(1, 5);

        return For(dialect).Select("id")
                           .Column(e => e.Coalesce(x => x.Column("age"), x => x.Number(0)).Plus().Number(k), "x")
                           .Column(e => e.Case(c => c.When(p => Predicate(p, random, "users.", 0), x => x.Number(1)).Else(x => x.Number(0))), "flag")
                           .From("users")
                           .Where(e => e.Coalesce(x => x.Column("age"), x => x.Number(0)).Times().Number(k))
                           .GreaterThan(random.Next(0, 150))
                           .Build();
    }


    // ---- predicates over users (id, name, email, active, age) ----

    private static SqlPredicateChain Predicate( SqlPredicate predicate, Random random, string prefix, int depth )
    {
        SqlPredicateChain chain = random.Next(depth > 0 ? 6 : 4) switch
                                  {
                                      < 4 => Compare(predicate.Column(prefix + Column(random, out Kind kind)), random, kind),
                                      4   => random.Next(2) is 0 ? predicate.Group(p => Predicate(p, random, prefix, depth - 1)) : predicate.Not(p => Predicate(p, random, prefix, depth - 1)),
                                      _   => random.Next(2) is 0
                                                 ? predicate.Exists(q => q.Select("id").From("orders").Where("orders.user_id").EqualToColumn(prefix + "id"))
                                                 : predicate.NotExists(q => q.Select("id").From("orders").Where("orders.user_id").EqualToColumn(prefix + "id").And("orders.total").GreaterThan(random.Next(0, 25)))
                                  };

        for ( int more = random.Next(3); more > 0; more-- )
        {
            bool and = random.Next(2) is 0;
            chain = random.Next(depth > 0 ? 3 : 2) switch
                    {
                        < 2 => Compare(and ? chain.And(prefix + Column(random, out Kind kind)) : chain.Or(prefix + Column(random, out kind)), random, kind),
                        _   => and ? chain.And(p => Predicate(p, random, prefix, depth - 1)) : chain.Or(p => Predicate(p, random, prefix, depth - 1))
                    };
        }

        return chain;
    }

    private enum Kind { Number, Text, Email, Flag }

    private static string Column( Random random, out Kind kind )
    {
        switch ( random.Next(5) )
        {
            case 0:
                kind = Kind.Number;
                return "id";

            case 1:
                kind = Kind.Number;
                return "age";

            case 2:
                kind = Kind.Text;
                return "name";

            case 3:
                kind = Kind.Email;
                return "email";

            default:
                kind = Kind.Flag;
                return "active";
        }
    }

    private static SqlPredicateChain Compare( SqlComparison<SqlPredicateChain> left, Random random, Kind kind )
    {
        int a = random.Next(0, 70), b = a + random.Next(0, 30);

        return kind switch
               {
                   Kind.Number => random.Next(14) switch
                                  {
                                      0  => left.EqualTo(a),
                                      1  => left.NotEqualTo(a),
                                      2  => left.GreaterThan((object)a),
                                      3  => left.LessOrEqual(a),
                                      4  => left.GreaterOrEqual(SqlValue.Of(a)),
                                      5  => left.Between(a, b),
                                      6  => left.NotBetween(a, b),
                                      7  => left.In(a % 5, b % 5, 3),
                                      8  => left.NotIn(a % 5, 2),
                                      9  => left.InParams(a % 5, 4),
                                      10 => left.IsNull(),
                                      11 => left.IsNotNull(),
                                      12 => left.IsDistinctFrom((object)( a % 5 )),
                                      _  => left.LessThan(e => e.Number(a).Plus().Number(1))
                                  },
                   Kind.Text => random.Next(5) switch
                                {
                                    0 => left.EqualTo((object)"Ada"),
                                    1 => left.NotEqualTo(SqlValue.Inline("O'Brien")),
                                    2 => left.InValues(SqlValue.Inline("Cy"), SqlValue.Inline("Bob")),
                                    3 => left.IsNotNull(),
                                    _ => left.IsNotDistinctFrom(SqlValue.Inline("Cy"))
                                },
                   Kind.Email => random.Next(5) switch
                                 {
                                     0 => left.Like("%x.io"),
                                     1 => left.NotLike("a%"),
                                     2 => left.Like("%b%"),
                                     3 => left.IsNull(),
                                     _ => left.IsDistinctFrom(SqlValue.Null)
                                 },
                   _ => left.EqualTo(random.Next(2) is 0)
               };
    }


    // ---- the reference engine ----

    private static readonly SemaphoreSlim     __lock      = new(1, 1);
    private static          SqliteConnection? __reference;

    /// <summary> How many rows the chain for <paramref name="seed"/> returns on in-process SQLite. </summary>
    public static async Task<int> ReferenceRows( int seed )
    {
        await __lock.WaitAsync();

        try
        {
            if ( __reference is null )
            {
                SqliteConnection connection = new("Data Source=:memory:");
                await connection.OpenAsync();

                foreach ( string sql in TestData.Schema(SqlDialectKind.Sqlite) )
                {
                    await using DbCommand command = connection.CreateCommand();
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();
                }

                __reference = connection;
            }

            return await Rows(__reference, Build(seed, SqlDialectKind.Sqlite), static token => token, null);
        }
        finally { __lock.Release(); }
    }

    public static async Task<int> Rows( DbConnection connection, SqlResult result, Func<string, string> bindName, Action<DbCommand>? configure )
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = result.Sql;
        configure?.Invoke(command);

        foreach ( SqlParameter parameter in result.Parameters )
        {
            DbParameter value = command.CreateParameter();
            value.ParameterName = bindName(parameter.Name);
            value.Value         = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(value);
        }

        int rows = 0;
        await using DbDataReader reader = await command.ExecuteReaderAsync();
        while ( await reader.ReadAsync() ) { rows++; }

        return rows;
    }


    // ---- structure ----

    /// <summary> Every structural property the specification promises of a built statement; <see langword="null"/> when it holds. </summary>
    public static string? Problems( SqlDialectKind dialect, in SqlResult result )
    {
        string sql   = result.Sql;
        int    depth = 0;
        char   quote = '\0';
        ( char open, char close ) = dialect.QuoteChars();

        for ( int i = 0; i < sql.Length; i++ )
        {
            char c = sql[i];
            if ( quote is not '\0' )
            {
                if ( c == quote )
                {
                    if ( i + 1 < sql.Length && sql[i + 1] == quote ) { i++; }
                    else { quote = '\0'; }
                }

                continue;
            }

            if ( c is '\'' ) { quote = '\''; }
            else if ( open is not '\0' && c == open ) { quote = close; }
            else if ( c is '(' ) { depth++; }
            else if ( c is ')' && --depth < 0 ) { return "a ')' closes nothing"; }
        }

        if ( quote is not '\0' ) { return "a quote is never closed"; }

        if ( depth is not 0 ) { return $"{depth} '(' never closed"; }

        if ( sql.EndsWith(';') != dialect.SupportsStatementTerminator() ) { return "wrong statement terminator"; }

        int ordinal = 0;
        foreach ( SqlParameter parameter in result.Parameters )
        {
            string expected = dialect.ParameterName(ordinal++);
            if ( parameter.Name != expected ) { return $"parameter {ordinal} is {parameter.Name}, expected {expected}"; }

            if ( !sql.Contains(expected, StringComparison.Ordinal) ) { return $"{expected} is bound but not in the SQL"; }
        }

        return null;
    }
}
