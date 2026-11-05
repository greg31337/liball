using System;
using htmlgui;

namespace htmlgui.Tests;

public class HtmlSerializerTests
{
    [Test]
    public void SerializesCanonicalAttributesBooleanValuesTextCommentsAndLineEndings()
    {
        DomAttribute title = new DomAttribute { Name = "TITLE", Value = "line\r\n\" < & '" };
        DomAttribute disabled = new DomAttribute { Name = "DISABLED", Value = "ignored", IsBoolean = true };
        DomAttribute cssClass = new DomAttribute { Name = "CLASS", Value = "b a" };
        DomNode node = Element("DIV", null,
            new DomAttribute[] { title, disabled, cssClass },
            new DomNode[]
            {
                Text("A\r\n<&>\rB"),
                new DomNode { Kind = DomNodeKind.Comment, Text = "safe\rcomment" }
            });
        HtmlSerializer serializer = new HtmlSerializer();

        string html = serializer.SerializeNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(html, Is.EqualTo("<div class=\"b a\" disabled title=\"line\n&quot; &lt; &amp; &#39;\">A\n&lt;&amp;&gt;\nB<!--safe\ncomment--></div>"));
            Assert.That(node.Attributes, Is.EqualTo(new DomAttribute[] { title, disabled, cssClass }));
            Assert.That(title.Value, Is.EqualTo("line\r\n\" < & '"));
            Assert.That(disabled.Value, Is.EqualTo("ignored"));
        });
    }

    [Test]
    public void SerializesHtmlVoidElementsWithoutSyntheticEndTags()
    {
        DomNode fragment = new DomNode
        {
            Kind = DomNodeKind.Fragment,
            Children = new DomNode[]
            {
                Element("BR", null, Array.Empty<DomAttribute>(), Array.Empty<DomNode>()),
                Element("IMG", null, new DomAttribute[] { new DomAttribute { Name = "alt", Value = "A & B" } }, Array.Empty<DomNode>()),
                Element("custom-card", null, Array.Empty<DomAttribute>(), Array.Empty<DomNode>())
            }
        };

        string html = new HtmlSerializer().SerializeNode(fragment);

        Assert.That(html, Is.EqualTo("<br><img alt=\"A &amp; B\"><custom-card></custom-card>"));
    }

    [Test]
    public void SerializesRawAndEscapableRawTextWithWholeElementProtection()
    {
        DomNode script = Element(HtmlElements.Script, null, Array.Empty<DomAttribute>(),
            new DomNode[] { Text("const value = '</scr"), Text("ipt>';\r\n") });
        DomNode style = Element(HtmlElements.Style, null, Array.Empty<DomAttribute>(),
            new DomNode[] { Text("a{content:'safe';}\r") });
        DomNode title = Element(HtmlElements.Title, null, Array.Empty<DomAttribute>(),
            new DomNode[] { Text("Title <&>\r\n") });
        DomNode json = Element(HtmlElements.Script, null,
            new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Type, Value = "application/ld+json" } },
            new DomNode[] { Text("{\"value\":\"</script>\"}") });
        HtmlSerializer serializer = new HtmlSerializer();

        string html = serializer.SerializeFragment(new DomNode[] { script, style, title, json });

        Assert.Multiple(() =>
        {
            Assert.That(html, Is.EqualTo("<script>const value = '\\x3C/script>';\n</script><style>a{content:'safe';}\n</style><title>Title &lt;&amp;&gt;\n</title><script type=\"application/ld+json\">{\"value\":\"\\u003C/script>\"}</script>"));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(
                Element(HtmlElements.Style, null, Array.Empty<DomAttribute>(), new DomNode[] { Text("a{content:'</style>';}") })));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(
                Element(HtmlElements.Script, null, Array.Empty<DomAttribute>(), new DomNode[] { new DomNode { Kind = DomNodeKind.Comment, Text = "not text" } })));
        });
    }

    [Test]
    public void EmitsDeterministicDefaultAndPrefixedNamespaceDeclarations()
    {
        string svgNamespace = "http://www.w3.org/2000/svg";
        DomNode svg = Element("svg", svgNamespace,
            new DomAttribute[]
            {
                new DomAttribute { NamespaceUri = "http://www.w3.org/1999/xlink", Prefix = "xlink", Name = "href", Value = "#icon" },
                new DomAttribute { Name = "viewBox", Value = "0 0 1 1" }
            },
            new DomNode[]
            {
                Element("path", svgNamespace, Array.Empty<DomAttribute>(), Array.Empty<DomNode>()),
                Element("DIV", null, Array.Empty<DomAttribute>(), new DomNode[] { Text("html") })
            });

        string html = new HtmlSerializer().SerializeNode(svg);

        Assert.That(html, Is.EqualTo("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" viewBox=\"0 0 1 1\" xlink:href=\"#icon\"><path></path><div xmlns=\"\">html</div></svg>"));
    }

    [Test]
    public void EmitsOnlyRawContentWithValidApplicationOrRendererProvenance()
    {
        DomNode doctype = new DomNode
        {
            Kind = DomNodeKind.RawContent,
            Text = "<!doctype html>",
            RawContentPolicyId = "$renderer:doctype"
        };
        DomNode trusted = new DomNode
        {
            Kind = DomNodeKind.RawContent,
            Text = "<b>\r\ntrusted</b>",
            RawContentPolicyId = "reviewed-markup"
        };
        HtmlSerializer serializer = new HtmlSerializer();

        string html = serializer.SerializeFragment(new DomNode[] { doctype, trusted });

        Assert.Multiple(() =>
        {
            Assert.That(html, Is.EqualTo("<!doctype html><b>\ntrusted</b>"));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(new DomNode { Kind = DomNodeKind.RawContent, Text = "<b>untrusted</b>" }));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(new DomNode { Kind = DomNodeKind.RawContent, Text = "<script>spoofed</script>", RawContentPolicyId = "$renderer:doctype" }));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(new DomNode { Kind = DomNodeKind.RawContent, Text = "<b>reserved</b>", RawContentPolicyId = "$renderer:custom" }));
        });
    }

    [Test]
    public void RejectsMalformedNodesAttributesNamespacesAndCycles()
    {
        HtmlSerializer serializer = new HtmlSerializer();
        DomNode voidWithChild = Element(HtmlElements.Image, null, Array.Empty<DomAttribute>(), new DomNode[] { Text("child") });
        DomNode duplicate = Element(HtmlElements.Division, null,
            new DomAttribute[] { new DomAttribute { Name = "TITLE" }, new DomAttribute { Name = "title" } }, Array.Empty<DomNode>());
        DomNode missingPrefix = Element("svg", "http://www.w3.org/2000/svg",
            new DomAttribute[] { new DomAttribute { NamespaceUri = "urn:test", Name = "value" } }, Array.Empty<DomNode>());
        DomNode conflictingPrefix = Element("svg", "http://www.w3.org/2000/svg",
            new DomAttribute[]
            {
                new DomAttribute { NamespaceUri = "urn:first", Prefix = "p", Name = "first" },
                new DomAttribute { NamespaceUri = "urn:second", Prefix = "p", Name = "second" }
            }, Array.Empty<DomNode>());
        DomNode cycle = Element(HtmlElements.Division, null, Array.Empty<DomAttribute>(), Array.Empty<DomNode>());
        cycle.Children = new DomNode[] { cycle };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(voidWithChild));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(duplicate));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(missingPrefix));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(conflictingPrefix));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(new DomNode { Kind = DomNodeKind.Element, Name = "bad name" }));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(new DomNode { Kind = (DomNodeKind)99 }));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(cycle));
        });
    }

    [Test]
    public void RejectsNullRootsCollectionEntriesAndMalformedUnicode()
    {
        HtmlSerializer serializer = new HtmlSerializer();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => serializer.SerializeNode(null));
            Assert.Throws<ArgumentNullException>(() => serializer.SerializeFragment(null));
            Assert.Throws<ArgumentException>(() => serializer.SerializeFragment(new DomNode[] { null! }));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(Text("bad\ud800text")));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(new DomNode { Kind = DomNodeKind.Comment, Text = "bad--comment" }));
            Assert.Throws<ArgumentException>(() => serializer.SerializeNode(
                Element(HtmlElements.Division, null, new DomAttribute[] { new DomAttribute { Name = "title", Value = "bad\0value" } }, Array.Empty<DomNode>())));
        });
    }

    private static DomNode Element(string name, string? namespaceUri, DomAttribute[] attributes, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Name = name,
            NamespaceUri = namespaceUri,
            Attributes = attributes,
            Children = children
        };
    }

    private static DomNode Text(string value)
    {
        return new DomNode { Kind = DomNodeKind.Text, Text = value };
    }
}
