// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Resolves <c>nameof(Record.Property)</c> references to mapped columns and validates them against the record's metadata. </summary>
internal static class TypedColumns
{
    public static ISqlColumn Resolve<T>( string propertyName, scoped in SqlWriter writer )
        where T : ISqlTable<T>
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        if ( T.TrySqlColumn(propertyName, out ISqlColumn? column) && column is not null ) { return column; }

        string suggestions = Suggest<T>(propertyName);
        throw writer.Error(SqlBuildError.UnknownColumn, $"'{propertyName}' is not a mapped column of '{typeof(T).Name}'.{suggestions}");
    }

    /// <summary> The closest mapped property names (edit distance), e.g. <c> Did you mean 'UserName'?</c>. </summary>
    private static string Suggest<T>( string propertyName )
        where T : ISqlTable<T>
    {
        List<(int Distance, string Name)> candidates = [];
        foreach ( ISqlColumn column in T.SqlColumns ) { candidates.Add(( Distance(propertyName, column.PropertyName), column.PropertyName )); }

        if ( candidates.Count is 0 ) { return string.Empty; }

        candidates.Sort(static ( x, y ) => x.Distance != y.Distance ? x.Distance.CompareTo(y.Distance) : string.CompareOrdinal(x.Name, y.Name));
        int                limit   = Math.Min(3, candidates.Count);
        ValueStringBuilder builder = new(" Did you mean ");

        for ( int i = 0; i < limit; i++ )
        {
            if ( i > 0 ) { builder.Append(i == limit - 1 ? " or " : ", "); }

            builder.Append('\'').Append(candidates[i].Name).Append('\'');
        }

        return builder.Append('?').ToString();
    }

    private static int Distance( string left, string right )
    {
        left  = left.ToUpperInvariant();
        right = right.ToUpperInvariant();

        int[] previous = new int[right.Length + 1];
        int[] current  = new int[right.Length + 1];
        for ( int j = 0; j <= right.Length; j++ ) { previous[j] = j; }

        for ( int i = 1; i <= left.Length; i++ )
        {
            current[0] = i;
            for ( int j = 1; j <= right.Length; j++ )
            {
                int cost = left[i - 1] == right[j - 1]
                               ? 0
                               : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            ( previous, current ) = ( current, previous );
        }

        return previous[right.Length];
    }
}
