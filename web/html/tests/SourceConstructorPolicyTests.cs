using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UniversalHtml.Tests;

[TestFixture]
public class SourceConstructorPolicyTests
{
    [Test]
    public void ProductionClassesDeclareAtMostOneConstructor()
    {
        string sourceDirectory = Path.Combine(FindRepositoryRoot(), "src");
        string[] paths = Directory.GetFiles(sourceDirectory, "*.cs",
            SearchOption.AllDirectories);

        foreach (string path in paths)
        {
            string source = File.ReadAllText(path);
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source)
                .GetCompilationUnitRoot();
            foreach (ClassDeclarationSyntax declaration in
                root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                int count = declaration.Members
                    .OfType<ConstructorDeclarationSyntax>().Count();
                Assert.That(count, Is.LessThanOrEqualTo(1),
                    $"{path}: class {declaration.Identifier.Text} declares "
                        + $"{count} constructors");
            }
        }
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src",
                    "html.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the repository root from the test output directory.");
    }
}
