namespace sip.Tests;

public class SipTransportContentCodingTests
{
    [Test]
    public void EncodesOutgoingBytesWithDetachedSigCompMetadata()
    {
        ContentCodingTestTransport transport = new ContentCodingTestTransport();
        ContentCodingTestCodec codec = new ContentCodingTestCodec();
        SipTransportManager manager = new SipTransportManager();
        Assert.That(manager.RegisterTransportContentCoding(codec), Is.True);
        manager.RegisterTransport(transport);
        SipSendRequest request = SendRequest(new byte[] { 1, 2, 3 });
        request.ContentCoding = Metadata("compartment-a");
        request.ContentCoding.SigCompCompartment!.RemoteCapability = Capability();

        SipSendResult result = manager.SendManaged(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(transport.LastRequest!.Message, Is.Null);
            Assert.That(transport.LastRequest.Bytes,
                Is.EqualTo(new byte[] { 0xF8, 1, 2, 3 }));
            Assert.That(codec.LastEncodeRequest!.Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(codec.LastEncodeRequest.Metadata!.SigCompCompartment!.Id,
                Is.EqualTo("compartment-a"));
            Assert.That(codec.LastEncodeRequest.Metadata.SigCompCompartment
                .RemoteCapability!.Version, Is.EqualTo(2));
            Assert.That(result.ContentCoding!.Name,
                Is.EqualTo(SipContentCodings.SigComp));
            Assert.That(request.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(request.ContentCoding.SigCompCompartment.Id,
                Is.EqualTo("compartment-a"));
        });
    }

    [Test]
    public void SerializesMessageBeforeCallingExternalContentCoding()
    {
        ContentCodingTestTransport transport = new ContentCodingTestTransport();
        ContentCodingTestCodec codec = new ContentCodingTestCodec();
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransportContentCoding(codec);
        manager.RegisterTransport(transport);
        SipSendRequest request = SendRequest(new byte[0]);
        request.Message = Parse(MessageText());
        request.ContentCoding = Metadata("compartment-message");

        SipSendResult result = manager.SendManaged(request);
        string encoded = System.Text.Encoding.UTF8.GetString(
            codec.LastEncodeRequest!.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(encoded, Does.StartWith("OPTIONS sip:bob@example.com SIP/2.0\r\n"));
            Assert.That(encoded, Does.EndWith("Content-Length: 0\r\n\r\n"));
            Assert.That(request.Message, Is.Not.Null);
            Assert.That(request.Bytes, Is.Empty);
        });
    }

    [Test]
    public void DecodesAndParsesIncomingContentCodedMessage()
    {
        ContentCodingTestTransport transport = new ContentCodingTestTransport();
        ContentCodingTestCodec codec = new ContentCodingTestCodec();
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransportContentCoding(codec);
        manager.RegisterTransport(transport);
        byte[] plain = System.Text.Encoding.UTF8.GetBytes(MessageText());
        byte[] encoded = new byte[plain.Length + 1];
        encoded[0] = 0xF8;
        System.Array.Copy(plain, 0, encoded, 1, plain.Length);
        SipTransportEvent incoming = new SipTransportEvent();
        incoming.Kind = SipTransportEventKind.MessageReceived;
        incoming.Transport = SipTransportKind.Custom;
        incoming.Message = new SipTransportMessage();
        incoming.Message.Bytes = encoded;
        incoming.Message.ContentCoding = Metadata("compartment-incoming");
        incoming.Message.CorrelationId = "incoming-1";
        transport.Events.Add(incoming);

        SipTransportEvent received = manager.PollManagedTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(received.Kind,
                Is.EqualTo(SipTransportEventKind.MessageReceived));
            Assert.That(received.Message!.Bytes, Is.EqualTo(plain));
            Assert.That(received.Message.ParseResult!.Status,
                Is.EqualTo(SipParseStatus.Complete));
            Assert.That(received.Message.Message!.Method,
                Is.EqualTo(SipMethod.Options));
            Assert.That(received.Message.ContentCoding!.SigCompCompartment!.Id,
                Is.EqualTo("compartment-incoming"));
            Assert.That(codec.LastDecodeRequest!.CorrelationId,
                Is.EqualTo("incoming-1"));
        });
    }

    [Test]
    public void ReportsMissingAndFailedContentCodingsWithoutSending()
    {
        ContentCodingTestTransport transport = new ContentCodingTestTransport();
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransport(transport);
        SipSendRequest missing = SendRequest(new byte[] { 1 });
        missing.ContentCoding = Metadata("missing");

        SipSendResult missingResult = manager.SendManaged(missing);
        SipTransportEvent missingEvent = manager.PollManagedTransportEvent();
        ContentCodingTestCodec codec = new ContentCodingTestCodec();
        codec.FailEncoding = true;
        manager.RegisterTransportContentCoding(codec);
        SipSendRequest failed = SendRequest(new byte[] { 2 });
        failed.ContentCoding = Metadata("failed");
        SipSendResult failedResult = manager.SendManaged(failed);
        SipTransportEvent failedEvent = manager.PollManagedTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Success, Is.False);
            Assert.That(missingResult.ErrorMessage,
                Does.Contain("No registered transport content coding"));
            Assert.That(missingEvent.Kind,
                Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(failedResult.Success, Is.False);
            Assert.That(failedResult.ErrorMessage, Is.EqualTo("encode failed"));
            Assert.That(failedEvent.Kind,
                Is.EqualTo(SipTransportEventKind.SendFailed));
            Assert.That(transport.SendCount, Is.Zero);
        });
    }

    [Test]
    public void ReportsIncomingDecodeFailureAndPreservesEncodedBytes()
    {
        ContentCodingTestTransport transport = new ContentCodingTestTransport();
        ContentCodingTestCodec codec = new ContentCodingTestCodec();
        codec.FailDecoding = true;
        SipTransportManager manager = new SipTransportManager();
        manager.RegisterTransportContentCoding(codec);
        manager.RegisterTransport(transport);
        SipTransportEvent incoming = new SipTransportEvent();
        incoming.Kind = SipTransportEventKind.MessageReceived;
        incoming.Message = new SipTransportMessage();
        incoming.Message.Bytes = new byte[] { 0xF8, 1, 2 };
        incoming.Message.ContentCoding = Metadata("failed-decode");
        transport.Events.Add(incoming);

        SipTransportEvent result = manager.PollManagedTransportEvent();

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind,
                Is.EqualTo(SipTransportEventKind.ContentCodingFailed));
            Assert.That(result.ErrorMessage, Is.EqualTo("decode failed"));
            Assert.That(result.Message!.Bytes,
                Is.EqualTo(new byte[] { 0xF8, 1, 2 }));
            Assert.That(result.Message.Message, Is.Null);
        });
    }

    [Test]
    public void RegistersOneCodingNameAndClosesDetachedCompartment()
    {
        ContentCodingTestCodec first = new ContentCodingTestCodec();
        ContentCodingTestCodec duplicateName = new ContentCodingTestCodec();
        SipTransportManager manager = new SipTransportManager();
        SipContentCodingMetadata metadata = Metadata("close-me");
        metadata.SigCompCompartment!.Closed = false;

        bool registered = manager.RegisterTransportContentCoding(first);
        bool duplicate = manager.RegisterTransportContentCoding(duplicateName);
        bool closed = manager.CloseTransportContentCodingCompartment(metadata);
        bool removed = manager.RemoveTransportContentCoding(first);

        Assert.Multiple(() =>
        {
            Assert.That(registered, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(closed, Is.True);
            Assert.That(first.ClosedCompartment!.Id, Is.EqualTo("close-me"));
            Assert.That(first.ClosedCompartment.Closed, Is.True);
            Assert.That(metadata.SigCompCompartment.Closed, Is.False);
            Assert.That(removed, Is.True);
            Assert.That(manager.CloseTransportContentCodingCompartment(metadata),
                Is.False);
        });
    }

    [Test]
    public void AggregatesSigCompAsAnExtensionContentCodingCapability()
    {
        SipExtensionRegistry registry = new SipExtensionRegistry();
        registry.RegisterExtension(new ContentCodingTestExtension(
            new string[] { "SIGCOMP", "future-coding" }));
        registry.RegisterExtension(new ContentCodingTestExtension(
            new string[] { "sigcomp" }));

        SipCapabilitySet capabilities = registry.GetRegisteredCapabilities();

        Assert.Multiple(() =>
        {
            Assert.That(capabilities.ContentCodings,
                Is.EqualTo(new string[] { "SIGCOMP", "future-coding" }));
            Assert.That(registry.SupportsContentCoding("sigcomp"), Is.True);
            Assert.That(registry.SupportsContentCoding("bad coding"), Is.False);
            Assert.That(SipContentCodings.SigComp, Is.EqualTo("sigcomp"));
        });
    }

    static SipSendRequest SendRequest(byte[] bytes)
    {
        SipSendRequest request = new SipSendRequest();
        request.Bytes = bytes;
        request.Transport = SipTransportKind.Custom;
        request.RemoteAddress = new SipEndpointAddress();
        request.RemoteAddress.Host = "192.0.2.10";
        request.RemoteAddress.Port = 5060;
        request.RemoteAddress.Transport = SipTransportKind.Custom;
        request.CorrelationId = "coding-test";
        return request;
    }

    static SipContentCodingMetadata Metadata(string compartmentId)
    {
        SipContentCodingMetadata metadata = new SipContentCodingMetadata();
        metadata.Name = SipContentCodings.SigComp;
        metadata.SigCompCompartment = new SipSigCompCompartment();
        metadata.SigCompCompartment.Id = compartmentId;
        metadata.SigCompCompartment.Transport = SipTransportKind.Custom;
        return metadata;
    }

    static SipSigCompCapability Capability()
    {
        SipSigCompCapability capability = new SipSigCompCapability();
        capability.Version = 2;
        capability.NegativeAcknowledgements = true;
        capability.StateMemorySize = 8192;
        capability.CyclesPerBit = 64;
        capability.DecompressionMemorySize = 16384;
        capability.StateIdentifiers =
            new byte[][] { new byte[] { 1, 2, 3 } };
        return capability;
    }

    static SipMessage Parse(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Message!;
    }

    static string MessageText()
    {
        return "OPTIONS sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bKcoding\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: coding@example.com\r\n"
            + "CSeq: 1 OPTIONS\r\n"
            + "Max-Forwards: 70\r\n"
            + "Content-Length: 0\r\n\r\n";
    }
}

class ContentCodingTestCodec : ISipTransportContentCoding
{
    public bool FailEncoding;
    public bool FailDecoding;
    public SipContentCodingRequest? LastEncodeRequest;
    public SipContentCodingRequest? LastDecodeRequest;
    public SipSigCompCompartment? ClosedCompartment;

    public string GetTransportContentCodingName()
    {
        return SipContentCodings.SigComp;
    }

    public SipContentCodingResult EncodeTransportContent(
        SipContentCodingRequest request)
    {
        LastEncodeRequest = request;
        SipContentCodingResult result = new SipContentCodingResult();
        if (FailEncoding)
        {
            result.ErrorMessage = "encode failed";
            return result;
        }
        result.Success = true;
        result.Bytes = new byte[request.Bytes.Length + 1];
        result.Bytes[0] = 0xF8;
        System.Array.Copy(request.Bytes, 0, result.Bytes, 1,
            request.Bytes.Length);
        result.Metadata = request.Metadata;
        return result;
    }

    public SipContentCodingResult DecodeTransportContent(
        SipContentCodingRequest request)
    {
        LastDecodeRequest = request;
        SipContentCodingResult result = new SipContentCodingResult();
        if (FailDecoding)
        {
            result.ErrorMessage = "decode failed";
            return result;
        }
        if (request.Bytes.Length == 0 || request.Bytes[0] != 0xF8)
        {
            result.ErrorMessage = "marker missing";
            return result;
        }
        result.Success = true;
        result.Bytes = new byte[request.Bytes.Length - 1];
        System.Array.Copy(request.Bytes, 1, result.Bytes, 0,
            result.Bytes.Length);
        result.Metadata = request.Metadata;
        return result;
    }

    public void CloseTransportContentCodingCompartment(
        SipSigCompCompartment compartment)
    {
        ClosedCompartment = compartment;
    }
}

class ContentCodingTestTransport : ISipTransport
{
    public int SendCount;
    public SipSendRequest? LastRequest;
    public List<SipTransportEvent> Events = new List<SipTransportEvent>();

    public SipEndpointAddress BindTransport(SipTransportOptions options,
        SipTlsOptions tlsOptions, SipWebSocketOptions webSocketOptions)
    {
        return new SipEndpointAddress();
    }

    public SipConnectionInfo ConnectTransport(SipEndpointAddress remoteAddress)
    {
        return new SipConnectionInfo();
    }

    public SipSendResult SendTransport(SipSendRequest request)
    {
        SendCount++;
        LastRequest = request;
        SipSendResult result = new SipSendResult();
        result.Success = true;
        result.BytesSent = request.Bytes.Length;
        return result;
    }

    public SipTransportEvent PollTransportEvent()
    {
        if (Events.Count == 0)
        {
            return new SipTransportEvent();
        }
        SipTransportEvent result = Events[0];
        Events.RemoveAt(0);
        return result;
    }

    public void CloseTransport()
    {
    }
}

class ContentCodingTestExtension : ISipExtension
{
    public string[] ContentCodings;

    public ContentCodingTestExtension(string[] contentCodings)
    {
        ContentCodings = contentCodings;
    }

    public SipCapabilitySet GetExtensionCapabilities()
    {
        SipCapabilitySet result = new SipCapabilitySet();
        result.ContentCodings = ContentCodings;
        return result;
    }

    public SipExtensionResult ProcessExtensionRequest(SipMessage message)
    {
        return new SipExtensionResult();
    }
}
