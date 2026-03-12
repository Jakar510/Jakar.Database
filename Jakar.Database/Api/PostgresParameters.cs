// Jakar.Database :: Jakar.Database
// 01/28/2026  18:42

using Microsoft.Data.SqlClient;
using ZLinq.Linq;



namespace Jakar.Database;


public readonly record struct SqlParameter( object? Value, string ParameterName, string SourceColumn, PostgresType DbType, bool IsNullable, ParameterDirection Direction, DataRowVersion SourceVersion )
{
    public readonly object?            Value         = Value ?? DBNull.Value;
    public readonly string             ParameterName = ParameterName.SqlName();
    public readonly string             SourceColumn  = SourceColumn;
    public readonly PostgresType       DbType        = DbType;
    public readonly bool               IsNullable    = IsNullable;
    public readonly ParameterDirection Direction     = Direction;
    public readonly DataRowVersion     SourceVersion = SourceVersion;


    public NpgsqlParameter ToPostgresParameter() => new(ParameterName, DbType.ToNpgsqlDbType(), 0, SourceColumn)
                                                    {
                                                        Value         = Value,
                                                        IsNullable    = IsNullable,
                                                        SourceVersion = SourceVersion,
                                                        Direction     = Direction,
                                                    };
    public Microsoft.Data.SqlClient.SqlParameter ToSqlParameter() => new(ParameterName, DbType.ToSqlDbType(), 0, SourceColumn)
                                                                     {
                                                                         Value         = Value,
                                                                         IsNullable    = IsNullable,
                                                                         SourceVersion = SourceVersion,
                                                                         Direction     = Direction,
                                                                     };
}



public readonly struct CommandParameters() : IEquatable<CommandParameters>
{
    // internal readonly List<SqlParameter>                    parameters = [];
    // internal readonly List<ImmutableArray<SqlParameter>>    Extras     = [];
    private readonly List<SqlParameter>                 __parameters = [];
    private readonly List<ImmutableArray<SqlParameter>> __extras     = [];


    public bool IsEmpty { [MemberNotNullWhen(true, nameof(Table))] get => Table is not null; }
    public required ITableMetaData Table
    {
        get;
        init
        {
            field = value;
            __parameters.EnsureCapacity(value.ColumnCount);
        }
    }
    public ReadOnlySpan<SqlParameter>                 Values                   => __parameters.AsSpan();
    public ReadOnlySpan<ImmutableArray<SqlParameter>> Extras                   => __extras.AsSpan();
    public ReadOnlySpan<SqlParameter>                 ExtraValues( int index ) => Extras[index].AsSpan();
    public int                                        Count                    => __parameters.Count;
    public int                                        ParameterCount           => __parameters.Count + Extras.Sum(static x => x.Length);
    public ArrayBuffer<SqlParameter> Parameters
    {
        [Pure] [MustDisposeResource] get
        {
            ArrayBuffer<SqlParameter> buffer = new(ParameterCount);
            foreach ( ref readonly SqlParameter parameter in Values ) { buffer.Add(in parameter); }

            foreach ( ref readonly ImmutableArray<SqlParameter> array in Extras )
            {
                foreach ( ref readonly SqlParameter parameter in array.AsSpan() ) { buffer.Add(in parameter); }
            }

            return buffer;
        }
    }
    public   int                                     Capacity                              => __extras.Capacity;
    public   bool                                    IsGrouped                             => __extras.Count > 0;
    public   ValueEnumerable<ParameterNames, string> ParameterNames                        { [Pure] get => new(new ParameterNames(this)); }
    public   int                                     SpacerCount                           => Math.Max(__parameters.Count, Table.ColumnCount) - 1;
    internal int                                     VariableNameLength                    => Table.MaxLength_ColumnName * Table.ColumnCount  + Parameters.Sum(static x => x.ParameterName.Length + 10);
    public   IndexedEnumerator                       IndexedParameters                     => new(this);
    internal int                                     KeyValuePairLength( int indentLevel ) => Table.Properties.Values.Sum(static x => x.KeyValuePair.Length) + ParameterCount * ( indentLevel * 4 + 3 );


    public ColumnNames   ColumnNames( int   indentLevel )                           => new(this, indentLevel);
    public VariableNames VariableNames( int indentLevel )                           => new(this, indentLevel);
    public KeyValuePairs KeyValuePairs( int indentLevel, string separator = "AND" ) => new(this, indentLevel, separator);


    public static CommandParameters Create<TSelf>()
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => new() { Table = TSelf.MetaData };
    public static CommandParameters Create<TSelf>( TSelf _ )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf> => new() { Table = TSelf.MetaData };
    public static CommandParameters Create<TSelf>( IEnumerable<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = new() { Table = TSelf.MetaData };
        foreach ( TSelf record in records ) { parameters.With(record.ToDynamicParameters()); }

        return parameters;
    }
    public static CommandParameters Create<TSelf>( params ReadOnlySpan<TSelf> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = Create<TSelf>(records.Length);
        foreach ( TSelf record in records ) { parameters.With(record.ToDynamicParameters()); }

        return parameters;
    }
    public static CommandParameters Create<TSelf>( params ReadOnlySpan<SqlParameter> records )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = Create<TSelf>(records.Length);
        foreach ( ref readonly SqlParameter record in records ) { parameters.Add(in record); }

        return parameters;
    }
    public static CommandParameters Create<TSelf>( int capacity )
        where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    {
        CommandParameters parameters = new() { Table = TSelf.MetaData };
        parameters.__parameters.EnsureCapacity(capacity);
        return parameters;
    }


    public ValueEnumerable<Where<FromSpan<SqlParameter>, SqlParameter>, SqlParameter> ColumnsFor( string propertyName ) => Values.AsValueEnumerable().Where(x => string.Equals(x.SourceColumn, propertyName.SqlName(), StringComparison.InvariantCulture));

    public CommandParameters With( in CommandParameters other ) => With([..other.__parameters]);
    public CommandParameters With( in ImmutableArray<SqlParameter> other )
    {
        __extras.Add(other);
        return this;
    }


    public CommandParameters Add<TSelf>( string propertyName, IRecordID value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Add(Table[propertyName].ToParameter(value.ID, parameterName, direction, sourceVersion));
    public CommandParameters Add<TSelf>( string propertyName, RecordID<TSelf> value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where TSelf : PairRecord<TSelf>, ITableRecord<TSelf> => Add(Table[propertyName].ToParameter(value.Value, parameterName, direction, sourceVersion));
    public CommandParameters Add<T>( string propertyName, T? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where T : struct, Enum => Add(Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion));
    public CommandParameters Add( string propertyName, object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default ) => Add(Table[propertyName].ToParameter(value, parameterName, direction, sourceVersion));
    public CommandParameters Add( in SqlParameter parameter )
    {
        foreach ( ref readonly SqlParameter value in Values )
        {
            if ( !string.Equals(value.ParameterName, parameter.ParameterName, StringComparison.InvariantCulture) ) { continue; }

            if ( Equals(value.Value, parameter.Value) ) { return this; }
        }

        __parameters.Add(parameter);
        return this;
    }


    public override int GetHashCode() => Extras.GetHashCode();
    public ulong GetHash64()
    {
        using ParameterNames array = new(this);
        ReadOnlySpan<string> names = array.Span;
        return Hashes.Hash(in names);
    }
    public UInt128 GetHash128()
    {
        using ParameterNames array = new(this);
        ReadOnlySpan<string> names = array.Span;
        return Hashes.Hash128(in names);
    }


    public          bool Equals( CommandParameters      other )                         => GetHash128() == other.GetHash128();
    public override bool Equals( object?                obj )                           => obj is CommandParameters other && Equals(other);
    public static   bool operator ==( CommandParameters left, CommandParameters right ) => left.Equals(right);
    public static   bool operator !=( CommandParameters left, CommandParameters right ) => !left.Equals(right);
}
