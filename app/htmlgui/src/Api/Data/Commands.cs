namespace htmlgui;

/// <summary>Describes a command and its projected status.</summary>
public class UiCommand
{
    /// <summary>The stable command identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The visible label.</summary>
    public string Label = string.Empty;
    /// <summary>The optional description.</summary>
    public string? Description;
    /// <summary>The optional icon key.</summary>
    public string? IconKey;
    /// <summary>The execute-handler identifier.</summary>
    public string HandlerId = string.Empty;
    /// <summary>The optional status-handler identifier.</summary>
    public string? StatusHandlerId;
    /// <summary>The command shortcuts.</summary>
    public UiShortcut[] Shortcuts = System.Array.Empty<UiShortcut>();
    /// <summary>The current status.</summary>
    public UiCommandStatus Status = new UiCommandStatus();
    /// <summary>The optional current invocation.</summary>
    public UiCommandInvocation? Invocation;
}

/// <summary>Describes one command invocation.</summary>
public class UiCommandInvocation
{
    /// <summary>The command identifier.</summary>
    public string CommandId = string.Empty;
    /// <summary>The optional scope identifier.</summary>
    public string? ScopeId;
    /// <summary>The optional source node identifier.</summary>
    public string? SourceNodeId;
    /// <summary>The portable command parameter.</summary>
    public UiValue Parameter = new UiValue();
    /// <summary>Whether the invocation came from a repeated key.</summary>
    public bool IsRepeat;
}

/// <summary>Describes command availability and check state.</summary>
public class UiCommandStatus
{
    /// <summary>Whether the command is enabled.</summary>
    public bool IsEnabled = true;
    /// <summary>Whether the command is visible.</summary>
    public bool IsVisible = true;
    /// <summary>Whether execution is pending.</summary>
    public bool IsBusy;
    /// <summary>Whether a check state is supplied.</summary>
    public bool HasCheckState;
    /// <summary>The check state.</summary>
    public UiCommandCheckState CheckState;
}

/// <summary>Describes a normalized keyboard shortcut or chord sequence.</summary>
public class UiShortcut
{
    /// <summary>The stable shortcut identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The ordered chords.</summary>
    public UiShortcutChord[] Chords = System.Array.Empty<UiShortcutChord>();
    /// <summary>The optional active-scope identifier.</summary>
    public string? ScopeId;
    /// <summary>Whether repeated key events are accepted.</summary>
    public bool AllowRepeat;
}

/// <summary>Describes one normalized shortcut chord.</summary>
public class UiShortcutChord
{
    /// <summary>The logical key.</summary>
    public string Key = string.Empty;
    /// <summary>Whether the platform primary modifier is required.</summary>
    public bool PrimaryModifier;
    /// <summary>Whether Alt is required.</summary>
    public bool Alt;
    /// <summary>Whether Shift is required.</summary>
    public bool Shift;
}

/// <summary>Describes the outcome returned by a command execute callback.</summary>
public class UiCommandHandlerResult
{
    /// <summary>Whether command execution succeeded.</summary>
    public bool Success = true;
    /// <summary>Whether execution supplies a new check state.</summary>
    public bool HasCheckState;
    /// <summary>The check state supplied after execution.</summary>
    public UiCommandCheckState CheckState;
    /// <summary>The optional portable callback result.</summary>
    public UiValue? Value;
    /// <summary>The ordered callback diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Describes a command query or execution result.</summary>
public class UiCommandResult
{
    /// <summary>Whether the operation and any callback succeeded.</summary>
    public bool Success;
    /// <summary>The resulting command status.</summary>
    public UiCommandStatus Status = new UiCommandStatus();
    /// <summary>The normalized invocation when execution started.</summary>
    public UiCommandInvocation? Invocation;
    /// <summary>The copied execute-callback result when a callback ran.</summary>
    public UiCommandHandlerResult? HandlerResult;
    /// <summary>The ordered operation and callback diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Describes command shortcut matching and any resulting execution.</summary>
public class UiCommandShortcutResult
{
    /// <summary>Whether matching and any resulting execution succeeded.</summary>
    public bool Success;
    /// <summary>Whether a shortcut consumed the input.</summary>
    public bool Handled;
    /// <summary>Whether more chords are required.</summary>
    public bool Pending;
    /// <summary>Whether repeat or composition policy suppressed the input.</summary>
    public bool Suppressed;
    /// <summary>The normalized logical key.</summary>
    public string NormalizedKey = string.Empty;
    /// <summary>The normalized modifier chord.</summary>
    public string Chord = string.Empty;
    /// <summary>The matched command identifier.</summary>
    public string? CommandId;
    /// <summary>The matched shortcut identifier.</summary>
    public string? ShortcutId;
    /// <summary>The ordered command identifiers involved in a conflict.</summary>
    public string[] ConflictingCommandIds = System.Array.Empty<string>();
    /// <summary>The accepted chord prefix awaiting completion.</summary>
    public string[] PendingChords = System.Array.Empty<string>();
    /// <summary>The command execution result when a complete shortcut matched.</summary>
    public UiCommandResult? CommandResult;
    /// <summary>The ordered matching and execution diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}
