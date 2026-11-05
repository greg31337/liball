using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPublicApiDocumentationTests
{
    [Test]
    public void ProductionClasses_DeclareAtMostOneConstructor()
    {
        string sourceDirectory = Path.Combine(FindRepositoryRoot(), "src");
        List<string> errors = new List<string>();

        foreach (string path in Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories).Order())
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
            CompilationUnitSyntax rootNode = tree.GetCompilationUnitRoot();

            foreach (ClassDeclarationSyntax classDeclaration in rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                int constructorCount = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().Count();

                if (constructorCount > 1)
                {
                    errors.Add(GetLocation(path, classDeclaration) + ": class '" + classDeclaration.Identifier.ValueText + "' declares " + constructorCount.ToString() + " constructors");
                }
            }
        }

        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
    }

    [Test]
    public void ProductionSymbols_FollowVisibilityAndDocumentationPolicy()
    {
        string root = FindRepositoryRoot();
        string sourceDirectory = Path.Combine(root, "src");
        string[] allowedModifiers = new string[] { "static", "public", "internal", "const" };
        string[] allowedElements = new string[] { "summary", "remarks", "param", "returns", "exception", "example", "see", "seealso", "para", "c" };
        List<string> errors = new List<string>();

        foreach (string path in Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories).Order())
        {
            string source = File.ReadAllText(path);
            CSharpParseOptions options = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, options, path);
            CompilationUnitSyntax rootNode = tree.GetCompilationUnitRoot();

            foreach (MemberDeclarationSyntax member in rootNode.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                SyntaxTokenList modifiers = GetModifiers(member);
                CheckModifiers(path, member, modifiers, allowedModifiers, errors);

                bool isPublic = modifiers.Any(token => token.IsKind(SyntaxKind.PublicKeyword));
                bool isInternal = modifiers.Any(token => token.IsKind(SyntaxKind.InternalKeyword));

                if (isPublic)
                {
                    CheckDocumentation(path, member, true, allowedElements, errors);
                }
                else if (isInternal)
                {
                    CheckDocumentation(path, member, false, allowedElements, errors);
                }
            }

            foreach (EnumDeclarationSyntax enumDeclaration in rootNode.DescendantNodes().OfType<EnumDeclarationSyntax>())
            {
                bool publicEnum = enumDeclaration.Modifiers.Any(token => token.IsKind(SyntaxKind.PublicKeyword));

                foreach (EnumMemberDeclarationSyntax enumMember in enumDeclaration.Members)
                {
                    CheckDocumentation(path, enumMember, publicEnum, allowedElements, errors);
                }
            }
        }

        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
    }

    static SyntaxTokenList GetModifiers(MemberDeclarationSyntax member)
    {
        if (member is BaseTypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration.Modifiers;
        }

        if (member is BaseMethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.Modifiers;
        }

        if (member is FieldDeclarationSyntax fieldDeclaration)
        {
            return fieldDeclaration.Modifiers;
        }

        return new SyntaxTokenList();
    }

    static void CheckModifiers(string path, SyntaxNode declaration, SyntaxTokenList modifiers, string[] allowedModifiers, List<string> errors)
    {
        foreach (SyntaxToken modifier in modifiers)
        {
            if (!allowedModifiers.Contains(modifier.ValueText))
            {
                errors.Add(GetLocation(path, declaration) + ": modifier '" + modifier.ValueText + "' is not allowed");
            }
        }
    }

    static void CheckDocumentation(string path, SyntaxNode declaration, bool required, string[] allowedElements, List<string> errors)
    {
        DocumentationCommentTriviaSyntax? documentation = declaration.GetLeadingTrivia()
            .Select(trivia => trivia.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        if (required && documentation == null)
        {
            errors.Add(GetLocation(path, declaration) + ": public symbol is missing XML documentation");
            return;
        }

        if (!required && documentation != null)
        {
            errors.Add(GetLocation(path, declaration) + ": internal symbol must not have XML documentation");
            return;
        }

        if (documentation == null)
        {
            return;
        }

        foreach (XmlNodeSyntax xmlNode in documentation.DescendantNodes().OfType<XmlNodeSyntax>())
        {
            string name = GetXmlElementName(xmlNode);
            if (name != "" && !allowedElements.Contains(name))
            {
                errors.Add(GetLocation(path, declaration) + ": XML element <" + name + "> is not allowed");
            }
        }
    }

    static string GetXmlElementName(XmlNodeSyntax node)
    {
        if (node is XmlElementSyntax element)
        {
            return element.StartTag.Name.LocalName.ValueText;
        }

        if (node is XmlEmptyElementSyntax emptyElement)
        {
            return emptyElement.Name.LocalName.ValueText;
        }

        return "";
    }

    static string GetLocation(string path, SyntaxNode declaration)
    {
        FileLinePositionSpan span = declaration.GetLocation().GetLineSpan();
        return path + ":" + (span.StartLinePosition.Line + 1).ToString();
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "docx.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
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
