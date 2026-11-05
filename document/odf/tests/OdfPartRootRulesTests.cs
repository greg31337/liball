using System.Text;

namespace odf.Tests;

public class OdfPartRootRulesTests
{
    [Test]
    public void MapsContentRootSectionsInDocumentOrder()
    {
        OdfPartRootMap map = ReadMap(
            OdfPartNames.Content,
            "document-content",
            "<office:scripts/>"
                + "<office:font-face-decls/>"
                + "<office:automatic-styles/>"
                + "<office:body/>");

        Assert.Multiple(() =>
        {
            Assert.That(map.RootName, Is.EqualTo("document-content"));
            Assert.That(map.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(map.IsFlatDocument, Is.False);
            Assert.That(map.Scripts?.Name?.LocalName, Is.EqualTo("scripts"));
            Assert.That(map.FontFaceDeclarations?.Name?.LocalName, Is.EqualTo("font-face-decls"));
            Assert.That(map.AutomaticStyles?.Name?.LocalName, Is.EqualTo("automatic-styles"));
            Assert.That(map.Body?.Name?.LocalName, Is.EqualTo("body"));
            Assert.That(map.Styles, Is.Null);
            Assert.That(map.Metadata, Is.Null);
            Assert.That(
                map.StandardSections.Select(section => section.Name?.LocalName),
                Is.EqualTo(new[]
                {
                    "scripts",
                    "font-face-decls",
                    "automatic-styles",
                    "body"
                }));
            Assert.That(map.Sections, Is.EqualTo(map.StandardSections));
            Assert.That(map.UnmappedSections, Is.Empty);
        });
    }

    [Test]
    public void MapsStylesRootSections()
    {
        OdfPartRootMap map = ReadMap(
            OdfPartNames.Styles,
            "document-styles",
            "<office:font-face-decls/>"
                + "<office:styles/>"
                + "<office:automatic-styles/>"
                + "<office:master-styles/>");

        Assert.Multiple(() =>
        {
            Assert.That(map.RootName, Is.EqualTo("document-styles"));
            Assert.That(map.FontFaceDeclarations, Is.Not.Null);
            Assert.That(map.Styles, Is.Not.Null);
            Assert.That(map.AutomaticStyles, Is.Not.Null);
            Assert.That(map.MasterStyles, Is.Not.Null);
            Assert.That(map.Scripts, Is.Null);
            Assert.That(map.Body, Is.Null);
            Assert.That(map.StandardSections, Has.Length.EqualTo(4));
            Assert.That(map.UnmappedSections, Is.Empty);
        });
    }

    [TestCase("meta.xml", "document-meta", "meta")]
    [TestCase("settings.xml", "document-settings", "settings")]
    public void MapsOptionalSingleSectionRoots(
        string partPath,
        string rootName,
        string sectionName)
    {
        OdfPartRootMap map = ReadMap(
            partPath,
            rootName,
            "<office:" + sectionName + "/>");

        Assert.Multiple(() =>
        {
            Assert.That(map.RootName, Is.EqualTo(rootName));
            Assert.That(map.StandardSections, Has.Length.EqualTo(1));
            Assert.That(map.StandardSections[0].Name?.LocalName, Is.EqualTo(sectionName));
            Assert.That(map.Metadata != null, Is.EqualTo(sectionName == "meta"));
            Assert.That(map.Settings != null, Is.EqualTo(sectionName == "settings"));
        });
    }

    [Test]
    public void FlatRootMapsEveryStandardSectionAndRetainsExtensions()
    {
        OdfPartRootMap map = ReadMap(
            "report.fodt",
            "document",
            "<office:meta/>"
                + "<ext:before/>"
                + "<office:settings/>"
                + "<office:scripts/>"
                + "<office:font-face-decls/>"
                + "<office:styles/>"
                + "<office:automatic-styles/>"
                + "<office:master-styles/>"
                + "<office:body/>"
                + "<office:future-section/>");

        Assert.Multiple(() =>
        {
            Assert.That(map.IsFlatDocument, Is.True);
            Assert.That(map.Metadata, Is.Not.Null);
            Assert.That(map.Settings, Is.Not.Null);
            Assert.That(map.Scripts, Is.Not.Null);
            Assert.That(map.FontFaceDeclarations, Is.Not.Null);
            Assert.That(map.Styles, Is.Not.Null);
            Assert.That(map.AutomaticStyles, Is.Not.Null);
            Assert.That(map.MasterStyles, Is.Not.Null);
            Assert.That(map.Body, Is.Not.Null);
            Assert.That(map.Sections, Has.Length.EqualTo(10));
            Assert.That(map.StandardSections, Has.Length.EqualTo(8));
            Assert.That(
                map.UnmappedSections.Select(section => section.Name?.LocalName),
                Is.EqualTo(new[] { "before", "future-section" }));
            Assert.That(
                map.Sections.Select(section => section.Name?.LocalName),
                Is.EqualTo(new[]
                {
                    "meta",
                    "before",
                    "settings",
                    "scripts",
                    "font-face-decls",
                    "styles",
                    "automatic-styles",
                    "master-styles",
                    "body",
                    "future-section"
                }));
        });
    }

    [Test]
    public void KeepsMissingDuplicateAndMisplacedSectionsAvailableForValidation()
    {
        OdfPartRootMap missing = ReadMap(
            OdfPartNames.Content,
            "document-content",
            "<!-- no body -->");
        OdfPartRootMap malformed = ReadMap(
            OdfPartNames.Content,
            "document-content",
            "<office:styles/><office:body/><office:body/>");

        Assert.Multiple(() =>
        {
            Assert.That(missing.Body, Is.Null);
            Assert.That(missing.Sections, Is.Empty);
            Assert.That(malformed.Body, Is.SameAs(malformed.StandardSections[0]));
            Assert.That(malformed.StandardSections, Has.Length.EqualTo(2));
            Assert.That(malformed.Sections, Has.Length.EqualTo(3));
            Assert.That(malformed.UnmappedSections, Has.Length.EqualTo(1));
            Assert.That(malformed.UnmappedSections[0].Name?.LocalName, Is.EqualTo("styles"));
        });
    }

    [Test]
    public void IgnoresTopLevelNonElementNodesWhenFindingRoot()
    {
        string xml = "<?open mode='preserve'?><!--before-->"
            + RootXml("document", "<office:body/>")
            + "<!--after-->";
        OdfNode[] nodes = OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            "report.fodt",
            Options());

        OdfPartRootMap map = OdfPartRootRules.MapRoot(nodes, "report.fodt");

        Assert.Multiple(() =>
        {
            Assert.That(nodes, Has.Length.EqualTo(4));
            Assert.That(map.Root, Is.SameAs(nodes[2]));
            Assert.That(map.Body, Is.SameAs(nodes[2].Children[0]));
        });
    }

    [TestCase("content.xml", "document-styles")]
    [TestCase("styles.xml", "document-content")]
    [TestCase("meta.xml", "document-settings")]
    [TestCase("settings.xml", "document-meta")]
    [TestCase("Object1/content.xml", "document")]
    public void RejectsRootsThatDoNotMatchStandardPackagePart(
        string partPath,
        string rootName)
    {
        OdfNode[] nodes = ReadNodes(partPath, rootName, "");

        Assert.That(
            () => OdfPartRootRules.MapRoot(nodes, partPath),
            Throws.TypeOf<OdfXmlException>()
                .With.Message.Contains("does not match"));
    }

    [TestCase("urn:example:not-office", "document")]
    [TestCase("urn:oasis:names:tc:opendocument:xmlns:office:1.0", "unknown-root")]
    public void RejectsNonStandardDocumentElements(
        string namespaceName,
        string rootName)
    {
        OdfNode root = new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = rootName
            }
        };

        Assert.That(
            () => OdfPartRootRules.MapRoot([root], "flat.xml"),
            Throws.TypeOf<OdfXmlException>()
                .With.Message.Contains("standard OpenDocument"));
    }

    [Test]
    public void RejectsMissingAndMultipleTopLevelElements()
    {
        OdfNode first = Element("document");
        OdfNode second = Element("document");

        Assert.Multiple(() =>
        {
            Assert.That(
                () => OdfPartRootRules.MapRoot([], "empty.xml"),
                Throws.TypeOf<OdfXmlException>()
                    .With.Message.Contains("does not contain"));
            Assert.That(
                () => OdfPartRootRules.MapRoot([first, second], "two.xml"),
                Throws.TypeOf<OdfXmlException>()
                    .With.Message.Contains("more than one"));
        });
    }

    private static OdfPartRootMap ReadMap(
        string partPath,
        string rootName,
        string children)
    {
        return OdfPartRootRules.MapRoot(
            ReadNodes(partPath, rootName, children),
            partPath);
    }

    private static OdfNode[] ReadNodes(
        string partPath,
        string rootName,
        string children)
    {
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(RootXml(rootName, children)),
            partPath,
            Options());
    }

    private static string RootXml(string rootName, string children)
    {
        return "<office:"
            + rootName
            + " xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:ext='urn:example:extension'>"
            + children
            + "</office:"
            + rootName
            + ">";
    }

    private static OdfNode Element(string localName)
    {
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = OdfNamespaces.Office,
                LocalName = localName,
                Prefix = OdfNamespaces.OfficePrefix
            }
        };
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
