using System;
using htmlgui;

namespace htmlgui.Tests;

public class HierarchyTests
{
    [Test]
    public void HierarchyDefaultsAreSafeAndEmpty()
    {
        UiApplication application = new UiApplication();
        UiWindow window = new UiWindow();
        UiPage page = new UiPage();
        UiView view = new UiView();
        UiDocumentMetadata metadata = new UiDocumentMetadata();
        UiResource resource = new UiResource();
        UiSlot slot = new UiSlot();
        UiPortal portal = new UiPortal();

        Assert.Multiple(() =>
        {
            Assert.That(application.Id, Is.Empty);
            Assert.That(application.Key, Is.Empty);
            Assert.That(application.RootSeed, Is.Empty);
            Assert.That(application.ThemeKey, Is.Null);
            Assert.That(application.RouteIds, Is.Empty);
            Assert.That(application.CommandIds, Is.Empty);
            Assert.That(application.State, Is.Empty);
            Assert.That(application.Windows, Is.Empty);

            Assert.That(window.Id, Is.Empty);
            Assert.That(window.Key, Is.Empty);
            Assert.That(window.Document, Is.Not.Null);
            Assert.That(window.Pages, Is.Empty);
            Assert.That(window.CurrentPageKey, Is.Null);
            Assert.That(window.Overlays, Is.Empty);
            Assert.That(window.HostCapabilities, Is.Empty);
            Assert.That(window.IsActive, Is.True);

            Assert.That(page.Id, Is.Empty);
            Assert.That(page.Key, Is.Empty);
            Assert.That(page.RouteId, Is.Null);
            Assert.That(page.Title, Is.Empty);
            Assert.That(page.Language, Is.Null);
            Assert.That(page.TextDirection, Is.Null);
            Assert.That(page.Metadata, Is.Not.Null);
            Assert.That(page.Resources, Is.Empty);
            Assert.That(page.ShellView, Is.Null);
            Assert.That(page.ContentView, Is.Null);
            Assert.That(page.NotFoundView, Is.Null);
            Assert.That(page.ErrorView, Is.Null);
            Assert.That(page.LoadingView, Is.Null);

            Assert.That(view.Id, Is.Empty);
            Assert.That(view.Key, Is.Empty);
            Assert.That(view.ComponentTypeId, Is.Null);
            Assert.That(view.Parameters, Is.Empty);
            Assert.That(view.LocalStateScopeId, Is.Null);
            Assert.That(view.ContentRoot, Is.Null);
            Assert.That(view.LifecycleStage, Is.EqualTo(UiLifecycleStage.Create));
            Assert.That(view.FocusScopeId, Is.Null);
            Assert.That(view.ErrorBoundaryView, Is.Null);
            Assert.That(view.IsActive, Is.True);

            Assert.That(metadata.Title, Is.Empty);
            Assert.That(metadata.Language, Is.Null);
            Assert.That(metadata.TextDirection, Is.Null);
            Assert.That(metadata.CharacterSet, Is.EqualTo("utf-8"));
            Assert.That(metadata.Viewport, Is.EqualTo("width=device-width, initial-scale=1"));
            Assert.That(metadata.BaseUrl, Is.Null);
            Assert.That(metadata.Description, Is.Null);
            Assert.That(metadata.ApplicationName, Is.Null);
            Assert.That(metadata.ContentSecurityPolicyNonce, Is.Null);
            Assert.That(metadata.Values, Is.Empty);

            Assert.That(resource.Key, Is.Empty);
            Assert.That(resource.Url, Is.Empty);
            Assert.That(resource.Relationship, Is.Empty);
            Assert.That(resource.MimeType, Is.Null);
            Assert.That(resource.Media, Is.Null);
            Assert.That(resource.Integrity, Is.Null);
            Assert.That(resource.CrossOrigin, Is.Null);
            Assert.That(resource.ReferrerPolicy, Is.Null);
            Assert.That(resource.IsModule, Is.False);
            Assert.That(resource.Attributes, Is.Empty);

            Assert.That(slot.Name, Is.Empty);
            Assert.That(slot.Content, Is.Empty);
            Assert.That(slot.FallbackContent, Is.Empty);
            Assert.That(portal.Key, Is.Empty);
            Assert.That(portal.TargetRegion, Is.Empty);
            Assert.That(portal.Content, Is.Empty);
            Assert.That(portal.IsActive, Is.True);
        });
    }

    [Test]
    public void PopulatedApplicationFormsTraversablePortableGraph()
    {
        UiNode projectedText = new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = "slot-text", StableId = "ui-text-slot", ParentStableId = "ui-root", SiblingIdentity = "slot:0" },
            Text = "Projected"
        };
        UiNode fallbackText = new UiNode { Kind = UiNodeKind.Text, Text = "Fallback" };
        UiSlot slot = new UiSlot
        {
            Name = "content",
            Content = new UiNode[] { projectedText },
            FallbackContent = new UiNode[] { fallbackText }
        };
        UiNode overlayText = new UiNode { Kind = UiNodeKind.Text, Text = "Overlay" };
        UiPortal portal = new UiPortal
        {
            Key = "dialog-portal",
            TargetRegion = "modal",
            Content = new UiNode[] { overlayText },
            IsActive = false
        };
        UiNode root = new UiNode
        {
            Kind = UiNodeKind.Component,
            Identity = new UiNodeIdentity { AuthorKey = "root", StableId = "ui-component-root", SiblingIdentity = "root" },
            State = new UiNodeState { IsBusy = true, Version = 7 },
            NamespaceUri = "urn:component",
            Name = "dashboard",
            Children = new UiNode[] { projectedText },
            Slots = new UiSlot[] { slot },
            Portal = portal,
            Values = new UiValueEntry[] { Entry("mode", TextValue("full")) }
        };
        UiView errorBoundary = new UiView { Id = "view-error", Key = "error", IsActive = false };
        UiView contentView = new UiView
        {
            Id = "view-main",
            Key = "main",
            ComponentTypeId = "dashboard-component",
            Parameters = new UiValueEntry[] { Entry("tenant", TextValue("north")) },
            LocalStateScopeId = "state-main",
            ContentRoot = root,
            LifecycleStage = UiLifecycleStage.Attach,
            FocusScopeId = "focus-main",
            ErrorBoundaryView = errorBoundary
        };
        UiView shellView = new UiView { Id = "view-shell", Key = "shell" };
        UiView notFoundView = new UiView { Id = "view-not-found", Key = "not-found" };
        UiView loadingView = new UiView { Id = "view-loading", Key = "loading" };
        UiDocumentMetadata metadata = new UiDocumentMetadata
        {
            Title = "Dashboard",
            Language = "ar",
            TextDirection = "rtl",
            CharacterSet = "utf-8",
            Viewport = "width=device-width",
            BaseUrl = "https://example.test/app/",
            Description = "Portable dashboard",
            ApplicationName = "Example",
            ContentSecurityPolicyNonce = "nonce-1",
            Values = new UiValueEntry[] { Entry("theme-color", TextValue("#123456")) }
        };
        UiResource resource = new UiResource
        {
            Key = "main-css",
            Url = "/main.css",
            Relationship = "stylesheet",
            MimeType = "text/css",
            Media = "screen",
            Integrity = "sha256-example",
            CrossOrigin = "anonymous",
            ReferrerPolicy = "no-referrer",
            IsModule = true,
            Attributes = new UiValueEntry[] { Entry("fetchpriority", TextValue("high")) }
        };
        UiPage page = new UiPage
        {
            Id = "page-home",
            Key = "home",
            RouteId = "route-home",
            Title = "Home",
            Language = "en",
            TextDirection = "ltr",
            Metadata = metadata,
            Resources = new UiResource[] { resource },
            ShellView = shellView,
            ContentView = contentView,
            NotFoundView = notFoundView,
            ErrorView = errorBoundary,
            LoadingView = loadingView
        };
        UiView overlay = new UiView { Id = "view-overlay", Key = "overlay", ContentRoot = overlayText };
        UiWindow window = new UiWindow
        {
            Id = "window-main",
            Key = "main-window",
            Document = metadata,
            Pages = new UiPage[] { page },
            CurrentPageKey = "home",
            Overlays = new UiView[] { overlay },
            HostCapabilities = new string[] { "clipboard", "resize" },
            IsActive = false
        };
        UiApplication application = new UiApplication
        {
            Id = "application-example",
            Key = "example",
            RootSeed = "stable-seed",
            ThemeKey = "dark",
            RouteIds = new string[] { "route-home" },
            CommandIds = new string[] { "command-save" },
            State = new UiValueEntry[] { Entry("signedIn", new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true }) },
            Windows = new UiWindow[] { window }
        };

        Assert.Multiple(() =>
        {
            Assert.That(application.Id, Is.EqualTo("application-example"));
            Assert.That(application.RootSeed, Is.EqualTo("stable-seed"));
            Assert.That(application.ThemeKey, Is.EqualTo("dark"));
            Assert.That(application.RouteIds, Is.EqualTo(new string[] { "route-home" }));
            Assert.That(application.CommandIds, Is.EqualTo(new string[] { "command-save" }));
            Assert.That(application.State[0].Value.BooleanValue, Is.True);
            Assert.That(application.Windows[0], Is.SameAs(window));
            Assert.That(window.Document, Is.SameAs(metadata));
            Assert.That(window.Pages[0], Is.SameAs(page));
            Assert.That(window.CurrentPageKey, Is.EqualTo("home"));
            Assert.That(window.HostCapabilities, Is.EqualTo(new string[] { "clipboard", "resize" }));
            Assert.That(window.IsActive, Is.False);
            Assert.That(page.ContentView, Is.SameAs(contentView));
            Assert.That(page.ShellView, Is.SameAs(shellView));
            Assert.That(page.NotFoundView, Is.SameAs(notFoundView));
            Assert.That(page.ErrorView, Is.SameAs(errorBoundary));
            Assert.That(page.LoadingView, Is.SameAs(loadingView));
            Assert.That(page.Resources[0], Is.SameAs(resource));
            Assert.That(resource.Attributes[0].Key, Is.EqualTo("fetchpriority"));
            Assert.That(contentView.ContentRoot, Is.SameAs(root));
            Assert.That(contentView.ErrorBoundaryView, Is.SameAs(errorBoundary));
            Assert.That(root.Slots[0], Is.SameAs(slot));
            Assert.That(root.Portal, Is.SameAs(portal));
            Assert.That(slot.Content[0].Text, Is.EqualTo("Projected"));
            Assert.That(slot.FallbackContent[0].Text, Is.EqualTo("Fallback"));
            Assert.That(portal.Content[0].Text, Is.EqualTo("Overlay"));
            Assert.That(portal.IsActive, Is.False);
            Assert.That(root.Identity.StableId, Is.EqualTo("ui-component-root"));
            Assert.That(root.State.Version, Is.EqualTo(7));
            Assert.That(metadata.ContentSecurityPolicyNonce, Is.EqualTo("nonce-1"));
        });
    }

    [Test]
    public void NodeDefaultsProvideIndependentIdentityStateAndCollections()
    {
        UiNode first = new UiNode();
        UiNode second = new UiNode();

        first.Identity.AuthorKey = "first";
        first.State.IsBusy = true;

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(UiNodeKind.Element));
            Assert.That(first.NamespaceUri, Is.Null);
            Assert.That(first.Name, Is.Empty);
            Assert.That(first.Text, Is.Null);
            Assert.That(first.Children, Is.Empty);
            Assert.That(first.Slots, Is.Empty);
            Assert.That(first.Conditional, Is.Null);
            Assert.That(first.Repeat, Is.Null);
            Assert.That(first.Portal, Is.Null);
            Assert.That(first.Values, Is.Empty);
            Assert.That(first.Attributes, Is.Empty);
            Assert.That(first.TokenLists, Is.Empty);
            Assert.That(first.DataAttributes, Is.Empty);
            Assert.That(first.Properties, Is.Empty);
            Assert.That(first.IsCustomElement, Is.False);
            Assert.That(first.RawContentPolicy, Is.Null);
            Assert.That(second.Identity.AuthorKey, Is.Empty);
            Assert.That(second.State.IsBusy, Is.False);
            Assert.That(first.Identity, Is.Not.SameAs(second.Identity));
            Assert.That(first.State, Is.Not.SameAs(second.State));
        });
    }

    [Test]
    public void NodeStateHasExplicitOptionalValueFlags()
    {
        UiNodeState defaults = new UiNodeState();
        UiNodeState supplied = new UiNodeState
        {
            IsVisible = false,
            IsEnabled = false,
            IsBusy = true,
            IsFocusable = true,
            HasTabIndex = true,
            TabIndex = 0,
            HasPressed = true,
            IsPressed = false,
            HasSelected = true,
            IsSelected = false,
            HasExpanded = true,
            IsExpanded = false,
            HasCheckState = true,
            CheckState = UiCommandCheckState.Mixed,
            HasCurrentKind = true,
            CurrentKind = UiAriaCurrentKind.Page,
            Version = 12
        };

        Assert.Multiple(() =>
        {
            Assert.That(defaults.IsVisible, Is.True);
            Assert.That(defaults.IsEnabled, Is.True);
            Assert.That(defaults.IsBusy, Is.False);
            Assert.That(defaults.IsFocusable, Is.False);
            Assert.That(defaults.HasTabIndex, Is.False);
            Assert.That(defaults.HasPressed, Is.False);
            Assert.That(defaults.HasSelected, Is.False);
            Assert.That(defaults.HasExpanded, Is.False);
            Assert.That(defaults.HasCheckState, Is.False);
            Assert.That(defaults.HasCurrentKind, Is.False);
            Assert.That(defaults.Version, Is.Zero);
            Assert.That(supplied.IsVisible, Is.False);
            Assert.That(supplied.IsEnabled, Is.False);
            Assert.That(supplied.HasTabIndex, Is.True);
            Assert.That(supplied.TabIndex, Is.Zero);
            Assert.That(supplied.HasPressed, Is.True);
            Assert.That(supplied.IsPressed, Is.False);
            Assert.That(supplied.HasSelected, Is.True);
            Assert.That(supplied.IsSelected, Is.False);
            Assert.That(supplied.HasExpanded, Is.True);
            Assert.That(supplied.IsExpanded, Is.False);
            Assert.That(supplied.CheckState, Is.EqualTo(UiCommandCheckState.Mixed));
            Assert.That(supplied.CurrentKind, Is.EqualTo(UiAriaCurrentKind.Page));
            Assert.That(supplied.Version, Is.EqualTo(12));
        });
    }

    [Test]
    public void ApplicationCanFlowThroughSourceContract()
    {
        UiApplication application = new UiApplication { Id = "app" };
        IUiApplicationSource source = new TestApplicationSource(application);

        UiApplication result = source.GetApplication();

        Assert.That(result, Is.SameAs(application));
    }

    private static UiValue TextValue(string text)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = text };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private sealed class TestApplicationSource : IUiApplicationSource
    {
        private readonly UiApplication application;

        public TestApplicationSource(UiApplication application)
        {
            this.application = application;
        }

        public UiApplication GetApplication()
        {
            return application;
        }
    }
}
