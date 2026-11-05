using rtp;

namespace rtp.Tests.Api;

public class ProtocolEnumTests
{
    [Test]
    public void PacketAndTransportEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)RtpPacketKind.Unknown, Is.EqualTo(0));
            Assert.That((int)RtpPacketKind.Rtp, Is.EqualTo(1));
            Assert.That((int)RtpPacketKind.Rtcp, Is.EqualTo(2));
            Assert.That((int)RtpHeaderExtensionFormat.Raw, Is.EqualTo(0));
            Assert.That((int)RtpHeaderExtensionFormat.OneByte, Is.EqualTo(1));
            Assert.That((int)RtpHeaderExtensionFormat.TwoByte, Is.EqualTo(2));
            Assert.That((int)RtpSessionTransportMode.Separate, Is.EqualTo(0));
            Assert.That((int)RtpSessionTransportMode.Multiplexed, Is.EqualTo(1));
        });
    }

    [Test]
    public void SequenceAndSourceEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)RtpSequenceStatus.Probation, Is.EqualTo(0));
            Assert.That((int)RtpSequenceStatus.Accepted, Is.EqualTo(1));
            Assert.That((int)RtpSequenceStatus.Duplicate, Is.EqualTo(2));
            Assert.That((int)RtpSequenceStatus.Reordered, Is.EqualTo(3));
            Assert.That((int)RtpSequenceStatus.LargeJump, Is.EqualTo(4));
            Assert.That((int)RtpSequenceStatus.Restarted, Is.EqualTo(5));
            Assert.That((int)RtpSequenceStatus.Invalid, Is.EqualTo(6));
            Assert.That((int)RtpSourceStatus.Probationary, Is.EqualTo(0));
            Assert.That((int)RtpSourceStatus.ActiveMember, Is.EqualTo(1));
            Assert.That((int)RtpSourceStatus.ActiveSender, Is.EqualTo(2));
            Assert.That((int)RtpSourceStatus.Inactive, Is.EqualTo(3));
            Assert.That((int)RtpSourceStatus.Goodbye, Is.EqualTo(4));
            Assert.That((int)RtpSourceStatus.Collision, Is.EqualTo(5));
        });
    }

    [Test]
    public void SessionEventAndJitterBufferEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)RtpSessionEventType.Source, Is.EqualTo(0));
            Assert.That((int)RtpSessionEventType.Loss, Is.EqualTo(1));
            Assert.That((int)RtpSessionEventType.Repair, Is.EqualTo(2));
            Assert.That((int)RtpSessionEventType.Collision, Is.EqualTo(3));
            Assert.That((int)RtpSessionEventType.Timeout, Is.EqualTo(4));
            Assert.That((int)RtpSessionEventType.Goodbye, Is.EqualTo(5));
            Assert.That((int)RtpSessionEventType.Report, Is.EqualTo(6));
            Assert.That((int)RtpSessionEventType.RecoverableError, Is.EqualTo(7));
            Assert.That((int)RtpJitterBufferStatus.Buffered, Is.EqualTo(0));
            Assert.That((int)RtpJitterBufferStatus.Released, Is.EqualTo(1));
            Assert.That((int)RtpJitterBufferStatus.Duplicate, Is.EqualTo(2));
            Assert.That((int)RtpJitterBufferStatus.Late, Is.EqualTo(3));
            Assert.That((int)RtpJitterBufferStatus.Lost, Is.EqualTo(4));
            Assert.That((int)RtpJitterBufferStatus.Overflow, Is.EqualTo(5));
            Assert.That((int)RtpJitterBufferStatus.Reset, Is.EqualTo(6));
        });
    }

    [Test]
    public void RepairAndFeedbackTimingEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)RtpRepairKind.Retransmission, Is.EqualTo(0));
            Assert.That((int)RtpRepairKind.Redundancy, Is.EqualTo(1));
            Assert.That((int)RtpRepairKind.ForwardErrorCorrection, Is.EqualTo(2));
            Assert.That((int)RtpRepairKind.ApplicationDefined, Is.EqualTo(3));
            Assert.That((int)RtcpFeedbackTimingMode.Regular, Is.EqualTo(0));
            Assert.That((int)RtcpFeedbackTimingMode.Early, Is.EqualTo(1));
            Assert.That((int)RtcpFeedbackTimingMode.Immediate, Is.EqualTo(2));
            Assert.That((int)RtcpFeedbackTimingMode.ReducedSize, Is.EqualTo(3));
        });
    }

    [Test]
    public void RtcpPacketEnumValuesMatchWirePacketTypes()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)RtcpPacketType.Unknown, Is.EqualTo(0));
            Assert.That((int)RtcpPacketType.HistoricFullIntraRequest, Is.EqualTo(192));
            Assert.That((int)RtcpPacketType.HistoricNegativeAcknowledgement, Is.EqualTo(193));
            Assert.That((int)RtcpPacketType.SmpteTimeCode, Is.EqualTo(194));
            Assert.That((int)RtcpPacketType.ExtendedInterArrivalJitter, Is.EqualTo(195));
            Assert.That((int)RtcpPacketType.SenderReport, Is.EqualTo(200));
            Assert.That((int)RtcpPacketType.ReceiverReport, Is.EqualTo(201));
            Assert.That((int)RtcpPacketType.SourceDescription, Is.EqualTo(202));
            Assert.That((int)RtcpPacketType.Goodbye, Is.EqualTo(203));
            Assert.That((int)RtcpPacketType.ApplicationDefined, Is.EqualTo(204));
            Assert.That((int)RtcpPacketType.TransportFeedback, Is.EqualTo(205));
            Assert.That((int)RtcpPacketType.PayloadFeedback, Is.EqualTo(206));
            Assert.That((int)RtcpPacketType.ExtendedReport, Is.EqualTo(207));
            Assert.That((int)RtcpPacketType.AudioVideoBridging, Is.EqualTo(208));
            Assert.That((int)RtcpPacketType.ReceiverSummaryInformation, Is.EqualTo(209));
            Assert.That((int)RtcpPacketType.PortMappingToken, Is.EqualTo(210));
            Assert.That((int)RtcpPacketType.InterDestinationMediaSynchronization, Is.EqualTo(211));
            Assert.That((int)RtcpPacketType.ReportingGroupSources, Is.EqualTo(212));
            Assert.That((int)RtcpPacketType.SplicingNotification, Is.EqualTo(213));
            Assert.That((int)RtcpFeedbackPacketType.Unknown, Is.EqualTo(0));
            Assert.That((int)RtcpFeedbackPacketType.TransportLayer, Is.EqualTo(205));
            Assert.That((int)RtcpFeedbackPacketType.PayloadSpecific, Is.EqualTo(206));
        });
    }

    [Test]
    public void SourceDescriptionEnumValuesMatchRfc3550()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)RtcpSdesItemType.End, Is.EqualTo(0));
            Assert.That((int)RtcpSdesItemType.CanonicalName, Is.EqualTo(1));
            Assert.That((int)RtcpSdesItemType.Name, Is.EqualTo(2));
            Assert.That((int)RtcpSdesItemType.Email, Is.EqualTo(3));
            Assert.That((int)RtcpSdesItemType.Phone, Is.EqualTo(4));
            Assert.That((int)RtcpSdesItemType.Location, Is.EqualTo(5));
            Assert.That((int)RtcpSdesItemType.Tool, Is.EqualTo(6));
            Assert.That((int)RtcpSdesItemType.Note, Is.EqualTo(7));
            Assert.That((int)RtcpSdesItemType.Private, Is.EqualTo(8));
            Assert.That((int)RtcpSdesItemType.Unknown, Is.EqualTo(255));
        });
    }

    [Test]
    public void TransportFeedbackEnumValuesMatchRegisteredFormats()
    {
        int[] values = new int[]
        {
            (int)RtcpTransportFeedbackFormat.Unknown,
            (int)RtcpTransportFeedbackFormat.GenericNack,
            (int)RtcpTransportFeedbackFormat.Reserved,
            (int)RtcpTransportFeedbackFormat.TemporaryMaximumBitrateRequest,
            (int)RtcpTransportFeedbackFormat.TemporaryMaximumBitrateNotification,
            (int)RtcpTransportFeedbackFormat.RapidResynchronizationRequest,
            (int)RtcpTransportFeedbackFormat.RapidMulticastAcquisition,
            (int)RtcpTransportFeedbackFormat.ThirdPartyLossEarlyIndication,
            (int)RtcpTransportFeedbackFormat.EcnFeedback,
            (int)RtcpTransportFeedbackFormat.PauseResume,
            (int)RtcpTransportFeedbackFormat.DelayBudgetInformation,
            (int)RtcpTransportFeedbackFormat.CongestionControlFeedback,
            (int)RtcpTransportFeedbackFormat.Extension
        };

        Assert.That(values, Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 31 }));
    }

    [Test]
    public void PayloadFeedbackEnumValuesMatchRegisteredFormats()
    {
        int[] values = new int[]
        {
            (int)RtcpPayloadFeedbackFormat.Unknown,
            (int)RtcpPayloadFeedbackFormat.PictureLossIndication,
            (int)RtcpPayloadFeedbackFormat.SliceLossIndication,
            (int)RtcpPayloadFeedbackFormat.ReferencePictureSelectionIndication,
            (int)RtcpPayloadFeedbackFormat.FullIntraRequest,
            (int)RtcpPayloadFeedbackFormat.TemporalSpatialTradeoffRequest,
            (int)RtcpPayloadFeedbackFormat.TemporalSpatialTradeoffNotification,
            (int)RtcpPayloadFeedbackFormat.VideoBackChannelMessage,
            (int)RtcpPayloadFeedbackFormat.ThirdPartyLossEarlyIndication,
            (int)RtcpPayloadFeedbackFormat.RegionOfInterest,
            (int)RtcpPayloadFeedbackFormat.LayerRefreshRequest,
            (int)RtcpPayloadFeedbackFormat.Viewport,
            (int)RtcpPayloadFeedbackFormat.ApplicationFeedback,
            (int)RtcpPayloadFeedbackFormat.Extension
        };

        Assert.That(values, Is.EqualTo(new int[]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 15, 31
        }));
    }

    [Test]
    public void ExtendedReportEnumValuesMatchRfc3611()
    {
        int[] values = new int[]
        {
            (int)RtcpExtendedReportBlockType.Unknown,
            (int)RtcpExtendedReportBlockType.LossRunLength,
            (int)RtcpExtendedReportBlockType.DuplicateRunLength,
            (int)RtcpExtendedReportBlockType.PacketReceiptTimes,
            (int)RtcpExtendedReportBlockType.ReceiverReferenceTime,
            (int)RtcpExtendedReportBlockType.DelaySinceLastReceiverReport,
            (int)RtcpExtendedReportBlockType.StatisticsSummary,
            (int)RtcpExtendedReportBlockType.VoiceOverIpMetrics
        };

        Assert.That(values, Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
    }

    [Test]
    public void PlannedDataModelsRetainEveryEnumCategory()
    {
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Format = RtpHeaderExtensionFormat.TwoByte;
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.TransportMode = RtpSessionTransportMode.Multiplexed;
        RtpFeedbackDecision decision = new RtpFeedbackDecision();
        decision.TimingMode = RtcpFeedbackTimingMode.Immediate;
        RtcpSourceDescriptionItem item = new RtcpSourceDescriptionItem();
        item.Type = RtcpSdesItemType.CanonicalName;
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.StatisticsSummary;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.PictureLossIndication;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;
        packet.SourceDescriptionItems = new RtcpSourceDescriptionItem[] { item };
        packet.ExtendedReportBlocks = new RtcpExtendedReportBlock[] { block };
        RtpSessionEvent sessionEvent = new RtpSessionEvent();
        sessionEvent.Type = RtpSessionEventType.Repair;
        sessionEvent.PacketKind = RtpPacketKind.Rtcp;
        sessionEvent.SequenceStatus = RtpSequenceStatus.Reordered;
        sessionEvent.SourceStatus = RtpSourceStatus.ActiveSender;
        sessionEvent.JitterBufferStatus = RtpJitterBufferStatus.Released;
        sessionEvent.RepairKind = RtpRepairKind.ForwardErrorCorrection;
        sessionEvent.HeaderExtension = extension;
        sessionEvent.ControlPacket = packet;
        sessionEvent.FeedbackDecision = decision;
        sessionEvent.Configuration = configuration;
        RtpSessionUpdate update = new RtpSessionUpdate();
        update.Events = new RtpSessionEvent[] { sessionEvent };

        Assert.Multiple(() =>
        {
            Assert.That(update.Events[0].Type, Is.EqualTo(RtpSessionEventType.Repair));
            Assert.That(update.Events[0].PacketKind, Is.EqualTo(RtpPacketKind.Rtcp));
            Assert.That(update.Events[0].SequenceStatus, Is.EqualTo(RtpSequenceStatus.Reordered));
            Assert.That(update.Events[0].SourceStatus, Is.EqualTo(RtpSourceStatus.ActiveSender));
            Assert.That(update.Events[0].JitterBufferStatus, Is.EqualTo(RtpJitterBufferStatus.Released));
            Assert.That(update.Events[0].RepairKind, Is.EqualTo(RtpRepairKind.ForwardErrorCorrection));
            Assert.That(update.Events[0].HeaderExtension?.Format,
                Is.EqualTo(RtpHeaderExtensionFormat.TwoByte));
            Assert.That(update.Events[0].ControlPacket?.Type,
                Is.EqualTo(RtcpPacketType.PayloadFeedback));
            Assert.That(update.Events[0].ControlPacket?.Feedback?.PacketType,
                Is.EqualTo(RtcpFeedbackPacketType.PayloadSpecific));
            Assert.That(update.Events[0].ControlPacket?.Feedback?.TransportFormat,
                Is.EqualTo(RtcpTransportFeedbackFormat.GenericNack));
            Assert.That(update.Events[0].ControlPacket?.Feedback?.PayloadFormat,
                Is.EqualTo(RtcpPayloadFeedbackFormat.PictureLossIndication));
            Assert.That(update.Events[0].ControlPacket?.SourceDescriptionItems[0].Type,
                Is.EqualTo(RtcpSdesItemType.CanonicalName));
            Assert.That(update.Events[0].ControlPacket?.ExtendedReportBlocks[0].Type,
                Is.EqualTo(RtcpExtendedReportBlockType.StatisticsSummary));
            Assert.That(update.Events[0].FeedbackDecision?.TimingMode,
                Is.EqualTo(RtcpFeedbackTimingMode.Immediate));
            Assert.That(update.Events[0].Configuration?.TransportMode,
                Is.EqualTo(RtpSessionTransportMode.Multiplexed));
        });
    }
}
