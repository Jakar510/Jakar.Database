// Jakar.Database :: Jakar.Database
// 02/12/2026  18:29

namespace Jakar.Database;


public enum OnAction
{
    NotSet,
    DeleteCascade,
    DeleteSetNull,
    DeleteSetDefault,
    DeleteNoAction,
    UpdateCascade,
    UpdateSetNull,
    UpdateSetDefault,
    UpdateNoAction
}



[AttributeUsage(AttributeTargets.Property)]
public sealed class ForeignKeyAttribute<TSelf, TOtherTable>( OnAction onAction = OnAction.NotSet ) : ForeignKeyAttribute(TSelf.TableName, TOtherTable.TableName, onAction)
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
    where TOtherTable : TableRecord<TOtherTable>, ITableRecord<TOtherTable>;



[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute( string tableName, string foreignTableName, OnAction onAction = OnAction.NotSet ) : Attribute
{
    public readonly string   TableName = foreignTableName.SqlColumnName();
    public readonly OnAction Action    = onAction;
    public          bool     IsValid     { [Pure] [MemberNotNullWhen(true, nameof(TableName))] get => !string.IsNullOrWhiteSpace(TableName); }
    public          bool     HasModifier { [Pure] get => Action is not OnAction.NotSet; }

    public string? Modifier => Action switch
                               {
                                   OnAction.NotSet           => null,
                                   OnAction.DeleteCascade    => "ON DELETE CASCADE",
                                   OnAction.DeleteSetNull    => "ON DELETE SET NULL",
                                   OnAction.DeleteSetDefault => "ON DELETE SET DEFAULT",
                                   OnAction.DeleteNoAction   => "ON DELETE NO ACTION",
                                   OnAction.UpdateCascade    => "ON UPDATE CASCADE",
                                   OnAction.UpdateSetNull    => "ON UPDATE SET NULL",
                                   OnAction.UpdateSetDefault => "ON UPDATE SET DEFAULT",
                                   OnAction.UpdateNoAction   => "ON UPDATE NO ACTION",
                                   _                         => throw new OutOfRangeException(Action)
                               };

    public string Index( string columnName ) => columnName.SqlColumnIndexName(tableName);
    public string Index( string columnName, int maxLength ) => Index(columnName).GetPadded(maxLength);
}
