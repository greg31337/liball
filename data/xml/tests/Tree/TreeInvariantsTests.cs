using System;
using xml;

namespace xml.Tests;

[TestFixture]
public class TreeInvariantsTests
{
    [Test]
    public void AcceptsParserBuiltDocumentsAndFragments()
    {
        XmlParserOptions options = Options();
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.PreserveCData = true;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        XmlDocument document = new XmlParser().ParseXmlDocument(Input("<?pre data?><!DOCTYPE root><root a='v'>text<![CDATA[cdata]]><!--note--><?inside value?><child/></root>"), options, null, null, null);
        XmlDocument fragment = new XmlParser().ParseXmlFragment(Input("text<a/><b><c/></b>"), Options(), null, null, null);
        TreeInvariants invariants = new TreeInvariants();

        Assert.DoesNotThrow(() => invariants.ValidateXmlDocumentTree(document));
        Assert.DoesNotThrow(() => invariants.ValidateXmlDocumentTree(fragment));
    }

    [Test]
    public void RejectsIllegalChildKindsAndDocumentCardinalityOrOrder()
    {
        TreeInvariants invariants = new TreeInvariants();
        XmlNode root = Node(XmlNodeKind.Element, "root");
        XmlNode text = Node(XmlNodeKind.Text, null);
        XmlDocument textAtDocument = Document([text, root], 1, false, false);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(textAtDocument));

        XmlDocument twoRoots = Document([Node(XmlNodeKind.Element, "first"), Node(XmlNodeKind.Element, "second")], 0, false, false);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(twoRoots));

        XmlDocument noRoot = Document([Node(XmlNodeKind.Comment, null)], -1, false, false);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(noRoot));

        XmlDocument twoTypes = Document([Node(XmlNodeKind.DocumentType, "root"), Node(XmlNodeKind.DocumentType, "root"), Node(XmlNodeKind.Element, "root")], 2, true, false);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(twoTypes));

        XmlDocument lateType = Document([Node(XmlNodeKind.Element, "root"), Node(XmlNodeKind.DocumentType, "root")], 0, true, false);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(lateType));

        XmlDocument fragmentType = Document([Node(XmlNodeKind.DocumentType, "root")], -1, true, true);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(fragmentType));

        XmlNode rootWithAttributeChild = Node(XmlNodeKind.Element, "root");
        rootWithAttributeChild.Children = [Node(XmlNodeKind.Attribute, "a")];
        XmlDocument attributeChild = Document([rootWithAttributeChild], 0, false, false);
        OwnTree(attributeChild.DocumentNode!, attributeChild, null);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(attributeChild));

        Assert.That(invariants.IsXmlLegalChildKind(XmlNodeKind.Element, XmlNodeKind.Text), Is.True);
        Assert.That(invariants.IsXmlLegalChildKind(XmlNodeKind.Text, XmlNodeKind.Element), Is.False);
    }

    [Test]
    public void RejectsCyclesAndRepeatedNodeReferences()
    {
        TreeInvariants invariants = new TreeInvariants();
        XmlNode root = Node(XmlNodeKind.Element, "root");
        XmlNode child = Node(XmlNodeKind.Element, "child");
        root.Children = [child];
        XmlDocument document = Document([root], 0, false, false);
        OwnTree(document.DocumentNode!, document, null);

        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlChildPlacement(child, root));

        XmlNode shared = Node(XmlNodeKind.Text, null);
        shared.Value = "value";
        root.Children = [shared, shared];
        OwnTree(document.DocumentNode!, document, null);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(document));

        root.Children = [child];
        child.Children = [root];
        root.Parent = child;
        child.Parent = root;
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(document));
    }

    [Test]
    public void EnforcesUniqueExpandedAttributeNames()
    {
        TreeInvariants invariants = new TreeInvariants();
        XmlNode root = Node(XmlNodeKind.Element, "root");
        XmlNode first = Attribute("p:id", "urn:first");
        XmlNode differentNamespace = Attribute("q:id", "urn:second");
        root.Attributes = [first, differentNamespace];
        XmlDocument valid = Document([root], 0, false, false);
        OwnTree(valid.DocumentNode!, valid, null);

        Assert.DoesNotThrow(() => invariants.ValidateXmlDocumentTree(valid));

        XmlNode duplicate = Attribute("other:id", "urn:first");
        root.Attributes = [first, duplicate];
        OwnTree(valid.DocumentNode!, valid, null);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(valid));

        root.Attributes = [first];
        OwnTree(valid.DocumentNode!, valid, null);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlAttributePlacement(root, duplicate, null));
        Assert.DoesNotThrow(() => invariants.ValidateXmlAttributePlacement(root, duplicate, first));

        XmlNode emptyNamespaceDuplicate = Attribute("id", "");
        XmlNode nullNamespace = Attribute("id", "");
        nullNamespace.Name!.NamespaceUri = null;
        root.Attributes = [nullNamespace, emptyNamespaceDuplicate];
        OwnTree(valid.DocumentNode!, valid, null);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(valid));

        XmlNode unnamed = Node(XmlNodeKind.Attribute, null);
        root.Attributes = [unnamed];
        OwnTree(valid.DocumentNode!, valid, null);
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(valid));
    }

    [Test]
    public void RejectsOwnerAndParentMismatches()
    {
        TreeInvariants invariants = new TreeInvariants();
        XmlNode root = Node(XmlNodeKind.Element, "root");
        XmlNode child = Node(XmlNodeKind.Element, "child");
        root.Children = [child];
        XmlDocument document = Document([root], 0, false, false);
        OwnTree(document.DocumentNode!, document, null);

        child.Parent = document.DocumentNode;
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(document));

        OwnTree(document.DocumentNode!, document, null);
        child.OwnerDocument = new XmlDocument();
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlDocumentTree(document));

        OwnTree(document.DocumentNode!, document, null);
        XmlNode foreign = Node(XmlNodeKind.Element, "foreign");
        foreign.OwnerDocument = new XmlDocument();
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlChildPlacement(root, foreign));

        XmlNode attribute = Attribute("id", "");
        attribute.OwnerDocument = foreign.OwnerDocument;
        Assert.Throws<ArgumentException>(() => invariants.ValidateXmlAttributePlacement(root, attribute, null));
    }

    [Test]
    public void FrozenNodesAncestorsAndDocumentsRejectMutation()
    {
        TreeInvariants invariants = new TreeInvariants();
        XmlNode root = Node(XmlNodeKind.Element, "root");
        XmlNode child = Node(XmlNodeKind.Element, "child");
        root.Children = [child];
        XmlDocument document = Document([root], 0, false, false);
        OwnTree(document.DocumentNode!, document, null);

        child.IsReadOnly = true;
        Assert.Throws<InvalidOperationException>(() => invariants.EnsureXmlTreeMutationAllowed(child));

        child.IsReadOnly = false;
        root.IsReadOnly = true;
        Assert.Throws<InvalidOperationException>(() => invariants.EnsureXmlTreeMutationAllowed(child));

        root.IsReadOnly = false;
        document.IsReadOnly = true;
        Assert.Throws<InvalidOperationException>(() => invariants.EnsureXmlTreeMutationAllowed(child));

        document.IsReadOnly = false;
        Assert.DoesNotThrow(() => invariants.EnsureXmlTreeMutationAllowed(child));
    }

    private static XmlDocument Document(XmlNode[] children, int elementIndex, bool hasDocumentType, bool fragment)
    {
        XmlDocument document = new XmlDocument();
        XmlNode container = Node(fragment ? XmlNodeKind.DocumentFragment : XmlNodeKind.Document, null);
        container.Children = children;
        document.DocumentNode = container;
        document.DocumentElement = elementIndex < 0 ? null : children[elementIndex];
        document.DocumentType = hasDocumentType ? new XmlDocumentType { Name = Name("root") } : null;
        OwnTree(container, document, null);
        return document;
    }

    private static XmlNode Node(XmlNodeKind kind, string? name)
    {
        return new XmlNode
        {
            Kind = kind,
            Name = name == null ? null : Name(name),
            Children = []
        };
    }

    private static XmlNode Attribute(string qualifiedName, string namespaceUri)
    {
        XmlNode attribute = Node(XmlNodeKind.Attribute, qualifiedName);
        attribute.Name!.NamespaceUri = namespaceUri;
        return attribute;
    }

    private static XmlName Name(string qualifiedName)
    {
        int colon = qualifiedName.IndexOf(':');
        return new XmlName
        {
            Prefix = colon < 0 ? null : qualifiedName.Substring(0, colon),
            LocalName = colon < 0 ? qualifiedName : qualifiedName.Substring(colon + 1),
            QualifiedName = qualifiedName
        };
    }

    private static void OwnTree(XmlNode node, XmlDocument document, XmlNode? parent)
    {
        node.OwnerDocument = document;
        node.Parent = parent;
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            OwnTree(attributes[attributeIndex], document, node);
            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            OwnTree(children[childIndex], document, node);
            childIndex = childIndex + 1;
        }
    }

    private static XmlInput Input(string text)
    {
        return new XmlInput { Kind = XmlInputKind.String, Text = text };
    }

    private static XmlParserOptions Options()
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Disabled,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
    }
}
