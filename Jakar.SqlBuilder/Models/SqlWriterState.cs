// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 10/25/2026

using System.Globalization;
using System.Runtime.CompilerServices;



namespace Jakar.SqlBuilder;


/// <summary>
///     Everything a statement being built owns: the pooled character buffer, parenthesis and quote depth, emitted-clause flags, bound parameters, paging and locking, recorded
///     positions and the table-alias registry. One instance per statement, rented per thread and reused, so building allocates nothing but the result.
///     <see cref="SqlWriter"/> is the handle the stages carry: it checks <see cref="Generation"/> (which use of this instance it belongs to) and <see cref="Version"/> (which stage is
///     current) before every write, so a stale stage can't write into a statement that moved on, or into another statement that reused the instance.
/// </summary>
internal sealed class SqlWriterState
{
    private const int TABLE_POOL       = 8;
    private const int KEEP_BUFFER_MAX  = 16 * 1024; // larger buffers go back to the pool rather than staying with the thread
    private const int THREAD_POOL_SIZE = 4;         // statements built at once on one thread (a builder used inside another's lambda)

    [ThreadStatic] private static SqlWriterState?[]? __pool;
    [ThreadStatic] private static int                __pooled;

    private char[]             __buffer = [];
    private int                __length;
    private int                __depth;
    private int                __openQuotes;
    private ClauseFlags        __flags;
    private ParameterCollector __parameters = new();
    private long?              __limit;
    private long?              __offset;
    private bool               __withTies;
    private bool               __percent;
    private SqlLockMode        __lock;
    private int                __outputAt;
    private int                __withAt;
    private int                __targetAt;
    private bool               __recursive;
    private string?            __requireOrderBy;
    private int                __orderKeyStart;
    private int                __orderKeyEnd;
    private int                __scope;
    private TableEntry[]       __tables  = [];
    private int                __tableCount;
    private PendingQualifier[] __pending = [];
    private int                __pendingCount;
    private ISqlColumn?        __comparedColumn;
    private bool               __namedParameters;
    private SqlLayout          __layout;
    private char               __open;
    private char               __close;

    /// <summary> Incremented by every write; a <see cref="SqlWriter"/> copy may write only while it holds the current value. </summary>
    internal int Version;

    /// <summary> Incremented every time the instance is released, so handles from an earlier statement can't touch the next one. </summary>
    internal int Generation;

    internal bool InUse;

    public SqlDialectKind Dialect          { get; private set; }
    public bool           AppendTerminator { get; private set; }
    public bool           StrictTypes      { get; private set; }
    public ClauseFlags    Flags            => __flags;
    public int            ParenthesisDepth => __depth;
    public int            Length           => __length;
    public int            ParameterCount   => __parameters.Count;


    // ---- rent / release ----

    public static SqlWriterState Rent( SqlDialectKind dialect, SqlBuilderOptions options )
    {
        SqlWriterState?[] pool  = __pool ??= new SqlWriterState?[THREAD_POOL_SIZE];
        SqlWriterState    state = __pooled > 0 ? pool[--__pooled]! : new SqlWriterState();
        pool[__pooled] = null;

        state.Start(dialect, options);
        return state;
    }

    private void Start( SqlDialectKind dialect, SqlBuilderOptions options )
    {
        int capacity = Math.Max(options.InitialBufferSize, 16);
        if ( __buffer.Length < capacity )
        {
            if ( __buffer.Length > 0 ) { ArrayPool<char>.Shared.Return(__buffer); }

            __buffer = ArrayPool<char>.Shared.Rent(capacity);
        }

        InUse            = true;
        Dialect          = dialect;
        AppendTerminator = options.AppendStatementTerminator;
        StrictTypes      = options.StrictTypes;
        __namedParameters = options.NamedPostgresParameters;
        __layout          = options.Layout;
        ( __open, __close ) = dialect.QuoteChars();
        __outputAt       = -1;
        __withAt         = -1;
        __targetAt       = -1;
        __orderKeyStart  = -1;
        __orderKeyEnd    = -1;
    }

    /// <summary> Ends the statement: every handle to it becomes stale, references are cleared, and the instance goes back to this thread's pool (or a too-large buffer to the shared pool). </summary>
    public void Release()
    {
        Generation++;
        Version++;
        InUse = false;

        __parameters.Reset();
        if ( __tableCount > 0 ) { Array.Clear(__tables, 0, __tableCount); }

        if ( __pendingCount > 0 ) { Array.Clear(__pending, 0, __pendingCount); }

        if ( __buffer.Length > KEEP_BUFFER_MAX )
        {
            ArrayPool<char>.Shared.Return(__buffer);
            __buffer = [];
        }

        __length         = 0;
        __depth          = 0;
        __openQuotes     = 0;
        __flags          = ClauseFlags.None;
        __limit          = null;
        __offset         = null;
        __withTies       = false;
        __percent        = false;
        __lock           = SqlLockMode.None;
        __recursive      = false;
        __requireOrderBy = null;
        __scope          = 0;
        __tableCount     = 0;
        __pendingCount   = 0;
        __comparedColumn = null;

        SqlWriterState?[] pool = __pool ??= new SqlWriterState?[THREAD_POOL_SIZE];
        if ( __pooled < pool.Length ) { pool[__pooled++] = this; }
    }


    // ---- buffer ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity( int additional )
    {
        if ( (uint)( __length + additional ) > (uint)__buffer.Length ) { Grow(additional); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow( int additional )
    {
        int    target = Math.Max(Math.Max(__buffer.Length * 2, __length + additional), 16);
        char[] grown  = ArrayPool<char>.Shared.Rent(target);
        __buffer.AsSpan(0, __length).CopyTo(grown);
        if ( __buffer.Length > 0 ) { ArrayPool<char>.Shared.Return(__buffer); }

        __buffer = grown;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append( char c )
    {
        if ( (uint)__length >= (uint)__buffer.Length ) { Grow(1); }

        __buffer[__length++] = c;
    }

    public void Append( scoped ReadOnlySpan<char> text )
    {
        if ( text.IsEmpty ) { return; }

        EnsureCapacity(text.Length);
        text.CopyTo(__buffer.AsSpan(__length));
        __length += text.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Separate()
    {
        if ( __length is 0 ) { return; }

        char last = __buffer[__length - 1];
        if ( last is not ' ' and not '(' and not '.' ) { Append(' '); }
    }

    public void Word( scoped ReadOnlySpan<char> text )
    {
        Separate();
        Append(text);
    }

    public void OpenParen()
    {
        Separate();
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

    public void MarkClause( ClauseFlags clause ) => __flags |= clause;
    public bool Has( ClauseFlags        clause ) => ( __flags & clause ) != 0;

    public string TextFrom( int start ) => new(__buffer, start, __length - start);
    public string Snapshot()            => new(__buffer, 0, __length);
    public ReadOnlySpan<char> Text => __buffer.AsSpan(0, __length);


    public void InsertAt( int position, scoped ReadOnlySpan<char> text )
    {
        if ( text.IsEmpty ) { return; }

        if ( (uint)position > (uint)__length ) { throw new ArgumentOutOfRangeException(nameof(position)); }

        EnsureCapacity(text.Length);
        __buffer.AsSpan(position, __length - position).CopyTo(__buffer.AsSpan(position + text.Length));
        text.CopyTo(__buffer.AsSpan(position));
        __length += text.Length;

        int shift = text.Length;
        if ( __outputAt      >= position ) { __outputAt      += shift; }
        if ( __withAt        >= position ) { __withAt        += shift; }
        if ( __targetAt      >= position ) { __targetAt      += shift; }
        if ( __orderKeyStart >= position ) { __orderKeyStart += shift; }
        if ( __orderKeyEnd   >= position ) { __orderKeyEnd   += shift; }

        for ( int i = 0; i < __pendingCount; i++ )
        {
            if ( __pending[i].Position >= position ) { __pending[i].Position += shift; }
        }
    }

    private void MoveTail( int start, int position )
    {
        int    length  = __length - start;
        char[] segment = ArrayPool<char>.Shared.Rent(length);

        try
        {
            __buffer.AsSpan(start, length).CopyTo(segment);
            __length = start;
            InsertAt(position, segment.AsSpan(0, length));
        }
        finally { ArrayPool<char>.Shared.Return(segment); }
    }


    // ---- identifiers ----

    public void Identifier( string name ) => WriteIdentifier(Dialect is SqlDialectKind.PostgreSql ? SqlDialects.FoldPostgres(name) : name);

    /// <summary> Folds (PostgreSQL, through a cache, so no string is made) and writes one name part. </summary>
    private void FoldedIdentifier( scoped ReadOnlySpan<char> name )
    {
        if ( Dialect is SqlDialectKind.PostgreSql ) { WriteIdentifier(SqlDialects.FoldPostgres(name)); }
        else { WriteIdentifier(name); }
    }

    public void WriteIdentifier( scoped ReadOnlySpan<char> folded )
    {
        Separate();

        char open = __open;
        if ( open is '\0' )
        {
            Append(folded);
            return;
        }

        char close = __close;
        EnsureCapacity(folded.Length + 2);
        __buffer[__length++] = open;
        __openQuotes++;

        // A closing quote inside the name is doubled; names almost never have one, so copy the rest in bulk.
        int index;
        while ( ( index = folded.IndexOf(close) ) >= 0 )
        {
            Append(folded[..( index + 1 )]);
            Append(close);
            folded = folded[( index + 1 )..];
        }

        Append(folded);
        Append(close);
        __openQuotes--;
    }

    public string QuotedIdentifier( string name )
    {
        string folded = Dialect.Fold(SqlWriter.RequireName(name));
        if ( __open is '\0' ) { return folded; }

        int extra = folded.AsSpan().Count(__close);
        return string.Create(folded.Length + extra + 2,
                             ( folded, __open, __close ),
                             static ( span, state ) =>
                             {
                                 span[0]  = state.__open;
                                 span[^1] = state.__close;
                                 int written = 1;
                                 foreach ( char c in state.folded )
                                 {
                                     span[written++] = c;
                                     if ( c == state.__close ) { span[written++] = c; }
                                 }
                             });
    }

    public void ColumnRef( string raw )
    {
        SqlWriter.RequireName(raw, "column");
        int dot = raw.IndexOf('.');
        if ( dot < 0 )
        {
            Identifier(raw);
            return;
        }

        Separate();

        ReadOnlySpan<char> rest = raw;
        while ( true )
        {
            int                next    = rest.IndexOf('.');
            ReadOnlySpan<char> segment = next < 0 ? rest : rest[..next];
            if ( segment.IsEmpty ) { throw new ArgumentException($"'{raw}' has an empty name part.", "column"); }

            FoldedIdentifier(segment);
            if ( next < 0 ) { break; }

            Append('.');
            rest = rest[( next + 1 )..];
        }
    }

    public void QualifiedColumn( string? qualifier, string columnName, bool alreadyFolded )
    {
        if ( qualifier is { Length: > 0 } )
        {
            Identifier(qualifier);
            Append('.');
        }

        if ( alreadyFolded ) { WriteIdentifier(Dialect.Fold(columnName)); }
        else { Identifier(columnName); }
    }

    public void DeferredColumn( string table, string columnName )
    {
        Separate();
        if ( __pendingCount == __pending.Length ) { GrowArray(ref __pending); }

        __pending[__pendingCount++] = new PendingQualifier(__length, table, __scope);
        WriteIdentifier(Dialect.Fold(columnName));
    }


    // ---- table scope ----

    public void RegisterTable( string table, string? alias )
    {
        if ( __tableCount == __tables.Length ) { GrowArray(ref __tables); }

        __tables[__tableCount++] = new TableEntry(table, alias is { Length: > 0 } ? alias : null, __scope);
    }

    public void ResetTables()
    {
        ResolvePending();

        while ( __tableCount > 0 && __tables[__tableCount - 1].Scope == __scope ) { __tables[--__tableCount] = default; }

        __flags &= ~( ClauseFlags.Join | ClauseFlags.From );
    }

    public string? FirstTable
    {
        get
        {
            for ( int i = 0; i < __tableCount; i++ )
            {
                if ( __tables[i].Scope == __scope ) { return __tables[i].Table; }
            }

            return null;
        }
    }

    public bool IsRegistered( string table )
    {
        for ( int i = __tableCount - 1; i >= 0; i-- )
        {
            ref readonly TableEntry entry = ref __tables[i];
            if ( entry.Scope != __scope ) { return false; }

            if ( string.Equals(entry.Table, table, StringComparison.Ordinal) ) { return true; }
        }

        return false;
    }

    public string? QualifierFor( string table )
    {
        int found = -1;

        for ( int i = __tableCount - 1; i >= 0; i-- )
        {
            ref readonly TableEntry entry = ref __tables[i];
            if ( !string.Equals(entry.Table, table, StringComparison.Ordinal) ) { continue; }

            if ( found >= 0 && __tables[found].Scope != entry.Scope ) { break; }

            found = i;
        }

        if ( found < 0 ) { return null; }

        ref readonly TableEntry match = ref __tables[found];
        if ( match.Alias is not null ) { return match.Alias; }

        return match.Scope != __scope || ( __flags & ClauseFlags.Join ) != 0
                   ? match.Table
                   : null;
    }

    private void ResolvePending()
    {
        // Back to front: an insertion only moves the positions after it, which are already done.
        for ( int i = __pendingCount - 1; i >= 0; i-- )
        {
            PendingQualifier pending = __pending[i];
            if ( pending.Scope != __scope ) { break; }

            __pending[--__pendingCount] = default;
            if ( QualifierFor(pending.Table) is not { } qualifier ) { continue; }

            // "<quoted qualifier>." written at the projection's position.
            string quoted = QuotedIdentifier(qualifier);
            Span<char> text = quoted.Length < 128 ? stackalloc char[quoted.Length + 1] : new char[quoted.Length + 1];
            quoted.CopyTo(text);
            text[^1] = '.';
            InsertAt(pending.Position, text);
        }
    }


    // ---- nested queries ----

    public SqlWriter.Scope BeginScope()
    {
        SqlWriter.Scope saved = new(__flags, __limit, __offset, __withTies, __percent, __lock, __orderKeyStart, __orderKeyEnd, __comparedColumn, __requireOrderBy);
        __scope++;
        __flags          = ClauseFlags.None;
        __limit          = null;
        __offset         = null;
        __withTies       = false;
        __percent        = false;
        __lock           = SqlLockMode.None;
        __orderKeyStart  = -1;
        __orderKeyEnd    = -1;
        __comparedColumn = null;
        __requireOrderBy = null;
        return saved;
    }

    public void EndScope( in SqlWriter.Scope saved )
    {
        WritePagingAndLocking();
        ResolvePending();

        while ( __tableCount > 0 && __tables[__tableCount - 1].Scope == __scope ) { __tables[--__tableCount] = default; }

        __scope--;
        __flags          = saved.Flags;
        __limit          = saved.Limit;
        __offset         = saved.Offset;
        __withTies       = saved.WithTies;
        __percent        = saved.Percent;
        __lock           = saved.Lock;
        __orderKeyStart  = saved.OrderKeyStart;
        __orderKeyEnd    = saved.OrderKeyEnd;
        __comparedColumn = saved.ComparedColumn;
        __requireOrderBy = saved.RequireOrderBy;
    }


    // ---- requirements ----

    public void Require( bool supported, string feature )
    {
        if ( !supported ) { throw new SqlBuildException(SqlBuildError.UnsupportedFeature, $"{feature} is not supported by {Dialect}.", Snapshot(), Dialect); }
    }

    public SqlBuildException Error( SqlBuildError reason, string message ) => new(reason, message, Snapshot(), Dialect);


    // ---- paging and locking ----

    public void SetLimit( long count )
    {
        __limit =  ValidatePaging(count, "limit");
        __flags |= ClauseFlags.Limit;
    }
    public void SetOffset( long count )
    {
        __offset = ValidatePaging(count, "offset");
        __flags  |= ClauseFlags.Offset;
    }

    public void SetLimitOptions( bool percent, bool withTies )
    {
        __percent  |= percent;
        __withTies |= withTies;
    }

    public void SetLock( SqlLockMode mode )
    {
        if ( ( mode & ( SqlLockMode.Update | SqlLockMode.Share ) ) != 0 ) { __lock = ( __lock & ~( SqlLockMode.Update | SqlLockMode.Share ) ) | mode; }
        else { __lock |= mode; }
    }

    private long ValidatePaging( long count, string what ) => count >= 0
                                                                  ? count
                                                                  : throw Error(SqlBuildError.InvalidLiteral, $"The {what} can't be negative ({count}).");

    public void RequireOrderBy( string feature ) => __requireOrderBy = feature;

    private void WritePagingAndLocking()
    {
        if ( __requireOrderBy is not null && !Has(ClauseFlags.OrderBy) ) { throw Error(SqlBuildError.MissingRequiredClause, $"{__requireOrderBy} needs an ORDER BY."); }

        WritePaging();
        WriteLocking();
    }

    private void WritePaging()
    {
        if ( __limit is null && __offset is null )
        {
            if ( __percent || __withTies ) { throw Error(SqlBuildError.MissingRequiredClause, "PERCENT / WITH TIES need a row limit."); }

            return;
        }

        if ( __withTies && !Has(ClauseFlags.OrderBy) ) { throw Error(SqlBuildError.MissingRequiredClause, "WITH TIES needs an ORDER BY."); }

        switch ( Dialect )
        {
            case SqlDialectKind.SqlServer:
                if ( Has(ClauseFlags.Top) ) { throw Error(SqlBuildError.UnsupportedFeature, "SQL Server can't combine TOP with OFFSET / FETCH."); }

                if ( !Has(ClauseFlags.OrderBy) ) { throw Error(SqlBuildError.MissingRequiredClause, "SQL Server paging (OFFSET / FETCH) needs an ORDER BY."); }

                OffsetRows(__offset ?? 0);
                FetchRows();
                return;

            case SqlDialectKind.Oracle:
                if ( __offset is { } offset ) { OffsetRows(offset); }

                FetchRows();
                return;

            case SqlDialectKind.Firebird:
                Require(!__percent && !__withTies, "FETCH ... PERCENT / WITH TIES");
                if ( __offset is { } skip ) { OffsetRows(skip); }

                FetchRows();
                return;

            case SqlDialectKind.PostgreSql when __withTies:
                Require(!__percent, "FETCH ... PERCENT");
                if ( __offset is { } rows ) { OffsetRows(rows); }

                FetchRows();
                return;

            case SqlDialectKind.Sqlite or SqlDialectKind.MySql:
                Require(!__percent && !__withTies, "LIMIT ... PERCENT / WITH TIES");

                // Both need LIMIT before OFFSET, and LIMIT for any OFFSET: an offset alone gets the dialect's "no limit".
                Word("LIMIT");
                if ( __limit is { } limit ) { InlineNumber(limit); }
                else { Word(Dialect is SqlDialectKind.Sqlite ? "-1" : "18446744073709551615"); }

                if ( __offset is { } skipped )
                {
                    Word("OFFSET");
                    InlineNumber(skipped);
                }

                return;

            default:
                Require(!__percent, "LIMIT ... PERCENT");
                if ( __limit is { } count )
                {
                    Word("LIMIT");
                    InlineNumber(count);
                }

                if ( __offset is { } skipCount )
                {
                    Word("OFFSET");
                    InlineNumber(skipCount);
                }

                return;
        }
    }
    private void OffsetRows( long offset )
    {
        Word("OFFSET");
        InlineNumber(offset);
        Word("ROWS");
    }
    private void FetchRows()
    {
        if ( __limit is not { } count ) { return; }

        Word("FETCH NEXT");
        InlineNumber(count);
        if ( __percent ) { Word("PERCENT"); }

        Word(__withTies ? "ROWS WITH TIES" : "ROWS ONLY");
    }

    private void WriteLocking()
    {
        if ( __lock is SqlLockMode.None ) { return; }

        bool share      = ( __lock & SqlLockMode.Share )      != 0;
        bool noWait     = ( __lock & SqlLockMode.NoWait )     != 0;
        bool skipLocked = ( __lock & SqlLockMode.SkipLocked ) != 0;

        if ( ( __lock & ( SqlLockMode.Update | SqlLockMode.Share ) ) == 0 ) { throw Error(SqlBuildError.MissingRequiredClause, "NOWAIT / SKIP LOCKED need ForUpdate() or ForShare()."); }

        if ( noWait && skipLocked ) { throw Error(SqlBuildError.InvalidLiteral, "NOWAIT and SKIP LOCKED can't be combined."); }

        Require(Dialect.SupportsRowLocking(), "SELECT ... FOR UPDATE (use table hints on SQL Server)");

        switch ( Dialect )
        {
            case SqlDialectKind.Oracle:
                Require(!share, "FOR SHARE");
                if ( __limit is not null || __offset is not null ) { throw Error(SqlBuildError.UnsupportedFeature, "Oracle can't combine FOR UPDATE with OFFSET / FETCH."); }

                break;

            case SqlDialectKind.Firebird:
                Require(!share,  "FOR SHARE");
                Require(!noWait, "NOWAIT (set it on the transaction)");
                Word("FOR UPDATE WITH LOCK");
                if ( skipLocked ) { Word("SKIP LOCKED"); }

                return;
        }

        Word(share ? "FOR SHARE" : "FOR UPDATE");
        if ( noWait ) { Word("NOWAIT"); }

        if ( skipLocked ) { Word("SKIP LOCKED"); }
    }


    // ---- recorded positions ----

    public void MarkOutputPosition()
    {
        if ( __outputAt < 0 ) { __outputAt = __length; }
    }

    public void MoveToOutputPosition( int start )
    {
        if ( __outputAt < 0 || __outputAt >= start ) { return; }

        MoveTail(start, __outputAt);
    }

    public void MarkTarget() => __targetAt = __length;

    public void InsertAtTarget( scoped ReadOnlySpan<char> text )
    {
        if ( __targetAt < 0 ) { throw Error(SqlBuildError.MissingRequiredClause, "No statement target was recorded."); }

        InsertAt(__targetAt, text);
    }

    public void MarkWith()
    {
        __withAt    = __length;
        __recursive = false;
    }

    public void RequireRecursive()
    {
        if ( __recursive || __withAt < 0 ) { return; }

        __recursive = true;
        if ( Dialect.RecursiveKeywordRequired() ) { InsertAt(__withAt, " RECURSIVE"); }
    }

    public void BeginOrderKey()
    {
        Separate();
        __orderKeyStart = __length;
    }
    public void EndOrderKey() => __orderKeyEnd = __length;

    public void NullsOrder( bool first )
    {
        if ( __orderKeyStart < 0 || __orderKeyEnd < __orderKeyStart ) { throw Error(SqlBuildError.MissingRequiredClause, "NULLS FIRST / LAST needs an ORDER BY key."); }

        if ( Dialect.SupportsNullsOrdering() )
        {
            Word(first ? "NULLS FIRST" : "NULLS LAST");
            return;
        }

        string key = new(__buffer, __orderKeyStart, __orderKeyEnd - __orderKeyStart);
        InsertAt(__orderKeyStart, $"CASE WHEN {key} IS NULL THEN {( first ? 0 : 1 )} ELSE {( first ? 1 : 0 )} END, ");
    }


    // ---- typed comparisons (StrictTypes) ----

    public void SetComparedColumn( ISqlColumn? column ) => __comparedColumn = column;

    public void CheckCompared<T>( T value, bool isNullCheck )
    {
        ISqlColumn? column = __comparedColumn;
        __comparedColumn = null;
        if ( !StrictTypes || column is null ) { return; }

        if ( isNullCheck )
        {
            if ( !column.IsNullable ) { throw Error(SqlBuildError.TypeMismatch, $"'{column.PropertyName}' is not nullable, so IS NULL is always false."); }

            return;
        }

        if ( !SqlTypeCheck.IsCompatible(column.ClrType, value) ) { throw Error(SqlBuildError.TypeMismatch, $"'{column.PropertyName}' is {column.ClrType.Name}; a {value?.GetType().Name ?? "null"} value can't be compared with it."); }
    }

    public void CheckCompared( in SqlValue value )
    {
        if ( StrictTypes && __comparedColumn is not null ) { CheckCompared(value.ComparableValue, false); }
        else { __comparedColumn = null; }
    }

    /// <summary> <see cref="CheckCompared{T}"/> for every value of an <c>IN</c> list. </summary>
    public void CheckComparedAll<T>( scoped ReadOnlySpan<T> values )
    {
        ISqlColumn? column = __comparedColumn;
        __comparedColumn = null;
        if ( !StrictTypes || column is null ) { return; }

        foreach ( T value in values )
        {
            if ( !SqlTypeCheck.IsCompatible(column.ClrType, value) ) { throw Error(SqlBuildError.TypeMismatch, $"'{column.PropertyName}' is {column.ClrType.Name}; a {value?.GetType().Name ?? "null"} value can't be compared with it."); }
        }
    }

    public void ClearCompared() => __comparedColumn = null;


    // ---- values ----

    public void Parameter( object? value )
    {
        int ordinal = __parameters.Add(value);
        Word(Dialect.ParameterName(ordinal, __namedParameters));
    }

    public void InlineBoolean( bool value )
    {
        if ( Dialect.SupportsBooleanLiteral() ) { Word(value ? "TRUE" : "FALSE"); }
        else { Word(value ? "1" : "0"); }
    }

    public void InlineNumber( long value )
    {
        Separate();
        EnsureCapacity(20);
        value.TryFormat(__buffer.AsSpan(__length), out int written, provider: CultureInfo.InvariantCulture);
        __length += written;
    }

    public void InlineString( scoped ReadOnlySpan<char> value )
    {
        Separate();

        // SQL Server needs N'...' to keep characters outside the database's code page.
        if ( Dialect is SqlDialectKind.SqlServer && !Ascii.IsValid(value) ) { Append('N'); }

        EnsureCapacity(value.Length + 2);
        Append('\'');

        int index;
        while ( ( index = value.IndexOf('\'') ) >= 0 )
        {
            Append(value[..( index + 1 )]);
            Append('\'');
            value = value[( index + 1 )..];
        }

        Append(value);
        Append('\'');
    }


    // ---- finishing ----

    private void Verify()
    {
        if ( __depth is not 0 ) { throw Error(SqlBuildError.UnbalancedParentheses, $"Unbalanced parentheses (depth {__depth})."); }

        if ( __openQuotes is not 0 ) { throw Error(SqlBuildError.UnclosedIdentifier, "An identifier was opened but not closed."); }

        if ( __scope is not 0 ) { throw Error(SqlBuildError.UnbalancedParentheses, "A nested query was not closed."); }

        if ( !__parameters.IsContiguous ) { throw Error(SqlBuildError.ParameterGap, "Parameter ordinals are not contiguous."); }

        if ( Has(ClauseFlags.Insert) && !Has(ClauseFlags.Values) ) { throw Error(SqlBuildError.MissingRequiredClause, "INSERT needs VALUES, a SELECT, or DEFAULT VALUES."); }

        if ( Has(ClauseFlags.Update) && !Has(ClauseFlags.Set) ) { throw Error(SqlBuildError.MissingRequiredClause, "UPDATE needs SET."); }

        if ( Has(ClauseFlags.With) && !Has(ClauseFlags.Select | ClauseFlags.Insert | ClauseFlags.Update | ClauseFlags.Delete | ClauseFlags.Merge) ) { throw Error(SqlBuildError.MissingRequiredClause, "WITH needs a statement."); }

        if ( Has(ClauseFlags.Ddl) && __parameters.Count > 0 ) { throw Error(SqlBuildError.InvalidLiteral, "DDL can't use bound parameters; use inline values."); }
    }

    /// <summary> Writes the deferred paging and locking, qualifies typed projections, verifies the statement and appends the terminator. </summary>
    public void Finish()
    {
        WritePagingAndLocking();
        ResolvePending();
        Verify();
        // SQL Server requires MERGE to be terminated.
        if ( ( AppendTerminator || ( Has(ClauseFlags.Merge) && Dialect is SqlDialectKind.SqlServer ) ) && Dialect.SupportsStatementTerminator() ) { Append(';'); }

        if ( __layout is SqlLayout.Readable ) { Relayout(); }
    }

    /// <summary> Rewrites the finished statement in <see cref="SqlLayout.Readable"/> form (whitespace only; see <see cref="SqlFormatter"/>). </summary>
    private void Relayout()
    {
        ReadOnlySpan<char> formatted = SqlFormatter.Format(Text, Dialect); // the formatter's own scratch, so the buffer can be rewritten from it
        __length = 0;
        Append(formatted);
    }

    public SqlParameterSet BuildParameters() => __parameters.Build(Dialect, __namedParameters);


    private static void GrowArray<T>( ref T[] array )
    {
        T[] grown = new T[Math.Max(array.Length * 2, TABLE_POOL)];
        Array.Copy(array, grown, array.Length);
        array = grown;
    }



    internal readonly record struct TableEntry( string Table, string? Alias, int Scope );



    internal record struct PendingQualifier( int Position, string Table, int Scope );
}
