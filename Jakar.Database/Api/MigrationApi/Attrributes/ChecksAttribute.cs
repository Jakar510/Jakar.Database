namespace Jakar.Database;


[AttributeUsage(AttributeTargets.Property)]
public sealed class ChecksAttribute( bool and, params string[] checks ) : DatabaseAttribute
{
    public readonly bool                   And         = and;
    public readonly ImmutableArray<string> Constraints = [..checks];
    public          bool                   IsValid { [MemberNotNullWhen(true, nameof(Constraints))] get => Constraints.Length is > 0; }


    public ChecksAttribute( params string[] checks ) : this(true, checks) { }
    public ChecksAttribute( string          columnName ) : this(true, $"length({columnName}) > 0") { }
    public ChecksAttribute( string          columnName, int      length ) : this(true, $"length({columnName}) <= {length}") { }
    public ChecksAttribute( string          columnName, IntRange range ) : this(true, $"length({columnName}) BETWEEN {range.Min} AND {range.Max}") { }


    public override StringBuilder ToStringBuilder()
    {
        StringBuilder sb = new(5 + Constraints.AsValueEnumerable().Sum(static x => x.Length));

        for ( int i = 0; i < Constraints.Length; i++ )
        {
            sb.Append(Constraints[i]);

            if ( i < Constraints.Length - 1 )
            {
                sb.Append(And
                              ? " AND "
                              : " OR ");
            }
        }

        return sb;
    }
}
