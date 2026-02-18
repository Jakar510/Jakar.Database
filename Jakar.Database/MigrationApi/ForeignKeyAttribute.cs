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
public sealed class ForeignKeyAttribute( string foreignTableName, OnAction onAction = OnAction.NotSet ) : Attribute
{
    public readonly string ForeignTableName = Validate.ThrowIfNull(foreignTableName)
                                                      .SqlColumnName();
    public readonly OnAction Action = onAction;
    public          bool     IsValid     { [Pure] [MemberNotNullWhen(true, nameof(ForeignTableName))] get => !string.IsNullOrWhiteSpace(ForeignTableName); }
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
}
