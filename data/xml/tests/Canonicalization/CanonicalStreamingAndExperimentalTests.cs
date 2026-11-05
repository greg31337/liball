using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class CanonicalStreamingAndExperimentalTests
{
    [Test]
    public void DeliversCanonicalUtf8ToByteSinksInBoundedBlocks()
    {
        string source = "<root>" + new string('x', 10000) + "</root>";
        XmlDocument document = Parse(source, null);
        RecordingSink sink = new RecordingSink();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.ByteSink, ByteSink = sink };

        new XmlCanonicalizer().CanonicalizeXml(document, output, new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11
        });

        Assert.Multiple(() =>
        {
            Assert.That(sink.Bytes, Is.EqualTo(Encoding.UTF8.GetBytes(source)));
            Assert.That(sink.BlockSizes, Has.Count.GreaterThan(5));
            Assert.That(sink.BlockSizes, Has.All.LessThanOrEqualTo(1024));
        });
    }

    [Test]
    public void ExperimentalProfileCanPropagateTheDefaultNamespace()
    {
        XmlDocument document = Parse("<outer xmlns=\"urn:outer\"><p:item xmlns:p=\"urn:item\"/></outer>", null);
        XmlNode item = document.DocumentElement!.Children!.Single();
        XmlNodeSelection selection = new XmlNodeSelection
        {
            Document = document,
            MutationVersion = document.MutationVersion,
            Nodes =
            [
                item,
                Namespace(document, item, "", "urn:outer"),
                Namespace(document, item, "p", "urn:item")
            ]
        };

        string local = CanonicalizeExperimental(document, selection, false, false);
        string propagated = CanonicalizeExperimental(document, selection, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(local, Is.EqualTo("<p:item xmlns:p=\"urn:item\"></p:item>"));
            Assert.That(propagated, Is.EqualTo("<p:item xmlns=\"urn:outer\" xmlns:p=\"urn:item\"></p:item>"));
        });
    }

    [Test]
    public void ExperimentalProfileCanRewriteRecognizedUriAttributes()
    {
        XmlDocument document = Parse("<root xml:base=\"https://EXAMPLE.test/a/../base/\"><link href=\"../item%7e.xml\"/></root>", null);
        XmlNode link = document.DocumentElement!.Children!.Single();
        XmlNode href = link.Attributes!.Single();
        href.TypeInfo = new XmlSchemaTypeInfo { AtomicType = XdmAtomicType.AnyUri };

        string unchanged = CanonicalizeExperimental(document, null, false, false);
        string rewritten = CanonicalizeExperimental(document, null, false, true);

        Assert.Multiple(() =>
        {
            Assert.That(unchanged, Is.EqualTo("<root xml:base=\"https://EXAMPLE.test/a/../base/\"><link href=\"../item%7e.xml\"></link></root>"));
            Assert.That(rewritten, Is.EqualTo("<root xml:base=\"https://example.test/base/\"><link href=\"https://example.test/item~.xml\"></link></root>"));
        });
    }

    private static string CanonicalizeExperimental(XmlDocument document, XmlNodeSelection? selection, bool propagateDefaultNamespace, bool rewriteUris)
    {
        StringWriter writer = new StringWriter();
        XmlCanonicalizationOptions options = new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml20Experimental,
            Selection = selection,
            PropagateDefaultNamespace = propagateDefaultNamespace,
            RewriteUris = rewriteUris
        };
        new XmlCanonicalizer().CanonicalizeXml(document, new XmlOutput { Kind = XmlOutputKind.TextWriter, TextWriter = writer }, options);
        return writer.ToString();
    }

    private static XmlDocument Parse(string source, string? baseUri)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreservePrefixes = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Kind = XmlInputKind.String, Text = source, BaseUri = baseUri }, options, null, null, null);
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

    private sealed class RecordingSink : IXmlByteSink
    {
        internal List<byte> Bytes = [];
        internal List<int> BlockSizes = [];

        public void WriteXmlBytes(ReadOnlyMemory<byte> bytes)
        {
            Bytes.AddRange(bytes.ToArray());
            BlockSizes.Add(bytes.Length);
        }
    }
}
