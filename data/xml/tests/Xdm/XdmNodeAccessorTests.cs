using xml;

namespace xml.Tests;

[TestFixture]
public class XdmNodeAccessorTests
{
    [Test]
    public void MapsExactlyTheSevenXdmNodeKindsFromXmlRepresentations()
    {
        XdmNodeAccessor accessor = new XdmNodeAccessor();

        Assert.Multiple(() =>
        {
            Assert.That(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Document }).Kind, Is.EqualTo(XdmNodeKind.Document));
            Assert.That(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.DocumentFragment }).Kind, Is.EqualTo(XdmNodeKind.Document));
            Assert.That(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Element }).Kind, Is.EqualTo(XdmNodeKind.Element));
            Assert.That(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Attribute }).Kind, Is.EqualTo(XdmNodeKind.Attribute));
            Assert.That(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Namespace }).Kind, Is.EqualTo(XdmNodeKind.Namespace));
            Assert.That(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.ProcessingInstruction }).Kind, Is.EqualTo(XdmNodeKind.ProcessingInstruction));
            Assert.That(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Comment }).Kind, Is.EqualTo(XdmNodeKind.Comment));
            Assert.That(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Text }).Kind, Is.EqualTo(XdmNodeKind.Text));
            Assert.That(accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.CData }).Kind, Is.EqualTo(XdmNodeKind.Text));
            Assert.Throws<ArgumentException>(() => accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.EntityReference }));
            Assert.Throws<ArgumentException>(() => accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.DocumentType }));
        });
    }

    [Test]
    public void ProjectsInfosetChildrenAttributesAndNamespacesIntoXdmNodes()
    {
        XmlDocument document = new XmlDocument { BaseUri = "https://example.test/base/", DocumentUri = "https://example.test/doc.xml" };
        XmlNode documentNode = new XmlNode { Kind = XmlNodeKind.Document, OwnerDocument = document };
        XmlNode root = new XmlNode { Kind = XmlNodeKind.Element, Name = Name("root"), Parent = documentNode, OwnerDocument = document };
        XmlNode documentType = new XmlNode { Kind = XmlNodeKind.DocumentType, Parent = documentNode, OwnerDocument = document };
        XmlNode namespaceAttribute = new XmlNode { Kind = XmlNodeKind.Attribute, Name = ExpandedName("xmlns", "p", XmlNamespaces.Xmlns), Value = "urn:p", Parent = root, OwnerDocument = document };
        XmlNode attribute = new XmlNode { Kind = XmlNodeKind.Attribute, Name = Name("id"), Value = "7", Parent = root, OwnerDocument = document };
        XmlNode firstText = new XmlNode { Kind = XmlNodeKind.Text, Value = "A", Parent = root, OwnerDocument = document };
        XmlNode cdata = new XmlNode { Kind = XmlNodeKind.CData, Value = "B", Parent = root, OwnerDocument = document };
        XmlNode entity = new XmlNode { Kind = XmlNodeKind.EntityReference, Name = Name("e"), Parent = root, OwnerDocument = document };
        XmlNode entityText = new XmlNode { Kind = XmlNodeKind.Text, Value = "C", Parent = entity, OwnerDocument = document };
        XmlNode comment = new XmlNode { Kind = XmlNodeKind.Comment, Value = "note", Parent = root, OwnerDocument = document };
        XmlNode emptyText = new XmlNode { Kind = XmlNodeKind.Text, Value = "", Parent = root, OwnerDocument = document };
        XmlNode child = new XmlNode { Kind = XmlNodeKind.Element, Name = Name("child"), Parent = root, OwnerDocument = document };
        document.DocumentNode = documentNode;
        document.DocumentElement = root;
        documentNode.Children = [documentType, root];
        root.Attributes = [namespaceAttribute, attribute];
        root.NamespaceBindings = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:p" }, new XmlNamespaceBinding { Prefix = "", NamespaceUri = "urn:default" }];
        root.Children = [firstText, cdata, entity, comment, emptyText, child];
        entity.Children = [entityText];

        XdmNodeAccessor accessor = new XdmNodeAccessor();
        XdmNode wrappedDocument = accessor.WrapXmlNode(documentNode);
        XdmNode wrappedRoot = accessor.WrapXmlNode(root);
        XdmNode[] documentChildren = accessor.GetXdmNodeChildren(wrappedDocument);
        XdmNode[] children = accessor.GetXdmNodeChildren(wrappedRoot);
        XdmNode[] attributes = accessor.GetXdmNodeAttributes(wrappedRoot);
        XdmNode[] namespaces = accessor.GetXdmNodeNamespaces(wrappedRoot);

        Assert.Multiple(() =>
        {
            Assert.That(documentChildren, Has.Length.EqualTo(1));
            Assert.That(documentChildren[0].Node, Is.SameAs(root));
            Assert.That(children.Select(value => value.Kind), Is.EqualTo(new[] { XdmNodeKind.Text, XdmNodeKind.Comment, XdmNodeKind.Element }));
            Assert.That(children[0].Node, Is.SameAs(firstText));
            Assert.That(accessor.GetXdmNodeStringValue(children[0]), Is.EqualTo("ABC"));
            Assert.That(accessor.GetXdmNodeParent(children[0]), Is.SameAs(wrappedRoot));
            Assert.That(accessor.GetXdmNodeParent(children[1]), Is.SameAs(wrappedRoot));
            Assert.That(attributes, Has.Length.EqualTo(1));
            Assert.That(attributes[0].Node, Is.SameAs(attribute));
            Assert.That(accessor.GetXdmNodeParent(attributes[0]), Is.SameAs(wrappedRoot));
            Assert.That(namespaces, Has.Length.EqualTo(3));
            Assert.That(namespaces.Select(value => accessor.GetXdmNodeStringValue(value)), Is.EquivalentTo(new[] { "urn:p", "urn:default", XmlNamespaces.Xml }));
            Assert.That(namespaces.All(value => accessor.GetXdmNodeParent(value) == wrappedRoot), Is.True);
            Assert.That(accessor.GetXdmNodeName(namespaces.Single(value => accessor.GetXdmNodeStringValue(value) == "urn:default")), Is.Null);
        });

        XdmNode[] repeated = accessor.GetXdmNodeNamespaces(wrappedRoot);
        namespaces[0] = wrappedRoot;
        Assert.Multiple(() =>
        {
            Assert.That(repeated[0].Kind, Is.EqualTo(XdmNodeKind.Namespace));
            Assert.That(accessor.HasSameXdmNodeIdentity(repeated[0], accessor.GetXdmNodeNamespaces(wrappedRoot)[0]), Is.True);
        });
    }

    [Test]
    public void PreservesIdentityAndProvidesStableTotalDocumentOrder()
    {
        XmlDocument document = new XmlDocument();
        XmlNode documentNode = new XmlNode { Kind = XmlNodeKind.Document, OwnerDocument = document };
        XmlNode root = new XmlNode { Kind = XmlNodeKind.Element, Name = Name("root"), Parent = documentNode, OwnerDocument = document };
        XmlNode attribute = new XmlNode { Kind = XmlNodeKind.Attribute, Name = Name("a"), Parent = root, OwnerDocument = document };
        XmlNode text = new XmlNode { Kind = XmlNodeKind.Text, Value = "text", Parent = root, OwnerDocument = document };
        XmlNode child = new XmlNode { Kind = XmlNodeKind.Element, Name = Name("child"), Parent = root, OwnerDocument = document };
        document.DocumentNode = documentNode;
        document.DocumentElement = root;
        documentNode.Children = [root];
        root.Attributes = [attribute];
        root.NamespaceBindings = [new XmlNamespaceBinding { Prefix = "p", NamespaceUri = "urn:p" }];
        root.Children = [text, child];
        XdmNodeAccessor accessor = new XdmNodeAccessor();
        XdmNode wrappedRoot = accessor.WrapXmlNode(root);
        XdmNode namespaceNode = accessor.GetXdmNodeNamespaces(wrappedRoot).Single(value => accessor.GetXdmNodeStringValue(value) == "urn:p");
        XdmNode wrappedAttribute = accessor.WrapXmlNode(attribute);
        XdmNode wrappedText = accessor.WrapXmlNode(text);
        XdmNode wrappedChild = accessor.WrapXmlNode(child);

        Assert.Multiple(() =>
        {
            Assert.That(accessor.WrapXmlNode(root), Is.SameAs(wrappedRoot));
            Assert.That(accessor.HasSameXdmNodeIdentity(wrappedRoot, accessor.WrapXmlNode(root)), Is.True);
            Assert.That(accessor.HasSameXdmNodeIdentity(wrappedRoot, wrappedChild), Is.False);
            Assert.That(accessor.CompareXdmNodeDocumentOrder(wrappedRoot, namespaceNode), Is.LessThan(0));
            Assert.That(accessor.CompareXdmNodeDocumentOrder(namespaceNode, wrappedAttribute), Is.LessThan(0));
            Assert.That(accessor.CompareXdmNodeDocumentOrder(wrappedAttribute, wrappedText), Is.LessThan(0));
            Assert.That(accessor.CompareXdmNodeDocumentOrder(wrappedText, wrappedChild), Is.LessThan(0));
            Assert.That(accessor.CompareXdmNodeDocumentOrder(wrappedChild, wrappedRoot), Is.GreaterThan(0));
            Assert.That(accessor.CompareXdmNodeDocumentOrder(wrappedChild, wrappedChild), Is.Zero);
        });

        XdmNode detachedOne = accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Comment });
        XdmNode detachedTwo = accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Comment });
        int detachedOrder = accessor.CompareXdmNodeDocumentOrder(detachedOne, detachedTwo);
        Assert.Multiple(() =>
        {
            Assert.That(detachedOrder, Is.Not.Zero);
            Assert.That(accessor.CompareXdmNodeDocumentOrder(detachedTwo, detachedOne), Is.EqualTo(-detachedOrder));
        });
    }

    [Test]
    public void ExposesNamesStringAndTypedValuesForAllSevenKinds()
    {
        XmlDocument document = new XmlDocument { BaseUri = "https://example.test/base/", DocumentUri = "https://example.test/doc.xml" };
        XmlNode documentNode = new XmlNode { Kind = XmlNodeKind.Document, OwnerDocument = document };
        XmlNode element = new XmlNode { Kind = XmlNodeKind.Element, Name = ExpandedName("p", "item", "urn:p"), Parent = documentNode, OwnerDocument = document };
        XmlNode attribute = new XmlNode { Kind = XmlNodeKind.Attribute, Name = Name("count"), Value = "0042", Parent = element, OwnerDocument = document };
        XmlNode text = new XmlNode { Kind = XmlNodeKind.Text, Value = "content", Parent = element, OwnerDocument = document };
        XmlNode processingInstruction = new XmlNode { Kind = XmlNodeKind.ProcessingInstruction, Name = Name("target"), Value = "data", Parent = element, OwnerDocument = document };
        XmlNode comment = new XmlNode { Kind = XmlNodeKind.Comment, Value = "note", Parent = element, OwnerDocument = document };
        XmlNode namespaceNode = new XmlNode { Kind = XmlNodeKind.Namespace, Name = Name("p"), Value = "urn:p", Parent = element, OwnerDocument = document };
        document.DocumentNode = documentNode;
        document.DocumentElement = element;
        documentNode.Children = [element];
        element.Attributes = [attribute];
        element.Children = [text, processingInstruction, comment];
        XdmNodeAccessor accessor = new XdmNodeAccessor();
        XdmNode[] nodes =
        [
            accessor.WrapXmlNode(documentNode),
            accessor.WrapXmlNode(element),
            accessor.WrapXmlNode(attribute),
            accessor.WrapXmlNode(namespaceNode),
            accessor.WrapXmlNode(processingInstruction),
            accessor.WrapXmlNode(comment),
            accessor.WrapXmlNode(text)
        ];
        XdmAtomicType[] types = nodes.Select(value => accessor.GetXdmNodeTypedValue(value).AtomicValues![0].Type).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(types, Is.EqualTo(new[] { XdmAtomicType.UntypedAtomic, XdmAtomicType.UntypedAtomic, XdmAtomicType.UntypedAtomic, XdmAtomicType.String, XdmAtomicType.String, XdmAtomicType.String, XdmAtomicType.UntypedAtomic }));
            Assert.That(accessor.GetXdmNodeStringValue(nodes[0]), Is.EqualTo("content"));
            Assert.That(accessor.GetXdmNodeStringValue(nodes[1]), Is.EqualTo("content"));
            Assert.That(accessor.GetXdmNodeStringValue(nodes[2]), Is.EqualTo("0042"));
            Assert.That(accessor.GetXdmNodeStringValue(nodes[3]), Is.EqualTo("urn:p"));
            Assert.That(accessor.GetXdmNodeName(nodes[0]), Is.Null);
            Assert.That(accessor.GetXdmNodeName(nodes[1])!.QualifiedName, Is.EqualTo("p:item"));
            Assert.That(accessor.GetXdmNodeName(nodes[2])!.LocalName, Is.EqualTo("count"));
            Assert.That(accessor.GetXdmNodeName(nodes[3])!.LocalName, Is.EqualTo("p"));
            Assert.That(accessor.GetXdmNodeName(nodes[4])!.LocalName, Is.EqualTo("target"));
            Assert.That(accessor.GetXdmNodeName(nodes[5]), Is.Null);
            Assert.That(accessor.GetXdmNodeName(nodes[6]), Is.Null);
            Assert.That(accessor.GetXdmNodeTypeName(nodes[0]), Is.Null);
            Assert.That(accessor.GetXdmNodeTypeName(nodes[1])!.LocalName, Is.EqualTo("untyped"));
            Assert.That(accessor.GetXdmNodeTypeName(nodes[2])!.LocalName, Is.EqualTo("untypedAtomic"));
            Assert.That(accessor.GetXdmNodeTypeName(nodes[3]), Is.Null);
            Assert.That(accessor.GetXdmNodeTypeName(nodes[4]), Is.Null);
            Assert.That(accessor.GetXdmNodeTypeName(nodes[5]), Is.Null);
            Assert.That(accessor.GetXdmNodeTypeName(nodes[6])!.LocalName, Is.EqualTo("untypedAtomic"));
        });

        XmlName detached = accessor.GetXdmNodeName(nodes[1])!;
        detached.LocalName = "changed";
        Assert.That(accessor.GetXdmNodeName(nodes[1])!.LocalName, Is.EqualTo("item"));
    }

    [Test]
    public void UsesSchemaAnnotationsForTypedValuesNilledAndIdProperties()
    {
        XmlName typeName = ExpandedName("t", "answer", "urn:types");
        XdmAtomicValue actual = new XdmAtomicValue { Type = XdmAtomicType.Integer, IntegerValue = 42, StringValue = "42" };
        XmlSchemaTypeInfo annotation = new XmlSchemaTypeInfo
        {
            TypeName = typeName,
            AtomicType = XdmAtomicType.Integer,
            NormalizedValue = "42",
            ActualValue = actual,
            IsId = true,
            IsIdReference = true
        };
        XmlNode element = new XmlNode { Kind = XmlNodeKind.Element, Name = Name("value"), TypeInfo = annotation, Children = [new XmlNode { Kind = XmlNodeKind.Text, Value = "0042" }] };
        element.Children[0].Parent = element;
        XdmNodeAccessor accessor = new XdmNodeAccessor();
        XdmNode node = accessor.WrapXmlNode(element);

        Assert.Multiple(() =>
        {
            Assert.That(accessor.GetXdmNodeTypeAnnotation(node), Is.SameAs(annotation));
            Assert.That(accessor.GetXdmNodeTypeName(node)!.QualifiedName, Is.EqualTo("t:answer"));
            Assert.That(accessor.GetXdmNodeStringValue(node), Is.EqualTo("42"));
            Assert.That(accessor.GetXdmNodeTypedValue(node).AtomicValues![0], Is.SameAs(actual));
            Assert.That(accessor.GetXdmNodeNilled(node).AtomicValues![0].BooleanValue, Is.False);
            Assert.That(accessor.GetXdmNodeIsId(node).AtomicValues![0].BooleanValue, Is.True);
            Assert.That(accessor.GetXdmNodeIsIdReference(node).AtomicValues![0].BooleanValue, Is.True);
        });

        annotation.IsNilled = true;
        Assert.That(accessor.GetXdmNodeNilled(node).AtomicValues![0].BooleanValue, Is.True);
        Assert.That(accessor.GetXdmNodeTypedValue(node).Count, Is.Zero);
        XdmNode comment = accessor.WrapXmlNode(new XmlNode { Kind = XmlNodeKind.Comment });
        Assert.Multiple(() =>
        {
            Assert.That(accessor.GetXdmNodeNilled(comment).Count, Is.Zero);
            Assert.That(accessor.GetXdmNodeIsId(comment).Count, Is.Zero);
            Assert.That(accessor.GetXdmNodeIsIdReference(comment).Count, Is.Zero);
        });
    }

    [Test]
    public void AppliesBaseAndDocumentUriRulesToEveryNodeCategory()
    {
        XmlDocument document = new XmlDocument { BaseUri = "https://example.test/base/", DocumentUri = "https://example.test/doc.xml" };
        XmlNode documentNode = new XmlNode { Kind = XmlNodeKind.Document, OwnerDocument = document };
        XmlNode element = new XmlNode { Kind = XmlNodeKind.Element, Name = Name("root"), Parent = documentNode, OwnerDocument = document };
        XmlNode attribute = new XmlNode { Kind = XmlNodeKind.Attribute, Name = Name("a"), Parent = element, OwnerDocument = document };
        XmlNode text = new XmlNode { Kind = XmlNodeKind.Text, Parent = element, OwnerDocument = document };
        XmlNode comment = new XmlNode { Kind = XmlNodeKind.Comment, Parent = element, OwnerDocument = document };
        XmlNode processingInstruction = new XmlNode { Kind = XmlNodeKind.ProcessingInstruction, Name = Name("pi"), Parent = element, OwnerDocument = document };
        XmlNode namespaceNode = new XmlNode { Kind = XmlNodeKind.Namespace, Name = Name("p"), Parent = element, OwnerDocument = document };
        document.DocumentNode = documentNode;
        document.DocumentElement = element;
        documentNode.Children = [element];
        element.Attributes = [attribute];
        element.Children = [text, comment, processingInstruction];
        XdmNodeAccessor accessor = new XdmNodeAccessor();
        XdmNode wrappedDocument = accessor.WrapXmlNode(documentNode);

        Assert.Multiple(() =>
        {
            Assert.That(accessor.GetXdmNodeBaseUri(wrappedDocument), Is.EqualTo(document.BaseUri));
            Assert.That(accessor.GetXdmNodeBaseUri(accessor.WrapXmlNode(element)), Is.EqualTo(document.BaseUri));
            Assert.That(accessor.GetXdmNodeBaseUri(accessor.WrapXmlNode(attribute)), Is.EqualTo(document.BaseUri));
            Assert.That(accessor.GetXdmNodeBaseUri(accessor.WrapXmlNode(text)), Is.EqualTo(document.BaseUri));
            Assert.That(accessor.GetXdmNodeBaseUri(accessor.WrapXmlNode(comment)), Is.EqualTo(document.BaseUri));
            Assert.That(accessor.GetXdmNodeBaseUri(accessor.WrapXmlNode(processingInstruction)), Is.EqualTo(document.BaseUri));
            Assert.That(accessor.GetXdmNodeBaseUri(accessor.WrapXmlNode(namespaceNode)), Is.Null);
            Assert.That(accessor.GetXdmNodeDocumentUri(wrappedDocument), Is.EqualTo(document.DocumentUri));
            Assert.That(accessor.GetXdmNodeDocumentUri(accessor.WrapXmlNode(element)), Is.Null);
            Assert.That(accessor.GetXdmNodeParent(wrappedDocument), Is.Null);
            Assert.That(accessor.GetXdmNodeChildren(accessor.WrapXmlNode(attribute)), Is.Empty);
            Assert.That(accessor.GetXdmNodeAttributes(accessor.WrapXmlNode(text)), Is.Empty);
            Assert.That(accessor.GetXdmNodeNamespaces(accessor.WrapXmlNode(comment)), Is.Empty);
        });

        XmlNode detachedText = new XmlNode { Kind = XmlNodeKind.Text, OwnerDocument = document };
        Assert.That(accessor.GetXdmNodeBaseUri(accessor.WrapXmlNode(detachedText)), Is.Null);
    }

    private static XmlName Name(string localName)
    {
        return ExpandedName("", localName, "");
    }

    private static XmlName ExpandedName(string prefix, string localName, string namespaceUri)
    {
        return new XmlName
        {
            Prefix = prefix,
            LocalName = localName,
            NamespaceUri = namespaceUri,
            QualifiedName = prefix.Length == 0 ? localName : prefix + ":" + localName
        };
    }
}
