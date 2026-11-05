using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Sends, tracks, correlates, and fails over outbound Diameter requests.</summary>
public class DiameterRequestClient : IDiameterRequestClient, IPeerEventHandler
{
    internal RequestClientRuntime Runtime;

    /// <summary>Initializes a request client and registers it for peer failure events.</summary>
    /// <param name="configuration">The node request limits and timers.</param>
    /// <param name="router">The outbound request router.</param>
    /// <param name="peerManager">The peer snapshot, transport, and event provider.</param>
    /// <param name="codec">The Diameter message codec.</param>
    /// <param name="identifierSource">The request identifier source.</param>
    /// <param name="clock">The request deadline clock.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when a dependency or configuration is invalid.
    /// </exception>
    public DiameterRequestClient(
        DiameterNodeConfiguration? configuration,
        IDiameterRouter? router,
        IDiameterPeerManager? peerManager,
        IDiameterMessageCodec? codec,
        IDiameterIdentifierSource? identifierSource,
        IDiameterClock? clock)
    {
        if (configuration == null || router == null ||
            peerManager == null || codec == null ||
            identifierSource == null || clock == null)
        {
            throw new ArgumentException(
                "Request client configuration and dependencies are required.");
        }

        Runtime = new RequestClientRuntime(
            configuration,
            router,
            peerManager,
            codec,
            identifierSource,
            clock);
        peerManager.RegisterPeerEventHandler(this);
    }

    /// <summary>Attempts to send and track one Diameter request.</summary>
    /// <param name="request">The outbound request and delivery policy.</param>
    /// <param name="handler">The handler for terminal and requested late outcomes.</param>
    /// <returns>The immediate send-admission outcome.</returns>
    public DiameterSendResult SendDiameterRequest(
        DiameterSendRequest? request,
        IDiameterAnswerHandler? handler)
    {
        if (request == null)
        {
            throw new ArgumentException("A send request is required.");
        }

        return Runtime.SendRequest(request, handler);
    }

    /// <summary>Cancels one pending Diameter request by its stable local identifier.</summary>
    /// <param name="requestIdentifier">The positive local request identifier.</param>
    /// <returns>The cancellation outcome.</returns>
    public DiameterAnswerResult CancelDiameterRequest(
        long requestIdentifier)
    {
        return Runtime.CancelRequest(requestIdentifier);
    }

    /// <summary>Reads independent snapshots of all pending requests.</summary>
    /// <returns>The pending request snapshots in creation order.</returns>
    public List<DiameterPendingRequestSnapshot> ReadPendingDiameterRequests()
    {
        return Runtime.ReadPendingRequests();
    }

    /// <summary>Handles peer failure and restart events for pending requests.</summary>
    /// <param name="peerEvent">The peer event to handle.</param>
    public void HandlePeerEvent(DiameterPeerEvent peerEvent)
    {
        if (peerEvent == null || peerEvent.Peer == null ||
            peerEvent.Peer.Configuration == null ||
            peerEvent.Peer.Configuration.Identity == null)
        {
            return;
        }

        bool failed = peerEvent.Kind ==
                DiameterPeerEventKind.TransportFailed ||
            peerEvent.Kind == DiameterPeerEventKind.RestartDetected ||
            (peerEvent.Kind == DiameterPeerEventKind.StateChanged &&
            peerEvent.CurrentState == DiameterPeerState.Closed);
        if (failed)
        {
            Runtime.HandlePeerFailure(
                peerEvent.Peer.Configuration.Identity);
        }
    }

    internal DiameterAnswerResult HandleReceivedDiameterAnswer(
        DiameterIdentity? peerIdentity,
        DiameterMessage? answer)
    {
        return Runtime.HandleReceivedAnswer(peerIdentity, answer);
    }

    internal List<DiameterAnswerResult> ExpireDiameterRequests()
    {
        return Runtime.ExpireRequests();
    }

    internal void ConfigureDiameterOverloadAdmission(
        IDiameterOverloadManager? manager)
    {
        Runtime.ConfigureDiameterOverloadAdmission(manager);
    }
}
