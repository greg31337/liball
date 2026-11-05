using System.Text;

namespace odf.Tests;

public class OdfDocumentAssemblyTests
{
    [Test]
    public void AssemblesSeparateTreesIntoOnePackageAggregate()
    {
        OdfNode[] content = ReadPart(
            OdfPartNames.Content,
            "document-content",
            "<office:automatic-styles/><office:body/>",
            "content");
        OdfNode[] styles = ReadPart(
            OdfPartNames.Styles,
            "document-styles",
            "<office:styles/><office:master-styles/>",
            "styles");
        OdfNode[] metadata = ReadPart(
            OdfPartNames.Metadata,
            "document-meta",
            "<office:meta/>",
            "metadata");
        OdfNode[] settings = ReadPart(
            OdfPartNames.Settings,
            "document-settings",
            "<office:settings/>",
            "settings");
        OdfNode originalFirst = content[0];

        OdfDocument document = OdfDocumentAssembly.AssembleSeparateParts(
            content,
            styles,
            metadata,
            settings);
        content[0] = new OdfNode();

        Assert.Multiple(() =>
        {
            Assert.That(document.PackageKind, Is.EqualTo(OdfPackageKind.Package));
            Assert.That(document.Nodes, Has.Length.EqualTo(8));
            Assert.That(document.Nodes[0], Is.SameAs(originalFirst));
            Assert.That(document.Nodes[1].Name?.LocalName, Is.EqualTo("document-content"));
            Assert.That(document.Nodes[3].Name?.LocalName, Is.EqualTo("document-styles"));
            Assert.That(document.Nodes[5].Name?.LocalName, Is.EqualTo("document-meta"));
            Assert.That(document.Nodes[7].Name?.LocalName, Is.EqualTo("document-settings"));
            Assert.That(document.PartTrees?.ContentRoot?.Body, Is.Not.Null);
            Assert.That(document.PartTrees?.StylesRoot?.Styles, Is.Not.Null);
            Assert.That(document.PartTrees?.MetadataRoot?.Metadata, Is.Not.Null);
            Assert.That(document.PartTrees?.SettingsRoot?.Settings, Is.Not.Null);
        });
    }

    [Test]
    public void AllowsOptionalSeparatePartsToBeAbsent()
    {
        OdfNode[] content = ReadPart(
            OdfPartNames.Content,
            "document-content",
            "<office:body/>",
            "");

        OdfDocument document = OdfDocumentAssembly.AssembleSeparateParts(
            content,
            [],
            [],
            []);
        OdfDocumentPartTrees parts = OdfDocumentAssembly.DisassembleSeparateParts(document);

        Assert.Multiple(() =>
        {
            Assert.That(document.Nodes, Has.Length.EqualTo(1));
            Assert.That(parts.ContentNodes, Has.Length.EqualTo(1));
            Assert.That(parts.ContentRoot?.Body, Is.Not.Null);
            Assert.That(parts.StylesNodes, Is.Empty);
            Assert.That(parts.MetadataNodes, Is.Empty);
            Assert.That(parts.SettingsNodes, Is.Empty);
        });
    }

    [Test]
    public void SeparateDisassemblyReturnsIndependentNodeArrays()
    {
        OdfDocument document = OdfDocumentAssembly.AssembleSeparateParts(
            ReadPart(
                OdfPartNames.Content,
                "document-content",
                "<office:body/>",
                ""),
            [],
            [],
            []);

        OdfDocumentPartTrees first = OdfDocumentAssembly.DisassembleSeparateParts(document);
        OdfNode root = first.ContentNodes[0];
        first.ContentNodes[0] = new OdfNode();
        OdfDocumentPartTrees second = OdfDocumentAssembly.DisassembleSeparateParts(document);

        Assert.Multiple(() =>
        {
            Assert.That(second.ContentNodes[0], Is.SameAs(root));
            Assert.That(second.ContentRoot?.Root, Is.SameAs(root));
            Assert.That(second.ContentRoot?.Body, Is.SameAs(root.Children[0]));
        });
    }

    [Test]
    public void AssemblesFlatTreeWithoutChangingItsTopLevelOrder()
    {
        OdfNode[] source = ReadPart(
            "report.fodt",
            "document",
            "<office:body/>",
            "flat");
        OdfNode originalFirst = source[0];

        OdfDocument document = OdfDocumentAssembly.AssembleFlatDocument(source);
        source[0] = new OdfNode();
        OdfNode[] output = OdfDocumentAssembly.DisassembleFlatDocument(document);
        output[0] = new OdfNode();
        OdfNode[] secondOutput = OdfDocumentAssembly.DisassembleFlatDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(document.PackageKind, Is.EqualTo(OdfPackageKind.Flat));
            Assert.That(document.Nodes, Has.Length.EqualTo(2));
            Assert.That(document.Nodes[0], Is.SameAs(originalFirst));
            Assert.That(document.Nodes[1].Name?.LocalName, Is.EqualTo("document"));
            Assert.That(document.PartTrees?.FlatRoot?.Body, Is.Not.Null);
            Assert.That(secondOutput[0], Is.SameAs(originalFirst));
        });
    }

    [Test]
    public void SplitsFlatSectionsIntoCanonicalSeparateRoots()
    {
        string children = "<!--inside-->"
            + "<office:meta/>"
            + "<office:settings/>"
            + "<office:scripts/>"
            + "<office:font-face-decls><style:font-face style:name='Font'/></office:font-face-decls>"
            + "<office:styles><style:style style:name='Named'/></office:styles>"
            + "<office:automatic-styles><style:style style:name='Auto'/></office:automatic-styles>"
            + "<office:master-styles><style:master-page style:name='Master'/></office:master-styles>"
            + "<ext:retained/>"
            + "<office:body/>";
        OdfNode[] flatNodes = ReadFlat(children, "application/vnd.oasis.opendocument.text");
        OdfDocument document = OdfDocumentAssembly.AssembleFlatDocument(flatNodes);
        OdfNode sourceRoot = Root(flatNodes);

        OdfDocumentPartTrees parts = OdfDocumentAssembly.DisassembleSeparateParts(document);

        Assert.Multiple(() =>
        {
            Assert.That(parts.ContentRoot?.RootName, Is.EqualTo("document-content"));
            Assert.That(parts.StylesRoot?.RootName, Is.EqualTo("document-styles"));
            Assert.That(parts.MetadataRoot?.RootName, Is.EqualTo("document-meta"));
            Assert.That(parts.SettingsRoot?.RootName, Is.EqualTo("document-settings"));
            Assert.That(parts.ContentRoot?.Scripts, Is.Not.Null);
            Assert.That(parts.ContentRoot?.AutomaticStyles, Is.Not.Null);
            Assert.That(parts.ContentRoot?.Body, Is.Not.Null);
            Assert.That(
                parts.ContentRoot?.UnmappedSections.Select(node => node.Name?.LocalName),
                Is.EqualTo(new[] { "retained" }));
            Assert.That(parts.StylesRoot?.FontFaceDeclarations, Is.Not.Null);
            Assert.That(parts.StylesRoot?.Styles, Is.Not.Null);
            Assert.That(parts.StylesRoot?.MasterStyles, Is.Not.Null);
            Assert.That(parts.MetadataRoot?.Metadata, Is.Not.Null);
            Assert.That(parts.SettingsRoot?.Settings, Is.Not.Null);
            Assert.That(parts.ContentNodes[0].Kind, Is.EqualTo(OdfNodeKind.ProcessingInstruction));
            Assert.That(parts.ContentRoot?.Root.Attributes.Any(
                attribute => attribute.Name.LocalName == "mimetype"), Is.False);
            Assert.That(parts.ContentRoot?.Root.Attributes.Any(
                attribute => attribute.Name.LocalName == "version"), Is.True);
            Assert.That(parts.ContentRoot?.Root, Is.Not.SameAs(sourceRoot));
            Assert.That(parts.ContentRoot?.Body, Is.Not.SameAs(sourceRoot.Children.Last()));
            Assert.That(parts.ContentRoot?.Root.Children.All(
                child => child.Parent == parts.ContentRoot.Root), Is.True);
        });

        AssertAllPartsWritable(parts);
    }

    [Test]
    public void MergesSeparateSectionsIntoOneFlatRootWithoutSharingOwnership()
    {
        OdfNode[] content = ReadPart(
            OdfPartNames.Content,
            "document-content",
            "<office:scripts/>"
                + "<office:font-face-decls><style:font-face style:name='ContentFont'/></office:font-face-decls>"
                + "<office:automatic-styles><style:style style:name='ContentAuto'/></office:automatic-styles>"
                + "<ext:content-extension/>"
                + "<office:body/>",
            "content");
        OdfNode[] styles = ReadPart(
            OdfPartNames.Styles,
            "document-styles",
            "<office:font-face-decls><style:font-face style:name='StylesFont'/></office:font-face-decls>"
                + "<office:styles><style:style style:name='Named'/></office:styles>"
                + "<office:automatic-styles><style:page-layout style:name='Page'/></office:automatic-styles>"
                + "<office:master-styles/>"
                + "<ext:styles-extension/>",
            "styles");
        OdfNode[] metadata = ReadPart(
            OdfPartNames.Metadata,
            "document-meta",
            "<office:meta/>",
            "meta");
        OdfNode[] settings = ReadPart(
            OdfPartNames.Settings,
            "document-settings",
            "<office:settings/>",
            "settings");
        OdfDocument document = OdfDocumentAssembly.AssembleSeparateParts(
            content,
            styles,
            metadata,
            settings);

        OdfNode[] flatNodes = OdfDocumentAssembly.DisassembleFlatDocument(document);
        OdfPartRootMap flat = OdfPartRootRules.MapRoot(flatNodes, "report.fodt");

        Assert.Multiple(() =>
        {
            Assert.That(flat.IsFlatDocument, Is.True);
            Assert.That(
                flat.StandardSections.Select(node => node.Name?.LocalName),
                Is.EqualTo(new[]
                {
                    "meta",
                    "settings",
                    "scripts",
                    "font-face-decls",
                    "styles",
                    "automatic-styles",
                    "master-styles",
                    "body"
                }));
            Assert.That(flat.FontFaceDeclarations?.Children, Has.Length.EqualTo(2));
            Assert.That(
                flat.FontFaceDeclarations?.Children.Select(StyleName),
                Is.EqualTo(new[] { "ContentFont", "StylesFont" }));
            Assert.That(flat.AutomaticStyles?.Children, Has.Length.EqualTo(2));
            Assert.That(
                flat.AutomaticStyles?.Children.Select(StyleName),
                Is.EqualTo(new[] { "ContentAuto", "Page" }));
            Assert.That(
                flat.UnmappedSections.Select(node => node.Name?.LocalName),
                Is.EqualTo(new[] { "styles-extension", "content-extension" }));
            Assert.That(flat.Body, Is.Not.SameAs(Root(content).Children.Last()));
            Assert.That(flat.Root.Children.All(node => node.Parent == flat.Root), Is.True);
            Assert.That(flatNodes.Count(node => node.Kind == OdfNodeKind.ProcessingInstruction), Is.EqualTo(4));
        });

        string serialized = OdfTreeWriter.WriteText(flatNodes, new OdfSaveOptions());
        Assert.That(serialized, Does.Contain("office:document"));
    }

    [Test]
    public void RejectsMissingContentAndWrongRepresentationRoots()
    {
        OdfNode[] separateRoot = ReadPart(
            OdfPartNames.Content,
            "document-content",
            "<office:body/>",
            "");
        OdfNode[] wrongStyles = ReadPart(
            OdfPartNames.Styles,
            "document-content",
            "<office:body/>",
            "");

        Assert.Multiple(() =>
        {
            Assert.That(
                () => OdfDocumentAssembly.AssembleSeparateParts([], [], [], []),
                Throws.TypeOf<OdfDocumentException>()
                    .With.Message.Contains("content.xml"));
            Assert.That(
                () => OdfDocumentAssembly.AssembleFlatDocument(separateRoot),
                Throws.TypeOf<OdfDocumentException>()
                    .With.Message.Contains("office:document"));
            Assert.That(
                () => OdfDocumentAssembly.AssembleSeparateParts(
                    separateRoot,
                    wrongStyles,
                    [],
                    []),
                Throws.TypeOf<OdfXmlException>()
                    .With.Message.Contains("does not match"));
        });
    }

    private static OdfNode[] ReadPart(
        string partPath,
        string rootName,
        string children,
        string instruction)
    {
        string prolog = "";
        if (instruction.Length > 0)
        {
            prolog = "<?" + instruction + " preserve?>";
        }
        string xml = prolog
            + "<office:"
            + rootName
            + " xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:style='"
            + OdfNamespaces.Style
            + "' xmlns:ext='urn:example:extension' office:version='1.3'>"
            + children
            + "</office:"
            + rootName
            + ">";
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            partPath,
            new OdfLoadOptions());
    }

    private static OdfNode[] ReadFlat(string children, string mediaType)
    {
        string xml = "<?flat preserve?>"
            + "<office:document xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:style='"
            + OdfNamespaces.Style
            + "' xmlns:ext='urn:example:extension' office:version='1.3' office:mimetype='"
            + mediaType
            + "'>"
            + children
            + "</office:document>";
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            "report.fodt",
            new OdfLoadOptions());
    }

    private static OdfNode Root(OdfNode[] nodes)
    {
        return nodes.Single(node => node.Kind == OdfNodeKind.Element);
    }

    private static string StyleName(OdfNode node)
    {
        return node.Attributes.Single(
            attribute => attribute.Name.NamespaceName == OdfNamespaces.Style
                && attribute.Name.LocalName == "name").Value;
    }

    private static void AssertAllPartsWritable(OdfDocumentPartTrees parts)
    {
        OdfSaveOptions options = new OdfSaveOptions();
        Assert.Multiple(() =>
        {
            Assert.That(
                OdfTreeWriter.WriteText(parts.ContentNodes, options),
                Does.Contain("office:document-content"));
            Assert.That(
                OdfTreeWriter.WriteText(parts.StylesNodes, options),
                Does.Contain("office:document-styles"));
            Assert.That(
                OdfTreeWriter.WriteText(parts.MetadataNodes, options),
                Does.Contain("office:document-meta"));
            Assert.That(
                OdfTreeWriter.WriteText(parts.SettingsNodes, options),
                Does.Contain("office:document-settings"));
        });
    }
}
