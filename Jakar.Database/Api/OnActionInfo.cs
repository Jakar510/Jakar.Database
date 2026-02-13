// Jakar.Database :: Jakar.Database
// 02/12/2026  18:29

namespace Jakar.Database;


[DefaultValue(nameof(Empty))]
public readonly record struct OnActionInfo( string Action )
{
    public static readonly OnActionInfo Empty  = new(string.Empty);
    public readonly        string       Action = Action;
    public                 bool         IsValid                             { [MemberNotNullWhen(true, nameof(Action))] get => !string.IsNullOrWhiteSpace(Action); }
    public override        string       ToString()                          => Action;
    public static          OnActionInfo OnDelete( string next = "CASCADE" ) => new($"ON DELETE {next}");
    public static          OnActionInfo OnUpdate( string next = "CASCADE" ) => new($"ON UPDATE {next}");
    public static OnActionInfo TryCreate( [NotNullIfNotNull(nameof(onAction))] string? onAction ) => !string.IsNullOrWhiteSpace(onAction)
                                                                                                         ? new OnActionInfo(onAction)
                                                                                                         : Empty;
}
