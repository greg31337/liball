using System;
using htmlgui;

namespace htmlgui.Tests;

public class SemanticExpanderTests
{
    [Test]
    public void ExpandsHierarchyComponentAndControlNodesIntoAKeyedTree()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("semantic-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "primary-window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "home-page");
        UiView shell = factory.CreateView(application.RootSeed, page.Id, "shell-view");
        UiView content = factory.CreateView(application.RootSeed, page.Id, "content-view");
        UiNode component = CreateNode(UiNodeKind.Component, "component", "component-id", 3);
        UiNode control = CreateNode(UiNodeKind.Control, "control", "control-id", 7);
        UiNode text = CreateNode(UiNodeKind.Text, "label", "label-id", 11);
        text.Text = "Ready";
        control.Children = new UiNode[] { text };
        component.Children = new UiNode[] { control };
        content.ContentRoot = component;
        page.ShellView = shell;
        page.ContentView = content;
        window.Pages = new UiPage[] { page };
        application.Windows = new UiWindow[] { window };
        SemanticExpander expander = new SemanticExpander();

        DomNode root = expander.ExpandApplication(application);
        DomNode expandedWindow = root.Children[0];
        DomNode expandedPage = expandedWindow.Children[0];
        DomNode expandedContent = expandedPage.Children[1];
        DomNode expandedComponent = expandedContent.Children[0];
        DomNode expandedControl = expandedComponent.Children[0];
        DomNode expandedText = expandedControl.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(root.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(root.Key, Is.EqualTo("application"));
            Assert.That(root.Id, Is.EqualTo(application.Id));
            Assert.That(expandedWindow.Key, Is.EqualTo("primary-window"));
            Assert.That(expandedWindow.Id, Is.EqualTo(window.Id));
            Assert.That(expandedPage.Key, Is.EqualTo("home-page"));
            Assert.That(expandedPage.Id, Is.EqualTo(page.Id));
            Assert.That(expandedPage.Children[0].Key, Is.EqualTo("shell-view"));
            Assert.That(expandedContent.Key, Is.EqualTo("content-view"));
            Assert.That(expandedComponent.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(expandedComponent.Key, Is.EqualTo("component"));
            Assert.That(expandedComponent.Id, Is.EqualTo("component-id"));
            Assert.That(expandedComponent.Version, Is.EqualTo(3));
            Assert.That(expandedControl.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(expandedControl.Key, Is.EqualTo("control"));
            Assert.That(expandedControl.Id, Is.EqualTo("control-id"));
            Assert.That(expandedControl.Version, Is.EqualTo(7));
            Assert.That(expandedText.Kind, Is.EqualTo(DomNodeKind.Text));
            Assert.That(expandedText.Key, Is.EqualTo("label"));
            Assert.That(expandedText.Text, Is.EqualTo("Ready"));
            Assert.That(expandedText.Version, Is.EqualTo(11));
        });
    }

    [Test]
    public void PreservesPageViewAndOverlayOrder()
    {
        UiPage page = new UiPage
        {
            Id = "page-id",
            Key = "page",
            ShellView = new UiView { Id = "shell-id", Key = "shell" },
            ContentView = new UiView { Id = "content-id", Key = "content" },
            NotFoundView = new UiView { Id = "missing-id", Key = "missing" },
            ErrorView = new UiView { Id = "error-id", Key = "error" },
            LoadingView = new UiView { Id = "loading-id", Key = "loading" }
        };
        UiWindow window = new UiWindow
        {
            Id = "window-id",
            Key = "window",
            Pages = new UiPage[] { page, new UiPage { Id = "second-id", Key = "second" } },
            Overlays = new UiView[]
            {
                new UiView { Id = "first-overlay-id", Key = "first-overlay" },
                new UiView { Id = "second-overlay-id", Key = "second-overlay" }
            }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedWindow = expander.ExpandWindow(window);
        DomNode expandedPage = expandedWindow.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(expandedWindow.Children, child => child.Key), Is.EqualTo(new string[] { "page", "second", "first-overlay", "second-overlay" }));
            Assert.That(Array.ConvertAll(expandedPage.Children, child => child.Key), Is.EqualTo(new string[] { "shell", "content", "missing", "error", "loading" }));
        });
    }

    [Test]
    public void ExpansionDoesNotMutateOrReuseSourceObjects()
    {
        UiApplication application = new UiApplication { Id = "application-id", Key = "application" };
        UiWindow window = new UiWindow { Id = "window-id", Key = "window" };
        UiPage page = new UiPage { Id = "page-id", Key = "page" };
        UiView view = new UiView { Id = "view-id", Key = "view" };
        UiNode component = CreateNode(UiNodeKind.Component, "component", "component-id", 1);
        UiNode control = CreateNode(UiNodeKind.Control, "control", "control-id", 2);
        UiNode[] sourceChildren = new UiNode[] { control };
        component.Children = sourceChildren;
        view.ContentRoot = component;
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        application.Windows = new UiWindow[] { window };
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandApplication(application);
        DomNode second = expander.ExpandApplication(application);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(first.Children[0], Is.Not.SameAs(second.Children[0]));
            Assert.That(component.Children, Is.SameAs(sourceChildren));
            Assert.That(component.Children[0], Is.SameAs(control));
            Assert.That(component.Identity.AuthorKey, Is.EqualTo("component"));
            Assert.That(control.Identity.AuthorKey, Is.EqualTo("control"));
        });
    }

    [Test]
    public void CarriesBasicLowLevelNodeKindsWithoutApplyingHtmlPolicy()
    {
        UiNode element = CreateNode(UiNodeKind.Element, "element", "element-id", 1);
        element.NamespaceUri = "urn:test";
        element.Name = "sample";
        UiNode comment = CreateNode(UiNodeKind.Comment, "comment", "comment-id", 2);
        comment.Text = "note";
        UiNode raw = CreateNode(UiNodeKind.RawContent, "raw", "raw-id", 3);
        raw.Text = "<trusted>";
        raw.RawContentPolicy = new UiRawContentPolicy { IsTrusted = true, PolicyId = "test-policy" };
        element.Children = new UiNode[] { comment, raw };
        SemanticExpander expander = new SemanticExpander();

        DomNode expanded = expander.ExpandNode(element);

        Assert.Multiple(() =>
        {
            Assert.That(expanded.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(expanded.NamespaceUri, Is.EqualTo("urn:test"));
            Assert.That(expanded.Name, Is.EqualTo("sample"));
            Assert.That(expanded.Children[0].Kind, Is.EqualTo(DomNodeKind.Comment));
            Assert.That(expanded.Children[0].Text, Is.EqualTo("note"));
            Assert.That(expanded.Children[1].Kind, Is.EqualTo(DomNodeKind.RawContent));
            Assert.That(expanded.Children[1].Text, Is.EqualTo("<trusted>"));
        });
    }

    [TestCase(UiNodeKind.Fragment)]
    [TestCase(UiNodeKind.Component)]
    [TestCase(UiNodeKind.Control)]
    public void LogicalContainerKindsRemainKeyedFragments(UiNodeKind kind)
    {
        UiNode node = CreateNode(kind, "container", "container-id", 5);
        SemanticExpander expander = new SemanticExpander();

        DomNode expanded = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(expanded.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(expanded.Key, Is.EqualTo("container"));
            Assert.That(expanded.Id, Is.EqualTo("container-id"));
            Assert.That(expanded.Version, Is.EqualTo(5));
        });
    }

    [Test]
    public void UsesSafeEmptyResultsForAbsentCollectionsAndFallbackSiblingKeys()
    {
        UiApplication application = new UiApplication
        {
            Id = "application-id",
            Key = "application",
            Windows = null!
        };
        UiWindow window = new UiWindow
        {
            Id = "window-id",
            Key = "window",
            Pages = null!,
            Overlays = null!
        };
        UiNode node = CreateNode(UiNodeKind.Component, string.Empty, "component-id", 0);
        node.Identity.SiblingIdentity = "fallback-key";
        node.Children = null!;
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedApplication = expander.ExpandApplication(application);
        DomNode expandedWindow = expander.ExpandWindow(window);
        DomNode expandedNode = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(expandedApplication.Children, Is.Empty);
            Assert.That(expandedWindow.Children, Is.Empty);
            Assert.That(expandedNode.Key, Is.EqualTo("fallback-key"));
            Assert.That(expandedNode.Children, Is.Empty);
            Assert.That(expander.ExpandWindows(null), Is.Empty);
            Assert.That(expander.ExpandNodes(null), Is.Empty);
        });
    }

    [Test]
    public void RejectsMissingApplicationsAndUnsupportedNodeKinds()
    {
        SemanticExpander expander = new SemanticExpander();
        UiNode unsupported = CreateNode((UiNodeKind)999, "unsupported", "unsupported-id", 0);

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => expander.ExpandApplication(null))!.ParamName, Is.EqualTo("application"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(unsupported))!.ParamName, Is.EqualTo("kind"));
        });
    }

    private static UiNode CreateNode(UiNodeKind kind, string key, string id, int version)
    {
        return new UiNode
        {
            Kind = kind,
            Identity = new UiNodeIdentity
            {
                AuthorKey = key,
                SiblingIdentity = key,
                StableId = id
            },
            State = new UiNodeState { Version = version }
        };
    }
}
