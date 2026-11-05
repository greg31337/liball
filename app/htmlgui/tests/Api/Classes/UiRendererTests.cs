using System;
using htmlgui;

namespace htmlgui.Tests;

public class UiRendererTests
{
    [Test]
    public void RendersCompleteDocumentWithStylesResourcesHydrationSnapshotAndHash()
    {
        UiApplication application = Application();
        UiResource originalResource = application.Windows[0].Pages[0].Resources[0];
        IUiRenderer renderer = new UiRenderer();

        UiRenderResult result = renderer.RenderDocument(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Html, Does.StartWith("<!doctype html><html"));
            Assert.That(result.Html, Does.Contain("<title>Dashboard</title>"));
            Assert.That(result.Html, Does.Contain("Hello renderer"));
            Assert.That(result.Html, Does.Contain("href=\"/app.css\""));
            Assert.That(result.Html, Does.Contain("htmlgui-hydration"));
            Assert.That(result.Html, Does.Contain("htmlgui-bootstrap"));
            Assert.That(result.Html, Does.Contain("htmlgui-scope-"));
            Assert.That(result.Css, Does.Contain(":root{"));
            Assert.That(result.Css, Does.Contain("color:navy"));
            Assert.That(result.Css, Does.Contain("display:grid"));
            Assert.That(result.Bootstrap, Does.StartWith("\"use strict\";\n(function (root)"));
            Assert.That(result.Bootstrap, Does.Contain("createRuntime"));
            Assert.That(result.Resources, Has.Length.EqualTo(1));
            Assert.That(result.Resources[0].Key, Is.EqualTo("application-style"));
            Assert.That(result.Resources[0], Is.Not.SameAs(originalResource));
            Assert.That(result.Snapshot, Is.Not.Null);
            Assert.That(result.Snapshot!.RootSeed, Is.EqualTo(application.RootSeed));
            Assert.That(result.Snapshot.SessionId, Does.StartWith("render-"));
            Assert.That(result.Snapshot.ContentHash, Has.Length.EqualTo(64));
            Assert.That(result.Snapshot.Roots[0].Sequence, Is.EqualTo(0));
            Assert.That(result.Snapshot.Roots[0].SubtreeHash, Has.Length.EqualTo(64));
            Assert.That(result.Hydration, Is.Not.Null);
            Assert.That(result.Hydration!.Hash, Has.Length.EqualTo(64));
            Assert.That(result.Hydration.Nodes, Is.Not.Empty);
            Assert.That(result.Hydration.State[0].Key, Is.EqualTo("counter"));
            Assert.That(result.Hash, Has.Length.EqualTo(64));
            Assert.That(application.RenderResult, Is.Null);
            Assert.That(application.Windows[0].Pages[0].ContentView!.ContentRoot!.Attributes, Is.Empty);
        });
    }

    [Test]
    public void RendersFragmentWithoutDocumentChromeAndKeepsOptionsImmutable()
    {
        UiApplication application = Application();
        application.RenderOptions.FullDocument = true;
        application.RenderOptions.IncludeHydration = false;
        application.RenderOptions.IncludeBootstrap = false;
        IUiRenderer renderer = new UiRenderer();

        UiRenderResult result = renderer.RenderFragment(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Html, Does.Not.Contain("<!doctype"));
            Assert.That(result.Html, Does.Not.Contain("<html"));
            Assert.That(result.Html, Does.Not.Contain("<head"));
            Assert.That(result.Html, Does.Contain("Hello renderer"));
            Assert.That(result.Html, Does.Contain("href=\"/app.css\""));
            Assert.That(result.Html, Does.Contain("<style"));
            Assert.That(result.Bootstrap, Is.Empty);
            Assert.That(result.Hydration, Is.Null);
            Assert.That(result.Snapshot, Is.Not.Null);
            Assert.That(application.RenderOptions.FullDocument, Is.True);
        });
    }

    [Test]
    public void RendersStylesAndBootstrapThroughDedicatedPublicOperations()
    {
        UiApplication application = Application();
        IUiRenderer renderer = new UiRenderer();

        UiRenderResult styles = renderer.RenderStyles(application);
        UiRenderResult bootstrap = renderer.RenderBootstrap();

        Assert.Multiple(() =>
        {
            Assert.That(styles.Diagnostics, Is.Empty);
            Assert.That(styles.Css, Does.Contain("color:navy"));
            Assert.That(styles.Css, Does.Contain("display:grid"));
            Assert.That(styles.Html, Is.Empty);
            Assert.That(styles.Bootstrap, Is.Empty);
            Assert.That(styles.Snapshot, Is.Null);
            Assert.That(styles.Hydration, Is.Null);
            Assert.That(styles.Resources, Has.Length.EqualTo(1));
            Assert.That(styles.Hash, Has.Length.EqualTo(64));
            Assert.That(bootstrap.Bootstrap, Does.Contain("requestResync"));
            Assert.That(bootstrap.Html, Is.Empty);
            Assert.That(bootstrap.Css, Is.Empty);
            Assert.That(bootstrap.Resources, Is.Empty);
            Assert.That(bootstrap.Diagnostics, Is.Empty);
            Assert.That(bootstrap.Hash, Has.Length.EqualTo(64));
        });
    }

    [Test]
    public void SelectsCurrentPageFromTheActiveWindowDeterministically()
    {
        UiApplication application = Application();
        IUiFactory factory = new UiFactory();
        UiWindow inactive = factory.CreateWindow(application.RootSeed, application.Id, "alpha-window");
        inactive.IsActive = false;
        UiPage inactivePage = factory.CreatePage(application.RootSeed, inactive.Id, "inactive-page");
        inactivePage.Title = "Inactive";
        inactive.Pages = new UiPage[] { inactivePage };
        UiWindow active = application.Windows[0];
        UiPage first = factory.CreatePage(application.RootSeed, active.Id, "alpha-page");
        first.Title = "Wrong page";
        active.Pages = new UiPage[] { application.Windows[0].Pages[0], first };
        active.CurrentPageKey = "page";
        application.Windows = new UiWindow[] { active, inactive };

        UiRenderResult result = new UiRenderer().RenderDocument(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Html, Does.Contain("<title>Dashboard</title>"));
            Assert.That(result.Html, Does.Not.Contain("Wrong page"));
            Assert.That(result.Html, Does.Not.Contain("Inactive"));
        });
    }

    [Test]
    public void ReturnsBoundedDiagnosticsWithoutPartialOutputForInvalidInput()
    {
        IUiRenderer renderer = new UiRenderer();
        UiApplication noWindow = new UiFactory().CreateApplication("seed", "empty");
        noWindow.RenderOptions.MaximumDiagnostics = 1;

        UiRenderResult missing = renderer.RenderDocument(null);
        UiRenderResult empty = renderer.RenderDocument(noWindow);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(missing.Diagnostics[0].Code, Is.EqualTo("model.null-root"));
            Assert.That(missing.Html, Is.Empty);
            Assert.That(missing.Snapshot, Is.Null);
            Assert.That(missing.Hash, Has.Length.EqualTo(64));
            Assert.That(empty.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(empty.Diagnostics[0].Code, Is.EqualTo("render.no-window"));
            Assert.That(empty.Diagnostics[0].Location, Is.EqualTo("/application/windows"));
            Assert.That(empty.Html, Is.Empty);
            Assert.That(empty.Resources, Is.Empty);
        });
    }

    [Test]
    public void ReturnsStyleDiagnosticsWithoutPublishingPartialStyleOutput()
    {
        UiApplication application = Application();
        application.Windows[0].Pages[0].Styles[0].Key = " ";

        UiRenderResult result = new UiRenderer().RenderStyles(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo("style.invalid-key"));
            Assert.That(result.Css, Is.Empty);
            Assert.That(result.Resources, Has.Length.EqualTo(1));
            Assert.That(result.Hash, Has.Length.EqualTo(64));
        });
    }

    private static UiApplication Application()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("renderer-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        UiNode text = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "text", "Hello renderer");
        root.Children = new UiNode[] { text };
        root.Properties = new DomProperty[]
        {
            new DomProperty
            {
                Name = "state",
                Value = new UiValue { Kind = UiValueKind.String, StringValue = "ready" }
            }
        };
        root.Style = new UiStyle
        {
            Key = "root-style",
            Declarations = new UiCssDeclaration[]
            {
                new UiCssDeclaration { Property = CssProperties.Display, Value = "grid", IsTrusted = true }
            }
        };
        view.ContentRoot = root;
        page.Title = "Dashboard";
        page.ContentView = view;
        page.Resources = new UiResource[]
        {
            new UiResource
            {
                Key = "application-style",
                Url = "/app.css",
                Relationship = "stylesheet",
                Attributes = new UiValueEntry[]
                {
                    new UiValueEntry { Key = "fetchpriority", Value = new UiValue { Kind = UiValueKind.String, StringValue = "high" } }
                }
            }
        };
        page.Styles = new UiStyle[]
        {
            new UiStyle
            {
                Key = "page-style",
                Declarations = new UiCssDeclaration[]
                {
                    new UiCssDeclaration { Property = CssProperties.Color, Value = "navy", IsTrusted = true }
                }
            }
        };
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        application.Windows = new UiWindow[] { window };
        application.Theme = new UiTheme
        {
            Key = "theme",
            Tokens = new UiValueEntry[]
            {
                new UiValueEntry
                {
                    Key = "accent",
                    Value = new UiValue { Kind = UiValueKind.String, StringValue = "navy" }
                }
            }
        };
        application.State = new UiValueEntry[]
        {
            new UiValueEntry { Key = "counter", Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = 3 } }
        };
        return application;
    }
}
