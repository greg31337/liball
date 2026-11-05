using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class SessionTableTests
{
    [Test]
    public void InMemoryStoreVersionsAndDeepOwnsCreatedAndUpdatedRecords()
    {
        InMemorySessionStore concrete = new InMemorySessionStore();
        IDiameterSessionStore store = concrete;
        DateTimeOffset created = CreateSessionTime();
        DiameterSessionRecord supplied = CreateSessionRecord(
            "first.example;1",
            created);

        DiameterSessionRecord first =
            store.SaveDiameterSession(supplied, 0);
        supplied.Configuration.DestinationRealm!.Value = "changed.example";
        supplied.ApplicationState[0] = 99;
        supplied.LastRequest!.Avps[0].Data[0] = 99;
        first.RouteIdentifier = "changed-result";
        first.Configuration.ApplicationState[0] = 88;
        first.LastRequest!.Avps[0].Data[0] = 88;
        DiameterSessionRecord loaded =
            store.LoadDiameterSession("first.example;1")!;
        loaded.State = DiameterSessionState.Open;
        loaded.UpdatedAt = created.AddSeconds(1);
        loaded.StateChangedAt = loaded.UpdatedAt;

        DiameterSessionRecord second =
            store.SaveDiameterSession(loaded, 1);
        second.ApplicationState[0] = 77;
        DiameterSessionRecord retained =
            store.LoadDiameterSession("first.example;1")!;
        store.SaveDiameterSession(
            CreateSessionRecord("second.example;1", created),
            0);
        List<DiameterSessionRecord> all =
            concrete.Table.ReadSessionRecords();
        all[0].Configuration.DestinationRealm!.Value = "list-change.example";

        Assert.Multiple(() =>
        {
            Assert.That(first.Version, Is.EqualTo(1));
            Assert.That(second.Version, Is.EqualTo(2));
            Assert.That(retained.Version, Is.EqualTo(2));
            Assert.That(retained.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(retained.RouteIdentifier,
                Is.EqualTo("route-first.example;1"));
            Assert.That(retained.Configuration.DestinationRealm!.Value,
                Is.EqualTo("remote.example"));
            Assert.That(retained.Configuration.ApplicationState,
                Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(retained.ApplicationState,
                Is.EqualTo(new byte[] { 6, 7 }));
            Assert.That(retained.LastRequest!.Avps[0].Data,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(all.ConvertAll(item => item.SessionId),
                Is.EqualTo(new string[]
                {
                    "first.example;1",
                    "second.example;1"
                }));
            Assert.That(store.LoadDiameterSession("FIRST.EXAMPLE;1"),
                Is.Null);
            Assert.That(concrete.Table.ReadSessionRecord(
                "first.example;1")!.Configuration.DestinationRealm!.Value,
                Is.EqualTo("remote.example"));
        });
    }

    [Test]
    public void OptimisticConflictsCannotOverwriteOrRemoveNewerState()
    {
        InMemorySessionStore concrete = new InMemorySessionStore();
        IDiameterSessionStore store = concrete;
        DateTimeOffset created = CreateSessionTime();
        DiameterSessionRecord original = CreateSessionRecord(
            "client.example;1",
            created);
        DiameterSessionRecord first =
            store.SaveDiameterSession(original, 0);
        DiameterSessionRecord stale =
            store.LoadDiameterSession("client.example;1")!;
        DiameterSessionRecord current =
            store.LoadDiameterSession("client.example;1")!;
        current.UpdatedAt = created.AddSeconds(1);
        current.RouteIdentifier = "new-route";
        DiameterSessionRecord second =
            store.SaveDiameterSession(current, 1);
        stale.UpdatedAt = created.AddSeconds(2);
        stale.RouteIdentifier = "stale-route";

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterSessionException>(() =>
                store.SaveDiameterSession(original, 0));
            Assert.Throws<DiameterSessionException>(() =>
                store.SaveDiameterSession(stale, 1));
            Assert.That(store.RemoveDiameterSession(
                "client.example;1",
                1), Is.False);
            Assert.That(store.LoadDiameterSession(
                "client.example;1")!.RouteIdentifier,
                Is.EqualTo("new-route"));
            Assert.That(second.Version, Is.EqualTo(2));
            Assert.That(store.RemoveDiameterSession(
                "client.example;1",
                2), Is.True);
            Assert.That(store.RemoveDiameterSession(
                "client.example;1",
                2), Is.False);
            Assert.That(concrete.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void AtomicSessionUpdateAppliesEveryRequestedChangeAndClear()
    {
        SessionTable table = new SessionTable();
        DateTimeOffset created = CreateSessionTime();
        table.SaveSessionRecord(
            CreateSessionRecord("client.example;1", created),
            0);
        DiameterSessionUpdate update = new DiameterSessionUpdate();
        update.SessionId = "client.example;1";
        update.ExpectedVersion = 1;
        update.UpdateState = true;
        update.State = DiameterSessionState.Reauthorizing;
        update.UpdatePeerIdentity = true;
        update.PeerIdentity = null;
        update.UpdateRouteIdentifier = true;
        update.RouteIdentifier = "alternate-route";
        update.UpdatePeerOriginStateIdentifier = true;
        update.PeerOriginStateIdentifierPresent = false;
        update.UpdateAuthorizationDeadline = true;
        update.AuthorizationDeadlinePresent = false;
        update.UpdateGraceDeadline = true;
        update.GraceDeadline = created.AddSeconds(30);
        update.GraceDeadlinePresent = true;
        update.UpdateSessionDeadline = true;
        update.SessionDeadline = created.AddMinutes(2);
        update.SessionDeadlinePresent = true;
        update.UpdateMultiRoundDeadline = true;
        update.MultiRoundDeadline = created.AddSeconds(10);
        update.MultiRoundDeadlinePresent = true;
        update.UpdatePendingRequestIdentifier = true;
        update.PendingRequestIdentifier = 0;
        update.UpdateLastRequest = true;
        update.LastRequest = null;
        update.UpdateLastAnswer = true;
        update.LastAnswer = CreateSessionMessage(false);
        update.UpdateTerminationCause = true;
        update.TerminationCausePresent = true;
        update.TerminationCause = DiameterTerminationCause.AuthExpired;
        update.UpdateInitialAuthorizationCompleted = true;
        update.InitialAuthorizationCompleted = true;
        update.UpdateReauthorizationRequested = true;
        update.ReauthorizationRequested = true;
        update.UpdateReleaseRequested = true;
        update.ReleaseRequested = true;
        update.UpdateReAuthRequestType = true;
        update.ReAuthRequestType =
            DiameterReAuthRequestType.AuthorizeAuthenticate;
        update.UpdateApplicationState = true;
        update.ApplicationState = new byte[] { 8, 9 };
        DateTimeOffset changedAt = created.AddSeconds(5);

        DiameterSessionRecord changed = table.ApplySessionUpdate(
            update,
            changedAt);
        update.LastAnswer!.Avps[0].Data[0] = 99;
        update.ApplicationState[0] = 99;
        DiameterSessionRecord retained = table.ReadSessionRecord(
            "client.example;1")!;

        Assert.Multiple(() =>
        {
            Assert.That(changed.Version, Is.EqualTo(2));
            Assert.That(retained.State,
                Is.EqualTo(DiameterSessionState.Reauthorizing));
            Assert.That(retained.StateChangedAt, Is.EqualTo(changedAt));
            Assert.That(retained.UpdatedAt, Is.EqualTo(changedAt));
            Assert.That(retained.PeerIdentity, Is.Null);
            Assert.That(retained.RouteIdentifier,
                Is.EqualTo("alternate-route"));
            Assert.That(retained.PeerOriginStateIdentifierPresent, Is.False);
            Assert.That(retained.ReAuthRequestType,
                Is.EqualTo(
                    DiameterReAuthRequestType.AuthorizeAuthenticate));
            Assert.That(retained.AuthorizationDeadlinePresent, Is.False);
            Assert.That(retained.GraceDeadlinePresent, Is.True);
            Assert.That(retained.SessionDeadlinePresent, Is.True);
            Assert.That(retained.MultiRoundDeadlinePresent, Is.True);
            Assert.That(retained.PendingRequestIdentifier, Is.Zero);
            Assert.That(retained.LastRequest, Is.Null);
            Assert.That(retained.LastAnswer!.Avps[0].Data,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(retained.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.AuthExpired));
            Assert.That(retained.InitialAuthorizationCompleted, Is.True);
            Assert.That(retained.ReauthorizationRequested, Is.True);
            Assert.That(retained.ReleaseRequested, Is.True);
            Assert.That(retained.ApplicationState,
                Is.EqualTo(new byte[] { 8, 9 }));
        });

        DiameterSessionUpdate stale = new DiameterSessionUpdate();
        stale.SessionId = "client.example;1";
        stale.ExpectedVersion = 1;
        stale.UpdateState = true;
        stale.State = DiameterSessionState.Open;
        DiameterSessionUpdate empty = new DiameterSessionUpdate();
        empty.SessionId = "client.example;1";
        empty.ExpectedVersion = 2;

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterSessionException>(() =>
                table.ApplySessionUpdate(stale, changedAt));
            Assert.Throws<ArgumentException>(() =>
                table.ApplySessionUpdate(empty, changedAt));
            Assert.Throws<ArgumentException>(() =>
                table.ApplySessionUpdate(
                    CreateStateUpdate(2),
                    created.AddSeconds(-1)));
        });
    }

    [Test]
    public void ConcurrentSavesAllowExactlyOneWriterForOneVersion()
    {
        InMemorySessionStore store = new InMemorySessionStore();
        DateTimeOffset created = CreateSessionTime();
        store.SaveDiameterSession(
            CreateSessionRecord("client.example;1", created),
            0);
        int workerCount = 8;
        Barrier barrier = new Barrier(workerCount);
        Task[] tasks = new Task[workerCount];
        int successes = 0;
        int conflicts = 0;
        int index = 0;
        while (index < workerCount)
        {
            int worker = index;
            tasks[index] = Task.Run(() =>
            {
                DiameterSessionRecord candidate =
                    store.LoadDiameterSession("client.example;1")!;
                candidate.RouteIdentifier =
                    "worker-" + worker.ToString();
                candidate.UpdatedAt = created.AddSeconds(1);
                barrier.SignalAndWait();
                try
                {
                    store.SaveDiameterSession(candidate, 1);
                    Interlocked.Increment(ref successes);
                }
                catch (DiameterSessionException)
                {
                    Interlocked.Increment(ref conflicts);
                }
            });
            index = index + 1;
        }

        Task.WaitAll(tasks);
        barrier.Dispose();
        DiameterSessionRecord retained =
            store.LoadDiameterSession("client.example;1")!;

        Assert.Multiple(() =>
        {
            Assert.That(successes, Is.EqualTo(1));
            Assert.That(conflicts, Is.EqualTo(workerCount - 1));
            Assert.That(retained.Version, Is.EqualTo(2));
            Assert.That(retained.RouteIdentifier,
                Does.StartWith("worker-"));
            Assert.That(store.Table.ReadSessionRecordCount(), Is.EqualTo(1));
        });
    }

    private static DiameterSessionUpdate CreateStateUpdate(
        long expectedVersion)
    {
        DiameterSessionUpdate result = new DiameterSessionUpdate();
        result.SessionId = "client.example;1";
        result.ExpectedVersion = expectedVersion;
        result.UpdateState = true;
        result.State = DiameterSessionState.Open;
        return result;
    }

    private static DiameterSessionRecord CreateSessionRecord(
        string sessionId,
        DateTimeOffset created)
    {
        DiameterSessionRecord result = new DiameterSessionRecord();
        result.SessionId = sessionId;
        result.Configuration.SessionId = sessionId;
        result.Configuration.ApplicationId = 4;
        result.Configuration.DestinationRealm =
            CreateSessionIdentity("remote.example");
        result.Configuration.DestinationHost =
            CreateSessionIdentity("service.remote.example");
        result.Configuration.PeerIdentity =
            CreateSessionIdentity("peer.example");
        result.Configuration.UserName = "alice@remote.example";
        result.Configuration.CorrelationText = "subscriber-42";
        result.Configuration.ApplicationState = new byte[] { 4, 5 };
        result.State = DiameterSessionState.Pending;
        result.PeerIdentity = CreateSessionIdentity("peer.example");
        result.RouteIdentifier = "route-" + sessionId;
        result.PeerOriginStateIdentifier = 41;
        result.PeerOriginStateIdentifierPresent = true;
        result.CreatedAt = created;
        result.UpdatedAt = created;
        result.StateChangedAt = created;
        result.AuthorizationDeadline = created.AddMinutes(1);
        result.AuthorizationDeadlinePresent = true;
        result.PendingRequestIdentifier = 7;
        result.LastRequest = CreateSessionMessage(true);
        result.ApplicationState = new byte[] { 6, 7 };
        return result;
    }

    private static DiameterMessage CreateSessionMessage(bool request)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = request ?
            DiameterMessageFlags.Request : DiameterMessageFlags.None;
        result.CommandCode = 300;
        result.ApplicationId = 4;
        result.HopByHopIdentifier = 11;
        result.EndToEndIdentifier = 12;
        DiameterAvp avp = new DiameterAvp();
        avp.Code = 999;
        avp.Data = new byte[] { 1, 2, 3 };
        result.Avps.Add(avp);
        return result;
    }

    private static DiameterIdentity CreateSessionIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DateTimeOffset CreateSessionTime()
    {
        return new DateTimeOffset(
            2026,
            8,
            29,
            12,
            0,
            0,
            TimeSpan.Zero);
    }
}
