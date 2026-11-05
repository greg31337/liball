using System;
using htmlgui;

namespace htmlgui.Tests;

public class LayoutExpanderTests
{
    [TestCase(UiLayoutKind.Flow, "flow", HtmlElements.Division)]
    [TestCase(UiLayoutKind.Inline, "inline", HtmlElements.Span)]
    [TestCase(UiLayoutKind.VerticalStack, "vertical-stack", HtmlElements.Division)]
    [TestCase(UiLayoutKind.HorizontalStack, "horizontal-stack", HtmlElements.Division)]
    [TestCase(UiLayoutKind.FlexRow, "flex-row", HtmlElements.Division)]
    [TestCase(UiLayoutKind.FlexColumn, "flex-column", HtmlElements.Division)]
    [TestCase(UiLayoutKind.Wrap, "wrap", HtmlElements.Division)]
    [TestCase(UiLayoutKind.Grid, "grid", HtmlElements.Division)]
    [TestCase(UiLayoutKind.UniformGrid, "uniform-grid", HtmlElements.Division)]
    [TestCase(UiLayoutKind.Dock, "dock", HtmlElements.Division)]
    [TestCase(UiLayoutKind.Overlay, "overlay", HtmlElements.Division)]
    [TestCase(UiLayoutKind.Absolute, "absolute", HtmlElements.Division)]
    [TestCase(UiLayoutKind.Split, "split", HtmlElements.Division)]
    [TestCase(UiLayoutKind.Scroll, "scroll", HtmlElements.Division)]
    [TestCase(UiLayoutKind.ResponsiveShell, "responsive-shell", HtmlElements.Division)]
    public void ExpandsEveryLayoutKindToACanonicalContainer(UiLayoutKind kind, string token, string elementName)
    {
        UiNode node = LayoutNode("layout", "layout-id", kind);
        node.State.Version = 9;
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(result.Key, Is.EqualTo("layout"));
            Assert.That(result.Id, Is.EqualTo("layout-id"));
            Assert.That(result.Name, Is.EqualTo(elementName));
            Assert.That(result.Attributes, Has.Length.EqualTo(3));
            Assert.That(result.Attributes[0].Name, Is.EqualTo(HtmlAttributes.Class));
            Assert.That(result.Attributes[0].Value, Is.EqualTo("htmlgui-layout htmlgui-layout-" + token));
            Assert.That(result.Attributes[1].Name, Is.EqualTo("data-layout"));
            Assert.That(result.Attributes[1].Value, Is.EqualTo(token));
            Assert.That(result.Attributes[2].Name, Is.EqualTo(HtmlAttributes.Style));
            Assert.That(result.Attributes[2].Value, Does.Contain("align-items:flex-start;"));
            Assert.That(result.Attributes[2].Value, Does.Contain("justify-content:flex-start;"));
            Assert.That(result.Version, Is.EqualTo(9));
        });
    }

    [Test]
    public void ExpandsLayoutChildrenInPresentationOrderAndSupportsNesting()
    {
        UiNode first = Text("first", "first-id", "First");
        UiNode nestedText = Text("nested-text", "nested-text-id", "Nested");
        UiNode nested = LayoutNode("nested", "nested-id", UiLayoutKind.Inline);
        nested.Layout!.Children = new UiNode[] { nestedText };
        UiNode root = LayoutNode("root", "root-id", UiLayoutKind.VerticalStack);
        root.Children = new UiNode[] { Text("ignored", "ignored-id", "Ignored logical child") };
        root.Layout!.Children = new UiNode[] { first, nested };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(root);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(Array.ConvertAll(result.Children, child => child.Key), Is.EqualTo(new string[] { "first", "nested" }));
            Assert.That(result.Children[0].Text, Is.EqualTo("First"));
            Assert.That(result.Children[1].Name, Is.EqualTo(HtmlElements.Span));
            Assert.That(result.Children[1].Children[0].Text, Is.EqualTo("Nested"));
            Assert.That(root.Children[0].Text, Is.EqualTo("Ignored logical child"));
        });
    }

    [Test]
    public void RepeatedExpansionReturnsFreshTreesWithoutMutatingTheLayout()
    {
        UiNode child = Text("child", "child-id", "Original");
        UiNode node = LayoutNode("layout", "layout-id", UiLayoutKind.Grid);
        UiNode[] sourceChildren = new UiNode[] { child };
        node.Layout!.Children = sourceChildren;
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);
        first.Children[0].Text = "Changed output";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Attributes, Is.Not.SameAs(second.Attributes));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(first.Children[0], Is.Not.SameAs(second.Children[0]));
            Assert.That(node.Layout.Children, Is.SameAs(sourceChildren));
            Assert.That(node.Layout.Children[0], Is.SameAs(child));
            Assert.That(child.Text, Is.EqualTo("Original"));
            Assert.That(second.Children[0].Text, Is.EqualTo("Original"));
        });
    }

    [Test]
    public void RejectsMissingDescriptionsAndUndefinedLayoutKinds()
    {
        UiNode missing = new UiNode
        {
            Kind = UiNodeKind.Layout,
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode undefined = LayoutNode("undefined", "undefined-id", (UiLayoutKind)999);
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missing));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(undefined))!.ParamName, Is.EqualTo("kind"));
        });
    }

    private static UiNode LayoutNode(string key, string id, UiLayoutKind kind)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Layout,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id },
            Layout = new UiLayout { Kind = kind }
        };
    }

    private static UiNode Text(string key, string id, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id },
            Text = text
        };
    }
}
