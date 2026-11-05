using System;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlTreeRangeTests
{
    [Test]
    public void SelectsCoveredSubtreesFromContainerOffsetsInDocumentOrder()
    {
        XmlDocument document = Parse("<root><a><x/></a><b>text</b><c/></root>");
        XmlNode root = document.DocumentElement!;
        XmlNode a = root.Children![0];
        XmlNode x = a.Children![0];
        XmlNode b = root.Children[1];
        XmlNode text = b.Children![0];
        XmlRange range = Range(document, root, 0, root, 2);

        XmlNodeSelection selection = new XmlTreeNavigator().SelectXmlRange(range);

        Assert.That(selection.Nodes, Is.EqualTo(new[] { a, x, b, text }));
        Assert.That(selection.Document, Is.SameAs(document));
        Assert.That(selection.Range, Is.SameAs(range));
        Assert.That(selection.MutationVersion, Is.EqualTo(document.MutationVersion));
        Assert.That(selection.IsFrozen, Is.False);
    }

    [Test]
    public void IncludesPartiallyCoveredCharacterNodesWithoutSelectingTheirAncestors()
    {
        XmlDocument document = Parse("<root><a>abcd</a><b/><c/></root>");
        XmlNode root = document.DocumentElement!;
        XmlNode a = root.Children![0];
        XmlNode text = a.Children![0];
        XmlNode b = root.Children[1];
        XmlRange crossBoundary = Range(document, text, 1, root, 2);
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        XmlNodeSelection selected = navigator.SelectXmlRange(crossBoundary);

        Assert.That(selected.Nodes, Is.EqualTo(new[] { text, b }));

        XmlRange insideText = Range(document, text, 1, text, 3);
        Assert.That(navigator.SelectXmlRange(insideText).Nodes, Is.EqualTo(new[] { text }));

        XmlRange collapsed = Range(document, text, 2, text, 2);
        Assert.That(navigator.SelectXmlRange(collapsed).Nodes, Is.Empty);

        XmlRange afterTextContent = Range(document, text, 4, root, 1);
        Assert.That(navigator.SelectXmlRange(afterTextContent).Nodes, Is.Empty);
    }

    [Test]
    public void RejectsMalformedReversedForeignAndDetachedBoundaries()
    {
        XmlDocument document = Parse("<root><a/><b/></root>");
        XmlNode root = document.DocumentElement!;
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        XmlRange badOffset = Range(document, root, 0, root, 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => navigator.SelectXmlRange(badOffset));
        Assert.That(badOffset.IsValid, Is.False);

        XmlRange reversed = Range(document, root, 2, root, 0);
        Assert.Throws<ArgumentException>(() => navigator.SelectXmlRange(reversed));
        Assert.That(reversed.IsValid, Is.False);

        XmlDocument foreign = Parse("<foreign/>");
        XmlRange mixed = Range(document, root, 0, foreign.DocumentElement!, 0);
        Assert.Throws<ArgumentException>(() => navigator.SelectXmlRange(mixed));
        Assert.That(mixed.IsValid, Is.False);

        XmlNode detached = new XmlTreeEditor().CreateXmlTreeNode(document, XmlNodeKind.Element, Name("detached"), null);
        XmlRange disconnected = Range(document, detached, 0, detached, 0);
        Assert.Throws<ArgumentException>(() => navigator.SelectXmlRange(disconnected));
        Assert.That(disconnected.IsValid, Is.False);
    }

    [Test]
    public void InvalidatesLiveRangesAfterDocumentMutation()
    {
        XmlDocument document = Parse("<root><a/><b/></root>");
        XmlNode root = document.DocumentElement!;
        XmlRange range = Range(document, root, 0, root, 1);
        XmlTreeNavigator navigator = new XmlTreeNavigator();
        Assert.That(navigator.SelectXmlRange(range).Nodes, Has.Length.EqualTo(1));

        XmlTreeEditor editor = new XmlTreeEditor();
        XmlNode added = editor.CreateXmlTreeNode(document, XmlNodeKind.Element, Name("added"), null);
        editor.AppendXmlTreeChild(root, added);

        Assert.Throws<InvalidOperationException>(() => navigator.SelectXmlRange(range));
        Assert.That(range.IsValid, Is.False);
    }

    [Test]
    public void NonFrozenCachedSelectionsDoNotBypassMutationInvalidation()
    {
        XmlDocument document = Parse("<root><a/></root>");
        XmlNode root = document.DocumentElement!;
        XmlRange range = Range(document, root, 0, root, 1);
        XmlTreeNavigator navigator = new XmlTreeNavigator();
        range.Snapshot = navigator.SelectXmlRange(range);
        Assert.That(range.Snapshot.IsFrozen, Is.False);

        new XmlTreeEditor().SetXmlTreeText(root, "changed");

        Assert.Throws<InvalidOperationException>(() => navigator.SelectXmlRange(range));
        Assert.That(range.IsValid, Is.False);
    }

    [Test]
    public void FrozenSnapshotsRetainMembershipAcrossDocumentMutations()
    {
        XmlDocument document = Parse("<root><a><x/></a><b/></root>");
        XmlNode root = document.DocumentElement!;
        XmlNode a = root.Children![0];
        XmlNode x = a.Children![0];
        XmlRange range = Range(document, root, 0, root, 1);
        XmlTreeNavigator navigator = new XmlTreeNavigator();
        XmlNodeSelection snapshot = navigator.SelectXmlRange(range);
        snapshot.IsFrozen = true;
        range.Snapshot = snapshot;
        XmlNode[] snapshotNodes = snapshot.Nodes!;

        new XmlTreeEditor().RemoveXmlTreeNode(root, a);
        XmlNodeSelection selected = navigator.SelectXmlRange(range);

        Assert.That(selected.Nodes, Is.EqualTo(new[] { a, x }));
        Assert.That(selected.Nodes, Is.Not.SameAs(snapshotNodes));
        Assert.That(selected.IsFrozen, Is.True);
        Assert.That(selected.MutationVersion, Is.EqualTo(snapshot.MutationVersion));
        Assert.That(range.IsValid, Is.True);
    }

    [Test]
    public void SupportsRangesWithinOneAttachedAttributeValue()
    {
        XmlDocument document = Parse("<root value='abcd'/>");
        XmlNode attribute = document.DocumentElement!.Attributes![0];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        XmlRange selectedRange = Range(document, attribute, 1, attribute, 3);
        Assert.That(navigator.SelectXmlRange(selectedRange).Nodes, Is.EqualTo(new[] { attribute }));

        XmlRange collapsed = Range(document, attribute, 2, attribute, 2);
        Assert.That(navigator.SelectXmlRange(collapsed).Nodes, Is.Empty);

        XmlRange reversed = Range(document, attribute, 3, attribute, 1);
        Assert.Throws<ArgumentException>(() => navigator.SelectXmlRange(reversed));
        Assert.That(reversed.IsValid, Is.False);

        XmlRange crossing = Range(document, attribute, 0, document.DocumentElement!, 0);
        Assert.Throws<ArgumentException>(() => navigator.SelectXmlRange(crossing));
        Assert.That(crossing.IsValid, Is.False);
    }

    [Test]
    public void RejectsExplicitlyInvalidRangesWithoutReadingTheirBoundaries()
    {
        XmlRange range = new XmlRange { IsValid = false };

        Assert.Throws<InvalidOperationException>(() => new XmlTreeNavigator().SelectXmlRange(range));
    }

    private static XmlRange Range(XmlDocument document, XmlNode start, int startOffset, XmlNode end, int endOffset)
    {
        return new XmlRange
        {
            Document = document,
            StartContainer = start,
            StartOffset = startOffset,
            EndContainer = end,
            EndOffset = endOffset,
            MutationVersion = document.MutationVersion,
            IsValid = true
        };
    }

    private static XmlName Name(string localName)
    {
        return new XmlName { LocalName = localName, QualifiedName = localName };
    }

    private static XmlDocument Parse(string text)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Disabled,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None
        };
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }
}
