// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 10/25/2026

namespace Jakar.SqlBuilder;


/// <summary> A column <see cref="CommandParameters"/> can bind values to: the driver-agnostic <see cref="ISqlColumn"/> shape plus its position in the table. </summary>
public interface ISqlCommandColumn : ISqlColumn
{
    /// <summary> The column's position in the table; parameters are ordered by it. </summary>
    int Index { get; }

    /// <summary> The <c>column = @parameter</c> text for the column. </summary>
    string KeyValuePair { get; }
}



/// <summary> The table metadata <see cref="CommandParameters"/> needs: column lookup, parameter naming, and the sizes used to pre-size the rendered SQL fragments. Implemented by the host (e.g. Jakar.Database <c>TableMetaData</c>). </summary>
public interface ISqlCommandTable
{
    int ColumnCount { get; }

    /// <summary> The longest column name, used to pre-size rendered fragments. </summary>
    int MaxLengthColumnName { get; }

    /// <summary> The combined length of every column's <see cref="ISqlCommandColumn.KeyValuePair"/>. </summary>
    int KeyValuePairsLength { get; }


    /// <exception cref="KeyNotFoundException"> The table has no column for <paramref name="propertyName"/>. </exception>
    ISqlCommandColumn GetColumn( string propertyName );

    /// <summary> The parameter name for a CLR argument expression (e.g. <c>record.UserName</c> → <c>user_name</c>). </summary>
    string GetParameterName( string parameterName );

    /// <summary> Appends the table's column list, one per line, at <paramref name="indentLevel"/>. </summary>
    void ColumnNames( ref ValueStringBuilder builder, ref int indentLevel );
}



/// <summary> A value that binds to SQL as something else, e.g. a record id as its <see cref="Guid"/>. <see cref="CommandParameter"/> unwraps it. </summary>
public interface ISqlParameterValue
{
    object? ParameterValue { get; }
}



/// <summary> An <see cref="ISqlParameterValue"/> that always binds as a <typeparamref name="TValue"/>: a column of this type compares like a <typeparamref name="TValue"/> column (<see cref="SqlBuilderOptions.StrictTypes"/>). </summary>
public interface ISqlParameterValue<TValue> : ISqlParameterValue;
