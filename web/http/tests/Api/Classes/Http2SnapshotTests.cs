namespace http.Tests;

public class Http2SnapshotTests
{
    [Test]
    public void InspectReturnsDetachedConnectionAndStreamObjectGraphs()
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Client;
        connection.ResetHttp2Connection(options);
        connection.state.localSettings.Values.Add(CreateSetting(7u));
        connection.state.origins.Add("https://example.test");
        connection.state.sentGoAwayDebugData = new byte[] { 10, 11 };
        Http2StreamSnapshot stored =
            connection.state.streamState.CreateStream(
                1, Http2StreamState.Open, true, 3);
        stored.PushRequest = CreateDetailedRequest();
        stored.ClientPriority = CreatePriority();
        stored.Priority = CreatePriority();
        connection.state.streamState.InsertStream(stored);

        Http2ConnectionSnapshot first =
            connection.InspectHttp2Connection();
        Http2StreamSnapshot firstStream = first.Streams[0];
        HttpRequest firstRequest = firstStream.PushRequest!;
        first.LocalSettings.Values[0].Value = 99u;
        first.Origins[0] = "https://changed.test";
        first.SentGoAwayDebugData[0] = 99;
        firstStream.StreamId = 99;
        firstStream.ClientPriority!.UnknownParameters[0]
            .ByteSequenceValue[0] = 99;
        firstStream.Priority!.StructuredValue!.Members.Clear();
        firstRequest.MethodMetadata!.Method = "CHANGED";
        firstRequest.Headers.Fields[0].Value[0] = 99;
        firstRequest.WeightedPreferences[0].Parameters[0].Value[0] = 99;
        firstRequest.AuthenticationValues[0].Token68![0] = 99;
        firstRequest.ViaEntries[0].Comment![0] = 99;
        firstRequest.Priority!.UnknownParameters[0]
            .ByteSequenceValue[0] = 99;
        firstRequest.Content[0] = 99;
        firstRequest.Trailers.Fields.Clear();
        first.Streams.Clear();

        Http2ConnectionSnapshot second =
            connection.InspectHttp2Connection();
        Http2StreamSnapshot secondStream = second.Streams.Single();
        HttpRequest secondRequest = secondStream.PushRequest!;

        Assert.Multiple(() =>
        {
            Assert.That(second.LocalSettings.Values[0].Value, Is.EqualTo(7u));
            Assert.That(second.Origins.Single(),
                Is.EqualTo("https://example.test"));
            Assert.That(second.SentGoAwayDebugData[0], Is.EqualTo(10));
            Assert.That(secondStream.StreamId, Is.EqualTo(1));
            Assert.That(secondStream.ClientPriority!.UnknownParameters[0]
                .ByteSequenceValue[0], Is.EqualTo(1));
            Assert.That(secondStream.Priority!.StructuredValue!.Members,
                Has.Count.EqualTo(1));
            Assert.That(secondRequest.MethodMetadata!.Method,
                Is.EqualTo(HttpMethods.Get));
            Assert.That(secondRequest.Headers.Fields[0].Value,
                Is.EqualTo(new byte[] { 2 }));
            Assert.That(secondRequest.WeightedPreferences[0]
                .Parameters[0].Value, Is.EqualTo(new byte[] { 3 }));
            Assert.That(secondRequest.AuthenticationValues[0].Token68,
                Is.EqualTo(new byte[] { 4 }));
            Assert.That(secondRequest.ViaEntries[0].Comment,
                Is.EqualTo(new byte[] { 5 }));
            Assert.That(secondRequest.Priority!.UnknownParameters[0]
                .ByteSequenceValue, Is.EqualTo(new byte[] { 1 }));
            Assert.That(secondRequest.Content,
                Is.EqualTo(new byte[] { 6 }));
            Assert.That(secondRequest.Trailers.Fields,
                Has.Count.EqualTo(1));
            Assert.That(second.LocalSettings,
                Is.Not.SameAs(first.LocalSettings));
            Assert.That(secondStream.PushRequest,
                Is.Not.SameAs(firstStream.PushRequest));
        });
    }

    [Test]
    public void SnapshotExposesSchedulingFlowControlAndPhaseDiagnostics()
    {
        Http2Connection connection = new Http2Connection();
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        options.EndpointRole = Http2EndpointRole.Client;
        connection.ResetHttp2Connection(options);
        Http2ConnectionState state = connection.state;
        state.ready = true;
        state.bufferedInput = new byte[] { 1, 2, 3 };
        state.maximumBufferedSize = 400;
        state.pendingLocalSettings.Add(new Http2Settings());
        state.pendingLocalPings.Add(new byte[8]);
        state.gracefulGoAwayStage = 1;
        state.headerBlockState.inboundActive = true;
        state.headerBlockState.inboundStreamId = 3;
        state.headerBlockState.inboundBytes = new byte[] { 1, 2 };
        state.headerBlockState.outboundActive = true;
        state.headerBlockState.outboundStreamId = 1;
        state.headerBlockState.outboundCompressedSize = 12;
        state.streamState.nextLocalStreamId = 5;
        state.streamState.maximumLocalConcurrentStreams = 3;
        state.streamState.maximumRemoteConcurrentStreams = 4;
        state.streamState.localInitialWindowSize = 100;
        state.streamState.remoteInitialWindowSize = 200;
        state.streamState.pendingPriorities.Add(new Http2PendingPriority());
        Http2StreamSnapshot stored = state.streamState.CreateStream(
            1, Http2StreamState.Open, false, 0);
        stored.SendWindow = 0;
        stored.IsRetryEligible = true;
        state.streamState.InsertStream(stored);
        state.flowControl.receiveWindow = 90;
        state.flowControl.sendWindow = 80;
        state.flowControl.unreleasedConnectionCapacity = 10;
        state.flowControl.pendingConnectionCredit = 6;
        state.flowControl.unreleasedStreamCapacity[1] = 9;
        state.flowControl.pendingStreamCredit[1] = 5;

        Http2ConnectionSnapshot snapshot =
            connection.InspectHttp2Connection();
        Http2StreamSnapshot stream = snapshot.Streams.Single();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.IsInitialized, Is.True);
            Assert.That(snapshot.IsReady, Is.True);
            Assert.That(snapshot.BufferedInputLength, Is.EqualTo(3));
            Assert.That(snapshot.MaximumBufferedSize, Is.EqualTo(400));
            Assert.That(snapshot.PendingLocalSettingsCount, Is.EqualTo(2));
            Assert.That(snapshot.PendingLocalPingCount, Is.EqualTo(1));
            Assert.That(snapshot.InitialLocalSettingsPending, Is.True);
            Assert.That(snapshot.GracefulShutdownStage, Is.EqualTo(1));
            Assert.That(snapshot.InboundHeaderBlockActive, Is.True);
            Assert.That(snapshot.InboundHeaderBlockStreamId, Is.EqualTo(3));
            Assert.That(snapshot.InboundHeaderBlockLength, Is.EqualTo(2));
            Assert.That(snapshot.OutboundHeaderBlockActive, Is.True);
            Assert.That(snapshot.OutboundHeaderBlockStreamId, Is.EqualTo(1));
            Assert.That(snapshot.OutboundHeaderBlockLength, Is.EqualTo(12));
            Assert.That(snapshot.MaximumLocalConcurrentStreams,
                Is.EqualTo(3));
            Assert.That(snapshot.MaximumRemoteConcurrentStreams,
                Is.EqualTo(4));
            Assert.That(snapshot.LocalInitialStreamWindow, Is.EqualTo(100));
            Assert.That(snapshot.RemoteInitialStreamWindow, Is.EqualTo(200));
            Assert.That(snapshot.PendingPriorityCount, Is.EqualTo(1));
            Assert.That(snapshot.CanCreateLocalRequestStream, Is.True);
            Assert.That(snapshot.ReceiveWindow, Is.EqualTo(90));
            Assert.That(snapshot.SendWindow, Is.EqualTo(80));
            Assert.That(snapshot.UnreleasedReceiveCapacity, Is.EqualTo(10));
            Assert.That(snapshot.PendingReceiveWindowCredit, Is.EqualTo(6));
            Assert.That(snapshot.IsFlowControlBlocked, Is.False);
            Assert.That(stream.UnreleasedReceiveCapacity, Is.EqualTo(9));
            Assert.That(stream.PendingReceiveWindowCredit, Is.EqualTo(5));
            Assert.That(stream.IsFlowControlBlocked, Is.True);
            Assert.That(stream.IsRetryEligible, Is.True);
        });
    }

    static HttpRequest CreateDetailedRequest()
    {
        HttpRequest request = new HttpRequest();
        request.Method = HttpMethods.Get;
        request.MethodMetadata = new HttpMethodMetadata();
        request.MethodMetadata.Method = HttpMethods.Get;
        request.Content = new byte[] { 6 };
        request.Headers.Fields.Add(CreateField("x-test", 2));
        request.Trailers.Fields.Add(CreateField("x-trailer", 7));
        HttpWeightedValue weighted = new HttpWeightedValue();
        HttpParameter parameter = new HttpParameter();
        parameter.Value = new byte[] { 3 };
        weighted.Parameters.Add(parameter);
        request.WeightedPreferences.Add(weighted);
        request.LanguagePreferences.Add(new HttpLanguageRange());
        HttpAuthenticationValue authentication =
            new HttpAuthenticationValue();
        authentication.Token68 = new byte[] { 4 };
        request.AuthenticationValues.Add(authentication);
        HttpViaEntry via = new HttpViaEntry();
        via.Comment = new byte[] { 5 };
        request.ViaEntries.Add(via);
        request.Priority = CreatePriority();
        request.UpgradeProtocols.Add("test");
        return request;
    }

    static HttpPriority CreatePriority()
    {
        StructuredFieldMember member = new StructuredFieldMember();
        member.ByteSequenceValue = new byte[] { 1 };
        StructuredFieldParameter parameter = new StructuredFieldParameter();
        parameter.BareItem = new StructuredFieldMember();
        parameter.BareItem.ByteSequenceValue = new byte[] { 8 };
        member.Parameters.Add(parameter);
        HttpPriority priority = new HttpPriority();
        priority.UnknownParameters.Add(member);
        priority.StructuredValue = new StructuredFieldValue();
        priority.StructuredValue.Members.Add(member);
        return priority;
    }

    static Http2Setting CreateSetting(uint value)
    {
        Http2Setting setting = new Http2Setting();
        setting.Identifier = Http2SettingIdentifiers.EnablePush;
        setting.Value = value;
        return setting;
    }

    static HttpHeaderField CreateField(string name, byte value)
    {
        HttpHeaderField field = new HttpHeaderField();
        field.Name = name;
        field.Value = new byte[] { value };
        return field;
    }
}
