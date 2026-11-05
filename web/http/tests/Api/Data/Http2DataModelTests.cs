using System.Text;

namespace http.Tests;

public class Http2DataModelTests
{
    [Test]
    public void SettingsRetainOrderedKnownAndUnknownWireValues()
    {
        Http2Setting known = new Http2Setting();
        Http2Setting unknown = new Http2Setting();
        Http2Settings settings = new Http2Settings();

        known.Identifier = Http2SettingIdentifiers.InitialWindowSize;
        known.Value = 100000u;
        known.IsKnown = true;
        known.IsValid = true;
        unknown.Identifier = 0xf0f0;
        unknown.Value = 0xf0000001u;
        settings.Values.Add(known);
        settings.Values.Add(unknown);
        settings.HasInitialWindowSize = true;
        settings.InitialWindowSize = 100000;
        settings.HasMaxHeaderListSize = true;
        settings.MaxHeaderListSize = 200000u;
        settings.HasMaxFrameSize = true;
        settings.MaxFrameSize = 20000;
        settings.HasEnableConnectProtocol = true;
        settings.EnableConnectProtocol = true;
        settings.HasNoRfc7540Priorities = true;
        settings.NoRfc7540Priorities = true;

        Assert.Multiple(() =>
        {
            Assert.That(settings.Values, Is.EqualTo(new[] { known, unknown }));
            Assert.That(known.Value, Is.EqualTo(100000u));
            Assert.That(unknown.IsKnown, Is.False);
            Assert.That(unknown.Value, Is.EqualTo(0xf0000001u));
            Assert.That(settings.HasInitialWindowSize, Is.True);
            Assert.That(settings.InitialWindowSize, Is.EqualTo(100000));
            Assert.That(settings.MaxHeaderListSize, Is.EqualTo(200000u));
            Assert.That(settings.HasMaxFrameSize, Is.True);
            Assert.That(settings.MaxFrameSize, Is.EqualTo(20000));
            Assert.That(settings.EnableConnectProtocol, Is.True);
            Assert.That(settings.HasNoRfc7540Priorities, Is.True);
            Assert.That(settings.NoRfc7540Priorities, Is.True);
        });
    }

    [Test]
    public void SettingsExposeRfcDefaultsWithAbsentPresenceFlags()
    {
        Http2Settings settings = new Http2Settings();

        Assert.Multiple(() =>
        {
            Assert.That(settings.Values, Is.Empty);
            Assert.That(settings.HasHeaderTableSize, Is.False);
            Assert.That(settings.HeaderTableSize, Is.EqualTo(Http2Constants.DefaultHeaderTableSize));
            Assert.That(settings.HasEnablePush, Is.False);
            Assert.That(settings.EnablePush, Is.True);
            Assert.That(settings.HasMaxConcurrentStreams, Is.False);
            Assert.That(settings.HasInitialWindowSize, Is.False);
            Assert.That(settings.InitialWindowSize, Is.EqualTo(Http2Constants.DefaultInitialWindowSize));
            Assert.That(settings.HasMaxFrameSize, Is.False);
            Assert.That(settings.MaxFrameSize, Is.EqualTo(Http2Constants.DefaultMaximumFrameSize));
            Assert.That(settings.HasMaxHeaderListSize, Is.False);
            Assert.That(settings.HasEnableConnectProtocol, Is.False);
            Assert.That(settings.EnableConnectProtocol, Is.False);
            Assert.That(settings.HasNoRfc7540Priorities, Is.False);
            Assert.That(settings.NoRfc7540Priorities, Is.False);
        });
    }

    [Test]
    public void FrameRetainsRawHeaderAndTypedPayloadMetadata()
    {
        Http2Frame frame = new Http2Frame();
        Http2Settings settings = new Http2Settings();

        frame.Length = 12;
        frame.FrameType = Http2FrameType.Headers;
        frame.TypeCode = 0x01;
        frame.IsKnownType = true;
        frame.Flags = 0x2d;
        frame.ReservedBit = true;
        frame.StreamId = 3;
        frame.Payload = [1, 2, 3];
        frame.Data = [4, 5];
        frame.HeaderBlockFragment = [0x82, 0x84];
        frame.EndStream = true;
        frame.EndHeaders = true;
        frame.IsPadded = true;
        frame.PaddingLength = 2;
        frame.FlowControlledLength = 12;
        frame.HasPriority = true;
        frame.ExclusiveDependency = true;
        frame.StreamDependency = 1;
        frame.Weight = 256;
        frame.HasErrorCode = true;
        frame.ErrorCode = Http2ErrorCode.Cancel;
        frame.ErrorCodeValue = 0x08u;
        frame.IsKnownErrorCode = true;
        frame.Settings = settings;
        frame.Acknowledgement = true;
        frame.HasPromisedStreamId = true;
        frame.PromisedStreamId = 4;
        frame.OpaqueData = [1, 2, 3, 4, 5, 6, 7, 8];
        frame.HasLastStreamId = true;
        frame.LastStreamId = 3;
        frame.DebugData = Encoding.ASCII.GetBytes("debug");
        frame.HasWindowIncrement = true;
        frame.WindowIncrement = 1024;
        frame.HasPrioritizedElementId = true;
        frame.PrioritizedElementId = 7;
        frame.PriorityFieldValue = Encoding.ASCII.GetBytes("u=1, i");
        frame.Priority = new HttpPriority();
        frame.Origins.Add("https://example.test");

        Assert.Multiple(() =>
        {
            Assert.That(frame.Length, Is.EqualTo(12));
            Assert.That(frame.TypeCode, Is.EqualTo(0x01));
            Assert.That(frame.Flags, Is.EqualTo(0x2d));
            Assert.That(frame.ReservedBit, Is.True);
            Assert.That(frame.StreamId, Is.EqualTo(3));
            Assert.That(frame.Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(frame.HeaderBlockFragment, Is.EqualTo(new byte[] { 0x82, 0x84 }));
            Assert.That(frame.EndStream, Is.True);
            Assert.That(frame.EndHeaders, Is.True);
            Assert.That(frame.PaddingLength, Is.EqualTo(2));
            Assert.That(frame.StreamDependency, Is.EqualTo(1));
            Assert.That(frame.Weight, Is.EqualTo(256));
            Assert.That(frame.ErrorCodeValue, Is.EqualTo(0x08u));
            Assert.That(frame.Settings, Is.SameAs(settings));
            Assert.That(frame.OpaqueData, Has.Length.EqualTo(8));
            Assert.That(frame.WindowIncrement, Is.EqualTo(1024));
            Assert.That(frame.PriorityFieldValue, Is.EqualTo(Encoding.ASCII.GetBytes("u=1, i")));
            Assert.That(frame.Priority, Is.Not.Null);
            Assert.That(frame.Origins, Is.EqualTo(new[] { "https://example.test" }));
        });
    }

    [Test]
    public void FrameResultRetainsIncrementalProgressOutputAndErrors()
    {
        Http2FrameResult result = new Http2FrameResult();
        Http2Frame frame = new Http2Frame();
        byte[] bytes = [0, 0, 0, 4, 0, 0, 0, 0, 0];

        result.Status = HttpOperationStatus.NeedOutput;
        result.Consumed = 9;
        result.Written = 4;
        result.RequiredSize = 9;
        result.Frame = frame;
        result.Bytes = bytes;
        result.HasErrorPosition = true;
        result.ErrorPosition = 3;
        result.ErrorCode = "frame-size";
        result.ErrorMessage = "invalid payload length";

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(result.Consumed, Is.EqualTo(9));
            Assert.That(result.Written, Is.EqualTo(4));
            Assert.That(result.RequiredSize, Is.EqualTo(9));
            Assert.That(result.Frame, Is.SameAs(frame));
            Assert.That(result.Bytes, Is.SameAs(bytes));
            Assert.That(result.ErrorPosition, Is.EqualTo(3));
            Assert.That(result.ErrorCode, Is.EqualTo("frame-size"));
        });
    }

    [Test]
    public void ConnectionOptionsAndInputRetainTransportIndependentOperations()
    {
        Http2ConnectionOptions options = new Http2ConnectionOptions();
        Http2ConnectionInput input = new Http2ConnectionInput();
        Http2Frame frame = new Http2Frame();
        Http2Action action = new Http2Action();
        HttpRequest request = new HttpRequest();

        options.EndpointRole = Http2EndpointRole.Server;
        options.AllowServerPush = false;
        options.PreserveUnknownFrames = false;
        options.EnablePriorityUpdate = false;
        options.EnableExtendedConnect = false;
        options.MaximumRetainedClosedStreams = 12;
        input.HasBytes = true;
        input.Bytes = [1, 2, 3];
        input.Frame = frame;
        input.Action = action;
        input.Request = request;
        input.HasStreamId = true;
        input.StreamId = 5;
        input.EndStream = true;
        input.EndOfInput = true;

        Assert.Multiple(() =>
        {
            Assert.That(options.EndpointRole, Is.EqualTo(Http2EndpointRole.Server));
            Assert.That(options.LocalSettings, Is.Not.Null);
            Assert.That(options.EncoderOptions, Is.Not.Null);
            Assert.That(options.DecoderOptions, Is.Not.Null);
            Assert.That(options.Limits, Is.Not.Null);
            Assert.That(options.AllowServerPush, Is.False);
            Assert.That(options.PreserveUnknownFrames, Is.False);
            Assert.That(options.EnablePriorityUpdate, Is.False);
            Assert.That(options.EnableExtendedConnect, Is.False);
            Assert.That(options.MaximumRetainedClosedStreams, Is.EqualTo(12));
            Assert.That(input.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(input.Frame, Is.SameAs(frame));
            Assert.That(input.Action, Is.SameAs(action));
            Assert.That(input.Request, Is.SameAs(request));
            Assert.That(input.HasStreamId, Is.True);
            Assert.That(input.StreamId, Is.EqualTo(5));
            Assert.That(input.EndStream, Is.True);
            Assert.That(input.EndOfInput, Is.True);
        });
    }

    [Test]
    public void MessageEventAndActionRetainApplicationAndProtocolData()
    {
        Http2MessageResult message = new Http2MessageResult();
        Http2Event http2Event = new Http2Event();
        Http2Action action = new Http2Action();
        HttpResponse response = new HttpResponse();
        HttpHeaders trailers = new HttpHeaders();

        message.Status = HttpOperationStatus.Done;
        message.StreamId = 9;
        message.MessageKind = HttpMessageKind.Response;
        message.Response = response;
        message.Trailers = trailers;
        message.EndStream = true;
        message.IsComplete = true;
        http2Event.EventKind = Http2EventKind.StreamComplete;
        http2Event.HasStreamId = true;
        http2Event.StreamId = 9;
        http2Event.MessageKind = HttpMessageKind.Response;
        http2Event.Response = response;
        http2Event.Content = [0x80];
        http2Event.Trailers = trailers;
        http2Event.Message = message;
        http2Event.PingData = [1, 2, 3, 4, 5, 6, 7, 8];
        action.ActionKind = Http2ActionKind.ResetStream;
        action.Message = message;
        action.HasStreamId = true;
        action.StreamId = 9;
        action.Capacity = 1024;
        action.HasErrorCode = true;
        action.ErrorCode = Http2ErrorCode.Cancel;
        action.ErrorCodeValue = 8u;
        action.IsKnownErrorCode = true;
        action.OpaqueData = Encoding.ASCII.GetBytes("cancelled");

        Assert.Multiple(() =>
        {
            Assert.That(message.Response, Is.SameAs(response));
            Assert.That(message.Trailers, Is.SameAs(trailers));
            Assert.That(message.IsComplete, Is.True);
            Assert.That(http2Event.StreamId, Is.EqualTo(9));
            Assert.That(http2Event.Content, Is.EqualTo(new byte[] { 0x80 }));
            Assert.That(http2Event.Message, Is.SameAs(message));
            Assert.That(http2Event.PingData, Has.Length.EqualTo(8));
            Assert.That(action.Message, Is.SameAs(message));
            Assert.That(action.StreamId, Is.EqualTo(9));
            Assert.That(action.Capacity, Is.EqualTo(1024));
            Assert.That(action.ErrorCode, Is.EqualTo(Http2ErrorCode.Cancel));
            Assert.That(action.OpaqueData, Is.EqualTo(Encoding.ASCII.GetBytes("cancelled")));
        });
    }

    [Test]
    public void StreamAndConnectionSnapshotsRetainFlowStateAndShutdownMetadata()
    {
        Http2StreamSnapshot stream = new Http2StreamSnapshot();
        Http2ConnectionSnapshot connection = new Http2ConnectionSnapshot();

        stream.StreamId = 11;
        stream.StreamState = Http2StreamState.HalfClosedRemote;
        stream.ReceiveWindow = 100;
        stream.SendWindow = -10;
        stream.ReceivedHeaders = true;
        stream.SentHeaders = true;
        stream.RemoteEndStream = true;
        stream.ReceivedContentLength = 1000;
        stream.IsPushed = true;
        stream.HasAssociatedStreamId = true;
        stream.AssociatedStreamId = 1;
        stream.IsRetryEligible = true;
        stream.Priority = new HttpPriority();
        connection.EndpointRole = Http2EndpointRole.Client;
        connection.ReceiveWindow = 200;
        connection.SendWindow = 300;
        connection.NextLocalStreamId = 13;
        connection.HighestRemoteStreamId = 10;
        connection.ActiveLocalStreams = 2;
        connection.ActiveRemoteStreams = 1;
        connection.SentPreface = true;
        connection.ReceivedPreface = true;
        connection.ReceivedInitialSettings = true;
        connection.InitialSettingsAcknowledged = true;
        connection.SentGoAway = true;
        connection.HasSentLastStreamId = true;
        connection.SentLastStreamId = 11;
        connection.HasSentGoAwayErrorCode = true;
        connection.SentGoAwayErrorCode = Http2ErrorCode.Cancel;
        connection.SentGoAwayErrorCodeValue = 0x08u;
        connection.IsKnownSentGoAwayErrorCode = true;
        connection.ReceivedGoAway = true;
        connection.HasReceivedLastStreamId = true;
        connection.ReceivedLastStreamId = 9;
        connection.HasReceivedGoAwayErrorCode = true;
        connection.ReceivedGoAwayErrorCode = Http2ErrorCode.Unknown;
        connection.ReceivedGoAwayErrorCodeValue = 0xf0000001u;
        connection.IsKnownReceivedGoAwayErrorCode = false;
        connection.Streams.Add(stream);

        Assert.Multiple(() =>
        {
            Assert.That(stream.SendWindow, Is.EqualTo(-10));
            Assert.That(stream.StreamState, Is.EqualTo(Http2StreamState.HalfClosedRemote));
            Assert.That(stream.ReceivedContentLength, Is.EqualTo(1000));
            Assert.That(stream.SentHeaders, Is.True);
            Assert.That(stream.AssociatedStreamId, Is.EqualTo(1));
            Assert.That(stream.IsRetryEligible, Is.True);
            Assert.That(connection.SendWindow, Is.EqualTo(300));
            Assert.That(connection.NextLocalStreamId, Is.EqualTo(13));
            Assert.That(connection.SentPreface, Is.True);
            Assert.That(connection.InitialSettingsAcknowledged, Is.True);
            Assert.That(connection.SentLastStreamId, Is.EqualTo(11));
            Assert.That(connection.HasSentGoAwayErrorCode, Is.True);
            Assert.That(connection.SentGoAwayErrorCodeValue,
                Is.EqualTo(0x08u));
            Assert.That(connection.IsKnownSentGoAwayErrorCode, Is.True);
            Assert.That(connection.HasReceivedLastStreamId, Is.True);
            Assert.That(connection.ReceivedLastStreamId, Is.EqualTo(9));
            Assert.That(connection.HasReceivedGoAwayErrorCode, Is.True);
            Assert.That(connection.ReceivedGoAwayErrorCodeValue,
                Is.EqualTo(0xf0000001u));
            Assert.That(connection.IsKnownReceivedGoAwayErrorCode, Is.False);
            Assert.That(connection.Streams.Single(), Is.SameAs(stream));
        });
    }

    [Test]
    public void ConnectionResultRetainsOrderedEventsActionsSnapshotAndRawErrors()
    {
        Http2ConnectionResult result = new Http2ConnectionResult();
        Http2ConnectionInput input = new Http2ConnectionInput();
        Http2Event http2Event = new Http2Event();
        Http2Action action = new Http2Action();
        Http2ConnectionSnapshot snapshot = new Http2ConnectionSnapshot();

        result.Status = HttpOperationStatus.Terminal;
        result.Consumed = 24;
        result.Input = input;
        result.Events.Add(http2Event);
        result.Actions.Add(action);
        result.Snapshot = snapshot;
        result.IsReady = false;
        result.IsTerminal = true;
        result.ErrorScope = Http2ErrorScope.Connection;
        result.HasErrorCode = true;
        result.ErrorCode = Http2ErrorCode.Unknown;
        result.ErrorCodeValue = 0xf0000001u;
        result.IsKnownErrorCode = false;
        result.ErrorCodeName = "extension-error";
        result.ErrorMessage = "peer sent an extension error";

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(result.Consumed, Is.EqualTo(24));
            Assert.That(result.Input, Is.SameAs(input));
            Assert.That(result.Events.Single(), Is.SameAs(http2Event));
            Assert.That(result.Actions.Single(), Is.SameAs(action));
            Assert.That(result.Snapshot, Is.SameAs(snapshot));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.ErrorScope, Is.EqualTo(Http2ErrorScope.Connection));
            Assert.That(result.ErrorCode, Is.EqualTo(Http2ErrorCode.Unknown));
            Assert.That(result.ErrorCodeValue, Is.EqualTo(0xf0000001u));
            Assert.That(result.IsKnownErrorCode, Is.False);
        });
    }
}
