using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class UiRouterTests
{
    [Test]
    public void RegistersAndResolvesDetachedRoutesThroughThePublicInterface()
    {
        IUiRouter router = new UiRouter();
        UiDocumentMetadata document = new UiDocumentMetadata { Title = "Application", Language = "en" };
        UiPage page = new UiPage { Key = "product-page", RouteId = "product", Title = "Product" };
        UiNavigationResult configured = router.ConfigureUiRouting(new UiPage[] { page }, document, "/app", false);
        UiRoute home = Route("home", "/home", "home-view");
        UiRoute product = Route("product", "/products/{id:int}", "product-view");
        UiNavigationResult homeRegistered = router.RegisterUiRoute(home);
        UiNavigationResult productRegistered = router.RegisterUiRoute(product);
        product.Template = "/changed";
        product.View!.Key = "changed-view";
        page.Title = "Changed";
        document.Language = "fr";

        UiNavigationResult resolved = router.ResolveUiRoute("/app/products/42?mode=full#details");
        UiNavigationResult parentRemoval = router.UnregisterUiRoute("home");
        UiNavigationResult removed = router.UnregisterUiRoute("product");
        UiNavigationResult missing = router.ResolveUiRoute("/app/products/42");

        Assert.Multiple(() =>
        {
            Assert.That(configured.Success, Is.True);
            Assert.That(homeRegistered.Success, Is.True);
            Assert.That(productRegistered.Success, Is.True);
            Assert.That(resolved.Success, Is.True);
            Assert.That(resolved.Status, Is.EqualTo(UiNavigationStatus.Resolved));
            Assert.That(resolved.CanonicalUrl, Is.EqualTo("/app/products/42?mode=full#details"));
            Assert.That(resolved.RouteData!.RouteId, Is.EqualTo("product"));
            Assert.That(resolved.RouteData.Parameters.Single().Value.IntegerValue, Is.EqualTo(42));
            Assert.That(resolved.View!.Key, Is.EqualTo("product-view"));
            Assert.That(resolved.Page!.Title, Is.EqualTo("Product"));
            Assert.That(resolved.Title, Is.EqualTo("Product"));
            Assert.That(resolved.Metadata!.Language, Is.EqualTo("en"));
            Assert.That(parentRemoval.Success, Is.True);
            Assert.That(removed.Success, Is.True);
            Assert.That(missing.Status, Is.EqualTo(UiNavigationStatus.NotFound));
        });
    }

    [Test]
    public void ReturnsDiagnosticsForInvalidConfigurationRegistrationAndResolution()
    {
        IUiRouter router = new UiRouter();
        UiRoute child = Route("child", "/child", null);
        child.ParentId = "missing";

        UiNavigationResult nullPages = router.ConfigureUiRouting(null, null, "/", false);
        UiNavigationResult invalidBase = router.ConfigureUiRouting(System.Array.Empty<UiPage>(), null, "../outside", false);
        UiNavigationResult nullRoute = router.RegisterUiRoute(null);
        UiNavigationResult unknownParent = router.RegisterUiRoute(child);
        UiNavigationResult malformedUrl = router.ResolveUiRoute("/%GG");
        UiNavigationResult unknownRemoval = router.UnregisterUiRoute("missing");

        Assert.Multiple(() =>
        {
            Assert.That(nullPages.Success, Is.False);
            Assert.That(nullPages.Diagnostics.Single().Code, Is.EqualTo("navigation.null-pages"));
            Assert.That(invalidBase.Diagnostics.Single().Code, Is.EqualTo("route.invalid-base-path"));
            Assert.That(nullRoute.Diagnostics.Single().Code, Is.EqualTo("route.null-route"));
            Assert.That(unknownParent.Diagnostics.Single().Code, Is.EqualTo("route.unknown-parent"));
            Assert.That(malformedUrl.Status, Is.EqualTo(UiNavigationStatus.Failed));
            Assert.That(malformedUrl.Diagnostics.Single().Code, Is.EqualTo("route.invalid-percent-encoding"));
            Assert.That(unknownRemoval.Diagnostics.Single().Code, Is.EqualTo("route.unknown-route"));
        });
    }

    [Test]
    public async Task ExposesNavigationHistoryStateAndRestorationThroughTheInterface()
    {
        IUiRouter router = ConfiguredRouter();
        UiValue initialState = Text("home-state");
        UiNavigationResult initial = router.InitializeUiNavigation("/app/home", initialState, 1);
        initialState.StringValue = "changed";
        UiNavigationResult captured = router.CaptureUiNavigationRestoration(4, 18, "home-heading");
        UiNavigationResult pushed = await router.NavigateUi(new UiNavigationRequest
        {
            Kind = UiNavigationKind.Push,
            Url = "/app/products/9#details",
            State = Text("product-state")
        });
        UiNavigationResult back = await router.NavigateUi(new UiNavigationRequest { Kind = UiNavigationKind.Back, MoveFocus = true });

        Assert.Multiple(() =>
        {
            Assert.That(initial.Success, Is.True);
            Assert.That(initial.History.Single().State!.StringValue, Is.EqualTo("home-state"));
            Assert.That(captured.Success, Is.True);
            Assert.That(captured.History.Single().ScrollY, Is.EqualTo(18));
            Assert.That(pushed.Success, Is.True);
            Assert.That(pushed.Applied, Is.True);
            Assert.That(pushed.Generation, Is.EqualTo(1));
            Assert.That(pushed.CanonicalUrl, Is.EqualTo("/app/products/9#details"));
            Assert.That(pushed.RouteData!.Parameters.Single().Value.IntegerValue, Is.EqualTo(9));
            Assert.That(pushed.FragmentTargetId, Is.EqualTo("details"));
            Assert.That(pushed.History, Has.Length.EqualTo(2));
            Assert.That(back.Success, Is.True);
            Assert.That(back.Generation, Is.EqualTo(2));
            Assert.That(back.CanonicalUrl, Is.EqualTo("/app/home"));
            Assert.That(back.State!.StringValue, Is.EqualTo("home-state"));
            Assert.That(back.HasScrollOffset, Is.True);
            Assert.That(back.ScrollY, Is.EqualTo(18));
            Assert.That(back.RestoreFocus, Is.True);
            Assert.That(back.FocusTargetId, Is.EqualTo("home-heading"));
        });
    }

    [Test]
    public async Task AwaitsGuardsAndDeliversCopiedBeforePendingAndAfterNotifications()
    {
        IUiRouter router = ConfiguredRouter();
        router.InitializeUiNavigation("/app/home", null, 0);
        RecordingNavigationObserver observer = new RecordingNavigationObserver();
        DelayedNavigationGuard guard = new DelayedNavigationGuard();
        UiNavigationResult observerRegistered = router.RegisterUiNavigationObserver("observer", observer);
        UiNavigationResult guardRegistered = router.RegisterUiNavigationGuard("guard", guard);
        UiNavigationRequest request = new UiNavigationRequest { Kind = UiNavigationKind.Push, Url = "/app/products/5", State = Text("original") };

        Task<UiNavigationResult> navigationTask = router.NavigateUi(request);
        await guard.Started.Task;
        bool completedBeforeGuard = navigationTask.IsCompleted;
        request.Url = "/app/home";
        request.State!.StringValue = "changed";
        guard.Completion.SetResult(new UiNavigationObserverResult { AllowNavigation = true });
        UiNavigationResult result = await navigationTask;

        Assert.Multiple(() =>
        {
            Assert.That(observerRegistered.Success, Is.True);
            Assert.That(guardRegistered.Success, Is.True);
            Assert.That(completedBeforeGuard, Is.False);
            Assert.That(guard.Notification!.Stage, Is.EqualTo("before"));
            Assert.That(guard.Notification.Request.Url, Is.EqualTo("/app/products/5"));
            Assert.That(guard.Notification.Request.State!.StringValue, Is.EqualTo("original"));
            Assert.That(observer.Notifications.Select(item => item.Stage), Is.EqualTo(new string[] { "before", "pending", "after" }));
            Assert.That(observer.Notifications.All(item => item.Generation == 1), Is.True);
            Assert.That(observer.Notifications.Last().Navigation.CanonicalUrl, Is.EqualTo("/app/products/5"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.CanonicalUrl, Is.EqualTo("/app/products/5"));
        });
    }

    [Test]
    public async Task RoutesGuardDenialAndCallbackFailureToCancelAndErrorObservers()
    {
        IUiRouter deniedRouter = ConfiguredRouter();
        deniedRouter.InitializeUiNavigation("/app/home", null, 0);
        RecordingNavigationObserver deniedObserver = new RecordingNavigationObserver();
        deniedRouter.RegisterUiNavigationObserver("observer", deniedObserver);
        deniedRouter.RegisterUiNavigationGuard("deny", new ImmediateNavigationHandler(false, true));
        UiNavigationResult denied = await deniedRouter.NavigateUi(new UiNavigationRequest { Kind = UiNavigationKind.Push, Url = "/app/products/2" });

        IUiRouter failedRouter = ConfiguredRouter();
        failedRouter.InitializeUiNavigation("/app/home", null, 0);
        RecordingNavigationObserver failedObserver = new RecordingNavigationObserver();
        failedRouter.RegisterUiNavigationObserver("observer", failedObserver);
        failedRouter.RegisterUiNavigationGuard("failure", new FaultingNavigationHandler());
        UiNavigationResult failed = await failedRouter.NavigateUi(new UiNavigationRequest { Kind = UiNavigationKind.Push, Url = "/app/products/3" });

        Assert.Multiple(() =>
        {
            Assert.That(denied.Success, Is.True);
            Assert.That(denied.Status, Is.EqualTo(UiNavigationStatus.Cancelled));
            Assert.That(denied.History, Has.Length.EqualTo(1));
            Assert.That(deniedObserver.Notifications.Select(item => item.Stage), Is.EqualTo(new string[] { "before", "pending", "cancel" }));
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Status, Is.EqualTo(UiNavigationStatus.Failed));
            Assert.That(failed.Diagnostics.Select(item => item.Code), Does.Contain("navigation.guard-callback-failed"));
            Assert.That(failedObserver.Notifications.Select(item => item.Stage), Is.EqualTo(new string[] { "before", "pending", "error" }));
        });
    }

    [Test]
    public async Task ValidatesCallbackRegistrationAndReportsObserverFailuresExceptionlessly()
    {
        IUiRouter router = ConfiguredRouter();
        router.InitializeUiNavigation("/app/home", null, 0);
        UiNavigationResult nullGuard = router.RegisterUiNavigationGuard("guard", null);
        UiNavigationResult registered = router.RegisterUiNavigationObserver("observer", new FaultingNavigationHandler());
        UiNavigationResult duplicate = router.RegisterUiNavigationObserver("observer", new RecordingNavigationObserver());
        UiNavigationResult unknown = router.UnregisterUiNavigationGuard("missing");
        UiNavigationResult navigation = await router.NavigateUi(new UiNavigationRequest { Kind = UiNavigationKind.Push, Url = "/app/products/4" });
        UiNavigationResult removed = router.UnregisterUiNavigationObserver("observer");

        Assert.Multiple(() =>
        {
            Assert.That(nullGuard.Diagnostics.Single().Code, Is.EqualTo("navigation.null-guard"));
            Assert.That(registered.Success, Is.True);
            Assert.That(duplicate.Diagnostics.Single().Code, Is.EqualTo("navigation.duplicate-observer"));
            Assert.That(unknown.Diagnostics.Single().Code, Is.EqualTo("navigation.unknown-guard"));
            Assert.That(navigation.Applied, Is.True);
            Assert.That(navigation.Status, Is.EqualTo(UiNavigationStatus.Resolved));
            Assert.That(navigation.Success, Is.False);
            Assert.That(navigation.Diagnostics.Select(item => item.Code), Does.Contain("navigation.observer-failed"));
            Assert.That(removed.Success, Is.True);
        });
    }

    private static IUiRouter ConfiguredRouter()
    {
        IUiRouter router = new UiRouter();
        router.ConfigureUiRouting(System.Array.Empty<UiPage>(), new UiDocumentMetadata { Title = "Application" }, "/app", false);
        router.RegisterUiRoute(Route("home", "/home", "home-view"));
        router.RegisterUiRoute(Route("product", "/products/{id:int}", "product-view"));
        return router;
    }

    private static UiRoute Route(string id, string template, string? viewKey)
    {
        return new UiRoute
        {
            Id = id,
            Template = template,
            View = viewKey == null ? null : new UiView { Key = viewKey, ContentRoot = new UiNode { Kind = UiNodeKind.Text, Text = viewKey } }
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}

public class RecordingNavigationObserver : IUiNavigationObserver
{
    public List<UiNavigationNotification> Notifications = new List<UiNavigationNotification>();

    public Task<UiNavigationObserverResult> HandleUiNavigation(UiNavigationNotification notification)
    {
        Notifications.Add(notification);
        return Task.FromResult(new UiNavigationObserverResult());
    }
}

public class DelayedNavigationGuard : IUiNavigationObserver
{
    public TaskCompletionSource<bool> Started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<UiNavigationObserverResult> Completion = new TaskCompletionSource<UiNavigationObserverResult>(TaskCreationOptions.RunContinuationsAsynchronously);
    public UiNavigationNotification? Notification;

    public Task<UiNavigationObserverResult> HandleUiNavigation(UiNavigationNotification notification)
    {
        Notification = notification;
        Started.SetResult(true);
        return Completion.Task;
    }
}

public class ImmediateNavigationHandler : IUiNavigationObserver
{
    public bool Allow;
    public bool Success;

    public ImmediateNavigationHandler(bool allow, bool success)
    {
        Allow = allow;
        Success = success;
    }

    public Task<UiNavigationObserverResult> HandleUiNavigation(UiNavigationNotification notification)
    {
        return Task.FromResult(new UiNavigationObserverResult { AllowNavigation = Allow, Success = Success });
    }
}

public class FaultingNavigationHandler : IUiNavigationObserver
{
    public Task<UiNavigationObserverResult> HandleUiNavigation(UiNavigationNotification notification)
    {
        return Task.FromException<UiNavigationObserverResult>(new System.InvalidOperationException("callback failure"));
    }
}
