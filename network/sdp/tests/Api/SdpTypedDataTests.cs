namespace sdp.Tests.Api;

public class SdpTypedDataTests
{
    [Test]
    public void RtpAttributeAndCodecGraph_PreservesTypedAndOpaqueValues()
    {
        SdpAttribute source = new SdpAttribute { Name = "rtpmap", Value = "111 opus/48000/2" };
        SdpRtpMapAttribute rtpMap = new SdpRtpMapAttribute
        {
            SourceAttribute = source,
            PayloadType = 111,
            EncodingName = "opus",
            ClockRate = 48000,
            HasEncodingParameters = true,
            EncodingParameters = "2",
            HasChannelCount = true,
            ChannelCount = 2
        };
        SdpFormatParametersAttribute format = new SdpFormatParametersAttribute
        {
            PayloadType = 111,
            RawParameters = "useinbandfec=1;stereo"
        };
        format.Parameters.Add(new SdpFormatParameter { Name = "useinbandfec", HasValue = true, Value = "1" });
        format.Parameters.Add(new SdpFormatParameter { Name = "stereo", HasValue = false, RawText = "stereo" });
        SdpRtcpFeedbackAttribute feedback = new SdpRtcpFeedbackAttribute
        {
            PayloadType = "111",
            Type = "nack",
            HasParameter = true,
            Parameter = "pli"
        };
        SdpCodec codec = new SdpCodec
        {
            PayloadType = 111,
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            EncodingName = "opus",
            ClockRate = 48000,
            HasChannelCount = true,
            ChannelCount = 2,
            RtpMap = rtpMap
        };
        codec.FormatParameters.Add(format);
        codec.RtcpFeedback.Add(feedback);
        SdpRtcpAttribute rtcp = new SdpRtcpAttribute
        {
            Port = 49171,
            HasNetworkAddress = true,
            NetworkType = "IN",
            AddressType = "IP4",
            Address = "203.0.113.2",
            AddressKind = SdpAddressKind.Ipv4
        };
        SdpRtcpExtendedReportAttribute reports = new SdpRtcpExtendedReportAttribute { RawValue = "pkt-loss-rle stat-summary" };
        reports.Blocks.Add("pkt-loss-rle");
        reports.Blocks.Add("stat-summary");

        Assert.Multiple(() =>
        {
            Assert.That(codec.RtpMap?.SourceAttribute, Is.SameAs(source));
            Assert.That(codec.EncodingName, Is.EqualTo("opus"));
            Assert.That(codec.ChannelCount, Is.EqualTo(2));
            Assert.That(codec.FormatParameters[0].Parameters.Select(x => x.Name), Is.EqualTo(new[] { "useinbandfec", "stereo" }));
            Assert.That(codec.FormatParameters[0].Parameters[1].HasValue, Is.False);
            Assert.That(codec.RtcpFeedback[0].Parameter, Is.EqualTo("pli"));
            Assert.That(rtcp.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4));
            Assert.That(reports.Blocks, Is.EqualTo(new[] { "pkt-loss-rle", "stat-summary" }));
            Assert.That(new SdpFormatParametersAttribute().Parameters, Is.Empty);
            Assert.That(new SdpCodec().RtcpFeedback, Is.Empty);
            Assert.That(new SdpRtcpExtendedReportAttribute().Blocks, Is.Empty);
        });
    }

    [Test]
    public void CapabilityGraph_PreservesPreferenceAndIndependentCollections()
    {
        SdpCodecCapability codec = new SdpCodecCapability
        {
            MediaKind = SdpMediaKind.Video,
            MediaType = "video",
            EncodingName = "VP9",
            ClockRate = 90000,
            Preference = 1
        };
        codec.FormatParameters.Add(new SdpFormatParameter { Name = "profile-id", HasValue = true, Value = "0" });
        codec.RtcpFeedbackTypes.Add("transport-cc");
        SdpMediaCapability media = new SdpMediaCapability
        {
            MediaKind = SdpMediaKind.Video,
            MediaType = "video",
            Direction = SdpDirection.SendReceive,
            SupportsRtcpMux = true,
            SupportsReducedSizeRtcp = true
        };
        media.Protocols.Add("UDP/TLS/RTP/SAVPF");
        media.Codecs.Add(codec);
        SdpEndpointCapabilities endpoint = new SdpEndpointCapabilities
        {
            SupportsIce = true,
            SupportsDtls = true,
            SupportsBundle = true,
            SupportsDataChannels = true
        };
        endpoint.Media.Add(media);
        endpoint.HeaderExtensions.Add(new SdpHeaderExtension { Uri = SdpHeaderExtensionUris.MediaId });
        endpoint.Fingerprints.Add(new SdpFingerprintAttribute { Algorithm = "sha-256" });
        SdpPayloadMapping mapping = new SdpPayloadMapping
        {
            MediaIndex = 0,
            OfferedPayloadType = 120,
            NegotiatedPayloadType = 98,
            EncodingName = "VP9"
        };

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Media[0].Protocols, Is.EqualTo(new[] { "UDP/TLS/RTP/SAVPF" }));
            Assert.That(endpoint.Media[0].Codecs[0].EncodingName, Is.EqualTo("VP9"));
            Assert.That(codec.FormatParameters[0].Value, Is.EqualTo("0"));
            Assert.That(codec.RtcpFeedbackTypes, Is.EqualTo(new[] { "transport-cc" }));
            Assert.That(endpoint.HeaderExtensions[0].Uri, Is.EqualTo(SdpHeaderExtensionUris.MediaId));
            Assert.That(endpoint.Fingerprints[0].Algorithm, Is.EqualTo("sha-256"));
            Assert.That(mapping.OfferedPayloadType, Is.EqualTo(120));
            Assert.That(mapping.NegotiatedPayloadType, Is.EqualTo(98));
            Assert.That(new SdpCodecCapability().FormatParameters, Is.Empty);
            Assert.That(new SdpMediaCapability().Codecs, Is.Empty);
            Assert.That(new SdpEndpointCapabilities().Media, Is.Empty);
        });
    }

    [Test]
    public void SourceGraph_PreservesUnsignedIdentifiersOrderAndExtensionTokens()
    {
        SdpSourceAttribute source = new SdpSourceAttribute
        {
            SynchronizationSource = 4294967295u,
            Name = "cname",
            HasValue = true,
            Value = "stream@example.test"
        };
        SdpSourceGroupAttribute group = new SdpSourceGroupAttribute { Semantic = "FID" };
        group.SynchronizationSources.Add(4294967295u);
        group.SynchronizationSources.Add(2122260223u);
        SdpMediaStreamId stream = new SdpMediaStreamId
        {
            StreamId = "stream-1",
            HasTrackId = true,
            TrackId = "track-1",
            SourceLevel = true,
            SynchronizationSource = 4294967295u
        };
        SdpSourceFilterAttribute filter = new SdpSourceFilterAttribute
        {
            FilterMode = "incl",
            NetworkType = "IN",
            AddressType = "IP4",
            DestinationAddress = "233.252.0.1",
            AddressKind = SdpAddressKind.Ipv4Multicast
        };
        filter.SourceAddresses.Add("192.0.2.10");
        filter.SourceAddresses.Add("192.0.2.11");

        Assert.Multiple(() =>
        {
            Assert.That(source.SynchronizationSource, Is.EqualTo(uint.MaxValue));
            Assert.That(source.Value, Is.EqualTo("stream@example.test"));
            Assert.That(group.SynchronizationSources, Is.EqualTo(new uint[] { uint.MaxValue, 2122260223u }));
            Assert.That(stream.TrackId, Is.EqualTo("track-1"));
            Assert.That(stream.SourceLevel, Is.True);
            Assert.That(filter.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4Multicast));
            Assert.That(filter.SourceAddresses, Is.EqualTo(new[] { "192.0.2.10", "192.0.2.11" }));
            Assert.That(new SdpSourceGroupAttribute().SynchronizationSources, Is.Empty);
            Assert.That(new SdpSourceFilterAttribute().SourceAddresses, Is.Empty);
        });
    }

    [Test]
    public void IceGraph_PreservesTypedTokensExtensionsAndCandidateOrder()
    {
        SdpIceCandidate candidate = new SdpIceCandidate
        {
            Foundation = "1",
            Component = 1,
            TransportProtocol = SdpIceTransportProtocol.Tcp,
            TransportToken = "TCP",
            Priority = 2122260223u,
            Address = "192.0.2.1",
            AddressKind = SdpAddressKind.Ipv4,
            Port = 54400,
            CandidateType = SdpIceCandidateType.ServerReflexive,
            CandidateTypeToken = "srflx",
            HasRelatedAddress = true,
            RelatedAddress = "10.0.0.1",
            HasRelatedPort = true,
            RelatedPort = 8998,
            TcpType = SdpIceTcpType.Passive,
            TcpTypeToken = "passive"
        };
        candidate.Extensions.Add(new SdpIceCandidateExtension { Name = "network-cost", Value = "10" });
        SdpIceDescription ice = new SdpIceDescription
        {
            Credentials = new SdpIceCredentials { UsernameFragment = "ufrag", Password = "password-value" },
            IceLite = true,
            EndOfCandidates = true
        };
        ice.Options.Add("trickle");
        ice.Candidates.Add(candidate);
        ice.RemoteCandidates.Add(new SdpRemoteCandidate
        {
            Component = 1,
            Address = "192.0.2.20",
            AddressKind = SdpAddressKind.Ipv4,
            Port = 6000
        });

        Assert.Multiple(() =>
        {
            Assert.That(ice.Credentials?.UsernameFragment, Is.EqualTo("ufrag"));
            Assert.That(ice.Candidates[0].Priority, Is.EqualTo(2122260223u));
            Assert.That(ice.Candidates[0].TransportProtocol, Is.EqualTo(SdpIceTransportProtocol.Tcp));
            Assert.That(ice.Candidates[0].CandidateTypeToken, Is.EqualTo("srflx"));
            Assert.That(ice.Candidates[0].Extensions[0].Name, Is.EqualTo("network-cost"));
            Assert.That(ice.RemoteCandidates[0].Port, Is.EqualTo(6000));
            Assert.That(new SdpIceCandidate().Extensions, Is.Empty);
            Assert.That(new SdpIceDescription().Candidates, Is.Empty);
            Assert.That(new SdpIceDescription().RemoteCandidates, Is.Empty);
        });
    }

    [Test]
    public void DtlsGraph_PreservesFingerprintBytesRolesAndSensitiveParameters()
    {
        SdpFingerprintAttribute fingerprint = new SdpFingerprintAttribute
        {
            Algorithm = "sha-256",
            Fingerprint = "01:AB"
        };
        fingerprint.Digest.Add(0x01);
        fingerprint.Digest.Add(0xAB);
        SdpCryptoAttribute crypto = new SdpCryptoAttribute
        {
            Tag = 1,
            Suite = SdpCryptoSuites.AesCounterMode128HmacSha1_80
        };
        crypto.KeyParameters.Add("inline:sensitive-key");
        crypto.SessionParameters.Add("UNENCRYPTED_SRTCP");
        SdpDtlsDescription dtls = new SdpDtlsDescription
        {
            SetupRole = SdpDtlsSetupRole.ActPass,
            SetupRoleToken = "actpass",
            ConnectionRole = "new",
            TlsId = "tls-1"
        };
        dtls.Fingerprints.Add(fingerprint);
        dtls.CryptoAttributes.Add(crypto);

        Assert.Multiple(() =>
        {
            Assert.That(dtls.SetupRole, Is.EqualTo(SdpDtlsSetupRole.ActPass));
            Assert.That(dtls.Fingerprints[0].Digest, Is.EqualTo(new byte[] { 0x01, 0xAB }));
            Assert.That(dtls.CryptoAttributes[0].KeyParameters, Is.EqualTo(new[] { "inline:sensitive-key" }));
            Assert.That(dtls.CryptoAttributes[0].SessionParameters, Is.EqualTo(new[] { "UNENCRYPTED_SRTCP" }));
            Assert.That(new SdpFingerprintAttribute().Digest, Is.Empty);
            Assert.That(new SdpCryptoAttribute().KeyParameters, Is.Empty);
            Assert.That(new SdpDtlsDescription().Fingerprints, Is.Empty);
        });
    }

    [Test]
    public void HeaderExtensionAndGroupingGraph_PreservesMappingsAndTagOrder()
    {
        SdpExtMapAttribute extMap = new SdpExtMapAttribute
        {
            Id = 3,
            Direction = SdpDirection.SendOnly,
            Encrypted = true,
            Uri = SdpHeaderExtensionUris.MediaId,
            ExtensionAttributes = "extension-data"
        };
        SdpHeaderExtension capability = new SdpHeaderExtension
        {
            Uri = SdpHeaderExtensionUris.MediaId,
            Direction = SdpDirection.SendReceive,
            SupportsEncryption = true,
            Preference = 1
        };
        SdpHeaderExtensionMapping mapping = new SdpHeaderExtensionMapping
        {
            OfferedId = 3,
            NegotiatedId = 5,
            Uri = capability.Uri,
            Direction = SdpDirection.SendOnly,
            Encrypted = true
        };
        SdpGroupAttribute group = new SdpGroupAttribute
        {
            Semantic = SdpGroupSemantic.Bundle,
            SemanticToken = "BUNDLE"
        };
        group.IdentificationTags.Add("audio");
        group.IdentificationTags.Add("video");
        SdpBundleGroup bundle = new SdpBundleGroup { SourceGroup = group, TaggedMediaId = "audio" };
        bundle.MediaIds.Add("audio");
        bundle.MediaIds.Add("video");
        SdpBundleTransport transport = new SdpBundleTransport
        {
            TaggedMediaId = "audio",
            Connection = new SdpConnectionData { Address = "203.0.113.5" },
            Ice = new SdpIceDescription(),
            Dtls = new SdpDtlsDescription(),
            RtcpMux = true
        };
        transport.HeaderExtensions.Add(extMap);

        Assert.Multiple(() =>
        {
            Assert.That(extMap.Direction, Is.EqualTo(SdpDirection.SendOnly));
            Assert.That(mapping.OfferedId, Is.EqualTo(3));
            Assert.That(mapping.NegotiatedId, Is.EqualTo(5));
            Assert.That(group.IdentificationTags, Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(bundle.SourceGroup, Is.SameAs(group));
            Assert.That(bundle.MediaIds, Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(transport.Connection?.Address, Is.EqualTo("203.0.113.5"));
            Assert.That(transport.HeaderExtensions[0].Uri, Is.EqualTo(SdpHeaderExtensionUris.MediaId));
            Assert.That(new SdpGroupAttribute().IdentificationTags, Is.Empty);
            Assert.That(new SdpBundleGroup().MediaIds, Is.Empty);
            Assert.That(new SdpBundleTransport().HeaderExtensions, Is.Empty);
        });
    }

    [Test]
    public void RidSimulcastAndImageGraph_PreservesAlternativesRestrictionsAndOpaqueConstraints()
    {
        SdpRidAttribute rid = new SdpRidAttribute { Id = "high", Direction = SdpDirection.SendOnly };
        rid.PayloadTypes.Add(96);
        rid.PayloadTypes.Add(97);
        rid.Restrictions.Add(new SdpRidRestriction
        {
            Name = "max-width",
            HasValue = true,
            Value = "1920",
            RawText = "max-width=1920"
        });
        SdpSimulcastAlternative active = new SdpSimulcastAlternative { RidId = "high", Paused = false };
        SdpSimulcastAlternative paused = new SdpSimulcastAlternative { RidId = "low", Paused = true };
        SdpSimulcastStream stream = new SdpSimulcastStream();
        stream.Alternatives.Add(active);
        stream.Alternatives.Add(paused);
        SdpSimulcastAttribute simulcast = new SdpSimulcastAttribute();
        simulcast.SendStreams.Add(stream);
        SdpImageAttribute image = new SdpImageAttribute
        {
            PayloadType = "96",
            HasSendConstraints = true,
            SendConstraints = "[x=1920,y=1080]",
            HasReceiveConstraints = true,
            ReceiveConstraints = "*"
        };

        Assert.Multiple(() =>
        {
            Assert.That(rid.PayloadTypes, Is.EqualTo(new[] { 96, 97 }));
            Assert.That(rid.Restrictions[0].Name, Is.EqualTo("max-width"));
            Assert.That(stream.Alternatives.Select(x => x.RidId), Is.EqualTo(new[] { "high", "low" }));
            Assert.That(stream.Alternatives[1].Paused, Is.True);
            Assert.That(simulcast.SendStreams[0], Is.SameAs(stream));
            Assert.That(image.SendConstraints, Is.EqualTo("[x=1920,y=1080]"));
            Assert.That(image.ReceiveConstraints, Is.EqualTo("*"));
            Assert.That(new SdpRidAttribute().Restrictions, Is.Empty);
            Assert.That(new SdpSimulcastStream().Alternatives, Is.Empty);
            Assert.That(new SdpSimulcastAttribute().ReceiveStreams, Is.Empty);
        });
    }

    [Test]
    public void ClockDataChannelAndBfcpGraph_PreservesProtocolSpecificValues()
    {
        SdpClockReferenceAttribute reference = new SdpClockReferenceAttribute
        {
            SourceKind = SdpClockSourceKind.Ptp,
            SourceToken = "ptp",
            Value = "IEEE1588-2008:00-11-22-FF-FE-33-44-55:0"
        };
        SdpMediaClockAttribute mediaClock = new SdpMediaClockAttribute
        {
            Id = "clk1",
            ClockName = "direct",
            ClockValue = "0",
            HasRate = true,
            RateNumerator = 1000,
            RateDenominator = 1001
        };
        SdpSctpDescription sctp = new SdpSctpDescription
        {
            Port = 5000,
            HasMaximumMessageSize = true,
            MaximumMessageSize = 262144,
            Format = "webrtc-datachannel",
            LegacySctpMap = false
        };
        SdpDataChannelAttribute channel = new SdpDataChannelAttribute { ChannelId = 4 };
        channel.Parameters.Add(new SdpDataChannelParameter
        {
            Name = "subprotocol",
            HasValue = true,
            Value = "chat",
            RawText = "subprotocol=chat"
        });
        channel.SubprotocolAttributes.Add(new SdpAttribute { Name = "dcsa", Value = "4 label:messages" });
        SdpBfcpDescription bfcp = new SdpBfcpDescription
        {
            FloorControl = "c-s",
            ConferenceId = "100",
            UserId = "200",
            SetupRole = SdpDtlsSetupRole.Active,
            SetupRoleToken = "active",
            ConnectionRole = "new"
        };
        bfcp.FloorIds.Add("1 mstrm:audio");
        bfcp.Versions.Add("2");

        Assert.Multiple(() =>
        {
            Assert.That(reference.SourceKind, Is.EqualTo(SdpClockSourceKind.Ptp));
            Assert.That(reference.Value, Does.StartWith("IEEE1588-2008"));
            Assert.That(mediaClock.RateNumerator, Is.EqualTo(1000));
            Assert.That(mediaClock.RateDenominator, Is.EqualTo(1001));
            Assert.That(sctp.MaximumMessageSize, Is.EqualTo(262144));
            Assert.That(channel.Parameters[0].Value, Is.EqualTo("chat"));
            Assert.That(channel.SubprotocolAttributes[0].Name, Is.EqualTo("dcsa"));
            Assert.That(bfcp.FloorIds, Is.EqualTo(new[] { "1 mstrm:audio" }));
            Assert.That(bfcp.Versions, Is.EqualTo(new[] { "2" }));
            Assert.That(new SdpDataChannelAttribute().Parameters, Is.Empty);
            Assert.That(new SdpBfcpDescription().FloorIds, Is.Empty);
        });
    }

    [Test]
    public void EffectiveMedia_InitializesEveryProjectionCollectionIndependently()
    {
        SdpEffectiveMedia first = new SdpEffectiveMedia();
        SdpEffectiveMedia second = new SdpEffectiveMedia();
        first.Bandwidths.Add(new SdpBandwidth());
        first.Languages.Add("en");
        first.SessionDescriptionLanguages.Add("en-US");
        first.Codecs.Add(new SdpCodec());
        first.RtcpExtendedReports.Add(new SdpRtcpExtendedReportAttribute());
        first.SourceAttributes.Add(new SdpSourceAttribute());
        first.SourceGroups.Add(new SdpSourceGroupAttribute());
        first.MediaStreamIds.Add(new SdpMediaStreamId());
        first.SourceFilters.Add(new SdpSourceFilterAttribute());
        first.HeaderExtensions.Add(new SdpExtMapAttribute());
        first.Rids.Add(new SdpRidAttribute());
        first.ImageAttributes.Add(new SdpImageAttribute());
        first.ClockReferences.Add(new SdpClockReferenceAttribute());
        first.MediaClocks.Add(new SdpMediaClockAttribute());
        first.DataChannels.Add(new SdpDataChannelAttribute());

        Assert.Multiple(() =>
        {
            Assert.That(second.Bandwidths, Is.Empty);
            Assert.That(second.Languages, Is.Empty);
            Assert.That(second.SessionDescriptionLanguages, Is.Empty);
            Assert.That(second.Codecs, Is.Empty);
            Assert.That(second.RtcpExtendedReports, Is.Empty);
            Assert.That(second.SourceAttributes, Is.Empty);
            Assert.That(second.SourceGroups, Is.Empty);
            Assert.That(second.MediaStreamIds, Is.Empty);
            Assert.That(second.SourceFilters, Is.Empty);
            Assert.That(second.HeaderExtensions, Is.Empty);
            Assert.That(second.Rids, Is.Empty);
            Assert.That(second.ImageAttributes, Is.Empty);
            Assert.That(second.ClockReferences, Is.Empty);
            Assert.That(second.MediaClocks, Is.Empty);
            Assert.That(second.DataChannels, Is.Empty);
            Assert.That(first.Codecs, Is.Not.SameAs(second.Codecs));
            Assert.That(first.HeaderExtensions, Is.Not.SameAs(second.HeaderExtensions));
            Assert.That(first.DataChannels, Is.Not.SameAs(second.DataChannels));
        });
    }

    [Test]
    public void NegotiationGraph_RepresentsAcceptedAndRejectedMediaExplicitly()
    {
        SdpEndpointCapabilities capabilities = new SdpEndpointCapabilities { SupportsBundle = true };
        SdpNegotiationOptions options = new SdpNegotiationOptions
        {
            Role = SdpNegotiationRole.Answerer,
            CodecMatchMode = SdpCodecMatchMode.Compatible,
            LocalCapabilities = capabilities,
            RequireIce = true,
            RequireDtls = true,
            RequireRtcpMux = true,
            RequireBundle = true
        };
        SdpMediaNegotiation accepted = new SdpMediaNegotiation
        {
            MediaIndex = 0,
            MediaId = "audio",
            Accepted = true,
            Direction = SdpDirection.SendReceive,
            EffectiveMedia = new SdpEffectiveMedia { MediaIndex = 0, MediaId = "audio" }
        };
        accepted.Codecs.Add(new SdpCodec { PayloadType = 111, EncodingName = "opus" });
        accepted.PayloadMappings.Add(new SdpPayloadMapping { OfferedPayloadType = 111, NegotiatedPayloadType = 111 });
        accepted.HeaderExtensionMappings.Add(new SdpHeaderExtensionMapping { OfferedId = 1, NegotiatedId = 1 });
        SdpRejection rejection = new SdpRejection
        {
            MediaIndex = 1,
            MediaId = "video",
            Reason = SdpRejectionReason.NoCompatibleCodec,
            ReasonToken = "no-compatible-codec",
            Detail = "No supported video codec was offered."
        };
        SdpMediaNegotiation rejected = new SdpMediaNegotiation
        {
            MediaIndex = 1,
            MediaId = "video",
            Accepted = false,
            Rejection = rejection
        };
        SdpNegotiationResult result = new SdpNegotiationResult
        {
            Success = true,
            Description = new SdpSessionDescription(),
            AppliedOptions = options
        };
        result.Media.Add(accepted);
        result.Media.Add(rejected);
        result.Rejections.Add(rejection);
        result.BundleGroups.Add(new SdpBundleGroup { TaggedMediaId = "audio" });
        result.Diagnostics.Add(new SdpDiagnostic { Severity = SdpValidationSeverity.Warning });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.AppliedOptions.Role, Is.EqualTo(SdpNegotiationRole.Answerer));
            Assert.That(result.AppliedOptions.LocalCapabilities, Is.SameAs(capabilities));
            Assert.That(result.Media, Has.Count.EqualTo(2));
            Assert.That(result.Media[0].Codecs[0].EncodingName, Is.EqualTo("opus"));
            Assert.That(result.Media[1].Rejection?.Reason, Is.EqualTo(SdpRejectionReason.NoCompatibleCodec));
            Assert.That(result.Rejections, Is.EqualTo(new[] { rejection }));
            Assert.That(result.BundleGroups[0].TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(new SdpNegotiationResult().Media, Is.Empty);
            Assert.That(new SdpMediaNegotiation().PayloadMappings, Is.Empty);
            Assert.That(new SdpNegotiationOptions().CodecMatchMode, Is.EqualTo(SdpCodecMatchMode.Strict));
        });
    }
}
