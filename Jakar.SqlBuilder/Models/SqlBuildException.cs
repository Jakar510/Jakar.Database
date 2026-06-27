// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


public enum SqlBuildError
{
    UnbalancedParentheses,
    MissingRequiredClause,
    UnsupportedFeature,
    ParameterGap,
    EmptyPredicate,
    UnknownColumn,
    TypeMismatch,
    InvalidLiteral,
    DialectNotSet
}



/// <summary> Thrown when a statement cannot be built into valid SQL. Carries the partially built text for debugging. </summary>
public sealed class SqlBuildException : InvalidOperationException
{
    public SqlBuildError  Reason     { get; }
    public string         PartialSql { get; }
    public SqlDialectKind Dialect    { get; }

    public SqlBuildException( SqlBuildError reason, string message, string partialSql, SqlDialectKind dialect ) : base(message)
    {
        Reason     = reason;
        PartialSql = partialSql;
        Dialect    = dialect;
    }
}
