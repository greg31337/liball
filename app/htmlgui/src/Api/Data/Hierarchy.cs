using System;

namespace htmlgui;

/// <summary>Describes a portable UI application and its independently addressed windows.</summary>
public class UiApplication
{
    /// <summary>The stable application identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The author-supplied application key.</summary>
    public string Key = string.Empty;
    /// <summary>The deterministic seed used when deriving descendant identifiers.</summary>
    public string RootSeed = string.Empty;
    /// <summary>The optional key of the shared theme.</summary>
    public string? ThemeKey;
    /// <summary>The stable identifiers of routes registered for the application.</summary>
    public string[] RouteIds = Array.Empty<string>();
    /// <summary>The stable identifiers of commands registered for the application.</summary>
    public string[] CommandIds = Array.Empty<string>();
    /// <summary>The initial shared application state.</summary>
    public UiValueEntry[] State = Array.Empty<UiValueEntry>();
    /// <summary>The windows owned by the application.</summary>
    public UiWindow[] Windows = Array.Empty<UiWindow>();
    /// <summary>The shared command descriptions.</summary>
    public UiCommand[] Commands = Array.Empty<UiCommand>();
    /// <summary>The shared observable state entries.</summary>
    public UiStateEntry[] StateEntries = Array.Empty<UiStateEntry>();
    /// <summary>The registered routes.</summary>
    public UiRoute[] Routes = Array.Empty<UiRoute>();
    /// <summary>The optional shared theme.</summary>
    public UiTheme? Theme;
    /// <summary>The deterministic rendering options.</summary>
    public UiRenderOptions RenderOptions = new UiRenderOptions();
    /// <summary>The optional most recent render result.</summary>
    public UiRenderResult? RenderResult;
}

/// <summary>Describes an independently addressed browser or WebView window.</summary>
public class UiWindow
{
    /// <summary>The stable window identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The author-supplied window key.</summary>
    public string Key = string.Empty;
    /// <summary>The document-level metadata inherited by pages in the window.</summary>
    public UiDocumentMetadata Document = new UiDocumentMetadata();
    /// <summary>The pages addressable within the window.</summary>
    public UiPage[] Pages = Array.Empty<UiPage>();
    /// <summary>The optional key of the page currently displayed in the window.</summary>
    public string? CurrentPageKey;
    /// <summary>The views displayed in named overlay regions.</summary>
    public UiView[] Overlays = Array.Empty<UiView>();
    /// <summary>The host capability names available to this window.</summary>
    public string[] HostCapabilities = Array.Empty<string>();
    /// <summary>Whether the window is active.</summary>
    public bool IsActive = true;
    /// <summary>The bridge options for the window.</summary>
    public UiBridgeOptions BridgeOptions = new UiBridgeOptions();
    /// <summary>The negotiated transport capabilities.</summary>
    public UiTransportCapabilities Transport = new UiTransportCapabilities();
    /// <summary>The current focus state.</summary>
    public UiFocusState Focus = new UiFocusState();
    /// <summary>The focus scopes registered in this window.</summary>
    public UiFocusScope[] FocusScopes = Array.Empty<UiFocusScope>();
    /// <summary>The focus targets registered in this window.</summary>
    public UiFocusTarget[] FocusTargets = Array.Empty<UiFocusTarget>();
    /// <summary>The optional most recent focus operation result.</summary>
    public UiFocusResult? FocusResult;
    /// <summary>The current navigation request.</summary>
    public UiNavigationRequest? NavigationRequest;
    /// <summary>The current navigation result.</summary>
    public UiNavigationResult? NavigationResult;
    /// <summary>The portable history entries.</summary>
    public UiHistoryEntry[] History = Array.Empty<UiHistoryEntry>();
    /// <summary>The optional most recent bridge envelope.</summary>
    public UiBridgeEnvelope? LastBridgeEnvelope;
}

/// <summary>Describes a routable page and its shell, content, and fallback views.</summary>
public class UiPage
{
    /// <summary>The stable page identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The author-supplied page key.</summary>
    public string Key = string.Empty;
    /// <summary>The optional stable route identifier associated with the page.</summary>
    public string? RouteId;
    /// <summary>The page title, overriding the inherited document title when non-empty.</summary>
    public string Title = string.Empty;
    /// <summary>The optional inherited-language override.</summary>
    public string? Language;
    /// <summary>The optional text-direction override, such as <c>ltr</c> or <c>rtl</c>.</summary>
    public string? TextDirection;
    /// <summary>The page-specific document metadata.</summary>
    public UiDocumentMetadata Metadata = new UiDocumentMetadata();
    /// <summary>The resources referenced by the page.</summary>
    public UiResource[] Resources = Array.Empty<UiResource>();
    /// <summary>The optional persistent shell or chrome view.</summary>
    public UiView? ShellView;
    /// <summary>The optional primary content view.</summary>
    public UiView? ContentView;
    /// <summary>The optional view shown when no route matches.</summary>
    public UiView? NotFoundView;
    /// <summary>The optional view shown after an unrecovered error.</summary>
    public UiView? ErrorView;
    /// <summary>The optional view shown while content is pending.</summary>
    public UiView? LoadingView;
    /// <summary>The page-level styles.</summary>
    public UiStyle[] Styles = Array.Empty<UiStyle>();
}

/// <summary>Describes a mounted view, its parameters, local scope, and content root.</summary>
public class UiView
{
    /// <summary>The stable view identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The author-supplied view key.</summary>
    public string Key = string.Empty;
    /// <summary>The optional stable component type identifier rendered by the view.</summary>
    public string? ComponentTypeId;
    /// <summary>The component parameter values supplied to the view.</summary>
    public UiValueEntry[] Parameters = Array.Empty<UiValueEntry>();
    /// <summary>The optional local state-scope identifier.</summary>
    public string? LocalStateScopeId;
    /// <summary>The optional root node of the view's portable content.</summary>
    public UiNode? ContentRoot;
    /// <summary>The current lifecycle stage represented by the description.</summary>
    public UiLifecycleStage LifecycleStage;
    /// <summary>The optional focus-scope identifier owned by the view.</summary>
    public string? FocusScopeId;
    /// <summary>The optional fallback view used as an error boundary.</summary>
    public UiView? ErrorBoundaryView;
    /// <summary>Whether the view is currently active.</summary>
    public bool IsActive = true;
    /// <summary>The optional component definition.</summary>
    public UiComponentDefinition? ComponentDefinition;
    /// <summary>The optional component context.</summary>
    public UiComponentContext? ComponentContext;
    /// <summary>The optional pending render request.</summary>
    public UiComponentRenderRequest? RenderRequest;
    /// <summary>The lifecycle notifications.</summary>
    public UiLifecycleNotification[] LifecycleNotifications = Array.Empty<UiLifecycleNotification>();
    /// <summary>The optional component error-boundary state.</summary>
    public UiErrorBoundaryState? ErrorBoundaryState;
    /// <summary>The optional pending focus request.</summary>
    public UiFocusRequest? FocusRequest;
    /// <summary>The keyboard maps active in the view.</summary>
    public UiKeyboardMap[] KeyboardMaps = Array.Empty<UiKeyboardMap>();
    /// <summary>The current type-ahead state.</summary>
    public UiTypeAheadState TypeAhead = new UiTypeAheadState();
}

/// <summary>Describes document metadata that can be inherited and overridden by a page.</summary>
public class UiDocumentMetadata
{
    /// <summary>The document title.</summary>
    public string Title = string.Empty;
    /// <summary>The optional document language tag.</summary>
    public string? Language;
    /// <summary>The optional document text direction, such as <c>ltr</c> or <c>rtl</c>.</summary>
    public string? TextDirection;
    /// <summary>The character encoding emitted for the document.</summary>
    public string CharacterSet = "utf-8";
    /// <summary>The viewport metadata content.</summary>
    public string Viewport = "width=device-width, initial-scale=1";
    /// <summary>The optional base URL used to resolve relative references.</summary>
    public string? BaseUrl;
    /// <summary>The optional document description.</summary>
    public string? Description;
    /// <summary>The optional application name.</summary>
    public string? ApplicationName;
    /// <summary>The optional nonce applied to generated content-security-policy-aware resources.</summary>
    public string? ContentSecurityPolicyNonce;
    /// <summary>Additional string-keyed metadata values.</summary>
    public UiValueEntry[] Values = Array.Empty<UiValueEntry>();
}

/// <summary>Describes a stylesheet, script, media source, or document resource hint.</summary>
public class UiResource
{
    /// <summary>The stable author-supplied resource key.</summary>
    public string Key = string.Empty;
    /// <summary>The resource URL.</summary>
    public string Url = string.Empty;
    /// <summary>The relationship token, such as <c>stylesheet</c> or <c>preload</c>.</summary>
    public string Relationship = string.Empty;
    /// <summary>The optional media type.</summary>
    public string? MimeType;
    /// <summary>The optional media condition.</summary>
    public string? Media;
    /// <summary>The optional subresource-integrity value.</summary>
    public string? Integrity;
    /// <summary>The optional cross-origin mode.</summary>
    public string? CrossOrigin;
    /// <summary>The optional referrer-policy token.</summary>
    public string? ReferrerPolicy;
    /// <summary>Whether a script resource uses module semantics.</summary>
    public bool IsModule;
    /// <summary>Additional resource attributes represented as portable values.</summary>
    public UiValueEntry[] Attributes = Array.Empty<UiValueEntry>();
}

/// <summary>Describes content projected into a named component slot.</summary>
public class UiSlot
{
    /// <summary>The slot name.</summary>
    public string Name = string.Empty;
    /// <summary>The content supplied for the slot.</summary>
    public UiNode[] Content = Array.Empty<UiNode>();
    /// <summary>The content used when no supplied content is present.</summary>
    public UiNode[] FallbackContent = Array.Empty<UiNode>();
}

/// <summary>Describes content projected into a named overlay region outside its logical parent.</summary>
public class UiPortal
{
    /// <summary>The stable author-supplied portal key.</summary>
    public string Key = string.Empty;
    /// <summary>The name of the destination overlay region.</summary>
    public string TargetRegion = string.Empty;
    /// <summary>The content rendered in the destination region.</summary>
    public UiNode[] Content = Array.Empty<UiNode>();
    /// <summary>Whether the portal content is active.</summary>
    public bool IsActive = true;
}
