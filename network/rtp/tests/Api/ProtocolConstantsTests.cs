using rtp;

namespace rtp.Tests.Api;

public class ProtocolConstantsTests
{
    [Test]
    public void RtpHeaderConstantsMatchTheWireFormat()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RtpProtocolConstants.Version, Is.EqualTo(2));
            Assert.That(RtpProtocolConstants.FixedHeaderSize, Is.EqualTo(12));
            Assert.That(RtpProtocolConstants.SourceIdentifierSize, Is.EqualTo(4));
            Assert.That(RtpProtocolConstants.MaximumCsrcCount, Is.EqualTo(15));
            Assert.That(RtpProtocolConstants.MaximumPayloadType, Is.EqualTo(127));
            Assert.That(RtpProtocolConstants.HeaderExtensionEnvelopeSize, Is.EqualTo(4));
            Assert.That(RtpProtocolConstants.HeaderExtensionWordSize, Is.EqualTo(4));
            Assert.That(RtpProtocolConstants.MaximumHeaderExtensionWordCount, Is.EqualTo(65535));
            Assert.That(RtpProtocolConstants.MaximumHeaderExtensionDataSize, Is.EqualTo(262140));
            Assert.That(RtpProtocolConstants.MaximumPaddingCount, Is.EqualTo(255));
            Assert.That(RtpProtocolConstants.VersionShift, Is.EqualTo(6));
            Assert.That(RtpProtocolConstants.VersionMask, Is.EqualTo(0xC0));
            Assert.That(RtpProtocolConstants.PaddingMask, Is.EqualTo(0x20));
            Assert.That(RtpProtocolConstants.ExtensionMask, Is.EqualTo(0x10));
            Assert.That(RtpProtocolConstants.CsrcCountMask, Is.EqualTo(0x0F));
            Assert.That(RtpProtocolConstants.MarkerMask, Is.EqualTo(0x80));
            Assert.That(RtpProtocolConstants.PayloadTypeMask, Is.EqualTo(0x7F));
            Assert.That(RtpProtocolConstants.MultiplexingForbiddenPayloadTypeMinimum, Is.EqualTo(64));
            Assert.That(RtpProtocolConstants.MultiplexingForbiddenPayloadTypeMaximum, Is.EqualTo(95));
        });
    }

    [Test]
    public void RtpSequenceConstantsMatchRfc3550Defaults()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RtpProtocolConstants.SequenceNumberModulus, Is.EqualTo(65536));
            Assert.That(RtpProtocolConstants.MaximumSequenceNumber, Is.EqualTo(65535));
            Assert.That(RtpProtocolConstants.DefaultMaximumDropout, Is.EqualTo(3000));
            Assert.That(RtpProtocolConstants.DefaultMaximumMisorder, Is.EqualTo(100));
            Assert.That(RtpProtocolConstants.DefaultMinimumSequential, Is.EqualTo(2));
        });
    }

    [Test]
    public void AvpStaticAudioAssignmentsMatchRfc3551()
    {
        byte[] payloadTypes = new byte[]
        {
            RtpAvpPayloadTypes.Pcmu,
            RtpAvpPayloadTypes.Gsm,
            RtpAvpPayloadTypes.G723,
            RtpAvpPayloadTypes.Dvi4At8000,
            RtpAvpPayloadTypes.Dvi4At16000,
            RtpAvpPayloadTypes.Lpc,
            RtpAvpPayloadTypes.Pcma,
            RtpAvpPayloadTypes.G722,
            RtpAvpPayloadTypes.L16Stereo,
            RtpAvpPayloadTypes.L16Mono,
            RtpAvpPayloadTypes.Qcelp,
            RtpAvpPayloadTypes.ComfortNoise,
            RtpAvpPayloadTypes.MpegAudio,
            RtpAvpPayloadTypes.G728,
            RtpAvpPayloadTypes.Dvi4At11025,
            RtpAvpPayloadTypes.Dvi4At22050,
            RtpAvpPayloadTypes.G729
        };
        int[] clockRates = new int[]
        {
            RtpAvpPayloadTypes.PcmuClockRate,
            RtpAvpPayloadTypes.GsmClockRate,
            RtpAvpPayloadTypes.G723ClockRate,
            RtpAvpPayloadTypes.Dvi4At8000ClockRate,
            RtpAvpPayloadTypes.Dvi4At16000ClockRate,
            RtpAvpPayloadTypes.LpcClockRate,
            RtpAvpPayloadTypes.PcmaClockRate,
            RtpAvpPayloadTypes.G722ClockRate,
            RtpAvpPayloadTypes.L16StereoClockRate,
            RtpAvpPayloadTypes.L16MonoClockRate,
            RtpAvpPayloadTypes.QcelpClockRate,
            RtpAvpPayloadTypes.ComfortNoiseClockRate,
            RtpAvpPayloadTypes.MpegAudioClockRate,
            RtpAvpPayloadTypes.G728ClockRate,
            RtpAvpPayloadTypes.Dvi4At11025ClockRate,
            RtpAvpPayloadTypes.Dvi4At22050ClockRate,
            RtpAvpPayloadTypes.G729ClockRate
        };

        Assert.Multiple(() =>
        {
            Assert.That(payloadTypes, Is.EqualTo(new byte[]
            {
                0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18
            }));
            Assert.That(clockRates, Is.EqualTo(new int[]
            {
                8000, 8000, 8000, 8000, 16000, 8000, 8000, 8000, 44100,
                44100, 8000, 8000, 90000, 8000, 11025, 22050, 8000
            }));
            Assert.That(RtpAvpPayloadTypes.MonoChannelCount, Is.EqualTo(1));
            Assert.That(RtpAvpPayloadTypes.StereoChannelCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void AvpStaticVideoAndAssignmentRangesMatchRfc3551()
    {
        byte[] payloadTypes = new byte[]
        {
            RtpAvpPayloadTypes.CelB,
            RtpAvpPayloadTypes.Jpeg,
            RtpAvpPayloadTypes.Nv,
            RtpAvpPayloadTypes.H261,
            RtpAvpPayloadTypes.MpegVideo,
            RtpAvpPayloadTypes.Mpeg2TransportStream,
            RtpAvpPayloadTypes.H263
        };
        int[] clockRates = new int[]
        {
            RtpAvpPayloadTypes.CelBClockRate,
            RtpAvpPayloadTypes.JpegClockRate,
            RtpAvpPayloadTypes.NvClockRate,
            RtpAvpPayloadTypes.H261ClockRate,
            RtpAvpPayloadTypes.MpegVideoClockRate,
            RtpAvpPayloadTypes.Mpeg2TransportStreamClockRate,
            RtpAvpPayloadTypes.H263ClockRate
        };

        Assert.Multiple(() =>
        {
            Assert.That(payloadTypes, Is.EqualTo(new byte[] { 25, 26, 28, 31, 32, 33, 34 }));
            Assert.That(clockRates, Is.All.EqualTo(90000));
            Assert.That(RtpAvpPayloadTypes.RtcpConflictAvoidanceMinimum, Is.EqualTo(72));
            Assert.That(RtpAvpPayloadTypes.RtcpConflictAvoidanceMaximum, Is.EqualTo(76));
            Assert.That(RtpAvpPayloadTypes.DynamicMinimum, Is.EqualTo(96));
            Assert.That(RtpAvpPayloadTypes.DynamicMaximum, Is.EqualTo(127));
        });
    }

    [Test]
    public void HeaderExtensionProfilesMatchRfc8285()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RtpHeaderExtensionProfiles.OneByte, Is.EqualTo(0xBEDE));
            Assert.That(RtpHeaderExtensionProfiles.TwoByte, Is.EqualTo(0x1000));
            Assert.That(RtpHeaderExtensionProfiles.TwoByteProfileMask, Is.EqualTo(0xFFF0));
            Assert.That(RtpHeaderExtensionProfiles.TwoByteApplicationBitsMask, Is.EqualTo(0x000F));
            Assert.That(RtpHeaderExtensionProfiles.PaddingIdentifier, Is.Zero);
            Assert.That(RtpHeaderExtensionProfiles.OneByteIdentifierMinimum, Is.EqualTo(1));
            Assert.That(RtpHeaderExtensionProfiles.OneByteIdentifierMaximum, Is.EqualTo(14));
            Assert.That(RtpHeaderExtensionProfiles.OneByteReservedIdentifier, Is.EqualTo(15));
            Assert.That(RtpHeaderExtensionProfiles.OneByteDataLengthMinimum, Is.EqualTo(1));
            Assert.That(RtpHeaderExtensionProfiles.OneByteDataLengthMaximum, Is.EqualTo(16));
            Assert.That(RtpHeaderExtensionProfiles.TwoByteIdentifierMinimum, Is.EqualTo(1));
            Assert.That(RtpHeaderExtensionProfiles.TwoByteIdentifierMaximum, Is.EqualTo(255));
            Assert.That(RtpHeaderExtensionProfiles.TwoByteApplicationBitsIdentifier, Is.EqualTo(256));
            Assert.That(RtpHeaderExtensionProfiles.TwoByteDataLengthMinimum, Is.Zero);
            Assert.That(RtpHeaderExtensionProfiles.TwoByteDataLengthMaximum, Is.EqualTo(255));
            Assert.That(RtpHeaderExtensionProfiles.OneByteElementHeaderSize, Is.EqualTo(1));
            Assert.That(RtpHeaderExtensionProfiles.TwoByteElementHeaderSize, Is.EqualTo(2));
        });
    }

    [Test]
    public void KnownHeaderExtensionUrisMatchTheirRegistrations()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RtpHeaderExtensionUris.Ntp64,
                Is.EqualTo("urn:ietf:params:rtp-hdrext:ntp-64"));
            Assert.That(RtpHeaderExtensionUris.SsrcAudioLevel,
                Is.EqualTo("urn:ietf:params:rtp-hdrext:ssrc-audio-level"));
            Assert.That(RtpHeaderExtensionUris.CsrcAudioLevel,
                Is.EqualTo("urn:ietf:params:rtp-hdrext:csrc-audio-level"));
            Assert.That(RtpHeaderExtensionUris.MediaIdentifier,
                Is.EqualTo("urn:ietf:params:rtp-hdrext:sdes:mid"));
            Assert.That(RtpHeaderExtensionUris.RtpStreamIdentifier,
                Is.EqualTo("urn:ietf:params:rtp-hdrext:sdes:rtp-stream-id"));
            Assert.That(RtpHeaderExtensionUris.RepairedRtpStreamIdentifier,
                Is.EqualTo("urn:ietf:params:rtp-hdrext:sdes:repaired-rtp-stream-id"));
        });
    }

    [Test]
    public void RtcpPacketTypesMatchTheIanaRegistry()
    {
        byte[] assignedTypes = new byte[]
        {
            RtcpProtocolConstants.HistoricFullIntraRequestPacketType,
            RtcpProtocolConstants.HistoricNegativeAcknowledgementPacketType,
            RtcpProtocolConstants.SmpteTimeCodePacketType,
            RtcpProtocolConstants.InterArrivalJitterPacketType,
            RtcpProtocolConstants.SenderReportPacketType,
            RtcpProtocolConstants.ReceiverReportPacketType,
            RtcpProtocolConstants.SourceDescriptionPacketType,
            RtcpProtocolConstants.GoodbyePacketType,
            RtcpProtocolConstants.ApplicationDefinedPacketType,
            RtcpProtocolConstants.TransportFeedbackPacketType,
            RtcpProtocolConstants.PayloadFeedbackPacketType,
            RtcpProtocolConstants.ExtendedReportPacketType,
            RtcpProtocolConstants.AudioVideoBridgingPacketType,
            RtcpProtocolConstants.ReceiverSummaryInformationPacketType,
            RtcpProtocolConstants.PortMappingTokenPacketType,
            RtcpProtocolConstants.InterDestinationMediaSynchronizationPacketType,
            RtcpProtocolConstants.ReportingGroupSourcesPacketType,
            RtcpProtocolConstants.SplicingNotificationPacketType
        };

        Assert.Multiple(() =>
        {
            Assert.That(assignedTypes, Is.EqualTo(new byte[]
            {
                192, 193, 194, 195, 200, 201, 202, 203, 204,
                205, 206, 207, 208, 209, 210, 211, 212, 213
            }));
            Assert.That(RtcpProtocolConstants.PrimaryPacketTypeMinimum, Is.EqualTo(200));
            Assert.That(RtcpProtocolConstants.PrimaryPacketTypeMaximum, Is.EqualTo(223));
            Assert.That(RtcpProtocolConstants.MultiplexingPacketTypeMinimum, Is.EqualTo(192));
            Assert.That(RtcpProtocolConstants.MultiplexingPacketTypeMaximum, Is.EqualTo(223));
        });
    }

    [Test]
    public void RtcpWireSizesAndLimitsMatchTheProtocols()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RtcpProtocolConstants.Version, Is.EqualTo(2));
            Assert.That(RtcpProtocolConstants.HeaderSize, Is.EqualTo(4));
            Assert.That(RtcpProtocolConstants.LengthWordSize, Is.EqualTo(4));
            Assert.That(RtcpProtocolConstants.MaximumLengthField, Is.EqualTo(65535));
            Assert.That(RtcpProtocolConstants.MaximumPacketSize, Is.EqualTo(262144));
            Assert.That(RtcpProtocolConstants.MaximumCount, Is.EqualTo(31));
            Assert.That(RtcpProtocolConstants.MaximumReceptionReportCount, Is.EqualTo(31));
            Assert.That(RtcpProtocolConstants.MaximumSourceCount, Is.EqualTo(31));
            Assert.That(RtcpProtocolConstants.MaximumTextLength, Is.EqualTo(255));
            Assert.That(RtcpProtocolConstants.MaximumPaddingCount, Is.EqualTo(255));
            Assert.That(RtcpProtocolConstants.SourceIdentifierSize, Is.EqualTo(4));
            Assert.That(RtcpProtocolConstants.SenderInformationSize, Is.EqualTo(20));
            Assert.That(RtcpProtocolConstants.ReceptionReportSize, Is.EqualTo(24));
            Assert.That(RtcpProtocolConstants.SenderReportMinimumSize, Is.EqualTo(28));
            Assert.That(RtcpProtocolConstants.ReceiverReportMinimumSize, Is.EqualTo(8));
            Assert.That(RtcpProtocolConstants.SourceDescriptionItemHeaderSize, Is.EqualTo(2));
            Assert.That(RtcpProtocolConstants.SourceDescriptionEnd, Is.Zero);
            Assert.That(RtcpProtocolConstants.ApplicationNameSize, Is.EqualTo(4));
            Assert.That(RtcpProtocolConstants.ApplicationMinimumSize, Is.EqualTo(12));
            Assert.That(RtcpProtocolConstants.FeedbackMinimumSize, Is.EqualTo(12));
            Assert.That(RtcpProtocolConstants.ExtendedReportMinimumSize, Is.EqualTo(8));
            Assert.That(RtcpProtocolConstants.ExtendedReportBlockHeaderSize, Is.EqualTo(4));
            Assert.That(RtcpProtocolConstants.CumulativeLossMinimum, Is.EqualTo(-8388608));
            Assert.That(RtcpProtocolConstants.CumulativeLossMaximum, Is.EqualTo(8388607));
            Assert.That(RtcpProtocolConstants.FractionLostMaximum, Is.EqualTo(255));
            Assert.That(RtcpProtocolConstants.VersionMask, Is.EqualTo(0xC0));
            Assert.That(RtcpProtocolConstants.PaddingMask, Is.EqualTo(0x20));
            Assert.That(RtcpProtocolConstants.CountMask, Is.EqualTo(0x1F));
            Assert.That(RtcpProtocolConstants.VersionShift, Is.EqualTo(6));
        });
    }

    [Test]
    public void RtcpTimingConstantsMatchRfc3550()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RtcpProtocolConstants.RecommendedBandwidthFraction, Is.EqualTo(0.05));
            Assert.That(RtcpProtocolConstants.SenderBandwidthFraction, Is.EqualTo(0.25));
            Assert.That(RtcpProtocolConstants.ReceiverBandwidthFraction, Is.EqualTo(0.75));
            Assert.That(RtcpProtocolConstants.MinimumIntervalSeconds, Is.EqualTo(5.0));
            Assert.That(RtcpProtocolConstants.InitialMinimumIntervalSeconds, Is.EqualTo(2.5));
            Assert.That(RtcpProtocolConstants.IntervalCompensation, Is.EqualTo(1.21828));
            Assert.That(RtcpProtocolConstants.AverageSizeSampleWeight, Is.EqualTo(1));
            Assert.That(RtcpProtocolConstants.AverageSizePreviousWeight, Is.EqualTo(15));
            Assert.That(RtcpProtocolConstants.AverageSizeWeightDenominator, Is.EqualTo(16));
            Assert.That(RtcpProtocolConstants.CompactNtpUnitsPerSecond, Is.EqualTo(65536));
            Assert.That(RtcpProtocolConstants.NtpToUnixEpochSeconds, Is.EqualTo(2208988800UL));
        });
    }

    [Test]
    public void FeedbackFormatsMatchTheIanaRegistries()
    {
        byte[] transportFormats = new byte[]
        {
            RtcpFeedbackFormats.TransportGenericNack,
            RtcpFeedbackFormats.TransportReserved,
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateRequest,
            RtcpFeedbackFormats.TransportTemporaryMaximumBitrateNotification,
            RtcpFeedbackFormats.TransportRapidResynchronizationRequest,
            RtcpFeedbackFormats.TransportRapidMulticastAcquisition,
            RtcpFeedbackFormats.TransportThirdPartyLossEarlyIndication,
            RtcpFeedbackFormats.TransportEcnFeedback,
            RtcpFeedbackFormats.TransportPauseResume,
            RtcpFeedbackFormats.TransportDelayBudgetInformation,
            RtcpFeedbackFormats.TransportCongestionControlFeedback,
            RtcpFeedbackFormats.TransportExtension
        };
        byte[] payloadFormats = new byte[]
        {
            RtcpFeedbackFormats.PayloadPictureLossIndication,
            RtcpFeedbackFormats.PayloadSliceLossIndication,
            RtcpFeedbackFormats.PayloadReferencePictureSelectionIndication,
            RtcpFeedbackFormats.PayloadFullIntraRequest,
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffRequest,
            RtcpFeedbackFormats.PayloadTemporalSpatialTradeoffNotification,
            RtcpFeedbackFormats.PayloadVideoBackChannelMessage,
            RtcpFeedbackFormats.PayloadThirdPartyLossEarlyIndication,
            RtcpFeedbackFormats.PayloadRegionOfInterest,
            RtcpFeedbackFormats.PayloadLayerRefreshRequest,
            RtcpFeedbackFormats.PayloadViewport,
            RtcpFeedbackFormats.PayloadApplicationFeedback,
            RtcpFeedbackFormats.PayloadExtension
        };

        Assert.Multiple(() =>
        {
            Assert.That(transportFormats, Is.EqualTo(new byte[]
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 31
            }));
            Assert.That(payloadFormats, Is.EqualTo(new byte[]
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 15, 31
            }));
        });
    }

    [Test]
    public void ExtendedReportBlockTypesMatchRfc3611()
    {
        byte[] blockTypes = new byte[]
        {
            RtcpExtendedReportBlockTypes.LossRunLength,
            RtcpExtendedReportBlockTypes.DuplicateRunLength,
            RtcpExtendedReportBlockTypes.PacketReceiptTimes,
            RtcpExtendedReportBlockTypes.ReceiverReferenceTime,
            RtcpExtendedReportBlockTypes.DelaySinceLastReceiverReport,
            RtcpExtendedReportBlockTypes.StatisticsSummary,
            RtcpExtendedReportBlockTypes.VoiceOverIpMetrics,
            RtcpExtendedReportBlockTypes.Extension
        };

        Assert.That(blockTypes, Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 255 }));
    }
}
