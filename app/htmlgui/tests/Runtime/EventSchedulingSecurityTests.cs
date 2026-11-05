using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class EventSchedulingSecurityTests
{
    [Test]
    public void DebounceReplacesPendingDataAndReleasesItAtTheDeterministicDueTime()
    {
        EventCore core = new EventCore();
        core.RegisterEventSubscription("target", Subscription("debounced", 50, 0));
        EventDispatchMetadata metadata = Metadata("https://app.example", true, 0);
        UiEventData firstEvent = PointerEvent(1);
        UiEventData latestEvent = PointerEvent(2);

        EventDispatchResult first = core.DispatchTrustedEventData(firstEvent, metadata, 100);
        EventDispatchResult latest = core.DispatchTrustedEventData(latestEvent, metadata, 120);
        EventDispatchResult[] early = core.FlushScheduledEvents(169);
        EventDispatchResult[] due = core.FlushScheduledEvents(170);

        Assert.Multiple(() =>
        {
            Assert.That(first.Invocations, Is.Empty);
            Assert.That(first.DeferredBindingIds, Is.EqualTo(new string[] { "debounced" }));
            Assert.That(latest.Invocations, Is.Empty);
            Assert.That(latest.DeferredBindingIds, Is.EqualTo(new string[] { "debounced" }));
            Assert.That(early, Is.Empty);
            Assert.That(due, Has.Length.EqualTo(1));
            Assert.That(due[0].Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "debounced" }));
            Assert.That(due[0].Invocations[0].Phase, Is.EqualTo(UiEventPhase.Target));
            Assert.That(due[0].EventData.Pointer!.ClientX, Is.EqualTo(2));
            Assert.That(due[0].Origin, Is.EqualTo("https://app.example"));
        });
    }

    [Test]
    public void FlushesDebouncedBindingsByDueTimeThenRegistrationOrder()
    {
        EventCore core = new EventCore();
        core.RegisterEventSubscription("target", Subscription("slow-first", 50, 0));
        core.RegisterEventSubscription("target", Subscription("fast-second", 20, 0));
        core.RegisterEventSubscription("target", Subscription("fast-third", 20, 0));

        EventDispatchResult queued = core.DispatchTrustedEventData(PointerEvent(4), Metadata("", true, 0), 0);
        EventDispatchResult[] results = core.FlushScheduledEvents(50);

        Assert.Multiple(() =>
        {
            Assert.That(queued.DeferredBindingIds, Is.EqualTo(new string[] { "slow-first", "fast-second", "fast-third" }));
            Assert.That(results.Select(item => item.Invocations[0].BindingId), Is.EqualTo(new string[] { "fast-second", "fast-third", "slow-first" }));
        });
    }

    [Test]
    public void ThrottleUsesALeadingEdgeAndReopensExactlyAtTheIntervalBoundary()
    {
        EventCore core = new EventCore();
        core.RegisterEventSubscription("target", Subscription("throttled", 0, 100));
        EventDispatchMetadata metadata = Metadata("", true, 0);

        EventDispatchResult first = core.DispatchTrustedEventData(PointerEvent(1), metadata, 1000);
        EventDispatchResult suppressed = core.DispatchTrustedEventData(PointerEvent(2), metadata, 1099);
        EventDispatchResult boundary = core.DispatchTrustedEventData(PointerEvent(3), metadata, 1100);

        Assert.Multiple(() =>
        {
            Assert.That(first.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "throttled" }));
            Assert.That(first.ThrottledBindingIds, Is.Empty);
            Assert.That(suppressed.Invocations, Is.Empty);
            Assert.That(suppressed.ThrottledBindingIds, Is.EqualTo(new string[] { "throttled" }));
            Assert.That(boundary.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "throttled" }));
            Assert.That(boundary.EventData.Pointer!.ClientX, Is.EqualTo(3));
        });
    }

    [Test]
    public void AppliesImmediatePropagationFlagsEvenWhenAHandlerIsDeferredOrThrottled()
    {
        EventCore debounceCore = new EventCore();
        DomEventSubscription debounce = Subscription("debounce", 10, 0);
        debounce.PreventDefault = true;
        debounce.StopPropagation = true;
        debounceCore.RegisterEventSubscription("target", debounce);
        EventCore throttleCore = new EventCore();
        DomEventSubscription throttle = Subscription("throttle", 0, 100);
        throttle.PreventDefault = true;
        throttleCore.RegisterEventSubscription("target", throttle);
        EventDispatchMetadata metadata = Metadata("", true, 0);

        EventDispatchResult deferred = debounceCore.DispatchTrustedEventData(PointerEvent(1), metadata, 0);
        throttleCore.DispatchTrustedEventData(PointerEvent(1), metadata, 0);
        EventDispatchResult throttled = throttleCore.DispatchTrustedEventData(PointerEvent(2), metadata, 1);

        Assert.Multiple(() =>
        {
            Assert.That(deferred.DefaultPrevented, Is.True);
            Assert.That(deferred.PropagationStopped, Is.True);
            Assert.That(throttled.DefaultPrevented, Is.True);
            Assert.That(throttled.Invocations, Is.Empty);
        });
    }

    [Test]
    public void RemovesOnceDebouncedBindingsOnlyAfterTheirPendingInvocation()
    {
        EventCore core = new EventCore();
        DomEventSubscription subscription = Subscription("once", 10, 0);
        subscription.Once = true;
        core.RegisterEventSubscription("target", subscription);
        EventDispatchMetadata metadata = Metadata("", true, 0);

        EventDispatchResult queued = core.DispatchTrustedEventData(PointerEvent(1), metadata, 0);
        EventDispatchResult[] flushed = core.FlushScheduledEvents(10);
        EventDispatchResult afterRemoval = core.DispatchTrustedEventData(PointerEvent(2), metadata, 11);

        Assert.Multiple(() =>
        {
            Assert.That(queued.RemovedBindingIds, Is.Empty);
            Assert.That(flushed[0].RemovedBindingIds, Is.EqualTo(new string[] { "once" }));
            Assert.That(afterRemoval.Invocations, Is.Empty);
            Assert.That(afterRemoval.DeferredBindingIds, Is.Empty);
        });
    }

    [Test]
    public void RejectsInvalidSchedulingPoliciesAndNonMonotonicClocks()
    {
        EventCore core = new EventCore();
        DomEventSubscription wrongName = Subscription("wrong-name", 0, 0);
        wrongName.EventName = DomEventNames.KeyDown;
        DomEventSubscription negativeDebounce = Subscription("negative-debounce", -1, 0);
        DomEventSubscription negativeThrottle = Subscription("negative-throttle", 0, -1);
        DomEventSubscription both = Subscription("both", 5, 5);
        core.RegisterEventSubscription("target", Subscription("valid", 0, 20));
        core.DispatchTrustedEventData(PointerEvent(1), Metadata("", true, 0), 20);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => core.RegisterEventSubscription("target", null));
            Assert.Throws<ArgumentException>(() => core.RegisterEventSubscription("target", wrongName));
            Assert.Throws<ArgumentOutOfRangeException>(() => core.RegisterEventSubscription("target", negativeDebounce));
            Assert.Throws<ArgumentOutOfRangeException>(() => core.RegisterEventSubscription("target", negativeThrottle));
            Assert.Throws<ArgumentException>(() => core.RegisterEventSubscription("target", both));
            Assert.Throws<ArgumentOutOfRangeException>(() => core.FlushScheduledEvents(-1));
            Assert.Throws<ArgumentException>(() => core.FlushScheduledEvents(19));
        });
    }

    [Test]
    public void AcceptsMatchingTrustedMainFrameMetadataAndReportsIt()
    {
        EventCore core = SecuredCore();
        core.RegisterEventBinding("target", Binding("handler"));

        EventDispatchResult result = core.DispatchTrustedEventData(TrustedEvent("target", "root"), Metadata("https://app.example", true, 7), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.RejectionCode, Is.Null);
            Assert.That(result.Origin, Is.EqualTo("https://app.example"));
            Assert.That(result.IsMainFrame, Is.True);
            Assert.That(result.IsTrustedOrigin, Is.True);
            Assert.That(result.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "handler" }));
        });
    }

    [Test]
    public void RejectsOriginFrameTrustGenerationAndStaleNodeFailuresWithoutInvocation()
    {
        EventCore originCore = SecuredCore();
        EventCore frameCore = SecuredCore();
        EventCore trustCore = SecuredCore();
        EventCore generationCore = SecuredCore();
        EventCore nodeCore = SecuredCore();
        EventCore[] cores = new EventCore[] { originCore, frameCore, trustCore, generationCore, nodeCore };
        foreach (EventCore core in cores)
        {
            core.RegisterEventBinding("target", Binding("handler"));
        }

        EventDispatchResult origin = originCore.DispatchTrustedEventData(TrustedEvent("target", "root"), Metadata("https://evil.example", true, 7), 0);
        EventDispatchResult frame = frameCore.DispatchTrustedEventData(TrustedEvent("target", "root"), Metadata("https://app.example", false, 7), 0);
        UiEventData untrustedEvent = Event(UiEventKind.Click, "target", "target", "root");
        EventDispatchResult trust = trustCore.DispatchTrustedEventData(untrustedEvent, Metadata("https://app.example", true, 7), 0);
        EventDispatchResult generation = generationCore.DispatchTrustedEventData(TrustedEvent("target", "root"), Metadata("https://app.example", true, 6), 0);
        EventDispatchResult node = nodeCore.DispatchTrustedEventData(TrustedEvent("missing", "root"), Metadata("https://app.example", true, 7), 0);

        Assert.Multiple(() =>
        {
            AssertRejected(origin, "event.origin");
            AssertRejected(frame, "event.main-frame");
            AssertRejected(trust, "event.untrusted");
            AssertRejected(generation, "event.stale-generation");
            AssertRejected(node, "event.stale-node");
        });
    }

    [Test]
    public void RechecksLivenessBeforeDeliveringADebouncedInvocation()
    {
        EventCore core = new EventCore();
        core.ConfigureEventSecurity("https://app.example", true, true);
        core.UpdateLiveEventNodes(new string[] { "target", "root" }, 1);
        DomEventSubscription subscription = Subscription("debounced", 10, 0);
        subscription.Kind = UiEventKind.Click;
        subscription.EventName = DomEventNames.Click;
        core.RegisterEventSubscription("target", subscription);
        core.DispatchTrustedEventData(TrustedEvent("target", "root"), Metadata("https://app.example", true, 1), 0);
        core.UpdateLiveEventNodes(new string[] { "target", "root" }, 2);

        EventDispatchResult[] results = core.FlushScheduledEvents(10);

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Length.EqualTo(1));
            AssertRejected(results[0], "event.stale-generation");
        });
    }

    [Test]
    public void ValidatesAndCopiesLiveNodeSetsAndSecurityConfiguration()
    {
        EventCore core = new EventCore();
        string[] nodes = new string[] { "target", "root" };
        core.ConfigureEventSecurity("", false, false);
        core.UpdateLiveEventNodes(nodes, 3);
        nodes[0] = "changed";

        EventDispatchResult accepted = core.DispatchTrustedEventData(Event(UiEventKind.Click, "target", "target", "root"), Metadata("any-origin", false, 3), 0);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.IsMainFrame, Is.False);
            Assert.That(accepted.IsTrustedOrigin, Is.False);
            Assert.Throws<ArgumentException>(() => core.ConfigureEventSecurity(" ", false, false));
            Assert.Throws<ArgumentNullException>(() => core.UpdateLiveEventNodes(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => core.UpdateLiveEventNodes(Array.Empty<string>(), -1));
            Assert.Throws<ArgumentException>(() => core.UpdateLiveEventNodes(new string[] { "same", "same" }, 0));
            Assert.Throws<ArgumentException>(() => core.UpdateLiveEventNodes(new string[] { " " }, 0));
        });
    }

    private static EventCore SecuredCore()
    {
        EventCore core = new EventCore();
        core.ConfigureEventSecurity("https://app.example", true, true);
        core.UpdateLiveEventNodes(new string[] { "target", "root" }, 7);
        return core;
    }

    private static DomEventSubscription Subscription(string id, int debounceMilliseconds, int throttleMilliseconds)
    {
        return new DomEventSubscription
        {
            Id = id,
            HandlerId = id + "-handler",
            Kind = UiEventKind.PointerMove,
            EventName = DomEventNames.PointerMove,
            Phase = UiEventPhase.Bubble,
            HasDebounceMilliseconds = debounceMilliseconds != 0,
            DebounceMilliseconds = debounceMilliseconds,
            HasThrottleMilliseconds = throttleMilliseconds != 0,
            ThrottleMilliseconds = throttleMilliseconds
        };
    }

    private static UiEventBinding Binding(string id)
    {
        return new UiEventBinding { Id = id, HandlerId = id + "-handler", Kind = UiEventKind.Click, Phase = UiEventPhase.Bubble };
    }

    private static EventDispatchMetadata Metadata(string origin, bool isMainFrame, int generation)
    {
        return new EventDispatchMetadata { Origin = origin, IsMainFrame = isMainFrame, NodeGeneration = generation };
    }

    private static UiEventData PointerEvent(double x)
    {
        UiEventData result = Event(UiEventKind.PointerMove, "target", "target");
        result.Pointer = new UiPointerData { PointerType = "mouse", ClientX = x };
        return result;
    }

    private static UiEventData TrustedEvent(params string[] path)
    {
        UiEventData result = Event(UiEventKind.Click, path[0], path);
        result.IsTrusted = true;
        return result;
    }

    private static UiEventData Event(UiEventKind kind, string targetId, params string[] path)
    {
        return new UiEventData { Kind = kind, TargetId = targetId, Path = path };
    }

    private static void AssertRejected(EventDispatchResult result, string code)
    {
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.RejectionCode, Is.EqualTo(code));
        Assert.That(result.Invocations, Is.Empty);
    }
}
