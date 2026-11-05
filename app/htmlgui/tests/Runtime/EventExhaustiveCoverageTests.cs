using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class EventExhaustiveCoverageTests
{
    [Test]
    public void RoutesOnlyTheApplicablePayloadForEveryEventKind()
    {
        EventCore core = new EventCore();
        UiEventKind[] kinds = Enum.GetValues<UiEventKind>();

        foreach (UiEventKind kind in kinds)
        {
            UiEventData source = Event(kind, "target", "target", "root");
            source.IsTrusted = true;
            source.Pointer = new UiPointerData { PointerType = "TOUCH", PointerId = 4, ClientX = 5, ClientY = 6 };
            source.Keyboard = new UiKeyboardData { Key = "Esc", Code = "Escape", PrimaryModifier = true };
            source.Input = new UiInputData { Value = Text("input"), InputType = "insertText", Data = "x" };
            source.Clipboard = new UiClipboardData { Data = new UiValueEntry[] { Entry("text/plain", Text("clipboard")) } };
            source.Drag = new UiDragData
            {
                SourceId = "source",
                TargetId = "target",
                Effect = "copy",
                AllowedEffect = "copyMove",
                Data = new UiValueEntry[] { Entry("text/plain", Text("drag")) },
                IsKeyboard = true
            };

            UiEventData result = core.NormalizeEventData(source);

            Assert.That(result.Kind, Is.EqualTo(kind), "kind " + kind);
            Assert.That(result.Pointer != null, Is.EqualTo(IsPointerPayloadKind(kind)), "pointer for " + kind);
            Assert.That(result.Keyboard != null, Is.EqualTo(IsKeyboardPayloadKind(kind)), "keyboard for " + kind);
            Assert.That(result.Input != null, Is.EqualTo(IsInputPayloadKind(kind)), "input for " + kind);
            Assert.That(result.Clipboard != null, Is.EqualTo(IsClipboardPayloadKind(kind)), "clipboard for " + kind);
            Assert.That(result.Drag != null, Is.EqualTo(IsDragPayloadKind(kind)), "drag for " + kind);
            if (result.Pointer != null)
            {
                Assert.That(result.Pointer.PointerType, Is.EqualTo("touch"), "pointer normalization for " + kind);
            }

            if (result.Keyboard != null)
            {
                Assert.That(result.Keyboard.Key, Is.EqualTo(AriaKeyNames.Escape), "key normalization for " + kind);
            }

            if (result.Input != null)
            {
                Assert.That(result.Input.Value.StringValue, Is.EqualTo("input"), "input cloning for " + kind);
            }

            if (result.Clipboard != null)
            {
                Assert.That(result.Clipboard.Operation, Is.EqualTo(ClipboardOperation(kind)), "clipboard operation for " + kind);
            }

            if (result.Drag != null)
            {
                Assert.That(result.Drag.Data.Single().Value.StringValue, Is.EqualTo("drag"), "drag cloning for " + kind);
            }
        }

        Assert.That(kinds, Has.Length.EqualTo(44));
    }

    [Test]
    public void DispatchesEveryEventKindThroughThePublicInterface()
    {
        UiEventKind[] kinds = Enum.GetValues<UiEventKind>();

        foreach (UiEventKind kind in kinds)
        {
            IUiEventDispatcher dispatcher = new UiEventDispatcher();
            RecordingUiEventHandler handler = new RecordingUiEventHandler();
            string id = "binding-" + (int)kind;
            UiEventBinding binding = new UiEventBinding
            {
                Id = id,
                HandlerId = "handler-" + (int)kind,
                Kind = kind,
                Phase = UiEventPhase.Bubble
            };
            UiResult registration = dispatcher.RegisterEventHandler("root", binding, handler);

            UiEventDispatchResult result = dispatcher.DispatchUiEvent(PayloadEvent(kind));

            Assert.That(registration.Success, Is.True, "registration for " + kind);
            Assert.That(result.Success, Is.True, "dispatch for " + kind);
            Assert.That(result.InvokedBindingIds, Is.EqualTo(new string[] { id }), "binding for " + kind);
            Assert.That(handler.Contexts.Single().EventData.Kind, Is.EqualTo(kind), "callback for " + kind);
            Assert.That(handler.Contexts.Single().Phase, Is.EqualTo(UiEventPhase.Bubble), "phase for " + kind);
        }
    }

    [Test]
    public void PreservesDelegatedPhaseAndRegistrationOrderAcrossADeepPath()
    {
        EventCore core = new EventCore();
        core.RegisterEventBinding("off-path", Binding("off-path", UiEventKind.Click, UiEventPhase.Capture));
        core.RegisterEventBinding("root", Binding("root-capture-1", UiEventKind.Click, UiEventPhase.Capture));
        core.RegisterEventBinding("section", Binding("section-capture", UiEventKind.Click, UiEventPhase.Capture));
        core.RegisterEventBinding("root", Binding("root-capture-2", UiEventKind.Click, UiEventPhase.Capture));
        core.RegisterEventBinding("target", Binding("target-capture", UiEventKind.Click, UiEventPhase.Capture));
        core.RegisterEventBinding("target", Binding("target-explicit", UiEventKind.Click, UiEventPhase.Target));
        core.RegisterEventBinding("target", Binding("target-bubble", UiEventKind.Click, UiEventPhase.Bubble));
        core.RegisterEventBinding("section", Binding("wrong-kind", UiEventKind.KeyDown, UiEventPhase.Bubble));
        core.RegisterEventBinding("section", Binding("section-bubble", UiEventKind.Click, UiEventPhase.Bubble));
        core.RegisterEventBinding("root", Binding("root-bubble", UiEventKind.Click, UiEventPhase.Bubble));

        EventDispatchResult result = core.DispatchEventData(Event(UiEventKind.Click, "target", "target", "section", "root"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[]
            {
                "root-capture-1", "root-capture-2", "section-capture",
                "target-capture", "target-explicit", "target-bubble",
                "section-bubble", "root-bubble"
            }));
            Assert.That(result.Invocations.Select(item => item.Phase), Is.EqualTo(new UiEventPhase[]
            {
                UiEventPhase.Capture, UiEventPhase.Capture, UiEventPhase.Capture,
                UiEventPhase.Target, UiEventPhase.Target, UiEventPhase.Target,
                UiEventPhase.Bubble, UiEventPhase.Bubble
            }));
        });
    }

    [Test]
    public void CombinesOncePassiveAndPropagationFlagsWithoutConsumingSkippedBindings()
    {
        EventCore core = new EventCore();
        UiEventBinding passiveOnce = Binding("root-passive-once", UiEventKind.Click, UiEventPhase.Capture);
        passiveOnce.Once = true;
        passiveOnce.Passive = true;
        passiveOnce.PreventDefault = true;
        core.RegisterEventBinding("root", passiveOnce);
        UiEventBinding preventing = Binding("parent-prevent", UiEventKind.Click, UiEventPhase.Capture);
        preventing.PreventDefault = true;
        core.RegisterEventBinding("parent", preventing);
        core.RegisterEventBinding("target", Binding("target", UiEventKind.Click, UiEventPhase.Target));
        UiEventBinding stoppingOnce = Binding("parent-stop-once", UiEventKind.Click, UiEventPhase.Bubble);
        stoppingOnce.Once = true;
        stoppingOnce.StopPropagation = true;
        core.RegisterEventBinding("parent", stoppingOnce);
        core.RegisterEventBinding("parent", Binding("parent-after-stop", UiEventKind.Click, UiEventPhase.Bubble));
        UiEventBinding skippedOnce = Binding("root-skipped-once", UiEventKind.Click, UiEventPhase.Bubble);
        skippedOnce.Once = true;
        core.RegisterEventBinding("root", skippedOnce);

        EventDispatchResult first = core.DispatchEventData(Event(UiEventKind.Click, "target", "target", "parent", "root"));
        EventDispatchResult second = core.DispatchEventData(Event(UiEventKind.Click, "target", "target", "parent", "root"));
        EventDispatchResult third = core.DispatchEventData(Event(UiEventKind.Click, "target", "target", "parent", "root"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[]
            {
                "root-passive-once", "parent-prevent", "target", "parent-stop-once", "parent-after-stop"
            }));
            Assert.That(first.DefaultPrevented, Is.True);
            Assert.That(first.PropagationStopped, Is.True);
            Assert.That(first.Invocations[0].PreventDefault, Is.False);
            Assert.That(first.RemovedBindingIds, Is.EqualTo(new string[] { "root-passive-once", "parent-stop-once" }));
            Assert.That(second.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[]
            {
                "parent-prevent", "target", "parent-after-stop", "root-skipped-once"
            }));
            Assert.That(second.RemovedBindingIds, Is.EqualTo(new string[] { "root-skipped-once" }));
            Assert.That(third.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "parent-prevent", "target", "parent-after-stop" }));
        });
    }

    [Test]
    public void RejectsAStaleAncestorWithoutConsumingAOnceBinding()
    {
        EventCore core = new EventCore();
        core.ConfigureEventSecurity("https://app.example", true, true);
        core.UpdateLiveEventNodes(new string[] { "target", "root" }, 4);
        UiEventBinding once = Binding("once", UiEventKind.Click, UiEventPhase.Bubble);
        once.Once = true;
        core.RegisterEventBinding("root", once);
        EventDispatchMetadata metadata = new EventDispatchMetadata { Origin = "https://app.example", IsMainFrame = true, NodeGeneration = 4 };
        UiEventData source = Event(UiEventKind.Click, "target", "target", "stale-parent", "root");
        source.IsTrusted = true;

        EventDispatchResult rejected = core.DispatchTrustedEventData(source, metadata, 0);
        core.UpdateLiveEventNodes(new string[] { "target", "stale-parent", "root" }, 4);
        EventDispatchResult accepted = core.DispatchTrustedEventData(source, metadata, 1);
        EventDispatchResult afterOnce = core.DispatchTrustedEventData(source, metadata, 2);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionCode, Is.EqualTo("event.stale-node"));
            Assert.That(rejected.Invocations, Is.Empty);
            Assert.That(accepted.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "once" }));
            Assert.That(accepted.RemovedBindingIds, Is.EqualTo(new string[] { "once" }));
            Assert.That(afterOnce.Invocations, Is.Empty);
        });
    }

    [Test]
    public void DebounceKeepsLatestPayloadAndOriginalDelegatedPhasePerBinding()
    {
        EventCore core = new EventCore();
        core.RegisterEventSubscription("root", Subscription("root-capture", UiEventPhase.Capture, 10, 0));
        core.RegisterEventSubscription("target", Subscription("target-bubble", UiEventPhase.Bubble, 10, 0));
        core.RegisterEventSubscription("parent", Subscription("parent-bubble", UiEventPhase.Bubble, 10, 0));
        EventDispatchMetadata metadata = new EventDispatchMetadata { IsMainFrame = true };

        EventDispatchResult first = core.DispatchTrustedEventData(PointerEvent(1), metadata, 0);
        EventDispatchResult latest = core.DispatchTrustedEventData(PointerEvent(9), metadata, 5);
        EventDispatchResult[] early = core.FlushScheduledEvents(14);
        EventDispatchResult[] due = core.FlushScheduledEvents(15);

        Assert.Multiple(() =>
        {
            Assert.That(first.DeferredBindingIds, Is.EqualTo(new string[] { "root-capture", "target-bubble", "parent-bubble" }));
            Assert.That(latest.DeferredBindingIds, Is.EqualTo(new string[] { "root-capture", "target-bubble", "parent-bubble" }));
            Assert.That(early, Is.Empty);
            Assert.That(due.Select(item => item.Invocations.Single().BindingId), Is.EqualTo(new string[] { "root-capture", "target-bubble", "parent-bubble" }));
            Assert.That(due.Select(item => item.Invocations.Single().Phase), Is.EqualTo(new UiEventPhase[]
            {
                UiEventPhase.Capture, UiEventPhase.Target, UiEventPhase.Bubble
            }));
            Assert.That(due.Select(item => item.EventData.Pointer!.ClientX), Is.EqualTo(new double[] { 9, 9, 9 }));
        });
    }

    [Test]
    public void ThrottleWindowsAreIndependentAndReopenAtTheExactBoundary()
    {
        EventCore core = new EventCore();
        core.RegisterEventSubscription("target", Subscription("target", UiEventPhase.Bubble, 0, 20));
        core.RegisterEventSubscription("root", Subscription("root", UiEventPhase.Bubble, 0, 50));
        EventDispatchMetadata metadata = new EventDispatchMetadata { IsMainFrame = true };

        EventDispatchResult first = core.DispatchTrustedEventData(PointerEvent(1), metadata, 100);
        EventDispatchResult middle = core.DispatchTrustedEventData(PointerEvent(2), metadata, 120);
        EventDispatchResult boundary = core.DispatchTrustedEventData(PointerEvent(3), metadata, 150);

        Assert.Multiple(() =>
        {
            Assert.That(first.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "target", "root" }));
            Assert.That(middle.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "target" }));
            Assert.That(middle.ThrottledBindingIds, Is.EqualTo(new string[] { "root" }));
            Assert.That(boundary.Invocations.Select(item => item.BindingId), Is.EqualTo(new string[] { "target", "root" }));
            Assert.That(boundary.EventData.Pointer!.ClientX, Is.EqualTo(3));
        });
    }

    private static UiEventData PayloadEvent(UiEventKind kind)
    {
        UiEventData result = Event(kind, "target", "target", "root");
        if (IsPointerPayloadKind(kind))
        {
            result.Pointer = new UiPointerData { PointerType = "mouse" };
        }
        else if (IsKeyboardPayloadKind(kind))
        {
            result.Keyboard = new UiKeyboardData { Key = "Enter" };
        }
        else if (IsInputPayloadKind(kind))
        {
            result.Input = new UiInputData { Value = Text("value") };
        }
        else if (IsClipboardPayloadKind(kind))
        {
            result.Clipboard = new UiClipboardData { Data = Array.Empty<UiValueEntry>() };
        }
        else if (IsDragPayloadKind(kind))
        {
            result.Drag = new UiDragData { Data = Array.Empty<UiValueEntry>() };
        }

        return result;
    }

    private static bool IsPointerPayloadKind(UiEventKind kind)
    {
        return kind >= UiEventKind.Click && kind <= UiEventKind.Wheel;
    }

    private static bool IsKeyboardPayloadKind(UiEventKind kind)
    {
        return kind == UiEventKind.KeyDown || kind == UiEventKind.KeyUp;
    }

    private static bool IsInputPayloadKind(UiEventKind kind)
    {
        return kind >= UiEventKind.BeforeInput && kind <= UiEventKind.Reset || kind >= UiEventKind.Scroll && kind <= UiEventKind.Toggle;
    }

    private static bool IsClipboardPayloadKind(UiEventKind kind)
    {
        return kind >= UiEventKind.Copy && kind <= UiEventKind.Paste;
    }

    private static bool IsDragPayloadKind(UiEventKind kind)
    {
        return kind >= UiEventKind.DragStart && kind <= UiEventKind.DragEnd;
    }

    private static string ClipboardOperation(UiEventKind kind)
    {
        if (kind == UiEventKind.Copy)
        {
            return "copy";
        }

        if (kind == UiEventKind.Cut)
        {
            return "cut";
        }

        return "paste";
    }

    private static UiEventBinding Binding(string id, UiEventKind kind, UiEventPhase phase)
    {
        return new UiEventBinding { Id = id, HandlerId = id + "-handler", Kind = kind, Phase = phase };
    }

    private static DomEventSubscription Subscription(string id, UiEventPhase phase, int debounce, int throttle)
    {
        return new DomEventSubscription
        {
            Id = id,
            HandlerId = id + "-handler",
            Kind = UiEventKind.PointerMove,
            EventName = DomEventNames.PointerMove,
            Phase = phase,
            HasDebounceMilliseconds = debounce > 0,
            DebounceMilliseconds = debounce,
            HasThrottleMilliseconds = throttle > 0,
            ThrottleMilliseconds = throttle
        };
    }

    private static UiEventData PointerEvent(double value)
    {
        UiEventData result = Event(UiEventKind.PointerMove, "target", "target", "parent", "root");
        result.Pointer = new UiPointerData { PointerType = "mouse", ClientX = value };
        return result;
    }

    private static UiEventData Event(UiEventKind kind, string targetId, params string[] path)
    {
        return new UiEventData { Kind = kind, TargetId = targetId, Path = path };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
