// Jakar.Database :: Jakar.Database
// 03/10/2026  22:04


namespace Jakar.Database;


/*
[Experimental(nameof(DataReader<>))]
public sealed class DataReader<TSelf> : IDataReader
    where TSelf : TableRecord<TSelf>, ITableRecord<TSelf>
{
    internal readonly List<TSelf> Records = [];
    private           TSelf?      _current;
    private           int         _index;

    public int  Depth           => 1;
    public int  FieldCount      => TSelf.MetaData.Count;
    public bool IsClosed        => false;
    public int  RecordsAffected => Records.Count;


    public object this[ int    index ] => null!;
    public object this[ string name ] => null!;


    public int GetOrdinal( string name ) => TSelf.MetaData[name].Index;


    public byte GetByte( int  index )                                                                 => 0;
    public long GetBytes( int index, long fieldOffset, byte[]? buffer, int bufferoffset, int length ) => 0;

    public char GetChar( int  index )                                                                 => '\0';
    public long GetChars( int index, long fieldoffset, char[]? buffer, int bufferoffset, int length ) => 0;

    public bool     GetBoolean( int      index )  => false;
    public string   GetDataTypeName( int index )  => TSelf.MetaData[index].Column.DataType;
    public DateTime GetDateTime( int     index )  => default;
    public decimal  GetDecimal( int      index )  => 0;
    public double   GetDouble( int       index )  => 0;
    public Type     GetFieldType( int    index )  => null!;
    public float    GetFloat( int        index )  => 0;
    public Guid     GetGuid( int         index )  => Guid.Empty;
    public short    GetInt16( int        index )  => 0;
    public int      GetInt32( int        index )  => 0;
    public long     GetInt64( int        index )  => 0;
    public string   GetName( int         index )  => null!;
    public string   GetString( int       index )  => null!;
    public object   GetValue( int        index )  => null!;
    public int      GetValues( object[]  values ) => 0;
    public bool     IsDBNull( int        index )  => false;


    public DataTable?  GetSchemaTable()     => null;
    public IDataReader GetData( int index ) => this;
    public bool NextResult()
    {
        if ( ++_index >= Records.Count ) { return false; }

        _current = Records[_index];
        return true;
    }
    public bool Read() => true;
    public void Dispose()
    {
        _current = null;
        Records.Clear();
    }
    public void Close()
    {
        _current = null;
        Records.Clear();
    }
}
*/
