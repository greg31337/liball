namespace sdp.Tests.Api;

public class SdpNegotiationModelTests
{
    [Test]
    public void CapabilityAndResultModels_HaveExplicitNeutralDefaults()
    {
        SdpEndpointCapabilities endpoint = new SdpEndpointCapabilities();
        SdpMediaCapability media = new SdpMediaCapability();
        SdpCodecCapability codec = new SdpCodecCapability();
        SdpHeaderExtension extension = new SdpHeaderExtension();
        SdpMediaNegotiation outcome = new SdpMediaNegotiation();
        SdpNegotiationResult result = new SdpNegotiationResult();
        SdpNegotiationResult secondResult = new SdpNegotiationResult();
        SdpRejection rejection = new SdpRejection();

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Connection, Is.Null);
            Assert.That(endpoint.Ice, Is.Null);
            Assert.That(endpoint.Dtls, Is.Null);
            Assert.That(endpoint.SupportsIce, Is.False);
            Assert.That(media.MediaKind, Is.EqualTo(SdpMediaKind.Unknown));
            Assert.That(media.MediaId, Is.Empty);
            Assert.That(media.Port, Is.Zero);
            Assert.That(media.PortCount, Is.EqualTo(1));
            Assert.That(media.Direction, Is.EqualTo(SdpDirection.SendReceive));
            Assert.That(media.Connection, Is.Null);
            Assert.That(media.Ice, Is.Null);
            Assert.That(media.Dtls, Is.Null);
            Assert.That(media.Simulcast, Is.Null);
            Assert.That(media.Sctp, Is.Null);
            Assert.That(codec.HasPreferredPayloadType, Is.False);
            Assert.That(codec.PreferredPayloadType, Is.Zero);
            Assert.That(codec.ChannelCount, Is.EqualTo(1));
            Assert.That(extension.HasPreferredId, Is.False);
            Assert.That(extension.PreferredId, Is.Zero);
            Assert.That(extension.Required, Is.False);
            Assert.That(outcome.MediaIndex, Is.EqualTo(-1));
            Assert.That(outcome.TransportMediaIndex, Is.EqualTo(-1));
            Assert.That(outcome.BundleGroupIndex, Is.EqualTo(-1));
            Assert.That(outcome.MediaKind, Is.EqualTo(SdpMediaKind.Unknown));
            Assert.That(outcome.OfferedDirection, Is.EqualTo(SdpDirection.Unknown));
            Assert.That(outcome.Rejection, Is.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Description, Is.Null);
            Assert.That(result.Media, Is.Empty);
            Assert.That(result.Rejections, Is.Empty);
            Assert.That(result.BundleGroups, Is.Empty);
            Assert.That(result.BundleTransports, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.DiagnosticLimitReached, Is.False);
            Assert.That(result.AppliedOptions, Is.Not.SameAs(secondResult.AppliedOptions));
            Assert.That(result.Media, Is.Not.SameAs(secondResult.Media));
            Assert.That(result.Rejections, Is.Not.SameAs(secondResult.Rejections));
            Assert.That(result.BundleGroups, Is.Not.SameAs(secondResult.BundleGroups));
            Assert.That(result.BundleTransports, Is.Not.SameAs(secondResult.BundleTransports));
            Assert.That(result.Diagnostics, Is.Not.SameAs(secondResult.Diagnostics));
            Assert.That(rejection.Reason, Is.EqualTo(SdpRejectionReason.Unknown));
            Assert.That(rejection.Path.Scope, Is.EqualTo(SdpScope.Unknown));
        });
    }

    [Test]
    public void NegotiationOptions_ExposeConservativeExplicitPolicyDefaults()
    {
        SdpNegotiationOptions options = new SdpNegotiationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Role, Is.EqualTo(SdpNegotiationRole.Unknown));
            Assert.That(options.CodecMatchMode, Is.EqualTo(SdpCodecMatchMode.Strict));
            Assert.That(options.LocalCapabilities, Is.Null);
            Assert.That(options.AllowMediaRejection, Is.True);
            Assert.That(options.PreservePayloadTypes, Is.True);
            Assert.That(options.PreferLocalCodecOrder, Is.True);
            Assert.That(options.AllowHeaderExtensionIdRemapping, Is.True);
            Assert.That(options.AllowBundleSubset, Is.True);
            Assert.That(options.AllowIceRestart, Is.True);
            Assert.That(options.PreserveMediaOrder, Is.True);
            Assert.That(options.PreserveMediaIds, Is.True);
            Assert.That(options.PreserveRejectedMedia, Is.True);
            Assert.That(options.IncrementOriginSessionVersion, Is.True);
            Assert.That(options.RequireIce, Is.False);
            Assert.That(options.RequireDtls, Is.False);
            Assert.That(options.RequireRtcpMux, Is.False);
            Assert.That(options.RequireBundle, Is.False);
            Assert.That(options.MaximumDiagnosticCount, Is.EqualTo(SdpLimits.DefaultMaximumDiagnosticCount));
        });
    }

    [Test]
    public void CapabilityGraph_RepresentsTransportCodecExtensionStreamAndDataChoices()
    {
        SdpCodecCapability codec = new SdpCodecCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            EncodingName = "opus",
            ClockRate = 48000,
            HasPreferredPayloadType = true,
            PreferredPayloadType = 111,
            HasChannelCount = true,
            ChannelCount = 2,
            Preference = 1
        };
        codec.FormatParameters.Add(new SdpFormatParameter { Name = "useinbandfec", HasValue = true, Value = "1" });
        codec.RtcpFeedbackTypes.Add("transport-cc");
        codec.RtcpFeedback.Add(new SdpRtcpFeedbackAttribute { PayloadType = "111", Type = "transport-cc" });
        SdpHeaderExtension extension = new SdpHeaderExtension
        {
            Uri = SdpHeaderExtensionUris.MediaId,
            HasPreferredId = true,
            PreferredId = 3,
            Direction = SdpDirection.SendReceive,
            SupportsEncryption = true,
            ExtensionAttributes = "preferred",
            Required = true,
            Preference = 2
        };
        SdpMediaCapability media = new SdpMediaCapability
        {
            MediaKind = SdpMediaKind.Audio,
            MediaType = "audio",
            MediaId = "audio-main",
            Port = 5004,
            HasPortCount = true,
            PortCount = 2,
            Direction = SdpDirection.SendReceive,
            Connection = new SdpConnectionData { Address = "198.51.100.20" },
            Ice = new SdpIceDescription { Credentials = new SdpIceCredentials { UsernameFragment = "mediaUfrag", Password = "mediaPasswordValue123" } },
            Dtls = new SdpDtlsDescription { SetupRole = SdpDtlsSetupRole.ActPass },
            Simulcast = new SdpSimulcastAttribute(),
            Sctp = new SdpSctpDescription { Port = 5000 },
            SupportsRtcpMux = true,
            SupportsRtcpMuxOnly = true,
            SupportsReducedSizeRtcp = true,
            BundleOnly = true,
            Preference = 3
        };
        media.Protocols.Add(SdpTransportProtocols.UdpTlsRtpSavpf);
        media.Codecs.Add(codec);
        media.HeaderExtensions.Add(extension);
        media.MediaStreamIds.Add(new SdpMediaStreamId { StreamId = "stream", TrackId = "track" });
        media.SourceAttributes.Add(new SdpSourceAttribute { SynchronizationSource = 10u, Name = "cname", HasValue = true, Value = "audio" });
        media.SourceGroups.Add(new SdpSourceGroupAttribute { Semantic = "FID" });
        media.Rids.Add(new SdpRidAttribute { Id = "high", Direction = SdpDirection.SendOnly });
        media.ImageAttributes.Add(new SdpImageAttribute { PayloadType = "111" });
        media.DataChannels.Add(new SdpDataChannelAttribute { ChannelId = 1 });
        SdpEndpointCapabilities endpoint = new SdpEndpointCapabilities
        {
            Connection = new SdpConnectionData { Address = "203.0.113.10" },
            Ice = new SdpIceDescription { IceLite = true },
            Dtls = new SdpDtlsDescription { SetupRole = SdpDtlsSetupRole.Passive },
            SupportsIce = true,
            SupportsDtls = true,
            SupportsBundle = true,
            SupportsDataChannels = true
        };
        endpoint.Media.Add(media);
        endpoint.HeaderExtensions.Add(extension);
        endpoint.Fingerprints.Add(new SdpFingerprintAttribute { Algorithm = "sha-256" });

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Connection!.Address, Is.EqualTo("203.0.113.10"));
            Assert.That(endpoint.Ice!.IceLite, Is.True);
            Assert.That(endpoint.Dtls!.SetupRole, Is.EqualTo(SdpDtlsSetupRole.Passive));
            Assert.That(endpoint.Media[0].MediaId, Is.EqualTo("audio-main"));
            Assert.That(endpoint.Media[0].PortCount, Is.EqualTo(2));
            Assert.That(endpoint.Media[0].Protocols, Is.EqualTo(new[] { SdpTransportProtocols.UdpTlsRtpSavpf }));
            Assert.That(endpoint.Media[0].Codecs[0].PreferredPayloadType, Is.EqualTo(111));
            Assert.That(endpoint.Media[0].Codecs[0].RtcpFeedback[0].Type, Is.EqualTo("transport-cc"));
            Assert.That(endpoint.Media[0].HeaderExtensions[0].PreferredId, Is.EqualTo(3));
            Assert.That(endpoint.Media[0].HeaderExtensions[0].Required, Is.True);
            Assert.That(endpoint.Media[0].MediaStreamIds[0].TrackId, Is.EqualTo("track"));
            Assert.That(endpoint.Media[0].SourceAttributes[0].SynchronizationSource, Is.EqualTo(10u));
            Assert.That(endpoint.Media[0].Rids[0].Id, Is.EqualTo("high"));
            Assert.That(endpoint.Media[0].Sctp!.Port, Is.EqualTo(5000));
            Assert.That(endpoint.Media[0].BundleOnly, Is.True);
        });
    }

    [Test]
    public void NegotiationResult_RecordsMediaIdentityTransportMappingsBundlesAndRejectionPath()
    {
        SdpRejection rejection = new SdpRejection
        {
            MediaIndex = 1,
            MediaId = "video",
            Reason = SdpRejectionReason.NoCompatibleCodec,
            ReasonToken = "no-compatible-codec",
            Path = new SdpSourcePath { Scope = SdpScope.Media, MediaIndex = 1, MediaId = "video", FieldName = 'm' },
            Detail = "No compatible video codec was found."
        };
        SdpMediaNegotiation media = new SdpMediaNegotiation
        {
            MediaIndex = 1,
            MediaId = "video",
            MediaKind = SdpMediaKind.Video,
            MediaType = "video",
            Protocol = SdpTransportProtocols.UdpTlsRtpSavpf,
            TransportMediaIndex = 0,
            BundleGroupIndex = 0,
            Accepted = false,
            OfferedDirection = SdpDirection.SendOnly,
            Direction = SdpDirection.ReceiveOnly,
            Sctp = new SdpSctpDescription { Port = 5000 },
            Rejection = rejection
        };
        media.PayloadMappings.Add(new SdpPayloadMapping { MediaIndex = 1, OfferedPayloadType = 120, NegotiatedPayloadType = 98 });
        media.HeaderExtensionMappings.Add(new SdpHeaderExtensionMapping { OfferedId = 3, NegotiatedId = 5 });
        SdpNegotiationResult result = new SdpNegotiationResult();
        result.Media.Add(media);
        result.Rejections.Add(rejection);
        result.BundleGroups.Add(new SdpBundleGroup { TaggedMediaId = "audio" });
        result.BundleTransports.Add(new SdpBundleTransport { TaggedMediaId = "audio" });
        SdpRejection firstDefaultRejection = new SdpRejection();
        SdpRejection secondDefaultRejection = new SdpRejection();

        Assert.Multiple(() =>
        {
            Assert.That(result.Media[0].MediaKind, Is.EqualTo(SdpMediaKind.Video));
            Assert.That(result.Media[0].Protocol, Is.EqualTo(SdpTransportProtocols.UdpTlsRtpSavpf));
            Assert.That(result.Media[0].TransportMediaIndex, Is.Zero);
            Assert.That(result.Media[0].BundleGroupIndex, Is.Zero);
            Assert.That(result.Media[0].OfferedDirection, Is.EqualTo(SdpDirection.SendOnly));
            Assert.That(result.Media[0].PayloadMappings[0].NegotiatedPayloadType, Is.EqualTo(98));
            Assert.That(result.Media[0].HeaderExtensionMappings[0].NegotiatedId, Is.EqualTo(5));
            Assert.That(result.Rejections[0].Path.FieldName, Is.EqualTo('m'));
            Assert.That(result.BundleTransports[0].TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(firstDefaultRejection.Path, Is.Not.SameAs(secondDefaultRejection.Path));
        });
    }
}
