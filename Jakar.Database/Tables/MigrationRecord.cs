// Jakar.Extensions :: Jakar.Database
// 09/30/2025  20:32

using ZLinq.Linq;



namespace Jakar.Database;


[Serializable]
public sealed record MigrationRecord : TableRecord<MigrationRecord>, ITableRecord<MigrationRecord>
{
    public const           string TABLE_NAME = "migrations";
    public static readonly string SelectSql  = $"SELECT * FROM {TABLE_NAME} ORDER BY {nameof(MigrationID).SqlColumnName()}";
    public static readonly string ApplySql = $"""
                                              INSERT INTO {TABLE_NAME} 
                                              (
                                              {nameof(MigrationID).SqlColumnName()},
                                              {nameof(TableID).SqlColumnName()},
                                              {nameof(Description).SqlColumnName()},
                                              {nameof(AppliedOn).SqlColumnName()},
                                              ) 
                                              VALUES 
                                              (
                                              @{nameof(MigrationID).SqlColumnName()},
                                              @{nameof(TableID).SqlColumnName()},
                                              @{nameof(Description).SqlColumnName()},
                                              @{nameof(AppliedOn).SqlColumnName()},
                                              )
                                              """;


    public static                                       string         TableName   => TABLE_NAME;
    public                                              DateTimeOffset AppliedOn   { get; init; } = DateTimeOffset.UtcNow;
    public required                                     string         Description { get; init; }
    [Key] public required                               ulong          MigrationID { get; init; }
    internal                                            string         SQL         { get; init; } = EMPTY;
    [ColumnInfo(ColumnOptions.Indexed, 256)] public string?        TableID     { get; init; }


    [SetsRequiredMembers] internal MigrationRecord( ulong migrationID, string description, string? tableID = null ) : base(DateTimeOffset.UtcNow)
    {
        MigrationID = migrationID;
        Description = description;
        TableID     = tableID;
    }
    [SetsRequiredMembers] public MigrationRecord( NpgsqlDataReader reader ) : base(reader)
    {
        Description = reader.GetFieldValue<MigrationRecord, string>(nameof(Description));
        TableID     = reader.GetFieldValue<MigrationRecord, string?>(nameof(TableID));
        AppliedOn   = reader.GetFieldValue<MigrationRecord, DateTimeOffset>(nameof(AppliedOn));
        MigrationID = reader.GetFieldValue<MigrationRecord, ulong>(nameof(MigrationID));
    }
    public static MigrationRecord SetLastModified( ulong migrationID )
    {
        string name = nameof(SetLastModified)
           .SqlColumnName();

        return new MigrationRecord(migrationID, $"create {name} function")
               {
                   SQL = $"""
                          CREATE OR REPLACE FUNCTION {name}()
                          RETURNS TRIGGER AS $$
                          BEGIN
                              NEW.{nameof(ILastModified.LastModified).SqlColumnName()} = now();
                              RETURN NEW;
                          END;
                          $$ LANGUAGE plpgsql;
                          """
               };
    }


    /// <summary>
    /// <para> pg_textsearch <see href="https://github.com/timescale/pg_textsearch"/> </para>
    /// <para> uint128 <see href="https://github.com/pg-uint/pg-uint128"/> </para>
    /// <para> pg_crypto <see href="https://www.postgresql.org/docs/current/pgcrypto.html"/> </para>
    /// <para> pg_crypto <see href="https://github.com/pgaudit/pgaudit/tree/main"/> </para>
    /// <para> pg_crypto <see href="https://learnsql.com/blog/postgis-basic-queries/"/> </para>
    /// </summary>
    /// <param name="migrationID"></param>
    /// <returns></returns>
    public static MigrationRecord AddPostgreSqlExtensions( ulong migrationID ) =>
        Create<MigrationRecord>(migrationID,
                                "Add PostgreSql extensions",

                                // ReSharper disable StringLiteralTypo
                                """
                                CREATE EXTENSION pg_textsearch;
                                CREATE EXTENSION uint128;
                                CREATE EXTENSION pg_crypto;
                                CREATE EXTENSION pgaudit;
                                CREATE EXTENSION postgis;
                                """

                                // ReSharper restore StringLiteralTypo
                               );
    public static MigrationRecord CreateTable( ulong migrationID ) => CreateTable<MigrationRecord>(migrationID);
    public static MigrationRecord CreateTable<TSelf>( ulong migrationID )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Create<TSelf>(migrationID, $"Create '{TSelf.TableName}' Table", TableMetaData<TSelf>.CreateTable());


    public static MigrationRecord FromEnum<TEnum>( ulong migrationID )
        where TEnum : unmanaged, Enum
    {
        ValueEnumerable<FromArray<string>, string> enumerable = Enum.GetNames(typeof(TEnum))
                                                                    .AsValueEnumerable();

        string tableName = typeof(TEnum).Name.SqlColumnName();
        int    length    = enumerable.Max(static x => x.Length);

        MigrationRecord record = new(migrationID, $"create {tableName} table")
                                 {
                                     TableID = tableName,
                                     SQL = $"""
                                            CREATE TABLE IF NOT EXISTS {tableName}
                                            (
                                            id   bigint            PRIMARY KEY,
                                            name varchar({length}) UNIQUE NOT NULL,
                                            );


                                            CREATE TRIGGER {nameof(SetLastModified).SqlColumnName()}
                                            BEFORE INSERT OR UPDATE ON {tableName}
                                            FOR EACH ROW
                                            EXECUTE FUNCTION {nameof(SetLastModified).SqlColumnName()}();


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

            using PooledArray<string> array = enumerable.Select(static ( v, i ) => $"    ({i}, '{v}')")
                                                        .ToArrayPool();

            values.AppendJoin(",\n", array.Span);
            return values;
        }
    }


    public static MigrationRecord Create<TSelf>( ulong migrationID, string description, string sql )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        MigrationRecord record = new(migrationID, description)
                                 {
                                     TableID = TSelf.TableName,
                                     SQL     = sql
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
        using PooledArray<ColumnMetaData> buffer = PropertyMetaData.SortedColumns;

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
