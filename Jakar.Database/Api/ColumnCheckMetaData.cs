namespace Jakar.Database;


public readonly record struct ColumnCheckMetaData( bool And, params string[] Checks )
{
    public static readonly ColumnCheckMetaData Default = new(true);
    public readonly        bool                And     = And;
    public readonly        string[]            Checks  = Checks;
    public                 bool                IsValid { [MemberNotNullWhen(true, nameof(Checks))] get => Checks?.Length is > 0; }


    public ColumnCheckMetaData( string   check ) : this(true, check) { }
    public ColumnCheckMetaData( string[] checks ) : this(true, checks) { }


    public static ColumnCheckMetaData Create( string    columnName )                  => new($"length({columnName}) > 0");
    public static ColumnCheckMetaData Create( string    columnName, int      length ) => new($"length({columnName}) > {length}");
    public static ColumnCheckMetaData Create( string    columnName, IntRange range )  => new($"length({columnName}) BETWEEN {range.Min} AND {range.Max}");
    public static ColumnCheckMetaData Create<T>( string columnName, T        arg )   => Default;
}
