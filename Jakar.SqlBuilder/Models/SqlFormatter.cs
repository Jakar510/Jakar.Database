// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 10/26/2026

using System.Runtime.CompilerServices;



namespace Jakar.SqlBuilder;


/// <summary> How a built statement is laid out. </summary>
public enum SqlLayout
{
    /// <summary> One line, single spaces: skips the layout pass, for when nobody reads the SQL. </summary>
    Compact,

    /// <summary> The default. For people to read: each clause on its own line, multi-item lists one item per line, column lists and subqueries indented. See <see cref="SqlFormatter"/>. </summary>
    Readable
}



/// <summary>
///     Lays SQL out for people to read. Only the whitespace <i>between</i> tokens changes: string literals, quoted identifiers, dollar-quoted bodies and comments are copied as they are, and
///     no token is added, dropped or reordered, so the statement means exactly what it did. Formatting formatted SQL returns it unchanged.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item> Each clause (<c>FROM</c>, <c>WHERE</c>, a join, <c>ORDER BY</c>, <c>SET</c>, <c>VALUES</c>, <c>RETURNING</c>, a set operation, ...) starts a line. </item>
///         <item> A list clause with more than one item (<c>SELECT</c>, <c>SET</c>, <c>GROUP BY</c>, <c>ORDER BY</c>, CTEs, <c>VALUES</c> rows, ...) puts each item on its own indented line. </item>
///         <item> <c>AND</c> / <c>OR</c> in a <c>WHERE</c>, <c>HAVING</c> or join condition start an indented line (not inside <c>CASE</c>, nor a <c>BETWEEN</c>'s <c>AND</c>). </item>
///         <item> Subqueries, and the column lists of <c>CREATE TABLE</c>, <c>INSERT</c> and a single <c>VALUES</c> row, open an indented block closed by <c>)</c> on its own line. </item>
///         <item> Everything else (calls, <c>IN</c> lists, predicate groups, type arguments) stays inline. </item>
///     </list>
///     The rules target statements built by <see cref="SqlBuilder"/>; any other SQL is still only re-spaced, never changed.
/// </remarks>
public static class SqlFormatter
{
    private const int INDENT = 4;


    /// <summary> <paramref name="sql"/>, laid out for people to read. <paramref name="dialect"/> only matters for MySQL, whose strings treat <c>\</c> as an escape. </summary>
    public static string Format( string sql, SqlDialectKind dialect = SqlDialectKind.NotSet )
    {
        ArgumentNullException.ThrowIfNull(sql);
        return Format(sql.AsSpan(), dialect).ToString();
    }

    /// <summary> <paramref name="sql"/>, laid out for people to read, in this thread's scratch buffer: valid until the next <c>Format</c> on the thread. </summary>
    internal static ReadOnlySpan<char> Format( ReadOnlySpan<char> sql, SqlDialectKind dialect )
    {
        // Per-thread scratch (Format never re-enters itself): after the first statement, laying one out allocates nothing.
        Token[] tokens = Scratch(ref __tokenScratch, sql.Length + 1);
        int     count  = Tokenize(sql, dialect, tokens);
        Frame[] frames = Scratch(ref __frameScratch, count + 1);
        int[]   stack  = Scratch(ref __stackScratch, count + 1);
        Output  output = new(Scratch(ref __outputScratch, sql.Length + sql.Length / 2 + 16));

        Layout layout = new(sql, tokens.AsSpan(0, count), frames.AsSpan(0, count + 1));
        layout.Analyze(stack);
        layout.Write(ref output);
        return output.Text;
    }

    private const int SCRATCH_MAX = 8 * 1024; // longer statements get arrays of their own, so a thread doesn't keep a huge one

    [ThreadStatic] private static Token[]? __tokenScratch;
    [ThreadStatic] private static Frame[]? __frameScratch;
    [ThreadStatic] private static int[]?   __stackScratch;
    [ThreadStatic] private static char[]?  __outputScratch;

    /// <summary> The formatted text, written into this thread's scratch buffer (grown as needed). </summary>
    private struct Output( char[] chars )
    {
        private char[] __chars = chars;
        private int    __length;

        public readonly ReadOnlySpan<char> Text => new(__chars, 0, __length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append( scoped ReadOnlySpan<char> text )
        {
            if ( __length + text.Length > __chars.Length ) { Grow(text.Length); }

            text.CopyTo(new Span<char>(__chars, __length, text.Length));
            __length += text.Length;
        }

        /// <summary> A new line, indented <paramref name="spaces"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Break( int spaces )
        {
            if ( __length + spaces + 1 > __chars.Length ) { Grow(spaces + 1); }

            __chars[__length++] = '\n';
            new Span<char>(__chars, __length, spaces).Fill(' ');
            __length += spaces;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow( int additional )
        {
            char[] grown = new char[Math.Max(__chars.Length * 2, __length + additional)];
            __chars.AsSpan(0, __length).CopyTo(grown);
            __chars = grown;
            if ( grown.Length <= SCRATCH_MAX * 4 ) { __outputScratch = grown; }
        }
    }

    private static T[] Scratch<T>( ref T[]? cache, int length )
    {
        if ( cache is not null && cache.Length >= length ) { return cache; }

        T[] array = new T[Math.Max(length, Math.Min(( cache?.Length ?? 128 ) * 2, SCRATCH_MAX))];
        if ( array.Length <= SCRATCH_MAX ) { cache = array; }

        return array;
    }


    // ---- tokens ----

    private static int Tokenize( scoped ReadOnlySpan<char> sql, SqlDialectKind dialect, Span<Token> tokens )
    {
        int count = 0;
        int i     = 0;

        while ( i < sql.Length )
        {
            char c = sql[i];

            if ( ( c <= ' ' || c > 127 ) && char.IsWhiteSpace(c) )
            {
                i++;
                continue;
            }

            int       start = i;
            TokenKind kind  = TokenKind.Opaque;

            switch ( c )
            {
                case '\'':
                    // MySQL strings, and PostgreSQL E'...' strings, treat a backslash as an escape.
                    bool backslash = dialect is SqlDialectKind.MySql || ( start > 0 && sql[start - 1] is 'E' or 'e' && ( start is 1 || !IsWordChar(sql[start - 2]) ) );
                    i = SkipQuoted(sql, i, '\'', backslash);
                    break;

                case '"':
                case '`':
                    i = SkipQuoted(sql, i, c, false);
                    break;

                case '[':
                    i = SkipQuoted(sql, i, ']', false);
                    break;

                case '-' when i + 1 < sql.Length && sql[i + 1] == '-':
                    int newLine = sql[i..].IndexOf('\n');
                    i    = newLine < 0 ? sql.Length : i + newLine;
                    kind = TokenKind.Comment;
                    break;

                case '/' when i + 1 < sql.Length && sql[i + 1] == '*':
                    int close = sql[( i + 2 )..].IndexOf("*/");
                    i    = close < 0 ? sql.Length : i + 2 + close + 2;
                    kind = TokenKind.Comment;
                    break;

                case '$' when DollarTag(sql, i) is > 0 and var tagLength:
                    // $$ ... $$ or $tag$ ... $tag$: a PostgreSQL body, copied as it is.
                    int body = sql[( i + tagLength )..].IndexOf(sql.Slice(i, tagLength));
                    i = body < 0 ? sql.Length : i + tagLength + body + tagLength;
                    break;

                default:
                    if ( IsWordChar(c) )
                    {
                        // ASCII runs at once; a non-ASCII letter or digit continues the word.
                        while ( true )
                        {
                            int run = sql[i..].IndexOfAnyExcept(__wordAscii);
                            i = run < 0 ? sql.Length : i + run;
                            if ( i >= sql.Length || sql[i] < 128 || !char.IsLetterOrDigit(sql[i]) ) { break; }

                            i++;
                        }

                        kind = TokenKind.Word;
                    }
                    else
                    {
                        i++;
                        kind = TokenKind.Punct;
                    }

                    break;
            }

            tokens[count++] = new Token(start, i - start, kind);
        }

        return count;
    }

    /// <summary> Where the quoted text starting at <paramref name="i"/> ends: a doubled <paramref name="close"/> is an escaped one; an unterminated quote runs to the end. </summary>
    private static int SkipQuoted( scoped ReadOnlySpan<char> sql, int i, char close, bool backslash )
    {
        for ( i++; i < sql.Length; i++ )
        {
            char c = sql[i];

            if ( backslash && c == '\\' )
            {
                i++;
                continue;
            }

            if ( c != close ) { continue; }

            if ( i + 1 < sql.Length && sql[i + 1] == close )
            {
                i++;
                continue;
            }

            return i + 1;
        }

        return sql.Length;
    }

    /// <summary> The length of the <c>$tag$</c> at <paramref name="i"/>, or 0 when it's a parameter (<c>$1</c>) or a plain word. </summary>
    private static int DollarTag( scoped ReadOnlySpan<char> sql, int i )
    {
        int j = i + 1;
        if ( j < sql.Length && char.IsAsciiDigit(sql[j]) ) { return 0; }

        while ( j < sql.Length && ( char.IsAsciiLetterOrDigit(sql[j]) || sql[j] == '_' ) ) { j++; }

        return j < sql.Length && sql[j] == '$'
                   ? j - i + 1
                   : 0;
    }

    private static readonly SearchValues<char> __wordAscii = SearchValues.Create("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_@#$:");

    private static bool IsWordChar( char c ) => c < 128
                                                    ? __wordAscii.Contains(c)
                                                    : char.IsLetterOrDigit(c);



    private enum TokenKind : byte { Word, Punct, Opaque, Comment }



    private enum ParenKind : byte
    {
        /// <summary> A call, an <c>IN</c> list, a group: kept on its line. </summary>
        Inline,

        /// <summary> A subquery: its clauses start lines, indented one level. </summary>
        Query,

        /// <summary> A column list (or a single <c>VALUES</c> row): one item per line, indented one level. </summary>
        List
    }



    private enum ClauseKind : byte
    {
        Other,

        /// <summary> A clause whose comma-separated items go one per line when there's more than one. </summary>
        List,

        /// <summary> A condition: its top-level <c>AND</c> / <c>OR</c> start lines. </summary>
        Condition
    }



    [Flags]
    private enum TokenFlags : byte
    {
        None           = 0,
        ClauseStart    = 1 << 0,
        StatementStart = 1 << 1,
        HasComma       = 1 << 2, // on a clause start: the clause has a comma at its own level
        SawTable       = 1 << 3, // on a clause start: TABLE came before the clause's first parenthesis
        ItemsStart     = 1 << 4, // the first item of a multi-item list clause
        ContainsBlock  = 1 << 5  // on a '(': a subquery is somewhere inside, so its tokens need a look (an inline group without one is skipped whole)
    }



    private struct Token( int start, int length, TokenKind kind )
    {
        public readonly int        Start      = start;
        public readonly int        Length     = length;
        public readonly TokenKind  Kind       = kind;
        public          int        Frame      = -1; // the '(' this token is inside, or -1 at the top level ('(' and ')' belong to the frame around them)
        public          int        Match      = -1; // for '(' and ')': the matching parenthesis
        public          int        Owner      = -1; // the clause start (in the same frame) this token belongs to
        public          int        FirstParen = -1; // on a clause start: the clause's first '(' at its own level
        public          ParenKind  Paren;
        public          ClauseKind Clause;
        public          TokenFlags Flags;

        public readonly int End => Start + Length;

        /// <summary> A bit test, not <see cref="Enum.HasFlag"/>, which boxes in unoptimized (Debug) code. </summary>
        public readonly bool Has( TokenFlags flag ) => ( Flags & flag ) != 0;
    }



    private struct Frame
    {
        public int  Clause;    // while analyzing: the current clause start
        public bool Expect;    // while analyzing: the next token starts a statement
        public int  Base;      // while writing: the indent of the line the '(' is on
        public int  CaseDepth; // while writing: open CASE expressions at this level
        public bool Between;   // while writing: the next AND belongs to a BETWEEN
    }



    private readonly ref struct Layout( ReadOnlySpan<char> sql, Span<Token> tokens, Span<Frame> frames )
    {
        private readonly ReadOnlySpan<char> __sql    = sql;
        private readonly Span<Token>        __tokens = tokens;
        private readonly Span<Frame>        __frames = frames; // indexed by frame + 1: [0] is the top level


        // ---- analysis ----

        public void Analyze( Span<int> stack )
        {
            // Parentheses and frames.
            int depth   = 0;
            int current = -1;

            for ( int t = 0; t < __tokens.Length; t++ )
            {
                ref Token token = ref __tokens[t];
                token.Frame = current;

                if ( IsPunct(t, '(') )
                {
                    stack[depth++] = current;
                    current        = t;
                }
                else if ( IsPunct(t, ')') && current >= 0 )
                {
                    int open = current;
                    token.Match         = open;
                    __tokens[open].Match = t;
                    current             = stack[--depth];
                    token.Frame         = current;

                    // A subquery opens with SELECT, WITH or VALUES; the groups around it are marked, so the rest can be skipped whole.
                    int next = Next(open);
                    if ( next != t && ( Is(next, "SELECT") || Is(next, "WITH") || Is(next, "VALUES") ) )
                    {
                        __tokens[open].Paren = ParenKind.Query;
                        for ( int f = current; f >= 0 && !__tokens[f].Has(TokenFlags.ContainsBlock); f = __tokens[f].Frame ) { __tokens[f].Flags |= TokenFlags.ContainsBlock; }
                    }
                }
                else if ( IsPunct(t, ',') && current >= 0 ) { __tokens[current].Flags |= TokenFlags.HasComma; } // on a '(': its list has more than one item
            }

            // Clauses, at the top level and in subqueries.
            for ( int f = 0; f < __frames.Length; f++ ) { __frames[f] = new Frame { Clause = -1, Expect = true }; }

            for ( int t = 0; t < __tokens.Length; t++ )
            {
                ref Token token = ref __tokens[t];
                if ( token.Kind is TokenKind.Comment || !IsStatementLevel(token.Frame) ) { continue; }

                ref Frame frame = ref __frames[token.Frame + 1];

                if ( frame.Expect )
                {
                    frame.Expect =  false;
                    token.Flags  |= TokenFlags.StatementStart;
                    frame.Clause =  -1;

                    if ( token.Kind is TokenKind.Word )
                    {
                        token.Flags  |= TokenFlags.ClauseStart;
                        token.Clause =  KindOf(t);
                        frame.Clause =  t;
                    }
                }
                else if ( token.Kind is TokenKind.Word && IsClauseStart(t) )
                {
                    token.Flags  |= TokenFlags.ClauseStart;
                    token.Clause =  KindOf(t);
                    frame.Clause =  t;
                }

                token.Owner = frame.Clause;

                if ( IsPunct(t, ';') )
                {
                    frame.Expect = true;
                    continue;
                }

                if ( frame.Clause < 0 || frame.Clause == t ) { continue; }

                ref Token clause = ref __tokens[frame.Clause];

                if ( IsPunct(t, ',') ) { clause.Flags |= TokenFlags.HasComma; }
                else if ( IsPunct(t, '(') && clause.FirstParen < 0 ) { clause.FirstParen = t; }
                else if ( clause.FirstParen < 0 && Is(t, "TABLE") ) { clause.Flags |= TokenFlags.SawTable; }

                if ( IsSkippable(t) ) { t = token.Match - 1; } // nothing inside is at statement level
            }

            // Where each multi-item list starts, and which parentheses are column lists.
            for ( int t = 0; t < __tokens.Length; t++ )
            {
                ref Token token = ref __tokens[t];

                if ( token.Has(TokenFlags.ClauseStart) && token.Clause is ClauseKind.List && token.Has(TokenFlags.HasComma) && ItemsStart(t) is >= 0 and var items )
                {
                    __tokens[items].Flags |= TokenFlags.ItemsStart;
                }

                if ( !IsPunct(t, '(') || token.Paren is not ParenKind.Inline || token.Match < 0 || token.Owner < 0 || !IsStatementLevel(token.Frame) )
                {
                    if ( IsSkippable(t) ) { t = token.Match - 1; }

                    continue;
                }

                // A one-item list stays inline: INSERT INTO t (a), VALUES (1).
                ref Token owner = ref __tokens[token.Owner];
                if ( owner.FirstParen != t || !token.Has(TokenFlags.HasComma) ) { continue; }

                if ( ( owner.Has(TokenFlags.StatementStart) && Is(token.Owner, "CREATE") && owner.Has(TokenFlags.SawTable) ) ||
                     Is(token.Owner, "INSERT")                                                                                                 ||
                     Is(token.Owner, "REPLACE")                                                                                                ||
                     ( Is(token.Owner, "VALUES") && !owner.Has(TokenFlags.HasComma) ) ) { token.Paren = ParenKind.List; }
            }
        }

        private bool IsStatementLevel( int frame ) => frame < 0 || __tokens[frame].Paren is ParenKind.Query;

        /// <summary> An inline group (a call, an IN list, a row) with no subquery inside: nothing in it starts a line, so analysis and writing jump to its ')'. </summary>
        private bool IsSkippable( int t ) => __tokens[t].Kind is TokenKind.Punct && __tokens[t].Match > t + 1 && __tokens[t].Paren is ParenKind.Inline && !__tokens[t].Has(TokenFlags.ContainsBlock) && __sql[__tokens[t].Start] == '(';

        private bool IsClauseStart( int t )
        {
            int next = Next(t);
            int prev = Prev(t);

            // A keyword written as a call (VALUES(a), LEFT(name, 3)) is a function.
            if ( IsPunct(next, '(') && __tokens[next].Start == __tokens[t].End ) { return false; }

            ReadOnlySpan<char> word = Text(t);

            return word switch
                   {
                       "SELECT" or "WHERE" or "HAVING" or "WINDOW" or "QUALIFY" or "RETURNING" or "OUTPUT" or "LIMIT" or "OFFSET" or "FETCH" or "UNION" or "INTERSECT" or "EXCEPT" or "MINUS" => true,
                       "FROM"                                                                                => !Is(prev, "DELETE") && !Is(prev, "DISTINCT"), // DELETE FROM, IS DISTINCT FROM
                       // MERGE's actions stay on their WHEN line; DO UPDATE SET stays together; ON DELETE SET NULL is a foreign-key action.
                       "VALUES"                                                                              => !Is(prev, "DEFAULT") && !IsStatement(t, "MERGE"),
                       "SET"                                                                                 => !( Is(Prev(prev), "ON") && ( Is(prev, "DELETE") || Is(prev, "UPDATE") ) ) && !( Is(prev, "UPDATE") && Is(Prev(prev), "DO") ) && !IsStatement(t, "MERGE"),
                       "USING"                                                                               => IsStatement(t, "MERGE") || IsStatement(t, "DELETE"),
                       "GROUP" or "ORDER"                                                                    => Is(next, "BY"),
                       "FOR"                                                                                 => Is(next, "UPDATE") || Is(next, "SHARE") || Is(next, "NO") || Is(next, "KEY") || Is(next, "READ") || Is(next, "JSON") || Is(next, "XML") || Is(next, "BROWSE"),
                       "ON"                                                                                  => Is(next, "CONFLICT") || Is(next, "DUPLICATE"),
                       "DO"                                                                                  => Is(next, "UPDATE") || Is(next, "NOTHING"),
                       "WHEN"                                                                                => Is(next, "MATCHED") || ( Is(next, "NOT") && Is(Next(next), "MATCHED") ),
                       "DEFAULT"                                                                             => Is(next, "VALUES"),
                       // The statement after a CTE, or after T-SQL's IF (...) guard.
                       "INSERT" or "UPDATE" or "DELETE" or "MERGE" or "CREATE" or "DROP" or "ALTER"         => IsPunct(prev, ')') && ( IsStatement(t, "WITH") || IsStatement(t, "IF") ),
                       "JOIN"                                                                                => !IsJoinPrefix(prev),
                       "INNER" or "LEFT" or "RIGHT" or "FULL" or "CROSS" or "NATURAL" or "OUTER" or "STRAIGHT_JOIN" => !IsJoinPrefix(prev) && ( IsJoin(next) || ( IsJoinPrefix(next) && IsJoin(Next(next)) ) ),
                       _                                                                                     => false
                   };
        }

        private bool IsJoin( int t )       => Is(t, "JOIN") || Is(t, "APPLY");
        private bool IsJoinPrefix( int t ) => Is(t, "INNER") || Is(t, "LEFT") || Is(t, "RIGHT") || Is(t, "FULL") || Is(t, "CROSS") || Is(t, "NATURAL") || Is(t, "OUTER");

        /// <summary> Whether the statement <paramref name="t"/> is in starts with <paramref name="verb"/>. </summary>
        private bool IsStatement( int t, string verb )
        {
            for ( int i = t - 1; i >= 0; i-- )
            {
                if ( __tokens[i].Frame != __tokens[t].Frame ) { continue; }

                if ( __tokens[i].Has(TokenFlags.StatementStart) ) { return Is(i, verb); }
            }

            return false;
        }

        private ClauseKind KindOf( int t ) => Text(t) switch
                                              {
                                                  "SELECT" or "FROM" or "SET" or "RETURNING" or "OUTPUT" or "VALUES" or "WINDOW" or "WITH" or "GROUP" or "ORDER" or "DO" or "ON" => ClauseKind.List,
                                                  "WHERE" or "HAVING" or "QUALIFY" or "JOIN" or "INNER" or "LEFT" or "RIGHT" or "FULL" or "CROSS" or "NATURAL" or "OUTER" or "STRAIGHT_JOIN" => ClauseKind.Condition,
                                                  _ => ClauseKind.Other
                                              };

        /// <summary> The first item of the list clause at <paramref name="t"/>: past <c>BY</c>, <c>RECURSIVE</c>, and <c>SELECT</c>'s <c>DISTINCT [ON (...)]</c> / <c>ALL</c> / <c>TOP (n) [PERCENT] [WITH TIES]</c>. </summary>
        private int ItemsStart( int t )
        {
            int s = Next(t);

            switch ( Text(t) )
            {
                case "GROUP" or "ORDER":
                    return Next(s);

                case "DO": // DO UPDATE SET a = ..., b = ...
                    return Is(Next(s), "SET")
                               ? Next(Next(s))
                               : s;

                case "ON": // ON DUPLICATE KEY UPDATE a = ..., b = ...
                    return Is(s, "DUPLICATE")
                               ? Next(Next(Next(s)))
                               : s;

                case "WITH":
                    return Is(s, "RECURSIVE")
                               ? Next(s)
                               : s;

                case "SELECT":
                    while ( true )
                    {
                        if ( Is(s, "DISTINCT") || Is(s, "ALL") )
                        {
                            s = Next(s);
                            if ( Is(s, "ON") && IsPunct(Next(s), '(') && __tokens[Next(s)].Match >= 0 ) { s = Next(__tokens[Next(s)].Match); }

                            continue;
                        }

                        if ( Is(s, "TOP") )
                        {
                            s = Next(s);
                            s = IsPunct(s, '(') && __tokens[s].Match >= 0
                                    ? Next(__tokens[s].Match)
                                    : Next(s);

                            if ( Is(s, "PERCENT") ) { s = Next(s); }

                            if ( Is(s, "WITH") && Is(Next(s), "TIES") ) { s = Next(Next(s)); }

                            continue;
                        }

                        return s;
                    }

                default:
                    return s;
            }
        }


        // ---- writing ----

        public void Write( ref Output output )
        {
            bool newLines = __sql.Contains('\n');
            int  line     = 0; // the indent level of the line being written
            int  end      = 0; // where the previous token ends
            int  copied   = 0; // how much of the SQL is in the output: unchanged text is copied in runs, up to each line break
            __frames[0].Base = 0;

            for ( int t = 0; t < __tokens.Length; t++ )
            {
                ref readonly Token token = ref __tokens[t];
                int                level = Indent(t);

                if ( level >= 0 )
                {
                    output.Append(__sql[copied..end]);
                    output.Break(level * INDENT);
                    copied = token.Start;
                    line   = level;
                }
                else if ( newLines && token.Start > end )
                {
                    // SQL that already has line breaks keeps them; follow its indent.
                    ReadOnlySpan<char> gap     = __sql[end..token.Start];
                    int                newLine = gap.LastIndexOf('\n');
                    if ( newLine >= 0 ) { line = ( gap.Length - newLine - 1 ) / INDENT; }
                }

                end = token.End;

                if ( token.Kind is TokenKind.Punct )
                {
                    if ( __sql[token.Start] == '(' ) { __frames[t + 1] = new Frame { Base = line }; }

                    // An inline group copies through untouched (unless the SQL has its own line breaks to follow).
                    if ( !newLines && IsSkippable(t) )
                    {
                        t   = token.Match - 1;
                        end = __tokens[t].End;
                    }
                }
                else if ( token.Kind is TokenKind.Word ) { Track(t); }
            }

            output.Append(__sql[copied..]);
        }

        /// <summary> The indent level to break the line at before <paramref name="t"/>, or -1 to keep the original spacing. </summary>
        private int Indent( int t )
        {
            ref readonly Token token = ref __tokens[t];
            if ( token.Kind is TokenKind.Comment ) { return -1; }

            int prev = Prev(t);
            if ( prev < 0 ) { return -1; }

            // A block's closing parenthesis, on its own line (an empty block stays "()").
            if ( IsPunct(t, ')') && token.Match >= 0 && __tokens[token.Match].Paren is not ParenKind.Inline )
            {
                return prev == token.Match
                           ? -1
                           : __frames[token.Match + 1].Base;
            }

            // A block's first token.
            if ( IsPunct(prev, '(') && __tokens[prev].Paren is not ParenKind.Inline && token.Frame == prev ) { return __frames[prev + 1].Base + 1; }

            int frame = token.Frame;

            // A column list's next item.
            if ( IsPunct(prev, ',') && __tokens[prev].Frame == frame && frame >= 0 && __tokens[frame].Paren is ParenKind.List ) { return __frames[frame + 1].Base + 1; }

            if ( !IsStatementLevel(frame) ) { return -1; }

            int statement = frame < 0
                                ? 0
                                : __frames[frame + 1].Base + 1;

            if ( IsPunct(prev, ';') ) { return statement; }

            if ( token.Has(TokenFlags.ClauseStart) && !token.Has(TokenFlags.StatementStart) ) { return statement; }

            if ( token.Owner < 0 ) { return -1; }

            ref readonly Token clause = ref __tokens[token.Owner];

            if ( clause.Clause is ClauseKind.List && clause.Has(TokenFlags.HasComma) && ( token.Has(TokenFlags.ItemsStart) || ( IsPunct(prev, ',') && __tokens[prev].Frame == frame ) ) ) { return statement + 1; }

            if ( clause.Clause is ClauseKind.Condition && ( Is(t, "AND") || Is(t, "OR") ) )
            {
                ref readonly Frame state = ref __frames[frame + 1];
                if ( state.CaseDepth > 0 || ( state.Between && Is(t, "AND") ) ) { return -1; }

                return statement + 1;
            }

            return -1;
        }

        /// <summary> Follows CASE ... END and BETWEEN ... AND, so their AND / OR stay inline. </summary>
        private void Track( int t )
        {
            ref Frame state = ref __frames[__tokens[t].Frame + 1];

            switch ( Text(t) )
            {
                case "CASE":
                    state.CaseDepth++;
                    break;

                case "END" when state.CaseDepth > 0:
                    state.CaseDepth--;
                    break;

                case "BETWEEN":
                    state.Between = true;
                    break;

                case "AND":
                    state.Between = false;
                    break;
            }
        }


        // ---- helpers ----

        private ReadOnlySpan<char> Text( int t ) => __sql.Slice(__tokens[t].Start, __tokens[t].Length);

        private bool Is( int t, string word ) => t >= 0 && t < __tokens.Length && __tokens[t].Kind is TokenKind.Word && Text(t).SequenceEqual(word);

        private bool IsPunct( int t, char c ) => t >= 0 && t < __tokens.Length && __tokens[t].Kind is TokenKind.Punct && __sql[__tokens[t].Start] == c;

        private int Next( int t )
        {
            if ( t < 0 ) { return -1; }

            for ( t++; t < __tokens.Length; t++ )
            {
                if ( __tokens[t].Kind is not TokenKind.Comment ) { return t; }
            }

            return -1;
        }

        private int Prev( int t )
        {
            for ( t--; t >= 0; t-- )
            {
                if ( __tokens[t].Kind is not TokenKind.Comment ) { return t; }
            }

            return -1;
        }
    }
}
