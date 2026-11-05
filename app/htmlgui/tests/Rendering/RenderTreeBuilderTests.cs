using System;
using htmlgui;

namespace htmlgui.Tests;

public class RenderTreeBuilderTests
{
    [Test]
    public void BuildsCanonicalAttributesTokensDataAndPropertiesWithoutAliasing()
    {
        UiValue sourceValue = new UiValue { Kind = UiValueKind.String, StringValue = "current" };
        DomAttribute title = new DomAttribute { Name = HtmlAttributes.Title, Value = "Details" };
        DomAttribute disabled = new DomAttribute { Name = HtmlAttributes.Disabled, Value = "ignored", IsBoolean = true };
        DomProperty value = new DomProperty { Name = "value", Value = sourceValue };
        DomProperty checkedProperty = new DomProperty { Name = "checked", Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true } };
        UiNode node = Element("element", "element-id", HtmlElements.Division);
        node.Attributes = new DomAttribute[] { title, disabled };
        node.TokenLists = new UiTokenList[]
        {
            new UiTokenList { Name = HtmlAttributes.Class, Tokens = new string[] { "zeta", "alpha", "zeta" } }
        };
        node.DataAttributes = new UiValueEntry[]
        {
            new UiValueEntry { Key = "state", Value = new UiValue { Kind = UiValueKind.String, StringValue = "ready" } },
            new UiValueEntry { Key = "count", Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = 3 } }
        };
        node.Properties = new DomProperty[] { value, checkedProperty };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(node);
        title.Value = "Changed";
        sourceValue.StringValue = "Changed";

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(result.Attributes, attribute => attribute.Name), Is.EqualTo(new string[] { "class", "data-count", "data-state", "disabled", "title" }));
            Assert.That(result.Attributes[0].Value, Is.EqualTo("alpha zeta"));
            Assert.That(result.Attributes[1].Value, Is.EqualTo("3"));
            Assert.That(result.Attributes[2].Value, Is.EqualTo("ready"));
            Assert.That(result.Attributes[3].IsBoolean, Is.True);
            Assert.That(result.Attributes[3].Value, Is.Empty);
            Assert.That(result.Attributes[4].Value, Is.EqualTo("Details"));
            Assert.That(Array.ConvertAll(result.Properties, property => property.Name), Is.EqualTo(new string[] { "checked", "value" }));
            Assert.That(result.Properties[0].Value.BooleanValue, Is.True);
            Assert.That(result.Properties[1].Value.StringValue, Is.EqualTo("current"));
            Assert.That(result.Attributes[4], Is.Not.SameAs(title));
            Assert.That(result.Properties[1], Is.Not.SameAs(value));
            Assert.That(result.Properties[1].Value, Is.Not.SameAs(sourceValue));
        });
    }

    [Test]
    public void PreservesElementNamespaceIdentityVersionAndChildren()
    {
        UiNode child = new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = "text", SiblingIdentity = "text", StableId = "text-id" },
            Text = "Vector"
        };
        UiNode svg = Element("vector", "vector-id", "svg");
        svg.NamespaceUri = "http://www.w3.org/2000/svg";
        svg.State.Version = 8;
        svg.Children = new UiNode[] { child };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(svg);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(result.Key, Is.EqualTo("vector"));
            Assert.That(result.Id, Is.EqualTo("vector-id"));
            Assert.That(result.NamespaceUri, Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(result.Name, Is.EqualTo("svg"));
            Assert.That(result.Version, Is.EqualTo(8));
            Assert.That(result.Children, Has.Length.EqualTo(1));
            Assert.That(result.Children[0].Kind, Is.EqualTo(DomNodeKind.Text));
            Assert.That(result.Children[0].Text, Is.EqualTo("Vector"));
        });
    }

    [Test]
    public void CustomElementsRequireExplicitOptInAndValidNames()
    {
        UiNode permitted = Element("permitted", "permitted-id", "user-card");
        permitted.IsCustomElement = true;
        UiNode missingOptIn = Element("missing", "missing-id", "user-card");
        UiNode missingHyphen = Element("hyphen", "hyphen-id", "usercard");
        missingHyphen.IsCustomElement = true;
        UiNode uppercase = Element("uppercase", "uppercase-id", "User-card");
        uppercase.IsCustomElement = true;
        UiNode invalid = Element("invalid", "invalid-id", "bad name");
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(permitted);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("user-card"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingOptIn));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingHyphen));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(uppercase));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(invalid));
        });
    }

    [Test]
    public void CommentsRejectUnsafeDelimiterForms()
    {
        UiNode valid = Node(UiNodeKind.Comment, "valid", "valid-id");
        valid.Text = "safe comment";
        UiNode doubleHyphen = Node(UiNodeKind.Comment, "double", "double-id");
        doubleHyphen.Text = "unsafe -- comment";
        UiNode leadingHyphen = Node(UiNodeKind.Comment, "leading", "leading-id");
        leadingHyphen.Text = "-unsafe";
        UiNode trailingHyphen = Node(UiNodeKind.Comment, "trailing", "trailing-id");
        trailingHyphen.Text = "unsafe-";
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(valid);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.Comment));
            Assert.That(result.Text, Is.EqualTo("safe comment"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(doubleHyphen));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(leadingHyphen));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(trailingHyphen));
        });
    }

    [Test]
    public void RawContentRequiresAnEnabledNamedTrustPolicy()
    {
        UiNode trusted = Node(UiNodeKind.RawContent, "trusted", "trusted-id");
        trusted.Text = "<strong>Trusted</strong>";
        trusted.RawContentPolicy = new UiRawContentPolicy { IsTrusted = true, PolicyId = "reviewed-markup" };
        UiNode missing = Node(UiNodeKind.RawContent, "missing", "missing-id");
        UiNode disabled = Node(UiNodeKind.RawContent, "disabled", "disabled-id");
        disabled.RawContentPolicy = new UiRawContentPolicy { IsTrusted = false, PolicyId = "reviewed-markup" };
        UiNode unnamed = Node(UiNodeKind.RawContent, "unnamed", "unnamed-id");
        unnamed.RawContentPolicy = new UiRawContentPolicy { IsTrusted = true, PolicyId = " " };
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(trusted);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.RawContent));
            Assert.That(result.Text, Is.EqualTo("<strong>Trusted</strong>"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missing));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(disabled));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unnamed));
        });
    }

    [Test]
    public void EnforcesVoidAndRawTextElementChildRules()
    {
        UiNode text = Node(UiNodeKind.Text, "text", "text-id");
        text.Text = "content";
        UiNode comment = Node(UiNodeKind.Comment, "comment", "comment-id");
        comment.Text = "comment";
        UiNode image = Element("image", "image-id", HtmlElements.Image);
        image.Children = new UiNode[] { text };
        UiNode validScript = Element("script", "script-id", HtmlElements.Script);
        validScript.Children = new UiNode[] { text };
        UiNode invalidScript = Element("invalid-script", "invalid-script-id", HtmlElements.Script);
        invalidScript.Children = new UiNode[] { comment };
        SemanticExpander expander = new SemanticExpander();

        DomNode script = expander.ExpandNode(validScript);

        Assert.Multiple(() =>
        {
            Assert.That(script.Children[0].Kind, Is.EqualTo(DomNodeKind.Text));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(image));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(invalidScript));
        });
    }

    [Test]
    public void RejectsDuplicateOrMalformedElementInputs()
    {
        UiNode duplicateAttribute = Element("attribute", "attribute-id", HtmlElements.Division);
        duplicateAttribute.Attributes = new DomAttribute[]
        {
            new DomAttribute { Name = HtmlAttributes.Class },
            new DomAttribute { Name = HtmlAttributes.Class }
        };
        UiNode conflictingTokenList = Element("tokens", "tokens-id", HtmlElements.Division);
        conflictingTokenList.Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class } };
        conflictingTokenList.TokenLists = new UiTokenList[] { new UiTokenList { Name = HtmlAttributes.Class } };
        UiNode whitespaceToken = Element("whitespace", "whitespace-id", HtmlElements.Division);
        whitespaceToken.TokenLists = new UiTokenList[] { new UiTokenList { Name = HtmlAttributes.Class, Tokens = new string[] { "two words" } } };
        UiNode malformedData = Element("data", "data-id", HtmlElements.Division);
        malformedData.DataAttributes = new UiValueEntry[] { new UiValueEntry { Key = "BadName" } };
        UiNode duplicateProperty = Element("property", "property-id", HtmlElements.Division);
        duplicateProperty.Properties = new DomProperty[] { new DomProperty { Name = "value" }, new DomProperty { Name = "value" } };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateAttribute));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(conflictingTokenList));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(whitespaceToken));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(malformedData));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicateProperty));
        });
    }

    private static UiNode Element(string key, string id, string name)
    {
        UiNode node = Node(UiNodeKind.Element, key, id);
        node.Name = name;
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
