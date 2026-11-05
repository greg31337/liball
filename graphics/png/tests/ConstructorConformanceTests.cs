using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace png.Tests;

[TestFixture]
public class ConstructorConformanceTests
{
    [Test]
    public void LibraryClasses_DeclareAtMostOneConstructor()
    {
        string root = FindRepositoryRoot();
        string sourceDirectory = Path.Combine(root, "src");
        string[] sourceFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(Path.GetRelativePath(root, file)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        List<string> violations = new();
        int index = 0;
        while (index < sourceFiles.Length)
        {
            violations.AddRange(FindClassesWithMultipleConstructors(root, sourceFiles[index]));
            index += 1;
        }

        Assert.That(violations, Is.Empty);
    }

    static IEnumerable<string> FindClassesWithMultipleConstructors(string root, string sourcePath)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            File.ReadAllText(sourcePath),
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            path: sourcePath);

        SyntaxNode rootNode = syntaxTree.GetRoot();
        foreach (ClassDeclarationSyntax declaration in rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            int constructorCount = declaration.Members.OfType<ConstructorDeclarationSyntax>().Count();
            if (constructorCount > 1)
            {
                FileLinePositionSpan lineSpan = declaration.Identifier.GetLocation().GetLineSpan();
                yield return Path.GetRelativePath(root, sourcePath).Replace(Path.DirectorySeparatorChar, '/')
                    + ":"
                    + (lineSpan.StartLinePosition.Line + 1)
                    + " class "
                    + declaration.Identifier.ValueText
                    + " declares "
                    + constructorCount
                    + " constructors";
            }
        }
    }

    static bool IsBuildOutput(string relativePath)
    {
        string normalized = "/" + relativePath.Replace(Path.DirectorySeparatorChar, '/') + "/";
        return normalized.Contains("/bin/", StringComparison.Ordinal)
            || normalized.Contains("/obj/", StringComparison.Ordinal);
    }

    static string FindRepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "png.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
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
