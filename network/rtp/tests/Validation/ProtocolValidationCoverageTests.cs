using rtp;

namespace rtp.Tests.Validation;

public class ProtocolValidationCoverageTests
{
    [Test]
    public void RtcpPacketEncodingRejectsEveryMissingStructuralPart()
    {
        IRtcpPacketCodec codec = CreateRtcpCodec();
        byte[] destination = new byte[64];
        RtcpPacket noHeader = new RtcpPacket();
        noHeader.Header = null!;
        RtcpPacket wrongVersion = CreateUnknownPacket();
        wrongVersion.Header.Version = 1;
        RtcpPacket noBody = new RtcpPacket();
        noBody.Type = RtcpPacketType.Unknown;
        RtcpPacket noPadding = CreateUnknownPacket();
        noPadding.Padding = null!;
        RtcpPacket emptyPadding = CreateUnknownPacket();
        emptyPadding.Header.HasPadding = true;
        RtcpPacket longPadding = CreateUnknownPacket();
        longPadding.Header.HasPadding = true;
        longPadding.Padding = new byte[256];
        RtcpPacket wrongPaddingCount = CreateUnknownPacket();
        wrongPaddingCount.Header.HasPadding = true;
        wrongPaddingCount.Padding = new byte[] { 0, 3 };
        RtcpPacket unflaggedPadding = CreateUnknownPacket();
        unflaggedPadding.Padding = new byte[] { 1 };

        RtcpPacket[] invalidPackets =
        [
            null!,
            noHeader,
            wrongVersion,
            noBody,
            noPadding,
            emptyPadding,
            longPadding,
            wrongPaddingCount,
            unflaggedPadding
        ];
        int index = 0;
        while (index < invalidPackets.Length)
        {
            RtcpCodecResult result = codec.TryEncodeRtcpPacket(
                invalidPackets[index],
                destination);
            Assert.That(result.Success, Is.False, $"packet {index}");
            Assert.That(result.WrittenCount, Is.Zero, $"packet {index}");
            index = index + 1;
        }

        RtcpCompoundPacket noPackets = new RtcpCompoundPacket();
        noPackets.Packets = null!;
        Assert.Multiple(() =>
        {
            Assert.That(codec.TryEncodeRtcpCompoundPacket(null!, destination).Success,
                Is.False);
            Assert.That(codec.TryEncodeRtcpCompoundPacket(noPackets, destination).Success,
                Is.False);
        });
    }

    [Test]
    public void RtcpTypedBodiesRejectMissingOwnedArraysAndEntries()
    {
        RtcpApplicationDefined application = new RtcpApplicationDefined();
        application.Name = null!;
        RtcpApplicationDefined applicationData = new RtcpApplicationDefined();
        applicationData.Name = new byte[] { 1, 2, 3, 4 };
        applicationData.Data = null!;
        RtcpGoodbye goodbyeSources = new RtcpGoodbye();
        goodbyeSources.Sources = null!;
        RtcpGoodbye goodbyeReason = new RtcpGoodbye();
        goodbyeReason.Sources = new uint[] { 1U };
        goodbyeReason.Reason = null!;
        RtcpApplicationFeedback applicationFeedback =
            new RtcpApplicationFeedback();
        applicationFeedback.Data = null!;
        RtcpGenericNack genericNack = new RtcpGenericNack();
        genericNack.Entries = null!;
        RtcpGenericNack genericNackEntry = new RtcpGenericNack();
        genericNackEntry.Entries = new RtcpGenericNackEntry[] { null! };
        RtcpSliceLossIndication slice = new RtcpSliceLossIndication();
        slice.Entries = null!;
        RtcpSliceLossIndication sliceEntry = new RtcpSliceLossIndication();
        sliceEntry.Entries = new RtcpSliceLossEntry[] { null! };
        RtcpFullIntraRequest fullIntra = new RtcpFullIntraRequest();
        fullIntra.Entries = null!;
        RtcpFullIntraRequest fullIntraEntry = new RtcpFullIntraRequest();
        fullIntraEntry.Entries = new RtcpFullIntraRequestEntry[] { null! };

        Assert.Multiple(() =>
        {
            Assert.That(RtcpApplicationDefinedWireCodec
                .GetApplicationDefinedValidationError(application), Is.Not.Null);
            Assert.That(RtcpApplicationDefinedWireCodec
                .GetApplicationDefinedValidationError(applicationData), Is.Not.Null);
            Assert.That(RtcpGoodbyeWireCodec.GetGoodbyeValidationError(goodbyeSources),
                Is.Not.Null);
            Assert.That(RtcpGoodbyeWireCodec.GetGoodbyeValidationError(goodbyeReason),
                Is.Not.Null);
            Assert.That(RtcpApplicationFeedbackWireCodec
                .GetApplicationFeedbackValidationError(null), Is.Not.Null);
            Assert.That(RtcpApplicationFeedbackWireCodec
                .GetApplicationFeedbackValidationError(applicationFeedback),
                Is.Not.Null);
            Assert.That(RtcpGenericNackWireCodec
                .GetGenericNackValidationError(null), Is.Not.Null);
            Assert.That(RtcpGenericNackWireCodec
                .GetGenericNackValidationError(genericNack), Is.Not.Null);
            Assert.That(RtcpGenericNackWireCodec
                .GetGenericNackValidationError(genericNackEntry), Is.Not.Null);
            Assert.That(RtcpSliceLossIndicationWireCodec
                .GetSliceLossIndicationValidationError(null), Is.Not.Null);
            Assert.That(RtcpSliceLossIndicationWireCodec
                .GetSliceLossIndicationValidationError(slice), Is.Not.Null);
            Assert.That(RtcpSliceLossIndicationWireCodec
                .GetSliceLossIndicationValidationError(sliceEntry), Is.Not.Null);
            Assert.That(RtcpFullIntraRequestWireCodec
                .GetFullIntraRequestValidationError(null, 0), Is.Not.Null);
            Assert.That(RtcpFullIntraRequestWireCodec
                .GetFullIntraRequestValidationError(fullIntra, 0), Is.Not.Null);
            Assert.That(RtcpFullIntraRequestWireCodec
                .GetFullIntraRequestValidationError(fullIntraEntry, 0), Is.Not.Null);
            Assert.That(RtcpPictureLossIndicationWireCodec
                .GetPictureLossIndicationValidationError(null), Is.Not.Null);
            Assert.That(RtcpRapidResynchronizationRequestWireCodec
                .GetRapidResynchronizationRequestValidationError(null), Is.Not.Null);
        });
    }

    [Test]
    public void RtcpFeedbackValidationRejectsContradictoryDiscriminators()
    {
        RtcpFeedbackPacket unknown = new RtcpFeedbackPacket();
        RtcpFeedbackPacket transportAsPayload = CreateFeedback(
            RtcpFeedbackPacketType.PayloadSpecific);
        RtcpFeedbackPacket payloadAsTransport = CreateFeedback(
            RtcpFeedbackPacketType.TransportLayer);
        RtcpFeedbackPacket noRaw = CreateFeedback(
            RtcpFeedbackPacketType.TransportLayer);
        noRaw.RawFeedbackControlInformation = null!;
        RtcpFeedbackPacket badTransportFormat = CreateFeedback(
            RtcpFeedbackPacketType.TransportLayer);
        badTransportFormat.TransportFormat = (RtcpTransportFeedbackFormat)30;
        RtcpFeedbackPacket transportWithPayload = CreateFeedback(
            RtcpFeedbackPacketType.TransportLayer);
        transportWithPayload.PayloadFormat =
            RtcpPayloadFeedbackFormat.PictureLossIndication;
        RtcpFeedbackPacket badPayloadFormat = CreateFeedback(
            RtcpFeedbackPacketType.PayloadSpecific);
        badPayloadFormat.PayloadFormat = (RtcpPayloadFeedbackFormat)30;
        RtcpFeedbackPacket payloadWithTransport = CreateFeedback(
            RtcpFeedbackPacketType.PayloadSpecific);
        payloadWithTransport.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;

        Assert.Multiple(() =>
        {
            Assert.That(RtcpFeedbackWireCodec.GetFeedbackValidationError(
                null,
                RtcpPacketType.TransportFeedback), Is.Not.Null);
            Assert.That(RtcpFeedbackWireCodec.GetFeedbackValidationError(
                unknown,
                RtcpPacketType.TransportFeedback), Is.Not.Null);
            Assert.That(RtcpFeedbackWireCodec.GetFeedbackValidationError(
                transportAsPayload,
                RtcpPacketType.TransportFeedback), Is.Not.Null);
            Assert.That(RtcpFeedbackWireCodec.GetFeedbackValidationError(
                payloadAsTransport,
                RtcpPacketType.PayloadFeedback), Is.Not.Null);
            Assert.That(RtcpFeedbackWireCodec.GetFeedbackValidationError(
                noRaw,
                RtcpPacketType.TransportFeedback), Is.Not.Null);
            Assert.That(RtcpFeedbackWireCodec.GetFeedbackValidationError(
                badTransportFormat,
                RtcpPacketType.TransportFeedback), Is.Not.Null);
            Assert.That(RtcpFeedbackWireCodec.GetFeedbackValidationError(
                transportWithPayload,
                RtcpPacketType.TransportFeedback), Is.Not.Null);
            Assert.That(RtcpFeedbackWireCodec.GetFeedbackValidationError(
                badPayloadFormat,
                RtcpPacketType.PayloadFeedback), Is.Not.Null);
            Assert.That(RtcpFeedbackWireCodec.GetFeedbackValidationError(
                payloadWithTransport,
                RtcpPacketType.PayloadFeedback), Is.Not.Null);
        });
    }

    [Test]
    public void RtcpLaterFeedbackRejectsMissingReportsAndEntryArrays()
    {
        RtcpBitrateControl noEntries = new RtcpBitrateControl();
        noEntries.Entries = null!;
        RtcpBitrateControl nullEntry = new RtcpBitrateControl();
        nullEntry.Entries = new RtcpBitrateControlEntry[] { null! };
        RtcpCongestionControlFeedback noReports =
            new RtcpCongestionControlFeedback();
        noReports.Reports = null!;
        RtcpCongestionControlFeedback nullReport =
            new RtcpCongestionControlFeedback();
        nullReport.Reports = new RtcpCongestionControlReport[] { null! };
        RtcpCongestionControlReport noMetrics =
            new RtcpCongestionControlReport();
        noMetrics.ReportCount = 1;
        noMetrics.Received = null!;
        RtcpTemporalSpatialTradeoff nullTradeoff =
            new RtcpTemporalSpatialTradeoff();

        Assert.Multiple(() =>
        {
            Assert.That(RtcpBitrateControlWireCodec
                .GetBitrateControlValidationError(null, false, 0), Is.Not.Null);
            Assert.That(RtcpBitrateControlWireCodec
                .GetBitrateControlValidationError(noEntries, false, 0), Is.Not.Null);
            Assert.That(RtcpBitrateControlWireCodec
                .GetBitrateControlValidationError(nullEntry, false, 0), Is.Not.Null);
            Assert.That(RtcpCongestionControlFeedbackWireCodec
                .GetCongestionControlFeedbackValidationError(null, 0), Is.Not.Null);
            Assert.That(RtcpCongestionControlFeedbackWireCodec
                .GetCongestionControlFeedbackValidationError(noReports, 0),
                Is.Not.Null);
            Assert.That(RtcpCongestionControlFeedbackWireCodec
                .GetCongestionControlFeedbackValidationError(nullReport, 0),
                Is.Not.Null);
            Assert.That(RtcpCongestionControlFeedbackWireCodec
                .GetCongestionControlReportValidationError(noMetrics), Is.Not.Null);
            Assert.That(RtcpTemporalSpatialTradeoffWireCodec
                .GetTemporalSpatialTradeoffValidationError(null, false, 0),
                Is.Not.Null);
            Assert.That(RtcpTemporalSpatialTradeoffWireCodec
                .GetTemporalSpatialTradeoffValidationError(
                    new RtcpTemporalSpatialTradeoff[] { null! },
                    false,
                    0), Is.Not.Null);
            Assert.That(RtcpTemporalSpatialTradeoffWireCodec
                .GetTemporalSpatialTradeoffValidationError(
                    new RtcpTemporalSpatialTradeoff[] { nullTradeoff },
                    true,
                    0), Is.Not.Null);
        });
    }

    [Test]
    public void RtpPacketAndHeaderExtensionsRejectMissingOwnedArrays()
    {
        IRtpPacketCodec packetCodec = new RtpPacketCodec(
            new RtpSessionConfiguration());
        RtpPacket noSources = new RtpPacket();
        noSources.ContributingSources = null!;
        RtpPacket noPayload = new RtpPacket();
        noPayload.Payload = null!;
        RtpPacket noPadding = new RtpPacket();
        noPadding.Padding = null!;
        RtpHeaderExtension oneByte = new RtpHeaderExtension();
        oneByte.Profile = RtpHeaderExtensionProfiles.OneByte;
        oneByte.Format = RtpHeaderExtensionFormat.OneByte;
        oneByte.Elements = null!;
        RtpHeaderExtension twoByte = new RtpHeaderExtension();
        twoByte.Profile = RtpHeaderExtensionProfiles.TwoByte;
        twoByte.Format = RtpHeaderExtensionFormat.TwoByte;
        twoByte.Data = null!;

        Assert.Multiple(() =>
        {
            Assert.That(packetCodec.TryEncodeRtpPacket(noSources, new byte[64]).Success,
                Is.False);
            Assert.That(packetCodec.TryEncodeRtpPacket(noPayload, new byte[64]).Success,
                Is.False);
            Assert.That(packetCodec.TryEncodeRtpPacket(noPadding, new byte[64]).Success,
                Is.False);
            Assert.That(RtpHeaderExtensionWireCodec
                .GetOneByteValidationError(null!), Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec
                .GetOneByteValidationError(oneByte), Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec
                .GetTwoByteValidationError(null!), Is.Not.Null);
            Assert.That(RtpHeaderExtensionWireCodec
                .GetTwoByteValidationError(twoByte), Is.Not.Null);
        });
    }

    static IRtcpPacketCodec CreateRtcpCodec()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        return new RtcpPacketCodec(configuration);
    }

    static RtcpPacket CreateUnknownPacket()
    {
        RtcpUnknownPacket unknown = new RtcpUnknownPacket();
        unknown.PacketType = 220;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.Unknown;
        packet.UnknownPacket = unknown;
        return packet;
    }

    static RtcpFeedbackPacket CreateFeedback(RtcpFeedbackPacketType packetType)
    {
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = packetType;
        return feedback;
    }
}
