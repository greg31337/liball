namespace uspto;

/// <summary>
/// Identifies the origin and authority of returned data.
/// </summary>
public enum DataProvenance
{
    /// <summary>The origin is not known.</summary>
    Unknown = 0,

    /// <summary>Authoritative data returned by the USPTO Open Data Portal.</summary>
    Odp = 1,

    /// <summary>Authoritative trademark data returned by TSDR.</summary>
    Tsdr = 2,

    /// <summary>Research-derived data returned by PatentsView.</summary>
    PatentsView = 3,

    /// <summary>Data read directly from a USPTO bulk product.</summary>
    Bulk = 4,

    /// <summary>Data read from a local dataset mirror.</summary>
    LocalMirror = 5,

    /// <summary>Data restored from a response cache.</summary>
    Cache = 6,

    /// <summary>Data produced by a client-side join or other derivation.</summary>
    ClientDerived = 7
}
