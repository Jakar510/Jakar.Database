// Jakar.SqlBuilder.Benchmarks :: Jakar.SqlBuilder.Benchmarks
// 10/25/2026

using System.Buffers;
using BenchmarkDotNet.Attributes;



namespace Jakar.SqlBuilder.Benchmarks;


/// <summary>
///     SPECIFICATION §11.3: time and allocations for a trivial <c>SELECT *</c>, a representative join + filter + order query, a 100-row multi-<c>VALUES</c> insert and a deeply nested
///     predicate. The bar: <c>0 B</c> on the <c>WriteTo</c> path for parameter-free queries, and exactly the string for <c>BuildSql</c> (also gated in Jakar.SqlBuilder.Tests'
///     <c>AllocationTests</c>).
/// </summary>
[MemoryDiagnoser]
public class BuilderBenchmarks
{
    private static readonly SqlValue[] __row = [SqlValue.Of(1), SqlValue.Inline("name"), SqlValue.Of(true)];

    private readonly ArrayBufferWriter<char> __destination = new(64 * 1024);

    [Params(SqlDialectKind.PostgreSql, SqlDialectKind.SqlServer)] public SqlDialectKind Dialect { get; set; }


    [Benchmark(Baseline = true)] public string SelectStar_BuildSql() => SqlBuilder.For(Dialect).Select("*").From("users").BuildSql();

    [Benchmark] public int SelectStar_WriteTo()
    {
        __destination.ResetWrittenCount();
        return SqlBuilder.For(Dialect).Select("*").From("users").WriteTo(__destination).Count;
    }

    [Benchmark] public string JoinFilterOrder_BuildSql() => SqlBuilder.For(Dialect)
                                                                      .Select("u.id", "u.name").Count("o.id", "orders")
                                                                      .From("users", "u")
                                                                      .LeftJoin("orders", "o").On("o.user_id").EqualToColumn("u.id")
                                                                      .Where("u.active").EqualTo(true).And("u.age").GreaterThan(18)
                                                                      .GroupBy("u.id", "u.name")
                                                                      .OrderByDesc("orders")
                                                                      .Limit(20)
                                                                      .BuildSql();

    [Benchmark] public int JoinFilterOrder_WriteTo()
    {
        __destination.ResetWrittenCount();
        return SqlBuilder.For(Dialect)
                         .Select("u.id", "u.name").Count("o.id", "orders")
                         .From("users", "u")
                         .LeftJoin("orders", "o").On("o.user_id").EqualToColumn("u.id")
                         .Where("u.active").EqualTo(true).And("u.age").GreaterThan(18)
                         .GroupBy("u.id", "u.name")
                         .OrderByDesc("orders")
                         .Limit(20)
                         .WriteTo(__destination)
                         .Count;
    }

    [Benchmark] public string Insert100Rows_BuildSql()
    {
        InsertValuesStage insert = SqlBuilder.For(Dialect).Insert().Into("users", "id", "name", "active").Values(__row);
        for ( int i = 1; i < 100; i++ ) { insert = insert.Values(__row); }

        return insert.BuildSql();
    }

    [Benchmark] public SqlResult Insert100Rows_Parameters()
    {
        InsertValuesStage insert = SqlBuilder.For(Dialect).Insert().Into("users", "id", "name", "active").ValuesParams(1, "name", true);
        for ( int i = 1; i < 100; i++ ) { insert = insert.ValuesParams(i, "name", true); }

        return insert.Build();
    }

    [Benchmark] public string NestedPredicate_BuildSql() => SqlBuilder.For(Dialect)
                                                                      .Select("id")
                                                                      .From("users")
                                                                      .Where(static a => a.Column("age").GreaterThan(18)
                                                                                          .And(static b => b.Column("active").EqualTo(true)
                                                                                                            .Or(static c => c.Column("vip").EqualTo(true)
                                                                                                                              .And(static d => d.Column("score").Between(10, 20)
                                                                                                                                                .Or("country").In(1, 2, 3)))))
                                                                      .OrNot(static e => e.Column("banned").EqualTo(true))
                                                                      .BuildSql();
}
