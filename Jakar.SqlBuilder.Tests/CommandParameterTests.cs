// Jakar.SqlBuilder.Tests :: Jakar.SqlBuilder.Tests
// 10/25/2026

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Jakar.Extensions;
using Jakar.Json;
using NUnit.Framework;
using ZLinq;
using Jakar.SqlBuilder;



namespace Jakar.SqlBuilder.Tests;


/// <summary> A record-id-like value: binds as its <see cref="Guid"/>. </summary>
internal readonly record struct RowId( Guid Id ) : ISqlParameterValue
{
    object ISqlParameterValue.ParameterValue => Id;
    public override string    ToString()     => Id.ToString();
}



/// <summary> A minimal <see cref="ISqlCommandColumn"/>: <c>KeyValuePair</c> is <c>column = @column</c>, as in Jakar.Database. </summary>
public sealed class CommandColumn( string propertyName, string columnName, int index, Type clrType, bool isNullable = false ) : ISqlCommandColumn
{
    public string PropertyName                           => propertyName;
    public string ColumnName                             => columnName;
    public Type   ClrType                                => clrType;
    public bool   IsNullable                             => isNullable;
    public bool   IsPrimaryKey                           => index is 0;
    public bool   IsIdentity                             => index is 0;
    public int    Index                                  => index;
    public string KeyValuePair                           => $"{columnName} = @{columnName}";
    public string GetTypeName( SqlDialectKind dialect ) => clrType.Name;
}



/// <summary> A four-column <see cref="ISqlCommandTable"/> whose parameter names are the snake_cased last segment of the argument expression. </summary>
public sealed class CommandTable : ISqlCommandTable
{
    public static readonly CommandTable Instance = new();

    private readonly CommandColumn[] __columns =
    [
        new(nameof(User.ID),     "id",     0, typeof(int)),
        new(nameof(User.Name),   "name",   1, typeof(string)),
        new(nameof(User.Email),  "email",  2, typeof(string), true),
        new(nameof(User.Active), "active", 3, typeof(bool))
    ];

    public int ColumnCount          => __columns.Length;
    public int MaxLengthColumnName => __columns.Max(static x => x.ColumnName.Length);
    public int KeyValuePairsLength  => __columns.Sum(static x => x.KeyValuePair.Length);

    public ISqlCommandColumn GetColumn( string        propertyName )  => __columns.FirstOrDefault(x => x.PropertyName == propertyName) ?? throw new KeyNotFoundException(propertyName);
    public string            GetParameterName( string parameterName ) => TestSchema.SnakeLower(parameterName.Split('.')[^1]);
    public void ColumnNames( ref ValueStringBuilder builder, ref int indentLevel )
    {
        for ( int i = 0; i < __columns.Length; i++ )
        {
            if ( i > 0 ) { builder.Append(",\n"); }

            builder.Append(' ', indentLevel * 4).Append(__columns[i].ColumnName);
        }
    }
}



[TestFixture]
public sealed class CommandParametersTests
{
    private static CommandParameters Create() => CommandParameters.Create(CommandTable.Instance);

    private static CommandParameters Row( int id, string name )
    {
        CommandParameters parameters = Create();
        parameters.Add(nameof(User.ID),   (object)id, "id");
        parameters.Add(nameof(User.Name), name,       "name");
        return parameters;
    }


    [Test] public void Create_And_Empty()
    {
        CommandParameters parameters = CommandParameters.Create(CommandTable.Instance, 16);

        Assert.Multiple(() =>
                        {
                            Assert.That(parameters.Table,          Is.SameAs(CommandTable.Instance));
                            Assert.That(parameters.Capacity,       Is.GreaterThanOrEqualTo(16));
                            Assert.That(parameters.Count,          Is.Zero);
                            Assert.That(parameters.IsGrouped,      Is.False);
                            Assert.That(CommandParameters.Empty.Table, Is.Null);
                            Assert.That(CommandParameters.Empty.Values.IsEmpty, Is.True);
                        });
    }

    [Test] public void Add_Overloads_Normalize_Values()
    {
        string?     name    = "Ada";
        UserRights? rights  = new("abc");
        DayOfWeek   day     = DayOfWeek.Friday;
        DayOfWeek?  missing = null;

        CommandParameters parameters = Create();
        parameters.Add(nameof(User.Name),   name);
        parameters.Add(nameof(User.Email),  rights);
        parameters.Add(nameof(User.Active), day);
        parameters.Add(nameof(User.ID),     missing);
        parameters.Add(nameof(User.Email),  (object)new RowId(Guid.Empty), "row.Id");
        parameters.Add(nameof(User.Email),  (object)new JObjectNode { ["a"] = 1 }, "json");
        parameters.Add(nameof(User.Email),  (UserRights?)null, "noRights");

        Dictionary<string, object?> values = parameters.Values.ToArray().ToDictionary(static x => x.ParameterName, static x => x.Value);

        Assert.Multiple(() =>
                        {
                            Assert.That(values["name"],      Is.EqualTo("Ada"));
                            Assert.That(values["rights"],    Is.EqualTo("abc"));
                            Assert.That(values["day"],       Is.EqualTo((long)DayOfWeek.Friday));
                            Assert.That(values["missing"],   Is.Null);
                            Assert.That(values["id"],        Is.EqualTo(Guid.Empty), "ISqlParameterValue binds as its value; the name is the last segment");
                            Assert.That(JNode.DeepEquals(JNode.Parse((string)values["json"]!), new JObjectNode { ["a"] = 1 }), Is.True, "JNode binds as JSON text");
                            Assert.That(values["no_rights"], Is.Null);
                            Assert.That(() => parameters.Add("NotAColumn", "x"), Throws.TypeOf<KeyNotFoundException>());
                        });
    }

    [Test] public void Duplicates_Are_Ignored_And_Values_Follow_Column_Order()
    {
        CommandParameters parameters = Create();
        CommandParameter  name       = new("Ada", "name", CommandTable.Instance.GetColumn(nameof(User.Name)));

        Assert.Multiple(() =>
                        {
                            Assert.That(parameters.TryAdd(in name), Is.True);
                            Assert.That(parameters.TryAdd(in name), Is.False, "same column and name");
                        });

        parameters.AddRange(new CommandParameter(true, "active", CommandTable.Instance.GetColumn(nameof(User.Active))), new CommandParameter(1, "id", CommandTable.Instance.GetColumn(nameof(User.ID))), name);

        Assert.Multiple(() =>
                        {
                            Assert.That(parameters.Count,                                          Is.EqualTo(3));
                            Assert.That(parameters.Values.ToArray().Select(static x => x.Column.Index).ToArray(), Is.Ordered);
                            Assert.That(parameters[nameof(User.Name)].ToArray(),                   Has.Length.EqualTo(1));
                            Assert.That(parameters.ColumnsFor(nameof(User.Email)).ToArray(),       Is.Empty);
                        });
    }

    [Test] public void Groups_Get_Numbered_Names()
    {
        CommandParameters parameters = Create();
        CommandParameters first      = Row(1, "Ada");
        CommandParameters second     = Row(2, "Bob");
        parameters.AddGroup(in first).AddGroup(in second);

        using ArrayBuffer<CommandParameter> all = parameters.Parameters;

        Assert.Multiple(() =>
                        {
                            Assert.That(parameters.IsGrouped,        Is.True);
                            Assert.That(parameters.Groups.Length,    Is.EqualTo(2));
                            Assert.That(parameters.ParameterCount,   Is.EqualTo(4));
                            Assert.That(parameters[1].ToArray().Select(static x => x.ParameterName).ToArray(), Is.EqualTo(new[] { "id2", "name2" }));
                            Assert.That(all.Span.ToArray().Select(static x => x.ParameterName).ToArray(), Is.EqualTo(new[] { "id1", "id2", "name1", "name2" }));
                            Assert.That(parameters.AllValues().ToArray(),                          Has.Length.EqualTo(4));
                        });
    }

    [Test] public void Parameter_Names_Enumerate_Every_Name()
    {
        CommandParameters parameters = Row(1, "Ada");
        CommandParameters grouped    = Create();
        CommandParameters row        = Row(2, "Bob");
        grouped.AddGroup(in row);

        List<string> names = [];
        foreach ( string name in parameters.ParameterNames ) { names.Add(name); }

        List<string> groupNames = [];
        foreach ( string name in grouped.GroupParameterNames ) { groupNames.Add(name); }

        using ParameterNames      buffer = parameters.ParameterNames;
        using ExtraParameterNames extras = grouped.GroupParameterNames;
        string[]                  copy   = new string[2];

        string[] bufferArray = [.. buffer.Array];
        string[] bufferSpan  = buffer.Span.ToArray();
        string[] extrasArray = [.. extras.Array];
        string[] extrasSpan  = extras.Span.ToArray();

        Assert.Multiple(() =>
                        {
                            Assert.That(names,       Is.EqualTo(new[] { "id", "name" }), "the first name isn't skipped");
                            Assert.That(groupNames,  Is.EqualTo(new[] { "id1", "name1" }));
                            Assert.That(bufferArray, Is.EqualTo(new[] { "id", "name" }));
                            Assert.That(bufferSpan,  Is.EqualTo(new[] { "id", "name" }));
                            Assert.That(extrasArray, Is.EqualTo(new[] { "id1", "name1" }));
                            Assert.That(extrasSpan,  Is.EqualTo(new[] { "id1", "name1" }));
                        });

        ParameterNames enumerator = parameters.ParameterNames;
        Assert.That(enumerator.TryGetNext(out string firstName), Is.True);
        Assert.That(firstName, Is.EqualTo("id"));
        Assert.That(enumerator.TryGetNonEnumeratedCount(out int count) && count == 2, Is.True);
        Assert.That(enumerator.TryGetSpan(out ReadOnlySpan<string> span) && span.Length == 2, Is.True);
        Assert.That(enumerator.TryCopyTo(copy, 0), Is.True);
        Assert.That(copy, Is.EqualTo(new[] { "id", "name" }));
        Assert.That(enumerator.TryGetNext(out _), Is.True);
        Assert.That(enumerator.TryGetNext(out _), Is.False);
        Assert.That(enumerator.Current,           Is.Null);
        enumerator.Dispose();

        ExtraParameterNames groupEnumerator = grouped.GroupParameterNames;
        Assert.That(groupEnumerator.TryGetNext(out string firstGroupName), Is.True);
        Assert.That(firstGroupName, Is.EqualTo("id1"));
        Assert.That(groupEnumerator.TryGetNonEnumeratedCount(out int groupCount) && groupCount == 2, Is.True);
        Assert.That(groupEnumerator.TryGetSpan(out ReadOnlySpan<string> groupSpan) && groupSpan.Length == 2, Is.True);
        Assert.That(groupEnumerator.TryCopyTo(copy, 1), Is.True);
        Assert.That(copy[0],                           Is.EqualTo("name1"));
        Assert.That(groupEnumerator.TryGetNext(out _), Is.True);
        Assert.That(groupEnumerator.TryGetNext(out _), Is.False);
        Assert.That(groupEnumerator.Current,           Is.Null);
        groupEnumerator.Dispose();
    }

    [Test] public void Indexed_Parameters_Yield_The_Values_Then_Each_Group()
    {
        CommandParameters parameters = Row(1, "Ada");
        CommandParameters row        = Row(2, "Bob");
        parameters.AddGroup(in row);

        List<(int Index, int Length)> sets = [];
        foreach ( ( int index, ReadOnlySpan<CommandParameter> span ) in parameters.IndexedParameters ) { sets.Add(( index, span.Length )); }

        IndexedEnumerator enumerator = parameters.IndexedParameters;
        Assert.That(enumerator.MoveNext(), Is.True);
        enumerator.Reset();
        Assert.That(enumerator.MoveNext(), Is.True, "Reset rewinds to the start");
        enumerator.Dispose();

        Assert.That(sets, Is.EqualTo(new[] { ( 0, 2 ), ( 0, 2 ) }), "the top-level values, then group 0");
    }

    [Test] public void Fragments_Render()
    {
        CommandParameters single = Create();
        single.Add(nameof(User.ID),   (object)1,  "id");
        single.Add(nameof(User.Name), "O'Brien", "name");

        CommandParameters grouped = Row(1, "Ada");
        CommandParameters row     = Row(2, "Bob");
        grouped.AddGroup(in row);

        Assert.Multiple(() =>
                        {
                            Assert.That(single.ColumnNames(1).ToString(),   Is.EqualTo("    id,\n    name,\n    email,\n    active"));
                            Assert.That(single.VariableNames(1).ToString(), Is.EqualTo("    1,\n    @name"), "literals are inlined, strings bound");
                            Assert.That(single.VariableNames(1).Values.Length, Is.EqualTo(2));
                            Assert.That(single.KeyValuePairs(1, "AND").ToString(), Is.EqualTo("    id = 1,\n    name = @name"));
                            Assert.That(single.KeyValuePairs(0).Values.Length,     Is.EqualTo(2));
                            Assert.That(grouped.VariableNames(0).ToString(),       Is.EqualTo("(\n    1,\n    @name\n),\n(\n    2,\n    @name1\n)"));
                            Assert.That(grouped.VariableNames(0).Values.Length,    Is.EqualTo(4), "binds the grouped parameters too");
                        });

        CommandParameters onlyGroups = Create();
        CommandParameters onlyRow    = Row(3, "Cy");
        onlyGroups.AddGroup(in onlyRow);
        Assert.That(onlyGroups.VariableNames(0).ToString(), Is.EqualTo("(\n    3,\n    @name1\n)"));
    }

    [Test] public void Remaining_Edges()
    {
        DayOfWeek? day = DayOfWeek.Sunday;

        CommandParameters parameters = Create();
        parameters.Add(nameof(User.Active), day);

        ParameterNames names = parameters.ParameterNames;
        string?        before = names.Current;
        names.Reset();
        bool moved = names.MoveNext();

        ExtraParameterNames groups      = Create().GroupParameterNames;
        string?             groupBefore = groups.Current;
        groups.Reset();
        bool groupMoved = groups.MoveNext();

        Assert.Multiple(() =>
                        {
                            Assert.That(parameters.Values[0].Value,    Is.EqualTo((long)DayOfWeek.Sunday), "a nullable enum with a value binds as its number");
                            Assert.That(before,                        Is.Null, "no current name before enumerating");
                            Assert.That(moved,                         Is.True);
                            Assert.That(groupBefore,                   Is.Null);
                            Assert.That(groupMoved,                    Is.False, "no groups");
                            Assert.That(Create().VariableNames(1).ToString(), Is.Empty);
                        });
    }

    [Test] public void Identity_Hashing_And_Text()
    {
        CommandParameters first  = Row(1, "Ada");
        CommandParameters second = Row(2, "Bob");
        CommandParameters copy   = first;
        object            boxed  = copy;

        Assert.Multiple(() =>
                        {
                            Assert.That(first.Equals(copy),     Is.True);
                            Assert.That(first.Equals(boxed),    Is.True);
                            Assert.That(first == copy,          Is.True);
                            Assert.That(first != second,        Is.True);
                            Assert.That(first.Equals((object)"x"), Is.False);
                            Assert.That(first.GetHashCode(),    Is.EqualTo(copy.GetHashCode()));
                            Assert.That(first.GetHash64(),      Is.EqualTo(second.GetHash64()),  "same parameter names");
                            Assert.That(first.GetHash128(),     Is.EqualTo(second.GetHash128()));
                            Assert.That(first.GetHash64(),      Is.Not.EqualTo(Create().GetHash64()));
                            Assert.That(first.ToString(),       Is.EqualTo("CommandParameters { Parameters: [id, name], Groups: 0 }"));
                        });
    }
}



[TestFixture]
public sealed class CommandParameterTests
{
    private static readonly ISqlCommandColumn Name = CommandTable.Instance.GetColumn(nameof(User.Name));
    private static readonly ISqlCommandColumn ID   = CommandTable.Instance.GetColumn(nameof(User.ID));

    private static string Literal( object? value )
    {
        ValueStringBuilder builder = new();
        CommandParameter.AppendLiteral(ref builder, value);
        return builder.ToString();
    }


    public static IEnumerable<TestCaseData> Literals()
    {
        yield return new TestCaseData(null,                                        "NULL").SetName("null");
        yield return new TestCaseData(DBNull.Value,                                "NULL").SetName("dbnull");
        yield return new TestCaseData(true,                                        "True").SetName("bool");
        yield return new TestCaseData('c',                                         "'c'").SetName("char");
        yield return new TestCaseData((byte)1,                                     "1").SetName("byte");
        yield return new TestCaseData((sbyte)-1,                                   "-1").SetName("sbyte");
        yield return new TestCaseData((short)2,                                    "2").SetName("short");
        yield return new TestCaseData((ushort)3,                                   "3").SetName("ushort");
        yield return new TestCaseData(4,                                           "4").SetName("int");
        yield return new TestCaseData(5u,                                          "5").SetName("uint");
        yield return new TestCaseData(6L,                                          "6").SetName("long");
        yield return new TestCaseData(7UL,                                         "7").SetName("ulong");
        yield return new TestCaseData((Int128)8,                                   "8").SetName("int128");
        yield return new TestCaseData((UInt128)9,                                  "9").SetName("uint128");
        yield return new TestCaseData(1.5f,                                        "1.5").SetName("float");
        yield return new TestCaseData(2.5d,                                        "2.5").SetName("double");
        yield return new TestCaseData(3.5m,                                        "3.5").SetName("decimal");
        yield return new TestCaseData(DayOfWeek.Monday,                            "1").SetName("enum");
        yield return new TestCaseData(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), "'2026-01-02T03:04:05.0000000Z'").SetName("datetime");
        yield return new TestCaseData(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "'2026-01-02T03:04:05.0000000+00:00'").SetName("datetimeoffset");
        yield return new TestCaseData(new DateOnly(2026, 1, 2),                    "'2026-01-02'").SetName("dateonly");
        yield return new TestCaseData(new TimeOnly(3, 4, 5),                       "'03:04:05.0000000'").SetName("timeonly");
        yield return new TestCaseData(TimeSpan.FromMinutes(90),                    "'01:30:00'").SetName("timespan");
        yield return new TestCaseData(Guid.Empty,                                  $"'{Guid.Empty}'").SetName("guid");
    }

    [TestCaseSource(nameof(Literals))]
    public void Literals_Are_Inlined( object? value, string expected )
    {
        CommandParameter parameter = new(value, "p", ID);

        Assert.Multiple(() =>
                        {
                            Assert.That(parameter.IsLiteral, Is.True);
                            Assert.That(Literal(value),      Is.EqualTo(expected));
                        });
    }

    [Test] public void Other_Values_Are_Bound()
    {
        ValueStringBuilder value      = new();
        ValueStringBuilder assignment = new();
        CommandParameter   text       = new("O'Brien", "name", Name);
        text.AppendValue(ref value);
        text.AppendAssignment(ref assignment);

        ValueStringBuilder literalAssignment = new();
        new CommandParameter(1, "id", ID).AppendAssignment(ref literalAssignment);

        // Read (and dispose) the builders before the assertion lambda: it can't capture them.
        string boundValue      = value.ToString();
        string boundAssignment = assignment.ToString();
        string inlined         = literalAssignment.ToString();

        Assert.Multiple(() =>
                        {
                            Assert.That(text.IsLiteral,                      Is.False);
                            Assert.That(new CommandParameter(new byte[1], "b", Name).IsLiteral, Is.False);
                            Assert.That(boundValue,                          Is.EqualTo("@name"));
                            Assert.That(boundAssignment,                     Is.EqualTo("name = @name"));
                            Assert.That(inlined,                             Is.EqualTo("id = 1"));
                            Assert.That(Literal(new RowId(Guid.Empty)),     Is.EqualTo($"'{Guid.Empty}'"), "an ISqlParameterValue written directly is quoted");
                            Assert.That(Literal("raw"),                      Is.EqualTo("raw"), "AppendLiteral trusts its caller: only IsLiteral values reach it");
                        });
    }

    [Test] public void Constructor_Defaults_And_Suffix()
    {
        CommandParameter parameter = new("x", "name", Name);
        CommandParameter suffixed  = parameter.WithSuffix(3);

        Assert.Multiple(() =>
                        {
                            Assert.That(parameter.Direction,     Is.EqualTo(ParameterDirection.Input));
                            Assert.That(parameter.SourceVersion, Is.EqualTo(DataRowVersion.Default));
                            Assert.That(suffixed.ParameterName,  Is.EqualTo("name3"));
                            Assert.That(suffixed.Value,          Is.EqualTo("x"));
                            Assert.That(suffixed.Column,         Is.SameAs(Name));
                        });
    }

    [Test] public void Ordering_And_Equality()
    {
        CommandParameter a      = new("x", "a", Name);
        CommandParameter b      = new("y", "b", Name);
        CommandParameter sameA  = new("other value", "a", Name);
        CommandParameter idA    = new(1, "a", ID);
        object           boxedB = b;

        Assert.Multiple(() =>
                        {
                            Assert.That(a.Equals(sameA),         Is.True, "column and name decide equality");
                            Assert.That(a == sameA,              Is.True);
                            Assert.That(a != b,                  Is.True);
                            Assert.That(a.Equals((object)sameA), Is.True);
                            Assert.That(a.Equals((object)"a"),   Is.False);
                            Assert.That(a < b,                   Is.True);
                            Assert.That(b > a,                   Is.True);
                            Assert.That(a <= sameA,              Is.True);
                            Assert.That(a >= sameA,              Is.True);
                            Assert.That(idA.CompareTo(a),        Is.LessThan(0), "by column name first");
                            Assert.That(a.CompareTo(boxedB),     Is.LessThan(0));
                            Assert.That(a.CompareTo((object?)null), Is.GreaterThan(0));
                            Assert.That(() => a.CompareTo((object)"x"), Throws.TypeOf<ExpectedValueTypeException>());
                            Assert.That(a.GetHashCode(),         Is.EqualTo(new CommandParameter("x", "a", Name).GetHashCode()));
                        });
    }

}

