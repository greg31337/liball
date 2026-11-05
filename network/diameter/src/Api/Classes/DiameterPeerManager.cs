using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>
/// Manages Diameter peer configuration, lifecycle state, health, and event delivery.
/// </summary>
public class DiameterPeerManager : IDiameterPeerManager
{
    internal PeerTable Table;
    internal PeerConnectionLifecycle Lifecycle;
    internal PeerDisconnectController DisconnectController;
    internal PeerWatchdogController WatchdogController;
    internal PeerCapabilitiesExchange CapabilitiesExchange;
    internal PeerCapabilitiesUpdateController CapabilitiesUpdateController;
    internal PeerConnectionElection ConnectionElection =
        new PeerConnectionElection();
    internal PeerRestartDetector RestartDetector;
    internal object HandlerLock = new object();
    internal List<IPeerEventHandler> Handlers = new List<IPeerEventHandler>();
    internal object ConnectionLock = new object();
    internal Dictionary<string, IDiameterConnection> Connections =
        new Dictionary<string, IDiameterConnection>();
    internal IDiameterClock Clock;

    /// <summary>Initializes a peer manager for one configured Diameter node.</summary>
    /// <param name="configuration">The validated node identity, limits, and timers.</param>
    /// <param name="clock">The UTC and monotonic clock.</param>
    /// <param name="identifierSource">The Diameter identifier source.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when a dependency or node configuration is invalid.
    /// </exception>
    public DiameterPeerManager(
        DiameterNodeConfiguration? configuration,
        IDiameterClock? clock,
        IDiameterIdentifierSource? identifierSource)
    {
        if (configuration == null || clock == null || identifierSource == null)
        {
            throw new ArgumentException(
                "A node configuration, clock, and identifier source are required.");
        }

        configuration.ValidateConfiguration();
        Clock = clock;
        Table = new PeerTable(
            clock,
            configuration.Timers,
            configuration.Limits.MaximumConnectionCount);
        Lifecycle = new PeerConnectionLifecycle(Table);
        DiameterBaseCommandFactory commands =
            new DiameterBaseCommandFactory(identifierSource);
        DiameterBaseCommandOptions origin = new DiameterBaseCommandOptions();
        origin.OriginHost.Value = configuration.OriginHost.Value;
        origin.OriginRealm.Value = configuration.OriginRealm.Value;
        DisconnectController = new PeerDisconnectController(
            Table,
            commands,
            origin);
        WatchdogController = new PeerWatchdogController(
            Table,
            commands,
            origin);
        CapabilitiesExchange = new PeerCapabilitiesExchange(
            identifierSource,
            clock,
            configuration.Limits);
        CapabilitiesUpdateController = new PeerCapabilitiesUpdateController(
            Table,
            identifierSource,
            configuration.Limits);
        PeerManagerRestartEventSink eventSink =
            new PeerManagerRestartEventSink(this);
        PeerManagerRestartEventSink requestSink =
            new PeerManagerRestartEventSink(null);
        PeerManagerRestartEventSink sessionSink =
            new PeerManagerRestartEventSink(null);
        RestartDetector = new PeerRestartDetector(
            Table,
            eventSink,
            requestSink,
            sessionSink);
    }

    /// <summary>Adds or replaces one peer configuration.</summary>
    /// <param name="configuration">The peer configuration to own.</param>
    /// <returns>The peer snapshot after applying the configuration.</returns>
    public DiameterPeerSnapshot AddDiameterPeer(
        DiameterPeerConfiguration? configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentException("A peer configuration is required.");
        }

        configuration.ValidatePeerConfiguration();
        DiameterPeerSnapshot? previous = Table.ReadPeerTableSnapshot(
            configuration.Identity.Value);
        DiameterPeerSnapshot peer = Table.AddOrUpdatePeerConfiguration(
            configuration);
        string code = "peer.added";
        string message = "A Diameter peer was added.";
        if (previous != null)
        {
            code = "peer.configuration.updated";
            message = "A Diameter peer configuration was updated.";
        }

        DiameterPeerEvent peerEvent = CreatePeerManagerEvent(
            DiameterPeerEventKind.Added,
            peer,
            peer.State,
            code,
            message);
        DispatchPeerManagerEvent(peerEvent);
        return PeerTableCloner.ClonePeerTableSnapshot(peer);
    }

    /// <summary>Removes one disconnected peer.</summary>
    /// <param name="identity">The peer identity to remove.</param>
    /// <returns><c>true</c> when a peer was removed.</returns>
    public bool RemoveDiameterPeer(DiameterIdentity? identity)
    {
        string value = ReadPeerManagerIdentity(identity);
        DiameterPeerSnapshot? peer = Table.ReadPeerTableSnapshot(value);
        if (peer == null)
        {
            return false;
        }

        if (peer.State != DiameterPeerState.Closed || peer.Connection != null)
        {
            throw new DiameterPeerException(
                "An active peer must be disconnected before it is removed.");
        }

        bool removed = Table.RemovePeerTableEntry(value);
        if (removed)
        {
            lock (ConnectionLock)
            {
                Connections.Remove(
                    ValueCodecs.NormalizeDiameterIdentity(identity).Text);
            }

            DiameterPeerEvent peerEvent = CreatePeerManagerEvent(
                DiameterPeerEventKind.Removed,
                peer,
                peer.State,
                "peer.removed",
                "A Diameter peer was removed.");
            DispatchPeerManagerEvent(peerEvent);
        }

        return removed;
    }

    /// <summary>Starts a connection attempt for one configured peer.</summary>
    /// <param name="identity">The peer identity to connect.</param>
    /// <returns>The peer snapshot after processing the request.</returns>
    public DiameterPeerSnapshot ConnectDiameterPeer(DiameterIdentity? identity)
    {
        string value = ReadPeerManagerIdentity(identity);
        DiameterPeerSnapshot? previous = Table.ReadPeerTableSnapshot(value);
        if (previous == null)
        {
            throw new DiameterPeerException("The requested peer is not configured.");
        }

        PeerConnectionLifecycleResult result =
            Lifecycle.RequestManualPeerConnection(value);
        if (result.Accepted && result.Peer.State != previous.State)
        {
            DiameterPeerEvent peerEvent = CreatePeerManagerEvent(
                DiameterPeerEventKind.StateChanged,
                result.Peer,
                previous.State,
                "peer.connection.requested",
                "A Diameter peer connection attempt was requested.");
            DispatchPeerManagerEvent(peerEvent);
        }

        return PeerTableCloner.ClonePeerTableSnapshot(result.Peer);
    }

    /// <summary>Starts a graceful disconnect for one configured peer.</summary>
    /// <param name="request">The disconnect cause and completion policy.</param>
    /// <returns>The peer snapshot after processing the request.</returns>
    public DiameterPeerSnapshot DisconnectDiameterPeer(
        DiameterDisconnectRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException("A peer disconnect request is required.");
        }

        request.ValidateDisconnectRequest();
        DiameterPeerSnapshot? previous;
        PeerDisconnectResult result;
        lock (Table.TableLock)
        {
            previous = Table.ReadPeerTableSnapshot(
                request.PeerIdentity.Value);
            if (previous == null)
            {
                throw new DiameterPeerException("The requested peer is not configured.");
            }

            result = DisconnectController.BeginPeerDisconnect(request);
        }

        if (result.Handled && result.Peer.State != previous.State)
        {
            DiameterPeerEvent peerEvent = CreatePeerManagerEvent(
                DiameterPeerEventKind.StateChanged,
                result.Peer,
                previous.State,
                "peer.disconnect.requested",
                "A graceful Diameter peer disconnect was requested.");
            DispatchPeerManagerEvent(peerEvent);
        }

        return PeerTableCloner.ClonePeerTableSnapshot(result.Peer);
    }

    /// <summary>Reads an owned snapshot for one peer.</summary>
    /// <param name="identity">The peer identity to read.</param>
    /// <returns>The peer snapshot, or <c>null</c> when it is not configured.</returns>
    public DiameterPeerSnapshot? ReadDiameterPeerSnapshot(
        DiameterIdentity? identity)
    {
        string value = ReadPeerManagerIdentity(identity);
        DiameterPeerSnapshot? result = Table.ReadPeerTableSnapshot(value);
        if (result == null)
        {
            return null;
        }

        return PeerTableCloner.ClonePeerTableSnapshot(result);
    }

    /// <summary>Reads owned snapshots for all peers in deterministic table order.</summary>
    /// <returns>The peer snapshots.</returns>
    public List<DiameterPeerSnapshot> ReadDiameterPeerSnapshots()
    {
        List<DiameterPeerSnapshot> source = Table.ReadAllPeerTableSnapshots();
        List<DiameterPeerSnapshot> result = new List<DiameterPeerSnapshot>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(PeerTableCloner.ClonePeerTableSnapshot(source[index]));
            index = index + 1;
        }

        return result;
    }

    /// <summary>Sends encoded Diameter bytes through one active peer connection.</summary>
    /// <param name="identity">The destination peer identity.</param>
    /// <param name="source">The complete encoded Diameter message.</param>
    /// <param name="timeoutMilliseconds">The positive send timeout in milliseconds.</param>
    /// <returns>The peer transport send outcome.</returns>
    public DiameterTransportIoResult SendDiameterPeerBytes(
        DiameterIdentity? identity,
        byte[]? source,
        int timeoutMilliseconds)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(identity);
        if (normalized.Status != DiameterCodecStatus.Success ||
            source == null || timeoutMilliseconds <= 0)
        {
            DiameterTransportIoResult invalid =
                new DiameterTransportIoResult();
            invalid.Status = DiameterTransportStatus.InvalidConfiguration;
            invalid.Code = "peer-send-input-invalid";
            invalid.Message =
                "A peer identity, encoded message, and positive timeout are required.";
            return invalid;
        }

        IDiameterConnection? connection = null;
        lock (ConnectionLock)
        {
            if (Connections.ContainsKey(normalized.Text))
            {
                connection = Connections[normalized.Text];
            }
        }

        if (connection == null)
        {
            DiameterTransportIoResult unavailable =
                new DiameterTransportIoResult();
            unavailable.Status = DiameterTransportStatus.Closed;
            unavailable.Code = "peer-connection-unavailable";
            unavailable.Message =
                "The selected peer does not have an active transport connection.";
            return unavailable;
        }

        return connection.SendDiameterConnectionBytes(
            source,
            timeoutMilliseconds);
    }

    /// <summary>Registers a peer event handler idempotently.</summary>
    /// <param name="handler">The handler to register.</param>
    public void RegisterPeerEventHandler(IPeerEventHandler? handler)
    {
        if (handler == null)
        {
            throw new ArgumentException("A peer event handler is required.");
        }

        lock (HandlerLock)
        {
            int index = 0;
            while (index < Handlers.Count)
            {
                if (object.ReferenceEquals(Handlers[index], handler))
                {
                    return;
                }

                index = index + 1;
            }

            Handlers.Add(handler);
        }
    }

    /// <summary>Unregisters a peer event handler idempotently.</summary>
    /// <param name="handler">The handler to unregister.</param>
    public void UnregisterPeerEventHandler(IPeerEventHandler? handler)
    {
        if (handler == null)
        {
            throw new ArgumentException("A peer event handler is required.");
        }

        lock (HandlerLock)
        {
            int index = 0;
            while (index < Handlers.Count)
            {
                if (object.ReferenceEquals(Handlers[index], handler))
                {
                    Handlers.RemoveAt(index);
                    return;
                }

                index = index + 1;
            }
        }
    }

    internal DiameterPeerSnapshot RecordPeerManagerCapabilities(
        string identity,
        DiameterPeerCapabilities capabilities)
    {
        DiameterPeerSnapshot? previous = Table.ReadPeerTableSnapshot(identity);
        if (previous == null)
        {
            throw new DiameterPeerException("The requested peer is not configured.");
        }

        PeerRestartObservation restart = RestartDetector.ObservePeerCapabilities(
            identity,
            capabilities);
        DiameterCapabilityChange change =
            CapabilitiesUpdateController.ReadPeerCapabilitiesUpdateChange(
                previous.Capabilities,
                restart.Peer.Capabilities);
        if (change != DiameterCapabilityChange.None)
        {
            DiameterPeerEvent peerEvent = CreatePeerManagerEvent(
                DiameterPeerEventKind.CapabilitiesChanged,
                restart.Peer,
                previous.State,
                "peer.capabilities.changed",
                "A Diameter peer capability snapshot changed.");
            peerEvent.CapabilityChange = change;
            peerEvent.PreviousOriginStateId =
                previous.Capabilities.OriginStateId;
            peerEvent.CurrentOriginStateId =
                restart.Peer.Capabilities.OriginStateId;
            DispatchPeerManagerEvent(peerEvent);
        }

        return PeerTableCloner.ClonePeerTableSnapshot(restart.Peer);
    }

    internal void AttachPeerManagerConnection(
        DiameterIdentity identity,
        IDiameterConnection connection)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(identity);
        if (normalized.Status != DiameterCodecStatus.Success ||
            connection == null)
        {
            throw new ArgumentException(
                "A valid peer identity and connection are required.");
        }

        lock (ConnectionLock)
        {
            Connections[normalized.Text] = connection;
        }
    }

    internal void DetachPeerManagerConnection(DiameterIdentity identity)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(identity);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A valid peer identity is required.");
        }

        lock (ConnectionLock)
        {
            Connections.Remove(normalized.Text);
        }
    }

    internal PeerWatchdogResult RecordPeerManagerTransportFailure(
        string identity)
    {
        DiameterPeerSnapshot? previous = Table.ReadPeerTableSnapshot(identity);
        if (previous == null)
        {
            throw new DiameterPeerException("The requested peer is not configured.");
        }

        PeerWatchdogResult result =
            WatchdogController.HandlePeerTransportFailure(identity);
        if (result.Handled)
        {
            lock (ConnectionLock)
            {
                DiameterIdentity failedIdentity = new DiameterIdentity();
                failedIdentity.Value = identity;
                Connections.Remove(
                    ValueCodecs.NormalizeDiameterIdentity(
                        failedIdentity).Text);
            }

            DiameterPeerEvent peerEvent = CreatePeerManagerEvent(
                DiameterPeerEventKind.TransportFailed,
                result.Peer,
                previous.State,
                "peer.transport.failed",
                "The Diameter peer transport failed.");
            DispatchPeerManagerEvent(peerEvent);
        }

        return result;
    }

    internal DiameterPeerEvent CreatePeerManagerEvent(
        DiameterPeerEventKind kind,
        DiameterPeerSnapshot peer,
        DiameterPeerState previousState,
        string code,
        string message)
    {
        DiameterPeerEvent result = new DiameterPeerEvent();
        result.Kind = kind;
        result.Peer = PeerTableCloner.ClonePeerTableSnapshot(peer);
        result.PreviousState = previousState;
        result.CurrentState = peer.State;
        result.OccurredAt = Clock.ReadUtcTime();
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal void DispatchPeerManagerEvent(DiameterPeerEvent peerEvent)
    {
        List<IPeerEventHandler> handlers = new List<IPeerEventHandler>();
        lock (HandlerLock)
        {
            int index = 0;
            while (index < Handlers.Count)
            {
                handlers.Add(Handlers[index]);
                index = index + 1;
            }
        }

        int handlerIndex = 0;
        while (handlerIndex < handlers.Count)
        {
            handlers[handlerIndex].HandlePeerEvent(
                PeerTableCloner.ClonePeerTableEvent(peerEvent));
            handlerIndex = handlerIndex + 1;
        }
    }

    internal string ReadPeerManagerIdentity(DiameterIdentity? identity)
    {
        if (identity == null ||
            ValueCodecs.NormalizeDiameterIdentity(identity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid peer identity is required.");
        }

        return identity.Value;
    }
}
