using System;
using System.Collections.Generic;
using System.Net;

namespace diameter;

/// <summary>Contains the configured identity, endpoints, and admission settings for a peer.</summary>
public class DiameterPeerConfiguration
{
    /// <summary>The peer's Diameter host identity.</summary>
    public DiameterIdentity Identity = new DiameterIdentity();

    /// <summary>The peer's Diameter realm.</summary>
    public DiameterIdentity Realm = new DiameterIdentity();

    /// <summary>The candidate transport endpoints in preference order.</summary>
    public List<DiameterEndpoint> Endpoints = new List<DiameterEndpoint>();

    /// <summary>The applications that must be common before the peer becomes usable.</summary>
    public List<DiameterApplicationId> RequiredApplications =
        new List<DiameterApplicationId>();

    /// <summary>Identifies whether the peer was configured or discovered.</summary>
    public DiameterPeerSource Source;

    /// <summary>Indicates that the peer may be used.</summary>
    public bool Enabled = true;

    /// <summary>Indicates that outbound connection attempts may be started automatically.</summary>
    public bool ConnectAutomatically = true;

    /// <summary>Indicates that inbound connections from this peer may be admitted.</summary>
    public bool AcceptInboundConnections = true;

    /// <summary>The dynamic-peer expiry time, or the default value for a static peer.</summary>
    public DateTimeOffset ExpiresAt;

    internal void ValidatePeerConfiguration()
    {
        if (Identity == null || Realm == null || Endpoints == null ||
            RequiredApplications == null)
        {
            throw new ArgumentException("Peer configuration values must not be null.");
        }

        if (ValueCodecs.NormalizeDiameterIdentity(Identity).Status !=
                DiameterCodecStatus.Success ||
            ValueCodecs.NormalizeDiameterRealm(Realm).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid peer identity and realm are required.");
        }

        if (Source != DiameterPeerSource.Static &&
            Source != DiameterPeerSource.Dynamic)
        {
            throw new ArgumentOutOfRangeException("Source");
        }

        if ((Source == DiameterPeerSource.Static &&
            ExpiresAt != default(DateTimeOffset)) ||
            (Source == DiameterPeerSource.Dynamic &&
            ExpiresAt == default(DateTimeOffset)))
        {
            throw new ArgumentException("Only dynamic peers require an expiry time.");
        }

        if (Endpoints.Count == 0)
        {
            throw new ArgumentException("At least one peer endpoint is required.");
        }

        int endpointIndex = 0;
        while (endpointIndex < Endpoints.Count)
        {
            DiameterEndpoint endpoint = Endpoints[endpointIndex];
            ValidatePeerEndpoint(endpoint);
            int comparisonIndex = endpointIndex + 1;
            while (comparisonIndex < Endpoints.Count)
            {
                DiameterEndpoint comparison = Endpoints[comparisonIndex];
                if (comparison == null)
                {
                    throw new ArgumentException("Peer endpoints must not contain null values.");
                }

                bool sameAddress = endpoint.Address == null && comparison.Address == null;
                if (endpoint.Address != null && comparison.Address != null)
                {
                    sameAddress = endpoint.Address.Equals(comparison.Address);
                }

                if (sameAddress && string.Equals(
                    endpoint.Host,
                    comparison.Host,
                    StringComparison.OrdinalIgnoreCase) &&
                    endpoint.Port == comparison.Port &&
                    endpoint.Transport == comparison.Transport &&
                    endpoint.Security == comparison.Security)
                {
                    throw new ArgumentException("Peer endpoints must be unique.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            endpointIndex = endpointIndex + 1;
        }

        int applicationIndex = 0;
        while (applicationIndex < RequiredApplications.Count)
        {
            DiameterApplicationId application = RequiredApplications[applicationIndex];
            if (application == null)
            {
                throw new ArgumentException(
                    "Required applications must not contain null values.");
            }

            int comparisonIndex = applicationIndex + 1;
            while (comparisonIndex < RequiredApplications.Count)
            {
                DiameterApplicationId comparison =
                    RequiredApplications[comparisonIndex];
                if (comparison == null)
                {
                    throw new ArgumentException(
                        "Required applications must not contain null values.");
                }

                if (application.Id == comparison.Id &&
                    application.VendorId == comparison.VendorId &&
                    application.IsAccounting == comparison.IsAccounting)
                {
                    throw new ArgumentException(
                        "Required applications must be unique.");
                }

                comparisonIndex = comparisonIndex + 1;
            }

            applicationIndex = applicationIndex + 1;
        }
    }

    internal void ValidatePeerEndpoint(DiameterEndpoint endpoint)
    {
        if (endpoint == null || endpoint.Identity == null)
        {
            throw new ArgumentException("Peer endpoints must not contain null values.");
        }

        if (endpoint.Transport != DiameterTransportKind.Tcp &&
            endpoint.Transport != DiameterTransportKind.Sctp &&
            endpoint.Transport != DiameterTransportKind.External)
        {
            throw new ArgumentOutOfRangeException("Endpoint transport");
        }

        if (endpoint.Security != DiameterSecurityMode.None &&
            endpoint.Security != DiameterSecurityMode.Tls &&
            endpoint.Security != DiameterSecurityMode.Dtls &&
            endpoint.Security != DiameterSecurityMode.Ipsec &&
            endpoint.Security != DiameterSecurityMode.External)
        {
            throw new ArgumentOutOfRangeException("Endpoint security");
        }

        if (endpoint.Port < 0 || endpoint.Port > 65535 ||
            (endpoint.Transport != DiameterTransportKind.External &&
            endpoint.Port == 0) ||
            (endpoint.Transport != DiameterTransportKind.External &&
            endpoint.Address == null && string.IsNullOrWhiteSpace(endpoint.Host)))
        {
            throw new ArgumentException("A valid peer endpoint address and port are required.");
        }

        if ((!string.IsNullOrWhiteSpace(endpoint.Identity.Value) &&
            ValueCodecs.NormalizeDiameterIdentity(endpoint.Identity).Status !=
                DiameterCodecStatus.Success) ||
            (endpoint.Security == DiameterSecurityMode.Tls &&
            endpoint.Transport != DiameterTransportKind.Tcp) ||
            (endpoint.Security == DiameterSecurityMode.Dtls &&
            endpoint.Transport != DiameterTransportKind.Sctp) ||
            (endpoint.Security == DiameterSecurityMode.External &&
            endpoint.Transport != DiameterTransportKind.External))
        {
            throw new ArgumentException("The peer endpoint identity or security is invalid.");
        }
    }
}

/// <summary>Contains capabilities advertised by a Diameter peer.</summary>
public class DiameterPeerCapabilities
{
    /// <summary>The peer's advertised Origin-Host.</summary>
    public DiameterIdentity OriginHost = new DiameterIdentity();

    /// <summary>The peer's advertised Origin-Realm.</summary>
    public DiameterIdentity OriginRealm = new DiameterIdentity();

    /// <summary>The advertised Host-IP-Address values.</summary>
    public List<IPAddress> HostAddresses = new List<IPAddress>();

    /// <summary>The advertised Vendor-Id.</summary>
    public uint VendorId;

    /// <summary>The advertised Product-Name.</summary>
    public string ProductName = string.Empty;

    /// <summary>The advertised Firmware-Revision.</summary>
    public uint FirmwareRevision;

    /// <summary>The advertised Supported-Vendor-Id values.</summary>
    public List<uint> SupportedVendorIds = new List<uint>();

    /// <summary>The advertised Auth-Application-Id values.</summary>
    public List<uint> AuthApplicationIds = new List<uint>();

    /// <summary>The advertised Acct-Application-Id values.</summary>
    public List<uint> AcctApplicationIds = new List<uint>();

    /// <summary>The advertised vendor-specific application identifiers.</summary>
    public List<DiameterApplicationId> VendorSpecificApplications =
        new List<DiameterApplicationId>();

    /// <summary>The advertised Inband-Security-Id values.</summary>
    public List<uint> InbandSecurityIds = new List<uint>();

    /// <summary>The last advertised Origin-State-Id.</summary>
    public uint OriginStateId;

    /// <summary>The applications common to the local node and peer.</summary>
    public List<DiameterApplicationId> CommonApplications =
        new List<DiameterApplicationId>();

    /// <summary>The time at which this capability snapshot was updated.</summary>
    public DateTimeOffset UpdatedAt;
}

/// <summary>Contains an immutable-style operational view of one peer table entry.</summary>
public class DiameterPeerSnapshot
{
    /// <summary>The peer configuration represented by the snapshot.</summary>
    public DiameterPeerConfiguration Configuration = new DiameterPeerConfiguration();

    /// <summary>The most recently accepted peer capabilities.</summary>
    public DiameterPeerCapabilities Capabilities = new DiameterPeerCapabilities();

    /// <summary>The endpoint currently selected for the peer, or <c>null</c> when absent.</summary>
    public DiameterEndpoint? CurrentEndpoint;

    /// <summary>The active connection metadata, or <c>null</c> when disconnected.</summary>
    public DiameterConnectionMetadata? Connection;

    /// <summary>The current RFC 6733 peer state.</summary>
    public DiameterPeerState State;

    /// <summary>The direction of the selected connection.</summary>
    public DiameterConnectionDirection Direction;

    /// <summary>The time of the most recent state transition.</summary>
    public DateTimeOffset StateChangedAt;

    /// <summary>The time at which the current connection became open.</summary>
    public DateTimeOffset ConnectedAt;

    /// <summary>The next scheduled outbound connection attempt.</summary>
    public DateTimeOffset NextConnectionAttemptAt;

    /// <summary>The time at which transport traffic was last observed.</summary>
    public DateTimeOffset LastActivityAt;

    /// <summary>The time of the most recent watchdog exchange.</summary>
    public DateTimeOffset LastWatchdogAt;

    /// <summary>The current RFC 3539 watchdog health state.</summary>
    public DiameterPeerWatchdogState WatchdogState;

    /// <summary>The next watchdog timer deadline.</summary>
    public DateTimeOffset WatchdogDeadlineAt;

    /// <summary>The Hop-by-Hop Identifier of the pending watchdog request.</summary>
    public uint PendingWatchdogHopByHopIdentifier;

    /// <summary>The End-to-End Identifier of the pending watchdog request.</summary>
    public uint PendingWatchdogEndToEndIdentifier;

    /// <summary>The number of consecutive pending expirations while stabilizing a reopened connection.</summary>
    public int ReopenWatchdogMisses;

    /// <summary>The number of consecutive successful watchdog exchanges.</summary>
    public int ConsecutiveWatchdogSuccesses;

    /// <summary>The number of consecutive unanswered watchdog exchanges.</summary>
    public int ConsecutiveWatchdogFailures;

    /// <summary>The number of outbound reconnection attempts.</summary>
    public long ReconnectCount;

    /// <summary>Indicates that periodic outbound reconnection is suppressed.</summary>
    public bool ReconnectSuppressed;

    /// <summary>The disconnect cause responsible for suppression when suppression is active.</summary>
    public DiameterDisconnectCause ReconnectSuppressionCause;

    /// <summary>The time at which periodic reconnection was suppressed.</summary>
    public DateTimeOffset ReconnectSuppressedAt;

    /// <summary>Indicates that a Disconnect-Peer exchange is active.</summary>
    public bool DisconnectPending;

    /// <summary>Indicates that the local node initiated the active disconnect.</summary>
    public bool DisconnectInitiatedLocally;

    /// <summary>Indicates that queued output must drain before transport closure.</summary>
    public bool DisconnectDrainPending;

    /// <summary>Indicates that the local node is waiting for a Disconnect-Peer answer.</summary>
    public bool DisconnectAnswerPending;

    /// <summary>Indicates that the peer is expected to close the transport after an answer.</summary>
    public bool DisconnectTransportClosurePending;

    /// <summary>The deadline for completing the active disconnect exchange.</summary>
    public DateTimeOffset DisconnectDeadlineAt;

    /// <summary>The Hop-by-Hop Identifier of the pending local Disconnect-Peer request.</summary>
    public uint PendingDisconnectHopByHopIdentifier;

    /// <summary>The End-to-End Identifier of the pending local Disconnect-Peer request.</summary>
    public uint PendingDisconnectEndToEndIdentifier;

    /// <summary>The cause associated with the active or most recent disconnect.</summary>
    public DiameterDisconnectCause LastDisconnectCause;

    /// <summary>The time at which the most recent disconnect completed.</summary>
    public DateTimeOffset LastDisconnectedAt;

    /// <summary>Indicates that a watchdog answer is pending.</summary>
    public bool WatchdogPending;

    /// <summary>Indicates that the peer is currently eligible for routing.</summary>
    public bool Available;
}

/// <summary>Contains one peer lifecycle or capability notification.</summary>
public class DiameterPeerEvent
{
    /// <summary>The event kind.</summary>
    public DiameterPeerEventKind Kind;

    /// <summary>The peer snapshot after the event.</summary>
    public DiameterPeerSnapshot Peer = new DiameterPeerSnapshot();

    /// <summary>The state before the event.</summary>
    public DiameterPeerState PreviousState;

    /// <summary>The state after the event.</summary>
    public DiameterPeerState CurrentState;

    /// <summary>The independently changed capability fields.</summary>
    public DiameterCapabilityChange CapabilityChange;

    /// <summary>The Origin-State-Id observed before a restart or update.</summary>
    public uint PreviousOriginStateId;

    /// <summary>The Origin-State-Id observed after a restart or update.</summary>
    public uint CurrentOriginStateId;

    /// <summary>The UTC time at which the event occurred.</summary>
    public DateTimeOffset OccurredAt;

    /// <summary>The stable machine-readable event code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable event description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains a request to gracefully disconnect one peer.</summary>
public class DiameterDisconnectRequest
{
    /// <summary>The Diameter identity of the peer to disconnect.</summary>
    public DiameterIdentity PeerIdentity = new DiameterIdentity();

    /// <summary>The Disconnect-Cause sent to the peer.</summary>
    public DiameterDisconnectCause Cause;

    /// <summary>The positive time allowed for the disconnect exchange.</summary>
    public int TimeoutMilliseconds = 30000;

    /// <summary>Indicates that shutdown should wait for a Disconnect-Peer answer.</summary>
    public bool WaitForAnswer = true;

    /// <summary>Prevents automatic reconnection after the disconnect.</summary>
    public bool SuppressReconnect;

    internal void ValidateDisconnectRequest()
    {
        if (PeerIdentity == null ||
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid peer identity is required.");
        }

        if (Cause != DiameterDisconnectCause.Rebooting &&
            Cause != DiameterDisconnectCause.Busy &&
            Cause != DiameterDisconnectCause.DoNotWantToTalkToYou)
        {
            throw new ArgumentOutOfRangeException("Cause");
        }

        if (TimeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException("TimeoutMilliseconds");
        }
    }
}
