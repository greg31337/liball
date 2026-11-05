using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class SessionDataTests
{
    [Test]
    public void SessionDataDefaultsAreSafeAndIndependentlyOwned()
    {
        DiameterSessionConfiguration first =
            new DiameterSessionConfiguration();
        DiameterSessionConfiguration second =
            new DiameterSessionConfiguration();
        DiameterSessionRecord record = new DiameterSessionRecord();
        DiameterSessionUpdate update = new DiameterSessionUpdate();
        DiameterSessionSnapshot snapshot = new DiameterSessionSnapshot();
        DiameterSessionEvent eventData = new DiameterSessionEvent();

        first.ApplicationState = new byte[] { 1 };
        record.ApplicationState = new byte[] { 2 };
        snapshot.ApplicationState = new byte[] { 3 };

        Assert.Multiple(() =>
        {
            Assert.That(first.SessionId, Is.Empty);
            Assert.That(first.Kind,
                Is.EqualTo(DiameterSessionKind.StatefulAuthorization));
            Assert.That(first.Role,
                Is.EqualTo(DiameterSessionRole.Client));
            Assert.That(first.AuthRequestType,
                Is.EqualTo(DiameterAuthRequestType.AuthorizeAuthenticate));
            Assert.That(first.AuthSessionState,
                Is.EqualTo(DiameterAuthSessionState.StateMaintained));
            Assert.That(first.MultiRoundTimeoutMilliseconds,
                Is.EqualTo(30000));
            Assert.That(first.AllowFailover, Is.True);
            Assert.That(first.DestinationRealm, Is.Null);
            Assert.That(first.DestinationHost, Is.Null);
            Assert.That(first.PeerIdentity, Is.Null);
            Assert.That(second.ApplicationState, Is.Empty);
            Assert.That(record.Configuration,
                Is.Not.SameAs(snapshot.Configuration));
            Assert.That(record.State,
                Is.EqualTo(DiameterSessionState.Created));
            Assert.That(record.LastRequest, Is.Null);
            Assert.That(record.LastAnswer, Is.Null);
            Assert.That(update.ApplicationState, Is.Empty);
            Assert.That(snapshot.ApplicationState,
                Is.EqualTo(new byte[] { 3 }));
            Assert.That(eventData.Session, Is.Not.SameAs(snapshot));
            Assert.That(eventData.PreviousState,
                Is.EqualTo(DiameterSessionState.Created));
            Assert.That(eventData.CurrentState,
                Is.EqualTo(DiameterSessionState.Created));
        });
    }

    [Test]
    public void SessionConfigurationValidatesLifecycleAndRoutingPolicy()
    {
        DiameterSessionConfiguration client =
            CreateClientConfiguration();
        client.ValidateSessionConfiguration();
        DiameterSessionConfiguration server =
            new DiameterSessionConfiguration();
        server.Role = DiameterSessionRole.Server;
        server.Kind = DiameterSessionKind.StatelessAuthorization;
        server.AuthSessionState =
            DiameterAuthSessionState.NoStateMaintained;
        server.SessionId = "server.example;1";
        server.ValidateSessionConfiguration();

        DiameterSessionConfiguration missingRealm =
            CreateClientConfiguration();
        missingRealm.DestinationRealm = null;
        DiameterSessionConfiguration mismatchedState =
            CreateClientConfiguration();
        mismatchedState.Kind =
            DiameterSessionKind.StatelessAuthorization;
        DiameterSessionConfiguration invalidTimer =
            CreateClientConfiguration();
        invalidTimer.MultiRoundTimeoutMilliseconds = 0;
        DiameterSessionConfiguration invalidFailover =
            CreateClientConfiguration();
        invalidFailover.SessionServerFailover = 4;
        DiameterSessionConfiguration invalidPeer =
            CreateClientConfiguration();
        invalidPeer.PeerIdentity = CreateIdentity("bad peer");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                missingRealm.ValidateSessionConfiguration());
            Assert.Throws<ArgumentException>(() =>
                mismatchedState.ValidateSessionConfiguration());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                invalidTimer.ValidateSessionConfiguration());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                invalidFailover.ValidateSessionConfiguration());
            Assert.Throws<ArgumentException>(() =>
                invalidPeer.ValidateSessionConfiguration());
        });
    }

    [Test]
    public void PersistableSessionRecordValidatesVersionStateAndOwnership()
    {
        DateTimeOffset created = new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);
        DiameterSessionRecord record = CreateSessionRecord(created);

        record.ValidateSessionRecord();

        DiameterSessionRecord mismatched = CreateSessionRecord(created);
        mismatched.Configuration.SessionId = "different.example;1";
        DiameterSessionRecord stale = CreateSessionRecord(created);
        stale.Version = -1;
        DiameterSessionRecord badTime = CreateSessionRecord(created);
        badTime.UpdatedAt = created.AddSeconds(-1);
        DiameterSessionRecord badCause = CreateSessionRecord(created);
        badCause.TerminationCausePresent = true;
        badCause.TerminationCause = (DiameterTerminationCause)99;

        Assert.Multiple(() =>
        {
            Assert.That(record.SessionId,
                Is.EqualTo("client.example;1"));
            Assert.That(record.Version, Is.EqualTo(4));
            Assert.That(record.PeerIdentity!.Value,
                Is.EqualTo("peer.example"));
            Assert.That(record.AuthorizationDeadlinePresent, Is.True);
            Assert.That(record.ApplicationState,
                Is.EqualTo(new byte[] { 7, 8, 9 }));
            Assert.Throws<ArgumentException>(() =>
                mismatched.ValidateSessionRecord());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                stale.ValidateSessionRecord());
            Assert.Throws<ArgumentException>(() =>
                badTime.ValidateSessionRecord());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                badCause.ValidateSessionRecord());
        });
    }

    [Test]
    public void SessionUpdateRepresentsAtomicChangesAndClears()
    {
        DiameterSessionUpdate update = new DiameterSessionUpdate();
        update.SessionId = "client.example;1";
        update.ExpectedVersion = 4;
        update.State = DiameterSessionState.Reauthorizing;
        update.UpdateState = true;
        update.PeerIdentity = CreateIdentity("alternate.example");
        update.UpdatePeerIdentity = true;
        update.UpdateAuthorizationDeadline = true;
        update.AuthorizationDeadlinePresent = false;
        update.UpdatePendingRequestIdentifier = true;
        update.PendingRequestIdentifier = 81;
        update.UpdateTerminationCause = true;
        update.TerminationCausePresent = true;
        update.TerminationCause = DiameterTerminationCause.AuthExpired;
        update.UpdateApplicationState = true;
        update.ApplicationState = new byte[] { 4, 5 };
        update.Code = "session-reauthorizing";
        update.Message = "Re-authorization was requested.";

        update.ValidateSessionUpdate();

        DiameterSessionUpdate invalidVersion = new DiameterSessionUpdate();
        invalidVersion.SessionId = "client.example;1";
        invalidVersion.ExpectedVersion = -1;
        DiameterSessionUpdate invalidState = new DiameterSessionUpdate();
        invalidState.SessionId = "client.example;1";
        invalidState.UpdateState = true;
        invalidState.State = (DiameterSessionState)99;
        DiameterSessionUpdate invalidCause = new DiameterSessionUpdate();
        invalidCause.SessionId = "client.example;1";
        invalidCause.UpdateTerminationCause = true;
        invalidCause.TerminationCausePresent = true;
        invalidCause.TerminationCause = (DiameterTerminationCause)99;

        Assert.Multiple(() =>
        {
            Assert.That(update.ExpectedVersion, Is.EqualTo(4));
            Assert.That(update.UpdateAuthorizationDeadline, Is.True);
            Assert.That(update.AuthorizationDeadlinePresent, Is.False);
            Assert.That(update.PendingRequestIdentifier, Is.EqualTo(81));
            Assert.That(update.ApplicationState,
                Is.EqualTo(new byte[] { 4, 5 }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                invalidVersion.ValidateSessionUpdate());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                invalidState.ValidateSessionUpdate());
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                invalidCause.ValidateSessionUpdate());
        });
    }

    [Test]
    public void SessionConfigurationRejectsEveryEnumAndOwnedValueBranch()
    {
        AssertEveryMutationIsRejected(
            CreateClientConfiguration,
            value => value.ValidateSessionConfiguration(),
            value => value.Kind = (DiameterSessionKind)(-1),
            value => value.Kind = (DiameterSessionKind)2,
            value => value.Role = (DiameterSessionRole)(-1),
            value => value.Role = (DiameterSessionRole)2,
            value => value.AuthRequestType =
                (DiameterAuthRequestType)(-1),
            value => value.AuthRequestType =
                (DiameterAuthRequestType)4,
            value => value.AuthSessionState =
                (DiameterAuthSessionState)(-1),
            value => value.AuthSessionState =
                (DiameterAuthSessionState)2,
            value => value.AuthSessionState =
                DiameterAuthSessionState.NoStateMaintained,
            value => value.SessionId = null!,
            value => value.UserName = null!,
            value => value.CorrelationText = null!,
            value => value.ApplicationState = null!,
            value => value.SessionId = "bad\ud800session",
            value => value.UserName = "bad\ud800user",
            value => value.CorrelationText = "bad\ud800correlation",
            value => value.DestinationRealm!.Value = "bad realm",
            value => value.DestinationHost = CreateIdentity("bad host"));
    }

    [Test]
    public void SessionRecordRejectsEveryOwnedValueAndEnumBranch()
    {
        DateTimeOffset created = DateTimeOffset.FromUnixTimeSeconds(1000);
        Func<DiameterSessionRecord> factory =
            () => CreateSessionRecord(created);

        AssertEveryMutationIsRejected(
            factory,
            value => value.ValidateSessionRecord(),
            value => value.SessionId = null!,
            value => value.SessionId = string.Empty,
            value => value.RouteIdentifier = null!,
            value => value.ApplicationState = null!,
            value => value.Configuration = null!,
            value => value.SessionId = "bad\ud800session",
            value => value.RouteIdentifier = "bad\ud800route",
            value => value.PeerIdentity = CreateIdentity("bad peer"),
            value =>
            {
                value.LastRequest = new DiameterMessage();
                value.LastRequest.Avps = null!;
            },
            value =>
            {
                value.LastAnswer = new DiameterMessage();
                value.LastAnswer.Avps = null!;
            },
            value =>
            {
                value.TerminationCausePresent = true;
                value.TerminationCause = (DiameterTerminationCause)(-1);
            },
            value => value.ReAuthRequestType =
                (DiameterReAuthRequestType)(-1),
            value => value.ReAuthRequestType =
                (DiameterReAuthRequestType)2);

        DiameterSessionRecord emptyConfigurationSession = factory();
        emptyConfigurationSession.Configuration.SessionId = string.Empty;
        Assert.DoesNotThrow(
            emptyConfigurationSession.ValidateSessionRecord);
    }

    [Test]
    public void SessionUpdateRejectsEveryOwnedValueAndConditionalBranch()
    {
        AssertEveryMutationIsRejected(
            CreateValidUpdate,
            value => value.ValidateSessionUpdate(),
            value => value.SessionId = null!,
            value => value.SessionId = string.Empty,
            value => value.RouteIdentifier = null!,
            value => value.ApplicationState = null!,
            value => value.Code = null!,
            value => value.Message = null!,
            value => value.SessionId = "bad\ud800session",
            value => value.RouteIdentifier = "bad\ud800route",
            value => value.Code = "bad\ud800code",
            value => value.Message = "bad\ud800message",
            value => value.PendingRequestIdentifier = -1,
            value =>
            {
                value.UpdateState = true;
                value.State = (DiameterSessionState)(-1);
            },
            value =>
            {
                value.UpdatePeerIdentity = true;
                value.PeerIdentity = CreateIdentity("bad peer");
            },
            value =>
            {
                value.UpdateLastRequest = true;
                value.LastRequest = new DiameterMessage();
                value.LastRequest.Avps = null!;
            },
            value =>
            {
                value.UpdateLastAnswer = true;
                value.LastAnswer = new DiameterMessage();
                value.LastAnswer.Avps = null!;
            },
            value =>
            {
                value.UpdateTerminationCause = true;
                value.TerminationCausePresent = true;
                value.TerminationCause = (DiameterTerminationCause)(-1);
            },
            value =>
            {
                value.UpdateReAuthRequestType = true;
                value.ReAuthRequestType =
                    (DiameterReAuthRequestType)(-1);
            },
            value =>
            {
                value.UpdateReAuthRequestType = true;
                value.ReAuthRequestType =
                    (DiameterReAuthRequestType)2;
            });
    }

    [Test]
    public void SessionContractsCarryCreateUpdateStoreAndEventOutcomes()
    {
        SessionContractProbe probe = new SessionContractProbe();
        DiameterSessionConfiguration configuration =
            CreateClientConfiguration();
        DiameterSessionUpdate update = new DiameterSessionUpdate();
        update.SessionId = "client.example;1";
        update.ExpectedVersion = 1;
        DiameterSessionRecord record = CreateSessionRecord(
            new DateTimeOffset(
                2026,
                8,
                29,
                12,
                0,
                0,
                TimeSpan.Zero));
        DiameterSessionEvent eventData = new DiameterSessionEvent();
        eventData.Session.SessionId = "client.example;1";
        IDiameterSessionManager manager = probe;
        IDiameterSessionStore store = probe;
        ISessionEventHandler handler = probe;

        DiameterSessionSnapshot created =
            manager.CreateDiameterSession(configuration);
        DiameterSessionSnapshot? found =
            manager.FindDiameterSession("client.example;1");
        DiameterSessionSnapshot updated =
            manager.UpdateDiameterSession(update);
        DiameterSessionSnapshot ended = manager.EndDiameterSession(
            "client.example;1",
            DiameterTerminationCause.Logout);
        List<DiameterSessionSnapshot> snapshots =
            manager.ReadDiameterSessionSnapshots();
        DiameterSessionRecord? loaded =
            store.LoadDiameterSession("client.example;1");
        DiameterSessionRecord saved =
            store.SaveDiameterSession(record, 4);
        bool removed = store.RemoveDiameterSession(
            "client.example;1",
            5);
        handler.HandleDiameterSessionEvent(eventData);

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.SameAs(probe.Snapshot));
            Assert.That(found, Is.SameAs(probe.Snapshot));
            Assert.That(updated, Is.SameAs(probe.Snapshot));
            Assert.That(ended, Is.SameAs(probe.Snapshot));
            Assert.That(snapshots, Is.SameAs(probe.Snapshots));
            Assert.That(loaded, Is.SameAs(probe.Record));
            Assert.That(saved, Is.SameAs(probe.Record));
            Assert.That(removed, Is.True);
            Assert.That(probe.Configuration,
                Is.SameAs(configuration));
            Assert.That(probe.Update, Is.SameAs(update));
            Assert.That(probe.SavedRecord, Is.SameAs(record));
            Assert.That(probe.SessionId,
                Is.EqualTo("client.example;1"));
            Assert.That(probe.ExpectedVersion, Is.EqualTo(5));
            Assert.That(probe.Cause,
                Is.EqualTo(DiameterTerminationCause.Logout));
            Assert.That(probe.EventData, Is.SameAs(eventData));
        });
    }

    private static DiameterSessionConfiguration CreateClientConfiguration()
    {
        DiameterSessionConfiguration result =
            new DiameterSessionConfiguration();
        result.SessionId = "client.example;1";
        result.ApplicationId = 4;
        result.DestinationRealm = CreateIdentity("remote.example");
        result.DestinationHost = CreateIdentity("service.remote.example");
        result.PeerIdentity = CreateIdentity("peer.example");
        result.UserName = "alice@remote.example";
        result.AuthorizationLifetimeMilliseconds = 60000;
        result.AuthGracePeriodMilliseconds = 5000;
        result.SessionTimeoutMilliseconds = 120000;
        result.SessionBinding = 1;
        result.SessionServerFailover = 1;
        result.CorrelationText = "subscriber-42";
        result.ApplicationState = new byte[] { 1, 2, 3 };
        return result;
    }

    private static DiameterSessionRecord CreateSessionRecord(
        DateTimeOffset created)
    {
        DiameterSessionRecord result = new DiameterSessionRecord();
        result.SessionId = "client.example;1";
        result.Configuration = CreateClientConfiguration();
        result.State = DiameterSessionState.Open;
        result.Version = 4;
        result.PeerIdentity = CreateIdentity("peer.example");
        result.RouteIdentifier = "remote-route";
        result.PeerOriginStateIdentifier = 91;
        result.PeerOriginStateIdentifierPresent = true;
        result.CreatedAt = created;
        result.UpdatedAt = created.AddSeconds(2);
        result.StateChangedAt = created.AddSeconds(1);
        result.AuthorizationDeadline = created.AddMinutes(1);
        result.AuthorizationDeadlinePresent = true;
        result.SessionDeadline = created.AddMinutes(2);
        result.SessionDeadlinePresent = true;
        result.PendingRequestIdentifier = 80;
        result.LastRequest = new DiameterMessage();
        result.LastRequest.Flags = DiameterMessageFlags.Request;
        result.LastAnswer = new DiameterMessage();
        result.InitialAuthorizationCompleted = true;
        result.ApplicationState = new byte[] { 7, 8, 9 };
        return result;
    }

    private static DiameterSessionUpdate CreateValidUpdate()
    {
        DiameterSessionUpdate result = new DiameterSessionUpdate();
        result.SessionId = "client.example;1";
        result.Code = "session-updated";
        result.Message = "The session was updated.";
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static void AssertEveryMutationIsRejected<T>(
        Func<T> factory,
        Action<T> validate,
        params Action<T>[] mutations)
    {
        int index = 0;
        while (index < mutations.Length)
        {
            T value = factory();
            mutations[index](value);
            Assert.That(
                new TestDelegate(() => validate(value)),
                Throws.InstanceOf<ArgumentException>(),
                $"Mutation {index} was accepted.");
            index = index + 1;
        }
    }
}

internal class SessionContractProbe :
    IDiameterSessionManager,
    IDiameterSessionStore,
    ISessionEventHandler
{
    internal DiameterSessionSnapshot Snapshot =
        new DiameterSessionSnapshot();
    internal List<DiameterSessionSnapshot> Snapshots =
        new List<DiameterSessionSnapshot>();
    internal DiameterSessionRecord Record = new DiameterSessionRecord();
    internal DiameterSessionConfiguration? Configuration;
    internal DiameterSessionUpdate? Update;
    internal DiameterSessionRecord? SavedRecord;
    internal DiameterSessionEvent? EventData;
    internal string SessionId = string.Empty;
    internal long ExpectedVersion;
    internal DiameterTerminationCause Cause;

    public DiameterSessionSnapshot CreateDiameterSession(
        DiameterSessionConfiguration? configuration)
    {
        Configuration = configuration;
        return Snapshot;
    }

    public DiameterSessionSnapshot? FindDiameterSession(string? sessionId)
    {
        SessionId = sessionId ?? string.Empty;
        return Snapshot;
    }

    public DiameterSessionSnapshot UpdateDiameterSession(
        DiameterSessionUpdate? update)
    {
        Update = update;
        return Snapshot;
    }

    public DiameterSessionSnapshot EndDiameterSession(
        string? sessionId,
        DiameterTerminationCause cause)
    {
        SessionId = sessionId ?? string.Empty;
        Cause = cause;
        return Snapshot;
    }

    public List<DiameterSessionSnapshot> ReadDiameterSessionSnapshots()
    {
        return Snapshots;
    }

    public DiameterSessionRecord? LoadDiameterSession(string? sessionId)
    {
        SessionId = sessionId ?? string.Empty;
        return Record;
    }

    public DiameterSessionRecord SaveDiameterSession(
        DiameterSessionRecord? record,
        long expectedVersion)
    {
        SavedRecord = record;
        ExpectedVersion = expectedVersion;
        return Record;
    }

    public bool RemoveDiameterSession(
        string? sessionId,
        long expectedVersion)
    {
        SessionId = sessionId ?? string.Empty;
        ExpectedVersion = expectedVersion;
        return true;
    }

    public void HandleDiameterSessionEvent(DiameterSessionEvent eventData)
    {
        EventData = eventData;
    }
}
