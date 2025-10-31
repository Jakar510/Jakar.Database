﻿// Jakar.Extensions :: Jakar.Database
// 09/29/2022  8:03 PM

namespace Jakar.Database;


public sealed class SqlException<TSelf> : Exception
    where TSelf : ITableRecord<TSelf>
{
    public PostgresParameters Parameters { get; init; }
    public string             SQL        { get; init; }

    // [JsonProperty] public string Value => base.ToString();


    public SqlException( string sql, string message ) : this(sql, PostgresParameters.Empty, message) { }
    public SqlException( string sql, PostgresParameters parameters, string? message = null ) : base(message ?? GetMessage(sql, parameters))
    {
        SQL        = sql;
        Parameters = parameters;
    }
    public SqlException( string               sql, Exception? inner ) : this(sql, PostgresParameters.Empty, GetMessage(sql), inner) => SQL = sql;
    public SqlException( string               sql, string     message, Exception? inner ) : this(sql, PostgresParameters.Empty, message, inner) { }
    public SqlException( in SqlCommand<TSelf> sql ) : this(sql.SQL, sql.Parameters, GetMessage(sql.SQL,                                      sql.Parameters)) { }
    public SqlException( in SqlCommand<TSelf> sql, Exception?         inner ) : this(sql.SQL, sql.Parameters, GetMessage(sql.SQL,            sql.Parameters), inner) { }
    public SqlException( string               sql, PostgresParameters parameters, Exception? inner ) : this(sql, parameters, GetMessage(sql, parameters), inner) { }
    public SqlException( string sql, PostgresParameters parameters, string message, Exception? inner ) : base(message, inner)
    {
        SQL        = sql;
        Parameters = parameters;
    }


    private static string GetMessage( string sql, PostgresParameters dynamicParameters = default )
    {
        string parameters = dynamicParameters.Count == 0
                                ? "NULL"
                                : dynamicParameters.Parameters.ToString();

        return $"""
                An error occurred with the following sql statement

                {nameof(SQL)}:    {sql}

                {nameof(Parameters)}:   {parameters}
                """;
    }


    /*
    private const string InnerExceptionPrefix     = " ---> ";
    private const string NewLineConst             = "\n";
    private const string EndOfInnerExceptionStack = "--- End Of Inner Exception Stack ---";
    public override string ToString()
    {
        base.ToString();
        Exception? _innerException = InnerException;

        string className = GetType()
           .ToString();

        string? message              = Message;
        string  innerExceptionString = _innerException?.ToString() ?? EMPTY;
        string? stackTrace           = StackTrace;

        // Calculate result string length
        int length = className.Length;


        checked
        {
            if ( Parameters is not null ) { length += Parameters.ParameterNames.Sum( x => x.Length ); }

            if ( !string.IsNullOrEmpty( message ) ) { length += 2 + message.Length; }

            if ( _innerException != null ) { length += NewLineConst.Length + InnerExceptionPrefix.Length + innerExceptionString.Length + NewLineConst.Length + 3 + EndOfInnerExceptionStack.Length; }

            if ( stackTrace != null ) { length += NewLineConst.Length + stackTrace.Length; }
        }

        // Create the string
        Span<char> resultSpan = stackalloc char[length];

        // Fill it in
        int index = 0;
        Write( className, resultSpan, ref index );

        if ( !string.IsNullOrEmpty( message ) )
        {
            Write( ": ",    resultSpan, ref index );
            Write( message, resultSpan, ref index );
        }

        if ( _innerException != null )
        {
            Write( NewLineConst,                resultSpan, ref index );
            Write( InnerExceptionPrefix,        resultSpan, ref index );
            Write( innerExceptionString,        resultSpan, ref index );
            Write( NewLineConst,                resultSpan, ref index );
            Write( "   ",                       resultSpan, ref index );
            Write( endOfInnerExceptionResource, resultSpan, ref index );
        }

        if ( stackTrace != null )
        {
            Write( NewLineConst, resultSpan, ref index );
            Write( stackTrace,   resultSpan, ref index );
        }


        Debug.Assert( resultSpan.Length == length );
        return resultSpan.ToString();


        static void Write( ReadOnlySpan<char> source, Span<char> dest, ref int index )
        {
            source.CopyTo( dest[index..] );
            index += source.Length;
        }
    }
    */
}
