// Jakar.Database :: Jakar.Database
// 02/17/2026  20:58

namespace Jakar.Database;


public readonly record struct SavePointName( string Value )
{
    public static implicit operator string( SavePointName name ) => name.Value;
    public static implicit operator SavePointName( string name ) => new(name);
    public override                 string ToString()            => Value;
}
