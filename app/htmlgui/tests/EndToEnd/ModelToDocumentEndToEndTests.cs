using htmlgui;

namespace htmlgui.Tests;

public class ModelToDocumentEndToEndTests
{
    [Test]
    public void DesktopWebViewModelProducesCompleteAdapterReadyDocument()
    {
        UiApplication application = Application("desktop-seed", "Desktop", "Desktop workspace");
        application.Windows[0].Pages[0].Resources = new UiResource[]
        {
            new UiResource { Key = "desktop-style", Url = "/desktop.css", Relationship = "stylesheet" },
            new UiResource { Key = "desktop-module", Url = "/desktop.js", Relationship = "script", IsModule = true }
        };

        UiRenderResult result = new UiRenderer().RenderDocument(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Html, Does.StartWith("<!doctype html>"));
            Assert.That(result.Html, Does.Contain("Desktop workspace"));
            Assert.That(result.Html, Does.Contain("href=\"/desktop.css\""));
            Assert.That(result.Html, Does.Contain("src=\"/desktop.js\""));
            Assert.That(result.Html, Does.Contain("id=\"htmlgui-hydration\""));
            Assert.That(result.Html, Does.Contain("id=\"htmlgui-bootstrap\""));
            Assert.That(result.Bootstrap, Does.Contain("createWebView2Transport"));
            Assert.That(result.Bootstrap, Does.Contain("createWebKitTransport"));
            Assert.That(result.Bootstrap, Does.Contain("createAndroidTransport"));
            Assert.That(result.Snapshot, Is.Not.Null);
            Assert.That(result.Hydration, Is.Not.Null);
        });
    }

    [Test]
    public void BrowserSpaModelProducesHydratableFragmentWithoutDocumentChrome()
    {
        UiApplication application = Application("spa-seed", "SPA", "Client route content");
        application.Windows[0].Pages[0].Styles = new UiStyle[]
        {
            new UiStyle
            {
                Key = "spa-layout",
                Declarations = new UiCssDeclaration[]
                {
                    new UiCssDeclaration { Property = CssProperties.Display, Value = "grid", IsTrusted = true }
                }
            }
        };

        UiRenderResult result = new UiRenderer().RenderFragment(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Html, Does.Not.Contain("<!doctype"));
            Assert.That(result.Html, Does.Not.Contain("<html"));
            Assert.That(result.Html, Does.Not.Contain("<head"));
            Assert.That(result.Html, Does.Contain("Client route content"));
            Assert.That(result.Html, Does.Contain("id=\"htmlgui-hydration\""));
            Assert.That(result.Html, Does.Contain("id=\"htmlgui-bootstrap\""));
            Assert.That(result.Css, Does.Contain("display:grid"));
            Assert.That(result.Snapshot, Is.Not.Null);
            Assert.That(result.Hydration, Is.Not.Null);
        });
    }

    [Test]
    public void ServerPrerenderedModelHydratesAgainstEquivalentClientDocument()
    {
        UiApplication serverApplication = Application("server-seed", "Server", "Server rendered content");
        serverApplication.RenderOptions.IncludeBootstrap = false;
        UiApplication clientApplication = Application("server-seed", "Server", "Server rendered content");
        clientApplication.RenderOptions.IncludeHydration = false;
        clientApplication.RenderOptions.IncludeBootstrap = false;
        IUiRenderer renderer = new UiRenderer();

        UiRenderResult server = renderer.RenderDocument(serverApplication);
        UiRenderResult client = renderer.RenderDocument(clientApplication);
        UiHydrationResult hydration = new UiHydrator().PlanHydration(client.Snapshot, client.Snapshot, server.Hydration, UiHydrationMode.Exact);

        Assert.Multiple(() =>
        {
            Assert.That(server.Diagnostics, Is.Empty);
            Assert.That(client.Diagnostics, Is.Empty);
            Assert.That(server.Html, Does.Contain("id=\"htmlgui-hydration\""));
            Assert.That(server.Html, Does.Not.Contain("id=\"htmlgui-bootstrap\""));
            Assert.That(client.Html, Does.Not.Contain("htmlgui-hydration"));
            Assert.That(server.Hydration!.Hash, Is.EqualTo(client.Snapshot!.ContentHash));
            Assert.That(hydration.Status, Is.EqualTo(UiHydrationStatus.Matched));
            Assert.That(hydration.ParityMatched, Is.True);
            Assert.That(hydration.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void MultiWindowModelRendersOnlyTheActiveWindowAndSelectedPage()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("multi-seed", "multi");
        UiWindow inactive = Window(factory, application, "inactive-window", false);
        inactive.Pages = new UiPage[] { Page(factory, application, inactive, "inactive-page", "Inactive window") };
        inactive.CurrentPageKey = "inactive-page";
        UiWindow active = Window(factory, application, "active-window", true);
        UiPage first = Page(factory, application, active, "first-page", "Unselected page");
        UiPage selected = Page(factory, application, active, "selected-page", "Selected active page");
        selected.Title = "Selected title";
        active.Pages = new UiPage[] { first, selected };
        active.CurrentPageKey = selected.Key;
        application.Windows = new UiWindow[] { inactive, active };

        UiRenderResult result = new UiRenderer().RenderDocument(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Html, Does.Contain("<title>Selected title</title>"));
            Assert.That(result.Html, Does.Contain("Selected active page"));
            Assert.That(result.Html, Does.Not.Contain("Unselected page"));
            Assert.That(result.Html, Does.Not.Contain("Inactive window"));
            Assert.That(result.Snapshot!.Roots[0].Id, Is.EqualTo(selected.Id));
        });
    }

    [Test]
    public void RtlHighContrastModelProducesDirectionalMarkupAndThemeVariables()
    {
        UiApplication application = Application("contrast-seed", "Contrast", "مرحبا بالعالم");
        UiPage page = application.Windows[0].Pages[0];
        page.Language = "ar";
        page.TextDirection = "rtl";
        application.Theme = new UiTheme
        {
            Key = "contrast",
            Mode = UiThemeMode.HighContrast,
            Tokens = new UiValueEntry[]
            {
                Entry("background", "Canvas"),
                Entry("foreground", "CanvasText")
            }
        };

        UiRenderResult result = new UiRenderer().RenderDocument(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Html, Does.Contain("dir=\"rtl\""));
            Assert.That(result.Html, Does.Contain("lang=\"ar\""));
            Assert.That(result.Html, Does.Contain("مرحبا بالعالم"));
            Assert.That(result.Css, Does.StartWith(":root{"));
            Assert.That(result.Css, Does.Contain("Canvas"));
            Assert.That(result.Css, Does.Contain("CanvasText"));
            Assert.That(result.Snapshot, Is.Not.Null);
        });
    }

    [Test]
    public void NoJavaScriptInitialViewRemainsACompleteReadableDocument()
    {
        UiApplication application = Application("static-seed", "Static", "This content works without JavaScript");
        application.RenderOptions.IncludeHydration = false;
        application.RenderOptions.IncludeBootstrap = false;
        application.Windows[0].Pages[0].Resources = new UiResource[]
        {
            new UiResource { Key = "static-style", Url = "/static.css", Relationship = "stylesheet" }
        };

        UiRenderResult result = new UiRenderer().RenderDocument(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Html, Does.StartWith("<!doctype html>"));
            Assert.That(result.Html, Does.Contain("<main"));
            Assert.That(result.Html, Does.Contain("This content works without JavaScript"));
            Assert.That(result.Html, Does.Contain("href=\"/static.css\""));
            Assert.That(result.Html, Does.Not.Contain("<script"));
            Assert.That(result.Bootstrap, Is.Empty);
            Assert.That(result.Hydration, Is.Null);
            Assert.That(result.Snapshot, Is.Not.Null);
        });
    }

    private static UiApplication Application(string seed, string title, string content)
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication(seed, "application");
        UiWindow window = Window(factory, application, "window", true);
        UiPage page = Page(factory, application, window, "page", content);
        page.Title = title;
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        application.Windows = new UiWindow[] { window };
        application.State = new UiValueEntry[] { Entry("status", "ready") };
        return application;
    }

    private static UiWindow Window(IUiFactory factory, UiApplication application, string key, bool active)
    {
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, key);
        window.IsActive = active;
        window.Document = new UiDocumentMetadata { Language = "en", TextDirection = "ltr", ApplicationName = "htmlgui" };
        return window;
    }

    private static UiPage Page(IUiFactory factory, UiApplication application, UiWindow window, string key, string content)
    {
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, key);
        UiView view = factory.CreateView(application.RootSeed, page.Id, key + "-view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, key + "-root", null, HtmlElements.Main);
        UiNode heading = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, key + "-heading", null, HtmlElements.Heading1);
        heading.Children = new UiNode[] { factory.CreateTextNode(application.RootSeed, heading.Identity.StableId, key + "-heading-text", page.Title) };
        UiNode text = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, key + "-text", content);
        root.Children = new UiNode[] { heading, text };
        view.ContentRoot = root;
        page.ContentView = view;
        return page;
    }

    private static UiValueEntry Entry(string key, string value)
    {
        return new UiValueEntry { Key = key, Value = new UiValue { Kind = UiValueKind.String, StringValue = value } };
    }
}
