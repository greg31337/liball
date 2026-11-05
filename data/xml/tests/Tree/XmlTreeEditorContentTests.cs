using System;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlTreeEditorContentTests
{
    [Test]
    public void NormalizesOrdinaryTextWhilePreservingCDataAndEntityBoundaries()
    {
        XmlDocument document = Parse("<root/>", Options());
        XmlNode root = document.DocumentElement!;
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode first = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "a");
        XmlNode second = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "b");
        XmlNode empty = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "");
        XmlNode cdata = editor.CreateXmlTreeNode(document, XmlNodeKind.CData, null, "c");
        XmlNode afterCData = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "d");
        XmlNode entity = editor.CreateXmlTreeNode(document, XmlNodeKind.EntityReference, Name("ent", null), null);
        XmlNode entityFirst = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "e");
        XmlNode entitySecond = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "f");
        XmlNode afterEntity = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "g");
        editor.AppendXmlTreeChild(entity, entityFirst);
        editor.AppendXmlTreeChild(entity, entitySecond);
        editor.AppendXmlTreeChild(root, first);
        editor.AppendXmlTreeChild(root, second);
        editor.AppendXmlTreeChild(root, empty);
        editor.AppendXmlTreeChild(root, cdata);
        editor.AppendXmlTreeChild(root, afterCData);
        editor.AppendXmlTreeChild(root, entity);
        editor.AppendXmlTreeChild(root, afterEntity);
        long before = document.MutationVersion;

        editor.NormalizeXmlTreeNode(root);

        Assert.That(root.Children, Has.Length.EqualTo(5));
        Assert.That(root.Children![0], Is.SameAs(first));
        Assert.That(first.Value, Is.EqualTo("ab"));
        Assert.That(second.Parent, Is.Null);
        Assert.That(empty.Parent, Is.Null);
        Assert.That(root.Children[1], Is.SameAs(cdata));
        Assert.That(root.Children[2], Is.SameAs(afterCData));
        Assert.That(root.Children[3], Is.SameAs(entity));
        Assert.That(root.Children[4], Is.SameAs(afterEntity));
        Assert.That(entity.Children, Has.Length.EqualTo(1));
        Assert.That(entity.Children![0], Is.SameAs(entityFirst));
        Assert.That(entityFirst.Value, Is.EqualTo("ef"));
        Assert.That(entitySecond.Parent, Is.Null);
        Assert.That(document.MutationVersion, Is.EqualTo(before + 1));

        editor.NormalizeXmlTreeNode(root);
        Assert.That(document.MutationVersion, Is.EqualTo(before + 1));
    }

    [Test]
    public void ReplacesContainerTextAndUpdatesLeafValues()
    {
        XmlParserOptions options = Options();
        options.PreserveSourceLocations = true;
        XmlDocument document = Parse("<root><child/></root>", options);
        XmlNode root = document.DocumentElement!;
        XmlNode child = root.Children![0];
        XmlTreeEditor editor = new XmlTreeEditor();
        long before = document.MutationVersion;

        editor.SetXmlTreeText(root, "value");

        Assert.That(root.Children, Has.Length.EqualTo(1));
        XmlNode text = root.Children![0];
        Assert.That(text.Kind, Is.EqualTo(XmlNodeKind.Text));
        Assert.That(text.Value, Is.EqualTo("value"));
        Assert.That(text.Parent, Is.SameAs(root));
        Assert.That(child.Parent, Is.Null);
        Assert.That(root.Range, Is.Null);
        Assert.That(document.MutationVersion, Is.EqualTo(before + 1));

        editor.SetXmlTreeText(text, "changed");
        Assert.That(text.Value, Is.EqualTo("changed"));
        Assert.That(document.MutationVersion, Is.EqualTo(before + 2));
        editor.SetXmlTreeText(text, "changed");
        Assert.That(document.MutationVersion, Is.EqualTo(before + 2));

        editor.SetXmlTreeText(root, "");
        Assert.That(root.Children, Is.Empty);
        Assert.That(text.Parent, Is.Null);
        Assert.That(document.MutationVersion, Is.EqualTo(before + 3));
        Assert.Throws<ArgumentException>(() => editor.SetXmlTreeText(document.DocumentNode!, "invalid"));
        Assert.That(document.MutationVersion, Is.EqualTo(before + 3));
    }

    [Test]
    public void SetsReplacesMovesAndRemovesAttributesByExpandedName()
    {
        XmlDocument document = Parse("<root><left a='1' b='2'/><right/></root>", Options());
        XmlNode root = document.DocumentElement!;
        XmlNode left = root.Children![0];
        XmlNode right = root.Children[1];
        XmlNode original = left.Attributes![0];
        XmlNode retained = left.Attributes[1];
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode replacement = editor.CreateXmlTreeNode(document, XmlNodeKind.Attribute, Name("a", null), "new");
        long before = document.MutationVersion;

        editor.SetXmlTreeAttribute(left, replacement);

        Assert.That(left.Attributes, Is.EqualTo(new[] { replacement, retained }));
        Assert.That(original.Parent, Is.Null);
        Assert.That(replacement.Parent, Is.SameAs(left));
        Assert.That(document.MutationVersion, Is.EqualTo(before + 1));

        editor.SetXmlTreeAttribute(right, replacement);
        Assert.That(left.Attributes, Is.EqualTo(new[] { retained }));
        Assert.That(right.Attributes, Is.EqualTo(new[] { replacement }));
        Assert.That(replacement.Parent, Is.SameAs(right));
        Assert.That(document.MutationVersion, Is.EqualTo(before + 2));

        long noOpVersion = document.MutationVersion;
        editor.SetXmlTreeAttribute(right, replacement);
        Assert.That(document.MutationVersion, Is.EqualTo(noOpVersion));

        XmlNode? removed = editor.RemoveXmlTreeAttribute(right, Name("different:a", null));
        Assert.That(removed, Is.SameAs(replacement));
        Assert.That(replacement.Parent, Is.Null);
        Assert.That(right.Attributes, Is.Empty);
        Assert.That(document.MutationVersion, Is.EqualTo(noOpVersion + 1));

        Assert.That(editor.RemoveXmlTreeAttribute(right, Name("missing", null)), Is.Null);
        Assert.That(document.MutationVersion, Is.EqualTo(noOpVersion + 1));
    }

    [Test]
    public void RepairsAttributePrefixesWithoutDisturbingConflictingBindings()
    {
        XmlParserOptions options = Options();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.PreservePrefixes = true;
        XmlDocument document = Parse("<root xmlns:p='urn:old'><child/></root>", options);
        XmlNode root = document.DocumentElement!;
        XmlNode child = root.Children![0];
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode conflict = editor.CreateXmlTreeNode(document, XmlNodeKind.Attribute, Name("p:item", "urn:new"), "one");
        XmlNode unbound = editor.CreateXmlTreeNode(document, XmlNodeKind.Attribute, Name("q:other", "urn:q"), "two");
        XmlNode xmlAttribute = editor.CreateXmlTreeNode(document, XmlNodeKind.Attribute, Name("wrong:lang", XmlNamespaces.Xml), "en");

        editor.SetXmlTreeAttribute(child, conflict);
        editor.SetXmlTreeAttribute(child, unbound);
        editor.SetXmlTreeAttribute(child, xmlAttribute);

        Assert.That(conflict.Name!.Prefix, Is.EqualTo("ns1"));
        Assert.That(conflict.Name.QualifiedName, Is.EqualTo("ns1:item"));
        Assert.That(unbound.Name!.Prefix, Is.EqualTo("q"));
        Assert.That(unbound.Name.QualifiedName, Is.EqualTo("q:other"));
        Assert.That(xmlAttribute.Name!.Prefix, Is.EqualTo("xml"));
        Assert.That(xmlAttribute.Name.QualifiedName, Is.EqualTo("xml:lang"));
        Assert.That(child.NamespaceBindings, Has.Length.EqualTo(2));
        Assert.That(child.NamespaceBindings![0].Prefix, Is.EqualTo("ns1"));
        Assert.That(child.NamespaceBindings[0].NamespaceUri, Is.EqualTo("urn:new"));
        Assert.That(child.NamespaceBindings[1].Prefix, Is.EqualTo("q"));
        Assert.That(child.NamespaceBindings[1].NamespaceUri, Is.EqualTo("urn:q"));
        Assert.That(root.NamespaceBindings![0].Prefix, Is.EqualTo("p"));
        Assert.That(root.NamespaceBindings[0].NamespaceUri, Is.EqualTo("urn:old"));
        Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(document));
    }

    [Test]
    public void FreezeHonorsDepthAndRejectsLaterMutation()
    {
        XmlDocument shallowDocument = Parse("<root a='1'><child><grand/></child></root>", Options());
        XmlNode shallowRoot = shallowDocument.DocumentElement!;
        XmlNode child = shallowRoot.Children![0];
        XmlNode grand = child.Children![0];
        XmlTreeEditor editor = new XmlTreeEditor();
        long shallowBefore = shallowDocument.MutationVersion;

        editor.FreezeXmlTree(child, false);

        Assert.That(child.IsReadOnly, Is.True);
        Assert.That(grand.IsReadOnly, Is.False);
        Assert.That(shallowDocument.MutationVersion, Is.EqualTo(shallowBefore + 1));
        Assert.Throws<InvalidOperationException>(() => editor.SetXmlTreeText(grand, "blocked"));

        XmlDocument deepDocument = Parse("<root a='1'><child><grand/></child></root>", Options());
        XmlNode deepRoot = deepDocument.DocumentElement!;
        long deepBefore = deepDocument.MutationVersion;
        editor.FreezeXmlTree(deepRoot, true);

        Assert.That(deepRoot.IsReadOnly, Is.True);
        Assert.That(deepRoot.Attributes![0].IsReadOnly, Is.True);
        Assert.That(deepRoot.Children![0].IsReadOnly, Is.True);
        Assert.That(deepRoot.Children[0].Children![0].IsReadOnly, Is.True);
        Assert.That(deepDocument.MutationVersion, Is.EqualTo(deepBefore + 1));
        editor.FreezeXmlTree(deepRoot, true);
        Assert.That(deepDocument.MutationVersion, Is.EqualTo(deepBefore + 1));
    }

    [Test]
    public void StructuralMutationsAdvanceVersionOnceAndNoOpsRemainStable()
    {
        XmlDocument document = Parse("<root><a/></root>", Options());
        XmlNode root = document.DocumentElement!;
        XmlNode a = root.Children![0];
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode b = editor.CreateXmlTreeNode(document, XmlNodeKind.Element, Name("b", null), null);
        long before = document.MutationVersion;

        editor.AppendXmlTreeChild(root, b);
        Assert.That(document.MutationVersion, Is.EqualTo(before + 1));
        Assert.That(b.MutationVersion, Is.EqualTo(before + 1));
        editor.AppendXmlTreeChild(root, b);
        Assert.That(document.MutationVersion, Is.EqualTo(before + 1));

        editor.InsertXmlTreeBefore(root, b, a);
        Assert.That(document.MutationVersion, Is.EqualTo(before + 2));
        editor.InsertXmlTreeBefore(root, b, a);
        Assert.That(document.MutationVersion, Is.EqualTo(before + 2));

        XmlDocument foreignDocument = Parse("<foreign/>", Options());
        XmlNode foreign = editor.CreateXmlTreeNode(foreignDocument, XmlNodeKind.Element, Name("foreign", null), null);
        Assert.Throws<ArgumentException>(() => editor.AppendXmlTreeChild(root, foreign));
        Assert.That(document.MutationVersion, Is.EqualTo(before + 2));

        editor.RemoveXmlTreeNode(root, b);
        Assert.That(document.MutationVersion, Is.EqualTo(before + 3));
        Assert.That(b.MutationVersion, Is.EqualTo(before + 3));
    }

    [Test]
    public void RejectedNormalizationAndAttributeChangesAreAtomic()
    {
        XmlDocument document = Parse("<root><left/><right/></root>", Options());
        XmlNode root = document.DocumentElement!;
        XmlNode left = root.Children![0];
        XmlNode right = root.Children[1];
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode first = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "a");
        XmlNode second = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "b");
        editor.AppendXmlTreeChild(left, first);
        editor.AppendXmlTreeChild(left, second);
        second.IsReadOnly = true;
        XmlNode[] originalChildren = left.Children!;
        long before = document.MutationVersion;

        Assert.Throws<InvalidOperationException>(() => editor.NormalizeXmlTreeNode(left));
        Assert.That(left.Children, Is.SameAs(originalChildren));
        Assert.That(first.Value, Is.EqualTo("a"));
        Assert.That(second.Parent, Is.SameAs(left));
        Assert.That(document.MutationVersion, Is.EqualTo(before));

        XmlDocument foreignDocument = Parse("<foreign/>", Options());
        XmlNode foreignAttribute = editor.CreateXmlTreeNode(foreignDocument, XmlNodeKind.Attribute, Name("a", null), "v");
        XmlNode[] originalAttributes = right.Attributes!;
        Assert.Throws<ArgumentException>(() => editor.SetXmlTreeAttribute(right, foreignAttribute));
        Assert.That(right.Attributes, Is.SameAs(originalAttributes));
        Assert.That(foreignAttribute.Parent, Is.Null);
        Assert.That(document.MutationVersion, Is.EqualTo(before));
    }

    private static XmlDocument Parse(string text, XmlParserOptions options)
    {
        return new XmlParser().ParseXmlDocument(new XmlInput { Kind = XmlInputKind.String, Text = text }, options, null, null, null);
    }

    private static XmlName Name(string qualifiedName, string? namespaceUri)
    {
        int colon = qualifiedName.IndexOf(':');
        return new XmlName
        {
            Prefix = colon < 0 ? null : qualifiedName.Substring(0, colon),
            LocalName = colon < 0 ? qualifiedName : qualifiedName.Substring(colon + 1),
            NamespaceUri = namespaceUri,
            QualifiedName = qualifiedName
        };
    }

    private static XmlParserOptions Options()
    {
        return new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Disabled,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveComments = true,
            PreserveProcessingInstructions = true
        };
    }
}
