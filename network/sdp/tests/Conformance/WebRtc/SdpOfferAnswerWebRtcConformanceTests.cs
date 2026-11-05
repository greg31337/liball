namespace sdp.Tests.Conformance.WebRtc;

public class SdpOfferAnswerWebRtcConformanceTests
{
    [Test]
    public void BundledOfferAnswer_NegotiatesTransportExtensionsStreamsSimulcastAndDataChannels()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities offerCapabilities = WebRtcCapabilities("offer", SdpDtlsSetupRole.ActPass, "H264", 0x11);
        SdpSessionDescription offer = service.CreateInitialOffer(Session("192.0.2.1"), Options(offerCapabilities)).Description!;
        SdpEndpointCapabilities answerCapabilities = WebRtcCapabilities("answer", SdpDtlsSetupRole.Passive, "VP9", 0x22);
        answerCapabilities.Ice!.Options.Remove("renomination");

        SdpNegotiationResult result = service.CreateAnswer(offer, Session("192.0.2.2"), Options(answerCapabilities));
        SdpSessionDescription answer = result.Description!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Media.Select(media => media.Accepted), Is.EqualTo(new[] { true, true, false, true }));
            Assert.That(result.Rejections.Single().Reason, Is.EqualTo(SdpRejectionReason.NoCompatibleCodec));
            Assert.That(result.BundleGroups.Single().MediaIds, Is.EqualTo(new[] { "audio", "video", "data" }));
            Assert.That(result.BundleTransports.Single().TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(result.BundleTransports[0].Ice!.Credentials!.UsernameFragment, Is.EqualTo("answerUfrag"));
            Assert.That(result.BundleTransports[0].Dtls!.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Passive));
            Assert.That(result.BundleTransports[0].Dtls!.TlsId, Is.EqualTo("answer-tls-association-1"));
            Assert.That(AttributeValue(answer.MediaDescriptions[0], SdpAttributeNames.IceOptions), Is.EqualTo("trickle"));
            Assert.That(HasAttribute(answer.MediaDescriptions[0], SdpAttributeNames.RtcpMux), Is.True);
            Assert.That(result.Media[0].HeaderExtensionMappings.Single().Uri, Is.EqualTo(SdpHeaderExtensionUris.MediaId));
            Assert.That(result.Media[1].HeaderExtensionMappings.Select(mapping => mapping.Uri), Is.EqualTo(new[] { SdpHeaderExtensionUris.MediaId, SdpHeaderExtensionUris.RtpStreamId }));
            Assert.That(result.Media[1].EffectiveMedia!.MediaStreamIds.Single().StreamId, Is.EqualTo("answer-stream"));
            Assert.That(result.Media[1].EffectiveMedia!.Rids.Select(rid => rid.Id), Is.EqualTo(new[] { "low" }));
            Assert.That(result.Media[1].EffectiveMedia!.Simulcast!.ReceiveStreams.Single().Alternatives.Single().RidId, Is.EqualTo("low"));
            Assert.That(result.Media[3].EffectiveMedia!.Sctp!.MaximumMessageSize, Is.EqualTo(65536));
            Assert.That(result.Media[3].EffectiveMedia!.DataChannels.Single().ChannelId, Is.EqualTo(1));
            Assert.That(AttributeValue(offer.MediaDescriptions[0], SdpAttributeNames.IceUsernameFragment), Is.EqualTo("offerUfrag"));
            Assert.That(offerCapabilities.Dtls!.Fingerprints[0].SourceAttribute, Is.Null);
            Assert.That(answerCapabilities.Media[1].Rids.Select(rid => rid.Id), Is.EqualTo(new[] { "low" }));
        });
    }

    [TestCase(SdpDtlsSetupRole.Active, "active")]
    [TestCase(SdpDtlsSetupRole.Passive, "passive")]
    public void DtlsAnswer_SelectsEitherLegalRoleFromAnActPassOffer(SdpDtlsSetupRole role, string token)
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities offerCapabilities = WebRtcCapabilities("offer", SdpDtlsSetupRole.ActPass, "H264", 0x11);
        SdpSessionDescription offer = service.CreateInitialOffer(Session("192.0.2.1"), Options(offerCapabilities)).Description!;
        SdpEndpointCapabilities answerCapabilities = WebRtcCapabilities("answer", role, "H264", 0x22);

        SdpNegotiationResult result = service.CreateAnswer(offer, Session("192.0.2.2"), Options(answerCapabilities));
        SdpValidationResult validation = Validate(result.Description!, SdpNegotiationRole.Answerer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result.Description!.MediaDescriptions[0], SdpAttributeNames.Setup), Is.EqualTo(token));
            Assert.That(result.Media[0].EffectiveMedia!.Dtls!.SetupRole, Is.EqualTo(role));
            Assert.That(result.Media[0].EffectiveMedia!.Dtls!.Fingerprints.Single().Digest[0], Is.EqualTo(0x22));
            Assert.That(validation.Issues.Any(issue => issue.Detail.Contains("reactivate", StringComparison.OrdinalIgnoreCase)), Is.False, Diagnostics(validation));
        });
    }

    [Test]
    public void BundledSubsequentOffer_RestartsIceWhilePreservingDtlsAssociationAndMappings()
    {
        SdpOfferAnswerService service = new SdpOfferAnswerService();
        SdpEndpointCapabilities initialCapabilities = WebRtcCapabilities("old", SdpDtlsSetupRole.ActPass, "H264", 0x31);
        SdpSessionDescription previous = service.CreateInitialOffer(Session("192.0.2.1"), Options(initialCapabilities)).Description!;
        SdpEndpointCapabilities restartedCapabilities = WebRtcCapabilities("new", SdpDtlsSetupRole.ActPass, "H264", 0x42);

        SdpNegotiationResult retained = service.CreateSubsequentOffer(previous, SubsequentOptions(restartedCapabilities, false));
        SdpNegotiationResult restarted = service.CreateSubsequentOffer(previous, SubsequentOptions(restartedCapabilities, true));

        Assert.Multiple(() =>
        {
            Assert.That(retained.BundleTransports.Single().Ice!.Credentials!.UsernameFragment, Is.EqualTo("oldUfrag"));
            Assert.That(restarted.BundleTransports.Single().Ice!.Credentials!.UsernameFragment, Is.EqualTo("newUfrag"));
            Assert.That(restarted.BundleGroups.Single().MediaIds, Is.EqualTo(new[] { "audio", "video", "screen", "data" }));
            Assert.That(AttributeValue(restarted.Description!.MediaDescriptions[0], SdpAttributeNames.IceOptions), Is.EqualTo("trickle renomination"));
            Assert.That(AttributeValue(restarted.Description.MediaDescriptions[0], SdpAttributeNames.TlsId), Is.EqualTo("old-tls-association-1"));
            Assert.That(AttributeValue(restarted.Description.MediaDescriptions[0], SdpAttributeNames.Fingerprint), Is.EqualTo(AttributeValue(previous.MediaDescriptions[0], SdpAttributeNames.Fingerprint)));
            Assert.That(restarted.Media[0].PayloadMappings.Single().NegotiatedPayloadType, Is.EqualTo(111));
            Assert.That(restarted.Media[1].HeaderExtensionMappings.Select(mapping => mapping.NegotiatedId), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(restarted.Description.Origin!.SessionVersion, Is.EqualTo("2"));
            Assert.That(AttributeValue(previous.MediaDescriptions[0], SdpAttributeNames.IceUsernameFragment), Is.EqualTo("oldUfrag"));
            Assert.That(previous.Origin!.SessionVersion, Is.EqualTo("1"));
            Assert.That(restartedCapabilities.Dtls!.TlsId, Is.EqualTo("new-tls-association-1"));
        });
    }

    static SdpNegotiationOptions Options(SdpEndpointCapabilities capabilities)
    {
        return new SdpNegotiationOptions
        {
            LocalCapabilities = capabilities,
            RequireIce = true,
            RequireDtls = true,
            RequireRtcpMux = true,
            RequireBundle = true,
            PreservePayloadTypes = true,
            AllowBundleSubset = true
        };
    }

    static SdpNegotiationOptions SubsequentOptions(SdpEndpointCapabilities capabilities, bool allowIceRestart)
    {
        SdpNegotiationOptions options = Options(capabilities);
        options.AllowIceRestart = allowIceRestart;
        return options;
    }

    static SdpEndpointCapabilities WebRtcCapabilities(string prefix, SdpDtlsSetupRole setupRole, string screenCodec, byte fingerprintByte)
    {
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities
        {
            SupportsIce = true,
            SupportsDtls = true,
            SupportsBundle = true,
            SupportsDataChannels = true,
            Ice = Ice(prefix),
            Dtls = Dtls(prefix, setupRole, fingerprintByte)
        };
        SdpMediaCapability audio = RtpMedia("audio", SdpMediaKind.Audio, "audio", 5000, "opus", 48000, 111, false);
        audio.Codecs[0].HasChannelCount = true;
        audio.Codecs[0].ChannelCount = 2;
        audio.HeaderExtensions.Add(Extension(SdpHeaderExtensionUris.MediaId, 1));
        audio.MediaStreamIds.Add(new SdpMediaStreamId { StreamId = prefix + "-stream", HasTrackId = true, TrackId = prefix + "-audio" });
        SdpMediaCapability video = RtpMedia("video", SdpMediaKind.Video, "video", 5000, "VP8", 90000, 96, true);
        video.HeaderExtensions.Add(Extension(SdpHeaderExtensionUris.MediaId, 1));
        video.HeaderExtensions.Add(Extension(SdpHeaderExtensionUris.RtpStreamId, 2));
        video.MediaStreamIds.Add(new SdpMediaStreamId { StreamId = prefix + "-stream", HasTrackId = true, TrackId = prefix + "-video" });
        SdpRidAttribute low = new SdpRidAttribute { Id = "low", Direction = setupRole == SdpDtlsSetupRole.ActPass ? SdpDirection.SendOnly : SdpDirection.ReceiveOnly };
        low.PayloadTypes.Add(96);
        video.Rids.Add(low);
        SdpSimulcastStream simulcastStream = new SdpSimulcastStream();
        simulcastStream.Alternatives.Add(new SdpSimulcastAlternative { RidId = "low" });
        video.Simulcast = new SdpSimulcastAttribute();
        if (setupRole == SdpDtlsSetupRole.ActPass)
        {
            video.Simulcast.SendStreams.Add(simulcastStream);
        }
        else
        {
            video.Simulcast.ReceiveStreams.Add(simulcastStream);
        }
        SdpMediaCapability screen = RtpMedia("video", SdpMediaKind.Video, "screen", 5000, screenCodec, 90000, 97, true);
        screen.HeaderExtensions.Add(Extension(SdpHeaderExtensionUris.MediaId, 1));
        SdpMediaCapability data = DataMedia();
        capabilities.Media.Add(audio);
        capabilities.Media.Add(video);
        capabilities.Media.Add(screen);
        capabilities.Media.Add(data);
        return capabilities;
    }

    static SdpMediaCapability RtpMedia(string mediaType, SdpMediaKind kind, string mediaId, int port, string codecName, int clockRate, int payloadType, bool bundleOnly)
    {
        SdpMediaCapability media = new SdpMediaCapability
        {
            MediaType = mediaType,
            MediaKind = kind,
            MediaId = mediaId,
            Port = port,
            Direction = SdpDirection.SendReceive,
            SupportsRtcpMux = true,
            BundleOnly = bundleOnly
        };
        media.Protocols.Add(SdpTransportProtocols.UdpTlsRtpSavpf);
        media.Codecs.Add(new SdpCodecCapability
        {
            MediaType = mediaType,
            MediaKind = kind,
            EncodingName = codecName,
            ClockRate = clockRate,
            HasPreferredPayloadType = true,
            PreferredPayloadType = payloadType
        });
        return media;
    }

    static SdpMediaCapability DataMedia()
    {
        SdpMediaCapability media = new SdpMediaCapability
        {
            MediaType = "application",
            MediaKind = SdpMediaKind.Application,
            MediaId = "data",
            Port = 5000,
            Direction = SdpDirection.SendReceive,
            BundleOnly = true,
            Sctp = new SdpSctpDescription
            {
                Port = 5000,
                Format = SdpDataChannelTokens.WebRtcDataChannel,
                HasMaximumMessageSize = true,
                MaximumMessageSize = 65536
            }
        };
        media.Protocols.Add(SdpTransportProtocols.UdpDtlsSctp);
        SdpDataChannelAttribute channel = new SdpDataChannelAttribute { ChannelId = 1 };
        channel.Parameters.Add(new SdpDataChannelParameter { Name = SdpDataChannelTokens.Subprotocol, HasValue = true, Value = "msrp" });
        media.DataChannels.Add(channel);
        return media;
    }

    static SdpHeaderExtension Extension(string uri, int identifier)
    {
        return new SdpHeaderExtension
        {
            Uri = uri,
            HasPreferredId = true,
            PreferredId = identifier,
            Direction = SdpDirection.SendReceive
        };
    }

    static SdpIceDescription Ice(string prefix)
    {
        SdpIceDescription ice = new SdpIceDescription
        {
            Credentials = new SdpIceCredentials
            {
                UsernameFragment = prefix + "Ufrag",
                Password = prefix + "PasswordValue1234567890"
            },
            EndOfCandidates = true
        };
        ice.Options.Add("trickle");
        ice.Options.Add("renomination");
        ice.Candidates.Add(new SdpIceCandidate
        {
            Foundation = "1",
            Component = 1,
            TransportProtocol = SdpIceTransportProtocol.Udp,
            TransportToken = "UDP",
            Priority = 2113937151u,
            Address = "192.0.2.10",
            AddressKind = SdpAddressKind.Ipv4,
            Port = 5000,
            CandidateType = SdpIceCandidateType.Host,
            CandidateTypeToken = "host"
        });
        return ice;
    }

    static SdpDtlsDescription Dtls(string prefix, SdpDtlsSetupRole setupRole, byte fingerprintByte)
    {
        SdpDtlsDescription dtls = new SdpDtlsDescription
        {
            SetupRole = setupRole,
            SetupRoleToken = setupRole == SdpDtlsSetupRole.ActPass ? "actpass" : setupRole == SdpDtlsSetupRole.Active ? "active" : "passive",
            TlsId = prefix + "-tls-association-1"
        };
        SdpFingerprintAttribute fingerprint = new SdpFingerprintAttribute { Algorithm = SdpFingerprintAlgorithms.Sha256 };
        int index = 0;
        while (index < 32)
        {
            fingerprint.Digest.Add(fingerprintByte);
            index = index + 1;
        }
        dtls.Fingerprints.Add(fingerprint);
        return dtls;
    }

    static SdpSessionDescription Session(string address)
    {
        return new SdpSessionFactory().CreateSession(
            new SdpOrigin
            {
                Username = "-",
                SessionId = "9000",
                SessionVersion = "1",
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                UnicastAddress = address,
                AddressKind = SdpAddressKind.Ipv4
            },
            "WebRTC",
            new SdpConnectionData
            {
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                Address = address,
                AddressKind = SdpAddressKind.Ipv4
            },
            new SdpTiming { StartTime = "0", StopTime = "0" });
    }

    static SdpValidationResult Validate(SdpSessionDescription description, SdpNegotiationRole role, SdpSessionDescription? offer)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.WebRtc,
            NegotiationRole = role,
            OfferDescription = offer,
            MaximumDiagnosticCount = 256
        });
    }

    static string Diagnostics(SdpValidationResult result)
    {
        return string.Join("|", result.Issues.Select(issue => issue.MediaIndex + ":" + issue.AttributeName + ":" + issue.Code + ":" + issue.Detail));
    }

    static bool HasAttribute(SdpMediaDescription media, string name)
    {
        return media.Attributes.Any(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    static string? AttributeValue(SdpMediaDescription media, string name)
    {
        return media.Attributes.FirstOrDefault(attribute => string.Equals(attribute.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}
