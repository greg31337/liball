namespace dns.Tests.Api;

public class StaticDataTests
{
    [Test]
    public void ProtocolWireLimitsMatchDnsFieldWidths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DnsProtocolLimits.HeaderLength, Is.EqualTo(12));
            Assert.That(DnsProtocolLimits.MaximumLabelLength, Is.EqualTo(63));
            Assert.That(DnsProtocolLimits.MaximumNameLength, Is.EqualTo(255));
            Assert.That(DnsProtocolLimits.ClassicUdpPayloadSize, Is.EqualTo(512));
            Assert.That(DnsProtocolLimits.MaximumEdnsUdpPayloadSize, Is.EqualTo(65535));
            Assert.That(DnsProtocolLimits.TcpLengthPrefixLength, Is.EqualTo(2));
            Assert.That(DnsProtocolLimits.MaximumTcpMessageLength, Is.EqualTo(65535));
            Assert.That(DnsProtocolLimits.MaximumSectionEntryCount, Is.EqualTo(65535));
            Assert.That(DnsProtocolLimits.CompressionPointerLength, Is.EqualTo(2));
            Assert.That(DnsProtocolLimits.CompressionPointerTag, Is.EqualTo(0xc000));
            Assert.That(DnsProtocolLimits.CompressionPointerOffsetMask, Is.EqualTo(0x3fff));
            Assert.That(DnsProtocolLimits.MaximumCompressionPointerOffset, Is.EqualTo(16383));
        });
    }

    [Test]
    public void ConservativeDefaultsRemainWithinProtocolLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DnsProtocolLimits.DefaultEdnsUdpPayloadSize,
                Is.GreaterThanOrEqualTo(DnsProtocolLimits.ClassicUdpPayloadSize));
            Assert.That(DnsProtocolLimits.DefaultEdnsUdpPayloadSize,
                Is.LessThanOrEqualTo(DnsProtocolLimits.MaximumEdnsUdpPayloadSize));
            Assert.That(DnsProtocolLimits.DefaultEdnsUdpPayloadSize, Is.EqualTo(1232));
            Assert.That(DnsProtocolLimits.DefaultCompressionPointerHopLimit,
                Is.GreaterThan(0));
        });
    }

    [Test]
    public void TransportPortsMatchProtocolAssignments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DnsPorts.Dns, Is.EqualTo(53));
            Assert.That(DnsPorts.DnsOverTls, Is.EqualTo(853));
            Assert.That(DnsPorts.DnsOverHttps, Is.EqualTo(443));
            Assert.That(DnsPorts.DnsOverQuic, Is.EqualTo(853));
        });
    }

    [Test]
    public void ReverseMappingNamesAreAbsolute()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DnsWellKnownNames.Root, Is.EqualTo("."));
            Assert.That(DnsWellKnownNames.Ipv4ReverseSuffix, Is.EqualTo("in-addr.arpa."));
            Assert.That(DnsWellKnownNames.Ipv6ReverseSuffix, Is.EqualTo("ip6.arpa."));
        });
    }

    [Test]
    public void DnssecAlgorithmMnemonicsMatchRegistryNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DnsWellKnownNames.DnssecRsaSha256, Is.EqualTo("RSASHA256"));
            Assert.That(DnsWellKnownNames.DnssecRsaSha512, Is.EqualTo("RSASHA512"));
            Assert.That(DnsWellKnownNames.DnssecEcdsaP256Sha256,
                Is.EqualTo("ECDSAP256SHA256"));
            Assert.That(DnsWellKnownNames.DnssecEcdsaP384Sha384,
                Is.EqualTo("ECDSAP384SHA384"));
            Assert.That(DnsWellKnownNames.DnssecEd25519, Is.EqualTo("ED25519"));
            Assert.That(DnsWellKnownNames.DnssecMlDsa44, Is.EqualTo("MLDSA44"));
            Assert.That(DnsWellKnownNames.DnssecEccGost12, Is.EqualTo("ECC-GOST12"));
            Assert.That(DnsWellKnownNames.DnssecPrivateOid, Is.EqualTo("PRIVATEOID"));
        });
    }

    [Test]
    public void TsigAlgorithmNamesAreCanonicalAbsoluteNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DnsWellKnownNames.TsigHmacMd5,
                Is.EqualTo("hmac-md5.sig-alg.reg.int."));
            Assert.That(DnsWellKnownNames.TsigGss, Is.EqualTo("gss-tsig."));
            Assert.That(DnsWellKnownNames.TsigHmacSha1, Is.EqualTo("hmac-sha1."));
            Assert.That(DnsWellKnownNames.TsigHmacSha256, Is.EqualTo("hmac-sha256."));
            Assert.That(DnsWellKnownNames.TsigHmacSha384, Is.EqualTo("hmac-sha384."));
            Assert.That(DnsWellKnownNames.TsigHmacSha512, Is.EqualTo("hmac-sha512."));
            Assert.That(DnsWellKnownNames.TsigHmacSha512Truncated256,
                Is.EqualTo("hmac-sha512-256."));
        });
    }
}
