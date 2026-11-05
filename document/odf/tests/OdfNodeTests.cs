namespace odf.Tests;

public class OdfNodeTests
{
    [Test]
    public void DefaultsRepresentAnUnattachedEmptyNode()
    {
        OdfNode node = new OdfNode();

        Assert.Multiple(() =>
        {
            Assert.That(node.Name, Is.Null);
            Assert.That(node.Parent, Is.Null);
            Assert.That(node.Kind, Is.EqualTo(OdfNodeKind.Unknown));
            Assert.That(node.NamespaceDeclarations, Is.Empty);
            Assert.That(node.Attributes, Is.Empty);
            Assert.That(node.Children, Is.Empty);
            Assert.That(node.Value, Is.Empty);
            Assert.That(node.XmlId, Is.Empty);
            Assert.That(node.Location, Is.Not.Null);
            Assert.That(node.IsSignificantWhitespace, Is.False);
            Assert.That(node.WasEmptyElement, Is.False);
        });
    }

    [Test]
    public void ElementRetainsNamespaceAwareAttributesAndOrderedMixedContent()
    {
        OdfNode root = Element("urn:example:document", "root", "doc");
        OdfNode leadingText = Content(OdfNodeKind.Text, " before ");
        OdfNode childElement = Element("urn:example:text", "span", "text");
        OdfNode childText = Content(OdfNodeKind.Text, "inside");
        OdfNode comment = Content(OdfNodeKind.Comment, "preserve this");
        OdfNode instruction = Content(OdfNodeKind.ProcessingInstruction, " mode = 'exact' ");
        OdfNode trailingText = Content(OdfNodeKind.Text, " after ");

        instruction.Name = new OdfQualifiedName
        {
            LocalName = "odf-processing"
        };
        root.NamespaceDeclarations =
        [
            new OdfNamespaceDeclaration
            {
                Prefix = "doc",
                NamespaceName = "urn:example:document"
            }
        ];
        root.Attributes =
        [
            new OdfAttribute
            {
                Name = new OdfQualifiedName
                {
                    LocalName = "id"
                },
                Value = "root-1"
            }
        ];
        childElement.Children = [childText];
        childText.Parent = childElement;
        root.Children = [leadingText, childElement, comment, instruction, trailingText];
        foreach (OdfNode child in root.Children)
        {
            child.Parent = root;
        }

        Assert.Multiple(() =>
        {
            Assert.That(root.Name?.NamespaceName, Is.EqualTo("urn:example:document"));
            Assert.That(root.Name?.LocalName, Is.EqualTo("root"));
            Assert.That(root.Name?.Prefix, Is.EqualTo("doc"));
            Assert.That(root.NamespaceDeclarations[0].Prefix, Is.EqualTo("doc"));
            Assert.That(root.Attributes[0].Value, Is.EqualTo("root-1"));
            Assert.That(root.Children, Is.EqualTo(new[] { leadingText, childElement, comment, instruction, trailingText }));
            Assert.That(root.Children.Select(node => node.Kind), Is.EqualTo(new[]
            {
                OdfNodeKind.Text,
                OdfNodeKind.Element,
                OdfNodeKind.Comment,
                OdfNodeKind.ProcessingInstruction,
                OdfNodeKind.Text
            }));
            Assert.That(root.Children.All(node => node.Parent == root), Is.True);
            Assert.That(childText.Parent, Is.SameAs(childElement));
            Assert.That(leadingText.Value, Is.EqualTo(" before "));
            Assert.That(childText.Value, Is.EqualTo("inside"));
            Assert.That(comment.Value, Is.EqualTo("preserve this"));
            Assert.That(instruction.Name?.LocalName, Is.EqualTo("odf-processing"));
            Assert.That(instruction.Value, Is.EqualTo(" mode = 'exact' "));
            Assert.That(trailingText.Value, Is.EqualTo(" after "));
        });
    }

    [Test]
    public void DocumentNodesRetainTopLevelProcessingInstructionCommentAndElementOrder()
    {
        OdfNode instruction = Content(OdfNodeKind.ProcessingInstruction, "version='1.0'");
        instruction.Name = new OdfQualifiedName
        {
            LocalName = "document"
        };
        OdfNode comment = Content(OdfNodeKind.Comment, "top-level");
        OdfNode root = Element("urn:example", "document", "e");
        OdfDocument document = new OdfDocument
        {
            Nodes = [instruction, comment, root]
        };

        Assert.Multiple(() =>
        {
            Assert.That(document.Nodes, Is.EqualTo(new[] { instruction, comment, root }));
            Assert.That(document.Nodes[0].Kind, Is.EqualTo(OdfNodeKind.ProcessingInstruction));
            Assert.That(document.Nodes[1].Kind, Is.EqualTo(OdfNodeKind.Comment));
            Assert.That(document.Nodes[2].Kind, Is.EqualTo(OdfNodeKind.Element));
            Assert.That(document.Nodes.All(node => node.Parent == null), Is.True);
        });
    }

    private static OdfNode Element(string namespaceName, string localName, string prefix)
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

    private static OdfNode Content(OdfNodeKind kind, string value)
    {
        return new OdfNode
        {
            Kind = kind,
            Value = value
        };
    }
}
