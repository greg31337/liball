using System.Text;

namespace odf.Tests;

public class OdfTreeEditorTests
{
    [Test]
    public void CreatesValidUnattachedNodeKindsAndRejectsInvalidMarkup()
    {
        OdfTreeEditor editor = new OdfTreeEditor([]);
        OdfQualifiedName requestedName = Name(OdfNamespaces.Text, "p", "text");
        OdfNode element = editor.CreateElement(requestedName);
        OdfNode text = editor.CreateText("content");
        OdfNode comment = editor.CreateComment("comment");
        OdfNode instruction = editor.CreateProcessingInstruction(
            "xml-stylesheet",
            "type='text/css'");
        requestedName.LocalName = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(element.Kind, Is.EqualTo(OdfNodeKind.Element));
            Assert.That(element.Name?.LocalName, Is.EqualTo("p"));
            Assert.That(element.Parent, Is.Null);
            Assert.That(text.Kind, Is.EqualTo(OdfNodeKind.Text));
            Assert.That(text.Value, Is.EqualTo("content"));
            Assert.That(comment.Kind, Is.EqualTo(OdfNodeKind.Comment));
            Assert.That(instruction.Kind, Is.EqualTo(OdfNodeKind.ProcessingInstruction));
            Assert.That(instruction.Name?.LocalName, Is.EqualTo("xml-stylesheet"));
            Assert.That(
                () => editor.CreateComment("bad--comment"),
                Throws.TypeOf<OdfXmlException>());
            Assert.That(
                () => editor.CreateProcessingInstruction("XML", "bad"),
                Throws.TypeOf<OdfXmlException>());
            Assert.That(
                () => editor.CreateElement(Name("", "1bad", "")),
                Throws.TypeOf<OdfXmlException>());
        });
    }

    [Test]
    public void InsertsReparentsReplacesAndRemovesNodesWhileMaintainingOwnership()
    {
        OdfTreeEditor editor = new OdfTreeEditor([]);
        OdfNode root = editor.CreateElement(Name("", "root", ""));
        OdfNode first = editor.CreateElement(Name("", "first", ""));
        OdfNode second = editor.CreateElement(Name("", "second", ""));
        OdfNode third = editor.CreateElement(Name("", "third", ""));
        editor.InsertRoot(0, root);
        editor.InsertChild(root, 0, first);
        editor.InsertChild(root, 1, third);
        editor.InsertChild(root, 1, second);

        Assert.Multiple(() =>
        {
            Assert.That(root.Children, Is.EqualTo(new[] { first, second, third }));
            Assert.That(root.Children.All(node => node.Parent == root), Is.True);
            Assert.That(editor.SearchQualifiedName(Name("", "second", "")), Is.EqualTo(new[] { second }));
        });

        editor.ReparentNode(second, first, 0);
        OdfNode replacement = editor.CreateElement(Name("", "replacement", ""));
        editor.ReplaceNode(third, replacement);
        editor.RemoveNode(second);
        editor.ReparentNode(first, null, 1);

        Assert.Multiple(() =>
        {
            Assert.That(root.Children, Is.EqualTo(new[] { replacement }));
            Assert.That(replacement.Parent, Is.SameAs(root));
            Assert.That(third.Parent, Is.Null);
            Assert.That(second.Parent, Is.Null);
            Assert.That(first.Children, Is.Empty);
            Assert.That(first.Parent, Is.Null);
            Assert.That(editor.GetRoots(), Is.EqualTo(new[] { root, first }));
            Assert.That(editor.SearchQualifiedName(Name("", "second", "")), Is.Empty);
            Assert.That(editor.SearchQualifiedName(Name("", "replacement", "")), Is.EqualTo(new[] { replacement }));
        });

        OdfNode newRoot = editor.CreateElement(Name("", "new-root", ""));
        editor.ReplaceNode(root, newRoot);
        editor.RemoveNode(first);

        Assert.Multiple(() =>
        {
            Assert.That(editor.GetRoots(), Is.EqualTo(new[] { newRoot }));
            Assert.That(editor.SearchQualifiedName(Name("", "root", "")), Is.Empty);
            Assert.That(editor.SearchQualifiedName(Name("", "first", "")), Is.Empty);
            Assert.That(editor.SearchQualifiedName(Name("", "new-root", "")), Is.EqualTo(new[] { newRoot }));
        });
    }

    [Test]
    public void FailedOwnershipAndCycleOperationsLeaveTheTreeUnchanged()
    {
        OdfTreeEditor editor = new OdfTreeEditor([]);
        OdfNode root = editor.CreateElement(Name("", "root", ""));
        OdfNode parent = editor.CreateElement(Name("", "parent", ""));
        OdfNode child = editor.CreateElement(Name("", "child", ""));
        editor.InsertRoot(0, root);
        editor.InsertChild(root, 0, parent);
        editor.InsertChild(parent, 0, child);
        OdfNode unattached = editor.CreateElement(Name("", "unattached", ""));

        Assert.Multiple(() =>
        {
            Assert.That(
                () => editor.ReparentNode(parent, child, 0),
                Throws.TypeOf<OdfEditException>());
            Assert.That(
                () => editor.InsertChild(root, 0, child),
                Throws.TypeOf<OdfEditException>());
            Assert.That(
                () => editor.ReplaceNode(parent, child),
                Throws.TypeOf<OdfEditException>());
            Assert.That(
                () => editor.InsertChild(root, 9, unattached),
                Throws.TypeOf<OdfEditException>());
        });

        Assert.Multiple(() =>
        {
            Assert.That(root.Children, Is.EqualTo(new[] { parent }));
            Assert.That(parent.Parent, Is.SameAs(root));
            Assert.That(parent.Children, Is.EqualTo(new[] { child }));
            Assert.That(child.Parent, Is.SameAs(parent));
            Assert.That(unattached.Parent, Is.Null);
            Assert.That(editor.TraverseAllNodes(), Is.EqualTo(new[] { root, parent, child }));
        });
    }

    [Test]
    public void ClonesSemanticSubtreesWithIndependentDataAndNewLocations()
    {
        OdfNode[] roots = Read("<root xmlns:e='urn:example' xml:id='root-id'>"
            + "<e:item e:value='one'>text<!--note--></e:item></root>");
        OdfTreeEditor editor = new OdfTreeEditor(roots);
        OdfNode source = Root(roots);

        OdfNode clone = editor.CloneSubtree(source);
        editor.InsertChild(source, source.Children.Length, clone);

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.Parent, Is.SameAs(source));
            Assert.That(clone.Name, Is.Not.SameAs(source.Name));
            Assert.That(clone.NamespaceDeclarations[0], Is.Not.SameAs(source.NamespaceDeclarations[0]));
            Assert.That(clone.Attributes[0], Is.Not.SameAs(source.Attributes[0]));
            Assert.That(clone.Children[0], Is.Not.SameAs(source.Children[0]));
            Assert.That(clone.Children[0].Parent, Is.SameAs(clone));
            Assert.That(clone.XmlId, Is.EqualTo("root-id"));
            Assert.That(source.Location.Line, Is.GreaterThan(0));
            Assert.That(clone.Location.Line, Is.EqualTo(-1));
            Assert.That(clone.Attributes[0].Location.Line, Is.EqualTo(-1));
            Assert.That(editor.SearchXmlId("root-id"), Is.EqualTo(new[] { source, clone }));
        });

        clone.Attributes[0].Value = "clone-id";
        clone.Children[0].Attributes[0].Value = "changed";
        Assert.Multiple(() =>
        {
            Assert.That(source.Attributes[0].Value, Is.EqualTo("root-id"));
            Assert.That(source.Children[0].Attributes[0].Value, Is.EqualTo("one"));
        });
    }

    [Test]
    public void TraversesSearchesAndReplacesTextContentInDocumentOrder()
    {
        OdfNode[] roots = Read("<root>before<span>inside</span><!--note--><?work go?>after</root>");
        OdfTreeEditor editor = new OdfTreeEditor(roots);
        OdfNode root = Root(roots);
        OdfNode span = root.Children[1];
        OdfNode leadingText = root.Children[0];
        OdfNode nestedText = span.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(editor.TraverseSubtree(root), Is.EqualTo(new[]
            {
                root,
                leadingText,
                span,
                nestedText,
                root.Children[2],
                root.Children[3],
                root.Children[4]
            }));
            Assert.That(editor.SearchText("in"), Is.EqualTo(new[] { nestedText }));
            Assert.That(editor.GetTextContent(root), Is.EqualTo("beforeinsideafter"));
        });

        editor.SetTextContent(root, "replacement");

        Assert.Multiple(() =>
        {
            Assert.That(root.Children, Has.Length.EqualTo(1));
            Assert.That(root.Children[0].Kind, Is.EqualTo(OdfNodeKind.Text));
            Assert.That(root.Children[0].Value, Is.EqualTo("replacement"));
            Assert.That(root.Children[0].Parent, Is.SameAs(root));
            Assert.That(leadingText.Parent, Is.Null);
            Assert.That(span.Parent, Is.Null);
            Assert.That(editor.GetTextContent(root), Is.EqualTo("replacement"));
            Assert.That(editor.SearchQualifiedName(Name("", "span", "")), Is.Empty);
        });
    }

    [Test]
    public void AttributeMutationsRefreshIdStyleAndReferenceIndexes()
    {
        string xml = "<root xmlns:style='"
            + OdfNamespaces.Style
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "' xmlns:xlink='"
            + OdfNamespaces.XLink
            + "'><style:style style:name='Body'/><text:p xml:id='p1'>"
            + "<text:a xlink:href='#p1'/></text:p></root>";
        OdfNode[] roots = Read(xml);
        OdfTreeEditor editor = new OdfTreeEditor(roots);
        OdfNode root = Root(roots);
        OdfNode style = root.Children[0];
        OdfNode paragraph = root.Children[1];
        OdfNode link = paragraph.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(editor.SearchXmlId("p1"), Is.EqualTo(new[] { paragraph }));
            Assert.That(editor.SearchStyleName("Body"), Is.EqualTo(new[] { style }));
            Assert.That(editor.SearchReferences("p1"), Is.EqualTo(new[] { link }));
        });

        editor.SetAttribute(paragraph, Name(OdfNamespaces.Xml, "id", "xml"), "p2");
        editor.SetAttribute(style, Name(OdfNamespaces.Style, "name", "style"), "Heading");
        editor.SetAttribute(link, Name(OdfNamespaces.XLink, "href", "xlink"), "#p2");

        Assert.Multiple(() =>
        {
            Assert.That(editor.SearchXmlId("p1"), Is.Empty);
            Assert.That(editor.SearchXmlId("p2"), Is.EqualTo(new[] { paragraph }));
            Assert.That(editor.SearchStyleName("Body"), Is.Empty);
            Assert.That(editor.SearchStyleName("Heading"), Is.EqualTo(new[] { style }));
            Assert.That(editor.SearchReferences("p1"), Is.Empty);
            Assert.That(editor.SearchReferences("p2"), Is.EqualTo(new[] { link }));
            Assert.That(editor.FindAttribute(paragraph, Name(OdfNamespaces.Xml, "id", "xml"))?.Value, Is.EqualTo("p2"));
        });

        bool removed = editor.RemoveAttribute(
            paragraph,
            Name(OdfNamespaces.Xml, "id", "xml"));
        bool absent = editor.RemoveAttribute(
            paragraph,
            Name(OdfNamespaces.Xml, "id", "xml"));

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(absent, Is.False);
            Assert.That(paragraph.XmlId, Is.Empty);
            Assert.That(editor.SearchXmlId("p2"), Is.Empty);
        });
    }

    private static OdfNode[] Read(string xml)
    {
        return OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            OdfPartNames.Content,
            new OdfLoadOptions());
    }

    private static OdfNode Root(OdfNode[] nodes)
    {
        return nodes.Single(node => node.Kind == OdfNodeKind.Element);
    }

    private static OdfQualifiedName Name(
        string namespaceName,
        string localName,
        string prefix)
    {
        return new OdfQualifiedName
        {
            NamespaceName = namespaceName,
            LocalName = localName,
            Prefix = prefix
        };
    }
}
