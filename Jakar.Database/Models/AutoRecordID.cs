// Jakar.Database :: Jakar.Database
// 02/09/2026  11:10

namespace Jakar.Database;


[DefaultMember(nameof(Empty))]
public readonly struct AutoRecordID<TSelf>( long id ) : IEquatable<AutoRecordID<TSelf>>, IComparable<AutoRecordID<TSelf>>, ISpanFormattable, ISpanParsable<AutoRecordID<TSelf>>, IRegisterDapperTypeHandlers
    where TSelf : class, ITableRecord<TSelf>
{
    public static readonly AutoRecordID<TSelf> Empty = new(0);
    public readonly        string              key   = $"{TSelf.TableName}:{id}";
    public readonly        long                Value = id;


    [Pure] public static AutoRecordID<TSelf>  Parse( string                    value )                       => Create(long.Parse(value));
    [Pure] public static AutoRecordID<TSelf>  Parse( params ReadOnlySpan<char> value )                       => Create(long.Parse(value));
    [Pure] public static AutoRecordID<TSelf>  ID( NpgsqlDataReader             reader )                      => Create(reader, nameof(IDateCreated.ID));
    [Pure] public static AutoRecordID<TSelf>? CreatedBy( NpgsqlDataReader      reader )                      => TryCreate(reader, nameof(ICreatedBy.CreatedBy));
    [Pure] public static AutoRecordID<TSelf>? TryCreate( NpgsqlDataReader      reader, string propertyName ) => TryCreate(reader.GetFieldValue<long?>(TSelf.PropertyMetaData[propertyName].Index));
    [Pure] public static AutoRecordID<TSelf>  Create( NpgsqlDataReader         reader, string propertyName ) => Create(reader.GetFieldValue<long>(TSelf.PropertyMetaData[propertyName].Index));
    [Pure] public static AutoRecordID<TSelf>  Create( long                     id ) => new(id);
    [Pure] public static AutoRecordID<TSelf> Create( [NotNullIfNotNull(nameof(id))] long? id ) => id.HasValue
                                                                                                      ? new AutoRecordID<TSelf>(id.Value)
                                                                                                      : Empty;
    [Pure] public static AutoRecordID<TSelf> Create<TValue>( TValue id )
        where TValue : IUniqueID<long> => Create(id.ID);
    [Pure] public static IEnumerable<AutoRecordID<TSelf>> Create<TValue>( IEnumerable<TValue> ids )
        where TValue : IUniqueID<long> => ids.Select(Create);
    [Pure] public static IAsyncEnumerable<AutoRecordID<TSelf>> Create<TValue>( IAsyncEnumerable<TValue> ids )
        where TValue : IUniqueID<long> => AsyncLinq.Select(ids, Create);
    [Pure] public static AutoRecordID<TSelf>? TryCreate( long? id ) => id.HasValue
                                                                           ? TryCreate(id.Value)
                                                                           : null;
    [Pure] public static AutoRecordID<TSelf>? TryCreate( long id )
    {
        if ( id.IsValidID() ) { return new AutoRecordID<TSelf>(id); }

        return null;
    }


    public static string              Description()                                                                   => $"AutoRecordID<{typeof(TSelf).Name}>";
    public static AutoRecordID<TSelf> Parse( string                         value, IFormatProvider?        provider ) => new(long.Parse(value, provider));
    public static bool                TryParse( [NotNullWhen(true)] string? value, out AutoRecordID<TSelf> result )   => TryParse(value, null, out result);
    public static bool TryParse( [NotNullWhen(               true)] string? value, IFormatProvider? provider, out AutoRecordID<TSelf> result )
    {
        if ( long.TryParse(value, provider, out long guid) )
        {
            result = Create(guid);
            return true;
        }

        result = Empty;
        return false;
    }


    public static AutoRecordID<TSelf> Parse( ReadOnlySpan<char>    value, IFormatProvider?        provider ) => new(long.Parse(value, provider));
    public static bool                TryParse( ReadOnlySpan<char> value, out AutoRecordID<TSelf> result )   => TryParse(value, null, out result);
    public static bool TryParse( ReadOnlySpan<char> value, IFormatProvider? provider, out AutoRecordID<TSelf> result )
    {
        if ( long.TryParse(value, provider, out long guid) )
        {
            result = Create(guid);
            return true;
        }

        result = Empty;
        return false;
    }


    public UInt128 GetHash() => key.Hash128();
    [Pure] public PostgresParameters ToDynamicParameters()
    {
        PostgresParameters parameters = PostgresParameters.Create<TSelf>();
        parameters.Add(nameof(IDateCreated.ID), Value);
        return parameters;
    }


    public bool IsValid()    => Value > 0;
    public bool IsNotValid() => Value <= 0;


    public override string ToString() => Value.ToString();
    public string ToString( string? format, IFormatProvider? formatProvider ) => string.Equals(format, "b64", StringComparison.InvariantCultureIgnoreCase)
                                                                                     ? Value.ToBase64()
                                                                                     : Value.ToString(format, formatProvider);
    public bool TryFormat( Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider )
    {
        if ( format is not "b64" ) { return Value.TryFormat(destination, out charsWritten, format); }

        ReadOnlySpan<char> span = Value.ToBase64();
        span.CopyTo(destination);
        charsWritten = span.Length;
        return span.Length > 0;
    }


    public          bool Equals( AutoRecordID<TSelf>    other )      => Value.Equals(other.Value);
    public          int  CompareTo( AutoRecordID<TSelf> other )      => Value.CompareTo(other.Value);
    public override int  GetHashCode()                               => Value.GetHashCode();
    public override bool Equals( [NotNullWhen(true)] object? other ) => other is AutoRecordID<TSelf> id && Equals(id);


    public static bool operator true( AutoRecordID<TSelf>  recordID )                         => recordID.IsValid();
    public static bool operator false( AutoRecordID<TSelf> recordID )                         => recordID.IsNotValid();
    public static bool operator ==( AutoRecordID<TSelf>?   left, AutoRecordID<TSelf>? right ) => Nullable.Equals(left, right);
    public static bool operator !=( AutoRecordID<TSelf>?   left, AutoRecordID<TSelf>? right ) => !Nullable.Equals(left, right);
    public static bool operator ==( AutoRecordID<TSelf>    left, AutoRecordID<TSelf>  right ) => EqualityComparer<AutoRecordID<TSelf>>.Default.Equals(left, right);
    public static bool operator !=( AutoRecordID<TSelf>    left, AutoRecordID<TSelf>  right ) => !EqualityComparer<AutoRecordID<TSelf>>.Default.Equals(left, right);
    public static bool operator >( AutoRecordID<TSelf>     left, AutoRecordID<TSelf>  right ) => Comparer<AutoRecordID<TSelf>>.Default.Compare(left, right) > 0;
    public static bool operator >=( AutoRecordID<TSelf>    left, AutoRecordID<TSelf>  right ) => Comparer<AutoRecordID<TSelf>>.Default.Compare(left, right) >= 0;
    public static bool operator <( AutoRecordID<TSelf>     left, AutoRecordID<TSelf>  right ) => Comparer<AutoRecordID<TSelf>>.Default.Compare(left, right) < 0;
    public static bool operator <=( AutoRecordID<TSelf>    left, AutoRecordID<TSelf>  right ) => Comparer<AutoRecordID<TSelf>>.Default.Compare(left, right) <= 0;


    public static void RegisterDapperTypeHandlers()
    {
        NullableDapperTypeHandler.Register();
        DapperTypeHandler.Register();
    }



    public sealed class DapperTypeHandler : SqlConverter<DapperTypeHandler, AutoRecordID<TSelf>>
    {
        public override void SetValue( IDbDataParameter parameter, AutoRecordID<TSelf> value ) => parameter.Value = value.Value;
        public override AutoRecordID<TSelf> Parse( object value ) =>
            value switch
            {
                long guidValue                                                                                            => new AutoRecordID<TSelf>(guidValue),
                string stringValue when !string.IsNullOrEmpty(stringValue) && long.TryParse(stringValue, out long result) => new AutoRecordID<TSelf>(result),
                _                                                                                                         => throw new InvalidCastException($"Unable to cast object of type {value.GetType()} to AutoRecordID<TSelf>")
            };
    }



    public sealed class NullableDapperTypeHandler : SqlConverter<NullableDapperTypeHandler, AutoRecordID<TSelf>?>
    {
        public override void SetValue( IDbDataParameter parameter, AutoRecordID<TSelf>? id ) => parameter.Value = id?.Value;
        public override AutoRecordID<TSelf>? Parse( object value ) =>
            value switch
            {
                null                                                                                                      => default,
                long guidValue                                                                                            => new AutoRecordID<TSelf>(guidValue),
                string stringValue when !string.IsNullOrEmpty(stringValue) && long.TryParse(stringValue, out long result) => new AutoRecordID<TSelf>(result),
                _                                                                                                         => throw new InvalidCastException($"Unable to cast object of type {value.GetType()} to AutoRecordID<TSelf>")
            };
    }
}
