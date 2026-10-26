// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Jakar.SqlBuilder;
using SqlBuilder = Jakar.SqlBuilder.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary>
///     SPECIFICATION §6.1 / §12: the type-state really blocks illegal orderings. Each snippet is compiled against the real Jakar.SqlBuilder assembly with Roslyn and must fail with the
///     expected diagnostic; the controls must compile, so a failure can't come from the harness itself.
/// </summary>
[TestFixture]
public sealed class CompileTimeTests
{
    private static readonly Lazy<MetadataReference[]> __references = new(static () =>
                                                                         {
                                                                             string platform = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
                                                                             return platform.Split(Path.PathSeparator)
                                                                                            .Where(static path => Path.GetFileName(path).StartsWith("System.", StringComparison.Ordinal) || Path.GetFileName(path) is "netstandard.dll" or "mscorlib.dll")
                                                                                            .Append(typeof(SqlBuilder).Assembly.Location)
                                                                                            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
                                                                                            .ToArray();
                                                                         });

    private static IReadOnlyList<Diagnostic> Compile( string body )
    {
        string source = $$"""
                          using Jakar.SqlBuilder;
                          public static class Snippet
                          {
                              public static object Run()
                              {
                                  {{body}}
                              }
                          }
                          """;

        CSharpCompilation compilation = CSharpCompilation.Create("Snippet",
                                                                 [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
                                                                 __references.Value,
                                                                 new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        return compilation.GetDiagnostics().Where(static d => d.Severity is DiagnosticSeverity.Error).ToArray();
    }


    // ---- controls: legal chains compile ----

    [TestCase("""return SqlBuilder.PostgreSQL.Select("a").From("t").Where("a").EqualTo(1).GroupBy("a").Having().Count().GreaterThan(1).OrderBy("a").Limit(1).Build();""")]
    [TestCase("""return SqlBuilder.SqlServer.Insert().Into("t", "a").Values(SqlValue.Of(1)).Output("a").Build();""")]
    [TestCase("""return SqlBuilder.PostgreSQL.Update("t").Set("a", 1).Where("b").EqualTo(2).Returning("a").Build();""")]
    [TestCase("""return SqlBuilder.SqlServer.MergeInto("t", "a").Using("s", "b").On(p => p.Column("a.id").EqualToColumn("b.id")).WhenMatchedThenDelete().Build();""")]
    [TestCase("""return SqlBuilder.PostgreSQL.With("c").As(q => q.Select("a").From("t")).Select("a").From("c").Build();""")]
    [TestCase("""return SqlBuilder.PostgreSQL.CreateTable("t").Column("a", SqlType.Int).Build();""")]
    public void Legal_Chain_Compiles( string body ) => Assert.That(Compile(body).Select(static d => d.ToString()), Is.Empty);


    // ---- illegal orderings: the stage simply has no such member (CS1061) ----

    [TestCase("""return SqlBuilder.PostgreSQL.Select("a").From("t").Having().Count().GreaterThan(1).Build();""",                                   TestName = "HAVING before GROUP BY")]
    [TestCase("""return SqlBuilder.PostgreSQL.Select("a").From("t").OrderBy("a").Where("a").EqualTo(1).Build();""",                               TestName = "WHERE after ORDER BY")]
    [TestCase("""return SqlBuilder.PostgreSQL.Select("a").From("t").GroupBy("a").Where("a").EqualTo(1).Build();""",                               TestName = "WHERE after GROUP BY")]
    [TestCase("""return SqlBuilder.PostgreSQL.Select("a").From("t").Limit(1).GroupBy("a").Build();""",                                             TestName = "GROUP BY after LIMIT")]
    [TestCase("""return SqlBuilder.PostgreSQL.Select("a").Where("a").EqualTo(1).Build();""",                                                       TestName = "WHERE before FROM")]
    [TestCase("""return SqlBuilder.PostgreSQL.Select("a").From("t").Where("a").Build();""",                                                        TestName = "WHERE without a comparison")]
    [TestCase("""return SqlBuilder.PostgreSQL.Select("a").From("t").InnerJoin("u").Build();""",                                                    TestName = "JOIN without ON")]
    [TestCase("""return SqlBuilder.PostgreSQL.Insert().Into("t", "a").Returning("a").Build();""",                                                  TestName = "RETURNING before VALUES")]
    [TestCase("""return SqlBuilder.PostgreSQL.Insert().Into("t", "a").Build();""",                                                                 TestName = "INSERT without VALUES")]
    [TestCase("""return SqlBuilder.PostgreSQL.Insert().Into("t", "a").Values(SqlValue.Of(1)).OnConflict("a").Build();""",                         TestName = "ON CONFLICT without an action")]
    [TestCase("""return SqlBuilder.PostgreSQL.Update("t").Where("a").EqualTo(1).Build();""",                                                       TestName = "UPDATE WHERE before SET")]
    [TestCase("""return SqlBuilder.PostgreSQL.Update("t").Build();""",                                                                             TestName = "UPDATE without SET")]
    [TestCase("""return SqlBuilder.PostgreSQL.DeleteFrom("t").Set("a", 1).Build();""",                                                            TestName = "SET on DELETE")]
    [TestCase("""return SqlBuilder.PostgreSQL.Insert().Into("t", "a").Values(SqlValue.Of(1)).Returning("a").Values(SqlValue.Of(2)).Build();""",   TestName = "VALUES after RETURNING")]
    [TestCase("""return SqlBuilder.SqlServer.MergeInto("t", "a").Using("s", "b").On(p => p.Column("a.id").EqualToColumn("b.id")).Build();""",     TestName = "MERGE without WHEN")]
    [TestCase("""return SqlBuilder.SqlServer.MergeInto("t", "a").Build();""",                                                                      TestName = "MERGE without USING")]
    [TestCase("""return SqlBuilder.PostgreSQL.With("c").Build();""",                                                                               TestName = "WITH without a body")]
    [TestCase("""return SqlBuilder.PostgreSQL.With("c").As(q => q.Select("a").From("t")).Build();""",                                             TestName = "WITH without a statement")]
    [TestCase("""return SqlBuilder.PostgreSQL.Select("a").From("t").ForUpdate().Where("a").EqualTo(1).Build();""",                                TestName = "WHERE after FOR UPDATE")]
    [TestCase("""return SqlBuilder.PostgreSQL.Select("a").From("t").GroupBy("a").ForUpdate().Build();""",                                         TestName = "FOR UPDATE after GROUP BY")]
    [TestCase("""return SqlBuilder.PostgreSQL.CreateIndex("i", "t").Build();""",                                                                   TestName = "CREATE INDEX without columns")]
    [TestCase("""return SqlBuilder.PostgreSQL.AlterTable("t").Build();""",                                                                         TestName = "ALTER TABLE without an action")]
    public void Illegal_Chain_Does_Not_Compile( string body )
        => Assert.That(Compile(body).Select(static d => d.Id), Has.Some.EqualTo("CS1061"), "the stage should have no such member");


    // ---- ref struct stages can't escape to the heap ----

    [TestCase("""object o = SqlBuilder.PostgreSQL.Select("a"); return o;""",                                                   "CS0029", TestName = "A stage can't be boxed")]
    [TestCase("""var list = new System.Collections.Generic.List<SelectStage>(); return list;""",                              "CS9244", TestName = "A stage can't be a type argument")]
    [TestCase("""var s = SqlBuilder.PostgreSQL.Select("a"); System.Func<int> f = () => { var x = s; return 0; }; return f;""", "CS8175", TestName = "A lambda can't capture a stage")]
    public void Stage_Cannot_Escape( string body, string diagnostic )
        => Assert.That(Compile(body).Select(static d => d.Id), Has.Some.EqualTo(diagnostic), string.Join("\n", Compile(body)));
}
