// Jakar.SqlBuilder.Benchmarks :: Jakar.SqlBuilder.Benchmarks
// 10/25/2026

using BenchmarkDotNet.Running;
using Jakar.SqlBuilder.Benchmarks;


// dotnet run -c Release --project Jakar.SqlBuilder.Benchmarks -- --filter *
BenchmarkSwitcher.FromAssembly(typeof(BuilderBenchmarks).Assembly).Run(args);
