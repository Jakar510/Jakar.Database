namespace Jakar.Database;


[DefaultValue(nameof(Default))]
public readonly record struct LengthInfo( int Value )
{
    public static readonly          LengthInfo Default = new(-1);
    public readonly                 bool       IsValid = Value >= 0;
    public readonly                 int        Value   = Value;
    public static implicit operator LengthInfo( int value ) => new(value);
}