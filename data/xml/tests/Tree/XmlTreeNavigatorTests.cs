using System;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlTreeNavigatorTests
{
    [Test]
    public void NavigatesParentsChildrenAttributesAndSelectionMetadata()
    {
        XmlDocument document = Parse("<root a='1'><first/><second/></root>", Options(), null);
        XmlNode root = document.DocumentElement!;
        XmlNode first = root.Children![0];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        XmlNodeSelection children = navigator.SelectXmlChildren(root);
        XmlNodeSelection attributes = navigator.SelectXmlAttributes(root);

        Assert.That(navigator.FindXmlParent(first), Is.SameAs(root));
        Assert.That(navigator.FindXmlParent(document.DocumentNode!), Is.Null);
        Assert.That(children.Nodes, Is.EqualTo(root.Children));
        Assert.That(children.Nodes, Is.Not.SameAs(root.Children));
        Assert.That(children.Document, Is.SameAs(document));
        Assert.That(children.MutationVersion, Is.EqualTo(document.MutationVersion));
        Assert.That(children.IsFrozen, Is.False);
        Assert.That(attributes.Nodes, Is.EqualTo(root.Attributes));
        Assert.That(attributes.Nodes, Is.Not.SameAs(root.Attributes));
        Assert.That(navigator.FindXmlParent(attributes.Nodes![0]), Is.SameAs(root));
        Assert.That(navigator.SelectXmlAttributes(first).Nodes, Is.Empty);
    }

    [Test]
    public void SelectsAncestorsDescendantsAndSiblingAxesInDocumentOrder()
    {
        XmlDocument document = Parse("<root><a><a1/></a><b><b1/></b><c/></root>", Options(), null);
        XmlNode root = document.DocumentElement!;
        XmlNode a = root.Children![0];
        XmlNode a1 = a.Children![0];
        XmlNode b = root.Children[1];
        XmlNode b1 = b.Children![0];
        XmlNode c = root.Children[2];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(navigator.SelectXmlAncestors(b1).Nodes, Is.EqualTo(new[] { document.DocumentNode, root, b }));
        Assert.That(navigator.SelectXmlDescendants(root).Nodes, Is.EqualTo(new[] { a, a1, b, b1, c }));
        Assert.That(navigator.SelectXmlFollowingSiblings(b).Nodes, Is.EqualTo(new[] { c }));
        Assert.That(navigator.SelectXmlPrecedingSiblings(b).Nodes, Is.EqualTo(new[] { a }));
        Assert.That(navigator.SelectXmlFollowing(a).Nodes, Is.EqualTo(new[] { b, b1, c }));
        Assert.That(navigator.SelectXmlPreceding(c).Nodes, Is.EqualTo(new[] { a, a1, b, b1 }));
        Assert.That(navigator.SelectXmlPreceding(b1).Nodes, Is.EqualTo(new[] { a, a1 }));
    }

    [Test]
    public void AttributeFollowingAxisBeginsWithOwnerElementDescendants()
    {
        XmlDocument document = Parse("<root><a key='v'><inside/></a><after/></root>", Options(), null);
        XmlNode root = document.DocumentElement!;
        XmlNode a = root.Children![0];
        XmlNode attribute = a.Attributes![0];
        XmlNode inside = a.Children![0];
        XmlNode after = root.Children[1];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(navigator.SelectXmlFollowing(attribute).Nodes, Is.EqualTo(new[] { inside, after }));
        Assert.That(navigator.SelectXmlPreceding(attribute).Nodes, Is.Empty);
        Assert.That(navigator.SelectXmlFollowingSiblings(attribute).Nodes, Is.Empty);
        Assert.That(navigator.SelectXmlPrecedingSiblings(attribute).Nodes, Is.Empty);
    }

    [Test]
    public void ResolvesNearestNamespaceBindingsAndImplicitXmlBinding()
    {
        XmlParserOptions options = Options();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.PreservePrefixes = true;
        XmlDocument document = Parse("<root xmlns='urn:default' xmlns:p='urn:outer'><child xmlns:p='urn:inner' xmlns:q='urn:q'><leaf/></child></root>", options, null);
        XmlNode root = document.DocumentElement!;
        XmlNode leaf = root.Children![0].Children![0];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        XmlNamespaceBinding[] bindings = navigator.SelectXmlNamespaces(leaf);

        Assert.That(bindings, Has.Length.EqualTo(4));
        AssertBinding(bindings[0], "p", "urn:inner");
        AssertBinding(bindings[1], "q", "urn:q");
        AssertBinding(bindings[2], "", "urn:default");
        AssertBinding(bindings[3], "xml", XmlNamespaces.Xml);
        Assert.That(bindings[0], Is.Not.SameAs(root.Children[0].NamespaceBindings![0]));
    }

    [Test]
    public void ComputesStringValuesAcrossTextCDataAndEntityNodes()
    {
        XmlParserOptions options = Options();
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.PreserveCData = true;
        options.PreserveEntityReferences = true;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        XmlDocument document = Parse("<!DOCTYPE root [<!ENTITY ent 'entity'>]><root a='value'>one<![CDATA[two]]><!--ignored--><?pi ignored?>&ent;<child>three</child></root>", options, null);
        XmlNode root = document.DocumentElement!;
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(navigator.GetXmlNodeStringValue(root), Is.EqualTo("onetwoentitythree"));
        Assert.That(navigator.GetXmlNodeStringValue(document.DocumentNode!), Is.EqualTo("onetwoentitythree"));
        Assert.That(navigator.GetXmlNodeStringValue(root.Attributes![0]), Is.EqualTo("value"));
        Assert.That(navigator.GetXmlNodeStringValue(document.DocumentNode!.Children![0]), Is.EqualTo(""));
    }

    [Test]
    public void ResolvesInheritedBaseLanguageAndSpaceContext()
    {
        XmlParserOptions options = Options();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.PreservePrefixes = true;
        XmlDocument document = Parse("<root xml:base='parts/' xml:lang='en' xml:space='preserve'><child xml:base='inner/' xml:lang='fr'><leaf xml:space='default'/></child><tail/></root>", options, "https://example.test/base/document.xml");
        XmlNode root = document.DocumentElement!;
        XmlNode child = root.Children![0];
        XmlNode leaf = child.Children![0];
        XmlNode tail = root.Children[1];
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(navigator.GetXmlNodeBaseUri(root), Is.EqualTo("https://example.test/base/parts/"));
        Assert.That(navigator.GetXmlNodeBaseUri(child), Is.EqualTo("https://example.test/base/parts/inner/"));
        Assert.That(navigator.GetXmlNodeBaseUri(child.Attributes![0]), Is.EqualTo("https://example.test/base/parts/inner/"));
        Assert.That(navigator.GetXmlNodeLanguage(leaf), Is.EqualTo("fr"));
        Assert.That(navigator.GetXmlNodeLanguage(tail), Is.EqualTo("en"));
        Assert.That(navigator.GetXmlNodeSpaceMode(child), Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(navigator.GetXmlNodeSpaceMode(leaf), Is.EqualTo(XmlSpaceMode.Default));
        Assert.That(navigator.GetXmlNodeSpaceMode(tail), Is.EqualTo(XmlSpaceMode.Preserve));
    }

    [Test]
    public void FindsDtdAndXmlIdentifiersInDocumentOrder()
    {
        XmlParserOptions options = Options();
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        XmlDocument document = Parse("<!DOCTYPE root [<!ELEMENT root ANY><!ELEMENT item EMPTY><!ATTLIST item key ID #IMPLIED>]><root><item key='alpha'/><item xml:id='beta'/></root>", options, null);
        XmlNode root = document.DocumentElement!;
        XmlTreeNavigator navigator = new XmlTreeNavigator();

        Assert.That(navigator.FindXmlNodeById(document, "alpha"), Is.SameAs(root.Children![0]));
        Assert.That(navigator.FindXmlNodeById(document, "beta"), Is.SameAs(root.Children[1]));
        Assert.That(navigator.FindXmlNodeById(document, "missing"), Is.Null);
        Assert.That(navigator.FindXmlNodeById(document, ""), Is.Null);
    }

    [Test]
    public void ComparesCurrentTreeOrderRatherThanAllocationOrder()
    {
        XmlDocument document = Parse("<root key='v'><a/><b/></root>", Options(), null);
        XmlNode root = document.DocumentElement!;
        XmlNode attribute = root.Attributes![0];
        XmlNode a = root.Children![0];
        XmlNode b = root.Children[1];
        XmlTreeNavigator navigator = new XmlTreeNavigator();
        XmlTreeEditor editor = new XmlTreeEditor();

        Assert.That(navigator.CompareXmlNodeOrder(root, attribute), Is.LessThan(0));
        Assert.That(navigator.CompareXmlNodeOrder(attribute, a), Is.LessThan(0));
        Assert.That(navigator.CompareXmlNodeOrder(a, b), Is.LessThan(0));
        Assert.That(navigator.CompareXmlNodeOrder(b, a), Is.GreaterThan(0));
        Assert.That(navigator.CompareXmlNodeOrder(a, a), Is.EqualTo(0));
        Assert.That(navigator.ContainsXmlNodeIdentity(a, a), Is.True);
        Assert.That(navigator.ContainsXmlNodeIdentity(a, b), Is.False);

        editor.InsertXmlTreeBefore(root, b, a);
        Assert.That(navigator.CompareXmlNodeOrder(b, a), Is.LessThan(0));

        XmlDocument other = Parse("<other/>", Options(), null);
        Assert.Throws<ArgumentException>(() => navigator.CompareXmlNodeOrder(a, other.DocumentElement!));
    }

    [Test]
    public void CopiesFrozenRangeSnapshotsWithoutAliasingTheirNodeArrays()
    {
        XmlDocument document = Parse("<root><child/></root>", Options(), null);
        XmlNode root = document.DocumentElement!;
        XmlNode[] snapshotNodes = new[] { root, root.Children![0] };
        XmlNodeSelection snapshot = new XmlNodeSelection
        {
            Document = document,
            Nodes = snapshotNodes,
            MutationVersion = document.MutationVersion,
            IsFrozen = true
        };
        XmlRange range = new XmlRange
        {
            Document = document,
            Snapshot = snapshot,
            MutationVersion = document.MutationVersion,
            IsValid = true
        };

        XmlNodeSelection selected = new XmlTreeNavigator().SelectXmlRange(range);

        Assert.That(selected.Nodes, Is.EqualTo(snapshotNodes));
        Assert.That(selected.Nodes, Is.Not.SameAs(snapshotNodes));
        Assert.That(selected.Document, Is.SameAs(document));
        Assert.That(selected.Range, Is.SameAs(range));
        Assert.That(selected.IsFrozen, Is.True);
    }

    private static void AssertBinding(XmlNamespaceBinding binding, string prefix, string uri)
    {
        Assert.That(binding.Prefix, Is.EqualTo(prefix));
        Assert.That(binding.NamespaceUri, Is.EqualTo(uri));
    }

    private static XmlDocument Parse(string text, XmlParserOptions options, string? baseUri)
    {
        XmlInput input = new XmlInput { Kind = XmlInputKind.String, Text = text, BaseUri = baseUri };
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
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
