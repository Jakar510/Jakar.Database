// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 06/27/2026

namespace Jakar.SqlBuilder;


/// <summary> Bitset of clauses emitted so far, used by runtime structural verification. </summary>
[Flags]
public enum ClauseFlags : uint
{
    None      = 0,
    Select    = 1 << 0,
    From      = 1 << 1,
    Join      = 1 << 2,
    On        = 1 << 3,
    Where     = 1 << 4,
    GroupBy   = 1 << 5,
    Having    = 1 << 6,
    OrderBy   = 1 << 7,
    Limit     = 1 << 8,
    Offset    = 1 << 9,
    Insert    = 1 << 10,
    Into      = 1 << 11,
    Values    = 1 << 12,
    Update    = 1 << 13,
    Set       = 1 << 14,
    Delete    = 1 << 15,
    Returning = 1 << 16,
    Output    = 1 << 17,
    Predicate = 1 << 18
}
