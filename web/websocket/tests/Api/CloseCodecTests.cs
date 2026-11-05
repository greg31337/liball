using System.Text;
using websocket;

namespace websocket.Tests;

public class CloseCodecTests
{
    [Test]
    public void EmptyPayloadIsAValidCloseWithoutCodeOrReason()
    {
        var codec = new WebSocketCloseCodec();

        var result = codec.ReadClosePayload([]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.HasError, Is.False);
            Assert.That(result.Close.HasCode, Is.False);
            Assert.That(result.Close.HasKnownCode, Is.False);
            Assert.That(result.Close.HasReason, Is.False);
            Assert.That(result.Close.Reason, Is.Empty);
        });
    }

    [TestCase(1000, WebSocketCloseCode.NormalClosure)]
    [TestCase(1001, WebSocketCloseCode.GoingAway)]
    [TestCase(1002, WebSocketCloseCode.ProtocolError)]
    [TestCase(1003, WebSocketCloseCode.UnsupportedData)]
    [TestCase(1007, WebSocketCloseCode.InvalidFramePayloadData)]
    [TestCase(1008, WebSocketCloseCode.PolicyViolation)]
    [TestCase(1009, WebSocketCloseCode.MessageTooBig)]
    [TestCase(1010, WebSocketCloseCode.MandatoryExtension)]
    [TestCase(1011, WebSocketCloseCode.InternalError)]
    [TestCase(1012, WebSocketCloseCode.ServiceRestart)]
    [TestCase(1013, WebSocketCloseCode.TryAgainLater)]
    [TestCase(1014, WebSocketCloseCode.BadGateway)]
    [TestCase(3000, WebSocketCloseCode.Unauthorized)]
    [TestCase(3003, WebSocketCloseCode.Forbidden)]
    [TestCase(3008, WebSocketCloseCode.Timeout)]
    public void KnownCodesAreReadInNetworkByteOrder(int code, WebSocketCloseCode knownCode)
    {
        var codec = new WebSocketCloseCodec();
        byte[] payload = [(byte)(code >> 8), (byte)code];

        var result = codec.ReadClosePayload(payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Close.HasCode, Is.True);
            Assert.That(result.Close.Code, Is.EqualTo(code));
            Assert.That(result.Close.HasKnownCode, Is.True);
            Assert.That(result.Close.KnownCode, Is.EqualTo(knownCode));
            Assert.That(result.Close.HasReason, Is.False);
        });
    }

    [TestCase(3001)]
    [TestCase(3999)]
    [TestCase(4000)]
    [TestCase(4999)]
    public void ApplicationAndPrivateCodesRetainTheirRawValue(int code)
    {
        var codec = new WebSocketCloseCodec();
        byte[] payload = [(byte)(code >> 8), (byte)code];

        var result = codec.ReadClosePayload(payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Close.HasCode, Is.True);
            Assert.That(result.Close.Code, Is.EqualTo(code));
            Assert.That(result.Close.HasKnownCode, Is.False);
        });
    }

    [Test]
    public void EveryRegisteredReservedAndPrivateCodeClassifiesConsistently()
    {
        WebSocketCloseCodec codec = new WebSocketCloseCodec();

        for (int code = WebSocketCloseCodes.NormalClosure;
            code <= WebSocketCloseCodes.PrivateUseRangeMaximum;
            code++)
        {
            byte[] payload = [(byte)(code >> 8), (byte)code];
            WebSocketCloseReadResult read = codec.ReadClosePayload(payload);
            bool valid = IsValidWireCode(code);

            Assert.That(read.Valid, Is.EqualTo(valid), $"Read code {code}");
            if (valid)
            {
                WebSocketCloseInfo close = new WebSocketCloseInfo();
                close.HasCode = true;
                close.Code = code;
                byte[] destination = [0xaa, 0xaa];

                int measured = codec.MeasureClosePayload(close);
                int written = codec.WriteClosePayload(close, destination);

                Assert.That(measured, Is.EqualTo(2), $"Measure code {code}");
                Assert.That(written, Is.EqualTo(2), $"Write code {code}");
                Assert.That(destination, Is.EqualTo(payload), $"Wire code {code}");
                Assert.That(read.Close.Code, Is.EqualTo(code), $"Raw code {code}");
                Assert.That(
                    read.Close.HasKnownCode,
                    Is.EqualTo(IsKnownWireCode(code)),
                    $"Known code {code}");
                if (read.Close.HasKnownCode)
                {
                    Assert.That(
                        read.Close.KnownCode,
                        Is.EqualTo((WebSocketCloseCode)code),
                        $"Known enum {code}");
                }
            }
            else
            {
                WebSocketCloseInfo close = new WebSocketCloseInfo();
                close.HasCode = true;
                close.Code = code;
                byte[] destination = [0xaa, 0xaa];

                Assert.That(read.HasError, Is.True, $"Read error {code}");
                Assert.That(
                    read.Error.Identifier,
                    Is.EqualTo("close-code-invalid"),
                    $"Read identifier {code}");
                Assert.Throws<WebSocketProtocolException>(
                    () => codec.MeasureClosePayload(close),
                    $"Measure code {code}");
                Assert.Throws<WebSocketProtocolException>(
                    () => codec.WriteClosePayload(close, destination),
                    $"Write code {code}");
                Assert.That(
                    destination,
                    Is.EqualTo(new byte[] { 0xaa, 0xaa }),
                    $"Unchanged destination {code}");
            }
        }
    }

    [Test]
    public void Utf8ReasonIsDecodedAfterTheStatusCode()
    {
        var codec = new WebSocketCloseCodec();
        const string reason = "done \u20ac \U00010348";
        var reasonBytes = Encoding.UTF8.GetBytes(reason);
        var payload = new byte[WebSocketProtocolConstants.CloseCodeLength + reasonBytes.Length];
        payload[0] = 0x03;
        payload[1] = 0xe8;
        reasonBytes.CopyTo(payload, WebSocketProtocolConstants.CloseCodeLength);

        var result = codec.ReadClosePayload(payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Close.Code, Is.EqualTo(WebSocketCloseCodes.NormalClosure));
            Assert.That(result.Close.HasReason, Is.True);
            Assert.That(result.Close.Reason, Is.EqualTo(reason));
        });
    }

    [Test]
    public void MaximumLengthPayloadIsAccepted()
    {
        var codec = new WebSocketCloseCodec();
        var payload = new byte[WebSocketProtocolConstants.ControlPayloadLengthMaximum];
        payload[0] = 0x03;
        payload[1] = 0xe8;
        Array.Fill(payload, (byte)'a', WebSocketProtocolConstants.CloseCodeLength, WebSocketProtocolConstants.CloseReasonLengthMaximum);

        var result = codec.ReadClosePayload(payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Close.HasReason, Is.True);
            Assert.That(result.Close.Reason, Has.Length.EqualTo(WebSocketProtocolConstants.CloseReasonLengthMaximum));
        });
    }

    [Test]
    public void OneByteAndOversizedPayloadsAreRejected()
    {
        var codec = new WebSocketCloseCodec();

        var oneByte = codec.ReadClosePayload(new byte[] { 0x03 });
        var oversized = codec.ReadClosePayload(new byte[WebSocketProtocolConstants.ControlPayloadLengthMaximum + 1]);

        AssertProtocolReadFailure(oneByte, "close-payload-one-byte");
        AssertProtocolReadFailure(oversized, "close-payload-too-large");
    }

    [TestCase(0)]
    [TestCase(999)]
    [TestCase(1004)]
    [TestCase(1005)]
    [TestCase(1006)]
    [TestCase(1015)]
    [TestCase(1016)]
    [TestCase(2999)]
    [TestCase(5000)]
    [TestCase(65535)]
    public void InvalidWireCodesAreRejected(int code)
    {
        var codec = new WebSocketCloseCodec();
        byte[] payload = [(byte)(code >> 8), (byte)code];

        var result = codec.ReadClosePayload(payload);

        AssertProtocolReadFailure(result, "close-code-invalid");
    }

    [TestCase("80")]
    [TestCase("c0af")]
    [TestCase("e08080")]
    [TestCase("eda080")]
    [TestCase("f4908080")]
    [TestCase("c2")]
    public void InvalidUtf8ReasonsAreRejected(string hexadecimalReason)
    {
        var codec = new WebSocketCloseCodec();
        var payload = Convert.FromHexString("03e8" + hexadecimalReason);

        var result = codec.ReadClosePayload(payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Utf8));
            Assert.That(result.Error.CloseCode, Is.EqualTo(WebSocketCloseCodes.InvalidFramePayloadData));
            Assert.That(result.Error.KnownCloseCode, Is.EqualTo(WebSocketCloseCode.InvalidFramePayloadData));
            Assert.That(result.Error.Identifier, Is.EqualTo("close-reason-invalid-utf8"));
        });
    }

    [Test]
    public void MeasurementHandlesEmptyCodeOnlyAndUnicodeReasons()
    {
        var codec = new WebSocketCloseCodec();
        var empty = new WebSocketCloseInfo();
        var codeOnly = new WebSocketCloseInfo();
        codeOnly.HasCode = true;
        codeOnly.Code = WebSocketCloseCodes.NormalClosure;
        var withReason = new WebSocketCloseInfo();
        withReason.HasCode = true;
        withReason.Code = WebSocketCloseCodes.NormalClosure;
        withReason.HasReason = true;
        withReason.Reason = "a\u20ac\U00010348";

        Assert.Multiple(() =>
        {
            Assert.That(codec.MeasureClosePayload(empty), Is.Zero);
            Assert.That(codec.MeasureClosePayload(codeOnly), Is.EqualTo(2));
            Assert.That(codec.MeasureClosePayload(withReason), Is.EqualTo(10));
        });
    }

    [Test]
    public void EmptyCloseWritesNoBytes()
    {
        var codec = new WebSocketCloseCodec();
        var close = new WebSocketCloseInfo();
        byte[] destination = [0xaa];

        var written = codec.WriteClosePayload(close, destination);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 0xaa }));
        });
    }

    [Test]
    public void WriteProducesNetworkOrderAndUtf8ThenRoundTrips()
    {
        var codec = new WebSocketCloseCodec();
        var close = new WebSocketCloseInfo();
        close.HasCode = true;
        close.Code = WebSocketCloseCodes.TryAgainLater;
        close.HasReason = true;
        close.Reason = "later \u20ac";
        var destination = Enumerable.Repeat((byte)0xaa, 32).ToArray();

        var written = codec.WriteClosePayload(close, destination);
        var read = codec.ReadClosePayload(destination.AsSpan(0, written));

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(codec.MeasureClosePayload(close)));
            Assert.That(destination[0], Is.EqualTo(0x03));
            Assert.That(destination[1], Is.EqualTo(0xf5));
            Assert.That(destination[written], Is.EqualTo(0xaa));
            Assert.That(read.Valid, Is.True);
            Assert.That(read.Close.Code, Is.EqualTo(close.Code));
            Assert.That(read.Close.Reason, Is.EqualTo(close.Reason));
        });
    }

    [Test]
    public void MaximumReasonWritesWithoutTouchingTrailingCapacity()
    {
        var codec = new WebSocketCloseCodec();
        var close = new WebSocketCloseInfo();
        close.HasCode = true;
        close.Code = WebSocketCloseCodes.NormalClosure;
        close.HasReason = true;
        close.Reason = new string('a', WebSocketProtocolConstants.CloseReasonLengthMaximum);
        var destination = Enumerable.Repeat((byte)0xaa, WebSocketProtocolConstants.ControlPayloadLengthMaximum + 1).ToArray();

        var written = codec.WriteClosePayload(close, destination);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(WebSocketProtocolConstants.ControlPayloadLengthMaximum));
            Assert.That(destination[written], Is.EqualTo(0xaa));
        });
    }

    [Test]
    public void ExactUtf8ReasonByteBoundaryMeasuresWritesAndRoundTrips()
    {
        WebSocketCloseCodec codec = new WebSocketCloseCodec();
        string reason = new string('\u20ac', 41);
        WebSocketCloseInfo close = new WebSocketCloseInfo();
        close.HasCode = true;
        close.Code = WebSocketCloseCodes.NormalClosure;
        close.HasReason = true;
        close.Reason = reason;
        byte[] destination =
            new byte[WebSocketProtocolConstants.ControlPayloadLengthMaximum];

        int measured = codec.MeasureClosePayload(close);
        int written = codec.WriteClosePayload(close, destination);
        WebSocketCloseReadResult read = codec.ReadClosePayload(destination);

        Assert.Multiple(() =>
        {
            Assert.That(
                Encoding.UTF8.GetByteCount(reason),
                Is.EqualTo(WebSocketProtocolConstants.CloseReasonLengthMaximum));
            Assert.That(
                measured,
                Is.EqualTo(WebSocketProtocolConstants.ControlPayloadLengthMaximum));
            Assert.That(written, Is.EqualTo(measured));
            Assert.That(read.Valid, Is.True);
            Assert.That(read.Close.Code,
                Is.EqualTo(WebSocketCloseCodes.NormalClosure));
            Assert.That(read.Close.HasReason, Is.True);
            Assert.That(read.Close.Reason, Is.EqualTo(reason));
        });

        close.Reason = reason + "a";

        Assert.Throws<WebSocketLimitException>(() =>
            codec.MeasureClosePayload(close));
    }

    [Test]
    public void ReasonWithoutCodeAndInvalidCodesAreRejectedBeforeWriting()
    {
        var codec = new WebSocketCloseCodec();
        var withoutCode = new WebSocketCloseInfo();
        withoutCode.HasReason = true;
        withoutCode.Reason = "reason";
        var invalidCode = new WebSocketCloseInfo();
        invalidCode.HasCode = true;
        invalidCode.Code = WebSocketCloseCodes.NoStatusReceived;
        var destination = Enumerable.Repeat((byte)0xaa, 16).ToArray();

        Assert.Throws<WebSocketProtocolException>(() => codec.MeasureClosePayload(withoutCode));
        Assert.Throws<WebSocketProtocolException>(() => codec.WriteClosePayload(invalidCode, destination));
        Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xaa, 16)));
    }

    [Test]
    public void InvalidUtf16ReasonsAreRejectedBeforeWriting()
    {
        var codec = new WebSocketCloseCodec();
        string[] invalidReasons =
        [
            new string((char)0xd800, 1),
            new string((char)0xdc00, 1),
            new string(new char[] { (char)0xd800, 'x' })
        ];

        foreach (var reason in invalidReasons)
        {
            var close = new WebSocketCloseInfo();
            close.HasCode = true;
            close.Code = WebSocketCloseCodes.NormalClosure;
            close.HasReason = true;
            close.Reason = reason;
            var destination = Enumerable.Repeat((byte)0xaa, 16).ToArray();

            Assert.Throws<WebSocketProtocolException>(() => codec.WriteClosePayload(close, destination));
            Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xaa, 16)));
        }
    }

    [Test]
    public void OversizedReasonAndShortDestinationAreRejectedWithoutWriting()
    {
        var codec = new WebSocketCloseCodec();
        var oversized = new WebSocketCloseInfo();
        oversized.HasCode = true;
        oversized.Code = WebSocketCloseCodes.NormalClosure;
        oversized.HasReason = true;
        oversized.Reason = new string('a', WebSocketProtocolConstants.CloseReasonLengthMaximum + 1);
        var normal = new WebSocketCloseInfo();
        normal.HasCode = true;
        normal.Code = WebSocketCloseCodes.NormalClosure;
        normal.HasReason = true;
        normal.Reason = "ok";
        var destination = Enumerable.Repeat((byte)0xaa, 3).ToArray();

        Assert.Throws<WebSocketLimitException>(() => codec.MeasureClosePayload(oversized));
        Assert.Throws<WebSocketLimitException>(() => codec.WriteClosePayload(normal, destination));
        Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xaa, 3)));
    }

    [Test]
    public void InactiveReasonFieldDoesNotAffectCodeOnlyPayload()
    {
        var codec = new WebSocketCloseCodec();
        var close = new WebSocketCloseInfo();
        close.HasCode = true;
        close.Code = WebSocketCloseCodes.NormalClosure;
        close.HasReason = false;
        close.Reason = new string('a', 1000);
        byte[] destination = [0xaa, 0xaa, 0xaa];

        var written = codec.WriteClosePayload(close, destination);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(2));
            Assert.That(destination, Is.EqualTo(new byte[] { 0x03, 0xe8, 0xaa }));
        });
    }

    static void AssertProtocolReadFailure(WebSocketCloseReadResult result, string identifier)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Protocol));
            Assert.That(result.Error.HasCloseCode, Is.True);
            Assert.That(result.Error.CloseCode, Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(result.Error.HasKnownCloseCode, Is.True);
            Assert.That(result.Error.KnownCloseCode, Is.EqualTo(WebSocketCloseCode.ProtocolError));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
        });
    }

    static bool IsValidWireCode(int code)
    {
        if (code == WebSocketCloseCodes.Reserved ||
            code == WebSocketCloseCodes.NoStatusReceived ||
            code == WebSocketCloseCodes.AbnormalClosure ||
            code == WebSocketCloseCodes.TlsHandshake)
        {
            return false;
        }

        if (code > WebSocketCloseCodes.TlsHandshake &&
            code < WebSocketCloseCodes.RegisteredApplicationRangeMinimum)
        {
            return false;
        }

        return code >= WebSocketCloseCodes.NormalClosure &&
            code <= WebSocketCloseCodes.PrivateUseRangeMaximum;
    }

    static bool IsKnownWireCode(int code)
    {
        if (code >= WebSocketCloseCodes.NormalClosure &&
            code <= WebSocketCloseCodes.BadGateway &&
            IsValidWireCode(code))
        {
            return true;
        }

        return code == WebSocketCloseCodes.Unauthorized ||
            code == WebSocketCloseCodes.Forbidden ||
            code == WebSocketCloseCodes.Timeout;
    }
}
