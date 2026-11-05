namespace sdp.Tests.Unit.Attributes;

public class SdpRemoteCandidatesCodecTests
{
    [Test]
    public void TupleList_DecodesInSourceOrderAndEncodesWithCanonicalWhitespace()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "REMOTE-CANDIDATES",
            Value = " 2 192.0.2.3 45665\t 1 192.0.2.3   45664 ",
            RawText = "REMOTE-CANDIDATES: 2 192.0.2.3 45665\t 1 192.0.2.3   45664 "
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.RemoteCandidatesAttribute!);
        SdpRemoteCandidatesAttribute projection = decoded.RemoteCandidatesAttribute ?? new SdpRemoteCandidatesAttribute();

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.Candidates.Select(x => x.Component), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(projection.Candidates.Select(x => x.Address), Is.EqualTo(new[] { "192.0.2.3", "192.0.2.3" }));
            Assert.That(projection.Candidates.Select(x => x.Port), Is.EqualTo(new[] { 45665, 45664 }));
            Assert.That(projection.Candidates.Select(x => x.AddressKind), Is.EqualTo(new[] { SdpAddressKind.Ipv4, SdpAddressKind.Ipv4 }));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.RemoteCandidatesAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("remote-candidates:2 192.0.2.3 45665 1 192.0.2.3 45664"));
        });
    }

    [TestCase("2001:db8::1", SdpAddressKind.Ipv6)]
    [TestCase("peer.local", SdpAddressKind.HostName)]
    [TestCase("233.252.0.1", SdpAddressKind.Ipv4Multicast)]
    [TestCase("ff0e::1", SdpAddressKind.Ipv6Multicast)]
    [TestCase("extension-address", SdpAddressKind.HostName)]
    public void AddressForms_AreClassifiedAndRoundTrip(string address, SdpAddressKind addressKind)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "remote-candidates", Value = "1 " + address + " 9" };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.RemoteCandidatesAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.RemoteCandidatesAttribute?.Candidates[0].AddressKind, Is.EqualTo(addressKind));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("remote-candidates:1 " + address + " 9"));
        });
    }

    [Test]
    public void ComponentSyntaxBoundary_IsDecodedWithoutApplyingLaterSemanticRangeRules()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "remote-candidates", Value = "000 opaque-address 0 999 192.0.2.2 65535" };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.RemoteCandidatesAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.RemoteCandidatesAttribute?.Candidates.Select(x => x.Component), Is.EqualTo(new[] { 0, 999 }));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("remote-candidates:0 opaque-address 0 999 192.0.2.2 65535"));
        });
    }

    [TestCase(SdpAttributeKind.Property, "1 192.0.2.3 45664")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "1 192.0.2.3")]
    [TestCase(SdpAttributeKind.Value, "1 192.0.2.3 45664 orphan")]
    [TestCase(SdpAttributeKind.Value, "component 192.0.2.3 45664")]
    [TestCase(SdpAttributeKind.Value, "1000 192.0.2.3 45664")]
    [TestCase(SdpAttributeKind.Value, "1 192.0.2.3 invalid")]
    [TestCase(SdpAttributeKind.Value, "1 192.0.2.3 65536")]
    [TestCase(SdpAttributeKind.Value, "1 192.0.2.3 45664 1 192.0.2.4 45665")]
    [TestCase(SdpAttributeKind.Value, "001 192.0.2.3 45664 1 192.0.2.4 45665")]
    public void InvalidTupleList_FailsAndRetainsOpaqueSource(SdpAttributeKind kind, string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "remote-candidates", Value = value, RawText = "opaque:" + value };
        source.Location.LineNumber = 53;

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute?.RawText, Is.EqualTo("opaque:" + value));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.RemoteCandidatesAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.Not.Empty);
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(53));
        });
    }

    [Test]
    public void CallerConstructedTupleList_PreservesOrder()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpRemoteCandidatesAttribute projection = new SdpRemoteCandidatesAttribute();
        projection.Candidates.Add(new SdpRemoteCandidate { Component = 3, Address = "2001:db8::3", AddressKind = SdpAddressKind.Ipv6, Port = 6003 });
        projection.Candidates.Add(new SdpRemoteCandidate { Component = 1, Address = "192.0.2.1", AddressKind = SdpAddressKind.Ipv4, Port = 6001 });
        projection.Candidates.Add(new SdpRemoteCandidate { Component = 2, Address = "peer.local", AddressKind = SdpAddressKind.HostName, Port = 6002 });

        SdpAttributeEncodeResult result = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TypedAttribute, Is.SameAs(projection));
            Assert.That(result.Attribute?.RawText, Is.EqualTo("remote-candidates:3 2001:db8::3 6003 1 192.0.2.1 6001 2 peer.local 6002"));
        });
    }

    [Test]
    public void CallerConstructedTupleList_RejectsEmptyDuplicateAndInvalidTuples()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpRemoteCandidatesAttribute empty = new SdpRemoteCandidatesAttribute();
        SdpRemoteCandidatesAttribute duplicate = CreateValidProjection();
        duplicate.Candidates.Add(new SdpRemoteCandidate { Component = 1, Address = "192.0.2.2", Port = 6001 });
        SdpRemoteCandidatesAttribute invalidComponent = CreateValidProjection();
        invalidComponent.Candidates[0].Component = 1000;
        SdpRemoteCandidatesAttribute invalidAddress = CreateValidProjection();
        invalidAddress.Candidates[0].Address = "two addresses";
        SdpRemoteCandidatesAttribute invalidPort = CreateValidProjection();
        invalidPort.Candidates[0].Port = 65536;

        SdpAttributeEncodeResult emptyResult = codec.EncodeAttribute(empty);
        SdpAttributeEncodeResult duplicateResult = codec.EncodeAttribute(duplicate);
        SdpAttributeEncodeResult componentResult = codec.EncodeAttribute(invalidComponent);
        SdpAttributeEncodeResult addressResult = codec.EncodeAttribute(invalidAddress);
        SdpAttributeEncodeResult portResult = codec.EncodeAttribute(invalidPort);

        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.Success, Is.False);
            Assert.That(duplicateResult.Success, Is.False);
            Assert.That(componentResult.Success, Is.False);
            Assert.That(addressResult.Success, Is.False);
            Assert.That(portResult.Success, Is.False);
            Assert.That(portResult.Attribute, Is.Null);
        });
    }

    static SdpRemoteCandidatesAttribute CreateValidProjection()
    {
        SdpRemoteCandidatesAttribute projection = new SdpRemoteCandidatesAttribute();
        projection.Candidates.Add(new SdpRemoteCandidate { Component = 1, Address = "192.0.2.1", Port = 6000 });
        return projection;
    }
}
