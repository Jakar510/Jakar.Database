// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary>
///     Dialect-divergent rendering, dispatched on <see cref="SqlDialectKind"/>. The <c>Supports...</c> flags say what a dialect can express; the builder throws
///     <see cref="SqlBuildError.UnsupportedFeature"/> for the rest (or emulates it, where noted), so it never emits SQL the engine would reject.
///     Version notes are the minimum each flag assumes: PostgreSQL 15, SQL Server 2022, SQLite 3.39, MySQL 8.0.31, Oracle 23ai, Firebird 5.
/// </summary>
public static class SqlDialects
{
    /// <summary> PostgreSQL identifier folder. Defaults to invariant snake_case + lower. Hosts may replace it (e.g. Jakar.Database <c>PostgresParams.SqlName</c>). </summary>
    public static Func<string, string> PostgresIdentifierFolder { get; set; } = DefaultSnakeCaseLower;

    /// <summary> The built-in <see cref="PostgresIdentifierFolder"/>: invariant snake_case + lower (<c>FirstName</c> → <c>first_name</c>), for restoring it after a host replaced it. </summary>
    public static Func<string, string> DefaultPostgresIdentifierFolder { get; } = DefaultSnakeCaseLower;


    // ---- statement shape ----



    extension( SqlDialectKind dialect )
    {
        /// <summary> <c>RETURNING</c> on INSERT / UPDATE / DELETE (SQLite 3.35+; Firebird 2.1+, but an UPDATE / DELETE that touches several rows needs Firebird 5; Oracle's <c>RETURNING ... INTO</c> needs output binds and isn't supported). </summary>
        public bool SupportsReturning() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite or SqlDialectKind.Firebird;

        public bool SupportsOutputClause() => dialect is SqlDialectKind.SqlServer;

        /// <summary> <c>INSERT ... VALUES (...), (...)</c>. Firebird has no multi-row <c>VALUES</c>; Oracle has it from 23ai. </summary>
        public bool SupportsMultiRowValues() => dialect is not SqlDialectKind.Firebird;

        /// <summary> Oracle rejects a trailing <c>;</c> in a single statement (ORA-00933 / ORA-00911), so it's never written there. </summary>
        public bool SupportsStatementTerminator() => dialect is not SqlDialectKind.Oracle;

        /// <summary> The table a <c>SELECT</c> without a <c>FROM</c> reads from, for dialects that require one: Oracle's <c>DUAL</c>, Firebird's <c>RDB$DATABASE</c>. </summary>
        public string? SourcelessFromTable() => dialect switch
                                                {
                                                    SqlDialectKind.Oracle   => "DUAL",
                                                    SqlDialectKind.Firebird => "RDB$DATABASE",
                                                    _                       => null
                                                };

        /// <summary> SQL Server's <c>OFFSET ... FETCH</c> needs an <c>ORDER BY</c>. </summary>
        public bool RequiresOrderByForOffset() => dialect is SqlDialectKind.SqlServer;

        /// <summary> <c>TOP (n) PERCENT</c> (SQL Server) / <c>FETCH FIRST n PERCENT</c> (Oracle). </summary>
        public bool SupportsLimitPercent() => dialect is SqlDialectKind.SqlServer or SqlDialectKind.Oracle;

        /// <summary> <c>TOP (n) WITH TIES</c> (SQL Server) / <c>FETCH FIRST n ROWS WITH TIES</c> (PostgreSQL 13+, Oracle). </summary>
        public bool SupportsWithTies() => dialect is SqlDialectKind.SqlServer or SqlDialectKind.PostgreSql or SqlDialectKind.Oracle;

        /// <summary> <c>FOR UPDATE</c> / <c>FOR SHARE</c> (Oracle and Firebird: update only; Firebird writes <c>FOR UPDATE WITH LOCK</c>). SQL Server locks through table hints instead. </summary>
        public bool SupportsRowLocking() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.MySql or SqlDialectKind.Oracle or SqlDialectKind.Firebird;

        /// <summary> SQL Server table hints: <c>FROM t WITH (NOLOCK)</c>. </summary>
        public bool SupportsTableHints() => dialect is SqlDialectKind.SqlServer;


        // ---- SELECT ----


        /// <summary> MySQL is the only target without <c>FULL OUTER JOIN</c> (SQLite has it since 3.39). </summary>
        public bool SupportsFullOuterJoin() => dialect is not SqlDialectKind.MySql;

        public bool SupportsNaturalJoin() => dialect is not SqlDialectKind.SqlServer;
        public bool SupportsUsingJoin()   => dialect is not SqlDialectKind.SqlServer;

        /// <summary> Correlated table sources: <c>CROSS / OUTER APPLY</c> (SQL Server, Oracle) or <c>JOIN LATERAL</c> (PostgreSQL, MySQL, Firebird). SQLite has neither. </summary>
        public bool SupportsLateral() => dialect is not SqlDialectKind.Sqlite;

        public bool SupportsDistinctOn() => dialect is SqlDialectKind.PostgreSql;

        public bool SupportsGroupingSets() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.SqlServer or SqlDialectKind.Oracle;

        /// <summary> <c>ROLLUP</c>: <c>GROUP BY ROLLUP (...)</c>, or MySQL's <c>GROUP BY ... WITH ROLLUP</c>. </summary>
        public bool SupportsRollup() => dialect.SupportsGroupingSets() || dialect is SqlDialectKind.MySql;

        /// <summary> <c>INTERSECT</c> / <c>EXCEPT</c> (every target but Firebird). </summary>
        public bool SupportsIntersectExcept() => dialect is not SqlDialectKind.Firebird;

        /// <summary> <c>INTERSECT ALL</c> / <c>EXCEPT ALL</c>. </summary>
        public bool SupportsIntersectExceptAll() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.MySql or SqlDialectKind.Oracle;

        /// <summary> <c>ORDER BY x NULLS FIRST | LAST</c>; SQL Server and MySQL get an equivalent extra sort key. </summary>
        public bool SupportsNullsOrdering() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Oracle or SqlDialectKind.Sqlite or SqlDialectKind.Firebird;

        /// <summary> Named windows: <c>WINDOW w AS (...)</c> (SQL Server 2022, Oracle 21c, Firebird 3+). </summary>
        public bool SupportsWindowClause() => true;

        /// <summary> <c>GROUPS</c> window frames. </summary>
        public bool SupportsGroupsFrame() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite or SqlDialectKind.Oracle;

        /// <summary> Aggregate <c>FILTER (WHERE ...)</c>; other dialects get the equivalent <c>CASE WHEN ... END</c> argument. </summary>
        public bool SupportsFilterClause() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite;

        /// <summary> <c>NTH_VALUE</c>. </summary>
        public bool SupportsNthValue() => dialect is not SqlDialectKind.SqlServer;

        /// <summary> Ordered string aggregation (<c>STRING_AGG ... ORDER BY</c> / <c>WITHIN GROUP</c> / <c>GROUP_CONCAT ... ORDER BY</c> / <c>LISTAGG</c>); Firebird's <c>LIST</c> takes no order. </summary>
        public bool SupportsOrderedStringAgg() => dialect is not SqlDialectKind.Firebird;

        public bool SupportsArrayAgg() => dialect is SqlDialectKind.PostgreSql;

        /// <summary> <c>CTE AS [NOT] MATERIALIZED</c>. </summary>
        public bool SupportsMaterializedCte() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite;

        /// <summary> <c>WITH RECURSIVE</c> (SQL Server and Oracle recurse with a plain <c>WITH</c>). </summary>
        public bool RecursiveKeywordRequired() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite or SqlDialectKind.MySql or SqlDialectKind.Firebird;

        /// <summary> A <c>WITH</c> prefix on INSERT / UPDATE / DELETE (MySQL: UPDATE and DELETE only). </summary>
        public bool SupportsDmlWith( ClauseFlags statement ) => dialect switch
                                                                {
                                                                    SqlDialectKind.PostgreSql or SqlDialectKind.SqlServer or SqlDialectKind.Sqlite => true,
                                                                    SqlDialectKind.MySql                                                           => statement is ClauseFlags.Update or ClauseFlags.Delete,
                                                                    _                                                                              => false
                                                                };


        // ---- predicates ----

        /// <summary> <c>ILIKE</c>; other dialects get <c>LOWER(x) LIKE LOWER(pattern)</c>. </summary>
        public bool SupportsIlike() => dialect is SqlDialectKind.PostgreSql;

        public bool SupportsGlob()      => dialect is SqlDialectKind.Sqlite;
        public bool SupportsSimilarTo() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Firebird;

        /// <summary> <c>= ANY(array)</c> with an array parameter. </summary>
        public bool SupportsAnyArray() => dialect is SqlDialectKind.PostgreSql;

        /// <summary> <c>IS [NOT] DISTINCT FROM</c> is written natively (PostgreSQL, SQL Server 2022, Firebird), as <c>IS [NOT]</c> (SQLite), <c>&lt;=&gt;</c> (MySQL) or a <c>DECODE</c> (Oracle). </summary>
        public bool SupportsIsDistinctFrom() => true;

        /// <summary> JSON field access (<c>-&gt;</c> / <c>-&gt;&gt;</c> / <c>JSON_VALUE</c> / <c>JSON_QUERY</c>); Firebird has no JSON. </summary>
        public bool SupportsJson() => dialect is not SqlDialectKind.Firebird;

        /// <summary> JSON containment (<c>@&gt;</c> / <c>JSON_CONTAINS</c>). </summary>
        public bool SupportsJsonContains() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.MySql;


        // ---- INSERT / UPDATE / DELETE / MERGE ----

        /// <summary> <c>INSERT ... DEFAULT VALUES</c> (MySQL writes <c>() VALUES ()</c>; Oracle has neither). </summary>
        public bool SupportsDefaultValues() => dialect is not SqlDialectKind.Oracle;

        /// <summary> <c>INSERT ... ON CONFLICT (...) DO NOTHING | DO UPDATE</c>; MySQL gets <c>ON DUPLICATE KEY UPDATE</c> (its conflict target is any unique key). </summary>
        public bool SupportsOnConflict() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite or SqlDialectKind.MySql;

        public bool SupportsOverridingSystemValue() => dialect is SqlDialectKind.PostgreSql;

        /// <summary> <c>MERGE</c> (PostgreSQL 15+, SQL Server, Oracle, Firebird). </summary>
        public bool SupportsMerge() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.SqlServer or SqlDialectKind.Oracle or SqlDialectKind.Firebird;

        /// <summary> <c>UPDATE ... SET (a, b) = (...)</c>; other dialects get one assignment per column (values only). </summary>
        public bool SupportsRowValueSet() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite;

        /// <summary> Updating from other tables: <c>UPDATE ... FROM</c> (PostgreSQL, SQLite 3.33, SQL Server) or MySQL's <c>UPDATE t, other SET</c>. </summary>
        public bool SupportsUpdateFrom() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite or SqlDialectKind.SqlServer or SqlDialectKind.MySql;

        /// <summary> Deleting with other tables: <c>DELETE ... USING</c> (PostgreSQL), SQL Server's <c>DELETE FROM t FROM other</c>, MySQL's <c>DELETE t FROM t, other</c>. </summary>
        public bool SupportsDeleteUsing() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.SqlServer or SqlDialectKind.MySql;

        /// <summary> <c>TRUNCATE TABLE</c>; SQLite and Firebird get <c>DELETE FROM</c>. </summary>
        public bool SupportsTruncate() => dialect is not SqlDialectKind.Sqlite and not SqlDialectKind.Firebird;


        // ---- DDL ----

        /// <summary> <c>CREATE TABLE IF NOT EXISTS</c> (SQL Server gets an <c>IF OBJECT_ID(...) IS NULL</c> guard; Firebird has neither). </summary>
        public bool SupportsCreateIfNotExists() => dialect is not SqlDialectKind.Firebird;

        /// <summary> <c>DROP ... IF EXISTS</c>. </summary>
        public bool SupportsDropIfExists() => dialect is not SqlDialectKind.Firebird;

        /// <summary> <c>DROP TABLE ... CASCADE</c> (Oracle: <c>CASCADE CONSTRAINTS</c>). </summary>
        public bool SupportsDropCascade() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Oracle or SqlDialectKind.MySql;

        public bool SupportsIndexInclude()     => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.SqlServer;
        public bool SupportsPartialIndex()     => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite or SqlDialectKind.SqlServer;
        public bool SupportsIndexMethod()      => dialect is SqlDialectKind.PostgreSql;
        public bool SupportsIndexIfNotExists() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Sqlite or SqlDialectKind.Oracle;

        /// <summary> Replacing a view in place: <c>CREATE OR REPLACE VIEW</c> / <c>CREATE OR ALTER VIEW</c> (SQL Server, Firebird). </summary>
        public bool SupportsReplaceView() => dialect is not SqlDialectKind.Sqlite;

        /// <summary> <c>ALTER TABLE ... ADD / DROP CONSTRAINT</c> (SQLite can only rebuild the table). </summary>
        public bool SupportsAlterConstraints() => dialect is not SqlDialectKind.Sqlite;

        /// <summary> Changing a column's type in place. </summary>
        public bool SupportsAlterColumnType() => dialect is not SqlDialectKind.Sqlite;

        /// <summary> Renaming a table (SQL Server through <c>sp_rename</c>). </summary>
        public bool SupportsRenameTable() => dialect is not SqlDialectKind.Firebird;


        // ---- literals and identifiers ----

        /// <summary> Whether <c>TRUE</c> / <c>FALSE</c> are written; otherwise <c>1</c> / <c>0</c>. Firebird's <c>BOOLEAN</c> doesn't compare with integers. </summary>
        public bool SupportsBooleanLiteral() => dialect is SqlDialectKind.PostgreSql or SqlDialectKind.Firebird;

        public bool FoldsIdentifiersToLower() => dialect is SqlDialectKind.PostgreSql;

        /// <param name="namedPostgres"> PostgreSQL as <c>@p0</c> instead of <c>$1</c> (<see cref="SqlBuilderOptions.NamedPostgresParameters"/>). </param>
        public string ParameterName( int ordinal, bool namedPostgres = false )
        {
            ParameterStyle style = dialect.StyleOf(namedPostgres);

            return (uint)ordinal < TOKENS
                       ? __tokens[(int)style][ordinal] ??= Token(style, ordinal)
                       : Token(style, ordinal);
        }

        private ParameterStyle StyleOf( bool namedPostgres ) => dialect switch
                                                                {
                                                                    SqlDialectKind.PostgreSql when !namedPostgres => ParameterStyle.Dollar,
                                                                    SqlDialectKind.Oracle                         => ParameterStyle.Colon,
                                                                    _                                             => ParameterStyle.At
                                                                };

        public (char Open, char Close) QuoteChars()
        {
            return dialect switch
                   {
                       SqlDialectKind.SqlServer  => ( '[', ']' ),
                       SqlDialectKind.MySql      => ( '`', '`' ),
                       SqlDialectKind.PostgreSql => ( '\0', '\0' ),
                       _                         => ( '"', '"' ) // SQLite, Oracle, Firebird: quoted identifiers keep their case
                   };
        }

        public string Fold( string name ) => dialect is SqlDialectKind.PostgreSql
                                                 ? FoldPostgres(name)
                                                 : name;
    }



    // Parameter tokens, cached per style (the dialects that share a style share its strings): an extension block can't hold fields or types.
    private enum ParameterStyle
    {
        Dollar = 0, // $1
        Colon  = 1, // :p0 
        At     = 2  // @p0
    }



    private const           int         TOKENS   = 256;
    private static readonly string?[][] __tokens = [new string?[TOKENS], new string?[TOKENS], new string?[TOKENS]];

    private static string Token( ParameterStyle style, int ordinal ) => style switch
                                                                        {
                                                                            ParameterStyle.Dollar => $"${ordinal + 1}",
                                                                            ParameterStyle.Colon  => $":p{ordinal}",
                                                                            ParameterStyle.At     => $"@p{ordinal}",
                                                                            _                     => throw new ArgumentOutOfRangeException(nameof(style), style, null)
                                                                        };


    /// <summary> <see cref="PostgresIdentifierFolder"/>, cached per folder and looked up by span, so folding a known name allocates nothing. </summary>
    internal static string FoldPostgres( scoped ReadOnlySpan<char> name )
    {
        Func<string, string> folder = PostgresIdentifierFolder;
        FoldCache            cache  = __fold;
        if ( !ReferenceEquals(cache.Folder, folder) ) { __fold = cache = new FoldCache(folder); }

        if ( cache.Lookup.TryGetValue(name, out string? folded) ) { return folded; }

        string key = name.ToString();
        folded = folder(key);

        // Bounded: identifiers built from arbitrary input shouldn't grow it forever.
        if ( cache.Map.Count < FoldCache.CAPACITY ) { cache.Map.TryAdd(key, folded); }

        return folded;
    }

    private static FoldCache __fold = new(DefaultSnakeCaseLower);



    private sealed class FoldCache
    {
        public const int CAPACITY = 4096;

        public readonly Func<string, string>                                                     Folder;
        public readonly ConcurrentDictionary<string, string>                                     Map;
        public readonly ConcurrentDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> Lookup;

        public FoldCache( Func<string, string> folder )
        {
            Folder = folder;
            Map    = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            Lookup = Map.GetAlternateLookup<ReadOnlySpan<char>>();
        }
    }



    internal static string DefaultSnakeCaseLower( string name )
    {
        if ( string.IsNullOrEmpty(name) ) { return name; }

        ValueStringBuilder builder = new(name.Length + 8);

        for ( int i = 0; i < name.Length; i++ )
        {
            char c = name[i];

            if ( char.IsUpper(c) )
            {
                if ( i > 0 && name[i - 1] is not '_' ) { builder.Append('_'); }

                builder.Append(char.ToLowerInvariant(c));
            }
            else { builder.Append(c); }
        }

        return builder.ToString();
    }
}
