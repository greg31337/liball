using System;
using htmlgui;

namespace htmlgui.Tests;

public class HtmlSecurityPolicyTests
{
    [Test]
    public void NormalizesAllowedSchemesAndPreservesRelativeAndUnicodeUrls()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlSecurityPolicy.NormalizeResourceUrl("HTTPS://cdn.example.test/😀%20icon.js"), Is.EqualTo("https://cdn.example.test/😀%20icon.js"));
            Assert.That(HtmlSecurityPolicy.NormalizeResourceUrl("BLOB:https://example.test/id"), Is.EqualTo("blob:https://example.test/id"));
            Assert.That(HtmlSecurityPolicy.NormalizeResourceUrl("ABOUT:blank#ready"), Is.EqualTo("about:blank#ready"));
            Assert.That(HtmlSecurityPolicy.NormalizeResourceUrl("../assets/app.js?v=https:value#main"), Is.EqualTo("../assets/app.js?v=https:value#main"));
            Assert.That(HtmlSecurityPolicy.NormalizeHyperlinkUrl("MAILTO:user@example.test"), Is.EqualTo("mailto:user@example.test"));
            Assert.That(HtmlSecurityPolicy.NormalizeHyperlinkUrl("TEL:+15551234567"), Is.EqualTo("tel:+15551234567"));
        });
    }

    [Test]
    public void RejectsUnsafeAmbiguousAndMalformedUrlForms()
    {
        string[] rejected = new string[]
        {
            "javascript:alert(1)",
            "data:text/html,<script>alert(1)</script>",
            "file:///private/data",
            "ftp://example.test/file",
            "https:///missing-host",
            "blob:javascript:alert(1)",
            "about:config",
            "//",
            "/bad path",
            "/bad\\path",
            "/bad%GGpath",
            "/bad\ud800path"
        };

        int index = 0;
        while (index < rejected.Length)
        {
            Assert.That(HtmlSecurityPolicy.IsSafeResourceUrl(rejected[index]), Is.False, rejected[index]);
            Assert.Throws<ArgumentException>(() => HtmlSecurityPolicy.NormalizeResourceUrl(rejected[index]), rejected[index]);
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSecurityPolicy.IsSafeResourceUrl("mailto:user@example.test"), Is.False);
            Assert.That(HtmlSecurityPolicy.IsSafeHyperlinkUrl("mailto:user@example.test"), Is.True);
            Assert.That(HtmlSecurityPolicy.IsSafeHyperlinkUrl("javascript:alert(1)"), Is.False);
        });
    }

    [Test]
    public void ProtectsInlineScriptAndJsonEndTagsWithoutEncodingLookalikes()
    {
        string script = "const value = '</ScRiPt >'; const safe = '</scripture>';";
        string json = "{\"value\":\"</script>\",\"safe\":\"&lt;/script>\"}";

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSecurityPolicy.ProtectScriptClosingTokens(script), Is.EqualTo("const value = '\\x3C/ScRiPt >'; const safe = '</scripture>';"));
            Assert.That(HtmlSecurityPolicy.ProtectJsonClosingTokens(json), Is.EqualTo("{\"value\":\"\\u003C/script>\",\"safe\":\"&lt;/script>\"}"));
            Assert.Throws<ArgumentException>(() => HtmlSecurityPolicy.ProtectScriptClosingTokens("bad\ud800script"));
            Assert.Throws<ArgumentException>(() => HtmlSecurityPolicy.ProtectJsonClosingTokens("bad\0json"));
        });
    }

    [Test]
    public void AppliesValidatedNonceAndSafeAttributesToGeneratedResources()
    {
        UiResource script = new UiResource
        {
            Key = "application",
            Url = "HTTPS://cdn.example.test/app.js",
            Relationship = "SCRIPT",
            IsModule = true,
            MimeType = "text/javascript",
            Integrity = "sha384-YWJjZA==",
            CrossOrigin = "anonymous",
            ReferrerPolicy = "strict-origin",
            Attributes = new UiValueEntry[]
            {
                new UiValueEntry { Key = "defer", Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true } },
                new UiValueEntry { Key = "fetchpriority", Value = new UiValue { Kind = UiValueKind.String, StringValue = "high" } }
            }
        };
        UiResource style = new UiResource { Key = "style", Url = "/app.css", Relationship = "STYLESHEET" };
        DocumentComposer composer = new DocumentComposer();

        DomNode scriptNode = composer.CreateResourceNode(script, "abc_DEF-123=");
        DomNode styleNode = composer.CreateResourceNode(style, "abc_DEF-123=");

        Assert.Multiple(() =>
        {
            Assert.That(scriptNode.Name, Is.EqualTo(HtmlElements.Script));
            Assert.That(AttributeValue(scriptNode, HtmlAttributes.Source), Is.EqualTo("https://cdn.example.test/app.js"));
            Assert.That(AttributeValue(scriptNode, BridgeProtocolNames.Nonce), Is.EqualTo("abc_DEF-123="));
            Assert.That(AttributeValue(scriptNode, "defer"), Is.EqualTo("true"));
            Assert.That(AttributeValue(styleNode, HtmlAttributes.Relationship), Is.EqualTo("stylesheet"));
            Assert.That(AttributeValue(styleNode, BridgeProtocolNames.Nonce), Is.EqualTo("abc_DEF-123="));
        });
    }

    [Test]
    public void PropagatesPageNonceWithWindowMetadataFallback()
    {
        UiWindow window = new UiWindow
        {
            Document = new UiDocumentMetadata { ContentSecurityPolicyNonce = "window_nonce" }
        };
        UiPage inherited = new UiPage
        {
            Key = "inherited",
            Resources = new UiResource[] { Resource("/inherited.js") }
        };
        UiPage overridden = new UiPage
        {
            Key = "overridden",
            Metadata = new UiDocumentMetadata { ContentSecurityPolicyNonce = "page_nonce" },
            Resources = new UiResource[] { Resource("/overridden.js") }
        };
        DocumentComposer composer = new DocumentComposer();

        DomNode inheritedHead = composer.ComposePageDocument(window, inherited).Children[1].Children[0];
        DomNode overriddenHead = composer.ComposePageDocument(window, overridden).Children[1].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(inheritedHead.Children[inheritedHead.Children.Length - 1], BridgeProtocolNames.Nonce), Is.EqualTo("window_nonce"));
            Assert.That(AttributeValue(overriddenHead.Children[overriddenHead.Children.Length - 1], BridgeProtocolNames.Nonce), Is.EqualTo("page_nonce"));
        });
    }

    [Test]
    public void RejectsUnsafeGeneratedResourceMetadataAndExtensionAttributes()
    {
        DocumentComposer composer = new DocumentComposer();
        UiResource unsafeUrl = Resource("javascript:alert(1)");
        UiResource unsafeAttribute = Resource("/app.js");
        unsafeAttribute.Attributes = new UiValueEntry[] { new UiValueEntry { Key = "onload", Value = new UiValue { Kind = UiValueKind.String, StringValue = "alert(1)" } } };
        UiResource unsafeIntegrity = Resource("/app.js");
        unsafeIntegrity.Integrity = "md5-not-allowed";
        UiResource unsafeCrossOrigin = Resource("/app.js");
        unsafeCrossOrigin.CrossOrigin = "include";
        UiResource unsafeReferrer = Resource("/app.js");
        unsafeReferrer.ReferrerPolicy = "send-everything";
        UiResource unsafeMime = Resource("/app.js");
        unsafeMime.MimeType = "text/javascript; charset=utf-8";

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => composer.CreateResourceNode(unsafeUrl, null));
            Assert.Throws<ArgumentException>(() => composer.CreateResourceNode(unsafeAttribute, null));
            Assert.Throws<ArgumentException>(() => composer.CreateResourceNode(unsafeIntegrity, null));
            Assert.Throws<ArgumentException>(() => composer.CreateResourceNode(unsafeCrossOrigin, null));
            Assert.Throws<ArgumentException>(() => composer.CreateResourceNode(unsafeReferrer, null));
            Assert.Throws<ArgumentException>(() => composer.CreateResourceNode(unsafeMime, null));
            Assert.Throws<ArgumentException>(() => composer.CreateResourceNode(Resource("/app.js"), "nonce with spaces"));
        });
    }

    [Test]
    public void CarriesNamedRawTrustIntoTheDomAndRejectsSpoofedOrMalformedContent()
    {
        UiNode trusted = RawNode("trusted", "<strong>approved 😀</strong>");
        UiNode reserved = RawNode("$renderer:doctype", "<em>spoofed</em>");
        UiNode malformed = RawNode("reviewed", "<em>bad\ud800</em>");
        SemanticExpander expander = new SemanticExpander();

        DomNode result = expander.ExpandNode(trusted);

        Assert.Multiple(() =>
        {
            Assert.That(result.RawContentPolicyId, Is.EqualTo("reviewed-markup"));
            Assert.That(result.Text, Is.EqualTo("<strong>approved 😀</strong>"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(reserved));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(malformed));
        });
    }

    [Test]
    public void RejectsUnsafeUrlsAcrossLowLevelAndSemanticRenderingPaths()
    {
        UiNode lowLevel = new UiNode
        {
            Kind = UiNodeKind.Element,
            Name = HtmlElements.Anchor,
            Identity = new UiNodeIdentity { AuthorKey = "low", StableId = "low-id" },
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.HyperlinkReference, Value = "javascript:alert(1)" } }
        };
        UiNode link = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "text",
            Identity = new UiNodeIdentity { AuthorKey = "link", StableId = "link-id" },
            TextContent = new UiText { Kind = UiTextKind.Link, Text = "Unsafe", Url = "data:text/html,bad" }
        };

        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(lowLevel));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(link));
        });
    }

    private static UiResource Resource(string url)
    {
        return new UiResource { Key = "script", Url = url, Relationship = "script" };
    }

    private static UiNode RawNode(string policyId, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.RawContent,
            Identity = new UiNodeIdentity { AuthorKey = "raw", StableId = "raw-id" },
            Text = text,
            RawContentPolicy = new UiRawContentPolicy { IsTrusted = true, PolicyId = policyId == "trusted" ? "reviewed-markup" : policyId }
        };
    }

    private static string? AttributeValue(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index].Value;
            }

            index++;
        }

        return null;
    }
}
