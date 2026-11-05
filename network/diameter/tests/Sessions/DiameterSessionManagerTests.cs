using System.Text;

namespace diameter.Tests;

public class DiameterSessionManagerTests
{
    [Test]
    public void PublicInMemoryStoreOwnsVersionsAndRemoval()
    {
        DiameterInMemorySessionStore concrete =
            new DiameterInMemorySessionStore();
        IDiameterSessionStore store = concrete;
        DateTimeOffset created = CreateManagerTime();
        DiameterSessionRecord supplied = CreateManagerRecord(
            "manager.example;store",
            created);

        DiameterSessionRecord first = store.SaveDiameterSession(
            supplied,
            0);
        supplied.ApplicationState[0] = 99;
        first.RouteIdentifier = "mutated-result";
        DiameterSessionRecord loaded = store.LoadDiameterSession(
            supplied.SessionId)!;
        loaded.RouteIdentifier = "updated-route";
        loaded.UpdatedAt = created.AddSeconds(1);
        DiameterSessionRecord second = store.SaveDiameterSession(
            loaded,
            loaded.Version);
        second.RouteIdentifier = "mutated-second";

        Assert.Multiple(() =>
        {
            Assert.That(first.Version, Is.EqualTo(1));
            Assert.That(second.Version, Is.EqualTo(2));
            Assert.That(store.LoadDiameterSession(
                supplied.SessionId)!.ApplicationState,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(store.LoadDiameterSession(
                supplied.SessionId)!.RouteIdentifier,
                Is.EqualTo("updated-route"));
            Assert.That(
                () => store.SaveDiameterSession(loaded, 1),
                Throws.TypeOf<DiameterSessionException>());
            Assert.That(store.RemoveDiameterSession(
                supplied.SessionId,
                1), Is.False);
            Assert.That(store.RemoveDiameterSession(
                supplied.SessionId,
                2), Is.True);
            Assert.That(store.LoadDiameterSession(supplied.SessionId),
                Is.Null);
            Assert.That(concrete.Table.ReadSessionRecordCount(), Is.Zero);
        });
    }

    [Test]
    public void CreateGeneratesOwnedSessionAndPublishesOwnedEvent()
    {
        ManagerTestContext context = CreateManagerContext(true);
        context.Handler!.MutateEvents = true;
        DiameterSessionConfiguration configuration =
            CreateManagerConfiguration(string.Empty, "first");

        DiameterSessionSnapshot created =
            context.Manager.CreateDiameterSession(configuration);
        created.ApplicationState[0] = 88;
        created.Configuration.ApplicationState[0] = 77;
        DiameterSessionSnapshot retained =
            context.Manager.FindDiameterSession(created.SessionId)!;

        Assert.Multiple(() =>
        {
            Assert.That(configuration.SessionId, Is.Empty);
            Assert.That(created.SessionId,
                Is.EqualTo("origin.example;generated;first"));
            Assert.That(created.State,
                Is.EqualTo(DiameterSessionState.Created));
            Assert.That(created.Version, Is.EqualTo(1));
            Assert.That(created.CreatedAt, Is.EqualTo(context.Clock.UtcTime));
            Assert.That(retained.ApplicationState,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(retained.Configuration.ApplicationState,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(retained.RouteIdentifier, Is.Empty);
            Assert.That(context.Identifiers.OriginHost,
                Is.EqualTo("origin.example"));
            Assert.That(context.Identifiers.CorrelationText,
                Is.EqualTo("first"));
            Assert.That(context.Handler.Events.Count, Is.EqualTo(1));
            Assert.That(context.Handler.ObservedCodes,
                Is.EqualTo(new[] { "session.created" }));
            Assert.That(context.Handler.ObservedStates,
                Is.EqualTo(new[] { DiameterSessionState.Created }));
            Assert.That(context.Handler.ObservedSessionIds,
                Is.EqualTo(new[] { created.SessionId }));
        });
    }

    [Test]
    public void UpdateAppliesOwnedFieldsAndRejectsStaleVersion()
    {
        ManagerTestContext context = CreateManagerContext(true);
        DiameterSessionSnapshot created =
            context.Manager.CreateDiameterSession(
                CreateManagerConfiguration(
                    "manager.example;update",
                    "update"));
        context.Clock.AdvanceDiscoveryTime(2);
        DiameterSessionUpdate update = new DiameterSessionUpdate();
        update.SessionId = created.SessionId;
        update.ExpectedVersion = created.Version;
        update.UpdateState = true;
        update.State = DiameterSessionState.Open;
        update.UpdateRouteIdentifier = true;
        update.RouteIdentifier = "primary-route";
        update.UpdatePeerIdentity = true;
        update.PeerIdentity = CreateManagerIdentity("peer.example");
        update.UpdatePeerOriginStateIdentifier = true;
        update.PeerOriginStateIdentifier = 42;
        update.PeerOriginStateIdentifierPresent = true;
        update.UpdateSessionDeadline = true;
        update.SessionDeadline = context.Clock.UtcTime.AddMinutes(1);
        update.SessionDeadlinePresent = true;
        update.UpdateLastRequest = true;
        update.LastRequest = CreateManagerMessage(created.SessionId);
        update.UpdateInitialAuthorizationCompleted = true;
        update.InitialAuthorizationCompleted = true;
        update.UpdateApplicationState = true;
        update.ApplicationState = new byte[] { 9, 10 };
        update.Code = "session.authorized";
        update.Message = "Authorization completed.";

        DiameterSessionSnapshot changed =
            context.Manager.UpdateDiameterSession(update);
        update.RouteIdentifier = "mutated-route";
        update.PeerIdentity!.Value = "mutated.example";
        update.ApplicationState[0] = 99;
        update.LastRequest!.Avps[0].Data[0] = 99;
        DiameterSessionSnapshot retained =
            context.Manager.FindDiameterSession(created.SessionId)!;

        DiameterSessionUpdate stale = new DiameterSessionUpdate();
        stale.SessionId = created.SessionId;
        stale.ExpectedVersion = 1;
        stale.UpdateState = true;
        stale.State = DiameterSessionState.Failed;

        Assert.Multiple(() =>
        {
            Assert.That(changed.Version, Is.EqualTo(2));
            Assert.That(changed.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(retained.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(retained.RouteIdentifier,
                Is.EqualTo("primary-route"));
            Assert.That(retained.PeerIdentity!.Value,
                Is.EqualTo("peer.example"));
            Assert.That(retained.PeerOriginStateIdentifier,
                Is.EqualTo(42));
            Assert.That(retained.SessionDeadlinePresent, Is.True);
            Assert.That(retained.InitialAuthorizationCompleted, Is.True);
            Assert.That(retained.ApplicationState,
                Is.EqualTo(new byte[] { 9, 10 }));
            Assert.That(retained.LastRequest!.Avps[0].Data,
                Is.EqualTo(Encoding.UTF8.GetBytes(created.SessionId)));
            Assert.That(context.Handler!.ObservedCodes,
                Is.EqualTo(new[]
                {
                    "session.created",
                    "session.authorized"
                }));
            Assert.That(context.Handler.Events[1].PreviousState,
                Is.EqualTo(DiameterSessionState.Created));
            Assert.That(context.Handler.Events[1].CurrentState,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(context.Handler.Events[1].TriggerMessage,
                Is.Not.Null);
            Assert.That(
                () => context.Manager.UpdateDiameterSession(stale),
                Throws.TypeOf<DiameterSessionException>());
        });
    }

    [Test]
    public void EndIsTerminalIdempotentAndClearsPendingState()
    {
        ManagerTestContext context = CreateManagerContext(true);
        DiameterSessionSnapshot created =
            context.Manager.CreateDiameterSession(
                CreateManagerConfiguration(
                    "manager.example;end",
                    "end"));
        context.Clock.AdvanceDiscoveryTime(1);
        DiameterSessionUpdate update = new DiameterSessionUpdate();
        update.SessionId = created.SessionId;
        update.ExpectedVersion = created.Version;
        update.UpdateState = true;
        update.State = DiameterSessionState.Open;
        update.UpdatePendingRequestIdentifier = true;
        update.PendingRequestIdentifier = 17;
        update.UpdateAuthorizationDeadline = true;
        update.AuthorizationDeadline = context.Clock.UtcTime.AddSeconds(30);
        update.AuthorizationDeadlinePresent = true;
        DiameterSessionSnapshot opened =
            context.Manager.UpdateDiameterSession(update);
        context.Clock.AdvanceDiscoveryTime(1);

        DiameterSessionSnapshot ended =
            context.Manager.EndDiameterSession(
                created.SessionId,
                DiameterTerminationCause.Logout);
        DiameterSessionSnapshot repeated =
            context.Manager.EndDiameterSession(
                created.SessionId,
                DiameterTerminationCause.Administrative);

        Assert.Multiple(() =>
        {
            Assert.That(ended.State,
                Is.EqualTo(DiameterSessionState.Closed));
            Assert.That(ended.Version, Is.EqualTo(opened.Version + 1));
            Assert.That(ended.PendingRequestIdentifier, Is.Zero);
            Assert.That(ended.AuthorizationDeadlinePresent, Is.False);
            Assert.That(ended.ReleaseRequested, Is.True);
            Assert.That(ended.TerminationCausePresent, Is.True);
            Assert.That(ended.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.Logout));
            Assert.That(repeated.Version, Is.EqualTo(ended.Version));
            Assert.That(repeated.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.Logout));
            Assert.That(context.Handler!.Events.Count, Is.EqualTo(3));
            Assert.That(context.Handler.Events[2].PreviousState,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(context.Handler.Events[2].CurrentState,
                Is.EqualTo(DiameterSessionState.Closed));
            Assert.That(context.Handler.ObservedCodes[2],
                Is.EqualTo("session.ended"));
        });
    }

    [Test]
    public void SnapshotOrderIsStableAndSkipsExternallyRemovedRecords()
    {
        ManagerTestContext context = CreateManagerContext(false);
        DiameterSessionSnapshot first =
            context.Manager.CreateDiameterSession(
                CreateManagerConfiguration(
                    "manager.example;first",
                    "first"));
        DiameterSessionSnapshot second =
            context.Manager.CreateDiameterSession(
                CreateManagerConfiguration(
                    "manager.example;second",
                    "second"));
        List<DiameterSessionSnapshot> before =
            context.Manager.ReadDiameterSessionSnapshots();
        before[0].SessionId = "mutated";
        context.Store.RemoveDiameterSession(first.SessionId, first.Version);

        List<DiameterSessionSnapshot> after =
            context.Manager.ReadDiameterSessionSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(before.ConvertAll(item => item.SessionId),
                Is.EqualTo(new[]
                {
                    "mutated",
                    "manager.example;second"
                }));
            Assert.That(after.ConvertAll(item => item.SessionId),
                Is.EqualTo(new[] { "manager.example;second" }));
            Assert.That(context.Manager.FindDiameterSession(
                second.SessionId)!.SessionId,
                Is.EqualTo(second.SessionId));
        });
    }

    [Test]
    public void ManagerRejectsEveryMissingDependencyAndOperationInput()
    {
        DiameterIdentity origin = CreateManagerIdentity("origin.example");
        DiameterInMemorySessionStore store =
            new DiameterInMemorySessionStore();
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        ManagerIdentifierSource identifiers =
            new ManagerIdentifierSource();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new DiameterSessionManager(
                    null, store, null, clock, identifiers));
            Assert.Throws<ArgumentException>(() =>
                new DiameterSessionManager(
                    origin, null, null, clock, identifiers));
            Assert.Throws<ArgumentException>(() =>
                new DiameterSessionManager(
                    origin, store, null, null, identifiers));
            Assert.Throws<ArgumentException>(() =>
                new DiameterSessionManager(
                    origin, store, null, clock, null));
        });

        DiameterSessionManager manager = new DiameterSessionManager(
            origin,
            store,
            null,
            clock,
            identifiers);
        DiameterSessionUpdate unchanged = new DiameterSessionUpdate();
        unchanged.SessionId = "missing.example;1";
        DiameterSessionUpdate missing = new DiameterSessionUpdate();
        missing.SessionId = "missing.example;1";
        missing.UpdateState = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                manager.CreateDiameterSession(null));
            Assert.That(manager.FindDiameterSession("missing.example;1"),
                Is.Null);
            Assert.Throws<ArgumentException>(() =>
                manager.UpdateDiameterSession(null));
            Assert.Throws<ArgumentException>(() =>
                manager.UpdateDiameterSession(unchanged));
            Assert.Throws<DiameterSessionException>(() =>
                manager.UpdateDiameterSession(missing));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                manager.EndDiameterSession(
                    "missing.example;1",
                    (DiameterTerminationCause)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                manager.EndDiameterSession(
                    "missing.example;1",
                    (DiameterTerminationCause)9));
            Assert.Throws<DiameterSessionException>(() =>
                manager.EndDiameterSession(
                    "missing.example;1",
                    DiameterTerminationCause.Logout));
        });
    }

    [Test]
    public void ManagerRejectsBackwardUpdateAndTerminationTimes()
    {
        ManagerTestContext updateContext = CreateManagerContext(false);
        DiameterSessionSnapshot updateCreated =
            updateContext.Manager.CreateDiameterSession(
                CreateManagerConfiguration(
                    "manager.example;backward-update",
                    "backward-update"));
        updateContext.Clock.UtcTime =
            updateCreated.UpdatedAt.AddSeconds(-1);
        DiameterSessionUpdate update = new DiameterSessionUpdate();
        update.SessionId = updateCreated.SessionId;
        update.ExpectedVersion = updateCreated.Version;
        update.UpdateState = true;
        update.State = DiameterSessionState.Open;

        ManagerTestContext endContext = CreateManagerContext(false);
        DiameterSessionSnapshot endCreated =
            endContext.Manager.CreateDiameterSession(
                CreateManagerConfiguration(
                    "manager.example;backward-end",
                    "backward-end"));
        endContext.Clock.UtcTime = endCreated.UpdatedAt.AddSeconds(-1);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                updateContext.Manager.UpdateDiameterSession(update));
            Assert.Throws<ArgumentException>(() =>
                endContext.Manager.EndDiameterSession(
                    endCreated.SessionId,
                    DiameterTerminationCause.Logout));
        });
    }

    [Test]
    public void EverySessionUpdateFlagIsRecognizedAndApplied()
    {
        ManagerTestContext context = CreateManagerContext(false);
        List<Action<DiameterSessionUpdate>> flags =
            new List<Action<DiameterSessionUpdate>>
            {
                value => value.UpdateState = true,
                value => value.UpdatePeerIdentity = true,
                value => value.UpdateRouteIdentifier = true,
                value => value.UpdatePeerOriginStateIdentifier = true,
                value => value.UpdateAuthorizationDeadline = true,
                value => value.UpdateGraceDeadline = true,
                value => value.UpdateSessionDeadline = true,
                value => value.UpdateMultiRoundDeadline = true,
                value => value.UpdatePendingRequestIdentifier = true,
                value => value.UpdateLastRequest = true,
                value => value.UpdateLastAnswer = true,
                value => value.UpdateTerminationCause = true,
                value => value.UpdateInitialAuthorizationCompleted = true,
                value => value.UpdateReauthorizationRequested = true,
                value => value.UpdateReleaseRequested = true,
                value => value.UpdateReAuthRequestType = true,
                value => value.UpdateApplicationState = true
            };
        int index = 0;
        while (index < flags.Count)
        {
            DiameterSessionUpdate single = new DiameterSessionUpdate();
            flags[index](single);
            Assert.That(
                context.Manager.ContainsManagerSessionChange(single),
                Is.True,
                $"Update flag {index} was not recognized.");
            index = index + 1;
        }

        Assert.That(
            context.Manager.ContainsManagerSessionChange(
                new DiameterSessionUpdate()),
            Is.False);

        DateTimeOffset now = CreateManagerTime().AddSeconds(10);
        DiameterSessionRecord record = CreateManagerRecord(
            "manager.example;all-updates",
            CreateManagerTime());
        DiameterSessionUpdate update = new DiameterSessionUpdate();
        update.UpdateGraceDeadline = true;
        update.GraceDeadlinePresent = true;
        update.GraceDeadline = now.AddMinutes(1);
        update.UpdateMultiRoundDeadline = true;
        update.MultiRoundDeadlinePresent = true;
        update.MultiRoundDeadline = now.AddMinutes(2);
        update.UpdateLastAnswer = true;
        update.LastAnswer = CreateManagerMessage(record.SessionId);
        update.LastAnswer.Flags = DiameterMessageFlags.None;
        update.UpdateTerminationCause = true;
        update.TerminationCausePresent = true;
        update.TerminationCause = DiameterTerminationCause.AuthExpired;
        update.UpdateReauthorizationRequested = true;
        update.ReauthorizationRequested = true;
        update.UpdateReleaseRequested = true;
        update.ReleaseRequested = true;
        update.UpdateReAuthRequestType = true;
        update.ReAuthRequestType =
            DiameterReAuthRequestType.AuthorizeAuthenticate;

        context.Manager.ApplyManagerSessionUpdate(record, update, now);

        Assert.Multiple(() =>
        {
            Assert.That(record.GraceDeadlinePresent, Is.True);
            Assert.That(record.MultiRoundDeadlinePresent, Is.True);
            Assert.That(record.LastAnswer, Is.Not.Null);
            Assert.That(record.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.AuthExpired));
            Assert.That(record.ReauthorizationRequested, Is.True);
            Assert.That(record.ReleaseRequested, Is.True);
            Assert.That(record.ReAuthRequestType,
                Is.EqualTo(
                    DiameterReAuthRequestType.AuthorizeAuthenticate));
            Assert.That(
                context.Manager.ReadManagerUpdateTrigger(update, record),
                Is.SameAs(record.LastAnswer));
        });
    }

    private static ManagerTestContext CreateManagerContext(bool withHandler)
    {
        ManagerTestContext result = new ManagerTestContext();
        result.Clock = new FakeDiscoveryClock();
        result.Store = new DiameterInMemorySessionStore();
        result.Identifiers = new ManagerIdentifierSource();
        if (withHandler)
        {
            result.Handler = new ManagerSessionEventHandler();
        }

        result.Manager = new DiameterSessionManager(
            CreateManagerIdentity("origin.example"),
            result.Store,
            result.Handler,
            result.Clock,
            result.Identifiers);
        return result;
    }

    private static DiameterSessionConfiguration CreateManagerConfiguration(
        string sessionId,
        string correlation)
    {
        DiameterSessionConfiguration result =
            new DiameterSessionConfiguration();
        result.SessionId = sessionId;
        result.ApplicationId = 4;
        result.DestinationRealm =
            CreateManagerIdentity("remote.example");
        result.PeerIdentity = CreateManagerIdentity("peer.example");
        result.UserName = "alice@remote.example";
        result.CorrelationText = correlation;
        result.ApplicationState = new byte[] { 1, 2 };
        return result;
    }

    private static DiameterSessionRecord CreateManagerRecord(
        string sessionId,
        DateTimeOffset created)
    {
        DiameterSessionRecord result = new DiameterSessionRecord();
        result.SessionId = sessionId;
        result.Configuration = CreateManagerConfiguration(
            sessionId,
            "store");
        result.CreatedAt = created;
        result.UpdatedAt = created;
        result.StateChangedAt = created;
        result.ApplicationState = new byte[] { 1, 2 };
        return result;
    }

    private static DiameterMessage CreateManagerMessage(string sessionId)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request;
        result.CommandCode = 300;
        result.ApplicationId = 4;
        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.SessionId;
        avp.Data = Encoding.UTF8.GetBytes(sessionId);
        result.Avps.Add(avp);
        return result;
    }

    private static DiameterIdentity CreateManagerIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }

    private static DateTimeOffset CreateManagerTime()
    {
        return new DateTimeOffset(
            2026,
            8,
            29,
            18,
            0,
            0,
            TimeSpan.Zero);
    }
}

internal class ManagerTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal DiameterInMemorySessionStore Store =
        new DiameterInMemorySessionStore();
    internal ManagerIdentifierSource Identifiers =
        new ManagerIdentifierSource();
    internal ManagerSessionEventHandler? Handler;
    internal DiameterSessionManager Manager = null!;
}

internal class ManagerIdentifierSource : IDiameterIdentifierSource
{
    internal string OriginHost = string.Empty;
    internal string CorrelationText = string.Empty;

    public uint CreateHopByHopIdentifier()
    {
        return 1;
    }

    public uint CreateEndToEndIdentifier()
    {
        return 2;
    }

    public string CreateSessionIdentifier(
        DiameterIdentity originHost,
        string? correlationText)
    {
        OriginHost = originHost.Value;
        CorrelationText = correlationText ?? string.Empty;
        return originHost.Value + ";generated;" + CorrelationText;
    }

    public uint CreateOriginStateIdentifier()
    {
        return 3;
    }
}

internal class ManagerSessionEventHandler : ISessionEventHandler
{
    internal bool MutateEvents;
    internal List<DiameterSessionEvent> Events =
        new List<DiameterSessionEvent>();
    internal List<string> ObservedCodes = new List<string>();
    internal List<string> ObservedSessionIds = new List<string>();
    internal List<DiameterSessionState> ObservedStates =
        new List<DiameterSessionState>();

    public void HandleDiameterSessionEvent(DiameterSessionEvent eventData)
    {
        ObservedCodes.Add(eventData.Code);
        ObservedSessionIds.Add(eventData.Session.SessionId);
        ObservedStates.Add(eventData.CurrentState);
        Events.Add(eventData);
        if (MutateEvents)
        {
            eventData.Session.RouteIdentifier = "event-mutated";
            eventData.Session.ApplicationState[0] = 99;
        }
    }
}
