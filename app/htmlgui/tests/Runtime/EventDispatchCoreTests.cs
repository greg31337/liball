using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class EventDispatchCoreTests
{
    [Test]
    public void DispatchesCaptureTargetAndBubbleInDeterministicOrder()
    {
        EventCore core = new EventCore();
        core.RegisterEventBinding("root", Binding("root-capture-1", "h1", UiEventPhase.Capture));
        core.RegisterEventBinding("target", Binding("target", "h2", UiEventPhase.Target));
        core.RegisterEventBinding("parent", Binding("parent-bubble", "h3", UiEventPhase.Bubble));
        core.RegisterEventBinding("parent", Binding("parent-capture", "h4", UiEventPhase.Capture));
        core.RegisterEventBinding("root", Binding("root-bubble", "h5", UiEventPhase.Bubble));
        core.RegisterEventBinding("root", Binding("root-capture-2", "h6", UiEventPhase.Capture));

        EventDispatchResult result = core.DispatchEventData(Event("target", "target", "parent", "root"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[]
            {
                "root-capture-1", "root-capture-2", "parent-capture", "target", "parent-bubble", "root-bubble"
            }));
            Assert.That(result.Invocations.Select(item => item.HandlerId), Is.EqualTo(new string[] { "h1", "h6", "h4", "h2", "h3", "h5" }));
            Assert.That(result.Invocations.Select(item => item.NodeId), Is.EqualTo(new string[] { "root", "root", "parent", "target", "parent", "root" }));
            Assert.That(result.Invocations.Select(item => item.Phase), Is.EqualTo(new UiEventPhase[]
            {
                UiEventPhase.Capture, UiEventPhase.Capture, UiEventPhase.Capture,
                UiEventPhase.Target, UiEventPhase.Bubble, UiEventPhase.Bubble
            }));
            Assert.That(result.DefaultPrevented, Is.False);
            Assert.That(result.PropagationStopped, Is.False);
        });
    }

    [Test]
    public void DelegatesOnlyMatchingEventKindsAlongTheSuppliedPath()
    {
        EventCore core = new EventCore();
        core.RegisterEventBinding("root", Binding("delegated-click", "click-handler", UiEventPhase.Bubble));
        UiEventBinding key = Binding("delegated-key", "key-handler", UiEventPhase.Bubble);
        key.Kind = UiEventKind.KeyDown;
        core.RegisterEventBinding("root", key);
        core.RegisterEventBinding("sibling", Binding("sibling", "sibling-handler", UiEventPhase.Bubble));

        EventDispatchResult click = core.DispatchEventData(Event("child", "child", "root"));
        UiEventData keyEvent = Event("child", "child", "root");
        keyEvent.Kind = UiEventKind.KeyDown;
        EventDispatchResult keyDown = core.DispatchEventData(keyEvent);

        Assert.Multiple(() =>
        {
            Assert.That(click.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "delegated-click" }));
            Assert.That(keyDown.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "delegated-key" }));
        });
    }

    [Test]
    public void StopPropagationFinishesTheCurrentNodeThenSkipsLaterNodesAndPhases()
    {
        EventCore core = new EventCore();
        UiEventBinding stopping = Binding("parent-stop", "stop-handler", UiEventPhase.Capture);
        stopping.StopPropagation = true;
        stopping.PreventDefault = true;
        core.RegisterEventBinding("root", Binding("root-capture", "root-handler", UiEventPhase.Capture));
        core.RegisterEventBinding("parent", stopping);
        core.RegisterEventBinding("parent", Binding("parent-after-stop", "after-handler", UiEventPhase.Capture));
        core.RegisterEventBinding("target", Binding("target", "target-handler", UiEventPhase.Target));
        core.RegisterEventBinding("root", Binding("root-bubble", "bubble-handler", UiEventPhase.Bubble));

        EventDispatchResult result = core.DispatchEventData(Event("target", "target", "parent", "root"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "root-capture", "parent-stop", "parent-after-stop" }));
            Assert.That(result.DefaultPrevented, Is.True);
            Assert.That(result.PropagationStopped, Is.True);
            Assert.That(result.Invocations[1].PreventDefault, Is.True);
            Assert.That(result.Invocations[1].StopPropagation, Is.True);
        });
    }

    [Test]
    public void TargetStopPropagationRunsOtherTargetBindingsButPreventsBubbling()
    {
        EventCore core = new EventCore();
        UiEventBinding stopping = Binding("target-stop", "stop-handler", UiEventPhase.Target);
        stopping.StopPropagation = true;
        core.RegisterEventBinding("target", stopping);
        core.RegisterEventBinding("target", Binding("target-after", "after-handler", UiEventPhase.Target));
        core.RegisterEventBinding("root", Binding("root-bubble", "root-handler", UiEventPhase.Bubble));

        EventDispatchResult result = core.DispatchEventData(Event("target", "target", "root"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "target-stop", "target-after" }));
            Assert.That(result.PropagationStopped, Is.True);
        });
    }

    [Test]
    public void PassiveBindingsCannotPreventDefault()
    {
        EventCore core = new EventCore();
        UiEventBinding passive = Binding("passive", "passive-handler", UiEventPhase.Target);
        passive.Passive = true;
        passive.PreventDefault = true;
        core.RegisterEventBinding("target", passive);

        EventDispatchResult result = core.DispatchEventData(Event("target", "target"));

        Assert.Multiple(() =>
        {
            Assert.That(result.DefaultPrevented, Is.False);
            Assert.That(result.Invocations[0].Passive, Is.True);
            Assert.That(result.Invocations[0].PreventDefault, Is.False);
        });
    }

    [Test]
    public void OnceBindingsAreRemovedOnlyAfterTheyAreInvoked()
    {
        EventCore core = new EventCore();
        UiEventBinding once = Binding("once", "once-handler", UiEventPhase.Bubble);
        once.Once = true;
        core.RegisterEventBinding("root", once);
        UiEventBinding blockedOnce = Binding("blocked-once", "blocked-handler", UiEventPhase.Bubble);
        blockedOnce.Once = true;
        core.RegisterEventBinding("other", blockedOnce);

        EventDispatchResult first = core.DispatchEventData(Event("target", "target", "root"));
        EventDispatchResult second = core.DispatchEventData(Event("target", "target", "root"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "once" }));
            Assert.That(first.RemovedBindingIds, Is.EqualTo(new string[] { "once" }));
            Assert.That(second.Invocations, Is.Empty);
            Assert.That(core.UnregisterEventBinding("blocked-once"), Is.True);
        });
    }

    [Test]
    public void CopiesBindingsAtRegistrationAndSupportsExplicitUnregistration()
    {
        EventCore core = new EventCore();
        UiEventBinding source = Binding("binding", "original-handler", UiEventPhase.Target);
        core.RegisterEventBinding("target", source);
        source.HandlerId = "mutated-handler";
        source.Kind = UiEventKind.KeyDown;
        source.Phase = UiEventPhase.Bubble;

        EventDispatchResult result = core.DispatchEventData(Event("target", "target"));
        bool removed = core.UnregisterEventBinding("binding");
        bool removedAgain = core.UnregisterEventBinding("binding");
        EventDispatchResult afterRemoval = core.DispatchEventData(Event("target", "target"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Invocations.Select(item => item.HandlerId), Is.EqualTo(new string[] { "original-handler" }));
            Assert.That(removed, Is.True);
            Assert.That(removedAgain, Is.False);
            Assert.That(afterRemoval.Invocations, Is.Empty);
        });
    }

    [Test]
    public void RunsAllTargetListenerKindsInTargetStageOrder()
    {
        EventCore core = new EventCore();
        core.RegisterEventBinding("target", Binding("target-capture", "capture-handler", UiEventPhase.Capture));
        core.RegisterEventBinding("target", Binding("target-target", "target-handler", UiEventPhase.Target));
        core.RegisterEventBinding("target", Binding("target-bubble", "bubble-handler", UiEventPhase.Bubble));

        EventDispatchResult result = core.DispatchEventData(Event("target", "target"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "target-capture", "target-target", "target-bubble" }));
            Assert.That(result.Invocations.Select(item => item.Phase), Is.EqualTo(new UiEventPhase[]
            {
                UiEventPhase.Target, UiEventPhase.Target, UiEventPhase.Target
            }));
        });
    }

    [Test]
    public void ProducesAnIndependentNormalizedEventInTheDispatchResult()
    {
        EventCore core = new EventCore();
        UiEventData source = Event("target", "target", "root");
        source.Pointer = new UiPointerData { PointerType = "TOUCH", ClientX = 2, ClientY = 3 };

        EventDispatchResult result = core.DispatchEventData(source);
        source.Path[0] = "changed";
        source.Pointer.PointerType = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(result.EventData, Is.Not.SameAs(source));
            Assert.That(result.EventData.Path, Is.EqualTo(new string[] { "target", "root" }));
            Assert.That(result.EventData.Pointer!.PointerType, Is.EqualTo("touch"));
        });
    }

    [Test]
    public void RejectsInvalidRegistrationsWithoutChangingExistingState()
    {
        EventCore core = new EventCore();
        core.RegisterEventBinding("target", Binding("valid", "handler", UiEventPhase.Target));
        UiEventBinding duplicate = Binding("valid", "other", UiEventPhase.Target);
        UiEventBinding invalidKind = Binding("invalid-kind", "handler", UiEventPhase.Target);
        invalidKind.Kind = (UiEventKind)999;
        UiEventBinding invalidPhase = Binding("invalid-phase", "handler", UiEventPhase.Target);
        invalidPhase.Phase = (UiEventPhase)999;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => core.RegisterEventBinding("target", null));
            Assert.Throws<ArgumentNullException>(() => core.RegisterEventBinding(null, Binding("null-node", "handler", UiEventPhase.Target)));
            Assert.Throws<ArgumentException>(() => core.RegisterEventBinding(" ", Binding("blank-node", "handler", UiEventPhase.Target)));
            Assert.Throws<ArgumentException>(() => core.RegisterEventBinding("target", Binding(" ", "handler", UiEventPhase.Target)));
            Assert.Throws<ArgumentException>(() => core.RegisterEventBinding("target", Binding("blank-handler", " ", UiEventPhase.Target)));
            Assert.Throws<ArgumentException>(() => core.RegisterEventBinding("target", duplicate));
            Assert.Throws<ArgumentOutOfRangeException>(() => core.RegisterEventBinding("target", invalidKind));
            Assert.Throws<ArgumentOutOfRangeException>(() => core.RegisterEventBinding("target", invalidPhase));
            Assert.Throws<ArgumentException>(() => core.UnregisterEventBinding(" "));
        });

        Assert.That(core.DispatchEventData(Event("target", "target")).Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "valid" }));
    }

    [Test]
    public void RejectsEmptyMismatchedRepeatedAndMalformedPaths()
    {
        EventCore core = new EventCore();
        UiEventData emptyTarget = Event(" ", "target");
        UiEventData emptyPath = Event("target");
        UiEventData mismatched = Event("target", "other", "root");
        UiEventData repeated = Event("target", "target", "root", "target");
        UiEventData blankEntry = Event("target", "target", " ");
        UiEventData nullEntry = Event("target", "target", null!);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => core.DispatchEventData(emptyTarget));
            Assert.Throws<ArgumentException>(() => core.DispatchEventData(emptyPath));
            Assert.Throws<ArgumentException>(() => core.DispatchEventData(mismatched));
            Assert.Throws<ArgumentException>(() => core.DispatchEventData(repeated));
            Assert.Throws<ArgumentException>(() => core.DispatchEventData(blankEntry));
            Assert.Throws<ArgumentNullException>(() => core.DispatchEventData(nullEntry));
        });
    }

    [Test]
    public void RejectsPathsAboveTheDefensiveLengthLimit()
    {
        string[] path = new string[EventCore.MaximumEventPathLength + 1];
        int index = 0;
        while (index < path.Length)
        {
            path[index] = "node-" + index;
            index++;
        }

        UiEventData source = Event(path[0], path);

        Assert.Throws<ArgumentException>(() => new EventCore().DispatchEventData(source));
    }

    private static UiEventBinding Binding(string id, string handlerId, UiEventPhase phase)
    {
        return new UiEventBinding
        {
            Id = id,
            HandlerId = handlerId,
            Kind = UiEventKind.Click,
            Phase = phase
        };
    }

    private static UiEventData Event(string targetId, params string[] path)
    {
        return new UiEventData
        {
            Kind = UiEventKind.Click,
            TargetId = targetId,
            Path = path
        };
    }
}
