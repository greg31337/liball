using System;
using System.Globalization;
using htmlgui;

namespace htmlgui.Tests;

public class DocumentComposerTests
{
    [Test]
    public void ComposesCompleteHeadAndBodyInStableOrder()
    {
        UiWindow window = new UiWindow
        {
            Id = "window-id",
            Key = "window",
            Document = new UiDocumentMetadata { Language = "en", TextDirection = "ltr" }
        };
        UiPage page = new UiPage
        {
            Id = "page-id",
            Key = "page",
            Title = "Dashboard",
            Language = "fr",
            TextDirection = "rtl",
            Metadata = new UiDocumentMetadata
            {
                CharacterSet = "utf-16",
                Viewport = "width=900",
                BaseUrl = "https://example.test/app/",
                Description = "Page description",
                ApplicationName = "Example app",
                Values = new UiValueEntry[]
                {
                    Entry("theme-color", new UiValue { Kind = UiValueKind.String, StringValue = "#123456" })
                }
            },
            Resources = new UiResource[]
            {
                new UiResource
                {
                    Key = "styles",
                    Url = "/app.css",
                    Relationship = "stylesheet",
                    MimeType = "text/css",
                    Media = "screen",
                    Integrity = "sha256-style",
                    CrossOrigin = "anonymous",
                    ReferrerPolicy = "no-referrer",
                    Attributes = new UiValueEntry[]
                    {
                        Entry("fetchpriority", new UiValue { Kind = UiValueKind.String, StringValue = "high" })
                    }
                },
                new UiResource
                {
                    Key = "module",
                    Url = "/app.js",
                    Relationship = "script",
                    IsModule = true
                }
            },
            ShellView = View("shell", "shell-id", "shell-root", "Shell"),
            ContentView = View("content", "content-id", "content-root", "Content")
        };
        window.Overlays = new UiView[]
        {
            View("dialog", "dialog-id", "dialog-root", "Dialog"),
            View("toast", "toast-id", "toast-root", "Toast")
        };
        DocumentComposer composer = new DocumentComposer();

        DomNode document = composer.ComposePageDocument(window, page);
        DomNode doctype = document.Children[0];
        DomNode html = document.Children[1];
        DomNode head = html.Children[0];
        DomNode body = html.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(document.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(document.Key, Is.EqualTo("page"));
            Assert.That(document.Id, Is.EqualTo("page-id"));
            Assert.That(doctype.Kind, Is.EqualTo(DomNodeKind.RawContent));
            Assert.That(doctype.Text, Is.EqualTo("<!doctype html>"));
            Assert.That(html.Name, Is.EqualTo(HtmlElements.Html));
            Assert.That(AttributeValue(html, HtmlAttributes.Language), Is.EqualTo("fr"));
            Assert.That(AttributeValue(html, HtmlAttributes.Direction), Is.EqualTo("rtl"));
            Assert.That(head.Name, Is.EqualTo(HtmlElements.Head));
            Assert.That(Array.ConvertAll(head.Children, child => child.Key), Is.EqualTo(new string[] { "$charset", "$viewport", "$title", "$base", "$description", "$application-name", "$metadata:theme-color", "$resource:styles", "$resource:module" }));
            Assert.That(AttributeValue(head.Children[0], HtmlAttributes.CharacterSet), Is.EqualTo("utf-16"));
            Assert.That(AttributeValue(head.Children[1], HtmlAttributes.Name), Is.EqualTo("viewport"));
            Assert.That(AttributeValue(head.Children[1], HtmlAttributes.Content), Is.EqualTo("width=900"));
            Assert.That(head.Children[2].Children[0].Text, Is.EqualTo("Dashboard"));
            Assert.That(AttributeValue(head.Children[3], HtmlAttributes.HyperlinkReference), Is.EqualTo("https://example.test/app/"));
            Assert.That(AttributeValue(head.Children[4], HtmlAttributes.Content), Is.EqualTo("Page description"));
            Assert.That(AttributeValue(head.Children[5], HtmlAttributes.Content), Is.EqualTo("Example app"));
            Assert.That(AttributeValue(head.Children[6], HtmlAttributes.Content), Is.EqualTo("#123456"));
            Assert.That(head.Children[7].Name, Is.EqualTo(HtmlElements.Link));
            Assert.That(AttributeValue(head.Children[7], HtmlAttributes.Relationship), Is.EqualTo("stylesheet"));
            Assert.That(AttributeValue(head.Children[7], HtmlAttributes.HyperlinkReference), Is.EqualTo("/app.css"));
            Assert.That(AttributeValue(head.Children[7], "integrity"), Is.EqualTo("sha256-style"));
            Assert.That(AttributeValue(head.Children[7], "fetchpriority"), Is.EqualTo("high"));
            Assert.That(head.Children[8].Name, Is.EqualTo(HtmlElements.Script));
            Assert.That(AttributeValue(head.Children[8], HtmlAttributes.Source), Is.EqualTo("/app.js"));
            Assert.That(AttributeValue(head.Children[8], HtmlAttributes.Type), Is.EqualTo("module"));
            Assert.That(body.Name, Is.EqualTo(HtmlElements.Body));
            Assert.That(Array.ConvertAll(body.Children, child => child.Key), Is.EqualTo(new string[] { "shell", "content", "dialog", "toast" }));
            Assert.That(body.Children[0].Children[0].Text, Is.EqualTo("Shell"));
            Assert.That(body.Children[1].Children[0].Text, Is.EqualTo("Content"));
            Assert.That(body.Children[2].Children[0].Text, Is.EqualTo("Dialog"));
        });
    }

    [Test]
    public void UsesMetadataAndWindowFallbacksAndOmitsAbsentOptionalHeadContent()
    {
        UiWindow window = new UiWindow
        {
            Document = new UiDocumentMetadata { Language = "de", TextDirection = "ltr" },
            Overlays = null!
        };
        UiPage page = new UiPage
        {
            Id = "page-id",
            Key = "page",
            Metadata = new UiDocumentMetadata { Title = "Metadata title" },
            Resources = null!
        };
        DocumentComposer composer = new DocumentComposer();

        DomNode document = composer.ComposePageDocument(window, page);
        DomNode html = document.Children[1];
        DomNode head = html.Children[0];
        DomNode body = html.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(html, HtmlAttributes.Language), Is.EqualTo("de"));
            Assert.That(AttributeValue(html, HtmlAttributes.Direction), Is.EqualTo("ltr"));
            Assert.That(Array.ConvertAll(head.Children, child => child.Key), Is.EqualTo(new string[] { "$charset", "$viewport", "$title" }));
            Assert.That(head.Children[2].Children[0].Text, Is.EqualTo("Metadata title"));
            Assert.That(body.Children, Is.Empty);
        });
    }

    [Test]
    public void FormatsMetadataValuesInvariantly()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        DocumentComposer composer = new DocumentComposer();
        string floating;
        string decimalText;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            floating = composer.FormatMetadataValue(new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = 12.5 });
            decimalText = composer.FormatMetadataValue(new UiValue { Kind = UiValueKind.Decimal, DecimalValue = 9.75m });
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        Assert.Multiple(() =>
        {
            Assert.That(composer.FormatMetadataValue(null), Is.Empty);
            Assert.That(composer.FormatMetadataValue(new UiValue()), Is.Empty);
            Assert.That(composer.FormatMetadataValue(new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true }), Is.EqualTo("true"));
            Assert.That(composer.FormatMetadataValue(new UiValue { Kind = UiValueKind.Integer, IntegerValue = 42 }), Is.EqualTo("42"));
            Assert.That(floating, Is.EqualTo("12.5"));
            Assert.That(decimalText, Is.EqualTo("9.75"));
            Assert.That(composer.FormatMetadataValue(new UiValue { Kind = UiValueKind.String, StringValue = "text" }), Is.EqualTo("text"));
            Assert.That(composer.FormatMetadataValue(new UiValue { Kind = UiValueKind.DateTime, DateTimeValue = "2026-09-14T00:00:00Z" }), Is.EqualTo("2026-09-14T00:00:00Z"));
            Assert.That(composer.FormatMetadataValue(new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[] { 1, 2, 3 } }), Is.EqualTo("AQID"));
            Assert.That(composer.FormatMetadataValue(new UiValue { Kind = UiValueKind.Array, ArrayValue = Array.Empty<UiValue>() }), Is.EqualTo("{\"kind\":\"array\",\"value\":[]}"));
        });
    }

    [Test]
    public void CompositionDoesNotMutateSourceModelsAndReturnsFreshTrees()
    {
        UiWindow window = new UiWindow { Id = "window-id", Key = "window" };
        UiPage page = new UiPage
        {
            Id = "page-id",
            Key = "page",
            ContentView = View("content", "content-id", "root-id", "Original")
        };
        UiView sourceView = page.ContentView;
        UiNode sourceRoot = sourceView.ContentRoot!;
        DocumentComposer composer = new DocumentComposer();

        DomNode first = composer.ComposePageDocument(window, page);
        DomNode second = composer.ComposePageDocument(window, page);
        first.Children[1].Children[1].Children[0].Children[0].Text = "Changed output";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(page.ContentView, Is.SameAs(sourceView));
            Assert.That(sourceView.ContentRoot, Is.SameAs(sourceRoot));
            Assert.That(sourceRoot.Text, Is.EqualTo("Original"));
            Assert.That(second.Children[1].Children[1].Children[0].Children[0].Text, Is.EqualTo("Original"));
        });
    }

    [Test]
    public void RejectsMissingWindowOrPage()
    {
        DocumentComposer composer = new DocumentComposer();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => composer.ComposePageDocument(null, new UiPage()))!.ParamName, Is.EqualTo("window"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => composer.ComposePageDocument(new UiWindow(), null))!.ParamName, Is.EqualTo("page"));
        });
    }

    private static UiView View(string key, string id, string rootId, string text)
    {
        return new UiView
        {
            Key = key,
            Id = id,
            ContentRoot = new UiNode
            {
                Kind = UiNodeKind.Text,
                Identity = new UiNodeIdentity { AuthorKey = key + "-root", SiblingIdentity = key + "-root", StableId = rootId },
                Text = text
            }
        };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static string? AttributeValue(DomNode node, string name)
    {
        DomAttribute? attribute = Array.Find(node.Attributes, item => item.Name == name);
        if (attribute == null)
        {
            return null;
        }

        return attribute.Value;
    }
}
