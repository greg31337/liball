using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace units.Tests;

public class PublicApiPolicyTests
{
    static readonly string[] AllowedModifiers = new string[] { "static", "public", "internal", "const" };
    static readonly string[] AllowedXmlElements = new string[] { "summary", "remarks", "param", "returns", "exception", "example", "see", "seealso", "para", "c" };

    [Test]
    public void SourceDeclarations_UseExplicitApiVisibilityAndDocumentOnlyPublicSymbols()
    {
        string sourceDirectory = FindSourceDirectory();
        string[] paths = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);

        foreach (string path in paths)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
            CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();

            foreach (MemberDeclarationSyntax declaration in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                if (!IsPolicyDeclaration(declaration))
                {
                    continue;
                }

                SyntaxTokenList modifiers = GetModifiers(declaration);
                Assert.That(HasModifier(modifiers, "public") || HasModifier(modifiers, "internal"), Is.True, Location(declaration) + " must explicitly be public or internal.");
                AssertAllowedModifiers(modifiers, declaration);

                bool hasDocumentation = HasDocumentation(declaration);
                if (HasModifier(modifiers, "public"))
                {
                    Assert.That(hasDocumentation, Is.True, Location(declaration) + " is public and must have XML documentation.");
                    AssertAllowedXmlElements(declaration);
                }
                else
                {
                    Assert.That(hasDocumentation, Is.False, Location(declaration) + " is internal and must not have XML documentation.");
                }
            }

            foreach (EnumMemberDeclarationSyntax member in root.DescendantNodes().OfType<EnumMemberDeclarationSyntax>())
            {
                EnumDeclarationSyntax enumDeclaration = (EnumDeclarationSyntax)member.Parent!;
                bool publicEnum = HasModifier(enumDeclaration.Modifiers, "public");
                Assert.That(HasDocumentation(member), Is.EqualTo(publicEnum), Location(member) + " documentation must match its enum's visibility.");
                if (publicEnum)
                {
                    AssertAllowedXmlElements(member);
                }
            }
        }
    }

    [Test]
    public void SourceInterfaces_FollowSupportedInterfaceShape()
    {
        string sourceDirectory = FindSourceDirectory();
        string[] paths = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);

        foreach (string path in paths)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
            CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();

            foreach (InterfaceDeclarationSyntax declaration in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                string name = declaration.Identifier.ValueText;
                Assert.That(name.Length >= 2 && name[0] == 'I' && char.IsUpper(name[1]), Is.True, Location(declaration) + " must use an I followed by an uppercase letter.");
                Assert.That(declaration.BaseList, Is.Null, Location(declaration) + " must not inherit another interface.");

                foreach (MemberDeclarationSyntax member in declaration.Members)
                {
                    Assert.That(member, Is.TypeOf<MethodDeclarationSyntax>(), Location(member) + " interfaces may contain only methods.");
                    MethodDeclarationSyntax method = (MethodDeclarationSyntax)member;
                    Assert.That(method.TypeParameterList, Is.Null, Location(method) + " interface methods must not be generic.");
                    Assert.That(method.Body, Is.Null, Location(method) + " interface methods must not have default implementations.");
                    Assert.That(method.ExpressionBody, Is.Null, Location(method) + " interface methods must not have default implementations.");
                    Assert.That(HasModifier(method.Modifiers, "static"), Is.False, Location(method) + " interface methods must be instance methods.");
                }
            }
        }
    }

    [Test]
    public void SourceClasses_DeclareAtMostOneConstructor()
    {
        string sourceDirectory = FindSourceDirectory();
        string[] paths = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);

        foreach (string path in paths)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
            CompilationUnitSyntax root = (CompilationUnitSyntax)tree.GetRoot();

            foreach (ClassDeclarationSyntax declaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                int constructorCount = declaration.Members.OfType<ConstructorDeclarationSyntax>().Count();
                Assert.That(constructorCount, Is.LessThanOrEqualTo(1), Location(declaration) + " classes may declare at most one constructor.");
            }
        }
    }

    static bool IsPolicyDeclaration(MemberDeclarationSyntax declaration)
    {
        return declaration is ClassDeclarationSyntax
            || declaration is InterfaceDeclarationSyntax
            || declaration is EnumDeclarationSyntax
            || declaration is FieldDeclarationSyntax
            || declaration is ConstructorDeclarationSyntax
            || declaration is MethodDeclarationSyntax;
    }

    static SyntaxTokenList GetModifiers(MemberDeclarationSyntax declaration)
    {
        if (declaration is BaseTypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration.Modifiers;
        }

        if (declaration is BaseFieldDeclarationSyntax fieldDeclaration)
        {
            return fieldDeclaration.Modifiers;
        }

        if (declaration is BaseMethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Modifiers;
        }

        throw new InvalidOperationException("Unsupported declaration in API policy test.");
    }

    static bool HasModifier(SyntaxTokenList modifiers, string modifier)
    {
        return modifiers.Any(token => string.Equals(token.ValueText, modifier, StringComparison.Ordinal));
    }

    static void AssertAllowedModifiers(SyntaxTokenList modifiers, SyntaxNode declaration)
    {
        foreach (SyntaxToken modifier in modifiers)
        {
            Assert.That(AllowedModifiers, Does.Contain(modifier.ValueText), Location(declaration) + " uses unsupported modifier " + modifier.ValueText + ".");
        }
    }

    static bool HasDocumentation(SyntaxNode declaration)
    {
        return declaration.GetLeadingTrivia().Any(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
    }

    static void AssertAllowedXmlElements(SyntaxNode declaration)
    {
        IEnumerable<DocumentationCommentTriviaSyntax> comments = declaration.GetLeadingTrivia()
            .Select(trivia => trivia.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>();

        foreach (DocumentationCommentTriviaSyntax comment in comments)
        {
            foreach (XmlElementSyntax element in comment.DescendantNodes().OfType<XmlElementSyntax>())
            {
                string name = element.StartTag.Name.LocalName.ValueText;
                Assert.That(AllowedXmlElements, Does.Contain(name), Location(declaration) + " uses unsupported XML element " + name + ".");
            }

            foreach (XmlEmptyElementSyntax element in comment.DescendantNodes().OfType<XmlEmptyElementSyntax>())
            {
                string name = element.Name.LocalName.ValueText;
                Assert.That(AllowedXmlElements, Does.Contain(name), Location(declaration) + " uses unsupported XML element " + name + ".");
            }
        }
    }

    static string Location(SyntaxNode declaration)
    {
        FileLinePositionSpan span = declaration.GetLocation().GetLineSpan();
        return Path.GetFileName(span.Path) + ":" + (span.StartLinePosition.Line + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    static string FindSourceDirectory()
    {
        DirectoryInfo directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null)
        {
            string sourceDirectory = Path.Combine(directory.FullName, "src");
            if (Directory.Exists(sourceDirectory))
            {
                return sourceDirectory;
            }

            directory = directory.Parent!;
        }

        throw new DirectoryNotFoundException("Could not locate the src directory.");
    }
}
