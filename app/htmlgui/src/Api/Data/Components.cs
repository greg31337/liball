namespace htmlgui;

/// <summary>Describes a reusable component type and handler identities.</summary>
public class UiComponentDefinition
{
    /// <summary>The stable component type identifier.</summary>
    public string TypeId = string.Empty;
    /// <summary>The render-handler identifier.</summary>
    public string RenderHandlerId = string.Empty;
    /// <summary>The optional lifecycle-handler identifier.</summary>
    public string? LifecycleHandlerId;
    /// <summary>The optional error-handler identifier.</summary>
    public string? ErrorHandlerId;
    /// <summary>The default parameters.</summary>
    public UiValueEntry[] DefaultParameters = System.Array.Empty<UiValueEntry>();
}

/// <summary>Describes component identity, parameters, state scope, slots, and context.</summary>
public class UiComponentContext
{
    /// <summary>The stable component instance identifier.</summary>
    public string ComponentId = string.Empty;
    /// <summary>The stable component type identifier.</summary>
    public string TypeId = string.Empty;
    /// <summary>The instance key.</summary>
    public string Key = string.Empty;
    /// <summary>The parameter values.</summary>
    public UiValueEntry[] Parameters = System.Array.Empty<UiValueEntry>();
    /// <summary>The local state-scope identifier.</summary>
    public string StateScopeId = string.Empty;
    /// <summary>The projected slots.</summary>
    public UiSlot[] Slots = System.Array.Empty<UiSlot>();
    /// <summary>The provided context values.</summary>
    public UiValueEntry[] ContextValues = System.Array.Empty<UiValueEntry>();
}

/// <summary>Describes one synchronous component render request.</summary>
public class UiComponentRenderRequest
{
    /// <summary>The component context.</summary>
    public UiComponentContext Context = new UiComponentContext();
    /// <summary>The render generation.</summary>
    public int Generation;
    /// <summary>The update priority.</summary>
    public UiUpdatePriority Priority = UiUpdatePriority.Normal;
    /// <summary>Whether the request is part of hydration.</summary>
    public bool IsHydration;
}

/// <summary>Describes one ordered lifecycle notification.</summary>
public class UiLifecycleNotification
{
    /// <summary>The component instance identifier.</summary>
    public string ComponentId = string.Empty;
    /// <summary>The lifecycle stage.</summary>
    public UiLifecycleStage Stage;
    /// <summary>The generation.</summary>
    public int Generation;
    /// <summary>The optional diagnostic associated with the stage.</summary>
    public UiDiagnostic? Diagnostic;
}

/// <summary>Describes component error-boundary state and fallback content.</summary>
public class UiErrorBoundaryState
{
    /// <summary>The stable boundary component identifier.</summary>
    public string BoundaryId = string.Empty;
    /// <summary>Whether the boundary currently contains an error.</summary>
    public bool HasError;
    /// <summary>The optional error diagnostic.</summary>
    public UiDiagnostic? Diagnostic;
    /// <summary>The optional fallback content.</summary>
    public UiNode? Fallback;
    /// <summary>The reset key.</summary>
    public string? ResetKey;
}

/// <summary>Describes the outcome of a component renderer callback.</summary>
public class UiComponentRenderResult
{
    /// <summary>Whether rendering completed successfully.</summary>
    public bool Success = true;
    /// <summary>The rendered component content.</summary>
    public UiNode? Content;
    /// <summary>The ordered renderer diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Describes the outcome of a component lifecycle callback.</summary>
public class UiLifecycleHandlerResult
{
    /// <summary>Whether lifecycle handling completed successfully.</summary>
    public bool Success = true;
    /// <summary>The ordered lifecycle-handler diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}

/// <summary>Contains a component runtime operation or update result.</summary>
public class UiComponentResult
{
    /// <summary>Whether the operation and all dispatched callbacks succeeded.</summary>
    public bool Success = true;
    /// <summary>Whether the operation changed component runtime state.</summary>
    public bool Applied;
    /// <summary>The copied component definition when one is relevant.</summary>
    public UiComponentDefinition? Definition;
    /// <summary>The copied component context when one is relevant.</summary>
    public UiComponentContext? Context;
    /// <summary>The copied rendered or fallback node when one is available.</summary>
    public UiNode? Content;
    /// <summary>The copied consumed context value when one is available.</summary>
    public UiValue? Value;
    /// <summary>The copied error-boundary state when one is relevant.</summary>
    public UiErrorBoundaryState? ErrorBoundary;
    /// <summary>The ordered render requests processed by an update cycle.</summary>
    public UiComponentRenderRequest[] RenderRequests = System.Array.Empty<UiComponentRenderRequest>();
    /// <summary>The ordered lifecycle notifications dispatched by the operation.</summary>
    public UiLifecycleNotification[] LifecycleNotifications = System.Array.Empty<UiLifecycleNotification>();
    /// <summary>The ordered operation and callback diagnostics.</summary>
    public UiDiagnostic[] Diagnostics = System.Array.Empty<UiDiagnostic>();
}
