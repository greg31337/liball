using System;
using System.Collections.Generic;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiEventDispatcherTests
{
    [Test]
    public void DispatchesRegisteredInterfaceHandlersInPropagationOrder()
    {
        IUiEventDispatcher dispatcher = new UiEventDispatcher();
        RecordingUiEventHandler rootCapture = new RecordingUiEventHandler();
        RecordingUiEventHandler target = new RecordingUiEventHandler();
        RecordingUiEventHandler rootBubble = new RecordingUiEventHandler();
        dispatcher.RegisterEventHandler("root", Binding("root-capture", "capture-handler", UiEventPhase.Capture), rootCapture);
        dispatcher.RegisterEventHandler("target", Binding("target", "target-handler", UiEventPhase.Bubble), target);
        dispatcher.RegisterEventHandler("root", Binding("root-bubble", "bubble-handler", UiEventPhase.Bubble), rootBubble);

        UiEventDispatchResult result = dispatcher.DispatchUiEvent(Event());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.InvokedBindingIds, Is.EqualTo(new string[] { "root-capture", "target", "root-bubble" }));
            Assert.That(result.InvokedHandlerIds, Is.EqualTo(new string[] { "capture-handler", "target-handler", "bubble-handler" }));
            Assert.That(rootCapture.Contexts.Single().CurrentTargetId, Is.EqualTo("root"));
            Assert.That(rootCapture.Contexts.Single().Phase, Is.EqualTo(UiEventPhase.Capture));
            Assert.That(target.Contexts.Single().CurrentTargetId, Is.EqualTo("target"));
            Assert.That(target.Contexts.Single().Phase, Is.EqualTo(UiEventPhase.Target));
            Assert.That(rootBubble.Contexts.Single().Phase, Is.EqualTo(UiEventPhase.Bubble));
        });
    }

    [Test]
    public void AggregatesHandlerOutcomesValuesAndDiagnostics()
    {
        IUiEventDispatcher dispatcher = new UiEventDispatcher();
        RecordingUiEventHandler first = new RecordingUiEventHandler();
        first.Result = new UiEventHandlerResult
        {
            Success = false,
            Handled = true,
            PreventDefault = true,
            Value = new UiValue { Kind = UiValueKind.String, StringValue = "result" },
            Diagnostics = new UiDiagnostic[]
            {
                new UiDiagnostic { Severity = UiDiagnosticSeverity.Warning, Code = "handler.warning", Message = "Warning", Location = "/handler" }
            }
        };
        RecordingUiEventHandler second = new RecordingUiEventHandler();
        dispatcher.RegisterEventHandler("target", Binding("first", "first-handler", UiEventPhase.Target), first);
        dispatcher.RegisterEventHandler("target", Binding("second", "second-handler", UiEventPhase.Target), second);

        UiEventDispatchResult result = dispatcher.DispatchUiEvent(Event());
        first.Result.Value!.StringValue = "mutated";
        first.Result.Diagnostics[0].Message = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Handled, Is.True);
            Assert.That(result.DefaultPrevented, Is.True);
            Assert.That(result.HandlerResults, Has.Length.EqualTo(2));
            Assert.That(result.HandlerResults[0], Is.Not.SameAs(first.Result));
            Assert.That(result.HandlerResults[0].Value!.StringValue, Is.EqualTo("result"));
            Assert.That(result.HandlerResults[0].Diagnostics[0].Message, Is.EqualTo("Warning"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Is.EqualTo(new string[] { "handler.warning" }));
        });
    }

    [Test]
    public void HandlerStopCompletesTheCurrentNodeAndSkipsAncestorCallbacks()
    {
        IUiEventDispatcher dispatcher = new UiEventDispatcher();
        RecordingUiEventHandler stopping = new RecordingUiEventHandler();
        stopping.Result.StopPropagation = true;
        RecordingUiEventHandler sameNode = new RecordingUiEventHandler();
        RecordingUiEventHandler ancestor = new RecordingUiEventHandler();
        dispatcher.RegisterEventHandler("target", Binding("stop", "stop-handler", UiEventPhase.Target), stopping);
        dispatcher.RegisterEventHandler("target", Binding("same", "same-handler", UiEventPhase.Bubble), sameNode);
        dispatcher.RegisterEventHandler("root", Binding("ancestor", "ancestor-handler", UiEventPhase.Bubble), ancestor);

        UiEventDispatchResult result = dispatcher.DispatchUiEvent(Event());

        Assert.Multiple(() =>
        {
            Assert.That(result.PropagationStopped, Is.True);
            Assert.That(result.InvokedBindingIds, Is.EqualTo(new string[] { "stop", "same" }));
            Assert.That(sameNode.Contexts, Has.Count.EqualTo(1));
            Assert.That(ancestor.Contexts, Is.Empty);
        });
    }

    [Test]
    public void DynamicStopRestoresAPlannedOnceBindingThatWasNotInvoked()
    {
        IUiEventDispatcher dispatcher = new UiEventDispatcher();
        RecordingUiEventHandler target = new RecordingUiEventHandler();
        target.Result.StopPropagation = true;
        RecordingUiEventHandler once = new RecordingUiEventHandler();
        UiEventBinding onceBinding = Binding("once", "once-handler", UiEventPhase.Bubble);
        onceBinding.Once = true;
        dispatcher.RegisterEventHandler("target", Binding("target", "target-handler", UiEventPhase.Target), target);
        dispatcher.RegisterEventHandler("root", onceBinding, once);

        UiEventDispatchResult stopped = dispatcher.DispatchUiEvent(Event());
        target.Result.StopPropagation = false;
        UiEventDispatchResult delivered = dispatcher.DispatchUiEvent(Event());
        UiEventDispatchResult removed = dispatcher.DispatchUiEvent(Event());

        Assert.Multiple(() =>
        {
            Assert.That(stopped.InvokedBindingIds, Is.EqualTo(new string[] { "target" }));
            Assert.That(stopped.RemovedBindingIds, Is.Empty);
            Assert.That(delivered.InvokedBindingIds, Is.EqualTo(new string[] { "target", "once" }));
            Assert.That(delivered.RemovedBindingIds, Is.EqualTo(new string[] { "once" }));
            Assert.That(removed.InvokedBindingIds, Is.EqualTo(new string[] { "target" }));
            Assert.That(once.Contexts, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void PassiveCallbacksCannotPreventDefault()
    {
        IUiEventDispatcher dispatcher = new UiEventDispatcher();
        RecordingUiEventHandler handler = new RecordingUiEventHandler();
        handler.Result.PreventDefault = true;
        UiEventBinding binding = Binding("passive", "handler", UiEventPhase.Target);
        binding.Passive = true;
        dispatcher.RegisterEventHandler("target", binding, handler);

        UiEventDispatchResult result = dispatcher.DispatchUiEvent(Event());

        Assert.Multiple(() =>
        {
            Assert.That(result.DefaultPrevented, Is.False);
            Assert.That(handler.Contexts.Single().Passive, Is.True);
        });
    }

    [Test]
    public void GivesEveryCallbackAndTheResultIndependentNormalizedEventData()
    {
        IUiEventDispatcher dispatcher = new UiEventDispatcher();
        MutatingUiEventHandler mutating = new MutatingUiEventHandler();
        RecordingUiEventHandler observing = new RecordingUiEventHandler();
        dispatcher.RegisterEventHandler("target", Binding("mutating", "mutating-handler", UiEventPhase.Target), mutating);
        dispatcher.RegisterEventHandler("target", Binding("observing", "observing-handler", UiEventPhase.Target), observing);
        UiEventData source = Event();
        source.Pointer = new UiPointerData { PointerType = "PEN", ClientX = 4 };

        UiEventDispatchResult result = dispatcher.DispatchUiEvent(source);

        Assert.Multiple(() =>
        {
            Assert.That(observing.Contexts.Single().EventData.TargetId, Is.EqualTo("target"));
            Assert.That(observing.Contexts.Single().EventData.Path[0], Is.EqualTo("target"));
            Assert.That(observing.Contexts.Single().EventData.Pointer!.PointerType, Is.EqualTo("pen"));
            Assert.That(result.EventData.TargetId, Is.EqualTo("target"));
            Assert.That(result.EventData.Path[0], Is.EqualTo("target"));
            Assert.That(result.EventData.Pointer!.PointerType, Is.EqualTo("pen"));
            Assert.That(source.Pointer.PointerType, Is.EqualTo("PEN"));
        });
    }

    [Test]
    public void ReportsNullCallbackResultsAndMalformedDiagnostics()
    {
        IUiEventDispatcher dispatcher = new UiEventDispatcher();
        dispatcher.RegisterEventHandler("target", Binding("null-result", "null-handler", UiEventPhase.Target), new NullUiEventHandler());
        RecordingUiEventHandler malformed = new RecordingUiEventHandler();
        malformed.Result.Diagnostics = new UiDiagnostic[] { null! };
        dispatcher.RegisterEventHandler("target", Binding("null-diagnostic", "diagnostic-handler", UiEventPhase.Target), malformed);

        UiEventDispatchResult result = dispatcher.DispatchUiEvent(Event());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code), Is.EqualTo(new string[] { "event.null-handler-result", "event.null-handler-diagnostic" }));
            Assert.That(result.HandlerResults, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ReturnsExceptionlessRegistrationAndUnregistrationFailures()
    {
        IUiEventDispatcher dispatcher = new UiEventDispatcher();
        RecordingUiEventHandler handler = new RecordingUiEventHandler();
        UiEventBinding invalidKind = Binding("kind", "handler", UiEventPhase.Target);
        invalidKind.Kind = (UiEventKind)999;
        UiEventBinding invalidPhase = Binding("phase", "handler", UiEventPhase.Target);
        invalidPhase.Phase = (UiEventPhase)999;
        UiEventBinding valid = Binding("valid", "handler", UiEventPhase.Target);
        UiResult registered = dispatcher.RegisterEventHandler("target", valid, handler);
        valid.HandlerId = "mutated";

        UiResult[] failures = new UiResult[]
        {
            dispatcher.RegisterEventHandler(null, Binding("node", "handler", UiEventPhase.Target), handler),
            dispatcher.RegisterEventHandler("target", null, handler),
            dispatcher.RegisterEventHandler("target", Binding(" ", "handler", UiEventPhase.Target), handler),
            dispatcher.RegisterEventHandler("target", Binding("handler-id", " ", UiEventPhase.Target), handler),
            dispatcher.RegisterEventHandler("target", invalidKind, handler),
            dispatcher.RegisterEventHandler("target", invalidPhase, handler),
            dispatcher.RegisterEventHandler("target", Binding("null-handler", "handler", UiEventPhase.Target), null),
            dispatcher.RegisterEventHandler("target", Binding("valid", "duplicate", UiEventPhase.Target), handler),
            dispatcher.UnregisterEventHandler("missing"),
            dispatcher.UnregisterEventHandler(" ")
        };
        UiEventDispatchResult dispatch = dispatcher.DispatchUiEvent(Event());
        UiResult removed = dispatcher.UnregisterEventHandler("valid");

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(failures, Has.All.Matches<UiResult>(item => !item.Success));
            Assert.That(failures.Select(item => item.Diagnostics.Single().Code), Is.EqualTo(new string[]
            {
                "event.invalid-node", "event.null-binding", "event.invalid-binding", "event.invalid-handler-id",
                "event.invalid-kind", "event.invalid-phase", "event.null-handler", "event.duplicate-binding",
                "event.unknown-binding", "event.invalid-binding"
            }));
            Assert.That(dispatch.InvokedHandlerIds, Is.EqualTo(new string[] { "handler" }));
            Assert.That(removed.Success, Is.True);
            Assert.That(dispatcher.DispatchUiEvent(Event()).InvokedHandlerIds, Is.Empty);
        });
    }

    [Test]
    public void PropagatesMalformedEventsAndHandlerExceptions()
    {
        IUiEventDispatcher dispatcher = new UiEventDispatcher();
        dispatcher.RegisterEventHandler("target", Binding("throwing", "throwing-handler", UiEventPhase.Target), new ThrowingUiEventHandler());
        UiEventData malformed = Event();
        malformed.Path = Array.Empty<string>();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => dispatcher.DispatchUiEvent(malformed));
            Assert.Throws<InvalidOperationException>(() => dispatcher.DispatchUiEvent(Event()));
        });
    }

    private static UiEventBinding Binding(string id, string handlerId, UiEventPhase phase)
    {
        return new UiEventBinding { Id = id, HandlerId = handlerId, Kind = UiEventKind.Click, Phase = phase };
    }

    private static UiEventData Event()
    {
        return new UiEventData
        {
            Kind = UiEventKind.Click,
            TargetId = "target",
            Path = new string[] { "target", "root" },
            IsTrusted = true
        };
    }
}

public class RecordingUiEventHandler : IUiEventHandler
{
    public UiEventHandlerResult Result = new UiEventHandlerResult();
    public List<UiEventHandlerContext> Contexts = new List<UiEventHandlerContext>();

    public UiEventHandlerResult HandleUiEvent(UiEventHandlerContext context)
    {
        Contexts.Add(context);
        return Result;
    }
}

public class MutatingUiEventHandler : IUiEventHandler
{
    public UiEventHandlerResult HandleUiEvent(UiEventHandlerContext context)
    {
        context.EventData.TargetId = "mutated";
        context.EventData.Path[0] = "mutated";
        if (context.EventData.Pointer != null)
        {
            context.EventData.Pointer.PointerType = "mutated";
        }

        return new UiEventHandlerResult { Success = true };
    }
}

public class NullUiEventHandler : IUiEventHandler
{
    public UiEventHandlerResult HandleUiEvent(UiEventHandlerContext context)
    {
        return null!;
    }
}

public class ThrowingUiEventHandler : IUiEventHandler
{
    public UiEventHandlerResult HandleUiEvent(UiEventHandlerContext context)
    {
        throw new InvalidOperationException("handler failed");
    }
}
