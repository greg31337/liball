using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiHydratorTests
{
    [Test]
    public void CreatesCanonicalDetachedManifestThroughPublicContract()
    {
        UiApplication application = Application();
        DomSnapshot snapshot = Snapshot(application, "Server");
        snapshot.ContentHash = "caller-supplied-stale-hash";
        IUiHydrator hydrator = new UiHydrator();

        UiHydrationManifest manifest = hydrator.CreateHydrationManifest(application, snapshot);
        application.State[0].Value.StringValue = "mutated";
        snapshot.Roots[0].StyleScope = "mutated";
        snapshot.Roots[0].EventSubscriptions[0].HandlerId = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(manifest.Version, Is.EqualTo(BridgeProtocolVersions.Current));
            Assert.That(manifest.Hash, Has.Length.EqualTo(64));
            Assert.That(manifest.Hash, Is.Not.EqualTo("caller-supplied-stale-hash"));
            Assert.That(manifest.Nodes, Has.Length.EqualTo(2));
            Assert.That(manifest.State.Single().Value.StringValue, Is.EqualTo("server-state"));
            Assert.That(manifest.Events.Single().Key, Is.EqualTo("click-event"));
            Assert.That(ObjectField(manifest.Events[0].Value, "handlerId").StringValue, Is.EqualTo("click-handler"));
            Assert.That(manifest.Styles.Single().Value.StringValue, Is.EqualTo("page-scope"));
        });
    }

    [Test]
    public void ManifestUsesTheActiveWindowAndSelectedPageMarkers()
    {
        UiApplication application = Application();
        IUiFactory factory = new UiFactory();
        UiWindow window = application.Windows[0];
        UiPage selected = factory.CreatePage(application.RootSeed, window.Id, "selected");
        UiView selectedView = factory.CreateView(application.RootSeed, selected.Id, "selected-view");
        UiNode selectedRoot = factory.CreateElementNode(application.RootSeed, selectedView.Id, "selected-root", null, HtmlElements.Section);
        selectedRoot.Bindings = new UiBinding[]
        {
            new UiBinding { Id = "selected-binding", StateId = "state", Path = string.Empty, TargetNodeId = selectedRoot.Identity.StableId, Target = "text" }
        };
        selectedView.ContentRoot = selectedRoot;
        selected.ContentView = selectedView;
        window.Pages = new UiPage[] { window.Pages[0], selected };
        window.CurrentPageKey = selected.Key;

        UiHydrationManifest manifest = new UiHydrator().CreateHydrationManifest(application, Snapshot(application, "Server"));

        Assert.Multiple(() =>
        {
            Assert.That(manifest.Bindings.Single().Key, Is.EqualTo("selected-binding"));
            Assert.That(ObjectField(manifest.Bindings[0].Value, "targetNodeId").StringValue, Is.EqualTo(selectedRoot.Identity.StableId));
        });
    }

    [Test]
    public void PlansExactMatchAndRepairThroughPublicContract()
    {
        UiApplication application = Application();
        DomSnapshot expected = Snapshot(application, "Server");
        DomSnapshot same = Snapshot(application, "Server");
        DomSnapshot changed = Snapshot(application, "Client");
        IUiHydrator hydrator = new UiHydrator();
        UiHydrationManifest manifest = hydrator.CreateHydrationManifest(application, expected);

        UiHydrationResult matched = hydrator.PlanHydration(expected, same, manifest, UiHydrationMode.Exact);
        UiHydrationResult repaired = hydrator.PlanHydration(expected, changed, manifest, UiHydrationMode.Repair);

        Assert.Multiple(() =>
        {
            Assert.That(matched.Status, Is.EqualTo(UiHydrationStatus.Matched));
            Assert.That(matched.ParityMatched, Is.True);
            Assert.That(matched.State.Single().Value.StringValue, Is.EqualTo("server-state"));
            Assert.That(matched.EventAttachments.Single().Key, Is.EqualTo("click-event"));
            Assert.That(repaired.Status, Is.EqualTo(UiHydrationStatus.Repaired));
            Assert.That(repaired.ParityMatched, Is.False);
            Assert.That(repaired.Mutations.Single().Kind, Is.EqualTo(UiMutationKind.SetText));
            Assert.That(repaired.Mutations.Single().Value, Is.EqualTo("Server"));
        });
    }

    [Test]
    public void PlansExactMismatchAndRootReplacementThroughPublicContract()
    {
        UiApplication application = Application();
        DomSnapshot expected = Snapshot(application, "Server");
        DomSnapshot changed = Snapshot(application, "Client");
        changed.Roots[0].Name = HtmlElements.Section;
        IUiHydrator hydrator = new UiHydrator();
        UiHydrationManifest manifest = hydrator.CreateHydrationManifest(application, expected);

        UiHydrationResult exact = hydrator.PlanHydration(expected, changed, manifest, UiHydrationMode.Exact);
        UiHydrationResult replaced = hydrator.PlanHydration(expected, changed, manifest, UiHydrationMode.Replace);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(UiHydrationStatus.Mismatch));
            Assert.That(exact.Mutations, Is.Empty);
            Assert.That(exact.Diagnostics.Select(item => item.Code), Does.Contain("hydration.tag"));
            Assert.That(replaced.Status, Is.EqualTo(UiHydrationStatus.Replaced));
            Assert.That(replaced.Mutations.Single().Kind, Is.EqualTo(UiMutationKind.Replace));
            Assert.That(replaced.Mutations.Single().Node!.Name, Is.EqualTo(HtmlElements.Main));
        });
    }

    [Test]
    public void PublicOperationsRejectMalformedInputsAndApplications()
    {
        IUiHydrator hydrator = new UiHydrator();
        UiApplication application = Application();
        DomSnapshot snapshot = Snapshot(application, "Server");
        UiApplication noWindow = new UiFactory().CreateApplication("seed", "empty");
        DomSnapshot wrongSeed = Snapshot(application, "Server");
        wrongSeed.RootSeed = "wrong";
        DomNode duplicateA = new DomNode { Kind = DomNodeKind.Text, Key = "same", Id = "a", Text = "a" };
        DomNode duplicateB = new DomNode { Kind = DomNodeKind.Text, Key = "same", Id = "b", Text = "b" };
        DomSnapshot invalidTree = new DomSnapshot
        {
            SessionId = "session",
            RootSeed = application.RootSeed,
            Roots = new DomNode[] { duplicateA, duplicateB }
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => hydrator.CreateHydrationManifest(null, snapshot), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("application"));
            Assert.That(() => hydrator.CreateHydrationManifest(application, null), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("snapshot"));
            Assert.That(() => hydrator.CreateHydrationManifest(noWindow, snapshot), Throws.TypeOf<UiHydrationException>());
            Assert.That(() => hydrator.CreateHydrationManifest(application, wrongSeed), Throws.ArgumentException.With.Property("ParamName").EqualTo("snapshot"));
            Assert.That(() => hydrator.CreateHydrationManifest(application, invalidTree), Throws.TypeOf<UiHydrationException>());
            Assert.That(() => hydrator.PlanHydration(null, snapshot, new UiHydrationManifest { Hash = "hash" }, UiHydrationMode.Exact), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("expected"));
        });
    }

    private static UiApplication Application()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("hydrator-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        root.Children = new UiNode[] { factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "text", "Model") };
        view.ContentRoot = root;
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        application.Windows = new UiWindow[] { window };
        application.State = new UiValueEntry[]
        {
            new UiValueEntry { Key = "state", Value = new UiValue { Kind = UiValueKind.String, StringValue = "server-state" } }
        };
        return application;
    }

    private static DomSnapshot Snapshot(UiApplication application, string text)
    {
        DomNode child = new DomNode { Kind = DomNodeKind.Text, Key = "text", Id = "text", Text = text };
        DomNode root = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "root",
            Id = "root",
            Name = HtmlElements.Main,
            StyleScope = "page-scope",
            EventSubscriptions = new DomEventSubscription[]
            {
                new DomEventSubscription { Id = "click-event", HandlerId = "click-handler", Kind = UiEventKind.Click, EventName = DomEventNames.Click }
            },
            Children = new DomNode[] { child }
        };
        return new UiUpdateEngine().CreateDomSnapshot(new DomNode[] { root }, "session", application.RootSeed, 0);
    }

    private static UiValue ObjectField(UiValue value, string key)
    {
        return value.ObjectValue!.Single(entry => entry.Key == key).Value;
    }
}
