namespace diameter;

/// <summary>
/// Identifies the state-management model of an authorization session.
/// </summary>
public enum DiameterSessionKind
{
    /// <summary>Maintains authorization session state.</summary>
    StatefulAuthorization = 0,

    /// <summary>Does not retain authorization session state.</summary>
    StatelessAuthorization = 1
}

/// <summary>
/// Identifies the local role in a Diameter session.
/// </summary>
public enum DiameterSessionRole
{
    /// <summary>The local node is the session client.</summary>
    Client = 0,

    /// <summary>The local node is the session server.</summary>
    Server = 1
}

/// <summary>
/// Identifies the lifecycle state of a Diameter authorization session.
/// </summary>
public enum DiameterSessionState
{
    /// <summary>The session has been created.</summary>
    Created = 0,

    /// <summary>The initial authorization is pending.</summary>
    Pending = 1,

    /// <summary>The session is open.</summary>
    Open = 2,

    /// <summary>Re-authorization is pending.</summary>
    Reauthorizing = 3,

    /// <summary>Session termination is pending.</summary>
    Terminating = 4,

    /// <summary>Session abort is pending.</summary>
    Aborting = 5,

    /// <summary>The session is closed.</summary>
    Closed = 6,

    /// <summary>The session ended because of a failure.</summary>
    Failed = 7
}

/// <summary>
/// Identifies an RFC 6733 authentication and authorization request type.
/// </summary>
public enum DiameterAuthRequestType
{
    /// <summary>Requests authentication only.</summary>
    AuthenticateOnly = 1,

    /// <summary>Requests authorization only.</summary>
    AuthorizeOnly = 2,

    /// <summary>Requests both authentication and authorization.</summary>
    AuthorizeAuthenticate = 3
}

/// <summary>
/// Identifies whether RFC 6733 authorization session state is maintained.
/// </summary>
public enum DiameterAuthSessionState
{
    /// <summary>Maintains authorization session state.</summary>
    StateMaintained = 0,

    /// <summary>Does not maintain authorization session state.</summary>
    NoStateMaintained = 1
}

/// <summary>
/// Identifies an RFC 6733 re-authorization request type.
/// </summary>
public enum DiameterReAuthRequestType
{
    /// <summary>Requests authorization only.</summary>
    AuthorizeOnly = 0,

    /// <summary>Requests authentication and authorization.</summary>
    AuthorizeAuthenticate = 1
}

/// <summary>
/// Identifies an RFC 6733 session termination cause.
/// </summary>
public enum DiameterTerminationCause
{
    /// <summary>The user requested logout.</summary>
    Logout = 1,

    /// <summary>The requested service was not provided.</summary>
    ServiceNotProvided = 2,

    /// <summary>A bad answer was received.</summary>
    BadAnswer = 3,

    /// <summary>An administrator terminated the session.</summary>
    Administrative = 4,

    /// <summary>The communication link broke.</summary>
    LinkBroken = 5,

    /// <summary>The authorization expired.</summary>
    AuthExpired = 6,

    /// <summary>The user moved to another access device.</summary>
    UserMoved = 7,

    /// <summary>The session timed out.</summary>
    SessionTimeout = 8
}
