// Jakar.Database :: Jakar.Database
// 06/29/2026  00:34

namespace Jakar.Database;


[AttributeUsage(AttributeTargets.Property)]
public sealed class StringCompareAttribute( StringComparison value ) : Attribute
{
    public StringComparison Value { get; } = value;
}
