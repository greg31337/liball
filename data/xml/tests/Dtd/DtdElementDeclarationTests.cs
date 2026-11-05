using xml;

namespace xml.Tests;

[TestFixture]
public class DtdElementDeclarationTests
{
    [Test]
    public void RetainsEmptyAnyAndQualifiedElementDeclarations()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        DtdSubset subset = parser.LoadDtdSubsets("<!ELEMENT empty EMPTY><!ELEMENT p:any ANY>", null, null, "urn:document");

        Assert.That(subset.ElementDeclarations, Has.Length.EqualTo(2));
        XmlElementDeclaration empty = subset.ElementDeclarations[0];
        Assert.That(empty.Name!.QualifiedName, Is.EqualTo("empty"));
        Assert.That(empty.ContentModel, Is.EqualTo("EMPTY"));
        Assert.That(empty.ReferencedNames, Is.Empty);
        Assert.That(empty.IsEmpty, Is.True);
        Assert.That(empty.IsAny, Is.False);
        Assert.That(empty.IsMixed, Is.False);

        XmlElementDeclaration any = subset.ElementDeclarations[1];
        Assert.That(any.Name!.QualifiedName, Is.EqualTo("p:any"));
        Assert.That(any.Name.Prefix, Is.EqualTo("p"));
        Assert.That(any.Name.LocalName, Is.EqualTo("any"));
        Assert.That(any.ContentModel, Is.EqualTo("ANY"));
        Assert.That(any.IsAny, Is.True);
    }

    [TestCase("(#PCDATA)", "(#PCDATA)", new string[0])]
    [TestCase("( #PCDATA )*", "(#PCDATA)*", new string[0])]
    [TestCase("(#PCDATA | inline | emphasis )*", "(#PCDATA|inline|emphasis)*", new string[] { "inline", "emphasis" })]
    public void ParsesAndNormalizesMixedContent(string sourceModel, string normalizedModel, string[] referencedNames)
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        XmlElementDeclaration declaration = parser.LoadDtdSubsets("<!ELEMENT root " + sourceModel + ">", null, null, null).ElementDeclarations[0];

        Assert.That(declaration.ContentModel, Is.EqualTo(normalizedModel));
        Assert.That(declaration.ReferencedNames, Is.EqualTo(referencedNames));
        Assert.That(declaration.IsMixed, Is.True);
        Assert.That(declaration.IsEmpty, Is.False);
        Assert.That(declaration.IsAny, Is.False);
    }

    [Test]
    public void ParsesNestedSequencesChoicesAndEveryOccurrenceOperator()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        XmlElementDeclaration declaration = parser.LoadDtdSubsets("<!ELEMENT root ( head , (section | note)+ , tail? , extra* )>", null, null, null).ElementDeclarations[0];

        Assert.That(declaration.ContentModel, Is.EqualTo("(head,(section|note)+,tail?,extra*)"));
        Assert.That(declaration.ReferencedNames, Is.EqualTo(new string[] { "head", "section", "note", "tail", "extra" }));
        Assert.That(declaration.IsEmpty, Is.False);
        Assert.That(declaration.IsAny, Is.False);
        Assert.That(declaration.IsMixed, Is.False);
    }

    [Test]
    public void AllowsDeterministicRepeatedNamesAndRetainsEachReferencedNameOnce()
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        XmlElementDeclaration declaration = parser.LoadDtdSubsets("<!ELEMENT root (a,a,b,a)>", null, null, null).ElementDeclarations[0];

        Assert.That(declaration.ContentModel, Is.EqualTo("(a,a,b,a)"));
        Assert.That(declaration.ReferencedNames, Is.EqualTo(new string[] { "a", "b" }));
    }

    [TestCase("(a|a)")]
    [TestCase("(a*,a)")]
    [TestCase("(a?,a)")]
    [TestCase("((a,b)|(a,c))")]
    [TestCase("((a|b)*,a)")]
    public void RejectsNonDeterministicChildrenModels(string model)
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        FormatException failure = Assert.Throws<FormatException>(() => parser.LoadDtdSubsets("<!ELEMENT root " + model + ">", null, null, null))!;

        Assert.That(failure.Message, Does.Contain("not deterministic"));
    }

    [TestCase("<!ELEMENT root>")]
    [TestCase("<!ELEMENT root empty>")]
    [TestCase("<!ELEMENT root child>")]
    [TestCase("<!ELEMENT root EMPTY extra>")]
    [TestCase("<!ELEMENT root ()>")]
    [TestCase("<!ELEMENT root (a|)>")]
    [TestCase("<!ELEMENT root (a,b|c)>")]
    [TestCase("<!ELEMENT root (a ?)>")]
    [TestCase("<!ELEMENT root (# PCDATA)>")]
    [TestCase("<!ELEMENT root (#PCDATA|a)>")]
    [TestCase("<!ELEMENT root (#PCDATA|a|a)*>")]
    [TestCase("<!ELEMENT root ((#PCDATA))>")]
    [TestCase("<!ELEMENT root (#PCDATA)+>")]
    [TestCase("<!ELEMENTroot EMPTY>")]
    [TestCase("<! ELEMENT root EMPTY>")]
    public void RejectsMalformedElementDeclarations(string declaration)
    {
        DtdParser parser = new DtdParser(XmlVersion.Xml10, null, null);

        Assert.Throws<FormatException>(() => parser.LoadDtdSubsets(declaration, null, null, null));
    }

    [Test]
    public void ExpandsParameterEntitiesInExternalContentModels()
    {
        ElementDeclarationResolver resolver = new ElementDeclarationResolver();
        resolver.Text = "<!ENTITY % model '(first|second)*'><!ELEMENT root %model;>";
        DtdParser parser = new DtdParser(XmlVersion.Xml10, resolver, AllowedPolicy());

        DtdSubset subset = parser.LoadDtdSubsets(null, null, "root.dtd", "urn:document");

        Assert.That(subset.ElementDeclarations, Has.Length.EqualTo(1));
        Assert.That(subset.ElementDeclarations[0].ContentModel, Is.EqualTo("(first|second)*"));
        Assert.That(subset.ElementDeclarations[0].ReferencedNames, Is.EqualTo(new string[] { "first", "second" }));
    }

    [Test]
    public void AppliesFirstDeclarationPrecedenceAndPublishesRetainedModels()
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = "<!DOCTYPE root [<!ELEMENT root (first)><!ELEMENT root ANY><!ELEMENT first EMPTY>]><root><first/></root>";
        XmlParserOptions options = ParserOptions();

        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, null, null, null);

        Assert.That(document.DocumentType!.ElementDeclarations, Has.Length.EqualTo(2));
        Assert.That(document.DocumentType.ElementDeclarations![0].Name!.QualifiedName, Is.EqualTo("root"));
        Assert.That(document.DocumentType.ElementDeclarations[0].ContentModel, Is.EqualTo("(first)"));
        Assert.That(document.DocumentType.ElementDeclarations[1].Name!.QualifiedName, Is.EqualTo("first"));
    }

    private static XmlResolverPolicy AllowedPolicy()
    {
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        return policy;
    }

    private static XmlParserOptions ParserOptions()
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.None;
        return options;
    }

    private sealed class ElementDeclarationResolver : IXmlResourceResolver
    {
        public string Text = "";

        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            XmlInput input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = Text;
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = kind;
            resource.Access = XmlResourceAccess.Allowed;
            resource.Input = input;
            resource.AbsoluteUri = "urn:root.dtd";
            return resource;
        }
    }
}
