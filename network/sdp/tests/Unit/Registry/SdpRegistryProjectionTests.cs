namespace sdp.Tests.Unit.Registry;

public class SdpRegistryProjectionTests
{
    [Test]
    public void TokenProjection_ProvidesCanonicalMetadataAndReferences()
    {
        ISdpRegistry registry = new SdpRegistry();
        SdpRegisteredToken media = registry.GetMediaTypeRegistration("AUDIO");
        SdpRegisteredToken protocol = registry.GetTransportProtocolRegistration("RTP/AVP");
        SdpRegisteredToken bandwidth = registry.GetBandwidthTypeRegistration("AS");
        SdpRegisteredToken network = registry.GetNetworkTypeRegistration("IN");
        SdpRegisteredToken address = registry.GetAddressTypeRegistration("IP6");

        Assert.Multiple(() =>
        {
            Assert.That(media.Found, Is.True);
            Assert.That(media.RegistryId, Is.EqualTo("sdp-parameters-1"));
            Assert.That(media.Token, Is.EqualTo("audio"));
            Assert.That(media.Name, Is.EqualTo("audio"));
            Assert.That(media.Status, Is.EqualTo("Registered"));
            Assert.That(media.References, Is.EqualTo(new[] { "rfc:rfc8866" }));
            Assert.That(protocol.Found, Is.True);
            Assert.That(protocol.Token, Is.EqualTo("RTP/AVP"));
            Assert.That(bandwidth.References, Does.Contain("rfc:rfc8859"));
            Assert.That(bandwidth.MuxCategory, Is.EqualTo(SdpMuxCategory.Sum));
            Assert.That(bandwidth.MuxCategoryToken, Is.EqualTo("SUM"));
            Assert.That(network.Name, Is.EqualTo("IN"));
            Assert.That(address.Name, Is.EqualTo("IP6"));
        });
    }

    [Test]
    public void TokenProjection_DerivesObsoleteAndDeprecatedStatuses()
    {
        ISdpRegistry registry = new SdpRegistry();
        SdpRegisteredToken encryption = registry.GetEncryptionMethodRegistration("clear");
        SdpRegisteredToken group = registry.GetGroupSemanticRegistration("FEC");
        SdpAttributeRegistration category = registry.GetAttributeRegistration("cat");

        Assert.Multiple(() =>
        {
            Assert.That(encryption.Found, Is.True);
            Assert.That(encryption.Status, Is.EqualTo("Obsolete"));
            Assert.That(group.Found, Is.True);
            Assert.That(group.Description, Is.EqualTo("Forward Error Correction (Deprecated)"));
            Assert.That(group.Status, Is.EqualTo("Deprecated"));
            Assert.That(category.Found, Is.True);
            Assert.That(category.Status, Is.EqualTo("Obsolete"));
        });
    }

    [Test]
    public void AttributeProjection_ProvidesTypedScopesMuxCategoryAndRawTokens()
    {
        ISdpRegistry registry = new SdpRegistry();
        SdpAttributeRegistration candidate = registry.GetAttributeRegistration("CANDIDATE");
        SdpAttributeRegistration extMap = registry.GetAttributeRegistration("extmap");
        SdpAttributeRegistration inherited = registry.GetAttributeRegistration("acap");

        Assert.Multiple(() =>
        {
            Assert.That(candidate.Found, Is.True);
            Assert.That(candidate.RegistryId, Is.EqualTo("sdp-att-field"));
            Assert.That(candidate.Name, Is.EqualTo("candidate"));
            Assert.That(candidate.ScopeText, Is.EqualTo("media"));
            Assert.That(candidate.Scopes, Is.EqualTo(new[] { SdpScope.Media }));
            Assert.That(candidate.MuxCategory, Is.EqualTo(SdpMuxCategory.Transport));
            Assert.That(candidate.MuxCategoryToken, Is.EqualTo("TRANSPORT"));
            Assert.That(candidate.References, Does.Contain("rfc:rfc8839"));
            Assert.That(extMap.Scopes, Is.EqualTo(new[] { SdpScope.Session, SdpScope.Media }));
            Assert.That(extMap.MuxCategory, Is.EqualTo(SdpMuxCategory.Special));
            Assert.That(inherited.MuxCategory, Is.EqualTo(SdpMuxCategory.Unknown));
            Assert.That(inherited.MuxCategoryToken, Is.EqualTo("INHERIT"));
        });
    }

    [Test]
    public void PayloadProjection_DistinguishesAssignedReservedUnassignedAndDynamicRecords()
    {
        ISdpRegistry registry = new SdpRegistry();
        SdpRegisteredToken assigned = registry.GetRtpPayloadTypeRegistration(0);
        SdpRegisteredToken reserved = registry.GetRtpPayloadTypeRegistration(1);
        SdpRegisteredToken unassigned = registry.GetRtpPayloadTypeRegistration(20);
        SdpRegisteredToken dynamic = registry.GetRtpPayloadTypeRegistration(96);

        Assert.Multiple(() =>
        {
            Assert.That(assigned.Found, Is.True);
            Assert.That(assigned.Token, Is.EqualTo("0"));
            Assert.That(assigned.Name, Is.EqualTo("PCMU"));
            Assert.That(assigned.Status, Is.EqualTo("Registered"));
            Assert.That(reserved.Status, Is.EqualTo("Reserved"));
            Assert.That(unassigned.Status, Is.EqualTo("Unassigned"));
            Assert.That(dynamic.Token, Is.EqualTo("96-127"));
            Assert.That(dynamic.Name, Is.EqualTo("dynamic"));
            Assert.That(dynamic.Status, Is.EqualTo("Dynamic"));
        });
    }

    [Test]
    public void RtpProjections_ProvideEncodingAndHeaderExtensionMetadata()
    {
        ISdpRegistry registry = new SdpRegistry();
        SdpRegisteredToken encoding = registry.GetRtpEncodingRegistration("AUDIO", "OPUS");
        SdpRegisteredToken extension = registry.GetHeaderExtensionRegistration("urn:ietf:params:rtp-hdrext:sdes:mid");

        Assert.Multiple(() =>
        {
            Assert.That(encoding.Found, Is.True);
            Assert.That(encoding.RegistryId, Is.EqualTo("rtp-parameters-2"));
            Assert.That(encoding.Token, Is.EqualTo("opus"));
            Assert.That(encoding.Name, Is.EqualTo("opus"));
            Assert.That(encoding.Description, Is.EqualTo("audio"));
            Assert.That(encoding.References, Does.Contain("rfc:rfc7587"));
            Assert.That(extension.Found, Is.True);
            Assert.That(extension.RegistryId, Is.EqualTo("sdes-compact-header-extensions"));
            Assert.That(extension.Description, Is.EqualTo("Media identification"));
            Assert.That(extension.References, Does.Contain("rfc:rfc9143"));
        });
    }

    [Test]
    public void UnknownProjection_PreservesQueryAndUsesExplicitFailureState()
    {
        ISdpRegistry registry = new SdpRegistry();
        SdpRegisteredToken media = registry.GetMediaTypeRegistration("future-media");
        SdpRegisteredToken protocol = registry.GetTransportProtocolRegistration("FUTURE/PROTOCOL");
        SdpRegisteredToken bandwidth = registry.GetBandwidthTypeRegistration("FUTURE-BANDWIDTH");
        SdpRegisteredToken network = registry.GetNetworkTypeRegistration("FUTURE-NETWORK");
        SdpRegisteredToken address = registry.GetAddressTypeRegistration("FUTURE-ADDRESS");
        SdpRegisteredToken encryption = registry.GetEncryptionMethodRegistration("future-encryption");
        SdpRegisteredToken payload = registry.GetRtpPayloadTypeRegistration(-1);
        SdpAttributeRegistration attribute = registry.GetAttributeRegistration("future-attribute");
        SdpRegisteredToken group = registry.GetGroupSemanticRegistration("FUTURE-GROUP");
        SdpRegisteredToken encoding = registry.GetRtpEncodingRegistration("audio", "future-encoding");
        SdpRegisteredToken extension = registry.GetHeaderExtensionRegistration("urn:example:future-extension");

        Assert.Multiple(() =>
        {
            Assert.That(media.Found, Is.False);
            Assert.That(media.RegistryId, Is.EqualTo("sdp-parameters-1"));
            Assert.That(media.Token, Is.EqualTo("future-media"));
            Assert.That(media.Name, Is.EqualTo("future-media"));
            Assert.That(media.Status, Is.EqualTo("Unknown"));
            Assert.That(media.References, Is.Empty);
            Assert.That(protocol.Found, Is.False);
            Assert.That(protocol.RegistryId, Is.EqualTo("sdp-parameters-2"));
            Assert.That(protocol.Token, Is.EqualTo("FUTURE/PROTOCOL"));
            Assert.That(bandwidth.Found, Is.False);
            Assert.That(bandwidth.RegistryId, Is.EqualTo("sdp-parameters-3"));
            Assert.That(network.Found, Is.False);
            Assert.That(network.RegistryId, Is.EqualTo("sdp-parameters-4"));
            Assert.That(address.Found, Is.False);
            Assert.That(address.RegistryId, Is.EqualTo("sdp-parameters-5"));
            Assert.That(encryption.Found, Is.False);
            Assert.That(encryption.RegistryId, Is.EqualTo("sdp-parameters-6"));
            Assert.That(payload.Found, Is.False);
            Assert.That(payload.RegistryId, Is.EqualTo("rtp-parameters-1"));
            Assert.That(payload.Token, Is.EqualTo("-1"));
            Assert.That(attribute.Found, Is.False);
            Assert.That(attribute.RegistryId, Is.EqualTo("sdp-att-field"));
            Assert.That(attribute.Name, Is.EqualTo("future-attribute"));
            Assert.That(attribute.Status, Is.EqualTo("Unknown"));
            Assert.That(attribute.Scopes, Is.Empty);
            Assert.That(attribute.MuxCategory, Is.EqualTo(SdpMuxCategory.Unknown));
            Assert.That(group.Found, Is.False);
            Assert.That(group.RegistryId, Is.EqualTo("sdp-parameters-13"));
            Assert.That(encoding.Found, Is.False);
            Assert.That(encoding.RegistryId, Is.EqualTo("rtp-parameters-2"));
            Assert.That(encoding.Token, Is.EqualTo("future-encoding"));
            Assert.That(extension.Found, Is.False);
            Assert.That(extension.RegistryId, Is.EqualTo("rtp-header-extensions"));
            Assert.That(extension.Token, Is.EqualTo("urn:example:future-extension"));
        });
    }

    [Test]
    public void DuplicateEncodingAlias_IsProjectedFromMatchingMediaRecord()
    {
        ISdpRegistry registry = new SdpRegistry();
        SdpRegisteredToken audio = registry.GetRtpEncodingRegistration("audio", "red");
        SdpRegisteredToken text = registry.GetRtpEncodingRegistration("text", "RED");
        SdpRegisteredToken video = registry.GetRtpEncodingRegistration("video", "red");

        Assert.Multiple(() =>
        {
            Assert.That(audio.Found, Is.True);
            Assert.That(audio.Token, Is.EqualTo("RED"));
            Assert.That(audio.Description, Is.EqualTo("audio"));
            Assert.That(audio.References, Is.EqualTo(new[] { "rfc:rfc2198", "rfc:rfc3555" }));
            Assert.That(text.Found, Is.True);
            Assert.That(text.Token, Is.EqualTo("red"));
            Assert.That(text.Description, Is.EqualTo("text"));
            Assert.That(text.References, Is.EqualTo(new[] { "rfc:rfc4102" }));
            Assert.That(video.Found, Is.False);
            Assert.That(video.Token, Is.EqualTo("red"));
        });
    }

    [Test]
    public void ProjectionCollections_AreIndependentBetweenResults()
    {
        ISdpRegistry registry = new SdpRegistry();
        SdpRegisteredToken firstToken = registry.GetMediaTypeRegistration("audio");
        SdpRegisteredToken secondToken = registry.GetMediaTypeRegistration("audio");
        SdpAttributeRegistration firstAttribute = registry.GetAttributeRegistration("extmap");
        SdpAttributeRegistration secondAttribute = registry.GetAttributeRegistration("extmap");

        firstToken.References.Clear();
        firstAttribute.References.Clear();
        firstAttribute.Scopes.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(secondToken.References, Is.EqualTo(new[] { "rfc:rfc8866" }));
            Assert.That(secondAttribute.References, Is.Not.Empty);
            Assert.That(secondAttribute.Scopes, Is.EqualTo(new[] { SdpScope.Session, SdpScope.Media }));
        });
    }
}
