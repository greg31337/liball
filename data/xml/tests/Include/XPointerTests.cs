using xml;

namespace xml.Tests;

[TestFixture]
public class XPointerTests
{
    [Test]
    public void ParsesShorthandAndSchemePartsWithFrameworkEscaping()
    {
        XPointerParser parser = new XPointerParser();

        XPointerExpression shorthand = parser.ParseXPointer("chapter", XmlVersion.Xml10);
        XPointerExpression schemes = parser.ParseXPointer("other(a^(b^)^^) p:nested(call(one)) element(/1)", XmlVersion.Xml10);

        Assert.That(shorthand.IsValid, Is.True);
        Assert.That(shorthand.Shorthand, Is.EqualTo("chapter"));
        Assert.That(shorthand.Parts, Is.Empty);
        Assert.That(schemes.IsValid, Is.True);
        Assert.That(schemes.Parts, Has.Length.EqualTo(3));
        Assert.That(schemes.Parts[0].SchemeName, Is.EqualTo("other"));
        Assert.That(schemes.Parts[0].SchemeData, Is.EqualTo("a(b)^"));
        Assert.That(schemes.Parts[1].Prefix, Is.EqualTo("p"));
        Assert.That(schemes.Parts[1].LocalName, Is.EqualTo("nested"));
        Assert.That(schemes.Parts[1].SchemeData, Is.EqualTo("call(one)"));
        Assert.That(schemes.Parts[2].LocalName, Is.EqualTo("element"));
        Assert.That(schemes.Parts[2].SchemeData, Is.EqualTo("/1"));
    }

    [TestCase("")]
    [TestCase(" element(/1)")]
    [TestCase("element (/1)")]
    [TestCase("element(/1) ")]
    [TestCase("element(/1")]
    [TestCase("unknown(a^b)")]
    [TestCase("bad:name:again(value)")]
    public void ReportsFrameworkSyntaxErrorsWithoutPartialEvaluation(string pointer)
    {
        XmlDocument document = ParseDocument("<root/>", Options());

        XPointerEvaluation result = new XPointerEvaluator().EvaluateXPointer(document, pointer);

        Assert.That(result.IsError, Is.True);
        Assert.That(result.Node, Is.Null);
        Assert.That(result.ErrorMessage, Is.Not.Empty);
    }

    [Test]
    public void EvaluatesAbsoluteAndIdRelativeElementChildSequences()
    {
        XmlDocument document = ParseDocument("<root>text<a/><!--comment--><section xml:id='anchor'><one/><two><target/></two></section></root>", Options());
        XmlNode root = document.DocumentElement!;
        XmlNode section = root.Children![3];
        XmlNode target = section.Children![1].Children![0];
        XPointerEvaluator evaluator = new XPointerEvaluator();

        XPointerEvaluation absolute = evaluator.EvaluateXPointer(document, "element(/1/2/2/1)");
        XPointerEvaluation relative = evaluator.EvaluateXPointer(document, "element(anchor/2/1)");

        Assert.That(absolute.IsError, Is.False);
        Assert.That(absolute.Node, Is.SameAs(target));
        Assert.That(relative.Node, Is.SameAs(target));
    }

    [Test]
    public void EvaluatesTopLevelElementPositionsForFragmentResources()
    {
        XmlDocument fragment = ParseFragment("text<a/><!--comment--><b><inside/></b>", Options());
        XmlNode second = fragment.DocumentNode!.Children![3];

        XPointerEvaluation result = new XPointerEvaluator().EvaluateXPointer(fragment, "element(/2)");

        Assert.That(result.IsError, Is.False);
        Assert.That(result.Node, Is.SameAs(second));
    }

    [Test]
    public void FallsBackAcrossUnsupportedMalformedAndUnmatchedPointerParts()
    {
        XmlDocument document = ParseDocument("<root><a/><b/></root>", Options());
        XmlNode expected = document.DocumentElement!.Children![1];

        XPointerEvaluation result = new XPointerEvaluator().EvaluateXPointer(document, "unsupported(call^(x^)) element(/0) element(missing/1) element(/1/2)");

        Assert.That(result.IsError, Is.False);
        Assert.That(result.Node, Is.SameAs(expected));
    }

    [Test]
    public void StopsAfterTheFirstPointerPartThatIdentifiesAnElement()
    {
        XmlDocument document = ParseDocument("<root><first/><second/></root>", Options());

        XPointerEvaluation result = new XPointerEvaluator().EvaluateXPointer(document, "element(/1/1) xmlns(late=urn:late) element(/1/2)");

        Assert.That(result.Node, Is.SameAs(document.DocumentElement!.Children![0]));
        Assert.That(result.NamespaceBindings, Has.Length.EqualTo(1));
        Assert.That(result.NamespaceBindings[0].Prefix, Is.EqualTo("xml"));
    }

    [Test]
    public void AppliesXmlnsBindingsOverridesEscapesAndReservedNameConstraints()
    {
        XmlDocument document = ParseDocument("<root/>", Options());
        string pointer = "xmlns(p=urn:first)xmlns(p=http://example.test/^(v1^)) xmlns(q = urn:q) p:unknown(value) " +
            "xmlns(xml=urn:wrong) xmlns(other=" + XmlNamespaces.Xml + ") xmlns(xmlns=urn:x) xmlns(n=" + XmlNamespaces.Xmlns + ") " +
            "xmlns(1bad=urn:ignored) element(/1)";

        XPointerEvaluation result = new XPointerEvaluator().EvaluateXPointer(document, pointer);

        Assert.That(result.IsError, Is.False);
        Assert.That(result.Node, Is.SameAs(document.DocumentElement));
        Assert.That(result.NamespaceBindings, Has.Length.EqualTo(3));
        AssertBinding(result.NamespaceBindings[0], "xml", XmlNamespaces.Xml);
        AssertBinding(result.NamespaceBindings[1], "p", "http://example.test/(v1)");
        AssertBinding(result.NamespaceBindings[2], "q", "urn:q");
    }

    [Test]
    public void UsesXmlDtdAndSchemaDeterminedIdentifiersForShorthandPointers()
    {
        XmlParserOptions options = Options();
        options.DtdProcessing = XmlDtdProcessing.Parse;
        XmlDocument document = ParseDocument("<!DOCTYPE root [<!ELEMENT root ANY><!ELEMENT item EMPTY><!ELEMENT holder ANY><!ELEMENT identifier (#PCDATA)><!ATTLIST item key ID #IMPLIED>]><root><item key='dtd-id'/><item xml:id=' xml-id '/><holder><identifier>schema-id</identifier></holder></root>", options);
        XmlNode root = document.DocumentElement!;
        XmlNode holder = root.Children![2];
        holder.Children![0].TypeInfo = new XmlSchemaTypeInfo { IsId = true, NormalizedValue = "schema-id" };
        XPointerEvaluator evaluator = new XPointerEvaluator();

        Assert.That(evaluator.EvaluateXPointer(document, "dtd-id").Node, Is.SameAs(root.Children[0]));
        Assert.That(evaluator.EvaluateXPointer(document, "xml-id").Node, Is.SameAs(root.Children[1]));
        Assert.That(evaluator.EvaluateXPointer(document, "schema-id").Node, Is.SameAs(holder));

        XPointerEvaluation missing = evaluator.EvaluateXPointer(document, "missing");
        Assert.That(missing.IsError, Is.True);
        Assert.That(missing.Node, Is.Null);
    }

    [TestCase("element()")]
    [TestCase("element(/0)")]
    [TestCase("element(/01)")]
    [TestCase("element(/1/)")]
    [TestCase("element(/2147483648)")]
    [TestCase("element(not an ncname)")]
    [TestCase("unknown(value)")]
    [TestCase("xmlns(p=urn:only)")]
    public void ReportsAnErrorWhenNoSchemePartIdentifiesAnElement(string pointer)
    {
        XmlDocument document = ParseDocument("<root/>", Options());

        XPointerEvaluation result = new XPointerEvaluator().EvaluateXPointer(document, pointer);

        Assert.That(result.IsError, Is.True);
        Assert.That(result.Node, Is.Null);
    }

    private static void AssertBinding(XmlNamespaceBinding binding, string prefix, string namespaceUri)
    {
        Assert.That(binding.Prefix, Is.EqualTo(prefix));
        Assert.That(binding.NamespaceUri, Is.EqualTo(namespaceUri));
    }

    private static XmlDocument ParseDocument(string text, XmlParserOptions options)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }

    private static XmlDocument ParseFragment(string text, XmlParserOptions options)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text };
        return new XmlParser().ParseXmlFragment(input, options, null, null, null);
    }

    private static XmlParserOptions Options()
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            PreservePrefixes = true,
            PreserveComments = true,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
    }
}
