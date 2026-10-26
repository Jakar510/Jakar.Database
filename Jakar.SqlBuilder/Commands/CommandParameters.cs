// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 01/28/2026  18:42

using System.Collections.Immutable;
using System.Data;
using System.Runtime.CompilerServices;
using Jakar.Extensions;
using JetBrains.Annotations;
using ZLinq;
using ZLinq.Linq;
using static Jakar.Extensions.Constants;



namespace Jakar.SqlBuilder;


/// <summary>
///     The parameters of one command, bound against an <see cref="ISqlCommandTable"/>: the top-level values plus, for multi-row statements, one group per row (with numbered parameter names).
///     <para> Hosts add record-aware factories (e.g. Jakar.Database's <c>CommandParameters.Create&lt;TSelf&gt;()</c>) and convert <see cref="CommandParameter"/>s to their driver's parameter type. </para>
/// </summary>
public readonly struct CommandParameters() : IEquatable<CommandParameters>
{
    private static readonly Comparer<CommandParameter>           __columnOrder = Comparer<CommandParameter>.Create(static ( x, y ) => x.Column.Index.CompareTo(y.Column.Index));
    private readonly        List<CommandParameter>                 __parameters  = [];
    private readonly        List<ImmutableArray<CommandParameter>> __groups      = [];
    #pragma warning disable RS1035
    private readonly UInt128 __id = new((ulong)Random.Shared.NextInt64(), (ulong)Random.Shared.NextInt64());
    #pragma warning restore RS1035
    public static readonly CommandParameters Empty = new() { Table = null! };


    public required ISqlCommandTable Table
    {
        get;
        init
        {
            field = value;
            __parameters.EnsureCapacity(value?.ColumnCount ?? 0);
        }
    }
    private List<CommandParameter> __Params
    {
        get
        {
            __parameters.Sort(__columnOrder);
            return __parameters;
        }
    }
    public ReadOnlySpan<CommandParameter>                 Values => __Params.AsSpan();
    public ReadOnlySpan<ImmutableArray<CommandParameter>> Groups => __groups.AsSpan();
    public ReadOnlySpan<CommandParameter> this[ int                                    groupIndex ] => Groups[groupIndex].AsSpan();
    public ValueEnumerable<ListWhere<CommandParameter>, CommandParameter> this[ string propertyName ] => ColumnsFor(propertyName);
    public int Count          => __parameters.Count;
    public int ParameterCount => __parameters.Count + Groups.Sum(static x => x.Length);
    public ArrayBuffer<CommandParameter> Parameters
    {
        [Pure] [MustDisposeResource] get
        {
            ArrayBuffer<CommandParameter> buffer = new(ParameterCount);

            foreach ( ref readonly CommandParameter parameter in Values ) { buffer.Add(in parameter); }

            foreach ( ref readonly ImmutableArray<CommandParameter> array in Groups )
            {
                foreach ( ref readonly CommandParameter parameter in array.AsSpan() ) { buffer.Add(in parameter); }
            }

            buffer.Span.Sort(Comparer<CommandParameter>.Default);
            return buffer;
        }
    }
    public int                 Capacity            => __parameters.Capacity;
    public bool                IsGrouped           => __groups.Count > 0;
    public ParameterNames      ParameterNames      { [Pure] [MustDisposeResource] get => new(this); }
    public ExtraParameterNames GroupParameterNames { [Pure] [MustDisposeResource] get => new(this); }
    internal int VariableNameLength
    {
        get
        {
            using ArrayBuffer<CommandParameter> buffer = Parameters;
            int length = Table.MaxLengthColumnName * Table.ColumnCount;
            foreach ( ref readonly CommandParameter parameter in buffer.Span ) { length += parameter.ParameterName.Length + 10; }

            return length;
        }
    }
    public IndexedEnumerator IndexedParameters => new(this);


    internal int           KeyValuePairLength( int indentLevel )                                      => Table.KeyValuePairsLength + ParameterCount * ( indentLevel * 4 + 3 );
    public   ColumnNames   ColumnNames( int        indentLevel )                                      => new(this, indentLevel);
    public   VariableNames VariableNames( int      indentLevel )                                      => new(this, indentLevel);
    public   KeyValuePairs KeyValuePairs( int      indentLevel, params ReadOnlySpan<char> separator ) => new(this, indentLevel, separator);

    public ValueEnumerable<Union<FromSpan<CommandParameter>, SelectMany<FromSpan<ImmutableArray<CommandParameter>>, ImmutableArray<CommandParameter>, CommandParameter>, CommandParameter>, CommandParameter> AllValues() => Values.AsValueEnumerable().Union(Groups.AsValueEnumerable().SelectMany(x => x));


    public static CommandParameters Create( ISqlCommandTable table, int capacity = 0 )
    {
        CommandParameters parameters = new() { Table = table };
        parameters.__parameters.EnsureCapacity(capacity);
        return parameters;
    }


    public ValueEnumerable<ListWhere<CommandParameter>, CommandParameter> ColumnsFor( string propertyName ) => __Params.AsValueEnumerable().Where(x => string.Equals(x.Column.PropertyName, propertyName, StringComparison.InvariantCulture));


    public CommandParameters AddGroup( ref readonly CommandParameters other )
    {
        // Each row inserted in a single command needs unique parameter names, so suffix this group's parameters with its 1-based position (e.g. normalized_name1, normalized_name2).
        int                            suffix = __groups.Count + 1;
        ReadOnlySpan<CommandParameter> values = other.Values;
        CommandParameter[]             group  = new CommandParameter[values.Length];
        for ( int i = 0; i < values.Length; i++ ) { group[i] = values[i].WithSuffix(suffix); }

        __groups.Add(group.AsImmutableArray());
        return this;
    }


    /// <summary> Adds <paramref name="parameter"/> unless an equal one (same column and parameter name) is already present. </summary>
    public bool TryAdd( ref readonly CommandParameter parameter )
    {
        foreach ( ref readonly CommandParameter existing in Values )
        {
            if ( parameter.Equals(in existing) ) { return false; }
        }

        __parameters.Add(parameter);
        return true;
    }
    public CommandParameters AddRange( params ReadOnlySpan<CommandParameter> parameters )
    {
        foreach ( ref readonly CommandParameter parameter in parameters ) { TryAdd(in parameter); }

        return this;
    }


    public CommandParameters Add( string propertyName, string? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default ) =>
        AddValue(propertyName, value, parameterName, direction, sourceVersion);
    public CommandParameters Add( string propertyName, UserRights? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default ) =>
        AddValue(propertyName, value?.Value, parameterName, direction, sourceVersion);
    public CommandParameters Add<T>( string propertyName, T value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where T : unmanaged, Enum => AddValue(propertyName, value.AsLong(), parameterName, direction, sourceVersion);
    public CommandParameters Add<T>( string propertyName, T? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default )
        where T : unmanaged, Enum => AddValue(propertyName, value?.AsLong(), parameterName, direction, sourceVersion);
    /// <remarks> An <see cref="ISqlParameterValue"/> (e.g. a record id) binds as its <see cref="ISqlParameterValue.ParameterValue"/>. </remarks>
    public CommandParameters Add( string propertyName, object? value, [CallerArgumentExpression(nameof(value))] string parameterName = EMPTY, ParameterDirection direction = ParameterDirection.Input, DataRowVersion sourceVersion = DataRowVersion.Default ) =>
        AddValue(propertyName, value, parameterName, direction, sourceVersion);
    private CommandParameters AddValue( string propertyName, object? value, string parameterName, ParameterDirection direction, DataRowVersion sourceVersion )
    {
        CommandParameter parameter = new(value, Table.GetParameterName(parameterName), Table.GetColumn(propertyName), direction, sourceVersion);
        TryAdd(in parameter);
        return this;
    }


    public override int GetHashCode() => __id.GetHashCode();
    public ulong GetHash64()
    {
        using ParameterNames buffer = ParameterNames;
        ReadOnlySpan<string> names  = buffer.Span;
        return Hashes.Hash(in names);
    }
    public UInt128 GetHash128()
    {
        using ParameterNames buffer = ParameterNames;
        ReadOnlySpan<string> names  = buffer.Span;
        return Hashes.Hash128(in names);
    }


    public override string ToString()                                                             => $"{nameof(CommandParameters)} {{ Parameters: [{string.Join(", ", __parameters.ConvertAll(static x => x.ParameterName))}], Groups: {__groups.Count} }}";
    public          bool   Equals( CommandParameters              other )                         => Equals(in other);
    public          bool   Equals( ref readonly CommandParameters other )                         => __id.Equals(other.__id);
    public override bool   Equals( object?                        obj )                           => obj is CommandParameters other && Equals(other);
    public static   bool operator ==( CommandParameters           left, CommandParameters right ) => left.Equals(right);
    public static   bool operator !=( CommandParameters           left, CommandParameters right ) => !left.Equals(right);
}
