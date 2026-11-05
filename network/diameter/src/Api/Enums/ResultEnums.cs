namespace diameter;

/// <summary>
/// Identifies the class encoded by the first digit of a Diameter result code.
/// </summary>
public enum DiameterResultClass
{
    /// <summary>Identifies an informational result.</summary>
    Informational = 1,

    /// <summary>Identifies a successful result.</summary>
    Success = 2,

    /// <summary>Identifies a protocol error.</summary>
    ProtocolError = 3,

    /// <summary>Identifies a transient failure.</summary>
    TransientFailure = 4,

    /// <summary>Identifies a permanent failure.</summary>
    PermanentFailure = 5
}

/// <summary>
/// Identifies the severity of a codec or validation issue.
/// </summary>
public enum DiameterIssueSeverity
{
    /// <summary>Provides diagnostic information.</summary>
    Information = 0,

    /// <summary>Identifies recoverable or permissive-profile input.</summary>
    Warning = 1,

    /// <summary>Identifies invalid input.</summary>
    Error = 2
}

/// <summary>
/// Identifies the outcome of a Diameter codec operation.
/// </summary>
public enum DiameterCodecStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success = 0,

    /// <summary>More input data is required.</summary>
    Incomplete = 1,

    /// <summary>The input data is invalid.</summary>
    Invalid = 2,

    /// <summary>The destination buffer is too small.</summary>
    DestinationTooSmall = 3,

    /// <summary>A configured codec limit was exceeded.</summary>
    LimitExceeded = 4
}

/// <summary>
/// Identifies the outcome of Diameter message validation.
/// </summary>
public enum DiameterValidationStatus
{
    /// <summary>The message is valid.</summary>
    Valid = 0,

    /// <summary>The message is invalid.</summary>
    Invalid = 1
}

/// <summary>
/// Identifies the outcome of registering a Diameter dictionary module.
/// </summary>
public enum DiameterDictionaryRegistrationStatus
{
    /// <summary>The module was registered and composed.</summary>
    Success = 0,

    /// <summary>An equivalent module is already registered.</summary>
    Duplicate = 1,

    /// <summary>The module or its dependencies are invalid.</summary>
    Invalid = 2,

    /// <summary>The module conflicts with a registered definition.</summary>
    Conflict = 3
}

/// <summary>
/// Identifies the outcome of accepting a request for sending.
/// </summary>
public enum DiameterSendStatus
{
    /// <summary>The request was accepted for sending.</summary>
    Accepted = 0,

    /// <summary>No usable route was available.</summary>
    NoRoute = 1,

    /// <summary>No compatible peer was available.</summary>
    PeerUnavailable = 2,

    /// <summary>The outbound queue was full.</summary>
    QueueFull = 3,

    /// <summary>The pending-request limit was reached.</summary>
    PendingLimitReached = 4,

    /// <summary>The request was rejected by overload control.</summary>
    Overloaded = 5,

    /// <summary>The sending subsystem is closed.</summary>
    Closed = 6,

    /// <summary>The send operation failed.</summary>
    Failed = 7
}

/// <summary>
/// Identifies the outcome of a transport capability, lifecycle, or input/output operation.
/// </summary>
public enum DiameterTransportStatus
{
    /// <summary>The transport operation completed successfully.</summary>
    Success = 0,

    /// <summary>The requested transport or security combination is not supported.</summary>
    Unsupported = 1,

    /// <summary>The supplied transport or security configuration is invalid.</summary>
    InvalidConfiguration = 2,

    /// <summary>The operation did not complete before its timeout.</summary>
    TimedOut = 3,

    /// <summary>A bounded transport queue could not accept more data.</summary>
    QueueFull = 4,

    /// <summary>The listener or connection is closed.</summary>
    Closed = 5,

    /// <summary>The remote endpoint ended the input stream cleanly.</summary>
    EndOfStream = 6,

    /// <summary>Transport security authentication failed.</summary>
    AuthenticationFailed = 7,

    /// <summary>The transport operation failed for another reason.</summary>
    Failed = 8
}

/// <summary>
/// Identifies the health of a node or subsystem.
/// </summary>
public enum DiameterHealthStatus
{
    /// <summary>The component is operating normally.</summary>
    Healthy = 0,

    /// <summary>The component is operating with reduced capability.</summary>
    Degraded = 1,

    /// <summary>The component cannot provide its required service.</summary>
    Unhealthy = 2,

    /// <summary>The component is stopped.</summary>
    Stopped = 3
}

/// <summary>Identifies the current lifecycle state of a Diameter node.</summary>
public enum DiameterNodeState
{
    /// <summary>The node has no active configuration.</summary>
    Unconfigured = 0,

    /// <summary>The node is validating and composing its configuration.</summary>
    Configuring = 1,

    /// <summary>The node is configured but not accepting work.</summary>
    Configured = 2,

    /// <summary>The node is starting configured applications and transports.</summary>
    Starting = 3,

    /// <summary>The node is running and accepting work.</summary>
    Running = 4,

    /// <summary>The node is finishing accepted work.</summary>
    Draining = 5,

    /// <summary>The node has finished accepted work but retains resources.</summary>
    Drained = 6,

    /// <summary>The node is releasing connections, workers, and stores.</summary>
    Stopping = 7,

    /// <summary>The node has released its owned runtime resources.</summary>
    Stopped = 8
}
