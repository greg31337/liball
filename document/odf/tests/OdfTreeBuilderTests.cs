using System.Text;

namespace odf.Tests;

public class OdfTreeBuilderTests
{
    [Test]
    public void LoadsOrderedMixedAndForeignContentWithPrefixesAndXmlIds()
    {
        string xml = "<?pre mode='one'?>\n"
            + "<office:document xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "' xmlns:ext='urn:example:foreign' xml:id='doc1' ext:mode='0007 &amp; exact'>"
            + "before<ext:widget ext:flag='yes'><![CDATA[literal <markup>]]></ext:widget>"
            + "<!--note--><?work exact?>after</office:document>\n<!--tail-->";

        OdfNode[] nodes = Read(xml, OdfPartNames.Content);
        OdfNode root = nodes.Single(node => node.Kind == OdfNodeKind.Element);
        OdfNode foreign = root.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(nodes[0].Kind, Is.EqualTo(OdfNodeKind.ProcessingInstruction));
            Assert.That(nodes[0].Name?.LocalName, Is.EqualTo("pre"));
            Assert.That(nodes[0].Value, Is.EqualTo("mode='one'"));
            Assert.That(nodes.Last().Kind, Is.EqualTo(OdfNodeKind.Comment));
            Assert.That(nodes.Last().Value, Is.EqualTo("tail"));
            Assert.That(root.Name?.NamespaceName, Is.EqualTo(OdfNamespaces.Office));
            Assert.That(root.Name?.LocalName, Is.EqualTo("document"));
            Assert.That(root.Name?.Prefix, Is.EqualTo("office"));
            Assert.That(root.NamespaceDeclarations.Select(item => item.Prefix), Is.EqualTo(new[] { "office", "text", "ext" }));
            Assert.That(root.Attributes, Has.Length.EqualTo(2));
            Assert.That(root.Attributes[0].Name.NamespaceName, Is.EqualTo(OdfNamespaces.Xml));
            Assert.That(root.Attributes[0].Name.LocalName, Is.EqualTo("id"));
            Assert.That(root.Attributes[1].Name.NamespaceName, Is.EqualTo("urn:example:foreign"));
            Assert.That(root.Attributes[1].Value, Is.EqualTo("0007 & exact"));
            Assert.That(root.XmlId, Is.EqualTo("doc1"));
            Assert.That(root.Location.XmlId, Is.EqualTo("doc1"));
            Assert.That(root.Location.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(root.Location.Line, Is.EqualTo(2));
            Assert.That(root.Children.Select(node => node.Kind), Is.EqualTo(new[]
            {
                OdfNodeKind.Text,
                OdfNodeKind.Element,
                OdfNodeKind.Comment,
                OdfNodeKind.ProcessingInstruction,
                OdfNodeKind.Text
            }));
            Assert.That(root.Children[0].Value, Is.EqualTo("before"));
            Assert.That(root.Children[2].Value, Is.EqualTo("note"));
            Assert.That(root.Children[3].Name?.LocalName, Is.EqualTo("work"));
            Assert.That(root.Children[3].Value, Is.EqualTo("exact"));
            Assert.That(root.Children[4].Value, Is.EqualTo("after"));
            Assert.That(root.Children.All(node => node.Parent == root), Is.True);
            Assert.That(foreign.Name?.NamespaceName, Is.EqualTo("urn:example:foreign"));
            Assert.That(foreign.Name?.Prefix, Is.EqualTo("ext"));
            Assert.That(foreign.Attributes[0].Name.Prefix, Is.EqualTo("ext"));
            Assert.That(foreign.Attributes[0].Value, Is.EqualTo("yes"));
            Assert.That(foreign.Children[0].Kind, Is.EqualTo(OdfNodeKind.Text));
            Assert.That(foreign.Children[0].Value, Is.EqualTo("literal <markup>"));
            Assert.That(foreign.Location.XmlId, Is.EqualTo("doc1"));
            Assert.That(foreign.Children[0].Location.XmlId, Is.EqualTo("doc1"));
        });
    }

    [Test]
    public void RetainsWhitespaceAndMarksXmlSpacePreservedText()
    {
        const string xml = "<root><ordinary> \n </ordinary>"
            + "<preserved xml:space='preserve'> \n </preserved>"
            + "<cdata><![CDATA[  ]]></cdata></root>";

        OdfNode root = Read(xml, "flat.xml").Single(node => node.Kind == OdfNodeKind.Element);
        OdfNode ordinaryText = root.Children[0].Children[0];
        OdfNode preservedText = root.Children[1].Children[0];
        OdfNode cdataText = root.Children[2].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(ordinaryText.Value, Is.EqualTo(" \n "));
            Assert.That(ordinaryText.IsSignificantWhitespace, Is.False);
            Assert.That(preservedText.Value, Is.EqualTo(" \n "));
            Assert.That(preservedText.IsSignificantWhitespace, Is.True);
            Assert.That(cdataText.Kind, Is.EqualTo(OdfNodeKind.Text));
            Assert.That(cdataText.Value, Is.EqualTo("  "));
            Assert.That(preservedText.Location.TextLength, Is.EqualTo(3));
        });
    }

    [Test]
    public void RecordsLineAndColumnLocationsForElementsAttributesAndDeclarations()
    {
        const string xml = "<root\n"
            + " xmlns:p='urn:example'\n"
            + " p:value='0007'>\n"
            + "  <p:child/>\n"
            + "</root>";

        OdfNode root = Read(xml, "Object1/content.xml").Single(node => node.Kind == OdfNodeKind.Element);
        OdfNode child = root.Children.Single(node => node.Kind == OdfNodeKind.Element);

        Assert.Multiple(() =>
        {
            Assert.That(root.Location.Line, Is.EqualTo(1));
            Assert.That(root.Location.Column, Is.GreaterThan(0));
            Assert.That(root.NamespaceDeclarations[0].Location.Line, Is.EqualTo(2));
            Assert.That(root.NamespaceDeclarations[0].Location.Column, Is.GreaterThan(0));
            Assert.That(root.Attributes[0].Location.Line, Is.EqualTo(3));
            Assert.That(root.Attributes[0].Location.Column, Is.GreaterThan(0));
            Assert.That(root.Attributes[0].Value, Is.EqualTo("0007"));
            Assert.That(child.Location.Line, Is.EqualTo(4));
            Assert.That(child.Location.Column, Is.GreaterThan(0));
            Assert.That(child.Location.PartPath, Is.EqualTo("Object1/content.xml"));
        });
    }

    [Test]
    public void AppliesSecureReaderLimitsDuringMaterialization()
    {
        OdfLoadOptions options = Options();
        options.SecurityLimits.MaximumXmlDepth = 1;

        Assert.That(
            () => OdfTreeBuilder.ReadBytes(
                Encoding.UTF8.GetBytes("<root><child/></root>"),
                OdfPartNames.Content,
                options),
            Throws.TypeOf<OdfXmlException>()
                .With.Message.Contains("element-depth"));
    }

    [Test]
    public void StopsBeforeReadingWhenCancellationWasRequested()
    {
        OdfLoadOptions options = Options();
        options.Cancellation.IsCancellationRequested = true;

        Assert.That(
            () => OdfTreeBuilder.ReadBytes(
                Encoding.UTF8.GetBytes("<root/>"),
                OdfPartNames.Content,
                options),
            Throws.TypeOf<OdfXmlException>()
                .With.Message.Contains("cancelled"));
    }

    private static OdfNode[] Read(string xml, string partPath)
    {
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            partPath,
            Options());
    }

    private static OdfLoadOptions Options()
    {
        return new OdfLoadOptions
        {
            SecurityLimits = new OdfSecurityLimits
            {
                MaximumSingleEntryBytes = 1048576,
                MaximumXmlDepth = 32,
                MaximumXmlAttributesPerElement = 32,
                MaximumXmlTextLength = 1048576
            }
        };
    }
}
