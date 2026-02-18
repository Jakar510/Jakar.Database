// Jakar.Database :: Jakar.Database
// 02/17/2026  22:22

namespace Jakar.Database;


[AttributeUsage(AttributeTargets.Property)]
public sealed class DefaultIdentityAttribute() : Attribute
{
    public override string ToString() => "DEFAULT IDENTITY";
}



[AttributeUsage(AttributeTargets.Property)]
public sealed class AlwaysIdentityAttribute() : Attribute
{
    public override string ToString() => "ALWAYS IDENTITY";
}



[AttributeUsage(AttributeTargets.Property)]
public sealed class FixedAttribute( int length ) : Attribute
{
    public readonly int Length = length;
}



[AttributeUsage(AttributeTargets.Property)]
public sealed class UniqueAttribute() : Attribute
{
    public override string ToString() => "UNIQUE";
}
