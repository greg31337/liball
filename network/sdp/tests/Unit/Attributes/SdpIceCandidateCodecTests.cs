namespace sdp.Tests.Unit.Attributes;

public class SdpIceCandidateCodecTests
{
    [TestCase("host", SdpIceCandidateType.Host)]
    [TestCase("srflx", SdpIceCandidateType.ServerReflexive)]
    [TestCase("prflx", SdpIceCandidateType.PeerReflexive)]
    [TestCase("relay", SdpIceCandidateType.Relayed)]
    public void StandardCandidateTypes_DecodeAndEncode(string candidateTypeToken, SdpIceCandidateType candidateType)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        string value = "2 1 UDP 1694498815 192.0.2.3 45664 typ " + candidateTypeToken;
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "CANDIDATE", Value = value, RawText = "CANDIDATE:" + value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.IceCandidate!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.IceCandidate?.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.IceCandidate?.Foundation, Is.EqualTo("2"));
            Assert.That(decoded.IceCandidate?.Component, Is.EqualTo(1));
            Assert.That(decoded.IceCandidate?.TransportProtocol, Is.EqualTo(SdpIceTransportProtocol.Udp));
            Assert.That(decoded.IceCandidate?.TransportToken, Is.EqualTo("UDP"));
            Assert.That(decoded.IceCandidate?.Priority, Is.EqualTo(1694498815u));
            Assert.That(decoded.IceCandidate?.Address, Is.EqualTo("192.0.2.3"));
            Assert.That(decoded.IceCandidate?.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4));
            Assert.That(decoded.IceCandidate?.Port, Is.EqualTo(45664));
            Assert.That(decoded.IceCandidate?.CandidateType, Is.EqualTo(candidateType));
            Assert.That(decoded.IceCandidate?.CandidateTypeToken, Is.EqualTo(candidateTypeToken));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.IceCandidate));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("candidate:" + value));
        });
    }

    [TestCase("active", SdpIceTcpType.Active)]
    [TestCase("passive", SdpIceTcpType.Passive)]
    [TestCase("so", SdpIceTcpType.SimultaneousOpen)]
    public void TcpCandidateTypes_DecodeAndEncode(string tcpTypeToken, SdpIceTcpType tcpType)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        string value = "1 1 tcp 2128609279 2001:db8::1 9 TYP host TCPTYPE " + tcpTypeToken;
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "candidate", Value = value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.IceCandidate!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.IceCandidate?.TransportProtocol, Is.EqualTo(SdpIceTransportProtocol.Tcp));
            Assert.That(decoded.IceCandidate?.AddressKind, Is.EqualTo(SdpAddressKind.Ipv6));
            Assert.That(decoded.IceCandidate?.TcpType, Is.EqualTo(tcpType));
            Assert.That(decoded.IceCandidate?.TcpTypeToken, Is.EqualTo(tcpTypeToken));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("candidate:1 1 TCP 2128609279 2001:db8::1 9 typ host tcptype " + tcpTypeToken));
        });
    }

    [Test]
    public void RelatedEndpointAndUnknownExtensions_AreProjectedWithoutLosingOrderOrDuplicates()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        string value = "  842163049\t1  X-QUIC  1677734910 peer.local 60769 typ x-nat generation 0 network-cost 999 generation 1 raddr 10.0.0.2 rport 8998  ";
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "Candidate", Value = value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.IceCandidate!);
        SdpIceCandidate candidate = decoded.IceCandidate ?? new SdpIceCandidate();

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(candidate.TransportProtocol, Is.EqualTo(SdpIceTransportProtocol.Unknown));
            Assert.That(candidate.TransportToken, Is.EqualTo("X-QUIC"));
            Assert.That(candidate.AddressKind, Is.EqualTo(SdpAddressKind.HostName));
            Assert.That(candidate.CandidateType, Is.EqualTo(SdpIceCandidateType.Unknown));
            Assert.That(candidate.CandidateTypeToken, Is.EqualTo("x-nat"));
            Assert.That(candidate.HasRelatedAddress, Is.True);
            Assert.That(candidate.RelatedAddress, Is.EqualTo("10.0.0.2"));
            Assert.That(candidate.HasRelatedPort, Is.True);
            Assert.That(candidate.RelatedPort, Is.EqualTo(8998));
            Assert.That(candidate.Extensions.Select(x => x.Name), Is.EqualTo(new[] { "generation", "network-cost", "generation" }));
            Assert.That(candidate.Extensions.Select(x => x.Value), Is.EqualTo(new[] { "0", "999", "1" }));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("candidate:842163049 1 X-QUIC 1677734910 peer.local 60769 typ x-nat raddr 10.0.0.2 rport 8998 generation 0 network-cost 999 generation 1"));
        });
    }

    [Test]
    public void CandidateCodec_LeavesDocumentLevelCandidateRulesForValidation()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "candidate",
            Value = "zero 0 TCP 0 opaque-address 0 typ srflx"
        };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.IceCandidate?.Component, Is.Zero);
            Assert.That(result.IceCandidate?.Priority, Is.Zero);
            Assert.That(result.IceCandidate?.AddressKind, Is.EqualTo(SdpAddressKind.HostName));
            Assert.That(result.IceCandidate?.HasRelatedAddress, Is.False);
            Assert.That(result.IceCandidate?.TcpType, Is.EqualTo(SdpIceTcpType.Unknown));
        });
    }

    [Test]
    public void EmptyUnknownExtensionValue_RoundTripsAtTheGrammarBoundary()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpIceCandidate candidate = CreateValidCandidate();
        candidate.Extensions.Add(new SdpIceCandidateExtension { Name = "x-empty", Value = string.Empty });

        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(candidate);
        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(encoded.Attribute!);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("candidate:1 1 UDP 1 192.0.2.1 5000 typ host x-empty "));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.IceCandidate?.Extensions, Has.Count.EqualTo(1));
            Assert.That(decoded.IceCandidate?.Extensions[0].Name, Is.EqualTo("x-empty"));
            Assert.That(decoded.IceCandidate?.Extensions[0].Value, Is.Empty);
        });
    }

    [TestCase(SdpAttributeKind.Property, "1 1 UDP 1 192.0.2.1 5000 typ host")]
    [TestCase(SdpAttributeKind.Value, "1 1 UDP 1 192.0.2.1 5000 typ")]
    [TestCase(SdpAttributeKind.Value, "foundation-too-long-for-an-ice-candidate 1 UDP 1 192.0.2.1 5000 typ host")]
    [TestCase(SdpAttributeKind.Value, "bad- 1 UDP 1 192.0.2.1 5000 typ host")]
    [TestCase(SdpAttributeKind.Value, "1 1000 UDP 1 192.0.2.1 5000 typ host")]
    [TestCase(SdpAttributeKind.Value, "1 component UDP 1 192.0.2.1 5000 typ host")]
    [TestCase(SdpAttributeKind.Value, "1 1 UDP 4294967296 192.0.2.1 5000 typ host")]
    [TestCase(SdpAttributeKind.Value, "1 1 UDP 1 192.0.2.1 65536 typ host")]
    [TestCase(SdpAttributeKind.Value, "1 1 UDP 1 192.0.2.1 5000 type host")]
    [TestCase(SdpAttributeKind.Value, "1 1 UDP 1 192.0.2.1 5000 typ bad/type")]
    [TestCase(SdpAttributeKind.Value, "1 1 UDP 1 192.0.2.1 5000 typ host orphan")]
    [TestCase(SdpAttributeKind.Value, "1 1 UDP 1 192.0.2.1 5000 typ host rport invalid")]
    [TestCase(SdpAttributeKind.Value, "1 1 UDP 1 192.0.2.1 5000 typ host raddr 10.0.0.1 raddr 10.0.0.2")]
    [TestCase(SdpAttributeKind.Value, "1 1 TCP 1 192.0.2.1 9 typ host tcptype active tcptype passive")]
    public void InvalidCandidate_FailsAndRetainsOpaqueSource(SdpAttributeKind kind, string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "candidate", Value = value, RawText = "opaque:" + value };
        source.Location.LineNumber = 52;

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute?.RawText, Is.EqualTo("opaque:" + value));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.IceCandidate, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.Not.Empty);
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(52));
        });
    }

    [Test]
    public void CallerConstructedCandidate_EncodesCanonicalRegisteredTokens()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpIceCandidate candidate = new SdpIceCandidate
        {
            Foundation = "abc+123/",
            Component = 1,
            TransportProtocol = SdpIceTransportProtocol.Tcp,
            TransportToken = "tcp",
            Priority = uint.MaxValue,
            Address = "203.0.113.1",
            AddressKind = SdpAddressKind.Ipv4,
            Port = 9,
            CandidateType = SdpIceCandidateType.PeerReflexive,
            CandidateTypeToken = "PRFLX",
            HasRelatedAddress = true,
            RelatedAddress = "192.0.2.1",
            HasRelatedPort = true,
            RelatedPort = 65535,
            TcpType = SdpIceTcpType.SimultaneousOpen,
            TcpTypeToken = "SO"
        };
        candidate.Extensions.Add(new SdpIceCandidateExtension { Name = "network-id", Value = "3" });

        SdpAttributeEncodeResult result = codec.EncodeAttribute(candidate);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TypedAttribute, Is.SameAs(candidate));
            Assert.That(result.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(result.Attribute?.RawText, Is.EqualTo("candidate:abc+123/ 1 TCP 4294967295 203.0.113.1 9 typ prflx raddr 192.0.2.1 rport 65535 tcptype so network-id 3"));
        });
    }

    [Test]
    public void CallerConstructedCandidate_RejectsInconsistentAndInvalidData()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpIceCandidate transportMismatch = CreateValidCandidate();
        transportMismatch.TransportProtocol = SdpIceTransportProtocol.Udp;
        transportMismatch.TransportToken = "TCP";
        SdpIceCandidate hiddenAddress = CreateValidCandidate();
        hiddenAddress.RelatedAddress = "192.0.2.2";
        SdpIceCandidate hiddenPort = CreateValidCandidate();
        hiddenPort.RelatedPort = 5001;
        SdpIceCandidate typeMismatch = CreateValidCandidate();
        typeMismatch.CandidateType = SdpIceCandidateType.Host;
        typeMismatch.CandidateTypeToken = "relay";
        SdpIceCandidate reservedExtension = CreateValidCandidate();
        reservedExtension.Extensions.Add(new SdpIceCandidateExtension { Name = "raddr", Value = "192.0.2.1" });

        SdpAttributeEncodeResult transportResult = codec.EncodeAttribute(transportMismatch);
        SdpAttributeEncodeResult addressResult = codec.EncodeAttribute(hiddenAddress);
        SdpAttributeEncodeResult portResult = codec.EncodeAttribute(hiddenPort);
        SdpAttributeEncodeResult typeResult = codec.EncodeAttribute(typeMismatch);
        SdpAttributeEncodeResult extensionResult = codec.EncodeAttribute(reservedExtension);

        Assert.Multiple(() =>
        {
            Assert.That(transportResult.Success, Is.False);
            Assert.That(addressResult.Success, Is.False);
            Assert.That(portResult.Success, Is.False);
            Assert.That(typeResult.Success, Is.False);
            Assert.That(extensionResult.Success, Is.False);
            Assert.That(extensionResult.Attribute, Is.Null);
        });
    }

    static SdpIceCandidate CreateValidCandidate()
    {
        return new SdpIceCandidate
        {
            Foundation = "1",
            Component = 1,
            TransportProtocol = SdpIceTransportProtocol.Udp,
            Priority = 1u,
            Address = "192.0.2.1",
            Port = 5000,
            CandidateType = SdpIceCandidateType.Host
        };
    }
}
