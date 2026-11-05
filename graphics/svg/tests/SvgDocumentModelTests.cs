namespace svg.Tests;

public class SvgDocumentModelTests
{
    [Test]
    public void DocumentCreatesAConnectedCleanTaggedDocumentNode()
    {
        SvgDocument document = new SvgDocument();

        Assert.Multiple(() =>
        {
            Assert.That(document.BaseUri, Is.Empty);
            Assert.That(document.ContentType, Is.Empty);
            Assert.That(document.ProcessingMode, Is.EqualTo(SvgProcessingMode.SecureStatic));
            Assert.That(document.IsFragment, Is.False);
            Assert.That(document.Version, Is.Zero);
            Assert.That(document.IsDirty, Is.False);
            Assert.That(document.DocumentNode.Kind, Is.EqualTo(SvgNodeKind.Document));
            Assert.That(document.DocumentNode.OwnerDocument, Is.SameAs(document));
            Assert.That(document.DocumentNode.Parent, Is.Null);
            Assert.That(document.DocumentNode.Children, Is.Empty);
            Assert.That(document.DocumentNode.Attributes, Is.Empty);
            Assert.That(document.DocumentElement, Is.Null);
            Assert.That(document.Source, Is.Null);
            Assert.That(document.SourceText, Is.Null);
            Assert.That(document.SourceSpan, Is.Null);
            Assert.That(document.Diagnostics, Is.Empty);
        });
    }

    [TestCase(SvgNodeKind.Document)]
    [TestCase(SvgNodeKind.Element)]
    [TestCase(SvgNodeKind.Text)]
    [TestCase(SvgNodeKind.CData)]
    [TestCase(SvgNodeKind.Comment)]
    [TestCase(SvgNodeKind.ProcessingInstruction)]
    [TestCase(SvgNodeKind.Doctype)]
    public void NodesRetainEveryTagAndInitializeIndependentOrderedCollections(SvgNodeKind kind)
    {
        SvgNode node = new SvgNode(kind, "p:name", "p", "name", "urn:test", "value");
        SvgNode other = new SvgNode(kind, "", "", "", "", "");
        node.Children.Add(new SvgNode(SvgNodeKind.Text, "", "", "", "", "child"));
        node.Attributes.Add(new SvgAttribute("a", "", "a", "", "1"));

        Assert.Multiple(() =>
        {
            Assert.That(node.Kind, Is.EqualTo(kind));
            Assert.That(node.ElementKind, Is.EqualTo(SvgElementKind.Unknown));
            Assert.That(node.QualifiedName, Is.EqualTo("p:name"));
            Assert.That(node.Prefix, Is.EqualTo("p"));
            Assert.That(node.LocalName, Is.EqualTo("name"));
            Assert.That(node.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(node.Value, Is.EqualTo("value"));
            Assert.That(node.Parent, Is.Null);
            Assert.That(node.OwnerDocument, Is.Null);
            Assert.That(node.IsDirty, Is.False);
            Assert.That(node.Version, Is.Zero);
            Assert.That(other.Children, Is.Empty);
            Assert.That(other.Attributes, Is.Empty);
        });
    }

    [Test]
    public void InitialConstructionPreservesChildAndAttributeOrderAndEstablishesLinks()
    {
        SvgDocument document = new SvgDocument();
        SvgNode comment = new SvgNode(SvgNodeKind.Comment, "", "", "", "", "before");
        SvgNode root = new SvgNode(SvgNodeKind.Element, "svg", "", "svg", SvgNamespaces.Svg, "");
        SvgNode firstChild = new SvgNode(SvgNodeKind.Element, "g", "", "g", SvgNamespaces.Svg, "");
        SvgNode secondChild = new SvgNode(SvgNodeKind.Text, "", "", "", "", "text");
        SvgAttribute firstAttribute = new SvgAttribute("id", "", "id", "", "one");
        SvgAttribute secondAttribute = new SvgAttribute("xml:lang", "xml", "lang", SvgNamespaces.Xml, "en");

        document.AttachInitialChild(document.DocumentNode, comment);
        document.AttachInitialChild(document.DocumentNode, root);
        document.AttachInitialChild(root, firstChild);
        document.AttachInitialChild(root, secondChild);
        document.AttachInitialAttribute(root, firstAttribute);
        document.AttachInitialAttribute(root, secondAttribute);

        Assert.Multiple(() =>
        {
            Assert.That(document.DocumentNode.Children, Is.EqualTo(new[] { comment, root }));
            Assert.That(document.DocumentElement, Is.SameAs(root));
            Assert.That(root.ElementKind, Is.EqualTo(SvgElementKind.Svg));
            Assert.That(root.Children, Is.EqualTo(new[] { firstChild, secondChild }));
            Assert.That(root.Attributes, Is.EqualTo(new[] { firstAttribute, secondAttribute }));
            Assert.That(comment.Parent, Is.SameAs(document.DocumentNode));
            Assert.That(root.Parent, Is.SameAs(document.DocumentNode));
            Assert.That(firstChild.Parent, Is.SameAs(root));
            Assert.That(secondChild.Parent, Is.SameAs(root));
            Assert.That(firstAttribute.OwnerElement, Is.SameAs(root));
            Assert.That(firstAttribute.AttributeKind, Is.EqualTo(SvgAttributeKind.Svg2));
            Assert.That(secondAttribute.AttributeKind, Is.EqualTo(SvgAttributeKind.Xml));
            Assert.That(secondAttribute.OwnerElement, Is.SameAs(root));
            Assert.That(new[] { comment.OwnerDocument, root.OwnerDocument, firstChild.OwnerDocument, secondChild.OwnerDocument }, Has.All.SameAs(document));
            Assert.That(new[] { firstAttribute.OwnerDocument, secondAttribute.OwnerDocument }, Has.All.SameAs(document));
            Assert.That(document.Version, Is.Zero);
            Assert.That(document.IsDirty, Is.False);
            Assert.That(root.IsDirty, Is.False);
            Assert.That(firstAttribute.IsDirty, Is.False);
        });
    }

    [Test]
    public void NodesAndAttributesRetainExactRawTokensAndSourceSpans()
    {
        string input = "<svg id='one'></svg>";
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
        SvgXmlToken start = loaded.XmlTokens[0];
        SvgXmlToken end = loaded.XmlTokens[1];
        SvgXmlAttributeToken rawAttribute = start.Attributes[0];
        SvgSourceUri source = new SvgSourceUri("memory:model");
        SvgSourceSpan nodeSpan = new SvgSourceSpan(source, start.StartOffset, start.StartLine, start.StartColumn, end.EndOffset, end.EndLine, end.EndColumn);
        SvgSourceSpan attributeSpan = new SvgSourceSpan(source, rawAttribute.StartOffset, rawAttribute.StartLine, rawAttribute.StartColumn, rawAttribute.EndOffset, rawAttribute.EndLine, rawAttribute.EndColumn);
        SvgNode node = new SvgNode(SvgNodeKind.Element, start.QualifiedName, start.Prefix, start.LocalName, start.NamespaceUri, "");
        SvgAttribute attribute = new SvgAttribute(rawAttribute.QualifiedName, rawAttribute.Prefix, rawAttribute.LocalName, rawAttribute.NamespaceUri, rawAttribute.NormalizedValue);
        node.RawToken = start;
        node.EndToken = end;
        node.SourceSpan = nodeSpan;
        attribute.RawToken = rawAttribute;
        attribute.SourceSpan = attributeSpan;

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Success, Is.True);
            Assert.That(node.RawToken, Is.SameAs(start));
            Assert.That(node.EndToken, Is.SameAs(end));
            Assert.That(node.SourceSpan, Is.SameAs(nodeSpan));
            Assert.That(node.SourceSpan.StartOffset, Is.Zero);
            Assert.That(node.SourceSpan.EndOffset, Is.EqualTo(input.Length));
            Assert.That(attribute.RawToken, Is.SameAs(rawAttribute));
            Assert.That(attribute.SourceSpan, Is.SameAs(attributeSpan));
            Assert.That(attribute.SourceSpan.StartOffset, Is.EqualTo(input.IndexOf("id", StringComparison.Ordinal)));
            Assert.That(attribute.Value, Is.EqualTo("one"));
        });
    }

    [Test]
    public void DirtyChangesUseOneMonotonicDocumentVersionAndMarkAffectedOwners()
    {
        SvgDocument document = new SvgDocument();
        SvgNode root = new SvgNode(SvgNodeKind.Element, "svg", "", "svg", SvgNamespaces.Svg, "");
        SvgNode child = new SvgNode(SvgNodeKind.Text, "", "", "", "", "text");
        SvgAttribute attribute = new SvgAttribute("id", "", "id", "", "one");
        document.AttachInitialChild(document.DocumentNode, root);
        document.AttachInitialChild(root, child);
        document.AttachInitialAttribute(root, attribute);

        long childVersion = child.MarkDirty();
        long attributeVersion = attribute.MarkDirty();
        long documentVersion = document.MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(childVersion, Is.EqualTo(1));
            Assert.That(attributeVersion, Is.EqualTo(2));
            Assert.That(documentVersion, Is.EqualTo(3));
            Assert.That(document.Version, Is.EqualTo(3));
            Assert.That(document.IsDirty, Is.True);
            Assert.That(document.DocumentNode.IsDirty, Is.True);
            Assert.That(document.DocumentNode.Version, Is.EqualTo(3));
            Assert.That(child.IsDirty, Is.True);
            Assert.That(child.Version, Is.EqualTo(1));
            Assert.That(attribute.IsDirty, Is.True);
            Assert.That(attribute.Version, Is.EqualTo(2));
            Assert.That(root.IsDirty, Is.True);
            Assert.That(root.Version, Is.EqualTo(2));
        });
    }

    [Test]
    public void DetachedNodesAndAttributesTrackIndependentVersions()
    {
        SvgNode node = new SvgNode(SvgNodeKind.Comment, "", "", "", "", "note");
        SvgAttribute attribute = new SvgAttribute("a", "", "a", "", "v");

        long nodeFirst = node.MarkDirty();
        long nodeSecond = node.MarkDirty();
        long attributeFirst = attribute.MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(nodeFirst, Is.EqualTo(1));
            Assert.That(nodeSecond, Is.EqualTo(2));
            Assert.That(node.Version, Is.EqualTo(2));
            Assert.That(node.IsDirty, Is.True);
            Assert.That(attributeFirst, Is.EqualTo(1));
            Assert.That(attribute.Version, Is.EqualTo(1));
            Assert.That(attribute.IsDirty, Is.True);
            Assert.That(node.OwnerDocument, Is.Null);
            Assert.That(attribute.OwnerDocument, Is.Null);
        });
    }

    [Test]
    public void InitialAttachmentRejectsInvalidOwnersAndAlreadyAttachedValues()
    {
        SvgDocument first = new SvgDocument();
        SvgDocument second = new SvgDocument();
        SvgNode root = new SvgNode(SvgNodeKind.Element, "svg", "", "svg", SvgNamespaces.Svg, "");
        SvgNode child = new SvgNode(SvgNodeKind.Text, "", "", "", "", "text");
        SvgAttribute attribute = new SvgAttribute("id", "", "id", "", "one");
        first.AttachInitialChild(first.DocumentNode, root);
        first.AttachInitialChild(root, child);
        first.AttachInitialAttribute(root, attribute);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => second.AttachInitialChild(root, new SvgNode(SvgNodeKind.Text, "", "", "", "", "x")));
            Assert.Throws<ArgumentException>(() => first.AttachInitialChild(root, child));
            Assert.Throws<ArgumentException>(() => first.AttachInitialChild(root, first.DocumentNode));
            Assert.Throws<ArgumentException>(() => first.AttachInitialChild(child, new SvgNode(SvgNodeKind.Text, "", "", "", "", "x")));
            Assert.Throws<ArgumentException>(() => first.AttachInitialAttribute(root, attribute));
            Assert.Throws<ArgumentException>(() => first.AttachInitialAttribute(child, new SvgAttribute("a", "", "a", "", "v")));
            Assert.Throws<ArgumentNullException>(() => first.AttachInitialChild(null, child));
            Assert.Throws<ArgumentNullException>(() => first.AttachInitialAttribute(root, null));
        });
    }

    [Test]
    public void ConstructorsRejectInvalidTagsAndNullTextFields()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgNode((SvgNodeKind)999, "", "", "", "", ""));
            Assert.Throws<ArgumentNullException>(() => new SvgNode(SvgNodeKind.Element, null, "", "", "", ""));
            Assert.Throws<ArgumentNullException>(() => new SvgNode(SvgNodeKind.Element, "svg", null, "svg", "", ""));
            Assert.Throws<ArgumentNullException>(() => new SvgNode(SvgNodeKind.Element, "svg", "", null, "", ""));
            Assert.Throws<ArgumentNullException>(() => new SvgNode(SvgNodeKind.Element, "svg", "", "svg", null, ""));
            Assert.Throws<ArgumentNullException>(() => new SvgNode(SvgNodeKind.Element, "svg", "", "svg", "", null));
            Assert.Throws<ArgumentNullException>(() => new SvgAttribute(null, "", "a", "", ""));
            Assert.Throws<ArgumentNullException>(() => new SvgAttribute("a", null, "a", "", ""));
            Assert.Throws<ArgumentNullException>(() => new SvgAttribute("a", "", null, "", ""));
            Assert.Throws<ArgumentNullException>(() => new SvgAttribute("a", "", "a", null, ""));
            Assert.Throws<ArgumentNullException>(() => new SvgAttribute("a", "", "a", "", null));
        });
    }
}
