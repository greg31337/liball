namespace htmlgui;

/// <summary>Describes a route template, nesting, and destination view.</summary>
public class UiRoute
{
    /// <summary>The stable route identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The route template.</summary>
    public string Template = string.Empty;
    /// <summary>The parsed segments.</summary>
    public UiRouteSegment[] Segments = System.Array.Empty<UiRouteSegment>();
    /// <summary>The optional parent route identifier.</summary>
    public string? ParentId;
    /// <summary>The optional destination view.</summary>
    public UiView? View;
    /// <summary>The optional named outlet in the parent route view, defaulting to <c>outlet</c>.</summary>
    public string? OutletName;
    /// <summary>The optional URL to which a successful match redirects.</summary>
    public string? RedirectUrl;
    /// <summary>Whether this is an index route.</summary>
    public bool IsIndex;
    /// <summary>Whether this is a not-found route.</summary>
    public bool IsNotFound;
}

/// <summary>Describes one parsed route segment.</summary>
public class UiRouteSegment
{
    /// <summary>The literal, parameter, or catch-all kind token.</summary>
    public string Kind = string.Empty;
    /// <summary>The literal text or parameter name.</summary>
    public string Value = string.Empty;
    /// <summary>The optional constraint token.</summary>
    public string? Constraint;
    /// <summary>The optional default value.</summary>
    public string? DefaultValue;
    /// <summary>Whether the segment is optional.</summary>
    public bool IsOptional;
}

/// <summary>Describes resolved route parameters, query values, and fragment.</summary>
public class UiRouteData
{
    /// <summary>The matched route identifier.</summary>
    public string RouteId = string.Empty;
    /// <summary>The decoded path parameters.</summary>
    public UiValueEntry[] Parameters = System.Array.Empty<UiValueEntry>();
    /// <summary>The decoded query values.</summary>
    public UiValueEntry[] Query = System.Array.Empty<UiValueEntry>();
    /// <summary>The optional decoded fragment.</summary>
    public string? Fragment;
}

/// <summary>Describes a navigation request.</summary>
public class UiNavigationRequest
{
    /// <summary>The navigation kind.</summary>
    public UiNavigationKind Kind;
    /// <summary>The requested URL or path.</summary>
    public string Url = string.Empty;
    /// <summary>The optional portable history state.</summary>
    public UiValue? State;
    /// <summary>The navigation generation.</summary>
    public int Generation;
    /// <summary>Whether focus should move after navigation.</summary>
    public bool MoveFocus = true;
}

/// <summary>Describes exceptionless navigation resolution.</summary>
public class UiNavigationResult
{
    /// <summary>Whether the routing or navigation operation succeeded.</summary>
    public bool Success;
    /// <summary>Whether the operation changed router state.</summary>
    public bool Applied;
    /// <summary>Whether the navigation is waiting for a guard.</summary>
    public bool IsPending;
    /// <summary>Whether the supplied generation was stale and ignored.</summary>
    public bool IsStale;
    /// <summary>The navigation status.</summary>
    public UiNavigationStatus Status;
    /// <summary>The optional resolved route data.</summary>
    public UiRouteData? RouteData;
    /// <summary>The optional resolved and layout-composed view.</summary>
    public UiView? View;
    /// <summary>The optional page selected for the resolved route.</summary>
    public UiPage? Page;
    /// <summary>The selected document title.</summary>
    public string Title = string.Empty;
    /// <summary>The optional selected document metadata.</summary>
    public UiDocumentMetadata? Metadata;
    /// <summary>The optional redirect URL.</summary>
    public string? RedirectUrl;
    /// <summary>The canonical destination URL.</summary>
    public string CanonicalUrl = string.Empty;
    /// <summary>The portable state associated with the destination history entry.</summary>
    public UiValue? State;
    /// <summary>The optional external URL handed to the host.</summary>
    public string? ExternalUrl;
    /// <summary>The optional decoded same-document fragment target.</summary>
    public string? FragmentTargetId;
    /// <summary>Whether the document should scroll to its initial position.</summary>
    public bool ScrollToTop;
    /// <summary>Whether a saved scroll offset should be restored.</summary>
    public bool HasScrollOffset;
    /// <summary>The horizontal scroll offset to restore.</summary>
    public double ScrollX;
    /// <summary>The vertical scroll offset to restore.</summary>
    public double ScrollY;
    /// <summary>Whether focus should move to the navigated content or fragment target.</summary>
    public bool MoveFocus;
    /// <summary>Whether focus should be restored to a saved node.</summary>
    public bool RestoreFocus;
    /// <summary>The optional node that should receive focus.</summary>
    public string? FocusTargetId;
    /// <summary>The copied navigation history after the operation.</summary>
    public UiHistoryEntry[] History = System.Array.Empty<UiHistoryEntry>();
    /// <summary>The current index in <see cref="UiNavigationResult.History"/>.</summary>
    public int HistoryIndex = -1;
    /// <summary>The optional diagnostic.</summary>
    public UiDiagnostic? Diagnostic;
    /// <summary>The ordered routing, navigation, guard, and observer diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
    /// <summary>The completed navigation generation.</summary>
    public int Generation;
}

/// <summary>Describes one copied navigation lifecycle callback.</summary>
public class UiNavigationNotification
{
    /// <summary>The callback stage token: before, pending, after, cancel, or error.</summary>
    public string Stage = string.Empty;
    /// <summary>The navigation generation.</summary>
    public int Generation;
    /// <summary>The copied navigation request.</summary>
    public UiNavigationRequest Request = new UiNavigationRequest();
    /// <summary>The copied navigation state at this stage.</summary>
    public UiNavigationResult Navigation = new UiNavigationResult();
    /// <summary>The optional stable cancellation or error code.</summary>
    public string? Code;
}

/// <summary>Describes an exceptionless navigation guard or observer callback outcome.</summary>
public class UiNavigationObserverResult
{
    /// <summary>Whether callback handling completed successfully.</summary>
    public bool Success = true;
    /// <summary>Whether a guard permits navigation to continue.</summary>
    public bool AllowNavigation = true;
    /// <summary>The ordered callback diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Describes one portable browser-history entry.</summary>
public class UiHistoryEntry
{
    /// <summary>The stable entry identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The canonical URL.</summary>
    public string Url = string.Empty;
    /// <summary>The portable history state.</summary>
    public UiValue? State;
    /// <summary>Whether a scroll offset is supplied.</summary>
    public bool HasScrollOffset;
    /// <summary>The horizontal scroll offset.</summary>
    public double ScrollX;
    /// <summary>The vertical scroll offset.</summary>
    public double ScrollY;
    /// <summary>The optional focused node identifier.</summary>
    public string? FocusedNodeId;
}
