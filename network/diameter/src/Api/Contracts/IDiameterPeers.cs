using System.Collections.Generic;

namespace diameter;

/// <summary>Manages configured Diameter peers and exposes owned operational snapshots.</summary>
public interface IDiameterPeerManager
{
    /// <summary>Adds or replaces one peer configuration.</summary>
    /// <param name="configuration">The peer configuration to own.</param>
    /// <returns>The peer snapshot after applying the configuration.</returns>
    DiameterPeerSnapshot AddDiameterPeer(
        DiameterPeerConfiguration? configuration);

    /// <summary>Removes one disconnected peer.</summary>
    /// <param name="identity">The peer identity to remove.</param>
    /// <returns><c>true</c> when a peer was removed.</returns>
    bool RemoveDiameterPeer(DiameterIdentity? identity);

    /// <summary>Starts a connection attempt for one configured peer.</summary>
    /// <param name="identity">The peer identity to connect.</param>
    /// <returns>The peer snapshot after processing the request.</returns>
    DiameterPeerSnapshot ConnectDiameterPeer(DiameterIdentity? identity);

    /// <summary>Starts a graceful disconnect for one configured peer.</summary>
    /// <param name="request">The disconnect cause and completion policy.</param>
    /// <returns>The peer snapshot after processing the request.</returns>
    DiameterPeerSnapshot DisconnectDiameterPeer(
        DiameterDisconnectRequest? request);

    /// <summary>Reads an owned snapshot for one peer.</summary>
    /// <param name="identity">The peer identity to read.</param>
    /// <returns>The peer snapshot, or <c>null</c> when it is not configured.</returns>
    DiameterPeerSnapshot? ReadDiameterPeerSnapshot(DiameterIdentity? identity);

    /// <summary>Reads owned snapshots for all peers in deterministic table order.</summary>
    /// <returns>The peer snapshots.</returns>
    List<DiameterPeerSnapshot> ReadDiameterPeerSnapshots();

    /// <summary>Sends encoded Diameter bytes through one active peer connection.</summary>
    /// <param name="identity">The destination peer identity.</param>
    /// <param name="source">The complete encoded Diameter message.</param>
    /// <param name="timeoutMilliseconds">The positive send timeout in milliseconds.</param>
    /// <returns>The peer transport send outcome.</returns>
    DiameterTransportIoResult SendDiameterPeerBytes(
        DiameterIdentity? identity,
        byte[]? source,
        int timeoutMilliseconds);

    /// <summary>Registers a peer event handler idempotently.</summary>
    /// <param name="handler">The handler to register.</param>
    void RegisterPeerEventHandler(IPeerEventHandler? handler);

    /// <summary>Unregisters a peer event handler idempotently.</summary>
    /// <param name="handler">The handler to unregister.</param>
    void UnregisterPeerEventHandler(IPeerEventHandler? handler);
}

/// <summary>Receives peer state, capability, restart, and failure events.</summary>
public interface IPeerEventHandler
{
    /// <summary>Handles one owned peer event snapshot.</summary>
    /// <param name="peerEvent">The peer event to handle.</param>
    void HandlePeerEvent(DiameterPeerEvent peerEvent);
}
