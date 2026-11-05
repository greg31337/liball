using System;
using System.Collections.Generic;

namespace htmlgui;

/// <summary>Provides deterministic normalized event dispatch through registered handler interfaces.</summary>
public class UiEventDispatcher : IUiEventDispatcher
{
    internal EventCore Core;
    internal List<string> NodeIds;
    internal List<UiEventBinding> Bindings;
    internal List<IUiEventHandler> Handlers;

    /// <summary>Initializes an empty event dispatcher.</summary>
    public UiEventDispatcher()
    {
        Core = new EventCore();
        NodeIds = new List<string>();
        Bindings = new List<UiEventBinding>();
        Handlers = new List<IUiEventHandler>();
    }

    /// <summary>Registers an event binding and its callback for one stable node.</summary>
    public UiResult RegisterEventHandler(string? nodeId, UiEventBinding? binding, IUiEventHandler? handler)
    {
        UiResult validation = ValidateEventHandlerRegistration(nodeId, binding, handler);
        if (!validation.Success || binding == null || handler == null || nodeId == null)
        {
            return validation;
        }

        Core.RegisterEventBinding(nodeId, binding);
        NodeIds.Add(nodeId);
        Bindings.Add(Core.CopyEventBinding(binding));
        Handlers.Add(handler);
        return validation;
    }

    /// <summary>Unregisters an event binding and its callback.</summary>
    public UiResult UnregisterEventHandler(string? bindingId)
    {
        if (string.IsNullOrWhiteSpace(bindingId))
        {
            return CreateEventOperationFailure("event.invalid-binding", "A binding identifier is required.", "/event/bindingId");
        }

        int index = FindPublicEventBindingIndex(bindingId);
        if (index < 0)
        {
            return CreateEventOperationFailure("event.unknown-binding", "The event binding is not registered.", "/event/bindingId");
        }

        Core.UnregisterEventBinding(bindingId);
        RemovePublicEventBinding(index);
        return new UiResult { Success = true };
    }

    /// <summary>Normalizes and dispatches an event to matching handlers.</summary>
    public UiEventDispatchResult DispatchUiEvent(UiEventData? eventData)
    {
        EventDispatchResult plan = Core.DispatchEventData(eventData);
        List<string> invokedBindingIds = new List<string>();
        List<string> invokedHandlerIds = new List<string>();
        List<string> removedBindingIds = new List<string>();
        List<UiEventHandlerResult> handlerResults = new List<UiEventHandlerResult>();
        List<UiDiagnostic> diagnostics = new List<UiDiagnostic>();
        bool success = true;
        bool handled = false;
        bool defaultPrevented = false;
        bool propagationStopped = false;
        string? callbackStopNodeId = null;
        int invocationIndex = 0;
        while (invocationIndex < plan.Invocations.Length)
        {
            EventHandlerInvocation invocation = plan.Invocations[invocationIndex];
            if (callbackStopNodeId != null && !ValueComparer.TextEquals(callbackStopNodeId, invocation.NodeId))
            {
                break;
            }

            if (invocation.PreventDefault)
            {
                defaultPrevented = true;
            }

            if (invocation.StopPropagation)
            {
                callbackStopNodeId = invocation.NodeId;
                propagationStopped = true;
            }

            int handlerIndex = FindPublicEventBindingIndex(invocation.BindingId);
            if (handlerIndex < 0)
            {
                success = false;
                diagnostics.Add(CreateEventDiagnostic("event.missing-handler", "The planned event handler is no longer registered.", "/event/bindings/" + invocation.BindingId));
            }
            else
            {
                UiEventHandlerContext context = new UiEventHandlerContext();
                context.EventData = Core.NormalizeEventData(plan.EventData);
                context.BindingId = invocation.BindingId;
                context.HandlerId = invocation.HandlerId;
                context.CurrentTargetId = invocation.NodeId;
                context.Phase = invocation.Phase;
                context.Passive = invocation.Passive;
                UiEventHandlerResult? callbackResult = Handlers[handlerIndex].HandleUiEvent(context);
                UiEventHandlerResult copiedResult;
                if (callbackResult == null)
                {
                    copiedResult = new UiEventHandlerResult();
                    copiedResult.Success = false;
                    copiedResult.Diagnostics = new UiDiagnostic[]
                    {
                        CreateEventDiagnostic("event.null-handler-result", "An event handler returned a null result.", "/event/handlers/" + invocation.HandlerId)
                    };
                }
                else
                {
                    copiedResult = CopyEventHandlerResult(callbackResult);
                }

                invokedBindingIds.Add(invocation.BindingId);
                invokedHandlerIds.Add(invocation.HandlerId);
                handlerResults.Add(copiedResult);
                AddEventDiagnostics(diagnostics, copiedResult.Diagnostics);
                if (!copiedResult.Success)
                {
                    success = false;
                }

                if (copiedResult.Handled)
                {
                    handled = true;
                }

                if (copiedResult.PreventDefault && !invocation.Passive)
                {
                    defaultPrevented = true;
                }

                if (copiedResult.StopPropagation)
                {
                    callbackStopNodeId = invocation.NodeId;
                    propagationStopped = true;
                }
            }

            invocationIndex++;
        }

        ReconcileEventOnceBindings(plan.RemovedBindingIds, invokedBindingIds, removedBindingIds);
        UiEventDispatchResult result = new UiEventDispatchResult();
        result.Success = success;
        result.Handled = handled;
        result.DefaultPrevented = defaultPrevented;
        result.PropagationStopped = propagationStopped;
        result.EventData = Core.NormalizeEventData(plan.EventData);
        result.InvokedBindingIds = invokedBindingIds.ToArray();
        result.InvokedHandlerIds = invokedHandlerIds.ToArray();
        result.RemovedBindingIds = removedBindingIds.ToArray();
        result.HandlerResults = handlerResults.ToArray();
        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    internal UiResult ValidateEventHandlerRegistration(string? nodeId, UiEventBinding? binding, IUiEventHandler? handler)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return CreateEventOperationFailure("event.invalid-node", "A stable node identifier is required.", "/event/nodeId");
        }

        if (binding == null)
        {
            return CreateEventOperationFailure("event.null-binding", "An event binding is required.", "/event/binding");
        }

        if (string.IsNullOrWhiteSpace(binding.Id))
        {
            return CreateEventOperationFailure("event.invalid-binding", "A stable binding identifier is required.", "/event/binding/id");
        }

        if (string.IsNullOrWhiteSpace(binding.HandlerId))
        {
            return CreateEventOperationFailure("event.invalid-handler-id", "A stable handler identifier is required.", "/event/binding/handlerId");
        }

        if (!IsSupportedEventKind(binding.Kind))
        {
            return CreateEventOperationFailure("event.invalid-kind", "The event kind is not supported.", "/event/binding/kind");
        }

        if (!IsSupportedEventPhase(binding.Phase))
        {
            return CreateEventOperationFailure("event.invalid-phase", "The event phase is not supported.", "/event/binding/phase");
        }

        if (handler == null)
        {
            return CreateEventOperationFailure("event.null-handler", "An event handler is required.", "/event/handler");
        }

        if (FindPublicEventBindingIndex(binding.Id) >= 0)
        {
            return CreateEventOperationFailure("event.duplicate-binding", "The event binding identifier is already registered.", "/event/binding/id");
        }

        return new UiResult { Success = true };
    }

    internal bool IsSupportedEventKind(UiEventKind kind)
    {
        switch (kind)
        {
            case UiEventKind.Click:
            case UiEventKind.DoubleClick:
            case UiEventKind.ContextMenu:
            case UiEventKind.PointerDown:
            case UiEventKind.PointerUp:
            case UiEventKind.PointerMove:
            case UiEventKind.PointerEnter:
            case UiEventKind.PointerLeave:
            case UiEventKind.PointerCancel:
            case UiEventKind.MouseDown:
            case UiEventKind.MouseUp:
            case UiEventKind.MouseMove:
            case UiEventKind.MouseEnter:
            case UiEventKind.MouseLeave:
            case UiEventKind.Wheel:
            case UiEventKind.KeyDown:
            case UiEventKind.KeyUp:
            case UiEventKind.Focus:
            case UiEventKind.Blur:
            case UiEventKind.FocusIn:
            case UiEventKind.FocusOut:
            case UiEventKind.BeforeInput:
            case UiEventKind.Input:
            case UiEventKind.Change:
            case UiEventKind.CompositionStart:
            case UiEventKind.CompositionUpdate:
            case UiEventKind.CompositionEnd:
            case UiEventKind.SelectionChange:
            case UiEventKind.Submit:
            case UiEventKind.Reset:
            case UiEventKind.DragStart:
            case UiEventKind.Drag:
            case UiEventKind.DragEnter:
            case UiEventKind.DragOver:
            case UiEventKind.DragLeave:
            case UiEventKind.Drop:
            case UiEventKind.DragEnd:
            case UiEventKind.Copy:
            case UiEventKind.Cut:
            case UiEventKind.Paste:
            case UiEventKind.Scroll:
            case UiEventKind.Resize:
            case UiEventKind.VisibilityChange:
            case UiEventKind.Toggle:
                return true;
            default:
                return false;
        }
    }

    internal bool IsSupportedEventPhase(UiEventPhase phase)
    {
        switch (phase)
        {
            case UiEventPhase.Capture:
            case UiEventPhase.Target:
            case UiEventPhase.Bubble:
                return true;
            default:
                return false;
        }
    }

    internal int FindPublicEventBindingIndex(string? bindingId)
    {
        int index = 0;
        while (bindingId != null && index < Bindings.Count)
        {
            if (ValueComparer.TextEquals(Bindings[index].Id, bindingId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void ReconcileEventOnceBindings(string[] plannedRemovalIds, List<string> invokedBindingIds, List<string> removedBindingIds)
    {
        int removalIndex = 0;
        while (removalIndex < plannedRemovalIds.Length)
        {
            string bindingId = plannedRemovalIds[removalIndex];
            int publicIndex = FindPublicEventBindingIndex(bindingId);
            if (publicIndex >= 0)
            {
                if (ContainsInvokedEventBinding(invokedBindingIds, bindingId))
                {
                    removedBindingIds.Add(bindingId);
                    RemovePublicEventBinding(publicIndex);
                }
                else
                {
                    Core.RegisterEventBinding(NodeIds[publicIndex], Bindings[publicIndex]);
                }
            }

            removalIndex++;
        }
    }

    internal bool ContainsInvokedEventBinding(List<string> bindingIds, string bindingId)
    {
        int index = 0;
        while (index < bindingIds.Count)
        {
            if (ValueComparer.TextEquals(bindingIds[index], bindingId))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void RemovePublicEventBinding(int index)
    {
        NodeIds.RemoveAt(index);
        Bindings.RemoveAt(index);
        Handlers.RemoveAt(index);
    }

    internal UiEventHandlerResult CopyEventHandlerResult(UiEventHandlerResult source)
    {
        UiEventHandlerResult result = new UiEventHandlerResult();
        result.Success = source.Success;
        result.Handled = source.Handled;
        result.PreventDefault = source.PreventDefault;
        result.StopPropagation = source.StopPropagation;
        if (source.Value != null)
        {
            result.Value = ValueJsonCodec.CloneValue(source.Value, EventCore.MaximumValueDepth, EventCore.MaximumValueCollectionCount, EventCore.MaximumValueStringLength, EventCore.MaximumValueBinaryLength);
        }

        result.Diagnostics = CopyEventDiagnostics(source.Diagnostics);
        return result;
    }

    internal UiDiagnostic[] CopyEventDiagnostics(UiDiagnostic[]? source)
    {
        UiDiagnostic[] diagnostics = source ?? Array.Empty<UiDiagnostic>();
        List<UiDiagnostic> result = new List<UiDiagnostic>();
        int index = 0;
        while (index < diagnostics.Length)
        {
            UiDiagnostic? diagnostic = diagnostics[index];
            if (diagnostic == null)
            {
                result.Add(CreateEventDiagnostic("event.null-handler-diagnostic", "An event handler returned a null diagnostic.", "/event/handlers/diagnostics/" + index));
            }
            else
            {
                UiDiagnostic copy = new UiDiagnostic();
                copy.Severity = diagnostic.Severity;
                copy.Code = diagnostic.Code ?? string.Empty;
                copy.Message = diagnostic.Message ?? string.Empty;
                copy.Location = diagnostic.Location;
                result.Add(copy);
            }

            index++;
        }

        return result.ToArray();
    }

    internal void AddEventDiagnostics(List<UiDiagnostic> target, UiDiagnostic[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            target.Add(source[index]);
            index++;
        }
    }

    internal UiResult CreateEventOperationFailure(string code, string message, string location)
    {
        return new UiResult
        {
            Success = false,
            Diagnostics = new UiDiagnostic[] { CreateEventDiagnostic(code, message, location) }
        };
    }

    internal UiDiagnostic CreateEventDiagnostic(string code, string message, string location)
    {
        return new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Error,
            Code = code,
            Message = message,
            Location = location
        };
    }
}
