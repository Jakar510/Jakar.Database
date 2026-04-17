// Jakar.Extensions :: Jakar.Database
// 10/19/2025  10:38

using Microsoft.Data.SqlClient;



namespace Jakar.Database;


public readonly struct SqlCommand : IEquatable<SqlCommand>
{
    public const    string             SPACER = ",\n      ";
    public readonly string             SQL;
    public readonly CommandParameters? Parameters;
    public readonly CommandType        CommandType;

    private SqlCommand( ref readonly string sql, ref readonly CommandParameters? parameters, ref readonly CommandType commandType )
    {
        SQL         = sql;
        Parameters  = parameters;
        CommandType = commandType;
    }


    public static implicit operator SqlCommand( string             sql ) => Create(sql);
    public static implicit operator string( SqlCommand             sql ) => sql.SQL;
    public static implicit operator CommandParameters?( SqlCommand sql ) => sql.Parameters;


    [Pure] [MustDisposeResource] public DbCommand ToCommand( DbConnectionContext context )
    {
        if ( context.TryAs(out NpgsqlConnection? postgresConnection, out NpgsqlTransaction? postgresTransaction) ) { return ToCommand(postgresConnection, postgresTransaction); }

        if ( context.TryAs(out SqlConnection? sqlConnection, out SqlTransaction? sqlTransaction) ) { return ToCommand(sqlConnection, sqlTransaction); }

        throw new InvalidOperationException("Unsupported connection type");
    }
    [Pure] [MustDisposeResource] public Microsoft.Data.SqlClient.SqlCommand ToCommand( SqlConnection connection, SqlTransaction? transaction = null )
    {
        ArgumentNullException.ThrowIfNull(connection);

        Microsoft.Data.SqlClient.SqlCommand command = new()
                                                      {
                                                          Connection     = connection,
                                                          CommandText    = SQL,
                                                          CommandType    = CommandType,
                                                          Transaction    = transaction,
                                                          CommandTimeout = 30
                                                      };

        if ( Parameters is null ) { return command; }

        foreach ( ref readonly SqlParameter parameter in Parameters.Value.Parameters ) { command.Parameters.Add(parameter.ToSqlParameter()); }

        return command;
    }
    [Pure] [MustDisposeResource] public DbCommand ToCommand( NpgsqlConnection connection, NpgsqlTransaction? transaction = null )
    {
        ArgumentNullException.ThrowIfNull(connection);

        NpgsqlCommand command = new()
                                {
                                    Connection               = connection,
                                    CommandText              = SQL,
                                    CommandType              = CommandType,
                                    Transaction              = transaction,
                                    CommandTimeout           = 30,
                                    AllResultTypesAreUnknown = false
                                };

        if ( Parameters is null ) { return command; }

        foreach ( ref readonly SqlParameter parameter in Parameters.Value.Parameters ) { command.Parameters.Add(parameter.ToPostgresParameter()); }

        return command;
    }


    public override string ToString() => DbSqlException.GetMessage(nameof(SqlCommand), SQL, Parameters);


    public static SqlCommand Create( string sql, in CommandParameters? parameters = null, in CommandType commandType = CommandType.Text ) => new(in sql, in parameters, in commandType);


    public static SqlCommand Create<TSelf>( string sql, params ReadOnlySpan<SqlParameter> parameters )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Create(sql, CommandParameters.Create<TSelf>(parameters));


    public static SqlCommand Parse<TSelf>( ref SqlInterpolatedStringHandler<TSelf> handler, in CommandType commandType = CommandType.Text )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => handler.ToSqlCommand(commandType);


    public static SqlCommand GetRandom<TSelf>( int count = 1 )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               ORDER BY RANDOM()
                                                                               LIMIT {count};
                                                                               """);
    public static SqlCommand GetRandom<TSelf>( UserRecord user, int count = 1 )
        where TSelf : OwnedTableRecord<TSelf>, ITableRecord<TSelf> => GetRandom<TSelf>(user.ID, count);
    public static SqlCommand GetRandom<TSelf>( in RecordID<UserRecord> userID, int count = 1 )
        where TSelf : OwnedTableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                                    SELECT * FROM {TSelf.TableName}
                                                                                    WHERE 
                                                                                         {nameof(IUserID.UserID)} = {userID.Value}
                                                                                    ORDER BY RANDOM()
                                                                                    LIMIT {count};
                                                                                    """);


    public static SqlCommand WherePaged<TSelf>( in CommandParameters parameters, int start, int count, string separator = "AND" )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                                 SELECT * FROM {TSelf.TableName} 
                                                                                 WHERE
                                                                                 {parameters.KeyValuePairs(1, separator)}
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
    public static SqlCommand Where<TSelf>( in CommandParameters parameters, string separator = "AND" )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName} 
                                                                               WHERE
                                                                               {parameters.KeyValuePairs(1, separator)}
                                                                               """);

    [OverloadResolutionPriority(3)] public static SqlCommand Get<TSelf>( in RecordID<TSelf> id )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              SELECT * FROM {TSelf.TableName}
                                                                              WHERE 
                                                                              {nameof(IUniqueID.ID)} = {id};
                                                                              """);
    [OverloadResolutionPriority(1)] public static SqlCommand Get<TSelf>( IEnumerable<RecordID<TSelf>> ids )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              SELECT * FROM {TSelf.TableName}
                                                                              WHERE 
                                                                                    {nameof(IUniqueID.ID)} in (
                                                                              {ids:2}
                                                                                    );
                                                                              """);
    [OverloadResolutionPriority(2)] public static SqlCommand Get<TSelf>( params ReadOnlySpan<RecordID<TSelf>> ids )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              SELECT * FROM {TSelf.TableName}
                                                                              WHERE 
                                                                                    {nameof(IUniqueID.ID)} in (
                                                                              {ids:2}
                                                                                    );
                                                                              """);
    [OverloadResolutionPriority(0)] public static SqlCommand Get<TSelf>( in CommandParameters parameters, string separator = "AND" )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               WHERE 
                                                                               {parameters.KeyValuePairs(1, separator)};
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
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              SELECT {nameof(IUniqueID.ID)}, {nameof(IDateCreated.DateCreated)} FROM {TSelf.TableName}
                                                                              ORDER BY {nameof(IDateCreated.DateCreated)} DESC;
                                                                              """);
    public static SqlCommand GetExists<TSelf>( in CommandParameters parameters )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                               EXISTS( 
                                                                               SELECT * FROM {TSelf.TableName}
                                                                               WHERE 
                                                                               {parameters.KeyValuePairs(1)};
                                                                               """);


    [OverloadResolutionPriority(0)] public static SqlCommand GetDelete<TSelf>( in CommandParameters parameters, string separator = "AND" )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              DELETE FROM {TSelf.TableName} 
                                                                              WHERE 
                                                                              {nameof(IUniqueID.ID)} in (
                                                                              {parameters.KeyValuePairs(2, separator)}
                                                                                );
                                                                              """);
    [OverloadResolutionPriority(3)] public static SqlCommand GetDelete<TSelf>( in RecordID<TSelf> id )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              DELETE FROM {TSelf.TableName}
                                                                              WHERE {nameof(IUniqueID.ID)} = {id.Value};
                                                                              """);
    [OverloadResolutionPriority(1)] public static SqlCommand GetDelete<TSelf>( IEnumerable<RecordID<TSelf>> ids )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              DELETE FROM {TSelf.TableName} 
                                                                              WHERE 
                                                                                    {nameof(IUniqueID.ID)} in (
                                                                              {ids:2}
                                                                                    );
                                                                              """);
    [OverloadResolutionPriority(2)] public static SqlCommand GetDelete<TSelf>( params ReadOnlySpan<RecordID<TSelf>> ids )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              DELETE FROM {TSelf.TableName} 
                                                                              WHERE 
                                                                                    {nameof(IUniqueID.ID)} in (
                                                                              {ids:2}
                                                                                    );
                                                                              """);
    public static SqlCommand GetDeleteAll<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"DELETE FROM {TSelf.TableName};");


    public static SqlCommand GetNext<TSelf>( in RecordPair<TSelf> pair )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              SELECT * FROM {TSelf.TableName}
                                                                              WHERE ( 
                                                                                    id = IFNULL(
                                                                                            (
                                                                                            SELECT MIN(
                                                                                                {nameof(IDateCreated.DateCreated)}) FROM {TSelf.TableName} 
                                                                                                    WHERE {nameof(IDateCreated.DateCreated)} > {pair.DateCreated}
                                                                                                    )
                                                                                                LIMIT 2
                                                                                            ),
                                                                                            0
                                                                                        )
                                                                                );
                                                                              """);
    public static SqlCommand GetNextID<TSelf>( in RecordPair<TSelf> pair )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              SELECT {nameof(IUniqueID.ID)} FROM {TSelf.TableName}
                                                                              WHERE (
                                                                                  id = IFNULL(
                                                                                          (
                                                                                          SELECT MIN({nameof(IDateCreated.DateCreated)}) FROM {TSelf.TableName}
                                                                                              WHERE {nameof(IDateCreated.DateCreated)} > {pair.DateCreated}
                                                                                          LIMIT 2
                                                                                          ),
                                                                                          0
                                                                                      )
                                                                                );
                                                                              """);


    public static SqlCommand GetCopy<TSelf>()
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Parse<TSelf>($"""
                                                                              CREATE TEMP TABLE temp_table (LIKE {TSelf.TableName} INCLUDING DEFAULTS);

                                                                              COPY temp_table (
                                                                              {TSelf.MetaData.ColumnNames(1)}
                                                                              )
                                                                              FROM STDIN;

                                                                              INSERT INTO {TSelf.TableName}
                                                                                  SELECT * FROM temp_table
                                                                                  RETURNING {nameof(IUniqueID.ID)};      
                                                                              """);
    [OverloadResolutionPriority(3)] public static SqlCommand GetInsert<TSelf>( TSelf record )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = CommandParameters.Create(record);

        return Parse<TSelf>($"""
                             INSERT INTO {TSelf.TableName} 
                                 (
                             {TSelf.MetaData.ColumnNames(2)}
                                 )
                             VALUES
                                 (
                             {parameters.VariableNames(2)}
                                 )

                             RETURNING {nameof(IUniqueID.ID)};
                             """);
    }
    [OverloadResolutionPriority(1)] public static SqlCommand GetInsert<TSelf>( IEnumerable<TSelf> records )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = CommandParameters.Create(records.AsValueEnumerable());

        return Parse<TSelf>($"""
                             INSERT INTO {TSelf.TableName} 
                                 (
                             {TSelf.MetaData.ColumnNames(2)}                 
                                 )
                             VALUES
                                 (
                             {parameters.VariableNames(2)}
                                 )

                             RETURNING {nameof(IUniqueID.ID)};
                             """);
    }
    [OverloadResolutionPriority(2)] public static SqlCommand GetInsert<TSelf>( params ReadOnlySpan<TSelf> records )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = CommandParameters.Create(records);

        return Parse<TSelf>($"""
                             INSERT INTO {TSelf.TableName}
                                 (
                             {TSelf.MetaData.ColumnNames(2)}
                                 )
                             VALUES
                                 (
                             {parameters.VariableNames(2)}
                                 )

                             RETURNING {nameof(IUniqueID.ID)};
                             """);
    }


    public static SqlCommand GetTryInsert<TSelf>( TSelf record, in CommandParameters parameters, string separator = "AND" )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters recordParameters = CommandParameters.Create(record);

        return Parse<TSelf>($"""
                             IF NOT EXISTS (
                             SELECT * FROM {TSelf.TableName} 
                                 WHERE 
                                    {nameof(IUniqueID.ID)} = @{record.ID}
                                 OR 
                                     (
                                     {parameters.KeyValuePairs(3, separator)}
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


    public static SqlCommand GetUpdate<TSelf>( TSelf record )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = record.ToDynamicParameters();

        return Parse<TSelf>($"""
                             UPDATE {TSelf.TableName} 
                             SET 
                             {parameters.KeyValuePairs(1, "AND")}
                             WHERE 
                                {nameof(IUniqueID.ID)} = @{nameof(IUniqueID.ID)};
                             """);
    }
    public static SqlCommand InsertOrUpdate<TSelf>( TSelf record, in CommandParameters parameters, string separator = "AND" )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters recordParameters = CommandParameters.Create(record);

        return Parse<TSelf>($"""
                             IF NOT EXISTS 
                             (
                             SELECT * FROM {TSelf.TableName}
                             WHERE
                                    {nameof(IUniqueID.ID)} = @{record.ID}
                                 OR 
                                     (
                                     {parameters.KeyValuePairs(4, separator)}
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
                                 {recordParameters.KeyValuePairs(3, null)}
                                 WHERE 
                                    {nameof(IUniqueID.ID)} = @{nameof(IUniqueID.ID)};

                             SELECT @{nameof(IUniqueID.ID)};
                             END
                             """);
    }


    public          bool Equals( SqlCommand other ) => string.Equals(SQL, other.SQL, StringComparison.InvariantCultureIgnoreCase) && Parameters.Equals(other.Parameters) && CommandType == other.CommandType;
    public override bool Equals( object?    obj )   => obj is SqlCommand other                                                    && Equals(other);
    public override int GetHashCode()
    {
        HashCode hashCode = new();
        hashCode.Add(SQL, StringComparer.InvariantCultureIgnoreCase);
        hashCode.Add(Parameters);
        hashCode.Add(CommandType);
        return hashCode.ToHashCode();
    }


    public static bool operator ==( SqlCommand left, SqlCommand right ) => left.Equals(right);
    public static bool operator !=( SqlCommand left, SqlCommand right ) => !left.Equals(right);
}
