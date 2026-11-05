namespace sdp.Tests.Unit.Operations;

public class SdpDataChannelServiceTests
{
    [Test]
    public void ResolveMediaSctp_ExposesModernSignalingAndDefaultMessageSize()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription media = description.MediaDescriptions[0];
        SdpAttribute port = ValueAttribute("SCTP-PORT", "5000");
        media.Attributes.Add(port);
        ISdpDataChannelService service = new SdpDataChannelService();

        SdpSctpDescription resolved = service.ResolveMediaSctp(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Port, Is.EqualTo(5000));
            Assert.That(resolved.PortAttribute!.SourceAttribute, Is.SameAs(port));
            Assert.That(resolved.Format, Is.EqualTo(SdpDataChannelTokens.WebRtcDataChannel));
            Assert.That(resolved.LegacySctpMap, Is.False);
            Assert.That(resolved.HasMaximumMessageSize, Is.False);
            Assert.That(resolved.MaximumMessageSize, Is.EqualTo(SdpDataChannelTokens.DefaultMaximumMessageSize));
        });
    }

    [Test]
    public void ResolveDataChannels_AttachesValidSubprotocolAttributesInSourceOrder()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription media = description.MediaDescriptions[0];
        SdpAttribute firstMap = ValueAttribute("dcmap", "1 subprotocol=\"msrp\";label=\"messages\";future=one");
        SdpAttribute secondMap = ValueAttribute("dcmap", "2");
        media.Attributes.Add(ValueAttribute("dcsa", "1 accept-types:message/cpim text/plain"));
        media.Attributes.Add(ValueAttribute("dcmap", "bad"));
        media.Attributes.Add(firstMap);
        media.Attributes.Add(ValueAttribute("dcsa", "3 ignored:value"));
        media.Attributes.Add(ValueAttribute("dcsa", "1 path:msrp://example.test/id;dc"));
        media.Attributes.Add(secondMap);
        media.Attributes.Add(ValueAttribute("dcsa", "2 inactive"));
        media.Attributes.Add(ValueAttribute("dcsa", "malformed"));
        ISdpDataChannelService service = new SdpDataChannelService();

        List<SdpDataChannelAttribute> first = service.ResolveDataChannels(description, 0);
        List<SdpDataChannelAttribute> second = service.ResolveDataChannels(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(channel => channel.ChannelId), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(first[0].SourceAttribute, Is.SameAs(firstMap));
            Assert.That(first[1].SourceAttribute, Is.SameAs(secondMap));
            Assert.That(first[0].Parameters.Select(parameter => parameter.Name), Is.EqualTo(new[] { "subprotocol", "label", "future" }));
            Assert.That(first[0].SubprotocolAttributes.Select(attribute => attribute.RawText), Is.EqualTo(new[]
            {
                "accept-types:message/cpim text/plain", "path:msrp://example.test/id;dc"
            }));
            Assert.That(first[1].SubprotocolAttributes.Select(attribute => attribute.RawText), Is.EqualTo(new[] { "inactive" }));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second[0], Is.Not.SameAs(first[0]));
            Assert.That(second[0].Parameters[0], Is.Not.SameAs(first[0].Parameters[0]));
            Assert.That(second[0].SubprotocolAttributes[0], Is.Not.SameAs(first[0].SubprotocolAttributes[0]));
            Assert.That(second[0].SubprotocolAttributes[0].Location, Is.Not.SameAs(first[0].SubprotocolAttributes[0].Location));
        });

        first[0].Parameters[0].Value = "changed";
        first[0].SubprotocolAttributes[0].Value = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(second[0].Parameters[0].Value, Is.EqualTo("msrp"));
            Assert.That(second[0].SubprotocolAttributes[0].Value, Is.EqualTo("message/cpim text/plain"));
            Assert.That(firstMap.Value, Does.Contain("msrp"));
        });
    }

    [TestCase(true, 100000L, true, 65536L, 65536L, true)]
    [TestCase(true, 0L, true, 200000L, 200000L, true)]
    [TestCase(true, 200000L, true, 0L, 200000L, true)]
    [TestCase(true, 0L, true, 0L, 0L, true)]
    [TestCase(false, 0L, true, 100000L, 65536L, true)]
    [TestCase(false, 0L, false, 0L, 65536L, false)]
    public void IntersectSctpCapabilities_AppliesDefaultsAndUnlimitedSemantics(bool remoteExplicit, long remoteSize, bool localExplicit, long localSize, long expectedSize, bool expectedExplicit)
    {
        SdpSctpDescription remote = Capability("webrtc-datachannel", remoteExplicit, remoteSize);
        remote.Port = 5000;
        remote.PortAttribute = new SdpSctpPortAttribute { Port = 5000 };
        SdpSctpDescription local = Capability("WEBRTC-DATACHANNEL", localExplicit, localSize);
        ISdpDataChannelService service = new SdpDataChannelService();

        SdpSctpDescription? intersection = service.IntersectSctpCapabilities(remote, local);

        Assert.Multiple(() =>
        {
            Assert.That(intersection, Is.Not.Null);
            Assert.That(intersection!.Port, Is.EqualTo(5000));
            Assert.That(intersection.Format, Is.EqualTo("webrtc-datachannel"));
            Assert.That(intersection.HasMaximumMessageSize, Is.EqualTo(expectedExplicit));
            Assert.That(intersection.MaximumMessageSize, Is.EqualTo(expectedSize));
            Assert.That(intersection.MaximumMessageSizeAttribute, Is.Null);
            Assert.That(intersection, Is.Not.SameAs(remote));
            Assert.That(intersection.PortAttribute, Is.Not.SameAs(remote.PortAttribute));
        });
    }

    [Test]
    public void IntersectSctpCapabilities_ReturnsNullForDifferentFormats()
    {
        ISdpDataChannelService service = new SdpDataChannelService();
        SdpSctpDescription remote = Capability("webrtc-datachannel", false, 0);
        SdpSctpDescription local = Capability("custom-data", false, 0);

        SdpSctpDescription? result = service.IntersectSctpCapabilities(remote, local);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void IntersectDataChannelCapabilities_UsesExactSubprotocolsAndOfferedOrder()
    {
        SdpDataChannelAttribute msrp = Channel(1, "msrp");
        msrp.SubprotocolAttributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "accept-types", Value = "text/plain" });
        SdpDataChannelAttribute defaultChannel = Channel(2, string.Empty);
        SdpDataChannelAttribute upper = Channel(3, "MSRP");
        SdpDataChannelAttribute duplicate = Channel(4, "bfcp");
        duplicate.Parameters.Add(Parameter(SdpDataChannelTokens.Subprotocol, "msrp"));
        List<SdpDataChannelAttribute> offered = new List<SdpDataChannelAttribute> { msrp, defaultChannel, upper, duplicate };
        List<string> supported = new List<string> { "msrp", string.Empty, "msrp" };
        ISdpDataChannelService service = new SdpDataChannelService();

        List<SdpDataChannelAttribute> result = service.IntersectDataChannelCapabilities(offered, supported);

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(channel => channel.ChannelId), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result[0], Is.Not.SameAs(msrp));
            Assert.That(result[0].Parameters[0], Is.Not.SameAs(msrp.Parameters[0]));
            Assert.That(result[0].SubprotocolAttributes[0], Is.Not.SameAs(msrp.SubprotocolAttributes[0]));
            Assert.That(result[0].SubprotocolAttributes[0].Value, Is.EqualTo("text/plain"));
        });

        result[0].SubprotocolAttributes[0].Value = "changed";
        Assert.That(msrp.SubprotocolAttributes[0].Value, Is.EqualTo("text/plain"));
    }

    [Test]
    public void ServiceArguments_AreValidated()
    {
        ISdpDataChannelService service = new SdpDataChannelService();
        SdpSessionDescription empty = new SdpSessionDescription();
        SdpSctpDescription capability = Capability("webrtc-datachannel", false, 0);
        List<SdpDataChannelAttribute> channels = new List<SdpDataChannelAttribute>();
        List<string> subprotocols = new List<string>();
        List<SdpDataChannelAttribute> channelsWithNull = new List<SdpDataChannelAttribute> { null! };
        SdpDataChannelAttribute incompleteChannel = new SdpDataChannelAttribute();
        incompleteChannel.Parameters.Add(null!);
        List<SdpDataChannelAttribute> incompleteChannels = new List<SdpDataChannelAttribute> { incompleteChannel };
        List<string> subprotocolsWithNull = new List<string> { null! };

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ResolveMediaSctp(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveDataChannels(empty, -1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveDataChannels(empty, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.IntersectSctpCapabilities(null!, capability), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("remoteCapability"));
            Assert.That(() => service.IntersectSctpCapabilities(capability, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("localCapability"));
            Assert.That(() => service.IntersectSctpCapabilities(Capability("x", true, -1), capability), Throws.ArgumentException.With.Property("ParamName").EqualTo("remoteCapability"));
            Assert.That(() => service.IntersectDataChannelCapabilities(null!, subprotocols), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("offeredChannels"));
            Assert.That(() => service.IntersectDataChannelCapabilities(channels, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("supportedSubprotocols"));
            Assert.That(() => service.IntersectDataChannelCapabilities(channelsWithNull, subprotocols), Throws.ArgumentException.With.Property("ParamName").EqualTo("offeredChannels"));
            Assert.That(() => service.IntersectDataChannelCapabilities(incompleteChannels, subprotocols), Throws.ArgumentException.With.Property("ParamName").EqualTo("offeredChannels"));
            Assert.That(() => service.IntersectDataChannelCapabilities(channels, subprotocolsWithNull), Throws.ArgumentException.With.Property("ParamName").EqualTo("supportedSubprotocols"));
        });
    }

    static SdpSessionDescription DescriptionWithMedia()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Application,
            MediaKind = SdpMediaKind.Application,
            Protocol = SdpTransportProtocols.UdpDtlsSctp
        };
        media.Formats.Add(SdpDataChannelTokens.WebRtcDataChannel);
        description.MediaDescriptions.Add(media);
        return description;
    }

    static SdpSctpDescription Capability(string format, bool explicitSize, long size)
    {
        return new SdpSctpDescription
        {
            Format = format,
            HasMaximumMessageSize = explicitSize,
            MaximumMessageSize = size
        };
    }

    static SdpDataChannelAttribute Channel(int channelId, string subprotocol)
    {
        SdpDataChannelAttribute result = new SdpDataChannelAttribute { ChannelId = channelId };
        if (subprotocol.Length > 0)
        {
            result.Parameters.Add(Parameter(SdpDataChannelTokens.Subprotocol, subprotocol));
        }
        return result;
    }

    static SdpDataChannelParameter Parameter(string name, string value)
    {
        return new SdpDataChannelParameter { Name = name, HasValue = true, Value = value, RawText = name + "=" + value };
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
