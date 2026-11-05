using System.Text;

namespace odf.Tests;

public class OdfNodeIndexTests
{
    [Test]
    public void IndexesIdsAndExpandedNamesInDocumentOrderAcrossForeignContent()
    {
        OdfNode[] nodes = ReadTree();
        OdfNodeIndex index = new OdfNodeIndex(nodes);
        OdfNode root = Root(nodes);
        OdfNode firstParagraph = Elements(root, OdfNamespaces.Text, "p")[0];
        OdfNode secondParagraph = Elements(root, OdfNamespaces.Text, "p")[1];
        OdfNode foreign = Elements(root, "urn:example:foreign", "widget")[0];

        OdfNode[] paragraphs = index.FindNodesByQualifiedName(new OdfQualifiedName
        {
            NamespaceName = OdfNamespaces.Text,
            LocalName = "p",
            Prefix = "different-prefix"
        });
        OdfNode[] duplicateIds = index.FindNodesByXmlId("p2");

        Assert.Multiple(() =>
        {
            Assert.That(index.FindNodesByXmlId("p1"), Is.EqualTo(new[] { firstParagraph }));
            Assert.That(duplicateIds, Is.EqualTo(new[] { secondParagraph, foreign }));
            Assert.That(paragraphs, Is.EqualTo(new[] { firstParagraph, secondParagraph }));
            Assert.That(index.FindNodesByQualifiedName(new OdfQualifiedName
            {
                NamespaceName = "urn:example:foreign",
                LocalName = "widget",
                Prefix = "ext"
            }), Is.EqualTo(new[] { foreign }));
            Assert.That(index.FindNodesByXmlId("missing"), Is.Empty);
        });
    }

    [Test]
    public void IndexesStyleDefinitionsAndCrossReferences()
    {
        OdfNode[] nodes = ReadTree();
        OdfNodeIndex index = new OdfNodeIndex(nodes);
        OdfNode root = Root(nodes);
        OdfNode[] styles = Elements(root, OdfNamespaces.Style, "style");
        OdfNode paragraph = Elements(root, OdfNamespaces.Text, "p")[0];
        OdfNode link = Elements(root, OdfNamespaces.Text, "a")[0];
        OdfNode line = Elements(root, OdfNamespaces.Drawing, "line")[0];
        OdfNode foreign = Elements(root, "urn:example:foreign", "widget")[0];

        Assert.Multiple(() =>
        {
            Assert.That(index.FindNodesByStyleName("Body"), Is.EqualTo(styles));
            Assert.That(index.FindNodesReferringTo("Body"), Is.EqualTo(new[] { paragraph }));
            Assert.That(index.FindNodesReferringTo("p2"), Is.EqualTo(new[] { link, line }));
            Assert.That(index.FindNodesReferringTo("p1"), Is.EqualTo(new[] { line, foreign }));
            Assert.That(index.FindNodesReferringTo("missing"), Is.Empty);
        });
    }

    [Test]
    public void InvalidationLazilyRebuildsEveryIndexAfterMutations()
    {
        OdfNode[] nodes = ReadTree();
        OdfNodeIndex index = new OdfNodeIndex(nodes);
        OdfNode root = Root(nodes);
        OdfNode firstStyle = Elements(root, OdfNamespaces.Style, "style")[0];
        OdfNode firstParagraph = Elements(root, OdfNamespaces.Text, "p")[0];
        OdfNode link = Elements(root, OdfNamespaces.Text, "a")[0];
        int initialRevision = index.GetRevision();

        Attribute(firstParagraph, OdfNamespaces.Xml, "id").Value = "renamed";
        firstParagraph.Name!.LocalName = "h";
        Attribute(firstStyle, OdfNamespaces.Style, "name").Value = "Heading";
        Attribute(link, OdfNamespaces.XLink, "href").Value = "#renamed";
        OdfNode added = Element(OdfNamespaces.Text, "p", "new-node");
        added.Parent = root;
        root.Children = root.Children.Concat([added]).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(index.FindNodesByXmlId("p1"), Is.EqualTo(new[] { firstParagraph }));
            Assert.That(index.FindNodesByStyleName("Heading"), Is.Empty);
            Assert.That(index.GetRevision(), Is.EqualTo(initialRevision));
        });

        index.Invalidate();
        OdfNode[] renamedIds = index.FindNodesByXmlId("renamed");
        int rebuiltRevision = index.GetRevision();

        Assert.Multiple(() =>
        {
            Assert.That(renamedIds, Is.EqualTo(new[] { firstParagraph }));
            Assert.That(index.FindNodesByXmlId("p1"), Is.Empty);
            Assert.That(index.FindNodesByXmlId("new-node"), Is.EqualTo(new[] { added }));
            Assert.That(index.FindNodesByQualifiedName(Name(OdfNamespaces.Text, "h")), Is.EqualTo(new[] { firstParagraph }));
            Assert.That(index.FindNodesByQualifiedName(Name(OdfNamespaces.Text, "p")), Does.Contain(added));
            Assert.That(index.FindNodesByStyleName("Heading"), Is.EqualTo(new[] { firstStyle }));
            Assert.That(index.FindNodesByStyleName("Body"), Has.Length.EqualTo(1));
            Assert.That(index.FindNodesReferringTo("renamed"), Is.EqualTo(new[] { link }));
            Assert.That(index.FindNodesReferringTo("p2"), Does.Not.Contain(link));
            Assert.That(rebuiltRevision, Is.EqualTo(initialRevision + 1));
            Assert.That(index.GetRevision(), Is.EqualTo(rebuiltRevision));
        });
    }

    [Test]
    public void RejectsInvalidQualifiedNameQueries()
    {
        OdfNodeIndex index = new OdfNodeIndex(ReadTree());

        Assert.That(
            () => index.FindNodesByQualifiedName(new OdfQualifiedName
            {
                NamespaceName = "",
                LocalName = "1bad",
                Prefix = ""
            }),
            Throws.TypeOf<OdfXmlException>());
    }

    private static OdfNode[] ReadTree()
    {
        string xml = "<office:document xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:style='"
            + OdfNamespaces.Style
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "' xmlns:draw='"
            + OdfNamespaces.Drawing
            + "' xmlns:xlink='"
            + OdfNamespaces.XLink
            + "' xmlns:ext='urn:example:foreign'>"
            + "<style:style xml:id='style-one' style:name='Body'/>"
            + "<style:style xml:id='style-two' style:name='Body'/>"
            + "<text:p xml:id='p1' text:style-name='Body'>"
            + "<text:a xlink:href='other.xml#p2'/></text:p>"
            + "<text:p xml:id='p2'/>"
            + "<draw:line draw:start-shape='p1' draw:end-shape='p2'/>"
            + "<ext:widget xml:id='p2' ext:ref-name='p1'/>"
            + "</office:document>";
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            OdfPartNames.Content,
            new OdfLoadOptions());
    }

    private static OdfNode Root(OdfNode[] nodes)
    {
        return nodes.Single(node => node.Kind == OdfNodeKind.Element);
    }

    private static OdfNode[] Elements(
        OdfNode root,
        string namespaceName,
        string localName)
    {
        List<OdfNode> result = new List<OdfNode>();
        AddElements(root, namespaceName, localName, result);
        return result.ToArray();
    }

    private static void AddElements(
        OdfNode node,
        string namespaceName,
        string localName,
        List<OdfNode> result)
    {
        if (node.Kind == OdfNodeKind.Element
            && node.Name?.NamespaceName == namespaceName
            && node.Name.LocalName == localName)
        {
            result.Add(node);
        }
        foreach (OdfNode child in node.Children)
        {
            AddElements(child, namespaceName, localName, result);
        }
    }

    private static OdfAttribute Attribute(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        return node.Attributes.Single(attribute =>
            attribute.Name.NamespaceName == namespaceName
            && attribute.Name.LocalName == localName);
    }

    private static OdfNode Element(
        string namespaceName,
        string localName,
        string xmlId)
    {
        OdfNode result = new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = Name(namespaceName, localName),
            XmlId = xmlId
        };
        result.Attributes =
        [
            new OdfAttribute
            {
                Name = Name(OdfNamespaces.Xml, "id"),
                Value = xmlId
            }
        ];
        return result;
    }

    private static OdfQualifiedName Name(
        string namespaceName,
        string localName)
    {
        string prefix = "";
        if (namespaceName == OdfNamespaces.Text)
        {
            prefix = OdfNamespaces.TextPrefix;
        }
        else if (namespaceName == OdfNamespaces.Xml)
        {
            prefix = OdfNamespaces.XmlPrefix;
        }
        return new OdfQualifiedName
        {
            NamespaceName = namespaceName,
            LocalName = localName,
            Prefix = prefix
        };
    }
}
