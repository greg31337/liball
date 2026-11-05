namespace diameter;

/// <summary>
/// Identifies the action selected for a Diameter request route.
/// </summary>
public enum DiameterRouteAction
{
    /// <summary>No usable route was found.</summary>
    NoRoute = 0,

    /// <summary>Delivers the request to a local application.</summary>
    Local = 1,

    /// <summary>Relays the request without application-level modification.</summary>
    Relay = 2,

    /// <summary>Proxies the request.</summary>
    Proxy = 3,

    /// <summary>Returns redirect routing information.</summary>
    Redirect = 4,

    /// <summary>Translates the request to another application.</summary>
    Translate = 5,

    /// <summary>Rejects the request by policy.</summary>
    Reject = 6
}

/// <summary>
/// Identifies the agent role applied while routing a request.
/// </summary>
public enum DiameterAgentRole
{
    /// <summary>No agent role is applied.</summary>
    None = 0,

    /// <summary>The node acts as a relay agent.</summary>
    Relay = 1,

    /// <summary>The node acts as a proxy agent.</summary>
    Proxy = 2,

    /// <summary>The node acts as a redirect agent.</summary>
    Redirect = 3,

    /// <summary>The node acts as a translation agent.</summary>
    Translation = 4
}

/// <summary>
/// Identifies the RFC 6733 scope of a cached redirect route.
/// </summary>
public enum DiameterRedirectHostUsage
{
    /// <summary>Does not cache the redirect host.</summary>
    DoNotCache = 0,

    /// <summary>Applies the redirect to the entire session.</summary>
    AllSession = 1,

    /// <summary>Applies the redirect to the entire realm.</summary>
    AllRealm = 2,

    /// <summary>Applies the redirect to one application in one realm.</summary>
    RealmAndApplication = 3,

    /// <summary>Applies the redirect to the entire application.</summary>
    AllApplication = 4,

    /// <summary>Applies the redirect to the destination host.</summary>
    AllHost = 5,

    /// <summary>Applies the redirect to the requested user.</summary>
    AllUser = 6
}

/// <summary>
/// Identifies the source that supplied a route.
/// </summary>
public enum DiameterRouteSource
{
    /// <summary>No source supplied a route.</summary>
    None = 0,

    /// <summary>The route came from static configuration.</summary>
    Static = 1,

    /// <summary>The route came from a redirect cache entry.</summary>
    Redirect = 2,

    /// <summary>The route came from dynamic discovery.</summary>
    Dynamic = 3,

    /// <summary>The route is a configured default.</summary>
    Default = 4,

    /// <summary>The route came from session affinity.</summary>
    SessionAffinity = 5
}

/// <summary>
/// Identifies an RFC 7944 Diameter routing message priority.
/// </summary>
public enum DiameterRoutingPriority
{
    /// <summary>The highest routing priority.</summary>
    Priority0 = 0,

    /// <summary>Routing priority one.</summary>
    Priority1 = 1,

    /// <summary>Routing priority two.</summary>
    Priority2 = 2,

    /// <summary>Routing priority three.</summary>
    Priority3 = 3,

    /// <summary>Routing priority four.</summary>
    Priority4 = 4,

    /// <summary>Routing priority five.</summary>
    Priority5 = 5,

    /// <summary>Routing priority six.</summary>
    Priority6 = 6,

    /// <summary>Routing priority seven.</summary>
    Priority7 = 7,

    /// <summary>Routing priority eight.</summary>
    Priority8 = 8,

    /// <summary>Routing priority nine.</summary>
    Priority9 = 9,

    /// <summary>Routing priority ten.</summary>
    Priority10 = 10,

    /// <summary>Routing priority eleven.</summary>
    Priority11 = 11,

    /// <summary>Routing priority twelve.</summary>
    Priority12 = 12,

    /// <summary>Routing priority thirteen.</summary>
    Priority13 = 13,

    /// <summary>Routing priority fourteen.</summary>
    Priority14 = 14,

    /// <summary>The lowest routing priority.</summary>
    Priority15 = 15
}

/// <summary>
/// Identifies the outcome of an application translation attempt.
/// </summary>
public enum DiameterTranslationDisposition
{
    /// <summary>The message was left untranslated.</summary>
    Untranslated = 0,

    /// <summary>The message was translated successfully.</summary>
    Translated = 1,

    /// <summary>The translation failed.</summary>
    Error = 2
}
