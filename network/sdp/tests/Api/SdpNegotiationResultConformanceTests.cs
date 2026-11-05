using System.Text.Json;

namespace sdp.Tests.Api;

public class SdpNegotiationResultConformanceTests
{
    [TestCase(SdpRejectionReason.Unknown, "unknown", SdpDiagnosticCodes.NegotiationRejectedByPolicy, 'm', "")]
    [TestCase(SdpRejectionReason.OfferedAsRejected, "offered-as-rejected", SdpDiagnosticCodes.NegotiationMediaAlreadyRejected, 'm', "")]
    [TestCase(SdpRejectionReason.UnsupportedMedia, "unsupported-media", SdpDiagnosticCodes.NegotiationUnsupportedMedia, 'm', "")]
    [TestCase(SdpRejectionReason.UnsupportedProtocol, "unsupported-protocol", SdpDiagnosticCodes.NegotiationUnsupportedProtocol, 'm', "")]
    [TestCase(SdpRejectionReason.NoCompatibleCodec, "no-compatible-codec", SdpDiagnosticCodes.NegotiationNoCompatibleCodec, 'm', "")]
    [TestCase(SdpRejectionReason.IncompatibleDirection, "incompatible-direction", SdpDiagnosticCodes.NegotiationIncompatibleDirection, 'a', SdpAttributeNames.SendReceive)]
    [TestCase(SdpRejectionReason.IceRequirementsNotMet, "ice-requirements-not-met", SdpDiagnosticCodes.NegotiationIceIncompatible, 'a', SdpAttributeNames.IceUsernameFragment)]
    [TestCase(SdpRejectionReason.DtlsRequirementsNotMet, "dtls-requirements-not-met", SdpDiagnosticCodes.NegotiationDtlsIncompatible, 'a', SdpAttributeNames.Fingerprint)]
    [TestCase(SdpRejectionReason.RtcpMuxRequired, "rtcp-mux-required", SdpDiagnosticCodes.NegotiationRtcpMuxIncompatible, 'a', SdpAttributeNames.RtcpMux)]
    [TestCase(SdpRejectionReason.BundleRequirementsNotMet, "bundle-requirements-not-met", SdpDiagnosticCodes.NegotiationBundleIncompatible, 'a', SdpAttributeNames.BundleOnly)]
    [TestCase(SdpRejectionReason.DataChannelRequirementsNotMet, "data-channel-requirements-not-met", SdpDiagnosticCodes.NegotiationDataChannelIncompatible, 'a', SdpAttributeNames.SctpPort)]
    [TestCase(SdpRejectionReason.RejectedByPolicy, "rejected-by-policy", SdpDiagnosticCodes.NegotiationRejectedByPolicy, 'a', SdpAttributeNames.ExtMap)]
    [TestCase(SdpRejectionReason.InvalidOffer, "invalid-offer", SdpDiagnosticCodes.NegotiationInvalidOffer, 'm', "")]
    public void RejectionResult_MapsEveryReasonToStableMetadata(SdpRejectionReason reason, string token, string code, char fieldName, string attributeName)
    {
        SdpSessionDescription source = Session();
        source.MediaDescriptions.Add(OfferMedia("audio", 4000, SdpTransportProtocols.RtpAvpf, "111", "audio"));
        SdpRejection rejection = SdpOfferAnswerCore.CreatePositionalRejection(0, "audio", reason);
        SdpNegotiationResult result = new SdpNegotiationResult();
        result.Rejections.Add(rejection);

        SdpOfferAnswerCore.PopulateRejectionDiagnostics(result, source, 8);

        Assert.Multiple(() =>
        {
            Assert.That(rejection.Reason, Is.EqualTo(reason));
            Assert.That(rejection.ReasonToken, Is.EqualTo(token));
            Assert.That(rejection.Detail, Is.Not.Empty);
            Assert.That(rejection.Path.Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(rejection.Path.MediaIndex, Is.Zero);
            Assert.That(rejection.Path.MediaId, Is.EqualTo("audio"));
            Assert.That(rejection.Path.FieldName, Is.EqualTo(fieldName));
            Assert.That(rejection.Path.AttributeName, Is.EqualTo(attributeName));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(code));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(result.Diagnostics[0].Path.MediaId, Is.EqualTo("audio"));
        });
    }

    [Test]
    public void AnswerResult_BoundsWarningsAndPropagatesDisallowedRejectionAsAnError()
    {
        SdpSessionDescription offer = Session();
        offer.MediaDescriptions.Add(OfferMedia("audio", 4000, SdpTransportProtocols.RtpAvpf, "111", "audio"));
        offer.MediaDescriptions.Add(OfferMedia("video", 4002, SdpTransportProtocols.RtpAvpf, "96", "video"));
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(RtpCapability("audio", "audio", 5000, SdpTransportProtocols.UdpTlsRtpSavpf, 111, "opus", 48000));
        capabilities.Media.Add(RtpCapability("audio", "video", 5002, SdpTransportProtocols.RtpAvpf, 96, "VP8", 90000));
        SdpNegotiationOptions bounded = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            MaximumDiagnosticCount = 1
        };
        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, Session(), bounded);
        SdpNegotiationOptions disallowed = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            AllowMediaRejection = false
        };

        SdpNegotiationException exception = Assert.Throws<SdpNegotiationException>(() => new SdpOfferAnswerService().CreateAnswer(offer, Session(), disallowed))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Rejections.Select(rejection => rejection.Reason), Is.EqualTo(new[] { SdpRejectionReason.UnsupportedProtocol, SdpRejectionReason.UnsupportedMedia }));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.DiagnosticLimitReached));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.DiagnosticLimitReached, Is.True);
            Assert.That(exception.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationUnsupportedProtocol));
        });
    }

    [Test]
    public void SubsequentNegotiation_PreservesUnknownExtensionsAndIsDeterministicAndNonMutating()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities initialCapabilities = new SdpEndpointCapabilities();
        initialCapabilities.Media.Add(RtpCapability("audio", "audio", 5000, SdpTransportProtocols.RtpAvpf, 111, "opus", 48000));
        SdpSessionDescription previous = service.CreateInitialOffer(Session(), new SdpNegotiationOptions { LocalCapabilities = initialCapabilities }).Description!;
        previous.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "x-session-choice", Value = "stable" });
        previous.MediaDescriptions[0].Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "x-media-choice", Value = "preserve-me" });
        SdpEndpointCapabilities currentCapabilities = new SdpEndpointCapabilities();
        SdpMediaCapability currentMedia = RtpCapability("audio", "audio", 6000, SdpTransportProtocols.RtpAvpf, 120, "opus", 48000);
        currentMedia.Codecs.Add(new SdpCodecCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            EncodingName = "PCMA",
            ClockRate = 8000,
            HasPreferredPayloadType = true,
            PreferredPayloadType = 8,
            Preference = 0
        });
        currentCapabilities.Media.Add(currentMedia);
        SdpNegotiationOptions options = new SdpNegotiationOptions { LocalCapabilities = currentCapabilities };
        string previousSnapshot = Snapshot(previous);
        string optionsSnapshot = Snapshot(options);

        SdpNegotiationResult first = service.CreateSubsequentOffer(previous, options);
        SdpNegotiationResult second = service.CreateSubsequentOffer(previous, options);
        string firstText = new SdpWriter().WriteText(first.Description!, new SdpWriteOptions()).Text;
        string secondText = new SdpWriter().WriteText(second.Description!, new SdpWriteOptions()).Text;

        Assert.Multiple(() =>
        {
            Assert.That(firstText, Is.EqualTo(secondText));
            Assert.That(AttributeValue(first.Description!.Attributes, "x-session-choice"), Is.EqualTo("stable"));
            Assert.That(AttributeValue(first.Description.MediaDescriptions[0].Attributes, "x-media-choice"), Is.EqualTo("preserve-me"));
            Assert.That(first.Media[0].PayloadMappings.Select(mapping => mapping.NegotiatedPayloadType), Is.EqualTo(new[] { 111, 8 }));
            Assert.That(Snapshot(previous), Is.EqualTo(previousSnapshot));
            Assert.That(Snapshot(options), Is.EqualTo(optionsSnapshot));
            Assert.That(currentMedia.Codecs.Select(codec => codec.PreferredPayloadType), Is.EqualTo(new[] { 120, 8 }));
        });
    }

    static SdpMediaDescription OfferMedia(string mediaType, int port, string protocol, string format, string mediaId)
    {
        SdpMediaDescription media = new SdpSessionFactory().CreateMedia(mediaType, port, protocol, new List<string> { format });
        media.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = SdpAttributeNames.MediaId, Value = mediaId });
        if (mediaType == "audio")
        {
            media.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = SdpAttributeNames.RtpMap, Value = format + " opus/48000/2" });
        }
        else if (mediaType == "video")
        {
            media.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = SdpAttributeNames.RtpMap, Value = format + " VP8/90000" });
        }
        return media;
    }

    static SdpMediaCapability RtpCapability(string mediaType, string mediaId, int port, string protocol, int payloadType, string encodingName, int clockRate)
    {
        SdpMediaKind kind = mediaType == "audio" ? SdpMediaKind.Audio : SdpMediaKind.Video;
        SdpMediaCapability media = new SdpMediaCapability
        {
            MediaKind = kind,
            MediaType = mediaType,
            MediaId = mediaId,
            Port = port
        };
        media.Protocols.Add(protocol);
        media.Codecs.Add(new SdpCodecCapability
        {
            MediaKind = kind,
            MediaType = mediaType,
            EncodingName = encodingName,
            ClockRate = clockRate,
            HasPreferredPayloadType = true,
            PreferredPayloadType = payloadType,
            HasChannelCount = kind == SdpMediaKind.Audio,
            ChannelCount = kind == SdpMediaKind.Audio ? 2 : 1
        });
        return media;
    }

    static SdpSessionDescription Session()
    {
        return new SdpSessionFactory().CreateSession(
            new SdpOrigin
            {
                Username = "-",
                SessionId = "150",
                SessionVersion = "1",
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                UnicastAddress = "192.0.2.150",
                AddressKind = SdpAddressKind.Ipv4
            },
            "Negotiation result conformance",
            new SdpConnectionData
            {
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                Address = "192.0.2.150",
                AddressKind = SdpAddressKind.Ipv4
            },
            new SdpTiming { StartTime = "0", StopTime = "0" });
    }

    static string? AttributeValue(List<SdpAttribute> attributes, string name)
    {
        return attributes.FirstOrDefault(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static string Snapshot(object value)
    {
        return JsonSerializer.Serialize(value, value.GetType(), new JsonSerializerOptions { IncludeFields = true });
    }
}
