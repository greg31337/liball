namespace sdp.Tests.Api;

public class SdpOfferAnswerServiceTests
{
    [Test]
    public void CreateInitialOffer_BuildsDeterministicBundledRtpMediaFromCapabilities()
    {
        SdpSessionDescription template = SessionTemplate();
        template.Origin!.SessionVersion = "7";
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities
        {
            SupportsIce = true,
            SupportsDtls = true,
            SupportsBundle = true
        };
        SdpFingerprintAttribute fingerprint = new SdpFingerprintAttribute { Algorithm = "sha-256" };
        int digestIndex = 0;
        while (digestIndex < 32)
        {
            fingerprint.Digest.Add((byte)(digestIndex + 1));
            digestIndex = digestIndex + 1;
        }
        capabilities.Fingerprints.Add(fingerprint);
        capabilities.HeaderExtensions.Add(new SdpHeaderExtension
        {
            Uri = SdpHeaderExtensionUris.MediaId,
            HasPreferredId = true,
            PreferredId = 3
        });
        capabilities.HeaderExtensions.Add(new SdpHeaderExtension
        {
            Uri = SdpHeaderExtensionUris.RtpStreamId,
            HasPreferredId = true,
            PreferredId = 4
        });
        SdpMediaCapability audio = RtpMediaCapability("audio", "audio", 5004, 111, "opus", 48000);
        audio.Codecs[0].HasChannelCount = true;
        audio.Codecs[0].ChannelCount = 2;
        audio.Codecs[0].FormatParameters.Add(new SdpFormatParameter { Name = "useinbandfec", HasValue = true, Value = "1" });
        audio.Codecs[0].RtcpFeedbackTypes.Add("transport-cc");
        audio.Ice = Ice("audioUfrag", "audioPasswordValue1234", 5004);
        audio.Dtls = Dtls(fingerprint, "bundle-tls-identifier-1");
        audio.MediaStreamIds.Add(new SdpMediaStreamId { StreamId = "stream", HasTrackId = true, TrackId = "audio-track" });
        SdpMediaCapability video = RtpMediaCapability("video", "video", 5006, 96, "VP8", 90000);
        video.Ice = Ice("videoUfrag", "videoPasswordValue1234", 5006);
        video.Dtls = Dtls(fingerprint, "bundle-tls-identifier-1");
        video.MediaStreamIds.Add(new SdpMediaStreamId { StreamId = "stream", HasTrackId = true, TrackId = "video-track" });
        SdpRidAttribute rid = new SdpRidAttribute { Id = "high", Direction = SdpDirection.SendOnly };
        rid.PayloadTypes.Add(96);
        video.Rids.Add(rid);
        SdpSimulcastStream stream = new SdpSimulcastStream();
        stream.Alternatives.Add(new SdpSimulcastAlternative { RidId = "high" });
        video.Simulcast = new SdpSimulcastAttribute();
        video.Simulcast.SendStreams.Add(stream);
        capabilities.Media.Add(audio);
        capabilities.Media.Add(video);
        SdpNegotiationOptions options = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            RequireIce = true,
            RequireDtls = true,
            RequireRtcpMux = true,
            RequireBundle = true
        };
        ISdpOfferAnswerService service = new SdpOfferAnswerService();

        SdpNegotiationResult first = service.CreateInitialOffer(template, options);
        SdpNegotiationResult second = service.CreateInitialOffer(template, options);
        SdpSessionDescription offer = first.Description!;
        SdpComparisonResult repeated = new SdpComparer().CompareDescriptions(offer, second.Description!, new SdpComparisonOptions());
        SdpValidationResult validation = new SdpValidator().ValidateDescription(offer, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.CombinedStrict,
            NegotiationRole = SdpNegotiationRole.Offerer
        });

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.AppliedOptions.Role, Is.EqualTo(SdpNegotiationRole.Offerer));
            Assert.That(offer.Origin!.SessionVersion, Is.EqualTo("7"));
            Assert.That(offer.MediaDescriptions.Select(media => media.MediaType), Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(offer.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "111" }));
            Assert.That(offer.MediaDescriptions[1].Formats, Is.EqualTo(new[] { "96" }));
            Assert.That(AttributeValue(offer.MediaDescriptions[0], "mid"), Is.EqualTo("audio"));
            Assert.That(AttributeValue(offer.MediaDescriptions[1], "mid"), Is.EqualTo("video"));
            Assert.That(AttributeValue(offer.MediaDescriptions[0], "rtpmap"), Is.EqualTo("111 opus/48000/2"));
            Assert.That(AttributeValue(offer.MediaDescriptions[0], "fmtp"), Is.EqualTo("111 useinbandfec=1"));
            Assert.That(AttributeValue(offer.MediaDescriptions[0], "ice-ufrag"), Is.EqualTo("audioUfrag"));
            Assert.That(AttributeValue(offer.MediaDescriptions[1], "ice-ufrag"), Is.EqualTo("videoUfrag"));
            Assert.That(AttributeValue(offer.MediaDescriptions[0], "setup"), Is.EqualTo("actpass"));
            Assert.That(AttributeValue(offer.MediaDescriptions[0], "extmap"), Is.EqualTo("3 " + SdpHeaderExtensionUris.MediaId));
            Assert.That(AttributeValue(offer.MediaDescriptions[1], "rid"), Is.EqualTo("high send pt=96"));
            Assert.That(AttributeValue(offer.MediaDescriptions[1], "simulcast"), Is.EqualTo("send high"));
            Assert.That(HasAttribute(offer.MediaDescriptions[0], "rtcp-mux"), Is.True);
            Assert.That(first.BundleGroups.Single().MediaIds, Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(first.BundleTransports.Single().TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(first.Media.Select(media => media.TransportMediaIndex), Is.EqualTo(new[] { 0, 0 }));
            Assert.That(first.Media.Select(media => media.BundleGroupIndex), Is.EqualTo(new[] { 0, 0 }));
            Assert.That(first.Media[0].PayloadMappings[0].OfferedPayloadType, Is.EqualTo(111));
            Assert.That(first.Media[0].HeaderExtensionMappings[0].NegotiatedId, Is.EqualTo(3));
            Assert.That(first.Media[0].EffectiveMedia!.Codecs.Select(codec => codec.EncodingName), Is.EqualTo(new[] { "opus" }));
            Assert.That(first.Media[0].EffectiveMedia!.MediaStreamIds.Select(streamId => streamId.TrackId), Is.EqualTo(new[] { "audio-track" }));
            Assert.That(first.Media[1].EffectiveMedia!.Rids.Select(value => value.Id), Is.EqualTo(new[] { "high" }));
            Assert.That(first.Media[1].EffectiveMedia!.Simulcast!.SendStreams.Single().Alternatives.Single().RidId, Is.EqualTo("high"));
            Assert.That(first.Media[1].EffectiveMedia!.BundleTransport!.TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(repeated.Equal, Is.True);
            Assert.That(validation.Issues.Where(issue => issue.Severity == SdpValidationSeverity.Error || issue.Severity == SdpValidationSeverity.Fatal), Is.Empty, string.Join("|", validation.Issues.Select(issue => issue.Code + ":" + issue.Detail)));
            Assert.That(template.MediaDescriptions, Is.Empty);
            Assert.That(template.Origin.SessionVersion, Is.EqualTo("7"));
            Assert.That(capabilities.Media[1].Port, Is.EqualTo(5006));
            Assert.That(fingerprint.SourceAttribute, Is.Null);
        });
    }

    [Test]
    public void CreateInitialOffer_BuildsModernSctpAndDataChannelAttributes()
    {
        SdpSessionDescription template = SessionTemplate();
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities { SupportsDataChannels = true };
        SdpMediaCapability application = new SdpMediaCapability
        {
            MediaKind = SdpMediaKind.Application,
            MediaType = "application",
            MediaId = "data",
            Port = 9,
            Direction = SdpDirection.SendReceive,
            Sctp = new SdpSctpDescription
            {
                Port = 5000,
                Format = SdpDataChannelTokens.WebRtcDataChannel,
                HasMaximumMessageSize = true,
                MaximumMessageSize = 262144
            }
        };
        application.Protocols.Add(SdpTransportProtocols.UdpDtlsSctp);
        SdpDataChannelAttribute channel = new SdpDataChannelAttribute { ChannelId = 4 };
        channel.Parameters.Add(new SdpDataChannelParameter { Name = "label", HasValue = true, Value = "chat" });
        channel.SubprotocolAttributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "path", Value = "msrp://example.test/id;dc" });
        application.DataChannels.Add(channel);
        capabilities.Media.Add(application);
        SdpNegotiationOptions options = new SdpNegotiationOptions { LocalCapabilities = capabilities };

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateInitialOffer(template, options);
        SdpMediaDescription media = result.Description!.MediaDescriptions.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(media.Formats, Is.EqualTo(new[] { SdpDataChannelTokens.WebRtcDataChannel }));
            Assert.That(AttributeValue(media, "mid"), Is.EqualTo("data"));
            Assert.That(AttributeValue(media, "sctp-port"), Is.EqualTo("5000"));
            Assert.That(AttributeValue(media, "max-message-size"), Is.EqualTo("262144"));
            Assert.That(AttributeValue(media, "dcmap"), Is.EqualTo("4 label=\"chat\""));
            Assert.That(AttributeValue(media, "dcsa"), Is.EqualTo("4 path:msrp://example.test/id;dc"));
            Assert.That(result.Media[0].Sctp!.Port, Is.EqualTo(5000));
            Assert.That(result.Media[0].EffectiveMedia!.Sctp!.MaximumMessageSize, Is.EqualTo(262144));
            Assert.That(result.Media[0].EffectiveMedia!.DataChannels.Single().ChannelId, Is.EqualTo(4));
            Assert.That(result.Media[0].EffectiveMedia!.DataChannels[0].SubprotocolAttributes.Single().Name, Is.EqualTo("path"));
            Assert.That(result.Media[0].BundleGroupIndex, Is.EqualTo(-1));
        });
    }

    [Test]
    public void CreateInitialOffer_ReservesCallerIdsBeforeDeterministicAllocation()
    {
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        SdpMediaCapability first = RtpMediaCapability("audio", string.Empty, 5004, 111, "opus", 48000);
        first.Codecs[0].HasPreferredPayloadType = false;
        first.Codecs.Add(new SdpCodecCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            EncodingName = "PCMA",
            ClockRate = 8000,
            HasPreferredPayloadType = true,
            PreferredPayloadType = 96
        });
        SdpMediaCapability second = RtpMediaCapability("video", "0", 5006, 100, "VP8", 90000);
        capabilities.Media.Add(first);
        capabilities.Media.Add(second);

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateInitialOffer(SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = capabilities });

        Assert.Multiple(() =>
        {
            Assert.That(result.Description!.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "97", "96" }));
            Assert.That(AttributeValue(result.Description.MediaDescriptions[0], "mid"), Is.EqualTo("1"));
            Assert.That(AttributeValue(result.Description.MediaDescriptions[1], "mid"), Is.EqualTo("0"));
            Assert.That(result.Media[0].PayloadMappings.Select(mapping => mapping.OfferedPayloadType), Is.EqualTo(new[] { 97, 96 }));
        });
    }

    [Test]
    public void CreateInitialOffer_ValidatesTemplateRoleCapabilitiesAndAssignments()
    {
        ISdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpSessionDescription template = SessionTemplate();
        SdpNegotiationOptions missingCapabilities = new SdpNegotiationOptions();
        SdpNegotiationOptions answerer = ValidOptions();
        answerer.Role = SdpNegotiationRole.Answerer;
        SdpNegotiationOptions duplicatePayload = ValidOptions();
        duplicatePayload.LocalCapabilities!.Media[0].Codecs.Add(new SdpCodecCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            EncodingName = "PCMA",
            ClockRate = 8000,
            HasPreferredPayloadType = true,
            PreferredPayloadType = 111
        });
        SdpSessionDescription populatedTemplate = SessionTemplate();
        populatedTemplate.MediaDescriptions.Add(new SdpMediaDescription());
        SdpNegotiationOptions missingRequiredIce = ValidOptions();
        missingRequiredIce.LocalCapabilities!.SupportsIce = true;
        missingRequiredIce.RequireIce = true;
        SdpNegotiationOptions missingRequiredMux = ValidOptions();
        missingRequiredMux.RequireRtcpMux = true;
        missingRequiredMux.LocalCapabilities!.Media[0].SupportsRtcpMux = false;

        Assert.Multiple(() =>
        {
            Assert.That(() => service.CreateInitialOffer(null!, new SdpNegotiationOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("sessionTemplate"));
            Assert.That(() => service.CreateInitialOffer(template, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => service.CreateInitialOffer(template, missingCapabilities), Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => service.CreateInitialOffer(template, answerer), Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => service.CreateInitialOffer(template, duplicatePayload), Throws.TypeOf<SdpNegotiationException>());
            Assert.That(() => service.CreateInitialOffer(populatedTemplate, ValidOptions()), Throws.ArgumentException.With.Property("ParamName").EqualTo("sessionTemplate"));
            Assert.That(() => service.CreateInitialOffer(template, missingRequiredIce), Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => service.CreateInitialOffer(template, missingRequiredMux), Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
        });
    }

    [Test]
    public void CreateAnswer_MatchesMediaByPositionAndAppliesDirectionProtocolAndConnectionRules()
    {
        SdpSessionDescription offer = SessionTemplate();
        offer.Connection!.Address = "198.51.100.10";
        offer.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "sendonly" });
        SdpMediaDescription offeredAudio = OfferMedia("audio", 4000, "rtp/avpf", "111");
        offeredAudio.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "mid", Value = "audio" });
        offer.MediaDescriptions.Add(offeredAudio);
        SdpMediaDescription offeredVideo = OfferMedia("video", 4002, SdpTransportProtocols.RtpAvpf, "96");
        offeredVideo.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "recvonly" });
        offer.MediaDescriptions.Add(offeredVideo);
        offer.MediaDescriptions.Add(OfferMedia("video", 4004, SdpTransportProtocols.RtpAvp, "97"));
        offer.MediaDescriptions.Add(OfferMedia("application", 0, SdpTransportProtocols.UdpDtlsSctp, SdpDataChannelTokens.WebRtcDataChannel));
        offer.MediaDescriptions.Add(OfferMedia("text", 4008, SdpTransportProtocols.Udp, "t140"));

        SdpSessionDescription template = SessionTemplate();
        template.Connection!.Address = "203.0.113.10";
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        SdpMediaCapability audio = RtpMediaCapability("audio", "local-audio", 6000, 111, "opus", 48000);
        audio.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        audio.Connection = new SdpConnectionData
        {
            NetworkType = SdpNetworkTypes.Internet,
            AddressType = SdpAddressTypes.Ip4,
            Address = "203.0.113.20",
            AddressKind = SdpAddressKind.Ipv4
        };
        capabilities.Media.Add(audio);
        SdpMediaCapability localVideo = RtpMediaCapability("video", "local-video", 6002, 96, "VP8", 90000);
        localVideo.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        capabilities.Media.Add(localVideo);
        SdpMediaCapability localLegacyVideo = RtpMediaCapability("video", "local-video-legacy", 6004, 97, "H264", 90000);
        localLegacyVideo.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        capabilities.Media.Add(localLegacyVideo);
        SdpMediaCapability application = new SdpMediaCapability
        {
            MediaKind = SdpMediaKind.Application,
            MediaType = "application",
            Port = 6006,
            Sctp = new SdpSctpDescription { Port = 5000, Format = SdpDataChannelTokens.WebRtcDataChannel }
        };
        application.Protocols.Add(SdpTransportProtocols.UdpDtlsSctp);
        capabilities.Media.Add(application);
        SdpNegotiationOptions options = new SdpNegotiationOptions
        {
            Role = SdpNegotiationRole.Answerer,
            LocalCapabilities = capabilities
        };

        SdpNegotiationResult negotiation = new SdpOfferAnswerService().CreateAnswer(offer, template, options);
        SdpSessionDescription answer = negotiation.Description!;

        Assert.Multiple(() =>
        {
            Assert.That(negotiation.Success, Is.True);
            Assert.That(negotiation.AppliedOptions.Role, Is.EqualTo(SdpNegotiationRole.Answerer));
            Assert.That(answer.MediaDescriptions.Select(media => media.MediaType), Is.EqualTo(new[] { "audio", "video", "video", "application", "text" }));
            Assert.That(answer.MediaDescriptions.Select(media => media.Protocol), Is.EqualTo(offer.MediaDescriptions.Select(media => media.Protocol)));
            Assert.That(answer.MediaDescriptions.Select(media => media.Formats.Single()), Is.EqualTo(new[] { "111", "96", "97", SdpDataChannelTokens.WebRtcDataChannel, "t140" }));
            Assert.That(answer.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 6000, 6002, 0, 0, 0 }));
            Assert.That(HasAttribute(answer.MediaDescriptions[0], "recvonly"), Is.True);
            Assert.That(HasAttribute(answer.MediaDescriptions[1], "sendonly"), Is.True);
            Assert.That(HasAttribute(answer.MediaDescriptions[2], "inactive"), Is.True);
            Assert.That(negotiation.Media.Select(media => media.Accepted), Is.EqualTo(new[] { true, true, false, false, false }));
            Assert.That(negotiation.Media.Select(media => media.OfferedDirection), Is.EqualTo(new[] { SdpDirection.SendOnly, SdpDirection.ReceiveOnly, SdpDirection.SendOnly, SdpDirection.Inactive, SdpDirection.SendOnly }));
            Assert.That(negotiation.Media.Select(media => media.Direction), Is.EqualTo(new[] { SdpDirection.ReceiveOnly, SdpDirection.SendOnly, SdpDirection.Inactive, SdpDirection.Inactive, SdpDirection.Inactive }));
            Assert.That(negotiation.Rejections.Select(rejection => rejection.Reason), Is.EqualTo(new[] { SdpRejectionReason.UnsupportedProtocol, SdpRejectionReason.OfferedAsRejected, SdpRejectionReason.UnsupportedMedia }));
            Assert.That(negotiation.Media[0].MediaId, Is.EqualTo("audio"));
            Assert.That(negotiation.Media[0].EffectiveMedia!.Connection!.Address, Is.EqualTo("203.0.113.20"));
            Assert.That(negotiation.Media[1].EffectiveMedia!.Connection!.Address, Is.EqualTo("203.0.113.10"));
            Assert.That(answer.Connection!.Address, Is.EqualTo("203.0.113.10"));
            Assert.That(offer.Connection.Address, Is.EqualTo("198.51.100.10"));
            Assert.That(capabilities.Media[0].Connection!.Address, Is.EqualTo("203.0.113.20"));
        });
    }

    [Test]
    public void CreateAnswer_DoesNotSearchOtherCapabilityPositionsForAMediaMatch()
    {
        SdpSessionDescription offer = SessionTemplate();
        offer.MediaDescriptions.Add(OfferMedia("audio", 4000, SdpTransportProtocols.UdpTlsRtpSavpf, "111"));
        offer.MediaDescriptions.Add(OfferMedia("video", 4002, SdpTransportProtocols.UdpTlsRtpSavpf, "96"));
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(RtpMediaCapability("video", "video", 6000, 96, "VP8", 90000));
        capabilities.Media.Add(RtpMediaCapability("audio", "audio", 6002, 111, "opus", 48000));

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = capabilities });

        Assert.Multiple(() =>
        {
            Assert.That(result.Description!.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 0, 0 }));
            Assert.That(result.Rejections.Select(rejection => rejection.Reason), Is.EqualTo(new[] { SdpRejectionReason.UnsupportedMedia, SdpRejectionReason.UnsupportedMedia }));
            Assert.That(result.Rejections.Select(rejection => rejection.Path.MediaIndex), Is.EqualTo(new[] { 0, 1 }));
        });
    }

    [Test]
    public void CreateAnswer_IntersectsOrdersAndRemapsCodecsWithFeedbackAndPacketTime()
    {
        SdpSessionDescription offer = SessionTemplate();
        SdpMediaDescription offeredMedia = new SdpSessionFactory().CreateMedia("audio", 4000, SdpTransportProtocols.RtpAvpf, new List<string> { "120", "121", "122" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = "120 X-LOW/8000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = "121 X-HIGH/8000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = "122 X-UNSUPPORTED/8000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "fmtp", Value = "120 mode=1;extra=offered" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "fmtp", Value = "121 mode=2" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtcp-fb", Value = "* nack" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtcp-fb", Value = "121 transport-cc" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ptime", Value = "20" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "maxptime", Value = "60" });
        offer.MediaDescriptions.Add(offeredMedia);
        SdpMediaCapability localMedia = RtpMediaCapability("audio", "audio", 6000, 100, "X-HIGH", 8000);
        localMedia.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        localMedia.Codecs.Clear();
        SdpCodecCapability high = new SdpCodecCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            EncodingName = "X-HIGH",
            ClockRate = 8000,
            Preference = 0
        };
        high.FormatParameters.Add(new SdpFormatParameter { Name = "mode", HasValue = true, Value = "2" });
        high.RtcpFeedbackTypes.Add("nack");
        SdpCodecCapability low = new SdpCodecCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            EncodingName = "X-LOW",
            ClockRate = 8000,
            Preference = 10
        };
        low.FormatParameters.Add(new SdpFormatParameter { Name = "mode", HasValue = true, Value = "1" });
        localMedia.Codecs.Add(high);
        localMedia.Codecs.Add(low);
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(localMedia);
        SdpNegotiationOptions localPreference = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            CodecMatchMode = SdpCodecMatchMode.Compatible,
            PreferLocalCodecOrder = true,
            PreservePayloadTypes = false
        };

        SdpNegotiationResult reordered = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), localPreference);
        SdpMediaDescription reorderedMedia = reordered.Description!.MediaDescriptions[0];
        SdpNegotiationOptions remotePreference = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            CodecMatchMode = SdpCodecMatchMode.Compatible,
            PreferLocalCodecOrder = false,
            PreservePayloadTypes = true
        };
        SdpNegotiationResult preserved = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), remotePreference);

        Assert.Multiple(() =>
        {
            Assert.That(reorderedMedia.Formats, Is.EqualTo(new[] { "96", "97" }));
            Assert.That(AttributeValues(reorderedMedia, "rtpmap"), Is.EqualTo(new[] { "96 X-HIGH/8000", "97 X-LOW/8000" }));
            Assert.That(AttributeValues(reorderedMedia, "fmtp"), Is.EqualTo(new[] { "96 mode=2", "97 mode=1;extra=offered" }));
            Assert.That(AttributeValues(reorderedMedia, "rtcp-fb"), Is.EqualTo(new[] { "96 nack" }));
            Assert.That(AttributeValues(reorderedMedia, "ptime"), Is.EqualTo(new[] { "20" }));
            Assert.That(AttributeValues(reorderedMedia, "maxptime"), Is.EqualTo(new[] { "60" }));
            Assert.That(reordered.Media[0].Codecs.Select(codec => codec.EncodingName), Is.EqualTo(new[] { "X-HIGH", "X-LOW" }));
            Assert.That(reordered.Media[0].PayloadMappings.Select(mapping => (mapping.OfferedPayloadType, mapping.NegotiatedPayloadType)), Is.EqualTo(new[] { (121, 96), (120, 97) }));
            Assert.That(preserved.Description!.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "120", "121" }));
            Assert.That(preserved.Media[0].PayloadMappings.Select(mapping => mapping.NegotiatedPayloadType), Is.EqualTo(new[] { 120, 121 }));
            Assert.That(offer.MediaDescriptions[0].Formats, Is.EqualTo(new[] { "120", "121", "122" }));
            Assert.That(AttributeValues(offer.MediaDescriptions[0], "rtcp-fb"), Is.EqualTo(new[] { "* nack", "121 transport-cc" }));
        });
    }

    [Test]
    public void CreateAnswer_AppliesFmtpModeAndPerMediaRejectionPolicy()
    {
        SdpSessionDescription offer = SessionTemplate();
        SdpMediaDescription offeredMedia = new SdpSessionFactory().CreateMedia("audio", 4000, SdpTransportProtocols.RtpAvp, new List<string> { "120" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = "120 X-CODEC/8000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "fmtp", Value = "120 mode=1;extension=offered" });
        offer.MediaDescriptions.Add(offeredMedia);
        SdpMediaCapability media = RtpMediaCapability("audio", "audio", 6000, 100, "X-CODEC", 8000);
        media.Protocols[0] = SdpTransportProtocols.RtpAvp;
        media.Codecs[0].FormatParameters.Add(new SdpFormatParameter { Name = "mode", HasValue = true, Value = "1" });
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(media);
        SdpNegotiationOptions strict = new SdpNegotiationOptions { LocalCapabilities = capabilities, CodecMatchMode = SdpCodecMatchMode.Strict };
        SdpNegotiationOptions compatible = new SdpNegotiationOptions { LocalCapabilities = capabilities, CodecMatchMode = SdpCodecMatchMode.Compatible };
        SdpNegotiationOptions required = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            CodecMatchMode = SdpCodecMatchMode.Strict,
            AllowMediaRejection = false
        };

        SdpNegotiationResult rejected = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), strict);
        SdpNegotiationResult accepted = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), compatible);
        SdpNegotiationException failure = Assert.Throws<SdpNegotiationException>(() => new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), required))!;

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Description!.MediaDescriptions[0].Port, Is.Zero);
            Assert.That(rejected.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.NoCompatibleCodec));
            Assert.That(rejected.Media[0].Rejection!.Path.FieldName, Is.EqualTo('m'));
            Assert.That(rejected.Rejections.Single(), Is.SameAs(rejected.Media[0].Rejection));
            Assert.That(rejected.Diagnostics.Single().Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationNoCompatibleCodec));
            Assert.That(rejected.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(accepted.Description!.MediaDescriptions[0].Port, Is.EqualTo(6000));
            Assert.That(AttributeValues(accepted.Description.MediaDescriptions[0], "fmtp"), Is.EqualTo(new[] { "120 mode=1;extension=offered" }));
            Assert.That(failure.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationNoCompatibleCodec));
        });
    }

    [Test]
    public void CreateAnswer_BoundsDiagnosticsWithoutLosingExplicitRejections()
    {
        SdpSessionDescription offer = SessionTemplate();
        offer.MediaDescriptions.Add(OfferMedia("audio", 4000, SdpTransportProtocols.RtpAvpf, "111"));
        offer.MediaDescriptions.Add(OfferMedia("video", 4002, SdpTransportProtocols.RtpAvpf, "96"));
        offer.MediaDescriptions.Add(OfferMedia("application", 4004, SdpTransportProtocols.UdpDtlsSctp, SdpDataChannelTokens.WebRtcDataChannel));
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        SdpMediaCapability local = RtpMediaCapability("audio", "audio", 6000, 111, "opus", 48000);
        local.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        capabilities.Media.Add(local);
        SdpNegotiationOptions options = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            MaximumDiagnosticCount = 1
        };

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Rejections.Select(rejection => rejection.Reason), Is.EqualTo(new[] { SdpRejectionReason.UnsupportedMedia, SdpRejectionReason.UnsupportedMedia }));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.DiagnosticLimitReached));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.DiagnosticLimitReached, Is.True);
            Assert.That(offer.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 4000, 4002, 4004 }));
            Assert.That(options.MaximumDiagnosticCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void CreateAnswer_PreservesAndRemapsOnlyCompleteAssociatedPayloadGraphs()
    {
        SdpSessionDescription offer = SessionTemplate();
        SdpMediaDescription offeredMedia = new SdpSessionFactory().CreateMedia("video", 4000, SdpTransportProtocols.RtpAvpf, new List<string> { "120", "121", "122", "123" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = "120 VP8/90000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = "121 rtx/90000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = "122 rtx/90000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = "123 red/90000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "fmtp", Value = "121 apt=120" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "fmtp", Value = "122 apt=119" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "fmtp", Value = "123 120/121" });
        offer.MediaDescriptions.Add(offeredMedia);
        SdpMediaCapability media = RtpMediaCapability("video", "video", 6000, 100, "VP8", 90000);
        media.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        media.Codecs.Add(AssociatedCodec(SdpCodecNames.Retransmission, "apt", "120"));
        media.Codecs.Add(AssociatedCodec(SdpCodecNames.Retransmission, "apt", "119"));
        media.Codecs.Add(AssociatedCodec(SdpCodecNames.RedundantAudioData, "120/121", string.Empty));
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(media);
        SdpNegotiationOptions options = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            PreservePayloadTypes = false,
            PreferLocalCodecOrder = false
        };

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), options);
        SdpMediaDescription answerMedia = result.Description!.MediaDescriptions[0];

        Assert.Multiple(() =>
        {
            Assert.That(answerMedia.Formats, Is.EqualTo(new[] { "96", "97", "98" }));
            Assert.That(AttributeValues(answerMedia, "rtpmap"), Is.EqualTo(new[] { "96 VP8/90000", "97 rtx/90000", "98 red/90000" }));
            Assert.That(AttributeValues(answerMedia, "fmtp"), Is.EqualTo(new[] { "97 apt=96", "98 96/97" }));
            Assert.That(result.Media[0].PayloadMappings.Select(mapping => mapping.OfferedPayloadType), Is.EqualTo(new[] { 120, 121, 123 }));
            Assert.That(result.Media[0].Codecs.Select(codec => codec.PayloadType), Is.EqualTo(new[] { 96, 97, 98 }));
            Assert.That(AttributeValues(offer.MediaDescriptions[0], "fmtp"), Is.EqualTo(new[] { "121 apt=120", "122 apt=119", "123 120/121" }));
        });
    }

    [Test]
    public void CreateAnswer_NegotiatesIceDtlsRtcpMuxAndHeaderExtensions()
    {
        SdpSessionDescription offer = SessionTemplate();
        SdpMediaDescription offeredMedia = OfferMedia("audio", 4000, SdpTransportProtocols.UdpTlsRtpSavpf, "111");
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ice-ufrag", Value = "remoteUfrag" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ice-pwd", Value = "remotePasswordValue12345" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ice-options", Value = "trickle renomination" });
        SdpFingerprintAttribute remoteFingerprint = Fingerprint(SdpFingerprintAlgorithms.Sha256, 0x11, 32);
        offeredMedia.Attributes.Add(EncodedAttribute(remoteFingerprint));
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "setup", Value = "actpass" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "tls-id", Value = "offer-tls-identifier-1" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "rtcp-mux" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "rtcp-mux-only" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "rtcp-rsize" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "extmap", Value = "3/sendonly " + SdpHeaderExtensionUris.MediaId });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "extmap", Value = "4096 urn:example:negotiated cfg" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "extmap", Value = "5 urn:example:configuration remote" });
        offer.MediaDescriptions.Add(offeredMedia);

        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities
        {
            SupportsIce = true,
            SupportsDtls = true
        };
        SdpMediaCapability media = RtpMediaCapability("audio", "audio", 6000, 111, "opus", 48000);
        media.SupportsReducedSizeRtcp = true;
        media.Ice = Ice("localUfrag", "localPasswordValue123456", 6000);
        media.Ice.Options.Add("trickle");
        media.Ice.Options.Add("local-option");
        media.Dtls = new SdpDtlsDescription
        {
            SetupRole = SdpDtlsSetupRole.Passive,
            SetupRoleToken = "passive",
            TlsId = "answer-tls-identifier-1"
        };
        media.Dtls.Fingerprints.Add(Fingerprint(SdpFingerprintAlgorithms.Sha384, 0x22, 48));
        SdpFingerprintAttribute localFingerprint = Fingerprint(SdpFingerprintAlgorithms.Sha256, 0x33, 32);
        media.Dtls.Fingerprints.Add(localFingerprint);
        media.HeaderExtensions.Add(new SdpHeaderExtension
        {
            Uri = SdpHeaderExtensionUris.MediaId,
            Direction = SdpDirection.SendReceive,
            Required = true
        });
        media.HeaderExtensions.Add(new SdpHeaderExtension
        {
            Uri = "urn:example:negotiated",
            Direction = SdpDirection.ReceiveOnly,
            ExtensionAttributes = "cfg"
        });
        media.HeaderExtensions.Add(new SdpHeaderExtension
        {
            Uri = "urn:example:configuration",
            Direction = SdpDirection.SendReceive,
            ExtensionAttributes = "local"
        });
        capabilities.Media.Add(media);
        SdpNegotiationOptions options = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            RequireIce = true,
            RequireDtls = true,
            RequireRtcpMux = true
        };

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), options);
        SdpMediaDescription answerMedia = result.Description!.MediaDescriptions[0];
        SdpValidationResult iceValidation = new SdpValidator().ValidateDescription(result.Description, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Ice,
            NegotiationRole = SdpNegotiationRole.Answerer,
            OfferDescription = offer
        });
        SdpValidationResult dtlsValidation = new SdpValidator().ValidateDescription(result.Description, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.TlsDtls,
            NegotiationRole = SdpNegotiationRole.Answerer,
            OfferDescription = offer
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Media[0].Accepted, Is.True);
            Assert.That(AttributeValue(answerMedia, "ice-ufrag"), Is.EqualTo("localUfrag"));
            Assert.That(AttributeValue(answerMedia, "ice-pwd"), Is.EqualTo("localPasswordValue123456"));
            Assert.That(AttributeValue(answerMedia, "ice-options"), Is.EqualTo("trickle"));
            Assert.That(AttributeValues(answerMedia, "candidate"), Has.Count.EqualTo(1));
            Assert.That(AttributeValue(answerMedia, "fingerprint"), Does.StartWith("sha-256 "));
            Assert.That(AttributeValue(answerMedia, "fingerprint"), Is.Not.EqualTo(EncodedAttribute(remoteFingerprint).Value));
            Assert.That(AttributeValue(answerMedia, "setup"), Is.EqualTo("passive"));
            Assert.That(AttributeValue(answerMedia, "tls-id"), Is.EqualTo("answer-tls-identifier-1"));
            Assert.That(HasAttribute(answerMedia, "rtcp-mux"), Is.True);
            Assert.That(HasAttribute(answerMedia, "rtcp-mux-only"), Is.False);
            Assert.That(HasAttribute(answerMedia, "rtcp-rsize"), Is.True);
            Assert.That(AttributeValues(answerMedia, "extmap"), Is.EqualTo(new[]
            {
                "3/recvonly " + SdpHeaderExtensionUris.MediaId,
                "1/recvonly urn:example:negotiated cfg"
            }));
            Assert.That(result.Media[0].HeaderExtensionMappings.Select(mapping => (mapping.OfferedId, mapping.NegotiatedId)), Is.EqualTo(new[] { (3, 3), (4096, 1) }));
            Assert.That(iceValidation.Issues.Where(issue => issue.Severity == SdpValidationSeverity.Error || issue.Severity == SdpValidationSeverity.Fatal), Is.Empty, string.Join("|", iceValidation.Issues.Select(issue => issue.Code + ":" + issue.Detail)));
            Assert.That(dtlsValidation.Issues.Where(issue => issue.Severity == SdpValidationSeverity.Error || issue.Severity == SdpValidationSeverity.Fatal), Is.Empty, string.Join("|", dtlsValidation.Issues.Select(issue => issue.Code + ":" + issue.Detail)));
            Assert.That(AttributeValue(offer.MediaDescriptions[0], "ice-ufrag"), Is.EqualTo("remoteUfrag"));
            Assert.That(AttributeValue(offer.MediaDescriptions[0], "setup"), Is.EqualTo("actpass"));
            Assert.That(localFingerprint.SourceAttribute, Is.Null);
        });
    }

    [Test]
    public void CreateAnswer_RejectsIncompatibleIceDtlsMuxAndRequiredExtMaps()
    {
        SdpSessionDescription secureOffer = SecureOffer();
        SdpEndpointCapabilities noDtls = AnswerCapabilities(SdpTransportProtocols.UdpTlsRtpSavpf);
        SdpNegotiationResult dtlsRejected = new SdpOfferAnswerService().CreateAnswer(secureOffer, SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = noDtls });

        SdpSessionDescription noIceOffer = PlainRtpOffer();
        SdpEndpointCapabilities iceRequiredCapabilities = AnswerCapabilities(SdpTransportProtocols.RtpAvpf);
        iceRequiredCapabilities.SupportsIce = true;
        iceRequiredCapabilities.Ice = Ice("localUfrag", "localPasswordValue123456", 6000);
        SdpNegotiationResult iceRejected = new SdpOfferAnswerService().CreateAnswer(noIceOffer, SessionTemplate(), new SdpNegotiationOptions
        {
            LocalCapabilities = iceRequiredCapabilities,
            RequireIce = true
        });

        SdpSessionDescription muxOnlyOffer = PlainRtpOffer();
        muxOnlyOffer.MediaDescriptions[0].Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "rtcp-mux" });
        muxOnlyOffer.MediaDescriptions[0].Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "rtcp-mux-only" });
        SdpEndpointCapabilities noMux = AnswerCapabilities(SdpTransportProtocols.RtpAvpf);
        noMux.Media[0].SupportsRtcpMux = false;
        SdpNegotiationResult muxRejected = new SdpOfferAnswerService().CreateAnswer(muxOnlyOffer, SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = noMux });

        SdpEndpointCapabilities requiredExtension = AnswerCapabilities(SdpTransportProtocols.RtpAvpf);
        requiredExtension.Media[0].HeaderExtensions.Add(new SdpHeaderExtension { Uri = SdpHeaderExtensionUris.MediaId, Required = true });
        SdpNegotiationResult extensionRejected = new SdpOfferAnswerService().CreateAnswer(PlainRtpOffer(), SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = requiredExtension });
        SdpNegotiationOptions noRejection = new SdpNegotiationOptions { LocalCapabilities = noDtls, AllowMediaRejection = false };
        SdpNegotiationException failure = Assert.Throws<SdpNegotiationException>(() => new SdpOfferAnswerService().CreateAnswer(secureOffer, SessionTemplate(), noRejection))!;
        SdpNegotiationException muxFailure = Assert.Throws<SdpNegotiationException>(() => new SdpOfferAnswerService().CreateAnswer(muxOnlyOffer, SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = noMux, AllowMediaRejection = false }))!;
        SdpNegotiationException policyFailure = Assert.Throws<SdpNegotiationException>(() => new SdpOfferAnswerService().CreateAnswer(PlainRtpOffer(), SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = requiredExtension, AllowMediaRejection = false }))!;

        Assert.Multiple(() =>
        {
            Assert.That(dtlsRejected.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.DtlsRequirementsNotMet));
            Assert.That(iceRejected.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.IceRequirementsNotMet));
            Assert.That(muxRejected.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.RtcpMuxRequired));
            Assert.That(extensionRejected.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.RejectedByPolicy));
            Assert.That(new[] { dtlsRejected, iceRejected, muxRejected, extensionRejected }.Select(item => item.Description!.MediaDescriptions[0].Port), Is.All.Zero);
            Assert.That(failure.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationDtlsIncompatible));
            Assert.That(muxFailure.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationRtcpMuxIncompatible));
            Assert.That(policyFailure.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationRejectedByPolicy));
        });
    }

    [Test]
    public void CreateAnswer_RejectsNegotiationRangeExtMapWhenRemappingIsDisabled()
    {
        SdpSessionDescription offer = PlainRtpOffer();
        offer.MediaDescriptions[0].Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "extmap", Value = "4096 urn:example:extension" });
        SdpEndpointCapabilities capabilities = AnswerCapabilities(SdpTransportProtocols.RtpAvpf);
        capabilities.Media[0].HeaderExtensions.Add(new SdpHeaderExtension { Uri = "urn:example:extension" });

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            AllowHeaderExtensionIdRemapping = false
        });

        Assert.That(result.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.RejectedByPolicy));
    }

    [Test]
    public void CreateAnswer_NegotiatesBundleSubsetGenericGroupAndRetaggedTransport()
    {
        SdpSessionDescription offer = SessionTemplate();
        offer.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "group", Value = "LS audio video" });
        offer.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "group", Value = "BUNDLE audio video data" });
        SdpMediaDescription offeredAudio = OfferMedia("audio", 4000, SdpTransportProtocols.RtpAvpf, "111");
        AddMediaId(offeredAudio, "audio");
        offeredAudio.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "rtcp-mux" });
        SdpMediaDescription offeredVideo = OfferMedia("video", 4002, SdpTransportProtocols.RtpAvpf, "96");
        AddMediaId(offeredVideo, "video");
        offeredVideo.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "rtcp-mux" });
        offeredVideo.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ice-ufrag", Value = "videoOffer" });
        offeredVideo.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ice-pwd", Value = "videoOfferPassword12345" });
        SdpMediaDescription offeredData = OfferMedia("video", 0, SdpTransportProtocols.RtpAvpf, "97");
        AddMediaId(offeredData, "data");
        offeredData.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "bundle-only" });
        offer.MediaDescriptions.Add(offeredAudio);
        offer.MediaDescriptions.Add(offeredVideo);
        offer.MediaDescriptions.Add(offeredData);

        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities { SupportsBundle = true, SupportsIce = true };
        SdpMediaCapability localAudio = RtpMediaCapability("audio", "ignored-audio", 6000, 111, "PCMA", 8000);
        localAudio.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        SdpMediaCapability localVideo = RtpMediaCapability("video", "ignored-video", 6002, 96, "VP8", 90000);
        localVideo.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        localVideo.Ice = Ice("videoLocal", "videoLocalPassword12345", 6002);
        SdpMediaCapability localData = RtpMediaCapability("video", "ignored-data", 6004, 97, "H264", 90000);
        localData.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        localData.Ice = Ice("dataLocal", "dataLocalPassword123456", 6004);
        capabilities.Media.Add(localAudio);
        capabilities.Media.Add(localVideo);
        capabilities.Media.Add(localData);

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            AllowBundleSubset = true
        });
        SdpSessionDescription answer = result.Description!;
        List<string> groups = answer.Attributes.Where(attribute => attribute.Name == "group").Select(attribute => attribute.Value).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(answer.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 0, 6002, 6002 }));
            Assert.That(answer.MediaDescriptions.Select(media => AttributeValue(media, "mid")), Is.EqualTo(new[] { "audio", "video", "data" }));
            Assert.That(groups, Is.EqualTo(new[] { "LS video", "BUNDLE video data" }));
            Assert.That(result.Media.Select(media => media.Accepted), Is.EqualTo(new[] { false, true, true }));
            Assert.That(result.Media.Select(media => media.TransportMediaIndex), Is.EqualTo(new[] { 0, 1, 1 }));
            Assert.That(result.Media.Select(media => media.BundleGroupIndex), Is.EqualTo(new[] { -1, 0, 0 }));
            Assert.That(result.BundleGroups.Single().TaggedMediaId, Is.EqualTo("video"));
            Assert.That(result.BundleGroups[0].MediaIds, Is.EqualTo(new[] { "video", "data" }));
            Assert.That(result.BundleTransports.Single().TaggedMediaId, Is.EqualTo("video"));
            Assert.That(result.BundleTransports[0].RtcpMux, Is.True);
            Assert.That(result.BundleTransports[0].Ice!.Credentials!.UsernameFragment, Is.EqualTo("videoLocal"));
            Assert.That(HasAttribute(answer.MediaDescriptions[1], "rtcp-mux"), Is.True);
            Assert.That(HasAttribute(answer.MediaDescriptions[2], "rtcp-mux"), Is.False);
            Assert.That(AttributeValue(answer.MediaDescriptions[1], "ice-ufrag"), Is.EqualTo("videoLocal"));
            Assert.That(AttributeValue(answer.MediaDescriptions[2], "ice-ufrag"), Is.Null);
            Assert.That(HasAttribute(answer.MediaDescriptions[2], "bundle-only"), Is.False);
            Assert.That(result.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.NoCompatibleCodec));
            Assert.That(offer.MediaDescriptions[2].Port, Is.Zero);
            Assert.That(HasAttribute(offer.MediaDescriptions[2], "bundle-only"), Is.True);
        });
    }

    [Test]
    public void CreateAnswer_DisallowsPartialBundleAcceptance()
    {
        SdpSessionDescription offer = SessionTemplate();
        offer.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "group", Value = "LS video" });
        offer.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "group", Value = "X-UNKNOWN audio" });
        offer.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "group", Value = "BUNDLE audio video" });
        SdpMediaDescription offeredAudio = OfferMedia("audio", 4000, SdpTransportProtocols.RtpAvpf, "111");
        AddMediaId(offeredAudio, "audio");
        SdpMediaDescription offeredVideo = OfferMedia("video", 4002, SdpTransportProtocols.RtpAvpf, "96");
        AddMediaId(offeredVideo, "video");
        offer.MediaDescriptions.Add(offeredAudio);
        offer.MediaDescriptions.Add(offeredVideo);
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities { SupportsBundle = true };
        SdpMediaCapability localAudio = RtpMediaCapability("audio", "audio", 6000, 111, "opus", 48000);
        localAudio.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        SdpMediaCapability localVideo = RtpMediaCapability("video", "video", 6002, 96, "H264", 90000);
        localVideo.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        capabilities.Media.Add(localAudio);
        capabilities.Media.Add(localVideo);
        SdpNegotiationOptions options = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            AllowBundleSubset = false
        };

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), options);
        options.AllowMediaRejection = false;
        SdpNegotiationException exception = Assert.Throws<SdpNegotiationException>(() => new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), options))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Media.Select(media => media.Accepted), Is.EqualTo(new[] { false, false }));
            Assert.That(result.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.BundleRequirementsNotMet));
            Assert.That(result.Media[0].Rejection!.ReasonToken, Is.EqualTo("bundle-requirements-not-met"));
            Assert.That(result.Media[1].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.NoCompatibleCodec));
            Assert.That(result.BundleGroups, Is.Empty);
            Assert.That(result.Description!.Attributes.Where(attribute => attribute.Name == "group").Select(attribute => attribute.Value), Is.EqualTo(new[] { "LS" }));
            Assert.That(result.Description.MediaDescriptions.Select(media => AttributeValue(media, "mid")), Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(exception.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationBundleIncompatible));
        });
    }

    [Test]
    public void CreateAnswer_RejectsBundleOnlyMediaWhenBundlingIsUnavailable()
    {
        SdpSessionDescription offer = SessionTemplate();
        offer.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "group", Value = "BUNDLE audio auxiliary" });
        SdpMediaDescription offeredAudio = OfferMedia("audio", 4000, SdpTransportProtocols.RtpAvpf, "111");
        AddMediaId(offeredAudio, "audio");
        SdpMediaDescription offeredAuxiliary = OfferMedia("audio", 0, SdpTransportProtocols.RtpAvpf, "112");
        AddMediaId(offeredAuxiliary, "auxiliary");
        offeredAuxiliary.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "bundle-only" });
        offer.MediaDescriptions.Add(offeredAudio);
        offer.MediaDescriptions.Add(offeredAuxiliary);
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        SdpMediaCapability localAudio = RtpMediaCapability("audio", "audio", 6000, 111, "opus", 48000);
        localAudio.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        SdpMediaCapability localAuxiliary = RtpMediaCapability("audio", "auxiliary", 6002, 112, "opus", 48000);
        localAuxiliary.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        capabilities.Media.Add(localAudio);
        capabilities.Media.Add(localAuxiliary);

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = capabilities });
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            RequireBundle = true
        }))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Media.Select(media => media.Accepted), Is.EqualTo(new[] { true, false }));
            Assert.That(result.Media[1].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.BundleRequirementsNotMet));
            Assert.That(result.Description!.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 6000, 0 }));
            Assert.That(result.Description.MediaDescriptions.Select(media => AttributeValue(media, "mid")), Is.EqualTo(new[] { "audio", "auxiliary" }));
            Assert.That(result.BundleGroups, Is.Empty);
            Assert.That(result.BundleTransports, Is.Empty);
            Assert.That(exception.ParamName, Is.EqualTo("options"));
        });
    }

    [Test]
    public void CreateAnswer_ResolvesMultipleBundleGroupsIndependently()
    {
        SdpSessionDescription offer = SessionTemplate();
        offer.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "group", Value = "BUNDLE audio video" });
        offer.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "group", Value = "BUNDLE auxiliary screen" });
        string[] mediaTypes = new[] { "audio", "video", "audio", "video" };
        string[] mediaIds = new[] { "audio", "video", "auxiliary", "screen" };
        string[] formats = new[] { "111", "96", "112", "97" };
        string[] encodings = new[] { "opus", "VP8", "opus", "H264" };
        int[] clocks = new[] { 48000, 90000, 48000, 90000 };
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities { SupportsBundle = true };
        int mediaIndex = 0;
        while (mediaIndex < mediaIds.Length)
        {
            SdpMediaDescription offeredMedia = OfferMedia(mediaTypes[mediaIndex], 4000 + mediaIndex * 2, SdpTransportProtocols.RtpAvpf, formats[mediaIndex]);
            AddMediaId(offeredMedia, mediaIds[mediaIndex]);
            offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "rtcp-mux" });
            offer.MediaDescriptions.Add(offeredMedia);
            SdpMediaCapability localMedia = RtpMediaCapability(mediaTypes[mediaIndex], mediaIds[mediaIndex], 6000 + mediaIndex * 2, int.Parse(formats[mediaIndex]), encodings[mediaIndex], clocks[mediaIndex]);
            localMedia.Protocols[0] = SdpTransportProtocols.RtpAvpf;
            capabilities.Media.Add(localMedia);
            mediaIndex = mediaIndex + 1;
        }

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = capabilities });

        Assert.Multiple(() =>
        {
            Assert.That(result.Description!.MediaDescriptions.Select(media => media.Port), Is.EqualTo(new[] { 6000, 6000, 6004, 6004 }));
            Assert.That(result.BundleGroups.Select(group => group.MediaIds), Is.EqualTo(new[] { new[] { "audio", "video" }, new[] { "auxiliary", "screen" } }));
            Assert.That(result.BundleTransports.Select(transport => transport.TaggedMediaId), Is.EqualTo(new[] { "audio", "auxiliary" }));
            Assert.That(result.Media.Select(media => media.TransportMediaIndex), Is.EqualTo(new[] { 0, 0, 2, 2 }));
            Assert.That(result.Media.Select(media => media.BundleGroupIndex), Is.EqualTo(new[] { 0, 0, 1, 1 }));
        });
    }

    [Test]
    public void CreateAnswer_NegotiatesStreamsRidSimulcastAndImageConstraints()
    {
        SdpSessionDescription offer = SessionTemplate();
        SdpMediaDescription offeredMedia = new SdpSessionFactory().CreateMedia("video", 4000, SdpTransportProtocols.RtpAvpf, new List<string> { "120", "121" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = "120 VP8/90000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = "121 H264/90000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "extmap", Value = "4 " + SdpHeaderExtensionUris.RtpStreamId });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rid", Value = "low send pt=120" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rid", Value = "high send pt=121" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rid", Value = "remote recv pt=120" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "simulcast", Value = "send low;high recv remote" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "imageattr", Value = "120 send [x=1280,y=720] recv *" });
        offer.MediaDescriptions.Add(offeredMedia);

        SdpMediaCapability localMedia = RtpMediaCapability("video", "video", 6000, 100, "VP8", 90000);
        localMedia.Protocols[0] = SdpTransportProtocols.RtpAvpf;
        localMedia.HeaderExtensions.Add(new SdpHeaderExtension { Uri = SdpHeaderExtensionUris.RtpStreamId });
        localMedia.MediaStreamIds.Add(new SdpMediaStreamId { StreamId = "local-stream", HasTrackId = true, TrackId = "local-track" });
        localMedia.SourceAttributes.Add(new SdpSourceAttribute { SynchronizationSource = 1000u, Name = "cname", HasValue = true, Value = "local@example.test" });
        localMedia.SourceAttributes.Add(new SdpSourceAttribute { SynchronizationSource = 1000u, Name = "msid", HasValue = true, Value = "local-stream local-track" });
        localMedia.SourceAttributes.Add(new SdpSourceAttribute { SynchronizationSource = 1001u, Name = "cname", HasValue = true, Value = "local@example.test" });
        SdpSourceGroupAttribute sourceGroup = new SdpSourceGroupAttribute { Semantic = SdpGroupSemantics.FlowIdentification };
        sourceGroup.SynchronizationSources.Add(1000u);
        sourceGroup.SynchronizationSources.Add(1001u);
        localMedia.SourceGroups.Add(sourceGroup);
        SdpRidAttribute low = new SdpRidAttribute { Id = "low", Direction = SdpDirection.ReceiveOnly };
        low.PayloadTypes.Add(120);
        low.Restrictions.Add(new SdpRidRestriction { Name = "max-width", HasValue = true, Value = "640", RawText = "max-width=640" });
        SdpRidAttribute high = new SdpRidAttribute { Id = "high", Direction = SdpDirection.ReceiveOnly };
        SdpRidAttribute remote = new SdpRidAttribute { Id = "remote", Direction = SdpDirection.SendOnly };
        remote.PayloadTypes.Add(96);
        localMedia.Rids.Add(low);
        localMedia.Rids.Add(high);
        localMedia.Rids.Add(remote);
        localMedia.Simulcast = new SdpSimulcastAttribute();
        SdpSimulcastStream localSend = new SdpSimulcastStream();
        localSend.Alternatives.Add(new SdpSimulcastAlternative { RidId = "remote" });
        localMedia.Simulcast.SendStreams.Add(localSend);
        SdpSimulcastStream localReceiveLow = new SdpSimulcastStream();
        localReceiveLow.Alternatives.Add(new SdpSimulcastAlternative { RidId = "low" });
        localMedia.Simulcast.ReceiveStreams.Add(localReceiveLow);
        SdpSimulcastStream localReceiveHigh = new SdpSimulcastStream();
        localReceiveHigh.Alternatives.Add(new SdpSimulcastAlternative { RidId = "high" });
        localMedia.Simulcast.ReceiveStreams.Add(localReceiveHigh);
        localMedia.ImageAttributes.Add(new SdpImageAttribute
        {
            PayloadType = "*",
            HasSendConstraints = true,
            SendConstraints = "[x=640,y=360]",
            HasReceiveConstraints = true,
            ReceiveConstraints = "[x=1280,y=720]"
        });
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(localMedia);
        SdpNegotiationOptions options = new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            PreservePayloadTypes = false
        };

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), options);
        SdpMediaDescription answerMedia = result.Description!.MediaDescriptions[0];
        SdpValidationResult validation = new SdpValidator().ValidateDescription(result.Description, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Rtp,
            NegotiationRole = SdpNegotiationRole.Answerer,
            OfferDescription = offer
        });
        localMedia.HeaderExtensions.Clear();
        SdpNegotiationResult withoutExtension = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), options);

        Assert.Multiple(() =>
        {
            Assert.That(answerMedia.Formats, Is.EqualTo(new[] { "96" }));
            Assert.That(AttributeValues(answerMedia, "msid"), Is.EqualTo(new[] { "local-stream local-track" }));
            Assert.That(AttributeValues(answerMedia, "ssrc-group"), Is.EqualTo(new[] { "FID 1000 1001" }));
            Assert.That(AttributeValues(answerMedia, "ssrc"), Is.EqualTo(new[]
            {
                "1000 cname:local@example.test",
                "1000 msid:local-stream local-track",
                "1001 cname:local@example.test"
            }));
            Assert.That(AttributeValues(answerMedia, "rid"), Is.EqualTo(new[] { "low recv pt=96;max-width=640", "remote send pt=96" }));
            Assert.That(AttributeValues(answerMedia, "simulcast"), Is.EqualTo(new[] { "send remote recv low" }));
            Assert.That(AttributeValues(answerMedia, "imageattr"), Is.EqualTo(new[] { "96 send [x=640,y=360] recv [x=1280,y=720]" }));
            Assert.That(result.Media[0].EffectiveMedia!.SourceAttributes, Has.Count.EqualTo(3));
            Assert.That(result.Media[0].EffectiveMedia!.SourceGroups.Single().SynchronizationSources, Is.EqualTo(new uint[] { 1000u, 1001u }));
            Assert.That(result.Media[0].EffectiveMedia!.Rids.Select(value => value.Id), Is.EqualTo(new[] { "low", "remote" }));
            Assert.That(result.Media[0].EffectiveMedia!.ImageAttributes.Single().PayloadType, Is.EqualTo("96"));
            Assert.That(validation.Issues.Where(issue => issue.Severity == SdpValidationSeverity.Error || issue.Severity == SdpValidationSeverity.Fatal), Is.Empty, string.Join("|", validation.Issues.Select(issue => issue.Code + ":" + issue.Detail)));
            Assert.That(AttributeValues(withoutExtension.Description!.MediaDescriptions[0], "rid"), Is.Empty);
            Assert.That(AttributeValues(withoutExtension.Description.MediaDescriptions[0], "simulcast"), Is.Empty);
            Assert.That(AttributeValues(offer.MediaDescriptions[0], "rid"), Is.EqualTo(new[] { "low send pt=120", "high send pt=121", "remote recv pt=120" }));
            Assert.That(low.PayloadTypes, Is.EqualTo(new[] { 120 }));
        });
    }

    [Test]
    public void CreateAnswer_NegotiatesModernSctpMessageSizeAndDataChannelSubset()
    {
        SdpSessionDescription offer = SessionTemplate();
        SdpMediaDescription offeredMedia = OfferMedia("application", 4000, SdpTransportProtocols.UdpDtlsSctp, SdpDataChannelTokens.WebRtcDataChannel);
        offeredMedia.Attributes.Add(EncodedAttribute(Fingerprint(SdpFingerprintAlgorithms.Sha256, 0x11, 32)));
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "setup", Value = "actpass" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "sctp-port", Value = "5000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "max-message-size", Value = "100000" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "dcmap", Value = "1 subprotocol=\"msrp\";label=\"messages\"" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "dcsa", Value = "1 accept-types:text/plain" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "dcmap", Value = "2 subprotocol=\"chat\"" });
        offer.MediaDescriptions.Add(offeredMedia);
        SdpMediaCapability localMedia = new SdpMediaCapability
        {
            MediaKind = SdpMediaKind.Application,
            MediaType = "application",
            Port = 6000,
            Sctp = new SdpSctpDescription
            {
                Port = 6000,
                Format = SdpDataChannelTokens.WebRtcDataChannel,
                HasMaximumMessageSize = true,
                MaximumMessageSize = 65536
            },
            Dtls = Dtls(Fingerprint(SdpFingerprintAlgorithms.Sha256, 0x22, 32), string.Empty)
        };
        localMedia.Protocols.Add(SdpTransportProtocols.UdpDtlsSctp);
        SdpDataChannelAttribute supportedChannel = new SdpDataChannelAttribute { ChannelId = 10 };
        supportedChannel.Parameters.Add(new SdpDataChannelParameter { Name = SdpDataChannelTokens.Subprotocol, HasValue = true, Value = "msrp", RawText = "subprotocol=msrp" });
        localMedia.DataChannels.Add(supportedChannel);
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities
        {
            SupportsDataChannels = true,
            SupportsDtls = true
        };
        capabilities.Media.Add(localMedia);

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), new SdpNegotiationOptions { LocalCapabilities = capabilities });
        SdpMediaDescription answerMedia = result.Description!.MediaDescriptions[0];
        SdpValidationResult validation = new SdpValidator().ValidateDescription(result.Description, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.DataChannel,
            NegotiationRole = SdpNegotiationRole.Answerer,
            OfferDescription = offer
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Media[0].Accepted, Is.True);
            Assert.That(answerMedia.Formats, Is.EqualTo(new[] { SdpDataChannelTokens.WebRtcDataChannel }));
            Assert.That(AttributeValue(answerMedia, "sctp-port"), Is.EqualTo("6000"));
            Assert.That(AttributeValue(answerMedia, "max-message-size"), Is.EqualTo("65536"));
            Assert.That(AttributeValues(answerMedia, "dcmap"), Is.EqualTo(new[] { "1 subprotocol=\"msrp\";label=\"messages\"" }));
            Assert.That(AttributeValues(answerMedia, "dcsa"), Is.EqualTo(new[] { "1 accept-types:text/plain" }));
            Assert.That(result.Media[0].Sctp!.Port, Is.EqualTo(6000));
            Assert.That(result.Media[0].Sctp!.MaximumMessageSize, Is.EqualTo(65536));
            Assert.That(result.Media[0].EffectiveMedia!.Sctp!.Port, Is.EqualTo(6000));
            Assert.That(result.Media[0].EffectiveMedia!.DataChannels.Single().ChannelId, Is.EqualTo(1));
            Assert.That(validation.Issues.Where(issue => issue.Severity == SdpValidationSeverity.Error || issue.Severity == SdpValidationSeverity.Fatal), Is.Empty, string.Join("|", validation.Issues.Select(issue => issue.Code + ":" + issue.Detail)));
            Assert.That(AttributeValue(offer.MediaDescriptions[0], "sctp-port"), Is.EqualTo("5000"));
            Assert.That(localMedia.Sctp!.Port, Is.EqualTo(6000));
        });
    }

    [Test]
    public void CreateAnswer_RejectsIncompatibleSctpCapabilities()
    {
        SdpSessionDescription offer = SessionTemplate();
        SdpMediaDescription offeredMedia = OfferMedia("application", 4000, SdpTransportProtocols.UdpDtlsSctp, SdpDataChannelTokens.WebRtcDataChannel);
        offeredMedia.Attributes.Add(EncodedAttribute(Fingerprint(SdpFingerprintAlgorithms.Sha256, 0x11, 32)));
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "setup", Value = "actpass" });
        offeredMedia.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "sctp-port", Value = "5000" });
        offer.MediaDescriptions.Add(offeredMedia);
        SdpMediaCapability localMedia = new SdpMediaCapability
        {
            MediaKind = SdpMediaKind.Application,
            MediaType = "application",
            Port = 6000,
            Sctp = new SdpSctpDescription { Port = 6000, Format = "unsupported-format" },
            Dtls = Dtls(Fingerprint(SdpFingerprintAlgorithms.Sha256, 0x22, 32), string.Empty)
        };
        localMedia.Protocols.Add(SdpTransportProtocols.UdpDtlsSctp);
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities
        {
            SupportsDataChannels = true,
            SupportsDtls = true
        };
        capabilities.Media.Add(localMedia);
        SdpNegotiationOptions options = new SdpNegotiationOptions { LocalCapabilities = capabilities };

        SdpNegotiationResult result = new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), options);
        options.AllowMediaRejection = false;
        SdpNegotiationException exception = Assert.Throws<SdpNegotiationException>(() => new SdpOfferAnswerService().CreateAnswer(offer, SessionTemplate(), options))!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Media[0].Rejection!.Reason, Is.EqualTo(SdpRejectionReason.DataChannelRequirementsNotMet));
            Assert.That(result.Media[0].Rejection!.ReasonToken, Is.EqualTo("data-channel-requirements-not-met"));
            Assert.That(result.Description!.MediaDescriptions[0].Port, Is.Zero);
            Assert.That(exception.Code, Is.EqualTo(SdpDiagnosticCodes.NegotiationDataChannelIncompatible));
        });
    }

    [Test]
    public void CreateAnswer_ValidatesArgumentsRoleAndOfferShape()
    {
        ISdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpSessionDescription offer = SessionTemplate();
        offer.MediaDescriptions.Add(OfferMedia("audio", 4000, SdpTransportProtocols.UdpTlsRtpSavpf, "111"));
        SdpNegotiationOptions offerer = ValidOptions();
        offerer.Role = SdpNegotiationRole.Offerer;

        Assert.Multiple(() =>
        {
            Assert.That(() => service.CreateAnswer(null!, SessionTemplate(), ValidOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("offer"));
            Assert.That(() => service.CreateAnswer(offer, null!, ValidOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("sessionTemplate"));
            Assert.That(() => service.CreateAnswer(offer, SessionTemplate(), null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => service.CreateAnswer(new SdpSessionDescription(), SessionTemplate(), ValidOptions()), Throws.ArgumentException.With.Property("ParamName").EqualTo("offer"));
            Assert.That(() => service.CreateAnswer(offer, SessionTemplate(), offerer), Throws.ArgumentException.With.Property("ParamName").EqualTo("options"));
        });
    }

    static SdpNegotiationOptions ValidOptions()
    {
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        capabilities.Media.Add(RtpMediaCapability("audio", "audio", 5004, 111, "opus", 48000));
        return new SdpNegotiationOptions { LocalCapabilities = capabilities };
    }

    static SdpMediaCapability RtpMediaCapability(string mediaType, string mediaId, int port, int payloadType, string encodingName, int clockRate)
    {
        SdpMediaCapability media = new SdpMediaCapability
        {
            MediaKind = mediaType == "audio" ? SdpMediaKind.Audio : SdpMediaKind.Video,
            MediaType = mediaType,
            MediaId = mediaId,
            Port = port,
            Direction = SdpDirection.SendReceive,
            SupportsRtcpMux = true
        };
        media.Protocols.Add(SdpTransportProtocols.UdpTlsRtpSavpf);
        media.Codecs.Add(new SdpCodecCapability
        {
            MediaKind = media.MediaKind,
            MediaType = mediaType,
            EncodingName = encodingName,
            ClockRate = clockRate,
            HasPreferredPayloadType = true,
            PreferredPayloadType = payloadType
        });
        return media;
    }

    static SdpMediaDescription OfferMedia(string mediaType, int port, string protocol, string format)
    {
        SdpMediaDescription media = new SdpSessionFactory().CreateMedia(mediaType, port, protocol, new List<string> { format });
        if (protocol.Contains("RTP", StringComparison.OrdinalIgnoreCase))
        {
            string encoding = mediaType == "audio" ? "opus/48000/2" : format == "96" ? "VP8/90000" : "H264/90000";
            media.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "rtpmap", Value = format + " " + encoding });
        }
        return media;
    }

    static void AddMediaId(SdpMediaDescription media, string mediaId)
    {
        media.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "mid", Value = mediaId });
    }

    static SdpCodecCapability AssociatedCodec(string encodingName, string parameterName, string parameterValue)
    {
        SdpCodecCapability codec = new SdpCodecCapability
        {
            MediaKind = SdpMediaKind.Video,
            MediaType = "video",
            EncodingName = encodingName,
            ClockRate = 90000
        };
        codec.FormatParameters.Add(new SdpFormatParameter
        {
            Name = parameterName,
            HasValue = parameterValue.Length > 0,
            Value = parameterValue
        });
        return codec;
    }

    static SdpSessionDescription PlainRtpOffer()
    {
        SdpSessionDescription offer = SessionTemplate();
        offer.MediaDescriptions.Add(OfferMedia("audio", 4000, SdpTransportProtocols.RtpAvpf, "111"));
        return offer;
    }

    static SdpSessionDescription SecureOffer()
    {
        SdpSessionDescription offer = SessionTemplate();
        SdpMediaDescription media = OfferMedia("audio", 4000, SdpTransportProtocols.UdpTlsRtpSavpf, "111");
        media.Attributes.Add(EncodedAttribute(Fingerprint(SdpFingerprintAlgorithms.Sha256, 0x11, 32)));
        media.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "setup", Value = "actpass" });
        offer.MediaDescriptions.Add(media);
        return offer;
    }

    static SdpEndpointCapabilities AnswerCapabilities(string protocol)
    {
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities();
        SdpMediaCapability media = RtpMediaCapability("audio", "audio", 6000, 111, "opus", 48000);
        media.Protocols[0] = protocol;
        capabilities.Media.Add(media);
        return capabilities;
    }

    static SdpFingerprintAttribute Fingerprint(string algorithm, byte value, int count)
    {
        SdpFingerprintAttribute fingerprint = new SdpFingerprintAttribute { Algorithm = algorithm };
        int index = 0;
        while (index < count)
        {
            fingerprint.Digest.Add(value);
            index = index + 1;
        }
        return fingerprint;
    }

    static SdpAttribute EncodedAttribute(object value)
    {
        return new SdpAttributeCodec().EncodeAttribute(value).Attribute!;
    }

    static SdpIceDescription Ice(string usernameFragment, string password, int port)
    {
        SdpIceDescription ice = new SdpIceDescription
        {
            Credentials = new SdpIceCredentials { UsernameFragment = usernameFragment, Password = password }
        };
        ice.Candidates.Add(new SdpIceCandidate
        {
            Foundation = "1",
            Component = 1,
            TransportProtocol = SdpIceTransportProtocol.Udp,
            TransportToken = "UDP",
            Priority = 2113937151u,
            Address = "192.0.2.1",
            AddressKind = SdpAddressKind.Ipv4,
            Port = port,
            CandidateType = SdpIceCandidateType.Host,
            CandidateTypeToken = "host"
        });
        return ice;
    }

    static SdpDtlsDescription Dtls(SdpFingerprintAttribute fingerprint, string tlsId)
    {
        SdpDtlsDescription dtls = new SdpDtlsDescription
        {
            SetupRole = SdpDtlsSetupRole.ActPass,
            SetupRoleToken = "actpass",
            TlsId = tlsId
        };
        dtls.Fingerprints.Add(fingerprint);
        return dtls;
    }

    static SdpSessionDescription SessionTemplate()
    {
        return new SdpSessionFactory().CreateSession(
            new SdpOrigin
            {
                Username = "-",
                SessionId = "1234",
                SessionVersion = "1",
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                UnicastAddress = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            "Generated offer",
            new SdpConnectionData
            {
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                Address = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            new SdpTiming { StartTime = "0", StopTime = "0" });
    }

    static string? AttributeValue(SdpMediaDescription media, string name)
    {
        return media.Attributes.FirstOrDefault(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    static List<string> AttributeValues(SdpMediaDescription media, string name)
    {
        return media.Attributes.Where(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase)).Select(attribute => attribute.Value).ToList();
    }

    static bool HasAttribute(SdpMediaDescription media, string name)
    {
        return media.Attributes.Any(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
