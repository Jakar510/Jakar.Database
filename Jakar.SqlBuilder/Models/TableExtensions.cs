using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Dapper.Contrib.Extensions;



namespace Jakar.SqlBuilder;


public static class TableExtensions
{
    private static readonly ConcurrentDictionary<Type, string> _cache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string GetTableName<TRecord>( this TRecord _ ) => typeof(TRecord).GetTableName();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static string GetTableName<TRecord>()                 => typeof(TRecord).GetTableName();



    extension( Type type )
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public string GetTableName() => _cache.GetOrAdd(type, GetTableNameInternal);
        
        
        private string GetTableNameInternal()
        {
            string name = type.GetCustomAttribute<TableAttribute>()?.Name ?? type.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>()?.Name ?? type.Name;

            // name = name.ToSnakeCase(CultureInfo.InvariantCulture)
            return name;
        }
    }
}
