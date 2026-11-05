using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace tiff.Tests;

[TestFixture]
public class TiffNullableAnnotationConformanceTests
{
    [Test]
    public void RepositorySources_UseOnlySupportedNullableTypeForms()
    {
        string root = FindRepositoryRoot();
        string[] files = GetSourceFiles(root);
        List<SyntaxTree> trees = new List<SyntaxTree>();
        int index = 0;
        while (index < files.Length)
        {
            string file = files[index];
            trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file));
            index = index + 1;
        }

        CSharpCompilation compilation = CreateCompilation(trees);
        List<string> violations = FindViolations(root, compilation, trees);

        if (violations.Count > 0)
        {
            Assert.Fail("Nullable annotations may only be applied to reference types:\n" + string.Join("\n", violations));
        }
    }

    [TestCase("class Example { string? Value; }")]
    [TestCase("class Example { object? Value; }")]
    [TestCase("class Example { List<string>? Value; }")]
    public void NullableReferenceTypes_AreAllowed(string source)
    {
        Assert.That(CountViolations(source), Is.EqualTo(0));
    }

    [TestCase("class Example { int? Value; }")]
    [TestCase("class Example { DayOfWeek? Value; }")]
    [TestCase("class Example<T> { T? Value; }")]
    [TestCase("class Example { Nullable<int> Value; }")]
    [TestCase("class Example { System.Nullable<int> Value; }")]
    public void NullableValueAndTypeParameterForms_AreRejected(string source)
    {
        Assert.That(CountViolations(source), Is.EqualTo(1));
    }

    static int CountViolations(string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "example.cs");
        List<SyntaxTree> trees = new List<SyntaxTree>();
        trees.Add(tree);
        CSharpCompilation compilation = CreateCompilation(trees);
        return FindViolations("", compilation, trees).Count;
    }

    static List<string> FindViolations(string root, CSharpCompilation compilation, List<SyntaxTree> trees)
    {
        List<string> violations = new List<string>();
        int treeIndex = 0;
        while (treeIndex < trees.Count)
        {
            SyntaxTree tree = trees[treeIndex];
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            CompilationUnitSyntax syntaxRoot = tree.GetCompilationUnitRoot();

            foreach (NullableTypeSyntax nullableType in syntaxRoot.DescendantNodes().OfType<NullableTypeSyntax>())
            {
                ITypeSymbol? annotatedType = semanticModel.GetTypeInfo(nullableType.ElementType).Type;
                if (annotatedType == null || annotatedType.IsValueType || annotatedType.TypeKind == TypeKind.TypeParameter || annotatedType.TypeKind == TypeKind.Error)
                {
                    AddViolation(root, nullableType, violations);
                }
            }

            foreach (GenericNameSyntax genericName in syntaxRoot.DescendantNodes().OfType<GenericNameSyntax>())
            {
                if (genericName.Identifier.ValueText == "Nullable" && genericName.TypeArgumentList.Arguments.Count == 1)
                {
                    AddViolation(root, genericName, violations);
                }
            }

            treeIndex = treeIndex + 1;
        }

        return violations;
    }

    static void AddViolation(string root, TypeSyntax type, List<string> violations)
    {
        FileLinePositionSpan span = type.GetLocation().GetLineSpan();
        LinePosition start = span.StartLinePosition;
        string path = span.Path;
        if (!string.IsNullOrEmpty(root) && !string.IsNullOrEmpty(path))
        {
            path = Path.GetRelativePath(root, path);
        }

        violations.Add(path + ":" + (start.Line + 1).ToString() + ":" + (start.Character + 1).ToString() + ": unsupported nullable type `" + type.ToString() + "`");
    }

    static CSharpCompilation CreateCompilation(List<SyntaxTree> trees)
    {
        List<SyntaxTree> compilationTrees = new List<SyntaxTree>(trees);
        compilationTrees.Add(CSharpSyntaxTree.ParseText(
            "global using System; global using System.Collections.Generic; global using System.IO; global using System.Linq; global using System.Threading; global using System.Threading.Tasks;",
            path: "ImplicitUsings.g.cs"));
        List<MetadataReference> references = new List<MetadataReference>();
        HashSet<string> locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            string location = assembly.Location;
            if (!string.IsNullOrEmpty(location) && locations.Add(location))
            {
                references.Add(MetadataReference.CreateFromFile(location));
            }
        }

        CSharpCompilationOptions options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);
        return CSharpCompilation.Create("NullableAnnotationConformance", compilationTrees, references, options);
    }

    static string[] GetSourceFiles(string root)
    {
        List<string> files = new List<string>();
        AddSourceFiles(Path.Combine(root, "src"), files);
        AddSourceFiles(Path.Combine(root, "tests"), files);
        return files.ToArray();
    }

    static void AddSourceFiles(string directory, List<string> files)
    {
        foreach (string file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            if (!IsBuildOutput(file))
            {
                files.Add(file);
            }
        }
    }

    static bool IsBuildOutput(string path)
    {
        string normalized = path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
        return normalized.Contains("/bin/") || normalized.Contains("/obj/");
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "tiff.slnx")) && Directory.Exists(Path.Combine(directory, "tests")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }
}
