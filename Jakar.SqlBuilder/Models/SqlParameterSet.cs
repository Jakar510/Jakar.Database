// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

using System.Collections;
using System.Data;
using Dapper;



namespace Jakar.SqlBuilder;


/// <summary> The ordered set of bound parameters produced alongside a built statement. </summary>
public sealed partial class SqlParameterSet : IReadOnlyList<SqlParameter>
{
    private readonly SqlParameter[] __parameters;

    public static SqlParameterSet Empty { get; } = new([], SqlDialectKind.NotSet);

    public SqlDialectKind Dialect { get; }
    public int            Count   => __parameters.Length;
    public bool           IsEmpty => __parameters.Length is 0;
    public ref readonly SqlParameter this[ int         index ] => ref __parameters[index];
    SqlParameter IReadOnlyList<SqlParameter>.this[ int index ] => this[index];

    internal SqlParameterSet( SqlParameter[] parameters, SqlDialectKind dialect )
    {
        __parameters = parameters;
        Dialect      = dialect;
    }

    public IEnumerator<SqlParameter> GetEnumerator()
    {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach ( SqlParameter parameter in __parameters ) { yield return parameter; }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Dictionary<string, object?> ToDictionary()
    {
        Dictionary<string, object?> result = new(__parameters.Length);
        foreach ( SqlParameter parameter in __parameters ) { result[parameter.Name] = parameter.Value; }

        return result;
    }


    /// <summary> The name a driver binds <paramref name="token"/> by: <c>$1</c> is positional (empty name), <c>:p0</c> binds as <c>p0</c>, <c>@p0</c> as itself. </summary>
    public static string BindName( string token ) => token switch
                                                     {
                                                         ['$', ..] => string.Empty,
                                                         [':', ..] => token[1..],
                                                         _         => token
                                                     };

    /// <summary>
    ///     Adds every parameter to <paramref name="command"/> (ADO.NET), in order. PostgreSQL's positional <c>$1</c> parameters are added unnamed; Oracle's <c>:p0</c> are named <c>p0</c> and
    ///     the command is switched to bind by name when its provider supports it (ODP.NET's <c>BindByName</c>).
    /// </summary>
    public void AddTo( IDbCommand command )
    {
        ArgumentNullException.ThrowIfNull(command);

        if ( Dialect is SqlDialectKind.Oracle ) { command.GetType().GetProperty("BindByName")?.SetValue(command, true); }

        foreach ( SqlParameter parameter in __parameters )
        {
            IDbDataParameter value = command.CreateParameter();
            value.ParameterName = BindName(parameter.Name);
            value.Value         = parameter.Value ?? DBNull.Value;
            if ( parameter.DbType is { } type ) { value.DbType = type; }

            command.Parameters.Add(value);
        }
    }

    /// <summary> The parameters as Dapper <see cref="DynamicParameters"/>, named without their prefix (<c>p0</c>). </summary>
    /// <exception cref="InvalidOperationException"> PostgreSQL's positional <c>$1</c> parameters can't be named; build with <see cref="SqlBuilderOptions.NamedPostgresParameters"/>. </exception>
    public DynamicParameters ToDapper()
    {
        DynamicParameters result = new();

        foreach ( SqlParameter parameter in __parameters )
        {
            if ( parameter.Name.StartsWith('$') ) { throw new InvalidOperationException($"'{parameter.Name}' is a positional PostgreSQL parameter, which Dapper can't bind by name; build with {nameof(SqlBuilderOptions)}.{nameof(SqlBuilderOptions.NamedPostgresParameters)}."); }

            result.Add(parameter.Name[1..], parameter.Value, parameter.DbType);
        }

        return result;
    }
}
