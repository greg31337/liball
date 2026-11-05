using System;
using htmlgui;

namespace htmlgui.Tests;

public class SemanticControlFlowTests
{
    [Test]
    public void ConditionalSwitchesContentWithoutChangingItsOwnIdentity()
    {
        UiNode whenTrue = Text("true", "true-id", "Enabled");
        UiNode whenFalse = Text("false", "false-id", "Disabled");
        UiNode conditional = Node(UiNodeKind.Conditional, "condition", "condition-id");
        conditional.State.Version = 4;
        conditional.Conditional = new UiConditional
        {
            Condition = true,
            WhenTrue = new UiNode[] { whenTrue },
            WhenFalse = new UiNode[] { whenFalse }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode trueResult = expander.ExpandNode(conditional);
        conditional.Conditional.Condition = false;
        DomNode falseResult = expander.ExpandNode(conditional);

        Assert.Multiple(() =>
        {
            Assert.That(trueResult.Kind, Is.EqualTo(DomNodeKind.Fragment));
            Assert.That(trueResult.Key, Is.EqualTo("condition"));
            Assert.That(trueResult.Id, Is.EqualTo("condition-id"));
            Assert.That(trueResult.Version, Is.EqualTo(4));
            Assert.That(trueResult.Children[0].Text, Is.EqualTo("Enabled"));
            Assert.That(falseResult.Key, Is.EqualTo(trueResult.Key));
            Assert.That(falseResult.Id, Is.EqualTo(trueResult.Id));
            Assert.That(falseResult.Children[0].Text, Is.EqualTo("Disabled"));
            Assert.That(conditional.Conditional.WhenTrue[0], Is.SameAs(whenTrue));
            Assert.That(conditional.Conditional.WhenFalse[0], Is.SameAs(whenFalse));
        });
    }

    [Test]
    public void RepeatItemIdentitySurvivesReorderingAndContentChanges()
    {
        UiCollectionItem alpha = new UiCollectionItem { Key = "alpha", Content = Text("alpha-content", "alpha-content-id", "Alpha") };
        UiCollectionItem beta = new UiCollectionItem { Key = "beta", Content = Text("beta-content", "beta-content-id", "Beta") };
        UiNode repeat = Node(UiNodeKind.Repeat, "repeat", "repeat-id");
        repeat.Repeat = new UiRepeat { Items = new UiCollectionItem[] { alpha, beta } };
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(repeat);
        beta.Content!.Text = "Changed beta";
        repeat.Repeat.Items = new UiCollectionItem[] { beta, alpha };
        DomNode reordered = expander.ExpandNode(repeat);

        Assert.Multiple(() =>
        {
            Assert.That(Array.ConvertAll(first.Children, child => child.Key), Is.EqualTo(new string[] { "alpha", "beta" }));
            Assert.That(Array.ConvertAll(reordered.Children, child => child.Key), Is.EqualTo(new string[] { "beta", "alpha" }));
            Assert.That(reordered.Children[0].Id, Is.EqualTo(first.Children[1].Id));
            Assert.That(reordered.Children[1].Id, Is.EqualTo(first.Children[0].Id));
            Assert.That(reordered.Children[0].Children[0].Text, Is.EqualTo("Changed beta"));
            Assert.That(first.Children[1].Children[0].Text, Is.EqualTo("Beta"));
            Assert.That(first.Id, Is.EqualTo(reordered.Id));
        });
    }

    [Test]
    public void RepeatRejectsMissingDescriptionsKeysAndDuplicates()
    {
        UiNode missingDescription = Node(UiNodeKind.Repeat, "missing", "missing-id");
        UiNode missingKey = Node(UiNodeKind.Repeat, "missing-key", "missing-key-id");
        missingKey.Repeat = new UiRepeat { Items = new UiCollectionItem[] { new UiCollectionItem() } };
        UiNode duplicate = Node(UiNodeKind.Repeat, "duplicate", "duplicate-id");
        duplicate.Repeat = new UiRepeat
        {
            Items = new UiCollectionItem[]
            {
                new UiCollectionItem { Key = "item" },
                new UiCollectionItem { Key = "item" }
            }
        };
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingDescription));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingKey));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicate));
        });
    }

    [Test]
    public void ComponentSlotsPreferSuppliedContentThenDeclaredAndLocalFallbacks()
    {
        UiNode placeholder = Node(UiNodeKind.Slot, "placeholder", "placeholder-id");
        placeholder.Name = "body";
        placeholder.Children = new UiNode[] { Text("local", "local-id", "Local fallback") };
        UiSlot projected = new UiSlot
        {
            Name = "body",
            Content = new UiNode[] { Text("supplied", "supplied-id", "Supplied") },
            FallbackContent = new UiNode[] { Text("declared", "declared-id", "Declared fallback") }
        };
        UiNode component = Node(UiNodeKind.Component, "component", "component-id");
        component.Children = new UiNode[] { placeholder };
        component.Slots = new UiSlot[] { projected };
        SemanticExpander expander = new SemanticExpander();

        DomNode supplied = expander.ExpandNode(component);
        projected.Content = Array.Empty<UiNode>();
        DomNode declaredFallback = expander.ExpandNode(component);
        component.Slots = Array.Empty<UiSlot>();
        DomNode localFallback = expander.ExpandNode(component);

        Assert.Multiple(() =>
        {
            Assert.That(supplied.Children[0].Id, Is.EqualTo("placeholder-id"));
            Assert.That(supplied.Children[0].Children[0].Text, Is.EqualTo("Supplied"));
            Assert.That(declaredFallback.Children[0].Id, Is.EqualTo("placeholder-id"));
            Assert.That(declaredFallback.Children[0].Children[0].Text, Is.EqualTo("Declared fallback"));
            Assert.That(localFallback.Children[0].Id, Is.EqualTo("placeholder-id"));
            Assert.That(localFallback.Children[0].Children[0].Text, Is.EqualTo("Local fallback"));
        });
    }

    [Test]
    public void DocumentCompositionRelocatesActivePortalsIntoNamedOverlayRegions()
    {
        UiNode portalNode = Node(UiNodeKind.Portal, "portal-node", "portal-node-id");
        portalNode.Portal = new UiPortal
        {
            Key = "dialog-portal",
            TargetRegion = "modal",
            Content = new UiNode[] { Text("dialog", "dialog-id", "Dialog content") },
            IsActive = true
        };
        UiView content = new UiView { Id = "content-id", Key = "content", ContentRoot = portalNode };
        UiView overlay = new UiView { Id = "overlay-id", Key = "modal" };
        UiWindow window = new UiWindow { Id = "window-id", Key = "window", Overlays = new UiView[] { overlay } };
        UiPage page = new UiPage { Id = "page-id", Key = "page", ContentView = content };
        DocumentComposer composer = new DocumentComposer();

        DomNode activeDocument = composer.ComposePageDocument(window, page);
        DomNode activeBody = activeDocument.Children[1].Children[1];
        DomNode activePlaceholder = activeBody.Children[0].Children[0];
        DomNode activePortal = activeBody.Children[1].Children[0];
        portalNode.Portal.IsActive = false;
        DomNode inactiveDocument = composer.ComposePageDocument(window, page);
        DomNode inactiveBody = inactiveDocument.Children[1].Children[1];
        DomNode inactivePortal = inactiveBody.Children[1].Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(activePlaceholder.Key, Is.EqualTo("portal-node"));
            Assert.That(activePlaceholder.Id, Is.EqualTo("portal-node-id"));
            Assert.That(activePlaceholder.Children, Is.Empty);
            Assert.That(activePortal.Key, Is.EqualTo("dialog-portal"));
            Assert.That(activePortal.Children[0].Text, Is.EqualTo("Dialog content"));
            Assert.That(inactivePortal.Key, Is.EqualTo("dialog-portal"));
            Assert.That(inactivePortal.Id, Is.EqualTo(activePortal.Id));
            Assert.That(inactivePortal.Children, Is.Empty);
        });
    }

    [Test]
    public void PortalExpandedAloneRetainsContentAndRejectsUnknownDocumentTargets()
    {
        UiNode portalNode = Node(UiNodeKind.Portal, "portal-node", "portal-node-id");
        portalNode.Portal = new UiPortal
        {
            Key = "menu-portal",
            TargetRegion = "menu",
            Content = new UiNode[] { Text("item", "item-id", "Item") }
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode inline = expander.ExpandNode(portalNode);
        UiWindow window = new UiWindow { Overlays = new UiView[] { new UiView { Key = "modal", Id = "modal-id" } } };
        UiPage page = new UiPage { Key = "page", Id = "page-id", ContentView = new UiView { Key = "content", Id = "content-id", ContentRoot = portalNode } };
        DocumentComposer composer = new DocumentComposer();

        Assert.Multiple(() =>
        {
            Assert.That(inline.Key, Is.EqualTo("menu-portal"));
            Assert.That(inline.Id, Is.EqualTo("portal-node-id"));
            Assert.That(inline.Children[0].Text, Is.EqualTo("Item"));
            Assert.Throws<UiRenderException>(() => composer.ComposePageDocument(window, page));
        });
    }

    [Test]
    public void ValidatorWalksConditionalBranchesAndRepeatItemsAtStableLocations()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("validation-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = Node(UiNodeKind.Fragment, "root", "root-id");
        root.Identity.ParentStableId = view.Id;
        UiNode conditional = Node(UiNodeKind.Conditional, "conditional", "conditional-id");
        conditional.Identity.ParentStableId = root.Identity.StableId;
        UiNode invalidBranch = Node(UiNodeKind.Text, string.Empty, "branch-id");
        invalidBranch.Identity.ParentStableId = conditional.Identity.StableId;
        conditional.Conditional = new UiConditional { WhenTrue = new UiNode[] { invalidBranch } };
        UiNode repeat = Node(UiNodeKind.Repeat, "repeat", "repeat-id");
        repeat.Identity.ParentStableId = root.Identity.StableId;
        UiNode repeatedContent = Text("content", "content-id", "Content");
        repeatedContent.Identity.ParentStableId = repeat.Identity.StableId;
        repeat.Repeat = new UiRepeat
        {
            Items = new UiCollectionItem[]
            {
                new UiCollectionItem { Key = "same", Content = repeatedContent },
                new UiCollectionItem { Key = "same" }
            }
        };
        root.Children = new UiNode[] { conditional, repeat };
        view.ContentRoot = root;
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        application.Windows = new UiWindow[] { window };
        IUiModelValidator validator = new UiModelValidator();

        UiResult result = validator.TryValidateModel(application);

        Assert.Multiple(() =>
        {
            Assert.That(Array.Exists(result.Diagnostics, diagnostic => diagnostic.Code == "model.missing-key" && diagnostic.Location!.EndsWith("/conditional/whenTrue/0/identity/authorKey", StringComparison.Ordinal)), Is.True);
            Assert.That(Array.Exists(result.Diagnostics, diagnostic => diagnostic.Code == "model.duplicate-key" && diagnostic.Location!.EndsWith("/repeat/items/1/key", StringComparison.Ordinal)), Is.True);
        });
    }

    private static UiNode Text(string key, string id, string text)
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
}
