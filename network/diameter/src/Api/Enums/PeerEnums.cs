namespace diameter;

/// <summary>
/// Identifies an RFC 6733 peer state.
/// </summary>
public enum DiameterPeerState
{
    /// <summary>No transport connection exists.</summary>
    Closed = 0,

    /// <summary>An outbound connection attempt is pending.</summary>
    WaitConnectionAcknowledgement = 1,

    /// <summary>The initiator is waiting for a capabilities answer.</summary>
    WaitInitiatorCapabilitiesAnswer = 2,

    /// <summary>A connection acknowledgement or election is pending.</summary>
    WaitConnectionAcknowledgementElection = 3,

    /// <summary>The peer is waiting for an election result.</summary>
    WaitReturns = 4,

    /// <summary>The responder connection is open.</summary>
    ResponderOpen = 5,

    /// <summary>The initiator connection is open.</summary>
    InitiatorOpen = 6,

    /// <summary>The peer connection is closing.</summary>
    Closing = 7
}

/// <summary>
/// Identifies the RFC 3539 watchdog health state of a peer connection.
/// </summary>
public enum DiameterPeerWatchdogState
{
    /// <summary>The peer has never completed an initial connection.</summary>
    Initial = 0,

    /// <summary>The peer connection is healthy and available.</summary>
    Okay = 1,

    /// <summary>A pending watchdog expired and failover has begun.</summary>
    Suspect = 2,

    /// <summary>The failed peer connection has been closed.</summary>
    Down = 3,

    /// <summary>A reopened connection is being stabilized by watchdog exchanges.</summary>
    Reopen = 4
}

/// <summary>
/// Identifies the kind of peer event delivered to an observer.
/// </summary>
public enum DiameterPeerEventKind
{
    /// <summary>A peer was added.</summary>
    Added = 0,

    /// <summary>A peer was removed.</summary>
    Removed = 1,

    /// <summary>The peer state changed.</summary>
    StateChanged = 2,

    /// <summary>The advertised capabilities changed.</summary>
    CapabilitiesChanged = 3,

    /// <summary>A peer restart was detected.</summary>
    RestartDetected = 4,

    /// <summary>The peer transport failed.</summary>
    TransportFailed = 5
}

/// <summary>
/// Identifies which side initiated a Diameter connection.
/// </summary>
public enum DiameterConnectionDirection
{
    /// <summary>The remote peer initiated the connection.</summary>
    Inbound = 0,

    /// <summary>The local node initiated the connection.</summary>
    Outbound = 1
}

/// <summary>
/// Identifies an RFC 6733 disconnect cause.
/// </summary>
public enum DiameterDisconnectCause
{
    /// <summary>The peer is rebooting.</summary>
    Rebooting = 0,

    /// <summary>The peer is resource constrained.</summary>
    Busy = 1,

    /// <summary>The peer does not want the connection to remain established.</summary>
    DoNotWantToTalkToYou = 2
}

/// <summary>
/// Identifies how a peer entered the peer table.
/// </summary>
public enum DiameterPeerSource
{
    /// <summary>The peer was configured explicitly.</summary>
    Static = 0,

    /// <summary>The peer was discovered dynamically.</summary>
    Dynamic = 1
}

/// <summary>
/// Defines independently detectable peer capability changes.
/// </summary>
public enum DiameterCapabilityChange
{
    /// <summary>No capability changed.</summary>
    None = 0,

    /// <summary>The advertised host addresses changed.</summary>
    HostAddresses = 1,

    /// <summary>The advertised vendor information changed.</summary>
    VendorInformation = 2,

    /// <summary>The advertised product name changed.</summary>
    ProductName = 4,

    /// <summary>The advertised firmware revision changed.</summary>
    FirmwareRevision = 8,

    /// <summary>The advertised applications changed.</summary>
    Applications = 16,

    /// <summary>The advertised security mechanisms changed.</summary>
    SecurityMechanisms = 32
}
