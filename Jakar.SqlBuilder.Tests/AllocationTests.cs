// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using System.Buffers;
using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary>
///     SPECIFICATION §11.1 as a gate: composing and building a parameter-free query allocates only the final <see cref="string"/>, and <c>WriteTo</c> allocates nothing.
///     (The benchmarks in Jakar.SqlBuilder.Benchmarks measure the same with <c>[MemoryDiagnoser]</c>.)
/// </summary>
[TestFixture, NonParallelizable]
public sealed class AllocationTests
{
    private const int RUNS = 64;

    private static readonly ArrayBufferWriter<char> __destination = new(4096);

    private static string Query( SqlDialectKind dialect ) => SqlBuilder.For(dialect).Select("id", "name").Column("email").From("users", "u").InnerJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").Where("u.active").EqualTo(true).And("u.age").GreaterThan(18).OrderBy("u.id").Limit(10).BuildSql();

    private static void Write( SqlDialectKind dialect )
    {
        __destination.ResetWrittenCount();
        _ = SqlBuilder.For(dialect).Select("id", "name").Column("email").From("users", "u").InnerJoin("orders", "o").On("o.user_id").EqualToColumn("u.id").Where("u.active").EqualTo(true).And("u.age").GreaterThan(18).OrderBy("u.id").Limit(10).WriteTo(__destination);
    }

    /// <summary> Bytes allocated per call of <paramref name="action"/>, after warming it up (pools, JIT, statics). </summary>
    private static long Allocated( Action action )
    {
        for ( int i = 0; i < RUNS; i++ ) { action(); }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for ( int i = 0; i < RUNS; i++ ) { action(); }

        return ( GC.GetAllocatedBytesForCurrentThread() - before ) / RUNS;
    }

    private static long StringSize( int length ) => Allocated(() => _ = new string('x', length));


    [TestCase(SqlDialectKind.SqlServer)]
    [TestCase(SqlDialectKind.PostgreSql)]
    [TestCase(SqlDialectKind.Sqlite)]
    [TestCase(SqlDialectKind.MySql)]
    [TestCase(SqlDialectKind.Oracle)]
    [TestCase(SqlDialectKind.Firebird)]
    public void BuildSql_Allocates_Only_The_String( SqlDialectKind dialect )
    {
        int length = Query(dialect).Length;
        Assert.That(Allocated(() => Query(dialect)), Is.EqualTo(StringSize(length)));
    }

    [TestCase(SqlDialectKind.SqlServer)]
    [TestCase(SqlDialectKind.PostgreSql)]
    [TestCase(SqlDialectKind.Sqlite)]
    [TestCase(SqlDialectKind.MySql)]
    [TestCase(SqlDialectKind.Oracle)]
    [TestCase(SqlDialectKind.Firebird)]
    public void WriteTo_Allocates_Nothing( SqlDialectKind dialect ) => Assert.That(Allocated(() => Write(dialect)), Is.Zero);
}
