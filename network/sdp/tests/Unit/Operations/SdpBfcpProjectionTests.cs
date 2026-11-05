namespace sdp.Tests.Unit.Operations;

public class SdpBfcpProjectionTests
{
    [TestCase("TCP/BFCP", "1")]
    [TestCase("tcp/dtls/bfcp", "1")]
    [TestCase("TCP/TLS/BFCP", "1")]
    [TestCase("UDP/BFCP", "2")]
    [TestCase("udp/tls/bfcp", "2")]
    public void ResolveMediaBfcp_RecognizesCurrentTransportsAndAppliesVersionDefault(string protocol, string expectedVersion)
    {
        SdpSessionDescription description = DescriptionWithMedia(protocol);
        ISdpDataChannelService service = new SdpDataChannelService();

        SdpBfcpDescription? resolved = service.ResolveMediaBfcp(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved!.Versions, Is.EqualTo(new[] { expectedVersion }));
            Assert.That(resolved.FloorIds, Is.Empty);
        });
    }

    [Test]
    public void ResolveMediaBfcp_ProjectsValidAttributesInSourceOrder()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.TcpTlsBfcp);
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(PropertyAttribute(SdpAttributeNames.FloorControl));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorControl, "future-role"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorControl, "  c-only s-only  "));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.ConferenceId, "invalid"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.ConferenceId, "004321"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.UserId, "1234"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorId, "invalid"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorId, "1 mstrm:audio presentation"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorId, "2 m-stream:video"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.BfcpVersion, "invalid"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.BfcpVersion, "1 2"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "active"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Connection, "new"));
        ISdpDataChannelService service = new SdpDataChannelService();

        SdpBfcpDescription? resolved = service.ResolveMediaBfcp(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved!.FloorControl, Is.EqualTo("c-only s-only"));
            Assert.That(resolved.ConferenceId, Is.EqualTo("004321"));
            Assert.That(resolved.UserId, Is.EqualTo("1234"));
            Assert.That(resolved.FloorIds, Is.EqualTo(new[] { "1 mstrm:audio presentation", "2 m-stream:video" }));
            Assert.That(resolved.Versions, Is.EqualTo(new[] { "1", "2" }));
            Assert.That(resolved.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Active));
            Assert.That(resolved.SetupRoleToken, Is.EqualTo("active"));
            Assert.That(resolved.ConnectionRole, Is.EqualTo("new"));
        });
    }

    [Test]
    public void ResolveMediaBfcp_UsesEffectiveSessionConnectionAttributes()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.TcpBfcp);
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "passive"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Connection, "existing"));
        ISdpDataChannelService service = new SdpDataChannelService();

        SdpBfcpDescription? resolved = service.ResolveMediaBfcp(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved!.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Passive));
            Assert.That(resolved.SetupRoleToken, Is.EqualTo("passive"));
            Assert.That(resolved.ConnectionRole, Is.EqualTo("existing"));
        });
    }

    [Test]
    public void ResolveMediaBfcp_ReturnsIndependentProjectionsAndNullForOtherTransports()
    {
        SdpSessionDescription description = DescriptionWithMedia(SdpTransportProtocols.UdpBfcp);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.FloorId, "1 mstrm:audio"));
        description.MediaDescriptions.Add(Media(SdpTransportProtocols.UdpDtlsSctp));
        ISdpDataChannelService service = new SdpDataChannelService();

        SdpBfcpDescription? first = service.ResolveMediaBfcp(description, 0);
        SdpBfcpDescription? second = service.ResolveMediaBfcp(description, 0);
        SdpBfcpDescription? other = service.ResolveMediaBfcp(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first!.FloorIds, Is.Not.SameAs(second!.FloorIds));
            Assert.That(first.Versions, Is.Not.SameAs(second.Versions));
            Assert.That(other, Is.Null);
        });

        first!.FloorIds[0] = "changed";
        Assert.That(second!.FloorIds[0], Is.EqualTo("1 mstrm:audio"));
    }

    [Test]
    public void ResolveMediaBfcp_ValidatesDescriptionAndMediaIndex()
    {
        ISdpDataChannelService service = new SdpDataChannelService();
        SdpSessionDescription empty = new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ResolveMediaBfcp(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveMediaBfcp(empty, -1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveMediaBfcp(empty, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
        });
    }

    static SdpSessionDescription DescriptionWithMedia(string protocol)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(Media(protocol));
        return description;
    }

    static SdpMediaDescription Media(string protocol)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Application,
            MediaKind = SdpMediaKind.Application,
            Protocol = protocol
        };
        media.Formats.Add("*");
        return media;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }
}
