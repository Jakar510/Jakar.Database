// Jakar.Database :: Jakar.Database
// 02/17/2026  21:47

namespace Jakar.Database;


public abstract class DatabaseAttribute : Attribute
{
    public abstract StringBuilder ToStringBuilder();
    public sealed override string ToString() => ToStringBuilder()
       .ToString();
}
