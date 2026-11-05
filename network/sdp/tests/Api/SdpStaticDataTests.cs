using System.Linq;

namespace sdp.Tests.Api;

public class SdpStaticDataTests
{
    [Test]
    public void FieldNames_UseRfc8866Discriminators()
    {
        char[] fields =
        {
            SdpFieldNames.Version,
            SdpFieldNames.Origin,
            SdpFieldNames.SessionName,
            SdpFieldNames.Information,
            SdpFieldNames.Uri,
            SdpFieldNames.Email,
            SdpFieldNames.Phone,
            SdpFieldNames.Connection,
            SdpFieldNames.Bandwidth,
            SdpFieldNames.Timing,
            SdpFieldNames.Repeat,
            SdpFieldNames.TimeZoneAdjustments,
            SdpFieldNames.EncryptionKey,
            SdpFieldNames.Attribute,
            SdpFieldNames.Media
        };

        Assert.That(new string(fields), Is.EqualTo("vosiuepcbtrzkam"));
        Assert.That(fields.Distinct().Count(), Is.EqualTo(fields.Length));
    }

    [Test]
    public void AttributeNames_UseCanonicalWireSpelling()
    {
        string[] names =
        {
            SdpAttributeNames.SendReceive,
            SdpAttributeNames.SendOnly,
            SdpAttributeNames.ReceiveOnly,
            SdpAttributeNames.Inactive,
            SdpAttributeNames.Orientation,
            SdpAttributeNames.ConferenceType,
            SdpAttributeNames.CharacterSet,
            SdpAttributeNames.SessionDescriptionLanguage,
            SdpAttributeNames.Language,
            SdpAttributeNames.FrameRate,
            SdpAttributeNames.Quality,
            SdpAttributeNames.Tool,
            SdpAttributeNames.Category,
            SdpAttributeNames.Keywords,
            SdpAttributeNames.Control,
            SdpAttributeNames.Content,
            SdpAttributeNames.Label,
            SdpAttributeNames.PacketTime,
            SdpAttributeNames.MaximumPacketTime,
            SdpAttributeNames.RtpMap,
            SdpAttributeNames.FormatParameters,
            SdpAttributeNames.Rtcp,
            SdpAttributeNames.RtcpMux,
            SdpAttributeNames.RtcpMuxOnly,
            SdpAttributeNames.RtcpReducedSize,
            SdpAttributeNames.RtcpFeedback,
            SdpAttributeNames.RtcpExtendedReports,
            SdpAttributeNames.SynchronizationSource,
            SdpAttributeNames.SynchronizationSourceGroup,
            SdpAttributeNames.MediaStreamId,
            SdpAttributeNames.MediaStreamIdSemantic,
            SdpAttributeNames.SourceFilter,
            SdpAttributeNames.Group,
            SdpAttributeNames.MediaId,
            SdpAttributeNames.BundleOnly,
            SdpAttributeNames.IceUsernameFragment,
            SdpAttributeNames.IcePassword,
            SdpAttributeNames.IceOptions,
            SdpAttributeNames.IceLite,
            SdpAttributeNames.IceMismatch,
            SdpAttributeNames.Candidate,
            SdpAttributeNames.RemoteCandidates,
            SdpAttributeNames.EndOfCandidates,
            SdpAttributeNames.Fingerprint,
            SdpAttributeNames.Setup,
            SdpAttributeNames.Connection,
            SdpAttributeNames.TlsId,
            SdpAttributeNames.Crypto,
            SdpAttributeNames.ExtMap,
            SdpAttributeNames.ExtMapAllowMixed,
            SdpAttributeNames.Rid,
            SdpAttributeNames.Simulcast,
            SdpAttributeNames.ImageAttribute,
            SdpAttributeNames.TimestampReferenceClock,
            SdpAttributeNames.MediaClock,
            SdpAttributeNames.SctpPort,
            SdpAttributeNames.MaximumMessageSize,
            SdpAttributeNames.SctpMap,
            SdpAttributeNames.DataChannelMap,
            SdpAttributeNames.DataChannelSubprotocol,
            SdpAttributeNames.FloorControl,
            SdpAttributeNames.ConferenceId,
            SdpAttributeNames.UserId,
            SdpAttributeNames.FloorId,
            SdpAttributeNames.BfcpVersion
        };

        Assert.That(names, Does.Contain("rtpmap"));
        Assert.That(names, Does.Contain("ice-ufrag"));
        Assert.That(names, Does.Contain("fingerprint"));
        Assert.That(names, Does.Contain("sctp-port"));
        Assert.That(names, Does.Not.Contain(string.Empty));
        Assert.That(names.Distinct().Count(), Is.EqualTo(names.Length));
    }

    [Test]
    public void RegisteredCoreTokens_UseRegistrySpelling()
    {
        string[] mediaTypes =
        {
            SdpMediaTypes.Audio,
            SdpMediaTypes.Video,
            SdpMediaTypes.Text,
            SdpMediaTypes.Application,
            SdpMediaTypes.Message,
            SdpMediaTypes.Image,
            SdpMediaTypes.Haptics
        };
        string[] networkTypes =
        {
            SdpNetworkTypes.Internet,
            SdpNetworkTypes.Telephony,
            SdpNetworkTypes.Atm,
            SdpNetworkTypes.PublicSwitchedTelephoneNetwork
        };
        string[] addressTypes =
        {
            SdpAddressTypes.Ip4,
            SdpAddressTypes.Ip6,
            SdpAddressTypes.NetworkServiceAccessPoint,
            SdpAddressTypes.GatewayIdentifier,
            SdpAddressTypes.E164
        };
        string[] bandwidthTypes =
        {
            SdpBandwidthTypes.ConferenceTotal,
            SdpBandwidthTypes.ApplicationSpecific,
            SdpBandwidthTypes.RtcpSender,
            SdpBandwidthTypes.RtcpReceiver,
            SdpBandwidthTypes.TransportIndependentApplicationSpecific
        };

        Assert.Multiple(() =>
        {
            Assert.That(mediaTypes, Is.EqualTo(new[] { "audio", "video", "text", "application", "message", "image", "haptics" }));
            Assert.That(networkTypes, Is.EqualTo(new[] { "IN", "TN", "ATM", "PSTN" }));
            Assert.That(addressTypes, Is.EqualTo(new[] { "IP4", "IP6", "NSAP", "GWID", "E164" }));
            Assert.That(bandwidthTypes, Is.EqualTo(new[] { "CT", "AS", "RS", "RR", "TIAS" }));
        });
    }

    [Test]
    public void TransportProtocols_UseRegisteredCompositeTokens()
    {
        string[] protocols =
        {
            SdpTransportProtocols.Udp,
            SdpTransportProtocols.Tcp,
            SdpTransportProtocols.RtpAvp,
            SdpTransportProtocols.RtpAvpf,
            SdpTransportProtocols.RtpSavp,
            SdpTransportProtocols.RtpSavpf,
            SdpTransportProtocols.TcpRtpAvp,
            SdpTransportProtocols.TcpRtpAvpf,
            SdpTransportProtocols.UdpTlsRtpSavp,
            SdpTransportProtocols.UdpTlsRtpSavpf,
            SdpTransportProtocols.TcpDtlsRtpSavp,
            SdpTransportProtocols.TcpDtlsRtpSavpf,
            SdpTransportProtocols.UdpDtlsSctp,
            SdpTransportProtocols.TcpDtlsSctp,
            SdpTransportProtocols.TcpBfcp,
            SdpTransportProtocols.TcpDtlsBfcp,
            SdpTransportProtocols.TcpTlsBfcp,
            SdpTransportProtocols.UdpBfcp,
            SdpTransportProtocols.UdpTlsBfcp
        };

        Assert.That(protocols, Does.Contain("UDP/TLS/RTP/SAVPF"));
        Assert.That(protocols, Does.Contain("UDP/DTLS/SCTP"));
        Assert.That(protocols, Does.Contain("TCP/DTLS/BFCP"));
        Assert.That(protocols, Does.Contain("TCP/TLS/BFCP"));
        Assert.That(protocols, Does.Contain("UDP/BFCP"));
        Assert.That(protocols, Does.Contain("UDP/TLS/BFCP"));
        Assert.That(protocols.Distinct().Count(), Is.EqualTo(protocols.Length));
    }

    [Test]
    public void GroupSemantics_IncludeRegisteredGroupingProfiles()
    {
        string[] semantics =
        {
            SdpGroupSemantics.LipSynchronization,
            SdpGroupSemantics.FlowIdentification,
            SdpGroupSemantics.SingleReservationFlow,
            SdpGroupSemantics.AlternativeNetworkAddressTypes,
            SdpGroupSemantics.ForwardErrorCorrection,
            SdpGroupSemantics.ForwardErrorCorrectionFramework,
            SdpGroupSemantics.CompositeSession,
            SdpGroupSemantics.DecodingDependency,
            SdpGroupSemantics.Duplication,
            SdpGroupSemantics.Splice,
            SdpGroupSemantics.Bundle,
            SdpGroupSemantics.Clue,
            SdpGroupSemantics.V3c
        };

        Assert.That(semantics, Does.Contain("BUNDLE"));
        Assert.That(semantics, Does.Contain("ANAT"));
        Assert.That(semantics.Distinct().Count(), Is.EqualTo(semantics.Length));
    }

    [Test]
    public void SecurityTokens_UseStandardAlgorithmNames()
    {
        string[] fingerprintAlgorithms =
        {
            SdpFingerprintAlgorithms.Sha1,
            SdpFingerprintAlgorithms.Sha224,
            SdpFingerprintAlgorithms.Sha256,
            SdpFingerprintAlgorithms.Sha384,
            SdpFingerprintAlgorithms.Sha512
        };

        Assert.Multiple(() =>
        {
            Assert.That(fingerprintAlgorithms, Is.EqualTo(new[] { "sha-1", "sha-224", "sha-256", "sha-384", "sha-512" }));
            Assert.That(SdpCryptoSuites.AesCounterMode128HmacSha1_80, Is.EqualTo("AES_CM_128_HMAC_SHA1_80"));
            Assert.That(SdpCryptoSuites.AesCounterMode128HmacSha1_32, Is.EqualTo("AES_CM_128_HMAC_SHA1_32"));
            Assert.That(SdpCryptoSuites.AesF8Mode128HmacSha1_80, Is.EqualTo("F8_128_HMAC_SHA1_80"));
        });
    }

    [Test]
    public void StaticPayloadTypes_MatchRfc3551Assignments()
    {
        int[] payloadTypes =
        {
            SdpStaticPayloadTypes.Pcmu,
            SdpStaticPayloadTypes.Gsm,
            SdpStaticPayloadTypes.G723,
            SdpStaticPayloadTypes.Dvi4_8000,
            SdpStaticPayloadTypes.Dvi4_16000,
            SdpStaticPayloadTypes.Lpc,
            SdpStaticPayloadTypes.Pcma,
            SdpStaticPayloadTypes.G722,
            SdpStaticPayloadTypes.Linear16Stereo,
            SdpStaticPayloadTypes.Linear16Mono,
            SdpStaticPayloadTypes.Qcelp,
            SdpStaticPayloadTypes.ComfortNoise,
            SdpStaticPayloadTypes.MpegAudio,
            SdpStaticPayloadTypes.G728,
            SdpStaticPayloadTypes.Dvi4_11025,
            SdpStaticPayloadTypes.Dvi4_22050,
            SdpStaticPayloadTypes.G729,
            SdpStaticPayloadTypes.CellB,
            SdpStaticPayloadTypes.Jpeg,
            SdpStaticPayloadTypes.Nv,
            SdpStaticPayloadTypes.H261,
            SdpStaticPayloadTypes.MpegVideo,
            SdpStaticPayloadTypes.Mpeg2TransportStream,
            SdpStaticPayloadTypes.H263,
            SdpStaticPayloadTypes.DynamicMinimum,
            SdpStaticPayloadTypes.DynamicMaximum
        };

        Assert.That(payloadTypes, Is.EqualTo(new[] { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 25, 26, 28, 31, 32, 33, 34, 96, 127 }));
    }

    [Test]
    public void CodecNames_PreserveRegisteredCase()
    {
        string[] names =
        {
            SdpCodecNames.Pcmu,
            SdpCodecNames.Gsm,
            SdpCodecNames.G723,
            SdpCodecNames.Dvi4,
            SdpCodecNames.Lpc,
            SdpCodecNames.Pcma,
            SdpCodecNames.G722,
            SdpCodecNames.Linear16,
            SdpCodecNames.Qcelp,
            SdpCodecNames.ComfortNoise,
            SdpCodecNames.MpegAudio,
            SdpCodecNames.G728,
            SdpCodecNames.G729,
            SdpCodecNames.CellB,
            SdpCodecNames.Jpeg,
            SdpCodecNames.Nv,
            SdpCodecNames.H261,
            SdpCodecNames.MpegVideo,
            SdpCodecNames.Mpeg2TransportStream,
            SdpCodecNames.H263,
            SdpCodecNames.Opus,
            SdpCodecNames.Vp8,
            SdpCodecNames.Vp9,
            SdpCodecNames.Av1,
            SdpCodecNames.H264,
            SdpCodecNames.H265,
            SdpCodecNames.Hevc,
            SdpCodecNames.Retransmission,
            SdpCodecNames.RedundantAudioData,
            SdpCodecNames.UnequalLossProtectionFec,
            SdpCodecNames.FlexibleFec,
            SdpCodecNames.TelephoneEvent
        };

        Assert.That(names, Is.EqualTo(new[] { "PCMU", "GSM", "G723", "DVI4", "LPC", "PCMA", "G722", "L16", "QCELP", "CN", "MPA", "G728", "G729", "CelB", "JPEG", "nv", "H261", "MPV", "MP2T", "H263", "opus", "VP8", "VP9", "AV1", "H264", "H265", "HEVC", "rtx", "red", "ulpfec", "flexfec", "telephone-event" }));
    }

    [Test]
    public void RtcpFeedbackTokens_RepresentTypesAndParameters()
    {
        string[] tokens =
        {
            SdpRtcpFeedbackTypes.Acknowledgement,
            SdpRtcpFeedbackTypes.NegativeAcknowledgement,
            SdpRtcpFeedbackTypes.MinimumInterval,
            SdpRtcpFeedbackTypes.CodecControlMessage,
            SdpRtcpFeedbackTypes.Application,
            SdpRtcpFeedbackTypes.PictureLossIndication,
            SdpRtcpFeedbackTypes.SliceLossIndication,
            SdpRtcpFeedbackTypes.ReferencePictureSelectionIndication,
            SdpRtcpFeedbackTypes.FullIntraRequest,
            SdpRtcpFeedbackTypes.TemporaryMaximumMediaBitRate,
            SdpRtcpFeedbackTypes.TemporaryMaximumMediaBitRateNotification,
            SdpRtcpFeedbackTypes.TemporalSpatialTradeOffRequest,
            SdpRtcpFeedbackTypes.TemporalSpatialTradeOffNotification,
            SdpRtcpFeedbackTypes.VideoBackChannelMessage,
            SdpRtcpFeedbackTypes.TransportWideCongestionControl,
            SdpRtcpFeedbackTypes.ReceiverEstimatedMaximumBitrate
        };

        Assert.That(tokens, Is.EqualTo(new[] { "ack", "nack", "trr-int", "ccm", "app", "pli", "sli", "rpsi", "fir", "tmmbr", "tmmbn", "tstr", "tstn", "vbcm", "transport-cc", "goog-remb" }));
    }

    [Test]
    public void HeaderExtensionUris_UseCompleteIdentifiers()
    {
        string[] uris =
        {
            SdpHeaderExtensionUris.TransmissionTimeOffset,
            SdpHeaderExtensionUris.AbsoluteSendTime,
            SdpHeaderExtensionUris.AbsoluteCaptureTime,
            SdpHeaderExtensionUris.SynchronizationSourceAudioLevel,
            SdpHeaderExtensionUris.ContributingSourceAudioLevel,
            SdpHeaderExtensionUris.Encryption,
            SdpHeaderExtensionUris.VideoOrientation,
            SdpHeaderExtensionUris.TransportWideCongestionControl,
            SdpHeaderExtensionUris.MediaId,
            SdpHeaderExtensionUris.RtpStreamId,
            SdpHeaderExtensionUris.RepairedRtpStreamId,
            SdpHeaderExtensionUris.PlayoutDelay,
            SdpHeaderExtensionUris.VideoContentType,
            SdpHeaderExtensionUris.VideoTiming,
            SdpHeaderExtensionUris.ColorSpace,
            SdpHeaderExtensionUris.FrameMarking
        };

        Assert.That(uris, Is.EqualTo(new[] { "urn:ietf:params:rtp-hdrext:toffset", "http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time", "http://www.webrtc.org/experiments/rtp-hdrext/abs-capture-time", "urn:ietf:params:rtp-hdrext:ssrc-audio-level", "urn:ietf:params:rtp-hdrext:csrc-audio-level", "urn:ietf:params:rtp-hdrext:encrypt", "urn:3gpp:video-orientation", "http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01", "urn:ietf:params:rtp-hdrext:sdes:mid", "urn:ietf:params:rtp-hdrext:sdes:rtp-stream-id", "urn:ietf:params:rtp-hdrext:sdes:repaired-rtp-stream-id", "http://www.webrtc.org/experiments/rtp-hdrext/playout-delay", "http://www.webrtc.org/experiments/rtp-hdrext/video-content-type", "http://www.webrtc.org/experiments/rtp-hdrext/video-timing", "http://www.webrtc.org/experiments/rtp-hdrext/color-space", "urn:ietf:params:rtp-hdrext:framemarking" }));
    }

    [Test]
    public void IceTokens_UseRfc8839AndTcpCandidateSpelling()
    {
        string[] tokens =
        {
            SdpIceTokens.HostCandidate,
            SdpIceTokens.ServerReflexiveCandidate,
            SdpIceTokens.PeerReflexiveCandidate,
            SdpIceTokens.RelayedCandidate,
            SdpIceTokens.UdpTransport,
            SdpIceTokens.TcpTransport,
            SdpIceTokens.ActiveTcp,
            SdpIceTokens.PassiveTcp,
            SdpIceTokens.SimultaneousOpenTcp,
            SdpIceTokens.CandidateType,
            SdpIceTokens.RelatedAddress,
            SdpIceTokens.RelatedPort,
            SdpIceTokens.TcpType,
            SdpIceTokens.Trickle,
            SdpIceTokens.Ice2,
            SdpIceTokens.Renomination
        };

        Assert.That(tokens, Is.EqualTo(new[] { "host", "srflx", "prflx", "relay", "UDP", "TCP", "active", "passive", "so", "typ", "raddr", "rport", "tcptype", "trickle", "ice2", "renomination" }));
    }

    [Test]
    public void DataChannelTokens_UseDcmapParameterSpelling()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SdpDataChannelTokens.WebRtcDataChannel, Is.EqualTo("webrtc-datachannel"));
            Assert.That(SdpDataChannelTokens.DefaultMaximumMessageSize, Is.EqualTo(65536));
            Assert.That(SdpDataChannelTokens.Subprotocol, Is.EqualTo("subprotocol"));
            Assert.That(SdpDataChannelTokens.Label, Is.EqualTo("label"));
            Assert.That(SdpDataChannelTokens.MaximumRetransmissions, Is.EqualTo("max-retr"));
            Assert.That(SdpDataChannelTokens.MaximumRetransmissionTime, Is.EqualTo("max-time"));
            Assert.That(SdpDataChannelTokens.Ordered, Is.EqualTo("ordered"));
            Assert.That(SdpDataChannelTokens.Priority, Is.EqualTo("priority"));
        });
    }

    [Test]
    public void ClockTokens_UseRfc7273Spelling()
    {
        string[] tokens =
        {
            SdpClockTokens.NetworkTimeProtocol,
            SdpClockTokens.PrecisionTimeProtocol,
            SdpClockTokens.GlobalPositioningSystem,
            SdpClockTokens.Galileo,
            SdpClockTokens.Glonass,
            SdpClockTokens.Local,
            SdpClockTokens.Private,
            SdpClockTokens.Traceable,
            SdpClockTokens.Ieee1588_2002,
            SdpClockTokens.Ieee1588_2008,
            SdpClockTokens.Ieee8021As_2011,
            SdpClockTokens.Sender,
            SdpClockTokens.Direct,
            SdpClockTokens.Rate
        };

        Assert.That(tokens, Is.EqualTo(new[] { "ntp", "ptp", "gps", "gal", "glonass", "local", "private", "traceable", "IEEE1588-2002", "IEEE1588-2008", "IEEE802.1AS-2011", "sender", "direct", "rate" }));
    }
}
