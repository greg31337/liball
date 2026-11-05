using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Coordinates the validated model, renderer, update engine, interaction services, router, and WebView bridge.</summary>
public interface IUiHost
{
    /// <summary>Validates, normalizes, and stores an application for hosting.</summary>
    /// <param name="application">The application model to host.</param>
    /// <returns>The exceptionless initialization result.</returns>
    UiResult InitializeUiHost(UiApplication? application);

    /// <summary>Starts the bridge session and renders the initial document.</summary>
    /// <param name="sessionId">The bridge session identifier.</param>
    /// <param name="nonce">The bridge session nonce.</param>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The initial full-document render or lifecycle diagnostics.</returns>
    UiRenderResult MountUiHost(string? sessionId, string? nonce, long nowMilliseconds);

    /// <summary>Renders the currently hosted application as a complete document.</summary>
    /// <returns>The full-document render or lifecycle diagnostics.</returns>
    UiRenderResult RenderUiHost();

    /// <summary>Validates and renders a new application state and commits its deterministic DOM update.</summary>
    /// <param name="application">The new declarative application state.</param>
    /// <param name="priority">The update scheduling priority.</param>
    /// <param name="preservation">The optional browser state to preserve through the update.</param>
    /// <returns>The render result with its update batch and acceptance result.</returns>
    UiRenderResult UpdateUiHost(UiApplication? application, UiUpdatePriority priority, UiPreservationState? preservation);

    /// <summary>Dispatches one normalized UI event.</summary>
    /// <param name="eventData">The event data to dispatch.</param>
    /// <returns>The event propagation result.</returns>
    UiEventDispatchResult DispatchUiHostEvent(UiEventData? eventData);

    /// <summary>Dispatches one command invocation.</summary>
    /// <param name="invocation">The command invocation.</param>
    /// <returns>The command execution result.</returns>
    UiCommandResult DispatchUiHostCommand(UiCommandInvocation? invocation);

    /// <summary>Runs one guarded navigation through the configured router.</summary>
    /// <param name="request">The navigation request.</param>
    /// <returns>A task containing the navigation result.</returns>
    Task<UiNavigationResult> NavigateUiHost(UiNavigationRequest? request);

    /// <summary>Validates and dispatches one inbound bridge message.</summary>
    /// <param name="message">The inbound bridge envelope.</param>
    /// <param name="isMainFrame">Whether the message originated in the main frame.</param>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless bridge result.</returns>
    UiResult ReceiveUiHostMessage(UiBridgeEnvelope? message, bool isMainFrame, long nowMilliseconds);

    /// <summary>Gets a detached copy of the currently committed DOM snapshot.</summary>
    /// <returns>The current snapshot, or <c>null</c> before a successful mount.</returns>
    DomSnapshot? GetUiHostSnapshot();

    /// <summary>Rejects the pending browser update and sends a full accepted snapshot for resynchronization.</summary>
    /// <param name="batchId">The pending batch identifier.</param>
    /// <param name="revision">The pending batch revision.</param>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The rejected update and recovery snapshot result.</returns>
    UiUpdateResult RejectUiHostUpdate(string? batchId, int revision, long nowMilliseconds);

    /// <summary>Reconnects a lost transport session and schedules a full snapshot handoff.</summary>
    /// <param name="sessionId">The replacement session identifier.</param>
    /// <param name="nonce">The replacement session nonce.</param>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless reconnect result.</returns>
    UiResult RecoverUiHostTransport(string? sessionId, string? nonce, long nowMilliseconds);

    /// <summary>Gets detached diagnostics routed by host orchestration and recovery.</summary>
    /// <returns>The bounded diagnostics in occurrence order.</returns>
    UiDiagnostic[] GetUiHostDiagnostics();

    /// <summary>Begins orderly bridge shutdown and closes the host lifecycle.</summary>
    /// <param name="nowMilliseconds">The monotonic current time in milliseconds.</param>
    /// <returns>The exceptionless shutdown result.</returns>
    UiResult ShutdownUiHost(long nowMilliseconds);
}
