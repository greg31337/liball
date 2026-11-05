namespace sdp.Tests.Unit.Attributes;

public class SdpSourceFilterCodecTests
{
    [TestCase(" incl IN IP4 232.3.4.5 192.0.2.10 ", "incl", "IP4", "232.3.4.5", SdpAddressKind.Ipv4Multicast, "incl IN IP4 232.3.4.5 192.0.2.10")]
    [TestCase("incl IN IP4 * 192.0.2.10", "incl", "IP4", "*", SdpAddressKind.Unknown, "incl IN IP4 * 192.0.2.10")]
    [TestCase("excl IN IP6 FF0E::11A 2001:DB8:1:2::1 2001:DB8:1:2::2", "excl", "IP6", "FF0E::11A", SdpAddressKind.Ipv6Multicast, "excl IN IP6 FF0E::11A 2001:DB8:1:2::1 2001:DB8:1:2::2")]
    [TestCase("incl IN * channel-1.example.com src-1.example.com", "incl", "*", "channel-1.example.com", SdpAddressKind.HostName, "incl IN * channel-1.example.com src-1.example.com")]
    public void SourceFilter_DecodesStandardAndExtensionAddressForms(string value, string mode, string addressType, string destination, SdpAddressKind addressKind, string canonicalValue)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "SOURCE-FILTER", Value = value, RawText = "SOURCE-FILTER:" + value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpSourceFilterAttribute projection = decoded.SourceFilterAttribute ?? new SdpSourceFilterAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.FilterMode, Is.EqualTo(mode));
            Assert.That(projection.NetworkType, Is.EqualTo("IN"));
            Assert.That(projection.AddressType, Is.EqualTo(addressType));
            Assert.That(projection.DestinationAddress, Is.EqualTo(destination));
            Assert.That(projection.AddressKind, Is.EqualTo(addressKind));
            Assert.That(projection.SourceAddresses, Is.Not.Empty);
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.SourceFilterAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("source-filter:" + canonicalValue));
        });
    }

    [TestCase(SdpAttributeKind.Property, "incl IN IP4 232.3.4.5 192.0.2.10", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "include IN IP4 232.3.4.5 192.0.2.10", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "incl IN IP4 232.3.4.5", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "incl IN IP4 999.3.4.5 192.0.2.10", "SDP_VALIDATION_CONNECTION")]
    [TestCase(SdpAttributeKind.Value, "incl IN IP4 232.3.4.5 233.0.0.1", "SDP_VALIDATION_CONNECTION")]
    [TestCase(SdpAttributeKind.Value, "incl IN * * src.example.com", "SDP_VALIDATION_CONNECTION")]
    [TestCase(SdpAttributeKind.Value, "incl IN IP6 FF0E::11A FF0E::22", "SDP_VALIDATION_CONNECTION")]
    public void InvalidSourceFilter_ReturnsStructuredFailure(SdpAttributeKind kind, string value, string code)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "source-filter", Value = value, RawText = "source-filter:" + value };
        source.Location.LineNumber = 23;

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceFilterAttribute, Is.Null);
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(23));
        });
    }

    [Test]
    public void CallerConstructedSourceFilter_RejectsMissingAndMismatchedSources()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpSourceFilterAttribute empty = new SdpSourceFilterAttribute
        {
            FilterMode = "incl",
            NetworkType = "IN",
            AddressType = "IP4",
            DestinationAddress = "232.3.4.5"
        };
        SdpSourceFilterAttribute multicastSource = new SdpSourceFilterAttribute
        {
            FilterMode = "incl",
            NetworkType = "IN",
            AddressType = "IP4",
            DestinationAddress = "232.3.4.5"
        };
        multicastSource.SourceAddresses.Add("233.4.5.6");

        SdpAttributeEncodeResult emptyResult = codec.EncodeAttribute(empty);
        SdpAttributeEncodeResult multicastResult = codec.EncodeAttribute(multicastSource);

        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.Success, Is.False);
            Assert.That(emptyResult.Attribute, Is.Null);
            Assert.That(multicastResult.Success, Is.False);
            Assert.That(multicastResult.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationConnection));
        });
    }
}
