// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;



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
///     The handle every stage carries to the statement being built. The statement itself — the pooled buffer, flags, parameters, paging, recorded positions — lives in one pooled
///     <see cref="SqlWriterState"/>, so passing a stage along copies only this handle.
///     <para>
///         Stages are single-use: every write advances a version shared with the state, so continuing a stage that a later stage has already moved past, or one whose statement was
///         streamed by <see cref="WriteTo"/>, throws <see cref="InvalidOperationException"/> instead of writing into a buffer another stage (or another statement) owns.
///         <see cref="Build"/> and <see cref="BuildSql"/> cache their result on the stage, so building the same stage again returns the same result.
///     </para>
///     <para>
///         Clauses that SQL places before something chosen later (SQL Server <c>OUTPUT</c>, <c>WITH RECURSIVE</c>, the qualifier of a typed projection, the <c>NULLS FIRST</c> emulation) are
///         inserted at a recorded position; <see cref="InsertAt"/> keeps every other recorded position in step.
///     </para>
/// </summary>
public ref struct SqlWriter
{
    private readonly SqlWriterState __state;
    private readonly int            __generation;
    private          int            __version;
    private          SqlResult?     __built;


    public SqlWriter( SqlDialectKind dialect, SqlBuilderOptions options )
    {
        if ( dialect is SqlDialectKind.NotSet ) { throw new SqlBuildException(SqlBuildError.DialectNotSet, "A SQL dialect must be selected before building.", string.Empty, dialect); }

        ArgumentNullException.ThrowIfNull(options);
        __state      = SqlWriterState.Rent(dialect, options);
        __generation = __state.Generation;
        __version    = __state.Version;
        __built      = null;
    }


    /// <summary> The state, for a write: this stage must be the current one; afterwards it (and only it) still is. </summary>
    private SqlWriterState __WriteState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] get
        {
            SqlWriterState state = __state;
            if ( state.Version != __version || state.Generation != __generation ) { ThrowStale(); }

            __version = ++state.Version;
            return state;
        }
    }

    /// <summary> The state, for a read: any stage of this statement may read it, but not one whose statement is finished. </summary>
    private readonly SqlWriterState __ReadState
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] get
        {
            SqlWriterState state = __state;
            if ( state.Generation != __generation ) { ThrowStale(); }

            return state;
        }
    }

    [DoesNotReturn] [MethodImpl(MethodImplOptions.NoInlining)] private readonly void ThrowStale()
    {
        if ( __state is null ) { throw new InvalidOperationException("This builder stage was never started; begin with SqlBuilder.For(dialect) or a named dialect."); }

        throw new InvalidOperationException(__state.Generation != __generation
                                                ? "This statement was already built or written; a stage can't be continued, or written again, once its statement is finished."
                                                : "A later stage has already continued from this one; builder stages are single-use, so branch by building each statement from the start.");
    }


    public readonly SqlDialectKind Dialect          => __ReadState.Dialect;
    public readonly bool           AppendTerminator => __ReadState.AppendTerminator;
    public readonly bool           StrictTypes      => __ReadState.StrictTypes;
    public readonly ClauseFlags    Flags            => __ReadState.Flags;
    public readonly int            ParenthesisDepth => __ReadState.ParenthesisDepth;
    public readonly int            Length           => __ReadState.Length;
    public readonly int            ParameterCount   => __ReadState.ParameterCount;


    // ---- buffer ----

    public void Append( char                      c )    => __WriteState.Append(c);
    public void Append( scoped ReadOnlySpan<char> text ) => __WriteState.Append(text);

    /// <summary> Writes the separator a following word needs, so the caller can record the exact position the word starts at. </summary>
    public void Separate() => __WriteState.Separate();

    public void Word( scoped ReadOnlySpan<char> text ) => __WriteState.Word(text);
    public void Space()                                => __WriteState.Append(' ');
    public void Comma()                                => __WriteState.Append(',');
    public void Dot()                                  => __WriteState.Append('.');
    public void OpenParen()                            => __WriteState.OpenParen();
    public void OpenCall()                             => __WriteState.OpenCall();
    public void CloseParen()                           => __WriteState.CloseParen();
    public void Star()                                 => __WriteState.Word("*");

    public          void MarkClause( ClauseFlags clause ) => __WriteState.MarkClause(clause);
    public readonly bool Has( ClauseFlags        clause ) => __ReadState.Has(clause);

    /// <summary> The text written since <paramref name="start"/>. </summary>
    public readonly string TextFrom( int start ) => __ReadState.TextFrom(start);

    /// <summary> Inserts <paramref name="text"/> at <paramref name="position"/>, shifting the text after it and every recorded position at or after it. </summary>
    public void InsertAt( int position, scoped ReadOnlySpan<char> text ) => __WriteState.InsertAt(position, text);


    // ---- identifiers ----

    /// <exception cref="ArgumentNullException"> <paramref name="name"/> is <see langword="null"/>. </exception>
    /// <exception cref="ArgumentException"> <paramref name="name"/> is empty. </exception>
    public void Identifier( string name ) => __WriteState.Identifier(RequireName(name));

    public void PreparedIdentifier( string name ) => __WriteState.WriteIdentifier(RequireName(name));

    internal static string RequireName( string name, string? parameter = "name" )
    {
        ArgumentNullException.ThrowIfNull(name, parameter);
        if ( name.Length is 0 ) { throw new ArgumentException("An identifier can't be empty.", parameter); }

        return name;
    }

    /// <summary> <paramref name="name"/> as the dialect writes it (folded and quoted), e.g. for a qualifier inserted later. </summary>
    public readonly string QuotedIdentifier( string name ) => __ReadState.QuotedIdentifier(name);

    public void ColumnRef( string raw ) => __WriteState.ColumnRef(raw);

    public void QualifiedColumn( string? qualifier, string columnName, bool alreadyFolded ) => __WriteState.QualifiedColumn(qualifier, columnName, alreadyFolded);

    /// <summary>
    ///     A typed column of <paramref name="table"/> written before its source is known (a projection): unqualified now, then given its alias, or its table name once the statement
    ///     joins tables, when the scope ends.
    /// </summary>
    public void DeferredColumn( string table, string columnName ) => __WriteState.DeferredColumn(table, columnName);


    // ---- table scope: lets typed column references be qualified once a statement has aliases or joins ----

    /// <summary> Records a table of the current <c>SELECT</c> (<c>FROM</c> / <c>JOIN</c>) and its alias. </summary>
    public void RegisterTable( string table, string? alias ) => __WriteState.RegisterTable(table, alias);

    /// <summary> Starts the next operand of a set operation (<c>UNION</c>): its tables and joins are its own. </summary>
    public void ResetTables() => __WriteState.ResetTables();

    /// <summary> The first table of the current query (a statement's target), or <see langword="null"/>. </summary>
    public readonly string? FirstTable => __ReadState.FirstTable;

    /// <summary> Whether <paramref name="table"/> is a source of the current query (not just an enclosing one). </summary>
    public readonly bool IsRegistered( string table ) => __ReadState.IsRegistered(table);

    /// <summary>
    ///     The qualifier for a typed column of <paramref name="table"/>: its alias, or the table name when the statement joins tables (or the table belongs to an enclosing query);
    ///     otherwise none. The innermost match wins; a table joined twice resolves to its first alias in that scope.
    /// </summary>
    public readonly string? QualifierFor( string table ) => __ReadState.QualifierFor(table);


    // ---- nested queries (subqueries, CTE bodies, derived tables) ----

    /// <summary> Starts a nested query: its tables, paging, locking and clause flags are its own until <see cref="EndScope"/>. </summary>
    public Scope BeginScope() => __WriteState.BeginScope();

    /// <summary> Ends a nested query: writes its paging and locking, qualifies its typed projections, and restores the enclosing query's state. </summary>
    public void EndScope( Scope saved ) => __WriteState.EndScope(in saved);



    /// <summary> The enclosing query's state while a nested one is written. </summary>
    public readonly struct Scope( ClauseFlags flags,
                                  long?       limit,
                                  long?       offset,
                                  bool        withTies,
                                  bool        percent,
                                  SqlLockMode lockMode,
                                  int         orderKeyStart,
                                  int         orderKeyEnd,
                                  ISqlColumn? comparedColumn,
                                  string?     requireOrderBy )
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
        if ( !supported ) { __ReadState.Require(false, feature); }
    }

    public readonly SqlBuildException Error( SqlBuildError reason, string message ) => __ReadState.Error(reason, message);


    // ---- paging and locking: recorded here, written in the dialect's form when the (sub)query ends, so the call order doesn't matter ----

    public void SetLimit( long  count ) => __WriteState.SetLimit(count);
    public void SetOffset( long count ) => __WriteState.SetOffset(count);

    /// <summary> <c>FETCH NEXT n PERCENT</c> / <c>WITH TIES</c> (Oracle, PostgreSQL 13+ for ties); SQL Server writes them in <c>TOP</c>. </summary>
    public void SetLimitOptions( bool percent, bool withTies ) => __WriteState.SetLimitOptions(percent, withTies);

    public void SetLock( SqlLockMode mode ) => __WriteState.SetLock(mode);

    /// <summary> Makes the (sub)query fail to build without an <c>ORDER BY</c> (e.g. SQL Server <c>TOP ... WITH TIES</c>). </summary>
    public void RequireOrderBy( string feature ) => __WriteState.RequireOrderBy(feature);


    // ---- SQL Server OUTPUT: it goes before VALUES / WHERE, but is chosen after them ----

    /// <summary> Records where an <c>OUTPUT</c> clause belongs (just before <c>VALUES</c> or <c>WHERE</c>); the first mark wins. </summary>
    public void MarkOutputPosition() => __WriteState.MarkOutputPosition();

    /// <summary> Moves the text written since <paramref name="start"/> to the recorded <c>OUTPUT</c> position, if there is one. </summary>
    public void MoveToOutputPosition( int start ) => __WriteState.MoveToOutputPosition(start);


    // ---- the statement's target (MySQL's multi-table UPDATE / DELETE add to it later) ----

    /// <summary> Records a position in the statement's target (e.g. after <c>DELETE</c>, or after <c>UPDATE t</c>). </summary>
    public void MarkTarget() => __WriteState.MarkTarget();

    /// <summary> Inserts <paramref name="text"/> at the recorded target position. </summary>
    public void InsertAtTarget( scoped ReadOnlySpan<char> text ) => __WriteState.InsertAtTarget(text);


    // ---- WITH [RECURSIVE] ----

    /// <summary> Records the position just after <c>WITH</c>, where <c>RECURSIVE</c> goes once any CTE needs it. </summary>
    public void MarkWith() => __WriteState.MarkWith();

    /// <summary> Writes <c>RECURSIVE</c> after <c>WITH</c> (PostgreSQL, SQLite, MySQL, Firebird; SQL Server and Oracle need no keyword), once. </summary>
    public void RequireRecursive() => __WriteState.RequireRecursive();


    // ---- ORDER BY keys (for the NULLS FIRST / LAST emulation) ----

    public void BeginOrderKey() => __WriteState.BeginOrderKey();
    public void EndOrderKey()   => __WriteState.EndOrderKey();

    /// <summary> <c>NULLS FIRST</c> / <c>NULLS LAST</c> for the last ORDER BY key; SQL Server and MySQL get an equivalent <c>CASE WHEN key IS NULL ...</c> sort key in front of it. </summary>
    public void NullsOrder( bool first ) => __WriteState.NullsOrder(first);


    // ---- typed comparisons (StrictTypes) ----

    /// <summary> The typed column the next comparison is about, checked by <see cref="CheckCompared{T}"/> when <see cref="StrictTypes"/> is on. </summary>
    public void SetComparedColumn( ISqlColumn? column ) => __WriteState.SetComparedColumn(column);

    /// <summary> With <see cref="StrictTypes"/>, rejects a value the compared typed column can't hold (<see cref="SqlBuildError.TypeMismatch"/>). </summary>
    public void CheckCompared<T>( T value, bool isNullCheck = false ) => __WriteState.CheckCompared(value, isNullCheck);

    /// <summary> <see cref="CheckCompared{T}"/> for a <see cref="SqlValue"/>, reading its value only when it will be checked. </summary>
    public void CheckCompared( in SqlValue value ) => __WriteState.CheckCompared(in value);

    /// <summary> Forgets the compared typed column (the right-hand side isn't a value, e.g. another column). </summary>
    public void ClearCompared() => __WriteState.ClearCompared();

    /// <summary> <see cref="CheckCompared{T}"/> for every value of an <c>IN</c> list. </summary>
    public void CheckComparedAll<T>( scoped ReadOnlySpan<T> values ) => __WriteState.CheckComparedAll(values);


    // ---- values ----

    public void Parameter( object? value )                      => __WriteState.Parameter(value);
    public void InlineNull()                                    => __WriteState.Word("NULL");
    public void InlineBoolean( bool                     value ) => __WriteState.InlineBoolean(value);
    public void InlineNumber( long                      value ) => __WriteState.InlineNumber(value);
    public void InlineString( scoped ReadOnlySpan<char> value ) => __WriteState.InlineString(value);
    public void InlineRaw( scoped    ReadOnlySpan<char> sql )   => __WriteState.Word(sql);

    public readonly string Snapshot() => __ReadState.Snapshot();


    // ---- terminals: Build / BuildSql are idempotent per stage; WriteTo streams the buffer away, so it's final ----

    /// <summary> Whether this stage already finished its statement (a repeated <see cref="Build"/> returns the same result). </summary>
    internal readonly bool IsBuilt => __built.HasValue;

    public SqlResult Build()
    {
        if ( __built is { } built ) { return built; }

        SqlWriterState state = __WriteState;

        try
        {
            state.Finish();
            SqlResult result = new(state.Text.ToString(), state.BuildParameters(), state.Dialect);
            __built = result;
            return result;
        }
        finally { state.Release(); }
    }

    /// <summary> The SQL text only; the parameters stay with this stage, so a later <see cref="Build"/> returns them. </summary>
    public string BuildSql() => Build().Sql;

    /// <summary>
    ///     Writes the SQL straight into <paramref name="destination"/> (no string is created) and returns the bound parameters (<see cref="SqlParameterSet.Empty"/> when there are none).
    ///     After a <see cref="Build"/>, it writes that result; otherwise it streams the buffer and the statement is finished, so no terminal can follow.
    /// </summary>
    public SqlParameterSet WriteTo( IBufferWriter<char> destination )
    {
        ArgumentNullException.ThrowIfNull(destination);

        if ( __built is { } built )
        {
            destination.Write(built.Sql);
            return built.Parameters;
        }

        SqlWriterState state = __WriteState;

        try
        {
            state.Finish();
            destination.Write(state.Text);
            return state.BuildParameters();
        }
        finally { state.Release(); }
    }
}
