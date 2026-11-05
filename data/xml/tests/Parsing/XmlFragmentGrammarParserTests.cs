using xml;

namespace xml.Tests;

[TestFixture]
public class XmlFragmentGrammarParserTests
{
    [Test]
    public void ParsesBalancedMixedContentWithMultipleTopLevelElements()
    {
        XmlNamespaceBinding defaultBinding = CreateBinding("", "urn:default");
        XmlNamespaceBinding prefixedBinding = CreateBinding("p", "urn:prefixed");
        XmlFragmentContext context = new XmlFragmentContext([defaultBinding, prefixedBinding], "https://example.test/base/", "en-GB", XmlSpaceMode.Preserve);
        XmlFragmentGrammarParser parser = CreateParser("text<a/><b><c/></b><!--comment--><?pi data?>tail", context);

        XmlFragmentStructure fragment = parser.ParseXmlFragmentStructure();

        Assert.That(fragment.InheritedContext, Is.SameAs(context));
        Assert.That(fragment.InheritedContext.NamespaceBindings, Is.EqualTo(new XmlNamespaceBinding[] { defaultBinding, prefixedBinding }));
        Assert.That(fragment.InheritedContext.BaseUri, Is.EqualTo("https://example.test/base/"));
        Assert.That(fragment.InheritedContext.Language, Is.EqualTo("en-GB"));
        Assert.That(fragment.InheritedContext.SpaceMode, Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(fragment.TopLevelItemCount, Is.EqualTo(6));
        Assert.That(fragment.TopLevelElementCount, Is.EqualTo(2));
        Assert.That(fragment.MaximumElementDepth, Is.EqualTo(2));
        Assert.That(fragment.HasTextDeclaration, Is.False);
        Assert.That(parser.CurrentXmlFragmentPhase(), Is.EqualTo(XmlFragmentPhase.Complete));
        Assert.That(parser.ElementParser.CurrentXmlElementDepth(), Is.Zero);
    }

    [Test]
    public void AcceptsEmptyAndNonElementFragments()
    {
        XmlFragmentStructure empty = CreateParser("", CreateEmptyContext()).ParseXmlFragmentStructure();
        XmlFragmentStructure content = CreateParser("text&amp;<![CDATA[raw < text]]><!--comment--><?pi?>", CreateEmptyContext()).ParseXmlFragmentStructure();

        Assert.That(empty.TopLevelItemCount, Is.Zero);
        Assert.That(empty.TopLevelElementCount, Is.Zero);
        Assert.That(empty.MaximumElementDepth, Is.Zero);
        Assert.That(content.TopLevelItemCount, Is.EqualTo(5));
        Assert.That(content.TopLevelElementCount, Is.Zero);
        Assert.That(content.MaximumElementDepth, Is.Zero);
    }

    [TestCase("<?xml encoding='UTF-8'?>text", XmlVersion.Xml10, "UTF-8")]
    [TestCase("<?xml version='1.1' encoding='utf-16'?>text", XmlVersion.Xml11, "utf-16")]
    public void RetainsInitialTextDeclarationMetadata(string text, XmlVersion expectedVersion, string expectedEncoding)
    {
        XmlFragmentGrammarParser parser = CreateParser(text, CreateEmptyContext());

        XmlFragmentStructure fragment = parser.ParseXmlFragmentStructure();

        Assert.That(fragment.HasTextDeclaration, Is.True);
        Assert.That(fragment.Version, Is.EqualTo(expectedVersion));
        Assert.That(fragment.EncodingName, Is.EqualTo(expectedEncoding));
        Assert.That(fragment.TopLevelItemCount, Is.EqualTo(1));
    }

    [TestCase("<a>")]
    [TestCase("<a><b/></a")]
    [TestCase("<a><b></b>")]
    [TestCase("<a/><b>")]
    public void RejectsFragmentsWithUnclosedElements(string text)
    {
        XmlFragmentGrammarParser parser = CreateParser(text, CreateEmptyContext());

        Assert.Throws<FormatException>(() => parser.ParseXmlFragmentStructure());
        Assert.That(parser.CurrentXmlFragmentPhase(), Is.EqualTo(XmlFragmentPhase.Content));
        Assert.That(parser.ElementParser.CurrentXmlElementDepth(), Is.GreaterThan(0));
    }

    [TestCase("</a>")]
    [TestCase("<a></b>")]
    [TestCase("<a><b></a></b>")]
    public void RejectsUnmatchedOrMismatchedEndTags(string text)
    {
        XmlFragmentGrammarParser parser = CreateParser(text, CreateEmptyContext());

        Assert.Throws<FormatException>(() => parser.ParseXmlFragmentStructure());
        Assert.That(parser.CurrentXmlFragmentPhase(), Is.EqualTo(XmlFragmentPhase.Content));
    }

    [TestCase("<!DOCTYPE a><a/>")]
    [TestCase("text<!ELEMENT a EMPTY>")]
    [TestCase("<a/><!DOCTYPE a>")]
    public void RejectsDocumentTypeAndOtherMarkupDeclarations(string text)
    {
        XmlFragmentGrammarParser parser = CreateParser(text, CreateEmptyContext());

        Assert.Throws<FormatException>(() => parser.ParseXmlFragmentStructure());
        Assert.That(parser.CurrentXmlFragmentPhase(), Is.EqualTo(XmlFragmentPhase.Content));
    }

    [TestCase("<?xml version='1.0'?>text")]
    [TestCase("text<?xml encoding='UTF-8'?>")]
    public void RejectsInvalidOrMisplacedTextDeclarations(string text)
    {
        XmlFragmentGrammarParser parser = CreateParser(text, CreateEmptyContext());

        Assert.Throws<FormatException>(() => parser.ParseXmlFragmentStructure());
    }

    [Test]
    public void FragmentAcceptanceDoesNotRelaxDocumentGrammar()
    {
        string text = "leading<a/><b/>trailing";
        XmlFragmentStructure fragment = CreateParser(text, CreateEmptyContext()).ParseXmlFragmentStructure();
        XmlScanner documentScanner = new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        XmlDocumentGrammarParser documentParser = new XmlDocumentGrammarParser(documentScanner, true, null);

        Assert.That(fragment.TopLevelElementCount, Is.EqualTo(2));
        Assert.Throws<FormatException>(() => documentParser.ParseXmlDocumentStructure());
        Assert.That(documentParser.CurrentXmlDocumentPhase(), Is.EqualTo(XmlDocumentPhase.Prolog));
    }

    [Test]
    public void FragmentContextSnapshotsTheNamespaceArray()
    {
        XmlNamespaceBinding original = CreateBinding("p", "urn:original");
        XmlNamespaceBinding replacement = CreateBinding("q", "urn:replacement");
        XmlNamespaceBinding[] bindings = [original];

        XmlFragmentContext context = new XmlFragmentContext(bindings, null, null, XmlSpaceMode.Default);
        bindings[0] = replacement;

        Assert.That(context.NamespaceBindings, Is.EqualTo(new XmlNamespaceBinding[] { original }));
    }

    [Test]
    public void RejectsInvalidInheritedContextInputs()
    {
        Assert.Throws<ArgumentException>(() => new XmlFragmentContext([null!], null, null, XmlSpaceMode.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() => new XmlFragmentContext([], null, null, (XmlSpaceMode)2));
    }

    [Test]
    public void ConstructorRequiresAnExternalParsedEntityScanner()
    {
        XmlScanner documentScanner = new XmlScanner("<a/>".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        Assert.Throws<ArgumentException>(() => new XmlFragmentGrammarParser(documentScanner, CreateEmptyContext(), true, null));
    }

    [Test]
    public void RejectsASecondParseAttempt()
    {
        XmlFragmentGrammarParser parser = CreateParser("<a/>", CreateEmptyContext());
        parser.ParseXmlFragmentStructure();

        Assert.Throws<InvalidOperationException>(() => parser.ParseXmlFragmentStructure());
    }

    private static XmlFragmentGrammarParser CreateParser(string text, XmlFragmentContext context)
    {
        XmlScanner scanner = new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.ExternalParsedEntity);
        return new XmlFragmentGrammarParser(scanner, context, true, null);
    }

    private static XmlFragmentContext CreateEmptyContext()
    {
        return new XmlFragmentContext([], null, null, XmlSpaceMode.Default);
    }

    private static XmlNamespaceBinding CreateBinding(string prefix, string namespaceUri)
    {
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = namespaceUri;
        return binding;
    }
}
