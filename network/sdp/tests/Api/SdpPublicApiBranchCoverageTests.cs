namespace sdp.Tests.Api;

public class SdpPublicApiBranchCoverageTests
{
    [Test]
    public void Editor_RejectsEveryInvalidIndexAndUninitializedCollectionShape()
    {
        ISdpEditor editor = new SdpEditor();
        SdpSessionDescription description = DescriptionWithMedia();
        description.Attributes.Add(new SdpAttribute { Name = "x-test" });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.MoveMedia(description, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.MoveMedia(description, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.MoveMedia(description, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.MoveMedia(description, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.ReplaceSessionAttribute(description, -1, new SdpAttribute()));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.ReplaceSessionAttribute(description, 1, new SdpAttribute()));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.InsertSessionAttribute(description, -1, new SdpAttribute()));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.InsertSessionAttribute(description, 2, new SdpAttribute()));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.RemoveMediaAt(description, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.RemoveMediaAt(description, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.InsertMedia(description, -1, new SdpMediaDescription()));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.InsertMedia(description, 2, new SdpMediaDescription()));
            Assert.Throws<ArgumentNullException>(() => editor.RemoveSessionAttributesByName(description, null!));
        });

        description.MediaDescriptions[0].Port = 0;
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.ReactivateMedia(description, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.ReactivateMedia(description, 0, 65536));
        });
        description.MediaDescriptions[0].HasPortCount = true;
        description.MediaDescriptions[0].PortCount = 2;
        Assert.Throws<ArgumentOutOfRangeException>(() => editor.ReactivateMedia(description, 0, 65535));

        SdpSessionDescription missingAttributes = new SdpSessionDescription { Attributes = null! };
        SdpSessionDescription missingMedia = new SdpSessionDescription { MediaDescriptions = null! };
        SdpSessionDescription nullMedia = new SdpSessionDescription();
        nullMedia.MediaDescriptions.Add(null!);
        SdpSessionDescription missingMediaAttributes = new SdpSessionDescription();
        missingMediaAttributes.MediaDescriptions.Add(new SdpMediaDescription { Attributes = null! });

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => editor.CloneDescription(missingAttributes));
            Assert.Throws<ArgumentException>(() => editor.CloneDescription(missingMedia));
            Assert.Throws<ArgumentNullException>(() => editor.RejectMedia(nullMedia, 0));
            Assert.Throws<ArgumentException>(() => editor.RejectMedia(missingMediaAttributes, 0));
        });
    }

    [Test]
    public void DataChannelService_RejectsEveryIncompleteCapabilityGraphShape()
    {
        ISdpDataChannelService service = new SdpDataChannelService();
        SdpSctpDescription validCapability = new SdpSctpDescription { Format = "webrtc-datachannel" };
        SdpSctpDescription missingFormat = new SdpSctpDescription { Format = null! };

        Assert.Throws<ArgumentException>(() => service.IntersectSctpCapabilities(missingFormat, validCapability));

        SdpDataChannelAttribute missingParameters = new SdpDataChannelAttribute { Parameters = null! };
        SdpDataChannelAttribute missingAttributes = new SdpDataChannelAttribute { SubprotocolAttributes = null! };
        AssertMalformedChannel(service, missingParameters);
        AssertMalformedChannel(service, missingAttributes);

        SdpDataChannelParameter missingName = CompleteParameter();
        missingName.Name = null!;
        AssertMalformedParameter(service, missingName);
        SdpDataChannelParameter missingValue = CompleteParameter();
        missingValue.Value = null!;
        AssertMalformedParameter(service, missingValue);
        SdpDataChannelParameter missingRawText = CompleteParameter();
        missingRawText.RawText = null!;
        AssertMalformedParameter(service, missingRawText);

        AssertMalformedAttribute(service, null!);
        SdpAttribute missingAttributeName = CompleteAttribute();
        missingAttributeName.Name = null!;
        AssertMalformedAttribute(service, missingAttributeName);
        SdpAttribute missingAttributeValue = CompleteAttribute();
        missingAttributeValue.Value = null!;
        AssertMalformedAttribute(service, missingAttributeValue);
        SdpAttribute missingAttributeRawText = CompleteAttribute();
        missingAttributeRawText.RawText = null!;
        AssertMalformedAttribute(service, missingAttributeRawText);
        SdpAttribute missingAttributeLocation = CompleteAttribute();
        missingAttributeLocation.Location = null!;
        AssertMalformedAttribute(service, missingAttributeLocation);
    }

    [Test]
    public void ExtMapService_RejectsNullElementsMappingsAndUnknownDirections()
    {
        ISdpExtMapService service = new SdpExtMapService();
        List<SdpExtMapAttribute> mappingsWithNull = new List<SdpExtMapAttribute> { null! };
        List<SdpHeaderExtension> capabilitiesWithNull = new List<SdpHeaderExtension> { null! };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => service.IntersectExtMaps(mappingsWithNull, new List<SdpHeaderExtension>()));
            Assert.Throws<ArgumentException>(() => service.IntersectExtMaps(new List<SdpExtMapAttribute>(), capabilitiesWithNull));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.SelectAnswerDirection((SdpDirection)999, SdpDirection.SendReceive));
            Assert.Throws<ArgumentNullException>(() => service.RemapExtMapIds(new SdpEffectiveMedia(), null!));
        });
    }

    [Test]
    public void GroupService_RejectsEveryNullAndIndexBranchAndPreservesExtensionSemantics()
    {
        ISdpGroupService service = new SdpGroupService();
        SdpSessionDescription description = DescriptionWithMedia();
        SdpGroupAttribute group = new SdpGroupAttribute();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => service.UpdateGroup(null!, SdpGroupSemantic.Bundle, string.Empty, new List<string>()));
            Assert.Throws<ArgumentNullException>(() => service.RemoveGroup(null!, group));
            Assert.Throws<ArgumentNullException>(() => service.ResolveMediaId(null!, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.ResolveMediaId(description, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => service.ResolveMediaId(description, 1));
            Assert.Throws<ArgumentNullException>(() => service.AddGroup(description, SdpGroupSemantic.Unknown, null!, new List<string>()));
            Assert.Throws<ArgumentNullException>(() => service.AddGroup(description, SdpGroupSemantic.Unknown, "X-VENDOR", null!));
            Assert.Throws<ArgumentException>(() => service.AddGroup(description, SdpGroupSemantic.Unknown, "X-VENDOR", new List<string> { null! }));
        });

        SdpGroupAttribute extension = service.AddGroup(description, SdpGroupSemantic.Unknown, "X-VENDOR", new List<string> { "audio" });

        Assert.Multiple(() =>
        {
            Assert.That(extension.Semantic, Is.EqualTo(SdpGroupSemantic.Unknown));
            Assert.That(extension.SemanticToken, Is.EqualTo("X-VENDOR"));
            Assert.That(extension.IdentificationTags, Is.EqualTo(new[] { "audio" }));
            Assert.That(service.ResolveGroups(description).Single().SemanticToken, Is.EqualTo("X-VENDOR"));
        });
    }

    [Test]
    public void IceAndStreamServices_RejectRemainingNullAndInvalidGraphInputs()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        ISdpIceService ice = new SdpIceService();
        ISdpStreamService streams = new SdpStreamService();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => ice.AddCandidate(description, 0, null!));
            Assert.Throws<ArgumentException>(() => streams.FilterSimulcastLayers(description, 0, new List<string> { null! }));
            Assert.Throws<ArgumentException>(() => streams.FilterSimulcastLayers(description, 0, new List<string> { "bad id" }));
            Assert.Throws<ArgumentException>(() => streams.FilterSimulcastLayers(description, 0, new List<string> { "one", "one" }));
        });
    }

    static SdpSessionDescription DescriptionWithMedia()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Audio,
            MediaKind = SdpMediaKind.Audio,
            Port = 9,
            Protocol = SdpTransportProtocols.RtpAvp
        });
        return description;
    }

    static SdpDataChannelParameter CompleteParameter()
    {
        return new SdpDataChannelParameter
        {
            Name = "subprotocol",
            HasValue = true,
            Value = "chat",
            RawText = "subprotocol=chat"
        };
    }

    static SdpAttribute CompleteAttribute()
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = SdpAttributeNames.DataChannelSubprotocol,
            Value = "0 label:chat",
            RawText = SdpAttributeNames.DataChannelSubprotocol + ":0 label:chat",
            Location = new SdpSourceLocation()
        };
    }

    static void AssertMalformedChannel(ISdpDataChannelService service, SdpDataChannelAttribute channel)
    {
        List<SdpDataChannelAttribute> channels = new List<SdpDataChannelAttribute> { channel };
        Assert.Throws<ArgumentException>(() => service.IntersectDataChannelCapabilities(channels, new List<string>()));
    }

    static void AssertMalformedParameter(ISdpDataChannelService service, SdpDataChannelParameter parameter)
    {
        SdpDataChannelAttribute channel = new SdpDataChannelAttribute();
        channel.Parameters.Add(parameter);
        AssertMalformedChannel(service, channel);
    }

    static void AssertMalformedAttribute(ISdpDataChannelService service, SdpAttribute attribute)
    {
        SdpDataChannelAttribute channel = new SdpDataChannelAttribute();
        channel.SubprotocolAttributes.Add(attribute);
        AssertMalformedChannel(service, channel);
    }
}
