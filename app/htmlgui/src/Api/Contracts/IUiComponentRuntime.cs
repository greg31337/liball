using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Coordinates component definitions, instances, updates, contexts, lifecycle, and disposal.</summary>
public interface IUiComponentRuntime
{
    /// <summary>Registers one copied component definition.</summary>
    /// <param name="definition">The component definition.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult RegisterUiComponentDefinition(UiComponentDefinition? definition);

    /// <summary>Unregisters one unused component definition.</summary>
    /// <param name="typeId">The stable component type identifier.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult UnregisterUiComponentDefinition(string? typeId);

    /// <summary>Registers a renderer under its stable handler identifier.</summary>
    /// <param name="handlerId">The stable render-handler identifier.</param>
    /// <param name="renderer">The renderer callback.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiComponentResult RegisterUiComponentRenderer(string? handlerId, IUiComponentRenderer? renderer);

    /// <summary>Unregisters a renderer callback.</summary>
    /// <param name="handlerId">The stable render-handler identifier.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiComponentResult UnregisterUiComponentRenderer(string? handlerId);

    /// <summary>Registers a lifecycle or error callback under its stable handler identifier.</summary>
    /// <param name="handlerId">The stable lifecycle-handler identifier.</param>
    /// <param name="handler">The lifecycle callback.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiComponentResult RegisterUiLifecycleHandler(string? handlerId, IUiLifecycleHandler? handler);

    /// <summary>Unregisters a lifecycle or error callback.</summary>
    /// <param name="handlerId">The stable lifecycle-handler identifier.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiComponentResult UnregisterUiLifecycleHandler(string? handlerId);

    /// <summary>Creates or preserves and activates one keyed component instance.</summary>
    /// <param name="rootSeed">The deterministic root seed.</param>
    /// <param name="parentComponentId">The optional parent component identifier.</param>
    /// <param name="typeId">The registered component type.</param>
    /// <param name="key">The stable instance key.</param>
    /// <param name="parameters">The supplied parameter values.</param>
    /// <param name="slots">The projected slots.</param>
    /// <param name="contextValues">The provided context values.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult MountUiComponent(string? rootSeed, string? parentComponentId, string? typeId, string? key, UiValueEntry[]? parameters, UiSlot[]? slots, UiValueEntry[]? contextValues);

    /// <summary>Remounts one keyed component with a fresh local state scope.</summary>
    /// <param name="rootSeed">The deterministic root seed.</param>
    /// <param name="parentComponentId">The optional parent component identifier.</param>
    /// <param name="typeId">The registered component type.</param>
    /// <param name="key">The stable instance key.</param>
    /// <param name="parameters">The supplied parameter values.</param>
    /// <param name="slots">The projected slots.</param>
    /// <param name="contextValues">The provided context values.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult RemountUiComponent(string? rootSeed, string? parentComponentId, string? typeId, string? key, UiValueEntry[]? parameters, UiSlot[]? slots, UiValueEntry[]? contextValues);

    /// <summary>Reads one copied component instance.</summary>
    /// <param name="componentId">The stable component identifier.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult ReadUiComponent(string? componentId);

    /// <summary>Updates component parameters.</summary>
    /// <param name="componentId">The stable component identifier.</param>
    /// <param name="parameters">The supplied parameter values.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult UpdateUiComponentParameters(string? componentId, UiValueEntry[]? parameters);

    /// <summary>Activates or conditionally deactivates a preserved component subtree.</summary>
    /// <param name="componentId">The stable component identifier.</param>
    /// <param name="isPresent">Whether the subtree is currently present.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult SetUiComponentPresence(string? componentId, bool isPresent);

    /// <summary>Provides one copied context value from a component.</summary>
    /// <param name="componentId">The stable component identifier.</param>
    /// <param name="contextKey">The context key.</param>
    /// <param name="value">The portable context value.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult ProvideUiComponentContext(string? componentId, string? contextKey, UiValue? value);

    /// <summary>Consumes the nearest copied ancestor context value.</summary>
    /// <param name="componentId">The stable component identifier.</param>
    /// <param name="contextKey">The context key.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult ConsumeUiComponentContext(string? componentId, string? contextKey);

    /// <summary>Configures fallback content and a reset key on an error boundary.</summary>
    /// <param name="componentId">The stable boundary identifier.</param>
    /// <param name="fallback">The optional fallback content.</param>
    /// <param name="resetKey">The optional reset key.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult ConfigureUiComponentErrorBoundary(string? componentId, UiNode? fallback, string? resetKey);

    /// <summary>Clears boundary error state when its reset key changes.</summary>
    /// <param name="componentId">The stable boundary identifier.</param>
    /// <param name="resetKey">The next optional reset key.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult ResetUiComponentErrorBoundary(string? componentId, string? resetKey);

    /// <summary>Queues one component invalidation.</summary>
    /// <param name="componentId">The stable component identifier.</param>
    /// <param name="priority">The requested update priority.</param>
    /// <param name="isHydration">Whether rendering participates in hydration.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult InvalidateUiComponent(string? componentId, UiUpdatePriority priority, bool isHydration);

    /// <summary>Processes one scheduled update cycle and its renderer and lifecycle tasks.</summary>
    /// <returns>A task containing the completed component result.</returns>
    Task<UiComponentResult> ProcessUiComponentUpdates();

    /// <summary>Disposes one component subtree and owned resources.</summary>
    /// <param name="componentId">The stable component identifier.</param>
    /// <returns>The exceptionless component result.</returns>
    UiComponentResult DisposeUiComponent(string? componentId);
}
