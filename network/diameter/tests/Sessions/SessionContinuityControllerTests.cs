using System.Text;

namespace diameter.Tests;

public class SessionContinuityControllerTests
{
    [Test]
    public void AuthorizationPolicyRetainsBindingFailoverOriginAndAffinity()
    {
        SessionContinuityTestContext context =
            CreateContinuityContext();
        DiameterSessionRecord record = AddContinuitySession(
            context,
            "continuity.example;1",
            DiameterSessionRole.Client,
            DiameterSessionState.Open,
            0,
            true,
            "old-route",
            "old-peer.example",
            0);
        DiameterIdentity peer =
            CreateContinuityIdentity("server.example");
        DiameterMessage answer = CreateContinuityAnswer(
            record.SessionId,
            10);
        AddContinuityUnsigned32(
            answer,
            DiameterAvpCodes.SessionBinding,
            5);
        AddContinuityUnsigned32(
            answer,
            DiameterAvpCodes.SessionServerFailover,
            3);
        AddContinuityIdentity(
            answer,
            DiameterAvpCodes.OriginHost,
            peer.Value);
        AddContinuityUnsigned32(
            answer,
            DiameterAvpCodes.OriginStateId,
            100);

        SessionContinuityResult result =
            context.Controller.ApplySessionAuthorizationPolicy(
                record.SessionId,
                answer,
                "primary-route",
                peer);
        peer.Value = "mutated.example";
        DiameterSessionRecord retained = context.Table.ReadSessionRecord(
            record.SessionId)!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Code,
                Is.EqualTo("session-continuity-policy-applied"));
            Assert.That(result.Session!.Configuration.SessionBinding,
                Is.EqualTo(5));
            Assert.That(
                result.Session.Configuration.SessionServerFailover,
                Is.EqualTo(3));
            Assert.That(result.Session.PeerOriginStateIdentifier,
                Is.EqualTo(100));
            Assert.That(
                result.Session.PeerOriginStateIdentifierPresent,
                Is.True);
            Assert.That(retained.PeerIdentity!.Value,
                Is.EqualTo("server.example"));
            Assert.That(retained.RouteIdentifier,
                Is.EqualTo("primary-route"));
            Assert.That(context.Affinities.ReadAffinityRoute(
                record.SessionId), Is.EqualTo("primary-route"));
        });
    }

    [Test]
    public void SessionBindingControlsDestinationHostForEachRequestKind()
    {
        SessionContinuityTestContext context =
            CreateContinuityContext();
        DiameterSessionRecord record = AddContinuitySession(
            context,
            "continuity.example;2",
            DiameterSessionRole.Client,
            DiameterSessionState.Open,
            5,
            true,
            "primary-route",
            "server.example",
            0);
        DiameterMessage reauth = CreateContinuityRequest(
            record.SessionId,
            300,
            20);
        AddContinuityIdentity(
            reauth,
            DiameterAvpCodes.DestinationHost,
            "stale.example");
        DiameterMessage termination = CreateContinuityRequest(
            record.SessionId,
            DiameterCommandCodes.SessionTermination,
            21);
        AddContinuityIdentity(
            termination,
            DiameterAvpCodes.DestinationHost,
            "stale.example");
        DiameterMessage accounting = CreateContinuityRequest(
            record.SessionId,
            DiameterCommandCodes.Accounting,
            22);
        AddContinuityIdentity(
            accounting,
            DiameterAvpCodes.DestinationHost,
            "stale.example");

        SessionContinuityResult preparedReauth =
            context.Controller.PrepareSessionReAuthRequest(
                record.SessionId,
                reauth);
        SessionContinuityResult preparedTermination =
            context.Controller.PrepareSessionTerminationRequest(
                record.SessionId,
                termination);
        SessionContinuityResult preparedAccounting =
            context.Controller.PrepareSessionAccountingRequest(
                record.SessionId,
                accounting);

        Assert.Multiple(() =>
        {
            Assert.That(CountContinuityAvps(
                preparedReauth.Message!,
                DiameterAvpCodes.DestinationHost), Is.Zero);
            Assert.That(ReadContinuityIdentity(
                preparedTermination.Message!,
                DiameterAvpCodes.DestinationHost),
                Is.EqualTo("server.example"));
            Assert.That(CountContinuityAvps(
                preparedAccounting.Message!,
                DiameterAvpCodes.DestinationHost), Is.Zero);
            Assert.That(ReadContinuityIdentity(
                termination,
                DiameterAvpCodes.DestinationHost),
                Is.EqualTo("stale.example"));
            Assert.That(preparedReauth.Changed, Is.False);
            Assert.That(preparedTermination.Changed, Is.False);
            Assert.That(preparedAccounting.Changed, Is.False);
        });
    }

    [Test]
    public void ReAuthFailurePoliciesRefuseRetryAllowAndRetryThenAllow()
    {
        SessionContinuityTestContext refusedContext =
            CreateContinuityContext();
        DiameterSessionRecord refusedRecord = AddPendingContinuitySession(
            refusedContext,
            "continuity.example;3",
            DiameterSessionState.Reauthorizing,
            0);
        SessionContinuityResult refused = refusedContext.Controller.
            HandleSessionReAuthDeliveryFailure(
                refusedRecord.SessionId,
                false);

        SessionContinuityTestContext retryContext =
            CreateContinuityContext();
        DiameterSessionRecord retryRecord = AddPendingContinuitySession(
            retryContext,
            "continuity.example;4",
            DiameterSessionState.Reauthorizing,
            1);
        SessionContinuityResult retry = retryContext.Controller.
            HandleSessionReAuthDeliveryFailure(
                retryRecord.SessionId,
                false);

        SessionContinuityTestContext allowContext =
            CreateContinuityContext();
        DiameterSessionRecord allowRecord = AddPendingContinuitySession(
            allowContext,
            "continuity.example;5",
            DiameterSessionState.Reauthorizing,
            2);
        SessionContinuityResult allowed = allowContext.Controller.
            HandleSessionReAuthDeliveryFailure(
                allowRecord.SessionId,
                false);

        SessionContinuityTestContext combinedContext =
            CreateContinuityContext();
        DiameterSessionRecord combinedRecord = AddPendingContinuitySession(
            combinedContext,
            "continuity.example;6",
            DiameterSessionState.Reauthorizing,
            3);
        SessionContinuityResult first = combinedContext.Controller.
            HandleSessionReAuthDeliveryFailure(
                combinedRecord.SessionId,
                false);
        SessionContinuityResult second = combinedContext.Controller.
            HandleSessionReAuthDeliveryFailure(
                combinedRecord.SessionId,
                true);

        Assert.Multiple(() =>
        {
            Assert.That(refused.Session!.State,
                Is.EqualTo(DiameterSessionState.Failed));
            Assert.That(refused.DisconnectRequired, Is.True);
            Assert.That(retry.RetryWithoutDestinationHost, Is.True);
            Assert.That(retry.FailoverRequired, Is.True);
            Assert.That(CountContinuityAvps(
                retry.Message!,
                DiameterAvpCodes.DestinationHost), Is.Zero);
            Assert.That(retry.Session!.RouteIdentifier, Is.Empty);
            Assert.That(allowed.AllowService, Is.True);
            Assert.That(allowed.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(first.RetryWithoutDestinationHost, Is.True);
            Assert.That(second.AllowService, Is.True);
            Assert.That(second.Session!.State,
                Is.EqualTo(DiameterSessionState.Open));
        });
    }

    [Test]
    public void TerminationFailureRetriesOnceOrReleasesImmediately()
    {
        SessionContinuityTestContext retryContext =
            CreateContinuityContext();
        DiameterSessionRecord retryRecord = AddPendingContinuitySession(
            retryContext,
            "continuity.example;7",
            DiameterSessionState.Terminating,
            1);
        SessionContinuityResult retry = retryContext.Controller.
            HandleSessionTerminationDeliveryFailure(
                retryRecord.SessionId,
                false);
        SessionContinuityResult exhausted = retryContext.Controller.
            HandleSessionTerminationDeliveryFailure(
                retryRecord.SessionId,
                true);

        SessionContinuityTestContext allowContext =
            CreateContinuityContext();
        DiameterSessionRecord allowRecord = AddPendingContinuitySession(
            allowContext,
            "continuity.example;8",
            DiameterSessionState.Terminating,
            2);
        SessionContinuityResult released = allowContext.Controller.
            HandleSessionTerminationDeliveryFailure(
                allowRecord.SessionId,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(retry.RetryWithoutDestinationHost, Is.True);
            Assert.That(exhausted.Released, Is.True);
            Assert.That(exhausted.Session!.State,
                Is.EqualTo(DiameterSessionState.Closed));
            Assert.That(retryContext.Table.ReadSessionRecordCount(),
                Is.Zero);
            Assert.That(released.Released, Is.True);
            Assert.That(released.AllowService, Is.False);
            Assert.That(allowContext.Table.ReadSessionRecordCount(),
                Is.Zero);
        });
    }

    [Test]
    public void PeerLossClearsOpenAffinityAndAppliesPendingFailover()
    {
        SessionContinuityTestContext context =
            CreateContinuityContext();
        DiameterSessionRecord open = AddContinuitySession(
            context,
            "continuity.example;9",
            DiameterSessionRole.Client,
            DiameterSessionState.Open,
            0,
            true,
            "open-route",
            "lost.example",
            3);
        DiameterSessionRecord pending = AddPendingContinuitySession(
            context,
            "continuity.example;10",
            DiameterSessionState.Reauthorizing,
            1);
        RebindContinuityPeer(
            context,
            pending.SessionId,
            "pending-route",
            "lost.example");

        SessionContinuityBatchResult result =
            context.Controller.ProcessSessionPeerLoss(
                CreateContinuityIdentity("LOST.EXAMPLE"));

        Assert.Multiple(() =>
        {
            Assert.That(result.MatchedSessions, Is.EqualTo(2));
            Assert.That(result.ChangedSessions, Is.EqualTo(2));
            Assert.That(result.ReleasedSessions, Is.Zero);
            Assert.That(result.Results[0].FailoverRequired, Is.True);
            Assert.That(result.Results[1].RetryWithoutDestinationHost,
                Is.True);
            Assert.That(context.Affinities.ReadAffinityRoute(open.SessionId),
                Is.Empty);
            Assert.That(context.Affinities.ReadAffinityRoute(
                pending.SessionId), Is.Empty);
            Assert.That(context.Table.ReadSessionRecordCount(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void PeerRestartReleasesInferredServerSessionsAndRebindsClients()
    {
        SessionContinuityTestContext context =
            CreateContinuityContext();
        DiameterSessionRecord server = AddContinuitySession(
            context,
            "continuity.example;11",
            DiameterSessionRole.Server,
            DiameterSessionState.Open,
            0,
            true,
            "server-route",
            "restarted.example",
            0);
        SetContinuityOriginState(context, server.SessionId, 10, true);
        DiameterSessionRecord client = AddContinuitySession(
            context,
            "continuity.example;12",
            DiameterSessionRole.Client,
            DiameterSessionState.Open,
            0,
            true,
            "client-route",
            "restarted.example",
            3);
        SetContinuityOriginState(context, client.SessionId, 10, true);
        DiameterSessionRecord baseline = AddContinuitySession(
            context,
            "continuity.example;13",
            DiameterSessionRole.Server,
            DiameterSessionState.Open,
            0,
            true,
            "baseline-route",
            "restarted.example",
            0);
        DiameterPeerEvent peerEvent = CreateContinuityRestartEvent(
            "restarted.example",
            10,
            20);

        context.Controller.HandlePeerRestart(peerEvent);
        SessionContinuityBatchResult result =
            context.Controller.LastRestartResult;
        DiameterSessionRecord retainedClient =
            context.Table.ReadSessionRecord(client.SessionId)!;
        DiameterSessionRecord retainedBaseline =
            context.Table.ReadSessionRecord(baseline.SessionId)!;

        Assert.Multiple(() =>
        {
            Assert.That(result.MatchedSessions, Is.EqualTo(3));
            Assert.That(result.ReleasedSessions, Is.EqualTo(1));
            Assert.That(result.Results[0].RestartInferred, Is.True);
            Assert.That(result.Results[0].Released, Is.True);
            Assert.That(context.Table.ReadSessionRecord(server.SessionId),
                Is.Null);
            Assert.That(retainedClient.State,
                Is.EqualTo(DiameterSessionState.Open));
            Assert.That(retainedClient.RouteIdentifier, Is.Empty);
            Assert.That(retainedClient.PeerOriginStateIdentifier,
                Is.EqualTo(20));
            Assert.That(result.Results[1].FailoverRequired, Is.True);
            Assert.That(retainedBaseline.PeerOriginStateIdentifier,
                Is.EqualTo(20));
            Assert.That(
                retainedBaseline.PeerOriginStateIdentifierPresent,
                Is.True);
            Assert.That(context.Table.ReadSessionRecordCount(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void ExplicitReleaseRemovesSessionAndAffinity()
    {
        SessionContinuityTestContext context =
            CreateContinuityContext();
        DiameterSessionRecord record = AddContinuitySession(
            context,
            "continuity.example;14",
            DiameterSessionRole.Client,
            DiameterSessionState.Open,
            0,
            true,
            "release-route",
            "server.example",
            0);

        SessionContinuityResult result =
            context.Controller.ReleaseSessionState(
                record.SessionId,
                DiameterTerminationCause.Logout);

        Assert.Multiple(() =>
        {
            Assert.That(result.Released, Is.True);
            Assert.That(result.DisconnectRequired, Is.True);
            Assert.That(result.Session!.State,
                Is.EqualTo(DiameterSessionState.Closed));
            Assert.That(result.Session.ReleaseRequested, Is.True);
            Assert.That(result.Session.TerminationCause,
                Is.EqualTo(DiameterTerminationCause.Logout));
            Assert.That(context.Table.ReadSessionRecordCount(), Is.Zero);
            Assert.That(context.Affinities.ReadAffinityRoute(
                record.SessionId), Is.Empty);
        });
    }

    [Test]
    public void InvalidNegotiationAndBindingMasksAreRejected()
    {
        SessionContinuityTestContext context =
            CreateContinuityContext();
        DiameterSessionRecord record = AddContinuitySession(
            context,
            "continuity.example;15",
            DiameterSessionRole.Client,
            DiameterSessionState.Open,
            0,
            true,
            "route",
            "server.example",
            0);
        DiameterMessage answer = CreateContinuityAnswer(
            record.SessionId,
            100);
        AddContinuityUnsigned32(
            answer,
            DiameterAvpCodes.SessionBinding,
            7);
        AddContinuityUnsigned32(
            answer,
            DiameterAvpCodes.SessionServerFailover,
            1);
        DiameterSessionConfiguration invalidConfiguration =
            CreateContinuityConfiguration(
                "continuity.example;invalid",
                DiameterSessionRole.Server,
                0,
                true);
        invalidConfiguration.SessionBinding = 8;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => context.Controller.ApplySessionAuthorizationPolicy(
                    record.SessionId,
                    answer,
                    "route",
                    CreateContinuityIdentity("server.example")),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => invalidConfiguration.ValidateSessionConfiguration(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(context.Table.ReadSessionRecord(
                record.SessionId)!.Configuration.SessionBinding,
                Is.Zero);
        });
    }

    private static SessionContinuityTestContext CreateContinuityContext()
    {
        SessionContinuityTestContext result =
            new SessionContinuityTestContext();
        result.Clock = new FakeDiscoveryClock();
        result.Table = new SessionTable();
        result.Affinities = new RouteSessionAffinityStore(32);
        result.Controller = new SessionContinuityController(
            result.Table,
            result.Affinities,
            result.Clock);
        return result;
    }

    private static DiameterSessionRecord AddPendingContinuitySession(
        SessionContinuityTestContext context,
        string sessionId,
        DiameterSessionState state,
        int failoverPolicy)
    {
        DiameterSessionRecord result = AddContinuitySession(
            context,
            sessionId,
            DiameterSessionRole.Client,
            state,
            0,
            true,
            "pending-route",
            "server.example",
            failoverPolicy);
        DiameterSessionRecord changed = context.Table.ReadSessionRecord(
            sessionId)!;
        changed.PendingRequestIdentifier = 50;
        changed.LastRequest = CreateContinuityRequest(
            sessionId,
            state == DiameterSessionState.Terminating ?
                DiameterCommandCodes.SessionTermination : 300,
            50);
        AddContinuityIdentity(
            changed.LastRequest,
            DiameterAvpCodes.DestinationHost,
            "server.example");
        changed.TerminationCause = DiameterTerminationCause.Logout;
        changed.TerminationCausePresent =
            state == DiameterSessionState.Terminating;
        return context.Table.SaveSessionRecord(
            changed,
            changed.Version);
    }

    private static DiameterSessionRecord AddContinuitySession(
        SessionContinuityTestContext context,
        string sessionId,
        DiameterSessionRole role,
        DiameterSessionState state,
        uint binding,
        bool allowFailover,
        string routeIdentifier,
        string peerIdentity,
        int failoverPolicy)
    {
        DiameterSessionRecord result = new DiameterSessionRecord();
        result.SessionId = sessionId;
        result.Configuration = CreateContinuityConfiguration(
            sessionId,
            role,
            failoverPolicy,
            allowFailover);
        result.Configuration.SessionBinding = binding;
        result.State = state;
        result.PeerIdentity = CreateContinuityIdentity(peerIdentity);
        result.RouteIdentifier = routeIdentifier;
        result.CreatedAt = context.Clock.UtcTime;
        result.UpdatedAt = context.Clock.UtcTime;
        result.StateChangedAt = context.Clock.UtcTime;
        DiameterSessionRecord saved = context.Table.SaveSessionRecord(
            result,
            0);
        context.Affinities.StoreAffinityRoute(sessionId, routeIdentifier);
        return saved;
    }

    private static DiameterSessionConfiguration CreateContinuityConfiguration(
        string sessionId,
        DiameterSessionRole role,
        int failoverPolicy,
        bool allowFailover)
    {
        DiameterSessionConfiguration result =
            new DiameterSessionConfiguration();
        result.SessionId = sessionId;
        result.Role = role;
        result.ApplicationId = 4;
        result.SessionServerFailover = failoverPolicy;
        result.AllowFailover = allowFailover;
        if (role == DiameterSessionRole.Client)
        {
            result.DestinationRealm =
                CreateContinuityIdentity("remote.example");
        }

        return result;
    }

    private static void RebindContinuityPeer(
        SessionContinuityTestContext context,
        string sessionId,
        string routeIdentifier,
        string peerIdentity)
    {
        context.Controller.RebindSessionRoute(
            sessionId,
            routeIdentifier,
            CreateContinuityIdentity(peerIdentity));
    }

    private static void SetContinuityOriginState(
        SessionContinuityTestContext context,
        string sessionId,
        uint value,
        bool present)
    {
        DiameterSessionRecord record = context.Table.ReadSessionRecord(
            sessionId)!;
        record.PeerOriginStateIdentifier = value;
        record.PeerOriginStateIdentifierPresent = present;
        context.Table.SaveSessionRecord(record, record.Version);
    }

    private static DiameterPeerEvent CreateContinuityRestartEvent(
        string peerIdentity,
        uint previous,
        uint current)
    {
        DiameterPeerEvent result = new DiameterPeerEvent();
        result.Kind = DiameterPeerEventKind.RestartDetected;
        result.Peer.Configuration.Identity =
            CreateContinuityIdentity(peerIdentity);
        result.PreviousOriginStateId = previous;
        result.CurrentOriginStateId = current;
        return result;
    }

    private static DiameterMessage CreateContinuityRequest(
        string sessionId,
        uint commandCode,
        uint identifier)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.CommandCode = commandCode;
        result.ApplicationId = commandCode ==
            DiameterCommandCodes.SessionTermination ? 0U : 4U;
        result.HopByHopIdentifier = identifier;
        result.EndToEndIdentifier = identifier + 100;
        AddContinuitySessionId(result, sessionId);
        return result;
    }

    private static DiameterMessage CreateContinuityAnswer(
        string sessionId,
        uint identifier)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Proxyable;
        result.CommandCode = 300;
        result.ApplicationId = 4;
        result.HopByHopIdentifier = identifier;
        result.EndToEndIdentifier = identifier + 100;
        AddContinuitySessionId(result, sessionId);
        AddContinuityUnsigned32(
            result,
            DiameterAvpCodes.ResultCode,
            DiameterResultCodes.Success);
        return result;
    }

    private static void AddContinuitySessionId(
        DiameterMessage message,
        string sessionId)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = DiameterAvpCodes.SessionId;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = Encoding.UTF8.GetBytes(sessionId);
        message.Avps.Add(avp);
    }

    private static void AddContinuityUnsigned32(
        DiameterMessage message,
        uint code,
        uint value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(avp.Data, value);
        message.Avps.Add(avp);
    }

    private static void AddContinuityIdentity(
        DiameterMessage message,
        uint code,
        string value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = Encoding.UTF8.GetBytes(value);
        message.Avps.Add(avp);
    }

    private static int CountContinuityAvps(
        DiameterMessage message,
        uint code)
    {
        int count = 0;
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].VendorId == DiameterVendorIds.Ietf &&
                message.Avps[index].Code == code)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    private static string ReadContinuityIdentity(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            if (message.Avps[index].VendorId == DiameterVendorIds.Ietf &&
                message.Avps[index].Code == code)
            {
                return Encoding.UTF8.GetString(
                    message.Avps[index].Data);
            }

            index = index + 1;
        }

        return string.Empty;
    }

    private static DiameterIdentity CreateContinuityIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class SessionContinuityTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal SessionTable Table = new SessionTable();
    internal RouteSessionAffinityStore Affinities =
        new RouteSessionAffinityStore(32);
    internal SessionContinuityController Controller = null!;
}
