// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 10/25/2026

using System.Globalization;



namespace Jakar.SqlBuilder;


/// <summary> Writes one SQL expression: <c>e =&gt; e.Column("age").Plus().Number(1)</c>. Return the expression you were given, after writing into it. </summary>
public delegate SqlExpr SqlExprFn( SqlExpr expression );

/// <summary> Writes a window specification: <c>w =&gt; w.PartitionBy("user_id").OrderByDesc("score")</c>. </summary>
public delegate SqlWindow SqlWindowFn( SqlWindow window );

/// <summary> Writes the branches of a <c>CASE</c>: <c>c =&gt; c.When(p =&gt; ..., t =&gt; ...).Else(e =&gt; ...)</c>. </summary>
public delegate SqlCase SqlCaseFn( SqlCase cases );



public enum SqlDatePart : byte
{
    Year,
    Month,
    Day,
    Hour,
    Minute,
    Second
}



/// <summary> A window frame bound: <c>UNBOUNDED PRECEDING</c>, <c>n PRECEDING</c>, <c>CURRENT ROW</c>, <c>n FOLLOWING</c>, <c>UNBOUNDED FOLLOWING</c>. </summary>
public readonly record struct SqlFrameBound
{
    private readonly long __offset;
    private readonly byte __kind;

    private SqlFrameBound( byte kind, long offset = 0 )
    {
        __kind   = kind;
        __offset = offset;
    }

    public static SqlFrameBound UnboundedPreceding { get; } = new(0);
    public static SqlFrameBound CurrentRow         { get; } = new(2);
    public static SqlFrameBound UnboundedFollowing { get; } = new(4);
    public static SqlFrameBound Preceding( long rows ) => new(1, rows >= 0 ? rows : throw new ArgumentOutOfRangeException(nameof(rows)));
    public static SqlFrameBound Following( long rows ) => new(3, rows >= 0 ? rows : throw new ArgumentOutOfRangeException(nameof(rows)));

    internal void Write( scoped ref SqlWriter writer )
    {
        switch ( __kind )
        {
            case 0: writer.Word("UNBOUNDED PRECEDING"); return;
            case 1:
                writer.InlineNumber(__offset);
                writer.Word("PRECEDING");
                return;
            case 2: writer.Word("CURRENT ROW"); return;
            case 3:
                writer.InlineNumber(__offset);
                writer.Word("FOLLOWING");
                return;
            default: writer.Word("UNBOUNDED FOLLOWING"); return;
        }
    }
}



/// <summary>
///     One SQL expression, written straight into the statement's buffer. Operands (<see cref="Column"/>, <see cref="Param"/>, <see cref="Number(long)"/>, ...) and infix operators
///     (<see cref="Plus"/>, <see cref="Times"/>, ...) chain left to right; functions take their arguments as nested <see cref="SqlExprFn"/>s, so parentheses always balance.
///     Function names and operators are written in each dialect's form (<c>CONCAT</c> vs <c>||</c>, <c>LEN</c> vs <c>LENGTH</c>, <c>MOD</c> vs <c>%</c>, ...).
/// </summary>
public ref struct SqlExpr
{
    private SqlWriter __w;

    internal SqlExpr( SqlWriter writer ) => __w = writer;

    internal readonly SqlWriter Writer => __w;

    private SqlExpr Next() => new(__w);


    /// <summary> Writes <paramref name="expression"/> into <paramref name="writer"/>. </summary>
    /// <exception cref="SqlBuildException"> The expression wrote nothing (<see cref="SqlBuildError.EmptyPredicate"/>). </exception>
    internal static void Write( scoped ref SqlWriter writer, SqlExprFn expression )
    {
        ArgumentNullException.ThrowIfNull(expression);
        int     start  = writer.Length;
        SqlExpr result = expression(new SqlExpr(writer));
        writer = result.__w;
        if ( writer.Length == start ) { throw writer.Error(SqlBuildError.EmptyPredicate, "An expression wrote nothing."); }
    }

    internal static void WriteList( scoped ref SqlWriter writer, scoped ReadOnlySpan<SqlExprFn> expressions, string what )
    {
        writer.RequireItems(expressions.Length, what);
        for ( int i = 0; i < expressions.Length; i++ )
        {
            if ( i > 0 ) { writer.Comma(); }

            Write(ref writer, expressions[i]);
        }
    }


    // ---- operands ----

    public SqlExpr Column( string column )
    {
        __w.ColumnRef(column);
        return Next();
    }
    public SqlExpr Column<T>( string propertyName )
        where T : ISqlTable<T>
    {
        SqlOps.TypedColumn<T>(ref __w, propertyName);
        __w.ClearCompared();
        return Next();
    }
    public SqlExpr Value( SqlValue value )
    {
        value.Write(ref __w);
        return Next();
    }
    public SqlExpr Param( object? value )
    {
        if ( value is null ) { __w.InlineNull(); }
        else { __w.Parameter(value); }

        return Next();
    }
    public SqlExpr Number( long value )
    {
        __w.InlineNumber(value);
        return Next();
    }
    public SqlExpr Number( decimal value ) => Value(SqlValue.Of(value));
    public SqlExpr Number( double  value ) => Value(SqlValue.Of(value));
    public SqlExpr Bool( bool value )
    {
        __w.InlineBoolean(value);
        return Next();
    }
    public SqlExpr String( string value )
    {
        __w.InlineString(value ?? throw new ArgumentNullException(nameof(value)));
        return Next();
    }
    public SqlExpr Null()
    {
        __w.InlineNull();
        return Next();
    }
    public SqlExpr Star()
    {
        __w.Star();
        return Next();
    }

    /// <summary> A trusted SQL fragment, written as given (the audited injection boundary, like <see cref="SqlValue.Raw(string)"/>). </summary>
    public SqlExpr Raw( string sql )
    {
        __w.InlineRaw(sql ?? throw new ArgumentNullException(nameof(sql)));
        return Next();
    }

    /// <summary> <c>( expression )</c>. </summary>
    public SqlExpr Group( SqlExprFn expression )
    {
        __w.OpenParen();
        Write(ref __w, expression);
        __w.CloseParen();
        return Next();
    }

    /// <summary> A scalar subquery: <c>(SELECT ...)</c>. </summary>
    public SqlExpr Subquery<TEnd>( SqlSubquery<TEnd> query )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        Subqueries.Write(ref __w, query, true);
        return Next();
    }


    // ---- infix operators ----

    public SqlExpr Plus()      => Operator("+");
    public SqlExpr Minus()     => Operator("-");
    public SqlExpr Times()     => Operator("*");
    public SqlExpr DividedBy() => Operator("/");

    private SqlExpr Operator( string op )
    {
        __w.Word(op);
        return Next();
    }

    /// <summary> <c>-(expression)</c>. </summary>
    public SqlExpr Negate( SqlExprFn expression )
    {
        __w.Word("-");
        return Group(expression);
    }

    /// <summary> <c>(a % b)</c>; Oracle and Firebird: <c>MOD(a, b)</c>. </summary>
    public SqlExpr Modulo( SqlExprFn dividend, SqlExprFn divisor )
    {
        if ( __w.Dialect is SqlDialectKind.Oracle or SqlDialectKind.Firebird ) { return Function("MOD", dividend, divisor); }

        __w.OpenParen();
        Write(ref __w, dividend);
        __w.Word("%");
        Write(ref __w, divisor);
        __w.CloseParen();
        return Next();
    }

    /// <summary> String concatenation: <c>(a || b)</c>; SQL Server and MySQL: <c>CONCAT(a, b)</c>. </summary>
    public SqlExpr Concat( params ReadOnlySpan<SqlExprFn> parts )
    {
        __w.RequireItems(parts.Length, "CONCAT");
        if ( __w.Dialect is SqlDialectKind.SqlServer or SqlDialectKind.MySql ) { return Function("CONCAT", parts); }

        __w.OpenParen();
        for ( int i = 0; i < parts.Length; i++ )
        {
            if ( i > 0 ) { __w.Word("||"); }

            Write(ref __w, parts[i]);
        }

        __w.CloseParen();
        return Next();
    }


    // ---- functions ----

    /// <summary> <c>name(arguments)</c>. The name is a trusted identifier (letters, digits, <c>_</c>, <c>.</c>, <c>$</c>). </summary>
    public SqlExpr Function( string name, params ReadOnlySpan<SqlExprFn> arguments )
    {
        WriteFunctionName(ref __w, name);
        __w.OpenCall();
        for ( int i = 0; i < arguments.Length; i++ )
        {
            if ( i > 0 ) { __w.Comma(); }

            Write(ref __w, arguments[i]);
        }

        __w.CloseParen();
        return Next();
    }

    internal static void WriteFunctionName( scoped ref SqlWriter writer, string name )
    {
        SqlWriter.RequireName(name);
        foreach ( char c in name )
        {
            if ( !char.IsAsciiLetterOrDigit(c) && c is not '_' and not '.' and not '$' ) { throw new ArgumentException($"'{name}' isn't a function name.", nameof(name)); }
        }

        writer.Word(name);
    }

    public SqlExpr Coalesce( params ReadOnlySpan<SqlExprFn> values )
    {
        __w.RequireItems(values.Length, "COALESCE");
        return Function("COALESCE", values);
    }
    public SqlExpr NullIf( SqlExprFn value, SqlExprFn compareTo ) => Function("NULLIF", value, compareTo);

    /// <summary> <c>CAST(value AS type)</c>, with the type written for the dialect (<see cref="SqlType.CastName"/>). </summary>
    public SqlExpr Cast( SqlExprFn value, SqlType type )
    {
        __w.Word("CAST");
        __w.OpenCall();
        Write(ref __w, value);
        __w.Word("AS");
        __w.Word(type.CastName(__w.Dialect));
        __w.CloseParen();
        return Next();
    }

    public SqlExpr Lower( SqlExprFn value ) => Function("LOWER", value);
    public SqlExpr Upper( SqlExprFn value ) => Function("UPPER", value);
    public SqlExpr Trim( SqlExprFn  value ) => Function("TRIM",  value);
    public SqlExpr Abs( SqlExprFn   value ) => Function("ABS",   value);

    /// <summary> The length in characters: <c>LENGTH</c>; SQL Server <c>LEN</c>; MySQL and Firebird <c>CHAR_LENGTH</c>. </summary>
    public SqlExpr Length( SqlExprFn value ) => Function(__w.Dialect switch
                                                         {
                                                             SqlDialectKind.SqlServer                         => "LEN",
                                                             SqlDialectKind.MySql or SqlDialectKind.Firebird  => "CHAR_LENGTH",
                                                             _                                                => "LENGTH"
                                                         }, value);

    public SqlExpr Round( SqlExprFn value, int digits = 0 )
    {
        __w.Word("ROUND");
        __w.OpenCall();
        Write(ref __w, value);
        __w.Comma();
        __w.InlineNumber(digits);
        __w.CloseParen();
        return Next();
    }


    // ---- CASE ----

    /// <summary> A searched <c>CASE WHEN condition THEN ... [ELSE ...] END</c>. </summary>
    public SqlExpr Case( SqlCaseFn cases )
    {
        ArgumentNullException.ThrowIfNull(cases);
        __w.Word("CASE");
        SqlCase result = cases(new SqlCase(__w, false));
        __w = result.Finish();
        return Next();
    }

    /// <summary> A simple <c>CASE subject WHEN value THEN ... [ELSE ...] END</c>; use <see cref="SqlCase.WhenValue"/> for its branches. </summary>
    public SqlExpr Case( SqlExprFn subject, SqlCaseFn cases )
    {
        ArgumentNullException.ThrowIfNull(cases);
        __w.Word("CASE");
        Write(ref __w, subject);
        SqlCase result = cases(new SqlCase(__w, true));
        __w = result.Finish();
        return Next();
    }


    // ---- aggregates ----

    /// <summary> <c>COUNT(*)</c>, optionally <c>FILTER (WHERE ...)</c> (emulated with <c>COUNT(CASE WHEN ... THEN 1 END)</c> where there's no FILTER). </summary>
    public SqlExpr Count( SqlPredicateFn? filter = null )
    {
        if ( filter is not null && !__w.Dialect.SupportsFilterClause() ) { return Aggregate("COUNT", static e => e.Number(1), false, filter); }

        __w.Word("COUNT");
        __w.OpenCall();
        __w.Append('*');
        __w.CloseParen();
        WriteFilter(ref __w, filter);
        return Next();
    }
    public SqlExpr Count( SqlExprFn value, bool distinct = false, SqlPredicateFn? filter = null ) => Aggregate("COUNT", value, distinct, filter);
    public SqlExpr Sum( SqlExprFn   value, bool distinct = false, SqlPredicateFn? filter = null ) => Aggregate("SUM",   value, distinct, filter);
    public SqlExpr Avg( SqlExprFn   value, bool distinct = false, SqlPredicateFn? filter = null ) => Aggregate("AVG",   value, distinct, filter);
    public SqlExpr Min( SqlExprFn   value, SqlPredicateFn? filter = null ) => Aggregate("MIN", value, false, filter);
    public SqlExpr Max( SqlExprFn   value, SqlPredicateFn? filter = null ) => Aggregate("MAX", value, false, filter);

    private SqlExpr Aggregate( string function, SqlExprFn value, bool distinct, SqlPredicateFn? filter )
    {
        bool native = filter is null || __w.Dialect.SupportsFilterClause();
        if ( distinct && filter is not null && !native ) { throw __w.Error(SqlBuildError.UnsupportedFeature, $"DISTINCT with a FILTER is not supported by {__w.Dialect}."); }

        __w.Word(function);
        __w.OpenCall();
        if ( distinct ) { __w.Word("DISTINCT"); }

        if ( native ) { Write(ref __w, value); }
        else
        {
            __w.Word("CASE WHEN");
            Predicates.Write(ref __w, filter!);
            __w.Word("THEN");
            Write(ref __w, value);
            __w.Word("END");
        }

        __w.CloseParen();
        if ( native ) { WriteFilter(ref __w, filter); }

        return Next();
    }

    private static void WriteFilter( scoped ref SqlWriter writer, SqlPredicateFn? filter )
    {
        if ( filter is null ) { return; }

        writer.Word("FILTER");
        writer.OpenParen();
        writer.Word("WHERE");
        Predicates.Write(ref writer, filter);
        writer.CloseParen();
    }

    /// <summary>
    ///     Concatenates the group's values with <paramref name="separator"/>: <c>STRING_AGG</c> (PostgreSQL, SQL Server), <c>GROUP_CONCAT</c> (SQLite, MySQL), <c>LISTAGG</c> (Oracle),
    ///     <c>LIST</c> (Firebird, which can't order it).
    /// </summary>
    public SqlExpr StringAgg( SqlExprFn value, string separator, SqlExprFn? orderBy = null, bool descending = false, bool distinct = false )
    {
        ArgumentNullException.ThrowIfNull(separator);
        SqlDialectKind dialect = __w.Dialect;
        if ( orderBy is not null ) { __w.Require(dialect.SupportsOrderedStringAgg(), "Ordered string aggregation"); }

        if ( distinct ) { __w.Require(dialect is SqlDialectKind.PostgreSql or SqlDialectKind.MySql or SqlDialectKind.Oracle or SqlDialectKind.Firebird, "DISTINCT string aggregation"); }

        switch ( dialect )
        {
            case SqlDialectKind.MySql:
                __w.Word("GROUP_CONCAT");
                __w.OpenCall();
                if ( distinct ) { __w.Word("DISTINCT"); }

                Write(ref __w, value);
                WriteOrderBy(ref __w, orderBy, descending);
                __w.Word("SEPARATOR");
                __w.InlineString(separator);
                __w.CloseParen();
                return Next();

            case SqlDialectKind.SqlServer or SqlDialectKind.Oracle:
                __w.Word(dialect is SqlDialectKind.Oracle ? "LISTAGG" : "STRING_AGG");
                __w.OpenCall();
                if ( distinct ) { __w.Word("DISTINCT"); }

                Write(ref __w, value);
                __w.Comma();
                __w.InlineString(separator);
                __w.CloseParen();

                // LISTAGG needs WITHIN GROUP; order by the value itself when no order was given.
                if ( orderBy is not null || dialect is SqlDialectKind.Oracle )
                {
                    __w.Word("WITHIN GROUP");
                    __w.OpenParen();
                    WriteOrderBy(ref __w, orderBy ?? value, descending);
                    __w.CloseParen();
                }

                return Next();

            case SqlDialectKind.Firebird:
                __w.Word("LIST");
                __w.OpenCall();
                if ( distinct ) { __w.Word("DISTINCT"); }

                Write(ref __w, value);
                __w.Comma();
                __w.InlineString(separator);
                __w.CloseParen();
                return Next();

            default:
                __w.Word(dialect is SqlDialectKind.Sqlite ? "GROUP_CONCAT" : "STRING_AGG");
                __w.OpenCall();
                if ( distinct ) { __w.Word("DISTINCT"); }

                Write(ref __w, value);
                __w.Comma();
                __w.InlineString(separator);
                WriteOrderBy(ref __w, orderBy, descending);
                __w.CloseParen();
                return Next();
        }
    }

    private static void WriteOrderBy( scoped ref SqlWriter writer, SqlExprFn? orderBy, bool descending )
    {
        if ( orderBy is null ) { return; }

        writer.Word("ORDER BY");
        Write(ref writer, orderBy);
        if ( descending ) { writer.Word("DESC"); }
    }

    /// <summary> <c>ARRAY_AGG(value [ORDER BY ...])</c> (PostgreSQL). </summary>
    public SqlExpr ArrayAgg( SqlExprFn value, SqlExprFn? orderBy = null, bool descending = false )
    {
        __w.Require(__w.Dialect.SupportsArrayAgg(), "ARRAY_AGG");
        __w.Word("ARRAY_AGG");
        __w.OpenCall();
        Write(ref __w, value);
        WriteOrderBy(ref __w, orderBy, descending);
        __w.CloseParen();
        return Next();
    }


    // ---- window functions ----

    public SqlExpr RowNumber()   => NoArgs("ROW_NUMBER");
    public SqlExpr Rank()        => NoArgs("RANK");
    public SqlExpr DenseRank()   => NoArgs("DENSE_RANK");
    public SqlExpr PercentRank() => NoArgs("PERCENT_RANK");
    public SqlExpr CumeDist()    => NoArgs("CUME_DIST");

    private SqlExpr NoArgs( string function )
    {
        __w.Word(function);
        __w.OpenCall();
        __w.CloseParen();
        return Next();
    }

    public SqlExpr Ntile( long buckets )
    {
        if ( buckets <= 0 ) { throw new ArgumentOutOfRangeException(nameof(buckets)); }

        __w.Word("NTILE");
        __w.OpenCall();
        __w.InlineNumber(buckets);
        __w.CloseParen();
        return Next();
    }

    public SqlExpr Lag( SqlExprFn  value, long offset = 1, SqlExprFn? defaultValue = null ) => Shift("LAG",  value, offset, defaultValue);
    public SqlExpr Lead( SqlExprFn value, long offset = 1, SqlExprFn? defaultValue = null ) => Shift("LEAD", value, offset, defaultValue);

    private SqlExpr Shift( string function, SqlExprFn value, long offset, SqlExprFn? defaultValue )
    {
        __w.Word(function);
        __w.OpenCall();
        Write(ref __w, value);
        __w.Comma();
        __w.InlineNumber(offset);
        if ( defaultValue is not null )
        {
            __w.Comma();
            Write(ref __w, defaultValue);
        }

        __w.CloseParen();
        return Next();
    }

    public SqlExpr FirstValue( SqlExprFn value ) => Function("FIRST_VALUE", value);
    public SqlExpr LastValue( SqlExprFn  value ) => Function("LAST_VALUE",  value);
    public SqlExpr NthValue( SqlExprFn value, long n )
    {
        __w.Require(__w.Dialect.SupportsNthValue(), "NTH_VALUE");
        __w.Word("NTH_VALUE");
        __w.OpenCall();
        Write(ref __w, value);
        __w.Comma();
        __w.InlineNumber(n);
        __w.CloseParen();
        return Next();
    }

    /// <summary> <c>OVER (window)</c> after a window or aggregate function. </summary>
    public SqlExpr Over( SqlWindowFn window )
    {
        __w.Word("OVER");
        SqlWindow.Write(ref __w, window);
        return Next();
    }

    /// <summary> <c>OVER name</c>, a window declared with <c>.Window(name, ...)</c>. </summary>
    public SqlExpr Over( string windowName )
    {
        __w.Word("OVER");
        __w.Identifier(windowName);
        return Next();
    }


    // ---- JSON ----

    /// <summary>
    ///     The text at <paramref name="path"/> (object keys) inside a JSON value: <c>-&gt;&gt;</c> (PostgreSQL <c>#&gt;&gt;</c> for nested paths, SQLite, MySQL) or <c>JSON_VALUE</c>
    ///     (SQL Server, Oracle). Firebird has no JSON.
    /// </summary>
    public SqlExpr JsonText( SqlExprFn json, params ReadOnlySpan<string> path ) => Json(json, path, true);

    /// <summary> The JSON value (object / array) at <paramref name="path"/>: <c>-&gt;</c> (PostgreSQL <c>#&gt;</c>, SQLite, MySQL) or <c>JSON_QUERY</c> (SQL Server, Oracle). </summary>
    public SqlExpr JsonObject( SqlExprFn json, params ReadOnlySpan<string> path ) => Json(json, path, false);

    private SqlExpr Json( SqlExprFn json, scoped ReadOnlySpan<string> path, bool text )
    {
        __w.Require(__w.Dialect.SupportsJson(), "JSON");
        __w.RequireItems(path.Length, "JSON path");

        switch ( __w.Dialect )
        {
            case SqlDialectKind.PostgreSql:
                __w.OpenParen();
                Write(ref __w, json);
                if ( path.Length is 1 )
                {
                    __w.Word(text ? "->>" : "->");
                    __w.InlineString(path[0]);
                }
                else
                {
                    __w.Word(text ? "#>>" : "#>");
                    __w.InlineString(PostgresPath(path));
                }

                __w.CloseParen();
                return Next();

            case SqlDialectKind.SqlServer or SqlDialectKind.Oracle:
                __w.Word(text ? "JSON_VALUE" : "JSON_QUERY");
                __w.OpenCall();
                Write(ref __w, json);
                __w.Comma();
                __w.InlineString(JsonPath(path));
                __w.CloseParen();
                return Next();

            default:
                __w.OpenParen();
                Write(ref __w, json);
                __w.Word(text ? "->>" : "->");
                __w.InlineString(JsonPath(path));
                __w.CloseParen();
                return Next();
        }
    }

    /// <summary> A PostgreSQL text-array path, <c>{a,b}</c>; keys with separators or quotes are double-quoted. </summary>
    private static string PostgresPath( scoped ReadOnlySpan<string> keys )
    {
        ValueStringBuilder builder = new("{");
        for ( int i = 0; i < keys.Length; i++ )
        {
            string key = keys[i] ?? throw new ArgumentNullException(nameof(keys));
            if ( i > 0 ) { builder.Append(','); }

            if ( key.AsSpan().IndexOfAny(",\"{}\\ ") >= 0 || key.Length is 0 ) { builder.Append('"').Append(key.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"'); }
            else { builder.Append(key); }
        }

        return builder.Append('}').ToString();
    }

    private static string JsonPath( scoped ReadOnlySpan<string> keys )
    {
        ValueStringBuilder builder = new("$");
        foreach ( string key in keys )
        {
            ArgumentNullException.ThrowIfNull(key);
            builder.Append(".\"").Append(key.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
        }

        return builder.ToString();
    }


    // ---- dates ----

    public SqlExpr CurrentTimestamp() => Operator("CURRENT_TIMESTAMP");

    /// <summary> Today's date: <c>CURRENT_DATE</c>; SQL Server <c>CAST(GETDATE() AS DATE)</c>; Oracle <c>TRUNC(CURRENT_DATE)</c>. </summary>
    public SqlExpr CurrentDate() => Operator(__w.Dialect switch
                                             {
                                                 SqlDialectKind.SqlServer => "CAST(GETDATE() AS DATE)",
                                                 SqlDialectKind.Oracle    => "TRUNC(CURRENT_DATE)",
                                                 _                        => "CURRENT_DATE"
                                             });

    /// <summary>
    ///     <paramref name="date"/> plus <paramref name="amount"/> <paramref name="part"/>s: <c>date + INTERVAL</c> (PostgreSQL), <c>DATEADD</c> (SQL Server, Firebird), <c>datetime(date, '+n unit')</c>
    ///     (SQLite, as text), <c>DATE_ADD</c> (MySQL), <c>ADD_MONTHS</c> / <c>NUMTODSINTERVAL</c> (Oracle).
    /// </summary>
    public SqlExpr AddInterval( SqlExprFn date, long amount, SqlDatePart part )
    {
        string unit = part.ToString().ToUpperInvariant();
        string n    = amount.ToString(CultureInfo.InvariantCulture);

        switch ( __w.Dialect )
        {
            case SqlDialectKind.PostgreSql:
                __w.OpenParen();
                Write(ref __w, date);
                __w.Word("+");
                __w.Word($"INTERVAL '{n} {unit.ToLowerInvariant()}'");
                __w.CloseParen();
                return Next();

            case SqlDialectKind.SqlServer:
                __w.Word("DATEADD");
                __w.OpenCall();
                __w.Append(unit.ToLowerInvariant());
                __w.Comma();
                __w.InlineNumber(amount);
                __w.Comma();
                Write(ref __w, date);
                __w.CloseParen();
                return Next();

            case SqlDialectKind.Sqlite:
                __w.Word("datetime");
                __w.OpenCall();
                Write(ref __w, date);
                __w.Comma();
                __w.InlineString($"{( amount >= 0 ? "+" : string.Empty )}{n} {unit.ToLowerInvariant()}");
                __w.CloseParen();
                return Next();

            case SqlDialectKind.MySql:
                __w.Word("DATE_ADD");
                __w.OpenCall();
                Write(ref __w, date);
                __w.Comma();
                __w.Word($"INTERVAL {n} {unit}");
                __w.CloseParen();
                return Next();

            case SqlDialectKind.Oracle when part is SqlDatePart.Year or SqlDatePart.Month:
                __w.Word("ADD_MONTHS");
                __w.OpenCall();
                Write(ref __w, date);
                __w.Comma();
                __w.InlineNumber(part is SqlDatePart.Year ? amount * 12 : amount);
                __w.CloseParen();
                return Next();

            case SqlDialectKind.Oracle:
                __w.OpenParen();
                Write(ref __w, date);
                __w.Word("+");
                __w.Word($"NUMTODSINTERVAL({n}, '{unit}')");
                __w.CloseParen();
                return Next();

            default:
                __w.Word("DATEADD");
                __w.OpenCall();
                __w.Append($"{n} {unit} TO");
                Write(ref __w, date);
                __w.CloseParen();
                return Next();
        }
    }
}



/// <summary> The branches of a <c>CASE</c>, closed with <c>END</c> when the builder returns. </summary>
public ref struct SqlCase
{
    private          SqlWriter __w;
    private readonly bool      __simple;
    private          bool      __hasBranch;
    private          bool      __hasElse;

    internal SqlCase( SqlWriter writer, bool simple )
    {
        __w         = writer;
        __simple    = simple;
        __hasBranch = false;
        __hasElse   = false;
    }

    private readonly SqlCase Next() => this;

    /// <summary> <c>WHEN condition THEN result</c> (searched CASE). </summary>
    public SqlCase When( SqlPredicateFn condition, SqlExprFn then )
    {
        if ( __simple ) { throw __w.Error(SqlBuildError.MissingRequiredClause, "A simple CASE (with a subject) takes WhenValue branches."); }

        Branch();
        __w.Word("WHEN");
        Predicates.Write(ref __w, condition);
        __w.Word("THEN");
        SqlExpr.Write(ref __w, then);
        return Next();
    }

    /// <summary> <c>WHEN value THEN result</c> (simple CASE). </summary>
    public SqlCase WhenValue( SqlExprFn value, SqlExprFn then )
    {
        if ( !__simple ) { throw __w.Error(SqlBuildError.MissingRequiredClause, "WhenValue branches need a CASE with a subject."); }

        Branch();
        __w.Word("WHEN");
        SqlExpr.Write(ref __w, value);
        __w.Word("THEN");
        SqlExpr.Write(ref __w, then);
        return Next();
    }

    public SqlCase Else( SqlExprFn result )
    {
        if ( !__hasBranch || __hasElse ) { throw __w.Error(SqlBuildError.MissingRequiredClause, "ELSE comes once, after at least one WHEN."); }

        __hasElse = true;
        __w.Word("ELSE");
        SqlExpr.Write(ref __w, result);
        return Next();
    }

    private void Branch()
    {
        if ( __hasElse ) { throw __w.Error(SqlBuildError.MissingRequiredClause, "WHEN can't follow ELSE."); }

        __hasBranch = true;
    }

    internal SqlWriter Finish()
    {
        if ( !__hasBranch ) { throw __w.Error(SqlBuildError.EmptyPredicate, "CASE needs at least one WHEN."); }

        __w.Word("END");
        return __w;
    }
}



/// <summary> A window specification: <c>PARTITION BY</c>, <c>ORDER BY</c> and a frame, written inside <c>OVER ( ... )</c> or <c>WINDOW name AS ( ... )</c>. </summary>
public ref struct SqlWindow
{
    private SqlWriter __w;
    private byte      __stage; // 0 nothing, 1 partition, 2 order, 3 frame
    private bool      __hasBase;

    internal SqlWindow( SqlWriter writer )
    {
        __w       = writer;
        __stage   = 0;
        __hasBase = false;
    }

    internal static void Write( scoped ref SqlWriter writer, SqlWindowFn window )
    {
        ArgumentNullException.ThrowIfNull(window);
        writer.OpenParen();
        SqlWindow result = window(new SqlWindow(writer));
        writer = result.__w;
        writer.CloseParen();
    }

    private SqlWindow Next( byte stage )
    {
        __stage = stage;
        return this;
    }

    /// <summary> Builds on a named window (<c>(base ORDER BY ...)</c>); a base window can't be repartitioned, so only ORDER BY and a frame may follow. </summary>
    public SqlWindow Base( string windowName )
    {
        if ( __stage is not 0 ) { throw __w.Error(SqlBuildError.MissingRequiredClause, "A base window name comes first."); }

        __w.Identifier(windowName);
        __hasBase = true;
        return Next(1);
    }

    public SqlWindow PartitionBy( params ReadOnlySpan<string> columns )
    {
        __w.RequireItems(columns.Length, "PARTITION BY");
        RequireNoBase();
        Start(1, "PARTITION BY");
        for ( int i = 0; i < columns.Length; i++ )
        {
            if ( i > 0 ) { __w.Comma(); }

            __w.ColumnRef(columns[i]);
        }

        return Next(1);
    }
    public SqlWindow PartitionBy( SqlExprFn expression )
    {
        RequireNoBase();
        Start(1, "PARTITION BY");
        SqlExpr.Write(ref __w, expression);
        return Next(1);
    }

    public SqlWindow OrderBy( string     column ) => Order(column, false);
    public SqlWindow OrderByDesc( string column ) => Order(column, true);
    public SqlWindow OrderBy( SqlExprFn expression, bool descending = false )
    {
        Start(2, "ORDER BY");
        SqlExpr.Write(ref __w, expression);
        if ( descending ) { __w.Word("DESC"); }

        return Next(2);
    }

    private SqlWindow Order( string column, bool descending )
    {
        Start(2, "ORDER BY");
        __w.ColumnRef(column);
        if ( descending ) { __w.Word("DESC"); }

        return Next(2);
    }

    public SqlWindow Rows( SqlFrameBound start, SqlFrameBound? end = null )  => Frame("ROWS",  start, end);
    public SqlWindow Range( SqlFrameBound start, SqlFrameBound? end = null ) => Frame("RANGE", start, end);
    public SqlWindow Groups( SqlFrameBound start, SqlFrameBound? end = null )
    {
        __w.Require(__w.Dialect.SupportsGroupsFrame(), "GROUPS frames");
        return Frame("GROUPS", start, end);
    }

    private SqlWindow Frame( string unit, SqlFrameBound start, SqlFrameBound? end )
    {
        if ( __stage >= 3 ) { throw __w.Error(SqlBuildError.MissingRequiredClause, "A window has one frame."); }

        __w.Word(unit);
        if ( end is { } upper )
        {
            __w.Word("BETWEEN");
            start.Write(ref __w);
            __w.Word("AND");
            upper.Write(ref __w);
        }
        else { start.Write(ref __w); }

        return Next(3);
    }

    private readonly void RequireNoBase()
    {
        if ( __hasBase ) { throw __w.Error(SqlBuildError.MissingRequiredClause, "A window built on a named window can't be repartitioned."); }
    }

    private void Start( byte stage, string keyword )
    {
        if ( stage is 2 && __hasBase && __stage is 1 )
        {
            __w.Word(keyword);
            return;
        }

        if ( stage < __stage ) { throw __w.Error(SqlBuildError.MissingRequiredClause, $"{keyword} comes before what was already written."); }

        if ( stage == __stage ) { __w.Comma(); }
        else { __w.Word(keyword); }
    }
}
