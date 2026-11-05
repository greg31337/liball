using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace tiff.Tests;

[TestFixture]
public class TiffConstructorConformanceTests
{
    [Test]
    public void SourceClasses_DeclareAtMostOneConstructor()
    {
        string root = FindRepositoryRoot();
        string sourceDirectory = Path.Combine(root, "src");
        string[] files = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);
        List<string> violations = new List<string>();

        foreach (string file in files)
        {
            if (IsBuildOutput(file))
            {
                continue;
            }

            CompilationUnitSyntax syntaxRoot = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file).GetCompilationUnitRoot();
            foreach (ClassDeclarationSyntax classDeclaration in syntaxRoot.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                int constructorCount = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().Count();
                if (constructorCount > 1)
                {
                    FileLinePositionSpan span = classDeclaration.Identifier.GetLocation().GetLineSpan();
                    LinePosition position = span.StartLinePosition;
                    string relativePath = Path.GetRelativePath(root, file);
                    violations.Add(relativePath + ":" + (position.Line + 1).ToString() + ": class " + classDeclaration.Identifier.ValueText + " declares " + constructorCount.ToString() + " constructors");
                }
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail("Source classes may declare at most one constructor:\n" + string.Join("\n", violations));
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
            if (File.Exists(Path.Combine(directory, "tiff.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
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
