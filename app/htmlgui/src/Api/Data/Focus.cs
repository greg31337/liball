namespace htmlgui;

/// <summary>Describes a focus, blur, move, restore, or reveal request.</summary>
public class UiFocusRequest
{
    /// <summary>The focus operation.</summary>
    public UiFocusOperation Operation;
    /// <summary>The optional target node identifier.</summary>
    public string? TargetId;
    /// <summary>The optional focus-scope identifier.</summary>
    public string? ScopeId;
    /// <summary>The logical movement direction.</summary>
    public UiFocusDirection Direction;
    /// <summary>Whether the target should be revealed.</summary>
    public bool Reveal;
    /// <summary>Whether focus should be restored.</summary>
    public bool Restore;
}

/// <summary>Describes one logical focus scope and its ordered targets.</summary>
public class UiFocusScope
{
    /// <summary>The stable scope identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The optional parent scope identifier.</summary>
    public string? ParentScopeId;
    /// <summary>The stable identifier of the scope's focusable composite owner.</summary>
    public string OwnerNodeId = string.Empty;
    /// <summary>The target identifiers in logical source order.</summary>
    public string[] TargetIds = System.Array.Empty<string>();
    /// <summary>The optional target selected when the scope is entered.</summary>
    public string? InitialFocusId;
    /// <summary>The optional target selected when focus leaves the scope.</summary>
    public string? ReturnFocusId;
    /// <summary>The optional currently active target.</summary>
    public string? ActiveTargetId;
    /// <summary>Whether focus is constrained to this scope.</summary>
    public bool TrapFocus;
    /// <summary>Whether targets use one active zero tab index and negative tab indexes otherwise.</summary>
    public bool RovingTabIndex;
    /// <summary>Whether the owner retains focus while the active descendant changes.</summary>
    public bool UseActiveDescendant;
    /// <summary>Whether directional movement wraps at either end.</summary>
    public bool Wrap;
}

/// <summary>Describes one candidate in a logical focus order.</summary>
public class UiFocusTarget
{
    /// <summary>The stable target identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The stable identifier of the target's owning scope.</summary>
    public string ScopeId = string.Empty;
    /// <summary>Whether the target can receive focus.</summary>
    public bool IsFocusable = true;
    /// <summary>Whether the target permits interaction.</summary>
    public bool IsEnabled = true;
    /// <summary>Whether the target participates in presentation.</summary>
    public bool IsVisible = true;
    /// <summary>Whether an explicit tab index is supplied.</summary>
    public bool HasTabIndex;
    /// <summary>The target's current or requested tab index.</summary>
    public int TabIndex;
}

/// <summary>Describes current focus, selection, and trap state.</summary>
public class UiFocusState
{
    /// <summary>The optional focused node identifier.</summary>
    public string? FocusedNodeId;
    /// <summary>The optional active focus-scope identifier.</summary>
    public string? ScopeId;
    /// <summary>The optional active-descendant identifier.</summary>
    public string? ActiveDescendantId;
    /// <summary>Whether focus is trapped.</summary>
    public bool IsTrapped;
    /// <summary>Whether keyboard focus is visibly indicated.</summary>
    public bool IsFocusVisible;
}

/// <summary>Contains the deterministic result of one focus operation.</summary>
public class UiFocusResult
{
    /// <summary>Whether the focus operation succeeded.</summary>
    public bool Success;
    /// <summary>The resulting focus state.</summary>
    public UiFocusState State = new UiFocusState();
    /// <summary>The resulting target states in input order.</summary>
    public UiFocusTarget[] Targets = System.Array.Empty<UiFocusTarget>();
    /// <summary>The optional target that should be revealed after focus moves.</summary>
    public string? RevealNodeId;
    /// <summary>The ordered focus diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Describes key-to-action mappings for one composite widget.</summary>
public class UiKeyboardMap
{
    /// <summary>The stable map identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The widget kind token.</summary>
    public string Widget = string.Empty;
    /// <summary>The orientation.</summary>
    public UiOrientation Orientation;
    /// <summary>The key-to-action entries.</summary>
    public UiValueEntry[] Mappings = System.Array.Empty<UiValueEntry>();
    /// <summary>Whether logical horizontal directions reverse in RTL.</summary>
    public bool IsRightToLeft;
}

/// <summary>Describes a composite widget's type-ahead buffer.</summary>
public class UiTypeAheadState
{
    /// <summary>The accumulated text.</summary>
    public string Buffer = string.Empty;
    /// <summary>The matched item key.</summary>
    public string? MatchedKey;
    /// <summary>The input generation.</summary>
    public int Generation;
    /// <summary>Whether matching ignores case.</summary>
    public bool IgnoreCase = true;
}

/// <summary>Contains the deterministic result of processing one keyboard input.</summary>
public class UiKeyboardResult
{
    /// <summary>Whether a mapped action or type-ahead match handled the input.</summary>
    public bool Handled;
    /// <summary>Whether composition or repeat policy suppressed the input.</summary>
    public bool Suppressed;
    /// <summary>Whether the input participated in type-ahead matching.</summary>
    public bool IsTypeAhead;
    /// <summary>The normalized logical key.</summary>
    public string NormalizedKey = string.Empty;
    /// <summary>The normalized modifier chord.</summary>
    public string Chord = string.Empty;
    /// <summary>The optional mapped action token.</summary>
    public string? Action;
    /// <summary>The optional key of the matched type-ahead item.</summary>
    public string? MatchedKey;
    /// <summary>The resulting type-ahead state.</summary>
    public UiTypeAheadState TypeAhead = new UiTypeAheadState();
    /// <summary>The ordered processing diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}
