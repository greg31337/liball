namespace htmlgui;

/// <summary>Describes deterministic document or fragment rendering options.</summary>
public class UiRenderOptions
{
    /// <summary>Whether a complete document is rendered.</summary>
    public bool FullDocument = true;
    /// <summary>Whether hydration markers are emitted.</summary>
    public bool IncludeHydration = true;
    /// <summary>Whether the browser bootstrap is emitted.</summary>
    public bool IncludeBootstrap = true;
    /// <summary>The optional CSP nonce.</summary>
    public string? ContentSecurityPolicyNonce;
    /// <summary>The maximum diagnostic count.</summary>
    public int MaximumDiagnostics = 100;
}

/// <summary>Describes rendered markup, styles, hydration data, and diagnostics.</summary>
public class UiRenderResult
{
    /// <summary>The rendered HTML.</summary>
    public string Html = string.Empty;
    /// <summary>The generated CSS.</summary>
    public string Css = string.Empty;
    /// <summary>The browser bootstrap source.</summary>
    public string Bootstrap = string.Empty;
    /// <summary>The renderer-neutral snapshot.</summary>
    public DomSnapshot? Snapshot;
    /// <summary>The hydration manifest.</summary>
    public UiHydrationManifest? Hydration;
    /// <summary>The canonical resources included in or associated with the output.</summary>
    public UiResource[] Resources = System.Array.Empty<UiResource>();
    /// <summary>The ordered diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
    /// <summary>The deterministic combined output hash.</summary>
    public string Hash = string.Empty;
    /// <summary>The optional incremental update result.</summary>
    public UiUpdateResult? UpdateResult;
    /// <summary>The optional incremental update batch.</summary>
    public UiUpdateBatch? UpdateBatch;
    /// <summary>The optional hydration result.</summary>
    public UiHydrationResult? HydrationResult;
}

/// <summary>Describes an ordered revision transaction of DOM mutations.</summary>
public class UiUpdateBatch
{
    /// <summary>The batch identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The required base revision.</summary>
    public int BaseRevision;
    /// <summary>The resulting revision.</summary>
    public int Revision;
    /// <summary>The update priority.</summary>
    public UiUpdatePriority Priority = UiUpdatePriority.Normal;
    /// <summary>The ordered mutations.</summary>
    public DomMutation[] Mutations = System.Array.Empty<DomMutation>();
    /// <summary>The preservation state.</summary>
    public UiPreservationState Preservation = new UiPreservationState();
}

/// <summary>Describes acceptance, rejection, or recovery for an update batch.</summary>
public class UiUpdateResult
{
    /// <summary>Whether the batch was accepted.</summary>
    public bool Accepted;
    /// <summary>The acknowledged revision.</summary>
    public int Revision;
    /// <summary>Whether a full resynchronization is required.</summary>
    public bool RequiresResync;
    /// <summary>The optional recovery snapshot.</summary>
    public DomSnapshot? Snapshot;
    /// <summary>The ordered diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
    /// <summary>The exceptionless portable result.</summary>
    public UiResult Result = new UiResult();
}

/// <summary>Describes hydration identities, parity hash, and serialized state.</summary>
public class UiHydrationManifest
{
    /// <summary>The protocol version.</summary>
    public int Version = BridgeProtocolVersions.Current;
    /// <summary>The server/client parity hash.</summary>
    public string Hash = string.Empty;
    /// <summary>The node marker values.</summary>
    public UiValueEntry[] Nodes = System.Array.Empty<UiValueEntry>();
    /// <summary>The component instance marker values.</summary>
    public UiValueEntry[] Components = System.Array.Empty<UiValueEntry>();
    /// <summary>The event subscription marker values.</summary>
    public UiValueEntry[] Events = System.Array.Empty<UiValueEntry>();
    /// <summary>The data-binding marker values.</summary>
    public UiValueEntry[] Bindings = System.Array.Empty<UiValueEntry>();
    /// <summary>The serialized state values.</summary>
    public UiValueEntry[] State = System.Array.Empty<UiValueEntry>();
    /// <summary>The style-scope marker values keyed by DOM node identifier.</summary>
    public UiValueEntry[] Styles = System.Array.Empty<UiValueEntry>();
    /// <summary>The canonical route URL represented by the hydrated document.</summary>
    public string? RouteUrl;
    /// <summary>The resolved route data represented by the hydrated document.</summary>
    public UiRouteData? RouteData;
    /// <summary>The portable state associated with the hydrated history entry.</summary>
    public UiValue? HistoryState;
    /// <summary>The stable identifier of the hydrated history entry.</summary>
    public string? HistoryEntryId;
    /// <summary>The navigation generation represented by the hydrated document.</summary>
    public int NavigationGeneration;
}

/// <summary>Describes a hydration comparison and repair plan.</summary>
public class UiHydrationResult
{
    /// <summary>The requested hydration mode.</summary>
    public UiHydrationMode Mode;
    /// <summary>The resulting hydration status.</summary>
    public UiHydrationStatus Status;
    /// <summary>The planned repair mutations.</summary>
    public DomMutation[] Mutations = System.Array.Empty<DomMutation>();
    /// <summary>The ordered mismatch diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
    /// <summary>The server-side canonical parity hash.</summary>
    public string ServerHash = string.Empty;
    /// <summary>The client-side canonical parity hash.</summary>
    public string ClientHash = string.Empty;
    /// <summary>Whether the canonical server and client trees have parity.</summary>
    public bool ParityMatched;
    /// <summary>The detached state values handed to the hydrated runtime.</summary>
    public UiValueEntry[] State = System.Array.Empty<UiValueEntry>();
    /// <summary>The event-listener attachment descriptions.</summary>
    public UiValueEntry[] EventAttachments = System.Array.Empty<UiValueEntry>();
    /// <summary>The data-binding attachment descriptions.</summary>
    public UiValueEntry[] BindingAttachments = System.Array.Empty<UiValueEntry>();
    /// <summary>The ordered component hydration lifecycle notifications.</summary>
    public UiLifecycleNotification[] LifecycleNotifications = System.Array.Empty<UiLifecycleNotification>();
}

/// <summary>Describes user-agent state preserved across a DOM patch.</summary>
public class UiPreservationState
{
    /// <summary>Whether focus can be preserved or restored across the patch.</summary>
    public bool PreserveFocus;
    /// <summary>Whether the focused control's selection can be preserved or restored.</summary>
    public bool PreserveSelection;
    /// <summary>Whether active text composition can be preserved.</summary>
    public bool PreserveComposition;
    /// <summary>Whether at least one captured scroll position can be preserved or restored.</summary>
    public bool PreserveScroll;
    /// <summary>The optional focused node identifier.</summary>
    public string? FocusedNodeId;
    /// <summary>Whether a selection range is supplied.</summary>
    public bool HasSelection;
    /// <summary>The selection start.</summary>
    public int SelectionStart;
    /// <summary>The selection end.</summary>
    public int SelectionEnd;
    /// <summary>Whether input composition is active.</summary>
    public bool IsComposing;
    /// <summary>The scroll offsets keyed by stable node identifier.</summary>
    public UiValueEntry[] ScrollOffsets = System.Array.Empty<UiValueEntry>();
    /// <summary>Whether a stable scroll anchor can be restored after the patch.</summary>
    public bool PreserveScrollAnchor;
    /// <summary>The optional stable identifier of the scroll anchor.</summary>
    public string? ScrollAnchorNodeId;
    /// <summary>Whether captured media playback state can be restored after the patch.</summary>
    public bool PreserveMediaPlayback;
    /// <summary>The captured media playback states keyed by stable media node identifier.</summary>
    public UiValueEntry[] MediaPlaybackStates = System.Array.Empty<UiValueEntry>();
    /// <summary>Whether open overlay state can be restored after the patch.</summary>
    public bool PreserveOpenOverlays;
    /// <summary>The stable identifiers of open overlays.</summary>
    public string[] OpenOverlayIds = System.Array.Empty<string>();
    /// <summary>Whether captured user-edited field values can be restored after the patch.</summary>
    public bool PreserveUserEditedFields;
    /// <summary>The captured user-edited values keyed by stable form-control node identifier.</summary>
    public UiValueEntry[] UserEditedFieldValues = System.Array.Empty<UiValueEntry>();
}
