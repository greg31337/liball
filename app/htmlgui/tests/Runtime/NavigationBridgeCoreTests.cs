using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class NavigationBridgeCoreTests
{
    [Test]
    public void CreatesOrderedHostNavigationMessagesWithHistoryAndRestorationIntents()
    {
        NavigationBridgeCore bridge = CreateBridge();
        bridge.Session = "session-1";
        bridge.Navigator.InitializeNavigationHistory("/home", Text("home"), 1);
        bridge.Navigator.CaptureNavigationRestoration(5, 12, "home-title");
        NavigationCoreResult navigation = bridge.Navigator.ExecuteNavigation(Request(UiNavigationKind.Push, "/products/7#details", Text("product"), 2));

        NavigationBridgeResult first = bridge.CreateBrowserHistoryMessage(navigation, UiNavigationKind.Push);
        NavigationBridgeResult second = bridge.CreateBrowserHistoryMessage(navigation, UiNavigationKind.Replace);
        UiValueEntry[] payload = first.Envelope!.Payload!.ObjectValue!;
        UiValue[] history = Value(payload, "history").ArrayValue!;
        payload[0].Value.StringValue = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Envelope.Kind, Is.EqualTo(UiBridgeMessageKind.Navigation));
            Assert.That(first.Envelope.Origin, Is.EqualTo(BridgeProtocolNames.HostOrigin));
            Assert.That(first.Envelope.Session, Is.EqualTo("session-1"));
            Assert.That(first.Envelope.Sequence, Is.EqualTo(1));
            Assert.That(second.Envelope!.Sequence, Is.EqualTo(2));
            Assert.That(Value(second.Envelope.Payload!.ObjectValue!, "operation").StringValue, Is.EqualTo("replace"));
            Assert.That(Value(payload, "url").StringValue, Is.EqualTo("/products/7#details"));
            Assert.That(Value(payload, "fragmentTargetId").StringValue, Is.EqualTo("details"));
            Assert.That(Value(payload, "moveFocus").BooleanValue, Is.True);
            Assert.That(history, Has.Length.EqualTo(2));
            Assert.That(Value(history[0].ObjectValue!, "scrollY").FloatingPointValue, Is.EqualTo(12));
            Assert.That(Value(history[0].ObjectValue!, "focusedNodeId").StringValue, Is.EqualTo("home-title"));
            Assert.That(Value(history[1].ObjectValue!, "state").StringValue, Is.EqualTo("product"));
            Assert.That(Value(second.Envelope.Payload.ObjectValue!, "operation").StringValue, Is.EqualTo("replace"));
        });
    }

    [Test]
    public void ResolvesAnInitialDeepLinkAndHandsRouteStateToHydration()
    {
        NavigationBridgeCore bridge = CreateBridge();
        UiHydrationManifest source = new UiHydrationManifest
        {
            Hash = "server-hash",
            State = new UiValueEntry[] { new UiValueEntry { Key = "theme", Value = Text("dark") } }
        };
        UiBridgeEnvelope initial = BrowserEnvelope(InitialPayload(bridge, "/products/42?view=full#specifications", Text("deep-link"), 8));

        NavigationBridgeResult result = bridge.ResolveInitialDeepLink(initial, source);
        initial.Payload!.ObjectValue![1].Value.StringValue = "/home";
        source.Hash = "changed";
        source.State[0].Value.StringValue = "light";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Applied, Is.True);
            Assert.That(result.Navigation!.Navigation.CanonicalUrl, Is.EqualTo("/products/42?view=full#specifications"));
            Assert.That(result.Navigation.Navigation.RouteData!.RouteId, Is.EqualTo("product"));
            Assert.That(result.Navigation.Navigation.RouteData.Parameters.Single().Value.IntegerValue, Is.EqualTo(42));
            Assert.That(result.Hydration!.Hash, Is.EqualTo("server-hash"));
            Assert.That(result.Hydration.RouteUrl, Is.EqualTo("/products/42?view=full#specifications"));
            Assert.That(result.Hydration.RouteData!.Fragment, Is.EqualTo("specifications"));
            Assert.That(result.Hydration.HistoryState!.StringValue, Is.EqualTo("deep-link"));
            Assert.That(result.Hydration.HistoryEntryId, Is.EqualTo("history-1"));
            Assert.That(result.Hydration.NavigationGeneration, Is.EqualTo(8));
            Assert.That(result.Hydration.State.Single().Value.StringValue, Is.EqualTo("dark"));
        });
    }

    [Test]
    public void ReconcilesBrowserHistoryAndRestoresTheBrowserSelectedEntry()
    {
        NavigationBridgeCore bridge = CreateBridge();
        bridge.Navigator.InitializeNavigationHistory("/home", Text("old"), 1);
        UiHistoryEntry home = History("browser-home", "/home", Text("home-state"), 2, 4, "home-title");
        UiHistoryEntry product = History("browser-product", "/products/7", Text("product-state"), 10, 50, "product-title");
        UiBridgeEnvelope reconnect = BrowserEnvelope(HistoryPayload(bridge, "reconnect", "/products/7", 5, 1, new UiHistoryEntry[] { home, product }));

        NavigationBridgeResult reconciled = bridge.ReconcileBrowserHistory(reconnect);
        reconnect.Payload!.ObjectValue![1].Value.StringValue = "/home";
        product.State!.StringValue = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(reconciled.Success, Is.True);
            Assert.That(reconciled.Applied, Is.True);
            Assert.That(reconciled.ReplacedHistory, Is.True);
            Assert.That(reconciled.Navigation!.HistoryIndex, Is.EqualTo(1));
            Assert.That(reconciled.Navigation.History.Select(item => item.Id), Is.EqualTo(new string[] { "browser-home", "browser-product" }));
            Assert.That(reconciled.Navigation.Navigation.CanonicalUrl, Is.EqualTo("/products/7"));
            Assert.That(reconciled.Navigation.Navigation.State!.StringValue, Is.EqualTo("product-state"));
            Assert.That(reconciled.Navigation.Navigation.HasScrollOffset, Is.True);
            Assert.That(reconciled.Navigation.Navigation.ScrollX, Is.EqualTo(10));
            Assert.That(reconciled.Navigation.Navigation.ScrollY, Is.EqualTo(50));
            Assert.That(reconciled.Navigation.Navigation.RestoreFocus, Is.True);
            Assert.That(reconciled.Navigation.Navigation.FocusTargetId, Is.EqualTo("product-title"));
            Assert.That(bridge.Navigator.History[1].State!.StringValue, Is.EqualTo("product-state"));
        });
    }

    [Test]
    public void TreatsMatchingReconnectsAsIdempotentAndSuppressesStaleGenerations()
    {
        NavigationBridgeCore bridge = CreateBridge();
        bridge.Navigator.InitializeNavigationHistory("/home", null, 1);
        UiHistoryEntry current = History("browser-home", "/home", null, 0, 0, null);
        UiBridgeEnvelope firstEnvelope = BrowserEnvelope(HistoryPayload(bridge, "reconnect", "/home", 4, 0, new UiHistoryEntry[] { current }));
        NavigationBridgeResult first = bridge.ReconcileBrowserHistory(firstEnvelope);
        UiBridgeEnvelope repeatedEnvelope = BrowserEnvelope(HistoryPayload(bridge, "reconnect", "/home", 4, 0, new UiHistoryEntry[] { current }));
        NavigationBridgeResult repeated = bridge.ReconcileBrowserHistory(repeatedEnvelope);
        UiBridgeEnvelope staleEnvelope = BrowserEnvelope(HistoryPayload(bridge, "reconnect", "/products/7", 3, 0, new UiHistoryEntry[] { History("stale", "/products/7", null, 0, 0, null) }));
        NavigationBridgeResult stale = bridge.ReconcileBrowserHistory(staleEnvelope);

        Assert.Multiple(() =>
        {
            Assert.That(first.Applied, Is.True);
            Assert.That(repeated.Success, Is.True);
            Assert.That(repeated.Applied, Is.False);
            Assert.That(repeated.ReplacedHistory, Is.False);
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.IsStale, Is.True);
            Assert.That(stale.Code, Is.EqualTo("navigation.bridge-stale-generation"));
            Assert.That(bridge.Navigator.History.Single().Url, Is.EqualTo("/home"));
            Assert.That(bridge.LastBrowserGeneration, Is.EqualTo(4));
        });
    }

    [Test]
    public void AcceptsPopMessagesAndRejectsInvalidBrowserHistorySnapshots()
    {
        NavigationBridgeCore bridge = CreateBridge();
        bridge.Navigator.InitializeNavigationHistory("/home", null, 1);
        UiHistoryEntry home = History("home", "/home", null, 0, 0, null);
        UiHistoryEntry product = History("product", "/products/7", null, 0, 0, null);
        NavigationBridgeResult pop = bridge.ReconcileBrowserHistory(BrowserEnvelope(HistoryPayload(bridge, "pop", "/products/7", 2, 1, new UiHistoryEntry[] { home, product })));
        NavigationBridgeResult duplicate = bridge.ReconcileBrowserHistory(BrowserEnvelope(HistoryPayload(bridge, "reconnect", "/products/7", 3, 1, new UiHistoryEntry[] { home, History("home", "/products/7", null, 0, 0, null) })));
        NavigationBridgeResult mismatch = bridge.ReconcileBrowserHistory(BrowserEnvelope(HistoryPayload(bridge, "reconnect", "/home", 3, 1, new UiHistoryEntry[] { home, product })));
        NavigationBridgeResult external = bridge.ReconcileBrowserHistory(BrowserEnvelope(HistoryPayload(bridge, "reconnect", "https://example.test", 3, 0, new UiHistoryEntry[] { History("external", "https://example.test", null, 0, 0, null) })));

        Assert.Multiple(() =>
        {
            Assert.That(pop.Success, Is.True);
            Assert.That(pop.Navigation!.Navigation.CanonicalUrl, Is.EqualTo("/products/7"));
            Assert.That(duplicate.Code, Is.EqualTo("navigation.bridge-invalid-history-entry"));
            Assert.That(mismatch.Code, Is.EqualTo("navigation.bridge-current-url-mismatch"));
            Assert.That(external.Code, Is.EqualTo("navigation.bridge-invalid-history-url"));
        });
    }

    [Test]
    public void ValidatesInitialMessagesAndOutboundSessionSequenceLimits()
    {
        NavigationBridgeCore bridge = CreateBridge();
        UiBridgeEnvelope wrongOrigin = BrowserEnvelope(InitialPayload(bridge, "/home", null, 1));
        wrongOrigin.Origin = BridgeProtocolNames.HostOrigin;
        NavigationBridgeResult invalidEnvelope = bridge.ResolveInitialDeepLink(wrongOrigin, null);
        UiBridgeEnvelope wrongOperation = BrowserEnvelope(InitialPayload(bridge, "/home", null, 1));
        Value(wrongOperation.Payload!.ObjectValue!, "operation").StringValue = "push";
        NavigationBridgeResult invalidOperation = bridge.ResolveInitialDeepLink(wrongOperation, null);
        NavigationCoreResult navigation = bridge.Navigator.InitializeNavigationHistory("/home", null, 1);
        NavigationBridgeResult missingSession = bridge.CreateBrowserHistoryMessage(navigation, UiNavigationKind.Push);
        bridge.Session = "session";
        bridge.OutboundSequence = int.MaxValue;
        NavigationBridgeResult exhausted = bridge.CreateBrowserHistoryMessage(navigation, UiNavigationKind.Push);

        Assert.Multiple(() =>
        {
            Assert.That(invalidEnvelope.Code, Is.EqualTo("navigation.bridge-invalid-envelope"));
            Assert.That(invalidOperation.Code, Is.EqualTo("navigation.bridge-invalid-operation"));
            Assert.That(missingSession.Code, Is.EqualTo("navigation.bridge-invalid-session"));
            Assert.That(exhausted.Code, Is.EqualTo("navigation.bridge-sequence-exhausted"));
        });
    }

    private static NavigationBridgeCore CreateBridge()
    {
        NavigationBridgeCore bridge = new NavigationBridgeCore();
        bridge.Navigator.Routes = new UiRoute[]
        {
            new UiRoute { Id = "home", Template = "/home" },
            new UiRoute { Id = "product", Template = "/products/{id:int}" }
        };
        return bridge;
    }

    private static UiNavigationRequest Request(UiNavigationKind kind, string url, UiValue? state, int generation)
    {
        return new UiNavigationRequest { Kind = kind, Url = url, State = state, Generation = generation };
    }

    private static UiBridgeEnvelope BrowserEnvelope(UiValue payload)
    {
        return new UiBridgeEnvelope
        {
            Version = BridgeProtocolVersions.Current,
            Session = "session-1",
            Sequence = 1,
            Origin = BridgeProtocolNames.BrowserOrigin,
            Kind = UiBridgeMessageKind.Navigation,
            Payload = payload
        };
    }

    private static UiValue InitialPayload(NavigationBridgeCore bridge, string url, UiValue? state, int generation)
    {
        return ObjectValue(new UiValueEntry[]
        {
            bridge.CreateBridgeTextEntry("operation", "initial"),
            bridge.CreateBridgeTextEntry("url", url),
            bridge.CreateBridgeIntegerEntry("generation", generation),
            bridge.CreateBridgeValueEntry("state", state)
        });
    }

    private static UiValue HistoryPayload(NavigationBridgeCore bridge, string operation, string url, int generation, int historyIndex, UiHistoryEntry[] history)
    {
        return ObjectValue(new UiValueEntry[]
        {
            bridge.CreateBridgeTextEntry("operation", operation),
            bridge.CreateBridgeTextEntry("url", url),
            bridge.CreateBridgeIntegerEntry("generation", generation),
            bridge.CreateBridgeIntegerEntry("historyIndex", historyIndex),
            bridge.CreateBridgeValueEntry("history", bridge.CreateBridgeHistoryValue(history))
        });
    }

    private static UiHistoryEntry History(string id, string url, UiValue? state, double scrollX, double scrollY, string? focus)
    {
        return new UiHistoryEntry
        {
            Id = id,
            Url = url,
            State = state,
            HasScrollOffset = focus != null || scrollX != 0 || scrollY != 0,
            ScrollX = scrollX,
            ScrollY = scrollY,
            FocusedNodeId = focus
        };
    }

    private static UiValue Value(UiValueEntry[] entries, string key)
    {
        return entries.Single(item => item.Key == key).Value;
    }

    private static UiValue ObjectValue(UiValueEntry[] entries)
    {
        return new UiValue { Kind = UiValueKind.Object, ObjectValue = entries };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
