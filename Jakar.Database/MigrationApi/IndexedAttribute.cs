// Jakar.Database :: Jakar.Database
// 02/17/2026  22:17

namespace Jakar.Database;


public enum ColumnIndex
{
    NotSet,
}



[AttributeUsage(AttributeTargets.Property)]
public sealed class IndexedAttribute( string propertyName ) : Attribute
{
    public readonly string Name = propertyName.SqlColumnIndexName();
    public          bool   IsValid { [MemberNotNullWhen(true, nameof(Name))] get => !string.IsNullOrWhiteSpace(Name); }
    public IndexedAttribute( ColumnIndex defaults ) : this(defaults switch
                                                           {
                                                               ColumnIndex.NotSet => "",
                                                               _                  => throw new OutOfRangeException(defaults)
                                                           }) { }
}
