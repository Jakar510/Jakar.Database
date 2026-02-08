// Jakar.Database :: Jakar.Database
// 02/07/2026  17:06

namespace Jakar.Database;


public readonly record struct ColumnDefaultMetaData( string? Defaults )
{
    public static readonly ColumnDefaultMetaData Empty    = new(null);
    public readonly        string?               Defaults = Defaults;
    public                 bool                  IsValid { [MemberNotNullWhen(true, nameof(Defaults))] get => !string.IsNullOrWhiteSpace(Defaults); }


    public static implicit operator ColumnDefaultMetaData( string check )      => new(check);
    public static                   ColumnDefaultMetaData? TryCreate<TValue>() => TryCreate(typeof(TValue));
    public static ColumnDefaultMetaData? TryCreate( in Type type )
    {
        if ( type == typeof(DateTimeOffset) ) { return new ColumnDefaultMetaData(@"SYSUTCDATETIME()"); }

        if ( type == typeof(DateTime) ) { return new ColumnDefaultMetaData(@"CURRENT_TIMESTAMP()"); }

        return null;
    }
}
