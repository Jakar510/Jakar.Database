// Jakar.Extensions :: Jakar.Database
// 09/30/2025  20:32

using ZLinq.Linq;



namespace Jakar.Database;


[Serializable]
public sealed record MigrationRecord : TableRecord<MigrationRecord>, ITableRecord<MigrationRecord>
{
    public const           string     TABLE_NAME = "migrations";
    public static readonly SqlCommand SelectSql  = SqlCommand.Parse<MigrationRecord>($"SELECT * FROM {TABLE_NAME} ORDER BY {nameof(MigrationID)};");
    public static readonly SqlCommand TryCreateSql = SqlCommand.Parse<MigrationRecord>($"""
                                                                                        CREATE TABLE IF NOT EXISTS {TABLE_NAME} (
                                                                                            {nameof(MigrationID)} bigint      PRIMARY KEY,
                                                                                            {nameof(AppliedOn)}   timestamptz NULL,
                                                                                            {nameof(DateCreated)} timestamptz NULL,
                                                                                            {nameof(Description)} text        NOT NULL,
                                                                                            {nameof(ReferenceID)} text        NULL
                                                                                        ); 
                                                                                        """);
    public static readonly string SetLastModifiedName = nameof(SetLastModified).SqlName();
    internal readonly      string RollbackID          = Randoms.RandomString(10);


    public static         string          TableName   => TABLE_NAME;
    public                DateTimeOffset? AppliedOn   { get; set; }
    public required       string          Description { get; init; }
    [Key] public required long            MigrationID { get; init; }
    public                string?         ReferenceID { get; init; }
    [DbIgnore] public     string          SQL         { get; internal init; } = EMPTY;


    public MigrationRecord() : base(DateTimeOffset.UtcNow) { }
    [SetsRequiredMembers] public MigrationRecord( DbDataReader reader ) : base(reader)
    {
        Description = reader.GetFieldValue<MigrationRecord, string>(nameof(Description));
        ReferenceID = reader.GetFieldValue<MigrationRecord, string?>(nameof(ReferenceID));
        AppliedOn   = reader.GetFieldValue<MigrationRecord, DateTimeOffset?>(nameof(AppliedOn));
        MigrationID = reader.GetFieldValue<MigrationRecord, long>(nameof(MigrationID));
    }

    public static MigrationRecord SetLastModified( long migrationID ) => new()
                                                                         {
                                                                             MigrationID = migrationID,
                                                                             Description = $"create {SetLastModifiedName} function",
                                                                             SQL = $"""
                                                                                    CREATE OR REPLACE FUNCTION {SetLastModifiedName}()
                                                                                    RETURNS TRIGGER AS $$
                                                                                    BEGIN
                                                                                        NEW.{nameof(ILastModified.LastModified).SqlName()} = now();
                                                                                        RETURN NEW;
                                                                                    END;
                                                                                    $$ LANGUAGE plpgsql;
                                                                                    """
                                                                         };


    /// <summary>
    ///     <para> pg_textsearch <see href="https://github.com/timescale/pg_textsearch"/> </para>
    ///     <para> uint128 <see href="https://github.com/pg-uint/pg-uint128"/> </para>
    ///     <para> pg_crypto <see href="https://www.postgresql.org/docs/current/pgcrypto.html"/> </para>
    ///     <para> pg_crypto <see href="https://github.com/pgaudit/pgaudit/tree/main"/> </para>
    ///     <para> pg_crypto <see href="https://learnsql.com/blog/postgis-basic-queries/"/> </para>
    /// </summary>
    /// <param name="migrationID"> </param>
    /// <returns> </returns>
    public static MigrationRecord AddPostgreSqlExtensions( long migrationID ) =>
        Create<MigrationRecord>(migrationID,
                                "Add PostgreSql extensions",

                                // ReSharper disable StringLiteralTypo
                                """

                                CREATE EXTENSION pg_crypto;
                                CREATE EXTENSION postgis;
                                CREATE EXTENSION pgaudit;
                                CREATE EXTENSION pg_textsearch;
                                CREATE EXTENSION uint128;
                                """

                                // ReSharper restore StringLiteralTypo
                               );


    public static MigrationRecord CreateTable( long migrationID ) => MetaData.CreateTable(migrationID);
    public SqlCommand ApplySql()
    {
        CommandParameters parameters = ToDynamicParameters();

        SqlCommand command = SqlCommand.Parse<MigrationRecord>($"""
                                                                INSERT INTO {TABLE_NAME} 
                                                                (
                                                                {MetaData.ColumnNames(1)}
                                                                )
                                                                VALUES
                                                                (
                                                                {parameters.VariableNames(1)}
                                                                )
                                                                """);

        return command;
    }


    public static MigrationRecord FromEnum<TEnum>( long migrationID )
        where TEnum : unmanaged, Enum
    {
        ValueEnumerable<FromArray<string>, string> enumerable = Enum.GetNames(typeof(TEnum)).AsValueEnumerable();

        string tableName = typeof(TEnum).SqlName();

        MigrationRecord record = new()
                                 {
                                     MigrationID = migrationID,
                                     Description = $"create {tableName} table",
                                     ReferenceID = tableName,
                                     SQL         = $"CREATE TYPE {tableName} AS ENUM ({getValues()})"
                                 };

        return record.Validate();

        static StringBuilder getValues()
        {
            StringBuilder values = new();
            values.AppendJoin(",\n", Enum.GetNames(typeof(TEnum)).Select(x => $"'{x}'"));
            return values;
        }
    }


    public static MigrationRecord Create<TSelf>( long migrationID, string description, string sql )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        MigrationRecord record = new()
                                 {
                                     MigrationID = migrationID,
                                     Description = description,
                                     ReferenceID = TSelf.TableName,
                                     SQL         = sql
                                 };

        return record.Validate();
    }


    public static MigrationRecord Create( DbDataReader reader )
    {
        MigrationRecord record = new(reader);
        return record.Validate();
    }


    protected override async ValueTask Import( NpgsqlBinaryImporter importer, string propertyName, NpgsqlDbType postgresDbType, CancellationToken token )
    {
        switch ( propertyName )
        {
            case nameof(MigrationID):
                await importer.WriteAsync(MigrationID, postgresDbType, token);
                return;

            case nameof(AppliedOn):
                await importer.WriteAsync(AppliedOn, postgresDbType, token);
                return;

            case nameof(ReferenceID):
                await importer.WriteAsync(ReferenceID, postgresDbType, token);
                return;

            case nameof(Description):
                await importer.WriteAsync(Description, postgresDbType, token);
                return;

            case nameof(DateCreated):
                await importer.WriteAsync(DateCreated, postgresDbType, token);
                return;

            default:
                throw new InvalidOperationException($"Unknown column: {propertyName}");
        }
    }
    public override ValueTask Import( DataRow row, CancellationToken token )
    {
        row[MetaData[nameof(ReferenceID)].DataColumn] = ReferenceID;
        row[MetaData[nameof(Description)].DataColumn] = Description;
        row[MetaData[nameof(AppliedOn)].DataColumn]   = AppliedOn;
        row[MetaData[nameof(MigrationID)].DataColumn] = MigrationID;
        return base.Import(row, token);
    }
    public override CommandParameters ToDynamicParameters()
    {
        CommandParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(MigrationID), MigrationID);
        parameters.Add(nameof(ReferenceID), ReferenceID);
        parameters.Add(nameof(AppliedOn),   AppliedOn);
        parameters.Add(nameof(Description), Description);
        return parameters;
    }


    public override bool Equals( MigrationRecord?    other ) => ReferenceEquals(this, other) || Nullable.Equals(MigrationID, other?.MigrationID) || string.Equals(Description, other?.Description);
    public override int  CompareTo( MigrationRecord? other ) => Nullable.Compare(AppliedOn, other?.AppliedOn);
    public override int  GetHashCode()                       => HashCode.Combine(MigrationID, ReferenceID, Description, SQL);


    public static bool operator >( MigrationRecord  left, MigrationRecord right ) => Comparer<MigrationRecord>.Default.Compare(left, right) > 0;
    public static bool operator >=( MigrationRecord left, MigrationRecord right ) => Comparer<MigrationRecord>.Default.Compare(left, right) >= 0;
    public static bool operator <( MigrationRecord  left, MigrationRecord right ) => Comparer<MigrationRecord>.Default.Compare(left, right) < 0;
    public static bool operator <=( MigrationRecord left, MigrationRecord right ) => Comparer<MigrationRecord>.Default.Compare(left, right) <= 0;
}
