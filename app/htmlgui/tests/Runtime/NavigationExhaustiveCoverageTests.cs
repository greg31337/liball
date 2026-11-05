using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class NavigationExhaustiveCoverageTests
{
    [TestCase(UiNavigationKind.Push, "/products", "/products", UiNavigationStatus.Resolved, 2, 1)]
    [TestCase(UiNavigationKind.Replace, "/products", "/products", UiNavigationStatus.Resolved, 1, 0)]
    [TestCase(UiNavigationKind.Back, "", "/home", UiNavigationStatus.Resolved, 2, 0)]
    [TestCase(UiNavigationKind.Forward, "", "/products", UiNavigationStatus.Resolved, 2, 1)]
    [TestCase(UiNavigationKind.Reload, "", "/home", UiNavigationStatus.Resolved, 1, 0)]
    [TestCase(UiNavigationKind.Link, "/products", "/products", UiNavigationStatus.Resolved, 2, 1)]
    [TestCase(UiNavigationKind.Fragment, "#details", "/home#details", UiNavigationStatus.Resolved, 2, 1)]
    [TestCase(UiNavigationKind.External, "https://example.test/path", "https://example.test/path", UiNavigationStatus.External, 1, 0)]
    public void ExecutesEveryNavigationKind(UiNavigationKind kind, string url, string expectedUrl, UiNavigationStatus expectedStatus, int expectedHistoryCount, int expectedHistoryIndex)
    {
        NavigationCore core = CreateCore();
        core.AllowExternalNavigation = true;
        core.InitializeNavigationHistory("/home", Text("home"), 0);
        if (kind == UiNavigationKind.Back || kind == UiNavigationKind.Forward)
        {
            core.ExecuteNavigation(Request(UiNavigationKind.Push, "/products", 1));
        }

        if (kind == UiNavigationKind.Forward)
        {
            core.ExecuteNavigation(Request(UiNavigationKind.Back, string.Empty, 2));
        }

        NavigationCoreResult result = core.ExecuteNavigation(Request(kind, url, 9));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Navigation.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.Navigation.CanonicalUrl, Is.EqualTo(expectedUrl));
            Assert.That(result.Navigation.Generation, Is.EqualTo(9));
            Assert.That(result.History, Has.Length.EqualTo(expectedHistoryCount));
            Assert.That(result.HistoryIndex, Is.EqualTo(expectedHistoryIndex));
        });
    }

    [TestCase("button")]
    [TestCase("modifier")]
    [TestCase("download")]
    [TestCase("blank")]
    [TestCase("parent")]
    public void LeavesNativeLinkBehaviorUninterceptedForEveryBypassCondition(string condition)
    {
        NavigationCore core = CreateCore();
        core.InitializeNavigationHistory("/home", null, 0);
        NavigationLinkRequest link = new NavigationLinkRequest { Url = "/products", Generation = 1 };
        if (condition == "button")
        {
            link.Button = 1;
        }
        else if (condition == "modifier")
        {
            link.HasModifier = true;
        }
        else if (condition == "download")
        {
            link.HasDownload = true;
        }
        else if (condition == "blank")
        {
            link.Target = "_blank";
        }
        else
        {
            link.Target = "_parent";
        }

        NavigationCoreResult result = core.ExecuteLinkNavigation(link);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Intercepted, Is.False);
            Assert.That(result.Code, Is.EqualTo("navigation.link-not-intercepted"));
            Assert.That(result.Navigation.Status, Is.EqualTo(UiNavigationStatus.Cancelled));
            Assert.That(result.History.Single().Url, Is.EqualTo("/home"));
        });
    }

    [TestCase("http://example.test/path", "http://example.test/path")]
    [TestCase("https://EXAMPLE.TEST:443/path", "https://example.test/path")]
    [TestCase("mailto:person@example.test", "mailto:person@example.test")]
    [TestCase("tel:+15551212", "tel:+15551212")]
    [TestCase("//EXAMPLE.TEST:443/path", "//example.test/path")]
    public void HandsEverySafeExternalSchemeToTheHostWhenPolicyAllowsIt(string url, string expected)
    {
        NavigationCore core = CreateCore();
        core.AllowExternalNavigation = true;
        core.InitializeNavigationHistory("/home", null, 0);

        NavigationCoreResult result = core.ExecuteNavigation(Request(UiNavigationKind.Push, url, 1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Navigation.Status, Is.EqualTo(UiNavigationStatus.External));
            Assert.That(result.Navigation.ExternalUrl, Is.EqualTo(expected));
            Assert.That(result.History.Single().Url, Is.EqualTo("/home"));
            Assert.That(result.HistoryChanged, Is.False);
        });
    }

    [Test]
    public void DetectsSelfAndMultiRouteRedirectLoopsWithoutWritingHistory()
    {
        NavigationCore selfCore = new NavigationCore();
        UiRoute self = Route("self", "/self");
        self.RedirectUrl = "/self";
        selfCore.Routes = new UiRoute[] { self };
        NavigationCoreResult selfResult = selfCore.ExecuteNavigation(Request(UiNavigationKind.Push, "/self", 1));

        NavigationCore cycleCore = new NavigationCore();
        UiRoute first = Route("first", "/first");
        first.RedirectUrl = "/second";
        UiRoute second = Route("second", "/second");
        second.RedirectUrl = "/first";
        cycleCore.Routes = new UiRoute[] { first, second };
        NavigationCoreResult cycleResult = cycleCore.ExecuteNavigation(Request(UiNavigationKind.Push, "/first", 2));

        Assert.Multiple(() =>
        {
            Assert.That(selfResult.Success, Is.False);
            Assert.That(selfResult.Code, Is.EqualTo("navigation.redirect-loop"));
            Assert.That(selfResult.History, Is.Empty);
            Assert.That(cycleResult.Success, Is.False);
            Assert.That(cycleResult.Code, Is.EqualTo("navigation.redirect-loop"));
            Assert.That(cycleResult.History, Is.Empty);
        });
    }

    [Test]
    public void EnforcesTheRedirectDepthBoundary()
    {
        NavigationCore allowedCore = new NavigationCore();
        allowedCore.Routes = RedirectChain(NavigationCore.MaximumRedirectCount);
        NavigationCoreResult allowed = allowedCore.ExecuteNavigation(Request(UiNavigationKind.Push, "/redirect-0", 1));
        NavigationCore excessiveCore = new NavigationCore();
        excessiveCore.Routes = RedirectChain(NavigationCore.MaximumRedirectCount + 1);
        NavigationCoreResult excessive = excessiveCore.ExecuteNavigation(Request(UiNavigationKind.Push, "/redirect-0", 2));

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Success, Is.True);
            Assert.That(allowed.Navigation.CanonicalUrl, Is.EqualTo("/redirect-16"));
            Assert.That(allowed.History.Single().Url, Is.EqualTo("/redirect-16"));
            Assert.That(excessive.Success, Is.False);
            Assert.That(excessive.Code, Is.EqualTo("navigation.too-many-redirects"));
            Assert.That(excessive.History, Is.Empty);
        });
    }

    [Test]
    public void ChoosesFragmentTopScrollAndFocusIntentsWhenNoSavedRestorationExists()
    {
        NavigationCore core = CreateCore();
        core.InitializeNavigationHistory("/home#section", null, 0);
        NavigationCoreResult pushed = core.ExecuteNavigation(Request(UiNavigationKind.Push, "/products", 1));
        NavigationCoreResult fragmentBack = core.ExecuteNavigation(Request(UiNavigationKind.Back, string.Empty, 2));
        NavigationCoreResult topForward = core.ExecuteNavigation(Request(UiNavigationKind.Forward, string.Empty, 3));
        UiNavigationRequest noFocusRequest = Request(UiNavigationKind.Back, string.Empty, 4);
        noFocusRequest.MoveFocus = false;
        NavigationCoreResult noFocus = core.ExecuteNavigation(noFocusRequest);

        Assert.Multiple(() =>
        {
            Assert.That(pushed.Navigation.ScrollToTop, Is.True);
            Assert.That(fragmentBack.Navigation.FragmentTargetId, Is.EqualTo("section"));
            Assert.That(fragmentBack.Navigation.HasScrollOffset, Is.False);
            Assert.That(fragmentBack.Navigation.ScrollToTop, Is.False);
            Assert.That(fragmentBack.Navigation.MoveFocus, Is.True);
            Assert.That(fragmentBack.Navigation.RestoreFocus, Is.False);
            Assert.That(fragmentBack.Navigation.FocusTargetId, Is.EqualTo("section"));
            Assert.That(topForward.Navigation.ScrollToTop, Is.True);
            Assert.That(topForward.Navigation.FocusTargetId, Is.Null);
            Assert.That(noFocus.Navigation.MoveFocus, Is.False);
            Assert.That(noFocus.Navigation.FocusTargetId, Is.Null);
        });
    }

    [Test]
    public void RejectsInvalidRequestsAndHistoryTransitionsWithoutMutation()
    {
        NavigationCore empty = CreateCore();
        NavigationCoreResult nullRequest = empty.ExecuteNavigation(null);
        NavigationCoreResult reloadEmpty = empty.ExecuteNavigation(Request(UiNavigationKind.Reload, string.Empty, 1));
        NavigationCoreResult backEmpty = empty.ExecuteNavigation(Request(UiNavigationKind.Back, string.Empty, 1));
        NavigationCoreResult invalidGeneration = empty.ExecuteNavigation(Request(UiNavigationKind.Push, "/home", -1));
        NavigationCoreResult invalidKind = empty.ExecuteNavigation(Request((UiNavigationKind)99, "/home", 1));
        NavigationCoreResult emptyUrl = empty.ExecuteNavigation(Request(UiNavigationKind.Push, string.Empty, 1));
        NavigationCore initialized = CreateCore();
        initialized.InitializeNavigationHistory("/home", null, 0);
        NavigationCoreResult initializedTwice = initialized.InitializeNavigationHistory("/products", null, 1);
        initialized.CaptureNavigationRestoration(double.NaN, 4, "ignored");

        Assert.Multiple(() =>
        {
            Assert.That(nullRequest.Code, Is.EqualTo("navigation.null-request"));
            Assert.That(reloadEmpty.Code, Is.EqualTo("navigation.history-empty"));
            Assert.That(backEmpty.Navigation.Status, Is.EqualTo(UiNavigationStatus.Cancelled));
            Assert.That(backEmpty.Code, Is.EqualTo("navigation.history-boundary"));
            Assert.That(invalidGeneration.Code, Is.EqualTo("navigation.invalid-generation"));
            Assert.That(invalidKind.Code, Is.EqualTo("navigation.invalid-kind"));
            Assert.That(emptyUrl.Code, Is.EqualTo("navigation.invalid-url"));
            Assert.That(initializedTwice.Code, Is.EqualTo("navigation.already-initialized"));
            Assert.That(initialized.History.Single().HasScrollOffset, Is.False);
        });
    }

    [Test]
    public async Task CancelsAPublicNavigationWhileItsGuardIsPendingAndSuppressesLateCompletion()
    {
        IUiRouter router = CreatePublicRouter();
        router.InitializeUiNavigation("/home", null, 0);
        ExhaustiveRecordingNavigationObserver observer = new ExhaustiveRecordingNavigationObserver();
        ExhaustiveDelayedNavigationGuard guard = new ExhaustiveDelayedNavigationGuard();
        router.RegisterUiNavigationObserver("observer", observer);
        router.RegisterUiNavigationGuard("guard", guard);

        Task<UiNavigationResult> pendingTask = router.NavigateUi(new UiNavigationRequest { Kind = UiNavigationKind.Push, Url = "/products" });
        await guard.Started.Task;
        UiNavigationResult cancelled = await router.CancelUiNavigation(1);
        guard.Completion.SetResult(new UiNavigationObserverResult());
        UiNavigationResult stale = await pendingTask;

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Success, Is.True);
            Assert.That(cancelled.Status, Is.EqualTo(UiNavigationStatus.Cancelled));
            Assert.That(cancelled.Applied, Is.True);
            Assert.That(cancelled.History.Single().Url, Is.EqualTo("/home"));
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.IsStale, Is.True);
            Assert.That(stale.Applied, Is.False);
            Assert.That(observer.Notifications.Select(item => item.Stage), Is.EqualTo(new string[] { "before", "pending", "cancel" }));
        });
    }

    [Test]
    public void RejectsInvalidAndStaleHydrationDeepLinksWithoutCreatingHistory()
    {
        NavigationBridgeCore invalidBridge = CreateNavigationBridge();
        NavigationBridgeResult invalid = invalidBridge.ResolveInitialDeepLink(BrowserInitialEnvelope(invalidBridge, "/%GG", 6), new UiHydrationManifest());
        NavigationBridgeCore staleBridge = CreateNavigationBridge();
        staleBridge.LastBrowserGeneration = 7;
        NavigationBridgeResult stale = staleBridge.ResolveInitialDeepLink(BrowserInitialEnvelope(staleBridge, "/home", 6), new UiHydrationManifest());
        NavigationBridgeCore outsideBridge = CreateNavigationBridge();
        outsideBridge.Navigator.BasePath = "/app/";
        NavigationBridgeResult outside = outsideBridge.ResolveInitialDeepLink(BrowserInitialEnvelope(outsideBridge, "/other/home", 8), null);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Code, Is.EqualTo("route.invalid-percent-encoding"));
            Assert.That(invalid.Hydration, Is.Null);
            Assert.That(invalidBridge.Navigator.History, Is.Empty);
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.IsStale, Is.True);
            Assert.That(stale.Code, Is.EqualTo("navigation.bridge-stale-generation"));
            Assert.That(staleBridge.Navigator.History, Is.Empty);
            Assert.That(outside.Code, Is.EqualTo("route.outside-base-path"));
            Assert.That(outsideBridge.Navigator.History, Is.Empty);
        });
    }

    private static NavigationCore CreateCore()
    {
        NavigationCore core = new NavigationCore();
        core.Routes = new UiRoute[] { Route("home", "/home"), Route("products", "/products") };
        return core;
    }

    private static IUiRouter CreatePublicRouter()
    {
        IUiRouter router = new UiRouter();
        router.ConfigureUiRouting(System.Array.Empty<UiPage>(), null, "/", false);
        router.RegisterUiRoute(Route("home", "/home"));
        router.RegisterUiRoute(Route("products", "/products"));
        return router;
    }

    private static NavigationBridgeCore CreateNavigationBridge()
    {
        NavigationBridgeCore bridge = new NavigationBridgeCore();
        bridge.Navigator.Routes = new UiRoute[] { Route("home", "/home") };
        return bridge;
    }

    private static UiRoute Route(string id, string template)
    {
        return new UiRoute { Id = id, Template = template };
    }

    private static UiRoute[] RedirectChain(int redirectCount)
    {
        UiRoute[] routes = new UiRoute[redirectCount + 1];
        int index = 0;
        while (index < routes.Length)
        {
            UiRoute route = Route("redirect-" + index, "/redirect-" + index);
            if (index < redirectCount)
            {
                route.RedirectUrl = "/redirect-" + (index + 1);
            }

            routes[index] = route;
            index++;
        }

        return routes;
    }

    private static UiNavigationRequest Request(UiNavigationKind kind, string url, int generation)
    {
        return new UiNavigationRequest { Kind = kind, Url = url, Generation = generation, MoveFocus = true };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiBridgeEnvelope BrowserInitialEnvelope(NavigationBridgeCore bridge, string url, int generation)
    {
        return new UiBridgeEnvelope
        {
            Version = BridgeProtocolVersions.Current,
            Session = "browser-session",
            Sequence = 1,
            Origin = BridgeProtocolNames.BrowserOrigin,
            Kind = UiBridgeMessageKind.Navigation,
            Payload = new UiValue
            {
                Kind = UiValueKind.Object,
                ObjectValue = new UiValueEntry[]
                {
                    bridge.CreateBridgeTextEntry("operation", "initial"),
                    bridge.CreateBridgeTextEntry("url", url),
                    bridge.CreateBridgeIntegerEntry("generation", generation),
                    bridge.CreateBridgeValueEntry("state", null)
                }
            }
        };
    }
}

public class ExhaustiveRecordingNavigationObserver : IUiNavigationObserver
{
    public List<UiNavigationNotification> Notifications = new List<UiNavigationNotification>();

    public Task<UiNavigationObserverResult> HandleUiNavigation(UiNavigationNotification notification)
    {
        Notifications.Add(notification);
        return Task.FromResult(new UiNavigationObserverResult());
    }
}

public class ExhaustiveDelayedNavigationGuard : IUiNavigationObserver
{
    public TaskCompletionSource<bool> Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<UiNavigationObserverResult> Completion = new TaskCompletionSource<UiNavigationObserverResult>(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<UiNavigationObserverResult> HandleUiNavigation(UiNavigationNotification notification)
    {
        Started.SetResult(true);
        return Completion.Task;
    }
}
