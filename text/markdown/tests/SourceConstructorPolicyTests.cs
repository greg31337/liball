using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace markdown.Tests;

public class SourceConstructorPolicyTests
{
    [Test]
    public void SourceClasses_DeclareAtMostOneConstructor()
    {
        string sourceDirectory = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../src"));
        Assert.That(Directory.Exists(sourceDirectory), Is.True);

        List<string> violations = new List<string>();
        string[] files = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, file);
            if (relativePath.StartsWith("obj" + Path.DirectorySeparatorChar))
            {
                continue;
            }

            SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
            foreach (ClassDeclarationSyntax declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                int constructorCount = declaration.Members.OfType<ConstructorDeclarationSyntax>().Count();
                if (constructorCount > 1)
                {
                    violations.Add(relativePath + ":" + declaration.Identifier.Text);
                }
            }
        }

        Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
    }
}
