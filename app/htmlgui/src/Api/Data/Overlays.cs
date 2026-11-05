using System;

namespace htmlgui;

/// <summary>Describes a modal, non-modal, alert, confirm, prompt, sheet, or drawer dialog.</summary>
public class UiDialog
{
    /// <summary>The dialog behavior.</summary>
    public UiDialogKind Kind;
    /// <summary>The stable dialog key.</summary>
    public string Key = string.Empty;
    /// <summary>The visible title.</summary>
    public string Title = string.Empty;
    /// <summary>The dialog content.</summary>
    public UiNode[] Content = Array.Empty<UiNode>();
    /// <summary>The dialog action buttons.</summary>
    public UiButton[] Actions = Array.Empty<UiButton>();
    /// <summary>The common overlay state.</summary>
    public UiOverlayState State = new UiOverlayState();
    /// <summary>The optional initial-focus target identifier.</summary>
    public string? InitialFocusId;
}

/// <summary>Describes content positioned relative to an anchor.</summary>
public class UiPopover
{
    /// <summary>The stable popover key.</summary>
    public string Key = string.Empty;
    /// <summary>The anchor and placement options.</summary>
    public UiAnchor Anchor = new UiAnchor();
    /// <summary>The popover content.</summary>
    public UiNode[] Content = Array.Empty<UiNode>();
    /// <summary>The common overlay state.</summary>
    public UiOverlayState State = new UiOverlayState();
}

/// <summary>Describes a non-interactive tooltip anchored to another node.</summary>
public class UiTooltip
{
    /// <summary>The stable tooltip key.</summary>
    public string Key = string.Empty;
    /// <summary>The tooltip text.</summary>
    public string Text = string.Empty;
    /// <summary>The anchor and placement options.</summary>
    public UiAnchor Anchor = new UiAnchor();
    /// <summary>The common overlay state.</summary>
    public UiOverlayState State = new UiOverlayState();
    /// <summary>Whether a show delay is supplied.</summary>
    public bool HasDelayMilliseconds;
    /// <summary>The show delay.</summary>
    public int DelayMilliseconds;
}

/// <summary>Describes a transient status notification.</summary>
public class UiToast
{
    /// <summary>The stable toast key.</summary>
    public string Key = string.Empty;
    /// <summary>The notification message.</summary>
    public string Message = string.Empty;
    /// <summary>The notification severity.</summary>
    public UiValidationSeverity Severity;
    /// <summary>The live-region behavior.</summary>
    public UiAriaLiveMode LiveMode = UiAriaLiveMode.Polite;
    /// <summary>The optional action button.</summary>
    public UiButton? Action;
    /// <summary>The common overlay state.</summary>
    public UiOverlayState State = new UiOverlayState();
    /// <summary>Whether an automatic dismissal interval is supplied.</summary>
    public bool HasDurationMilliseconds;
    /// <summary>The automatic dismissal interval.</summary>
    public int DurationMilliseconds;
}

/// <summary>Describes overlay anchoring and collision behavior.</summary>
public class UiAnchor
{
    /// <summary>The stable anchor node identifier.</summary>
    public string TargetId = string.Empty;
    /// <summary>The logical placement token.</summary>
    public string Placement = "bottom";
    /// <summary>The main-axis offset.</summary>
    public UiLength Offset = new UiLength();
    /// <summary>The cross-axis offset.</summary>
    public UiLength CrossOffset = new UiLength();
    /// <summary>Whether placement may flip to avoid clipping.</summary>
    public bool AllowFlip = true;
    /// <summary>Whether placement may shift to remain visible.</summary>
    public bool AllowShift = true;
}

/// <summary>Describes common open, dismissal, focus, backdrop, and stacking state.</summary>
public class UiOverlayState
{
    /// <summary>Whether the overlay is open.</summary>
    public bool IsOpen;
    /// <summary>Whether the overlay is modal.</summary>
    public bool IsModal;
    /// <summary>Whether a backdrop is shown.</summary>
    public bool HasBackdrop;
    /// <summary>Whether Escape dismisses the overlay.</summary>
    public bool DismissOnEscape = true;
    /// <summary>Whether an outside click dismisses the overlay.</summary>
    public bool DismissOnOutsideClick = true;
    /// <summary>Whether focus is trapped while open.</summary>
    public bool TrapFocus;
    /// <summary>The optional focus-restoration target identifier.</summary>
    public string? RestoreFocusId;
    /// <summary>The deterministic stack order.</summary>
    public int StackOrder;
}
