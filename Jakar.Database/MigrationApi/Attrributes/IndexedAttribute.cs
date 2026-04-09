// Jakar.Database :: Jakar.Database
// 02/17/2026  22:17

namespace Jakar.Database;


public enum ColumnIndex
{
    NotSet
}



[AttributeUsage(AttributeTargets.Property)]
public sealed class IndexedAttribute<TSelf>( string propertyName ) : IndexedAttribute(propertyName, TSelf.TableName)
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>;



[AttributeUsage(AttributeTargets.Property)]
public class IndexedAttribute( string propertyName, string tableName ) : Attribute
{
    public readonly string Name = propertyName.SqlIndexName(tableName);
    public          bool   IsValid { [MemberNotNullWhen(true, nameof(Name))] get => !string.IsNullOrWhiteSpace(Name); }

    public IndexedAttribute( ColumnIndex defaults, string tableName ) : this(defaults switch
                                                                             {
                                                                                 ColumnIndex.NotSet => "",
                                                                                 _                  => throw new OutOfRangeException(defaults)
                                                                             },
                                                                             tableName) { }
}
