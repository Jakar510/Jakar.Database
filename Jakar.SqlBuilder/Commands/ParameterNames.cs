// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 03/09/2026  21:55

using System.Collections.Immutable;
using System.Threading;



namespace Jakar.SqlBuilder;


/// <summary> The sorted names of a set of parameters, loaded on first use. Shared by <see cref="ParameterNames"/> and <see cref="ExtraParameterNames"/>. </summary>
internal static class ParameterNameList
{
    public static string[] Sorted( scoped ReadOnlySpan<CommandParameter> parameters )
    {
        string[] names = new string[parameters.Length];
        for ( int i = 0; i < names.Length; i++ ) { names[i] = parameters[i].ParameterName; }

        Array.Sort(names, Comparer<string>.Default);
        return names;
    }

    public static string[] Sorted( scoped ReadOnlySpan<ImmutableArray<CommandParameter>> groups )
    {
        int count = 0;
        foreach ( ImmutableArray<CommandParameter> group in groups ) { count += group.Length; }

        string[] names = new string[count];
        int      index = 0;

        foreach ( ImmutableArray<CommandParameter> group in groups )
        {
            foreach ( CommandParameter parameter in group ) { names[index++] = parameter.ParameterName; }
        }

        Array.Sort(names, Comparer<string>.Default);
        return names;
    }
}



/// <summary> The sorted names of the top-level <see cref="CommandParameters.Values"/>. </summary>
public ref struct ParameterNames( CommandParameters self ) : IValueEnumerator<ParameterNames, string>
{
    private int       __index = -1;
    private string[]? __names;


    public ImmutableArray<string> Array   => [..Names];
    public ReadOnlySpan<string>   Span    => Names;
    public string? Current => __names is not null && __index >= 0 && __index < __names.Length
                                  ? __names[__index]
                                  : null;
    private string[] Names => __names ??= ParameterNameList.Sorted(self.Values);


    public bool MoveNext()
    {
        int index = Interlocked.Increment(ref __index);
        return index < Names.Length;
    }

    public void Reset()
    {
        __index = -1; // before the first name: MoveNext advances to 0
        __names = ParameterNameList.Sorted(self.Values);
    }
    public void Dispose()
    {
        __names = null;
        __index = -1;
    }
    public ParameterNames GetEnumerator() => this;
    public bool TryGetNext( out string current )
    {
        bool result = MoveNext();
        current = Current!;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return result && current is not null;
    }
    public bool TryGetNonEnumeratedCount( out int count )
    {
        count = Names.Length;
        return true;
    }
    public bool TryGetSpan( out ReadOnlySpan<string> span )
    {
        span = Names;
        return true;
    }
    public bool TryCopyTo( scoped Span<string> destination, Index offset ) => Names.AsSpan()[offset..].TryCopyTo(destination);
}



/// <summary> The sorted names of every group's parameters (<see cref="CommandParameters.Groups"/>). </summary>
public ref struct ExtraParameterNames( CommandParameters self ) : IValueEnumerator<ExtraParameterNames, string>
{
    private int       __index = -1;
    private string[]? __names;


    public ImmutableArray<string> Array   => [..Names];
    public ReadOnlySpan<string>   Span    => Names;
    public string? Current => __names is not null && __index >= 0 && __index < __names.Length
                                  ? __names[__index]
                                  : null;
    private string[] Names => __names ??= ParameterNameList.Sorted(self.Groups);


    public bool MoveNext()
    {
        int index = Interlocked.Increment(ref __index);
        return index < Names.Length;
    }

    public void Reset()
    {
        __index = -1; // before the first name: MoveNext advances to 0
        __names = ParameterNameList.Sorted(self.Groups);
    }
    public void Dispose()
    {
        __names = null;
        __index = -1;
    }
    public ExtraParameterNames GetEnumerator() => this;
    public bool TryGetNext( out string current )
    {
        bool result = MoveNext();
        current = Current!;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return result && current is not null;
    }
    public bool TryGetNonEnumeratedCount( out int count )
    {
        count = Names.Length;
        return true;
    }
    public bool TryGetSpan( out ReadOnlySpan<string> span )
    {
        span = Names;
        return true;
    }
    public bool TryCopyTo( scoped Span<string> destination, Index offset ) => Names.AsSpan()[offset..].TryCopyTo(destination);
}
