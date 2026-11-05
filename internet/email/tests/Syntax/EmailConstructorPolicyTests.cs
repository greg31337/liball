using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace email.Tests;

public class EmailConstructorPolicyTests
{
    [Test]
    public void SourceClassesDeclareAtMostOneConstructor()
    {
        string root = FindRepositoryRoot();
        string sourceRoot = Path.Combine(root, "src");
        List<string> violations = new List<string>();

        foreach (string path in Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(path);
            CompilationUnitSyntax unit = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
            foreach (ClassDeclarationSyntax declaration in unit.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                int constructorCount = declaration.Members.OfType<ConstructorDeclarationSyntax>().Count();
                if (constructorCount > 1)
                {
                    string relativePath = Path.GetRelativePath(root, path);
                    violations.Add(relativePath + ": " + declaration.Identifier.Text + " declares " + constructorCount + " constructors");
                }
            }
        }

        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "email.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.That(directory, Is.Not.Null, "Could not locate the repository root from the test directory.");
        return directory!.FullName;
    }
}
