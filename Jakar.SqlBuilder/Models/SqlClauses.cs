// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 10/25/2026

namespace Jakar.SqlBuilder;


/// <summary> Clause emission shared by the SELECT stages (each stage keeps its own return types, so the type-state stays exact). </summary>
internal static class Clauses
{
    // ---- set operations ----

    public static SqlRoot SetOperation( scoped ref SqlWriter writer, string keyword, bool all )
    {
        if ( keyword is not "UNION" ) { writer.Require(writer.Dialect.SupportsIntersectExcept(), keyword); }

        if ( all && keyword is not "UNION" ) { writer.Require(writer.Dialect.SupportsIntersectExceptAll(), $"{keyword} ALL"); }

        writer.Word(all ? $"{keyword} ALL" : keyword);
        writer.MarkClause(ClauseFlags.SetOp);
        return SqlRoot.Continue(writer);
    }


    // ---- locking ----

    public static LockStage Lock( scoped ref SqlWriter writer, SqlLockMode mode )
    {
        if ( writer.Has(ClauseFlags.SetOp) ) { throw writer.Error(SqlBuildError.UnsupportedFeature, "Row locking can't be combined with UNION / INTERSECT / EXCEPT."); }

        if ( writer.Has(ClauseFlags.GroupBy) || writer.Has(ClauseFlags.Having) ) { throw writer.Error(SqlBuildError.UnsupportedFeature, "Row locking can't be combined with GROUP BY."); }

        writer.SetLock(mode);
        return new LockStage(writer);
    }


    // ---- GROUP BY ----

    public static void GroupBy( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns )
    {
        writer.RequireItems(columns.Length, "GROUP BY");
        writer.Word("GROUP BY");
        WriteColumns(ref writer, columns);
        writer.MarkClause(ClauseFlags.GroupBy);
    }

    public static void GroupByTyped<T>( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> propertyNames )
        where T : ISqlTable<T>
    {
        writer.RequireItems(propertyNames.Length, "GROUP BY");
        writer.Word("GROUP BY");
        for ( int i = 0; i < propertyNames.Length; i++ )
        {
            if ( i > 0 ) { writer.Comma(); }

            SqlOps.TypedColumn<T>(ref writer, propertyNames[i]);
        }

        writer.MarkClause(ClauseFlags.GroupBy);
    }

    public static void GroupByExpr( scoped ref SqlWriter writer, scoped ReadOnlySpan<SqlExprFn> expressions )
    {
        writer.RequireItems(expressions.Length, "GROUP BY");
        writer.Word("GROUP BY");
        SqlExpr.WriteList(ref writer, expressions, "GROUP BY");
        writer.MarkClause(ClauseFlags.GroupBy);
    }

    /// <summary> <c>GROUP BY ROLLUP (a, b)</c>; MySQL: <c>GROUP BY a, b WITH ROLLUP</c>. </summary>
    public static void Rollup( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns )
    {
        writer.Require(writer.Dialect.SupportsRollup(), "GROUP BY ROLLUP");
        writer.RequireItems(columns.Length, "ROLLUP");
        writer.Word("GROUP BY");

        if ( writer.Dialect is SqlDialectKind.MySql )
        {
            WriteColumns(ref writer, columns);
            writer.Word("WITH ROLLUP");
        }
        else
        {
            writer.Word("ROLLUP");
            writer.OpenParen();
            WriteColumns(ref writer, columns);
            writer.CloseParen();
        }

        writer.MarkClause(ClauseFlags.GroupBy);
    }

    public static void Cube( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns )
    {
        writer.Require(writer.Dialect.SupportsGroupingSets(), "GROUP BY CUBE");
        writer.RequireItems(columns.Length, "CUBE");
        writer.Word("GROUP BY CUBE");
        writer.OpenParen();
        WriteColumns(ref writer, columns);
        writer.CloseParen();
        writer.MarkClause(ClauseFlags.GroupBy);
    }

    /// <summary> <c>GROUP BY GROUPING SETS ((a, b), (a), ())</c>; an empty set is the grand total. </summary>
    public static void GroupingSets( scoped ref SqlWriter writer, scoped ReadOnlySpan<string[]> sets )
    {
        writer.Require(writer.Dialect.SupportsGroupingSets(), "GROUP BY GROUPING SETS");
        writer.RequireItems(sets.Length, "GROUPING SETS");
        writer.Word("GROUP BY GROUPING SETS");
        writer.OpenParen();
        for ( int i = 0; i < sets.Length; i++ )
        {
            if ( i > 0 ) { writer.Comma(); }

            writer.OpenParen();
            WriteColumns(ref writer, sets[i] ?? throw new ArgumentNullException(nameof(sets)));
            writer.CloseParen();
        }

        writer.CloseParen();
        writer.MarkClause(ClauseFlags.GroupBy);
    }

    public static void WriteColumns( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> columns )
    {
        for ( int i = 0; i < columns.Length; i++ )
        {
            if ( i > 0 ) { writer.Comma(); }

            writer.ColumnRef(columns[i]);
        }
    }

    public static void WriteIdentifiers( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> names, bool parenthesize = true )
    {
        if ( parenthesize ) { writer.OpenParen(); }

        for ( int i = 0; i < names.Length; i++ )
        {
            if ( i > 0 ) { writer.Comma(); }

            writer.Identifier(names[i]);
        }

        if ( parenthesize ) { writer.CloseParen(); }
    }


    // ---- WINDOW ----

    public static void Window( scoped ref SqlWriter writer, string name, SqlWindowFn window )
    {
        writer.Require(writer.Dialect.SupportsWindowClause(), "WINDOW");
        if ( writer.Has(ClauseFlags.Window) ) { writer.Comma(); }
        else { writer.Word("WINDOW"); }

        writer.Identifier(name);
        writer.Word("AS");
        SqlWindow.Write(ref writer, window);
        writer.MarkClause(ClauseFlags.Window);
    }


    // ---- ORDER BY ----

    public static OrderStage OrderBy( scoped ref SqlWriter writer, string column, bool descending )
    {
        StartOrder(ref writer);
        writer.BeginOrderKey();
        writer.ColumnRef(column);
        writer.EndOrderKey();
        if ( descending ) { writer.Word("DESC"); }

        return new OrderStage(writer);
    }

    public static OrderStage OrderByTyped<T>( scoped ref SqlWriter writer, string propertyName, bool descending )
        where T : ISqlTable<T>
    {
        StartOrder(ref writer);
        writer.BeginOrderKey();
        SqlOps.TypedColumn<T>(ref writer, propertyName);
        writer.EndOrderKey();
        if ( descending ) { writer.Word("DESC"); }

        return new OrderStage(writer);
    }

    public static OrderStage OrderByExpr( scoped ref SqlWriter writer, SqlExprFn expression, bool descending )
    {
        StartOrder(ref writer, false);
        writer.BeginOrderKey();
        SqlExpr.Write(ref writer, expression);
        writer.EndOrderKey();
        if ( descending ) { writer.Word("DESC"); }

        return new OrderStage(writer);
    }

    /// <param name="named"> The key names a column (not an expression or position), which Firebird can't use to order a set operation. </param>
    private static void StartOrder( scoped ref SqlWriter writer, bool named = true )
    {
        if ( named && writer.Has(ClauseFlags.SetOp) && writer.Dialect is SqlDialectKind.Firebird ) { throw writer.Error(SqlBuildError.UnsupportedFeature, "Firebird orders a UNION by column position only; order by an expression (e => e.Number(1)) or wrap it in a derived table."); }

        if ( writer.Has(ClauseFlags.OrderBy) ) { writer.Comma(); }
        else
        {
            writer.Word("ORDER BY");
            writer.MarkClause(ClauseFlags.OrderBy);
        }
    }


    // ---- table sources ----

    public static void Source( scoped ref SqlWriter writer, string keyword, string table, string? alias )
    {
        writer.Word(keyword);
        writer.Identifier(table);
        if ( alias is { Length: > 0 } ) { writer.Identifier(alias); }

        writer.RegisterTable(table, alias);
    }

    public static void DerivedTable<TEnd>( scoped ref SqlWriter writer, string keyword, SqlSubquery<TEnd> query, string alias )
        where TEnd : ISqlQueryEnd, allows ref struct
    {
        SqlWriter.RequireName(alias, nameof(alias));
        writer.Word(keyword);
        Subqueries.Write(ref writer, query, true);
        writer.Identifier(alias);
        writer.RegisterTable(alias, alias);
    }

    /// <summary> Validates SQL Server table hints (<c>NOLOCK</c>, <c>UPDLOCK</c>, <c>INDEX(ix)</c>, ...) and writes <c>WITH (hints)</c>. </summary>
    public static void TableHints( scoped ref SqlWriter writer, scoped ReadOnlySpan<string> hints )
    {
        writer.Require(writer.Dialect.SupportsTableHints(), "Table hints");
        writer.RequireItems(hints.Length, "Table hints");
        writer.Word("WITH");
        writer.OpenParen();

        for ( int i = 0; i < hints.Length; i++ )
        {
            string hint = hints[i] ?? throw new ArgumentNullException(nameof(hints));
            foreach ( char c in hint )
            {
                if ( !char.IsAsciiLetterOrDigit(c) && c is not '_' and not '(' and not ')' and not ' ' and not '=' ) { throw new ArgumentException($"'{hint}' isn't a table hint.", nameof(hints)); }
            }

            if ( i > 0 ) { writer.Comma(); }

            writer.Word(hint.ToUpperInvariant());
        }

        writer.CloseParen();
    }
}
