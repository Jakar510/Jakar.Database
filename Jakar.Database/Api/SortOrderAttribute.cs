// Jakar.Database :: Jakar.Database

namespace Jakar.Database;


/// <summary>
///     Sets the relative priority of a property when <c>Jakar.Database.Generators.TableRecordGenerator</c> generates
///     <see cref="System.IComparable{T}.CompareTo(T)"/> and <see cref="System.IEquatable{T}.Equals(T)"/> for an <see cref="ITableRecord{TSelf}"/>.
///     <para>
///         Lower <see cref="Priority"/> values are compared first. Properties without this attribute are ordered after all
///         decorated properties, in declaration (source) order. Ties between equal priorities also fall back to declaration order.
///     </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SortOrderAttribute( int priority ) : Attribute
{
    public int Priority { get; } = priority;
}
