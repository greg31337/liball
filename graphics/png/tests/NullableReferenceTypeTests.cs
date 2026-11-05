using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System.Xml.Linq;

namespace png.Tests;

[TestFixture]
public class NullableReferenceTypeTests
{
    [Test]
    public void ProjectFiles_EnableNullableReferenceTypes()
    {
        string root = FindRepositoryRoot();
        string[] projectFiles = new string[]
        {
            Path.Combine(root, "src", "png.csproj"),
            Path.Combine(root, "tests", "png.Tests.csproj")
        };

        int index = 0;
        while (index < projectFiles.Length)
        {
            XDocument project = XDocument.Load(projectFiles[index]);
            XElement? nullable = project.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "Nullable");

            Assert.That(nullable, Is.Not.Null, projectFiles[index]);
            Assert.That(nullable?.Value, Is.EqualTo("enable").IgnoreCase, projectFiles[index]);
            index += 1;
        }
    }

    [Test]
    public void LibrarySources_DoNotSuppressNullableWarnings()
    {
        string root = FindRepositoryRoot();
        string[] sourceFiles = Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(Path.GetRelativePath(root, file)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        List<string> violations = new();
        int index = 0;
        while (index < sourceFiles.Length)
        {
            violations.AddRange(FindNullForgivingOperators(root, sourceFiles[index]));
            index += 1;
        }

        Assert.That(violations, Is.Empty);
    }

    static IEnumerable<string> FindNullForgivingOperators(string root, string sourcePath)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            File.ReadAllText(sourcePath),
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            path: sourcePath);

        SyntaxNode rootNode = syntaxTree.GetRoot();

        foreach (PostfixUnaryExpressionSyntax expression in rootNode.DescendantNodes().OfType<PostfixUnaryExpressionSyntax>())
        {
            if (expression.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            {
                FileLinePositionSpan lineSpan = expression.GetLocation().GetLineSpan();
                yield return Path.GetRelativePath(root, sourcePath).Replace(Path.DirectorySeparatorChar, '/')
                    + ":"
                    + (lineSpan.StartLinePosition.Line + 1)
                    + " suppresses a nullable warning with "
                    + expression;
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
