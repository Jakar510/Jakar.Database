// Jakar.Database :: Jakar.Database

namespace Jakar.Database;


/// <summary>
///     Emitted by <c>Jakar.Database.Generators.TableRecordGenerator</c> onto every generated <see cref="ITableRecord{TSelf}"/> partial type.
///     <para>
///         Carries the build-time, size-packed column order (property names) so the runtime does not have to compute it via reflection / <see cref="PostgresTypeComparer"/>.
///         The order is consumed by <see cref="TableMetaData{TSelf}"/> when assigning <see cref="ColumnMetaData.Index"/>. If the attribute is absent (or does not cover every column), the runtime falls back to <see cref="TableMetaData{TSelf}.SortedProperties"/>, so this is purely an optimization and can never change correctness.
///     </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class GeneratedColumnOrderAttribute( params string[] propertyNames ) : Attribute
{
    public string[] PropertyNames { get; } = propertyNames;
}
