using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class PeerWatchdogControllerTests
{
    [Test]
    public void InitialConnectionIsImmediatelyAvailableWithoutWatchdogValidation()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);

        PeerWatchdogResult result =
            context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Code,
                Is.EqualTo("initial-watchdog-connection-ready"));
            Assert.That(result.Message, Is.Null);
            Assert.That(result.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Okay));
            Assert.That(result.Peer.Available, Is.True);
            Assert.That(result.Peer.WatchdogPending, Is.False);
            Assert.That(result.Peer.WatchdogDeadlineAt,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(6)));
        });
    }

    [Test]
    public void ReceivedTrafficResetsIdleTimerAndSuppressesWatchdogTraffic()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Clock.AdvanceDiscoveryTime(6);
        DiameterMessage message = new DiameterMessage();
        message.CommandCode = 999;

        PeerWatchdogResult activity = context.Controller.HandlePeerMessageReceived(
            "peer.example",
            message);
        PeerWatchdogResult early = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        context.Clock.AdvanceDiscoveryTime(6);
        PeerWatchdogResult due = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(activity.Handled, Is.True);
            Assert.That(activity.Peer.LastActivityAt, Is.EqualTo(
                context.Clock.UtcTime.AddSeconds(-6)));
            Assert.That(early.Handled, Is.False);
            Assert.That(early.Code, Is.EqualTo("watchdog-timer-not-due"));
            Assert.That(due.Handled, Is.True);
            Assert.That(due.Code, Is.EqualTo("idle-watchdog-sent"));
            Assert.That(due.Message, Is.Not.Null);
            Assert.That(due.Message!.CommandCode,
                Is.EqualTo(DiameterCommandCodes.DeviceWatchdog));
        });
    }

    [Test]
    public void IdleWatchdogAndCorrelatedAnswerMaintainHealthyPeer()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Clock.AdvanceDiscoveryTime(6);

        PeerWatchdogResult sent = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        DiameterMessage answer = CreateWatchdogTestAnswer(context, sent.Message!);
        PeerWatchdogResult accepted = context.Controller.HandlePeerWatchdogAnswer(
            "peer.example",
            answer);

        Assert.Multiple(() =>
        {
            Assert.That(sent.Peer.WatchdogPending, Is.True);
            Assert.That(sent.Peer.PendingWatchdogHopByHopIdentifier,
                Is.EqualTo(sent.Message!.HopByHopIdentifier));
            Assert.That(sent.Peer.PendingWatchdogEndToEndIdentifier,
                Is.EqualTo(sent.Message.EndToEndIdentifier));
            Assert.That(accepted.Handled, Is.True);
            Assert.That(accepted.Code, Is.EqualTo("watchdog-answer-accepted"));
            Assert.That(accepted.Peer.WatchdogPending, Is.False);
            Assert.That(accepted.Peer.ConsecutiveWatchdogSuccesses, Is.EqualTo(1));
            Assert.That(accepted.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Okay));
            Assert.That(accepted.Peer.Available, Is.True);
        });
    }

    [Test]
    public void IncomingWatchdogRequestProducesCorrelatedAnswer()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        DiameterMessage request = context.Factory.CreateDeviceWatchdogRequest(
            CreateWatchdogTestOriginOptions());

        PeerWatchdogResult result = context.Controller.HandlePeerWatchdogRequest(
            "peer.example",
            request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Code, Is.EqualTo("watchdog-request-answered"));
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.Message!.CommandCode,
                Is.EqualTo(DiameterCommandCodes.DeviceWatchdog));
            Assert.That(
                result.Message.Flags & DiameterMessageFlags.Request,
                Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(result.Message.HopByHopIdentifier,
                Is.EqualTo(request.HopByHopIdentifier));
            Assert.That(result.Message.EndToEndIdentifier,
                Is.EqualTo(request.EndToEndIdentifier));
            Assert.That(result.Peer.LastActivityAt, Is.EqualTo(context.Clock.UtcTime));
        });
    }

    [Test]
    public void TwoPendingExpirationsInitiateFailoverThenCloseAndReconnect()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Clock.AdvanceDiscoveryTime(6);
        context.Controller.HandlePeerWatchdogTimer("peer.example");
        context.Clock.AdvanceDiscoveryTime(6);

        PeerWatchdogResult suspect = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        context.Clock.AdvanceDiscoveryTime(6);
        PeerWatchdogResult down = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(suspect.Handled, Is.True);
            Assert.That(suspect.FailoverRequired, Is.True);
            Assert.That(suspect.CloseConnection, Is.False);
            Assert.That(suspect.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Suspect));
            Assert.That(suspect.Peer.Available, Is.False);
            Assert.That(suspect.Peer.ConsecutiveWatchdogFailures, Is.EqualTo(1));
            Assert.That(down.Handled, Is.True);
            Assert.That(down.CloseConnection, Is.True);
            Assert.That(down.FailoverRequired, Is.False);
            Assert.That(down.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Down));
            Assert.That(down.Peer.State, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(down.Peer.Connection, Is.Null);
            Assert.That(down.Peer.ConsecutiveWatchdogFailures, Is.EqualTo(2));
            Assert.That(down.Peer.NextConnectionAttemptAt,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(1)));
        });
    }

    [Test]
    public void TrafficRestoresSuspectPeerWithoutClosingConnection()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Clock.AdvanceDiscoveryTime(6);
        context.Controller.HandlePeerWatchdogTimer("peer.example");
        context.Clock.AdvanceDiscoveryTime(6);
        context.Controller.HandlePeerWatchdogTimer("peer.example");
        DiameterMessage message = new DiameterMessage();
        message.CommandCode = 999;

        PeerWatchdogResult restored = context.Controller.HandlePeerMessageReceived(
            "peer.example",
            message);

        Assert.Multiple(() =>
        {
            Assert.That(restored.Handled, Is.True);
            Assert.That(restored.FailbackReady, Is.True);
            Assert.That(restored.Code, Is.EqualTo("suspect-peer-restored"));
            Assert.That(restored.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Okay));
            Assert.That(restored.Peer.Available, Is.True);
            Assert.That(restored.Peer.Connection, Is.Not.Null);
            Assert.That(restored.Peer.WatchdogPending, Is.True);
        });
    }

    [Test]
    public void TransportFailureImmediatelyFailsOverAndSchedulesReconnect()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");

        PeerWatchdogResult failed = context.Controller.HandlePeerTransportFailure(
            "peer.example");
        PeerWatchdogResult repeated = context.Controller.HandlePeerTransportFailure(
            "peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(failed.Handled, Is.True);
            Assert.That(failed.FailoverRequired, Is.True);
            Assert.That(failed.CloseConnection, Is.True);
            Assert.That(failed.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Down));
            Assert.That(failed.Peer.NextConnectionAttemptAt,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(1)));
            Assert.That(repeated.Handled, Is.False);
            Assert.That(repeated.Code, Is.EqualTo("peer-transport-already-down"));
        });
    }

    [Test]
    public void ReopenedPeerRequiresThreeCorrelatedWatchdogAnswersForFailback()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Controller.HandlePeerTransportFailure("peer.example");
        ReopenWatchdogTestConnection(context);

        PeerWatchdogResult opened =
            context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        DiameterMessage nonWatchdog = new DiameterMessage();
        nonWatchdog.CommandCode = 999;
        PeerWatchdogResult discarded = context.Controller.HandlePeerMessageReceived(
            "peer.example",
            nonWatchdog);
        PeerWatchdogResult first = AcceptCurrentWatchdogAnswer(context, opened);
        context.Clock.AdvanceDiscoveryTime(6);
        PeerWatchdogResult secondRequest = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        PeerWatchdogResult second = AcceptCurrentWatchdogAnswer(
            context,
            secondRequest);
        context.Clock.AdvanceDiscoveryTime(6);
        PeerWatchdogResult thirdRequest = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        PeerWatchdogResult third = AcceptCurrentWatchdogAnswer(
            context,
            thirdRequest);

        Assert.Multiple(() =>
        {
            Assert.That(opened.Code, Is.EqualTo("reopen-watchdog-sent"));
            Assert.That(opened.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Reopen));
            Assert.That(opened.Peer.Available, Is.False);
            Assert.That(discarded.MessageDiscarded, Is.True);
            Assert.That(first.Peer.ConsecutiveWatchdogSuccesses, Is.EqualTo(1));
            Assert.That(first.Peer.Available, Is.False);
            Assert.That(first.FailbackReady, Is.False);
            Assert.That(second.Peer.ConsecutiveWatchdogSuccesses, Is.EqualTo(2));
            Assert.That(second.Peer.Available, Is.False);
            Assert.That(second.FailbackReady, Is.False);
            Assert.That(third.Peer.ConsecutiveWatchdogSuccesses, Is.EqualTo(3));
            Assert.That(third.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Okay));
            Assert.That(third.Peer.Available, Is.True);
            Assert.That(third.FailbackReady, Is.True);
            Assert.That(third.Code, Is.EqualTo("reopen-peer-stabilized"));
        });
    }

    [Test]
    public void ReopenedPeerClosesAfterTwoPendingExpirations()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Controller.HandlePeerTransportFailure("peer.example");
        ReopenWatchdogTestConnection(context);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Clock.AdvanceDiscoveryTime(6);

        PeerWatchdogResult firstMiss = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        context.Clock.AdvanceDiscoveryTime(6);
        PeerWatchdogResult secondMiss = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(firstMiss.Code, Is.EqualTo("reopen-watchdog-pending"));
            Assert.That(firstMiss.Peer.ReopenWatchdogMisses, Is.EqualTo(1));
            Assert.That(firstMiss.Peer.State,
                Is.EqualTo(DiameterPeerState.InitiatorOpen));
            Assert.That(secondMiss.Code, Is.EqualTo("reopen-watchdog-failed"));
            Assert.That(secondMiss.CloseConnection, Is.True);
            Assert.That(secondMiss.Peer.State, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(secondMiss.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Down));
        });
    }

    [Test]
    public void UncorrelatedOrMalformedAnswerDoesNotClearPendingWatchdog()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Clock.AdvanceDiscoveryTime(6);
        PeerWatchdogResult sent = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        DiameterMessage uncorrelated = CreateWatchdogTestAnswer(
            context,
            sent.Message!);
        uncorrelated.HopByHopIdentifier++;
        DiameterMessage malformed = new DiameterMessage();
        malformed.CommandCode = DiameterCommandCodes.DeviceWatchdog;

        PeerWatchdogResult correlation =
            context.Controller.HandlePeerWatchdogAnswer(
                "peer.example",
                uncorrelated);
        PeerWatchdogResult invalid = context.Controller.HandlePeerWatchdogAnswer(
            "peer.example",
            malformed);

        Assert.Multiple(() =>
        {
            Assert.That(correlation.Handled, Is.False);
            Assert.That(correlation.Code,
                Is.EqualTo("watchdog-answer-not-correlated"));
            Assert.That(correlation.Peer.WatchdogPending, Is.True);
            Assert.That(invalid.Handled, Is.False);
            Assert.That(invalid.Code, Is.EqualTo("invalid-watchdog-result-code"));
            Assert.That(invalid.Peer.WatchdogPending, Is.True);
        });
    }

    [Test]
    public void ConcurrentPendingExpirationInitiatesFailoverOnlyOnce()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Clock.AdvanceDiscoveryTime(6);
        context.Controller.HandlePeerWatchdogTimer("peer.example");
        context.Clock.AdvanceDiscoveryTime(6);
        Barrier barrier = new Barrier(3);

        Task<PeerWatchdogResult> firstTask = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return context.Controller.HandlePeerWatchdogTimer("peer.example");
        });
        Task<PeerWatchdogResult> secondTask = Task.Run(() =>
        {
            barrier.SignalAndWait();
            return context.Controller.HandlePeerWatchdogTimer("peer.example");
        });
        barrier.SignalAndWait();
        Task.WaitAll(firstTask, secondTask);
        PeerWatchdogResult first = firstTask.Result;
        PeerWatchdogResult second = secondTask.Result;
        int failoverCount = (first.FailoverRequired ? 1 : 0) +
            (second.FailoverRequired ? 1 : 0);
        int notDueCount = (first.Code == "watchdog-timer-not-due" ? 1 : 0) +
            (second.Code == "watchdog-timer-not-due" ? 1 : 0);
        DiameterPeerSnapshot peer = context.Table.ReadPeerTableSnapshot(
            "peer.example")!;

        Assert.Multiple(() =>
        {
            Assert.That(failoverCount, Is.EqualTo(1));
            Assert.That(notDueCount, Is.EqualTo(1));
            Assert.That(peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Suspect));
            Assert.That(peer.ConsecutiveWatchdogFailures, Is.EqualTo(1));
            Assert.That(peer.Available, Is.False);
            Assert.That(peer.Connection, Is.Not.Null);
        });
    }

    [Test]
    public void MissedReopenAnswerResetsConsecutiveFailbackSuccesses()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Controller.HandlePeerTransportFailure("peer.example");
        ReopenWatchdogTestConnection(context);
        PeerWatchdogResult firstRequest =
            context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        PeerWatchdogResult first = AcceptCurrentWatchdogAnswer(
            context,
            firstRequest);
        context.Clock.AdvanceDiscoveryTime(6);
        PeerWatchdogResult secondRequest = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        PeerWatchdogResult second = AcceptCurrentWatchdogAnswer(
            context,
            secondRequest);
        context.Clock.AdvanceDiscoveryTime(6);
        PeerWatchdogResult missedRequest = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        context.Clock.AdvanceDiscoveryTime(6);

        PeerWatchdogResult missed = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        PeerWatchdogResult recovered = AcceptCurrentWatchdogAnswer(
            context,
            missedRequest);
        context.Clock.AdvanceDiscoveryTime(6);
        PeerWatchdogResult fourthRequest = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        PeerWatchdogResult fourth = AcceptCurrentWatchdogAnswer(
            context,
            fourthRequest);
        context.Clock.AdvanceDiscoveryTime(6);
        PeerWatchdogResult fifthRequest = context.Controller.HandlePeerWatchdogTimer(
            "peer.example");
        PeerWatchdogResult fifth = AcceptCurrentWatchdogAnswer(
            context,
            fifthRequest);

        Assert.Multiple(() =>
        {
            Assert.That(first.Peer.ConsecutiveWatchdogSuccesses, Is.EqualTo(1));
            Assert.That(second.Peer.ConsecutiveWatchdogSuccesses, Is.EqualTo(2));
            Assert.That(missed.Code, Is.EqualTo("reopen-watchdog-pending"));
            Assert.That(missed.Peer.ConsecutiveWatchdogSuccesses, Is.EqualTo(0));
            Assert.That(recovered.Peer.ConsecutiveWatchdogSuccesses, Is.EqualTo(1));
            Assert.That(recovered.FailbackReady, Is.False);
            Assert.That(fourth.Peer.ConsecutiveWatchdogSuccesses, Is.EqualTo(2));
            Assert.That(fourth.FailbackReady, Is.False);
            Assert.That(fifth.Peer.ConsecutiveWatchdogSuccesses, Is.EqualTo(3));
            Assert.That(fifth.FailbackReady, Is.True);
            Assert.That(fifth.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Okay));
            Assert.That(fifth.Peer.Available, Is.True);
        });
    }

    [Test]
    public void ReopenedPeerDiscardsIncomingWatchdogRequestsDuringFailback()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Controller.HandlePeerTransportFailure("peer.example");
        ReopenWatchdogTestConnection(context);
        PeerWatchdogResult opened =
            context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        DiameterMessage request = context.Factory.CreateDeviceWatchdogRequest(
            CreateWatchdogTestOriginOptions());

        PeerWatchdogResult discarded =
            context.Controller.HandlePeerWatchdogRequest(
                "peer.example",
                request);

        Assert.Multiple(() =>
        {
            Assert.That(discarded.Handled, Is.False);
            Assert.That(discarded.MessageDiscarded, Is.True);
            Assert.That(discarded.Code, Is.EqualTo("watchdog-request-discarded"));
            Assert.That(discarded.Message, Is.Null);
            Assert.That(discarded.Peer.WatchdogState,
                Is.EqualTo(DiameterPeerWatchdogState.Reopen));
            Assert.That(discarded.Peer.WatchdogPending, Is.True);
            Assert.That(discarded.Peer.PendingWatchdogHopByHopIdentifier,
                Is.EqualTo(opened.Message!.HopByHopIdentifier));
            Assert.That(discarded.Peer.PendingWatchdogEndToEndIdentifier,
                Is.EqualTo(opened.Message.EndToEndIdentifier));
        });
    }

    [Test]
    public void TransportFailureDoesNotScheduleAutomaticReconnectWhenDisabled()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            0,
            0);
        context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        context.Configuration.ConnectAutomatically = false;
        context.Table.AddOrUpdatePeerConfiguration(context.Configuration);

        PeerWatchdogResult failed = context.Controller.HandlePeerTransportFailure(
            "peer.example");
        context.Clock.AdvanceDiscoveryTime(2);
        PeerConnectionLifecycleResult retry =
            new PeerConnectionLifecycle(context.Table).ClaimDuePeerConnectionAttempt(
                "peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(failed.Handled, Is.True);
            Assert.That(failed.FailoverRequired, Is.True);
            Assert.That(failed.CloseConnection, Is.True);
            Assert.That(failed.Peer.State, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(failed.Peer.NextConnectionAttemptAt,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(retry.Accepted, Is.False);
            Assert.That(retry.Code,
                Is.EqualTo("automatic-peer-connect-disabled"));
        });
    }

    [Test]
    public void WatchdogJitterIsResampledWithinConfiguredBounds()
    {
        WatchdogTestContext context = CreateWatchdogTestContext(
            6000,
            1000,
            0,
            1000,
            2000);

        PeerWatchdogResult opened =
            context.Controller.HandlePeerWatchdogConnectionOpened("peer.example");
        DiameterMessage firstMessage = new DiameterMessage();
        firstMessage.CommandCode = 999;
        PeerWatchdogResult center = context.Controller.HandlePeerMessageReceived(
            "peer.example",
            firstMessage);
        DiameterMessage secondMessage = new DiameterMessage();
        secondMessage.CommandCode = 998;
        PeerWatchdogResult maximum = context.Controller.HandlePeerMessageReceived(
            "peer.example",
            secondMessage);

        Assert.Multiple(() =>
        {
            Assert.That(opened.Peer.WatchdogDeadlineAt,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(5)));
            Assert.That(center.Peer.WatchdogDeadlineAt,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(6)));
            Assert.That(maximum.Peer.WatchdogDeadlineAt,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(7)));
        });
    }

    private static WatchdogTestContext CreateWatchdogTestContext(
        int watchdogMilliseconds,
        int watchdogJitterMilliseconds,
        params int[] jitterValues)
    {
        WatchdogTestContext result = new WatchdogTestContext();
        DiameterTimerConfiguration timers = new DiameterTimerConfiguration();
        timers.ConnectionRetryMilliseconds = 1000;
        timers.ConnectionRetryJitterPercent = 0;
        timers.WatchdogIntervalMilliseconds = watchdogMilliseconds;
        timers.WatchdogJitterMilliseconds = watchdogJitterMilliseconds;
        result.Table = new PeerTable(result.Clock, timers, 1);
        result.Table.WatchdogJitterSource =
            new SequencePeerWatchdogJitterSource(jitterValues);
        result.Configuration = CreateWatchdogTestPeerConfiguration();
        result.Table.AddOrUpdatePeerConfiguration(result.Configuration);
        result.Table.AttachPeerTableConnection(
            "peer.example",
            result.Configuration.Endpoints[0],
            CreateWatchdogTestConnection(result.Configuration.Endpoints[0]),
            DiameterConnectionDirection.Outbound);
        result.Table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.InitiatorOpen);
        result.Controller = new PeerWatchdogController(
            result.Table,
            result.Factory,
            CreateWatchdogTestOriginOptions());
        return result;
    }

    private static DiameterPeerConfiguration CreateWatchdogTestPeerConfiguration()
    {
        DiameterPeerConfiguration result = new DiameterPeerConfiguration();
        result.Identity.Value = "peer.example";
        result.Realm.Value = "example";
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = "peer.example";
        endpoint.Address = IPAddress.Parse("192.0.2.10");
        endpoint.Port = DiameterProtocolConstants.Port;
        endpoint.Transport = DiameterTransportKind.Tcp;
        result.Endpoints.Add(endpoint);
        return result;
    }

    private static DiameterConnectionMetadata CreateWatchdogTestConnection(
        DiameterEndpoint endpoint)
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = "watchdog-connection";
        result.LocalEndpoint = new DiameterEndpoint();
        result.LocalEndpoint.Identity.Value = "node.example";
        result.LocalEndpoint.Address = IPAddress.Parse("192.0.2.1");
        result.LocalEndpoint.Port = DiameterProtocolConstants.Port;
        result.LocalEndpoint.Transport = DiameterTransportKind.Tcp;
        result.RemoteEndpoint = PeerTableCloner.ClonePeerTableEndpoint(endpoint);
        result.Transport.Kind = DiameterTransportKind.Tcp;
        result.Security.Mode = DiameterSecurityMode.None;
        result.Open = true;
        return result;
    }

    private static DiameterBaseCommandOptions CreateWatchdogTestOriginOptions()
    {
        DiameterBaseCommandOptions result = new DiameterBaseCommandOptions();
        result.OriginHost.Value = "node.example";
        result.OriginRealm.Value = "example";
        return result;
    }

    private static DiameterMessage CreateWatchdogTestAnswer(
        WatchdogTestContext context,
        DiameterMessage request)
    {
        DiameterBaseCommandOptions options = CreateWatchdogTestOriginOptions();
        options.Request = request;
        options.ResultCode = DiameterResultCodes.Success;
        return context.Factory.CreateDeviceWatchdogAnswer(options);
    }

    private static PeerWatchdogResult AcceptCurrentWatchdogAnswer(
        WatchdogTestContext context,
        PeerWatchdogResult request)
    {
        DiameterMessage answer = CreateWatchdogTestAnswer(
            context,
            request.Message!);
        return context.Controller.HandlePeerWatchdogAnswer(
            "peer.example",
            answer);
    }

    private static void ReopenWatchdogTestConnection(WatchdogTestContext context)
    {
        context.Table.AttachPeerTableConnection(
            "peer.example",
            context.Configuration.Endpoints[0],
            CreateWatchdogTestConnection(context.Configuration.Endpoints[0]),
            DiameterConnectionDirection.Outbound);
        context.Table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.InitiatorOpen);
    }
}

internal class WatchdogTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal BaseCommandTestIdentifierSource Identifiers =
        new BaseCommandTestIdentifierSource();
    internal IDiameterBaseCommandFactory Factory;
    internal PeerTable Table = null!;
    internal DiameterPeerConfiguration Configuration =
        new DiameterPeerConfiguration();
    internal PeerWatchdogController Controller = null!;

    internal WatchdogTestContext()
    {
        Factory = new DiameterBaseCommandFactory(Identifiers);
    }
}

internal class SequencePeerWatchdogJitterSource : IPeerWatchdogJitterSource
{
    internal List<int> Values = new List<int>();
    internal int Index;

    internal SequencePeerWatchdogJitterSource(params int[] values)
    {
        Values.AddRange(values);
    }

    public int ReadPeerWatchdogJitterValue(int maximumExclusive)
    {
        if (Values.Count == 0)
        {
            return 0;
        }

        int result = Values[Index % Values.Count];
        Index++;
        return result;
    }
}
