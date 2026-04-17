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
public class IndexedAttribute( string propertyName, SqlName tableName ) : Attribute
{
    public readonly SqlName Name = propertyName.SqlIndexName(tableName);
    public          bool    IsValid { [MemberNotNullWhen(true, nameof(Name))] get => !string.IsNullOrWhiteSpace(Name.Value); }

    public IndexedAttribute( ColumnIndex defaults, SqlName tableName ) : this(defaults switch
                                                                              {
                                                                                  ColumnIndex.NotSet => "",
                                                                                  _                  => throw new OutOfRangeException(defaults)
                                                                              },
                                                                              tableName) { }
}
