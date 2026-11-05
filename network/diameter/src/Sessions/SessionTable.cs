using System;
using System.Collections.Generic;

namespace diameter;

internal static class SessionRecordCloner
{
    internal static DiameterSessionRecord CloneSessionRecord(
        DiameterSessionRecord? source)
    {
        if (source == null)
        {
            throw new ArgumentException("A session record is required.");
        }

        DiameterSessionRecord result = new DiameterSessionRecord();
        result.SessionId = source.SessionId;
        result.Configuration = CloneSessionConfiguration(
            source.Configuration);
        result.State = source.State;
        result.Version = source.Version;
        result.PeerIdentity = CloneOptionalSessionIdentity(
            source.PeerIdentity);
        result.RouteIdentifier = source.RouteIdentifier;
        result.PeerOriginStateIdentifier =
            source.PeerOriginStateIdentifier;
        result.PeerOriginStateIdentifierPresent =
            source.PeerOriginStateIdentifierPresent;
        result.CreatedAt = source.CreatedAt;
        result.UpdatedAt = source.UpdatedAt;
        result.StateChangedAt = source.StateChangedAt;
        result.AuthorizationDeadline = source.AuthorizationDeadline;
        result.AuthorizationDeadlinePresent =
            source.AuthorizationDeadlinePresent;
        result.GraceDeadline = source.GraceDeadline;
        result.GraceDeadlinePresent = source.GraceDeadlinePresent;
        result.SessionDeadline = source.SessionDeadline;
        result.SessionDeadlinePresent = source.SessionDeadlinePresent;
        result.MultiRoundDeadline = source.MultiRoundDeadline;
        result.MultiRoundDeadlinePresent = source.MultiRoundDeadlinePresent;
        result.PendingRequestIdentifier = source.PendingRequestIdentifier;
        result.LastRequest = CloneOptionalSessionMessage(source.LastRequest);
        result.LastAnswer = CloneOptionalSessionMessage(source.LastAnswer);
        result.TerminationCause = source.TerminationCause;
        result.TerminationCausePresent = source.TerminationCausePresent;
        result.InitialAuthorizationCompleted =
            source.InitialAuthorizationCompleted;
        result.ReauthorizationRequested = source.ReauthorizationRequested;
        result.ReleaseRequested = source.ReleaseRequested;
        result.ReAuthRequestType = source.ReAuthRequestType;
        result.ApplicationState = CloneSessionBytes(source.ApplicationState);
        return result;
    }

    internal static DiameterSessionConfiguration CloneSessionConfiguration(
        DiameterSessionConfiguration? source)
    {
        if (source == null)
        {
            throw new ArgumentException(
                "A session configuration is required.");
        }

        DiameterSessionConfiguration result =
            new DiameterSessionConfiguration();
        result.SessionId = source.SessionId;
        result.Kind = source.Kind;
        result.Role = source.Role;
        result.ApplicationId = source.ApplicationId;
        result.DestinationRealm = CloneOptionalSessionIdentity(
            source.DestinationRealm);
        result.DestinationHost = CloneOptionalSessionIdentity(
            source.DestinationHost);
        result.PeerIdentity = CloneOptionalSessionIdentity(
            source.PeerIdentity);
        result.UserName = source.UserName;
        result.AuthRequestType = source.AuthRequestType;
        result.AuthSessionState = source.AuthSessionState;
        result.AuthorizationLifetimeMilliseconds =
            source.AuthorizationLifetimeMilliseconds;
        result.AuthGracePeriodMilliseconds =
            source.AuthGracePeriodMilliseconds;
        result.SessionTimeoutMilliseconds =
            source.SessionTimeoutMilliseconds;
        result.MultiRoundTimeoutMilliseconds =
            source.MultiRoundTimeoutMilliseconds;
        result.SessionBinding = source.SessionBinding;
        result.SessionServerFailover = source.SessionServerFailover;
        result.AllowFailover = source.AllowFailover;
        result.CorrelationText = source.CorrelationText;
        result.ApplicationState = CloneSessionBytes(source.ApplicationState);
        return result;
    }

    internal static DiameterIdentity? CloneOptionalSessionIdentity(
        DiameterIdentity? source)
    {
        if (source == null)
        {
            return null;
        }

        DiameterIdentity result = new DiameterIdentity();
        result.Value = source.Value;
        return result;
    }

    internal static DiameterMessage? CloneOptionalSessionMessage(
        DiameterMessage? source)
    {
        if (source == null)
        {
            return null;
        }

        if (source.Avps == null)
        {
            throw new ArgumentException(
                "A session message requires an AVP collection.");
        }

        DiameterMessage result = new DiameterMessage();
        result.Version = source.Version;
        result.Flags = source.Flags;
        result.CommandCode = source.CommandCode;
        result.ApplicationId = source.ApplicationId;
        result.HopByHopIdentifier = source.HopByHopIdentifier;
        result.EndToEndIdentifier = source.EndToEndIdentifier;
        int index = 0;
        while (index < source.Avps.Count)
        {
            DiameterAvp sourceAvp = source.Avps[index];
            if (sourceAvp == null || sourceAvp.Data == null)
            {
                throw new ArgumentException(
                    "Session messages require owned AVP values.");
            }

            DiameterAvp avp = new DiameterAvp();
            avp.Code = sourceAvp.Code;
            avp.Flags = sourceAvp.Flags;
            avp.VendorId = sourceAvp.VendorId;
            avp.Data = CloneSessionBytes(sourceAvp.Data);
            result.Avps.Add(avp);
            index = index + 1;
        }

        return result;
    }

    internal static byte[] CloneSessionBytes(byte[]? source)
    {
        if (source == null)
        {
            throw new ArgumentException(
                "Owned session bytes are required.");
        }

        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }
}

internal class SessionTable
{
    internal object TableLock = new object();
    internal Dictionary<string, DiameterSessionRecord> Records =
        new Dictionary<string, DiameterSessionRecord>(
            StringComparer.Ordinal);
    internal List<string> RecordOrder = new List<string>();

    internal DiameterSessionRecord SaveSessionRecord(
        DiameterSessionRecord? record,
        long expectedVersion)
    {
        if (expectedVersion < 0)
        {
            throw new ArgumentOutOfRangeException("expectedVersion");
        }

        DiameterSessionRecord owned =
            SessionRecordCloner.CloneSessionRecord(record);
        owned.ValidateSessionRecord();
        lock (TableLock)
        {
            return SaveOwnedSessionRecord(owned, expectedVersion);
        }
    }

    internal DiameterSessionRecord? ReadSessionRecord(string? sessionId)
    {
        string key = ValidateSessionRecordIdentifier(sessionId);
        lock (TableLock)
        {
            if (!Records.ContainsKey(key))
            {
                return null;
            }

            return SessionRecordCloner.CloneSessionRecord(Records[key]);
        }
    }

    internal DiameterSessionRecord ApplySessionUpdate(
        DiameterSessionUpdate? update,
        DateTimeOffset updatedAt)
    {
        if (update == null)
        {
            throw new ArgumentException("A session update is required.");
        }

        update.ValidateSessionUpdate();
        if (!ContainsSessionRecordChange(update))
        {
            throw new ArgumentException(
                "A session update must change persisted state.");
        }

        string key = ValidateSessionRecordIdentifier(update.SessionId);
        lock (TableLock)
        {
            if (!Records.ContainsKey(key))
            {
                throw new DiameterSessionException(
                    "The session does not exist.");
            }

            DiameterSessionRecord current = Records[key];
            if (current.Version != update.ExpectedVersion)
            {
                throw new DiameterSessionException(
                    "The session version changed before the update.");
            }

            if (updatedAt < current.UpdatedAt)
            {
                throw new ArgumentException(
                    "A session update cannot move time backward.");
            }

            DiameterSessionRecord changed =
                SessionRecordCloner.CloneSessionRecord(current);
            ApplyOwnedSessionUpdate(changed, update, updatedAt);
            changed.ValidateSessionRecord();
            return SaveOwnedSessionRecord(changed, update.ExpectedVersion);
        }
    }

    internal bool RemoveSessionRecord(
        string? sessionId,
        long expectedVersion)
    {
        if (expectedVersion < 0)
        {
            throw new ArgumentOutOfRangeException("expectedVersion");
        }

        string key = ValidateSessionRecordIdentifier(sessionId);
        lock (TableLock)
        {
            if (!Records.ContainsKey(key) ||
                Records[key].Version != expectedVersion)
            {
                return false;
            }

            Records.Remove(key);
            RecordOrder.Remove(key);
            return true;
        }
    }

    internal List<DiameterSessionRecord> ReadSessionRecords()
    {
        List<DiameterSessionRecord> result =
            new List<DiameterSessionRecord>();
        lock (TableLock)
        {
            int index = 0;
            while (index < RecordOrder.Count)
            {
                string key = RecordOrder[index];
                if (Records.ContainsKey(key))
                {
                    result.Add(SessionRecordCloner.CloneSessionRecord(
                        Records[key]));
                }

                index = index + 1;
            }
        }

        return result;
    }

    internal int ReadSessionRecordCount()
    {
        lock (TableLock)
        {
            return Records.Count;
        }
    }

    internal DiameterSessionRecord SaveOwnedSessionRecord(
        DiameterSessionRecord owned,
        long expectedVersion)
    {
        string key = owned.SessionId;
        if (!Records.ContainsKey(key))
        {
            if (expectedVersion != 0 || owned.Version != 0)
            {
                throw new DiameterSessionException(
                    "The session create version is not current.");
            }

            owned.Version = 1;
            Records[key] = owned;
            RecordOrder.Add(key);
            return SessionRecordCloner.CloneSessionRecord(owned);
        }

        DiameterSessionRecord current = Records[key];
        if (current.Version != expectedVersion ||
            owned.Version != expectedVersion)
        {
            throw new DiameterSessionException(
                "The session version changed before it was saved.");
        }

        if (current.Version == long.MaxValue)
        {
            throw new DiameterSessionException(
                "The session version cannot be advanced.");
        }

        if (owned.CreatedAt != current.CreatedAt ||
            owned.UpdatedAt < current.UpdatedAt)
        {
            throw new DiameterSessionException(
                "Persisted session time cannot move backward.");
        }

        owned.Version = current.Version + 1;
        Records[key] = owned;
        return SessionRecordCloner.CloneSessionRecord(owned);
    }

    internal void ApplyOwnedSessionUpdate(
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
            record.SessionDeadlinePresent = update.SessionDeadlinePresent;
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

    internal bool ContainsSessionRecordChange(DiameterSessionUpdate update)
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

    internal string ValidateSessionRecordIdentifier(string? sessionId)
    {
        if (sessionId == null || sessionId.Length == 0 ||
            ValueCodecs.FindInvalidUtf16Index(sessionId) >= 0)
        {
            throw new ArgumentException("A valid Session-Id is required.");
        }

        return sessionId;
    }
}

internal class InMemorySessionStore : IDiameterSessionStore
{
    internal SessionTable Table = new SessionTable();

    /// <summary>Loads an owned in-memory session record.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <returns>An owned record, or <c>null</c> when absent.</returns>
    public DiameterSessionRecord? LoadDiameterSession(string? sessionId)
    {
        return Table.ReadSessionRecord(sessionId);
    }

    /// <summary>Atomically saves an owned in-memory session record.</summary>
    /// <param name="record">The complete record to save.</param>
    /// <param name="expectedVersion">The version that must still be current.</param>
    /// <returns>The owned saved record with its new version.</returns>
    public DiameterSessionRecord SaveDiameterSession(
        DiameterSessionRecord? record,
        long expectedVersion)
    {
        return Table.SaveSessionRecord(record, expectedVersion);
    }

    /// <summary>Atomically removes a matching in-memory session record.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <param name="expectedVersion">The version that must still be current.</param>
    /// <returns><c>true</c> when the matching record was removed.</returns>
    public bool RemoveDiameterSession(
        string? sessionId,
        long expectedVersion)
    {
        return Table.RemoveSessionRecord(sessionId, expectedVersion);
    }
}
