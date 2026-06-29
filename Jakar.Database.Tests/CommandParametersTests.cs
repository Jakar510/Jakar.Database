// Jakar.Database :: Jakar.Database.Tests
// 06/28/2026

using System.Collections.Immutable;
using System.Data;
using Jakar.Extensions;



namespace Jakar.Database.Tests;


[TestFixture]
public sealed class CommandParametersTests : Assert
{
    private static RoleRecord CreateRole() => RoleRecord.Create("Admin", Permissions<TestDatabase.TestRight>.SA(), "ADMINS");


    [Test] public void Create_empty_binds_table_metadata_and_starts_empty()
    {
        CommandParameters parameters = CommandParameters.Create<RoleRecord>();

        Multiple(() =>
                 {
                     That(parameters.Count,             Is.EqualTo(0));
                     That(parameters.IsGrouped,         Is.False);
                     That(parameters.ParameterCount,    Is.EqualTo(0));
                     That((object)parameters.Table,     Is.SameAs(RoleRecord.MetaData));
                     That(parameters.Table.ColumnCount, Is.EqualTo(RoleRecord.MetaData.ColumnCount));
                 });
    }

    [Test] public void Add_string_values_increments_count_and_skips_duplicates()
    {
        CommandParameters parameters = CommandParameters.Create<RoleRecord>();
        parameters.Add(nameof(RoleRecord.NameOfRole),     "Admin");
        parameters.Add(nameof(RoleRecord.NormalizedName), "ADMINS");

        // Re-adding the exact same parameter (same column + parameter name) is a no-op.
        parameters.Add(nameof(RoleRecord.NameOfRole), "Admin");

        Multiple(() =>
                 {
                     That(parameters.Count,         Is.EqualTo(2));
                     That(parameters.Values.Length, Is.EqualTo(2));
                 });
    }

    [Test] public void Create_from_record_covers_every_column()
    {
        RoleRecord        role       = CreateRole();
        CommandParameters parameters = CommandParameters.Create(role);

        That(parameters.Count, Is.EqualTo(RoleRecord.PropertyCount));
    }

    [Test] public void Validate_returns_self_when_all_columns_present()
    {
        CommandParameters validated = CommandParameters.Create(CreateRole()).Validate<RoleRecord>();

        That(validated.Count, Is.EqualTo(RoleRecord.PropertyCount));
    }

    [Test] public void Validate_throws_when_columns_are_missing()
    {
        InvalidOperationException? exception = Throws<InvalidOperationException>(() => CommandParameters.Create<RoleRecord>().Validate<RoleRecord>());

        That(exception, Is.Not.Null);
    }

    [Test] public void AddGroup_marks_collection_grouped_and_counts_grouped_parameters()
    {
        CommandParameters parameters = CommandParameters.Create<RoleRecord>();
        parameters.AddGroup(CreateRole());

        Multiple(() =>
                 {
                     That(parameters.IsGrouped,      Is.True);
                     That(parameters.Groups.Length,  Is.EqualTo(1));
                     That(parameters.Count,          Is.EqualTo(0));
                     That(parameters.ParameterCount, Is.EqualTo(RoleRecord.PropertyCount));
                 });
    }

    [Test] public void Parameters_buffer_length_matches_parameter_count()
    {
        CommandParameters parameters = CommandParameters.Create(CreateRole());

        using ArrayBuffer<SqlParameter> buffer = parameters.Parameters;
        That(buffer.Span.Length, Is.EqualTo(parameters.ParameterCount));
    }

    [Test] public void ParameterNames_are_sorted_and_match_count()
    {
        CommandParameters parameters = CommandParameters.Create(CreateRole());

        using ParameterNames   names = parameters.ParameterNames;
        names.Reset();
        ImmutableArray<string> array = names.Array;

        Multiple(() =>
                 {
                     That(array, Has.Length.EqualTo(parameters.Count));
                     That(array, Is.Ordered);
                 });
    }

    [Test] public void IndexedParameters_yields_primary_set_then_each_group()
    {
        CommandParameters parameters = CommandParameters.Create<RoleRecord>();
        parameters.AddGroup(CreateRole());

        int sets = 0;
        foreach ( IndexedEnumerator.Set set in parameters.IndexedParameters ) { sets++; }

        That(sets, Is.EqualTo(1 + parameters.Groups.Length));
    }

    [Test] public void VariableNames_ColumnNames_and_KeyValuePairs_render_expected_fragments()
    {
        CommandParameters parameters = CommandParameters.Create(CreateRole());

        string variables    = parameters.VariableNames(1).ToString();
        string columnNames  = parameters.ColumnNames(1).ToString();
        string keyValuePair = parameters.KeyValuePairs(1, "AND").ToString();

        Multiple(() =>
                 {
                     That(variables,    Does.Contain("@"));
                     That(columnNames,  Is.Not.Empty);
                     That(keyValuePair, Does.Contain(" = @"));
                     That(keyValuePair, Does.Contain("AND"));
                 });
    }

    [Test] public void Hashing_is_stable_for_the_same_instance()
    {
        CommandParameters parameters = CommandParameters.Create(CreateRole());

        ulong   firstHash64   = parameters.GetHash64();
        ulong   secondHash64  = parameters.GetHash64();
        UInt128 firstHash128  = parameters.GetHash128();
        UInt128 secondHash128 = parameters.GetHash128();
        int     firstCode     = parameters.GetHashCode();
        int     secondCode    = parameters.GetHashCode();

        Multiple(() =>
                 {
                     That(secondHash64,  Is.EqualTo(firstHash64));
                     That(secondHash128, Is.EqualTo(firstHash128));
                     That(secondCode,    Is.EqualTo(firstCode));
                 });
    }

    [Test] public void Equality_is_identity_based_per_instance()
    {
        CommandParameters left  = CommandParameters.Create<RoleRecord>();
        CommandParameters right = CommandParameters.Create<RoleRecord>();

        Multiple(() =>
                 {
                     That(left.Equals(left),  Is.True);
                     That(left.Equals(right), Is.False);
                     That(left == right,      Is.False);
                     That(left != right,      Is.True);
                 });
    }

    [Test] public void Empty_sentinel_is_inert()
    {
        CommandParameters empty = CommandParameters.Empty;

        Multiple(() =>
                 {
                     That(empty.Count,     Is.EqualTo(0));
                     That(empty.IsGrouped, Is.False);
                 });
    }

    [Test] public void Add_record_id_and_string_overloads_store_values()
    {
        RecordID<UserRecord> userID     = RecordID<UserRecord>.New();
        CommandParameters    parameters = CommandParameters.Create<RoleRecord>();
        parameters.Add(nameof(RoleRecord.UserID),     userID);
        parameters.Add(nameof(RoleRecord.NameOfRole), "Admin");

        That(parameters.Count, Is.EqualTo(2));
    }
}
