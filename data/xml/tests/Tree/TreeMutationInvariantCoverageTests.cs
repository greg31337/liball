using System;
using xml;

namespace xml.Tests;

[TestFixture]
public class TreeMutationInvariantCoverageTests
{
    [TestCase(XmlNodeKind.Element, true)]
    [TestCase(XmlNodeKind.Text, true)]
    [TestCase(XmlNodeKind.CData, true)]
    [TestCase(XmlNodeKind.EntityReference, true)]
    [TestCase(XmlNodeKind.ProcessingInstruction, true)]
    [TestCase(XmlNodeKind.Comment, true)]
    [TestCase(XmlNodeKind.Document, false)]
    [TestCase(XmlNodeKind.DocumentFragment, false)]
    [TestCase(XmlNodeKind.Attribute, false)]
    [TestCase(XmlNodeKind.Namespace, false)]
    [TestCase(XmlNodeKind.DocumentType, false)]
    public void EnforcesTheCompleteElementChildPlacementMatrix(XmlNodeKind childKind, bool legal)
    {
        XmlDocument document = Parse("<root/>", Options());
        XmlNode root = document.DocumentElement!;
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlName? name = childKind == XmlNodeKind.Element || childKind == XmlNodeKind.EntityReference || childKind == XmlNodeKind.ProcessingInstruction || childKind == XmlNodeKind.Attribute || childKind == XmlNodeKind.DocumentType ? Name("item", null) : null;
        XmlNode child = editor.CreateXmlTreeNode(document, childKind, name, childKind == XmlNodeKind.Text ? "value" : null);
        long version = document.MutationVersion;

        if (legal)
        {
            Assert.That(editor.AppendXmlTreeChild(root, child), Is.SameAs(child));
            Assert.That(child.Parent, Is.SameAs(root));
            Assert.That(document.MutationVersion, Is.EqualTo(version + 1));
            Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(document));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => editor.AppendXmlTreeChild(root, child));
            Assert.That(child.Parent, Is.Null);
            Assert.That(root.Children, Is.Empty);
            Assert.That(document.MutationVersion, Is.EqualTo(version));
        }
    }

    [Test]
    public void AdoptsAttachedAttributesAcrossDocumentsWithOwnershipAndVersionUpdates()
    {
        XmlDocument source = Parse("<source a='v'/>", Options());
        XmlDocument target = Parse("<target/>", Options());
        XmlNode sourceRoot = source.DocumentElement!;
        XmlNode targetRoot = target.DocumentElement!;
        XmlNode attribute = sourceRoot.Attributes![0];
        long sourceVersion = source.MutationVersion;
        long targetVersion = target.MutationVersion;
        XmlTreeEditor editor = new XmlTreeEditor();

        XmlNode adopted = editor.AdoptXmlTreeNode(target, attribute);

        Assert.That(adopted, Is.SameAs(attribute));
        Assert.That(sourceRoot.Attributes, Is.Empty);
        Assert.That(attribute.Parent, Is.Null);
        Assert.That(attribute.OwnerDocument, Is.SameAs(target));
        Assert.That(source.MutationVersion, Is.EqualTo(sourceVersion + 1));
        Assert.That(target.MutationVersion, Is.EqualTo(targetVersion + 1));

        editor.SetXmlTreeAttribute(targetRoot, attribute);
        Assert.That(targetRoot.Attributes, Is.EqualTo(new[] { attribute }));
        Assert.That(attribute.Parent, Is.SameAs(targetRoot));
        Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(source));
        Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(target));
    }

    [Test]
    public void SynchronizesRetainedNamespaceDeclarationsThroughReplaceAndRemove()
    {
        XmlParserOptions options = Options();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces11;
        options.PreservePrefixes = true;
        options.RetainNamespaceDeclarations = true;
        XmlDocument document = Parse("<root xmlns:p='urn:old'/>", options);
        XmlNode root = document.DocumentElement!;
        XmlNode original = root.Attributes![0];
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode replacement = editor.CreateXmlTreeNode(document, XmlNodeKind.Attribute, Name("xmlns:p", XmlNamespaces.Xmlns), "urn:new");

        editor.SetXmlTreeAttribute(root, replacement);

        Assert.That(original.Parent, Is.Null);
        Assert.That(root.Attributes, Is.EqualTo(new[] { replacement }));
        Assert.That(root.NamespaceBindings, Has.Length.EqualTo(1));
        Assert.That(root.NamespaceBindings![0].Prefix, Is.EqualTo("p"));
        Assert.That(root.NamespaceBindings[0].NamespaceUri, Is.EqualTo("urn:new"));

        XmlNode? removed = editor.RemoveXmlTreeAttribute(root, Name("xmlns:p", XmlNamespaces.Xmlns));

        Assert.That(removed, Is.SameAs(replacement));
        Assert.That(replacement.Parent, Is.Null);
        Assert.That(root.Attributes, Is.Empty);
        Assert.That(root.NamespaceBindings, Is.Empty);
        Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(document));
    }

    [Test]
    public void RejectsMovesThatWouldLeaveTheSourceDocumentInvalidWithoutPartialMutation()
    {
        XmlDocument document = Parse("<root><child/></root>", Options());
        XmlNode container = document.DocumentNode!;
        XmlNode root = document.DocumentElement!;
        XmlNode[] originalDocumentChildren = container.Children!;
        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode fragment = editor.CreateXmlTreeNode(document, XmlNodeKind.DocumentFragment, null, null);
        long version = document.MutationVersion;

        Assert.Throws<ArgumentException>(() => editor.AppendXmlTreeChild(fragment, root));

        Assert.That(container.Children, Is.SameAs(originalDocumentChildren));
        Assert.That(document.DocumentElement, Is.SameAs(root));
        Assert.That(root.Parent, Is.SameAs(container));
        Assert.That(fragment.Children, Is.Empty);
        Assert.That(document.MutationVersion, Is.EqualTo(version));
        Assert.DoesNotThrow(() => new TreeInvariants().ValidateXmlDocumentTree(document));
    }

    [Test]
    public void NavigationAndRangesTrackAReparentedSubtreeInCurrentDocumentOrder()
    {
        XmlDocument document = Parse("<root><left><a/></left><right><b/></right></root>", Options());
        XmlNode root = document.DocumentElement!;
        XmlNode left = root.Children![0];
        XmlNode right = root.Children[1];
        XmlNode a = left.Children![0];
        XmlNode b = right.Children![0];
        XmlRange range = new XmlRange
        {
            Document = document,
            StartContainer = root,
            StartOffset = 0,
            EndContainer = root,
            EndOffset = 1,
            MutationVersion = document.MutationVersion,
            IsValid = true
        };
        XmlTreeNavigator navigator = new XmlTreeNavigator();
        Assert.That(navigator.SelectXmlRange(range).Nodes, Is.EqualTo(new[] { left, a }));

        new XmlTreeEditor().InsertXmlTreeBefore(right, a, b);

        Assert.That(left.Children, Is.Empty);
        Assert.That(navigator.FindXmlParent(a), Is.SameAs(right));
        Assert.That(navigator.SelectXmlChildren(right).Nodes, Is.EqualTo(new[] { a, b }));
        Assert.That(navigator.SelectXmlAncestors(a).Nodes, Is.EqualTo(new[] { document.DocumentNode, root, right }));
        Assert.That(navigator.SelectXmlDescendants(root).Nodes, Is.EqualTo(new[] { left, right, a, b }));
        Assert.That(navigator.SelectXmlFollowingSiblings(a).Nodes, Is.EqualTo(new[] { b }));
        Assert.That(navigator.SelectXmlPrecedingSiblings(b).Nodes, Is.EqualTo(new[] { a }));
        Assert.That(navigator.SelectXmlFollowing(a).Nodes, Is.EqualTo(new[] { b }));
        Assert.That(navigator.SelectXmlPreceding(b).Nodes, Is.EqualTo(new[] { left, a }));
        Assert.That(navigator.CompareXmlNodeOrder(a, b), Is.LessThan(0));
        Assert.That(navigator.ContainsXmlNodeIdentity(a, left), Is.False);
        Assert.Throws<InvalidOperationException>(() => navigator.SelectXmlRange(range));
        Assert.That(range.IsValid, Is.False);
    }

    [Test]
    public void FreezingOneAttributeBlocksItsMutationWithoutFreezingSiblings()
    {
        XmlDocument document = Parse("<root a='one' b='two'/>", Options());
        XmlNode root = document.DocumentElement!;
        XmlNode first = root.Attributes![0];
        XmlNode second = root.Attributes[1];
        XmlTreeEditor editor = new XmlTreeEditor();

        editor.FreezeXmlTree(first, true);

        Assert.That(first.IsReadOnly, Is.True);
        Assert.That(second.IsReadOnly, Is.False);
        Assert.Throws<InvalidOperationException>(() => editor.SetXmlTreeText(first, "blocked"));
        Assert.Throws<InvalidOperationException>(() => editor.RemoveXmlTreeAttribute(root, first.Name!));
        editor.SetXmlTreeText(second, "changed");
        Assert.That(second.Value, Is.EqualTo("changed"));
        Assert.That(root.Attributes, Is.EqualTo(new[] { first, second }));
    }

    [Test]
    public void PublicNavigatorRejectsNullAndForeignOrderInputsWithoutChangingTrees()
    {
        XmlDocument first = Parse("<first/>", Options());
        XmlDocument second = Parse("<second/>", Options());
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.Throws<ArgumentNullException>(() => navigator.FindXmlParent(null!));
        Assert.Throws<ArgumentNullException>(() => navigator.SelectXmlChildren(null!));
        Assert.Throws<ArgumentNullException>(() => navigator.SelectXmlRange(null!));
        Assert.Throws<ArgumentNullException>(() => navigator.FindXmlNodeById(null!, "id"));
        Assert.Throws<ArgumentNullException>(() => navigator.FindXmlNodeById(first, null!));
        Assert.Throws<ArgumentException>(() => navigator.CompareXmlNodeOrder(first.DocumentElement!, second.DocumentElement!));
        Assert.That(first.DocumentElement!.Parent, Is.SameAs(first.DocumentNode));
        Assert.That(second.DocumentElement!.Parent, Is.SameAs(second.DocumentNode));
    }

    private static XmlDocument Parse(string text, XmlParserOptions options)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
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
