// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 03/09/2026  21:55

using System.Collections.Immutable;
using ZLinq;



namespace Jakar.SqlBuilder;


public interface IValueEnumerator<out TSelf, T> : IValueEnumerator<T>
    where TSelf : allows ref struct
{
    public ImmutableArray<T> Array   { get; }
    public T?                Current { get; }
    public ReadOnlySpan<T>   Span    { get; }


    public bool  MoveNext();
    public void  Reset();
    public TSelf GetEnumerator();
}
