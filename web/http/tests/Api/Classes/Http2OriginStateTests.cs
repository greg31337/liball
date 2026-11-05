namespace http.Tests;

public class Http2OriginStateTests
{
    [Test]
    public void ApplicableFramesInitializeAndIncrementTheClientOriginSet()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.InitialOrigin = "https://primary.test";
        Http2Connection connection = CreateReadyConnection(
            options, Http2EndpointRole.Client);

        Http2ConnectionSnapshot before =
            connection.InspectHttp2Connection();
        Http2ConnectionResult first = ProcessFrame(
            connection,
            CreateOrigin(
                "https://one.test",
                "https://primary.test",
                "https://one.test"));
        Http2ConnectionResult second = ProcessFrame(
            connection,
            CreateOrigin("https://two.test:8443", "https://one.test"));

        Assert.Multiple(() =>
        {
            Assert.That(before.OriginSetInitialized, Is.False);
            Assert.That(before.Origins, Is.Empty);
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Events.Single().EventKind,
                Is.EqualTo(Http2EventKind.FrameReceived));
            Assert.That(first.Events.Single().Frame!.OriginIgnored, Is.False);
            Assert.That(first.Snapshot!.OriginSetInitialized, Is.True);
            Assert.That(first.Snapshot.Origins, Is.EqualTo(new[]
            {
                "https://primary.test",
                "https://one.test"
            }));
            Assert.That(second.Snapshot!.Origins, Is.EqualTo(new[]
            {
                "https://primary.test",
                "https://one.test",
                "https://two.test:8443"
            }));
        });

        second.Snapshot!.Origins.Clear();
        Assert.That(connection.InspectHttp2Connection().Origins,
            Has.Count.EqualTo(3));
    }

    [Test]
    public void AdviceReportsOnlyOriginSetConstraints()
    {
        Http2Connection connection = CreateReadyConnection(
            new Http2ConnectionOptions(), Http2EndpointRole.Client);

        Http2OriginAdvice baseline = connection.EvaluateHttp2Origin(
            "https://ordinary.test");
        ProcessFrame(connection, CreateOrigin("https://advertised.test"));
        Http2OriginAdvice advertised = connection.EvaluateHttp2Origin(
            "https://advertised.test");
        Http2OriginAdvice absent = connection.EvaluateHttp2Origin(
            "https://absent.test");
        Http2OriginAdvice invalid = connection.EvaluateHttp2Origin(
            "HTTPS://advertised.test");

        Assert.Multiple(() =>
        {
            Assert.That(baseline.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(baseline.Outcome,
                Is.EqualTo(Http2OriginOutcome.DefaultAuthorityRules));
            Assert.That(baseline.OriginSetInitialized, Is.False);
            Assert.That(baseline.OriginSetPermitsAuthority, Is.True);
            Assert.That(baseline.OriginSetPermitsCoalescing, Is.True);
            Assert.That(baseline.RequiresExternalAuthorityValidation,
                Is.True);
            Assert.That(baseline.RequiresExternalConnectionSelection,
                Is.True);

            Assert.That(advertised.Outcome,
                Is.EqualTo(Http2OriginOutcome.OriginSetAllows));
            Assert.That(advertised.OriginSetInitialized, Is.True);
            Assert.That(advertised.OriginInSet, Is.True);
            Assert.That(advertised.OriginSetPermitsAuthority, Is.True);
            Assert.That(advertised.OriginSetPermitsCoalescing, Is.True);
            Assert.That(advertised.RequiresExternalAuthorityValidation,
                Is.True);
            Assert.That(advertised.RequiresExternalConnectionSelection,
                Is.True);

            Assert.That(absent.Outcome,
                Is.EqualTo(Http2OriginOutcome.OriginSetRejects));
            Assert.That(absent.OriginInSet, Is.False);
            Assert.That(absent.OriginSetPermitsAuthority, Is.False);
            Assert.That(absent.OriginSetPermitsCoalescing, Is.False);
            Assert.That(absent.RequiresExternalAuthorityValidation,
                Is.False);
            Assert.That(absent.RequiresExternalConnectionSelection,
                Is.False);

            Assert.That(invalid.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(invalid.Outcome,
                Is.EqualTo(Http2OriginOutcome.NotApplicable));
            Assert.That(invalid.ErrorCodeName,
                Is.EqualTo("invalid-http2-origin"));
        });
    }

    [Test]
    public void IgnoredFramesDoNotInitializeButAnEmptyApplicableFrameDoes()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.InitialOrigin = "https://primary.test";
        Http2Connection connection = CreateReadyConnection(
            options, Http2EndpointRole.Client);
        Http2Frame wrongStream = CreateOrigin("https://ignored.test");
        wrongStream.StreamId = 3;
        Http2ConnectionResult streamResult = ProcessFrame(
            connection, wrongStream);
        Http2Frame unknownFlags = CreateOrigin("https://ignored.test");
        unknownFlags.Flags = 0x01;
        Http2ConnectionResult flagsResult = ProcessFrame(
            connection, unknownFlags);
        Http2ConnectionResult emptyResult = ProcessFrame(
            connection, CreateOrigin());

        Assert.Multiple(() =>
        {
            Assert.That(streamResult.Events.Single().Frame!.OriginIgnored,
                Is.True);
            Assert.That(streamResult.Snapshot!.OriginSetInitialized,
                Is.False);
            Assert.That(flagsResult.Events.Single().Frame!.OriginIgnored,
                Is.True);
            Assert.That(flagsResult.Snapshot!.OriginSetInitialized,
                Is.False);
            Assert.That(emptyResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(emptyResult.Events.Single().Frame!.OriginIgnored,
                Is.False);
            Assert.That(emptyResult.Snapshot!.OriginSetInitialized,
                Is.True);
            Assert.That(emptyResult.Snapshot.Origins,
                Is.EqualTo(new[] { "https://primary.test" }));
        });
    }

    [Test]
    public void DisabledOrServerSideProcessingIgnoresInboundOriginFrames()
    {
        Http2ConnectionOptions disabledOptions = new Http2ConnectionOptions();
        disabledOptions.EnableOrigin = false;
        disabledOptions.InitialOrigin = "https://primary.test";
        Http2Connection disabled = CreateReadyConnection(
            disabledOptions, Http2EndpointRole.Client);
        Http2ConnectionResult disabledResult = ProcessFrame(
            disabled, CreateOrigin("https://advertised.test"));
        Http2OriginAdvice disabledAdvice = disabled.EvaluateHttp2Origin(
            "https://advertised.test");

        Http2Connection server = CreateReadyConnection(
            new Http2ConnectionOptions(), Http2EndpointRole.Server);
        Http2ConnectionResult serverResult = ProcessFrame(
            server, CreateOrigin("https://advertised.test"));

        Assert.Multiple(() =>
        {
            Assert.That(disabledResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(disabledResult.Events.Single().Frame!.OriginIgnored,
                Is.True);
            Assert.That(disabledResult.Snapshot!.OriginSetInitialized,
                Is.False);
            Assert.That(disabledResult.Snapshot.Origins, Is.Empty);
            Assert.That(disabledAdvice.Status,
                Is.EqualTo(HttpOperationStatus.Unsupported));
            Assert.That(disabledAdvice.Outcome,
                Is.EqualTo(Http2OriginOutcome.NotApplicable));
            Assert.That(serverResult.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serverResult.Events.Single().Frame!.OriginIgnored,
                Is.True);
            Assert.That(serverResult.Snapshot!.OriginSetInitialized,
                Is.False);
        });
    }

    [Test]
    public void OnlyEnabledServersCanQueueOriginFrames()
    {
        Http2Connection server = CreateReadyConnection(
            new Http2ConnectionOptions(), Http2EndpointRole.Server);
        Http2ConnectionResult queued = QueueFrame(
            server, CreateOrigin("https://advertised.test"));

        Http2Connection client = CreateReadyConnection(
            new Http2ConnectionOptions(), Http2EndpointRole.Client);
        Http2ConnectionResult clientRejected = QueueFrame(
            client, CreateOrigin("https://advertised.test"));

        Http2ConnectionOptions disabledOptions = new Http2ConnectionOptions();
        disabledOptions.EnableOrigin = false;
        Http2Connection disabledServer = CreateReadyConnection(
            disabledOptions, Http2EndpointRole.Server);
        Http2ConnectionResult disabledRejected = QueueFrame(
            disabledServer, CreateOrigin("https://advertised.test"));

        Assert.Multiple(() =>
        {
            Assert.That(queued.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(queued.Actions.Single().Frame!.Origins,
                Is.EqualTo(new[] { "https://advertised.test" }));
            Assert.That(clientRejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(clientRejected.ErrorCodeName,
                Is.EqualTo("http2-client-cannot-send-origin"));
            Assert.That(disabledRejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(disabledRejected.ErrorCodeName,
                Is.EqualTo("http2-origin-disabled"));
        });
    }

    [Test]
    public void ResetRejectsANonCanonicalInitialOrigin()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Client;
        options.InitialOrigin = "https://example.test:443";

        Http2ConnectionResult result =
            new Http2Connection().ResetHttp2Connection(options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCodeName,
                Is.EqualTo("invalid-http2-connection-options"));
        });
    }

    static Http2Connection CreateReadyConnection(
        Http2ConnectionOptions options,
        Http2EndpointRole role)
    {
        options.EndpointRole = role;
        Http2Connection connection = new Http2Connection();
        Assert.That(connection.ResetHttp2Connection(options).Status,
            Is.EqualTo(HttpOperationStatus.Done));

        Http2ConnectionInput input = new Http2ConnectionInput();
        input.Frame = CreateSettings();
        if (role == Http2EndpointRole.Server)
        {
            input.Frame = null;
            input.HasBytes = true;
            input.Bytes = Concat(
                Http2Constants.ClientConnectionPreface,
                new byte[] { 0, 0, 0, 4, 0, 0, 0, 0, 0 });
        }

        Assert.That(connection.ProcessHttp2Input(input).IsReady, Is.True);
        return connection;
    }

    static Http2ConnectionResult ProcessFrame(
        Http2Connection connection,
        Http2Frame frame)
    {
        Http2ConnectionInput input = new Http2ConnectionInput();
        input.Frame = frame;
        return connection.ProcessHttp2Input(input);
    }

    static Http2ConnectionResult QueueFrame(
        Http2Connection connection,
        Http2Frame frame)
    {
        Http2Action action = new Http2Action();
        action.ActionKind = Http2ActionKind.SendFrame;
        action.Frame = frame;
        return connection.QueueHttp2Action(action);
    }

    static Http2Frame CreateSettings()
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
        return frame;
    }

    static Http2Frame CreateOrigin(params string[] origins)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Origin;
        frame.Origins.AddRange(origins);
        return frame;
    }

    static byte[] Concat(byte[] left, byte[] right)
    {
        byte[] result = new byte[left.Length + right.Length];
        left.CopyTo(result, 0);
        right.CopyTo(result, left.Length);
        return result;
    }
}
