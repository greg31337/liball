namespace sip;

/// <summary>Identifies how a proxy processes a request.</summary>
public enum SipProxyMode
{
    /// <summary>No proxy mode is selected.</summary>
    None = 0,
    /// <summary>Stateless proxy processing.</summary>
    Stateless = 1,
    /// <summary>Stateful sequential proxy processing.</summary>
    StatefulSequential = 2,
    /// <summary>Stateful parallel proxy processing.</summary>
    StatefulParallel = 3,
    /// <summary>Redirect-server processing.</summary>
    Redirect = 4
}
