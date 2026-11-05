using System;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlTreeEditorCoreTests
{
    [Test]
    public void CreatesDetachedNodesWithUniqueDocumentIdentities()
    {
        XmlDocument document = Parse("<root/>", Options());
        document.NextIdentity = 1;
        XmlName suppliedName = Name("p:item", "urn:p");
        XmlTreeEditor editor = new XmlTreeEditor();

        XmlNode first = editor.CreateXmlTreeNode(document, XmlNodeKind.Element, suppliedName, null);
        XmlNode second = editor.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "value");

        Assert.That(first.OwnerDocument, Is.SameAs(document));
        Assert.That(first.Parent, Is.Null);
        Assert.That(first.Children, Is.Empty);
        Assert.That(first.Attributes, Is.Empty);
        Assert.That(first.NamespaceBindings, Is.Empty);
        Assert.That(first.Name, Is.Not.SameAs(suppliedName));
        Assert.That(first.Name!.QualifiedName, Is.EqualTo("p:item"));
        Assert.That(first.Identity, Is.GreaterThan(document.DocumentElement!.Identity));
        Assert.That(second.Identity, Is.GreaterThan(first.Identity));
        Assert.That(second.Value, Is.EqualTo("value"));
        Assert.That(second.MutationVersion, Is.EqualTo(document.MutationVersion));
        Assert.Throws<ArgumentOutOfRangeException>(() => editor.CreateXmlTreeNode(document, (XmlNodeKind)100, null, null));

        document.IsReadOnly = true;
        Assert.Throws<InvalidOperationException>(() => editor.CreateXmlTreeNode(document, XmlNodeKind.Comment, null, "blocked"));
    }

    [Test]
    public void ClonesShallowAndDeepSubtreesWithoutAliasing()
    {
        XmlParserOptions options = Options();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.PreservePrefixes = true;
        options.PreserveSourceLocations = true;
        XmlDocument document = Parse("<p:root xmlns:p='urn:p' a='v'><child>text</child></p:root>", options);
        XmlNode source = document.DocumentElement!;
        source.IsReadOnly = true;
        XmlTreeEditor editor = new XmlTreeEditor();

        XmlNode shallow = editor.CloneXmlTreeNode(source, false);
        XmlNode deep = editor.CloneXmlTreeNode(source, true);

        Assert.That(shallow.Parent, Is.Null);
        Assert.That(shallow.OwnerDocument, Is.SameAs(document));
        Assert.That(shallow.Children, Is.Empty);
        Assert.That(shallow.Attributes, Has.Length.EqualTo(1));
        Assert.That(shallow.Attributes![0].Parent, Is.SameAs(shallow));
        Assert.That(shallow.Attributes[0], Is.Not.SameAs(source.Attributes![0]));
        Assert.That(shallow.Name, Is.Not.SameAs(source.Name));
        Assert.That(shallow.NamespaceBindings, Is.Not.SameAs(source.NamespaceBindings));
        Assert.That(shallow.NamespaceBindings![0], Is.Not.SameAs(source.NamespaceBindings![0]));
        Assert.That(shallow.Range, Is.Not.SameAs(source.Range));
        Assert.That(shallow.IsReadOnly, Is.False);
        Assert.That(deep.Children, Has.Length.EqualTo(1));
        XmlNode deepChild = deep.Children![0];
        Assert.That(deepChild, Is.Not.SameAs(source.Children![0]));
        Assert.That(deepChild.Parent, Is.SameAs(deep));
        Assert.That(deepChild.Children![0].Value, Is.EqualTo("text"));
        Assert.That(deepChild.Children[0].OwnerDocument, Is.SameAs(document));
        Assert.That(deep.Identity, Is.Not.EqualTo(source.Identity));
        Assert.That(deepChild.Identity, Is.Not.EqualTo(source.Children[0].Identity));
        Assert.That(source.Parent, Is.SameAs(document.DocumentNode));
    }

    [Test]
    public void ImportsCopiesIntoTheTargetDocumentWithoutChangingTheSource()
    {
        XmlDocument sourceDocument = Parse("<source a='v'><child>text</child></source>", Options());
        XmlDocument targetDocument = Parse("<target/>", Options());
        XmlNode source = sourceDocument.DocumentElement!;
        XmlTreeEditor editor = new XmlTreeEditor();

        XmlNode shallow = editor.ImportXmlTreeNode(targetDocument, source, false);
        XmlNode deep = editor.ImportXmlTreeNode(targetDocument, source, true);

        Assert.That(shallow.OwnerDocument, Is.SameAs(targetDocument));
        Assert.That(shallow.Parent, Is.Null);
        Assert.That(shallow.Attributes, Has.Length.EqualTo(1));
        Assert.That(shallow.Children, Is.Empty);
        Assert.That(deep.OwnerDocument, Is.SameAs(targetDocument));
        Assert.That(deep.Children![0].OwnerDocument, Is.SameAs(targetDocument));
        Assert.That(deep.Attributes![0].OwnerDocument, Is.SameAs(targetDocument));
        Assert.That(deep.Children[0].Parent, Is.SameAs(deep));
        Assert.That(deep.Identity, Is.GreaterThan(targetDocument.DocumentElement!.Identity));
        Assert.That(source.OwnerDocument, Is.SameAs(sourceDocument));
        Assert.That(source.Parent, Is.SameAs(sourceDocument.DocumentNode));
        Assert.That(source.Children, Has.Length.EqualTo(1));
    }

    [Test]
    public void AdoptsAndDetachesAWholeSubtreeBeforeItCanBeReattached()
    {
        XmlDocument sourceDocument = Parse("<source><move a='v'><nested/></move><stay/></source>", Options());
        XmlDocument targetDocument = Parse("<target/>", Options());
        XmlNode sourceRoot = sourceDocument.DocumentElement!;
        XmlNode moved = sourceRoot.Children![0];
        XmlTreeEditor editor = new XmlTreeEditor();

        XmlNode adopted = editor.AdoptXmlTreeNode(targetDocument, moved);

        Assert.That(adopted, Is.SameAs(moved));
        Assert.That(adopted.Parent, Is.Null);
        Assert.That(sourceRoot.Children, Has.Length.EqualTo(1));
        Assert.That(sourceRoot.Children![0].Name!.LocalName, Is.EqualTo("stay"));
        Assert.That(adopted.OwnerDocument, Is.SameAs(targetDocument));
        Assert.That(adopted.Attributes![0].OwnerDocument, Is.SameAs(targetDocument));
        Assert.That(adopted.Children![0].OwnerDocument, Is.SameAs(targetDocument));
        Assert.That(adopted.Identity, Is.Not.EqualTo(targetDocument.DocumentElement!.Identity));
        Assert.That(adopted.Children[0].Identity, Is.Not.EqualTo(adopted.Identity));

        editor.AppendXmlTreeChild(targetDocument.DocumentElement!, adopted);
        Assert.That(targetDocument.DocumentElement!.Children, Is.EqualTo(new[] { adopted }));
        Assert.That(adopted.Parent, Is.SameAs(targetDocument.DocumentElement));
        Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(sourceDocument));
        Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(targetDocument));
    }

    [Test]
    public void AppendsInsertsRepositionsReplacesAndRemovesInOrder()
    {
        XmlDocument document = Parse("<root><a/><c/></root>", Options());
        XmlNode root = document.DocumentElement!;
        XmlNode a = root.Children![0];
        XmlNode c = root.Children[1];
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode b = editor.CreateXmlTreeNode(document, XmlNodeKind.Element, Name("b", null), null);

        Assert.That(editor.AppendXmlTreeChild(root, b), Is.SameAs(b));
        AssertNames(root, "a", "c", "b");

        Assert.That(editor.InsertXmlTreeBefore(root, b, a), Is.SameAs(b));
        AssertNames(root, "b", "a", "c");

        XmlNode d = editor.CreateXmlTreeNode(document, XmlNodeKind.Element, Name("d", null), null);
        Assert.That(editor.ReplaceXmlTreeNode(root, d, c), Is.SameAs(c));
        AssertNames(root, "b", "a", "d");
        Assert.That(c.Parent, Is.Null);
        Assert.That(d.Parent, Is.SameAs(root));

        Assert.That(editor.RemoveXmlTreeNode(root, a), Is.SameAs(a));
        AssertNames(root, "b", "d");
        Assert.That(a.Parent, Is.Null);
        Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(document));
    }

    [Test]
    public void UpdatesDocumentElementAndDocumentTypeMetadata()
    {
        XmlDocument document = Parse("<root/>", Options());
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode replacement = editor.CreateXmlTreeNode(document, XmlNodeKind.Element, Name("replacement", null), null);

        XmlNode removed = editor.ReplaceXmlTreeNode(document.DocumentNode!, replacement, document.DocumentElement!);

        Assert.That(document.DocumentElement, Is.SameAs(replacement));
        Assert.That(removed.Parent, Is.Null);
        Assert.That(replacement.Parent, Is.SameAs(document.DocumentNode));

        XmlParserOptions dtdOptions = Options();
        dtdOptions.DtdProcessing = XmlDtdProcessing.Parse;
        XmlDocument withType = Parse("<!DOCTYPE root><root/>", dtdOptions);
        XmlNode documentType = withType.DocumentNode!.Children![0];

        editor.RemoveXmlTreeNode(withType.DocumentNode, documentType);

        Assert.That(withType.DocumentType, Is.Null);
        Assert.That(documentType.Parent, Is.Null);
        Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(withType));
    }

    [Test]
    public void FailedMutationsLeaveEveryParticipatingTreeUnchanged()
    {
        XmlDocument document = Parse("<root><child/></root>", Options());
        XmlNode root = document.DocumentElement!;
        XmlNode child = root.Children![0];
        XmlNode[] originalChildren = root.Children;
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlDocument foreignDocument = Parse("<foreign/>", Options());
        XmlNode foreign = editor.CreateXmlTreeNode(foreignDocument, XmlNodeKind.Element, Name("item", null), null);

        Assert.Throws<ArgumentException>(() => editor.AppendXmlTreeChild(root, foreign));
        Assert.That(root.Children, Is.SameAs(originalChildren));
        Assert.That(foreign.Parent, Is.Null);

        Assert.Throws<ArgumentException>(() => editor.AppendXmlTreeChild(child, root));
        Assert.That(root.Parent, Is.SameAs(document.DocumentNode));
        Assert.That(child.Children, Is.Empty);

        XmlNode candidate = editor.CreateXmlTreeNode(document, XmlNodeKind.Element, Name("candidate", null), null);
        XmlNode absentReference = editor.CreateXmlTreeNode(document, XmlNodeKind.Element, Name("absent", null), null);
        Assert.Throws<ArgumentException>(() => editor.InsertXmlTreeBefore(root, candidate, absentReference));
        Assert.That(root.Children, Is.SameAs(originalChildren));
        Assert.That(candidate.Parent, Is.Null);

        XmlNode illegal = editor.CreateXmlTreeNode(document, XmlNodeKind.Attribute, Name("a", null), "v");
        Assert.Throws<ArgumentException>(() => editor.ReplaceXmlTreeNode(root, illegal, child));
        Assert.That(root.Children, Is.SameAs(originalChildren));
        Assert.That(child.Parent, Is.SameAs(root));

        Assert.Throws<ArgumentException>(() => editor.RemoveXmlTreeNode(document.DocumentNode!, root));
        Assert.That(document.DocumentElement, Is.SameAs(root));
        Assert.That(root.Parent, Is.SameAs(document.DocumentNode));

        root.IsReadOnly = true;
        Assert.Throws<InvalidOperationException>(() => editor.AppendXmlTreeChild(root, candidate));
        Assert.That(root.Children, Is.SameAs(originalChildren));

        root.IsReadOnly = false;
        Assert.Throws<ArgumentException>(() => editor.AdoptXmlTreeNode(foreignDocument, root));
        Assert.That(root.OwnerDocument, Is.SameAs(document));
        Assert.That(root.Parent, Is.SameAs(document.DocumentNode));
    }

    private static void AssertNames(XmlNode parent, params string[] names)
    {
        Assert.That(parent.Children, Has.Length.EqualTo(names.Length));
        int index = 0;
        while (index < names.Length)
        {
            Assert.That(parent.Children![index].Name!.LocalName, Is.EqualTo(names[index]));
            Assert.That(parent.Children[index].Parent, Is.SameAs(parent));
            index = index + 1;
        }
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
            ValidationMode = XmlValidationMode.None
        };
    }
}
