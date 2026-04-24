using ZLinq;



namespace Jakar.Database.Generators;


[Generator]
public sealed class TableRecordGenerator : IIncrementalGenerator
{
    public void Initialize( IncrementalGeneratorInitializationContext context )
    {
        IncrementalValuesProvider<GenerationCandidate> candidates = context.SyntaxProvider.CreateSyntaxProvider(static ( node, _ ) => node is TypeDeclarationSyntax { Modifiers: var modifiers } && modifiers.Any(SyntaxKind.PartialKeyword), static ( ctx, _ ) => GetCandidate(ctx)).Where(static candidate => candidate is not null).Select(static ( candidate, _ ) => candidate!);

        context.RegisterSourceOutput(candidates.Collect(),
                                     static ( SourceProductionContext spc, ImmutableArray<GenerationCandidate> values ) =>
                                     {
                                         HashSet<GenerationCandidate> set = new();

                                         // ReSharper disable once ForCanBeConvertedToForeach
                                         for ( int i = 0; i < values.Length; i++ )
                                         {
                                             GenerationCandidate candidate = values[i];
                                             if ( !candidate.ShouldGenerate || !set.Add(candidate) ) { continue; }

                                             spc.AddSource($"{candidate.HintName}.g.cs", SourceText.From(Render(candidate), Encoding.Default));
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
        bool hasExport              = HasMethod(symbol, "Export",              static method => !method.IsStatic && method.Parameters.Length == 2 && IsNpgsqlBinaryExporter(method.Parameters[0].Type) && IsCancellationToken(method.Parameters[1].Type));
        bool hasBatchImport         = HasMethod(symbol, "Import",              static method => !method.IsStatic && method.Parameters.Length == 2 && IsNpgsqlBatchCommand(method.Parameters[0].Type)   && IsCancellationToken(method.Parameters[1].Type));
        bool hasBinaryImport        = HasMethod(symbol, "Import",              static method => !method.IsStatic && method.Parameters.Length == 4 && IsNpgsqlBinaryImporter(method.Parameters[0].Type) && IsString(method.Parameters[1].Type) && IsNpgsqlDbType(method.Parameters[2].Type) && IsCancellationToken(method.Parameters[3].Type));

        bool generateCreate              = hasReaderCtor                 && !hasCreate;
        bool generateToDynamicParameters = declaredProperties.Length > 0 && !hasToDynamicParameters;
        bool generateExport              = !hasExport;
        bool generateBatchImport         = !hasBatchImport;
        bool generateBinaryImport        = importProperties.Length > 0 && !hasBinaryImport;

        if ( !generateCreate && !generateToDynamicParameters && !generateExport && !generateBatchImport && !generateBinaryImport ) { return null; }

        return new GenerationCandidate(symbol.ContainingNamespace.IsGlobalNamespace
                                           ? null
                                           : symbol.ContainingNamespace.ToDisplayString(),
                                       symbol.Name,
                                       $"{GetHintPrefix(symbol)}.{symbol.Name}.TableRecord",
                                       GetTypeDeclaration(syntax),
                                       declaredProperties,
                                       importProperties,
                                       generateCreate,
                                       generateToDynamicParameters,
                                       generateExport,
                                       generateBatchImport,
                                       generateBinaryImport);
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

                results.Add(new ImportProperty(property.Name, GetWriteExpression(property), IsNullableValueType(property.Type)));
            }
        }

        return [..results];
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
    private static bool IsNpgsqlDbType( ITypeSymbol              type ) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::NpgsqlTypes.NpgsqlDbType";
    private static bool IsCancellationToken( ITypeSymbol         type ) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Threading.CancellationToken";
    private static bool IsString( ITypeSymbol                    type ) => type.SpecialType                                               == SpecialType.System_String;

    private static string GetWriteExpression( IPropertySymbol property )
    {
        if ( IsRecordId(property.Type) || IsUserRights(property.Type) ) { return $"{property.Name}.Value"; }

        return property.Name;
    }

    private static bool IsRecordId( ITypeSymbol   type ) => type is INamedTypeSymbol { Name: "RecordID", ContainingNamespace: { } ns } && ns.ToDisplayString() == "Jakar.Database";
    private static bool IsUserRights( ITypeSymbol type ) => type.Name == "UserRights";

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
        sb.AppendLine("using System.CodeDom.Compiler;");
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

        if ( candidate.GenerateExport )
        {
            if ( needsBlankLine ) { sb.AppendLine(); }

            sb.AppendLine("    public override ValueTask Export( NpgsqlBinaryExporter exporter, CancellationToken token ) => default;");
            needsBlankLine = true;
        }

        if ( candidate.GenerateBatchImport )
        {
            if ( needsBlankLine ) { sb.AppendLine(); }

            sb.AppendLine("    public override ValueTask Import( NpgsqlBatchCommand batch, CancellationToken token ) => default;");
            needsBlankLine = true;
        }

        if ( candidate.GenerateBinaryImport )
        {
            if ( needsBlankLine ) { sb.AppendLine(); }

            sb.AppendLine("    protected override async ValueTask Import( NpgsqlBinaryImporter importer, string propertyName, NpgsqlDbType postgresDbType, CancellationToken token )");
            sb.AppendLine("    {");
            sb.AppendLine("        switch ( propertyName )");
            sb.AppendLine("        {");

            foreach ( ImportProperty property in candidate.ImportProperties )
            {
                sb.Append("            case nameof(").Append(property.Name).AppendLine("):");

                if ( property.IsNullableValueType )
                {
                    sb.Append("                if ( ").Append(property.Name).AppendLine(".HasValue )");
                    sb.AppendLine("                {");
                    sb.Append("                    await importer.WriteAsync(").Append(property.Name).AppendLine(".Value, postgresDbType, token);");
                    sb.AppendLine("                }");
                    sb.AppendLine("                else");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    await importer.WriteNullAsync(token);");
                    sb.AppendLine("                }");
                    sb.AppendLine();
                    sb.AppendLine("                return;");
                    sb.AppendLine();
                    continue;
                }

                sb.Append("                await importer.WriteAsync(").Append(property.WriteExpression).AppendLine(", postgresDbType, token);");
                sb.AppendLine("                return;");
                sb.AppendLine();
            }

            sb.AppendLine("            default:");
            sb.AppendLine("                throw new InvalidOperationException($\"Unknown column: {propertyName}\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }



    private sealed class GenerationCandidate( string?                        ns,
                                              string                         typeName,
                                              string                         hintName,
                                              string                         typeDeclaration,
                                              ImmutableArray<string>         declaredProperties,
                                              ImmutableArray<ImportProperty> importProperties,
                                              bool                           generateCreate,
                                              bool                           generateToDynamicParameters,
                                              bool                           generateExport,
                                              bool                           generateBatchImport,
                                              bool                           generateBinaryImport ) : IEquatable<GenerationCandidate>
    {
        public string?                        Namespace                   { get; } = ns;
        public string                         TypeName                    { get; } = typeName;
        public string                         HintName                    { get; } = hintName;
        public string                         TypeDeclaration             { get; } = typeDeclaration;
        public ImmutableArray<string>         DeclaredProperties          { get; } = declaredProperties;
        public ImmutableArray<ImportProperty> ImportProperties            { get; } = importProperties;
        public bool                           GenerateCreate              { get; } = generateCreate;
        public bool                           GenerateToDynamicParameters { get; } = generateToDynamicParameters;
        public bool                           GenerateExport              { get; } = generateExport;
        public bool                           GenerateBatchImport         { get; } = generateBatchImport;
        public bool                           GenerateBinaryImport        { get; } = generateBinaryImport;
        public bool                           ShouldGenerate              => GenerateCreate || GenerateToDynamicParameters || GenerateExport || GenerateBatchImport || GenerateBinaryImport;


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



    private readonly struct ImportProperty( string name, string writeExpression, bool isNullableValueType )
    {
        public string Name                { get; } = name;
        public string WriteExpression     { get; } = writeExpression;
        public bool   IsNullableValueType { get; } = isNullableValueType;
    }
}
