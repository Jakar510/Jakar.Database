// Jakar.SqlBuilder :: Jakar.SqlBuilder
// 10/25/2026

namespace Jakar.SqlBuilder;


/// <summary> Whether a CLR value can be compared with a column of a CLR type (<see cref="SqlBuilderOptions.StrictTypes"/>). Numbers compare with numbers, text with text, and so on. </summary>
internal static class SqlTypeCheck
{
    public static bool IsCompatible( Type columnType, object? value )
    {
        if ( value is null or DBNull ) { return true; }

        if ( value is ISqlParameterValue wrapped ) { return IsCompatible(columnType, wrapped.ParameterValue); }

        Type column = Storage(Nullable.GetUnderlyingType(columnType) ?? columnType);
        Type actual = value.GetType();

        if ( column.IsAssignableFrom(actual) ) { return true; }

        TypeKind left  = Kind(column);
        TypeKind right = Kind(actual);
        return left is not TypeKind.Other && left == right;
    }

    private static readonly ConcurrentDictionary<Type, Type> __storage = new();

    /// <summary> What a column of <paramref name="type"/> stores: the <c>TValue</c> of an <see cref="ISqlParameterValue{TValue}"/> (a record id column holds a <see cref="Guid"/>), else the type itself. Found once per type. </summary>
    private static Type Storage( Type type ) => __storage.GetOrAdd(type, static type =>
                                                                         {
                                                                             foreach ( Type contract in type.GetInterfaces() )
                                                                             {
                                                                                 if ( contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ISqlParameterValue<>) ) { return contract.GetGenericArguments()[0]; }
                                                                             }

                                                                             return type;
                                                                         });

    private static TypeKind Kind( Type type )
    {
        if ( type.IsEnum ) { return TypeKind.Number; }

        return Type.GetTypeCode(type) switch
               {
                   TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal => TypeKind.Number,
                   TypeCode.String or TypeCode.Char                                                                                                                                                       => TypeKind.Text,
                   TypeCode.Boolean                                                                                                                                                                       => TypeKind.Boolean,
                   TypeCode.DateTime                                                                                                                                                                      => TypeKind.Time,
                   _ when type == typeof(Int128) || type == typeof(UInt128) || type == typeof(Half)                                                                                                       => TypeKind.Number,
                   _ when type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan)                                                              => TypeKind.Time,
                   _ when type == typeof(Guid)                                                                                                                                                            => TypeKind.Guid,
                   _ when type == typeof(byte[])                                                                                                                                                          => TypeKind.Binary,
                   _                                                                                                                                                                                      => TypeKind.Other
               };
    }



    private enum TypeKind
    {
        Other,
        Number,
        Text,
        Boolean,
        Time,
        Guid,
        Binary
    }
}
