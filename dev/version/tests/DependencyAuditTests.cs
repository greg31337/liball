using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class DependencyAuditTests
{
    [Test]
    public void ProductionProjects_DoNotReferenceExternalPackages()
    {
        string root = FindRepositoryRoot();
        string sourceDirectory = Path.Combine(root, "src");
        string[] projectFiles = Directory.GetFiles(sourceDirectory, "*.csproj", SearchOption.AllDirectories);
        List<string> violations = new List<string>();

        foreach (string projectFile in projectFiles)
        {
            AddPackageReferenceViolations(root, projectFile, violations);
        }

        if (violations.Count > 0)
        {
            Assert.Fail("Production project dependency violations:\n" + string.Join("\n", violations));
        }
    }

    [Test]
    public void ProductionSource_DoesNotReferenceExternalEcosystemLibrariesOrLinq()
    {
        string root = FindRepositoryRoot();
        string sourceDirectory = Path.Combine(root, "src");
        string[] sourceFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);
        List<string> violations = new List<string>();

        foreach (string sourceFile in sourceFiles)
        {
            if (IsGeneratedOrBuildOutput(sourceFile))
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(root, sourceFile);
            string source = File.ReadAllText(sourceFile);
            violations.AddRange(DependencyAuditScanner.FindViolations(source, relativePath));
        }

        if (violations.Count > 0)
        {
            Assert.Fail("Production source dependency violations:\n" + string.Join("\n", violations));
        }
    }

    [Test]
    public void DependencyAuditScanner_ReportsRepresentativeForbiddenReferences()
    {
        string sample = string.Join(
            "\n",
            new string[]
            {
                "using System.Linq;",
                "using NuGet.Versioning;",
                "namespace version;",
                "public class BadDependencySample",
                "{",
                "    public void Run()",
                "    {",
                "        System.Linq.Enumerable.Empty<string>();",
                "        NuGet.Versioning.VersionRange.Parse(\"[1.0.0,)\");",
                "    }",
                "}"
            });

        List<string> violations = DependencyAuditScanner.FindViolations(sample, "BadDependencySample.cs");
        string messages = string.Join("\n", violations);

        Assert.That(messages, Does.Contain("LINQ namespace references are not allowed"));
        Assert.That(messages, Does.Contain("NuGet client library references are not allowed"));
        Assert.That(messages, Does.Contain("LINQ calls are not allowed"));
    }

    static void AddPackageReferenceViolations(string root, string projectFile, List<string> violations)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.Load(projectFile);

        System.Xml.XmlNodeList packageReferences = document.GetElementsByTagName("PackageReference");
        int index = 0;

        while (index < packageReferences.Count)
        {
            System.Xml.XmlNode? packageReference = packageReferences[index];
            if (packageReference != null)
            {
                string includeValue = GetAttributeValue(packageReference, "Include");
                if (includeValue.Length == 0)
                {
                    includeValue = GetAttributeValue(packageReference, "Update");
                }

                violations.Add(
                    Path.GetRelativePath(root, projectFile)
                    + ": PackageReference '"
                    + includeValue
                    + "' is not allowed in production source projects.");
            }

            index = index + 1;
        }
    }

    static string GetAttributeValue(System.Xml.XmlNode node, string attributeName)
    {
        if (node.Attributes == null)
        {
            return "";
        }

        System.Xml.XmlAttribute? attribute = node.Attributes[attributeName];
        if (attribute == null)
        {
            return "";
        }

        return attribute.Value;
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "version.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
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

    static bool IsGeneratedOrBuildOutput(string file)
    {
        string normalized = file.Replace(Path.DirectorySeparatorChar, '/');
        if (normalized.Contains("/bin/"))
        {
            return true;
        }

        if (normalized.Contains("/obj/"))
        {
            return true;
        }

        if (normalized.EndsWith(".g.cs") || normalized.EndsWith(".AssemblyInfo.cs") || normalized.Contains(".AssemblyAttributes.cs"))
        {
            return true;
        }

        return false;
    }
}

public static class DependencyAuditScanner
{
    public static List<string> FindViolations(string source, string filePath)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        SyntaxNode root = tree.GetRoot();
        List<string> violations = new List<string>();

        foreach (UsingDirectiveSyntax usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            string namespaceName = "";
            if (usingDirective.Name != null)
            {
                namespaceName = usingDirective.Name.ToString();
            }

            AddNamespaceViolation(tree, usingDirective, filePath, namespaceName, violations);
        }

        foreach (QualifiedNameSyntax qualifiedName in root.DescendantNodes().OfType<QualifiedNameSyntax>())
        {
            AddNamespaceViolation(tree, qualifiedName, filePath, qualifiedName.ToString(), violations);
        }

        foreach (MemberAccessExpressionSyntax memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            AddNamespaceViolation(tree, memberAccess, filePath, memberAccess.ToString(), violations);
        }

        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            AddLinqInvocationViolation(tree, invocation, filePath, violations);
        }

        return violations;
    }

    static void AddNamespaceViolation(
        SyntaxTree tree,
        SyntaxNode node,
        string filePath,
        string namespaceName,
        List<string> violations)
    {
        if (namespaceName == "System.Linq" || namespaceName.StartsWith("System.Linq."))
        {
            AddViolation(tree, node, filePath, "LINQ namespace references are not allowed in production source.", violations);
            return;
        }

        if (IsForbiddenExternalNamespace(namespaceName))
        {
            AddViolation(tree, node, filePath, GetExternalNamespaceMessage(namespaceName), violations);
        }
    }

    static void AddLinqInvocationViolation(SyntaxTree tree, InvocationExpressionSyntax invocation, string filePath, List<string> violations)
    {
        string invocationName = GetInvocationName(invocation);
        if (IsForbiddenLinqInvocation(invocationName))
        {
            AddViolation(tree, invocation, filePath, "LINQ calls are not allowed in production source.", violations);
            return;
        }

        string invocationText = invocation.Expression.ToString();
        if (invocationText.StartsWith("System.Linq.Enumerable.") || invocationText.StartsWith("Enumerable."))
        {
            AddViolation(tree, invocation, filePath, "LINQ calls are not allowed in production source.", violations);
        }
    }

    static string GetInvocationName(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.Text;
        }

        if (invocation.Expression is IdentifierNameSyntax identifierName)
        {
            return identifierName.Identifier.Text;
        }

        return "";
    }

    static bool IsForbiddenLinqInvocation(string invocationName)
    {
        return invocationName == "Aggregate"
            || invocationName == "All"
            || invocationName == "Any"
            || invocationName == "First"
            || invocationName == "FirstOrDefault"
            || invocationName == "GroupBy"
            || invocationName == "OrderBy"
            || invocationName == "Select"
            || invocationName == "Single"
            || invocationName == "SingleOrDefault"
            || invocationName == "Skip"
            || invocationName == "Sum"
            || invocationName == "Take"
            || invocationName == "Where";
    }

    static bool IsForbiddenExternalNamespace(string namespaceName)
    {
        return namespaceName == "NuGet"
            || namespaceName.StartsWith("NuGet.")
            || namespaceName == "Maven"
            || namespaceName.StartsWith("Maven.")
            || namespaceName == "Org.Apache.Maven"
            || namespaceName.StartsWith("Org.Apache.Maven.")
            || namespaceName == "Cargo"
            || namespaceName.StartsWith("Cargo.")
            || namespaceName == "RubyGems"
            || namespaceName.StartsWith("RubyGems.")
            || namespaceName == "Gem"
            || namespaceName.StartsWith("Gem.")
            || namespaceName == "Python.Runtime"
            || namespaceName.StartsWith("Python.Runtime.")
            || namespaceName == "Packaging"
            || namespaceName.StartsWith("Packaging.");
    }

    static string GetExternalNamespaceMessage(string namespaceName)
    {
        if (namespaceName == "NuGet" || namespaceName.StartsWith("NuGet."))
        {
            return "NuGet client library references are not allowed in production source.";
        }

        if (namespaceName == "Maven" || namespaceName.StartsWith("Maven.") || namespaceName == "Org.Apache.Maven" || namespaceName.StartsWith("Org.Apache.Maven."))
        {
            return "Maven resolver library references are not allowed in production source.";
        }

        if (namespaceName == "Cargo" || namespaceName.StartsWith("Cargo."))
        {
            return "Cargo library references are not allowed in production source.";
        }

        if (namespaceName == "RubyGems" || namespaceName.StartsWith("RubyGems.") || namespaceName == "Gem" || namespaceName.StartsWith("Gem."))
        {
            return "RubyGems library references are not allowed in production source.";
        }

        return "Python packaging library references are not allowed in production source.";
    }

    static void AddViolation(SyntaxTree tree, SyntaxNode node, string filePath, string message, List<string> violations)
    {
        FileLinePositionSpan lineSpan = tree.GetLineSpan(node.Span);
        int lineNumber = lineSpan.StartLinePosition.Line + 1;
        int columnNumber = lineSpan.StartLinePosition.Character + 1;

        violations.Add(
            filePath
            + ":"
            + lineNumber.ToString()
            + ":"
            + columnNumber.ToString()
            + ": "
            + message);
    }
}
