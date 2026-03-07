// Jakar.Extensions :: Jakar.Database
// 10/19/2025  10:38

namespace Jakar.Database;


public readonly struct SqlCommand : IEquatable<SqlCommand>
{
    public const    string             SPACER = ",\n      ";
    public readonly string             SQL;
    public readonly PostgresParameters Parameters;
    public readonly CommandType?       CommandType;
    public readonly CommandFlags       Flags;


    private SqlCommand( string sql, PostgresParameters parameters, CommandType? commandType = null, CommandFlags flags = CommandFlags.None )
    {
        SQL         = sql;
        Parameters  = parameters;
        CommandType = commandType;
        Flags       = flags;
    }


    public static implicit operator string( SqlCommand             sql ) => sql.SQL;
    public static implicit operator PostgresParameters( SqlCommand sql ) => sql.Parameters;


    [Pure] [MustDisposeResource] public NpgsqlCommand ToCommand( NpgsqlConnection connection, NpgsqlTransaction? transaction = null )
    {
        ArgumentNullException.ThrowIfNull(connection);

        NpgsqlCommand command = new()
                                {
                                    Connection               = connection,
                                    CommandText              = SQL,
                                    CommandType              = CommandType ?? System.Data.CommandType.Text,
                                    Transaction              = transaction,
                                    CommandTimeout           = 30,
                                    AllResultTypesAreUnknown = false
                                };

        foreach ( NpgsqlParameter parameter in Parameters.Parameters ) { command.Parameters.Add(parameter); }

        return command;
    }


    public override string ToString() => DbSqlException.GetMessage(nameof(SqlCommand), SQL, Parameters);
    public async ValueTask ExecuteNonQueryAsync( NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken token )
    {
        await using NpgsqlCommand command = ToCommand(connection, transaction);
        await command.ExecuteNonQueryAsync(token);
    }
    public async IAsyncEnumerable<TSelf> ExecuteAsync<TSelf>( NpgsqlConnection connection, NpgsqlTransaction? transaction, [EnumeratorCancellation] CancellationToken token )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        await using NpgsqlCommand    command = ToCommand(connection, transaction);
        await using NpgsqlDataReader reader  = await command.ExecuteReaderAsync(token);
        while ( await reader.ReadAsync(token) ) { yield return TSelf.Create(reader); }
    }


    public static SqlCommand Create( string sql, in PostgresParameters parameters, CommandType? commandType = null, CommandFlags flags = CommandFlags.None ) => new(sql, parameters, commandType, flags);


    public static SqlCommand Create<TSelf>( string sql )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => new(sql, PostgresParameters.Create<TSelf>());
    public static SqlCommand Create<TSelf>( string sql, params ReadOnlySpan<NpgsqlParameter> parameters )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => new(sql, PostgresParameters.Create<TSelf>(parameters));
    public static SqlCommand Create<TSelf>( (string SQL, NpgsqlParameter[] Parameters) pair )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Create<TSelf>(pair.SQL, pair.Parameters);
    public static SqlCommand Create<TSelf>( (string SQL, List<NpgsqlParameter> Parameters) pair )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Create<TSelf>(pair.SQL, pair.Parameters.AsSpan());
    public static SqlCommand Create<TSelf>( (string SQL, ImmutableArray<NpgsqlParameter> Parameters) pair )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Create<TSelf>(pair.SQL, pair.Parameters.AsSpan());


    public static SqlCommand Parse<TSelf>( ref SqlInterpolatedStringHandler<TSelf> handler, CommandType? commandType = null, CommandFlags flags = CommandFlags.None )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => handler.ToSqlCommand(commandType, flags);


    public static SqlCommand GetRandom<TSelf>( int count = 1 )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               ORDER BY RANDOM()
                                                                               LIMIT {count};
                                                                               """);
    public static SqlCommand GetRandom<TSelf>( UserRecord user, int count = 1 )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => GetRandom<TSelf>(user.ID, count);
    public static SqlCommand GetRandom<TSelf>( in RecordID<UserRecord> userID, int count = 1 )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               WHERE 
                                                                                    {nameof(IUserID.UserID)} = {userID.Value}
                                                                               ORDER BY RANDOM()
                                                                               LIMIT {count};
                                                                               """);


    public static SqlCommand WherePaged<TSelf>( in PostgresParameters parameters, int start, int count )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                                 SELECT * FROM {TSelf.TableName} 
                                                                                 WHERE
                                                                                 {parameters.KeyValuePairs(1)}
                                                                                 OFFSET {start}
                                                                                 LIMIT {count}
                                                                               """);
    public static SqlCommand WherePaged<TSelf>( in RecordID<UserRecord> userID, int start, int count )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               WHERE 
                                                                                   {nameof(IUserID.UserID)} = {userID.Value}
                                                                               OFFSET {start}
                                                                               LIMIT {count};
                                                                               """);
    public static SqlCommand WherePaged<TSelf>( int start, int count )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               OFFSET {start}
                                                                               LIMIT {count};
                                                                               """);
    public static SqlCommand WherePaged<TSelf>( DateTimeOffset startTime, int start, int count )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               WHERE 
                                                                                    {nameof(IDateCreated.DateCreated)} > {startTime}
                                                                               OFFSET {start}
                                                                               LIMIT {count};
                                                                               """);
    public static SqlCommand Where<TSelf, TValue>( string columnName, TValue? value )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"SELECT * FROM {TSelf.TableName} WHERE {columnName} = @{value};");


    public static SqlCommand Get<TSelf>( in RecordID<TSelf> id )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              SELECT * FROM {TSelf.TableName}
                                                                              WHERE 
                                                                              {nameof(IUniqueID.ID)} = {id};
                                                                              """);
    public static SqlCommand Get<TSelf>( IEnumerable<RecordID<TSelf>> ids )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              SELECT * FROM {TSelf.TableName}
                                                                              WHERE 
                                                                                    {nameof(IUniqueID.ID)} in (
                                                                                    {ids.Select(RecordID<TSelf>.GetValue):2}
                                                                                    );
                                                                              """);
    public static SqlCommand Get<TSelf>( in PostgresParameters parameters )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               WHERE 
                                                                               {parameters.KeyValuePairs(1)};
                                                                               """);
    public static SqlCommand GetAll<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"SELECT * FROM {TSelf.TableName};");
    public static SqlCommand GetFirst<TSelf>( int count = 1 )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               ORDER BY {nameof(IDateCreated.DateCreated)} ASC 
                                                                               LIMIT {count};
                                                                               """);
    public static SqlCommand GetLast<TSelf>( int count = 1 )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               ORDER BY {nameof(IDateCreated.DateCreated)} DESC 
                                                                               LIMIT {count}
                                                                               """);


    public static SqlCommand GetCount<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"SELECT COUNT({nameof(IUniqueID.ID)}) FROM {TSelf.TableName};");
    public static SqlCommand GetSortedID<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT {nameof(IUniqueID.ID)}, {nameof(IDateCreated.DateCreated)} FROM {TSelf.TableName}
                                                                               ORDER BY {nameof(IDateCreated.DateCreated)} DESC;
                                                                               """);
    public static SqlCommand GetExists<TSelf>( in PostgresParameters parameters )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               EXISTS( 
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               WHERE 
                                                                               {parameters.KeyValuePairs(1)};
                                                                               """);


    public static SqlCommand GetDelete<TSelf>( in PostgresParameters parameters )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               DELETE FROM {TSelf.TableName} 
                                                                               WHERE 
                                                                               {nameof(IUniqueID.ID)} in (
                                                                               {parameters.KeyValuePairs(2)}
                                                                               );
                                                                               """);
    public static SqlCommand GetDelete<TSelf>( in RecordID<TSelf> id )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              DELETE FROM {TSelf.TableName}
                                                                              WHERE {nameof(IUniqueID.ID)} = {id.Value};
                                                                              """);
    public static SqlCommand GetDelete<TSelf>( IEnumerable<RecordID<TSelf>> ids )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              DELETE FROM {TSelf.TableName} 
                                                                              WHERE 
                                                                                    {nameof(IUniqueID.ID)} in (
                                                                                    {ids.Select(RecordID<TSelf>.GetValue):2}
                                                                                    );
                                                                              """);
    public static SqlCommand GetDeleteAll<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"DELETE FROM {TSelf.TableName};");


    public static SqlCommand GetNext<TSelf>( in RecordPair<TSelf> pair )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              SELECT * FROM {TSelf.TableName}
                                                                              WHERE ( id = IFNULL((SELECT MIN({nameof(IDateCreated.DateCreated)}) FROM {TSelf.TableName} WHERE {nameof(IDateCreated.DateCreated)} > {pair.DateCreated} LIMIT 2), 0) );
                                                                              """);
    public static SqlCommand GetNextID<TSelf>( in RecordPair<TSelf> pair )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              SELECT {nameof(IUniqueID.ID)} FROM {TSelf.TableName}
                                                                              WHERE ( id = IFNULL((SELECT MIN({nameof(IDateCreated.DateCreated)}) FROM {TSelf.TableName} WHERE {nameof(IDateCreated.DateCreated)} > {pair.DateCreated} LIMIT 2), 0) );
                                                                              """);


    public static SqlCommand GetCopy<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               CREATE TEMP TABLE temp_table (LIKE {TSelf.TableName} INCLUDING DEFAULTS);

                                                                               COPY temp_table (
                                                                               {TSelf.MetaData.ColumnNames(1)}
                                                                               )
                                                                               FROM STDIN;

                                                                               INSERT INTO {TSelf.TableName}
                                                                               SELECT *
                                                                               FROM temp_table
                                                                               RETURNING {nameof(IUniqueID.ID)};      
                                                                               """);
    public static SqlCommand GetInsert<TSelf>( TSelf record )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters parameters = record.ToDynamicParameters();

        return Parse<TSelf>($"""
                             INSERT INTO {TSelf.TableName} 
                             (
                             {TSelf.MetaData.ColumnNames(1)}
                             )
                             VALUES
                             (
                             {parameters.VariableNames(1)}
                             )
                             RETURNING {nameof(IUniqueID.ID)};
                             """);
    }
    public static SqlCommand GetInsert<TSelf>( IEnumerable<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters parameters = PostgresParameters.Create(records);

        return Parse<TSelf>($"""
                             INSERT INTO {TSelf.TableName} 
                             (
                             {TSelf.MetaData.ColumnNames(1)}
                             )
                             VALUES
                             (
                             {parameters.VariableNames(1)}
                             )
                             RETURNING {nameof(IUniqueID.ID)};
                             """);
    }
    public static SqlCommand GetInsert<TSelf>( params ReadOnlySpan<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters parameters = PostgresParameters.Create(records);

        return Parse<TSelf>($"""
                             INSERT INTO {TSelf.TableName} 
                             (
                             {TSelf.MetaData.ColumnNames(1)}
                             )
                             VALUES
                             (
                             {parameters.VariableNames(1)}
                             )
                             RETURNING {nameof(IUniqueID.ID)};
                             """);
    }


    public static SqlCommand GetUpdate<TSelf>( TSelf record )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters parameters = record.ToDynamicParameters();

        return Parse<TSelf>($"""
                             UPDATE {TSelf.TableName} 
                             SET 
                             {parameters} 
                             WHERE 
                                  {nameof(IUniqueID.ID)} = @{nameof(IUniqueID.ID)};
                             """);
    }
    public static SqlCommand GetTryInsert<TSelf>( TSelf record, in PostgresParameters parameters )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters recordParameters = record.ToDynamicParameters();

        return Parse<TSelf>($"""
                             IF NOT EXISTS (
                             SELECT * FROM {TSelf.TableName} 
                                WHERE 
                                    {nameof(IUniqueID.ID)} = @{record.ID}
                                OR 
                                (
                             {parameters.KeyValuePairs(2)}
                                )
                             )

                             BEGIN
                             INSERT INTO {TSelf.TableName}
                                (
                             {recordParameters.ColumnNames(2)}
                                )
                             VALUES
                                (
                             {recordParameters.VariableNames(2)}
                                ) 
                             RETURNING {nameof(IUniqueID.ID)};
                             END

                             ELSE
                             BEGIN
                             SELECT {Guid.Empty};
                             END
                             """);
    }
    public static SqlCommand InsertOrUpdate<TSelf>( TSelf record, in PostgresParameters parameters )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        PostgresParameters recordParameters = record.ToDynamicParameters();

        return Parse<TSelf>($"""
                             IF NOT EXISTS 
                             (
                                 SELECT * FROM {TSelf.TableName}
                                     WHERE
                                         {nameof(IUniqueID.ID)} = @{record.ID}
                                         OR 
                                             (
                             {parameters.KeyValuePairs(4)}
                                             )
                             )
                                 
                             BEGIN
                             INSERT INTO {TSelf.TableName}
                                (
                             {recordParameters.ColumnNames(2)}
                                ) 
                             VALUES 
                                 (
                             {recordParameters.VariableNames(2)}
                                 ) 
                             RETURNING {nameof(IUniqueID.ID)};
                             END

                             ELSE
                             BEGIN
                             UPDATE {TSelf.TableName} 
                                SET
                             {recordParameters.VariableNames(2)}
                                WHERE 
                                    {nameof(IUniqueID.ID)} = @{nameof(IUniqueID.ID)};

                             SELECT @{nameof(IUniqueID.ID)};
                             END
                             """);
    }


    public          bool Equals( SqlCommand other ) => string.Equals(SQL, other.SQL, StringComparison.InvariantCultureIgnoreCase) && Parameters.Equals(other.Parameters) && CommandType == other.CommandType && Flags == other.Flags;
    public override bool Equals( object?    obj )   => obj is SqlCommand other                                                    && Equals(other);
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(SQL, StringComparer.InvariantCultureIgnoreCase);
        hashCode.Add(Parameters);
        hashCode.Add(CommandType);
        hashCode.Add((int)Flags);
        return hashCode.ToHashCode();
    }


    public static bool operator ==( SqlCommand left, SqlCommand right ) => left.Equals(right);
    public static bool operator !=( SqlCommand left, SqlCommand right ) => !left.Equals(right);
}
