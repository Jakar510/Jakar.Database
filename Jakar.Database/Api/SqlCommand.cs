// Jakar.Extensions :: Jakar.Database
// 10/19/2025  10:38

namespace Jakar.Database;


public readonly struct SqlCommand<TSelf>( string sql, in PostgresParameters parameters, CommandType? commandType = null, CommandFlags flags = CommandFlags.None ) : IEquatable<SqlCommand<TSelf>>
    where TSelf : class, ITableRecord<TSelf>
{
    public readonly                 string             SQL         = sql;
    public readonly                 PostgresParameters Parameters  = parameters;
    public readonly                 CommandType?       CommandType = commandType;
    public readonly                 CommandFlags       Flags       = flags;
    public static implicit operator SqlCommand<TSelf>( string sql ) => new(sql, PostgresParameters.Create<TSelf>());


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

        command.Parameters.Add(Parameters.Values);
        return command;
    }
    public async ValueTask ExecuteNonQueryAsync( NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken token )
    {
        await using NpgsqlCommand command = ToCommand(connection, transaction);
        await command.ExecuteNonQueryAsync(token);
    }


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


    public static implicit operator string( SqlCommand<TSelf> sql ) => sql.SQL;


    public const string SPACER       = ",\n      ";
    public const string CREATED_BY   = "created_by";
    public const string DATE_CREATED = "date_created";
    public const string ID           = "id";


    public static IEnumerable<string> GetColumnNames   => TSelf.PropertyMetaData.Properties.Values.Select(ColumnMetaData.GetColumnName);
    public static IEnumerable<string> GetKeyValuePairs => TSelf.PropertyMetaData.Properties.Values.Select(ColumnMetaData.GetKeyValuePair);


    public static StringBuilder ColumnNames
    {
        get
        {
            TableMetaData<TSelf> data = TSelf.PropertyMetaData;

            int length = data.Properties.Values.AsValueEnumerable()
                             .Sum(static x => x.ColumnName.Length) +
                         ( data.Count - 1 ) * SPACER.Length;

            StringBuilder sb    = new(length);
            int           count = data.Count;
            int           index = 0;

            foreach ( string pair in data.Properties.Values.AsValueEnumerable()
                                         .Select(ColumnMetaData.GetColumnName) )
            {
                if ( index++ < count - 1 )
                {
                    sb.Append(pair)
                      .Append(SPACER);
                }
                else { sb.Append(pair); }
            }

            return sb;
        }
    }
    public static StringBuilder KeyValuePairs
    {
        get
        {
            TableMetaData<TSelf> data = TSelf.PropertyMetaData;

            int length = data.Properties.Values.AsValueEnumerable()
                             .Sum(static x => x.ColumnName.Length) +
                         ( data.Count - 1 ) * SPACER.Length;

            StringBuilder sb    = new(length);
            int           count = data.Count;
            int           index = 0;

            foreach ( string pair in data.Properties.Values.AsValueEnumerable()
                                         .Select(ColumnMetaData.GetKeyValuePair) )
            {
                if ( index++ < count - 1 )
                {
                    sb.Append(pair)
                      .Append(SPACER);
                }
                else { sb.Append(pair); }
            }

            return sb;
        }
    }


    public static SqlCommand<TSelf> Create( string sql, in PostgresParameters parameters ) => new(sql, in parameters);


    public static SqlCommand<TSelf> GetRandom() => $"""
                                                    SELECT * FROM {TSelf.TableName} 
                                                    ORDER BY RANDOM()
                                                    LIMIT 1;
                                                    """;
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
    public static SqlCommand<TSelf> GetRandom( in RecordID<UserRecord> createdBy, int count )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName} 
                      WHERE {CREATED_BY} = '{createdBy.Value}'
                      ORDER BY RANDOM() 
                      LIMIT {count};
                      """;

        return sql;
    }


    public static SqlCommand<TSelf> WherePaged( bool matchAll, in PostgresParameters parameters, int start, int count )
    {
        string sql = $"""
                        SELECT * FROM {TSelf.TableName}
                        {parameters.KeyValuePairs(matchAll)}
                        OFFSET {start}
                        LIMIT {count}
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }
    public static SqlCommand<TSelf> WherePaged( in RecordID<UserRecord> createdBy, int start, int count )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName} 
                      WHERE {CREATED_BY} = '{createdBy.Value}'
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


    public static SqlCommand<TSelf> Get( in RecordID<TSelf> id )
    {
        string sql = $$"""
                       SELECT * FROM {{TSelf.TableName}} 
                       WHERE {{ID}} = '{0}';
                       """;

        return string.Format(sql, id.Value.ToString());
    }
    public static SqlCommand<TSelf> Get( IEnumerable<RecordID<TSelf>> ids )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName}
                      WHERE {ID} in ({string.Join(',', ids.Select(GetValue))});
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> Get( bool matchAll, in PostgresParameters parameters )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName}
                      WHERE {parameters.KeyValuePairs(matchAll)};
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }
    public static SqlCommand<TSelf> GetAll() => $"SELECT * FROM {TSelf.TableName};";
    public static SqlCommand<TSelf> GetFirst() => $"""
                                                   SELECT * FROM {TSelf.TableName} 
                                                   ORDER BY {DATE_CREATED} ASC 
                                                   LIMIT 1;
                                                   """;
    public static SqlCommand<TSelf> GetLast() => $"""
                                                  SELECT * FROM {TSelf.TableName} 
                                                  ORDER BY {DATE_CREATED} DESC 
                                                  LIMIT 1
                                                  """;


    public static SqlCommand<TSelf> GetCount() => $"SELECT COUNT(*) FROM {TSelf.TableName};";
    public static SqlCommand<TSelf> GetSortedID() => $"""
                                                      SELECT {ID}, {DATE_CREATED} FROM {TSelf.TableName} 
                                                      ORDER BY {DATE_CREATED} DESC;
                                                      """;
    public static SqlCommand<TSelf> GetExists( bool matchAll, in PostgresParameters parameters )
    {
        string sql = $"""
                      EXISTS( 
                      SELECT * FROM {TSelf.TableName}
                      WHERE {parameters.KeyValuePairs(matchAll)};
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }


    public static SqlCommand<TSelf> GetDelete( bool matchAll, in PostgresParameters parameters )
    {
        string sql = $"""
                      DELETE FROM {TSelf.TableName} 
                      WHERE {ID} in ({parameters.KeyValuePairs(matchAll)});
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }
    public static SqlCommand<TSelf> GetDeleteID( in RecordID<TSelf> id )
    {
        string sql = $"""
                      DELETE FROM {TSelf.TableName}
                      WHERE {ID} = '{id.Value}';
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> GetDelete( IEnumerable<RecordID<TSelf>> ids )
    {
        // ReSharper disable PossibleMultipleEnumeration
        StringBuilder sb = new(256 + 30 * ids.Count());
        sb.AppendJoin(',', ids.Select(GetValue));

        // ReSharper restore PossibleMultipleEnumeration

        string sql = $"""
                      DELETE FROM {TSelf.TableName} 
                      WHERE {ID} in ({sb});
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> GetDeleteAll() => $"DELETE FROM {TSelf.TableName};";


    public static SqlCommand<TSelf> GetNext( in RecordPair<TSelf> pair )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName}
                      WHERE ( id = IFNULL((SELECT MIN({DATE_CREATED}) FROM {TSelf.TableName} WHERE {DATE_CREATED} > '{pair.DateCreated}' LIMIT 2, 0) );
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> GetNextID( in RecordPair<TSelf> pair )
    {
        string sql = $"""
                      SELECT {ID} FROM {TSelf.TableName}
                      WHERE ( id = IFNULL((SELECT MIN({DATE_CREATED}) FROM {TSelf.TableName} WHERE {DATE_CREATED} > '{pair.DateCreated}' LIMIT 2), 0) );
                      """;

        return sql;
    }


    public static SqlCommand<TSelf> GetCopy()
    {
        string sql = $"""
                      CREATE TEMP TABLE tmp_mytable (LIKE {TSelf.TableName} INCLUDING DEFAULTS);

                      COPY tmp_mytable ({ColumnNames})
                      FROM STDIN;

                      INSERT INTO {TSelf.TableName}
                      SELECT *
                      FROM tmp_mytable
                      RETURNING *;
                      """;

        return sql;
    }
    public static SqlCommand<TSelf> GetInsert( TSelf record )
    {
        PostgresParameters parameters = record.ToDynamicParameters();

        string sql = $"""
                      INSERT INTO {TSelf.TableName} 
                      (
                        {parameters.ColumnNames}
                      )
                      VALUES
                      (
                        {parameters.VariableNames}
                      ) 
                      RETURNING {ID};
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }
    public static SqlCommand<TSelf> GetUpdate( TSelf record ) => new($"""
                                                                      UPDATE {TSelf.TableName} 
                                                                      SET {KeyValuePairs} 
                                                                      WHERE {ID} = @{ID};
                                                                      """,
                                                                     record.ToDynamicParameters());
    public static SqlCommand<TSelf> GetTryInsert( TSelf record, bool matchAll, in PostgresParameters parameters )
    {
        PostgresParameters param = record.ToDynamicParameters()
                                         .With(in parameters);

        string sql = $"""
                      IF NOT EXISTS(SELECT * FROM {TSelf.TableName} WHERE {parameters.KeyValuePairs(matchAll)})
                      BEGIN
                      INSERT INTO {TSelf.TableName}
                      (
                        {parameters.ColumnNames}
                      )
                      VALUES
                      (
                        {parameters.VariableNames}
                      ) 
                      RETURNING {ID};
                      END

                      ELSE
                      BEGIN
                      SELECT {ID} = '{Guid.Empty}';
                      END
                      """;

        return new SqlCommand<TSelf>(sql, param);
    }
    public static SqlCommand<TSelf> InsertOrUpdate( TSelf record, bool matchAll, in PostgresParameters parameters )
    {
        PostgresParameters param = record.ToDynamicParameters()
                                         .With(in parameters);

        string sql = $"""
                      IF NOT EXISTS(SELECT * FROM {TSelf.TableName} WHERE {parameters.KeyValuePairs(matchAll)})
                      BEGIN
                      INSERT INTO {TSelf.TableName}
                      (
                      {param.ColumnNames}
                      ) 
                      VALUES 
                      (
                      {param.VariableNames}
                      ) 
                      RETURNING {ID};
                      END

                      ELSE
                      BEGIN
                          UPDATE {TSelf.TableName} 
                          SET {KeyValuePairs}
                          WHERE {ID} = @{ID};
                      SELECT @{ID};
                      END
                      """;

        return new SqlCommand<TSelf>(sql, in parameters);
    }


    private static Guid GetValue( RecordID<TSelf> id ) => id.Value;
}
