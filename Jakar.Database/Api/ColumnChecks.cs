namespace Jakar.Database;


public readonly record struct ColumnChecks( bool And, params string[] Checks )
{
    public static readonly ColumnChecks Empty  = new(true);
    public readonly        bool                And    = And;
    public readonly        string[]            Checks = Checks;
    public                 bool                IsValid { [MemberNotNullWhen(true, nameof(Checks))] get => Checks?.Length is > 0; }


    public ColumnChecks( string   check ) : this(true, check) { }
    public ColumnChecks( string[] checks ) : this(true, checks) { }


    public static ColumnChecks Create( string    columnName )                  => new($"length({columnName}) > 0");
    public static ColumnChecks Create( string    columnName, int      length ) => new($"length({columnName}) <= {length}");
    public static ColumnChecks Create( string    columnName, IntRange range )  => new($"length({columnName}) BETWEEN {range.Min} AND {range.Max}");
    public static ColumnChecks Create<T>( string columnName, T        arg )    => Empty;
}