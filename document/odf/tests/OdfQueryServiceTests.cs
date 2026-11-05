using System.Text;

namespace odf.Tests;

public class OdfQueryServiceTests
{
    [Test]
    public void QueriesImmediatelyReflectIdentifierAndReferenceUpdates()
    {
        OdfDocument document = CreateReferenceDocument();
        IOdfQueryService service = new OdfQueryService();
        IOdfDocumentEditor editor = new OdfDocumentEditor();

        OdfEditResult renamed = editor.RenameDocumentIdentifier(
            document,
            "p1",
            "paragraph-renamed");
        OdfSearchMatch[] oldReferences = service.QueryReferences(
            document,
            "p1");
        OdfSearchMatch[] newReferences = service.QueryReferences(
            document,
            "paragraph-renamed");
        OdfQualifiedName identifierName = Name(OdfNamespaces.Xml, "id");
        identifierName.Prefix = "xml";
        OdfSearchMatch[] identifierField = service.QueryField(
            document,
            identifierName,
            "paragraph-renamed",
            true);
        OdfSearchMatch[] literal = service.QueryLiteral(
            document,
            "paragraph-renamed",
            true);
        OdfSearchMatch[] nodes = service.QueryNodes(
            document,
            Name(OdfNamespaces.Text, "p"));
        OdfSearchMatch[] expression = service.QueryRegularExpression(
            document,
            "paragraph-(renamed)",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(renamed.Status, Is.EqualTo(OdfEditStatus.Success));
            Assert.That(oldReferences, Is.Empty);
            Assert.That(newReferences, Has.Length.EqualTo(1));
            Assert.That(newReferences[0].Context,
                Is.EqualTo("#paragraph-renamed"));
            Assert.That(identifierField, Has.Length.EqualTo(1));
            Assert.That(identifierField[0].IsExact, Is.True);
            Assert.That(literal, Has.Length.EqualTo(2));
            Assert.That(nodes, Has.Length.EqualTo(2));
            Assert.That(expression, Has.Length.EqualTo(2));
            Assert.That(expression.All(item =>
                    item.MatchedValue == "paragraph-renamed"),
                Is.True);
        });
    }

    [Test]
    public void QueriesLiteralAndRegularExpressionValuesAcrossParts()
    {
        OdfDocument document = CreateDocument();
        IOdfQueryService service = new OdfQueryService();

        OdfSearchMatch[] insensitive = service.QueryLiteral(
            document,
            "alpha",
            false);
        OdfSearchMatch[] sensitive = service.QueryLiteral(
            document,
            "alpha",
            true);
        OdfSearchMatch[] expressions =
            service.QueryRegularExpression(
                document,
                "alpha|styles",
                false);

        Assert.Multiple(() =>
        {
            Assert.That(insensitive, Has.Length.EqualTo(3));
            Assert.That(insensitive.Select(item => item.Location.PartPath),
                Does.Contain(OdfPartNames.Content));
            Assert.That(insensitive.Select(item => item.Location.PartPath),
                Does.Contain(OdfPartNames.Styles));
            Assert.That(insensitive.All(item =>
                item.Location.TextOffset >= 0
                && item.Location.TextLength == 5), Is.True);
            Assert.That(sensitive, Has.Length.EqualTo(1));
            Assert.That(sensitive[0].MatchedValue, Is.EqualTo("alpha"));
            Assert.That(expressions.Select(item => item.MatchedValue),
                Is.EquivalentTo(new[]
                {
                    "Alpha",
                    "alpha",
                    "ALPHA",
                    "styles"
                }));
            Assert.That(expressions.Single(item =>
                item.MatchedValue == "styles").Context,
                Is.EqualTo("ALPHA styles"));
        });
    }

    [Test]
    public void QueriesNodesAndFieldsUsingExpandedNames()
    {
        OdfDocument document = CreateDocument();
        IOdfQueryService service = new OdfQueryService();

        OdfSearchMatch[] paragraphs = service.QueryNodes(
            document,
            Name(OdfNamespaces.Text, "p"));
        OdfSearchMatch[] styleNames = service.QueryField(
            document,
            Name(OdfNamespaces.Style, "name"),
            "",
            true);
        OdfSearchMatch[] href = service.QueryField(
            document,
            Name(OdfNamespaces.XLink, "href"),
            "pictures/hero",
            false);

        Assert.Multiple(() =>
        {
            Assert.That(paragraphs, Has.Length.EqualTo(2));
            Assert.That(paragraphs.Select(item => item.Location.PartPath),
                Is.EquivalentTo(new[]
                {
                    OdfPartNames.Content,
                    OdfPartNames.Styles
                }));
            Assert.That(paragraphs.All(item =>
                item.Node?.Name?.LocalName == "p"), Is.True);
            Assert.That(styleNames.Single().MatchedValue,
                Is.EqualTo("Body"));
            Assert.That(styleNames.Single().IsExact, Is.True);
            Assert.That(href.Single().MatchedValue,
                Is.EqualTo("Pictures/Hero"));
            Assert.That(href.Single().Context,
                Is.EqualTo("Pictures/Hero.png"));
            Assert.That(href.Single().Location.TextOffset, Is.Zero);
        });
    }

    [Test]
    public void QueriesExactAndFragmentReferenceTargetsWithoutDuplicates()
    {
        OdfDocument document = CreateDocument();
        IOdfQueryService service = new OdfQueryService();

        OdfSearchMatch[] image = service.QueryReferences(
            document,
            "Pictures/Hero.png");
        OdfSearchMatch[] fragment = service.QueryReferences(
            document,
            "p1");
        OdfSearchMatch[] style = service.QueryReferences(
            document,
            "Body");

        Assert.Multiple(() =>
        {
            Assert.That(image, Has.Length.EqualTo(1));
            Assert.That(image[0].Node?.Name?.LocalName,
                Is.EqualTo("image"));
            Assert.That(image[0].IsExact, Is.True);
            Assert.That(fragment, Has.Length.EqualTo(1));
            Assert.That(fragment[0].Context, Is.EqualTo("#p1"));
            Assert.That(fragment[0].IsExact, Is.False);
            Assert.That(style, Has.Length.EqualTo(1));
            Assert.That(style[0].Node?.Name?.LocalName,
                Is.EqualTo("p"));
        });
    }

    [Test]
    public void QueriesTypedModelValuesAndRejectsInvalidRequests()
    {
        OdfDocument document = CreateDocument();
        IOdfQueryService service = new OdfQueryService();

        OdfSearchMatch[] formula = service.QueryLiteral(
            document,
            "SUM([.A1:.A2])",
            true);
        OdfSearchMatch[] annotation = service.QueryLiteral(
            document,
            "Review note",
            true);
        OdfSearchMatch[] change = service.QueryLiteral(
            document,
            "Change comment",
            true);

        Assert.Multiple(() =>
        {
            Assert.That(formula, Has.Length.EqualTo(1));
            Assert.That(formula[0].Context,
                Is.EqualTo("of:=SUM([.A1:.A2])"));
            Assert.That(annotation, Has.Length.EqualTo(1));
            Assert.That(annotation[0].Location.PartPath,
                Is.EqualTo(OdfPartNames.Content));
            Assert.That(change, Has.Length.EqualTo(1));
            Assert.That(() => service.QueryLiteral(
                    document,
                    "",
                    true),
                Throws.TypeOf<OdfDocumentException>());
            Assert.That(() => service.QueryReferences(
                    document,
                    ""),
                Throws.TypeOf<OdfDocumentException>());
            Assert.That(() => service.QueryNodes(
                    document,
                    new OdfQualifiedName()),
                Throws.TypeOf<OdfXmlException>());
        });
    }

    static OdfDocument CreateDocument()
    {
        string content = "<office:document-content xmlns:office='"
            + OdfNamespaces.Office + "' xmlns:text='"
            + OdfNamespaces.Text + "' xmlns:style='"
            + OdfNamespaces.Style + "' xmlns:draw='"
            + OdfNamespaces.Drawing + "' xmlns:xlink='"
            + OdfNamespaces.XLink + "' xmlns:xml='"
            + OdfNamespaces.Xml + "'><office:body><office:text>"
            + "<text:p xml:id='p1' text:style-name='Body'>"
            + "Alpha alpha 42</text:p>"
            + "<draw:image draw:name='Hero' "
            + "xlink:href='Pictures/Hero.png'/>"
            + "<text:a xlink:href='#p1'>jump</text:a>"
            + "</office:text></office:body></office:document-content>";
        string styles = "<office:document-styles xmlns:office='"
            + OdfNamespaces.Office + "' xmlns:style='"
            + OdfNamespaces.Style + "' xmlns:text='"
            + OdfNamespaces.Text + "'><office:styles>"
            + "<style:style style:name='Body' "
            + "style:display-name='Body Display'/>"
            + "<text:p>ALPHA styles</text:p>"
            + "</office:styles></office:document-styles>";
        OdfPart contentPart = ParsePart(content, OdfPartNames.Content);
        OdfPart stylesPart = ParsePart(styles, OdfPartNames.Styles);
        OdfNode imageNode = Descendant(
            contentPart.Root!,
            OdfNamespaces.Drawing,
            "image");
        OdfDocument document = new OdfDocument();
        document.Parts = [contentPart, stylesPart];
        document.Formulas =
        [
            new OdfFormula { Source = "of:=SUM([.A1:.A2])" }
        ];
        document.Images =
        [
            new OdfImage
            {
                Href = "Pictures/Hero.png",
                PackagePath = "Pictures/Hero.png",
                Node = imageNode
            }
        ];
        document.Annotations =
        [
            new OdfAnnotation
            {
                Name = "note1",
                Text = "Review note",
                PartPath = OdfPartNames.Content
            }
        ];
        document.Changes =
        [
            new OdfChange
            {
                Id = "change1",
                Comment = "Change comment",
                PartPath = OdfPartNames.Content
            }
        ];
        return document;
    }

    static OdfDocument CreateReferenceDocument()
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:xlink='" + OdfNamespaces.XLink
            + "' xmlns:xml='" + OdfNamespaces.Xml
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'><office:body><office:text>"
            + "<text:p xml:id='p1'>Target</text:p>"
            + "<text:a xlink:href='#p1'>Reference</text:a>"
            + "<text:p>Second</text:p></office:text></office:body>"
            + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "query-reference.fodt",
            new OdfLoadOptions());
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
            Nodes = nodes,
            Root = nodes.Single(item => item.Kind == OdfNodeKind.Element)
        };
    }

    static OdfQualifiedName Name(
        string namespaceName,
        string localName)
    {
        return new OdfQualifiedName
        {
            NamespaceName = namespaceName,
            LocalName = localName
        };
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
