using System.Text;

namespace odf.Tests;

public class OdfXmlContractTests
{
    [Test]
    public void ByteReaderMaterializesEveryNodeKindAndNamespaceCase()
    {
        string xml = "<?top alpha?><!--head-->"
            + "<office:document xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "' xmlns:f='urn:foreign' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text
            + "'><office:body><office:text><text:p xml:id='p1' plain='bare' f:flag='yes'>"
            + "before<f:inline xmlns:f='urn:shadow' f:mode='exact' xml:space='preserve'> \n </f:inline>"
            + "<plain xmlns='urn:default' value='unqualified'/>"
            + "<f:cdata><![CDATA[raw <markup>]]></f:cdata>"
            + "<!--inside--><?work go?>after"
            + "</text:p></office:text></office:body></office:document><!--tail-->";
        IOdfXmlReader reader = new OdfXmlReader();

        OdfPart part = reader.ReadXmlBytes(
            Encoding.UTF8.GetBytes(xml),
            "",
            new OdfLoadOptions());
        OdfNode root = part.Root!;
        OdfNode paragraph = FindElement(root, OdfNamespaces.Text, "p");
        OdfNode shadow = FindElement(paragraph, "urn:shadow", "inline");
        OdfNode defaultElement = FindElement(
            paragraph,
            "urn:default",
            "plain");
        OdfNode cdata = FindElement(paragraph, "urn:foreign", "cdata");

        Assert.Multiple(() =>
        {
            Assert.That(part.Path, Is.Empty);
            Assert.That(part.IsFlatDocument, Is.True);
            Assert.That(part.MediaType, Is.EqualTo(OdfMediaTypes.Text));
            Assert.That(part.Nodes.Select(node => node.Kind), Is.EqualTo(new[]
            {
                OdfNodeKind.ProcessingInstruction,
                OdfNodeKind.Comment,
                OdfNodeKind.Element,
                OdfNodeKind.Comment
            }));
            Assert.That(part.Nodes[0].Name?.LocalName, Is.EqualTo("top"));
            Assert.That(part.Nodes[0].Value, Is.EqualTo("alpha"));
            Assert.That(part.Nodes[1].Value, Is.EqualTo("head"));
            Assert.That(part.Nodes[3].Value, Is.EqualTo("tail"));
            Assert.That(root.Name?.NamespaceName, Is.EqualTo(OdfNamespaces.Office));
            Assert.That(root.NamespaceDeclarations.Select(item => item.Prefix),
                Is.EqualTo(new[] { "office", "text", "f" }));
            Assert.That(paragraph.XmlId, Is.EqualTo("p1"));
            Assert.That(FindAttribute(paragraph, "", "plain").Value,
                Is.EqualTo("bare"));
            Assert.That(FindAttribute(paragraph, "urn:foreign", "flag").Value,
                Is.EqualTo("yes"));
            Assert.That(shadow.Name?.Prefix, Is.EqualTo("f"));
            Assert.That(FindAttribute(shadow, "urn:shadow", "mode").Value,
                Is.EqualTo("exact"));
            Assert.That(shadow.Children, Has.Length.EqualTo(1));
            Assert.That(shadow.Children[0].Kind, Is.EqualTo(OdfNodeKind.Text));
            Assert.That(shadow.Children[0].Value, Is.EqualTo(" \n "));
            Assert.That(shadow.Children[0].IsSignificantWhitespace, Is.True);
            Assert.That(defaultElement.Name?.Prefix, Is.Empty);
            Assert.That(FindAttribute(defaultElement, "", "value").Value,
                Is.EqualTo("unqualified"));
            Assert.That(cdata.Children[0].Kind, Is.EqualTo(OdfNodeKind.Text));
            Assert.That(cdata.Children[0].Value, Is.EqualTo("raw <markup>"));
            Assert.That(paragraph.Children.Select(node => node.Kind),
                Is.EqualTo(new[]
                {
                    OdfNodeKind.Text,
                    OdfNodeKind.Element,
                    OdfNodeKind.Element,
                    OdfNodeKind.Element,
                    OdfNodeKind.Comment,
                    OdfNodeKind.ProcessingInstruction,
                    OdfNodeKind.Text
                }));
            Assert.That(paragraph.Children[4].Value, Is.EqualTo("inside"));
            Assert.That(paragraph.Children[5].Name?.LocalName,
                Is.EqualTo("work"));
            Assert.That(paragraph.Children[5].Value, Is.EqualTo("go"));
            Assert.That(paragraph.Children.All(node => node.Parent == paragraph),
                Is.True);
        });
    }

    [Test]
    public void ByteWriterSupportsPreservedCanonicalDeterministicAndFilteredModes()
    {
        string xml = "<o:document xmlns:o='"
            + OdfNamespaces.Office
            + "' xmlns:t='"
            + OdfNamespaces.Text
            + "' xmlns:z='urn:vendor' o:version='1.4' o:mimetype='"
            + OdfMediaTypes.Text
            + "' z:last='0007'><z:vendor/><o:body><o:text><t:p>"
            + "<z:empty/><z:full></z:full>value"
            + "</t:p></o:text></o:body></o:document>";
        IOdfXmlReader reader = new OdfXmlReader();
        IOdfXmlWriter writer = new OdfXmlWriter();
        OdfPart part = reader.ReadXmlBytes(
            Encoding.UTF8.GetBytes(xml),
            "",
            new OdfLoadOptions());

        string preserved = Encoding.UTF8.GetString(writer.WriteXmlBytes(
            part,
            new OdfSaveOptions()));
        string canonical = Encoding.UTF8.GetString(writer.WriteXmlBytes(
            part,
            new OdfSaveOptions
            {
                PreserveLexicalValues = false
            }));
        OdfSaveOptions deterministicOptions = new OdfSaveOptions
        {
            Deterministic = true,
            IndentXml = true
        };
        byte[] deterministicBytes = writer.WriteXmlBytes(
            part,
            deterministicOptions);
        string deterministic = Encoding.UTF8.GetString(deterministicBytes);
        byte[] repeated = writer.WriteXmlBytes(part, deterministicOptions);
        string filtered = Encoding.UTF8.GetString(writer.WriteXmlBytes(
            part,
            new OdfSaveOptions
            {
                PreserveUnknownContent = false
            }));

        Assert.Multiple(() =>
        {
            Assert.That(preserved, Does.Contain("<o:document"));
            Assert.That(preserved, Does.Contain("<z:vendor />"));
            Assert.That(preserved, Does.Contain("<z:empty />"));
            Assert.That(preserved, Does.Contain("<z:full></z:full>"));
            Assert.That(canonical, Does.Contain("<office:document"));
            Assert.That(canonical, Does.Contain("<text:p"));
            Assert.That(canonical, Does.Contain("<z:full />"));
            Assert.That(deterministicBytes, Is.EqualTo(repeated));
            Assert.That(deterministic, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>"));
            Assert.That(deterministic, Does.Contain("\n  <z:vendor></z:vendor>"));
            Assert.That(deterministic, Does.Contain("<z:empty></z:empty>"));
            Assert.That(filtered, Does.Not.Contain("<z:vendor"));
            Assert.That(filtered, Does.Contain("<o:body"));
            Assert.That(part.PassthroughNodes, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void StreamMethodsHonorBothOwnershipModes()
    {
        byte[] source = Encoding.UTF8.GetBytes(FlatXml("stream contract"));
        IOdfXmlReader reader = new OdfXmlReader();
        TrackingInputStream keptInput = new TrackingInputStream(source);
        TrackingInputStream closedInput = new TrackingInputStream(source);

        OdfPart keptPart = reader.ReadXmlStream(
            keptInput,
            "",
            new OdfLoadOptions
            {
                LeaveInputOpen = true
            });
        OdfPart closedPart = reader.ReadXmlStream(
            closedInput,
            "",
            new OdfLoadOptions
            {
                LeaveInputOpen = false
            });

        IOdfXmlWriter writer = new OdfXmlWriter();
        TrackingOutputStream keptOutput = new TrackingOutputStream();
        TrackingOutputStream closedOutput = new TrackingOutputStream();
        writer.WriteXmlStream(
            keptPart,
            keptOutput,
            new OdfSaveOptions
            {
                LeaveOutputOpen = true
            });
        writer.WriteXmlStream(
            closedPart,
            closedOutput,
            new OdfSaveOptions
            {
                LeaveOutputOpen = false
            });
        OdfPart roundTrip = reader.ReadXmlBytes(
            keptOutput.ToArray(),
            "",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(keptInput.WasDisposed, Is.False);
            Assert.That(closedInput.WasDisposed, Is.True);
            Assert.That(keptOutput.WasDisposed, Is.False);
            Assert.That(closedOutput.WasDisposed, Is.True);
            Assert.That(keptOutput.CanWrite, Is.True);
            Assert.That(roundTrip.IsFlatDocument, Is.True);
            Assert.That(FindElement(roundTrip.Root!, OdfNamespaces.Text, "p")
                .Children[0].Value, Is.EqualTo("stream contract"));
        });
    }

    [Test]
    public void PathMethodsRoundTripMappedStylesPart()
    {
        string sourcePath = Path.GetTempFileName();
        string outputPath = Path.GetTempFileName();
        string xml = "<office:document-styles xmlns:office='"
            + OdfNamespaces.Office
            + "' office:version='1.4'><office:styles/>"
            + "<office:automatic-styles/></office:document-styles>";
        IOdfXmlReader reader = new OdfXmlReader();
        IOdfXmlWriter writer = new OdfXmlWriter();
        try
        {
            File.WriteAllText(sourcePath, xml);
            OdfPart part = reader.ReadXmlPath(
                sourcePath,
                OdfPartNames.Styles,
                new OdfLoadOptions());
            writer.WriteXmlPath(
                part,
                outputPath,
                new OdfSaveOptions
                {
                    Deterministic = true
                });
            OdfPart roundTrip = reader.ReadXmlPath(
                outputPath,
                OdfPartNames.Styles,
                new OdfLoadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(part.Path, Is.EqualTo(OdfPartNames.Styles));
                Assert.That(part.RootName, Is.EqualTo("document-styles"));
                Assert.That(part.Styles, Is.Not.Null);
                Assert.That(part.AutomaticStyles, Is.Not.Null);
                Assert.That(roundTrip.Sections, Has.Length.EqualTo(2));
                Assert.That(File.ReadAllText(outputPath),
                    Does.Contain("<office:styles></office:styles>"));
            });
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Test]
    public void PublicMethodsRejectInvalidStreamsTreesAndPartMappings()
    {
        IOdfXmlReader reader = new OdfXmlReader();
        IOdfXmlWriter writer = new OdfXmlWriter();
        TrackingInputStream unreadable = new TrackingInputStream(
            Encoding.UTF8.GetBytes(FlatXml("closed")));
        unreadable.Dispose();
        MemoryStream unwritable = new MemoryStream(new byte[256], false);
        OdfPart part = reader.ReadXmlBytes(
            Encoding.UTF8.GetBytes(FlatXml("valid")),
            "",
            new OdfLoadOptions());
        OdfNode unknown = new OdfNode
        {
            Kind = OdfNodeKind.Unknown,
            Parent = part.Root
        };
        part.Root!.Children = [.. part.Root.Children, unknown];
        OdfLoadOptions cancelledLoad = new OdfLoadOptions();
        cancelledLoad.Cancellation.IsCancellationRequested = true;
        OdfSaveOptions cancelledSave = new OdfSaveOptions();
        cancelledSave.Cancellation.IsCancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadXmlStream(
                    unreadable,
                    "",
                    new OdfLoadOptions()),
                Throws.TypeOf<OdfXmlException>());
            Assert.That(
                () => writer.WriteXmlStream(
                    part,
                    unwritable,
                    new OdfSaveOptions()),
                Throws.TypeOf<OdfXmlException>());
            Assert.That(
                () => reader.ReadXmlBytes(
                    Encoding.UTF8.GetBytes("<root/>"),
                    OdfPartNames.Content,
                    new OdfLoadOptions()),
                Throws.TypeOf<OdfXmlException>());
            Assert.That(
                () => reader.ReadXmlBytes(
                    Encoding.UTF8.GetBytes(FlatXml("cancelled")),
                    "",
                    cancelledLoad),
                Throws.TypeOf<OdfXmlException>());
            Assert.That(
                () => writer.WriteXmlBytes(part, cancelledSave),
                Throws.TypeOf<OdfXmlException>());
            Assert.That(
                () => writer.WriteXmlBytes(part, new OdfSaveOptions()),
                Throws.TypeOf<OdfXmlException>()
                    .With.Message.Contains("unknown node kind"));
        });
    }

    static string FlatXml(string value)
    {
        return "<office:document xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text
            + "'><office:body><office:text><text:p>"
            + value
            + "</text:p></office:text></office:body></office:document>";
    }

    static OdfNode FindElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Kind == OdfNodeKind.Element
            && node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? found = FindOptionalElement(
                node.Children[index],
                namespaceName,
                localName);
            if (found != null)
            {
                return found;
            }
            index++;
        }
        Assert.Fail($"Element '{{{namespaceName}}}{localName}' was not found.");
        return new OdfNode();
    }

    static OdfNode? FindOptionalElement(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Kind == OdfNodeKind.Element
            && node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode? result = FindOptionalElement(
                node.Children[index],
                namespaceName,
                localName);
            if (result != null)
            {
                return result;
            }
            index++;
        }
        return null;
    }

    static OdfAttribute FindAttribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == namespaceName
                && attribute.Name.LocalName == localName)
            {
                return attribute;
            }
            index++;
        }
        Assert.Fail($"Attribute '{{{namespaceName}}}{localName}' was not found.");
        return new OdfAttribute();
    }
}
