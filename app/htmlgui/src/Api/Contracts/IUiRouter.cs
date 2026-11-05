using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Registers routes and coordinates resolution, guarded navigation, history, and observers.</summary>
public interface IUiRouter
{
    /// <summary>Configures copied page metadata, the base path, and external-navigation policy.</summary>
    /// <param name="pages">The pages available for route metadata selection.</param>
    /// <param name="document">The optional inherited document metadata.</param>
    /// <param name="basePath">The application base path.</param>
    /// <param name="allowExternalNavigation">Whether safe external destinations may be handed to the host.</param>
    /// <returns>The exceptionless configuration result.</returns>
    UiNavigationResult ConfigureUiRouting(UiPage[]? pages, UiDocumentMetadata? document, string? basePath, bool allowExternalNavigation);

    /// <summary>Registers one copied route.</summary>
    /// <param name="route">The route definition.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiNavigationResult RegisterUiRoute(UiRoute? route);

    /// <summary>Unregisters one route that has no registered children.</summary>
    /// <param name="routeId">The stable route identifier.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiNavigationResult UnregisterUiRoute(string? routeId);

    /// <summary>Resolves a route without changing history or invoking callbacks.</summary>
    /// <param name="url">The URL or application-relative path.</param>
    /// <returns>The exceptionless route-resolution result.</returns>
    UiNavigationResult ResolveUiRoute(string? url);

    /// <summary>Initializes history from the browser's current deep link.</summary>
    /// <param name="url">The initial URL.</param>
    /// <param name="state">The optional portable history state.</param>
    /// <param name="generation">The browser navigation generation.</param>
    /// <returns>The exceptionless navigation result.</returns>
    UiNavigationResult InitializeUiNavigation(string? url, UiValue? state, int generation);

    /// <summary>Runs one guarded navigation and its ordered callbacks.</summary>
    /// <param name="request">The navigation request.</param>
    /// <returns>A task containing the exceptionless navigation result.</returns>
    Task<UiNavigationResult> NavigateUi(UiNavigationRequest? request);

    /// <summary>Cancels the active navigation generation and delivers cancellation callbacks.</summary>
    /// <param name="generation">The active navigation generation.</param>
    /// <returns>A task containing the exceptionless cancellation result.</returns>
    Task<UiNavigationResult> CancelUiNavigation(int generation);

    /// <summary>Captures the current history entry's scroll and focus restoration state.</summary>
    /// <param name="scrollX">The horizontal scroll offset.</param>
    /// <param name="scrollY">The vertical scroll offset.</param>
    /// <param name="focusedNodeId">The optional focused node identifier.</param>
    /// <returns>The exceptionless state result.</returns>
    UiNavigationResult CaptureUiNavigationRestoration(double scrollX, double scrollY, string? focusedNodeId);

    /// <summary>Registers a cancellable navigation guard.</summary>
    /// <param name="handlerId">The stable callback identifier.</param>
    /// <param name="observer">The guard callback.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiNavigationResult RegisterUiNavigationGuard(string? handlerId, IUiNavigationObserver? observer);

    /// <summary>Unregisters a navigation guard.</summary>
    /// <param name="handlerId">The stable callback identifier.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiNavigationResult UnregisterUiNavigationGuard(string? handlerId);

    /// <summary>Registers a navigation lifecycle observer.</summary>
    /// <param name="handlerId">The stable callback identifier.</param>
    /// <param name="observer">The observer callback.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiNavigationResult RegisterUiNavigationObserver(string? handlerId, IUiNavigationObserver? observer);

    /// <summary>Unregisters a navigation lifecycle observer.</summary>
    /// <param name="handlerId">The stable callback identifier.</param>
    /// <returns>The exceptionless registration result.</returns>
    UiNavigationResult UnregisterUiNavigationObserver(string? handlerId);
}
