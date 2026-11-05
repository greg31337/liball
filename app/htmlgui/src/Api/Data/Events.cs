namespace htmlgui;

/// <summary>Describes a model event binding.</summary>
public class UiEventBinding
{
    /// <summary>The stable binding identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The stable handler identifier.</summary>
    public string HandlerId = string.Empty;
    /// <summary>The event kind.</summary>
    public UiEventKind Kind;
    /// <summary>The propagation phase.</summary>
    public UiEventPhase Phase = UiEventPhase.Bubble;
    /// <summary>Whether default behavior is prevented.</summary>
    public bool PreventDefault;
    /// <summary>Whether propagation stops.</summary>
    public bool StopPropagation;
    /// <summary>Whether the binding runs once.</summary>
    public bool Once;
    /// <summary>Whether the binding is passive.</summary>
    public bool Passive;
}

/// <summary>Describes normalized common and kind-specific event data.</summary>
public class UiEventData
{
    /// <summary>The event kind.</summary>
    public UiEventKind Kind;
    /// <summary>The target node identifier.</summary>
    public string TargetId = string.Empty;
    /// <summary>The validated target-to-root path.</summary>
    public string[] Path = System.Array.Empty<string>();
    /// <summary>Whether the browser reported a trusted event.</summary>
    public bool IsTrusted;
    /// <summary>The optional pointer data.</summary>
    public UiPointerData? Pointer;
    /// <summary>The optional keyboard data.</summary>
    public UiKeyboardData? Keyboard;
    /// <summary>The optional input data.</summary>
    public UiInputData? Input;
    /// <summary>The optional clipboard data.</summary>
    public UiClipboardData? Clipboard;
    /// <summary>The optional drag data.</summary>
    public UiDragData? Drag;
}

/// <summary>Describes normalized pointer coordinates, buttons, and modifiers.</summary>
public class UiPointerData
{
    /// <summary>The pointer type token.</summary>
    public string PointerType = string.Empty;
    /// <summary>The pointer identifier.</summary>
    public int PointerId;
    /// <summary>The client x-coordinate.</summary>
    public double ClientX;
    /// <summary>The client y-coordinate.</summary>
    public double ClientY;
    /// <summary>The changed button.</summary>
    public int Button;
    /// <summary>The pressed-button bit mask.</summary>
    public uint Buttons;
    /// <summary>Whether the primary modifier is active.</summary>
    public bool PrimaryModifier;
    /// <summary>Whether Shift is active.</summary>
    public bool Shift;
}

/// <summary>Describes normalized keyboard input.</summary>
public class UiKeyboardData
{
    /// <summary>The logical key.</summary>
    public string Key = string.Empty;
    /// <summary>The physical key code.</summary>
    public string Code = string.Empty;
    /// <summary>Whether the primary modifier is active.</summary>
    public bool PrimaryModifier;
    /// <summary>Whether Alt is active.</summary>
    public bool Alt;
    /// <summary>Whether Shift is active.</summary>
    public bool Shift;
    /// <summary>Whether the key is repeating.</summary>
    public bool Repeat;
    /// <summary>Whether IME composition is active.</summary>
    public bool IsComposing;
}

/// <summary>Describes normalized text or form input.</summary>
public class UiInputData
{
    /// <summary>The current portable value.</summary>
    public UiValue Value = new UiValue();
    /// <summary>The optional input-type token.</summary>
    public string? InputType;
    /// <summary>The optional inserted data.</summary>
    public string? Data;
    /// <summary>Whether composition is active.</summary>
    public bool IsComposing;
    /// <summary>Whether a selection start is supplied.</summary>
    public bool HasSelectionStart;
    /// <summary>The selection start.</summary>
    public int SelectionStart;
    /// <summary>Whether a selection end is supplied.</summary>
    public bool HasSelectionEnd;
    /// <summary>The selection end.</summary>
    public int SelectionEnd;
}

/// <summary>Describes normalized clipboard data.</summary>
public class UiClipboardData
{
    /// <summary>The clipboard operation token.</summary>
    public string Operation = string.Empty;
    /// <summary>The permitted MIME-keyed values.</summary>
    public UiValueEntry[] Data = System.Array.Empty<UiValueEntry>();
    /// <summary>Whether policy permitted the operation.</summary>
    public bool IsAllowed;
}

/// <summary>Describes normalized drag-and-drop data.</summary>
public class UiDragData
{
    /// <summary>The stable source node identifier.</summary>
    public string? SourceId;
    /// <summary>The stable target node identifier.</summary>
    public string? TargetId;
    /// <summary>The requested effect token.</summary>
    public string Effect = string.Empty;
    /// <summary>The allowed effect token.</summary>
    public string AllowedEffect = string.Empty;
    /// <summary>The validated MIME-keyed payload.</summary>
    public UiValueEntry[] Data = System.Array.Empty<UiValueEntry>();
    /// <summary>Whether the operation uses its keyboard alternative.</summary>
    public bool IsKeyboard;
}

/// <summary>Describes one event-handler callback invocation.</summary>
public class UiEventHandlerContext
{
    /// <summary>The normalized event data supplied to the handler.</summary>
    public UiEventData EventData = new UiEventData();
    /// <summary>The event-binding identifier.</summary>
    public string BindingId = string.Empty;
    /// <summary>The registered handler identifier.</summary>
    public string HandlerId = string.Empty;
    /// <summary>The current propagation target identifier.</summary>
    public string CurrentTargetId = string.Empty;
    /// <summary>The active propagation phase.</summary>
    public UiEventPhase Phase;
    /// <summary>Whether the handler is passive and therefore cannot prevent default behavior.</summary>
    public bool Passive;
}

/// <summary>Describes the outcome requested by one event handler.</summary>
public class UiEventHandlerResult
{
    /// <summary>Whether the handler completed successfully.</summary>
    public bool Success = true;
    /// <summary>Whether the handler acted on the event.</summary>
    public bool Handled;
    /// <summary>Whether browser default behavior should be prevented.</summary>
    public bool PreventDefault;
    /// <summary>Whether propagation should stop after other handlers on the current target.</summary>
    public bool StopPropagation;
    /// <summary>The optional portable handler result.</summary>
    public UiValue? Value;
    /// <summary>The ordered handler diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Describes an event dispatch and its ordered callback outcomes.</summary>
public class UiEventDispatchResult
{
    /// <summary>Whether every invoked handler completed successfully.</summary>
    public bool Success = true;
    /// <summary>Whether at least one handler acted on the event.</summary>
    public bool Handled;
    /// <summary>Whether default browser behavior was prevented.</summary>
    public bool DefaultPrevented;
    /// <summary>Whether propagation was stopped.</summary>
    public bool PropagationStopped;
    /// <summary>The normalized event that was dispatched.</summary>
    public UiEventData EventData = new UiEventData();
    /// <summary>The invoked binding identifiers in callback order.</summary>
    public string[] InvokedBindingIds = System.Array.Empty<string>();
    /// <summary>The invoked handler identifiers in callback order.</summary>
    public string[] InvokedHandlerIds = System.Array.Empty<string>();
    /// <summary>The once-only binding identifiers removed by the dispatch.</summary>
    public string[] RemovedBindingIds = System.Array.Empty<string>();
    /// <summary>The ordered handler results.</summary>
    public UiEventHandlerResult[] HandlerResults = System.Array.Empty<UiEventHandlerResult>();
    /// <summary>The ordered diagnostics produced by handlers or dispatch validation.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
    /// <summary>The state and binding propagation performed by the host transaction.</summary>
    public UiBindingResult? BindingResult;
    /// <summary>The validation performed by the host transaction.</summary>
    public UiValidationResult? ValidationResult;
    /// <summary>The command execution performed by the host transaction.</summary>
    public UiCommandResult? CommandResult;
    /// <summary>The component update cycle performed by the host transaction.</summary>
    public UiComponentResult? ComponentResult;
    /// <summary>The navigation performed by the host transaction.</summary>
    public UiNavigationResult? NavigationResult;
    /// <summary>The focus operation performed by the host transaction.</summary>
    public UiFocusResult? FocusResult;
    /// <summary>The render and DOM update produced by the host transaction.</summary>
    public UiRenderResult? RenderResult;
    /// <summary>The bridge delivery result for the transaction's DOM batch.</summary>
    public UiResult? BridgeResult;
    /// <summary>The completed host transaction stages in deterministic order.</summary>
    public string[] TransactionStages = System.Array.Empty<string>();
    /// <summary>Whether a component error boundary isolated a failed component update.</summary>
    public bool RecoveredByComponentBoundary;
}
