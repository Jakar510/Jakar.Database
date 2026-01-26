// Jakar.Extensions :: Jakar.Database
// 10/19/2025  10:38

using ZLinq.Linq;



namespace Jakar.Database;


public static class PostgresParams
{
    public static readonly ConcurrentDictionary<string, string> NameSnakeCaseCache = new(StringComparer.InvariantCultureIgnoreCase)
                                                                                     {
                                                                                         [nameof(MimeType)]            = "mime_types",
                                                                                         [nameof(SupportedLanguage)]   = "languages",
                                                                                         [nameof(ProgrammingLanguage)] = "programming_languages",
                                                                                         [nameof(SubscriptionStatus)]  = "subscription_status",
                                                                                         [nameof(DeviceCategory)]      = "device_categories",
                                                                                         [nameof(DevicePlatform)]      = "device_platforms",
                                                                                         [nameof(DeviceTypes)]         = "device_types",
                                                                                         [nameof(DistanceUnit)]        = "distance_units",
                                                                                         [nameof(Status)]              = "statuses",
                                                                                         [nameof(NpgsqlDbType)]        = "db_types"
                                                                                     };
    public static  string SqlColumnName( this string name ) => NameSnakeCaseCache.GetOrAdd(name, ToSnakeCase);
    private static string ToSnakeCase( string        x )    => x.ToSnakeCase();
}



[DefaultMember(nameof(Empty))]
public readonly struct PostgresParameters( FrozenDictionary<string, ColumnMetaData> dictionary ) : IEquatable<PostgresParameters>
{
    public static readonly PostgresParameters    Empty    = new(FrozenDictionary<string, ColumnMetaData>.Empty);
    private readonly       List<NpgsqlParameter> __buffer = new(Math.Max(dictionary.Count, DEFAULT_CAPACITY));


    public int                           Count    => __buffer.Count;
    public int                           Capacity => __buffer.Capacity;
    public ReadOnlySpan<NpgsqlParameter> Values   => __buffer.AsSpan();
    public ValueEnumerable<Select<FromSpan<NpgsqlParameter>, NpgsqlParameter, string>, string> ParameterNames
    {
        [Pure] get => Values.AsValueEnumerable()
                            .Select(static x => x.ParameterName);
    }
    public StringBuilder Parameters
    {
        get
        {
            StringBuilder sb = new();

            int count = Count;
            int index = 0;

            foreach ( string pair in ParameterNames.Select(GetColumnName) )
            {
                if ( index++ < count - 1 )
                {
                    sb.Append(pair)
                      .Append(", ");
                }
                else { sb.Append(pair); }
            }

            return sb;
        }
    }
    public StringBuilder ColumnNames
    {
        get
        {
            const string  SPACER = ",\n      ";
            int           length = dictionary.Values.Sum(static x => x.ColumnName.Length) + ( dictionary.Count - 1 ) * SPACER.Length;
            StringBuilder sb     = new(length);
            int           count  = Count;
            int           index  = 0;

            foreach ( string pair in ParameterNames.Select(GetColumnName) )
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
    public StringBuilder VariableNames
    {
        get
        {
            const string  SPACER = ",\n      ";
            int           length = dictionary.Values.Sum(static x => x.VariableName.Length) + ( dictionary.Count - 1 ) * SPACER.Length;
            StringBuilder sb     = new(length);
            int           count  = Count;
            int           index  = 0;

            foreach ( string pair in ParameterNames.Select(GetVariableName) )
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


    [Obsolete("For serialization only", true)] public PostgresParameters() : this(FrozenDictionary<string, ColumnMetaData>.Empty) => throw new NotSupportedException();

    public static PostgresParameters Create<TSelf>()
        where TSelf : ITableRecord<TSelf> => new(TSelf.PropertyMetaData);


    public PostgresParameters With( in PostgresParameters parameters )
    {
        __buffer.AddRange(parameters.Values);
        return this;
    }


    /*
    public PostgresParameters Add<T>( T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        PrecisionInfo precision = PrecisionInfo.Default;
        PostgresType  pgType    = PostgresTypes.GetType<T>(out bool isNullable, out bool isEnum, ref precision);

        NpgsqlParameter parameter = new NpgsqlParameter(parameterName, value)
                                    {
                                        Direction     = direction,
                                        SourceVersion = sourceVersion,
                                        NpgsqlDbType  = pgType.ToNpgsqlDbType(),
                                        IsNullable    = isNullable
                                    };

        return Add(parameter);
    }
    */
    public PostgresParameters Add<T>( string propertyName, T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        ColumnMetaData meta = dictionary[propertyName];
        return Add(meta, value, parameterName, direction, sourceVersion);
    }
    public PostgresParameters Add<T>( ColumnMetaData meta, T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
    {
        NpgsqlParameter parameter = new(parameterName, meta.DbType.ToNpgsqlDbType())
                                    {
                                        SourceColumn  = meta.ColumnName,
                                        IsNullable    = meta.IsNullable,
                                        SourceVersion = sourceVersion,
                                        Direction     = direction,
                                        Value         = value
                                    };

        return Add(parameter);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public PostgresParameters Add( NpgsqlParameter parameter )
    {
        __buffer.Add(parameter);
        return this;
    }


    public StringBuilder KeyValuePairs( bool matchAll )
    {
        string        match  = matchAll.GetAndOr();
        int           count  = Count;
        int           length = dictionary.Values.Sum(static x => x.KeyValuePair.Length) + ( dictionary.Count - 1 ) * match.Length;
        StringBuilder sb     = new(length);
        int           index  = 0;

        foreach ( string pair in ParameterNames.Select(GetKeyValuePair) )
        {
            if ( index++ < count - 1 )
            {
                sb.Append(pair)
                  .Append(match);
            }
            else { sb.Append(pair); }
        }

        return sb;
    }


    private string GetColumnName( string   propertyName ) => dictionary[propertyName].ColumnName;
    private string GetVariableName( string propertyName ) => dictionary[propertyName].VariableName;
    private string GetKeyValuePair( string propertyName ) => dictionary[propertyName].KeyValuePair;


    public override int GetHashCode() => HashCode.Combine(__buffer);
    public ulong GetHash64()
    {
        ReadOnlySpan<string> names = ParameterNames.ToArray();
        return Hashes.Hash(in names);
    }
    public UInt128 GetHash128()
    {
        ReadOnlySpan<string> names = ParameterNames.ToArray();
        return Hashes.Hash128(in names);
    }


    public          bool Equals( PostgresParameters      other )                          => __buffer.Equals(other.__buffer);
    public override bool Equals( object?                 obj )                            => obj is PostgresParameters other && Equals(other);
    public static   bool operator ==( PostgresParameters left, PostgresParameters right ) => left.Equals(right);
    public static   bool operator !=( PostgresParameters left, PostgresParameters right ) => !left.Equals(right);
}



public readonly struct SqlCommand<TSelf>( string sql, in PostgresParameters parameters = default, CommandType? commandType = null, CommandFlags flags = CommandFlags.None ) : IEquatable<SqlCommand<TSelf>>
    where TSelf : ITableRecord<TSelf>
{
    public readonly                 string             SQL         = sql;
    public readonly                 PostgresParameters Parameters  = parameters;
    public readonly                 CommandType?       CommandType = commandType;
    public readonly                 CommandFlags       Flags       = flags;
    public static implicit operator SqlCommand<TSelf>( string sql ) => new(sql, in PostgresParameters.Empty);


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


    public static IEnumerable<string> GetColumnNames   => TSelf.PropertyMetaData.Values.Select(ColumnMetaData.GetColumnName);
    public static IEnumerable<string> GetKeyValuePairs => TSelf.PropertyMetaData.Values.Select(ColumnMetaData.GetKeyValuePair);


    public static StringBuilder ColumnNames
    {
        get
        {
            FrozenDictionary<string, ColumnMetaData> data = TSelf.PropertyMetaData;

            int length = data.Values.AsValueEnumerable()
                             .Sum(static x => x.ColumnName.Length) +
                         ( data.Count - 1 ) * SPACER.Length;

            StringBuilder sb    = new(length);
            int           count = data.Count;
            int           index = 0;

            foreach ( string pair in data.Values.AsValueEnumerable()
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
            FrozenDictionary<string, ColumnMetaData> data = TSelf.PropertyMetaData;

            int length = data.Values.AsValueEnumerable()
                             .Sum(static x => x.ColumnName.Length) +
                         ( data.Count - 1 ) * SPACER.Length;

            StringBuilder sb    = new(length);
            int           count = data.Count;
            int           index = 0;

            foreach ( string pair in data.Values.AsValueEnumerable()
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

        return new SqlCommand<TSelf>(sql);
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

        return new SqlCommand<TSelf>(sql);
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

        return new SqlCommand<TSelf>(sql);
    }
    public static SqlCommand<TSelf> WherePaged( int start, int count )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName} 
                      OFFSET {start}
                      LIMIT {count};
                      """;

        return new SqlCommand<TSelf>(sql);
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

        return new SqlCommand<TSelf>(string.Format(sql, id.Value.ToString()));
    }
    public static SqlCommand<TSelf> Get( IEnumerable<RecordID<TSelf>> ids )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName}
                      WHERE {ID} in ({string.Join(',', ids.Select(GetValue))});
                      """;

        return new SqlCommand<TSelf>(sql);
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

        return new SqlCommand<TSelf>(sql);
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

        return new SqlCommand<TSelf>(sql);
    }
    public static SqlCommand<TSelf> GetDeleteAll() => $"DELETE FROM {TSelf.TableName};";


    public static SqlCommand<TSelf> GetNext( in RecordPair<TSelf> pair )
    {
        string sql = $"""
                      SELECT * FROM {TSelf.TableName}
                      WHERE ( id = IFNULL((SELECT MIN({DATE_CREATED}) FROM {TSelf.TableName} WHERE {DATE_CREATED} > '{pair.DateCreated}' LIMIT 2, 0) );
                      """;

        return new SqlCommand<TSelf>(string.Format(sql));
    }
    public static SqlCommand<TSelf> GetNextID( in RecordPair<TSelf> pair )
    {
        string sql = $"""
                      SELECT {ID} FROM {TSelf.TableName}
                      WHERE ( id = IFNULL((SELECT MIN({DATE_CREATED}) FROM {TSelf.TableName} WHERE {DATE_CREATED} > '{pair.DateCreated}' LIMIT 2), 0) );
                      """;

        return new SqlCommand<TSelf>(sql);
    }


    public static SqlCommand<TSelf> GetCopy()
    {
        string sql = $"""
                      COPY INTO {TSelf.TableName} 
                      (
                        {ColumnNames}
                      ) 
                      FROM STDIN;
                      """;

        return new SqlCommand<TSelf>(sql);
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
