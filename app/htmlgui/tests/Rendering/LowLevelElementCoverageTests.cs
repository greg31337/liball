using System;
using htmlgui;

namespace htmlgui.Tests;

public class LowLevelElementCoverageTests
{
    [Test]
    public void PreservesElementAndAttributeNamespacesWithCanonicalAttributeOrder()
    {
        UiNode node = Element("use", "use-id", "use");
        node.NamespaceUri = "http://www.w3.org/2000/svg";
        node.Attributes = new DomAttribute[]
        {
            new DomAttribute
            {
                NamespaceUri = "http://www.w3.org/1999/xlink",
                Prefix = "xlink",
                Name = "href",
                Value = "#symbol"
            },
            new DomAttribute { Name = "href", Value = "#fallback" },
            new DomAttribute { Name = "focusable", IsBoolean = true, Value = "ignored" }
        };
        node.Children = new UiNode[] { Element("path", "path-id", "path") };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.NamespaceUri, Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(Array.ConvertAll(result.Attributes, attribute => attribute.Name), Is.EqualTo(new string[] { "focusable", "href", "href" }));
            Assert.That(result.Attributes[0].IsBoolean, Is.True);
            Assert.That(result.Attributes[0].Value, Is.Empty);
            Assert.That(result.Attributes[1].NamespaceUri, Is.Null);
            Assert.That(result.Attributes[1].Value, Is.EqualTo("#fallback"));
            Assert.That(result.Attributes[2].NamespaceUri, Is.EqualTo("http://www.w3.org/1999/xlink"));
            Assert.That(result.Attributes[2].Prefix, Is.EqualTo("xlink"));
            Assert.That(result.Attributes[2].Value, Is.EqualTo("#symbol"));
            Assert.That(result.Children[0].Name, Is.EqualTo("path"));
        });
    }

    [TestCase("area")]
    [TestCase(HtmlElements.Base)]
    [TestCase(HtmlElements.LineBreak)]
    [TestCase(HtmlElements.Column)]
    [TestCase(HtmlElements.Embed)]
    [TestCase(HtmlElements.HorizontalRule)]
    [TestCase(HtmlElements.Image)]
    [TestCase(HtmlElements.Input)]
    [TestCase(HtmlElements.Link)]
    [TestCase(HtmlElements.Meta)]
    [TestCase(HtmlElements.Source)]
    [TestCase(HtmlElements.Track)]
    [TestCase("wbr")]
    public void EveryVoidHtmlElementRejectsChildNodes(string elementName)
    {
        UiNode node = Element("void", "void-id", elementName);
        node.Children = new UiNode[] { TextNode("text", "text-id", "invalid child") };
        SemanticExpander expander = new SemanticExpander();

        UiRenderException exception = Assert.Throws<UiRenderException>(() => expander.ExpandNode(node))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.NodeId, Is.EqualTo("void-id"));
            Assert.That(exception.Message, Does.Contain("Void HTML elements"));
        });
    }

    [TestCase(HtmlElements.Script)]
    [TestCase(HtmlElements.Style)]
    [TestCase(HtmlElements.Title)]
    [TestCase(HtmlElements.TextArea)]
    public void EveryRawTextElementAllowsOnlyTextChildren(string elementName)
    {
        UiNode valid = Element("valid", "valid-id", elementName);
        valid.Children = new UiNode[]
        {
            TextNode("first", "first-id", "first"),
            TextNode("second", "second-id", "second")
        };
        UiNode invalidElement = Element("invalid-element", "invalid-element-id", elementName);
        invalidElement.Children = new UiNode[] { Element("span", "span-id", HtmlElements.Span) };
        UiNode invalidComment = Element("invalid-comment", "invalid-comment-id", elementName);
        invalidComment.Children = new UiNode[] { CommentNode("comment", "comment-id", "comment") };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(valid);

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(result.Children, child => child.Text), Is.EqualTo(new string[] { "first", "second" }));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(invalidElement));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(invalidComment));
        });
    }

    [Test]
    public void CanonicalizesIndependentTokenListsAndPortableDataValues()
    {
        UiNode node = Element("tokens", "tokens-id", HtmlElements.Anchor);
        node.TokenLists = new UiTokenList[]
        {
            new UiTokenList { Name = HtmlAttributes.Relationship, Tokens = new string[] { "tag", "author", "tag" } },
            new UiTokenList { Name = HtmlAttributes.Class, Tokens = new string[] { "selected", "item", "selected" } }
        };
        node.DataAttributes = new UiValueEntry[]
        {
            new UiValueEntry { Key = "enabled", Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true } },
            new UiValueEntry { Key = "payload", Value = new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[] { 1, 2, 3 } } },
            new UiValueEntry { Key = "empty", Value = new UiValue() }
        };
        node.Properties = new DomProperty[]
        {
            new DomProperty { Name = "scrollTop", Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = 12 } },
            new DomProperty { Name = "ariaLabel", Value = new UiValue { Kind = UiValueKind.String, StringValue = "Link" } }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(result.Attributes, attribute => attribute.Name), Is.EqualTo(new string[] { "class", "data-empty", "data-enabled", "data-payload", "rel" }));
            Assert.That(result.Attributes[0].Value, Is.EqualTo("item selected"));
            Assert.That(result.Attributes[1].Value, Is.Empty);
            Assert.That(result.Attributes[2].Value, Is.EqualTo("true"));
            Assert.That(result.Attributes[3].Value, Is.EqualTo("AQID"));
            Assert.That(result.Attributes[4].Value, Is.EqualTo("author tag"));
            Assert.That(Array.ConvertAll(result.Properties, property => property.Name), Is.EqualTo(new string[] { "ariaLabel", "scrollTop" }));
            Assert.That(result.Properties[0].Value.StringValue, Is.EqualTo("Link"));
            Assert.That(result.Properties[1].Value.IntegerValue, Is.EqualTo(12));
        });
    }

    [Test]
    public void RejectsNullDuplicateAndMalformedAttributePropertyAndTokenEntries()
    {
        UiNode nullAttribute = Element("null-attribute", "null-attribute-id", HtmlElements.Division);
        nullAttribute.Attributes = new DomAttribute[] { null! };
        UiNode duplicateNamespacedAttribute = Element("duplicate-attribute", "duplicate-attribute-id", HtmlElements.Division);
        duplicateNamespacedAttribute.Attributes = new DomAttribute[]
        {
            new DomAttribute { NamespaceUri = "urn:test", Prefix = "a", Name = "value" },
            new DomAttribute { NamespaceUri = "urn:test", Prefix = "b", Name = "value" }
        };
        UiNode nullTokenList = Element("null-token-list", "null-token-list-id", HtmlElements.Division);
        nullTokenList.TokenLists = new UiTokenList[] { null! };
        UiNode nullToken = Element("null-token", "null-token-id", HtmlElements.Division);
        nullToken.TokenLists = new UiTokenList[] { new UiTokenList { Name = HtmlAttributes.Class, Tokens = new string[] { null! } } };
        UiNode duplicateData = Element("duplicate-data", "duplicate-data-id", HtmlElements.Division);
        duplicateData.DataAttributes = new UiValueEntry[]
        {
            new UiValueEntry { Key = "mode" },
            new UiValueEntry { Key = "mode" }
        };
        UiNode nullProperty = Element("null-property", "null-property-id", HtmlElements.Division);
        nullProperty.Properties = new DomProperty[] { null! };
        UiNode malformedProperty = Element("malformed-property", "malformed-property-id", HtmlElements.Division);
        malformedProperty.Properties = new DomProperty[] { new DomProperty { Name = "bad property" } };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullAttribute));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateNamespacedAttribute));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullTokenList));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullToken));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateData));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullProperty));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(malformedProperty));
        });
    }

    [Test]
    public void CommentsPreserveSafeEmptyMultilineAndUnicodeContent()
    {
        UiNode empty = CommentNode("empty", "empty-id", null);
        UiNode multiline = CommentNode("multiline", "multiline-id", "first line\nsecond ☃ line");
        multiline.State.Version = 4;
        SemanticExpander expander = new SemanticExpander();

        DomNode emptyResult = expander.ExpandNode(empty);
        DomNode multilineResult = expander.ExpandNode(multiline);

        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.Kind, Is.EqualTo(DomNodeKind.Comment));
            Assert.That(emptyResult.Text, Is.Empty);
            Assert.That(multilineResult.Key, Is.EqualTo("multiline"));
            Assert.That(multilineResult.Id, Is.EqualTo("multiline-id"));
            Assert.That(multilineResult.Text, Is.EqualTo("first line\nsecond ☃ line"));
            Assert.That(multilineResult.Version, Is.EqualTo(4));
        });
    }

    [Test]
    public void TrustedRawContentPreservesIdentityAndRejectedPoliciesReportTheNode()
    {
        UiNode trusted = Node(UiNodeKind.RawContent, "trusted", "trusted-id");
        trusted.Text = "<em>trusted</em>";
        trusted.State.Version = 6;
        trusted.RawContentPolicy = new UiRawContentPolicy { IsTrusted = true, PolicyId = "sanitized" };
        UiNode rejected = Node(UiNodeKind.RawContent, "rejected", "rejected-id");
        rejected.Text = "<script>unsafe</script>";
        rejected.RawContentPolicy = new UiRawContentPolicy { IsTrusted = false, PolicyId = "sanitized" };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(trusted);
        UiRenderException exception = Assert.Throws<UiRenderException>(() => expander.ExpandNode(rejected))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.RawContent));
            Assert.That(result.Key, Is.EqualTo("trusted"));
            Assert.That(result.Id, Is.EqualTo("trusted-id"));
            Assert.That(result.Text, Is.EqualTo("<em>trusted</em>"));
            Assert.That(result.Version, Is.EqualTo(6));
            Assert.That(exception.NodeId, Is.EqualTo("rejected-id"));
            Assert.That(exception.OutputKind, Is.EqualTo("dom"));
        });
    }

    [TestCase("user-card")]
    [TestCase("x-menu-item2")]
    [TestCase("htmlgui-panel")]
    public void ExplicitlyEnabledCustomElementsPreserveValidPortableNames(string name)
    {
        UiNode node = Element("custom", "custom-id", name);
        node.IsCustomElement = true;
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(name));
            Assert.That(result.Key, Is.EqualTo("custom"));
            Assert.That(result.Id, Is.EqualTo("custom-id"));
        });
    }

    private static UiNode Element(string key, string id, string name)
    {
        UiNode node = Node(UiNodeKind.Element, key, id);
        node.Name = name;
        return node;
    }

    private static UiNode TextNode(string key, string id, string text)
    {
        UiNode node = Node(UiNodeKind.Text, key, id);
        node.Text = text;
        return node;
    }

    private static UiNode CommentNode(string key, string id, string? text)
    {
        UiNode node = Node(UiNodeKind.Comment, key, id);
        node.Text = text;
        return node;
    }

    private static UiNode Node(UiNodeKind kind, string key, string id)
    {
        return new UiNode
        {
            Kind = kind,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id }
        };
    }
}
