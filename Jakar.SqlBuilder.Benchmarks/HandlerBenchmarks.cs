// Jakar.SqlBuilder.Benchmarks :: Jakar.SqlBuilder.Benchmarks
// 10/25/2026

using System;
using BenchmarkDotNet.Attributes;
using Jakar.Database;



namespace Jakar.SqlBuilder.Benchmarks;


/// <summary> Jakar.Database's <see cref="SqlInterpolatedStringHandler{TSelf}"/>: the commands its stores build, from a single parameter to a multi-line fragment and an id list. </summary>
[MemoryDiagnoser]
public class HandlerBenchmarks
{
    private static readonly Guid[]   __ids  = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()];
    private static readonly DateTime __date = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
    private readonly        string   __name = "Admin";


    [Benchmark(Baseline = true)] public SqlCommand Parameter() => SqlCommand.Parse<RoleRecord>($"SELECT * FROM {RoleRecord.TableName} WHERE {nameof(RoleRecord.NameOfRole)} = @{__name};");

    [Benchmark] public SqlCommand Values() => SqlCommand.Parse<RoleRecord>($"SELECT * FROM {RoleRecord.TableName} WHERE count > {42} AND ratio < {1.5d} AND created > {__date} AND kind = {'A'} AND id = {__ids[0]};");

    [Benchmark] public SqlCommand IdList() => SqlCommand.Parse<RoleRecord>($"""
                                                                            SELECT * FROM {RoleRecord.TableName}
                                                                            WHERE id IN (
                                                                            {__ids:1}
                                                                            );
                                                                            """);

    [Benchmark] public SqlCommand ColumnFragment() => SqlCommand.Parse<RoleRecord>($"""
                                                                                    SELECT
                                                                                    {RoleRecord.MetaData.ColumnNames(1)}
                                                                                    FROM {RoleRecord.TableName};
                                                                                    """);
}
