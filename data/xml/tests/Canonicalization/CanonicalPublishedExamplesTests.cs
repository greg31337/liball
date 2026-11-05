using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class CanonicalPublishedExamplesTests
{
    [Test]
    public void MatchesTheCanonicalXmlPiAndCommentExample()
    {
        string source =
            "<?xml version=\"1.0\"?>\n" +
            "<?xml-stylesheet   href=\"doc.xsl\"\n   type=\"text/xsl\"   ?>\n" +
            "<!DOCTYPE doc>\n" +
            "<doc>Hello, world!<!-- Comment 1 --></doc>\n" +
            "<?pi-without-data     ?>\n" +
            "<!-- Comment 2 -->\n" +
            "<!-- Comment 3 -->";
        XmlDocument document = Parse(source, XmlDtdProcessing.Parse);

        string withoutComments = Canonicalize(document, XmlCanonicalizationMethod.CanonicalXml11, false, null);
        string withComments = Canonicalize(document, XmlCanonicalizationMethod.CanonicalXml11, true, null);

        Assert.Multiple(() =>
        {
            Assert.That(withoutComments, Is.EqualTo("<?xml-stylesheet href=\"doc.xsl\"\n   type=\"text/xsl\"   ?>\n<doc>Hello, world!</doc>\n<?pi-without-data?>"));
            Assert.That(withComments, Is.EqualTo("<?xml-stylesheet href=\"doc.xsl\"\n   type=\"text/xsl\"   ?>\n<doc>Hello, world!<!-- Comment 1 --></doc>\n<?pi-without-data?>\n<!-- Comment 2 -->\n<!-- Comment 3 -->"));
        });
    }

    [Test]
    public void MatchesTheCanonicalXmlStartTagAndOrderingExample()
    {
        string source =
            "<!DOCTYPE doc [<!ATTLIST e9 attr CDATA \"default\">]>\n" +
            "<doc>\n" +
            "   <e1   />\n" +
            "   <e2   ></e2>\n" +
            "   <e3   name = \"elem3\"   id=\"elem3\"   />\n" +
            "   <e4   name=\"elem4\"   id=\"elem4\"   ></e4>\n" +
            "   <e5 a:attr=\"out\" b:attr=\"sorted\" attr2=\"all\" attr=\"I'm\" xmlns:b=\"http://www.ietf.org\" xmlns:a=\"http://www.w3.org\" xmlns=\"http://example.org\"/>\n" +
            "   <e6 xmlns=\"\" xmlns:a=\"http://www.w3.org\">\n" +
            "      <e7 xmlns=\"http://www.ietf.org\">\n" +
            "         <e8 xmlns=\"\" xmlns:a=\"http://www.w3.org\">\n" +
            "            <e9 xmlns=\"\" xmlns:a=\"http://www.ietf.org\"/>\n" +
            "         </e8>\n" +
            "      </e7>\n" +
            "   </e6>\n" +
            "</doc>";
        string expected =
            "<doc>\n" +
            "   <e1></e1>\n" +
            "   <e2></e2>\n" +
            "   <e3 id=\"elem3\" name=\"elem3\"></e3>\n" +
            "   <e4 id=\"elem4\" name=\"elem4\"></e4>\n" +
            "   <e5 xmlns=\"http://example.org\" xmlns:a=\"http://www.w3.org\" xmlns:b=\"http://www.ietf.org\" attr=\"I'm\" attr2=\"all\" b:attr=\"sorted\" a:attr=\"out\"></e5>\n" +
            "   <e6 xmlns:a=\"http://www.w3.org\">\n" +
            "      <e7 xmlns=\"http://www.ietf.org\">\n" +
            "         <e8 xmlns=\"\">\n" +
            "            <e9 xmlns:a=\"http://www.ietf.org\" attr=\"default\"></e9>\n" +
            "         </e8>\n" +
            "      </e7>\n" +
            "   </e6>\n" +
            "</doc>";
        XmlDocument document = Parse(source, XmlDtdProcessing.Parse);

        string result = Canonicalize(document, XmlCanonicalizationMethod.CanonicalXml11, false, null);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void MatchesTheExclusiveCanonicalizationEnvelopingExample()
    {
        XmlDocument standalone = Parse("<n1:elem1 xmlns:n1=\"http://b.example\">content</n1:elem1>", XmlDtdProcessing.Prohibit);
        XmlDocument enveloped = Parse("<n0:pdu xmlns:n0=\"http://a.example\"><n1:elem1 xmlns:n1=\"http://b.example\">content</n1:elem1></n0:pdu>", XmlDtdProcessing.Prohibit);
        XmlNode element = enveloped.DocumentElement!.Children!.Single();
        XmlNode text = element.Children!.Single();
        XmlNodeSelection selection = Selection(enveloped,
        [
            element,
            Namespace(enveloped, element, "n0", "http://a.example"),
            Namespace(enveloped, element, "n1", "http://b.example"),
            text
        ]);

        string standaloneResult = Canonicalize(standalone, XmlCanonicalizationMethod.ExclusiveXml10, false, null);
        string envelopedInclusive = Canonicalize(enveloped, XmlCanonicalizationMethod.CanonicalXml11, false, selection);
        string envelopedExclusive = Canonicalize(enveloped, XmlCanonicalizationMethod.ExclusiveXml10, false, selection);

        Assert.Multiple(() =>
        {
            Assert.That(standaloneResult, Is.EqualTo("<n1:elem1 xmlns:n1=\"http://b.example\">content</n1:elem1>"));
            Assert.That(envelopedInclusive, Is.EqualTo("<n1:elem1 xmlns:n0=\"http://a.example\" xmlns:n1=\"http://b.example\">content</n1:elem1>"));
            Assert.That(envelopedExclusive, Is.EqualTo(standaloneResult));
        });
    }

    [TestCase(XmlOutputKind.Stream, "utf-16")]
    [TestCase(XmlOutputKind.Stream, "utf-32")]
    [TestCase(XmlOutputKind.ByteSink, "us-ascii")]
    public void EmitsTheSameUtf8OctetsForEveryByteDestinationAndEncodingHint(XmlOutputKind kind, string encodingName)
    {
        XmlDocument document = Parse("<root>café</root>", XmlDtdProcessing.Prohibit);
        MemoryStream stream = new MemoryStream();
        XmlByteCollector collector = new XmlByteCollector();
        XmlOutput output = new XmlOutput
        {
            Kind = kind,
            ByteStream = kind == XmlOutputKind.Stream ? stream : null,
            ByteSink = kind == XmlOutputKind.ByteSink ? collector : null,
            EncodingName = encodingName,
            Encoding = Encoding.Unicode
        };

        new XmlCanonicalizer().CanonicalizeXml(document, output, new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11
        });

        byte[] actual = kind == XmlOutputKind.Stream ? stream.ToArray() : collector.SnapshotCollectedXmlBytes();
        Assert.That(actual, Is.EqualTo(Encoding.UTF8.GetBytes("<root>café</root>")));
    }

    private static string Canonicalize(XmlDocument document, XmlCanonicalizationMethod method, bool includeComments, XmlNodeSelection? selection)
    {
        StringWriter writer = new StringWriter();
        XmlCanonicalizationOptions options = new XmlCanonicalizationOptions
        {
            Method = method,
            IncludeComments = includeComments,
            Selection = selection
        };
        new XmlCanonicalizer().CanonicalizeXml(document, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    private static XmlDocument Parse(string source, XmlDtdProcessing dtdProcessing)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = dtdProcessing,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveCData = true,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreservePrefixes = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Kind = XmlInputKind.String, Text = source }, options, null, null, null);
    }

    private static XmlNodeSelection Selection(XmlDocument document, XmlNode[] nodes)
    {
        return new XmlNodeSelection
        {
            Document = document,
            Nodes = nodes,
            MutationVersion = document.MutationVersion
        };
    }

    private static XmlNode Namespace(XmlDocument document, XmlNode owner, string prefix, string namespaceUri)
    {
        return new XmlNode
        {
            Kind = XmlNodeKind.Namespace,
            Name = new XmlName { LocalName = prefix, QualifiedName = prefix },
            Value = namespaceUri,
            Parent = owner,
            OwnerDocument = document,
            MutationVersion = document.MutationVersion
        };
    }
}
