using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class PublicApiPolicyTests
{
    static string[] allowedModifiers = new string[] { "static", "public", "internal", "const" };
    static string[] allowedDocumentationElements = new string[]
    {
        "summary",
        "remarks",
        "param",
        "returns",
        "exception",
        "example",
        "see",
        "seealso",
        "para",
        "c"
    };

    [Test]
    public void ProductionDeclarations_UseExplicitApiVisibilityAndAllowedModifiers()
    {
        List<string> violations = new List<string>();

        foreach (SourceFile sourceFile in ReadProductionSourceFiles())
        {
            foreach (MemberDeclarationSyntax declaration in sourceFile.root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                SyntaxTokenList modifiers;
                if (!TryGetReviewedModifiers(declaration, out modifiers))
                {
                    continue;
                }

                int visibilityCount = modifiers.Count(token => token.IsKind(SyntaxKind.PublicKeyword) || token.IsKind(SyntaxKind.InternalKeyword));
                if (visibilityCount != 1)
                {
                    violations.Add(Location(sourceFile.path, declaration) + ": declaration must have exactly one public or internal modifier.");
                }

                foreach (SyntaxToken modifier in modifiers)
                {
                    if (!allowedModifiers.Contains(modifier.ValueText, StringComparer.Ordinal))
                    {
                        violations.Add(Location(sourceFile.path, declaration) + ": modifier '" + modifier.ValueText + "' is not allowed.");
                    }
                }
            }
        }

        AssertNoViolations(violations);
    }

    [Test]
    public void ProductionClasses_DeclareAtMostOneConstructor()
    {
        List<string> violations = new List<string>();

        foreach (SourceFile sourceFile in ReadProductionSourceFiles())
        {
            foreach (ClassDeclarationSyntax declaration in sourceFile.root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                int constructorCount = declaration.Members.OfType<ConstructorDeclarationSyntax>().Count();
                if (constructorCount > 1)
                {
                    violations.Add(
                        Location(sourceFile.path, declaration)
                        + ": class '"
                        + declaration.Identifier.ValueText
                        + "' declares "
                        + constructorCount
                        + " constructors; at most one is allowed.");
                }
            }
        }

        AssertNoViolations(violations);
    }

    [Test]
    public void ProductionPublicApi_HasPolicyCompliantXmlDocumentation()
    {
        List<string> violations = new List<string>();

        foreach (SourceFile sourceFile in ReadProductionSourceFiles())
        {
            foreach (MemberDeclarationSyntax declaration in sourceFile.root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                SyntaxTokenList modifiers;
                if (!TryGetReviewedModifiers(declaration, out modifiers) || !IsPublicApiDeclaration(declaration))
                {
                    continue;
                }

                CheckDocumentation(sourceFile.path, declaration, violations);
            }

            foreach (EnumMemberDeclarationSyntax enumMember in sourceFile.root.DescendantNodes().OfType<EnumMemberDeclarationSyntax>())
            {
                EnumDeclarationSyntax containingEnum = enumMember.Ancestors().OfType<EnumDeclarationSyntax>().First();
                if (containingEnum.Modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    CheckDocumentation(sourceFile.path, enumMember, violations);
                }
            }
        }

        AssertNoViolations(violations);
    }

    [Test]
    public void ProductionInterfaces_FollowRestrictedInterfaceSyntax()
    {
        List<string> violations = new List<string>();

        foreach (SourceFile sourceFile in ReadProductionSourceFiles())
        {
            foreach (InterfaceDeclarationSyntax declaration in sourceFile.root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                string name = declaration.Identifier.ValueText;
                if (name.Length < 2 || name[0] != 'I' || !char.IsUpper(name[1]))
                {
                    violations.Add(Location(sourceFile.path, declaration) + ": interface names must start with 'I' followed by an uppercase letter.");
                }

                if (declaration.BaseList != null)
                {
                    violations.Add(Location(sourceFile.path, declaration) + ": interface inheritance is not allowed.");
                }

                if (declaration.TypeParameterList != null)
                {
                    violations.Add(Location(sourceFile.path, declaration) + ": generic interfaces are not allowed.");
                }

                foreach (MemberDeclarationSyntax member in declaration.Members)
                {
                    if (!(member is MethodDeclarationSyntax))
                    {
                        violations.Add(Location(sourceFile.path, member) + ": interfaces may contain instance methods only.");
                        continue;
                    }

                    MethodDeclarationSyntax method = (MethodDeclarationSyntax)member;
                    if (method.Modifiers.Any(SyntaxKind.StaticKeyword))
                    {
                        violations.Add(Location(sourceFile.path, method) + ": static interface methods are not allowed.");
                    }

                    if (method.TypeParameterList != null)
                    {
                        violations.Add(Location(sourceFile.path, method) + ": generic interface methods are not allowed.");
                    }

                    if (method.Body != null || method.ExpressionBody != null)
                    {
                        violations.Add(Location(sourceFile.path, method) + ": default interface implementations are not allowed.");
                    }
                }
            }
        }

        AssertNoViolations(violations);
    }

    [Test]
    public void ProductionPublicApi_DoesNotExposeImplementationEngines()
    {
        List<string> violations = new List<string>();

        foreach (SourceFile sourceFile in ReadProductionSourceFiles())
        {
            foreach (BaseTypeDeclarationSyntax declaration in sourceFile.root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                if (!declaration.Modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    continue;
                }

                string name = declaration.Identifier.ValueText;
                if (name.EndsWith("Parser", StringComparison.Ordinal)
                    || name.EndsWith("Comparer", StringComparison.Ordinal)
                    || name.EndsWith("Resolver", StringComparison.Ordinal)
                    || name.EndsWith("Validator", StringComparison.Ordinal)
                    || name.EndsWith("Scanner", StringComparison.Ordinal)
                    || name.EndsWith("Dispatcher", StringComparison.Ordinal))
                {
                    violations.Add(Location(sourceFile.path, declaration) + ": implementation type '" + name + "' must be internal and reached through a facade or shared API.");
                }
            }
        }

        AssertNoViolations(violations);
    }

    static bool TryGetReviewedModifiers(MemberDeclarationSyntax declaration, out SyntaxTokenList modifiers)
    {
        if (declaration is BaseTypeDeclarationSyntax typeDeclaration)
        {
            modifiers = typeDeclaration.Modifiers;
            return true;
        }

        if (declaration is MethodDeclarationSyntax method)
        {
            modifiers = method.Modifiers;
            return true;
        }

        if (declaration is ConstructorDeclarationSyntax constructor)
        {
            modifiers = constructor.Modifiers;
            return true;
        }

        if (declaration is FieldDeclarationSyntax field)
        {
            modifiers = field.Modifiers;
            return true;
        }

        modifiers = default;
        return false;
    }

    static bool IsPublicApiDeclaration(MemberDeclarationSyntax declaration)
    {
        if (!declaration.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return false;
        }

        foreach (BaseTypeDeclarationSyntax containingType in declaration.Ancestors().OfType<BaseTypeDeclarationSyntax>())
        {
            if (!containingType.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                return false;
            }
        }

        return true;
    }

    static void CheckDocumentation(string path, SyntaxNode declaration, List<string> violations)
    {
        SyntaxTrivia documentation = declaration.GetLeadingTrivia().FirstOrDefault(
            trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
        if (documentation == default)
        {
            violations.Add(Location(path, declaration) + ": public declaration is missing XML documentation.");
            return;
        }

        MatchCollection tags = Regex.Matches(documentation.ToFullString(), @"</?([A-Za-z]+)\b");
        foreach (Match tag in tags)
        {
            string elementName = tag.Groups[1].Value;
            if (!allowedDocumentationElements.Contains(elementName, StringComparer.Ordinal))
            {
                violations.Add(Location(path, declaration) + ": XML element <" + elementName + "> is not allowed.");
            }
        }
    }

    static List<SourceFile> ReadProductionSourceFiles()
    {
        string root = FindRepositoryRoot();
        List<SourceFile> files = new List<SourceFile>();
        foreach (string path in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            string normalized = path.Replace(Path.DirectorySeparatorChar, '/');
            if (normalized.Contains("/bin/") || normalized.Contains("/obj/"))
            {
                continue;
            }

            SyntaxNode syntaxRoot = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
            files.Add(new SourceFile(Path.GetRelativePath(root, path), syntaxRoot));
        }

        return files;
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "version.slnx")))
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

    static string Location(string path, SyntaxNode declaration)
    {
        return path + ":" + (declaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    static void AssertNoViolations(List<string> violations)
    {
        if (violations.Count > 0)
        {
            Assert.Fail(string.Join("\n", violations));
        }
    }
}

public class SourceFile
{
    public string path;
    public SyntaxNode root;

    public SourceFile(string pathValue, SyntaxNode rootValue)
    {
        path = pathValue;
        root = rootValue;
    }
}
