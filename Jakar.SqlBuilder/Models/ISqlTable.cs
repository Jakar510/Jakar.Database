// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> A type that knows its own SQL table name. </summary>
public interface ISqlTableName
{
    static abstract string SqlTableName { get; }
}



/// <summary>
///     The minimal, driver-agnostic contract the builder constrains its strongly-typed overloads against.
///     Jakar.Database's <c>ITableRecord&lt;TSelf&gt;</c> extends this, so every <c>TableRecord</c> satisfies it for free.
/// </summary>
public interface ISqlTable<TSelf> : ISqlTableName
    where TSelf : ISqlTable<TSelf>
{
    static abstract IReadOnlyList<ISqlColumn> SqlColumns { get; }
    static abstract bool TrySqlColumn( string propertyName, out ISqlColumn? column );
}
