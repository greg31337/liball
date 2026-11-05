using System.Text.Json;
using htmlgui;

namespace htmlgui.Tests;

public class BridgeCodecValidationCoverageTests
{
    [TestCase(UiBridgeMessageKind.Initialize, "initialize")]
    [TestCase(UiBridgeMessageKind.Ready, "ready")]
    [TestCase(UiBridgeMessageKind.RenderSnapshot, "renderSnapshot")]
    [TestCase(UiBridgeMessageKind.DomPatch, "domPatch")]
    [TestCase(UiBridgeMessageKind.Event, "event")]
    [TestCase(UiBridgeMessageKind.Command, "command")]
    [TestCase(UiBridgeMessageKind.StateMutation, "stateMutation")]
    [TestCase(UiBridgeMessageKind.Validation, "validation")]
    [TestCase(UiBridgeMessageKind.Navigation, "navigation")]
    [TestCase(UiBridgeMessageKind.Focus, "focus")]
    [TestCase(UiBridgeMessageKind.Measurement, "measurement")]
    [TestCase(UiBridgeMessageKind.VirtualizationRange, "virtualizationRange")]
    [TestCase(UiBridgeMessageKind.VirtualizationResult, "virtualizationResult")]
    [TestCase(UiBridgeMessageKind.Lifecycle, "lifecycle")]
    [TestCase(UiBridgeMessageKind.Log, "log")]
    [TestCase(UiBridgeMessageKind.Diagnostic, "diagnostic")]
    [TestCase(UiBridgeMessageKind.HostCall, "hostCall")]
    [TestCase(UiBridgeMessageKind.HostResult, "hostResult")]
    [TestCase(UiBridgeMessageKind.Request, "request")]
    [TestCase(UiBridgeMessageKind.Response, "response")]
    [TestCase(UiBridgeMessageKind.Error, "error")]
    [TestCase(UiBridgeMessageKind.Cancel, "cancel")]
    [TestCase(UiBridgeMessageKind.Acknowledgement, "acknowledgement")]
    [TestCase(UiBridgeMessageKind.Heartbeat, "heartbeat")]
    [TestCase(UiBridgeMessageKind.Shutdown, "shutdown")]
    [TestCase(UiBridgeMessageKind.Chunk, "chunk")]
    public void RoundTripsAndValidatesEveryBridgeMessageKind(UiBridgeMessageKind kind, string token)
    {
        BridgeCodec codec = new BridgeCodec();
        UiBridgeEnvelope source = ValidEnvelope(kind);

        string encoded = codec.SerializeBridgeEnvelope(source, 16384);
        UiBridgeEnvelope decoded = codec.DeserializeBridgeEnvelope(encoded, 16384);
        BridgeValidationResult validation = new BridgeValidator().ValidateBridgeEnvelope(decoded, Options(), "session-all-kinds", true, -1);
        using JsonDocument json = JsonDocument.Parse(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(json.RootElement.GetProperty("kind").GetString(), Is.EqualTo(token));
            Assert.That(decoded.Kind, Is.EqualTo(kind));
            Assert.That(validation.Success, Is.True, validation.Code);
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("{")]
    [TestCase("[]")]
    [TestCase("{\"version\":\"1\",\"session\":\"s\",\"sequence\":0,\"origin\":\"host\",\"kind\":\"heartbeat\"}")]
    [TestCase("{\"version\":1,\"session\":null,\"sequence\":0,\"origin\":\"host\",\"kind\":\"heartbeat\"}")]
    [TestCase("{\"version\":1,\"session\":\"s\",\"sequence\":0.5,\"origin\":\"host\",\"kind\":\"heartbeat\"}")]
    [TestCase("{\"version\":1,\"session\":\"s\",\"sequence\":0,\"correlationId\":{},\"origin\":\"host\",\"kind\":\"cancel\"}")]
    [TestCase("{\"version\":1,\"session\":\"s\",\"sequence\":0,\"origin\":0,\"kind\":\"heartbeat\"}")]
    [TestCase("{\"version\":1,\"session\":\"s\",\"sequence\":0,\"origin\":\"host\",\"kind\":\"event\",\"payload\":{\"kind\":\"string\",\"value\":1}}")]
    [TestCase("{\"version\":1,\"session\":\"s\",\"sequence\":0,\"correlationId\":\"c\",\"origin\":\"host\",\"kind\":\"request\",\"request\":{\"correlationId\":\"c\",\"operation\":\"op\",\"arguments\":[],\"timeoutMilliseconds\":1}}")]
    [TestCase("{\"version\":1,\"session\":\"s\",\"sequence\":0,\"correlationId\":\"c\",\"origin\":\"host\",\"kind\":\"response\",\"response\":{\"correlationId\":\"c\",\"success\":\"true\"}}")]
    [TestCase("{\"version\":1,\"session\":\"s\",\"sequence\":0,\"origin\":\"host\",\"kind\":\"error\",\"error\":{\"errorCode\":\"bad\",\"errorMessage\":\"bad\",\"retryable\":\"false\"}}")]
    public void RejectsMalformedJsonAndSchemas(string json)
    {
        Assert.That(() => new BridgeCodec().DeserializeBridgeEnvelope(json, 16384), Throws.Exception);
    }

    [Test]
    public void RejectsVersionCapabilityOriginFrameSessionAndSequenceMismatches()
    {
        BridgeValidator validator = new BridgeValidator();
        UiBridgeOptions options = Options();
        UiBridgeEnvelope unsupportedVersion = ValidEnvelope(UiBridgeMessageKind.Heartbeat);
        unsupportedVersion.Version = BridgeProtocolVersions.Current + 1;
        UiBridgeEnvelope wrongOrigin = ValidEnvelope(UiBridgeMessageKind.Heartbeat);
        wrongOrigin.Origin = BridgeProtocolNames.BrowserOrigin;
        UiBridgeEnvelope valid = ValidEnvelope(UiBridgeMessageKind.Heartbeat);
        UiBridgeOptions framesAllowed = Options();
        framesAllowed.RequireMainFrame = false;

        CapabilityNegotiator negotiator = new CapabilityNegotiator();
        UiTransportCapabilities local = Capabilities(new UiCapability { Name = "stream", IsSupported = true, Version = "1" });
        UiTransportCapabilities remote = Capabilities(new UiCapability { Name = "stream", IsSupported = true, Version = "2" });
        CapabilityNegotiationResult capabilityMismatch = negotiator.NegotiateBridgeCapabilities(local, remote);
        UiTransportCapabilities unsupportedCapabilities = Capabilities();
        unsupportedCapabilities.Version = BridgeProtocolVersions.Current + 1;

        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateBridgeEnvelope(unsupportedVersion, options, "session-all-kinds", true, -1).Code, Is.EqualTo("bridge.unsupported-version"));
            Assert.That(validator.ValidateBridgeEnvelope(wrongOrigin, options, "session-all-kinds", true, -1).Code, Is.EqualTo("bridge.origin-mismatch"));
            Assert.That(validator.ValidateBridgeEnvelope(valid, options, "different-session", true, -1).Code, Is.EqualTo("bridge.session-mismatch"));
            Assert.That(validator.ValidateBridgeEnvelope(valid, options, "session-all-kinds", false, -1).Code, Is.EqualTo("bridge.frame-rejected"));
            Assert.That(validator.ValidateBridgeEnvelope(valid, options, "session-all-kinds", true, valid.Sequence).Code, Is.EqualTo("bridge.sequence-rejected"));
            Assert.That(validator.ValidateBridgeEnvelope(valid, options, "session-all-kinds", true, -2).Code, Is.EqualTo("bridge.invalid-last-sequence"));
            Assert.That(validator.ValidateBridgeEnvelope(valid, framesAllowed, "session-all-kinds", false, -1).Success, Is.True);
            Assert.That(capabilityMismatch.Success, Is.True);
            Assert.That(capabilityMismatch.Capabilities.Capabilities[0].IsSupported, Is.False);
            Assert.That(negotiator.ValidateBridgeCapabilities(unsupportedCapabilities).Code, Is.EqualTo("bridge.unsupported-capability-version"));
        });
    }

    [Test]
    public void EnforcesExactDepthCollectionTextBinaryAndMessageLimits()
    {
        BridgeValidator validator = new BridgeValidator();
        UiBridgeEnvelope exactDepth = PayloadEnvelope(DeepValue(32));
        UiBridgeEnvelope excessiveDepth = PayloadEnvelope(DeepValue(33));
        UiBridgeEnvelope exactCollection = PayloadEnvelope(ArrayValue(4096));
        UiBridgeEnvelope excessiveCollection = PayloadEnvelope(ArrayValue(4097));
        UiBridgeEnvelope exactText = PayloadEnvelope(Text(new string('x', 64)));
        UiBridgeEnvelope excessiveText = PayloadEnvelope(Text(new string('x', 65)));
        UiBridgeEnvelope exactBinary = PayloadEnvelope(new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[64] });
        UiBridgeEnvelope excessiveBinary = PayloadEnvelope(new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[65] });
        BridgeCodec codec = new BridgeCodec();
        string encoded = codec.SerializeBridgeEnvelope(PayloadEnvelope(Text("size")), 16384);
        int encodedLength = System.Text.Encoding.UTF8.GetByteCount(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateBridgePayload(exactDepth, 16384).Success, Is.True);
            Assert.That(validator.ValidateBridgePayload(excessiveDepth, 16384).Code, Is.EqualTo("bridge.payload-depth-exceeded"));
            Assert.That(validator.ValidateBridgePayload(exactCollection, 16384).Success, Is.True);
            Assert.That(validator.ValidateBridgePayload(excessiveCollection, 16384).Code, Is.EqualTo("bridge.payload-count-exceeded"));
            Assert.That(validator.ValidateBridgePayload(exactText, 64).Success, Is.True);
            Assert.That(validator.ValidateBridgePayload(excessiveText, 64).Code, Is.EqualTo("bridge.invalid-value"));
            Assert.That(validator.ValidateBridgePayload(exactBinary, 64).Success, Is.True);
            Assert.That(validator.ValidateBridgePayload(excessiveBinary, 64).Code, Is.EqualTo("bridge.invalid-value"));
            Assert.That(() => codec.DeserializeBridgeEnvelope(encoded, encodedLength - 1), Throws.ArgumentException);
            Assert.That(codec.DeserializeBridgeEnvelope(encoded, encodedLength).Payload!.StringValue, Is.EqualTo("size"));
        });
    }

    private static UiBridgeEnvelope ValidEnvelope(UiBridgeMessageKind kind)
    {
        UiBridgeEnvelope envelope = new UiBridgeEnvelope
        {
            Version = BridgeProtocolVersions.Current,
            Session = "session-all-kinds",
            Sequence = 1,
            Origin = BridgeProtocolNames.HostOrigin,
            Kind = kind
        };
        if (kind == UiBridgeMessageKind.Request || kind == UiBridgeMessageKind.HostCall)
        {
            envelope.CorrelationId = "correlation";
            envelope.Request = new UiBridgeRequest
            {
                CorrelationId = "correlation",
                Operation = "operation",
                Arguments = new UiValueEntry[] { new UiValueEntry { Key = "argument", Value = Text("value") } },
                TimeoutMilliseconds = 1000
            };
        }
        else if (kind == UiBridgeMessageKind.Response || kind == UiBridgeMessageKind.HostResult)
        {
            envelope.CorrelationId = "correlation";
            envelope.Response = new UiBridgeResponse { CorrelationId = "correlation", Success = true, Value = Text("value") };
        }
        else if (kind == UiBridgeMessageKind.Error)
        {
            envelope.CorrelationId = "correlation";
            envelope.Error = new UiBridgeError { Code = "failed", Message = "Failed", CorrelationId = "correlation" };
        }
        else if (kind == UiBridgeMessageKind.Cancel)
        {
            envelope.CorrelationId = "correlation";
        }

        return envelope;
    }

    private static UiBridgeEnvelope PayloadEnvelope(UiValue payload)
    {
        UiBridgeEnvelope envelope = ValidEnvelope(UiBridgeMessageKind.Event);
        envelope.Payload = payload;
        return envelope;
    }

    private static UiBridgeOptions Options()
    {
        return new UiBridgeOptions
        {
            Version = BridgeProtocolVersions.Current,
            Origin = BridgeProtocolNames.HostOrigin,
            RequireMainFrame = true,
            MaximumMessageLength = 16384,
            MaximumPendingRequests = 8,
            TimeoutMilliseconds = 1000
        };
    }

    private static UiTransportCapabilities Capabilities(params UiCapability[] capabilities)
    {
        return new UiTransportCapabilities
        {
            State = UiTransportState.Starting,
            Version = BridgeProtocolVersions.Current,
            Capabilities = capabilities
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue ArrayValue(int count)
    {
        UiValue[] values = new UiValue[count];
        int index = 0;
        while (index < count)
        {
            values[index] = new UiValue();
            index++;
        }

        return new UiValue { Kind = UiValueKind.Array, ArrayValue = values };
    }

    private static UiValue DeepValue(int depth)
    {
        UiValue value = new UiValue();
        int index = 0;
        while (index < depth)
        {
            value = new UiValue { Kind = UiValueKind.Array, ArrayValue = new UiValue[] { value } };
            index++;
        }

        return value;
    }
}
