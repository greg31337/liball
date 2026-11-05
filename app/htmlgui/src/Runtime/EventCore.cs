using System;
using System.Collections.Generic;

namespace htmlgui;

internal class EventHandlerRegistration
{
    internal string NodeId = string.Empty;
    internal UiEventBinding Binding = new UiEventBinding();
    internal bool HasDebounceMilliseconds;
    internal int DebounceMilliseconds;
    internal bool HasThrottleMilliseconds;
    internal int ThrottleMilliseconds;
    internal bool HasLastDispatchMilliseconds;
    internal long LastDispatchMilliseconds;
    internal bool HasPendingEvent;
    internal long PendingDueMilliseconds;
    internal UiEventData? PendingEvent;
    internal EventDispatchMetadata? PendingMetadata;
}

internal class EventHandlerInvocation
{
    internal string BindingId = string.Empty;
    internal string HandlerId = string.Empty;
    internal string NodeId = string.Empty;
    internal UiEventPhase Phase;
    internal bool PreventDefault;
    internal bool StopPropagation;
    internal bool Passive;
}

internal class EventDispatchResult
{
    internal UiEventData EventData = new UiEventData();
    internal EventHandlerInvocation[] Invocations = Array.Empty<EventHandlerInvocation>();
    internal string[] RemovedBindingIds = Array.Empty<string>();
    internal string[] DeferredBindingIds = Array.Empty<string>();
    internal string[] ThrottledBindingIds = Array.Empty<string>();
    internal bool DefaultPrevented;
    internal bool PropagationStopped;
    internal bool Accepted = true;
    internal string? RejectionCode;
    internal string Origin = string.Empty;
    internal bool IsMainFrame;
    internal bool IsTrustedOrigin;
}

internal class EventDispatchMetadata
{
    internal string Origin = string.Empty;
    internal bool IsMainFrame = true;
    internal int NodeGeneration;
}

internal class EventCore
{
    internal const int MaximumValueDepth = 64;
    internal const int MaximumValueCollectionCount = 10000;
    internal const int MaximumValueStringLength = 1048576;
    internal const int MaximumValueBinaryLength = 1048576;
    internal const int MaximumEventPathLength = 1024;
    internal List<EventHandlerRegistration> Registrations = new List<EventHandlerRegistration>();
    internal string ExpectedEventOrigin = string.Empty;
    internal bool RequireEventMainFrame;
    internal bool RequireTrustedEvent;
    internal bool HasLiveEventNodes;
    internal string[] LiveEventNodeIds = Array.Empty<string>();
    internal int LiveEventNodeGeneration;
    internal bool HasEventClock;
    internal long LastEventClockMilliseconds;

    internal UiEventData NormalizeEventData(UiEventData? eventData)
    {
        if (eventData == null)
        {
            throw new ArgumentNullException("eventData");
        }

        ConvertEventKind(eventData.Kind);
        UiEventData result = new UiEventData();
        result.Kind = eventData.Kind;
        result.TargetId = eventData.TargetId ?? string.Empty;
        result.Path = CopyEventPath(eventData.Path);
        result.IsTrusted = eventData.IsTrusted;

        switch (eventData.Kind)
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
                if (eventData.Pointer != null)
                {
                    result.Pointer = NormalizePointerData(eventData.Pointer, eventData.Kind);
                }

                break;
            case UiEventKind.KeyDown:
            case UiEventKind.KeyUp:
                if (eventData.Keyboard != null)
                {
                    result.Keyboard = NormalizeKeyboardData(eventData.Keyboard);
                }

                break;
            case UiEventKind.Focus:
            case UiEventKind.Blur:
            case UiEventKind.FocusIn:
            case UiEventKind.FocusOut:
                break;
            case UiEventKind.BeforeInput:
            case UiEventKind.Input:
            case UiEventKind.Change:
            case UiEventKind.CompositionStart:
            case UiEventKind.CompositionUpdate:
            case UiEventKind.CompositionEnd:
            case UiEventKind.SelectionChange:
            case UiEventKind.Submit:
            case UiEventKind.Reset:
            case UiEventKind.Scroll:
            case UiEventKind.Resize:
            case UiEventKind.VisibilityChange:
            case UiEventKind.Toggle:
                if (eventData.Input != null)
                {
                    result.Input = NormalizeInputData(eventData.Input, eventData.Kind);
                }

                break;
            case UiEventKind.DragStart:
            case UiEventKind.Drag:
            case UiEventKind.DragEnter:
            case UiEventKind.DragOver:
            case UiEventKind.DragLeave:
            case UiEventKind.Drop:
            case UiEventKind.DragEnd:
                if (eventData.Drag != null)
                {
                    result.Drag = NormalizeDragData(eventData.Drag);
                }

                break;
            case UiEventKind.Copy:
            case UiEventKind.Cut:
            case UiEventKind.Paste:
                if (eventData.Clipboard != null)
                {
                    result.Clipboard = NormalizeClipboardData(eventData.Clipboard, eventData.Kind);
                }

                break;
        }

        return result;
    }

    internal UiEventKind ConvertEventName(string? eventName)
    {
        string checkedName = Guard.ArgumentNotWhiteSpace(eventName, "eventName");
        switch (checkedName)
        {
            case DomEventNames.Click:
                return UiEventKind.Click;
            case DomEventNames.DoubleClick:
                return UiEventKind.DoubleClick;
            case DomEventNames.ContextMenu:
                return UiEventKind.ContextMenu;
            case DomEventNames.PointerDown:
                return UiEventKind.PointerDown;
            case DomEventNames.PointerUp:
                return UiEventKind.PointerUp;
            case DomEventNames.PointerMove:
                return UiEventKind.PointerMove;
            case DomEventNames.PointerEnter:
                return UiEventKind.PointerEnter;
            case DomEventNames.PointerLeave:
                return UiEventKind.PointerLeave;
            case DomEventNames.PointerCancel:
                return UiEventKind.PointerCancel;
            case DomEventNames.MouseDown:
                return UiEventKind.MouseDown;
            case DomEventNames.MouseUp:
                return UiEventKind.MouseUp;
            case DomEventNames.MouseMove:
                return UiEventKind.MouseMove;
            case DomEventNames.MouseEnter:
                return UiEventKind.MouseEnter;
            case DomEventNames.MouseLeave:
                return UiEventKind.MouseLeave;
            case DomEventNames.Wheel:
                return UiEventKind.Wheel;
            case DomEventNames.KeyDown:
                return UiEventKind.KeyDown;
            case DomEventNames.KeyUp:
                return UiEventKind.KeyUp;
            case DomEventNames.Focus:
                return UiEventKind.Focus;
            case DomEventNames.Blur:
                return UiEventKind.Blur;
            case DomEventNames.FocusIn:
                return UiEventKind.FocusIn;
            case DomEventNames.FocusOut:
                return UiEventKind.FocusOut;
            case DomEventNames.BeforeInput:
                return UiEventKind.BeforeInput;
            case DomEventNames.Input:
                return UiEventKind.Input;
            case DomEventNames.Change:
                return UiEventKind.Change;
            case DomEventNames.CompositionStart:
                return UiEventKind.CompositionStart;
            case DomEventNames.CompositionUpdate:
                return UiEventKind.CompositionUpdate;
            case DomEventNames.CompositionEnd:
                return UiEventKind.CompositionEnd;
            case DomEventNames.SelectionChange:
                return UiEventKind.SelectionChange;
            case DomEventNames.Submit:
                return UiEventKind.Submit;
            case DomEventNames.Reset:
                return UiEventKind.Reset;
            case DomEventNames.DragStart:
                return UiEventKind.DragStart;
            case DomEventNames.Drag:
                return UiEventKind.Drag;
            case DomEventNames.DragEnter:
                return UiEventKind.DragEnter;
            case DomEventNames.DragOver:
                return UiEventKind.DragOver;
            case DomEventNames.DragLeave:
                return UiEventKind.DragLeave;
            case DomEventNames.Drop:
                return UiEventKind.Drop;
            case DomEventNames.DragEnd:
                return UiEventKind.DragEnd;
            case DomEventNames.Copy:
                return UiEventKind.Copy;
            case DomEventNames.Cut:
                return UiEventKind.Cut;
            case DomEventNames.Paste:
                return UiEventKind.Paste;
            case DomEventNames.Scroll:
                return UiEventKind.Scroll;
            case DomEventNames.Resize:
                return UiEventKind.Resize;
            case DomEventNames.VisibilityChange:
                return UiEventKind.VisibilityChange;
            case DomEventNames.Toggle:
                return UiEventKind.Toggle;
            default:
                throw new ArgumentException("The DOM event name is not supported.", "eventName");
        }
    }

    internal string ConvertEventKind(UiEventKind eventKind)
    {
        switch (eventKind)
        {
            case UiEventKind.Click:
                return DomEventNames.Click;
            case UiEventKind.DoubleClick:
                return DomEventNames.DoubleClick;
            case UiEventKind.ContextMenu:
                return DomEventNames.ContextMenu;
            case UiEventKind.PointerDown:
                return DomEventNames.PointerDown;
            case UiEventKind.PointerUp:
                return DomEventNames.PointerUp;
            case UiEventKind.PointerMove:
                return DomEventNames.PointerMove;
            case UiEventKind.PointerEnter:
                return DomEventNames.PointerEnter;
            case UiEventKind.PointerLeave:
                return DomEventNames.PointerLeave;
            case UiEventKind.PointerCancel:
                return DomEventNames.PointerCancel;
            case UiEventKind.MouseDown:
                return DomEventNames.MouseDown;
            case UiEventKind.MouseUp:
                return DomEventNames.MouseUp;
            case UiEventKind.MouseMove:
                return DomEventNames.MouseMove;
            case UiEventKind.MouseEnter:
                return DomEventNames.MouseEnter;
            case UiEventKind.MouseLeave:
                return DomEventNames.MouseLeave;
            case UiEventKind.Wheel:
                return DomEventNames.Wheel;
            case UiEventKind.KeyDown:
                return DomEventNames.KeyDown;
            case UiEventKind.KeyUp:
                return DomEventNames.KeyUp;
            case UiEventKind.Focus:
                return DomEventNames.Focus;
            case UiEventKind.Blur:
                return DomEventNames.Blur;
            case UiEventKind.FocusIn:
                return DomEventNames.FocusIn;
            case UiEventKind.FocusOut:
                return DomEventNames.FocusOut;
            case UiEventKind.BeforeInput:
                return DomEventNames.BeforeInput;
            case UiEventKind.Input:
                return DomEventNames.Input;
            case UiEventKind.Change:
                return DomEventNames.Change;
            case UiEventKind.CompositionStart:
                return DomEventNames.CompositionStart;
            case UiEventKind.CompositionUpdate:
                return DomEventNames.CompositionUpdate;
            case UiEventKind.CompositionEnd:
                return DomEventNames.CompositionEnd;
            case UiEventKind.SelectionChange:
                return DomEventNames.SelectionChange;
            case UiEventKind.Submit:
                return DomEventNames.Submit;
            case UiEventKind.Reset:
                return DomEventNames.Reset;
            case UiEventKind.DragStart:
                return DomEventNames.DragStart;
            case UiEventKind.Drag:
                return DomEventNames.Drag;
            case UiEventKind.DragEnter:
                return DomEventNames.DragEnter;
            case UiEventKind.DragOver:
                return DomEventNames.DragOver;
            case UiEventKind.DragLeave:
                return DomEventNames.DragLeave;
            case UiEventKind.Drop:
                return DomEventNames.Drop;
            case UiEventKind.DragEnd:
                return DomEventNames.DragEnd;
            case UiEventKind.Copy:
                return DomEventNames.Copy;
            case UiEventKind.Cut:
                return DomEventNames.Cut;
            case UiEventKind.Paste:
                return DomEventNames.Paste;
            case UiEventKind.Scroll:
                return DomEventNames.Scroll;
            case UiEventKind.Resize:
                return DomEventNames.Resize;
            case UiEventKind.VisibilityChange:
                return DomEventNames.VisibilityChange;
            case UiEventKind.Toggle:
                return DomEventNames.Toggle;
            default:
                throw new ArgumentOutOfRangeException("eventKind", eventKind, "The event kind is not supported.");
        }
    }

    internal string[] CopyEventPath(string[]? path)
    {
        string[] source = path ?? Array.Empty<string>();
        string[] result = new string[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal UiPointerData NormalizePointerData(UiPointerData pointer, UiEventKind eventKind)
    {
        string pointerType = pointer.PointerType ?? string.Empty;
        if (ValueComparer.TextEqualsIgnoreCase(pointerType, "mouse"))
        {
            pointerType = "mouse";
        }
        else if (ValueComparer.TextEqualsIgnoreCase(pointerType, "pen"))
        {
            pointerType = "pen";
        }
        else if (ValueComparer.TextEqualsIgnoreCase(pointerType, "touch"))
        {
            pointerType = "touch";
        }
        else if (pointerType.Length == 0)
        {
            pointerType = GetDefaultPointerType(eventKind);
        }

        if (double.IsNaN(pointer.ClientX) || double.IsInfinity(pointer.ClientX))
        {
            throw new ArgumentOutOfRangeException("pointer", pointer.ClientX, "Pointer coordinates must be finite.");
        }

        if (double.IsNaN(pointer.ClientY) || double.IsInfinity(pointer.ClientY))
        {
            throw new ArgumentOutOfRangeException("pointer", pointer.ClientY, "Pointer coordinates must be finite.");
        }

        UiPointerData result = new UiPointerData();
        result.PointerType = pointerType;
        result.PointerId = pointer.PointerId;
        result.ClientX = pointer.ClientX;
        result.ClientY = pointer.ClientY;
        result.Button = pointer.Button;
        result.Buttons = pointer.Buttons;
        result.PrimaryModifier = pointer.PrimaryModifier;
        result.Shift = pointer.Shift;
        return result;
    }

    internal string GetDefaultPointerType(UiEventKind eventKind)
    {
        switch (eventKind)
        {
            case UiEventKind.MouseDown:
            case UiEventKind.MouseUp:
            case UiEventKind.MouseMove:
            case UiEventKind.MouseEnter:
            case UiEventKind.MouseLeave:
            case UiEventKind.Wheel:
                return "mouse";
            default:
                return string.Empty;
        }
    }

    internal UiKeyboardData NormalizeKeyboardData(UiKeyboardData keyboard)
    {
        UiKeyboardData result = new UiKeyboardData();
        result.Key = new KeyboardEngine().NormalizeKeyboardKey(keyboard);
        result.Code = keyboard.Code ?? string.Empty;
        result.PrimaryModifier = keyboard.PrimaryModifier;
        result.Alt = keyboard.Alt;
        result.Shift = keyboard.Shift;
        result.Repeat = keyboard.Repeat;
        result.IsComposing = keyboard.IsComposing;
        return result;
    }

    internal UiInputData NormalizeInputData(UiInputData input, UiEventKind eventKind)
    {
        if (input.HasSelectionStart && input.SelectionStart < 0)
        {
            throw new ArgumentOutOfRangeException("input", input.SelectionStart, "The selection start cannot be negative.");
        }

        if (input.HasSelectionEnd && input.SelectionEnd < 0)
        {
            throw new ArgumentOutOfRangeException("input", input.SelectionEnd, "The selection end cannot be negative.");
        }

        if (input.HasSelectionStart && input.HasSelectionEnd && input.SelectionEnd < input.SelectionStart)
        {
            throw new ArgumentException("The selection end cannot precede the selection start.", "input");
        }

        bool isComposing = input.IsComposing;
        if (eventKind == UiEventKind.CompositionStart || eventKind == UiEventKind.CompositionUpdate)
        {
            isComposing = true;
        }
        else if (eventKind == UiEventKind.CompositionEnd)
        {
            isComposing = false;
        }

        UiInputData result = new UiInputData();
        result.Value = ValueJsonCodec.CloneValue(input.Value, MaximumValueDepth, MaximumValueCollectionCount, MaximumValueStringLength, MaximumValueBinaryLength);
        result.InputType = input.InputType;
        result.Data = input.Data;
        result.IsComposing = isComposing;
        result.HasSelectionStart = input.HasSelectionStart;
        result.SelectionStart = input.HasSelectionStart ? input.SelectionStart : 0;
        result.HasSelectionEnd = input.HasSelectionEnd;
        result.SelectionEnd = input.HasSelectionEnd ? input.SelectionEnd : 0;
        return result;
    }

    internal UiClipboardData NormalizeClipboardData(UiClipboardData clipboard, UiEventKind eventKind)
    {
        UiClipboardData result = new UiClipboardData();
        result.Operation = GetClipboardOperation(eventKind);
        result.Data = CopyEventValues(clipboard.Data);
        result.IsAllowed = clipboard.IsAllowed;
        return result;
    }

    internal string GetClipboardOperation(UiEventKind eventKind)
    {
        switch (eventKind)
        {
            case UiEventKind.Copy:
                return DomEventNames.Copy;
            case UiEventKind.Cut:
                return DomEventNames.Cut;
            case UiEventKind.Paste:
                return DomEventNames.Paste;
            default:
                throw new ArgumentOutOfRangeException("eventKind", eventKind, "The event kind is not a clipboard event.");
        }
    }

    internal UiDragData NormalizeDragData(UiDragData drag)
    {
        UiDragData result = new UiDragData();
        result.SourceId = drag.SourceId;
        result.TargetId = drag.TargetId;
        result.Effect = drag.Effect ?? string.Empty;
        result.AllowedEffect = drag.AllowedEffect ?? string.Empty;
        result.Data = CopyEventValues(drag.Data);
        result.IsKeyboard = drag.IsKeyboard;
        return result;
    }

    internal UiValueEntry[] CopyEventValues(UiValueEntry[]? entries)
    {
        UiValueEntry[] source = entries ?? Array.Empty<UiValueEntry>();
        UiValueEntry[] result = new UiValueEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            UiValueEntry? sourceEntry = source[index];
            if (sourceEntry == null)
            {
                throw new ArgumentException("Event data entries cannot contain null.", "entries");
            }

            UiValueEntry resultEntry = new UiValueEntry();
            resultEntry.Key = sourceEntry.Key ?? string.Empty;
            resultEntry.Value = ValueJsonCodec.CloneValue(sourceEntry.Value, MaximumValueDepth, MaximumValueCollectionCount, MaximumValueStringLength, MaximumValueBinaryLength);
            result[index] = resultEntry;
            index++;
        }

        return result;
    }

    internal void RegisterEventBinding(string? nodeId, UiEventBinding? binding)
    {
        string checkedNodeId = Guard.ArgumentNotWhiteSpace(nodeId, "nodeId");
        if (binding == null)
        {
            throw new ArgumentNullException("binding");
        }

        Guard.ArgumentNotWhiteSpace(binding.Id, "binding");
        Guard.ArgumentNotWhiteSpace(binding.HandlerId, "binding");
        ConvertEventKind(binding.Kind);
        ValidateEventPhase(binding.Phase);
        if (FindEventRegistrationIndex(binding.Id) >= 0)
        {
            throw new ArgumentException("An event binding with the same identifier is already registered.", "binding");
        }

        EventHandlerRegistration registration = new EventHandlerRegistration();
        registration.NodeId = checkedNodeId;
        registration.Binding = CopyEventBinding(binding);
        Registrations.Add(registration);
    }

    internal void RegisterEventSubscription(string? nodeId, DomEventSubscription? subscription)
    {
        if (subscription == null)
        {
            throw new ArgumentNullException("subscription");
        }

        if (!ValueComparer.TextEquals(subscription.EventName, ConvertEventKind(subscription.Kind)))
        {
            throw new ArgumentException("The subscription event name does not match its event kind.", "subscription");
        }

        if (subscription.HasDebounceMilliseconds && subscription.DebounceMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException("subscription", subscription.DebounceMilliseconds, "The debounce interval cannot be negative.");
        }

        if (subscription.HasThrottleMilliseconds && subscription.ThrottleMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException("subscription", subscription.ThrottleMilliseconds, "The throttle interval cannot be negative.");
        }

        if (subscription.HasDebounceMilliseconds && subscription.HasThrottleMilliseconds)
        {
            throw new ArgumentException("An event subscription cannot be both debounced and throttled.", "subscription");
        }

        UiEventBinding binding = new UiEventBinding();
        binding.Id = subscription.Id;
        binding.HandlerId = subscription.HandlerId;
        binding.Kind = subscription.Kind;
        binding.Phase = subscription.Phase;
        binding.PreventDefault = subscription.PreventDefault;
        binding.StopPropagation = subscription.StopPropagation;
        binding.Once = subscription.Once;
        binding.Passive = subscription.Passive;
        RegisterEventBinding(nodeId, binding);
        int registrationIndex = FindEventRegistrationIndex(binding.Id);
        EventHandlerRegistration registration = Registrations[registrationIndex];
        registration.HasDebounceMilliseconds = subscription.HasDebounceMilliseconds;
        registration.DebounceMilliseconds = subscription.HasDebounceMilliseconds ? subscription.DebounceMilliseconds : 0;
        registration.HasThrottleMilliseconds = subscription.HasThrottleMilliseconds;
        registration.ThrottleMilliseconds = subscription.HasThrottleMilliseconds ? subscription.ThrottleMilliseconds : 0;
    }

    internal void ConfigureEventSecurity(string? expectedOrigin, bool requireMainFrame, bool requireTrusted)
    {
        string checkedOrigin = expectedOrigin ?? string.Empty;
        if (checkedOrigin.Length > 0)
        {
            Guard.ArgumentNotWhiteSpace(checkedOrigin, "expectedOrigin");
        }

        ExpectedEventOrigin = checkedOrigin;
        RequireEventMainFrame = requireMainFrame;
        RequireTrustedEvent = requireTrusted;
    }

    internal void UpdateLiveEventNodes(string[]? nodeIds, int generation)
    {
        Guard.ArgumentNonNegative(generation, "generation");
        if (nodeIds == null)
        {
            throw new ArgumentNullException("nodeIds");
        }

        string[] result = new string[nodeIds.Length];
        int index = 0;
        while (index < nodeIds.Length)
        {
            string nodeId = Guard.ArgumentNotWhiteSpace(nodeIds[index], "nodeIds");
            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (ValueComparer.TextEquals(result[earlierIndex], nodeId))
                {
                    throw new ArgumentException("Live event node identifiers must be unique.", "nodeIds");
                }

                earlierIndex++;
            }

            result[index] = nodeId;
            index++;
        }

        LiveEventNodeIds = result;
        LiveEventNodeGeneration = generation;
        HasLiveEventNodes = true;
    }

    internal bool UnregisterEventBinding(string? bindingId)
    {
        string checkedBindingId = Guard.ArgumentNotWhiteSpace(bindingId, "bindingId");
        int index = FindEventRegistrationIndex(checkedBindingId);
        if (index < 0)
        {
            return false;
        }

        Registrations.RemoveAt(index);
        return true;
    }

    internal EventDispatchResult DispatchEventData(UiEventData? eventData)
    {
        UiEventData normalized = NormalizeEventData(eventData);
        ValidateEventPath(normalized);
        return CreateEventDispatch(normalized, false, 0L, null);
    }

    internal EventDispatchResult DispatchTrustedEventData(UiEventData? eventData, EventDispatchMetadata? metadata, long clockMilliseconds)
    {
        ValidateEventClock(clockMilliseconds);
        UiEventData normalized = NormalizeEventData(eventData);
        ValidateEventPath(normalized);
        if (metadata == null)
        {
            throw new ArgumentNullException("metadata");
        }

        string? rejectionCode = GetEventRejectionCode(normalized, metadata);
        if (rejectionCode != null)
        {
            return CreateRejectedEventDispatch(normalized, metadata, rejectionCode);
        }

        EventDispatchResult result = CreateEventDispatch(normalized, true, clockMilliseconds, metadata);
        SetEventDispatchMetadata(result, normalized, metadata);
        return result;
    }

    internal EventDispatchResult CreateEventDispatch(UiEventData normalized, bool applyScheduling, long clockMilliseconds, EventDispatchMetadata? metadata)
    {
        EventDispatchResult result = new EventDispatchResult();
        result.EventData = normalized;
        List<EventHandlerInvocation> invocations = new List<EventHandlerInvocation>();
        List<string> onceBindingIds = new List<string>();
        List<string> deferredBindingIds = new List<string>();
        List<string> throttledBindingIds = new List<string>();

        bool stopped = DispatchEventPhase(normalized, UiEventPhase.Capture, UiEventPhase.Capture, normalized.Path.Length - 1, 1, -1, applyScheduling, clockMilliseconds, metadata, invocations, onceBindingIds, deferredBindingIds, throttledBindingIds, result);
        if (!stopped)
        {
            stopped = DispatchEventTarget(normalized, applyScheduling, clockMilliseconds, metadata, invocations, onceBindingIds, deferredBindingIds, throttledBindingIds, result);
        }

        if (!stopped)
        {
            DispatchEventPhase(normalized, UiEventPhase.Bubble, UiEventPhase.Bubble, 1, normalized.Path.Length - 1, 1, applyScheduling, clockMilliseconds, metadata, invocations, onceBindingIds, deferredBindingIds, throttledBindingIds, result);
        }

        int removeIndex = 0;
        while (removeIndex < onceBindingIds.Count)
        {
            RemoveOnceEventBinding(onceBindingIds[removeIndex]);
            removeIndex++;
        }

        result.Invocations = invocations.ToArray();
        result.RemovedBindingIds = onceBindingIds.ToArray();
        result.DeferredBindingIds = deferredBindingIds.ToArray();
        result.ThrottledBindingIds = throttledBindingIds.ToArray();
        return result;
    }

    internal void ValidateEventPath(UiEventData eventData)
    {
        string targetId = Guard.ArgumentNotWhiteSpace(eventData.TargetId, "eventData");
        if (eventData.Path == null || eventData.Path.Length == 0)
        {
            throw new ArgumentException("An event path must contain its target.", "eventData");
        }

        if (eventData.Path.Length > MaximumEventPathLength)
        {
            throw new ArgumentException("The event path exceeds the maximum length.", "eventData");
        }

        if (!ValueComparer.TextEquals(eventData.Path[0], targetId))
        {
            throw new ArgumentException("The first event path entry must be the target.", "eventData");
        }

        int index = 0;
        while (index < eventData.Path.Length)
        {
            string nodeId = Guard.ArgumentNotWhiteSpace(eventData.Path[index], "eventData");
            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (ValueComparer.TextEquals(eventData.Path[earlierIndex], nodeId))
                {
                    throw new ArgumentException("An event path cannot contain the same node more than once.", "eventData");
                }

                earlierIndex++;
            }

            index++;
        }
    }

    internal void ValidateEventPhase(UiEventPhase phase)
    {
        switch (phase)
        {
            case UiEventPhase.Capture:
            case UiEventPhase.Target:
            case UiEventPhase.Bubble:
                return;
            default:
                throw new ArgumentOutOfRangeException("phase", phase, "The event phase is not supported.");
        }
    }

    internal int FindEventRegistrationIndex(string bindingId)
    {
        int index = 0;
        while (index < Registrations.Count)
        {
            if (ValueComparer.TextEquals(Registrations[index].Binding.Id, bindingId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal UiEventBinding CopyEventBinding(UiEventBinding binding)
    {
        UiEventBinding result = new UiEventBinding();
        result.Id = binding.Id;
        result.HandlerId = binding.HandlerId;
        result.Kind = binding.Kind;
        result.Phase = binding.Phase;
        result.PreventDefault = binding.PreventDefault;
        result.StopPropagation = binding.StopPropagation;
        result.Once = binding.Once;
        result.Passive = binding.Passive;
        return result;
    }

    internal bool DispatchEventTarget(UiEventData eventData, bool applyScheduling, long clockMilliseconds, EventDispatchMetadata? metadata, List<EventHandlerInvocation> invocations, List<string> onceBindingIds, List<string> deferredBindingIds, List<string> throttledBindingIds, EventDispatchResult result)
    {
        bool captureStopped = DispatchEventPhase(eventData, UiEventPhase.Capture, UiEventPhase.Target, 0, 0, 1, applyScheduling, clockMilliseconds, metadata, invocations, onceBindingIds, deferredBindingIds, throttledBindingIds, result);
        bool targetStopped = DispatchEventPhase(eventData, UiEventPhase.Target, UiEventPhase.Target, 0, 0, 1, applyScheduling, clockMilliseconds, metadata, invocations, onceBindingIds, deferredBindingIds, throttledBindingIds, result);
        bool bubbleStopped = DispatchEventPhase(eventData, UiEventPhase.Bubble, UiEventPhase.Target, 0, 0, 1, applyScheduling, clockMilliseconds, metadata, invocations, onceBindingIds, deferredBindingIds, throttledBindingIds, result);
        return captureStopped || targetStopped || bubbleStopped;
    }

    internal bool DispatchEventPhase(UiEventData eventData, UiEventPhase bindingPhase, UiEventPhase invocationPhase, int startIndex, int endIndex, int step, bool applyScheduling, long clockMilliseconds, EventDispatchMetadata? metadata, List<EventHandlerInvocation> invocations, List<string> onceBindingIds, List<string> deferredBindingIds, List<string> throttledBindingIds, EventDispatchResult result)
    {
        int pathIndex = startIndex;
        while ((step > 0 && pathIndex <= endIndex) || (step < 0 && pathIndex >= endIndex))
        {
            bool stoppedAtNode = false;
            string nodeId = eventData.Path[pathIndex];
            int registrationIndex = 0;
            while (registrationIndex < Registrations.Count)
            {
                EventHandlerRegistration registration = Registrations[registrationIndex];
                UiEventBinding binding = registration.Binding;
                if (ValueComparer.TextEquals(registration.NodeId, nodeId) && binding.Kind == eventData.Kind && binding.Phase == bindingPhase)
                {
                    bool invokeNow = true;
                    if (applyScheduling && registration.HasDebounceMilliseconds)
                    {
                        registration.HasPendingEvent = true;
                        registration.PendingDueMilliseconds = AddEventDelay(clockMilliseconds, registration.DebounceMilliseconds);
                        registration.PendingEvent = NormalizeEventData(eventData);
                        registration.PendingMetadata = CopyEventMetadata(metadata);
                        deferredBindingIds.Add(binding.Id);
                        invokeNow = false;
                    }
                    else if (applyScheduling && registration.HasThrottleMilliseconds)
                    {
                        if (registration.HasLastDispatchMilliseconds && clockMilliseconds - registration.LastDispatchMilliseconds < registration.ThrottleMilliseconds)
                        {
                            throttledBindingIds.Add(binding.Id);
                            invokeNow = false;
                        }
                        else
                        {
                            registration.HasLastDispatchMilliseconds = true;
                            registration.LastDispatchMilliseconds = clockMilliseconds;
                        }
                    }

                    if (invokeNow)
                    {
                        EventHandlerInvocation invocation = CreateEventInvocation(registration, invocationPhase);
                        invocations.Add(invocation);
                        if (binding.Once)
                        {
                            onceBindingIds.Add(binding.Id);
                        }
                    }

                    if (binding.PreventDefault && !binding.Passive)
                    {
                        result.DefaultPrevented = true;
                    }

                    if (binding.StopPropagation)
                    {
                        stoppedAtNode = true;
                        result.PropagationStopped = true;
                    }
                }

                registrationIndex++;
            }

            if (stoppedAtNode)
            {
                return true;
            }

            pathIndex += step;
        }

        return false;
    }

    internal EventHandlerInvocation CreateEventInvocation(EventHandlerRegistration registration, UiEventPhase invocationPhase)
    {
        UiEventBinding binding = registration.Binding;
        EventHandlerInvocation invocation = new EventHandlerInvocation();
        invocation.BindingId = binding.Id;
        invocation.HandlerId = binding.HandlerId;
        invocation.NodeId = registration.NodeId;
        invocation.Phase = invocationPhase;
        invocation.PreventDefault = binding.PreventDefault && !binding.Passive;
        invocation.StopPropagation = binding.StopPropagation;
        invocation.Passive = binding.Passive;
        return invocation;
    }

    internal long AddEventDelay(long clockMilliseconds, int delayMilliseconds)
    {
        if (clockMilliseconds > long.MaxValue - delayMilliseconds)
        {
            return long.MaxValue;
        }

        return clockMilliseconds + delayMilliseconds;
    }

    internal void ValidateEventClock(long clockMilliseconds)
    {
        if (clockMilliseconds < 0L)
        {
            throw new ArgumentOutOfRangeException("clockMilliseconds", clockMilliseconds, "The event clock cannot be negative.");
        }

        if (HasEventClock && clockMilliseconds < LastEventClockMilliseconds)
        {
            throw new ArgumentException("The event clock must be monotonic.", "clockMilliseconds");
        }

        HasEventClock = true;
        LastEventClockMilliseconds = clockMilliseconds;
    }

    internal EventDispatchMetadata? CopyEventMetadata(EventDispatchMetadata? metadata)
    {
        if (metadata == null)
        {
            return null;
        }

        EventDispatchMetadata result = new EventDispatchMetadata();
        result.Origin = metadata.Origin ?? string.Empty;
        result.IsMainFrame = metadata.IsMainFrame;
        result.NodeGeneration = metadata.NodeGeneration;
        return result;
    }

    internal string? GetEventRejectionCode(UiEventData eventData, EventDispatchMetadata metadata)
    {
        if (ExpectedEventOrigin.Length > 0 && !ValueComparer.TextEquals(ExpectedEventOrigin, metadata.Origin))
        {
            return "event.origin";
        }

        if (RequireEventMainFrame && !metadata.IsMainFrame)
        {
            return "event.main-frame";
        }

        if (RequireTrustedEvent && !eventData.IsTrusted)
        {
            return "event.untrusted";
        }

        if (HasLiveEventNodes && metadata.NodeGeneration != LiveEventNodeGeneration)
        {
            return "event.stale-generation";
        }

        if (HasLiveEventNodes)
        {
            int index = 0;
            while (index < eventData.Path.Length)
            {
                if (!ContainsLiveEventNode(eventData.Path[index]))
                {
                    return "event.stale-node";
                }

                index++;
            }
        }

        return null;
    }

    internal bool ContainsLiveEventNode(string nodeId)
    {
        int index = 0;
        while (index < LiveEventNodeIds.Length)
        {
            if (ValueComparer.TextEquals(LiveEventNodeIds[index], nodeId))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal EventDispatchResult CreateRejectedEventDispatch(UiEventData eventData, EventDispatchMetadata metadata, string rejectionCode)
    {
        EventDispatchResult result = new EventDispatchResult();
        result.EventData = eventData;
        result.Accepted = false;
        result.RejectionCode = rejectionCode;
        SetEventDispatchMetadata(result, eventData, metadata);
        return result;
    }

    internal void SetEventDispatchMetadata(EventDispatchResult result, UiEventData eventData, EventDispatchMetadata metadata)
    {
        result.Origin = metadata.Origin ?? string.Empty;
        result.IsMainFrame = metadata.IsMainFrame;
        result.IsTrustedOrigin = eventData.IsTrusted && (ExpectedEventOrigin.Length == 0 || ValueComparer.TextEquals(ExpectedEventOrigin, metadata.Origin));
    }

    internal EventDispatchResult[] FlushScheduledEvents(long clockMilliseconds)
    {
        ValidateEventClock(clockMilliseconds);
        List<EventDispatchResult> results = new List<EventDispatchResult>();
        while (true)
        {
            int selectedIndex = FindDueEventRegistration(clockMilliseconds);
            if (selectedIndex < 0)
            {
                break;
            }

            EventHandlerRegistration registration = Registrations[selectedIndex];
            UiEventData? pendingEvent = registration.PendingEvent;
            EventDispatchMetadata? pendingMetadata = registration.PendingMetadata;
            registration.HasPendingEvent = false;
            registration.PendingEvent = null;
            registration.PendingMetadata = null;
            if (pendingEvent != null)
            {
                EventDispatchResult scheduledResult;
                string? rejectionCode = pendingMetadata == null ? null : GetEventRejectionCode(pendingEvent, pendingMetadata);
                if (pendingMetadata != null && rejectionCode != null)
                {
                    scheduledResult = CreateRejectedEventDispatch(pendingEvent, pendingMetadata, rejectionCode);
                }
                else
                {
                    scheduledResult = CreateScheduledEventDispatch(registration, pendingEvent, pendingMetadata);
                }

                results.Add(scheduledResult);
                if (registration.Binding.Once && scheduledResult.Accepted && scheduledResult.Invocations.Length > 0)
                {
                    RemoveOnceEventBinding(registration.Binding.Id);
                }
            }
        }

        return results.ToArray();
    }

    internal int FindDueEventRegistration(long clockMilliseconds)
    {
        int selectedIndex = -1;
        long selectedDue = long.MaxValue;
        int index = 0;
        while (index < Registrations.Count)
        {
            EventHandlerRegistration registration = Registrations[index];
            if (registration.HasPendingEvent && registration.PendingDueMilliseconds <= clockMilliseconds && (selectedIndex < 0 || registration.PendingDueMilliseconds < selectedDue))
            {
                selectedIndex = index;
                selectedDue = registration.PendingDueMilliseconds;
            }

            index++;
        }

        return selectedIndex;
    }

    internal EventDispatchResult CreateScheduledEventDispatch(EventHandlerRegistration registration, UiEventData eventData, EventDispatchMetadata? metadata)
    {
        EventDispatchResult result = new EventDispatchResult();
        result.EventData = eventData;
        result.Invocations = new EventHandlerInvocation[] { CreateEventInvocation(registration, GetScheduledInvocationPhase(registration, eventData)) };
        result.DefaultPrevented = registration.Binding.PreventDefault && !registration.Binding.Passive;
        result.PropagationStopped = registration.Binding.StopPropagation;
        if (registration.Binding.Once)
        {
            result.RemovedBindingIds = new string[] { registration.Binding.Id };
        }

        if (metadata != null)
        {
            SetEventDispatchMetadata(result, eventData, metadata);
        }

        return result;
    }

    internal UiEventPhase GetScheduledInvocationPhase(EventHandlerRegistration registration, UiEventData eventData)
    {
        if (ValueComparer.TextEquals(registration.NodeId, eventData.TargetId))
        {
            return UiEventPhase.Target;
        }

        return registration.Binding.Phase;
    }

    internal UiEventData[] SynthesizeControlEvents(UiNode? control, UiEventData? sourceEvent)
    {
        if (control == null)
        {
            throw new ArgumentNullException("control");
        }

        UiEventData source = NormalizeEventData(sourceEvent);
        ValidateEventPath(source);
        UiNodeState state = control.State ?? new UiNodeState();
        if (!state.IsVisible || !state.IsEnabled || state.IsBusy)
        {
            return Array.Empty<UiEventData>();
        }

        bool isActivation = IsControlActivationEvent(source);
        if (!isActivation)
        {
            return Array.Empty<UiEventData>();
        }

        if (control.Button != null)
        {
            if (!control.Button.IsVisible || !control.Button.IsEnabled || control.Button.IsBusy)
            {
                return Array.Empty<UiEventData>();
            }

            switch (control.Button.Kind)
            {
                case UiButtonKind.Submit:
                    return new UiEventData[] { CreateSemanticControlEvent(source, UiEventKind.Submit, false, false) };
                case UiButtonKind.Reset:
                    return new UiEventData[] { CreateSemanticControlEvent(source, UiEventKind.Reset, false, false) };
                case UiButtonKind.Toggle:
                    return new UiEventData[] { CreateSemanticControlEvent(source, UiEventKind.Toggle, true, !GetControlPressedState(control)) };
                case UiButtonKind.Push:
                case UiButtonKind.Split:
                case UiButtonKind.Icon:
                case UiButtonKind.Command:
                    if (source.Kind == UiEventKind.KeyDown)
                    {
                        return new UiEventData[] { CreateSemanticControlEvent(source, UiEventKind.Click, false, false) };
                    }

                    return Array.Empty<UiEventData>();
                default:
                    throw new ArgumentOutOfRangeException("control", control.Button.Kind, "The button kind is not supported.");
            }
        }

        if (control.Disclosure != null)
        {
            if (control.Disclosure.IsDisabled)
            {
                return Array.Empty<UiEventData>();
            }

            return new UiEventData[] { CreateSemanticControlEvent(source, UiEventKind.Toggle, true, !control.Disclosure.IsExpanded) };
        }

        return Array.Empty<UiEventData>();
    }

    internal bool IsControlActivationEvent(UiEventData sourceEvent)
    {
        if (sourceEvent.Kind == UiEventKind.Click)
        {
            return true;
        }

        if (sourceEvent.Kind != UiEventKind.KeyDown || sourceEvent.Keyboard == null || sourceEvent.Keyboard.IsComposing)
        {
            return false;
        }

        return sourceEvent.Keyboard.Key == AriaKeyNames.Enter || sourceEvent.Keyboard.Key == AriaKeyNames.Space;
    }

    internal bool GetControlPressedState(UiNode control)
    {
        if (control.Button != null && control.Button.HasPressed)
        {
            return control.Button.IsPressed;
        }

        if (control.State != null && control.State.HasPressed)
        {
            return control.State.IsPressed;
        }

        return false;
    }

    internal UiEventData CreateSemanticControlEvent(UiEventData sourceEvent, UiEventKind eventKind, bool hasBooleanValue, bool booleanValue)
    {
        UiEventData result = new UiEventData();
        result.Kind = eventKind;
        result.TargetId = sourceEvent.TargetId;
        result.Path = CopyEventPath(sourceEvent.Path);
        result.IsTrusted = sourceEvent.IsTrusted;
        if (hasBooleanValue)
        {
            UiInputData input = new UiInputData();
            input.Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = booleanValue };
            result.Input = input;
        }

        return NormalizeEventData(result);
    }

    internal void RemoveOnceEventBinding(string bindingId)
    {
        int index = FindEventRegistrationIndex(bindingId);
        if (index >= 0)
        {
            Registrations.RemoveAt(index);
        }
    }
}
