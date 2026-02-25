// Jakar.Extensions :: Jakar.Database
// 09/30/2025  20:32

using ZLinq.Linq;



namespace Jakar.Database;


[Serializable]
public sealed record MigrationRecord : TableRecord<MigrationRecord>, ITableRecord<MigrationRecord>
{
    public const string CREATE_SAVE_POINT = "MigrationRecord.CreateSql";
    public const string TABLE_NAME        = "migrations";
    public static readonly string SelectSql = $"""
                                               DO $$
                                               BEGIN
                                                   IF EXISTS (
                                                       SELECT 1
                                                       FROM information_schema.tables
                                                       WHERE table_schema = 'public'
                                                       AND table_name = '{TABLE_NAME}'
                                                   ) 
                                                   THEN
                                                        PERFORM * FROM {TABLE_NAME} ORDER BY {nameof(MigrationID).SqlColumnName()};
                                                   END IF;
                                               END $$; 
                                               """;
    public static readonly string ApplySql = $"""
                                              INSERT INTO {TABLE_NAME} 
                                              (
                                                  {nameof(MigrationID).SqlColumnName()},
                                                  {nameof(AppliedOn).SqlColumnName()},
                                                  {nameof(Description).SqlColumnName()},
                                                  {nameof(TableID).SqlColumnName()}
                                              ) 
                                              VALUES 
                                              (
                                                  @{nameof(MigrationID).SqlColumnName()},
                                                  @{nameof(AppliedOn).SqlColumnName()},
                                                  @{nameof(Description).SqlColumnName()},
                                                  @{nameof(TableID).SqlColumnName()}
                                              )
                                              """;
    public static readonly string TryCreateSql = $"""
                                                  CREATE TABLE IF NOT EXISTS {TABLE_NAME} (
                                                      {nameof(MigrationID).SqlColumnName()} bigint      PRIMARY KEY,
                                                      {nameof(AppliedOn).SqlColumnName()}   timestamptz NULL,
                                                      {nameof(Description).SqlColumnName()} text        NOT NULL,
                                                      {nameof(TableID).SqlColumnName()}     text        NULL
                                                  );

                                                  CREATE INDEX IF NOT EXISTS idx_{TABLE_NAME}_{nameof(TableID).SqlColumnName()}
                                                  ON migrations({nameof(TableID).SqlColumnName()});
                                                  """;

    internal               long   MigrationIdValue;
    public static readonly string SetLastModifiedName = nameof(SetLastModified).SqlColumnName();
    internal readonly      string RollbackID          = Randoms.RandomString(10);


    public static              string          TableName   => TABLE_NAME;
    public                     DateTimeOffset? AppliedOn   { get;                     set; }
    public required            string          Description { get;                     init; }
    [Key]      public required long            MigrationID { get => MigrationIdValue; init => MigrationIdValue = value; }
    [DbIgnore] public          string          SQL         { get;                     internal init; } = EMPTY;
    public                     string?         TableID     { get;                     init; }


    public MigrationRecord() : base(DateTimeOffset.UtcNow) { }
    [SetsRequiredMembers] public MigrationRecord( NpgsqlDataReader reader ) : base(reader)
    {
        Description = reader.GetFieldValue<MigrationRecord, string>(nameof(Description));
        TableID     = reader.GetFieldValue<MigrationRecord, string?>(nameof(TableID));
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
                                                                                        NEW.{nameof(ILastModified.LastModified).SqlColumnName()} = now();
                                                                                        RETURN NEW;
                                                                                    END;
                                                                                    $$ LANGUAGE plpgsql;
                                                                                    """
                                                                         };


    /// <summary>
    /// <para> pg_textsearch <see href="https://github.com/timescale/pg_textsearch"/> </para>
    /// <para> uint128 <see href="https://github.com/pg-uint/pg-uint128"/> </para>
    /// <para> pg_crypto <see href="https://www.postgresql.org/docs/current/pgcrypto.html"/> </para>
    /// <para> pg_crypto <see href="https://github.com/pgaudit/pgaudit/tree/main"/> </para>
    /// <para> pg_crypto <see href="https://learnsql.com/blog/postgis-basic-queries/"/> </para>
    /// </summary>
    /// <param name="migrationID"></param>
    /// <returns></returns>
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


    public static MigrationRecord FromEnum<TEnum>( long migrationID )
        where TEnum : unmanaged, Enum
    {
        ValueEnumerable<FromArray<string>, string> enumerable = Enum.GetNames(typeof(TEnum)).AsValueEnumerable();

        string tableName = typeof(TEnum).Name.SqlColumnName();
        int    length    = enumerable.Max(static x => x.Length);

        MigrationRecord record = new()
                                 {
                                     MigrationID = migrationID,
                                     Description = $"create {tableName} table",
                                     TableID     = tableName,
                                     SQL = $"""
                                            CREATE TABLE IF NOT EXISTS {tableName}
                                            (
                                            id   bigint            PRIMARY KEY,
                                            name varchar({length}) UNIQUE NOT NULL,
                                            );

                                            -- Insert values if they do not exist with explicit ids (enum order)
                                            INSERT INTO {tableName} (id, name)
                                            SELECT v.id, v.name
                                            FROM ( VALUES {getValues(enumerable)} ) AS v(id, name)
                                            WHERE NOT EXISTS ( SELECT 1 FROM mime_types m WHERE m.id = v.id OR m.name = v.name );
                                            """
                                 };

        return record.Validate();

        static StringBuilder getValues( ValueEnumerable<FromArray<string>, string> enumerable )
        {
            StringBuilder values = new();

            using PooledArray<string> array = enumerable.Select(static ( v, i ) => $"    ({i}, '{v}')").ToArrayPool();

            values.AppendJoin(",\n", array.Span);
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
                                     TableID     = TSelf.TableName,
                                     SQL         = sql
                                 };

        return record.Validate();
    }


    public static MigrationRecord Create( NpgsqlDataReader reader )
    {
        MigrationRecord record = new(reader);
        return record.Validate();
    }


    public override async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )
    {
        using PooledArray<ColumnMetaData> buffer = MetaData.SortedColumns;

        foreach ( ColumnMetaData column in buffer.Array )
        {
            switch ( column.PropertyName )
            {
                case nameof(MigrationID):
                    await importer.WriteAsync(MigrationID, NpgsqlDbType.Bigint, token);
                    break;

                case nameof(AppliedOn):
                    await importer.WriteAsync(AppliedOn, NpgsqlDbType.TimestampTz, token);
                    break;

                case nameof(TableID):
                    await importer.WriteAsync(TableID, NpgsqlDbType.Bigint, token);
                    break;

                case nameof(Description):
                    await importer.WriteAsync(Description, NpgsqlDbType.Text, token);
                    break;

                case nameof(DateCreated):
                    await importer.WriteAsync(DateCreated, NpgsqlDbType.TimestampTz, token);
                    break;
            }
        }
    }
    public override PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = base.ToDynamicParameters();
        parameters.Add(nameof(MigrationID), MigrationID);
        parameters.Add(nameof(TableID),     TableID);
        parameters.Add(nameof(AppliedOn),   AppliedOn);
        parameters.Add(nameof(Description), Description);
        return parameters;
    }


    public override bool Equals( MigrationRecord?    other ) => ReferenceEquals(this, other) || Nullable.Equals(MigrationID, other?.MigrationID) || string.Equals(Description, other?.Description);
    public override int  CompareTo( MigrationRecord? other ) => Nullable.Compare(AppliedOn, other?.AppliedOn);
    public override int  GetHashCode()                       => HashCode.Combine(TableID, Description);


    public static bool operator >( MigrationRecord  left, MigrationRecord right ) => Comparer<MigrationRecord>.Default.Compare(left, right) > 0;
    public static bool operator >=( MigrationRecord left, MigrationRecord right ) => Comparer<MigrationRecord>.Default.Compare(left, right) >= 0;
    public static bool operator <( MigrationRecord  left, MigrationRecord right ) => Comparer<MigrationRecord>.Default.Compare(left, right) < 0;
    public static bool operator <=( MigrationRecord left, MigrationRecord right ) => Comparer<MigrationRecord>.Default.Compare(left, right) <= 0;
}
