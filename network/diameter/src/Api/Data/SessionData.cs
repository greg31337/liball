using System;

namespace diameter;

/// <summary>Configures one client or server authorization session.</summary>
public class DiameterSessionConfiguration
{
    /// <summary>The supplied Session-Id, or an empty string to generate one.</summary>
    public string SessionId = string.Empty;

    /// <summary>The state-management model used by the session.</summary>
    public DiameterSessionKind Kind =
        DiameterSessionKind.StatefulAuthorization;

    /// <summary>The local role in the session.</summary>
    public DiameterSessionRole Role = DiameterSessionRole.Client;

    /// <summary>The authentication application identifier.</summary>
    public uint ApplicationId;

    /// <summary>The destination realm for a client session, or <c>null</c>.</summary>
    public DiameterIdentity? DestinationRealm;

    /// <summary>The destination host for a client session, or <c>null</c>.</summary>
    public DiameterIdentity? DestinationHost;

    /// <summary>The peer bound to the session, or <c>null</c>.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The User-Name associated with the session, or an empty string.</summary>
    public string UserName = string.Empty;

    /// <summary>The authentication and authorization operation requested.</summary>
    public DiameterAuthRequestType AuthRequestType =
        DiameterAuthRequestType.AuthorizeAuthenticate;

    /// <summary>Indicates whether authorization session state is maintained.</summary>
    public DiameterAuthSessionState AuthSessionState =
        DiameterAuthSessionState.StateMaintained;

    /// <summary>The nonnegative authorization lifetime in milliseconds, or zero when absent.</summary>
    public int AuthorizationLifetimeMilliseconds;

    /// <summary>The nonnegative authorization grace period in milliseconds.</summary>
    public int AuthGracePeriodMilliseconds;

    /// <summary>The nonnegative overall session timeout in milliseconds, or zero when absent.</summary>
    public int SessionTimeoutMilliseconds;

    /// <summary>The positive timeout for a multi-round exchange in milliseconds.</summary>
    public int MultiRoundTimeoutMilliseconds = 30000;

    /// <summary>The negotiated Session-Binding bit mask.</summary>
    public uint SessionBinding;

    /// <summary>The negotiated Session-Server-Failover value.</summary>
    public int SessionServerFailover;

    /// <summary>Allows an eligible request to fail over to another server.</summary>
    public bool AllowFailover = true;

    /// <summary>Caller correlation text retained with the session.</summary>
    public string CorrelationText = string.Empty;

    /// <summary>Owned application-defined state used to initialize the session.</summary>
    public byte[] ApplicationState = new byte[0];

    internal void ValidateSessionConfiguration()
    {
        int kind = (int)Kind;
        int role = (int)Role;
        int requestType = (int)AuthRequestType;
        int authState = (int)AuthSessionState;
        if (kind < (int)DiameterSessionKind.StatefulAuthorization ||
            kind > (int)DiameterSessionKind.StatelessAuthorization ||
            role < (int)DiameterSessionRole.Client ||
            role > (int)DiameterSessionRole.Server ||
            requestType < (int)DiameterAuthRequestType.AuthenticateOnly ||
            requestType >
                (int)DiameterAuthRequestType.AuthorizeAuthenticate ||
            authState < (int)DiameterAuthSessionState.StateMaintained ||
            authState >
                (int)DiameterAuthSessionState.NoStateMaintained)
        {
            throw new ArgumentOutOfRangeException(
                "Session kind, role, or authorization mode");
        }

        if ((Kind == DiameterSessionKind.StatefulAuthorization &&
            AuthSessionState != DiameterAuthSessionState.StateMaintained) ||
            (Kind == DiameterSessionKind.StatelessAuthorization &&
            AuthSessionState !=
                DiameterAuthSessionState.NoStateMaintained))
        {
            throw new ArgumentException(
                "The session kind and Auth-Session-State must agree.");
        }

        if (SessionId == null || UserName == null ||
            CorrelationText == null || ApplicationState == null ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(UserName) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(CorrelationText) >= 0)
        {
            throw new ArgumentException(
                "Session text and application state must be valid.");
        }

        bool hasRealm = DestinationRealm != null &&
            DestinationRealm.Value != null &&
            DestinationRealm.Value.Length > 0;
        if (Role == DiameterSessionRole.Client && !hasRealm)
        {
            throw new ArgumentException(
                "A client session requires a destination realm.");
        }

        if (hasRealm && ValueCodecs.NormalizeDiameterRealm(
            DestinationRealm).Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The session destination realm is invalid.");
        }

        if (DestinationHost != null &&
            ValueCodecs.NormalizeDiameterIdentity(DestinationHost).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The session destination host is invalid.");
        }

        if (PeerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The session peer identity is invalid.");
        }

        if (AuthorizationLifetimeMilliseconds < 0 ||
            AuthGracePeriodMilliseconds < 0 ||
            SessionTimeoutMilliseconds < 0 ||
            MultiRoundTimeoutMilliseconds <= 0 ||
            SessionBinding > 7U ||
            SessionServerFailover < 0 || SessionServerFailover > 3)
        {
            throw new ArgumentOutOfRangeException(
                "Session timer or failover value");
        }
    }
}

/// <summary>Contains the complete persistable state of one authorization session.</summary>
public class DiameterSessionRecord
{
    /// <summary>The globally unique Session-Id.</summary>
    public string SessionId = string.Empty;

    /// <summary>The configuration used to create the session.</summary>
    public DiameterSessionConfiguration Configuration =
        new DiameterSessionConfiguration();

    /// <summary>The current lifecycle state.</summary>
    public DiameterSessionState State = DiameterSessionState.Created;

    /// <summary>The nonnegative optimistic concurrency version.</summary>
    public long Version;

    /// <summary>The peer currently bound to the session, or <c>null</c>.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The route identifier retained for session affinity.</summary>
    public string RouteIdentifier = string.Empty;

    /// <summary>The last observed peer Origin-State-Id.</summary>
    public uint PeerOriginStateIdentifier;

    /// <summary>Indicates that a peer Origin-State-Id has been observed.</summary>
    public bool PeerOriginStateIdentifierPresent;

    /// <summary>The UTC time at which the session was created.</summary>
    public DateTimeOffset CreatedAt;

    /// <summary>The UTC time at which the record was last updated.</summary>
    public DateTimeOffset UpdatedAt;

    /// <summary>The UTC time at which the lifecycle state last changed.</summary>
    public DateTimeOffset StateChangedAt;

    /// <summary>The authorization lifetime deadline.</summary>
    public DateTimeOffset AuthorizationDeadline;

    /// <summary>Indicates that an authorization lifetime deadline is active.</summary>
    public bool AuthorizationDeadlinePresent;

    /// <summary>The authorization grace-period deadline.</summary>
    public DateTimeOffset GraceDeadline;

    /// <summary>Indicates that an authorization grace-period deadline is active.</summary>
    public bool GraceDeadlinePresent;

    /// <summary>The overall session timeout deadline.</summary>
    public DateTimeOffset SessionDeadline;

    /// <summary>Indicates that an overall session deadline is active.</summary>
    public bool SessionDeadlinePresent;

    /// <summary>The current multi-round exchange deadline.</summary>
    public DateTimeOffset MultiRoundDeadline;

    /// <summary>Indicates that a multi-round exchange deadline is active.</summary>
    public bool MultiRoundDeadlinePresent;

    /// <summary>The pending request identifier, or zero when none is pending.</summary>
    public long PendingRequestIdentifier;

    /// <summary>The most recent owned session request, or <c>null</c>.</summary>
    public DiameterMessage? LastRequest;

    /// <summary>The most recent owned session answer, or <c>null</c>.</summary>
    public DiameterMessage? LastAnswer;

    /// <summary>The terminal session cause when one is present.</summary>
    public DiameterTerminationCause TerminationCause;

    /// <summary>Indicates that a terminal session cause is present.</summary>
    public bool TerminationCausePresent;

    /// <summary>Indicates that initial authorization completed successfully.</summary>
    public bool InitialAuthorizationCompleted;

    /// <summary>Indicates that re-authorization has been requested.</summary>
    public bool ReauthorizationRequested;

    /// <summary>Indicates that explicit release has been requested.</summary>
    public bool ReleaseRequested;

    /// <summary>The re-authorization operation requested at lifetime expiry.</summary>
    public DiameterReAuthRequestType ReAuthRequestType =
        DiameterReAuthRequestType.AuthorizeOnly;

    /// <summary>Owned application-defined state persisted with the session.</summary>
    public byte[] ApplicationState = new byte[0];

    internal void ValidateSessionRecord()
    {
        if (SessionId == null || SessionId.Length == 0 ||
            RouteIdentifier == null || ApplicationState == null ||
            Configuration == null ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(RouteIdentifier) >= 0)
        {
            throw new ArgumentException(
                "A session record requires valid owned state.");
        }

        Configuration.ValidateSessionConfiguration();
        if (Configuration.SessionId.Length > 0 &&
            Configuration.SessionId != SessionId)
        {
            throw new ArgumentException(
                "The record and configuration Session-Id values differ.");
        }

        int state = (int)State;
        if (state < (int)DiameterSessionState.Created ||
            state > (int)DiameterSessionState.Failed ||
            Version < 0 || PendingRequestIdentifier < 0)
        {
            throw new ArgumentOutOfRangeException(
                "Session state, version, or request identifier");
        }

        if (PeerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The session record peer identity is invalid.");
        }

        if (UpdatedAt < CreatedAt || StateChangedAt < CreatedAt ||
            StateChangedAt > UpdatedAt)
        {
            throw new ArgumentException(
                "Session record timestamps are inconsistent.");
        }

        if ((LastRequest != null && LastRequest.Avps == null) ||
            (LastAnswer != null && LastAnswer.Avps == null))
        {
            throw new ArgumentException(
                "Session messages require AVP collections.");
        }

        int cause = (int)TerminationCause;
        int reauthType = (int)ReAuthRequestType;
        if (TerminationCausePresent &&
            (cause < (int)DiameterTerminationCause.Logout ||
            cause > (int)DiameterTerminationCause.SessionTimeout))
        {
            throw new ArgumentOutOfRangeException("TerminationCause");
        }

        if (reauthType <
                (int)DiameterReAuthRequestType.AuthorizeOnly ||
            reauthType >
                (int)DiameterReAuthRequestType.AuthorizeAuthenticate)
        {
            throw new ArgumentOutOfRangeException("ReAuthRequestType");
        }
    }
}

/// <summary>Contains a requested optimistic update to one session.</summary>
public class DiameterSessionUpdate
{
    /// <summary>The Session-Id of the record to update.</summary>
    public string SessionId = string.Empty;

    /// <summary>The version that must still be current when the update is applied.</summary>
    public long ExpectedVersion;

    /// <summary>The lifecycle state to store when state updating is enabled.</summary>
    public DiameterSessionState State;

    /// <summary>Indicates that the lifecycle state is updated.</summary>
    public bool UpdateState;

    /// <summary>The peer identity to store, or <c>null</c> to clear it.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>Indicates that the peer identity is updated.</summary>
    public bool UpdatePeerIdentity;

    /// <summary>The route identifier to store.</summary>
    public string RouteIdentifier = string.Empty;

    /// <summary>Indicates that the affinity route identifier is updated.</summary>
    public bool UpdateRouteIdentifier;

    /// <summary>The peer Origin-State-Id to store.</summary>
    public uint PeerOriginStateIdentifier;

    /// <summary>Indicates whether the updated peer Origin-State-Id is present.</summary>
    public bool PeerOriginStateIdentifierPresent;

    /// <summary>Indicates that peer Origin-State-Id metadata is updated.</summary>
    public bool UpdatePeerOriginStateIdentifier;

    /// <summary>The authorization lifetime deadline to store.</summary>
    public DateTimeOffset AuthorizationDeadline;

    /// <summary>Indicates whether the updated authorization deadline is active.</summary>
    public bool AuthorizationDeadlinePresent;

    /// <summary>Indicates that authorization deadline metadata is updated.</summary>
    public bool UpdateAuthorizationDeadline;

    /// <summary>The authorization grace-period deadline to store.</summary>
    public DateTimeOffset GraceDeadline;

    /// <summary>Indicates whether the updated grace deadline is active.</summary>
    public bool GraceDeadlinePresent;

    /// <summary>Indicates that grace deadline metadata is updated.</summary>
    public bool UpdateGraceDeadline;

    /// <summary>The overall session deadline to store.</summary>
    public DateTimeOffset SessionDeadline;

    /// <summary>Indicates whether the updated session deadline is active.</summary>
    public bool SessionDeadlinePresent;

    /// <summary>Indicates that overall session deadline metadata is updated.</summary>
    public bool UpdateSessionDeadline;

    /// <summary>The multi-round exchange deadline to store.</summary>
    public DateTimeOffset MultiRoundDeadline;

    /// <summary>Indicates whether the updated multi-round deadline is active.</summary>
    public bool MultiRoundDeadlinePresent;

    /// <summary>Indicates that multi-round deadline metadata is updated.</summary>
    public bool UpdateMultiRoundDeadline;

    /// <summary>The pending request identifier to store, or zero to clear it.</summary>
    public long PendingRequestIdentifier;

    /// <summary>Indicates that the pending request identifier is updated.</summary>
    public bool UpdatePendingRequestIdentifier;

    /// <summary>The most recent request to store, or <c>null</c> to clear it.</summary>
    public DiameterMessage? LastRequest;

    /// <summary>Indicates that the most recent request is updated.</summary>
    public bool UpdateLastRequest;

    /// <summary>The most recent answer to store, or <c>null</c> to clear it.</summary>
    public DiameterMessage? LastAnswer;

    /// <summary>Indicates that the most recent answer is updated.</summary>
    public bool UpdateLastAnswer;

    /// <summary>The termination cause to store.</summary>
    public DiameterTerminationCause TerminationCause;

    /// <summary>Indicates whether the updated termination cause is present.</summary>
    public bool TerminationCausePresent;

    /// <summary>Indicates that termination-cause metadata is updated.</summary>
    public bool UpdateTerminationCause;

    /// <summary>Indicates whether initial authorization has completed.</summary>
    public bool InitialAuthorizationCompleted;

    /// <summary>Indicates that initial-authorization completion is updated.</summary>
    public bool UpdateInitialAuthorizationCompleted;

    /// <summary>Indicates whether re-authorization has been requested.</summary>
    public bool ReauthorizationRequested;

    /// <summary>Indicates that the re-authorization request marker is updated.</summary>
    public bool UpdateReauthorizationRequested;

    /// <summary>Indicates whether explicit release has been requested.</summary>
    public bool ReleaseRequested;

    /// <summary>Indicates that the explicit-release marker is updated.</summary>
    public bool UpdateReleaseRequested;

    /// <summary>The re-authorization operation to store.</summary>
    public DiameterReAuthRequestType ReAuthRequestType;

    /// <summary>Indicates that the re-authorization operation is updated.</summary>
    public bool UpdateReAuthRequestType;

    /// <summary>Owned application-defined state to store.</summary>
    public byte[] ApplicationState = new byte[0];

    /// <summary>Indicates that application-defined state is updated.</summary>
    public bool UpdateApplicationState;

    /// <summary>The stable transition code reported with the update.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable transition description.</summary>
    public string Message = string.Empty;

    internal void ValidateSessionUpdate()
    {
        if (SessionId == null || SessionId.Length == 0 ||
            RouteIdentifier == null || ApplicationState == null ||
            Code == null || Message == null ||
            ValueCodecs.FindInvalidUtf16Index(SessionId) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(RouteIdentifier) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(Code) >= 0 ||
            ValueCodecs.FindInvalidUtf16Index(Message) >= 0)
        {
            throw new ArgumentException(
                "A session update requires valid owned values.");
        }

        int state = (int)State;
        if (ExpectedVersion < 0 || PendingRequestIdentifier < 0 ||
            (UpdateState &&
            (state < (int)DiameterSessionState.Created ||
            state > (int)DiameterSessionState.Failed)))
        {
            throw new ArgumentOutOfRangeException(
                "Session update version, state, or request identifier");
        }

        if (UpdatePeerIdentity && PeerIdentity != null &&
            ValueCodecs.NormalizeDiameterIdentity(PeerIdentity).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "The updated session peer identity is invalid.");
        }

        if ((UpdateLastRequest && LastRequest != null &&
            LastRequest.Avps == null) ||
            (UpdateLastAnswer && LastAnswer != null &&
            LastAnswer.Avps == null))
        {
            throw new ArgumentException(
                "Updated session messages require AVP collections.");
        }

        int cause = (int)TerminationCause;
        int reauthType = (int)ReAuthRequestType;
        if (UpdateTerminationCause && TerminationCausePresent &&
            (cause < (int)DiameterTerminationCause.Logout ||
            cause > (int)DiameterTerminationCause.SessionTimeout))
        {
            throw new ArgumentOutOfRangeException("TerminationCause");
        }

        if (UpdateReAuthRequestType &&
            (reauthType <
                (int)DiameterReAuthRequestType.AuthorizeOnly ||
            reauthType >
                (int)DiameterReAuthRequestType.AuthorizeAuthenticate))
        {
            throw new ArgumentOutOfRangeException("ReAuthRequestType");
        }
    }
}

/// <summary>Contains an owned inspection snapshot of one authorization session.</summary>
public class DiameterSessionSnapshot
{
    /// <summary>The globally unique Session-Id.</summary>
    public string SessionId = string.Empty;

    /// <summary>The session configuration.</summary>
    public DiameterSessionConfiguration Configuration =
        new DiameterSessionConfiguration();

    /// <summary>The current lifecycle state.</summary>
    public DiameterSessionState State = DiameterSessionState.Created;

    /// <summary>The optimistic concurrency version.</summary>
    public long Version;

    /// <summary>The peer currently bound to the session, or <c>null</c>.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The route retained for session affinity.</summary>
    public string RouteIdentifier = string.Empty;

    /// <summary>The last observed peer Origin-State-Id.</summary>
    public uint PeerOriginStateIdentifier;

    /// <summary>Indicates that a peer Origin-State-Id has been observed.</summary>
    public bool PeerOriginStateIdentifierPresent;

    /// <summary>The UTC time at which the session was created.</summary>
    public DateTimeOffset CreatedAt;

    /// <summary>The UTC time at which the session was last updated.</summary>
    public DateTimeOffset UpdatedAt;

    /// <summary>The UTC time at which the lifecycle state last changed.</summary>
    public DateTimeOffset StateChangedAt;

    /// <summary>The authorization lifetime deadline.</summary>
    public DateTimeOffset AuthorizationDeadline;

    /// <summary>Indicates that an authorization lifetime deadline is active.</summary>
    public bool AuthorizationDeadlinePresent;

    /// <summary>The authorization grace-period deadline.</summary>
    public DateTimeOffset GraceDeadline;

    /// <summary>Indicates that an authorization grace-period deadline is active.</summary>
    public bool GraceDeadlinePresent;

    /// <summary>The overall session timeout deadline.</summary>
    public DateTimeOffset SessionDeadline;

    /// <summary>Indicates that an overall session deadline is active.</summary>
    public bool SessionDeadlinePresent;

    /// <summary>The current multi-round exchange deadline.</summary>
    public DateTimeOffset MultiRoundDeadline;

    /// <summary>Indicates that a multi-round exchange deadline is active.</summary>
    public bool MultiRoundDeadlinePresent;

    /// <summary>The pending request identifier, or zero when none is pending.</summary>
    public long PendingRequestIdentifier;

    /// <summary>The most recent owned session request, or <c>null</c>.</summary>
    public DiameterMessage? LastRequest;

    /// <summary>The most recent owned session answer, or <c>null</c>.</summary>
    public DiameterMessage? LastAnswer;

    /// <summary>The terminal session cause when one is present.</summary>
    public DiameterTerminationCause TerminationCause;

    /// <summary>Indicates that a terminal session cause is present.</summary>
    public bool TerminationCausePresent;

    /// <summary>Indicates that initial authorization completed successfully.</summary>
    public bool InitialAuthorizationCompleted;

    /// <summary>Indicates that re-authorization has been requested.</summary>
    public bool ReauthorizationRequested;

    /// <summary>Indicates that explicit release has been requested.</summary>
    public bool ReleaseRequested;

    /// <summary>The re-authorization operation requested at lifetime expiry.</summary>
    public DiameterReAuthRequestType ReAuthRequestType =
        DiameterReAuthRequestType.AuthorizeOnly;

    /// <summary>Owned application-defined state associated with the session.</summary>
    public byte[] ApplicationState = new byte[0];
}

/// <summary>Describes one authorization-session lifecycle transition.</summary>
public class DiameterSessionEvent
{
    /// <summary>The owned session snapshot after the transition.</summary>
    public DiameterSessionSnapshot Session = new DiameterSessionSnapshot();

    /// <summary>The lifecycle state before the transition.</summary>
    public DiameterSessionState PreviousState = DiameterSessionState.Created;

    /// <summary>The lifecycle state after the transition.</summary>
    public DiameterSessionState CurrentState = DiameterSessionState.Created;

    /// <summary>The peer associated with the transition, or <c>null</c>.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>The message that triggered the transition, or <c>null</c>.</summary>
    public DiameterMessage? TriggerMessage;

    /// <summary>The UTC time at which the transition occurred.</summary>
    public DateTimeOffset OccurredAt;

    /// <summary>The stable machine-readable transition code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable transition description.</summary>
    public string Message = string.Empty;
}
