// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 10/25/2026

namespace Jakar.SqlBuilder;


public enum SqlTypeKind : byte
{
    Boolean,
    SmallInt,
    Int,
    BigInt,
    Decimal,
    Real,
    Double,
    Char,
    VarChar,
    Text,
    Date,
    Time,
    Timestamp,
    TimestampTz,
    Guid,
    Binary,
    VarBinary,
    Blob,
    Json,
    Raw
}



/// <summary>
///     A portable column / <c>CAST</c> type, written per dialect (<see cref="ColumnName"/> for DDL, <see cref="CastName"/> for <c>CAST(x AS ...)</c>, which MySQL restricts to a few
///     target names). <see cref="Raw"/> passes a dialect-specific name through unchanged.
/// </summary>
public readonly record struct SqlType
{
    public SqlTypeKind Kind      { get; }
    public int         Length    { get; }
    public int         Precision { get; }
    public int         Scale     { get; }
    public string?     RawName   { get; }

    private SqlType( SqlTypeKind kind, int length = 0, int precision = 0, int scale = 0, string? raw = null )
    {
        Kind      = kind;
        Length    = length;
        Precision = precision;
        Scale     = scale;
        RawName   = raw;
    }


    public static SqlType Boolean     { get; } = new(SqlTypeKind.Boolean);
    public static SqlType SmallInt    { get; } = new(SqlTypeKind.SmallInt);
    public static SqlType Int         { get; } = new(SqlTypeKind.Int);
    public static SqlType BigInt      { get; } = new(SqlTypeKind.BigInt);
    public static SqlType Real        { get; } = new(SqlTypeKind.Real);
    public static SqlType Double      { get; } = new(SqlTypeKind.Double);
    public static SqlType Text        { get; } = new(SqlTypeKind.Text);
    public static SqlType Date        { get; } = new(SqlTypeKind.Date);
    public static SqlType Time        { get; } = new(SqlTypeKind.Time);
    public static SqlType Timestamp   { get; } = new(SqlTypeKind.Timestamp);
    public static SqlType TimestampTz { get; } = new(SqlTypeKind.TimestampTz);
    public static SqlType Guid        { get; } = new(SqlTypeKind.Guid);
    public static SqlType Blob        { get; } = new(SqlTypeKind.Blob);
    public static SqlType Json        { get; } = new(SqlTypeKind.Json);

    public static SqlType Decimal( int   precision, int scale = 0 ) => new(SqlTypeKind.Decimal, precision: Positive(precision), scale: scale);
    public static SqlType Char( int      length ) => new(SqlTypeKind.Char,      Positive(length));
    public static SqlType VarChar( int   length ) => new(SqlTypeKind.VarChar,   Positive(length));
    public static SqlType Binary( int    length ) => new(SqlTypeKind.Binary,    Positive(length));
    public static SqlType VarBinary( int length ) => new(SqlTypeKind.VarBinary, Positive(length));

    /// <summary> A dialect-specific type name, written as given (e.g. <c>"TSVECTOR"</c>). It's a trusted fragment: never pass untrusted input. </summary>
    public static SqlType Raw( string name ) => new(SqlTypeKind.Raw, raw: SqlWriter.RequireName(name));

    private static int Positive( int value ) => value > 0
                                                    ? value
                                                    : throw new ArgumentOutOfRangeException(nameof(value), value, "Type sizes must be positive.");


    /// <summary> The type for a column definition. </summary>
    public string ColumnName( SqlDialectKind dialect ) => dialect switch
                                                          {
                                                              SqlDialectKind.PostgreSql => Postgres(),
                                                              SqlDialectKind.SqlServer  => SqlServer(),
                                                              SqlDialectKind.Sqlite     => Sqlite(),
                                                              SqlDialectKind.MySql      => MySql(),
                                                              SqlDialectKind.Oracle     => Oracle(),
                                                              SqlDialectKind.Firebird   => Firebird(),
                                                              _                         => throw new ArgumentOutOfRangeException(nameof(dialect), dialect, null)
                                                          };

    /// <summary> The type for <c>CAST(x AS ...)</c>: the column type, except for MySQL, whose <c>CAST</c> takes only <c>SIGNED</c>, <c>CHAR</c>, <c>DATETIME</c>, ... </summary>
    public string CastName( SqlDialectKind dialect ) => dialect is SqlDialectKind.MySql
                                                            ? MySqlCast()
                                                            : ColumnName(dialect);


    private string Postgres() => Kind switch
                                 {
                                     SqlTypeKind.Boolean     => "BOOLEAN",
                                     SqlTypeKind.SmallInt    => "SMALLINT",
                                     SqlTypeKind.Int         => "INTEGER",
                                     SqlTypeKind.BigInt      => "BIGINT",
                                     SqlTypeKind.Decimal     => $"NUMERIC({Precision}, {Scale})",
                                     SqlTypeKind.Real        => "REAL",
                                     SqlTypeKind.Double      => "DOUBLE PRECISION",
                                     SqlTypeKind.Char        => $"CHAR({Length})",
                                     SqlTypeKind.VarChar     => $"VARCHAR({Length})",
                                     SqlTypeKind.Text        => "TEXT",
                                     SqlTypeKind.Date        => "DATE",
                                     SqlTypeKind.Time        => "TIME",
                                     SqlTypeKind.Timestamp   => "TIMESTAMP",
                                     SqlTypeKind.TimestampTz => "TIMESTAMPTZ",
                                     SqlTypeKind.Guid        => "UUID",
                                     SqlTypeKind.Binary      => "BYTEA",
                                     SqlTypeKind.VarBinary   => "BYTEA",
                                     SqlTypeKind.Blob        => "BYTEA",
                                     SqlTypeKind.Json        => "JSONB",
                                     _                       => RawName!
                                 };

    private string SqlServer() => Kind switch
                                  {
                                      SqlTypeKind.Boolean     => "BIT",
                                      SqlTypeKind.SmallInt    => "SMALLINT",
                                      SqlTypeKind.Int         => "INT",
                                      SqlTypeKind.BigInt      => "BIGINT",
                                      SqlTypeKind.Decimal     => $"DECIMAL({Precision}, {Scale})",
                                      SqlTypeKind.Real        => "REAL",
                                      SqlTypeKind.Double      => "FLOAT",
                                      SqlTypeKind.Char        => $"NCHAR({Length})",
                                      SqlTypeKind.VarChar     => $"NVARCHAR({Length})",
                                      SqlTypeKind.Text        => "NVARCHAR(MAX)",
                                      SqlTypeKind.Date        => "DATE",
                                      SqlTypeKind.Time        => "TIME",
                                      SqlTypeKind.Timestamp   => "DATETIME2",
                                      SqlTypeKind.TimestampTz => "DATETIMEOFFSET",
                                      SqlTypeKind.Guid        => "UNIQUEIDENTIFIER",
                                      SqlTypeKind.Binary      => $"BINARY({Length})",
                                      SqlTypeKind.VarBinary   => $"VARBINARY({Length})",
                                      SqlTypeKind.Blob        => "VARBINARY(MAX)",
                                      SqlTypeKind.Json        => "NVARCHAR(MAX)",
                                      _                       => RawName!
                                  };

    private string Sqlite() => Kind switch
                               {
                                   SqlTypeKind.Boolean or SqlTypeKind.SmallInt or SqlTypeKind.Int or SqlTypeKind.BigInt => "INTEGER",
                                   SqlTypeKind.Decimal                                                                   => "NUMERIC",
                                   SqlTypeKind.Real or SqlTypeKind.Double                                                => "REAL",
                                   SqlTypeKind.Binary or SqlTypeKind.VarBinary or SqlTypeKind.Blob                       => "BLOB",
                                   SqlTypeKind.Raw                                                                       => RawName!,
                                   _                                                                                     => "TEXT"
                               };

    private string MySql() => Kind switch
                              {
                                  SqlTypeKind.Boolean     => "BOOLEAN",
                                  SqlTypeKind.SmallInt    => "SMALLINT",
                                  SqlTypeKind.Int         => "INT",
                                  SqlTypeKind.BigInt      => "BIGINT",
                                  SqlTypeKind.Decimal     => $"DECIMAL({Precision}, {Scale})",
                                  SqlTypeKind.Real        => "FLOAT",
                                  SqlTypeKind.Double      => "DOUBLE",
                                  SqlTypeKind.Char        => $"CHAR({Length})",
                                  SqlTypeKind.VarChar     => $"VARCHAR({Length})",
                                  SqlTypeKind.Text        => "LONGTEXT",
                                  SqlTypeKind.Date        => "DATE",
                                  SqlTypeKind.Time        => "TIME",
                                  SqlTypeKind.Timestamp   => "DATETIME(6)",
                                  SqlTypeKind.TimestampTz => "DATETIME(6)",
                                  SqlTypeKind.Guid        => "CHAR(36)",
                                  SqlTypeKind.Binary      => $"BINARY({Length})",
                                  SqlTypeKind.VarBinary   => $"VARBINARY({Length})",
                                  SqlTypeKind.Blob        => "LONGBLOB",
                                  SqlTypeKind.Json        => "JSON",
                                  _                       => RawName!
                              };

    private string MySqlCast() => Kind switch
                                  {
                                      SqlTypeKind.Boolean or SqlTypeKind.SmallInt or SqlTypeKind.Int or SqlTypeKind.BigInt => "SIGNED",
                                      SqlTypeKind.Decimal                                                                   => $"DECIMAL({Precision}, {Scale})",
                                      SqlTypeKind.Real                                                                      => "FLOAT",
                                      SqlTypeKind.Double                                                                    => "DOUBLE",
                                      SqlTypeKind.Char or SqlTypeKind.VarChar                                               => $"CHAR({Length})",
                                      SqlTypeKind.Text                                                                      => "CHAR",
                                      SqlTypeKind.Guid                                                                      => "CHAR(36)",
                                      SqlTypeKind.Date                                                                      => "DATE",
                                      SqlTypeKind.Time                                                                      => "TIME",
                                      SqlTypeKind.Timestamp or SqlTypeKind.TimestampTz                                      => "DATETIME(6)",
                                      SqlTypeKind.Binary or SqlTypeKind.VarBinary                                           => $"BINARY({Length})",
                                      SqlTypeKind.Blob                                                                      => "BINARY",
                                      SqlTypeKind.Json                                                                      => "JSON",
                                      _                                                                                     => RawName!
                                  };

    private string Oracle() => Kind switch
                               {
                                   SqlTypeKind.Boolean     => "NUMBER(1)",
                                   SqlTypeKind.SmallInt    => "NUMBER(5)",
                                   SqlTypeKind.Int         => "NUMBER(10)",
                                   SqlTypeKind.BigInt      => "NUMBER(19)",
                                   SqlTypeKind.Decimal     => $"NUMBER({Precision}, {Scale})",
                                   SqlTypeKind.Real        => "BINARY_FLOAT",
                                   SqlTypeKind.Double      => "BINARY_DOUBLE",
                                   SqlTypeKind.Char        => $"CHAR({Length})",
                                   SqlTypeKind.VarChar     => $"VARCHAR2({Length})",
                                   SqlTypeKind.Text        => "CLOB",
                                   SqlTypeKind.Date        => "DATE",
                                   SqlTypeKind.Time        => "INTERVAL DAY(0) TO SECOND(7)",
                                   SqlTypeKind.Timestamp   => "TIMESTAMP",
                                   SqlTypeKind.TimestampTz => "TIMESTAMP WITH TIME ZONE",
                                   SqlTypeKind.Guid        => "RAW(16)",
                                   SqlTypeKind.Binary      => $"RAW({Length})",
                                   SqlTypeKind.VarBinary   => $"RAW({Length})",
                                   SqlTypeKind.Blob        => "BLOB",
                                   SqlTypeKind.Json        => "JSON",
                                   _                       => RawName!
                               };

    private string Firebird() => Kind switch
                                 {
                                     SqlTypeKind.Boolean     => "BOOLEAN",
                                     SqlTypeKind.SmallInt    => "SMALLINT",
                                     SqlTypeKind.Int         => "INTEGER",
                                     SqlTypeKind.BigInt      => "BIGINT",
                                     SqlTypeKind.Decimal     => $"DECIMAL({Precision}, {Scale})",
                                     SqlTypeKind.Real        => "FLOAT",
                                     SqlTypeKind.Double      => "DOUBLE PRECISION",
                                     SqlTypeKind.Char        => $"CHAR({Length})",
                                     SqlTypeKind.VarChar     => $"VARCHAR({Length})",
                                     SqlTypeKind.Text        => "BLOB SUB_TYPE TEXT",
                                     SqlTypeKind.Date        => "DATE",
                                     SqlTypeKind.Time        => "TIME",
                                     SqlTypeKind.Timestamp   => "TIMESTAMP",
                                     SqlTypeKind.TimestampTz => "TIMESTAMP WITH TIME ZONE",
                                     SqlTypeKind.Guid        => "CHAR(16) CHARACTER SET OCTETS",
                                     SqlTypeKind.Binary      => $"CHAR({Length}) CHARACTER SET OCTETS",
                                     SqlTypeKind.VarBinary   => $"VARCHAR({Length}) CHARACTER SET OCTETS",
                                     SqlTypeKind.Blob        => "BLOB",
                                     SqlTypeKind.Json        => "BLOB SUB_TYPE TEXT",
                                     _                       => RawName!
                                 };
}
