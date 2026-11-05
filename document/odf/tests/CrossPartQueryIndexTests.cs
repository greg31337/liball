using System.Text;

namespace odf.Tests;

public class CrossPartQueryIndexTests
{
    [Test]
    public void IndexesXmlNamesIdsQualifiedNamesTextAndStylesAcrossParts()
    {
        OdfDocument document = CreateDocument();
        CrossPartQueryIndex index = new CrossPartQueryIndex(document);
        CrossPartQueryRecord[] named = index.FindNames("Hero");
        CrossPartQueryRecord[] firstId = index.FindIds("p1");
        CrossPartQueryRecord[] secondId = index.FindIds("p2");
        CrossPartQueryRecord[] paragraphs = index.FindQualifiedNames(
            Name(OdfNamespaces.Text, "p"));
        CrossPartQueryRecord[] text = index.FindText(
            "CROSS PART",
            false);
        CrossPartQueryRecord[] caseSensitiveText = index.FindText(
            "CROSS PART",
            true);
        CrossPartQueryRecord[] styles = index.FindStyles("Body");

        Assert.Multiple(() =>
        {
            Assert.That(named, Has.Length.EqualTo(1));
            Assert.That(named[0].PartPath,
                Is.EqualTo(OdfPartNames.Content));
            Assert.That(firstId.Select(item => item.PartPath),
                Does.Contain(OdfPartNames.Content));
            Assert.That(secondId.Select(item => item.PartPath),
                Does.Contain(OdfPartNames.Styles));
            Assert.That(paragraphs.Select(item => item.PartPath),
                Does.Contain(OdfPartNames.Content));
            Assert.That(paragraphs.Select(item => item.PartPath),
                Does.Contain(OdfPartNames.Styles));
            Assert.That(text.Select(item => item.PartPath),
                Is.EquivalentTo(new[]
                {
                    OdfPartNames.Content,
                    OdfPartNames.Styles
                }));
            Assert.That(caseSensitiveText, Is.Empty);
            Assert.That(styles.Any(item =>
                item.PartPath == OdfPartNames.Content
                && item.Node?.Name?.LocalName == "p"), Is.True);
            Assert.That(styles.Any(item =>
                item.PartPath == OdfPartNames.Styles
                && item.Node?.Name?.LocalName == "style"), Is.True);
            Assert.That(styles.Any(item =>
                item.Style == document.Styles[0]), Is.True);
        });
    }

    [Test]
    public void IndexesRangesFormulasResourcesAnnotationsAndChanges()
    {
        OdfDocument document = CreateDocument();
        CrossPartQueryIndex index = new CrossPartQueryIndex(document);
        string formulaSource = "of:=SUM([.A1:.A2])";
        string rangeAddress = "Sheet1.A1:Sheet1.A2";
        CrossPartQueryRecord[] ranges = index.FindRanges(rangeAddress);
        CrossPartQueryRecord[] formulas =
            index.FindFormulas(formulaSource);
        CrossPartQueryRecord[] resources =
            index.FindResources("Pictures/a.png");
        CrossPartQueryRecord[] annotations =
            index.FindAnnotations("note1");
        CrossPartQueryRecord[] textChanges = index.FindChanges("ct1");
        CrossPartQueryRecord[] sheetChanges = index.FindChanges("sc1");

        Assert.Multiple(() =>
        {
            Assert.That(ranges.Any(item =>
                item.Node?.Name?.LocalName == "table-cell"), Is.True);
            Assert.That(ranges.Any(item =>
                item.NamedExpression == document.NamedExpressions[0]),
                Is.True);
            Assert.That(formulas.Any(item =>
                item.Formula == document.Formulas[0]), Is.True);
            Assert.That(formulas.Any(item =>
                item.Node?.Name?.LocalName == "table-cell"), Is.True);
            Assert.That(resources.Any(item =>
                item.Image == document.Images[0]), Is.True);
            Assert.That(resources.Any(item =>
                item.Node?.Name?.LocalName == "image"), Is.True);
            Assert.That(annotations.Any(item =>
                item.Annotation == document.Annotations[0]), Is.True);
            Assert.That(annotations.Any(item =>
                item.Node?.Name?.LocalName == "annotation"), Is.True);
            Assert.That(textChanges.Any(item =>
                item.Change == document.Changes[0]), Is.True);
            Assert.That(textChanges.Any(item =>
                item.Node?.Name?.LocalName == "change-start"), Is.True);
            Assert.That(sheetChanges.Single().SpreadsheetChange,
                Is.SameAs(document.SpreadsheetChanges[0]));
            Assert.That(index.GetAllRecords().Select(item => item.Category),
                Does.Contain("resource"));
        });
    }

    [Test]
    public void InvalidatesAndRebuildsSnapshotsAfterDocumentMutation()
    {
        OdfNode root = Element("urn:test", "item");
        OdfAttribute name = Attribute("urn:test", "name", "before");
        root.Attributes = [name];
        OdfDocument document = DocumentWithPart(root, "first.xml");
        CrossPartQueryIndex index = new CrossPartQueryIndex(document);

        CrossPartQueryRecord[] first = index.FindNames("before");
        int firstRevision = index.GetRevision();
        name.Value = "after";
        CrossPartQueryRecord[] cached = index.FindNames("before");
        index.Invalidate();
        CrossPartQueryRecord[] rebuilt = index.FindNames("after");
        int secondRevision = index.GetRevision();
        OdfNode replacement = Element("urn:test", "replacement");
        replacement.Attributes =
        [
            Attribute("urn:test", "name", "replacement")
        ];
        index.UpdateDocument(DocumentWithPart(
            replacement,
            "second.xml"));
        CrossPartQueryRecord[] updated =
            index.FindNames("replacement");

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(1));
            Assert.That(cached, Has.Length.EqualTo(1));
            Assert.That(rebuilt, Has.Length.EqualTo(1));
            Assert.That(index.FindNames("before"), Is.Empty);
            Assert.That(secondRevision, Is.EqualTo(firstRevision + 1));
            Assert.That(updated.Single().PartPath,
                Is.EqualTo("second.xml"));
            Assert.That(index.GetRevision(),
                Is.EqualTo(secondRevision + 1));
        });
    }

    static OdfDocument CreateDocument()
    {
        string content = "<office:document-content xmlns:office='"
            + OdfNamespaces.Office + "' xmlns:text='"
            + OdfNamespaces.Text + "' xmlns:style='"
            + OdfNamespaces.Style + "' xmlns:table='"
            + OdfNamespaces.Table + "' xmlns:draw='"
            + OdfNamespaces.Drawing + "' xmlns:xlink='"
            + OdfNamespaces.XLink + "' xmlns:xml='"
            + OdfNamespaces.Xml + "'><office:body><office:text>"
            + "<text:p xml:id='p1' text:style-name='Body'>"
            + "Alpha cross part</text:p>"
            + "<draw:image draw:name='Hero' "
            + "xlink:href='Pictures/a.png'/>"
            + "<table:table-cell table:formula='of:=SUM([.A1:.A2])' "
            + "table:cell-range-address='Sheet1.A1:Sheet1.A2'/>"
            + "<office:annotation office:name='note1' xml:id='ann1'>"
            + "<text:p>Remark</text:p></office:annotation>"
            + "<text:change-start text:change-id='ct1'/>"
            + "</office:text></office:body></office:document-content>";
        string styles = "<office:document-styles xmlns:office='"
            + OdfNamespaces.Office + "' xmlns:style='"
            + OdfNamespaces.Style + "' xmlns:text='"
            + OdfNamespaces.Text + "' xmlns:xml='"
            + OdfNamespaces.Xml + "'><office:styles>"
            + "<style:style style:name='Body'/>"
            + "<text:p xml:id='p2'>Beta cross part</text:p>"
            + "</office:styles></office:document-styles>";
        OdfPart contentPart = ParsePart(content, OdfPartNames.Content);
        OdfPart stylesPart = ParsePart(styles, OdfPartNames.Styles);
        OdfDocument document = new OdfDocument();
        document.Parts = [contentPart, stylesPart];
        OdfNode imageNode = Descendant(
            contentPart.Root!,
            OdfNamespaces.Drawing,
            "image");
        OdfNode annotationNode = Descendant(
            contentPart.Root!,
            OdfNamespaces.Office,
            "annotation");
        OdfNode changeNode = Descendant(
            contentPart.Root!,
            OdfNamespaces.Text,
            "change-start");
        document.Styles =
        [
            new OdfStyle
            {
                Name = "Body",
                PartPath = OdfPartNames.Styles,
                Node = Descendant(
                    stylesPart.Root!,
                    OdfNamespaces.Style,
                    "style")
            }
        ];
        document.NamedExpressions =
        [
            new OdfNamedExpression
            {
                Name = "DataRange",
                IsRange = true,
                RangeAddress = "Sheet1.A1:Sheet1.A2"
            }
        ];
        document.Formulas =
        [
            new OdfFormula { Source = "of:=SUM([.A1:.A2])" }
        ];
        document.Images =
        [
            new OdfImage
            {
                Href = "Pictures/a.png",
                PackagePath = "Pictures/a.png",
                Node = imageNode
            }
        ];
        document.Annotations =
        [
            new OdfAnnotation
            {
                Name = "note1",
                XmlId = "ann1",
                Creator = "Author",
                Text = "Remark",
                PartPath = OdfPartNames.Content,
                Node = annotationNode
            }
        ];
        document.Changes =
        [
            new OdfChange
            {
                Id = "ct1",
                XmlId = "change-xml",
                PartPath = OdfPartNames.Content,
                Node = changeNode
            }
        ];
        document.SpreadsheetChanges =
        [
            new OdfSpreadsheetChange
            {
                Id = "sc1",
                Kind = "insertion"
            }
        ];
        return document;
    }

    static OdfPart ParsePart(string xml, string partPath)
    {
        OdfNode[] nodes = OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            partPath,
            new OdfLoadOptions());
        return new OdfPart
        {
            Path = partPath,
            MediaType = OdfMediaTypes.Xml,
            Nodes = nodes,
            Root = nodes.Single(item => item.Kind == OdfNodeKind.Element)
        };
    }

    static OdfDocument DocumentWithPart(OdfNode root, string partPath)
    {
        root.Location.PartPath = partPath;
        OdfPart part = new OdfPart();
        part.Path = partPath;
        part.Nodes = [root];
        part.Root = root;
        OdfDocument document = new OdfDocument();
        document.Parts = [part];
        return document;
    }

    static OdfNode Element(string namespaceName, string localName)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = Name(namespaceName, localName);
        return result;
    }

    static OdfAttribute Attribute(
        string namespaceName,
        string localName,
        string value)
    {
        OdfAttribute result = new OdfAttribute();
        result.Name = Name(namespaceName, localName);
        result.Value = value;
        return result;
    }

    static OdfQualifiedName Name(
        string namespaceName,
        string localName)
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = namespaceName;
        result.LocalName = localName;
        return result;
    }

    static OdfNode Descendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            OdfNode child = node.Children[index];
            if (ContainsDescendant(child, namespaceName, localName))
            {
                return Descendant(child, namespaceName, localName);
            }
            index = index + 1;
        }
        throw new AssertionException("Expected descendant was not found.");
    }

    static bool ContainsDescendant(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        if (node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            return true;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            if (ContainsDescendant(
                node.Children[index],
                namespaceName,
                localName))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
