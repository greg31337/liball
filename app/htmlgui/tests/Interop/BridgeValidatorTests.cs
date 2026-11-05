using System;
using htmlgui;

namespace htmlgui.Tests;

public class BridgeValidatorTests
{
    [Test]
    public void AcceptsMatchingProtocolSessionOriginFrameSequenceAndCorrelation()
    {
        UiBridgeEnvelope envelope = RequestEnvelope();
        UiBridgeOptions options = Options();

        BridgeValidationResult result = new BridgeValidator().ValidateBridgeEnvelope(envelope, options, "session-1", true, 6);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Code, Is.Empty);
            Assert.That(result.Message, Is.Empty);
        });
    }

    [Test]
    public void RejectsProtocolSessionOriginFrameAndSequenceMismatches()
    {
        BridgeValidator validator = new BridgeValidator();
        UiBridgeOptions options = Options();
        UiBridgeEnvelope unsupported = RequestEnvelope();
        unsupported.Version = BridgeProtocolVersions.Current + 1;
        UiBridgeEnvelope wrongSession = RequestEnvelope();
        UiBridgeEnvelope wrongOrigin = RequestEnvelope();
        wrongOrigin.Origin = BridgeProtocolNames.BrowserOrigin;
        UiBridgeEnvelope stale = RequestEnvelope();

        BridgeValidationResult versionResult = validator.ValidateBridgeEnvelope(unsupported, options, "session-1", true, -1);
        BridgeValidationResult sessionResult = validator.ValidateBridgeEnvelope(wrongSession, options, "other-session", true, -1);
        BridgeValidationResult originResult = validator.ValidateBridgeEnvelope(wrongOrigin, options, "session-1", true, -1);
        BridgeValidationResult frameResult = validator.ValidateBridgeEnvelope(RequestEnvelope(), options, "session-1", false, -1);
        BridgeValidationResult sequenceResult = validator.ValidateBridgeEnvelope(stale, options, "session-1", true, 7);

        Assert.Multiple(() =>
        {
            Assert.That(versionResult.Code, Is.EqualTo("bridge.unsupported-version"));
            Assert.That(sessionResult.Code, Is.EqualTo("bridge.session-mismatch"));
            Assert.That(originResult.Code, Is.EqualTo("bridge.origin-mismatch"));
            Assert.That(frameResult.Code, Is.EqualTo("bridge.frame-rejected"));
            Assert.That(sequenceResult.Code, Is.EqualTo("bridge.sequence-rejected"));
        });
    }

    [Test]
    public void EnforcesTypedBodyAndCorrelationRules()
    {
        BridgeValidator validator = new BridgeValidator();
        UiBridgeEnvelope missingRequest = Envelope(UiBridgeMessageKind.Request);
        missingRequest.CorrelationId = "c";
        UiBridgeEnvelope mismatch = RequestEnvelope();
        mismatch.Request!.CorrelationId = "other";
        UiBridgeEnvelope cancel = Envelope(UiBridgeMessageKind.Cancel);
        UiBridgeEnvelope failedResponse = Envelope(UiBridgeMessageKind.Response);
        failedResponse.CorrelationId = "c";
        failedResponse.Response = new UiBridgeResponse { CorrelationId = "c" };
        UiBridgeEnvelope successfulError = Envelope(UiBridgeMessageKind.Response);
        successfulError.CorrelationId = "c";
        successfulError.Response = new UiBridgeResponse
        {
            CorrelationId = "c",
            Success = true,
            Error = Error("c")
        };

        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateBridgePayload(missingRequest, 4096).Code, Is.EqualTo("bridge.missing-request"));
            Assert.That(validator.ValidateBridgePayload(mismatch, 4096).Code, Is.EqualTo("bridge.correlation-mismatch"));
            Assert.That(validator.ValidateBridgePayload(cancel, 4096).Code, Is.EqualTo("bridge.missing-correlation"));
            Assert.That(validator.ValidateBridgePayload(failedResponse, 4096).Code, Is.EqualTo("bridge.invalid-response"));
            Assert.That(validator.ValidateBridgePayload(successfulError, 4096).Code, Is.EqualTo("bridge.invalid-response"));
        });
    }

    [Test]
    public void EnforcesPayloadDepthCountStringBinaryAndCycleBounds()
    {
        BridgeValidator validator = new BridgeValidator();
        UiBridgeEnvelope deep = Envelope(UiBridgeMessageKind.Event);
        deep.Payload = DeepValue(34);
        UiBridgeEnvelope many = Envelope(UiBridgeMessageKind.Event);
        many.Payload = ArrayValue(4097);
        UiBridgeEnvelope longText = Envelope(UiBridgeMessageKind.Event);
        longText.Payload = Text(new string('x', 101));
        UiBridgeEnvelope binary = Envelope(UiBridgeMessageKind.Event);
        binary.Payload = new UiValue { Kind = UiValueKind.Binary, BinaryValue = new byte[101] };
        UiValue cycleValue = new UiValue { Kind = UiValueKind.Array };
        cycleValue.ArrayValue = new UiValue[] { cycleValue };
        UiBridgeEnvelope cycle = Envelope(UiBridgeMessageKind.Event);
        cycle.Payload = cycleValue;

        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateBridgePayload(deep, 4096).Code, Is.EqualTo("bridge.payload-depth-exceeded"));
            Assert.That(validator.ValidateBridgePayload(many, 100000).Code, Is.EqualTo("bridge.payload-count-exceeded"));
            Assert.That(validator.ValidateBridgePayload(longText, 100).Code, Is.EqualTo("bridge.invalid-value"));
            Assert.That(validator.ValidateBridgePayload(binary, 100).Code, Is.EqualTo("bridge.invalid-value"));
            Assert.That(validator.ValidateBridgePayload(cycle, 4096).Code, Is.EqualTo("bridge.payload-cycle"));
        });
    }

    [Test]
    public void RejectsInvalidOptionsEnvelopeFieldsArgumentsAndValueTags()
    {
        BridgeValidator validator = new BridgeValidator();
        UiBridgeOptions invalidOptions = Options();
        invalidOptions.MaximumPendingRequests = 0;
        UiBridgeOptions nullOrigin = Options();
        nullOrigin.Origin = null!;
        UiBridgeEnvelope invalidOrigin = RequestEnvelope();
        invalidOrigin.Origin = "page";
        UiBridgeEnvelope duplicateArguments = RequestEnvelope();
        duplicateArguments.Request!.Arguments = new UiValueEntry[]
        {
            new UiValueEntry { Key = "same", Value = Text("one") },
            new UiValueEntry { Key = "same", Value = Text("two") }
        };
        UiBridgeEnvelope malformedValue = Envelope(UiBridgeMessageKind.Event);
        malformedValue.Payload = new UiValue { Kind = UiValueKind.String };

        Assert.Multiple(() =>
        {
            Assert.That(validator.ValidateBridgeEnvelope(RequestEnvelope(), null, "session-1", true, -1).Code, Is.EqualTo("bridge.null-options"));
            Assert.That(validator.ValidateBridgeEnvelope(RequestEnvelope(), invalidOptions, "session-1", true, -1).Code, Is.EqualTo("bridge.invalid-options-limit"));
            Assert.That(validator.ValidateBridgeEnvelope(RequestEnvelope(), nullOrigin, "session-1", true, -1).Code, Is.EqualTo("bridge.invalid-options-origin"));
            Assert.That(validator.ValidateBridgePayload(invalidOrigin, 4096).Code, Is.EqualTo("bridge.invalid-origin"));
            Assert.That(validator.ValidateBridgePayload(duplicateArguments, 4096).Code, Is.EqualTo("bridge.invalid-arguments"));
            Assert.That(validator.ValidateBridgePayload(malformedValue, 4096).Code, Is.EqualTo("bridge.invalid-value"));
            Assert.That(validator.ValidateBridgePayload(null, 4096).Code, Is.EqualTo("bridge.null-envelope"));
        });
    }

    private static UiBridgeOptions Options()
    {
        return new UiBridgeOptions
        {
            Version = BridgeProtocolVersions.Current,
            Origin = BridgeProtocolNames.HostOrigin,
            RequireMainFrame = true,
            MaximumMessageLength = 4096,
            MaximumPendingRequests = 8,
            TimeoutMilliseconds = 1000
        };
    }

    private static UiBridgeEnvelope RequestEnvelope()
    {
        UiBridgeEnvelope envelope = Envelope(UiBridgeMessageKind.Request);
        envelope.CorrelationId = "correlation-1";
        envelope.Request = new UiBridgeRequest
        {
            CorrelationId = "correlation-1",
            Operation = "lookup",
            Arguments = new UiValueEntry[] { new UiValueEntry { Key = "query", Value = Text("value") } },
            TimeoutMilliseconds = 1000
        };
        return envelope;
    }

    private static UiBridgeEnvelope Envelope(UiBridgeMessageKind kind)
    {
        return new UiBridgeEnvelope
        {
            Version = BridgeProtocolVersions.Current,
            Session = "session-1",
            Sequence = 7,
            Origin = BridgeProtocolNames.HostOrigin,
            Kind = kind
        };
    }

    private static UiBridgeError Error(string correlationId)
    {
        return new UiBridgeError { Code = "failed", Message = "Failed", CorrelationId = correlationId };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue ArrayValue(int count)
    {
        UiValue[] items = new UiValue[count];
        int index = 0;
        while (index < count)
        {
            items[index] = new UiValue();
            index++;
        }

        return new UiValue { Kind = UiValueKind.Array, ArrayValue = items };
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
