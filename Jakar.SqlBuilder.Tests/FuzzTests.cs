// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System.Collections.Generic;
using NUnit.Framework;
using Jakar.SqlBuilder;
using Jakar.SqlBuilder.Tests.Execution;



namespace Jakar.SqlBuilder.Tests;


/// <summary> SPECIFICATION §12: every fuzzed chain, in every dialect, is balanced, terminated per dialect and binds contiguous parameters. The engines re-parse them in <c>Execution/</c>. </summary>
[TestFixture]
public sealed class FuzzTests
{
    [TestCase(SqlDialectKind.PostgreSql)]
    [TestCase(SqlDialectKind.SqlServer)]
    [TestCase(SqlDialectKind.Sqlite)]
    [TestCase(SqlDialectKind.MySql)]
    [TestCase(SqlDialectKind.Oracle)]
    [TestCase(SqlDialectKind.Firebird)]
    public void Fuzzed_Chains_Are_Well_Formed( SqlDialectKind dialect )
    {
        List<string> failures = [];

        for ( int seed = 0; seed < FuzzChains.COUNT * 10; seed++ )
        {
            SqlResult result = FuzzChains.Build(seed, dialect);
            if ( FuzzChains.Problems(dialect, in result) is { } problem ) { failures.Add($"seed {seed}: {problem}\n{result.Sql}"); }
        }

        Assert.That(failures, Is.Empty, string.Join("\n\n", failures.GetRange(0, System.Math.Min(5, failures.Count))));
    }

    [Test] public void A_Seed_Builds_The_Same_Chain_Every_Time()
        => Assert.That(FuzzChains.Build(1234, SqlDialectKind.PostgreSql).Sql, Is.EqualTo(FuzzChains.Build(1234, SqlDialectKind.PostgreSql).Sql));
}
