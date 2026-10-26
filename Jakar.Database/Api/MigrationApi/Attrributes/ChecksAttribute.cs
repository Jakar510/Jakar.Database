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


    public override void Append( ref ValueStringBuilder builder )
    {
        for ( int i = 0; i < Constraints.Length; i++ )
        {
            builder.Append(Constraints[i]);

            if ( i < Constraints.Length - 1 )
            {
                builder.Append(And
                                   ? " AND "
                                   : " OR ");
            }
        }
    }
}
