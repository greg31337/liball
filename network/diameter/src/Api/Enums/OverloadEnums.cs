namespace diameter;

/// <summary>
/// Identifies the target of a Diameter overload report.
/// </summary>
public enum DiameterOverloadReportType
{
    /// <summary>The report applies to host-routed requests.</summary>
    Host = 0,

    /// <summary>The report applies to realm-routed requests.</summary>
    Realm = 1,

    /// <summary>The report applies to requests bound for a peer.</summary>
    Peer = 2
}

/// <summary>
/// Identifies a Diameter overload abatement algorithm.
/// </summary>
public enum DiameterOverloadAlgorithm
{
    /// <summary>No overload abatement algorithm is selected.</summary>
    None = 0,

    /// <summary>Uses the RFC 7683 loss abatement algorithm.</summary>
    Loss = 1,

    /// <summary>Uses the RFC 8582 rate abatement algorithm.</summary>
    Rate = 4
}

/// <summary>
/// Identifies the internal scope of overload state.
/// </summary>
public enum DiameterOverloadScope
{
    /// <summary>Scopes overload state to a host.</summary>
    Host = 0,

    /// <summary>Scopes overload state to a realm.</summary>
    Realm = 1,

    /// <summary>Scopes overload state to a peer.</summary>
    Peer = 2,

    /// <summary>Scopes overload state to an application.</summary>
    Application = 3
}

/// <summary>
/// Identifies the admission action selected for a request.
/// </summary>
public enum DiameterAdmissionAction
{
    /// <summary>Admits the request.</summary>
    Admit = 0,

    /// <summary>Throttles the request.</summary>
    Throttle = 1,

    /// <summary>Diverts the request to another destination.</summary>
    Divert = 2
}

/// <summary>
/// Identifies the source represented by a Diameter load report.
/// </summary>
public enum DiameterLoadSource
{
    /// <summary>The load report describes a host.</summary>
    Host = 0,

    /// <summary>The load report describes a peer.</summary>
    Peer = 1
}
