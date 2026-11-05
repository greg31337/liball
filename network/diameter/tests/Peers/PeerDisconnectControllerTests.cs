using System.Net;

namespace diameter.Tests;

public class PeerDisconnectControllerTests
{
    [Test]
    public void LocalDisconnectDrainsRequestAndWaitsForCorrelatedAnswer()
    {
        DisconnectTestContext context = CreateDisconnectTestContext();
        DiameterDisconnectRequest request = CreateDisconnectTestRequest(
            DiameterDisconnectCause.Rebooting,
            true,
            true);

        PeerDisconnectResult started =
            context.Controller.BeginPeerDisconnect(request);
        PeerDisconnectResult drained =
            context.Controller.CompletePeerDisconnectDrain("peer.example");
        DiameterMessage answer = CreateDisconnectTestAnswer(
            context,
            started.Message!);
        PeerDisconnectResult completed =
            context.Controller.HandlePeerDisconnectAnswer(
                "peer.example",
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(started.Handled, Is.True);
            Assert.That(started.Code,
                Is.EqualTo("disconnect-request-pending-drain"));
            Assert.That(started.DrainRequired, Is.True);
            Assert.That(started.WaitingForAnswer, Is.True);
            Assert.That(started.CloseConnection, Is.False);
            Assert.That(started.Message, Is.Not.Null);
            Assert.That(started.Message!.CommandCode,
                Is.EqualTo(DiameterCommandCodes.DisconnectPeer));
            Assert.That(
                started.Message.Flags & DiameterMessageFlags.Request,
                Is.EqualTo(DiameterMessageFlags.Request));
            Assert.That(ReadDisconnectTestCause(started.Message),
                Is.EqualTo(DiameterDisconnectCause.Rebooting));
            Assert.That(started.Peer.State,
                Is.EqualTo(DiameterPeerState.Closing));
            Assert.That(started.Peer.Connection, Is.Not.Null);
            Assert.That(started.Peer.DisconnectPending, Is.True);
            Assert.That(started.Peer.DisconnectDrainPending, Is.True);
            Assert.That(started.Peer.DisconnectAnswerPending, Is.True);
            Assert.That(started.Peer.DisconnectDeadlineAt,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(5)));
            Assert.That(drained.Code, Is.EqualTo("disconnect-answer-pending"));
            Assert.That(drained.WaitingForAnswer, Is.True);
            Assert.That(drained.Peer.Connection, Is.Not.Null);
            Assert.That(drained.Peer.DisconnectDrainPending, Is.False);
            Assert.That(completed.Handled, Is.True);
            Assert.That(completed.Code,
                Is.EqualTo("disconnect-answer-accepted"));
            Assert.That(completed.CloseConnection, Is.True);
            Assert.That(completed.Peer.State,
                Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(completed.Peer.Connection, Is.Null);
            Assert.That(completed.Peer.DisconnectPending, Is.False);
            Assert.That(completed.Peer.ReconnectSuppressed, Is.True);
            Assert.That(completed.Peer.NextConnectionAttemptAt,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(completed.Peer.LastDisconnectedAt,
                Is.EqualTo(context.Clock.UtcTime));
        });
    }

    [Test]
    public void LocalDisconnectWithoutAnswerClosesOnlyAfterDrain()
    {
        DisconnectTestContext context = CreateDisconnectTestContext();
        DiameterDisconnectRequest request = CreateDisconnectTestRequest(
            DiameterDisconnectCause.Rebooting,
            false,
            false);

        PeerDisconnectResult started =
            context.Controller.BeginPeerDisconnect(request);
        PeerDisconnectResult completed =
            context.Controller.CompletePeerDisconnectDrain("peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(started.CloseConnection, Is.False);
            Assert.That(started.Peer.Connection, Is.Not.Null);
            Assert.That(started.Peer.DisconnectAnswerPending, Is.False);
            Assert.That(completed.Handled, Is.True);
            Assert.That(completed.Code,
                Is.EqualTo("disconnect-without-answer-completed"));
            Assert.That(completed.CloseConnection, Is.True);
            Assert.That(completed.Peer.Connection, Is.Null);
            Assert.That(completed.Peer.DisconnectPending, Is.False);
            Assert.That(completed.Peer.NextConnectionAttemptAt,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(1)));
        });
    }

    [TestCase(DiameterDisconnectCause.Busy)]
    [TestCase(DiameterDisconnectCause.DoNotWantToTalkToYou)]
    public void RemotePermanentDisconnectAnswersThenSuppressesReconnect(
        DiameterDisconnectCause cause)
    {
        DisconnectTestContext context = CreateDisconnectTestContext();
        DiameterMessage request = CreateDisconnectTestRequestMessage(
            context,
            cause);

        PeerDisconnectResult answered =
            context.Controller.HandlePeerDisconnectRequest(
                "peer.example",
                request);
        PeerDisconnectResult drained =
            context.Controller.CompletePeerDisconnectDrain("peer.example");
        PeerDisconnectResult closed =
            context.Controller.HandlePeerDisconnectTransportClosed(
                "peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(answered.Handled, Is.True);
            Assert.That(answered.Code,
                Is.EqualTo("disconnect-answer-pending-drain"));
            Assert.That(answered.DrainRequired, Is.True);
            Assert.That(answered.Message, Is.Not.Null);
            Assert.That(
                answered.Message!.Flags & DiameterMessageFlags.Request,
                Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(answered.Message.HopByHopIdentifier,
                Is.EqualTo(request.HopByHopIdentifier));
            Assert.That(answered.Message.EndToEndIdentifier,
                Is.EqualTo(request.EndToEndIdentifier));
            Assert.That(answered.Peer.Connection, Is.Not.Null);
            Assert.That(answered.Peer.ReconnectSuppressed, Is.True);
            Assert.That(answered.Peer.ReconnectSuppressionCause,
                Is.EqualTo(cause));
            Assert.That(drained.CloseConnection, Is.False);
            Assert.That(drained.WaitingForTransportClose, Is.True);
            Assert.That(drained.Code,
                Is.EqualTo("peer-transport-close-pending"));
            Assert.That(drained.Peer.Connection, Is.Not.Null);
            Assert.That(closed.Handled, Is.True);
            Assert.That(closed.CloseConnection, Is.False);
            Assert.That(closed.Peer.State,
                Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(closed.Peer.Connection, Is.Null);
            Assert.That(closed.Peer.NextConnectionAttemptAt,
                Is.EqualTo(default(DateTimeOffset)));
            Assert.That(closed.Peer.LastDisconnectCause, Is.EqualTo(cause));
        });
    }

    [Test]
    public void RemoteRebootingDisconnectSchedulesReconnectAfterPeerCloses()
    {
        DisconnectTestContext context = CreateDisconnectTestContext();
        context.Table.ReadPeerTableSnapshot("peer.example");
        DiameterMessage request = CreateDisconnectTestRequestMessage(
            context,
            DiameterDisconnectCause.Rebooting);

        PeerDisconnectResult answered =
            context.Controller.HandlePeerDisconnectRequest(
                "peer.example",
                request);
        context.Controller.CompletePeerDisconnectDrain("peer.example");
        PeerDisconnectResult closed =
            context.Controller.HandlePeerDisconnectTransportClosed(
                "peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(answered.Peer.ReconnectSuppressed, Is.False);
            Assert.That(closed.Peer.ReconnectSuppressed, Is.False);
            Assert.That(closed.Peer.NextConnectionAttemptAt,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(1)));
            Assert.That(closed.Peer.LastDisconnectCause,
                Is.EqualTo(DiameterDisconnectCause.Rebooting));
        });
    }

    [Test]
    public void UncorrelatedAnswerDoesNotCompleteLocalDisconnect()
    {
        DisconnectTestContext context = CreateDisconnectTestContext();
        PeerDisconnectResult started = context.Controller.BeginPeerDisconnect(
            CreateDisconnectTestRequest(
                DiameterDisconnectCause.Rebooting,
                true,
                false));
        context.Controller.CompletePeerDisconnectDrain("peer.example");
        DiameterMessage answer = CreateDisconnectTestAnswer(
            context,
            started.Message!);
        answer.HopByHopIdentifier = answer.HopByHopIdentifier + 1;

        PeerDisconnectResult result =
            context.Controller.HandlePeerDisconnectAnswer(
                "peer.example",
                answer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(result.Code,
                Is.EqualTo("disconnect-answer-not-correlated"));
            Assert.That(result.Peer.State,
                Is.EqualTo(DiameterPeerState.Closing));
            Assert.That(result.Peer.Connection, Is.Not.Null);
            Assert.That(result.Peer.DisconnectPending, Is.True);
            Assert.That(result.Peer.DisconnectAnswerPending, Is.True);
        });
    }

    [Test]
    public void MissingAnswerClosesConnectionAtBoundedDeadline()
    {
        DisconnectTestContext context = CreateDisconnectTestContext();
        context.Controller.BeginPeerDisconnect(CreateDisconnectTestRequest(
            DiameterDisconnectCause.Rebooting,
            true,
            false));
        context.Controller.CompletePeerDisconnectDrain("peer.example");
        context.Clock.AdvanceDiscoveryTime(5);

        PeerDisconnectResult result =
            context.Controller.HandlePeerDisconnectTimer("peer.example");

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Code, Is.EqualTo("disconnect-answer-timeout"));
            Assert.That(result.CloseConnection, Is.True);
            Assert.That(result.Peer.State, Is.EqualTo(DiameterPeerState.Closed));
            Assert.That(result.Peer.Connection, Is.Null);
            Assert.That(result.Peer.DisconnectPending, Is.False);
            Assert.That(result.Peer.NextConnectionAttemptAt,
                Is.EqualTo(context.Clock.UtcTime.AddSeconds(1)));
        });
    }

    [Test]
    public void InvalidRemoteDisconnectCauseIsRejectedWithoutChangingPeer()
    {
        DisconnectTestContext context = CreateDisconnectTestContext();
        DiameterMessage request = CreateDisconnectTestRequestMessage(
            context,
            DiameterDisconnectCause.Busy);
        DiameterAvp cause = FindDisconnectTestAvp(
            request,
            DiameterAvpCodes.DisconnectCause);
        cause.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(cause.Data, 99);

        PeerDisconnectResult result =
            context.Controller.HandlePeerDisconnectRequest(
                "peer.example",
                request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(result.Code, Is.EqualTo("invalid-disconnect-cause"));
            Assert.That(result.Message, Is.Null);
            Assert.That(result.Peer.State,
                Is.EqualTo(DiameterPeerState.InitiatorOpen));
            Assert.That(result.Peer.Connection, Is.Not.Null);
            Assert.That(result.Peer.DisconnectPending, Is.False);
            Assert.That(result.Peer.ReconnectSuppressed, Is.False);
        });
    }

    private static DisconnectTestContext CreateDisconnectTestContext()
    {
        DisconnectTestContext result = new DisconnectTestContext();
        DiameterTimerConfiguration timers = new DiameterTimerConfiguration();
        timers.ConnectionRetryMilliseconds = 1000;
        timers.ConnectionRetryJitterPercent = 0;
        timers.DrainTimeoutMilliseconds = 5000;
        result.Table = new PeerTable(result.Clock, timers, 1);
        DiameterPeerConfiguration configuration =
            CreateDisconnectTestPeerConfiguration();
        result.Table.AddOrUpdatePeerConfiguration(configuration);
        result.Table.AttachPeerTableConnection(
            "peer.example",
            configuration.Endpoints[0],
            CreateDisconnectTestConnection(configuration.Endpoints[0]),
            DiameterConnectionDirection.Outbound);
        result.Table.TransitionPeerTableState(
            "peer.example",
            DiameterPeerState.InitiatorOpen);
        result.Controller = new PeerDisconnectController(
            result.Table,
            result.Factory,
            CreateDisconnectTestOriginOptions());
        return result;
    }

    private static DiameterPeerConfiguration
        CreateDisconnectTestPeerConfiguration()
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

    private static DiameterConnectionMetadata CreateDisconnectTestConnection(
        DiameterEndpoint endpoint)
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = "disconnect-connection";
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

    private static DiameterBaseCommandOptions CreateDisconnectTestOriginOptions()
    {
        DiameterBaseCommandOptions result = new DiameterBaseCommandOptions();
        result.OriginHost.Value = "node.example";
        result.OriginRealm.Value = "example";
        return result;
    }

    private static DiameterDisconnectRequest CreateDisconnectTestRequest(
        DiameterDisconnectCause cause,
        bool waitForAnswer,
        bool suppressReconnect)
    {
        DiameterDisconnectRequest result = new DiameterDisconnectRequest();
        result.PeerIdentity.Value = "peer.example";
        result.Cause = cause;
        result.TimeoutMilliseconds = 10000;
        result.WaitForAnswer = waitForAnswer;
        result.SuppressReconnect = suppressReconnect;
        return result;
    }

    private static DiameterMessage CreateDisconnectTestRequestMessage(
        DisconnectTestContext context,
        DiameterDisconnectCause cause)
    {
        DiameterBaseCommandOptions options = CreateDisconnectTestOriginOptions();
        options.DisconnectCause = cause;
        return context.Factory.CreateDisconnectPeerRequest(options);
    }

    private static DiameterMessage CreateDisconnectTestAnswer(
        DisconnectTestContext context,
        DiameterMessage request)
    {
        DiameterBaseCommandOptions options = CreateDisconnectTestOriginOptions();
        options.Request = request;
        options.ResultCode = DiameterResultCodes.Success;
        return context.Factory.CreateDisconnectPeerAnswer(options);
    }

    private static DiameterDisconnectCause ReadDisconnectTestCause(
        DiameterMessage message)
    {
        DiameterAvp avp = FindDisconnectTestAvp(
            message,
            DiameterAvpCodes.DisconnectCause);
        return (DiameterDisconnectCause)NetworkByteOrder.ReadUInt32(avp.Data);
    }

    private static DiameterAvp FindDisconnectTestAvp(
        DiameterMessage message,
        uint code)
    {
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == code && avp.VendorId == DiameterVendorIds.Ietf)
            {
                return avp;
            }

            index = index + 1;
        }

        throw new AssertionException("Expected Diameter AVP was not found.");
    }
}

internal class DisconnectTestContext
{
    internal FakeDiscoveryClock Clock = new FakeDiscoveryClock();
    internal BaseCommandTestIdentifierSource Identifiers =
        new BaseCommandTestIdentifierSource();
    internal IDiameterBaseCommandFactory Factory;
    internal PeerTable Table = null!;
    internal PeerDisconnectController Controller = null!;

    internal DisconnectTestContext()
    {
        Factory = new DiameterBaseCommandFactory(Identifiers);
    }
}
