namespace htmlgui;

/// <summary>Describes one versioned state value.</summary>
public class UiStateEntry
{
    /// <summary>The stable state identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The RFC 6901-compatible path.</summary>
    public string Path = string.Empty;
    /// <summary>The current value.</summary>
    public UiValue Value = new UiValue();
    /// <summary>The monotonically increasing version.</summary>
    public int Version;
    /// <summary>The last mutation origin.</summary>
    public string Origin = string.Empty;
    /// <summary>The pending patch operations.</summary>
    public UiStatePatch[] Patches = System.Array.Empty<UiStatePatch>();
    /// <summary>The emitted change notifications.</summary>
    public UiStateChange[] Changes = System.Array.Empty<UiStateChange>();
    /// <summary>The derived values dependent on this entry.</summary>
    public UiDerivedState[] Derived = System.Array.Empty<UiDerivedState>();
    /// <summary>The post-commit effects.</summary>
    public UiEffect[] Effects = System.Array.Empty<UiEffect>();
    /// <summary>The observer subscriptions.</summary>
    public UiSubscription[] Subscriptions = System.Array.Empty<UiSubscription>();
}

/// <summary>Describes one state change notification.</summary>
public class UiStateChange
{
    /// <summary>The state identifier.</summary>
    public string StateId = string.Empty;
    /// <summary>The changed path.</summary>
    public string Path = string.Empty;
    /// <summary>The previous value.</summary>
    public UiValue? PreviousValue;
    /// <summary>The current value.</summary>
    public UiValue? CurrentValue;
    /// <summary>The resulting version.</summary>
    public int Version;
    /// <summary>The mutation origin.</summary>
    public string Origin = string.Empty;
}

/// <summary>Describes one bounded state patch operation.</summary>
public class UiStatePatch
{
    /// <summary>The add, remove, replace, move, copy, or test token.</summary>
    public string Operation = string.Empty;
    /// <summary>The target path.</summary>
    public string Path = string.Empty;
    /// <summary>The optional source path.</summary>
    public string? FromPath;
    /// <summary>The optional operation value.</summary>
    public UiValue? Value;
}

/// <summary>Describes memoized state derived from explicit dependencies.</summary>
public class UiDerivedState
{
    /// <summary>The stable derived-state identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The dependency state identifiers.</summary>
    public string[] DependencyIds = System.Array.Empty<string>();
    /// <summary>The evaluator handler identifier.</summary>
    public string HandlerId = string.Empty;
    /// <summary>The memoized value.</summary>
    public UiValue Value = new UiValue();
    /// <summary>The memoized version.</summary>
    public int Version;
    /// <summary>Whether reevaluation is required.</summary>
    public bool IsDirty = true;
}

/// <summary>Describes a post-commit effect and cleanup handler.</summary>
public class UiEffect
{
    /// <summary>The stable effect identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The dependency state identifiers.</summary>
    public string[] DependencyIds = System.Array.Empty<string>();
    /// <summary>The effect handler identifier.</summary>
    public string HandlerId = string.Empty;
    /// <summary>The optional cleanup handler identifier.</summary>
    public string? CleanupHandlerId;
    /// <summary>The scheduled generation.</summary>
    public int Generation;
    /// <summary>Whether the effect is cancelled.</summary>
    public bool IsCancelled;
}

/// <summary>Describes a state observer subscription.</summary>
public class UiSubscription
{
    /// <summary>The stable subscription identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The observed state identifier.</summary>
    public string StateId = string.Empty;
    /// <summary>The optional observed path.</summary>
    public string? Path;
    /// <summary>The observer handler identifier.</summary>
    public string HandlerId = string.Empty;
    /// <summary>The optional lifetime scope identifier.</summary>
    public string? ScopeId;
    /// <summary>Whether the subscription is active.</summary>
    public bool IsActive = true;
}

/// <summary>Associates a state entry with its owning state scope.</summary>
public class UiScopedStateEntry
{
    /// <summary>The normalized state scope identifier.</summary>
    public string ScopeId = string.Empty;
    /// <summary>The copied state entry.</summary>
    public UiStateEntry Entry = new UiStateEntry();
}

/// <summary>Associates a state change with its owning state scope.</summary>
public class UiScopedStateChange
{
    /// <summary>The normalized state scope identifier.</summary>
    public string ScopeId = string.Empty;
    /// <summary>The copied state change.</summary>
    public UiStateChange Change = new UiStateChange();
}

/// <summary>Contains the result of one state entry operation.</summary>
public class UiStateResult
{
    /// <summary>Whether the operation and observer delivery succeeded.</summary>
    public bool Success;
    /// <summary>Whether the operation changed stored state.</summary>
    public bool Applied;
    /// <summary>The zero-based failed patch operation index, or <c>-1</c>.</summary>
    public int OperationIndex = -1;
    /// <summary>The normalized state scope identifier.</summary>
    public string ScopeId = string.Empty;
    /// <summary>The resulting state entry when one exists.</summary>
    public UiStateEntry? Entry;
    /// <summary>The resulting state change when one was applied.</summary>
    public UiStateChange? Change;
    /// <summary>The ordered operation and observer diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Contains the result of one state batch operation.</summary>
public class UiStateBatchResult
{
    /// <summary>Whether the operation and observer delivery succeeded.</summary>
    public bool Success;
    /// <summary>Whether the batch committed changes.</summary>
    public bool Applied;
    /// <summary>The batch identifier.</summary>
    public string BatchId = string.Empty;
    /// <summary>The committed changes in deterministic order.</summary>
    public UiScopedStateChange[] Changes = System.Array.Empty<UiScopedStateChange>();
    /// <summary>The ordered operation and observer diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Contains the result of one named state snapshot operation.</summary>
public class UiStateSnapshotResult
{
    /// <summary>Whether the operation and observer delivery succeeded.</summary>
    public bool Success;
    /// <summary>Whether restoring the snapshot changed stored state.</summary>
    public bool Applied;
    /// <summary>The snapshot identifier.</summary>
    public string SnapshotId = string.Empty;
    /// <summary>The copied entries captured by a created snapshot.</summary>
    public UiScopedStateEntry[] Entries = System.Array.Empty<UiScopedStateEntry>();
    /// <summary>The changes produced by a snapshot restore.</summary>
    public UiScopedStateChange[] Changes = System.Array.Empty<UiScopedStateChange>();
    /// <summary>The ordered operation and observer diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Contains the result of one derived-state operation.</summary>
public class UiDerivedStateResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success;
    /// <summary>Whether the operation changed the derived state.</summary>
    public bool Applied;
    /// <summary>The normalized state scope identifier.</summary>
    public string ScopeId = string.Empty;
    /// <summary>The resulting derived-state definition.</summary>
    public UiDerivedState? Derived;
    /// <summary>The ordered operation diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Contains the result of one state-effect operation.</summary>
public class UiStateEffectResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success;
    /// <summary>Whether the operation changed the effect state.</summary>
    public bool Applied;
    /// <summary>The normalized state scope identifier.</summary>
    public string ScopeId = string.Empty;
    /// <summary>The resulting effect definition.</summary>
    public UiEffect? Effect;
    /// <summary>The ordered operation diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Contains the result of one state-subscription operation.</summary>
public class UiStateSubscriptionResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success;
    /// <summary>Whether the operation changed subscription state.</summary>
    public bool Applied;
    /// <summary>The resulting subscription definition.</summary>
    public UiSubscription? Subscription;
    /// <summary>The ordered operation diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Describes one projected state observer callback.</summary>
public class UiStateObserverContext
{
    /// <summary>The normalized state scope identifier.</summary>
    public string StateScopeId = string.Empty;
    /// <summary>The stable subscription identifier.</summary>
    public string SubscriptionId = string.Empty;
    /// <summary>The observer handler identifier.</summary>
    public string HandlerId = string.Empty;
    /// <summary>The complete copied state change.</summary>
    public UiStateChange Change = new UiStateChange();
    /// <summary>The previous value projected through the subscription path.</summary>
    public UiValue? PreviousValue;
    /// <summary>The current value projected through the subscription path.</summary>
    public UiValue? CurrentValue;
}
