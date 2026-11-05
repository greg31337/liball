namespace sdp.Tests.Unit.Registry;

public class SdpRegistryTests
{
    [Test]
    public void MediaTypeLookup_IsCaseInsensitive()
    {
        ISdpRegistry registry = new SdpRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsRegisteredMediaType("audio"), Is.True);
            Assert.That(registry.IsRegisteredMediaType("AUDIO"), Is.True);
            Assert.That(registry.IsRegisteredMediaType("haptics"), Is.True);
            Assert.That(registry.IsRegisteredMediaType("unknown-media"), Is.False);
        });
    }

    [Test]
    public void TransportProtocolLookup_IsCaseSensitive()
    {
        ISdpRegistry registry = new SdpRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsRegisteredTransportProtocol("RTP/AVP"), Is.True);
            Assert.That(registry.IsRegisteredTransportProtocol("UDP/DTLS/SCTP"), Is.True);
            Assert.That(registry.IsRegisteredTransportProtocol("rtp/avp"), Is.False);
            Assert.That(registry.IsRegisteredTransportProtocol("unknown-protocol"), Is.False);
        });
    }

    [Test]
    public void CoreTokenLookups_UseCaseSensitiveRegistryNames()
    {
        ISdpRegistry registry = new SdpRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsRegisteredBandwidthType("AS"), Is.True);
            Assert.That(registry.IsRegisteredBandwidthType("as"), Is.False);
            Assert.That(registry.IsRegisteredNetworkType("IN"), Is.True);
            Assert.That(registry.IsRegisteredNetworkType("in"), Is.False);
            Assert.That(registry.IsRegisteredAddressType("IP6"), Is.True);
            Assert.That(registry.IsRegisteredAddressType("ip6"), Is.False);
            Assert.That(registry.IsRegisteredEncryptionMethod("clear"), Is.True);
            Assert.That(registry.IsRegisteredEncryptionMethod("Clear"), Is.False);
        });
    }

    [Test]
    public void AttributeNameLookup_IsCaseInsensitive()
    {
        ISdpRegistry registry = new SdpRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsRegisteredAttributeName("candidate"), Is.True);
            Assert.That(registry.IsRegisteredAttributeName("CANDIDATE"), Is.True);
            Assert.That(registry.IsRegisteredAttributeName("extmap-allow-mixed"), Is.True);
            Assert.That(registry.IsRegisteredAttributeName("unknown-attribute"), Is.False);
        });
    }

    [Test]
    public void GroupSemanticLookup_IsCaseSensitive()
    {
        ISdpRegistry registry = new SdpRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsRegisteredGroupSemantic("BUNDLE"), Is.True);
            Assert.That(registry.IsRegisteredGroupSemantic("ANAT"), Is.True);
            Assert.That(registry.IsRegisteredGroupSemantic("bundle"), Is.False);
            Assert.That(registry.IsRegisteredGroupSemantic("unknown-group"), Is.False);
        });
    }

    [Test]
    public void StaticPayloadLookup_ExcludesReservedUnassignedAndDynamicValues()
    {
        ISdpRegistry registry = new SdpRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsAssignedStaticPayloadType(0), Is.True);
            Assert.That(registry.IsAssignedStaticPayloadType(34), Is.True);
            Assert.That(registry.IsAssignedStaticPayloadType(1), Is.False);
            Assert.That(registry.IsAssignedStaticPayloadType(20), Is.False);
            Assert.That(registry.IsAssignedStaticPayloadType(96), Is.False);
            Assert.That(registry.IsAssignedStaticPayloadType(-1), Is.False);
            Assert.That(registry.IsAssignedStaticPayloadType(128), Is.False);
        });
    }

    [Test]
    public void RtpEncodingLookup_IsCaseInsensitiveForMediaAndEncodingNames()
    {
        ISdpRegistry registry = new SdpRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsRegisteredRtpEncoding("audio", "opus"), Is.True);
            Assert.That(registry.IsRegisteredRtpEncoding("AUDIO", "OPUS"), Is.True);
            Assert.That(registry.IsRegisteredRtpEncoding("video", "VP9"), Is.True);
            Assert.That(registry.IsRegisteredRtpEncoding("VIDEO", "hevc"), Is.True);
            Assert.That(registry.IsRegisteredRtpEncoding("audio", "VP9"), Is.False);
            Assert.That(registry.IsRegisteredRtpEncoding("video", "unknown-codec"), Is.False);
        });
    }

    [Test]
    public void HeaderExtensionUriLookup_UsesExactUriIdentity()
    {
        ISdpRegistry registry = new SdpRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsRegisteredHeaderExtensionUri("urn:ietf:params:rtp-hdrext:toffset"), Is.True);
            Assert.That(registry.IsRegisteredHeaderExtensionUri("urn:ietf:params:rtp-hdrext:sdes:mid"), Is.True);
            Assert.That(registry.IsRegisteredHeaderExtensionUri("URN:IETF:PARAMS:RTP-HDREXT:TOFFSET"), Is.False);
            Assert.That(registry.IsRegisteredHeaderExtensionUri("urn:example:unknown"), Is.False);
        });
    }

    [Test]
    public void EmptyTokens_AreNotRegistered()
    {
        ISdpRegistry registry = new SdpRegistry();

        Assert.Multiple(() =>
        {
            Assert.That(registry.IsRegisteredMediaType(string.Empty), Is.False);
            Assert.That(registry.IsRegisteredTransportProtocol(string.Empty), Is.False);
            Assert.That(registry.IsRegisteredBandwidthType(string.Empty), Is.False);
            Assert.That(registry.IsRegisteredNetworkType(string.Empty), Is.False);
            Assert.That(registry.IsRegisteredAddressType(string.Empty), Is.False);
            Assert.That(registry.IsRegisteredEncryptionMethod(string.Empty), Is.False);
            Assert.That(registry.IsRegisteredAttributeName(string.Empty), Is.False);
            Assert.That(registry.IsRegisteredGroupSemantic(string.Empty), Is.False);
            Assert.That(registry.IsRegisteredRtpEncoding(string.Empty, string.Empty), Is.False);
            Assert.That(registry.IsRegisteredHeaderExtensionUri(string.Empty), Is.False);
        });
    }
}
