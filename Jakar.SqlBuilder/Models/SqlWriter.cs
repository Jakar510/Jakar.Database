// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

using System.Buffers;



namespace Jakar.SqlBuilder;


/// <summary>
///     The single mutable building surface: a growable, pooled character buffer plus parenthesis depth, emitted-clause
///     flags and the bound-parameter collector. A <see langword="ref"/> <see langword="struct"/> carried by value through
///     the stages (mutate-then-forward); only the latest copy is read, so the shared backing array is safe.
/// </summary>
public ref struct SqlWriter
{
    private char[]             __buffer;
    private int                __length;
    private int                __depth;
    private ClauseFlags        __flags;
    private ParameterCollector __parameters;

    public readonly SqlDialectKind Dialect          { get; }
    public readonly bool           AppendTerminator { get; }
    public readonly bool           StrictTypes      { get; }
    public readonly ClauseFlags    Flags            => __flags;
    public readonly int            ParenthesisDepth => __depth;


    public SqlWriter( SqlDialectKind dialect, SqlBuilderOptions options )
    {
        if ( dialect is SqlDialectKind.NotSet ) { throw new SqlBuildException(SqlBuildError.DialectNotSet, "A SQL dialect must be selected before building.", string.Empty, dialect); }

        __buffer         = ArrayPool<char>.Shared.Rent(options.InitialBufferSize);
        __length         = 0;
        __depth          = 0;
        __flags          = ClauseFlags.None;
        __parameters     = new ParameterCollector();
        Dialect          = dialect;
        AppendTerminator = options.AppendStatementTerminator;
        StrictTypes      = options.StrictTypes;
    }


    private void EnsureCapacity( int additional )
    {
        if ( __length + additional <= __buffer.Length ) { return; }

        int    target = Math.Max(__buffer.Length * 2, __length + additional);
        char[] grown  = ArrayPool<char>.Shared.Rent(target);
        Array.Copy(__buffer, grown, __length);
        ArrayPool<char>.Shared.Return(__buffer);
        __buffer = grown;
    }
    public void Append( char c )
    {
        EnsureCapacity(1);
        __buffer[__length++] = c;
    }
    public void Append( scoped ReadOnlySpan<char> text )
    {
        if ( text.IsEmpty ) { return; }

        EnsureCapacity(text.Length);
        text.CopyTo(__buffer.AsSpan(__length));
        __length += text.Length;
    }

    private readonly bool NeedsSeparator()
    {
        if ( __length is 0 ) { return false; }

        char last = __buffer[__length - 1];
        return last is not ' ' and not '(' and not '.';
    }

    public void Word( scoped ReadOnlySpan<char> text )
    {
        if ( NeedsSeparator() ) { Append(' '); }

        Append(text);
    }
    public void Space() => Append(' ');
    public void Comma() => Append(',');
    public void Dot()   => Append('.');

    public void OpenParen()
    {
        if ( NeedsSeparator() ) { Append(' '); }

        Append('(');
        __depth++;
    }
    public void OpenCall()
    {
        Append('(');
        __depth++;
    }
    public void CloseParen()
    {
        Append(')');
        __depth--;
    }
    public void Star() => Word("*");

    public void MarkClause( ClauseFlags clause ) => __flags |= clause;

    public void Identifier( string         name ) => WriteIdentifier(SqlDialects.Fold(Dialect, name));
    public void PreparedIdentifier( string name ) => WriteIdentifier(name);

    private void WriteIdentifier( string folded )
    {
        if ( NeedsSeparator() ) { Append(' '); }

        ( char open, char close ) = SqlDialects.QuoteChars(Dialect);
        if ( open is '\0' )
        {
            Append(folded);
            return;
        }

        Append(open);
        foreach ( char c in folded )
        {
            if ( c == close ) { Append(close); }

            Append(c);
        }

        Append(close);
    }

    public void ColumnRef( string raw )
    {
        int dot = raw.IndexOf('.');
        if ( dot < 0 )
        {
            Identifier(raw);
            return;
        }

        if ( NeedsSeparator() ) { Append(' '); }

        int start = 0;
        while ( true )
        {
            int    next    = raw.IndexOf('.', start);
            string segment = next < 0
                                 ? raw[start..]
                                 : raw[start..next];
            WriteIdentifier(SqlDialects.Fold(Dialect, segment));
            if ( next < 0 ) { break; }

            Dot();
            start = next + 1;
        }
    }

    public void QualifiedColumn( string? qualifier, string columnName, bool alreadyFolded )
    {
        if ( qualifier is { Length: > 0 } )
        {
            Identifier(qualifier);
            Dot();
            if ( alreadyFolded ) { PreparedIdentifier(SqlDialects.Fold(Dialect, columnName)); }
            else { Identifier(columnName); }

            return;
        }

        if ( alreadyFolded ) { PreparedIdentifier(SqlDialects.Fold(Dialect, columnName)); }
        else { Identifier(columnName); }
    }

    public void Parameter( object? value )
    {
        int    ordinal = __parameters.Add(value);
        string token   = SqlDialects.ParameterName(Dialect, ordinal);
        Word(token);
    }
    public void InlineNull() => Word("NULL");
    public void InlineBoolean( bool value )
    {
        if ( SqlDialects.SupportsBooleanLiteral(Dialect) ) { Word(value ? "TRUE" : "FALSE"); }
        else { Word(value ? "1" : "0"); }
    }
    public void InlineNumber( long value )
    {
        Span<char> scratch = stackalloc char[20];
        value.TryFormat(scratch, out int written, provider: System.Globalization.CultureInfo.InvariantCulture);
        Word(scratch[..written]);
    }
    public void InlineString( scoped ReadOnlySpan<char> value )
    {
        if ( NeedsSeparator() ) { Append(' '); }

        Append('\'');
        foreach ( char c in value )
        {
            if ( c == '\'' ) { Append('\''); }

            Append(c);
        }

        Append('\'');
    }
    public void InlineRaw( scoped ReadOnlySpan<char> sql ) => Word(sql);

    public readonly string Snapshot() => new(__buffer, 0, __length);

    private readonly void Verify()
    {
        if ( __depth is not 0 ) { throw new SqlBuildException(SqlBuildError.UnbalancedParentheses, $"Unbalanced parentheses (depth {__depth}).", Snapshot(), Dialect); }
    }

    public SqlResult Build()
    {
        try
        {
            Verify();
            if ( AppendTerminator ) { Append(';'); }

            string          sql        = new(__buffer, 0, __length);
            SqlParameterSet parameters = __parameters.Build(Dialect);
            return new SqlResult(sql, parameters, Dialect);
        }
        finally
        {
            if ( __buffer.Length > 0 ) { ArrayPool<char>.Shared.Return(__buffer); }

            __buffer = [];
            __length = 0;
        }
    }
}
