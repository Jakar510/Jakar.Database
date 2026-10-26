// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

using System.Buffers;



namespace Jakar.SqlBuilder;


/// <summary> Row locking requested with <c>ForUpdate()</c> / <c>ForShare()</c>, written after paging. </summary>
[Flags]
public enum SqlLockMode : byte
{
    None       = 0,
    Update     = 1 << 0,
    Share      = 1 << 1,
    NoWait     = 1 << 2,
    SkipLocked = 1 << 3
}



/// <summary>
///     The single mutable building surface: a growable, pooled character buffer plus parenthesis depth, emitted-clause
///     flags and the bound-parameter collector. A <see langword="ref"/> <see langword="struct"/> carried by value through
///     the stages (mutate-then-forward); only the latest copy is read, so the shared backing arrays are safe.
///     <para>
///         Clauses that SQL places before something chosen later (SQL Server <c>OUTPUT</c>, <c>WITH RECURSIVE</c>, the qualifier of a typed projection, the <c>NULLS FIRST</c> emulation) are
///         inserted at a recorded position; <see cref="InsertAt"/> keeps every other recorded position in step.
///     </para>
/// </summary>
public ref struct SqlWriter
{
    private const int TABLE_POOL = 8;

    private char[]                 __buffer;
    private int                    __length;
    private int                    __depth;
    private int                    __openQuotes;
    private ClauseFlags            __flags;
    private ParameterCollector     __parameters;
    private long?                  __limit;
    private long?                  __offset;
    private bool                   __withTies;
    private bool                   __percent;
    private SqlLockMode            __lock;
    private int                    __outputAt;
    private int                    __withAt;
    private int                    __targetAt;
    private bool                   __recursive;
    private string?                __requireOrderBy;
    private int                    __orderKeyStart;
    private int                    __orderKeyEnd;
    private int                    __scope;
    private TableEntry[]?          __tables;
    private int                    __tableCount;
    private PendingQualifier[]?    __pending;
    private int                    __pendingCount;
    private ISqlColumn?            __comparedColumn;
    private readonly bool          __namedParameters;

    public readonly SqlDialectKind Dialect          { get; }
    public readonly bool           AppendTerminator { get; }
    public readonly bool           StrictTypes      { get; }
    public readonly ClauseFlags    Flags            => __flags;
    public readonly int            ParenthesisDepth => __depth;
    public readonly int            Length           => __length;
    public readonly int            ParameterCount   => __parameters.Count;


    public SqlWriter( SqlDialectKind dialect, SqlBuilderOptions options )
    {
        if ( dialect is SqlDialectKind.NotSet ) { throw new SqlBuildException(SqlBuildError.DialectNotSet, "A SQL dialect must be selected before building.", string.Empty, dialect); }

        __buffer          = ArrayPool<char>.Shared.Rent(Math.Max(options.InitialBufferSize, 16));
        __length          = 0;
        __depth           = 0;
        __openQuotes      = 0;
        __flags           = ClauseFlags.None;
        __parameters      = new ParameterCollector();
        __limit           = null;
        __offset          = null;
        __withTies        = false;
        __percent         = false;
        __lock            = SqlLockMode.None;
        __outputAt        = -1;
        __withAt          = -1;
        __targetAt        = -1;
        __recursive       = false;
        __requireOrderBy  = null;
        __orderKeyStart   = -1;
        __orderKeyEnd     = -1;
        __scope           = 0;
        __tables          = null;
        __tableCount      = 0;
        __pending         = null;
        __pendingCount    = 0;
        __comparedColumn  = null;
        __namedParameters = options.NamedPostgresParameters;
        Dialect           = dialect;
        AppendTerminator  = options.AppendStatementTerminator;
        StrictTypes       = options.StrictTypes;
    }


    // ---- buffer ----

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

    /// <summary> Writes the separator a following word needs, so the caller can record the exact position the word starts at. </summary>
    public void Separate()
    {
        if ( NeedsSeparator() ) { Append(' '); }
    }

    public void Word( scoped ReadOnlySpan<char> text )
    {
        Separate();
        Append(text);
    }
    public void Space() => Append(' ');
    public void Comma() => Append(',');
    public void Dot()   => Append('.');

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
    public void Star() => Word("*");

    public void MarkClause( ClauseFlags clause ) => __flags |= clause;
    public readonly bool Has( ClauseFlags clause ) => ( __flags & clause ) != 0;

    /// <summary> The text written since <paramref name="start"/>. </summary>
    public readonly string TextFrom( int start ) => new(__buffer, start, __length - start);


    /// <summary> Inserts <paramref name="text"/> at <paramref name="position"/>, shifting the text after it and every recorded position at or after it. </summary>
    public void InsertAt( int position, scoped ReadOnlySpan<char> text )
    {
        if ( text.IsEmpty ) { return; }

        if ( (uint)position > (uint)__length ) { throw new ArgumentOutOfRangeException(nameof(position)); }

        EnsureCapacity(text.Length);
        Array.Copy(__buffer, position, __buffer, position + text.Length, __length - position);
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
            if ( __pending![i].Position >= position ) { __pending[i].Position += shift; }
        }
    }

    /// <summary> Moves the text written since <paramref name="start"/> to <paramref name="position"/>. </summary>
    private void MoveTail( int start, int position )
    {
        int    length  = __length - start;
        char[] segment = ArrayPool<char>.Shared.Rent(length);

        try
        {
            Array.Copy(__buffer, start, segment, 0, length);
            __length = start;
            InsertAt(position, segment.AsSpan(0, length));
        }
        finally { ArrayPool<char>.Shared.Return(segment); }
    }


    // ---- identifiers ----

    /// <exception cref="ArgumentNullException"> <paramref name="name"/> is <see langword="null"/>. </exception>
    /// <exception cref="ArgumentException"> <paramref name="name"/> is empty. </exception>
    public void Identifier( string name ) => FoldedIdentifier(RequireName(name));
    public void PreparedIdentifier( string name ) => WriteIdentifier(RequireName(name));

    internal static string RequireName( string name, string? parameter = "name" )
    {
        ArgumentNullException.ThrowIfNull(name, parameter);
        if ( name.Length is 0 ) { throw new ArgumentException("An identifier can't be empty.", parameter); }

        return name;
    }

    /// <summary> Folds (PostgreSQL, through a cache, so no string is made) and writes one name part. </summary>
    private void FoldedIdentifier( scoped ReadOnlySpan<char> name )
    {
        if ( Dialect is SqlDialectKind.PostgreSql ) { WriteIdentifier(SqlDialects.FoldPostgres(name)); }
        else { WriteIdentifier(name); }
    }

    private void WriteIdentifier( scoped ReadOnlySpan<char> folded )
    {
        Separate();

        ( char open, char close ) = Dialect.QuoteChars();
        if ( open is '\0' )
        {
            Append(folded);
            return;
        }

        Append(open);
        __openQuotes++;
        foreach ( char c in folded )
        {
            if ( c == close ) { Append(close); }

            Append(c);
        }

        Append(close);
        __openQuotes--;
    }

    /// <summary> <paramref name="name"/> as the dialect writes it (folded and quoted), e.g. for a qualifier inserted later. </summary>
    public readonly string QuotedIdentifier( string name )
    {
        string folded = Dialect.Fold(RequireName(name));
        ( char open, char close ) = Dialect.QuoteChars();

        return open is '\0'
                   ? folded
                   : $"{open}{folded.Replace(close.ToString(), new string(close, 2), StringComparison.Ordinal)}{close}";
    }

    public void ColumnRef( string raw )
    {
        RequireName(raw, "column");
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
            ReadOnlySpan<char> segment = next < 0
                                             ? rest
                                             : rest[..next];
            if ( segment.IsEmpty ) { throw new ArgumentException($"'{raw}' has an empty name part.", "column"); }

            FoldedIdentifier(segment);
            if ( next < 0 ) { break; }

            Dot();
            rest = rest[( next + 1 )..];
        }
    }

    public void QualifiedColumn( string? qualifier, string columnName, bool alreadyFolded )
    {
        if ( qualifier is { Length: > 0 } )
        {
            Identifier(qualifier);
            Dot();
            if ( alreadyFolded ) { PreparedIdentifier(Dialect.Fold(columnName)); }
            else { Identifier(columnName); }

            return;
        }

        if ( alreadyFolded ) { PreparedIdentifier(Dialect.Fold(columnName)); }
        else { Identifier(columnName); }
    }

    /// <summary>
    ///     A typed column of <paramref name="table"/> written before its source is known (a projection): unqualified now, then given its alias, or its table name once the statement
    ///     joins tables, when the scope ends.
    /// </summary>
    public void DeferredColumn( string table, string columnName )
    {
        Separate();
        __pending ??= ArrayPool<PendingQualifier>.Shared.Rent(TABLE_POOL);
        if ( __pendingCount == __pending.Length ) { Grow(ref __pending); }

        __pending[__pendingCount++] = new PendingQualifier(__length, table, __scope);
        PreparedIdentifier(Dialect.Fold(columnName));
    }


    // ---- table scope: lets typed column references be qualified once a statement has aliases or joins ----

    /// <summary> Records a table of the current <c>SELECT</c> (<c>FROM</c> / <c>JOIN</c>) and its alias. </summary>
    public void RegisterTable( string table, string? alias )
    {
        __tables ??= ArrayPool<TableEntry>.Shared.Rent(TABLE_POOL);
        if ( __tableCount == __tables.Length ) { Grow(ref __tables); }

        __tables[__tableCount++] = new TableEntry(table, alias is { Length: > 0 } ? alias : null, __scope);
    }

    /// <summary> Starts the next operand of a set operation (<c>UNION</c>): its tables and joins are its own. </summary>
    public void ResetTables()
    {
        ResolvePending();

        while ( __tableCount > 0 && __tables![__tableCount - 1].Scope == __scope ) { __tableCount--; }

        __flags &= ~( ClauseFlags.Join | ClauseFlags.From );
    }

    /// <summary> The first table of the current query (a statement's target), or <see langword="null"/>. </summary>
    public readonly string? FirstTable
    {
        get
        {
            for ( int i = 0; i < __tableCount; i++ )
            {
                if ( __tables![i].Scope == __scope ) { return __tables[i].Table; }
            }

            return null;
        }
    }

    /// <summary> Whether <paramref name="table"/> is a source of the current query (not just an enclosing one). </summary>
    public readonly bool IsRegistered( string table )
    {
        for ( int i = __tableCount - 1; i >= 0; i-- )
        {
            TableEntry entry = __tables![i];
            if ( entry.Scope != __scope ) { return false; }

            if ( string.Equals(entry.Table, table, StringComparison.Ordinal) ) { return true; }
        }

        return false;
    }

    /// <summary>
    ///     The qualifier for a typed column of <paramref name="table"/>: its alias, or the table name when the statement joins tables (or the table belongs to an enclosing query);
    ///     otherwise none. The innermost match wins; a table joined twice resolves to its first alias in that scope.
    /// </summary>
    public readonly string? QualifierFor( string table )
    {
        int found = -1;

        for ( int i = __tableCount - 1; i >= 0; i-- )
        {
            TableEntry entry = __tables![i];
            if ( !string.Equals(entry.Table, table, StringComparison.Ordinal) ) { continue; }

            if ( found >= 0 && __tables[found].Scope != entry.Scope ) { break; }

            found = i;
        }

        if ( found < 0 ) { return null; }

        TableEntry match = __tables![found];
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
            PendingQualifier pending = __pending![i];
            if ( pending.Scope != __scope ) { break; }

            __pendingCount--;
            if ( QualifierFor(pending.Table) is { } qualifier ) { InsertAt(pending.Position, $"{QuotedIdentifier(qualifier)}."); }
        }
    }


    // ---- nested queries (subqueries, CTE bodies, derived tables) ----

    /// <summary> Starts a nested query: its tables, paging, locking and clause flags are its own until <see cref="EndScope"/>. </summary>
    public Scope BeginScope()
    {
        Scope saved = new(__flags, __limit, __offset, __withTies, __percent, __lock, __orderKeyStart, __orderKeyEnd, __comparedColumn, __requireOrderBy);
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

    /// <summary> Ends a nested query: writes its paging and locking, qualifies its typed projections, and restores the enclosing query's state. </summary>
    public void EndScope( Scope saved )
    {
        WritePagingAndLocking();
        ResolvePending();

        while ( __tableCount > 0 && __tables![__tableCount - 1].Scope == __scope ) { __tableCount--; }

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



    /// <summary> The enclosing query's state while a nested one is written. </summary>
    public readonly struct Scope( ClauseFlags flags, long? limit, long? offset, bool withTies, bool percent, SqlLockMode lockMode, int orderKeyStart, int orderKeyEnd, ISqlColumn? comparedColumn, string? requireOrderBy )
    {
        internal readonly ClauseFlags Flags          = flags;
        internal readonly long?       Limit          = limit;
        internal readonly long?       Offset         = offset;
        internal readonly bool        WithTies       = withTies;
        internal readonly bool        Percent        = percent;
        internal readonly SqlLockMode Lock           = lockMode;
        internal readonly int         OrderKeyStart  = orderKeyStart;
        internal readonly int         OrderKeyEnd    = orderKeyEnd;
        internal readonly ISqlColumn? ComparedColumn = comparedColumn;
        internal readonly string?     RequireOrderBy = requireOrderBy;
    }



    /// <exception cref="ArgumentException"> <paramref name="count"/> is zero: an empty list (columns, values, ...) can't be written. </exception>
    public readonly void RequireItems( int count, string what )
    {
        if ( count <= 0 ) { throw new ArgumentException($"{what} needs at least one item.", what); }
    }

    /// <exception cref="SqlBuildException"> <paramref name="supported"/> is <see langword="false"/> (<see cref="SqlBuildError.UnsupportedFeature"/>). </exception>
    public readonly void Require( bool supported, string feature )
    {
        if ( !supported ) { throw new SqlBuildException(SqlBuildError.UnsupportedFeature, $"{feature} is not supported by {Dialect}.", Snapshot(), Dialect); }
    }

    public readonly SqlBuildException Error( SqlBuildError reason, string message ) => new(reason, message, Snapshot(), Dialect);


    // ---- paging and locking: recorded here, written in the dialect's form when the (sub)query ends, so the call order doesn't matter ----

    public void SetLimit( long count )
    {
        __limit = ValidatePaging(count, "limit");
        __flags |= ClauseFlags.Limit;
    }
    public void SetOffset( long count )
    {
        __offset = ValidatePaging(count, "offset");
        __flags  |= ClauseFlags.Offset;
    }

    /// <summary> <c>FETCH NEXT n PERCENT</c> / <c>WITH TIES</c> (Oracle, PostgreSQL 13+ for ties); SQL Server writes them in <c>TOP</c>. </summary>
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

    private readonly long ValidatePaging( long count, string what ) => count >= 0
                                                                           ? count
                                                                           : throw Error(SqlBuildError.InvalidLiteral, $"The {what} can't be negative ({count}).");

    /// <summary> Makes the (sub)query fail to build without an <c>ORDER BY</c> (e.g. SQL Server <c>TOP ... WITH TIES</c>). </summary>
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


    // ---- SQL Server OUTPUT: it goes before VALUES / WHERE, but is chosen after them ----

    /// <summary> Records where an <c>OUTPUT</c> clause belongs (just before <c>VALUES</c> or <c>WHERE</c>); the first mark wins. </summary>
    public void MarkOutputPosition()
    {
        if ( __outputAt < 0 ) { __outputAt = __length; }
    }

    /// <summary> Moves the text written since <paramref name="start"/> to the recorded <c>OUTPUT</c> position, if there is one. </summary>
    public void MoveToOutputPosition( int start )
    {
        if ( __outputAt < 0 || __outputAt >= start ) { return; }

        MoveTail(start, __outputAt);
    }


    // ---- the statement's target (MySQL's multi-table UPDATE / DELETE add to it later) ----

    /// <summary> Records a position in the statement's target (e.g. after <c>DELETE</c>, or after <c>UPDATE t</c>). </summary>
    public void MarkTarget() => __targetAt = __length;

    /// <summary> Inserts <paramref name="text"/> at the recorded target position. </summary>
    public void InsertAtTarget( scoped ReadOnlySpan<char> text )
    {
        if ( __targetAt < 0 ) { throw Error(SqlBuildError.MissingRequiredClause, "No statement target was recorded."); }

        InsertAt(__targetAt, text);
    }


    // ---- WITH [RECURSIVE] ----

    /// <summary> Records the position just after <c>WITH</c>, where <c>RECURSIVE</c> goes once any CTE needs it. </summary>
    public void MarkWith()
    {
        __withAt    = __length;
        __recursive = false;
    }

    /// <summary> Writes <c>RECURSIVE</c> after <c>WITH</c> (PostgreSQL, SQLite, MySQL, Firebird; SQL Server and Oracle need no keyword), once. </summary>
    public void RequireRecursive()
    {
        if ( __recursive || __withAt < 0 ) { return; }

        __recursive = true;
        if ( Dialect.RecursiveKeywordRequired() ) { InsertAt(__withAt, " RECURSIVE"); }
    }


    // ---- ORDER BY keys (for the NULLS FIRST / LAST emulation) ----

    public void BeginOrderKey()
    {
        Separate();
        __orderKeyStart = __length;
    }
    public void EndOrderKey() => __orderKeyEnd = __length;

    /// <summary> <c>NULLS FIRST</c> / <c>NULLS LAST</c> for the last ORDER BY key; SQL Server and MySQL get an equivalent <c>CASE WHEN key IS NULL ...</c> sort key in front of it. </summary>
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

    /// <summary> The typed column the next comparison is about, checked by <see cref="CheckCompared"/> when <see cref="StrictTypes"/> is on. </summary>
    public void SetComparedColumn( ISqlColumn? column ) => __comparedColumn = column;

    /// <summary> With <see cref="StrictTypes"/>, rejects a value the compared typed column can't hold (<see cref="SqlBuildError.TypeMismatch"/>). </summary>
    public void CheckCompared<T>( T value, bool isNullCheck = false )
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

    /// <summary> <see cref="CheckCompared{T}"/> for a <see cref="SqlValue"/>, reading its value only when it will be checked. </summary>
    public void CheckCompared( in SqlValue value )
    {
        if ( StrictTypes && __comparedColumn is not null ) { CheckCompared(value.ComparableValue); }
        else { __comparedColumn = null; }
    }

    /// <summary> Forgets the compared typed column (the right-hand side isn't a value, e.g. another column). </summary>
    public void ClearCompared() => __comparedColumn = null;


    // ---- values ----

    public void Parameter( object? value )
    {
        int    ordinal = __parameters.Add(value);
        string token   = Dialect.ParameterName(ordinal, __namedParameters);
        Word(token);
    }
    public void InlineNull() => Word("NULL");
    public void InlineBoolean( bool value )
    {
        if ( Dialect.SupportsBooleanLiteral() ) { Word(value ? "TRUE" : "FALSE"); }
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
        Separate();

        // SQL Server needs N'...' to keep characters outside the database's code page.
        if ( Dialect is SqlDialectKind.SqlServer && !Ascii.IsValid(value) ) { Append('N'); }

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


    // ---- terminals ----

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

    private void Finish()
    {
        WritePagingAndLocking();
        ResolvePending();
        Verify();
        // SQL Server requires MERGE to be terminated.
        if ( ( AppendTerminator || ( Has(ClauseFlags.Merge) && Dialect is SqlDialectKind.SqlServer ) ) && Dialect.SupportsStatementTerminator() ) { Append(';'); }
    }

    private void Release()
    {
        if ( __buffer.Length > 0 ) { ArrayPool<char>.Shared.Return(__buffer); }

        if ( __tables is not null ) { ArrayPool<TableEntry>.Shared.Return(__tables, true); }

        if ( __pending is not null ) { ArrayPool<PendingQualifier>.Shared.Return(__pending, true); }

        __parameters.Release();
        __buffer  = [];
        __tables  = null;
        __pending = null;
        __length  = 0;
    }

    public SqlResult Build()
    {
        try
        {
            Finish();
            string          sql        = new(__buffer, 0, __length);
            SqlParameterSet parameters = __parameters.Build(Dialect, __namedParameters);
            return new SqlResult(sql, parameters, Dialect);
        }
        finally { Release(); }
    }

    /// <summary> The SQL text only; the parameters are discarded. </summary>
    public string BuildSql()
    {
        try
        {
            Finish();
            return new string(__buffer, 0, __length);
        }
        finally { Release(); }
    }

    /// <summary> Writes the SQL straight into <paramref name="destination"/> (no string is created) and returns the bound parameters (<see cref="SqlParameterSet.Empty"/> when there are none). </summary>
    public SqlParameterSet WriteTo( IBufferWriter<char> destination )
    {
        ArgumentNullException.ThrowIfNull(destination);

        try
        {
            Finish();
            Span<char> target = destination.GetSpan(__length);
            __buffer.AsSpan(0, __length).CopyTo(target);
            destination.Advance(__length);
            return __parameters.Build(Dialect, __namedParameters);
        }
        finally { Release(); }
    }


    private static void Grow<T>( ref T[] array )
    {
        T[] grown = ArrayPool<T>.Shared.Rent(array.Length * 2);
        Array.Copy(array, grown, array.Length);
        ArrayPool<T>.Shared.Return(array, true);
        array = grown;
    }



    internal readonly record struct TableEntry( string Table, string? Alias, int Scope );



    internal record struct PendingQualifier( int Position, string Table, int Scope );
}
