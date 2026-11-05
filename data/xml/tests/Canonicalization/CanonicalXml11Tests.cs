using System.IO;
using System.Linq;
using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class CanonicalXml11Tests
{
    [Test]
    public void CanonicalizesTheFullDocumentWithRequiredOrderingAndEscaping()
    {
        XmlDocument document = Parse("<?before x?><!DOCTYPE doc><doc xmlns:z=\"urn:z\" xmlns=\"urn:d\" b=\"2\" z:a=\"1\" a=\"&quot;&amp;&lt;&#x9;&#xA;&#xD;\"><empty/>text&amp;&lt;&gt;&#xD;<!--comment--><?pi data?></doc><?after?>", XmlDtdProcessing.Parse);
        XmlNode root = document.DocumentElement!;
        XmlNode[] children = root.Children!;
        children.Single(node => node.Kind == XmlNodeKind.Comment).Value = "c\rd";
        children.Single(node => node.Kind == XmlNodeKind.ProcessingInstruction).Value = "p\rq";

        string result = Canonicalize(document, new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11,
            IncludeComments = true
        });

        Assert.That(result, Is.EqualTo("<?before x?>\n<doc xmlns=\"urn:d\" xmlns:z=\"urn:z\" a=\"&quot;&amp;&lt;&#x9;&#xA;&#xD;\" b=\"2\" z:a=\"1\"><empty></empty>text&amp;&lt;&gt;&#xD;<!--c&#xD;d--><?pi p&#xD;q?></doc>\n<?after?>"));
    }

    [Test]
    public void OmitsCommentsWhenTheCommentSwitchIsDisabled()
    {
        XmlDocument document = Parse("<!--before--><root><!--inside--><empty/></root><!--after-->", XmlDtdProcessing.Prohibit);

        string result = Canonicalize(document, new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11,
            IncludeComments = false
        });

        Assert.That(result, Is.EqualTo("<root><empty></empty></root>"));
    }

    [Test]
    public void WritesUtf8WithoutABomRegardlessOfTheDestinationEncodingHint()
    {
        XmlDocument document = Parse("<root>é</root>", XmlDtdProcessing.Prohibit);
        using MemoryStream stream = new MemoryStream();
        XmlOutput output = new XmlOutput
        {
            Kind = XmlOutputKind.Stream,
            ByteStream = stream,
            EncodingName = "utf-16"
        };

        new XmlCanonicalizer().CanonicalizeXml(document, output, new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11
        });

        Assert.That(stream.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes("<root>é</root>")));
    }

    [Test]
    public void TreatsAnExplicitNodeSetAsExactRatherThanImplicitlyIncludingAxes()
    {
        XmlDocument document = Parse("<p:root xmlns:p=\"urn:p\" a=\"1\"><child>text</child></p:root>", XmlDtdProcessing.Prohibit);
        XmlNode root = document.DocumentElement!;
        XmlNodeSelection selection = Selection(document, [root]);

        string result = Canonicalize(document, new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11,
            Selection = selection
        });

        Assert.That(result, Is.EqualTo("<p:root></p:root>"));
    }

    [Test]
    public void AppliesCanonicalXml11SubsetInheritanceAndBaseUriFixup()
    {
        XmlDocument document = Parse("<doc xmlns=\"http://www.ietf.org\" xmlns:w3c=\"http://www.w3.org\" xml:base=\"something/else\"><e1><e2 xmlns=\"\" xml:id=\"abc\" xml:base=\"bar/\" xml:space=\"preserve\"><e3 id=\"E3\" xml:base=\"foo\"/></e2></e1></doc>", XmlDtdProcessing.Prohibit);
        XmlNode documentElement = document.DocumentElement!;
        XmlNode e1 = documentElement.Children!.Single(node => node.Kind == XmlNodeKind.Element);
        XmlNode e2 = e1.Children!.Single(node => node.Kind == XmlNodeKind.Element);
        XmlNode e3 = e2.Children!.Single(node => node.Kind == XmlNodeKind.Element);
        XmlNode id = e3.Attributes!.Single(node => node.Name!.LocalName == "id");
        XmlNodeSelection selection = Selection(document,
        [
            e1,
            Namespace(document, e1, "", "http://www.ietf.org"),
            Namespace(document, e1, "w3c", "http://www.w3.org"),
            e3,
            Namespace(document, e3, "w3c", "http://www.w3.org"),
            id
        ]);

        string result = Canonicalize(document, new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11,
            Selection = selection
        });

        Assert.That(result, Is.EqualTo("<e1 xmlns=\"http://www.ietf.org\" xmlns:w3c=\"http://www.w3.org\" xml:base=\"something/else\"><e3 xmlns=\"\" id=\"E3\" xml:base=\"something/bar/foo\" xml:space=\"preserve\"></e3></e1>"));
    }

    [Test]
    public void PreservesLeadingParentSegmentsDuringRelativeBaseUriFixup()
    {
        XmlDocument document = Parse("<a xml:base=\"foo/bar\"><b xml:base=\"..\"><c xml:base=\"..\"><d xml:base=\"x\"/></c></b></a>", XmlDtdProcessing.Prohibit);
        XmlNode a = document.DocumentElement!;
        XmlNode b = a.Children!.Single();
        XmlNode c = b.Children!.Single();
        XmlNode d = c.Children!.Single();
        XmlNode aBase = a.Attributes!.Single();
        XmlNode dBase = d.Attributes!.Single();
        XmlNodeSelection selection = Selection(document, [a, aBase, d, dBase]);

        string result = Canonicalize(document, new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11,
            Selection = selection
        });

        Assert.That(result, Is.EqualTo("<a xml:base=\"foo/bar\"><d xml:base=\"../../x\"></d></a>"));
    }

    private static string Canonicalize(XmlDocument document, XmlCanonicalizationOptions options)
    {
        StringWriter writer = new StringWriter();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer };
        new XmlCanonicalizer().CanonicalizeXml(document, output, options);
        return writer.ToString();
    }

    private static XmlDocument Parse(string source, XmlDtdProcessing dtdProcessing)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml11,
            NamespaceHandling = XmlNamespaceHandling.Namespaces11,
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
