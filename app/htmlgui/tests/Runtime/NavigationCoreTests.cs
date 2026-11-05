using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class NavigationCoreTests
{
    [Test]
    public void PushesReplacesAndTruncatesHistoryWithDetachedPortableState()
    {
        NavigationCore core = CreateCore();
        UiValue initialState = Text("initial");
        NavigationCoreResult initial = core.InitializeNavigationHistory("/home", initialState, 1);
        initialState.StringValue = "changed";

        UiNavigationRequest push = Request(UiNavigationKind.Push, "/products", Text("products"), 2, true);
        NavigationCoreResult pushed = core.ExecuteNavigation(push);
        push.State!.StringValue = "changed";
        string pushedId = pushed.History[1].Id;

        UiNavigationRequest replace = Request(UiNavigationKind.Replace, "/products?view=grid", Text("grid"), 3, false);
        NavigationCoreResult replaced = core.ExecuteNavigation(replace);
        NavigationCoreResult backed = core.ExecuteNavigation(Request(UiNavigationKind.Back, string.Empty, null, 4, true));
        NavigationCoreResult branched = core.ExecuteNavigation(Request(UiNavigationKind.Push, "/products#details", Text("branch"), 5, true));

        Assert.Multiple(() =>
        {
            Assert.That(initial.Success, Is.True);
            Assert.That(initial.History.Single().State!.StringValue, Is.EqualTo("initial"));
            Assert.That(pushed.History, Has.Length.EqualTo(2));
            Assert.That(pushed.History[1].State!.StringValue, Is.EqualTo("products"));
            Assert.That(pushed.Navigation.ScrollToTop, Is.True);
            Assert.That(pushed.Navigation.MoveFocus, Is.True);
            Assert.That(replaced.History, Has.Length.EqualTo(2));
            Assert.That(replaced.History[1].Id, Is.EqualTo(pushedId));
            Assert.That(replaced.History[1].Url, Is.EqualTo("/products?view=grid"));
            Assert.That(replaced.History[1].State!.StringValue, Is.EqualTo("grid"));
            Assert.That(replaced.ReplaceHistory, Is.True);
            Assert.That(replaced.Navigation.MoveFocus, Is.False);
            Assert.That(backed.HistoryIndex, Is.EqualTo(0));
            Assert.That(branched.History, Has.Length.EqualTo(2));
            Assert.That(branched.History[1].Url, Is.EqualTo("/products#details"));
            Assert.That(branched.HistoryIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void RestoresSavedScrollFocusAndStateWhenMovingThroughHistory()
    {
        NavigationCore core = CreateCore();
        core.InitializeNavigationHistory("/home", Text("home-state"), 1);
        core.CaptureNavigationRestoration(4.5, 8.25, "home-title");
        core.ExecuteNavigation(Request(UiNavigationKind.Push, "/products", Text("products-state"), 2, true));
        core.CaptureNavigationRestoration(12, 24, "product-filter");

        NavigationCoreResult back = core.ExecuteNavigation(Request(UiNavigationKind.Back, string.Empty, null, 3, true));
        NavigationCoreResult forward = core.ExecuteNavigation(Request(UiNavigationKind.Forward, string.Empty, null, 4, true));
        NavigationCoreResult boundary = core.ExecuteNavigation(Request(UiNavigationKind.Forward, string.Empty, null, 5, true));

        Assert.Multiple(() =>
        {
            Assert.That(back.Success, Is.True);
            Assert.That(back.Navigation.CanonicalUrl, Is.EqualTo("/home"));
            Assert.That(back.Navigation.State!.StringValue, Is.EqualTo("home-state"));
            Assert.That(back.Navigation.HasScrollOffset, Is.True);
            Assert.That(back.Navigation.ScrollX, Is.EqualTo(4.5));
            Assert.That(back.Navigation.ScrollY, Is.EqualTo(8.25));
            Assert.That(back.Navigation.RestoreFocus, Is.True);
            Assert.That(back.Navigation.FocusTargetId, Is.EqualTo("home-title"));
            Assert.That(forward.Navigation.CanonicalUrl, Is.EqualTo("/products"));
            Assert.That(forward.Navigation.ScrollX, Is.EqualTo(12));
            Assert.That(forward.Navigation.ScrollY, Is.EqualTo(24));
            Assert.That(forward.Navigation.FocusTargetId, Is.EqualTo("product-filter"));
            Assert.That(boundary.Success, Is.True);
            Assert.That(boundary.Navigation.Status, Is.EqualTo(UiNavigationStatus.Cancelled));
            Assert.That(boundary.Code, Is.EqualTo("navigation.history-boundary"));
            Assert.That(boundary.HistoryIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReloadsTheCurrentEntryAndPreservesItsRestorationIntents()
    {
        NavigationCore core = CreateCore();
        core.InitializeNavigationHistory("/products#specifications", Text("saved"), 1);
        core.CaptureNavigationRestoration(3, 90, "product-heading");

        NavigationCoreResult reloaded = core.ExecuteNavigation(Request(UiNavigationKind.Reload, string.Empty, null, 2, true));

        Assert.Multiple(() =>
        {
            Assert.That(reloaded.Success, Is.True);
            Assert.That(reloaded.Reload, Is.True);
            Assert.That(reloaded.HistoryChanged, Is.False);
            Assert.That(reloaded.History, Has.Length.EqualTo(1));
            Assert.That(reloaded.Navigation.CanonicalUrl, Is.EqualTo("/products#specifications"));
            Assert.That(reloaded.Navigation.FragmentTargetId, Is.EqualTo("specifications"));
            Assert.That(reloaded.Navigation.HasScrollOffset, Is.True);
            Assert.That(reloaded.Navigation.ScrollY, Is.EqualTo(90));
            Assert.That(reloaded.Navigation.RestoreFocus, Is.True);
            Assert.That(reloaded.Navigation.FocusTargetId, Is.EqualTo("product-heading"));
            Assert.That(reloaded.Navigation.State!.StringValue, Is.EqualTo("saved"));
        });
    }

    [Test]
    public void ResolvesSameDocumentFragmentTargetsAndRejectsCrossDocumentFragmentRequests()
    {
        NavigationCore core = CreateCore();
        core.InitializeNavigationHistory("/home?mode=full#old", null, 1);

        NavigationCoreResult fragment = core.ExecuteNavigation(Request(UiNavigationKind.Fragment, "#new section", null, 2, true));
        NavigationCoreResult crossDocument = core.ExecuteNavigation(Request(UiNavigationKind.Fragment, "/products#details", null, 3, true));
        NavigationCoreResult queryFragment = core.ExecuteNavigation(Request(UiNavigationKind.Push, "?mode=compact#summary", null, 4, false));

        Assert.Multiple(() =>
        {
            Assert.That(fragment.Success, Is.True);
            Assert.That(fragment.Navigation.CanonicalUrl, Is.EqualTo("/home?mode=full#new%20section"));
            Assert.That(fragment.Navigation.FragmentTargetId, Is.EqualTo("new section"));
            Assert.That(fragment.Navigation.ScrollToTop, Is.False);
            Assert.That(fragment.Navigation.FocusTargetId, Is.EqualTo("new section"));
            Assert.That(crossDocument.Success, Is.False);
            Assert.That(crossDocument.Code, Is.EqualTo("navigation.fragment-cross-document"));
            Assert.That(crossDocument.History, Has.Length.EqualTo(2));
            Assert.That(queryFragment.Success, Is.True);
            Assert.That(queryFragment.Navigation.CanonicalUrl, Is.EqualTo("/home?mode=compact#summary"));
            Assert.That(queryFragment.Navigation.MoveFocus, Is.False);
            Assert.That(queryFragment.Navigation.FocusTargetId, Is.Null);
        });
    }

    [Test]
    public void InterceptsOnlyEligibleInternalLinksAndAppliesExternalPolicy()
    {
        NavigationCore core = CreateCore();
        core.InitializeNavigationHistory("/home", null, 1);

        NavigationLinkRequest internalLink = Link("/products", 2);
        NavigationCoreResult internalResult = core.ExecuteLinkNavigation(internalLink);
        NavigationLinkRequest modifiedLink = Link("/home", 3);
        modifiedLink.HasModifier = true;
        NavigationCoreResult ignored = core.ExecuteLinkNavigation(modifiedLink);
        NavigationCoreResult blocked = core.ExecuteLinkNavigation(Link("https://example.test/catalog", 4));
        core.AllowExternalNavigation = true;
        NavigationCoreResult external = core.ExecuteLinkNavigation(Link("https://example.test/catalog", 5));

        Assert.Multiple(() =>
        {
            Assert.That(internalResult.Success, Is.True);
            Assert.That(internalResult.Intercepted, Is.True);
            Assert.That(internalResult.Navigation.Status, Is.EqualTo(UiNavigationStatus.Resolved));
            Assert.That(ignored.Success, Is.True);
            Assert.That(ignored.Intercepted, Is.False);
            Assert.That(ignored.Navigation.Status, Is.EqualTo(UiNavigationStatus.Cancelled));
            Assert.That(ignored.History, Has.Length.EqualTo(2));
            Assert.That(blocked.Success, Is.False);
            Assert.That(blocked.Intercepted, Is.True);
            Assert.That(blocked.Code, Is.EqualTo("navigation.external-blocked"));
            Assert.That(external.Success, Is.True);
            Assert.That(external.Intercepted, Is.False);
            Assert.That(external.Navigation.Status, Is.EqualTo(UiNavigationStatus.External));
            Assert.That(external.Navigation.ExternalUrl, Is.EqualTo("https://example.test/catalog"));
            Assert.That(external.History, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void RequiresExplicitExternalPolicyAndAValidExternalDestination()
    {
        NavigationCore core = CreateCore();
        UiNavigationRequest externalRequest = Request(UiNavigationKind.External, "mailto:person@example.test", null, 1, false);
        NavigationCoreResult blocked = core.ExecuteNavigation(externalRequest);
        core.AllowExternalNavigation = true;
        NavigationCoreResult allowed = core.ExecuteNavigation(externalRequest);
        NavigationCoreResult internalDestination = core.ExecuteNavigation(Request(UiNavigationKind.External, "/home", null, 2, false));
        NavigationCoreResult unsafeDestination = core.ExecuteNavigation(Request(UiNavigationKind.External, "javascript:alert(1)", null, 3, false));

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Code, Is.EqualTo("navigation.external-blocked"));
            Assert.That(allowed.Success, Is.True);
            Assert.That(allowed.Navigation.Status, Is.EqualTo(UiNavigationStatus.External));
            Assert.That(allowed.Navigation.ExternalUrl, Is.EqualTo("mailto:person@example.test"));
            Assert.That(internalDestination.Code, Is.EqualTo("navigation.expected-external-url"));
            Assert.That(unsafeDestination.Success, Is.False);
            Assert.That(unsafeDestination.Code, Is.EqualTo("route.unsafe-scheme"));
            Assert.That(core.History, Is.Empty);
        });
    }

    [Test]
    public void FollowsRedirectsBeforeWritingTheDestinationHistoryEntry()
    {
        NavigationCore core = CreateCore();
        UiRoute legacy = Route("legacy", "/legacy");
        legacy.RedirectUrl = "/products#new";
        core.Routes = new UiRoute[] { legacy, Route("home", "/home"), Route("products", "/products") };

        NavigationCoreResult result = core.InitializeNavigationHistory("/legacy", Text("redirect-state"), 7);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Navigation.Status, Is.EqualTo(UiNavigationStatus.Resolved));
            Assert.That(result.Navigation.CanonicalUrl, Is.EqualTo("/products#new"));
            Assert.That(result.Navigation.FragmentTargetId, Is.EqualTo("new"));
            Assert.That(result.History.Single().Url, Is.EqualTo("/products#new"));
            Assert.That(result.History.Single().State!.StringValue, Is.EqualTo("redirect-state"));
        });
    }

    private static NavigationCore CreateCore()
    {
        NavigationCore core = new NavigationCore();
        core.Routes = new UiRoute[] { Route("home", "/home"), Route("products", "/products") };
        return core;
    }

    private static UiRoute Route(string id, string template)
    {
        return new UiRoute { Id = id, Template = template };
    }

    private static UiNavigationRequest Request(UiNavigationKind kind, string url, UiValue? state, int generation, bool moveFocus)
    {
        return new UiNavigationRequest { Kind = kind, Url = url, State = state, Generation = generation, MoveFocus = moveFocus };
    }

    private static NavigationLinkRequest Link(string url, int generation)
    {
        return new NavigationLinkRequest { Url = url, Generation = generation };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
