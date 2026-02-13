namespace Jakar.Database;


[Flags]
public enum ColumnOptions : ulong
{
    None            = 0,
    PrimaryKey      = 1 << 0,
    ForeignKey      = 1 << 1,
    Unique          = 1 << 2,
    Nullable        = 1 << 3,
    Indexed         = 1 << 4,
    Fixed           = 1 << 5,
    AlwaysIdentity  = 1 << 6,
    DefaultIdentity = 1 << 7,
    All             = ~0UL
}
