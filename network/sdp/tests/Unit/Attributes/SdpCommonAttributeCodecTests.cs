namespace sdp.Tests.Unit.Attributes;

public class SdpCommonAttributeCodecTests
{
    [TestCase("sendrecv", SdpDirection.SendReceive, "sendrecv")]
    [TestCase("SENDONLY", SdpDirection.SendOnly, "sendonly")]
    [TestCase("recvonly", SdpDirection.ReceiveOnly, "recvonly")]
    [TestCase("inactive", SdpDirection.Inactive, "inactive")]
    public void DirectionAttributes_DecodeAndEncodeAsProperties(string sourceName, SdpDirection direction, string canonicalName)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Property, Name = sourceName, RawText = sourceName };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.CommonAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.CommonAttribute?.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.CommonAttribute?.Name, Is.EqualTo(canonicalName));
            Assert.That(decoded.CommonAttribute?.Direction, Is.EqualTo(direction));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.CommonAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Property));
            Assert.That(encoded.Attribute?.Name, Is.EqualTo(canonicalName));
            Assert.That(encoded.Attribute?.Value, Is.Empty);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo(canonicalName));
        });
    }

    [TestCase("orient", "portrait", false)]
    [TestCase("type", "H332", false)]
    [TestCase("charset", "ISO-8859-1", false)]
    [TestCase("sdplang", "en-US", false)]
    [TestCase("lang", "de", false)]
    [TestCase("tool", "example generator 3.2", false)]
    [TestCase("content", "slides,speaker", false)]
    [TestCase("label", "camera-1", false)]
    [TestCase("control", "rtsp://media.example.test/trackID=1", false)]
    [TestCase("cat", "conference.training", true)]
    [TestCase("keywds", "SDP session description", true)]
    public void TextualAttributes_RoundTripThroughCommonProjection(string name, string value, bool obsolete)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name.ToUpperInvariant(),
            Value = value,
            RawText = name.ToUpperInvariant() + ":" + value
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.CommonAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.CommonAttribute?.Name, Is.EqualTo(name));
            Assert.That(decoded.CommonAttribute?.Value, Is.EqualTo(value));
            Assert.That(decoded.CommonAttribute?.Obsolete, Is.EqualTo(obsolete));
            Assert.That(decoded.CommonAttribute?.HasIntegerValue, Is.False);
            Assert.That(decoded.CommonAttribute?.HasNumericValue, Is.False);
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Diagnostics, Is.Empty);
            Assert.That(encoded.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute?.Name, Is.EqualTo(name));
            Assert.That(encoded.Attribute?.Value, Is.EqualTo(value));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo(name + ":" + value));
        });
    }

    [TestCase("ptime", "20", 20.0, false, 0)]
    [TestCase("maxptime", "20.5", 20.5, false, 0)]
    [TestCase("framerate", "59.94", 59.94, false, 0)]
    [TestCase("framerate", "0.1", 0.1, false, 0)]
    [TestCase("quality", "0", 0.0, true, 0)]
    [TestCase("quality", "10", 10.0, true, 10)]
    public void NumericAttributes_ProjectValidatedValues(string name, string value, double numericValue, bool hasInteger, int integerValue)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.CommonAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.CommonAttribute?.HasNumericValue, Is.True);
            Assert.That(decoded.CommonAttribute?.NumericValue, Is.EqualTo(numericValue).Within(0.000001));
            Assert.That(decoded.CommonAttribute?.HasIntegerValue, Is.EqualTo(hasInteger));
            Assert.That(decoded.CommonAttribute?.IntegerValue, Is.EqualTo(integerValue));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Value, Is.EqualTo(value));
        });
    }

    [TestCase("sendrecv", SdpAttributeKind.Value, "unexpected")]
    [TestCase("orient", SdpAttributeKind.Value, "Landscape")]
    [TestCase("type", SdpAttributeKind.Value, "h332")]
    [TestCase("charset", SdpAttributeKind.Value, "bad name")]
    [TestCase("lang", SdpAttributeKind.Value, "en--US")]
    [TestCase("cat", SdpAttributeKind.Value, "two words")]
    [TestCase("label", SdpAttributeKind.Value, "two words")]
    [TestCase("content", SdpAttributeKind.Value, "slides,,speaker")]
    [TestCase("quality", SdpAttributeKind.Value, "11")]
    [TestCase("quality", SdpAttributeKind.Value, "01")]
    [TestCase("ptime", SdpAttributeKind.Value, "0")]
    [TestCase("maxptime", SdpAttributeKind.Value, "20.0")]
    [TestCase("framerate", SdpAttributeKind.Value, ".5")]
    [TestCase("tool", SdpAttributeKind.Value, "")]
    [TestCase("control", SdpAttributeKind.Property, "")]
    public void InvalidCommonSyntax_FailsWithoutDiscardingTheGenericAttribute(string name, SdpAttributeKind kind, string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = name, Value = value };
        source.Location.LineNumber = 7;
        source.Location.ColumnNumber = 3;

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.CommonAttribute, Is.Null);
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo(name));
            Assert.That(result.Diagnostics[0].Path.AttributeName, Is.EqualTo(name));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(7));
            Assert.That(result.Diagnostics[0].Location, Is.Not.SameAs(source.Location));
        });
    }

    [Test]
    public void CallerConstructedCommonAttributes_EncodeDirectionTextAndNumbers()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpCommonAttribute direction = new SdpCommonAttribute { Direction = SdpDirection.ReceiveOnly };
        SdpCommonAttribute text = new SdpCommonAttribute { Name = "CONTROL", Value = "*" };
        SdpCommonAttribute quality = new SdpCommonAttribute { Name = "quality", HasIntegerValue = true, IntegerValue = 7 };
        SdpCommonAttribute packetTime = new SdpCommonAttribute { Name = "ptime", HasNumericValue = true, NumericValue = 12.5 };

        SdpAttributeEncodeResult encodedDirection = codec.EncodeAttribute(direction);
        SdpAttributeEncodeResult encodedText = codec.EncodeAttribute(text);
        SdpAttributeEncodeResult encodedQuality = codec.EncodeAttribute(quality);
        SdpAttributeEncodeResult encodedPacketTime = codec.EncodeAttribute(packetTime);

        Assert.Multiple(() =>
        {
            Assert.That(encodedDirection.Success, Is.True);
            Assert.That(encodedDirection.Attribute?.RawText, Is.EqualTo("recvonly"));
            Assert.That(encodedText.Success, Is.True);
            Assert.That(encodedText.Attribute?.RawText, Is.EqualTo("control:*"));
            Assert.That(encodedQuality.Success, Is.True);
            Assert.That(encodedQuality.Attribute?.RawText, Is.EqualTo("quality:7"));
            Assert.That(encodedPacketTime.Success, Is.True);
            Assert.That(encodedPacketTime.Attribute?.RawText, Is.EqualTo("ptime:12.5"));
        });
    }

    [Test]
    public void InvalidCallerConstructedCommonAttribute_ReturnsEncodingDiagnostic()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpCommonAttribute mismatchedDirection = new SdpCommonAttribute
        {
            Name = "inactive",
            Direction = SdpDirection.SendOnly
        };

        SdpAttributeEncodeResult result = codec.EncodeAttribute(mismatchedDirection);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.TypedAttribute, Is.SameAs(mismatchedDirection));
            Assert.That(result.Attribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
        });
    }
}
