namespace Jakar.Database;


public readonly record struct ColumnCheckMetaData( bool? And, params string[] Checks )
{
    public static readonly          ColumnCheckMetaData Default = new(null);
    public readonly                 bool?               And     = And;
    public readonly                 string[]            Checks  = Checks;
    public                          bool                IsValid            { [MemberNotNullWhen(true, nameof(And), nameof(Checks))] get => And.HasValue && Checks?.Length is > 0; }
    public static implicit operator ColumnCheckMetaData( string   check )  => new(true, check);
    public static implicit operator ColumnCheckMetaData( string[] checks ) => new(true, checks);
}