using ZLinq;



namespace Jakar.Database.Generators;


[Generator]
public sealed class TableRecordGenerator : IIncrementalGenerator
{
    // Reported when a concrete type implements ITableRecord<TSelf> but is neither 'sealed' nor 'abstract'. End records must be sealed (or abstract for intermediate bases).
    private static readonly DiagnosticDescriptor MUST_BE_SEALED_OR_ABSTRACT = new("JDB001",
                                                                                  "ITableRecord must be sealed or abstract",
                                                                                  "'{0}' implements ITableRecord<TSelf> and must be declared 'sealed' or 'abstract'",
                                                                                  "Jakar.Database.Generators",
                                                                                  DiagnosticSeverity.Error,
                                                                                  true);

    // Reported when [StringCompare] is applied to a property whose type is not 'string'; the attribute is ignored in that case.
    private static readonly DiagnosticDescriptor STRING_COMPARE_ON_NON_STRING = new("JDB002",
                                                                                    "StringCompare is only valid on string properties",
                                                                                    "[StringCompare] on '{0}' is ignored because its type is not 'string'",
                                                                                    "Jakar.Database.Generators",
                                                                                    DiagnosticSeverity.Warning,
                                                                                    true);


    public void Initialize( IncrementalGeneratorInitializationContext context )
    {
        IncrementalValuesProvider<GenerationCandidate> candidates = context.SyntaxProvider.CreateSyntaxProvider(static ( node, _ ) => node is TypeDeclarationSyntax { Modifiers: var modifiers } && modifiers.Any(SyntaxKind.PartialKeyword), static ( ctx, _ ) => GetCandidate(ctx)).Where(static candidate => candidate is not null).Select(static ( candidate, _ ) => candidate!);

        IncrementalValuesProvider<Diagnostic> diagnostics = context.SyntaxProvider.CreateSyntaxProvider(static ( node, _ ) => node is TypeDeclarationSyntax, static ( ctx, _ ) => GetDiagnostic(ctx)).Where(static diagnostic => diagnostic is not null).Select(static ( diagnostic, _ ) => diagnostic!);

        IncrementalValuesProvider<Diagnostic> stringCompareDiagnostics = context.SyntaxProvider.CreateSyntaxProvider(static ( node, _ ) => node is PropertyDeclarationSyntax { AttributeLists.Count: > 0 }, static ( ctx, _ ) => GetStringCompareDiagnostic(ctx)).Where(static diagnostic => diagnostic is not null).Select(static ( diagnostic, _ ) => diagnostic!);

        context.RegisterSourceOutput(diagnostics,              static ( SourceProductionContext spc, Diagnostic diagnostic ) => spc.ReportDiagnostic(diagnostic));
        context.RegisterSourceOutput(stringCompareDiagnostics, static ( SourceProductionContext spc, Diagnostic diagnostic ) => spc.ReportDiagnostic(diagnostic));

        context.RegisterSourceOutput(candidates.Collect(),
                                     static ( SourceProductionContext spc, ImmutableArray<GenerationCandidate> values ) =>
                                     {
                                         HashSet<GenerationCandidate> set = new();

                                         // ReSharper disable once ForCanBeConvertedToForeach
                                         for ( int i = 0; i < values.Length; i++ )
                                         {
                                             GenerationCandidate candidate = values[i];
                                             if ( !candidate.ShouldGenerate || !set.Add(candidate) ) { continue; }

                                             spc.AddSource($"{candidate.HintName}.g.cs", SourceText.From(Render(candidate), Encoding.UTF8));
                                         }
                                     });
    }


    private static GenerationCandidate? GetCandidate( GeneratorSyntaxContext context )
    {
        if ( context.Node is not TypeDeclarationSyntax syntax ) { return null; }

        if ( context.SemanticModel.GetDeclaredSymbol(syntax) is not INamedTypeSymbol symbol ) { return null; }

        if ( symbol.IsAbstract || symbol.TypeParameters.Length > 0 || symbol.ContainingType is not null ) { return null; }

        if ( !ImplementsITableRecord(symbol) ) { return null; }

        ImmutableArray<string>         declaredProperties = GetDeclaredProperties(symbol);
        ImmutableArray<ImportProperty> importProperties   = GetImportProperties(symbol);

        bool hasReaderCtor          = symbol.InstanceConstructors.Any(static ctor => ctor.Parameters.Length == 1 && IsDbDataReader(ctor.Parameters[0].Type));
        bool hasCreate              = HasMethod(symbol, "Create",              static method => method.IsStatic  && method.Parameters.Length == 1 && IsDbDataReader(method.Parameters[0].Type));
        bool hasToDynamicParameters = HasMethod(symbol, "ToDynamicParameters", static method => !method.IsStatic && method.Parameters.Length == 0);
        bool hasExport              = HasMethod(symbol, "Export",              static method => method.IsStatic  && method.Parameters.Length == 2 && IsNpgsqlBinaryExporter(method.Parameters[0].Type) && IsCancellationToken(method.Parameters[1].Type));
        bool hasBatchImport         = HasMethod(symbol, "Import",              static method => !method.IsStatic && method.Parameters.Length == 2 && IsNpgsqlBatchCommand(method.Parameters[0].Type)   && IsCancellationToken(method.Parameters[1].Type));
        bool hasBinaryImport        = HasMethod(symbol, "Import",              static method => !method.IsStatic && method.Parameters.Length == 2 && IsNpgsqlBinaryImporter(method.Parameters[0].Type) && IsCancellationToken(method.Parameters[1].Type));
        bool hasDataRowImport       = HasMethod(symbol, "Import",              static method => !method.IsStatic && method.Parameters.Length == 2 && IsDataRow(method.Parameters[0].Type)             && IsCancellationToken(method.Parameters[1].Type));

        // The generated Export factory adds a secondary constructor chained to the framework base ctor; a record with a primary constructor would require `: this(...)` instead, so skip Export for those.
        bool hasPrimaryConstructor = syntax is RecordDeclarationSyntax { ParameterList: not null };

        ImmutableArray<EqualityProperty> equalityProperties = GetEqualityProperties(symbol);

        bool        generateCreate              = hasReaderCtor                 && !hasCreate;
        bool        generateToDynamicParameters = declaredProperties.Length > 0 && !hasToDynamicParameters;
        ExportPlan? exportPlan                  = hasExport || hasPrimaryConstructor ? null : GetExportPlan(symbol);
        bool        generateBatchImport         = !hasBatchImport;
        bool        generateBinaryImport        = !hasBinaryImport;
        bool        generateDataRowImport       = declaredProperties.Length > 0 && !hasDataRowImport;

        ImmutableArray<string> columnOrder = GetColumnOrder(symbol) ?? [];

        return new GenerationCandidate(symbol.ContainingNamespace.IsGlobalNamespace
                                           ? null
                                           : symbol.ContainingNamespace.ToDisplayString(),
                                       symbol.Name,
                                       $"{GetHintPrefix(symbol)}.{symbol.Name}.TableRecord",
                                       GetTypeDeclaration(syntax),
                                       declaredProperties,
                                       importProperties,
                                       equalityProperties,
                                       columnOrder,
                                       generateCreate,
                                       generateToDynamicParameters,
                                       exportPlan,
                                       generateBatchImport,
                                       generateBinaryImport,
                                       generateDataRowImport);
    }


    private static Diagnostic? GetDiagnostic( GeneratorSyntaxContext context )
    {
        if ( context.Node is not TypeDeclarationSyntax syntax ) { return null; }

        if ( context.SemanticModel.GetDeclaredSymbol(syntax) is not INamedTypeSymbol symbol ) { return null; }

        if ( symbol.IsAbstract || symbol.IsSealed || symbol.IsValueType ) { return null; }

        if ( !ImplementsITableRecord(symbol) ) { return null; }

        // A partial type has multiple declarations; only report once (on the first declaring syntax) to avoid duplicate diagnostics.
        if ( symbol.DeclaringSyntaxReferences.Length > 0 && symbol.DeclaringSyntaxReferences[0].GetSyntax() != context.Node ) { return null; }

        return Diagnostic.Create(MUST_BE_SEALED_OR_ABSTRACT, syntax.Identifier.GetLocation(), symbol.Name);
    }


    private static Diagnostic? GetStringCompareDiagnostic( GeneratorSyntaxContext context )
    {
        if ( context.Node is not PropertyDeclarationSyntax syntax ) { return null; }

        if ( context.SemanticModel.GetDeclaredSymbol(syntax) is not IPropertySymbol symbol ) { return null; }

        if ( !HasAttribute(symbol, "StringCompareAttribute") ) { return null; }

        if ( symbol.Type.SpecialType == SpecialType.System_String ) { return null; }

        return Diagnostic.Create(STRING_COMPARE_ON_NON_STRING, syntax.Identifier.GetLocation(), symbol.Name);
    }

    private static ImmutableArray<string> GetDeclaredProperties( INamedTypeSymbol symbol ) =>
    [
        ..symbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(static property => property is { IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null })
                .Where(property => SymbolEqualityComparer.Default.Equals(property.ContainingType, symbol))
                .Where(static property => !HasAttribute(property, "DbIgnoreAttribute"))
                .OrderBy(GetPropertyOrder)
                .Select(static property => property.Name)
    ];

    // Declared (this-type-only) public instance properties that have BOTH a getter and a setter (init counts), excluding [DbIgnore].
    // Ordered by [SortOrder(priority)] ascending (lower compared first), then declaration order. Used for the generated Equals / CompareTo / GetHashCode.
    private static ImmutableArray<EqualityProperty> GetEqualityProperties( INamedTypeSymbol symbol ) =>
    [
        ..symbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(static property => property is { IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null, SetMethod: not null })
                .Where(property => SymbolEqualityComparer.Default.Equals(property.ContainingType, symbol))
                .Where(static property => !HasAttribute(property, "DbIgnoreAttribute"))
                .OrderBy(GetSortOrder)
                .ThenBy(GetPropertyOrder)
                .Select(static property => CreateEqualityProperty(property))
    ];

    private static EqualityProperty CreateEqualityProperty( IPropertySymbol property )
    {
        bool isString = property.Type.SpecialType == SpecialType.System_String;

        // Strings use string.Equals / string.Compare with an explicit StringComparison ([StringCompare] or Ordinal by default); [StringCompare] on non-string properties is ignored (warned via JDB002).
        string? stringComparison = isString
                                       ? $"global::System.StringComparison.{GetStringComparison(property) ?? "Ordinal"}"
                                       : null;

        return new EqualityProperty(property.Name, property.Type.ToDisplayString(FULLY_QUALIFIED_NULLABLE), isString, stringComparison);
    }

    private static int GetSortOrder( IPropertySymbol property )
    {
        foreach ( AttributeData attribute in property.GetAttributes() )
        {
            if ( attribute.AttributeClass?.Name != "SortOrderAttribute" ) { continue; }

            if ( attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is int priority ) { return priority; }
        }

        return int.MaxValue;
    }

    private static string? GetStringComparison( IPropertySymbol property )
    {
        foreach ( AttributeData attribute in property.GetAttributes() )
        {
            if ( attribute.AttributeClass?.Name != "StringCompareAttribute" ) { continue; }

            if ( attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is int value )
            {
                return value switch
                       {
                           0 => "CurrentCulture",
                           1 => "CurrentCultureIgnoreCase",
                           2 => "InvariantCulture",
                           3 => "InvariantCultureIgnoreCase",
                           4 => "Ordinal",
                           5 => "OrdinalIgnoreCase",
                           _ => "Ordinal"
                       };
            }
        }

        return null;
    }

    private static ImmutableArray<ImportProperty> GetImportProperties( INamedTypeSymbol symbol )
    {
        List<INamedTypeSymbol> hierarchy = [];
        HashSet<string>        seen      = new(StringComparer.Ordinal);
        List<ImportProperty>   results   = [];

        for ( INamedTypeSymbol? current = symbol; current is not null && !IsSystemObject(current); current = current.BaseType ) { hierarchy.Add(current); }

        hierarchy.Reverse();

        foreach ( INamedTypeSymbol current in hierarchy )
        {
            foreach ( IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>().Where(static property => property is { IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null }).Where(static property => !HasAttribute(property, "DbIgnoreAttribute")).OrderBy(GetPropertyOrder) )
            {
                if ( !seen.Add(property.Name) ) { continue; }

                results.Add(new ImportProperty(property.Name, GetWriteExpression(property), IsNullableValueType(property.Type), GetNullableValueExpression(property)));
            }
        }

        return [..results];
    }

    // Size-packed type ordering, ported from Jakar.Database.PostgresTypeComparer.GetSizeInfo so the column order can be computed at build time.
    // SizeKind: 0 = Fixed, 1 = VariableFixed, 2 = VariableUnbounded. TYPE_ORDER_* values are a stable tiebreak between types that share a (kind, size).
    private const int TYPE_ORDER_BOOLEAN          = 0;
    private const int TYPE_ORDER_BYTE             = 1;
    private const int TYPE_ORDER_SHORT            = 2;
    private const int TYPE_ORDER_INT              = 3;
    private const int TYPE_ORDER_SINGLE           = 4;
    private const int TYPE_ORDER_LONG             = 5;
    private const int TYPE_ORDER_DOUBLE           = 6;
    private const int TYPE_ORDER_TIME             = 7;
    private const int TYPE_ORDER_DATETIME         = 8;
    private const int TYPE_ORDER_GUID             = 9;
    private const int TYPE_ORDER_INT128           = 10;
    private const int TYPE_ORDER_UINT128          = 11;
    private const int TYPE_ORDER_DATETIMEOFFSET   = 12;
    private const int TYPE_ORDER_DATE             = 13;
    private const int TYPE_ORDER_DECIMAL          = 14;
    private const int TYPE_ORDER_ENUM             = 15;
    private const int TYPE_ORDER_STRING           = 16;
    private const int TYPE_ORDER_JSON             = 17;
    private const int TYPE_ORDER_XML              = 18;
    private const int TYPE_ORDER_BINARY           = 19;
    private const int TYPE_ORDER_SERIAL           = 20;

    // Fully-qualified display that also carries nullable-reference annotations (string?, JObject?), so generated parameter/local types match the property's declared nullability.
    private static readonly SymbolDisplayFormat FULLY_QUALIFIED_NULLABLE = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);


    /// <summary> Computes the build-time, size-packed column order. Returns <c>null</c> when any column type cannot be confidently mapped, so the runtime falls back to its own ordering. </summary>
    private static ImmutableArray<string>? GetColumnOrder( INamedTypeSymbol symbol )
    {
        ImmutableArray<IPropertySymbol>? sorted = GetSortedColumnProperties(symbol);
        if ( sorted is null ) { return null; }

        return [..sorted.Value.Select(static property => property.Name)];
    }

    /// <summary> Build-time, size-packed column property symbols (same order used for <see cref="GetColumnOrder"/>). Returns <c>null</c> when any column type cannot be confidently mapped. </summary>
    private static ImmutableArray<IPropertySymbol>? GetSortedColumnProperties( INamedTypeSymbol symbol )
    {
        List<INamedTypeSymbol> hierarchy = [];
        for ( INamedTypeSymbol? current = symbol; current is not null && !IsSystemObject(current); current = current.BaseType ) { hierarchy.Add(current); }

        hierarchy.Reverse();

        HashSet<string>  seen    = new(StringComparer.Ordinal);
        List<ColumnSort> columns = [];

        foreach ( INamedTypeSymbol current in hierarchy )
        {
            foreach ( IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>().Where(static property => property is { IsStatic: false, DeclaredAccessibility: Accessibility.Public, GetMethod: not null }).Where(static property => !HasAttribute(property, "DbIgnoreAttribute")).OrderBy(GetPropertyOrder) )
            {
                if ( !seen.Add(property.Name) ) { continue; }

                DbKind? kind = GetDbKind(property.Type);
                if ( kind is null ) { return null; }            // unknown type: let the runtime ordering handle it
                if ( kind.Value.IsExcluded ) { continue; }      // not a real column (e.g. RecordID<,> maps to DbColumnType.NotSet)

                columns.Add(new ColumnSort(property, kind.Value.Kind, kind.Value.Size, kind.Value.TypeOrder, HasAttribute(property, "FixedAttribute"), GetDbSizeMax(property)));
            }
        }

        if ( columns.Count == 0 ) { return null; }

        columns.Sort(static ( a, b ) =>
                     {
                         int compare = a.Kind.CompareTo(b.Kind);
                         if ( compare != 0 ) { return compare; }

                         compare = a.Size.CompareTo(b.Size);
                         if ( compare != 0 ) { return compare; }

                         compare = a.TypeOrder.CompareTo(b.TypeOrder);
                         if ( compare != 0 ) { return compare; }

                         compare = b.IsFixed.CompareTo(a.IsFixed); // [Fixed] columns first
                         if ( compare != 0 ) { return compare; }

                         compare = a.DbSize.CompareTo(b.DbSize); // no [DbSize] (-1) first, then ascending
                         if ( compare != 0 ) { return compare; }

                         return string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase);
                     });

        return [..columns.Select(static column => column.Property)];
    }

    private static int GetDbSizeMax( IPropertySymbol property )
    {
        foreach ( AttributeData attribute in property.GetAttributes() )
        {
            if ( attribute.AttributeClass?.Name != "DbSizeAttribute" ) { continue; }

            // DbSizeAttribute(int? min, int? max)
            if ( attribute.ConstructorArguments.Length == 2 && attribute.ConstructorArguments[1].Value is int max ) { return max; }
        }

        return -1;
    }

    private static DbKind? GetDbKind( ITypeSymbol type )
    {
        if ( type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable ) { type = nullable.TypeArguments[0]; }

        if ( type.TypeKind == TypeKind.Enum ) { return new DbKind(1, 16, TYPE_ORDER_ENUM); }

        switch ( type.SpecialType )
        {
            case SpecialType.System_Boolean: { return new DbKind(0, 1,  TYPE_ORDER_BOOLEAN); }
            case SpecialType.System_Byte:    { return new DbKind(0, 1,  TYPE_ORDER_BYTE); }
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:  { return new DbKind(0, 2,  TYPE_ORDER_SHORT); }
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:  { return new DbKind(0, 4,  TYPE_ORDER_INT); }
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:  { return new DbKind(0, 8,  TYPE_ORDER_LONG); }
            case SpecialType.System_Single:  { return new DbKind(0, 4,  TYPE_ORDER_SINGLE); }
            case SpecialType.System_Double:  { return new DbKind(0, 8,  TYPE_ORDER_DOUBLE); }
            case SpecialType.System_Decimal: { return new DbKind(1, 32, TYPE_ORDER_DECIMAL); }
            case SpecialType.System_String:  { return new DbKind(1, 64, TYPE_ORDER_STRING); }
        }

        if ( type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte } ) { return new DbKind(2, int.MaxValue, TYPE_ORDER_BINARY); }

        if ( type is INamedTypeSymbol named )
        {
            if ( named.Name == "RecordID" )
            {
                return named.TypeArguments.Length switch
                       {
                           1 => new DbKind(0, 16, TYPE_ORDER_GUID),
                           2 => DbKind.Excluded,
                           _ => null
                       };
            }

            if ( named.Name == "AutoRecordID" && named.TypeArguments.Length == 1 ) { return new DbKind(2, int.MaxValue, TYPE_ORDER_SERIAL); }

            if ( named.Name == "UserRights" ) { return new DbKind(1, 64, TYPE_ORDER_STRING); }

            switch ( type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) )
            {
                case "global::System.Guid":           { return new DbKind(0, 16, TYPE_ORDER_GUID); }
                case "global::System.Int128":         { return new DbKind(0, 16, TYPE_ORDER_INT128); }
                case "global::System.UInt128":        { return new DbKind(0, 16, TYPE_ORDER_UINT128); }
                case "global::System.DateTime":       { return new DbKind(0, 8,  TYPE_ORDER_DATETIME); }
                case "global::System.DateTimeOffset": { return new DbKind(0, 16, TYPE_ORDER_DATETIMEOFFSET); }
                case "global::System.DateOnly":       { return new DbKind(0, 4,  TYPE_ORDER_DATE); }
                case "global::System.TimeOnly":
                case "global::System.TimeSpan":       { return new DbKind(0, 8,  TYPE_ORDER_TIME); }
            }

            if ( IsJsonType(named) ) { return new DbKind(2, int.MaxValue, TYPE_ORDER_JSON); }

            if ( IsXmlType(named) ) { return new DbKind(2, int.MaxValue, TYPE_ORDER_XML); }
        }

        return null;
    }

    private static bool IsJsonType( INamedTypeSymbol type )
    {
        for ( INamedTypeSymbol? current = type; current is not null; current = current.BaseType )
        {
            switch ( current.Name )
            {
                case "JToken":
                case "JObject":
                case "JArray":
                case "JValue":
                case "JsonNode":
                case "JsonObject":
                case "JsonArray":
                case "JsonDocument":
                case "JsonElement": { return true; }
            }
        }

        return false;
    }

    private static bool IsXmlType( INamedTypeSymbol type )
    {
        for ( INamedTypeSymbol? current = type; current is not null; current = current.BaseType )
        {
            if ( current.Name is "XmlNode" or "XmlDocument" or "XmlElement" ) { return true; }
        }

        return false;
    }


    // ----- Export (binary COPY TO STDOUT) read -> TSelf factory ------------------------------------------------------------------------------------------------------------------------
    // Reads each column positionally (build-time column order) with Read<T> and constructs the record via a generated positional constructor that chains to the framework base ctor.
    // Returns null (no Export generated) when the base type or any column type cannot be confidently handled, so this never breaks a build.

    private static BaseCtorKind? GetBaseCtorKind( INamedTypeSymbol symbol ) =>
        symbol.BaseType?.Name switch
        {
            "OwnedTableRecord"   => BaseCtorKind.OwnedTableRecord,
            "Mapping"            => BaseCtorKind.Mapping,
            "PairRecord"         => BaseCtorKind.PairRecord,
            "LastModifiedRecord" => BaseCtorKind.LastModifiedRecord,
            "TableRecord"        => BaseCtorKind.TableRecord,
            _                    => null
        };

    private static FrameworkRole GetFrameworkRole( string name ) =>
        name switch
        {
            "DateCreated"    => FrameworkRole.DateCreated,
            "LastModified"   => FrameworkRole.LastModified,
            "ID"             => FrameworkRole.Id,
            "UserID"         => FrameworkRole.UserId,
            "AdditionalData" => FrameworkRole.AdditionalData,
            "KeyID"          => FrameworkRole.KeyId,
            "ValueID"        => FrameworkRole.ValueId,
            _                => FrameworkRole.Leaf
        };

    private static ExportPlan? GetExportPlan( INamedTypeSymbol symbol )
    {
        BaseCtorKind? baseKind = GetBaseCtorKind(symbol);
        if ( baseKind is null ) { return null; }

        ImmutableArray<IPropertySymbol>? sorted = GetSortedColumnProperties(symbol);
        if ( sorted is null ) { return null; }

        List<ExportColumn> columns = [];

        foreach ( IPropertySymbol property in sorted.Value )
        {
            ExportColumn? column = GetExportColumn(property);
            if ( column is null ) { return null; } // a column we cannot read back: skip Export for this type

            columns.Add(column);
        }

        return HasRequiredFramework(baseKind.Value, columns)
                   ? new ExportPlan(baseKind.Value, [..columns])
                   : null;
    }

    private static bool HasRequiredFramework( BaseCtorKind baseKind, List<ExportColumn> columns )
    {
        bool has( FrameworkRole role ) => columns.Any(column => column.Role == role);

        return baseKind switch
               {
                   BaseCtorKind.TableRecord        => has(FrameworkRole.DateCreated),
                   BaseCtorKind.LastModifiedRecord => has(FrameworkRole.DateCreated) && has(FrameworkRole.LastModified),
                   BaseCtorKind.PairRecord         => has(FrameworkRole.Id)    && has(FrameworkRole.DateCreated) && has(FrameworkRole.AdditionalData) && has(FrameworkRole.LastModified),
                   BaseCtorKind.OwnedTableRecord   => has(FrameworkRole.UserId) && has(FrameworkRole.Id)         && has(FrameworkRole.DateCreated)    && has(FrameworkRole.LastModified) && has(FrameworkRole.AdditionalData),
                   BaseCtorKind.Mapping            => has(FrameworkRole.KeyId)  && has(FrameworkRole.ValueId)    && has(FrameworkRole.DateCreated),
                   _                               => false
               };
    }

    private static ExportColumn? GetExportColumn( IPropertySymbol property )
    {
        ITypeSymbol   type        = property.Type;
        string        typeDisplay = type.ToDisplayString(FULLY_QUALIFIED_NULLABLE);
        string        localName   = ToLocalName(property.Name);
        FrameworkRole role        = GetFrameworkRole(property.Name);
        bool          settable    = property.SetMethod is not null; // get-only computed columns are read (to advance the stream) but not constructed

        bool nullable = false;
        if ( type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } wrapper )
        {
            nullable = true;
            type     = wrapper.TypeArguments[0];
        }

        string underlying = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if ( type is INamedTypeSymbol jsonType && IsJsonType(jsonType) ) { return new ExportColumn(property.Name, localName, typeDisplay, ReadKind.AdditionalData, "string", role, settable); }

        if ( type is INamedTypeSymbol { Name: "RecordID" } recordId && recordId.TypeArguments.Length == 1 ) { return new ExportColumn(property.Name, localName, typeDisplay, nullable ? ReadKind.NullableRecordId : ReadKind.RecordIdValue, underlying, role, settable); }

        if ( type.TypeKind == TypeKind.Enum ) { return new ExportColumn(property.Name, localName, typeDisplay, nullable ? ReadKind.NullableEnum : ReadKind.EnumValue, underlying, role, settable); }

        if ( type.SpecialType == SpecialType.System_String ) { return new ExportColumn(property.Name, localName, typeDisplay, ReadKind.NullSafeDirect, "string", role, settable); }

        if ( IsDirectlyReadable(type) ) { return new ExportColumn(property.Name, localName, typeDisplay, nullable ? ReadKind.NullableDirect : ReadKind.Direct, underlying, role, settable); }

        return null; // UserRights, byte[], Xml, and anything else: skip Export for this type
    }

    private static bool IsDirectlyReadable( ITypeSymbol type )
    {
        switch ( type.SpecialType )
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal: { return true; }
        }

        switch ( type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) )
        {
            case "global::System.Guid":
            case "global::System.Int128":
            case "global::System.UInt128":
            case "global::System.DateTime":
            case "global::System.DateTimeOffset":
            case "global::System.DateOnly":
            case "global::System.TimeOnly":
            case "global::System.TimeSpan": { return true; }
        }

        return false;
    }

    private static string ToLocalName( string name ) => name.Length == 0
                                                            ? name
                                                            : char.ToLowerInvariant(name[0]) + name.Substring(1);


    private static List<ExportColumn> FrameworkOrder( ExportPlan plan )
    {
        ExportColumn byRole( FrameworkRole role ) => plan.Columns.First(column => column.Role == role);

        return plan.BaseKind switch
               {
                   BaseCtorKind.TableRecord        => [byRole(FrameworkRole.DateCreated)],
                   BaseCtorKind.LastModifiedRecord => [byRole(FrameworkRole.DateCreated), byRole(FrameworkRole.LastModified)],
                   BaseCtorKind.PairRecord         => [byRole(FrameworkRole.Id), byRole(FrameworkRole.DateCreated), byRole(FrameworkRole.AdditionalData), byRole(FrameworkRole.LastModified)],
                   BaseCtorKind.OwnedTableRecord   => [byRole(FrameworkRole.UserId), byRole(FrameworkRole.Id), byRole(FrameworkRole.DateCreated), byRole(FrameworkRole.LastModified), byRole(FrameworkRole.AdditionalData)],
                   BaseCtorKind.Mapping            => [byRole(FrameworkRole.KeyId), byRole(FrameworkRole.ValueId), byRole(FrameworkRole.DateCreated)],
                   _                               => []
               };
    }

    private static void RenderExport( StringBuilder sb, ExportPlan plan, string typeName )
    {
        List<ExportColumn> frameworkOrder = FrameworkOrder(plan);

        // get-only computed columns are still read (below, to advance the export stream) but cannot be assigned, so they are excluded from construction
        List<ExportColumn> leaf             = plan.Columns.Where(static column => column.Role == FrameworkRole.Leaf && column.IsSettable).ToList();
        List<ExportColumn> constructorOrder = [..frameworkOrder, ..leaf];

        HashSet<string> constructed = new(constructorOrder.Select(static column => column.Name), StringComparer.Ordinal);

        sb.AppendLine("    /// <summary>Reads the current binary-export row (COPY TO STDOUT) and constructs the record. The caller must call <c>StartRowAsync</c> before each row.</summary>");
        sb.Append("    public static async ValueTask<").Append(typeName).AppendLine("> Export( NpgsqlBinaryExporter exporter, CancellationToken token )");
        sb.AppendLine("    {");

        foreach ( ExportColumn column in plan.Columns )
        {
            if ( constructed.Contains(column.Name) ) { AppendRead(sb, column); }
            else { AppendConsume(sb, column); } // get-only computed column: consume from the stream and discard
        }

        sb.AppendLine();
        sb.Append("        return new ").Append(typeName).Append('(');

        for ( int i = 0; i < constructorOrder.Count; i++ )
        {
            if ( i > 0 ) { sb.Append(','); }

            sb.Append(' ').Append(constructorOrder[i].LocalName);
        }

        sb.AppendLine(" ).Validate();");
        sb.AppendLine("    }");

        sb.AppendLine();
        sb.AppendLine("    [global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]");
        sb.Append("    private ").Append(typeName).Append('(');

        for ( int i = 0; i < constructorOrder.Count; i++ )
        {
            if ( i > 0 ) { sb.Append(','); }

            sb.Append(' ').Append(constructorOrder[i].TypeDisplay).Append(' ').Append(constructorOrder[i].LocalName);
        }

        sb.Append(" ) : base(");

        for ( int i = 0; i < frameworkOrder.Count; i++ )
        {
            if ( i > 0 ) { sb.Append(','); }

            sb.Append(' ').Append(frameworkOrder[i].LocalName);
        }

        sb.AppendLine(" )");
        sb.AppendLine("    {");

        foreach ( ExportColumn column in leaf ) { sb.Append("        ").Append(column.Name).Append(" = ").Append(column.LocalName).AppendLine(";"); }

        sb.AppendLine("    }");
    }

    private static void AppendRead( StringBuilder sb, ExportColumn column )
    {
        sb.Append("        ").Append(column.TypeDisplay).Append(' ').Append(column.LocalName).AppendLine(";");

        switch ( column.ReadKind )
        {
            case ReadKind.Direct:
                sb.Append("        ").Append(column.LocalName).Append(" = await exporter.ReadAsync<").Append(column.ReadType).AppendLine(">(token);");
                return;

            case ReadKind.EnumValue:
                sb.Append("        ").Append(column.LocalName).Append(" = (").Append(column.ReadType).AppendLine(")await exporter.ReadAsync<long>(token);");
                return;

            case ReadKind.RecordIdValue:
                sb.Append("        ").Append(column.LocalName).Append(" = ").Append(column.ReadType).AppendLine(".Create(await exporter.ReadAsync<global::System.Guid>(token));");
                return;

            case ReadKind.NullSafeDirect:
            case ReadKind.NullableDirect:
                AppendNullableRead(sb, column, $"await exporter.ReadAsync<{column.ReadType}>(token)");
                return;

            case ReadKind.NullableEnum:
                AppendNullableRead(sb, column, $"({column.ReadType})await exporter.ReadAsync<long>(token)");
                return;

            case ReadKind.NullableRecordId:
                AppendNullableRead(sb, column, $"{column.ReadType}.Create(await exporter.ReadAsync<global::System.Guid>(token))");
                return;

            case ReadKind.AdditionalData:
                AppendNullableRead(sb, column, "(await exporter.ReadAsync<string>(token))?.GetAdditionalData()");
                return;
        }
    }

    private static void AppendNullableRead( StringBuilder sb, ExportColumn column, string readExpression )
    {
        sb.Append("        if ( exporter.IsNull ) { await exporter.SkipAsync(token); ").Append(column.LocalName).AppendLine(" = default!; }");
        sb.Append("        else { ").Append(column.LocalName).Append(" = ").Append(readExpression).AppendLine("; }");
    }

    /// <summary> Consumes a column from the export stream and discards it (used for get-only computed columns that cannot be assigned during construction). </summary>
    private static void AppendConsume( StringBuilder sb, ExportColumn column )
    {
        string rawType = column.ReadKind switch
                         {
                             ReadKind.RecordIdValue or ReadKind.NullableRecordId => "global::System.Guid",
                             ReadKind.EnumValue or ReadKind.NullableEnum         => "long",
                             ReadKind.AdditionalData                             => "string",
                             _                                                   => column.ReadType
                         };

        bool nullable = column.ReadKind is ReadKind.NullableDirect or ReadKind.NullableEnum or ReadKind.NullableRecordId or ReadKind.NullSafeDirect or ReadKind.AdditionalData;

        if ( nullable ) { sb.Append("        if ( exporter.IsNull ) { await exporter.SkipAsync(token); } else { _ = await exporter.ReadAsync<").Append(rawType).AppendLine(">(token); }"); }
        else { sb.Append("        _ = await exporter.ReadAsync<").Append(rawType).AppendLine(">(token);"); }
    }

    private static int  GetPropertyOrder( IPropertySymbol        property )                                                 => property.Locations.FirstOrDefault(static location => location.IsInSource)?.SourceSpan.Start ?? int.MaxValue;
    private static bool ImplementsITableRecord( INamedTypeSymbol symbol )                                                   => symbol.AllInterfaces.Any(static type => type.Name == "ITableRecord" && type.ContainingNamespace.ToDisplayString() == "Jakar.Database");
    private static bool HasAttribute( ISymbol                    symbol, string attributeName )                             => symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == attributeName);
    private static bool HasMethod( INamedTypeSymbol              symbol, string name, Func<IMethodSymbol, bool> predicate ) => symbol.GetMembers(name).OfType<IMethodSymbol>().Any(method => SymbolEqualityComparer.Default.Equals(method.ContainingType, symbol) && predicate(method));
    private static bool IsSystemObject( ITypeSymbol              type ) => type.SpecialType is SpecialType.System_Object;
    private static bool IsNullableValueType( ITypeSymbol         type ) => type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
    private static bool IsDbDataReader( ITypeSymbol              type ) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Data.Common.DbDataReader";
    private static bool IsNpgsqlBinaryExporter( ITypeSymbol      type ) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Npgsql.NpgsqlBinaryExporter";
    private static bool IsNpgsqlBatchCommand( ITypeSymbol        type ) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Npgsql.NpgsqlBatchCommand";
    private static bool IsNpgsqlBinaryImporter( ITypeSymbol      type ) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::Npgsql.NpgsqlBinaryImporter";
    private static bool IsCancellationToken( ITypeSymbol         type ) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Threading.CancellationToken";
    private static bool IsDataRow( ITypeSymbol                   type ) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Data.DataRow";

    private static string GetWriteExpression( IPropertySymbol property )
    {
        if ( IsRecordId(property.Type) || IsUserRights(property.Type) ) { return $"{property.Name}.Value"; }

        return property.Name;
    }

    // Expression written inside the `if ( X.HasValue )` branch for a nullable value-type column.
    // For a nullable RecordID<T>? / UserRights? the underlying struct still needs its own `.Value` (the Guid / long), so it is `X.Value.Value`; otherwise just `X.Value`.
    private static string GetNullableValueExpression( IPropertySymbol property )
    {
        if ( property.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable )
        {
            ITypeSymbol underlying = nullable.TypeArguments[0];
            if ( IsRecordId(underlying) || IsUserRights(underlying) ) { return $"{property.Name}.Value.Value"; }
        }

        return $"{property.Name}.Value";
    }

    private static bool IsRecordId( ITypeSymbol   type ) => type is INamedTypeSymbol { Name: "RecordID", ContainingNamespace: { } ns } && ns.ToDisplayString() == "Jakar.Database";
    private static bool IsUserRights( ITypeSymbol type ) => type is INamedTypeSymbol { Name: "UserRights", ContainingNamespace: { } ns } && ns.ToDisplayString().StartsWith("Jakar.", StringComparison.Ordinal);

    private static string GetHintPrefix( INamedTypeSymbol symbol )
    {
        StringBuilder builder = new(symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        builder.Replace("global::", string.Empty);
        builder.Replace('<',        '_');
        builder.Replace('>',        '_');
        builder.Replace('.',        '_');
        return builder.ToString();
    }
    private static string GetTypeDeclaration( TypeDeclarationSyntax syntax ) => syntax switch
                                                                                {
                                                                                    RecordDeclarationSyntax { ClassOrStructKeyword.RawKind: (int)SyntaxKind.StructKeyword } => "partial record struct",
                                                                                    RecordDeclarationSyntax                                                                 => "partial record",
                                                                                    StructDeclarationSyntax                                                                 => "partial struct",
                                                                                    ClassDeclarationSyntax                                                                  => "partial class",
                                                                                    _                                                                                       => throw new InvalidOperationException($"Unsupported declaration kind: {syntax.Kind()}")
                                                                                };

    private static string Render( GenerationCandidate candidate )
    {
        StringBuilder sb = new();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.CodeDom.Compiler;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Data.Common;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Npgsql;");
        sb.AppendLine("using NpgsqlTypes;");
        sb.AppendLine();

        if ( !string.IsNullOrWhiteSpace(candidate.Namespace) )
        {
            sb.Append("namespace ").Append(candidate.Namespace).AppendLine(";");
            sb.AppendLine();
        }

        sb.AppendLine("[GeneratedCode(\"Jakar.Database.TableRecordGenerator\", \"1.0.0\")]");

        if ( !candidate.ColumnOrder.IsDefaultOrEmpty )
        {
            sb.Append("[global::Jakar.Database.GeneratedColumnOrder(");

            for ( int i = 0; i < candidate.ColumnOrder.Length; i++ )
            {
                if ( i > 0 ) { sb.Append(", "); }

                sb.Append('"').Append(candidate.ColumnOrder[i]).Append('"');
            }

            sb.AppendLine(")]");
        }

        sb.Append(candidate.TypeDeclaration).Append(' ').Append(candidate.TypeName).AppendLine();
        sb.AppendLine("{");

        bool needsBlankLine = false;

        if ( candidate.GenerateCreate )
        {
            sb.Append("    public static ").Append(candidate.TypeName).Append(" Create( DbDataReader reader ) => new ").Append(candidate.TypeName).AppendLine("(reader).Validate();");
            needsBlankLine = true;
        }

        if ( candidate.GenerateToDynamicParameters )
        {
            if ( needsBlankLine ) { sb.AppendLine(); }

            sb.AppendLine("    public override CommandParameters ToDynamicParameters()");
            sb.AppendLine("    {");
            sb.AppendLine("        CommandParameters parameters = base.ToDynamicParameters();");

            foreach ( string propertyName in candidate.DeclaredProperties ) { sb.Append("        parameters.Add(nameof(").Append(propertyName).Append("), ").Append(propertyName).AppendLine(");"); }

            sb.AppendLine("        return parameters;");
            sb.AppendLine("    }");
            needsBlankLine = true;
        }

        if ( candidate.ExportPlan is not null )
        {
            if ( needsBlankLine ) { sb.AppendLine(); }

            RenderExport(sb, candidate.ExportPlan, candidate.TypeName);
            needsBlankLine = true;
        }

        if ( candidate.GenerateBatchImport )
        {
            if ( needsBlankLine ) { sb.AppendLine(); }

            sb.AppendLine("    /// <summary>Adds one parameter per column to the batch command so it can be sent as part of a single <see cref=\"NpgsqlBatch\"/> round trip.</summary>");
            sb.AppendLine("    /// <remarks>The caller is responsible for setting <c>batch.CommandText</c>; parameter names match the property names (e.g. <c>@PropertyName</c>).</remarks>");
            sb.AppendLine("    public override ValueTask Import( NpgsqlBatchCommand batch, CancellationToken token )");
            sb.AppendLine("    {");

            foreach ( ImportProperty property in candidate.ImportProperties )
            {
                if ( property.IsNullableValueType )
                {
                    sb.Append("        batch.Parameters.Add(new NpgsqlParameter(nameof(").Append(property.Name).Append("), ").Append(property.Name).Append(".HasValue ? (object)").Append(property.NullableValueExpression).AppendLine(" : DBNull.Value));");
                    continue;
                }

                sb.Append("        batch.Parameters.Add(new NpgsqlParameter(nameof(").Append(property.Name).Append("), (object?)").Append(property.WriteExpression).AppendLine(" ?? DBNull.Value));");
            }

            sb.AppendLine();
            sb.AppendLine("        return token.IsCancellationRequested ? ValueTask.FromCanceled(token) : default;");
            sb.AppendLine("    }");
            needsBlankLine = true;
        }

        if ( candidate.GenerateBinaryImport )
        {
            if ( needsBlankLine ) { sb.AppendLine(); }

            RenderBinaryImport(sb, candidate);
            needsBlankLine = true;
        }

        if ( candidate.GenerateDataRowImport )
        {
            if ( needsBlankLine ) { sb.AppendLine(); }

            sb.AppendLine("    /// <summary>Writes the declared columns of this record into the <see cref=\"DataRow\"/>, then chains to the framework columns on the base type.</summary>");
            sb.AppendLine("    public override ValueTask Import( DataRow row, CancellationToken token )");
            sb.AppendLine("    {");

            foreach ( string propertyName in candidate.DeclaredProperties ) { sb.Append("        row[MetaData[nameof(").Append(propertyName).Append(")].DataColumn] = ").Append(propertyName).AppendLine(";"); }

            sb.AppendLine("        return base.Import(row, token);");
            sb.AppendLine("    }");
            needsBlankLine = true;
        }

        if ( needsBlankLine ) { sb.AppendLine(); }

        RenderEquals(sb, candidate);
        sb.AppendLine();
        RenderGetHashCode(sb, candidate);
        sb.AppendLine();
        RenderCompareTo(sb, candidate);
        sb.AppendLine();
        RenderComparisonOperators(sb, candidate);

        sb.AppendLine("}");
        return sb.ToString();
    }


    // ----- IEqualComparable<TSelf> : Equals / GetHashCode / CompareTo / ordering operators --------------------------------------------------------------------------------------------
    // base.Equals / base.CompareTo / base.GetHashCode fold in every inherited framework column; the generated bodies append the declared columns (get + set) in [SortOrder] order.

    private static void RenderEquals( StringBuilder sb, GenerationCandidate candidate )
    {
        sb.Append("    public override bool Equals( ").Append(candidate.TypeName).AppendLine("? other )");
        sb.AppendLine("    {");
        sb.AppendLine("        if ( other is null ) { return false; }");
        sb.AppendLine();
        sb.AppendLine("        if ( ReferenceEquals(this, other) ) { return true; }");
        sb.AppendLine();

        if ( candidate.EqualityProperties.IsDefaultOrEmpty ) { sb.AppendLine("        return base.Equals(other);"); }
        else
        {
            sb.Append("        return base.Equals(other)");

            foreach ( EqualityProperty property in candidate.EqualityProperties )
            {
                sb.AppendLine();

                if ( property.IsString ) { sb.Append("            && string.Equals(").Append(property.Name).Append(", other.").Append(property.Name).Append(", ").Append(property.StringComparison).Append(")"); }
                else { sb.Append("            && global::System.Collections.Generic.EqualityComparer<").Append(property.TypeDisplay).Append(">.Default.Equals(").Append(property.Name).Append(", other.").Append(property.Name).Append(")"); }
            }

            sb.AppendLine(";");
        }

        sb.AppendLine("    }");
    }

    private static void RenderGetHashCode( StringBuilder sb, GenerationCandidate candidate )
    {
        if ( candidate.EqualityProperties.IsDefaultOrEmpty )
        {
            sb.AppendLine("    public override int GetHashCode() => base.GetHashCode();");
            return;
        }

        sb.AppendLine("    public override int GetHashCode()");
        sb.AppendLine("    {");
        sb.AppendLine("        global::System.HashCode hash = new global::System.HashCode();");
        sb.AppendLine("        hash.Add(base.GetHashCode());");

        foreach ( EqualityProperty property in candidate.EqualityProperties )
        {
            if ( property.IsString ) { sb.Append("        hash.Add(").Append(property.Name).Append(", global::System.StringComparer.FromComparison(").Append(property.StringComparison).AppendLine("));"); }
            else { sb.Append("        hash.Add(").Append(property.Name).AppendLine(");"); }
        }

        sb.AppendLine("        return hash.ToHashCode();");
        sb.AppendLine("    }");
    }

    private static void RenderCompareTo( StringBuilder sb, GenerationCandidate candidate )
    {
        sb.Append("    public override int CompareTo( ").Append(candidate.TypeName).AppendLine("? other )");
        sb.AppendLine("    {");
        sb.AppendLine("        if ( other is null ) { return 1; }");
        sb.AppendLine();
        sb.AppendLine("        if ( ReferenceEquals(this, other) ) { return 0; }");
        sb.AppendLine();
        sb.AppendLine("        int compare = base.CompareTo(other);");
        sb.AppendLine("        if ( compare != 0 ) { return compare; }");

        foreach ( EqualityProperty property in candidate.EqualityProperties )
        {
            sb.AppendLine();

            if ( property.IsString ) { sb.Append("        compare = string.Compare(").Append(property.Name).Append(", other.").Append(property.Name).Append(", ").Append(property.StringComparison).AppendLine(");"); }
            else { sb.Append("        compare = global::System.Collections.Generic.Comparer<").Append(property.TypeDisplay).Append(">.Default.Compare(").Append(property.Name).Append(", other.").Append(property.Name).AppendLine(");"); }

            sb.AppendLine("        if ( compare != 0 ) { return compare; }");
        }

        sb.AppendLine();
        sb.AppendLine("        return compare;");
        sb.AppendLine("    }");
    }

    private static void RenderComparisonOperators( StringBuilder sb, GenerationCandidate candidate )
    {
        string name = candidate.TypeName;

        // Record types already synthesize == / != from their equality contract; only non-record types need them generated here.
        if ( candidate.TypeDeclaration.IndexOf("record", StringComparison.Ordinal) < 0 )
        {
            sb.Append("    public static bool operator ==( ").Append(name).Append("? left, ").Append(name).AppendLine("? right ) => left is null ? right is null : left.Equals(right);");
            sb.Append("    public static bool operator !=( ").Append(name).Append("? left, ").Append(name).AppendLine("? right ) => !( left == right );");
        }

        sb.Append("    public static bool operator >( ").Append(name).Append("  left, ").Append(name).AppendLine(" right ) => left.CompareTo(right) > 0;");
        sb.Append("    public static bool operator >=( ").Append(name).Append(" left, ").Append(name).AppendLine(" right ) => left.CompareTo(right) >= 0;");
        sb.Append("    public static bool operator <( ").Append(name).Append("  left, ").Append(name).AppendLine(" right ) => left.CompareTo(right) < 0;");
        sb.Append("    public static bool operator <=( ").Append(name).Append(" left, ").Append(name).AppendLine(" right ) => left.CompareTo(right) <= 0;");
    }


    // ----- Binary COPY import (public 2-param) ----------------------------------------------------------------------------------------------------------------------------------------
    // When the build-time column order is known, the columns are written straight-line in that exact order (the same order the runtime assigns via [GeneratedColumnOrder]); the
    // per-column NpgsqlDbType is still resolved from MetaData. When the order cannot be computed, falls back to iterating the runtime SortedColumns and dispatching per property name.

    private static void RenderBinaryImport( StringBuilder sb, GenerationCandidate candidate )
    {
        sb.AppendLine("    /// <summary>Writes one row to the binary COPY stream (column order matches the table definition).</summary>");
        sb.AppendLine("    public override async ValueTask Import( NpgsqlBinaryImporter importer, CancellationToken token )");
        sb.AppendLine("    {");
        sb.AppendLine("        await importer.StartRowAsync(token);");

        if ( !candidate.ColumnOrder.IsDefaultOrEmpty )
        {
            foreach ( string columnName in candidate.ColumnOrder )
            {
                if ( !TryGetImportProperty(candidate, columnName, out ImportProperty property) ) { continue; }

                AppendBinaryWrite(sb, property, $"MetaData[nameof({property.Name})].PostgresDbType", "        ");
            }

            sb.AppendLine("    }");
            return;
        }

        // Fallback: drive the writes from the runtime column order.
        sb.AppendLine();
        sb.AppendLine("        foreach ( global::Jakar.Database.ColumnMetaData column in MetaData.SortedColumns )");
        sb.AppendLine("        {");
        sb.AppendLine("            switch ( column.PropertyName )");
        sb.AppendLine("            {");

        foreach ( ImportProperty property in candidate.ImportProperties )
        {
            sb.Append("                case nameof(").Append(property.Name).AppendLine("):");
            AppendBinaryWrite(sb, property, "column.PostgresDbType", "                    ");
            sb.AppendLine("                    break;");
        }

        sb.AppendLine("                default:");
        sb.AppendLine("                    throw new InvalidOperationException($\"Unknown column: {column.PropertyName}\");");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void AppendBinaryWrite( StringBuilder sb, ImportProperty property, string dbTypeExpression, string indent )
    {
        if ( property.IsNullableValueType )
        {
            sb.Append(indent).Append("if ( ").Append(property.Name).Append(".HasValue ) { await importer.WriteAsync(").Append(property.NullableValueExpression).Append(", ").Append(dbTypeExpression).AppendLine(", token); }");
            sb.Append(indent).AppendLine("else { await importer.WriteNullAsync(token); }");
            return;
        }

        sb.Append(indent).Append("await importer.WriteAsync(").Append(property.WriteExpression).Append(", ").Append(dbTypeExpression).AppendLine(", token);");
    }

    private static bool TryGetImportProperty( GenerationCandidate candidate, string name, out ImportProperty property )
    {
        foreach ( ImportProperty candidateProperty in candidate.ImportProperties )
        {
            if ( !string.Equals(candidateProperty.Name, name, StringComparison.Ordinal) ) { continue; }

            property = candidateProperty;
            return true;
        }

        property = default;
        return false;
    }



    private sealed class GenerationCandidate : IEquatable<GenerationCandidate>
    {
        public string?                          Namespace                   { get; }
        public string                           TypeName                    { get; }
        public string                           HintName                    { get; }
        public string                           TypeDeclaration             { get; }
        public ImmutableArray<string>           DeclaredProperties          { get; }
        public ImmutableArray<ImportProperty>   ImportProperties            { get; }
        public ImmutableArray<EqualityProperty> EqualityProperties          { get; }
        public ImmutableArray<string>           ColumnOrder                 { get; }
        public bool                             GenerateCreate              { get; }
        public bool                             GenerateToDynamicParameters { get; }
        public ExportPlan?                      ExportPlan                  { get; }
        public bool                             GenerateBatchImport         { get; }
        public bool                             GenerateBinaryImport        { get; }
        public bool                             GenerateDataRowImport       { get; }
        // Equals / GetHashCode / CompareTo / comparison operators are always generated, so a valid candidate always produces output.
        public bool                             ShouldGenerate              => true;


        public GenerationCandidate( string?                          nameSpace,
                                    string                           typeName,
                                    string                           hintName,
                                    string                           typeDeclaration,
                                    ImmutableArray<string>           declaredProperties,
                                    ImmutableArray<ImportProperty>   importProperties,
                                    ImmutableArray<EqualityProperty> equalityProperties,
                                    ImmutableArray<string>           columnOrder,
                                    bool                             generateCreate,
                                    bool                             generateToDynamicParameters,
                                    ExportPlan?                      exportPlan,
                                    bool                             generateBatchImport,
                                    bool                             generateBinaryImport,
                                    bool                             generateDataRowImport )
        {
            Namespace                   = nameSpace;
            TypeName                    = typeName;
            HintName                    = hintName;
            TypeDeclaration             = typeDeclaration;
            DeclaredProperties          = declaredProperties;
            ImportProperties            = importProperties;
            EqualityProperties          = equalityProperties;
            ColumnOrder                 = columnOrder;
            GenerateCreate              = generateCreate;
            GenerateToDynamicParameters = generateToDynamicParameters;
            ExportPlan                  = exportPlan;
            GenerateBatchImport         = generateBatchImport;
            GenerateBinaryImport        = generateBinaryImport;
            GenerateDataRowImport       = generateDataRowImport;
        }
        public bool Equals( GenerationCandidate? other )
        {
            if ( other is null ) { return false; }

            if ( ReferenceEquals(this, other) ) { return true; }

            return string.Equals(HintName, other.HintName, StringComparison.InvariantCulture);
        }
        public override bool Equals( object? obj )                                                => ReferenceEquals(this, obj) || obj is GenerationCandidate other && Equals(other);
        public override int  GetHashCode()                                                        => StringComparer.InvariantCulture.GetHashCode(HintName);
        public static   bool operator ==( GenerationCandidate? left, GenerationCandidate? right ) => Equals(left, right);
        public static   bool operator !=( GenerationCandidate? left, GenerationCandidate? right ) => !Equals(left, right);
    }



    private readonly struct ImportProperty( string name, string writeExpression, bool isNullableValueType, string nullableValueExpression )
    {
        public string Name                    { get; } = name;
        public string WriteExpression         { get; } = writeExpression;
        public bool   IsNullableValueType     { get; } = isNullableValueType;
        public string NullableValueExpression { get; } = nullableValueExpression;
    }



    private readonly struct EqualityProperty( string name, string typeDisplay, bool isString, string? stringComparison )
    {
        public string  Name             { get; } = name;
        public string  TypeDisplay      { get; } = typeDisplay;
        public bool    IsString         { get; } = isString;
        public string? StringComparison { get; } = stringComparison;
    }



    private readonly struct DbKind
    {
        public int  Kind       { get; }
        public int  Size       { get; }
        public int  TypeOrder  { get; }
        public bool IsExcluded { get; }


        public DbKind( int kind, int size, int typeOrder )
        {
            Kind       = kind;
            Size       = size;
            TypeOrder  = typeOrder;
            IsExcluded = false;
        }
        private DbKind( bool isExcluded )
        {
            Kind       = 0;
            Size       = 0;
            TypeOrder  = 0;
            IsExcluded = isExcluded;
        }
        public static DbKind Excluded { get; } = new(true);
    }



    private readonly struct ColumnSort( IPropertySymbol property, int kind, int size, int typeOrder, bool isFixed, int dbSize )
    {
        public IPropertySymbol Property  { get; } = property;
        public string          Name      => Property.Name;
        public int             Kind      { get; } = kind;
        public int             Size      { get; } = size;
        public int             TypeOrder { get; } = typeOrder;
        public bool            IsFixed   { get; } = isFixed;
        public int             DbSize    { get; } = dbSize;
    }



    private enum BaseCtorKind
    {
        TableRecord,
        LastModifiedRecord,
        PairRecord,
        OwnedTableRecord,
        Mapping
    }



    private enum FrameworkRole
    {
        Leaf,
        DateCreated,
        LastModified,
        Id,
        UserId,
        AdditionalData,
        KeyId,
        ValueId
    }



    private enum ReadKind
    {
        Direct,
        EnumValue,
        RecordIdValue,
        NullSafeDirect,
        NullableDirect,
        NullableEnum,
        NullableRecordId,
        AdditionalData
    }



    private sealed class ExportColumn( string name, string localName, string typeDisplay, ReadKind readKind, string readType, FrameworkRole role, bool isSettable )
    {
        public string        Name        { get; } = name;
        public string        LocalName   { get; } = localName;
        public string        TypeDisplay { get; } = typeDisplay;
        public ReadKind      ReadKind    { get; } = readKind;
        public string        ReadType    { get; } = readType;
        public FrameworkRole Role        { get; } = role;
        public bool          IsSettable  { get; } = isSettable;
    }



    private sealed class ExportPlan( BaseCtorKind baseKind, ImmutableArray<ExportColumn> columns )
    {
        public BaseCtorKind                 BaseKind { get; } = baseKind;
        public ImmutableArray<ExportColumn> Columns  { get; } = columns;
    }
}
