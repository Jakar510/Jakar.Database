// Jakar.Extensions :: Jakar.Database
// 10/19/2025  10:38

namespace Jakar.Database;


public readonly struct SqlCommand<TSelf>( string sql, in PostgresParameters parameters, CommandType? commandType = null, CommandFlags flags = CommandFlags.None ) : IEquatable<SqlCommand<TSelf>>
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    public const    string             SPACER      = ",\n      ";
    public readonly string             SQL         = sql;
    public readonly PostgresParameters Parameters  = parameters;
    public readonly CommandType?       CommandType = commandType;
    public readonly CommandFlags       Flags       = flags;

    public static IEnumerable<string> GetColumnNames   => TSelf.MetaData.Properties.Values.Select(ColumnMetaData.GetColumnName);
    public static IEnumerable<string> GetKeyValuePairs => TSelf.MetaData.Properties.Values.Select(ColumnMetaData.GetKeyValuePair);


    public static implicit operator string( SqlCommand<TSelf>                                                   sql )  => sql.SQL;
    public static implicit operator SqlCommand<TSelf>( string                                                   sql )  => new(sql, PostgresParameters.Create<TSelf>());
    public static implicit operator SqlCommand<TSelf>( (string SQL, PostgresParameters Parameters)              pair ) => new(pair.SQL, pair.Parameters);
    public static implicit operator SqlCommand<TSelf>( (string SQL, NpgsqlParameter[] Parameters)               pair ) => new(pair.SQL, PostgresParameters.Create<TSelf>(pair.Parameters.AsSpan()));
    public static implicit operator SqlCommand<TSelf>( (string SQL, List<NpgsqlParameter> Parameters)           pair ) => new(pair.SQL, PostgresParameters.Create<TSelf>(pair.Parameters.AsSpan()));
    public static implicit operator SqlCommand<TSelf>( (string SQL, ImmutableArray<NpgsqlParameter> Parameters) pair ) => new(pair.SQL, PostgresParameters.Create<TSelf>(pair.Parameters.AsSpan()));


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

        command.Parameters.Add(Parameters.Span);
        return command;
    }


    public async ValueTask ExecuteNonQueryAsync( NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken token )
    {
        await using NpgsqlCommand command = ToCommand(connection, transaction);
        await command.ExecuteNonQueryAsync(token);
    }
    public async IAsyncEnumerable<TSelf> ExecuteAsync( NpgsqlConnection connection, NpgsqlTransaction? transaction, [EnumeratorCancellation] CancellationToken token )
    {
        await using NpgsqlCommand    command = ToCommand(connection, transaction);
        await using NpgsqlDataReader reader  = await command.ExecuteReaderAsync(token);
        while ( await reader.ReadAsync(token) ) { yield return TSelf.Create(reader); }
    }


    public static StringBuilder ColumnNames( int   indentLevel ) => TSelf.MetaData.ColumnNames(indentLevel);
    public static StringBuilder KeyValuePairs( int indentLevel ) => TSelf.MetaData.KeyValuePairs(indentLevel);


    public static SqlCommand<TSelf> Create( ref SqlInterpolatedStringHandler<TSelf> handler, CommandType?          commandType = null, CommandFlags flags = CommandFlags.None ) => handler.ToSqlCommand(commandType, flags);
    public static SqlCommand<TSelf> Create( string                                  sql,     in PostgresParameters parameters ) => new(sql, in parameters);


    public static SqlCommand<TSelf> GetRandom() => GetRandom(1);
    public static SqlCommand<TSelf> GetRandom( int count )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName} 
                      ORDER BY RANDOM() 
                      LIMIT {count};
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> GetRandom( UserRecord user, int count ) => GetRandom(user.ID, count);
    public static SqlCommand<TSelf> GetRandom( in RecordID<UserRecord> userID, int count )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName} 
                      WHERE {nameof(IUserID.UserID).SqlName()} = '{userID.Value}'
                      ORDER BY RANDOM() 
                      LIMIT {count};
                      """;

        return sql;
    }


    public static SqlCommand<TSelf> WherePaged( bool matchAll, in PostgresParameters parameters, int start, int count )
    {
        string sql = $"""
                        SELECT * FROM {TSelf.TableName}
                        {parameters.KeyValuePairs(matchAll, 1)}
                        OFFSET {start}
                        LIMIT {count}
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }
    public static SqlCommand<TSelf> WherePaged( in RecordID<UserRecord> userID, int start, int count )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName}
                      WHERE 
                          {nameof(IUserID.UserID).SqlName()} = '{userID.Value}'
                      OFFSET {start}
                      LIMIT {count};
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> WherePaged( int start, int count )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName}
                      OFFSET {start}
                      LIMIT {count};
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> Where<TValue>( string columnName, TValue? value )
    {
        string sql = $"SELECT * FROM {TSelf.TableName} WHERE {columnName} = @{nameof(value)};";

        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(value), value);

        return new SqlCommand<TSelf>(sql, in parameters);
    }


    public static SqlCommand<TRecord> Get<TRecord>( in RecordID<TRecord> id )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        string sql = $$"""
                       SELECT * FROM {{TSelf.TableName}}
                       WHERE 
                           {{nameof(IUniqueID.ID).SqlName()}} = '{0}';
                       """;

        return string.Format(sql, id.Value.ToString());
    }
    public static SqlCommand<TRecord> Get<TRecord>( IEnumerable<RecordID<TRecord>> ids )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName}
                      WHERE {nameof(IUniqueID.ID).SqlName()} in (
                             {string.Join(",\n        ", ids.Select(GetValue))}
                      );
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> Get( bool matchAll, in PostgresParameters parameters )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName}
                      WHERE 
                      {parameters.KeyValuePairs(matchAll, 1)};
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }
    public static SqlCommand<TSelf> GetAll() => $"SELECT * FROM {TSelf.TableName};";
    public static SqlCommand<TSelf> GetFirst( int count = 1 ) => $"""
                                                                  SELECT * FROM {TSelf.TableName}
                                                                  ORDER BY {nameof(IDateCreated.DateCreated).SqlName()} ASC 
                                                                  LIMIT {count};
                                                                  """;
    public static SqlCommand<TSelf> GetLast( int count = 1 ) => $"""
                                                                 SELECT * FROM {TSelf.TableName}
                                                                 ORDER BY {nameof(IDateCreated.DateCreated).SqlName()} DESC 
                                                                 LIMIT {count}
                                                                 """;


    public static SqlCommand<TSelf> GetCount() => $"SELECT COUNT(*) FROM {TSelf.TableName};";
    public static SqlCommand<TSelf> GetSortedID() => $"""
                                                      SELECT {nameof(IUniqueID.ID).SqlName()}, {nameof(IDateCreated.DateCreated).SqlName()} FROM {TSelf.TableName}
                                                      ORDER BY {nameof(IDateCreated.DateCreated).SqlName()} DESC;
                                                      """;
    public static SqlCommand<TSelf> GetExists( bool matchAll, in PostgresParameters parameters )
    {
        string sql = $"""
                      EXISTS( 
                      SELECT * FROM {TSelf.TableName}
                      WHERE 
                      {parameters.KeyValuePairs(matchAll, 1)};
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }


    public static SqlCommand<TSelf> GetDelete( bool matchAll, in PostgresParameters parameters )
    {
        string sql = $"""
                      DELETE FROM {TSelf.TableName} 
                      WHERE 
                         {nameof(IUniqueID.ID).SqlName()} in (
                      {parameters.KeyValuePairs(matchAll, 2)}
                         );
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }
    public static SqlCommand<TRecord> GetDeleteID<TRecord>( in RecordID<TRecord> id )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        string sql = $"""
                      DELETE FROM {TSelf.TableName}
                      WHERE {nameof(IUniqueID.ID).SqlName()} = '{id.Value}';
                      """;

        return sql;
    }
    public static SqlCommand<TRecord> GetDelete<TRecord>( IEnumerable<RecordID<TRecord>> ids )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        // ReSharper disable PossibleMultipleEnumeration
        StringBuilder sb = new(256 + 30 * ids.Count());
        sb.AppendJoin(',', ids.Select(GetValue));

        // ReSharper restore PossibleMultipleEnumeration

        string sql = $"""
                      DELETE FROM {TSelf.TableName} 
                      WHERE {nameof(IUniqueID.ID).SqlName()} in ({sb});
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> GetDeleteAll() => $"DELETE FROM {TSelf.TableName};";


    public static SqlCommand<TRecord> GetNext<TRecord>( in RecordPair<TRecord> pair )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName}
                      WHERE ( id = IFNULL((SELECT MIN({nameof(IDateCreated.DateCreated).SqlName()}) FROM {TSelf.TableName} WHERE {nameof(IDateCreated.DateCreated).SqlName()} > '{pair.DateCreated}' LIMIT 2, 0) );
                      """;

        return sql;
    }
    public static SqlCommand<TRecord> GetNextID<TRecord>( in RecordPair<TRecord> pair )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        string sql = $"""
                      SELECT {nameof(IUniqueID.ID).SqlName()} FROM {TSelf.TableName}
                      WHERE ( id = IFNULL((SELECT MIN({nameof(IDateCreated.DateCreated).SqlName()}) FROM {TSelf.TableName} WHERE {nameof(IDateCreated.DateCreated).SqlName()} > '{pair.DateCreated}' LIMIT 2), 0) );
                      """;

        return sql;
    }


    public static SqlCommand<TSelf> GetCopy()
    {
        string sql = $"""
                      CREATE TEMP TABLE tmp_mytable (LIKE {TSelf.TableName} INCLUDING DEFAULTS);

                      COPY tmp_mytable (
                      {ColumnNames(1)}
                      )
                      FROM STDIN;

                      INSERT INTO {TSelf.TableName}
                      SELECT *
                      FROM tmp_mytable
                      RETURNING {nameof(IUniqueID.ID).SqlName()};      
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> GetInsert( TSelf record )
    {
        PostgresParameters parameters = record.ToDynamicParameters();

        string sql = $"""
                      INSERT INTO {TSelf.TableName} 
                      (
                      {ColumnNames(1)}
                      )
                      VALUES
                      (
                      {parameters.GetVariableNames(1)}
                      )
                      RETURNING {nameof(IUniqueID.ID).SqlName()};
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }
    public static SqlCommand<TSelf> GetInsert( IEnumerable<TSelf> records )
    {
        PostgresParameters parameters = PostgresParameters.Create(records);

        string sql = $"""
                      INSERT INTO {TSelf.TableName} 
                      (
                      {ColumnNames(1)}
                      )
                      VALUES
                      (
                      {parameters.GetVariableNames(1)}
                      )
                      RETURNING {nameof(IUniqueID.ID).SqlName()};
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }
    public static SqlCommand<TSelf> GetInsert( params ReadOnlySpan<TSelf> records )
    {
        PostgresParameters parameters = PostgresParameters.Create(records);

        string sql = $"""
                      INSERT INTO {TSelf.TableName} 
                      (
                      {ColumnNames(1)}
                      )
                      VALUES
                      (
                      {parameters.GetVariableNames(1)}
                      )
                      RETURNING {nameof(IUniqueID.ID).SqlName()};
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }


    public static SqlCommand<TSelf> GetUpdate( TSelf record ) => new($"""
                                                                      UPDATE {TSelf.TableName} 
                                                                      SET 
                                                                      {KeyValuePairs(1)} 
                                                                      WHERE {nameof(IUniqueID.ID).SqlName()} = @{nameof(IUniqueID.ID).SqlName()};
                                                                      """,
                                                                     record.ToDynamicParameters());
    public static SqlCommand<TSelf> GetTryInsert( TSelf record, bool matchAll, in PostgresParameters parameters )
    {
        PostgresParameters param = record.ToDynamicParameters().With(in parameters);

        string sql = $"""
                      IF NOT EXISTS (
                          SELECT * FROM {TSelf.TableName} 
                          WHERE 
                          {parameters.KeyValuePairs(matchAll, 2)}
                      )

                      BEGIN
                      INSERT INTO {TSelf.TableName}
                      (
                      {ColumnNames(1)}
                      )
                      VALUES
                      (
                      {parameters.GetVariableNames(1)}
                      ) 
                      RETURNING {nameof(IUniqueID.ID).SqlName()};
                      END

                      ELSE
                      BEGIN
                      SELECT {nameof(IUniqueID.ID).SqlName()} = '{Guid.Empty}';
                      END
                      """;

        return new SqlCommand<TSelf>(sql, param);
    }
    public static SqlCommand<TSelf> InsertOrUpdate( TSelf record, bool matchAll, in PostgresParameters parameters )
    {
        PostgresParameters param = record.ToDynamicParameters().With(parameters);

        string sql = $"""
                      IF NOT EXISTS (
                      SELECT * FROM {TSelf.TableName}
                      WHERE
                      {parameters.KeyValuePairs(matchAll, 2)}
                      )
                      BEGIN
                      INSERT INTO {TSelf.TableName}
                      (
                      {ColumnNames(1)}
                      ) 
                      VALUES 
                      (
                      {param.GetVariableNames(1)}
                      ) 
                      RETURNING {nameof(IUniqueID.ID).SqlName()};
                      END

                      ELSE
                      BEGIN
                      UPDATE {TSelf.TableName} 
                        SET 
                        {KeyValuePairs(2)}
                        WHERE {nameof(IUniqueID.ID).SqlName()} = @{nameof(IUniqueID.ID).SqlName()};
                        SELECT @{nameof(IUniqueID.ID).SqlName()};
                      END
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }


    private static Guid GetValue<TRecord>( RecordID<TRecord> id )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord> => id.Value;


    public          bool Equals( SqlCommand<TSelf> other ) => string.Equals(SQL, other.SQL, StringComparison.InvariantCultureIgnoreCase) && Parameters.Equals(other.Parameters) && CommandType == other.CommandType && Flags == other.Flags;
    public override bool Equals( object?           obj )   => obj is SqlCommand<TSelf> other                                             && Equals(other);
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(SQL, StringComparer.InvariantCultureIgnoreCase);
        hashCode.Add(Parameters);
        hashCode.Add(CommandType);
        hashCode.Add((int)Flags);
        return hashCode.ToHashCode();
    }


    public static bool operator ==( SqlCommand<TSelf> left, SqlCommand<TSelf> right ) => left.Equals(right);
    public static bool operator !=( SqlCommand<TSelf> left, SqlCommand<TSelf> right ) => !left.Equals(right);
}
