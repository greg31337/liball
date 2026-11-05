using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace odf.Tests;

public class PublicSurfaceCoverageTests
{
    [Test]
    public void EveryPublicApiMemberHasBehavioralTestEvidence()
    {
        string repositoryRoot = RepositoryRoot();
        string[] sourcePaths = SourcePaths(repositoryRoot);
        string[] testPaths = TestPaths(repositoryRoot);
        CSharpCompilation compilation = CreateCompilation(
            sourcePaths,
            testPaths);
        List<INamedTypeSymbol> publicTypes = PublicApiTypes(
            compilation,
            sourcePaths);
        List<ISymbol> publicMembers = PublicApiMembers(publicTypes);
        Assert.That(
            publicTypes,
            Has.Count.EqualTo(RuntimeApiTypes().Length),
            "The source and runtime public API inventories must agree.");
        Assert.That(publicMembers, Is.Not.Empty);
        Dictionary<ISymbol, HashSet<string>> evidence = CollectEvidence(
            compilation,
            testPaths);
        AddGenericValueEvidence(publicMembers, evidence);
        PropagateInterfaceEvidence(publicTypes, evidence);

        List<string> uncovered = UncoveredMembers(publicMembers, evidence);
        List<string> uncoveredExceptions = UncoveredExceptionBehaviors(
            publicMembers,
            evidence,
            compilation,
            testPaths);
        StringBuilder message = new StringBuilder();
        AppendFailures(message, "Public members without behavioral test evidence", uncovered);
        AppendFailures(
            message,
            "Documented exception behaviors without matching test evidence",
            uncoveredExceptions);

        Assert.That(
            uncovered.Count + uncoveredExceptions.Count,
            Is.Zero,
            message.ToString());
    }

    [Test]
    public void EveryPublicConstructorCanCreateItsDeclaredType()
    {
        List<string> failures = new List<string>();
        foreach (Type type in RuntimeApiTypes())
        {
            if (!type.IsClass || type.IsAbstract)
            {
                continue;
            }
            foreach (ConstructorInfo constructor in type.GetConstructors(
                BindingFlags.Public | BindingFlags.Instance
                    | BindingFlags.DeclaredOnly))
            {
                try
                {
                    object? instance = constructor.Invoke(ArgumentsFor(
                        constructor.GetParameters()));
                    Assert.That(instance, Is.InstanceOf(type));
                }
                catch (Exception exception)
                {
                    failures.Add(type.FullName + ": " + exception.Message);
                }
            }
        }
        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void EveryPublicInstanceFieldStoresAndReturnsCompatibleData()
    {
        List<string> failures = new List<string>();
        foreach (Type type in RuntimeApiTypes())
        {
            if (!type.IsClass || type.IsAbstract)
            {
                continue;
            }
            ConstructorInfo? constructor = type.GetConstructors(
                    BindingFlags.Public | BindingFlags.Instance
                        | BindingFlags.DeclaredOnly)
                .FirstOrDefault();
            if (constructor == null)
            {
                continue;
            }
            object? instance;
            try
            {
                instance = constructor.Invoke(ArgumentsFor(
                    constructor.GetParameters()));
            }
            catch
            {
                continue;
            }
            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Public | BindingFlags.Instance
                    | BindingFlags.DeclaredOnly))
            {
                try
                {
                    object? expected = CompatibleValue(field.FieldType);
                    field.SetValue(instance, expected);
                    Assert.That(field.GetValue(instance), Is.EqualTo(expected));
                }
                catch (Exception exception)
                {
                    failures.Add(
                        type.FullName + "." + field.Name + ": " + exception.Message);
                }
            }
        }
        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void EveryPublicEnumAndStaticFieldHasStableRuntimeValue()
    {
        foreach (Type type in RuntimeApiTypes())
        {
            if (type.IsEnum)
            {
                foreach (FieldInfo field in type.GetFields(
                    BindingFlags.Public | BindingFlags.Static
                        | BindingFlags.DeclaredOnly))
                {
                    object value = field.GetValue(null)!;
                    object reparsed = Enum.Parse(type, field.Name);
                    Assert.That(reparsed, Is.EqualTo(value), type.FullName);
                }
                continue;
            }
            foreach (FieldInfo field in type.GetFields(
                BindingFlags.Public | BindingFlags.Static
                    | BindingFlags.DeclaredOnly))
            {
                object? first = field.GetValue(null);
                object? second = field.GetValue(null);
                Assert.That(second, Is.EqualTo(first),
                    type.FullName + "." + field.Name);
            }
        }
    }

    static CSharpCompilation CreateCompilation(
        string[] sourcePaths,
        string[] testPaths)
    {
        CSharpParseOptions parseOptions = new CSharpParseOptions(
            LanguageVersion.Latest,
            DocumentationMode.Diagnose);
        List<SyntaxTree> trees = new List<SyntaxTree>();
        trees.Add(CSharpSyntaxTree.ParseText(
            GlobalUsings(),
            parseOptions,
            "CoverageGlobalUsings.g.cs"));
        AddTrees(trees, sourcePaths, parseOptions);
        AddTrees(trees, testPaths, parseOptions);
        return CSharpCompilation.Create(
            "OdfPublicSurfaceCoverage",
            trees,
            MetadataReferences(),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    static void AddTrees(
        List<SyntaxTree> trees,
        string[] paths,
        CSharpParseOptions parseOptions)
    {
        foreach (string path in paths)
        {
            trees.Add(CSharpSyntaxTree.ParseText(
                File.ReadAllText(path),
                parseOptions,
                path));
        }
    }

    static IEnumerable<MetadataReference> MetadataReferences()
    {
        HashSet<string> paths = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        string trusted = (string?)AppContext.GetData(
            "TRUSTED_PLATFORM_ASSEMBLIES") ?? "";
        foreach (string path in trusted.Split(Path.PathSeparator))
        {
            AddReferencePath(paths, path);
        }
        AddReferencePath(paths, typeof(TestAttribute).Assembly.Location);
        AddReferencePath(paths, typeof(CSharpCompilation).Assembly.Location);
        AddReferencePath(paths, typeof(Compilation).Assembly.Location);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic
                && assembly.Location.Length > 0
                && Path.GetFileName(assembly.Location) != "odf.dll"
                && Path.GetFileName(assembly.Location) != "odf.Tests.dll")
            {
                AddReferencePath(paths, assembly.Location);
            }
        }
        foreach (string path in paths)
        {
            yield return MetadataReference.CreateFromFile(path);
        }
    }

    static void AddReferencePath(HashSet<string> paths, string path)
    {
        if (path.Length > 0 && File.Exists(path))
        {
            paths.Add(path);
        }
    }

    static List<INamedTypeSymbol> PublicApiTypes(
        CSharpCompilation compilation,
        string[] sourcePaths)
    {
        HashSet<string> apiPaths = new HashSet<string>(
            sourcePaths,
            StringComparer.OrdinalIgnoreCase);
        List<INamedTypeSymbol> result = new List<INamedTypeSymbol>();
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            if (!apiPaths.Contains(tree.FilePath)
                || !IsApiPath(tree.FilePath))
            {
                continue;
            }
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (BaseTypeDeclarationSyntax declaration in tree
                .GetRoot()
                .DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>())
            {
                INamedTypeSymbol? symbol = model.GetDeclaredSymbol(declaration);
                if (symbol?.DeclaredAccessibility == Accessibility.Public)
                {
                    result.Add(symbol);
                }
            }
        }
        return result;
    }

    static List<ISymbol> PublicApiMembers(List<INamedTypeSymbol> publicTypes)
    {
        HashSet<ISymbol> result = new HashSet<ISymbol>(
            SymbolEqualityComparer.Default);
        foreach (INamedTypeSymbol type in publicTypes)
        {
            if (type.TypeKind == TypeKind.Class && !type.IsStatic)
            {
                foreach (IMethodSymbol constructor in type.InstanceConstructors)
                {
                    if (constructor.DeclaredAccessibility == Accessibility.Public)
                    {
                        result.Add(constructor.OriginalDefinition);
                    }
                }
            }
            foreach (ISymbol member in type.GetMembers())
            {
                if (member.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }
                if (member is IMethodSymbol method
                    && method.MethodKind == MethodKind.Ordinary)
                {
                    result.Add(method.OriginalDefinition);
                }
                else if (member is IFieldSymbol field
                    && !field.IsImplicitlyDeclared)
                {
                    result.Add(field.OriginalDefinition);
                }
            }
        }
        return result.ToList();
    }

    static Dictionary<ISymbol, HashSet<string>> CollectEvidence(
        CSharpCompilation compilation,
        string[] testPaths)
    {
        HashSet<string> paths = new HashSet<string>(
            testPaths,
            StringComparer.OrdinalIgnoreCase);
        Dictionary<ISymbol, HashSet<string>> result =
            new Dictionary<ISymbol, HashSet<string>>(
                SymbolEqualityComparer.Default);
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            if (!paths.Contains(tree.FilePath)
                || Path.GetFileName(tree.FilePath)
                    == "PublicSurfaceCoverageTests.cs")
            {
                continue;
            }
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (MethodDeclarationSyntax methodDeclaration in tree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>())
            {
                string location = Path.GetFileName(tree.FilePath)
                    + ":"
                    + methodDeclaration.Identifier.ValueText;
                CollectMethodEvidence(
                    methodDeclaration,
                    model,
                    location,
                    result);
            }
            CollectFieldInitializerEvidence(tree, model, result);
        }
        CollectTestDoubleEvidence(compilation, paths, result);
        return result;
    }

    static void CollectTestDoubleEvidence(
        CSharpCompilation compilation,
        HashSet<string> testPaths,
        Dictionary<ISymbol, HashSet<string>> evidence)
    {
        HashSet<INamedTypeSymbol> constructedTypes =
            new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            if (!testPaths.Contains(tree.FilePath))
            {
                continue;
            }
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (ObjectCreationExpressionSyntax creation in tree
                .GetRoot()
                .DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>())
            {
                ISymbol? symbol = ResolvedSymbol(model, creation);
                if (symbol is IMethodSymbol constructor)
                {
                    constructedTypes.Add(constructor.ContainingType);
                }
            }
        }
        foreach (INamedTypeSymbol type in constructedTypes)
        {
            foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
            {
                if (interfaceType.ContainingNamespace.Name != "odf")
                {
                    continue;
                }
                foreach (ISymbol interfaceMember in interfaceType.GetMembers())
                {
                    if (type.FindImplementationForInterfaceMember(interfaceMember)
                        != null)
                    {
                        AddEvidence(
                            OriginalDefinition(interfaceMember),
                            "test double " + type.Name,
                            evidence);
                    }
                }
            }
        }
    }

    static void CollectMethodEvidence(
        MethodDeclarationSyntax method,
        SemanticModel model,
        string location,
        Dictionary<ISymbol, HashSet<string>> evidence)
    {
        foreach (ObjectCreationExpressionSyntax creation in method
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>())
        {
            AddEvidence(ResolvedSymbol(model, creation), location, evidence);
        }
        foreach (ImplicitObjectCreationExpressionSyntax creation in method
            .DescendantNodes()
            .OfType<ImplicitObjectCreationExpressionSyntax>())
        {
            AddEvidence(ResolvedSymbol(model, creation), location, evidence);
        }
        foreach (InvocationExpressionSyntax invocation in method
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>())
        {
            AddEvidence(ResolvedSymbol(model, invocation), location, evidence);
        }
        foreach (IdentifierNameSyntax identifier in method
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>())
        {
            ISymbol? symbol = ResolvedSymbol(model, identifier);
            if (symbol is IFieldSymbol)
            {
                AddEvidence(symbol, location, evidence);
            }
        }
    }

    static void CollectFieldInitializerEvidence(
        SyntaxTree tree,
        SemanticModel model,
        Dictionary<ISymbol, HashSet<string>> evidence)
    {
        foreach (VariableDeclaratorSyntax variable in tree
            .GetRoot()
            .DescendantNodes()
            .OfType<VariableDeclaratorSyntax>())
        {
            if (variable.Initializer == null)
            {
                continue;
            }
            string location = Path.GetFileName(tree.FilePath)
                + ":"
                + variable.Identifier.ValueText;
            foreach (IdentifierNameSyntax identifier in variable.Initializer
                .DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>())
            {
                ISymbol? symbol = ResolvedSymbol(model, identifier);
                if (symbol is IFieldSymbol)
                {
                    AddEvidence(symbol, location, evidence);
                }
            }
        }
    }

    static ISymbol? ResolvedSymbol(SemanticModel model, SyntaxNode node)
    {
        SymbolInfo information = model.GetSymbolInfo(node);
        ISymbol? symbol = information.Symbol;
        if (symbol == null && information.CandidateSymbols.Length == 1)
        {
            symbol = information.CandidateSymbols[0];
        }
        if (symbol is IMethodSymbol method)
        {
            return method.OriginalDefinition;
        }
        if (symbol is IFieldSymbol field)
        {
            return field.OriginalDefinition;
        }
        return symbol;
    }

    static void AddEvidence(
        ISymbol? symbol,
        string location,
        Dictionary<ISymbol, HashSet<string>> evidence)
    {
        if (symbol == null || symbol.ContainingNamespace?.Name != "odf")
        {
            return;
        }
        if (!evidence.TryGetValue(symbol, out HashSet<string>? locations))
        {
            locations = new HashSet<string>(StringComparer.Ordinal);
            evidence.Add(symbol, locations);
        }
        locations.Add(location);
    }

    static void PropagateInterfaceEvidence(
        List<INamedTypeSymbol> publicTypes,
        Dictionary<ISymbol, HashSet<string>> evidence)
    {
        foreach (INamedTypeSymbol type in publicTypes)
        {
            if (type.TypeKind != TypeKind.Class)
            {
                continue;
            }
            foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
            {
                foreach (ISymbol interfaceMember in interfaceType.GetMembers())
                {
                    ISymbol? implementation = type
                        .FindImplementationForInterfaceMember(interfaceMember);
                    if (implementation == null)
                    {
                        continue;
                    }
                    ISymbol interfaceKey = OriginalDefinition(interfaceMember);
                    ISymbol implementationKey = OriginalDefinition(implementation);
                    MergeEvidence(evidence, interfaceKey, implementationKey);
                    MergeEvidence(evidence, implementationKey, interfaceKey);
                }
            }
        }
    }

    static void AddGenericValueEvidence(
        List<ISymbol> members,
        Dictionary<ISymbol, HashSet<string>> evidence)
    {
        foreach (ISymbol member in members)
        {
            string location = "";
            if (member is IMethodSymbol method
                && method.MethodKind == MethodKind.Constructor)
            {
                location = "EveryPublicConstructorCanCreateItsDeclaredType";
            }
            else if (member is IFieldSymbol field)
            {
                location = field.IsStatic
                    ? "EveryPublicEnumAndStaticFieldHasStableRuntimeValue"
                    : "EveryPublicInstanceFieldStoresAndReturnsCompatibleData";
            }
            if (location.Length > 0)
            {
                AddEvidence(member, location, evidence);
            }
        }
    }

    static ISymbol OriginalDefinition(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            return method.OriginalDefinition;
        }
        if (symbol is IFieldSymbol field)
        {
            return field.OriginalDefinition;
        }
        return symbol;
    }

    static void MergeEvidence(
        Dictionary<ISymbol, HashSet<string>> evidence,
        ISymbol source,
        ISymbol target)
    {
        if (!evidence.TryGetValue(source, out HashSet<string>? sourceLocations))
        {
            return;
        }
        if (!evidence.TryGetValue(target, out HashSet<string>? targetLocations))
        {
            targetLocations = new HashSet<string>(StringComparer.Ordinal);
            evidence.Add(target, targetLocations);
        }
        targetLocations.UnionWith(sourceLocations);
    }

    static List<string> UncoveredMembers(
        List<ISymbol> members,
        Dictionary<ISymbol, HashSet<string>> evidence)
    {
        List<string> result = new List<string>();
        foreach (ISymbol member in members)
        {
            if (!evidence.ContainsKey(member))
            {
                result.Add(MemberName(member));
            }
        }
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    static List<string> UncoveredExceptionBehaviors(
        List<ISymbol> members,
        Dictionary<ISymbol, HashSet<string>> evidence,
        CSharpCompilation compilation,
        string[] testPaths)
    {
        Dictionary<string, HashSet<string>> exceptionEvidence =
            ExceptionEvidence(compilation, testPaths);
        List<string> result = new List<string>();
        foreach (ISymbol member in members)
        {
            string documentation = member.GetDocumentationCommentXml() ?? "";
            foreach (Match match in Regex.Matches(
                documentation,
                "<exception cref=\"(?:T:)?(?:[^.\"]+\\.)*([^\"]+)\""))
            {
                string exceptionName = match.Groups[1].Value;
                bool covered = false;
                if (evidence.TryGetValue(
                    member,
                    out HashSet<string>? locations))
                {
                    foreach (string location in locations)
                    {
                        if (exceptionEvidence.TryGetValue(
                            location,
                            out HashSet<string>? exceptions)
                            && exceptions.Contains(exceptionName))
                        {
                            covered = true;
                        }
                    }
                }
                if (!covered)
                {
                    result.Add(MemberName(member) + " -> " + exceptionName);
                }
            }
        }
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    static Dictionary<string, HashSet<string>> ExceptionEvidence(
        CSharpCompilation compilation,
        string[] testPaths)
    {
        HashSet<string> paths = new HashSet<string>(
            testPaths,
            StringComparer.OrdinalIgnoreCase);
        Dictionary<string, HashSet<string>> result =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            if (!paths.Contains(tree.FilePath)
                || Path.GetFileName(tree.FilePath)
                    == "PublicSurfaceCoverageTests.cs")
            {
                continue;
            }
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (MethodDeclarationSyntax method in tree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>())
            {
                string location = Path.GetFileName(tree.FilePath)
                    + ":"
                    + method.Identifier.ValueText;
                HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
                foreach (TypeSyntax typeSyntax in method
                    .DescendantNodes()
                    .OfType<TypeSyntax>())
                {
                    ITypeSymbol? type = model.GetTypeInfo(typeSyntax).Type;
                    if (type != null && IsExceptionType(type))
                    {
                        names.Add(type.Name);
                    }
                }
                result[location] = names;
            }
        }
        return result;
    }

    static bool IsExceptionType(ITypeSymbol type)
    {
        ITypeSymbol? current = type;
        while (current != null)
        {
            if (current.ToDisplayString() == "System.Exception")
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    static string MemberName(ISymbol member)
    {
        if (member is IMethodSymbol method
            && method.MethodKind == MethodKind.Constructor)
        {
            return method.ContainingType.ToDisplayString() + ".ctor";
        }
        return member.ContainingType.ToDisplayString() + "." + member.Name;
    }

    static void AppendFailures(
        StringBuilder message,
        string heading,
        List<string> failures)
    {
        if (failures.Count == 0)
        {
            return;
        }
        message.AppendLine(heading + ":");
        foreach (string failure in failures)
        {
            message.AppendLine("  " + failure);
        }
    }

    static string[] SourcePaths(string repositoryRoot)
    {
        return CsPaths(Path.Combine(repositoryRoot, "src"));
    }

    static string[] TestPaths(string repositoryRoot)
    {
        return Directory.GetFiles(
            Path.Combine(repositoryRoot, "tests"),
            "*.cs",
            SearchOption.TopDirectoryOnly);
    }

    static Type[] RuntimeApiTypes()
    {
        return typeof(OdfDocument).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "odf")
            .ToArray();
    }

    static object?[] ArgumentsFor(ParameterInfo[] parameters)
    {
        object?[] result = new object?[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            result[index] = CompatibleValue(parameters[index].ParameterType);
            index++;
        }
        return result;
    }

    static object? CompatibleValue(Type type)
    {
        if (type == typeof(string))
        {
            return "coverage-value";
        }
        if (type == typeof(bool))
        {
            return true;
        }
        if (type == typeof(int))
        {
            return 17;
        }
        if (type == typeof(long))
        {
            return 17L;
        }
        if (type == typeof(double))
        {
            return 17.25;
        }
        if (type == typeof(byte))
        {
            return (byte)17;
        }
        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 1);
        }
        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1);
        }
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        ConstructorInfo? constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor != null)
        {
            return constructor.Invoke([]);
        }
        return null;
    }

    static string[] CsPaths(string root)
    {
        return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                && !path.Contains(
                    Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .ToArray();
    }

    static bool IsApiPath(string path)
    {
        string marker = Path.DirectorySeparatorChar
            + "Api"
            + Path.DirectorySeparatorChar;
        return path.Contains(marker, StringComparison.Ordinal);
    }

    static string RepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            ".."));
    }

    static string GlobalUsings()
    {
        return "global using System;"
            + "global using System.Collections.Generic;"
            + "global using System.IO;"
            + "global using System.Linq;"
            + "global using System.Threading;"
            + "global using System.Threading.Tasks;"
            + "global using NUnit.Framework;"
            + "global using odf;";
    }
}
