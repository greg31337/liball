using System.Text.Json;

namespace sdp.Tests.Conformance.Rfc3264;

public class SdpSubsequentOfferConformanceTests
{
    [Test]
    public void SubsequentOffer_RejectsAddsAndReactivatesMediaWithStableIdentityAndMappings()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities initialCapabilities = Capabilities(
            Rtp("audio", "audio", 5000, 111, "opus", 48000, 3),
            Rtp("video", "video", 5002, 96, "VP8", 90000, 7));
        SdpSessionDescription initial = service.CreateInitialOffer(Session(), Options(initialCapabilities)).Description!;

        SdpEndpointCapabilities changedCapabilities = Capabilities(
            Rtp("video", "video", 6002, 120, "VP8", 90000, 2),
            Rtp("video", "screen", 6004, 97, "H264", 90000, 9));
        SdpNegotiationResult changed = service.CreateSubsequentOffer(initial, Options(changedCapabilities));

        SdpEndpointCapabilities reactivatedCapabilities = Capabilities(
            Rtp("audio", "audio", 7000, 110, "opus", 48000, 1),
            Rtp("video", "video", 7002, 121, "VP8", 90000, 4),
            Rtp("video", "screen", 7004, 122, "H264", 90000, 5));
        SdpNegotiationOptions reactivatedOptions = Options(reactivatedCapabilities);
        SdpNegotiationResult reactivated = service.CreateSubsequentOffer(changed.Description!, reactivatedOptions);
        SdpNegotiationResult unchanged = service.CreateSubsequentOffer(reactivated.Description!, reactivatedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(MediaIds(changed.Description!), Is.EqualTo(new[] { "audio", "video", "screen" }));
            Assert.That(changed.Description!.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 0, 6002, 6004 }));
            Assert.That(changed.Media.Select(media => media.Accepted), Is.EqualTo(new[] { false, true, true }));
            Assert.That(changed.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.RejectedByPolicy));
            Assert.That(changed.BundleGroups.Single().MediaIds, Is.EqualTo(new[] { "video", "screen" }));
            Assert.That(changed.Description.Origin!.SessionVersion, Is.EqualTo("2"));
            Assert.That(changed.Media[1].PayloadMappings.Single().NegotiatedPayloadType, Is.EqualTo(96));
            Assert.That(changed.Media[1].HeaderExtensionMappings.Single().NegotiatedId, Is.EqualTo(7));

            Assert.That(MediaIds(reactivated.Description!), Is.EqualTo(new[] { "audio", "video", "screen" }));
            Assert.That(reactivated.Description!.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 7000, 7002, 7004 }));
            Assert.That(reactivated.Media.All(media => media.Accepted), Is.True);
            Assert.That(reactivated.Rejections, Is.Empty);
            Assert.That(reactivated.Media[0].PayloadMappings.Single().NegotiatedPayloadType, Is.EqualTo(111));
            Assert.That(reactivated.Media[0].HeaderExtensionMappings.Single().NegotiatedId, Is.EqualTo(7));
            Assert.That(reactivated.Media[1].PayloadMappings.Single().NegotiatedPayloadType, Is.EqualTo(96));
            Assert.That(reactivated.Media[2].PayloadMappings.Single().NegotiatedPayloadType, Is.EqualTo(97));
            Assert.That(reactivated.BundleGroups.Single().MediaIds, Is.EqualTo(new[] { "video", "screen", "audio" }));
            Assert.That(reactivated.BundleGroups[0].TaggedMediaId, Is.EqualTo("video"));
            Assert.That(reactivated.Media[0].TransportMediaIndex, Is.EqualTo(1));
            Assert.That(reactivated.Description.Origin!.SessionVersion, Is.EqualTo("3"));
            Assert.That(unchanged.Description!.Origin!.SessionVersion, Is.EqualTo("3"));
            Assert.That(initial.MediaDescriptions[0].Port, Is.EqualTo(5000));
            Assert.That(initial.Origin!.SessionVersion, Is.EqualTo("1"));
            Assert.That(reactivatedCapabilities.Media[0].Codecs[0].PreferredPayloadType, Is.EqualTo(110));
        });
    }

    [Test]
    public void SubsequentOffer_CanRemoveRejectedSlotsAndDissolveBundle()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities initialCapabilities = Capabilities(
            Rtp("audio", "audio", 5000, 111, "opus", 48000, 3),
            Rtp("video", "video", 5002, 96, "VP8", 90000, 7));
        SdpSessionDescription initial = service.CreateInitialOffer(Session(), Options(initialCapabilities)).Description!;
        SdpEndpointCapabilities videoOnly = Capabilities(Rtp("video", "video", 6002, 120, "VP8", 90000, 2));
        videoOnly.SupportsBundle = false;
        SdpNegotiationOptions options = Options(videoOnly);
        options.PreserveRejectedMedia = false;

        SdpNegotiationResult result = service.CreateSubsequentOffer(initial, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Description!.MediaDescriptions, Has.Count.EqualTo(1));
            Assert.That(MediaIds(result.Description), Is.EqualTo(new[] { "video" }));
            Assert.That(result.Media.Single().PayloadMappings.Single().NegotiatedPayloadType, Is.EqualTo(96));
            Assert.That(result.BundleGroups, Is.Empty);
            Assert.That(result.BundleTransports, Is.Empty);
            Assert.That(result.Description.Attributes.Any(attribute => attribute.Name == SdpAttributeNames.Group), Is.False);
            Assert.That(result.Description.Origin!.SessionVersion, Is.EqualTo("2"));
            Assert.That(initial.MediaDescriptions, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void SubsequentOffer_LateVersionFailureDoesNotMutateAnyInput()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities initialCapabilities = Capabilities(Rtp("audio", "audio", 5000, 111, "opus", 48000, 3));
        SdpSessionDescription previous = service.CreateInitialOffer(Session(), Options(initialCapabilities)).Description!;
        previous.Origin!.SessionVersion = ulong.MaxValue.ToString();
        SdpEndpointCapabilities changedCapabilities = Capabilities(Rtp("audio", "audio", 6000, 110, "opus", 48000, 1));
        SdpNegotiationOptions options = Options(changedCapabilities);
        string previousSnapshot = Snapshot(previous);
        string optionsSnapshot = Snapshot(options);

        Assert.That(() => service.CreateSubsequentOffer(previous, options), Throws.InvalidOperationException);

        Assert.Multiple(() =>
        {
            Assert.That(Snapshot(previous), Is.EqualTo(previousSnapshot));
            Assert.That(Snapshot(options), Is.EqualTo(optionsSnapshot));
            Assert.That(changedCapabilities.Media[0].Codecs[0].PreferredPayloadType, Is.EqualTo(110));
        });
    }

    static SdpEndpointCapabilities Capabilities(params SdpMediaCapability[] media)
    {
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities { SupportsBundle = true };
        capabilities.Media.AddRange(media);
        return capabilities;
    }

    static SdpNegotiationOptions Options(SdpEndpointCapabilities capabilities)
    {
        return new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            PreserveMediaOrder = true,
            PreserveMediaIds = true,
            PreservePayloadTypes = true,
            PreserveRejectedMedia = true
        };
    }

    static SdpMediaCapability Rtp(string mediaType, string mediaId, int port, int payloadType, string encodingName, int clockRate, int extensionId)
    {
        SdpMediaKind kind = mediaType == "audio" ? SdpMediaKind.Audio : SdpMediaKind.Video;
        SdpMediaCapability media = new SdpMediaCapability
        {
            MediaKind = kind,
            MediaType = mediaType,
            MediaId = mediaId,
            Port = port,
            SupportsRtcpMux = true
        };
        media.Protocols.Add(SdpTransportProtocols.RtpAvpf);
        SdpCodecCapability codec = new SdpCodecCapability
        {
            MediaKind = kind,
            MediaType = mediaType,
            EncodingName = encodingName,
            ClockRate = clockRate,
            HasPreferredPayloadType = true,
            PreferredPayloadType = payloadType
        };
        if (kind == SdpMediaKind.Audio)
        {
            codec.HasChannelCount = true;
            codec.ChannelCount = 2;
        }
        media.Codecs.Add(codec);
        media.HeaderExtensions.Add(new SdpHeaderExtension
        {
            Uri = SdpHeaderExtensionUris.MediaId,
            HasPreferredId = true,
            PreferredId = extensionId
        });
        return media;
    }

    static SdpSessionDescription Session()
    {
        return new SdpSessionFactory().CreateSession(
            new SdpOrigin
            {
                Username = "-",
                SessionId = "149",
                SessionVersion = "1",
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                UnicastAddress = "192.0.2.149",
                AddressKind = SdpAddressKind.Ipv4
            },
            "Subsequent offer conformance",
            new SdpConnectionData
            {
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                Address = "192.0.2.149",
                AddressKind = SdpAddressKind.Ipv4
            },
            new SdpTiming { StartTime = "0", StopTime = "0" });
    }

    static string[] MediaIds(SdpSessionDescription description)
    {
        return description.MediaDescriptions.Select(media => AttributeValue(media, SdpAttributeNames.MediaId) ?? string.Empty).ToArray();
    }

    static string? AttributeValue(SdpMediaDescription media, string name)
    {
        return media.Attributes.FirstOrDefault(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static string Snapshot(object value)
    {
        return JsonSerializer.Serialize(value, value.GetType(), new JsonSerializerOptions { IncludeFields = true });
    }
}
