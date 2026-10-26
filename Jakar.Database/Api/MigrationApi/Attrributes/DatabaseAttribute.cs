// Jakar.Database :: Jakar.Database
// 02/17/2026  21:47

namespace Jakar.Database;


public abstract class DatabaseAttribute : Attribute
{
    /// <summary> Writes the attribute's SQL. </summary>
    public abstract void Append( ref ValueStringBuilder builder );
    public sealed override string ToString()
    {
        ValueStringBuilder builder = new();
        Append(ref builder);
        return builder.ToString();
    }
}
