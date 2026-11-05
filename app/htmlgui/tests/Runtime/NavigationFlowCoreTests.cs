using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class NavigationFlowCoreTests
{
    [Test]
    public void RunsGuardsInRegistrationOrderAndPublishesOrderedObserverStages()
    {
        NavigationFlowCore flow = CreateFlow();
        Assert.That(flow.RegisterNavigationObserver("observer-a").Success, Is.True);
        Assert.That(flow.RegisterNavigationObserver("observer-b").Success, Is.True);
        Assert.That(flow.RegisterNavigationGuard("guard-a").Success, Is.True);
        Assert.That(flow.RegisterNavigationGuard("guard-b").Success, Is.True);

        UiNavigationRequest request = Request("/products", Text("original"));
        NavigationFlowResult begun = flow.BeginNavigationFlow(request);
        request.Url = "/home";
        request.State!.StringValue = "changed";
        NavigationGuardInvocation first = flow.DrainNavigationGuardInvocations().Single();
        first.Request.Url = "/changed-by-handler";
        NavigationFlowResult firstCompleted = flow.CompleteNavigationGuard("guard-a", begun.Generation, true, true, null);
        NavigationGuardInvocation second = flow.DrainNavigationGuardInvocations().Single();
        NavigationFlowResult completed = flow.CompleteNavigationGuard("guard-b", begun.Generation, true, true, null);
        NavigationObserverInvocation[] observers = flow.DrainNavigationObserverInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(begun.IsPending, Is.True);
            Assert.That(begun.Generation, Is.EqualTo(1));
            Assert.That(first.HandlerId, Is.EqualTo("guard-a"));
            Assert.That(first.Request.Url, Is.EqualTo("/changed-by-handler"));
            Assert.That(firstCompleted.IsPending, Is.True);
            Assert.That(second.HandlerId, Is.EqualTo("guard-b"));
            Assert.That(second.Request.Url, Is.EqualTo("/products"));
            Assert.That(second.Request.State!.StringValue, Is.EqualTo("original"));
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Applied, Is.True);
            Assert.That(completed.IsPending, Is.False);
            Assert.That(completed.Navigation!.Navigation.CanonicalUrl, Is.EqualTo("/products"));
            Assert.That(observers.Select(item => item.Stage), Is.EqualTo(new string[]
            {
                "before", "before", "pending", "pending", "after", "after"
            }));
            Assert.That(observers.Select(item => item.HandlerId), Is.EqualTo(new string[]
            {
                "observer-a", "observer-b", "observer-a", "observer-b", "observer-a", "observer-b"
            }));
            Assert.That(observers.All(item => item.Generation == 1), Is.True);
            Assert.That(observers.Last().Navigation.CanonicalUrl, Is.EqualTo("/products"));
        });
    }

    [Test]
    public void CancelsNavigationWhenAGuardDeniesIt()
    {
        NavigationFlowCore flow = CreateFlow();
        flow.RegisterNavigationObserver("observer");
        flow.RegisterNavigationGuard("guard");
        NavigationFlowResult begun = flow.BeginNavigationFlow(Request("/products", null));

        NavigationFlowResult cancelled = flow.CompleteNavigationGuard("guard", begun.Generation, true, false, null);
        NavigationObserverInvocation[] observers = flow.DrainNavigationObserverInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Success, Is.True);
            Assert.That(cancelled.Applied, Is.True);
            Assert.That(cancelled.Code, Is.EqualTo("navigation.guard-cancelled"));
            Assert.That(cancelled.Navigation!.Navigation.Status, Is.EqualTo(UiNavigationStatus.Cancelled));
            Assert.That(cancelled.Navigation.History.Single().Url, Is.EqualTo("/home"));
            Assert.That(observers.Select(item => item.Stage), Is.EqualTo(new string[] { "before", "pending", "cancel" }));
            Assert.That(observers.Last().Code, Is.EqualTo("navigation.guard-cancelled"));
        });
    }

    [Test]
    public void PublishesErrorsWhenAGuardFailsAndPreservesTheDiagnosticCode()
    {
        NavigationFlowCore flow = CreateFlow();
        flow.RegisterNavigationObserver("observer");
        flow.RegisterNavigationGuard("guard");
        NavigationFlowResult begun = flow.BeginNavigationFlow(Request("/products", null));

        NavigationFlowResult failed = flow.CompleteNavigationGuard("guard", begun.Generation, false, false, "guard.connection-failed");
        NavigationObserverInvocation[] observers = flow.DrainNavigationObserverInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Code, Is.EqualTo("guard.connection-failed"));
            Assert.That(failed.Navigation!.Navigation.Status, Is.EqualTo(UiNavigationStatus.Failed));
            Assert.That(failed.Navigation.Navigation.Diagnostic!.Code, Is.EqualTo("guard.connection-failed"));
            Assert.That(observers.Select(item => item.Stage), Is.EqualTo(new string[] { "before", "pending", "error" }));
            Assert.That(observers.Last().Code, Is.EqualTo("guard.connection-failed"));
            Assert.That(observers.Last().Navigation.Status, Is.EqualTo(UiNavigationStatus.Failed));
            Assert.That(failed.Navigation.History.Single().Url, Is.EqualTo("/home"));
        });
    }

    [Test]
    public void SuppressesStaleGuardCompletionAfterANewerGenerationStarts()
    {
        NavigationFlowCore flow = CreateFlow();
        flow.RegisterNavigationObserver("observer");
        flow.RegisterNavigationGuard("guard");
        NavigationFlowResult first = flow.BeginNavigationFlow(Request("/products", null));
        flow.DrainNavigationGuardInvocations();
        flow.DrainNavigationObserverInvocations();

        NavigationFlowResult second = flow.BeginNavigationFlow(Request("/home?fresh=true", null));
        NavigationGuardInvocation currentGuard = flow.DrainNavigationGuardInvocations().Single();
        NavigationObserverInvocation[] transitionObservers = flow.DrainNavigationObserverInvocations();
        NavigationFlowResult stale = flow.CompleteNavigationGuard("guard", first.Generation, true, true, null);
        NavigationFlowResult current = flow.CompleteNavigationGuard("guard", second.Generation, true, true, null);

        Assert.Multiple(() =>
        {
            Assert.That(second.Generation, Is.EqualTo(2));
            Assert.That(currentGuard.Generation, Is.EqualTo(2));
            Assert.That(transitionObservers.Select(item => item.Stage), Is.EqualTo(new string[] { "cancel", "before", "pending" }));
            Assert.That(transitionObservers[0].Generation, Is.EqualTo(1));
            Assert.That(transitionObservers[0].Code, Is.EqualTo("navigation.superseded"));
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.IsStale, Is.True);
            Assert.That(stale.Applied, Is.False);
            Assert.That(stale.Code, Is.EqualTo("navigation.stale-generation"));
            Assert.That(current.Success, Is.True);
            Assert.That(current.Navigation!.History, Has.Length.EqualTo(2));
            Assert.That(current.Navigation.History.Last().Url, Is.EqualTo("/home?fresh=true"));
            Assert.That(current.Navigation.History.Any(item => item.Url == "/products"), Is.False);
        });
    }

    [Test]
    public void SupportsExplicitCancellationAndUsesTheFlowObserverSnapshot()
    {
        NavigationFlowCore flow = CreateFlow();
        flow.RegisterNavigationObserver("observer");
        flow.RegisterNavigationGuard("guard");
        NavigationFlowResult begun = flow.BeginNavigationFlow(Request("/products", null));
        Assert.That(flow.UnregisterNavigationObserver("observer").Success, Is.True);

        NavigationFlowResult cancelled = flow.CancelNavigationFlow(begun.Generation);
        NavigationFlowResult staleCancellation = flow.CancelNavigationFlow(begun.Generation);
        NavigationObserverInvocation[] observers = flow.DrainNavigationObserverInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(cancelled.Success, Is.True);
            Assert.That(cancelled.Code, Is.EqualTo("navigation.cancelled"));
            Assert.That(staleCancellation.IsStale, Is.True);
            Assert.That(flow.DrainNavigationGuardInvocations(), Is.Empty);
            Assert.That(observers.Select(item => item.Stage), Is.EqualTo(new string[] { "before", "pending", "cancel" }));
            Assert.That(observers.All(item => item.HandlerId == "observer"), Is.True);
        });
    }

    [Test]
    public void CompletesImmediatelyWithoutGuardsAndValidatesRegistrationsAndGenerationLimits()
    {
        NavigationFlowCore flow = CreateFlow();
        NavigationRegistrationResult invalidGuard = flow.RegisterNavigationGuard(" ");
        NavigationRegistrationResult registered = flow.RegisterNavigationObserver("observer");
        NavigationRegistrationResult duplicate = flow.RegisterNavigationObserver("observer");
        NavigationRegistrationResult unknown = flow.UnregisterNavigationGuard("missing");
        NavigationFlowResult completed = flow.BeginNavigationFlow(Request("/products", null));
        NavigationObserverInvocation[] observers = flow.DrainNavigationObserverInvocations();
        flow.Generation = int.MaxValue;
        NavigationFlowResult exhausted = flow.BeginNavigationFlow(Request("/home", null));

        Assert.Multiple(() =>
        {
            Assert.That(invalidGuard.Code, Is.EqualTo("navigation.invalid-guard"));
            Assert.That(registered.Success, Is.True);
            Assert.That(duplicate.Code, Is.EqualTo("navigation.duplicate-observer"));
            Assert.That(unknown.Code, Is.EqualTo("navigation.unknown-guard"));
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.IsPending, Is.False);
            Assert.That(completed.Navigation!.Navigation.Status, Is.EqualTo(UiNavigationStatus.Resolved));
            Assert.That(observers.Select(item => item.Stage), Is.EqualTo(new string[] { "before", "pending", "after" }));
            Assert.That(exhausted.Success, Is.False);
            Assert.That(exhausted.Code, Is.EqualTo("navigation.generation-exhausted"));
        });
    }

    private static NavigationFlowCore CreateFlow()
    {
        NavigationFlowCore flow = new NavigationFlowCore();
        flow.Navigator.Routes = new UiRoute[]
        {
            new UiRoute { Id = "home", Template = "/home" },
            new UiRoute { Id = "products", Template = "/products" }
        };
        NavigationCoreResult initialized = flow.Navigator.InitializeNavigationHistory("/home", null, 0);
        Assert.That(initialized.Success, Is.True);
        return flow;
    }

    private static UiNavigationRequest Request(string url, UiValue? state)
    {
        return new UiNavigationRequest { Kind = UiNavigationKind.Push, Url = url, State = state, MoveFocus = true };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
