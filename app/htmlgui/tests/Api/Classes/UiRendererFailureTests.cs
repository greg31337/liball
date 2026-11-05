using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiRendererFailureTests
{
    [Test]
    public void MalformedModelReturnsLocationAwareFailureFromEveryModelRenderOperation()
    {
        UiApplication application = Application();
        application.Windows[0].Pages[0].ContentView!.ContentRoot!.Children = null!;
        IUiRenderer renderer = new UiRenderer();

        UiRenderResult document = renderer.RenderDocument(application);
        UiRenderResult fragment = renderer.RenderFragment(application);
        UiRenderResult styles = renderer.RenderStyles(application);

        Assert.Multiple(() =>
        {
            AssertFailure(document, "model.null-collection", "/application/windows/0/pages/0/contentView/contentRoot/children");
            AssertFailure(fragment, "model.null-collection", "/application/windows/0/pages/0/contentView/contentRoot/children");
            AssertFailure(styles, "model.null-collection", "/application/windows/0/pages/0/contentView/contentRoot/children");
            Assert.That(fragment.Hash, Is.EqualTo(document.Hash));
            Assert.That(styles.Hash, Is.EqualTo(document.Hash));
        });
    }

    [Test]
    public void ModelCycleReturnsOrderedDiagnosticsWithTheExactCycleLocation()
    {
        UiApplication application = Application();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        root.Children = new UiNode[] { root };

        UiRenderResult result = new UiRenderer().RenderDocument(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[]
            {
                "model.duplicate-id",
                "model.parent-mismatch",
                "model.cycle"
            }));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Location), Is.EqualTo(new string[]
            {
                "/application/windows/0/pages/0/contentView/contentRoot/children/0/identity/stableId",
                "/application/windows/0/pages/0/contentView/contentRoot/children/0/identity/parentStableId",
                "/application/windows/0/pages/0/contentView/contentRoot/children/0"
            }));
            AssertNoPartialOutput(result);
        });
    }

    [Test]
    public void DuplicateSiblingKeyAndIdReturnOrderedLocationAwareDiagnostics()
    {
        UiApplication application = Application();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        IUiFactory factory = new UiFactory();
        UiNode first = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "duplicate", "First");
        UiNode second = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "second", "Second");
        second.Identity.AuthorKey = first.Identity.AuthorKey;
        second.Identity.StableId = first.Identity.StableId;
        root.Children = new UiNode[] { first, second };

        UiRenderResult result = new UiRenderer().RenderDocument(application);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[]
            {
                "model.duplicate-key",
                "model.duplicate-id"
            }));
            Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Location), Is.EqualTo(new string[]
            {
                "/application/windows/0/pages/0/contentView/contentRoot/children/1/identity/authorKey",
                "/application/windows/0/pages/0/contentView/contentRoot/children/1/identity/stableId"
            }));
            Assert.That(result.Diagnostics.All(diagnostic => diagnostic.Severity == UiDiagnosticSeverity.Error), Is.True);
            AssertNoPartialOutput(result);
        });
    }

    [Test]
    public void DiagnosticLimitBoundsRendererFailureResultsWithoutPublishingResources()
    {
        UiApplication application = Application();
        application.RenderOptions.MaximumDiagnostics = 2;
        application.Windows[0].Pages[0].Resources = new UiResource[]
        {
            new UiResource { Key = "style", Url = "/style.css", Relationship = "stylesheet" }
        };
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        root.Children = new UiNode[] { root, root, root };

        UiRenderResult first = new UiRenderer().RenderDocument(application);
        UiRenderResult repeated = new UiRenderer().RenderDocument(application);

        Assert.Multiple(() =>
        {
            Assert.That(first.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(repeated.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(first.Diagnostics.Select(diagnostic => diagnostic.Code)));
            Assert.That(repeated.Diagnostics.Select(diagnostic => diagnostic.Location), Is.EqualTo(first.Diagnostics.Select(diagnostic => diagnostic.Location)));
            Assert.That(first.Resources, Is.Empty);
            Assert.That(first.Hash, Is.EqualTo(repeated.Hash));
            AssertNoPartialOutput(first);
        });
    }

    [Test]
    public void InvalidRawContentReportsStableNodeContext()
    {
        UiApplication untrustedApplication = Application();
        UiNode untrusted = RawRoot(untrustedApplication, "<strong>unsafe</strong>");
        UiApplication malformedApplication = Application();
        UiNode malformed = RawRoot(malformedApplication, "bad\ud800text");
        malformed.RawContentPolicy = new UiRawContentPolicy { IsTrusted = true, PolicyId = "approved" };

        UiRenderException untrustedFailure = Assert.Throws<UiRenderException>(() => new UiRenderer().RenderDocument(untrustedApplication))!;
        UiRenderException malformedFailure = Assert.Throws<UiRenderException>(() => new UiRenderer().RenderFragment(malformedApplication))!;

        Assert.Multiple(() =>
        {
            Assert.That(untrustedFailure.NodeId, Is.EqualTo(untrusted.Identity.StableId));
            Assert.That(untrustedFailure.OutputKind, Is.EqualTo("dom"));
            Assert.That(untrustedFailure.Message, Does.Contain("Raw content requires explicit trust"));
            Assert.That(malformedFailure.NodeId, Is.EqualTo(malformed.Identity.StableId));
            Assert.That(malformedFailure.OutputKind, Is.EqualTo("dom"));
            Assert.That(malformedFailure.Message, Does.Contain("valid HTML Unicode scalar values"));
        });
    }

    [Test]
    public void InlineSvgRejectsUnsupportedDescendantNamespaceWithStableNodeContext()
    {
        UiApplication application = Application();
        UiWindow window = application.Windows[0];
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        IUiFactory factory = new UiFactory();
        root.Kind = UiNodeKind.Control;
        root.Name = "inline-svg";
        root.Media = new UiMedia { Kind = "inline-svg", AlternativeText = "Diagram" };
        root.Children = new UiNode[]
        {
            factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "path", "urn:unsupported", "path")
        };
        window.HostCapabilities = new string[] { "media" };

        UiRenderException exception = Assert.Throws<UiRenderException>(() => new UiRenderer().RenderDocument(application))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.NodeId, Is.EqualTo(root.Identity.StableId));
            Assert.That(exception.OutputKind, Is.EqualTo("dom"));
            Assert.That(exception.Message, Does.Contain("Inline SVG element children must use the SVG namespace"));
            Assert.That(exception.Message, Does.Contain("node '" + root.Identity.StableId + "'"));
        });
    }

    private static UiNode RawRoot(UiApplication application, string text)
    {
        UiView view = application.Windows[0].Pages[0].ContentView!;
        UiNode root = view.ContentRoot!;
        root.Kind = UiNodeKind.RawContent;
        root.Name = string.Empty;
        root.Text = text;
        return root;
    }

    private static UiApplication Application()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("renderer-failure-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        view.ContentRoot = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        application.Windows = new UiWindow[] { window };
        return application;
    }

    private static void AssertFailure(UiRenderResult result, string code, string location)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(UiDiagnosticSeverity.Error));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].Location, Is.EqualTo(location));
            Assert.That(result.Diagnostics[0].Message, Is.Not.Empty);
            AssertNoPartialOutput(result);
        });
    }

    private static void AssertNoPartialOutput(UiRenderResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Html, Is.Empty);
            Assert.That(result.Css, Is.Empty);
            Assert.That(result.Bootstrap, Is.Empty);
            Assert.That(result.Snapshot, Is.Null);
            Assert.That(result.Hydration, Is.Null);
            Assert.That(result.Hash, Does.Match("^[0-9a-f]{64}$"));
        });
    }
}
