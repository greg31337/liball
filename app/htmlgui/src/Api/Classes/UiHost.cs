using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Coordinates high-level UI hosting over injected rendering, interaction, routing, update, and transport services.</summary>
public class UiHost : IUiHost, IUiBridgeMessageHandler
{
    internal IUiModelValidator Validator;
    internal IUiRenderer Renderer;
    internal IUiUpdateEngine Updates;
    internal IUiEventDispatcher Events;
    internal IUiBindingEngine Bindings;
    internal IUiValidationEngine Validation;
    internal IUiCommandDispatcher Commands;
    internal IUiComponentRuntime Components;
    internal IUiRouter Router;
    internal IUiFocusController Focus;
    internal IUiMessageTransport Transport;
    internal UpdateScheduler Scheduler;
    internal IUiWebViewBridge? Bridge;
    internal UiApplication? Application;
    internal DomSnapshot? Snapshot;
    internal bool Initialized;
    internal bool Mounted;
    internal bool Closed;
    internal long TransactionMilliseconds;
    internal List<UiDiagnostic> HostDiagnostics;
    internal List<string> RegisteredRouteIds;
    internal UiApplication? PendingApplication;
    internal DomSnapshot? PendingSnapshot;
    internal UiUpdateBatch? PendingBatch;
    internal string PendingStateSnapshotId = string.Empty;
    internal int PendingBridgeSequence = -1;
    internal bool RecoveryPending;

    /// <summary>Initializes a UI host with its required public services and host transport.</summary>
    /// <param name="validator">The portable model validator.</param>
    /// <param name="renderer">The deterministic UI renderer.</param>
    /// <param name="updates">The transactional DOM update engine.</param>
    /// <param name="events">The normalized event dispatcher.</param>
    /// <param name="bindings">The state and binding engine.</param>
    /// <param name="validation">The form validation engine.</param>
    /// <param name="commands">The command dispatcher.</param>
    /// <param name="components">The component runtime.</param>
    /// <param name="router">The navigation router.</param>
    /// <param name="focus">The focus controller.</param>
    /// <param name="transport">The host-specific bridge transport.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an injected dependency is null.</exception>
    public UiHost(IUiModelValidator? validator, IUiRenderer? renderer, IUiUpdateEngine? updates, IUiEventDispatcher? events, IUiBindingEngine? bindings, IUiValidationEngine? validation, IUiCommandDispatcher? commands, IUiComponentRuntime? components, IUiRouter? router, IUiFocusController? focus, IUiMessageTransport? transport)
    {
        Validator = validator ?? throw new ArgumentNullException("validator");
        Renderer = renderer ?? throw new ArgumentNullException("renderer");
        Updates = updates ?? throw new ArgumentNullException("updates");
        Events = events ?? throw new ArgumentNullException("events");
        Bindings = bindings ?? throw new ArgumentNullException("bindings");
        Validation = validation ?? throw new ArgumentNullException("validation");
        Commands = commands ?? throw new ArgumentNullException("commands");
        Components = components ?? throw new ArgumentNullException("components");
        Router = router ?? throw new ArgumentNullException("router");
        Focus = focus ?? throw new ArgumentNullException("focus");
        Transport = transport ?? throw new ArgumentNullException("transport");
        Scheduler = new UpdateScheduler(Events, Bindings, Validation, Commands, Components, Router, Focus);
        HostDiagnostics = new List<UiDiagnostic>();
        RegisteredRouteIds = new List<string>();
    }

    /// <summary>Validates, normalizes, and stores an application for hosting.</summary>
    public UiResult InitializeUiHost(UiApplication? application)
    {
        if (Closed)
        {
            return CreateHostFailure("host.closed", "The host has already been shut down.", "/host");
        }

        UiResult validation = Validator.TryValidateModel(application);
        if (!validation.Success || application == null)
        {
            return CopyUiResult(validation);
        }

        UiApplication normalized = new ModelNormalizer().NormalizeApplication(application);
        UiWindow? window = SelectActiveWindow(normalized);
        if (window == null)
        {
            return CreateHostFailure("host.no-window", "The application does not contain an active window.", "/application/windows");
        }

        UiNavigationResult configured = Router.ConfigureUiRouting(window.Pages, window.Document, window.Document.BaseUrl, false);
        if (!configured.Success)
        {
            return CreateHostFailureFromDiagnostics(configured.Diagnostics);
        }

        int routeIndex = 0;
        while (routeIndex < normalized.Routes.Length)
        {
            UiNavigationResult registered = Router.RegisterUiRoute(normalized.Routes[routeIndex]);
            if (!registered.Success)
            {
                CleanupRegisteredRoutes();
                RouteHostDiagnostics(registered.Diagnostics);
                return CreateHostFailureFromDiagnostics(registered.Diagnostics);
            }

            RegisteredRouteIds.Add(normalized.Routes[routeIndex].Id);
            routeIndex++;
        }

        UiResult services = Scheduler.InitializeHostServices(normalized);
        if (!services.Success)
        {
            Scheduler.CleanupHostServices();
            CleanupRegisteredRoutes();
            RouteHostDiagnostics(services.Diagnostics);
            return services;
        }

        Application = normalized;
        Snapshot = null;
        Bridge = new UiWebViewBridge(window.BridgeOptions, Transport, this);
        Initialized = true;
        Mounted = false;
        UiResult result = CopyUiResult(validation);
        result.Success = true;
        return result;
    }

    /// <summary>Starts the bridge session and renders the initial document.</summary>
    public UiRenderResult MountUiHost(string? sessionId, string? nonce, long nowMilliseconds)
    {
        if (!Initialized || Application == null || Bridge == null)
        {
            return CreateRenderFailure("host.not-initialized", "The host must be initialized before it can be mounted.", "/host");
        }

        if (Mounted)
        {
            return CreateRenderFailure("host.already-mounted", "The host is already mounted.", "/host");
        }

        UiResult started = Bridge.StartUiBridge(sessionId, nonce, nowMilliseconds);
        if (!started.Success)
        {
            return CreateRenderFailureFromDiagnostics(started.Diagnostics);
        }

        UiRenderResult rendered = Renderer.RenderDocument(Application);
        if (rendered.Snapshot == null || HasDiagnosticErrors(rendered.Diagnostics))
        {
            return rendered;
        }

        Snapshot = CopySnapshot(rendered.Snapshot);
        rendered.Snapshot = CopySnapshot(Snapshot);
        UiWindow? window = SelectActiveWindow(Application);
        if (window != null)
        {
            window.Transport = Bridge.GetUiBridgeCapabilities();
        }

        Mounted = true;
        TransactionMilliseconds = nowMilliseconds;
        return rendered;
    }

    /// <summary>Renders the currently hosted application as a complete document.</summary>
    public UiRenderResult RenderUiHost()
    {
        if (!Initialized || Application == null)
        {
            return CreateRenderFailure("host.not-initialized", "The host must be initialized before it can be rendered.", "/host");
        }

        UiRenderResult rendered = Renderer.RenderDocument(Application);
        if (rendered.Snapshot == null || HasDiagnosticErrors(rendered.Diagnostics))
        {
            return rendered;
        }

        if (Snapshot != null)
        {
            rendered.Snapshot = Updates.CreateDomSnapshot(rendered.Snapshot.Roots, Snapshot.SessionId, Snapshot.RootSeed, Snapshot.Revision);
        }

        Snapshot = CopySnapshot(rendered.Snapshot);
        rendered.Snapshot = CopySnapshot(Snapshot);
        return rendered;
    }

    /// <summary>Validates and renders a new application state and commits its deterministic DOM update.</summary>
    public UiRenderResult UpdateUiHost(UiApplication? application, UiUpdatePriority priority, UiPreservationState? preservation)
    {
        if (!Mounted || Snapshot == null)
        {
            return CreateRenderFailure("host.not-mounted", "The host must be mounted before it can be updated.", "/host");
        }

        if (PendingBatch != null)
        {
            return CreateRenderFailure("host.update-pending", "A browser update is awaiting acknowledgement.", "/host/update");
        }

        UiResult validation = Validator.TryValidateModel(application);
        if (!validation.Success || application == null)
        {
            return CreateRenderFailureFromDiagnostics(validation.Diagnostics);
        }

        UiApplication normalized = new ModelNormalizer().NormalizeApplication(application);
        UiRenderResult rendered = Renderer.RenderDocument(normalized);
        if (rendered.Snapshot == null || HasDiagnosticErrors(rendered.Diagnostics))
        {
            return rendered;
        }

        if (!string.Equals(rendered.Snapshot.RootSeed, Snapshot.RootSeed, StringComparison.Ordinal)
            || !string.Equals(rendered.Snapshot.SessionId, Snapshot.SessionId, StringComparison.Ordinal))
        {
            return CreateRenderFailure("host.update-identity-changed", "An incremental update must retain the mounted application, window, page, and root identities.", "/application");
        }

        DomSnapshot desired = Updates.CreateDomSnapshot(rendered.Snapshot.Roots, Snapshot.SessionId, Snapshot.RootSeed, Snapshot.Revision);
        UiUpdateBatch batch = Updates.ComputeDomUpdates(Snapshot, desired, priority, preservation);
        UiUpdateResult accepted = Updates.AcceptDomUpdates(batch.Id, batch.Revision);
        rendered.UpdateBatch = batch;
        rendered.UpdateResult = accepted;
        if (!accepted.Accepted)
        {
            return rendered;
        }

        DomSnapshot committed = Updates.CreateDomSnapshot(desired.Roots, desired.SessionId, desired.RootSeed, batch.Revision);
        Application = normalized;
        Snapshot = committed;
        rendered.Snapshot = CopySnapshot(committed);
        return rendered;
    }

    /// <summary>Dispatches one normalized UI event.</summary>
    public UiEventDispatchResult DispatchUiHostEvent(UiEventData? eventData)
    {
        if (!Mounted || Application == null)
        {
            UiEventDispatchResult unavailable = new UiEventDispatchResult();
            unavailable.Success = false;
            unavailable.Diagnostics = new UiDiagnostic[]
            {
                new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = "host.not-mounted", Message = "The host must be mounted before it can dispatch an event.", Location = "/host" }
            };
            return unavailable;
        }

        UiApplication working = CopyApplication(Application);
        HostScheduleResult scheduled = Scheduler.ExecuteHostEvent(working, eventData);
        UiEventDispatchResult result = scheduled.EventResult;
        result.BindingResult = scheduled.BindingResult;
        result.ValidationResult = scheduled.ValidationResult;
        result.CommandResult = scheduled.CommandResult;
        result.ComponentResult = scheduled.ComponentResult;
        result.NavigationResult = scheduled.NavigationResult;
        result.FocusResult = scheduled.FocusResult;
        result.RecoveredByComponentBoundary = scheduled.RecoveredByComponentBoundary;
        if (!scheduled.Success)
        {
            result.Success = false;
            result.TransactionStages = scheduled.Stages.ToArray();
            RouteScheduledDiagnostics(scheduled);
            return result;
        }

        UiWindow? window = SelectActiveWindow(working);
        UiPreservationState preservation = new UiPreservationState();
        if (window != null)
        {
            preservation.FocusedNodeId = window.Focus.FocusedNodeId;
        }

        scheduled.Stages.Add("render");
        UiRenderResult rendered = PrepareScheduledUpdate(working, UiUpdatePriority.UserInteraction, preservation);
        result.RenderResult = rendered;
        if (rendered.UpdateResult == null || !rendered.UpdateResult.Result.Success || rendered.UpdateBatch == null)
        {
            result.Success = false;
            result.TransactionStages = scheduled.Stages.ToArray();
            Scheduler.RestoreHostState(scheduled.StateSnapshotId);
            RouteHostDiagnostics(rendered.Diagnostics);
            return result;
        }

        PendingStateSnapshotId = scheduled.StateSnapshotId;

        scheduled.Stages.Add("bridge");
        UiResult delivered;
        if (Bridge == null)
        {
            delivered = CreateHostFailure("host.bridge-unavailable", "The host bridge is unavailable.", "/host/bridge");
        }
        else
        {
            delivered = Bridge.SendUiBridgeNotification(UiBridgeMessageKind.DomPatch, Scheduler.CreateUpdateBatchValue(rendered.UpdateBatch), NextTransactionMilliseconds());
            UiWebViewBridge concreteBridge = (UiWebViewBridge)Bridge;
            if (concreteBridge.Flow.PendingAcknowledgements.Count > 0)
            {
                PendingBridgeSequence = concreteBridge.Flow.PendingAcknowledgements[concreteBridge.Flow.PendingAcknowledgements.Count - 1].Sequence;
            }
        }

        result.BridgeResult = delivered;
        result.TransactionStages = scheduled.Stages.ToArray();
        if (!delivered.Success)
        {
            result.Success = false;
            RejectPendingUpdateState();
            RouteHostDiagnostics(delivered.Diagnostics);
        }

        else if (scheduled.RecoveredByComponentBoundary)
        {
            RouteHostDiagnostics(scheduled.ComponentResult.Diagnostics);
        }

        return result;
    }

    /// <summary>Dispatches one command invocation.</summary>
    public UiCommandResult DispatchUiHostCommand(UiCommandInvocation? invocation)
    {
        return Commands.ExecuteUiCommand(invocation);
    }

    /// <summary>Runs one guarded navigation through the configured router.</summary>
    public Task<UiNavigationResult> NavigateUiHost(UiNavigationRequest? request)
    {
        return Router.NavigateUi(request);
    }

    /// <summary>Validates and dispatches one inbound bridge message.</summary>
    public UiResult ReceiveUiHostMessage(UiBridgeEnvelope? message, bool isMainFrame, long nowMilliseconds)
    {
        if (Bridge == null || !Mounted && !Closed)
        {
            return CreateHostFailure("host.not-mounted", "The host must be mounted before it can receive bridge messages.", "/host");
        }

        if (nowMilliseconds > TransactionMilliseconds)
        {
            TransactionMilliseconds = nowMilliseconds;
        }

        UiResult received = Bridge.ReceiveUiBridgeMessage(message, isMainFrame, nowMilliseconds);
        if (!received.Success)
        {
            RouteHostDiagnostics(received.Diagnostics);
            return received;
        }

        if (message != null && message.Kind == UiBridgeMessageKind.Acknowledgement && ReadAcknowledgedSequence(message) == PendingBridgeSequence)
        {
            UiUpdateResult accepted = AcceptPendingUpdateState();
            if (!accepted.Accepted)
            {
                RouteHostDiagnostics(accepted.Diagnostics);
                return accepted.Result;
            }
        }

        if (message != null && message.Kind == UiBridgeMessageKind.Ready && RecoveryPending && Snapshot != null)
        {
            RecoveryPending = false;
            UiResult resynchronized = Bridge.SendUiBridgeNotification(UiBridgeMessageKind.RenderSnapshot, Scheduler.CreateSnapshotValue(Snapshot), NextTransactionMilliseconds());
            if (!resynchronized.Success)
            {
                RouteHostDiagnostics(resynchronized.Diagnostics);
            }

            return resynchronized;
        }

        return received;
    }

    /// <summary>Gets a detached copy of the currently committed DOM snapshot.</summary>
    public DomSnapshot? GetUiHostSnapshot()
    {
        if (Snapshot == null)
        {
            return null;
        }

        return CopySnapshot(Snapshot);
    }

    /// <summary>Rejects the pending browser update and sends a full accepted snapshot for resynchronization.</summary>
    public UiUpdateResult RejectUiHostUpdate(string? batchId, int revision, long nowMilliseconds)
    {
        UiUpdateResult rejected = Updates.RejectDomUpdates(batchId, revision);
        if (PendingBatch != null && string.Equals(PendingBatch.Id, batchId, StringComparison.Ordinal) && PendingBatch.Revision == revision)
        {
            if (PendingStateSnapshotId.Length > 0)
            {
                Scheduler.RestoreHostState(PendingStateSnapshotId);
            }

            ClearPendingUpdateState();
        }

        RouteHostDiagnostics(rejected.Diagnostics);
        if (rejected.RequiresResync && rejected.Snapshot != null && Bridge != null)
        {
            if (nowMilliseconds > TransactionMilliseconds)
            {
                TransactionMilliseconds = nowMilliseconds;
            }

            UiResult sent = Bridge.SendUiBridgeNotification(UiBridgeMessageKind.RenderSnapshot, Scheduler.CreateSnapshotValue(rejected.Snapshot), NextTransactionMilliseconds());
            if (!sent.Success)
            {
                RouteHostDiagnostics(sent.Diagnostics);
            }
        }

        return rejected;
    }

    /// <summary>Reconnects a lost transport session and schedules a full snapshot handoff.</summary>
    public UiResult RecoverUiHostTransport(string? sessionId, string? nonce, long nowMilliseconds)
    {
        if (Bridge == null || Snapshot == null || Closed)
        {
            UiResult unavailable = CreateHostFailure("host.recovery-unavailable", "The host has no mounted snapshot to recover.", "/host/recovery");
            RouteHostDiagnostics(unavailable.Diagnostics);
            return unavailable;
        }

        if (nowMilliseconds > TransactionMilliseconds)
        {
            TransactionMilliseconds = nowMilliseconds;
        }

        UiResult reconnecting = Bridge.ReconnectUiBridge(sessionId, nonce, 0, nowMilliseconds);
        if (reconnecting.Success)
        {
            RecoveryPending = true;
        }
        else
        {
            RouteHostDiagnostics(reconnecting.Diagnostics);
        }

        return reconnecting;
    }

    /// <summary>Gets detached diagnostics routed by host orchestration and recovery.</summary>
    public UiDiagnostic[] GetUiHostDiagnostics()
    {
        UiDiagnostic[] result = new UiDiagnostic[HostDiagnostics.Count];
        int index = 0;
        while (index < HostDiagnostics.Count)
        {
            object? copied = new ModelNormalizer().CopyValue(HostDiagnostics[index]);
            result[index] = copied == null ? new UiDiagnostic() : (UiDiagnostic)copied;
            index++;
        }

        return result;
    }

    /// <summary>Begins orderly bridge shutdown and closes the host lifecycle.</summary>
    public UiResult ShutdownUiHost(long nowMilliseconds)
    {
        if (Closed)
        {
            return CreateHostSuccess();
        }

        UiResult bridgeResult = CreateHostSuccess();
        if (Bridge != null && Mounted)
        {
            bridgeResult = Bridge.ShutdownUiBridge(nowMilliseconds);
            RouteHostDiagnostics(bridgeResult.Diagnostics);
        }

        RejectPendingUpdateState();
        UiResult cleanup = Scheduler.CleanupHostServices();
        RouteHostDiagnostics(cleanup.Diagnostics);
        CleanupRegisteredRoutes();

        Closed = true;
        Initialized = false;
        Mounted = false;
        Application = null;
        Snapshot = null;
        if (!bridgeResult.Success)
        {
            return bridgeResult;
        }

        return cleanup;
    }

    /// <summary>Handles one validated inbound bridge message for the active hosted window.</summary>
    public UiResult HandleUiBridgeMessage(UiBridgeEnvelope message)
    {
        if (Application == null)
        {
            return CreateHostFailure("host.not-initialized", "The host has no application for the bridge message.", "/host");
        }

        if (message.Kind == UiBridgeMessageKind.Request && message.Request != null && string.Equals(message.Request.Operation, "resync", StringComparison.Ordinal))
        {
            RejectPendingUpdateState();
            if (Snapshot == null)
            {
                UiResult unavailable = CreateHostFailure("host.resync-unavailable", "The host has no accepted snapshot for resynchronization.", "/host/resync");
                RouteHostDiagnostics(unavailable.Diagnostics);
                return unavailable;
            }

            return new UiResult { Success = true, Value = Scheduler.CreateSnapshotValue(Snapshot) };
        }

        UiWindow? window = SelectActiveWindow(Application);
        if (window == null)
        {
            return CreateHostFailure("host.no-window", "The hosted application has no active window.", "/application/windows");
        }

        object? copied = new ModelNormalizer().CopyValue(message);
        window.LastBridgeEnvelope = (UiBridgeEnvelope?)copied;
        return CreateHostSuccess();
    }

    internal UiRenderResult PrepareScheduledUpdate(UiApplication application, UiUpdatePriority priority, UiPreservationState preservation)
    {
        if (Snapshot == null)
        {
            return CreateRenderFailure("host.not-mounted", "The host must have an accepted snapshot before scheduling an update.", "/host/update");
        }

        if (PendingBatch != null)
        {
            return CreateRenderFailure("host.update-pending", "A browser update is awaiting acknowledgement.", "/host/update");
        }

        UiRenderResult rendered = Renderer.RenderDocument(application);
        if (rendered.Snapshot == null || HasDiagnosticErrors(rendered.Diagnostics))
        {
            return rendered;
        }

        if (!string.Equals(rendered.Snapshot.RootSeed, Snapshot.RootSeed, StringComparison.Ordinal)
            || !string.Equals(rendered.Snapshot.SessionId, Snapshot.SessionId, StringComparison.Ordinal))
        {
            return CreateRenderFailure("host.update-identity-changed", "An incremental update must retain the mounted application, window, page, and root identities.", "/application");
        }

        DomSnapshot desired = Updates.CreateDomSnapshot(rendered.Snapshot.Roots, Snapshot.SessionId, Snapshot.RootSeed, Snapshot.Revision);
        UiUpdateBatch batch = Updates.ComputeDomUpdates(Snapshot, desired, priority, preservation);
        DomSnapshot pendingSnapshot = Updates.CreateDomSnapshot(desired.Roots, desired.SessionId, desired.RootSeed, batch.Revision);
        PendingApplication = CopyApplication(application);
        PendingSnapshot = pendingSnapshot;
        PendingBatch = batch;
        rendered.Snapshot = CopySnapshot(pendingSnapshot);
        rendered.UpdateBatch = batch;
        rendered.UpdateResult = new UiUpdateResult
        {
            Accepted = false,
            Revision = batch.Revision,
            Result = new UiResult { Success = true }
        };
        return rendered;
    }

    internal UiUpdateResult AcceptPendingUpdateState()
    {
        if (PendingBatch == null)
        {
            UiUpdateResult missing = new UiUpdateResult();
            missing.Diagnostics = new UiDiagnostic[]
            {
                new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = "host.no-pending-update", Message = "The host has no pending browser update.", Location = "/host/update" }
            };
            missing.Result.Diagnostics = missing.Diagnostics;
            return missing;
        }

        UiResult stateCommit = Scheduler.CommitHostState(PendingStateSnapshotId);
        if (!stateCommit.Success)
        {
            UiUpdateResult rejected = Updates.RejectDomUpdates(PendingBatch.Id, PendingBatch.Revision);
            rejected.Diagnostics = stateCommit.Diagnostics;
            rejected.Result = stateCommit;
            ClearPendingUpdateState();
            return rejected;
        }

        UiUpdateResult accepted = Updates.AcceptDomUpdates(PendingBatch.Id, PendingBatch.Revision);
        if (accepted.Accepted && PendingApplication != null && PendingSnapshot != null)
        {
            Application = PendingApplication;
            Snapshot = PendingSnapshot;
            ClearPendingUpdateState();
        }

        return accepted;
    }

    internal void RejectPendingUpdateState()
    {
        if (PendingBatch != null)
        {
            Updates.RejectDomUpdates(PendingBatch.Id, PendingBatch.Revision);
        }

        if (PendingStateSnapshotId.Length > 0)
        {
            Scheduler.RestoreHostState(PendingStateSnapshotId);
        }

        ClearPendingUpdateState();
    }

    internal void ClearPendingUpdateState()
    {
        PendingApplication = null;
        PendingSnapshot = null;
        PendingBatch = null;
        PendingStateSnapshotId = string.Empty;
        PendingBridgeSequence = -1;
    }

    internal int ReadAcknowledgedSequence(UiBridgeEnvelope message)
    {
        if (message.Payload == null || message.Payload.Kind != UiValueKind.Object || message.Payload.ObjectValue == null)
        {
            return -1;
        }

        int index = 0;
        while (index < message.Payload.ObjectValue.Length)
        {
            UiValueEntry entry = message.Payload.ObjectValue[index];
            if (string.Equals(entry.Key, "acknowledgement", StringComparison.Ordinal)
                && entry.Value.Kind == UiValueKind.Integer
                && entry.Value.IntegerValue >= 0
                && entry.Value.IntegerValue <= int.MaxValue)
            {
                return (int)entry.Value.IntegerValue;
            }

            index++;
        }

        return -1;
    }

    internal UiApplication CopyApplication(UiApplication application)
    {
        object? copied = new ModelNormalizer().CopyValue(application);
        if (copied == null)
        {
            throw new InvalidOperationException("The hosted application copy could not be created.");
        }

        return (UiApplication)copied;
    }

    internal void RouteScheduledDiagnostics(HostScheduleResult scheduled)
    {
        RouteHostDiagnostics(scheduled.EventResult.Diagnostics);
        RouteHostDiagnostics(scheduled.BindingResult.Diagnostics);
        RouteHostDiagnostics(scheduled.ValidationResult.Diagnostics);
        RouteHostDiagnostics(scheduled.CommandResult.Diagnostics);
        RouteHostDiagnostics(scheduled.ComponentResult.Diagnostics);
        RouteHostDiagnostics(scheduled.NavigationResult.Diagnostics);
        RouteHostDiagnostics(scheduled.FocusResult.Diagnostics);
    }

    internal void RouteHostDiagnostics(UiDiagnostic[]? diagnostics)
    {
        UiDiagnostic[] values = diagnostics ?? Array.Empty<UiDiagnostic>();
        int index = 0;
        while (index < values.Length && HostDiagnostics.Count < 100)
        {
            object? copied = new ModelNormalizer().CopyValue(values[index]);
            if (copied != null)
            {
                HostDiagnostics.Add((UiDiagnostic)copied);
            }

            index++;
        }
    }

    internal void CleanupRegisteredRoutes()
    {
        int index = RegisteredRouteIds.Count - 1;
        while (index >= 0)
        {
            UiNavigationResult removed = Router.UnregisterUiRoute(RegisteredRouteIds[index]);
            RouteHostDiagnostics(removed.Diagnostics);
            index--;
        }

        RegisteredRouteIds.Clear();
    }

    internal UiWindow? SelectActiveWindow(UiApplication application)
    {
        int index = 0;
        while (index < application.Windows.Length)
        {
            UiWindow? window = application.Windows[index];
            if (window != null && window.IsActive)
            {
                return window;
            }

            index++;
        }

        return null;
    }

    internal long NextTransactionMilliseconds()
    {
        if (TransactionMilliseconds < long.MaxValue)
        {
            TransactionMilliseconds++;
        }

        return TransactionMilliseconds;
    }

    internal DomSnapshot CopySnapshot(DomSnapshot snapshot)
    {
        object? copied = new ModelNormalizer().CopyValue(snapshot);
        if (copied == null)
        {
            throw new InvalidOperationException("The DOM snapshot copy could not be created.");
        }

        return (DomSnapshot)copied;
    }

    internal UiResult CopyUiResult(UiResult source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        if (copied == null)
        {
            throw new InvalidOperationException("The operation result copy could not be created.");
        }

        return (UiResult)copied;
    }

    internal bool HasDiagnosticErrors(UiDiagnostic[]? diagnostics)
    {
        UiDiagnostic[] values = diagnostics ?? Array.Empty<UiDiagnostic>();
        int index = 0;
        while (index < values.Length)
        {
            UiDiagnostic? diagnostic = values[index];
            if (diagnostic != null && diagnostic.Severity == UiDiagnosticSeverity.Error)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal UiResult CreateHostSuccess()
    {
        return new UiResult { Success = true };
    }

    internal UiResult CreateHostFailure(string code, string message, string location)
    {
        return CreateHostFailureFromDiagnostics(new UiDiagnostic[]
        {
            new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = code, Message = message, Location = location }
        });
    }

    internal UiResult CreateHostFailureFromDiagnostics(UiDiagnostic[]? diagnostics)
    {
        UiResult result = new UiResult();
        result.Diagnostics = diagnostics ?? Array.Empty<UiDiagnostic>();
        return result;
    }

    internal UiRenderResult CreateRenderFailure(string code, string message, string location)
    {
        return CreateRenderFailureFromDiagnostics(new UiDiagnostic[]
        {
            new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = code, Message = message, Location = location }
        });
    }

    internal UiRenderResult CreateRenderFailureFromDiagnostics(UiDiagnostic[]? diagnostics)
    {
        UiRenderResult result = new UiRenderResult();
        result.Diagnostics = diagnostics ?? Array.Empty<UiDiagnostic>();
        return result;
    }
}
