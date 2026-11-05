using System.Text;

namespace diameter.Tests;

public class DiameterOverloadAdmissionIntegrationTests
{
    [Test]
    public void ApplicationAdmissionRejectsBeforeQueueingWithTooBusy()
    {
        DiameterApplicationRegistration registration =
            CreateApplicationRegistration();
        ApplicationWorkQueue queue = new ApplicationWorkQueue(registration);
        AdmissionTestOverloadManager overload =
            new AdmissionTestOverloadManager();
        overload.ThrottleAll = true;
        queue.ConfigureDiameterOverloadAdmission(overload);

        ApplicationWorkAdmission admission = queue.EnqueueApplicationWork(
            new AdmissionTestRequestHandler(),
            CreateApplicationContext(registration),
            new AdmissionTestCompletionHandler());
        ApplicationWorkQueueSnapshot snapshot =
            queue.ReadApplicationWorkQueueSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(admission.Accepted, Is.False);
            Assert.That(admission.Overloaded, Is.True);
            Assert.That(admission.RejectionResult, Is.Not.Null);
            Assert.That(admission.RejectionResult!.ResultCode,
                Is.EqualTo(DiameterResultCodes.TooBusy));
            Assert.That(admission.RejectionResult.Code,
                Is.EqualTo("test-overload-throttle"));
            Assert.That(snapshot.ActiveCount, Is.Zero);
            Assert.That(snapshot.QueuedCount, Is.Zero);
            Assert.That(snapshot.RejectedCount, Is.EqualTo(1L));
            Assert.That(overload.EvaluationCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void OriginatedRequestIsRejectedBeforePendingTrackingOrTransport()
    {
        RequestClientPeerManager peers = new RequestClientPeerManager();
        peers.AddAvailablePeer("first.example", 4U);
        AdmissionTestRouter router = new AdmissionTestRouter();
        router.Peers.Add("first.example");
        DiameterRequestClient client = CreateRequestClient(router, peers);
        AdmissionTestOverloadManager overload =
            new AdmissionTestOverloadManager();
        overload.ThrottleAll = true;
        client.ConfigureDiameterOverloadAdmission(overload);

        DiameterSendResult result = client.SendDiameterRequest(
            CreateSendRequest("first.example", 300U, 4U),
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DiameterSendStatus.Overloaded));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Code, Is.EqualTo("test-overload-throttle"));
            Assert.That(peers.SentMessages, Is.Empty);
            Assert.That(client.ReadPendingDiameterRequests(), Is.Empty);
            Assert.That(overload.EvaluatedPeers,
                Is.EqualTo(new[] { "first.example" }));
        });
    }

    [Test]
    public void RoutingAndFailoverDivertAroundOverloadedCandidates()
    {
        RequestClientPeerManager peers = new RequestClientPeerManager();
        peers.AddAvailablePeer("first.example", 4U);
        peers.AddAvailablePeer("second.example", 4U);
        peers.AddAvailablePeer("third.example", 4U);
        AdmissionTestRouter router = new AdmissionTestRouter();
        router.Peers.Add("second.example");
        router.Peers.Add("third.example");
        DiameterRequestClient client = CreateRequestClient(router, peers);
        AdmissionTestOverloadManager overload =
            new AdmissionTestOverloadManager();
        client.ConfigureDiameterOverloadAdmission(overload);
        DiameterSendResult first = client.SendDiameterRequest(
            CreateSendRequest("first.example", 300U, 4U),
            null);
        overload.DivertPeers.Add("second.example");

        peers.PublishTransportFailure("first.example");
        DiameterPendingRequestSnapshot pending =
            client.ReadPendingDiameterRequests()[0];

        Assert.Multiple(() =>
        {
            Assert.That(first.Accepted, Is.True);
            Assert.That(peers.SentPeers,
                Is.EqualTo(new[] { "first.example", "third.example" }));
            Assert.That(router.SelectedPeers,
                Is.EqualTo(new[] { "second.example", "third.example" }));
            Assert.That(router.Requests[1].ExcludedPeers.Select(
                peer => peer.Value),
                Does.Contain("second.example"));
            Assert.That(pending.PeerIdentity.Value,
                Is.EqualTo("third.example"));
            Assert.That(pending.AttemptCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void WatchdogBypassesOverloadEvaluationAndStillSends()
    {
        RequestClientPeerManager peers = new RequestClientPeerManager();
        peers.AddAvailablePeer("first.example", DiameterApplicationIds.Common);
        AdmissionTestRouter router = new AdmissionTestRouter();
        DiameterRequestClient client = CreateRequestClient(router, peers);
        AdmissionTestOverloadManager overload =
            new AdmissionTestOverloadManager();
        overload.ThrottleAll = true;
        client.ConfigureDiameterOverloadAdmission(overload);

        DiameterSendResult result = client.SendDiameterRequest(
            CreateSendRequest(
                "first.example",
                DiameterCommandCodes.DeviceWatchdog,
                DiameterApplicationIds.Common),
            null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DiameterSendStatus.Accepted));
            Assert.That(result.Delivered, Is.True);
            Assert.That(peers.SentPeers,
                Is.EqualTo(new[] { "first.example" }));
            Assert.That(overload.EvaluationCount, Is.Zero);
        });
    }

    private static DiameterRequestClient CreateRequestClient(
        IDiameterRouter router,
        RequestClientPeerManager peers)
    {
        DiameterNodeConfiguration configuration =
            new DiameterNodeConfiguration();
        configuration.OriginHost.Value = "client.example";
        configuration.OriginRealm.Value = "example";
        configuration.ProductName = "overload-admission-tests";
        configuration.Roles = DiameterNodeRole.Client;
        configuration.Security.AllowUnsecuredTransport = true;
        return new DiameterRequestClient(
            configuration,
            router,
            peers,
            new DiameterMessageCodec(),
            new RequestClientIdentifierSource(),
            new FakeDiscoveryClock());
    }

    private static DiameterSendRequest CreateSendRequest(
        string peer,
        uint commandCode,
        uint applicationId)
    {
        DiameterSendRequest result = new DiameterSendRequest();
        result.Request.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        result.Request.CommandCode = commandCode;
        result.Request.ApplicationId = applicationId;
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationRealm,
            "example"));
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationHost,
            "server.example"));
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.OriginHost,
            "client.example"));
        result.PreferredPeer = CreateIdentity(peer);
        return result;
    }

    private static DiameterApplicationRegistration
        CreateApplicationRegistration()
    {
        DiameterApplicationRegistration result =
            new DiameterApplicationRegistration();
        result.Name = "overload-queue";
        result.ApplicationId = 4U;
        DiameterCommandRegistration command =
            new DiameterCommandRegistration();
        command.CommandCode = 300U;
        command.Name = "Overload-Queue";
        command.RequireValidation = false;
        result.Commands.Add(command);
        return result;
    }

    private static DiameterRequestContext CreateApplicationContext(
        DiameterApplicationRegistration registration)
    {
        DiameterRequestContext result = new DiameterRequestContext();
        result.Registration = registration;
        result.Command = registration.Commands[0];
        result.Request.Flags = DiameterMessageFlags.Request;
        result.Request.CommandCode = result.Command.CommandCode;
        result.Request.ApplicationId = registration.ApplicationId;
        result.Request.Avps.Add(CreateTextAvp(
            DiameterAvpCodes.DestinationRealm,
            "example"));
        result.PeerIdentity.Value = "peer.example";
        result.RouteIdentifier = "route-a";
        result.ReceivedAt = DateTimeOffset.UtcNow;
        return result;
    }

    private static DiameterAvp CreateTextAvp(uint code, string value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = Encoding.UTF8.GetBytes(value);
        return result;
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}

internal class AdmissionTestOverloadManager : IDiameterOverloadManager
{
    internal bool ThrottleAll;
    internal List<string> DivertPeers = new List<string>();
    internal List<string> EvaluatedPeers = new List<string>();
    internal int EvaluationCount;

    public DiameterAdmissionDecision EvaluateDiameterAdmission(
        DiameterAdmissionRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentException("request");
        }

        EvaluationCount = EvaluationCount + 1;
        string peer = request.Candidate == null ||
            request.Candidate.PeerIdentity == null ?
            string.Empty : request.Candidate.PeerIdentity.Value;
        EvaluatedPeers.Add(peer);
        DiameterAdmissionDecision result =
            new DiameterAdmissionDecision();
        result.Request = DiameterOverloadState.CloneDiameterAdmissionRequest(
            request);
        if (ThrottleAll)
        {
            result.Action = DiameterAdmissionAction.Throttle;
            result.Admitted = false;
            result.OverloadApplied = true;
            result.ResultCode = DiameterResultCodes.TooBusy;
            result.Code = "test-overload-throttle";
            result.Message = "The test overload policy throttled the request.";
        }
        else if (DivertPeers.Contains(peer))
        {
            result.Action = DiameterAdmissionAction.Divert;
            result.Admitted = false;
            result.OverloadApplied = true;
            result.DiversionCandidate =
                RouteTableCloner.CloneRouteCandidate(request.Candidate!);
            result.Code = "test-overload-divert";
            result.Message = "The test overload policy diverted the request.";
        }
        else
        {
            result.Code = "test-overload-admit";
            result.Message = "The test overload policy admitted the request.";
        }

        return result;
    }

    public DiameterOverloadReport PublishDiameterOverloadReport(
        DiameterAdmissionRequest? request)
    {
        return new DiameterOverloadReport();
    }

    public DiameterOverloadReport ProcessDiameterOverloadReport(
        DiameterOverloadReport? report,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt)
    {
        return report == null ? new DiameterOverloadReport() : report;
    }

    public DiameterLoadReport PublishDiameterLoadReport(
        DiameterLoadSource source)
    {
        return new DiameterLoadReport();
    }

    public DiameterLoadReport ProcessDiameterLoadReport(
        DiameterLoadReport? report,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt)
    {
        return report == null ? new DiameterLoadReport() : report;
    }

    public List<DiameterOverloadReport> ReadDiameterOverloadReports()
    {
        return new List<DiameterOverloadReport>();
    }

    public List<DiameterLoadReport> ReadDiameterLoadReports()
    {
        return new List<DiameterLoadReport>();
    }
}

internal class AdmissionTestRouter : IDiameterRouter
{
    internal List<string> Peers = new List<string>();
    internal List<string> SelectedPeers = new List<string>();
    internal List<DiameterRouteRequest> Requests =
        new List<DiameterRouteRequest>();

    public DiameterRouteDecision SelectDiameterRoute(
        DiameterRouteRequest? request)
    {
        DiameterRouteDecision result = new DiameterRouteDecision();
        if (request == null)
        {
            return result;
        }

        Requests.Add(RouteTableCloner.CloneRouteRequest(request));
        int index = 0;
        while (index < Peers.Count)
        {
            if (!ContainsPeer(request.ExcludedPeers, Peers[index]))
            {
                result.Request = RouteTableCloner.CloneRouteRequest(request);
                result.Routed = true;
                result.RoutedMessage =
                    RouteTableCloner.CloneRouteMessage(request.Message);
                result.SelectedCandidate = new DiameterRouteCandidate();
                result.SelectedCandidate.PeerIdentity =
                    new DiameterIdentity();
                result.SelectedCandidate.PeerIdentity.Value = Peers[index];
                SelectedPeers.Add(Peers[index]);
                return result;
            }

            index = index + 1;
        }

        result.Request = RouteTableCloner.CloneRouteRequest(request);
        result.Code = "test-no-route";
        result.Message = "No test route remained.";
        return result;
    }

    public List<DiameterRedirectEntry> ProcessDiameterRedirectAnswer(
        DiameterRouteRequest? request,
        DiameterMessage? answer,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt)
    {
        return new List<DiameterRedirectEntry>();
    }

    public List<DiameterRouteSnapshot> ReadDiameterRouteSnapshots()
    {
        return new List<DiameterRouteSnapshot>();
    }

    private bool ContainsPeer(
        List<DiameterIdentity> excluded,
        string peer)
    {
        int index = 0;
        while (index < excluded.Count)
        {
            if (string.Equals(
                excluded[index].Value,
                peer,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}

internal class AdmissionTestRequestHandler : IDiameterRequestHandler
{
    public DiameterApplicationResult HandleDiameterApplicationRequest(
        DiameterRequestContext context)
    {
        return new DiameterApplicationResult();
    }
}

internal class AdmissionTestCompletionHandler :
    IApplicationWorkCompletionHandler
{
    public void CompleteApplicationWork(ApplicationWorkCompletion completion)
    {
    }
}
