namespace sdp.Tests.Unit.Attributes;

public class SdpClockAttributeCodecTests
{
    [TestCase("ntp=/traceable/", SdpClockSourceKind.Ntp, "ntp", "/traceable/", "ntp=/traceable/")]
    [TestCase("ptp=IEEE1588-2008:39-A7-94-FF-FE-07-CB-D0:0", SdpClockSourceKind.Ptp, "ptp", "IEEE1588-2008:39-A7-94-FF-FE-07-CB-D0:0", "ptp=IEEE1588-2008:39-A7-94-FF-FE-07-CB-D0:0")]
    [TestCase(" gps ", SdpClockSourceKind.Gps, "gps", "", "gps")]
    [TestCase("gal", SdpClockSourceKind.Galileo, "gal", "", "gal")]
    [TestCase("glonass", SdpClockSourceKind.Glonass, "glonass", "", "glonass")]
    [TestCase("local", SdpClockSourceKind.Local, "local", "", "local")]
    [TestCase("private", SdpClockSourceKind.Private, "private", "", "private")]
    [TestCase("private:traceable", SdpClockSourceKind.Private, "private", "traceable", "private:traceable")]
    [TestCase("x-clock=opaque:data", SdpClockSourceKind.Unknown, "x-clock", "opaque:data", "x-clock=opaque:data")]
    public void TimestampReferenceClock_DecodesRegisteredAndExtensionSources(string value, SdpClockSourceKind kind, string token, string clockValue, string canonicalValue)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "TS-REFCLK", Value = value, RawText = "TS-REFCLK:" + value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpClockReferenceAttribute projection = decoded.ClockReferenceAttribute ?? new SdpClockReferenceAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.SourceKind, Is.EqualTo(kind));
            Assert.That(projection.SourceToken, Is.EqualTo(token));
            Assert.That(projection.Value, Is.EqualTo(clockValue));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.ClockReferenceAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("ts-refclk:" + canonicalValue));
        });
    }

    [TestCase("ntp")]
    [TestCase("ptp=")]
    [TestCase("local=server")]
    [TestCase("private:other")]
    [TestCase("bad/source")]
    public void InvalidTimestampReferenceClock_FailsAndRetainsSource(string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ts-refclk", Value = value };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.ClockReferenceAttribute, Is.Null);
            Assert.That(result.TypedAttribute, Is.Null);
        });
    }

    [TestCase("sender", "", "sender", "", false, 0L, 1L, "mediaclk:sender")]
    [TestCase(" direct=963214424   rate=1000/1001 ", "", "direct", "963214424", true, 1000L, 1001L, "mediaclk:direct=963214424 rate=1000/1001")]
    [TestCase("direct rate=1/1", "", "direct", "", true, 1L, 1L, "mediaclk:direct rate=1/1")]
    [TestCase("direct rate=9223372036854775807/1", "", "direct", "", true, 9223372036854775807L, 1L, "mediaclk:direct rate=9223372036854775807/1")]
    [TestCase("id=src:Y2xvY2sx sender", "src:Y2xvY2sx", "sender", "", false, 0L, 1L, "mediaclk:id=src:Y2xvY2sx sender")]
    [TestCase("IEEE1722=38-D6-6D-8E-D2-78-13-2F", "", "IEEE1722", "38-D6-6D-8E-D2-78-13-2F", false, 0L, 1L, "mediaclk:IEEE1722=38-D6-6D-8E-D2-78-13-2F")]
    [TestCase("x-clock=opaque", "", "x-clock", "opaque", false, 0L, 1L, "mediaclk:x-clock=opaque")]
    public void MediaClock_DecodesIdentifiersOffsetsRatesAndExtensions(string value, string id, string name, string clockValue, bool hasRate, long numerator, long denominator, string rawText)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "MEDIACLK", Value = value, RawText = "MEDIACLK:" + value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpMediaClockAttribute projection = decoded.MediaClockAttribute ?? new SdpMediaClockAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.Id, Is.EqualTo(id));
            Assert.That(projection.ClockName, Is.EqualTo(name));
            Assert.That(projection.ClockValue, Is.EqualTo(clockValue));
            Assert.That(projection.HasRate, Is.EqualTo(hasRate));
            Assert.That(projection.RateNumerator, Is.EqualTo(numerator));
            Assert.That(projection.RateDenominator, Is.EqualTo(denominator));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.MediaClockAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo(rawText));
        });
    }

    [TestCase("id= sender")]
    [TestCase("sender rate=1/1")]
    [TestCase("direct rate=0/1")]
    [TestCase("direct rate=01/1")]
    [TestCase("direct rate=9223372036854775808/1")]
    [TestCase("IEEE1722=not-an-eui")]
    [TestCase("direct=offset")]
    [TestCase("sender extra extra")]
    public void InvalidMediaClock_ReturnsStructuredFailure(string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "mediaclk", Value = value };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.MediaClockAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void CallerConstructedClockData_ValidatesKindsAndRationalState()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpClockReferenceAttribute mismatched = new SdpClockReferenceAttribute { SourceKind = SdpClockSourceKind.Ntp, SourceToken = "ptp", Value = "server.example" };
        SdpMediaClockAttribute hiddenRate = new SdpMediaClockAttribute { ClockName = "direct", RateNumerator = 1000, RateDenominator = 1001 };
        SdpMediaClockAttribute wrongRateSource = new SdpMediaClockAttribute { ClockName = "sender", HasRate = true, RateNumerator = 1, RateDenominator = 1 };

        SdpAttributeEncodeResult referenceResult = codec.EncodeAttribute(mismatched);
        SdpAttributeEncodeResult hiddenResult = codec.EncodeAttribute(hiddenRate);
        SdpAttributeEncodeResult sourceResult = codec.EncodeAttribute(wrongRateSource);

        Assert.Multiple(() =>
        {
            Assert.That(referenceResult.Success, Is.False);
            Assert.That(hiddenResult.Success, Is.False);
            Assert.That(sourceResult.Success, Is.False);
            Assert.That(sourceResult.Attribute, Is.Null);
        });
    }
}
