using System;
using System.Text.Json;
using htmlgui;

namespace htmlgui.Tests;

public class BridgeCodecTests
{
    [Test]
    public void SerializesRequestsCanonicallyAndRoundTripsTypedArguments()
    {
        UiBridgeEnvelope envelope = RequestEnvelope();
        BridgeCodec codec = new BridgeCodec();

        string first = codec.SerializeBridgeEnvelope(envelope, 4096);
        UiValueEntry firstArgument = envelope.Request!.Arguments[0];
        envelope.Request.Arguments[0] = envelope.Request.Arguments[1];
        envelope.Request.Arguments[1] = firstArgument;
        string reordered = codec.SerializeBridgeEnvelope(envelope, 4096);
        UiBridgeEnvelope decoded = codec.DeserializeBridgeEnvelope(first, 4096);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("{\"version\":1,\"session\":\"session-1\",\"sequence\":7,\"correlationId\":\"correlation-1\",\"origin\":\"host\",\"kind\":\"request\",\"request\":{\"correlationId\":\"correlation-1\",\"operation\":\"lookup\",\"arguments\":{\"a\":{\"kind\":\"integer\",\"value\":2},\"z\":{\"kind\":\"string\",\"value\":\"last\"}},\"timeoutMilliseconds\":1000}}"));
            Assert.That(reordered, Is.EqualTo(first));
            Assert.That(decoded, Is.Not.SameAs(envelope));
            Assert.That(decoded.Kind, Is.EqualTo(UiBridgeMessageKind.Request));
            Assert.That(decoded.Request, Is.Not.Null);
            Assert.That(decoded.Request!.Operation, Is.EqualTo("lookup"));
            Assert.That(decoded.Request.Arguments[0].Key, Is.EqualTo("a"));
            Assert.That(decoded.Request.Arguments[0].Value.IntegerValue, Is.EqualTo(2));
            Assert.That(decoded.Request.Arguments[1].Key, Is.EqualTo("z"));
            Assert.That(decoded.Request.Arguments[1].Value.StringValue, Is.EqualTo("last"));
        });
    }

    [Test]
    public void RoundTripsPortablePayloadResponsesAndErrors()
    {
        BridgeCodec codec = new BridgeCodec();
        UiBridgeEnvelope payloadEnvelope = Envelope(UiBridgeMessageKind.Event, 1);
        payloadEnvelope.Payload = new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[]
            {
                new UiValueEntry { Key = "text", Value = Text("hello") },
                new UiValueEntry { Key = "enabled", Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true } }
            }
        };
        UiBridgeEnvelope responseEnvelope = Envelope(UiBridgeMessageKind.Response, 2);
        responseEnvelope.CorrelationId = "response-1";
        responseEnvelope.Response = new UiBridgeResponse
        {
            CorrelationId = "response-1",
            Success = true,
            Value = Text("complete")
        };
        UiBridgeEnvelope errorEnvelope = Envelope(UiBridgeMessageKind.Error, 3);
        errorEnvelope.CorrelationId = "error-1";
        errorEnvelope.Error = new UiBridgeError
        {
            Code = "host.failed",
            Message = "Failed",
            CorrelationId = "error-1",
            IsRetryable = true
        };

        UiBridgeEnvelope payload = codec.DeserializeBridgeEnvelope(codec.SerializeBridgeEnvelope(payloadEnvelope, 4096), 4096);
        UiBridgeEnvelope response = codec.DeserializeBridgeEnvelope(codec.SerializeBridgeEnvelope(responseEnvelope, 4096), 4096);
        UiBridgeEnvelope error = codec.DeserializeBridgeEnvelope(codec.SerializeBridgeEnvelope(errorEnvelope, 4096), 4096);

        Assert.Multiple(() =>
        {
            Assert.That(payload.Payload!.ObjectValue![0].Key, Is.EqualTo("enabled"));
            Assert.That(payload.Payload.ObjectValue[0].Value.BooleanValue, Is.True);
            Assert.That(payload.Payload.ObjectValue[1].Key, Is.EqualTo("text"));
            Assert.That(response.Response!.Success, Is.True);
            Assert.That(response.Response.Value!.StringValue, Is.EqualTo("complete"));
            Assert.That(error.Error!.Code, Is.EqualTo("host.failed"));
            Assert.That(error.Error.CorrelationId, Is.EqualTo("error-1"));
            Assert.That(error.Error.IsRetryable, Is.True);
        });
    }

    [TestCase("{\"version\":1,\"session\":\"s\",\"sequence\":0,\"origin\":\"host\",\"kind\":\"heartbeat\",\"extra\":true}")]
    [TestCase("{\"version\":1,\"version\":1,\"session\":\"s\",\"sequence\":0,\"origin\":\"host\",\"kind\":\"heartbeat\"}")]
    [TestCase("{\"version\":1,\"session\":\"s\",\"origin\":\"host\",\"kind\":\"heartbeat\"}")]
    [TestCase("{\"version\":1,\"session\":\"s\",\"sequence\":0,\"origin\":\"host\",\"kind\":\"unknown\"}")]
    [TestCase("{\"version\":1,\"session\":\"s\",\"sequence\":0,\"origin\":\"host\",\"kind\":\"heartbeat\",\"payload\":null}")]
    public void RejectsUnknownDuplicateMissingAndMalformedEnvelopeMembers(string json)
    {
        Assert.That(() => new BridgeCodec().DeserializeBridgeEnvelope(json, 4096), Throws.TypeOf<JsonException>());
    }

    [Test]
    public void RejectsMalformedTypedPayloadsAndAmbiguousBodies()
    {
        string missingRequestMember = "{\"version\":1,\"session\":\"s\",\"sequence\":0,\"correlationId\":\"c\",\"origin\":\"host\",\"kind\":\"request\"}";
        string unknownRequestMember = "{\"version\":1,\"session\":\"s\",\"sequence\":0,\"correlationId\":\"c\",\"origin\":\"host\",\"kind\":\"request\",\"request\":{\"correlationId\":\"c\",\"operation\":\"op\",\"arguments\":{},\"timeoutMilliseconds\":1,\"extra\":0}}";
        string ambiguous = "{\"version\":1,\"session\":\"s\",\"sequence\":0,\"correlationId\":\"c\",\"origin\":\"host\",\"kind\":\"request\",\"payload\":{\"kind\":\"null\"},\"request\":{\"correlationId\":\"c\",\"operation\":\"op\",\"arguments\":{},\"timeoutMilliseconds\":1}}";
        string mismatchedCorrelation = "{\"version\":1,\"session\":\"s\",\"sequence\":0,\"correlationId\":\"outer\",\"origin\":\"host\",\"kind\":\"request\",\"request\":{\"correlationId\":\"inner\",\"operation\":\"op\",\"arguments\":{},\"timeoutMilliseconds\":1}}";
        BridgeCodec codec = new BridgeCodec();

        Assert.Multiple(() =>
        {
            Assert.That(() => codec.DeserializeBridgeEnvelope(missingRequestMember, 4096), Throws.TypeOf<JsonException>());
            Assert.That(() => codec.DeserializeBridgeEnvelope(unknownRequestMember, 4096), Throws.TypeOf<JsonException>());
            Assert.That(() => codec.DeserializeBridgeEnvelope(ambiguous, 4096), Throws.TypeOf<JsonException>());
            Assert.That(() => codec.DeserializeBridgeEnvelope(mismatchedCorrelation, 4096), Throws.TypeOf<JsonException>());
        });
    }

    [Test]
    public void EnforcesUtf8MessageLengthOnEncodingAndDecoding()
    {
        UiBridgeEnvelope envelope = Envelope(UiBridgeMessageKind.Log, 1);
        envelope.Payload = Text("four-byte: \U0001F600");
        BridgeCodec codec = new BridgeCodec();
        string json = codec.SerializeBridgeEnvelope(envelope, 4096);
        int byteLength = System.Text.Encoding.UTF8.GetByteCount(json);

        Assert.Multiple(() =>
        {
            Assert.That(() => codec.SerializeBridgeEnvelope(envelope, byteLength - 1), Throws.ArgumentException);
            Assert.That(() => codec.DeserializeBridgeEnvelope(json, byteLength - 1), Throws.ArgumentException);
            Assert.That(codec.DeserializeBridgeEnvelope(json, byteLength).Payload!.StringValue, Is.EqualTo("four-byte: \U0001F600"));
            Assert.That(() => codec.DeserializeBridgeEnvelope(json, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static UiBridgeEnvelope RequestEnvelope()
    {
        UiBridgeEnvelope envelope = Envelope(UiBridgeMessageKind.Request, 7);
        envelope.CorrelationId = "correlation-1";
        envelope.Request = new UiBridgeRequest
        {
            CorrelationId = "correlation-1",
            Operation = "lookup",
            Arguments = new UiValueEntry[]
            {
                new UiValueEntry { Key = "z", Value = Text("last") },
                new UiValueEntry { Key = "a", Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = 2 } }
            },
            TimeoutMilliseconds = 1000
        };
        return envelope;
    }

    private static UiBridgeEnvelope Envelope(UiBridgeMessageKind kind, int sequence)
    {
        return new UiBridgeEnvelope
        {
            Version = BridgeProtocolVersions.Current,
            Session = "session-1",
            Sequence = sequence,
            Origin = BridgeProtocolNames.HostOrigin,
            Kind = kind
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
