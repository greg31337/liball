using System;
using htmlgui;

namespace htmlgui.Tests;

public class HierarchyCompositionCoverageTests
{
    [Test]
    public void ExpandsMultipleWindowsPagesAndViewsInDeclaredOrderWithStableIds()
    {
        UiApplication firstSource = CreateHierarchy("hierarchy-seed");
        UiApplication secondSource = CreateHierarchy("hierarchy-seed");
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandApplication(firstSource);
        DomNode second = expander.ExpandApplication(secondSource);

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(first.Children, child => child.Key), Is.EqualTo(new string[] { "primary", "secondary" }));
            Assert.That(Array.ConvertAll(first.Children[0].Children, child => child.Key), Is.EqualTo(new string[] { "home", "settings", "primary-overlay" }));
            Assert.That(Array.ConvertAll(first.Children[1].Children, child => child.Key), Is.EqualTo(new string[] { "reports", "secondary-overlay" }));
            Assert.That(Array.ConvertAll(first.Children[0].Children[0].Children, child => child.Key), Is.EqualTo(new string[] { "home-shell", "home-content", "home-missing", "home-error", "home-loading" }));
            Assert.That(first.Children[0].Children[0].Children[1].Children[0].Text, Is.EqualTo("home-content"));
            Assert.That(first.Id, Is.EqualTo(second.Id));
            Assert.That(first.Children[0].Id, Is.EqualTo(second.Children[0].Id));
            Assert.That(first.Children[0].Children[1].Id, Is.EqualTo(second.Children[0].Children[1].Id));
            Assert.That(first.Children[1].Children[0].Children[0].Id, Is.EqualTo(second.Children[1].Children[0].Children[0].Id));
            Assert.That(first.Children[0].Id, Is.Not.EqualTo(first.Children[1].Id));
        });
    }

    [Test]
    public void NormalizedMetadataInheritanceFlowsIntoComposedDocumentsWithPageOverrides()
    {
        IUiFactory factory = new UiFactory();
        UiApplication source = factory.CreateApplication("metadata-seed", "application");
        UiWindow window = factory.CreateWindow(source.RootSeed, source.Id, "window");
        window.Document = new UiDocumentMetadata
        {
            Title = "Window title",
            Language = "en-US",
            TextDirection = "ltr",
            CharacterSet = "windows-1252",
            Viewport = "width=1200",
            BaseUrl = "https://example.test/root/",
            Description = "Window description",
            ApplicationName = "Window application",
            Values = new UiValueEntry[]
            {
                TextEntry("region", "west"),
                TextEntry("theme", "window")
            }
        };
        UiPage inherited = factory.CreatePage(source.RootSeed, window.Id, "inherited");
        UiPage overridden = factory.CreatePage(source.RootSeed, window.Id, "overridden");
        overridden.Title = "Page title";
        overridden.Language = "fr-FR";
        overridden.TextDirection = "rtl";
        overridden.Metadata = new UiDocumentMetadata
        {
            CharacterSet = "utf-16",
            Viewport = "width=800",
            BaseUrl = "https://example.test/page/",
            Description = "Page description",
            Values = new UiValueEntry[]
            {
                TextEntry("theme", "page"),
                TextEntry("density", "compact")
            }
        };
        window.Pages = new UiPage[] { inherited, overridden };
        source.Windows = new UiWindow[] { window };
        ModelNormalizer normalizer = new ModelNormalizer();
        DocumentComposer composer = new DocumentComposer();

        UiApplication normalized = normalizer.NormalizeApplication(source);
        UiWindow normalizedWindow = normalized.Windows[0];
        UiPage normalizedInherited = FindPage(normalizedWindow, "inherited");
        UiPage normalizedOverridden = FindPage(normalizedWindow, "overridden");
        DomNode inheritedDocument = composer.ComposePageDocument(normalizedWindow, normalizedInherited);
        DomNode overriddenDocument = composer.ComposePageDocument(normalizedWindow, normalizedOverridden);
        DomNode inheritedHtml = inheritedDocument.Children[1];
        DomNode inheritedHead = inheritedHtml.Children[0];
        DomNode overriddenHtml = overriddenDocument.Children[1];
        DomNode overriddenHead = overriddenHtml.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(inheritedHtml, HtmlAttributes.Language), Is.EqualTo("en-US"));
            Assert.That(AttributeValue(inheritedHtml, HtmlAttributes.Direction), Is.EqualTo("ltr"));
            Assert.That(HeadChild(inheritedHead, "$title").Children[0].Text, Is.EqualTo("Window title"));
            Assert.That(AttributeValue(HeadChild(inheritedHead, "$charset"), HtmlAttributes.CharacterSet), Is.EqualTo("windows-1252"));
            Assert.That(AttributeValue(HeadChild(inheritedHead, "$viewport"), HtmlAttributes.Content), Is.EqualTo("width=1200"));
            Assert.That(AttributeValue(HeadChild(inheritedHead, "$base"), HtmlAttributes.HyperlinkReference), Is.EqualTo("https://example.test/root/"));
            Assert.That(AttributeValue(HeadChild(inheritedHead, "$description"), HtmlAttributes.Content), Is.EqualTo("Window description"));
            Assert.That(AttributeValue(HeadChild(inheritedHead, "$application-name"), HtmlAttributes.Content), Is.EqualTo("Window application"));
            Assert.That(AttributeValue(HeadChild(inheritedHead, "$metadata:region"), HtmlAttributes.Content), Is.EqualTo("west"));
            Assert.That(AttributeValue(HeadChild(inheritedHead, "$metadata:theme"), HtmlAttributes.Content), Is.EqualTo("window"));

            Assert.That(AttributeValue(overriddenHtml, HtmlAttributes.Language), Is.EqualTo("fr-FR"));
            Assert.That(AttributeValue(overriddenHtml, HtmlAttributes.Direction), Is.EqualTo("rtl"));
            Assert.That(HeadChild(overriddenHead, "$title").Children[0].Text, Is.EqualTo("Page title"));
            Assert.That(AttributeValue(HeadChild(overriddenHead, "$charset"), HtmlAttributes.CharacterSet), Is.EqualTo("utf-16"));
            Assert.That(AttributeValue(HeadChild(overriddenHead, "$viewport"), HtmlAttributes.Content), Is.EqualTo("width=800"));
            Assert.That(AttributeValue(HeadChild(overriddenHead, "$base"), HtmlAttributes.HyperlinkReference), Is.EqualTo("https://example.test/page/"));
            Assert.That(AttributeValue(HeadChild(overriddenHead, "$description"), HtmlAttributes.Content), Is.EqualTo("Page description"));
            Assert.That(AttributeValue(HeadChild(overriddenHead, "$application-name"), HtmlAttributes.Content), Is.EqualTo("Window application"));
            Assert.That(AttributeValue(HeadChild(overriddenHead, "$metadata:region"), HtmlAttributes.Content), Is.EqualTo("west"));
            Assert.That(AttributeValue(HeadChild(overriddenHead, "$metadata:theme"), HtmlAttributes.Content), Is.EqualTo("page"));
            Assert.That(AttributeValue(HeadChild(overriddenHead, "$metadata:density"), HtmlAttributes.Content), Is.EqualTo("compact"));

            Assert.That(inherited.Title, Is.Empty);
            Assert.That(inherited.Metadata.BaseUrl, Is.Null);
            Assert.That(inherited.Metadata.Values, Is.Empty);
            Assert.That(overridden.Metadata.ApplicationName, Is.Null);
            Assert.That(overridden.Metadata.Values, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void NestedSlotsFragmentsConditionsAndRepeatsPreserveContainerAndItemIdentity()
    {
        UiNode conditional = Node(UiNodeKind.Conditional, "condition", "condition-id");
        conditional.Conditional = new UiConditional
        {
            Condition = true,
            WhenTrue = new UiNode[] { TextNode("true", "true-id", "True branch") },
            WhenFalse = new UiNode[] { TextNode("false", "false-id", "False branch") }
        };
        UiCollectionItem alpha = new UiCollectionItem { Key = "alpha", Content = TextNode("alpha-content", "alpha-content-id", "Alpha") };
        UiCollectionItem beta = new UiCollectionItem { Key = "beta", Content = TextNode("beta-content", "beta-content-id", "Beta") };
        UiNode repeat = Node(UiNodeKind.Repeat, "items", "items-id");
        repeat.Repeat = new UiRepeat { Items = new UiCollectionItem[] { alpha, beta } };
        UiNode projectedFragment = Node(UiNodeKind.Fragment, "projected", "projected-id");
        projectedFragment.Children = new UiNode[] { conditional, repeat };
        UiNode slot = Node(UiNodeKind.Slot, "slot", "slot-id");
        slot.Name = "body";
        UiNode component = Node(UiNodeKind.Component, "component", "component-id");
        component.Children = new UiNode[] { slot };
        component.Slots = new UiSlot[] { new UiSlot { Name = "body", Content = new UiNode[] { projectedFragment } } };
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(component);
        conditional.Conditional.Condition = false;
        repeat.Repeat.Items = new UiCollectionItem[] { beta, alpha };
        DomNode changed = expander.ExpandNode(component);
        DomNode firstProjection = first.Children[0].Children[0];
        DomNode changedProjection = changed.Children[0].Children[0];
        DomNode firstConditional = firstProjection.Children[0];
        DomNode changedConditional = changedProjection.Children[0];
        DomNode firstRepeat = firstProjection.Children[1];
        DomNode changedRepeat = changedProjection.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(changed.Id, Is.EqualTo(first.Id));
            Assert.That(changed.Children[0].Id, Is.EqualTo(first.Children[0].Id));
            Assert.That(changedProjection.Id, Is.EqualTo(firstProjection.Id));
            Assert.That(changedConditional.Id, Is.EqualTo(firstConditional.Id));
            Assert.That(firstConditional.Children[0].Text, Is.EqualTo("True branch"));
            Assert.That(changedConditional.Children[0].Text, Is.EqualTo("False branch"));
            Assert.That(changedRepeat.Id, Is.EqualTo(firstRepeat.Id));
            Assert.That(Array.ConvertAll(firstRepeat.Children, child => child.Key), Is.EqualTo(new string[] { "alpha", "beta" }));
            Assert.That(Array.ConvertAll(changedRepeat.Children, child => child.Key), Is.EqualTo(new string[] { "beta", "alpha" }));
            Assert.That(changedRepeat.Children[0].Id, Is.EqualTo(firstRepeat.Children[1].Id));
            Assert.That(changedRepeat.Children[1].Id, Is.EqualTo(firstRepeat.Children[0].Id));
        });
    }

    [Test]
    public void MultiplePortalsKeepSourceOrderPerTargetAndStableIdsAcrossReordering()
    {
        UiNode firstModal = PortalNode("first-modal-node", "first-modal-node-id", "first-modal", "modal", "First modal");
        UiNode toast = PortalNode("toast-node", "toast-node-id", "toast", "notifications", "Toast");
        UiNode secondModal = PortalNode("second-modal-node", "second-modal-node-id", "second-modal", "modal", "Second modal");
        UiNode root = Node(UiNodeKind.Fragment, "root", "root-id");
        root.Children = new UiNode[] { firstModal, toast, secondModal };
        UiPage page = new UiPage
        {
            Key = "page",
            Id = "page-id",
            ContentView = new UiView { Key = "content", Id = "content-id", ContentRoot = root }
        };
        UiWindow window = new UiWindow
        {
            Key = "window",
            Id = "window-id",
            Overlays = new UiView[]
            {
                new UiView { Key = "modal", Id = "modal-id" },
                new UiView { Key = "notifications", Id = "notifications-id" }
            }
        };
        DocumentComposer composer = new DocumentComposer();

        DomNode firstDocument = composer.ComposePageDocument(window, page);
        DomNode firstBody = firstDocument.Children[1].Children[1];
        DomNode firstModalRegion = firstBody.Children[1];
        DomNode firstNotificationRegion = firstBody.Children[2];
        root.Children = new UiNode[] { secondModal, toast, firstModal };
        DomNode reorderedDocument = composer.ComposePageDocument(window, page);
        DomNode reorderedBody = reorderedDocument.Children[1].Children[1];
        DomNode reorderedModalRegion = reorderedBody.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(firstModalRegion.Children, child => child.Key), Is.EqualTo(new string[] { "first-modal", "second-modal" }));
            Assert.That(firstModalRegion.Children[0].Children[0].Text, Is.EqualTo("First modal"));
            Assert.That(firstModalRegion.Children[1].Children[0].Text, Is.EqualTo("Second modal"));
            Assert.That(Array.ConvertAll(firstNotificationRegion.Children, child => child.Key), Is.EqualTo(new string[] { "toast" }));
            Assert.That(firstNotificationRegion.Children[0].Children[0].Text, Is.EqualTo("Toast"));
            Assert.That(Array.ConvertAll(reorderedModalRegion.Children, child => child.Key), Is.EqualTo(new string[] { "second-modal", "first-modal" }));
            Assert.That(reorderedModalRegion.Children[0].Id, Is.EqualTo(firstModalRegion.Children[1].Id));
            Assert.That(reorderedModalRegion.Children[1].Id, Is.EqualTo(firstModalRegion.Children[0].Id));
            Assert.That(Array.ConvertAll(firstBody.Children[0].Children[0].Children, child => child.Id), Is.EqualTo(new string[] { "first-modal-node-id", "toast-node-id", "second-modal-node-id" }));
        });
    }

    private static UiApplication CreateHierarchy(string seed)
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication(seed, "application");
        UiWindow primary = factory.CreateWindow(seed, application.Id, "primary");
        UiWindow secondary = factory.CreateWindow(seed, application.Id, "secondary");
        UiPage home = Page(factory, seed, primary.Id, "home", true);
        UiPage settings = Page(factory, seed, primary.Id, "settings", false);
        UiPage reports = Page(factory, seed, secondary.Id, "reports", false);
        primary.Pages = new UiPage[] { home, settings };
        primary.Overlays = new UiView[] { View(factory, seed, primary.Id, "primary-overlay") };
        secondary.Pages = new UiPage[] { reports };
        secondary.Overlays = new UiView[] { View(factory, seed, secondary.Id, "secondary-overlay") };
        application.Windows = new UiWindow[] { primary, secondary };
        return application;
    }

    private static UiPage Page(IUiFactory factory, string seed, string parentId, string key, bool includeFallbacks)
    {
        UiPage page = factory.CreatePage(seed, parentId, key);
        page.ShellView = View(factory, seed, page.Id, key + "-shell");
        page.ContentView = View(factory, seed, page.Id, key + "-content");
        if (includeFallbacks)
        {
            page.NotFoundView = View(factory, seed, page.Id, key + "-missing");
            page.ErrorView = View(factory, seed, page.Id, key + "-error");
            page.LoadingView = View(factory, seed, page.Id, key + "-loading");
        }

        return page;
    }

    private static UiView View(IUiFactory factory, string seed, string parentId, string key)
    {
        UiView view = factory.CreateView(seed, parentId, key);
        view.ContentRoot = factory.CreateTextNode(seed, view.Id, key + "-text", key);
        return view;
    }

    private static UiNode PortalNode(string key, string id, string portalKey, string target, string text)
    {
        UiNode node = Node(UiNodeKind.Portal, key, id);
        node.Portal = new UiPortal
        {
            Key = portalKey,
            TargetRegion = target,
            Content = new UiNode[] { TextNode(portalKey + "-content", portalKey + "-content-id", text) }
        };
        return node;
    }

    private static UiNode TextNode(string key, string id, string text)
    {
        UiNode node = Node(UiNodeKind.Text, key, id);
        node.Text = text;
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

    private static UiValueEntry TextEntry(string key, string value)
    {
        return new UiValueEntry
        {
            Key = key,
            Value = new UiValue { Kind = UiValueKind.String, StringValue = value }
        };
    }

    private static UiPage FindPage(UiWindow window, string key)
    {
        int index = 0;
        while (index < window.Pages.Length)
        {
            if (window.Pages[index].Key == key)
            {
                return window.Pages[index];
            }

            index++;
        }

        throw new AssertionException("Page was not found: " + key);
    }

    private static DomNode HeadChild(DomNode head, string key)
    {
        int index = 0;
        while (index < head.Children.Length)
        {
            if (head.Children[index].Key == key)
            {
                return head.Children[index];
            }

            index++;
        }

        throw new AssertionException("Head child was not found: " + key);
    }

    private static string AttributeValue(DomNode node, string name)
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

        throw new AssertionException("Attribute was not found: " + name);
    }
}
