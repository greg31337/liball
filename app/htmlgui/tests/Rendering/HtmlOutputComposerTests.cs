using System;
using System.Text.Json;
using htmlgui;

namespace htmlgui.Tests;

public class HtmlOutputComposerTests
{
    [Test]
    public void ComposesFullDocumentAssetsAndContentInDeterministicOrder()
    {
        UiWindow window = Window();
        UiPage page = Page();
        page.Resources = new UiResource[]
        {
            new UiResource { Key = "z-script", Url = "/z.js", Relationship = "script" },
            new UiResource { Key = "a-style", Url = "/a.css", Relationship = "stylesheet" }
        };
        UiCssCompilation css = new UiCssCompilation { Css = ".card{color:red;}\r\n", Success = true };
        UiHydrationManifest hydration = new UiHydrationManifest
        {
            Version = 3,
            Hash = "page-hash",
            Nodes = new UiValueEntry[] { Entry("root", TextValue("</script>")) }
        };
        UiRenderOptions options = new UiRenderOptions
        {
            FullDocument = true,
            IncludeHydration = true,
            IncludeBootstrap = true,
            ContentSecurityPolicyNonce = "option_nonce"
        };
        HtmlOutputComposer composer = new HtmlOutputComposer();

        HtmlOutputComposition output = composer.ComposePageOutput(window, page, css, hydration, options);

        int styleResource = output.Html.IndexOf("/a.css", StringComparison.Ordinal);
        int scriptResource = output.Html.IndexOf("/z.js", StringComparison.Ordinal);
        int generatedStyle = output.Html.IndexOf("<style nonce=\"option_nonce\">", StringComparison.Ordinal);
        int content = output.Html.IndexOf("Page &lt;content&gt;", StringComparison.Ordinal);
        int hydrationScript = output.Html.IndexOf("id=\"htmlgui-hydration\"", StringComparison.Ordinal);
        int bootstrapScript = output.Html.IndexOf("id=\"htmlgui-bootstrap\"", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(output.Html, Does.StartWith("<!doctype html><html"));
            Assert.That(output.Html, Does.EndWith("</body></html>"));
            Assert.That(styleResource, Is.GreaterThan(0));
            Assert.That(scriptResource, Is.GreaterThan(0));
            Assert.That(styleResource, Is.GreaterThan(scriptResource));
            Assert.That(generatedStyle, Is.GreaterThan(styleResource));
            Assert.That(content, Is.GreaterThan(generatedStyle));
            Assert.That(hydrationScript, Is.GreaterThan(content));
            Assert.That(bootstrapScript, Is.GreaterThan(hydrationScript));
            Assert.That(output.Html, Does.Contain("href=\"/a.css\" nonce=\"option_nonce\" rel=\"stylesheet\""));
            Assert.That(output.Html, Does.Contain("nonce=\"option_nonce\" src=\"/z.js\""));
            Assert.That(output.Css, Is.EqualTo(".card{color:red;}\n"));
            Assert.That(output.Bootstrap, Does.StartWith("\"use strict\";\n(function (root)"));
            Assert.That(output.Bootstrap, Does.Contain("createRuntime"));
            Assert.That(output.HydrationJson, Does.Contain("\\u003C/script\\u003E"));
            Assert.That(output.Root.Kind, Is.EqualTo(DomNodeKind.Fragment));
        });
    }

    [Test]
    public void ComposesSelfContainedFragmentWithoutDocumentChrome()
    {
        UiWindow window = Window();
        UiPage page = Page();
        page.Resources = new UiResource[]
        {
            new UiResource { Key = "z", Url = "/z.js", Relationship = "script" },
            new UiResource { Key = "a", Url = "/a.css", Relationship = "stylesheet" }
        };
        UiRenderOptions options = new UiRenderOptions { FullDocument = false, IncludeHydration = false, IncludeBootstrap = false };

        HtmlOutputComposition output = new HtmlOutputComposer().ComposePageOutput(
            window,
            page,
            new UiCssCompilation { Css = ".fragment{display:block;}", Success = true },
            new UiHydrationManifest(),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(output.Html, Does.Not.Contain("<!doctype"));
            Assert.That(output.Html, Does.Not.Contain("<html"));
            Assert.That(output.Html, Does.Not.Contain("<head"));
            Assert.That(output.Html, Does.Not.Contain("<body"));
            Assert.That(output.Html.IndexOf("/z.js", StringComparison.Ordinal), Is.LessThan(output.Html.IndexOf("/a.css", StringComparison.Ordinal)));
            Assert.That(output.Html.IndexOf("/a.css", StringComparison.Ordinal), Is.LessThan(output.Html.IndexOf("<style>", StringComparison.Ordinal)));
            Assert.That(output.Html.IndexOf("<style>", StringComparison.Ordinal), Is.LessThan(output.Html.IndexOf("Page &lt;content&gt;", StringComparison.Ordinal)));
            Assert.That(output.Html, Does.Not.Contain("htmlgui-hydration"));
            Assert.That(output.Html, Does.Not.Contain("htmlgui-bootstrap"));
            Assert.That(output.Bootstrap, Is.Empty);
            Assert.That(output.HydrationJson, Is.Empty);
            Assert.That(output.Root.Kind, Is.EqualTo(DomNodeKind.Fragment));
        });
    }

    [Test]
    public void SerializesHydrationManifestWithStableFieldAndEntryOrder()
    {
        UiHydrationManifest first = Manifest(false);
        UiHydrationManifest second = Manifest(true);
        HtmlOutputComposer composer = new HtmlOutputComposer();

        string firstJson = composer.SerializeHydrationManifest(first);
        string secondJson = composer.SerializeHydrationManifest(second);

        using (JsonDocument document = JsonDocument.Parse(firstJson))
        {
            JsonElement root = document.RootElement;
            Assert.Multiple(() =>
            {
                Assert.That(firstJson, Is.EqualTo(secondJson));
                Assert.That(firstJson, Does.StartWith("{\"version\":2,\"hash\":\"manifest-hash\",\"nodes\":"));
                Assert.That(root.GetProperty("version").GetInt32(), Is.EqualTo(2));
                Assert.That(root.GetProperty("nodes").GetProperty("value").EnumerateObject().First().Name, Is.EqualTo("alpha"));
                Assert.That(root.GetProperty("routeData").GetProperty("routeId").GetString(), Is.EqualTo("product"));
                Assert.That(root.GetProperty("historyEntryId").GetString(), Is.EqualTo("history-7"));
                Assert.That(root.GetProperty("navigationGeneration").GetInt32(), Is.EqualTo(7));
            });
        }
    }

    [Test]
    public void RenderOptionsNonceOverridesMetadataAndBootstrapIsStable()
    {
        UiWindow window = Window();
        window.Document.ContentSecurityPolicyNonce = "window_nonce";
        UiPage page = Page();
        page.Metadata.ContentSecurityPolicyNonce = "page_nonce";
        page.Resources = new UiResource[] { new UiResource { Key = "style", Url = "/app.css", Relationship = "stylesheet" } };
        HtmlOutputComposer composer = new HtmlOutputComposer();

        HtmlOutputComposition output = composer.ComposePageOutput(
            window,
            page,
            null,
            null,
            new UiRenderOptions { ContentSecurityPolicyNonce = "option_nonce", IncludeHydration = false });

        Assert.Multiple(() =>
        {
            Assert.That(output.Html, Does.Contain("href=\"/app.css\" nonce=\"option_nonce\""));
            Assert.That(output.Html, Does.Contain("id=\"htmlgui-bootstrap\" nonce=\"option_nonce\""));
            Assert.That(composer.LoadBootstrapAsset(), Is.EqualTo(output.Bootstrap));
            Assert.That(composer.LoadBootstrapAsset(), Does.Contain("requestResync"));
        });
    }

    [Test]
    public void ResourceRenderingPreservesDeclaredOrderAndRejectsDuplicateKeys()
    {
        UiWindow window = Window();
        UiPage firstPage = Page();
        firstPage.Resources = new UiResource[]
        {
            new UiResource { Key = "second", Url = "/second.css", Relationship = "stylesheet" },
            new UiResource { Key = "first", Url = "/first.css", Relationship = "stylesheet" }
        };
        UiPage duplicatePage = Page();
        duplicatePage.Resources = new UiResource[]
        {
            new UiResource { Key = "same", Url = "/one.css", Relationship = "stylesheet" },
            new UiResource { Key = "same", Url = "/two.css", Relationship = "stylesheet" }
        };
        DocumentComposer composer = new DocumentComposer();
        HtmlSerializer serializer = new HtmlSerializer();

        string first = serializer.SerializeNode(composer.ComposePageDocument(window, firstPage));
        string repeated = serializer.SerializeNode(composer.ComposePageDocument(window, firstPage));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(repeated));
            Assert.That(first.IndexOf("/second.css", StringComparison.Ordinal), Is.LessThan(first.IndexOf("/first.css", StringComparison.Ordinal)));
            Assert.Throws<ArgumentException>(() => composer.ComposePageDocument(window, duplicatePage));
            Assert.That(firstPage.Resources[0].Key, Is.EqualTo("second"));
        });
    }

    [Test]
    public void RejectsInvalidInputsCssNonceAndHydrationData()
    {
        HtmlOutputComposer composer = new HtmlOutputComposer();
        UiWindow window = Window();
        UiPage page = Page();
        UiHydrationManifest negative = new UiHydrationManifest { Version = -1 };
        UiHydrationManifest malformed = new UiHydrationManifest { Hash = "bad\ud800hash" };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => composer.ComposePageOutput(null, page, null, null, new UiRenderOptions()));
            Assert.Throws<ArgumentNullException>(() => composer.ComposePageOutput(window, null, null, null, new UiRenderOptions()));
            Assert.Throws<ArgumentNullException>(() => composer.ComposePageOutput(window, page, null, null, null));
            Assert.Throws<ArgumentException>(() => composer.ComposePageOutput(window, page, new UiCssCompilation { Css = "a{content:'</style>';}", Success = true }, null, new UiRenderOptions()));
            Assert.Throws<ArgumentException>(() => composer.ComposePageOutput(window, page, null, null, new UiRenderOptions { ContentSecurityPolicyNonce = "bad nonce" }));
            Assert.Throws<ArgumentException>(() => composer.SerializeHydrationManifest(negative));
            Assert.Throws<ArgumentException>(() => composer.SerializeHydrationManifest(malformed));
            Assert.Throws<ArgumentNullException>(() => composer.SerializeHydrationManifest(null));
        });
    }

    private static UiWindow Window()
    {
        return new UiWindow
        {
            Id = "window-id",
            Key = "window",
            Document = new UiDocumentMetadata { Language = "en" }
        };
    }

    private static UiPage Page()
    {
        UiNode paragraph = new UiNode
        {
            Kind = UiNodeKind.Element,
            Name = HtmlElements.Paragraph,
            Identity = new UiNodeIdentity { AuthorKey = "paragraph", StableId = "paragraph-id" },
            Children = new UiNode[]
            {
                new UiNode
                {
                    Kind = UiNodeKind.Text,
                    Text = "Page <content>",
                    Identity = new UiNodeIdentity { AuthorKey = "text", StableId = "text-id" }
                }
            }
        };
        return new UiPage
        {
            Id = "page-id",
            Key = "page",
            Title = "Page title",
            ContentView = new UiView { Id = "view-id", Key = "view", ContentRoot = paragraph }
        };
    }

    private static UiHydrationManifest Manifest(bool reverse)
    {
        UiValueEntry alpha = Entry("alpha", TextValue("one"));
        UiValueEntry zeta = Entry("zeta", TextValue("two"));
        UiValueEntry parameterA = Entry("a", TextValue("first"));
        UiValueEntry parameterZ = Entry("z", TextValue("last"));
        return new UiHydrationManifest
        {
            Version = 2,
            Hash = "manifest-hash",
            Nodes = reverse ? new UiValueEntry[] { alpha, zeta } : new UiValueEntry[] { zeta, alpha },
            State = reverse ? new UiValueEntry[] { zeta, alpha } : new UiValueEntry[] { alpha, zeta },
            RouteUrl = "/products/7#details",
            RouteData = new UiRouteData
            {
                RouteId = "product",
                Parameters = reverse ? new UiValueEntry[] { parameterA, parameterZ } : new UiValueEntry[] { parameterZ, parameterA },
                Query = new UiValueEntry[] { Entry("view", TextValue("full")) },
                Fragment = "details"
            },
            HistoryState = TextValue("state"),
            HistoryEntryId = "history-7",
            NavigationGeneration = 7
        };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue TextValue(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
