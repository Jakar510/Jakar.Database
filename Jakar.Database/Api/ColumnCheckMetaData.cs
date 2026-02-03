namespace Jakar.Database;


public readonly record struct ColumnCheckMetaData( bool And, params string[] Checks )
{
    public static readonly ColumnCheckMetaData Default = new(true);
    public readonly        bool                And     = And;
    public readonly        string[]            Checks  = Checks;
    public                 bool                IsValid { [MemberNotNullWhen(true, nameof(Checks))] get => Checks?.Length is > 0; }


    public ColumnCheckMetaData( string   check ) : this(true, check) { }
    public ColumnCheckMetaData( string[] checks ) : this(true, checks) { }


    public static ColumnCheckMetaData Create( string    propertyName )                  => new($"length({propertyName.SqlColumnName()}) > 0");
    public static ColumnCheckMetaData Create( string    propertyName, int      length ) => new($"length({propertyName.SqlColumnName()}) > {length}");
    public static ColumnCheckMetaData Create( string    propertyName, IntRange range )  => new($"length({propertyName.SqlColumnName()}) BETWEEN {range.Min} AND {range.Max}");
    public static ColumnCheckMetaData Create<T>( string propertyName, T        arg )   => Default;
}
