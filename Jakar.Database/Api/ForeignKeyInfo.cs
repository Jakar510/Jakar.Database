// Jakar.Database :: Jakar.Database
// 02/12/2026  18:29

namespace Jakar.Database;


public readonly record struct ForeignKeyInfo( string ForeignTableName, OnActionInfo Info = default )
{
    public static readonly ForeignKeyInfo Empty            = new(string.Empty);
    public readonly        string         ForeignTableName = ForeignTableName.SqlColumnName();
    public readonly        OnActionInfo   Info             = Info;
    public                 bool           IsValid    { [MemberNotNullWhen(true, nameof(ForeignTableName))] get => !string.IsNullOrWhiteSpace(ForeignTableName); }
    public override        string         ToString() => ForeignTableName;

    public static implicit operator ForeignKeyInfo( string foreignTableName ) => new(foreignTableName);
    public static ForeignKeyInfo Create<T>( OnActionInfo info = default )
        where T : TableRecord<T>, ITableRecord<T> => new(T.TableName, info);
}
