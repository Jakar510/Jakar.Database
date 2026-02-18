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
public sealed class ForeignKeyAttribute( string foreignTableName, OnAction onAction = OnAction.NotSet ) : DatabaseAttribute
{
    public readonly string ForeignTableName = foreignTableName.SqlColumnName();
    public readonly string? OnAction = onAction switch
                                       {
                                           Jakar.Database.OnAction.NotSet           => null,
                                           Jakar.Database.OnAction.DeleteCascade    => "ON DELETE CASCADE",
                                           Jakar.Database.OnAction.DeleteSetNull    => "ON DELETE SET NULL",
                                           Jakar.Database.OnAction.DeleteSetDefault => "ON DELETE SET DEFAULT",
                                           Jakar.Database.OnAction.DeleteNoAction   => "ON DELETE NO ACTION",
                                           Jakar.Database.OnAction.UpdateCascade    => "ON UPDATE CASCADE",
                                           Jakar.Database.OnAction.UpdateSetNull    => "ON UPDATE SET NULL",
                                           Jakar.Database.OnAction.UpdateSetDefault => "ON UPDATE SET DEFAULT",
                                           Jakar.Database.OnAction.UpdateNoAction   => "ON UPDATE NO ACTION",
                                           _                                        => throw new ArgumentOutOfRangeException(nameof(onAction), onAction, null)
                                       };
    public bool IsValid { [MemberNotNullWhen(true, nameof(ForeignTableName))] get => !string.IsNullOrWhiteSpace(ForeignTableName); }


    public override StringBuilder ToStringBuilder()
    {
        ReadOnlySpan<char> onAction = OnAction;
        StringBuilder      sb       = new(11 + ForeignTableName.Length + onAction.Length);

        sb.Append("REFERENCES ")
          .Append(ForeignTableName)
          .Append(' ')
          .Append(onAction);

        return sb;
    }
}
