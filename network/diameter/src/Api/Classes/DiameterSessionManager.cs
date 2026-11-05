using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>
/// Creates, updates, ends, and inspects owned authorization-session state.
/// </summary>
public class DiameterSessionManager : IDiameterSessionManager
{
    internal IDiameterSessionStore Store;
    internal ISessionEventHandler? EventHandler;
    internal IDiameterClock Clock;
    internal IDiameterIdentifierSource IdentifierSource;
    internal DiameterIdentity OriginHost = new DiameterIdentity();
    internal object ManagerLock = new object();
    internal List<string> SessionOrder = new List<string>();

    /// <summary>Initializes a session manager over the supplied persistence and event dependencies.</summary>
    /// <param name="originHost">The local Diameter identity used to generate Session-Ids.</param>
    /// <param name="store">The optimistic session persistence provider.</param>
    /// <param name="eventHandler">The transition handler, or <c>null</c> when events are not required.</param>
    /// <param name="clock">The UTC clock used for session timestamps.</param>
    /// <param name="identifierSource">The source used to generate Session-Ids.</param>
    /// <exception cref="System.ArgumentException">Thrown when a required dependency or identity is invalid.</exception>
    public DiameterSessionManager(
        DiameterIdentity? originHost,
        IDiameterSessionStore? store,
        ISessionEventHandler? eventHandler,
        IDiameterClock? clock,
        IDiameterIdentifierSource? identifierSource)
    {
        DiameterValueResult normalized =
            ValueCodecs.NormalizeDiameterIdentity(originHost);
        if (store == null || clock == null || identifierSource == null ||
            normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException(
                "A valid origin identity and session dependencies are required.");
        }

        OriginHost.Value = normalized.Text;
        Store = store;
        EventHandler = eventHandler;
        Clock = clock;
        IdentifierSource = identifierSource;
    }

    /// <summary>Creates one authorization session with owned initial state.</summary>
    /// <param name="configuration">The validated session configuration.</param>
    /// <returns>The created owned session snapshot.</returns>
    public DiameterSessionSnapshot CreateDiameterSession(
        DiameterSessionConfiguration? configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentException(
                "A session configuration is required.");
        }

        configuration.ValidateSessionConfiguration();
        DiameterSessionConfiguration owned =
            SessionRecordCloner.CloneSessionConfiguration(configuration);
        if (owned.SessionId.Length == 0)
        {
            owned.SessionId = IdentifierSource.CreateSessionIdentifier(
                OriginHost,
                owned.CorrelationText);
        }

        DateTimeOffset now = Clock.ReadUtcTime();
        DiameterSessionRecord record = new DiameterSessionRecord();
        record.SessionId = owned.SessionId;
        record.Configuration = owned;
        record.State = DiameterSessionState.Created;
        record.PeerIdentity =
            SessionRecordCloner.CloneOptionalSessionIdentity(
                owned.PeerIdentity);
        record.CreatedAt = now;
        record.UpdatedAt = now;
        record.StateChangedAt = now;
        record.ApplicationState = SessionRecordCloner.CloneSessionBytes(
            owned.ApplicationState);
        DiameterSessionRecord saved = Store.SaveDiameterSession(record, 0);
        TrackManagerSessionIdentifier(saved.SessionId);
        DiameterSessionSnapshot result = CreateManagerSessionSnapshot(saved);
        PublishManagerSessionEvent(
            saved,
            DiameterSessionState.Created,
            null,
            "session.created",
            "The authorization session was created.");
        return result;
    }

    /// <summary>Finds one session without changing persisted state.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <returns>An owned snapshot, or <c>null</c> when absent.</returns>
    public DiameterSessionSnapshot? FindDiameterSession(string? sessionId)
    {
        DiameterSessionRecord? record = Store.LoadDiameterSession(sessionId);
        if (record == null)
        {
            return null;
        }

        TrackManagerSessionIdentifier(record.SessionId);
        return CreateManagerSessionSnapshot(record);
    }

    /// <summary>Applies one optimistic session update.</summary>
    /// <param name="update">The requested changes and expected version.</param>
    /// <returns>The updated owned session snapshot.</returns>
    public DiameterSessionSnapshot UpdateDiameterSession(
        DiameterSessionUpdate? update)
    {
        if (update == null)
        {
            throw new ArgumentException("A session update is required.");
        }

        update.ValidateSessionUpdate();
        if (!ContainsManagerSessionChange(update))
        {
            throw new ArgumentException(
                "A session update must change persisted state.");
        }

        DiameterSessionRecord? loaded = Store.LoadDiameterSession(
            update.SessionId);
        if (loaded == null)
        {
            throw new DiameterSessionException(
                "The session does not exist.");
        }

        if (loaded.Version != update.ExpectedVersion)
        {
            throw new DiameterSessionException(
                "The session version changed before the update.");
        }

        DateTimeOffset now = Clock.ReadUtcTime();
        if (now < loaded.UpdatedAt)
        {
            throw new ArgumentException(
                "A session update cannot move time backward.");
        }

        DiameterSessionState previous = loaded.State;
        ApplyManagerSessionUpdate(loaded, update, now);
        DiameterSessionRecord saved = Store.SaveDiameterSession(
            loaded,
            update.ExpectedVersion);
        TrackManagerSessionIdentifier(saved.SessionId);
        DiameterSessionSnapshot result = CreateManagerSessionSnapshot(saved);
        PublishManagerSessionEvent(
            saved,
            previous,
            ReadManagerUpdateTrigger(update, saved),
            update.Code.Length > 0 ? update.Code : "session.updated",
            update.Message.Length > 0 ?
                update.Message :
                "The authorization session was updated.");
        return result;
    }

    /// <summary>Ends one session and retains its terminal snapshot.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <param name="cause">The reason the session ended.</param>
    /// <returns>The terminal owned session snapshot.</returns>
    public DiameterSessionSnapshot EndDiameterSession(
        string? sessionId,
        DiameterTerminationCause cause)
    {
        int causeValue = (int)cause;
        if (causeValue < (int)DiameterTerminationCause.Logout ||
            causeValue > (int)DiameterTerminationCause.SessionTimeout)
        {
            throw new ArgumentOutOfRangeException("cause");
        }

        DiameterSessionRecord? loaded = Store.LoadDiameterSession(sessionId);
        if (loaded == null)
        {
            throw new DiameterSessionException(
                "The session does not exist.");
        }

        if (loaded.State == DiameterSessionState.Closed)
        {
            TrackManagerSessionIdentifier(loaded.SessionId);
            return CreateManagerSessionSnapshot(loaded);
        }

        DiameterSessionState previous = loaded.State;
        DateTimeOffset now = Clock.ReadUtcTime();
        if (now < loaded.UpdatedAt)
        {
            throw new ArgumentException(
                "Session termination cannot move time backward.");
        }

        loaded.State = DiameterSessionState.Closed;
        loaded.StateChangedAt = now;
        loaded.UpdatedAt = now;
        loaded.PendingRequestIdentifier = 0;
        loaded.TerminationCause = cause;
        loaded.TerminationCausePresent = true;
        loaded.ReleaseRequested = true;
        loaded.ReauthorizationRequested = false;
        ClearManagerSessionTimers(loaded);
        DiameterSessionRecord saved = Store.SaveDiameterSession(
            loaded,
            loaded.Version);
        TrackManagerSessionIdentifier(saved.SessionId);
        DiameterSessionSnapshot result = CreateManagerSessionSnapshot(saved);
        PublishManagerSessionEvent(
            saved,
            previous,
            null,
            "session.ended",
            "The authorization session ended.");
        return result;
    }

    /// <summary>Reads owned snapshots in deterministic manager creation order.</summary>
    /// <returns>The retained session snapshots.</returns>
    public List<DiameterSessionSnapshot> ReadDiameterSessionSnapshots()
    {
        List<string> identifiers = new List<string>();
        lock (ManagerLock)
        {
            identifiers.AddRange(SessionOrder);
        }

        List<DiameterSessionSnapshot> result =
            new List<DiameterSessionSnapshot>();
        int index = 0;
        while (index < identifiers.Count)
        {
            DiameterSessionRecord? record = Store.LoadDiameterSession(
                identifiers[index]);
            if (record != null)
            {
                result.Add(CreateManagerSessionSnapshot(record));
            }

            index = index + 1;
        }

        return result;
    }

    internal void TrackManagerSessionIdentifier(string sessionId)
    {
        lock (ManagerLock)
        {
            if (!SessionOrder.Contains(sessionId))
            {
                SessionOrder.Add(sessionId);
            }
        }
    }

    internal bool ContainsManagerSessionChange(
        DiameterSessionUpdate update)
    {
        return update.UpdateState || update.UpdatePeerIdentity ||
            update.UpdateRouteIdentifier ||
            update.UpdatePeerOriginStateIdentifier ||
            update.UpdateAuthorizationDeadline ||
            update.UpdateGraceDeadline ||
            update.UpdateSessionDeadline ||
            update.UpdateMultiRoundDeadline ||
            update.UpdatePendingRequestIdentifier ||
            update.UpdateLastRequest || update.UpdateLastAnswer ||
            update.UpdateTerminationCause ||
            update.UpdateInitialAuthorizationCompleted ||
            update.UpdateReauthorizationRequested ||
            update.UpdateReleaseRequested ||
            update.UpdateReAuthRequestType ||
            update.UpdateApplicationState;
    }

    internal void ApplyManagerSessionUpdate(
        DiameterSessionRecord record,
        DiameterSessionUpdate update,
        DateTimeOffset updatedAt)
    {
        if (update.UpdateState)
        {
            record.State = update.State;
            record.StateChangedAt = updatedAt;
        }

        if (update.UpdatePeerIdentity)
        {
            record.PeerIdentity =
                SessionRecordCloner.CloneOptionalSessionIdentity(
                    update.PeerIdentity);
        }

        if (update.UpdateRouteIdentifier)
        {
            record.RouteIdentifier = update.RouteIdentifier;
        }

        if (update.UpdatePeerOriginStateIdentifier)
        {
            record.PeerOriginStateIdentifier =
                update.PeerOriginStateIdentifier;
            record.PeerOriginStateIdentifierPresent =
                update.PeerOriginStateIdentifierPresent;
        }

        if (update.UpdateAuthorizationDeadline)
        {
            record.AuthorizationDeadline = update.AuthorizationDeadline;
            record.AuthorizationDeadlinePresent =
                update.AuthorizationDeadlinePresent;
        }

        if (update.UpdateGraceDeadline)
        {
            record.GraceDeadline = update.GraceDeadline;
            record.GraceDeadlinePresent = update.GraceDeadlinePresent;
        }

        if (update.UpdateSessionDeadline)
        {
            record.SessionDeadline = update.SessionDeadline;
            record.SessionDeadlinePresent =
                update.SessionDeadlinePresent;
        }

        if (update.UpdateMultiRoundDeadline)
        {
            record.MultiRoundDeadline = update.MultiRoundDeadline;
            record.MultiRoundDeadlinePresent =
                update.MultiRoundDeadlinePresent;
        }

        if (update.UpdatePendingRequestIdentifier)
        {
            record.PendingRequestIdentifier =
                update.PendingRequestIdentifier;
        }

        if (update.UpdateLastRequest)
        {
            record.LastRequest =
                SessionRecordCloner.CloneOptionalSessionMessage(
                    update.LastRequest);
        }

        if (update.UpdateLastAnswer)
        {
            record.LastAnswer =
                SessionRecordCloner.CloneOptionalSessionMessage(
                    update.LastAnswer);
        }

        if (update.UpdateTerminationCause)
        {
            record.TerminationCause = update.TerminationCause;
            record.TerminationCausePresent =
                update.TerminationCausePresent;
        }

        if (update.UpdateInitialAuthorizationCompleted)
        {
            record.InitialAuthorizationCompleted =
                update.InitialAuthorizationCompleted;
        }

        if (update.UpdateReauthorizationRequested)
        {
            record.ReauthorizationRequested =
                update.ReauthorizationRequested;
        }

        if (update.UpdateReleaseRequested)
        {
            record.ReleaseRequested = update.ReleaseRequested;
        }

        if (update.UpdateReAuthRequestType)
        {
            record.ReAuthRequestType = update.ReAuthRequestType;
        }

        if (update.UpdateApplicationState)
        {
            record.ApplicationState =
                SessionRecordCloner.CloneSessionBytes(
                    update.ApplicationState);
        }

        record.UpdatedAt = updatedAt;
    }

    internal DiameterMessage? ReadManagerUpdateTrigger(
        DiameterSessionUpdate update,
        DiameterSessionRecord saved)
    {
        if (update.UpdateLastRequest)
        {
            return saved.LastRequest;
        }

        if (update.UpdateLastAnswer)
        {
            return saved.LastAnswer;
        }

        return null;
    }

    internal void ClearManagerSessionTimers(DiameterSessionRecord record)
    {
        record.AuthorizationDeadlinePresent = false;
        record.GraceDeadlinePresent = false;
        record.SessionDeadlinePresent = false;
        record.MultiRoundDeadlinePresent = false;
    }

    internal DiameterSessionSnapshot CreateManagerSessionSnapshot(
        DiameterSessionRecord record)
    {
        DiameterSessionSnapshot result = new DiameterSessionSnapshot();
        result.SessionId = record.SessionId;
        result.Configuration =
            SessionRecordCloner.CloneSessionConfiguration(
                record.Configuration);
        result.State = record.State;
        result.Version = record.Version;
        result.PeerIdentity =
            SessionRecordCloner.CloneOptionalSessionIdentity(
                record.PeerIdentity);
        result.RouteIdentifier = record.RouteIdentifier;
        result.PeerOriginStateIdentifier =
            record.PeerOriginStateIdentifier;
        result.PeerOriginStateIdentifierPresent =
            record.PeerOriginStateIdentifierPresent;
        result.CreatedAt = record.CreatedAt;
        result.UpdatedAt = record.UpdatedAt;
        result.StateChangedAt = record.StateChangedAt;
        result.AuthorizationDeadline = record.AuthorizationDeadline;
        result.AuthorizationDeadlinePresent =
            record.AuthorizationDeadlinePresent;
        result.GraceDeadline = record.GraceDeadline;
        result.GraceDeadlinePresent = record.GraceDeadlinePresent;
        result.SessionDeadline = record.SessionDeadline;
        result.SessionDeadlinePresent = record.SessionDeadlinePresent;
        result.MultiRoundDeadline = record.MultiRoundDeadline;
        result.MultiRoundDeadlinePresent =
            record.MultiRoundDeadlinePresent;
        result.PendingRequestIdentifier =
            record.PendingRequestIdentifier;
        result.LastRequest =
            SessionRecordCloner.CloneOptionalSessionMessage(
                record.LastRequest);
        result.LastAnswer =
            SessionRecordCloner.CloneOptionalSessionMessage(
                record.LastAnswer);
        result.TerminationCause = record.TerminationCause;
        result.TerminationCausePresent =
            record.TerminationCausePresent;
        result.InitialAuthorizationCompleted =
            record.InitialAuthorizationCompleted;
        result.ReauthorizationRequested =
            record.ReauthorizationRequested;
        result.ReleaseRequested = record.ReleaseRequested;
        result.ReAuthRequestType = record.ReAuthRequestType;
        result.ApplicationState = SessionRecordCloner.CloneSessionBytes(
            record.ApplicationState);
        return result;
    }

    internal void PublishManagerSessionEvent(
        DiameterSessionRecord record,
        DiameterSessionState previous,
        DiameterMessage? trigger,
        string code,
        string message)
    {
        if (EventHandler == null)
        {
            return;
        }

        DiameterSessionEvent eventData = new DiameterSessionEvent();
        eventData.Session = CreateManagerSessionSnapshot(record);
        eventData.PreviousState = previous;
        eventData.CurrentState = record.State;
        eventData.PeerIdentity =
            SessionRecordCloner.CloneOptionalSessionIdentity(
                record.PeerIdentity);
        eventData.TriggerMessage =
            SessionRecordCloner.CloneOptionalSessionMessage(trigger);
        eventData.OccurredAt = record.UpdatedAt;
        eventData.Code = code;
        eventData.Message = message;
        EventHandler.HandleDiameterSessionEvent(eventData);
    }
}
