// Jakar.Database :: Jakar.Database
// 02/17/2026  21:25

namespace Jakar.Database;


public enum ColumnDefaults
{
    Guid,
    DateTimeOffset,
    DateTime,
    DateOnly
}



[AttributeUsage(AttributeTargets.Property)]
public sealed class DefaultsAttribute( string defaults ) : DatabaseAttribute
{
    public readonly string Defaults = defaults;
    public          bool   IsValid { [MemberNotNullWhen(true, nameof(Defaults))] get => !string.IsNullOrWhiteSpace(Defaults); }
    public DefaultsAttribute( ColumnDefaults defaults ) : this(defaults switch
                                                               {
                                                                   ColumnDefaults.Guid           => @"gen_random_uuid()",
                                                                   ColumnDefaults.DateTimeOffset => @"SYSUTCDATETIME()",
                                                                   ColumnDefaults.DateTime       => @"NOW()",
                                                                   ColumnDefaults.DateOnly       => @"CURRENT_DATE()",
                                                                   _                             => throw new OutOfRangeException(defaults)
                                                               }) { }


    public override StringBuilder ToStringBuilder() => new(Defaults);
}
