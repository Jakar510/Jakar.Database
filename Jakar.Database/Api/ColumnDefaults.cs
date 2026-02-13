// Jakar.Database :: Jakar.Database
// 02/07/2026  17:06

namespace Jakar.Database;


public readonly record struct ColumnDefaults( string? Defaults )
{
    public static readonly ColumnDefaults Empty           = new(null);
    public static readonly ColumnDefaults Guid            = new(@"gen_random_uuid()");
    public static readonly ColumnDefaults DateTimeOffset  = new(@"SYSUTCDATETIME()");
    public static readonly ColumnDefaults DateTime        = new(@"NOW()");
    public static readonly ColumnDefaults DateOnly        = new(@"CURRENT_DATE()");
    public readonly        string?        Defaults        = Defaults;
    public                 bool           IsValid { [MemberNotNullWhen(true, nameof(Defaults))] get => !string.IsNullOrWhiteSpace(Defaults); }


    public static implicit operator ColumnDefaults( string check )      => Create(check);
    public static implicit operator ColumnDefaults?( Type  check )      => TryCreate(check);
    public static                   ColumnDefaults? TryCreate<TValue>() => TryCreate(typeof(TValue));
    public static ColumnDefaults? TryCreate( in Type type ) => type.Name switch
                                                               {
                                                                   nameof(System.DateTimeOffset) => DateTimeOffset,
                                                                   nameof(System.DateTime)       => DateTime,
                                                                   nameof(System.Guid)           => Guid,
                                                                   nameof(System.DateOnly)       => DateOnly,
                                                                   _                             => null!
                                                               };
    public static ColumnDefaults Create( in string type ) => TryCreate(type) ?? Empty;
    public static ColumnDefaults? TryCreate( in string type ) => type switch
                                                                 {
                                                                     nameof(System.DateTimeOffset) => DateTimeOffset,
                                                                     nameof(System.DateTime)       => DateTime,
                                                                     nameof(System.Guid)           => Guid,
                                                                     nameof(System.DateOnly)       => DateOnly, 
                                                                     null                          => null!,
                                                                     _                             => new ColumnDefaults(type)
                                                                 };
}
