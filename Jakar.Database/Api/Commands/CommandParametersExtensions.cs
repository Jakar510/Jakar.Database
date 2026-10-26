// Jakar.Database :: Jakar.Database
// 10/25/2026

using ZLinq.Linq;



namespace Jakar.Database;


/// <summary> The record-aware and driver-specific members of Jakar.SqlBuilder's <see cref="CommandParameters"/> and <see cref="CommandParameter"/>. </summary>
public static class CommandParametersExtensions
{
    extension( CommandParameters )
    {
        public static CommandParameters Create<TSelf>()
            where TSelf : class, ITableRecord<TSelf> => CommandParameters.Create(TSelf.MetaData);
        public static CommandParameters Create<TSelf>( int capacity )
            where TSelf : class, ITableRecord<TSelf> => CommandParameters.Create(TSelf.MetaData, capacity);
        public static CommandParameters Create<TSelf>( TSelf self )
            where TSelf : class, ITableRecord<TSelf> => self.ToDynamicParameters().Validate<TSelf>();
        public static CommandParameters Create<TSelf>( IEnumerable<TSelf> records )
            where TSelf : class, ITableRecord<TSelf>
        {
            CommandParameters parameters = CommandParameters.Create<TSelf>();
            foreach ( TSelf record in records ) { parameters.AddGroup(record); }

            return parameters;
        }
        public static CommandParameters Create<TEnumerator, TSelf>( ValueEnumerable<TEnumerator, TSelf> records )
            where TEnumerator : struct, IValueEnumerator<TSelf>, allows ref struct
            where TSelf : class, ITableRecord<TSelf>
        {
            CommandParameters parameters = CommandParameters.Create<TSelf>();
            foreach ( TSelf record in records ) { parameters.AddGroup(record); }

            return parameters;
        }
        public static CommandParameters Create<TSelf>( params ReadOnlySpan<TSelf> records )
            where TSelf : class, ITableRecord<TSelf>
        {
            CommandParameters parameters = CommandParameters.Create<TSelf>(records.Length);
            foreach ( TSelf record in records ) { parameters.AddGroup(record); }

            return parameters;
        }
        public static CommandParameters Create<TSelf>( params ReadOnlySpan<CommandParameter> parameters )
            where TSelf : class, ITableRecord<TSelf> => CommandParameters.Create<TSelf>(parameters.Length).AddRange(parameters);
    }



    extension( CommandParameters self )
    {
        public CommandParameters AddGroup<TSelf>( TSelf other )
            where TSelf : class, ITableRecord<TSelf>
        {
            CommandParameters parameters = CommandParameters.Create(other);
            return self.AddGroup(in parameters);
        }


        public CommandParameters Validate<TSelf>()
            where TSelf : class, ITableRecord<TSelf>
        {
            if ( !string.Equals(TSelf.TableName.Value, TSelf.TableName.Value.ToSnakeCase()) ) { throw new InvalidOperationException($"{typeof(TSelf).Name}: {nameof(TSelf.TableName)} is not snake_case: '{TSelf.TableName}'"); }

            if ( self.Count == TSelf.PropertyCount ) { return self; }

            HashSet<string>           missing = [.. TSelf.MetaData.SortedColumns.Select(static x => x.PropertyName)];
            using ArrayBuffer<string> buffer  = self.Values.AsValueEnumerable().Select(x => x.Column.PropertyName).ToArrayBuffer();
            missing.ExceptWith(buffer.Array);

            string message = $"""
                              '{typeof(TSelf).FullName}': ToDynamicParameters.Length ({self.Count}) != {nameof(TSelf.ClassProperties)}.Length ({TSelf.PropertyCount})
                              {Jakar.Extensions.Json.Serialize([.. missing])}
                              """;

            throw new InvalidOperationException(message);
        }
    }



    extension( CommandParameter self )
    {
        /// <exception cref="InvalidOperationException"> The parameter's column isn't a <see cref="ColumnMetaData"/>, so it has no driver type. </exception>
        public ColumnMetaData ColumnMetaData => self.Column as ColumnMetaData ?? throw new InvalidOperationException($"Parameter '{self.ParameterName}' is bound to a {self.Column.GetType().Name}, not a {nameof(ColumnMetaData)}.");


        // Every conversion sends a missing value as DBNull: SqlClient and Microsoft.Data.Sqlite reject a null Value instead of treating it as SQL NULL.

        public NpgsqlParameter ToPostgresParameter()
        {
            ColumnMetaData column = self.ColumnMetaData;

            return new NpgsqlParameter(self.ParameterName, column.PostgresDbType, 0, column.ColumnName)
                   {
                       Value         = self.Value ?? DBNull.Value,
                       IsNullable    = column.IsNullable,
                       SourceVersion = self.SourceVersion,
                       Direction     = self.Direction
                   };
        }
        public Microsoft.Data.SqlClient.SqlParameter ToSqlParameter()
        {
            ColumnMetaData column = self.ColumnMetaData;

            return new Microsoft.Data.SqlClient.SqlParameter(self.ParameterName, column.SqlDbType, 0, column.ColumnName)
                   {
                       Value         = self.Value ?? DBNull.Value,
                       IsNullable    = column.IsNullable,
                       SourceVersion = self.SourceVersion,
                       Direction     = self.Direction
                   };
        }
        public Microsoft.Data.Sqlite.SqliteParameter ToSqliteParameter()
        {
            ColumnMetaData column = self.ColumnMetaData;

            return new Microsoft.Data.Sqlite.SqliteParameter(self.ParameterName, column.SqliteType, 0, column.ColumnName)
                   {
                       Value         = self.Value ?? DBNull.Value,
                       IsNullable    = column.IsNullable,
                       SourceVersion = self.SourceVersion,
                       Direction     = self.Direction
                   };
        }
        public MySqlConnector.MySqlParameter ToMySqlParameter()
        {
            ColumnMetaData column = self.ColumnMetaData;

            return new MySqlConnector.MySqlParameter(self.ParameterName, column.MySqlDbType, 0, column.ColumnName)
                   {
                       Value         = self.Value ?? DBNull.Value,
                       IsNullable    = column.IsNullable,
                       SourceVersion = self.SourceVersion,
                       Direction     = self.Direction
                   };
        }
        /// <summary> A parameter for any ADO.NET provider, created by <paramref name="command"/> and typed with the provider-neutral <see cref="ColumnMetaData.AdoDbType"/>. </summary>
        public DbParameter ToDbParameter( DbCommand command )
        {
            ArgumentNullException.ThrowIfNull(command);
            ColumnMetaData column    = self.ColumnMetaData;
            DbParameter    parameter = command.CreateParameter();

            parameter.ParameterName = self.ParameterName;
            parameter.DbType        = column.AdoDbType;
            parameter.SourceColumn  = column.ColumnName;
            parameter.Value         = self.Value ?? DBNull.Value;
            parameter.IsNullable    = column.IsNullable;
            parameter.SourceVersion = self.SourceVersion;
            parameter.Direction     = self.Direction;
            return parameter;
        }
    }
}
