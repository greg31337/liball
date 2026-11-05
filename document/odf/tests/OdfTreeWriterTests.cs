using System.Text;

namespace odf.Tests;

public class OdfTreeWriterTests
{
    [Test]
    public void PreservationModeRetainsPrefixesOrderMixedContentAndEmptyElementForm()
    {
        string xml = "<?pre keep?>\n<o:document xmlns:z='urn:z' xmlns:o='"
            + OdfNamespaces.Office
            + "' xmlns:a='urn:a' z:last='0007 &amp; exact' a:first='one'>"
            + "before<z:empty/><z:full></z:full><!--note--><?work go?>after"
            + "</o:document>";
        OdfNode[] source = Read(xml);
        OdfSaveOptions options = new OdfSaveOptions();

        string written = OdfTreeWriter.WriteText(source, options);
        OdfNode[] reparsed = Read(written);
        OdfNode root = Root(reparsed);
        int before = written.IndexOf("before", StringComparison.Ordinal);
        int empty = written.IndexOf("z:empty", StringComparison.Ordinal);
        int full = written.IndexOf("z:full", StringComparison.Ordinal);
        int comment = written.IndexOf("<!--note-->", StringComparison.Ordinal);
        int instruction = written.IndexOf("<?work go?>", StringComparison.Ordinal);
        int after = written.IndexOf("after", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(written, Does.Contain("<o:document"));
            Assert.That(written.IndexOf("xmlns:z", StringComparison.Ordinal), Is.LessThan(written.IndexOf("xmlns:o", StringComparison.Ordinal)));
            Assert.That(written.IndexOf("xmlns:o", StringComparison.Ordinal), Is.LessThan(written.IndexOf("xmlns:a", StringComparison.Ordinal)));
            Assert.That(written.IndexOf("z:last", StringComparison.Ordinal), Is.LessThan(written.IndexOf("a:first", StringComparison.Ordinal)));
            Assert.That(written, Does.Contain("z:last=\"0007 &amp; exact\""));
            Assert.That(written, Does.Contain("<z:empty />"));
            Assert.That(written, Does.Contain("<z:full></z:full>"));
            Assert.That(before, Is.LessThan(empty));
            Assert.That(empty, Is.LessThan(full));
            Assert.That(full, Is.LessThan(comment));
            Assert.That(comment, Is.LessThan(instruction));
            Assert.That(instruction, Is.LessThan(after));
            Assert.That(reparsed[0].Kind, Is.EqualTo(OdfNodeKind.ProcessingInstruction));
            Assert.That(root.Name?.Prefix, Is.EqualTo("o"));
            Assert.That(root.Attributes[0].Value, Is.EqualTo("0007 & exact"));
            Assert.That(root.Children.Select(node => node.Kind), Is.EqualTo(new[]
            {
                OdfNodeKind.Text,
                OdfNodeKind.Element,
                OdfNodeKind.Element,
                OdfNodeKind.Comment,
                OdfNodeKind.ProcessingInstruction,
                OdfNodeKind.Text
            }));
            Assert.That(root.Children[1].WasEmptyElement, Is.True);
            Assert.That(root.Children[2].WasEmptyElement, Is.False);
        });
    }

    [Test]
    public void DeterministicModeStandardizesPrefixesSortsNamesAndIsStable()
    {
        string xml = "<t:p xmlns:z='urn:z' xmlns:t='"
            + OdfNamespaces.Text
            + "' xmlns:a='urn:a' z:last='z' a:first='a'></t:p>";
        OdfNode[] source = Read(xml);
        OdfSaveOptions options = new OdfSaveOptions
        {
            Deterministic = true
        };

        string first = OdfTreeWriter.WriteText(source, options);
        string second = OdfTreeWriter.WriteText(source, options);
        OdfNode root = Root(Read(first));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Does.Contain("<text:p"));
            Assert.That(first, Does.Contain("</text:p>"));
            Assert.That(first.IndexOf("xmlns:a", StringComparison.Ordinal), Is.LessThan(first.IndexOf("xmlns:t", StringComparison.Ordinal)));
            Assert.That(first.IndexOf("xmlns:t", StringComparison.Ordinal), Is.LessThan(first.IndexOf("xmlns:text", StringComparison.Ordinal)));
            Assert.That(first.IndexOf("xmlns:text", StringComparison.Ordinal), Is.LessThan(first.IndexOf("xmlns:z", StringComparison.Ordinal)));
            Assert.That(first.IndexOf("a:first", StringComparison.Ordinal), Is.LessThan(first.IndexOf("z:last", StringComparison.Ordinal)));
            Assert.That(root.Name?.NamespaceName, Is.EqualTo(OdfNamespaces.Text));
            Assert.That(root.Name?.Prefix, Is.EqualTo(OdfNamespaces.TextPrefix));
            Assert.That(root.Attributes.Select(item => item.Value), Is.EqualTo(new[] { "a", "z" }));
        });
    }

    [Test]
    public void CompactAndReadableModesDifferOnlyInFormatting()
    {
        OdfNode root = Element("", "root", "");
        OdfNode child = Element("", "child", "");
        root.Children = [child];
        child.Parent = root;
        OdfNode[] nodes = [root];
        OdfSaveOptions compactOptions = new OdfSaveOptions
        {
            PreserveLexicalValues = false
        };
        OdfSaveOptions readableOptions = new OdfSaveOptions
        {
            PreserveLexicalValues = false,
            IndentXml = true
        };

        string compact = OdfTreeWriter.WriteText(nodes, compactOptions);
        string readable = OdfTreeWriter.WriteText(nodes, readableOptions);
        OdfNode compactRoot = Root(Read(compact));
        OdfNode readableRoot = Root(Read(readable));

        Assert.Multiple(() =>
        {
            Assert.That(compact, Does.Not.Contain("\n"));
            Assert.That(compact, Does.Contain("<root><child /></root>"));
            Assert.That(readable, Does.Contain("\n<root>\n  <child />\n</root>"));
            Assert.That(compactRoot.Children.Single(node => node.Kind == OdfNodeKind.Element).Name?.LocalName, Is.EqualTo("child"));
            Assert.That(readableRoot.Children.Single(node => node.Kind == OdfNodeKind.Element).Name?.LocalName, Is.EqualTo("child"));
        });
    }

    [Test]
    public void StructuredWritingDeclaresSyntheticStandardAndForeignNames()
    {
        OdfNode root = Element(OdfNamespaces.Text, "p", "legacy");
        OdfNode foreign = Element("urn:foreign", "widget", "ext");
        foreign.Attributes =
        [
            new OdfAttribute
            {
                Name = new OdfQualifiedName
                {
                    NamespaceName = "urn:foreign-attribute",
                    LocalName = "mode",
                    Prefix = "custom"
                },
                Value = "keep"
            }
        ];
        root.Children = [foreign];
        foreign.Parent = root;
        OdfSaveOptions options = new OdfSaveOptions
        {
            PreserveLexicalValues = false
        };

        string written = OdfTreeWriter.WriteText([root], options);
        OdfNode reparsed = Root(Read(written));

        Assert.Multiple(() =>
        {
            Assert.That(written, Does.Contain("<text:p"));
            Assert.That(written, Does.Contain("xmlns:text=\"" + OdfNamespaces.Text + "\""));
            Assert.That(written, Does.Contain("<ext:widget"));
            Assert.That(written, Does.Contain("xmlns:ext=\"urn:foreign\""));
            Assert.That(written, Does.Contain("xmlns:custom=\"urn:foreign-attribute\""));
            Assert.That(reparsed.Children[0].Name?.NamespaceName, Is.EqualTo("urn:foreign"));
            Assert.That(reparsed.Children[0].Attributes[0].Value, Is.EqualTo("keep"));
        });
    }

    [Test]
    public void StopsBeforeWritingWhenCancellationWasRequested()
    {
        OdfSaveOptions options = new OdfSaveOptions();
        options.Cancellation.IsCancellationRequested = true;

        Assert.That(
            () => OdfTreeWriter.WriteText([Element("", "root", "")], options),
            Throws.TypeOf<OdfXmlException>()
                .With.Message.Contains("cancelled"));
    }

    private static OdfNode[] Read(string xml)
    {
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            OdfPartNames.Content,
            new OdfLoadOptions());
    }

    private static OdfNode Root(OdfNode[] nodes)
    {
        return nodes.Single(node => node.Kind == OdfNodeKind.Element);
    }

    private static OdfNode Element(
        string namespaceName,
        string localName,
        string prefix)
    {
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = localName,
                Prefix = prefix
            }
        };
    }
}
