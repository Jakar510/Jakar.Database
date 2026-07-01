// Jakar.Database :: Jakar.Database
// 03/04/2026  17:09

namespace Jakar.Database;


[UseWithCaution]
public readonly record struct SqlRaw( string Value )
{
    public readonly string Value = Value;
    public          bool   IsValid                { [Pure] [MemberNotNullWhen(true, nameof(Value))] get => !string.IsNullOrWhiteSpace(Value); }
    public          int    Length                 => Value.Length;
    public static   SqlRaw Create( string value ) => new(value);
    public override string ToString()             => Value;
}



[UseWithCaution]
public readonly record struct SqlName( string Value )
{
    public static readonly SqlName Empty = new(string.Empty);
    public readonly string Value = string.IsNullOrWhiteSpace(Value)
                                       ? string.Empty
                                       : Value.SqlName();
    public                          bool    IsValid                { [Pure] [MemberNotNullWhen(true, nameof(Value))] get => !string.IsNullOrWhiteSpace(Value); }
    public                          int     Length                 => Value.Length;
    public static                   SqlName Create( string value ) => new(value);
    public static implicit operator SqlName( string        value ) => new(value);
    public static implicit operator string( SqlName        value ) => value.Value;
    public override                 string ToString()              => Value;
}



[InterpolatedStringHandler]
public ref struct SqlInterpolatedStringHandler<TSelf>( int literalLength, int formattedCount )
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    internal readonly StringBuilder     Sb         = new(literalLength);
    internal readonly CommandParameters Parameters = CommandParameters.Create<TSelf>(formattedCount);
    private           string?           __lastColumnName;


    public void AppendNull()                                                                                                                         => AppendLiteral("null");
    public void AppendLiteral( ReadOnlySpan<char> value )                                                                                            => Sb.Append(value);
    public void AppendFormatted( StringBuilder?   value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY ) => AppendFragment(value);


    public void AppendFormatted( ReadOnlySpan<string> value, string? format = null )
    {
        ParseFormat(ref format, out ushort indentLevel);
        AppendQuoted(value, indentLevel);
    }


    public void AppendFormatted<TRecord>( RecordID<TRecord> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        ParseFormat(ref format, out ushort indentLevel);
        AppendSingle(value.Value, format, true);
    }

    public void AppendFormatted<TRecord>( IEnumerable<RecordID<TRecord>> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        ParseFormat(ref format, out ushort indentLevel);
        AppendMany(value, format, indentLevel, true);
    }

    public void AppendFormatted<TRecord>( ReadOnlySpan<RecordID<TRecord>> value, string? format = null, [CallerArgumentExpression(nameof(value))] string paramName = EMPTY )
        where TRecord : PairRecord<TRecord>, ITableRecord<TRecord>
    {
        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        ParseFormat(ref format, out ushort indentLevel);
        AppendMany(value, format, indentLevel, true);
    }


    public void AppendFormatted( bool value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( bool? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( byte value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( byte? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( sbyte value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( sbyte? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( char value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( char? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( short value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( short? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( ushort value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( ushort? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( int value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( int? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( uint value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( uint? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( long value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( long? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( ulong value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( ulong? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( float value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( float? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( double value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( double? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( decimal value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => Sb.Append(value);
    public void AppendFormatted( decimal? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }


    public void AppendFormatted( Guid value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => AppendSingle(value, format, true);
    public void AppendFormatted( Guid? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( DateOnly value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => AppendSingle(value, format, true);
    public void AppendFormatted( DateOnly? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( DateTime value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => AppendSingle(value, format, true);
    public void AppendFormatted( DateTime? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( DateTimeOffset value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => AppendSingle(value, format, true);
    public void AppendFormatted( DateTimeOffset? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( TimeOnly value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => AppendSingle(value, format, true);
    public void AppendFormatted( TimeOnly? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }

    public void AppendFormatted( TimeSpan value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY ) => AppendSingle(value, format, true);
    public void AppendFormatted( TimeSpan? value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        if ( value.HasValue ) { AppendFormatted(value.Value, format, paramName); }
        else { AppendNull(); }
    }


    public void AppendFormatted<TValue>( TValue value, string? format = null, [CallerArgumentExpression(nameof(value))] in string paramName = EMPTY )
    {
        char lastChar = Sb.Length > 0
                            ? Sb[^1]
                            : '\0';

        bool isParameter = lastChar == '@';

        ParseFormat(ref format, out ushort indentLevel);

        switch ( value )
        {
            case null:
            case DBNull:
                AppendNull();
                return;

            case string n:
            {
                bool isColumName = paramName.StartsWith("nameof(", StringComparison.Ordinal) || paramName == "columnName";

                if ( isColumName )
                {
                    // if ( __lastColumnName is not null ) { ThrowFormatException($"Missing @parameter for '{paramName}' with a value of '{value}'", isColumName, isParameter, lastChar); }

                    __lastColumnName = n;
                }

                switch ( n.Length )
                {
                    case > 0 when isColumName:
                        Sb.Append(n.SqlName());
                        return;

                    case > 0 when isParameter:
                        string parameterName = paramName.Parameterize();
                        Sb.Append(parameterName);

                        if ( __lastColumnName is null ) { ThrowFormatException($"Missing parameter (column_name) for '{paramName}' with a value of '{value}'", isColumName, isParameter, lastChar, parameterName); }

                        Parameters.Add(__lastColumnName, value, parameterName);
                        Debug.Assert(Parameters.Count > 0);
                        __lastColumnName = null;
                        return;

                    case > 0:
                        AppendQuoted(n);
                        return;

                    default:
                        AppendQuoted(EMPTY);
                        return;
                }
            }

            case SqlRaw n:
                Sb.Append(n.Value);
                return;

            case SqlName n:
                Sb.Append(n.Value);
                return;

            case ColumnNames n:
                AppendFragment(n.Value);
                return;

            case VariableNames n:
                Parameters.AddInternal(n.Values);
                AppendFragment(n.Value);
                return;

            case KeyValuePairs n:
                Parameters.AddInternal(n.Values);
                AppendFragment(n.Value);
                return;

            case CommandParameters:
                throw new InvalidOperationException($"""
                                                     Not supported by design. Use any of the following methods instead: 
                                                        • {nameof(CommandParameters)}.{nameof(CommandParameters.VariableNames)}
                                                        • {nameof(CommandParameters)}.{nameof(CommandParameters.KeyValuePairs)}
                                                        • {nameof(CommandParameters)}.{nameof(CommandParameters.ColumnNames)}
                                                     """);

            case Enum n:
                // ReSharper disable once RedundantToStringCall
                if ( string.Equals(format, "str", StringComparison.OrdinalIgnoreCase) || string.Equals(format, "string", StringComparison.OrdinalIgnoreCase) ) { Sb.Append("'").Append(n.ToString()).Append("'"); }
                else { Sb.Append(Convert.ToInt64(n)); }

                return;

            case Type n:
                AppendQuoted(n.Name);
                return;

            case StringBuilder n:
                AppendFragment(n);
                return;

            case IEnumerable<string> n:
                AppendQuoted(n, indentLevel);
                return;

            case IEnumerable<Int128> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<UInt128> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<byte> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<sbyte> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<short> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<int> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<long> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<uint> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<ushort> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<ulong> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<float> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<double> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<decimal> n:
                AppendMany(n, format, indentLevel);
                return;

            case IEnumerable<char> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<IRecordID> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<Guid> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<DateTime> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<DateTimeOffset> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<DateOnly> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<TimeSpan> n:
                AppendMany(n, format, indentLevel, true);
                return;

            case IEnumerable<TimeOnly> n:
                AppendMany(n, format, indentLevel, true);
                return;


            case ISpanFormattable x:
                AppendSingle(x, format);
                return;

            case IFormattable x:
                Sb.Append(x.ToString(format, CultureInfo.InvariantCulture));
                return;

            default:
                // ReSharper disable once RedundantToStringCallForValueType
                Sb.Append(value.ToString());
                return;
        }
    }


    public void AppendFormatted<TValue>( ReadOnlySpan<TValue> value, string? format = null )
        where TValue : ISpanFormattable
    {
        bool needsQuotes = NeedsQuotes<TValue>();
        ParseFormat(ref format, out ushort indentLevel);
        AppendMany(value, format, indentLevel, in needsQuotes);
    }


    public void AppendFormatted<TEnumerator, TValue>( ValueEnumerable<TEnumerator, TValue> value, string? format = null )
        where TEnumerator : struct, IValueEnumerator<TValue>
        where TValue : ISpanFormattable
    {
        Span<char> destination = stackalloc char[256];
        bool       needsQuotes = NeedsQuotes<TValue>();
        ParseFormat(ref format, out ushort indentLevel);

        using ValueEnumerator<TEnumerator, TValue> enumerator = value.GetEnumerator();

        bool first = true;

        while ( enumerator.MoveNext() )
        {
            if ( !first ) { Sb.Append(",\n"); }

            first = false;
            Sb.Spacer(indentLevel);
            AppendSingle(destination, enumerator.Current, format, in needsQuotes);
        }
    }


    private void AppendSingle<TValue>( TValue value, string? format, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        Span<char> destination = stackalloc char[256];
        AppendSingle(destination, value, format, in needsQuotes);
    }
    private void AppendSingle<TValue>( scoped Span<char> destination, TValue value, string? format, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        if ( typeof(TValue).IsOneOf(typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly)) ) { format ??= "o"; }

        if ( value.TryFormat(destination, out int charsWritten, format, CultureInfo.InvariantCulture) )
        {
            if ( needsQuotes ) { Sb.Append('\''); }

            Sb.Append(destination[..charsWritten]);
            if ( needsQuotes ) { Sb.Append('\''); }
        }
        else
        {
            if ( needsQuotes ) { Sb.Append('\''); }

            Sb.Append(value.ToString(null, CultureInfo.InvariantCulture));
            if ( needsQuotes ) { Sb.Append('\''); }
        }
    }


    private void AppendMany<TValue>( IEnumerable<TValue> enumerable, string? format, ushort indentLevel, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        Span<char> destination = stackalloc char[256];

        switch ( enumerable )
        {
            case IReadOnlyList<TValue> list:
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    Sb.Spacer(indentLevel);
                    AppendSingle(destination, list[i], format, in needsQuotes);
                    if ( i < list.Count - 1 ) { Sb.Append(",\n"); }
                }

                return;
            }

            case IList<TValue> list:
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    Sb.Spacer(indentLevel);
                    AppendSingle(destination, list[i], format, in needsQuotes);
                    if ( i < list.Count - 1 ) { Sb.Append(",\n"); }
                }

                return;
            }

            default:
            {
                using IEnumerator<TValue> enumerator = enumerable.GetEnumerator();

                bool first = true;

                while ( enumerator.MoveNext() )
                {
                    if ( !first ) { Sb.Append(",\n"); }

                    first = false;
                    Sb.Spacer(indentLevel);
                    AppendSingle(destination, enumerator.Current, format, in needsQuotes);
                }

                return;
            }
        }
    }
    private void AppendMany<TValue>( ReadOnlySpan<TValue> array, string? format, ushort indentLevel, in bool needsQuotes = false )
        where TValue : ISpanFormattable
    {
        Span<char> destination = stackalloc char[256];

        for ( int i = 0; i < array.Length; i++ )
        {
            Sb.Spacer(indentLevel);
            AppendSingle(destination, array[i], format, in needsQuotes);
            if ( i < array.Length - 1 ) { Sb.Append(",\n"); }
        }
    }


    /// <summary>
    ///     Appends a pre-formatted, possibly multi-line fragment (<see cref="ColumnNames"/> , <see cref="VariableNames"/> , <see cref="KeyValuePairs"/>) so that every continuation line is
    ///     hang-indented to the column at which the fragment starts. This keeps the block aligned regardless of where the interpolation placeholder sits within the surrounding statement.
    /// </summary>
    private void AppendFragment( StringBuilder? fragment )
    {
        if ( fragment is null || fragment.Length is 0 ) { return; }

        int hang = CurrentLineIndent();

        if ( hang is 0 )
        {
            Sb.Append(fragment);
            return;
        }

        for ( int i = 0; i < fragment.Length; i++ )
        {
            char c = fragment[i];
            Sb.Append(c);
            if ( c == '\n' ) { Sb.Append(' ', hang); }
        }
    }
    /// <summary> Number of characters written on the current line since the last newline (the column at which the next character would be appended). </summary>
    private int CurrentLineIndent()
    {
        int index = Sb.Length - 1;
        while ( index >= 0 && Sb[index] != '\n' ) { index--; }

        return Sb.Length - 1 - index;
    }


    private void AppendQuoted( ReadOnlySpan<char> value )                     => Sb.Append("'").Append(value).Append("'");
    private void AppendQuoted( ReadOnlySpan<char> value, ushort indentLevel ) => Sb.Spacer(indentLevel).Append("'").Append(value).Append("'");


    private void AppendQuoted( IEnumerable<string> enumerable, ushort indentLevel )
    {
        switch ( enumerable )
        {
            case IReadOnlyList<string> array:
            {
                for ( int i = 0; i < array.Count; i++ )
                {
                    AppendQuoted(array[i], indentLevel);
                    if ( i < array.Count - 1 ) { Sb.Append(",\n"); }
                }

                return;
            }

            case IList<string> list:
            {
                for ( int i = 0; i < list.Count; i++ )
                {
                    AppendQuoted(list[i], indentLevel);
                    if ( i < list.Count - 1 ) { Sb.Append(",\n"); }
                }

                return;
            }

            default:
            {
                using IEnumerator<string> enumerator = enumerable.GetEnumerator();

                bool first = true;

                while ( enumerator.MoveNext() )
                {
                    if ( !first ) { Sb.Append(",\n"); }

                    first = false;
                    AppendQuoted(enumerator.Current, indentLevel);
                }

                return;
            }
        }
    }
    private void AppendQuoted( ReadOnlySpan<string> array, ushort indentLevel )
    {
        for ( int i = 0; i < array.Length; i++ )
        {
            AppendQuoted(array[i], indentLevel);
            if ( i < array.Length - 1 ) { Sb.Append(",\n"); }
        }
    }


    public override string ToString() => Clean(Sb.ToString());


    /// <summary>
    ///     Normalizes generated SQL for emission: trailing whitespace is stripped from every line, consecutive blank lines are collapsed to a single blank line, and leading/trailing blank lines are removed.
    ///     Indentation and alignment produced by <see cref="AppendFragment"/> are preserved.
    /// </summary>
    public static string Clean( string sql )
    {
        if ( string.IsNullOrEmpty(sql) ) { return sql; }

        StringBuilder      builder = new(sql.Length);
        ReadOnlySpan<char> span    = sql;
        bool               hasBody = false;
        bool               pending = false;

        while ( !span.IsEmpty || hasBody )
        {
            int                newLine = span.IndexOf('\n');
            ReadOnlySpan<char> line;

            if ( newLine < 0 )
            {
                line = span;
                span = ReadOnlySpan<char>.Empty;
            }
            else
            {
                line = span[..newLine];
                span = span[( newLine + 1 )..];
            }

            ReadOnlySpan<char> trimmed = line.TrimEnd();

            if ( trimmed.IsEmpty )
            {
                if ( hasBody ) { pending = true; }
            }
            else
            {
                if ( hasBody ) { builder.Append('\n'); }

                if ( pending )
                {
                    builder.Append('\n');
                    pending = false;
                }

                builder.Append(trimmed);
                hasBody = true;
            }

            if ( newLine < 0 ) { break; }
        }

        return builder.ToString();
    }
    [DoesNotReturn] [MethodImpl(MethodImplOptions.AggressiveInlining)] private void ThrowFormatException( string message, bool isColumName, bool isParameter, char lastChar, string? parameterName = null )
    {
        throw new FormatException($"""
                                   {message}

                                   {nameof(isColumName)}: '{isColumName}'
                                   {nameof(isParameter)}: '{isParameter}'
                                   {nameof(lastChar)}: '{lastChar}'
                                   {nameof(parameterName)}: '{parameterName}'
                                   {nameof(__lastColumnName)}: '{__lastColumnName}'
                                   Current Value of handler: '{ToString()}'
                                   """);
    }


    public (string SQL, ImmutableArray<SqlParameter> Parameters) Build() => new(ToString(), [..Parameters.Values]);


    public SqlCommand ToSqlCommand( in CommandType commandType = CommandType.Text ) => SqlCommand.Create(ToString(), Parameters, in commandType);


    public static string SanitizeParameterName( ReadOnlySpan<char> name )
    {
        if ( name.IsNullOrWhiteSpace() ) { return "p"; }

        // Keep only the last segment after '.'
        int lastDot = name.LastIndexOf('.');
        if ( lastDot >= 0 && lastDot < name.Length - 1 ) { name = name[( lastDot + 1 )..]; }

        if ( name.IsNullOrWhiteSpace() ) { return "p"; }

        Span<char> buffer = stackalloc char[name.Length];
        int        index  = 0;

        foreach ( char c in name )
        {
            buffer[index++] = char.IsLetterOrDigit(c) || c == '_'
                                  ? c
                                  : '_';
        }

        return buffer[..index].ToString();
    }
    public static void ParseFormat( ref string? format, out ushort indentLevel )
    {
        ReadOnlySpan<char> span  = format;
        int                index = span.IndexOf('|');
        if ( index >= 0 ) { span = span[..index]; }

        if ( ushort.TryParse(span, out indentLevel) )
        {
            format = index >= 0
                         ? format?[index..]
                         : null;
        }
    }
    public static bool NeedsQuotes<TValue>()
    {
        if ( typeof(TValue).HasInterface<IRecordID>() ) { return true; }

        ReadOnlySpan<Type> span =
        [
            typeof(string),
            typeof(char),
            typeof(Guid),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(DateOnly),
            typeof(TimeSpan),
            typeof(TimeOnly)
        ];

        foreach ( Type type in span )
        {
            if ( typeof(TValue) == type ) { return true; }
        }

        return false;
    }
}
