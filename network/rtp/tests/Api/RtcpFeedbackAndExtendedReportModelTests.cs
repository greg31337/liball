using rtp;

namespace rtp.Tests.Api;

public class RtcpFeedbackAndExtendedReportModelTests
{
    [Test]
    public void FeedbackAndExtendedReportModelsProvideUsableEmptyDefaults()
    {
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        RtcpGenericNack nack = new RtcpGenericNack();
        RtcpSliceLossIndication sliceLoss = new RtcpSliceLossIndication();
        RtcpReferencePictureSelectionIndication referencePicture =
            new RtcpReferencePictureSelectionIndication();
        RtcpApplicationFeedback application = new RtcpApplicationFeedback();
        RtcpFullIntraRequest fullIntra = new RtcpFullIntraRequest();
        RtcpBitrateControl bitrate = new RtcpBitrateControl();
        RtcpCongestionControlFeedback congestion = new RtcpCongestionControlFeedback();
        RtcpCongestionControlReport congestionReport = new RtcpCongestionControlReport();
        RtcpExtendedReport extendedReport = new RtcpExtendedReport();
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();

        Assert.Multiple(() =>
        {
            Assert.That(feedback.TemporalSpatialTradeoffs, Is.Empty);
            Assert.That(feedback.RawFeedbackControlInformation, Is.Empty);
            Assert.That(nack.Entries, Is.Empty);
            Assert.That(sliceLoss.Entries, Is.Empty);
            Assert.That(referencePicture.NativeBitString, Is.Empty);
            Assert.That(application.Data, Is.Empty);
            Assert.That(fullIntra.Entries, Is.Empty);
            Assert.That(bitrate.Entries, Is.Empty);
            Assert.That(congestion.Reports, Is.Empty);
            Assert.That(congestionReport.Received, Is.Empty);
            Assert.That(congestionReport.EcnMarks, Is.Empty);
            Assert.That(congestionReport.ArrivalTimeOffsets, Is.Empty);
            Assert.That(extendedReport.Blocks, Is.Empty);
            Assert.That(block.RunLengthChunks, Is.Empty);
            Assert.That(block.ReceiptTimes, Is.Empty);
            Assert.That(block.DelayReports, Is.Empty);
            Assert.That(block.Data, Is.Empty);
        });
    }

    [Test]
    public void FeedbackCopyIsolatesRfc4585AndRfc5104Data()
    {
        RtcpGenericNackEntry nackEntry = new RtcpGenericNackEntry();
        nackEntry.PacketIdentifier = 65530;
        nackEntry.LostPacketBitmask = 0x8001;
        RtcpSliceLossEntry sliceEntry = new RtcpSliceLossEntry();
        sliceEntry.FirstMacroblock = 17;
        sliceEntry.MacroblockCount = 31;
        sliceEntry.PictureIdentifier = 22;
        RtcpFullIntraRequestEntry intraEntry = new RtcpFullIntraRequestEntry();
        intraEntry.SourceIdentifier = 0x10203040U;
        intraEntry.SequenceNumber = 7;
        RtcpBitrateControlEntry bitrateEntry = new RtcpBitrateControlEntry();
        bitrateEntry.SourceIdentifier = 0x50607080U;
        bitrateEntry.Exponent = 14;
        bitrateEntry.Mantissa = 65537U;
        bitrateEntry.Overhead = 48;
        RtcpTemporalSpatialTradeoff tradeoff = new RtcpTemporalSpatialTradeoff();
        tradeoff.IsNotification = true;
        tradeoff.SourceIdentifier = 0x90A0B0C0U;
        tradeoff.SequenceNumber = 8;
        tradeoff.TradeoffIndex = 19;

        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.Format = 15;
        feedback.TransportFormat = RtcpTransportFeedbackFormat.GenericNack;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.ApplicationFeedback;
        feedback.SenderSourceIdentifier = 0x01020304U;
        feedback.MediaSourceIdentifier = 0x05060708U;
        feedback.GenericNack = new RtcpGenericNack();
        feedback.GenericNack.Entries = new RtcpGenericNackEntry[] { nackEntry };
        feedback.PictureLossIndication = new RtcpPictureLossIndication();
        feedback.PictureLossIndication.Requested = true;
        feedback.SliceLossIndication = new RtcpSliceLossIndication();
        feedback.SliceLossIndication.Entries = new RtcpSliceLossEntry[] { sliceEntry };
        feedback.ReferencePictureSelectionIndication =
            new RtcpReferencePictureSelectionIndication();
        feedback.ReferencePictureSelectionIndication.PaddingBitCount = 3;
        feedback.ReferencePictureSelectionIndication.PayloadType = 96;
        feedback.ReferencePictureSelectionIndication.BitLength = 13;
        feedback.ReferencePictureSelectionIndication.NativeBitString = new byte[] { 0xAA, 0xA8 };
        feedback.ApplicationFeedback = new RtcpApplicationFeedback();
        feedback.ApplicationFeedback.Data = new byte[] { 82, 69, 77, 66 };
        feedback.FullIntraRequest = new RtcpFullIntraRequest();
        feedback.FullIntraRequest.Entries = new RtcpFullIntraRequestEntry[] { intraEntry };
        feedback.BitrateControl = new RtcpBitrateControl();
        feedback.BitrateControl.IsNotification = true;
        feedback.BitrateControl.Entries = new RtcpBitrateControlEntry[] { bitrateEntry };
        feedback.TemporalSpatialTradeoffs = new RtcpTemporalSpatialTradeoff[] { tradeoff };
        feedback.RapidResynchronizationRequest = new RtcpRapidResynchronizationRequest();
        feedback.RapidResynchronizationRequest.Requested = true;
        feedback.RawFeedbackControlInformation = new byte[] { 1, 2, 3, 4 };

        RtcpFeedbackPacket copy = feedback.Copy();
        nackEntry.PacketIdentifier = 1;
        sliceEntry.FirstMacroblock = 1;
        feedback.ReferencePictureSelectionIndication.NativeBitString[0] = 0;
        feedback.ApplicationFeedback.Data[0] = 0;
        intraEntry.SourceIdentifier = 0U;
        bitrateEntry.Mantissa = 0U;
        tradeoff.TradeoffIndex = 0;
        feedback.RawFeedbackControlInformation[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(copy.PacketType, Is.EqualTo(RtcpFeedbackPacketType.PayloadSpecific));
            Assert.That(copy.Format, Is.EqualTo(15));
            Assert.That(copy.TransportFormat, Is.EqualTo(RtcpTransportFeedbackFormat.GenericNack));
            Assert.That(copy.PayloadFormat,
                Is.EqualTo(RtcpPayloadFeedbackFormat.ApplicationFeedback));
            Assert.That(copy.SenderSourceIdentifier, Is.EqualTo(0x01020304U));
            Assert.That(copy.MediaSourceIdentifier, Is.EqualTo(0x05060708U));
            Assert.That(copy.GenericNack?.Entries[0].PacketIdentifier, Is.EqualTo(65530));
            Assert.That(copy.GenericNack?.Entries[0].LostPacketBitmask, Is.EqualTo(0x8001));
            Assert.That(copy.PictureLossIndication?.Requested, Is.True);
            Assert.That(copy.SliceLossIndication?.Entries[0].FirstMacroblock, Is.EqualTo(17));
            Assert.That(copy.SliceLossIndication?.Entries[0].MacroblockCount, Is.EqualTo(31));
            Assert.That(copy.SliceLossIndication?.Entries[0].PictureIdentifier, Is.EqualTo(22));
            Assert.That(copy.ReferencePictureSelectionIndication?.PaddingBitCount, Is.EqualTo(3));
            Assert.That(copy.ReferencePictureSelectionIndication?.PayloadType, Is.EqualTo(96));
            Assert.That(copy.ReferencePictureSelectionIndication?.BitLength, Is.EqualTo(13));
            Assert.That(copy.ReferencePictureSelectionIndication?.NativeBitString,
                Is.EqualTo(new byte[] { 0xAA, 0xA8 }));
            Assert.That(copy.ApplicationFeedback?.Data,
                Is.EqualTo(new byte[] { 82, 69, 77, 66 }));
            Assert.That(copy.FullIntraRequest?.Entries[0].SourceIdentifier,
                Is.EqualTo(0x10203040U));
            Assert.That(copy.FullIntraRequest?.Entries[0].SequenceNumber, Is.EqualTo(7));
            Assert.That(copy.BitrateControl?.IsNotification, Is.True);
            Assert.That(copy.BitrateControl?.Entries[0].SourceIdentifier,
                Is.EqualTo(0x50607080U));
            Assert.That(copy.BitrateControl?.Entries[0].Exponent, Is.EqualTo(14));
            Assert.That(copy.BitrateControl?.Entries[0].Mantissa, Is.EqualTo(65537U));
            Assert.That(copy.BitrateControl?.Entries[0].Overhead, Is.EqualTo(48));
            Assert.That(copy.TemporalSpatialTradeoffs[0].IsNotification, Is.True);
            Assert.That(copy.TemporalSpatialTradeoffs[0].SourceIdentifier,
                Is.EqualTo(0x90A0B0C0U));
            Assert.That(copy.TemporalSpatialTradeoffs[0].SequenceNumber, Is.EqualTo(8));
            Assert.That(copy.TemporalSpatialTradeoffs[0].TradeoffIndex, Is.EqualTo(19));
            Assert.That(copy.RapidResynchronizationRequest?.Requested, Is.True);
            Assert.That(copy.RawFeedbackControlInformation, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(copy.GenericNack?.Entries, Is.Not.SameAs(feedback.GenericNack.Entries));
            Assert.That(copy.TemporalSpatialTradeoffs,
                Is.Not.SameAs(feedback.TemporalSpatialTradeoffs));
        });
    }

    [Test]
    public void FeedbackCopyIsolatesRfc8888CongestionReports()
    {
        RtcpCongestionControlReport report = new RtcpCongestionControlReport();
        report.SourceIdentifier = 0x12345678U;
        report.BeginSequenceNumber = 65534;
        report.ReportCount = 3;
        report.Received = new bool[] { true, false, true };
        report.EcnMarks = new byte[] { 1, 0, 3 };
        report.ArrivalTimeOffsets = new ushort[] { 5, 0, 8191 };
        RtcpCongestionControlFeedback congestion = new RtcpCongestionControlFeedback();
        congestion.Reports = new RtcpCongestionControlReport[] { report };
        congestion.ReportTimestamp = 0x9ABCDEF0U;
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.CongestionControlFeedback = congestion;

        RtcpFeedbackPacket copy = feedback.Copy();
        report.SourceIdentifier = 0U;
        report.Received[0] = false;
        report.EcnMarks[0] = 0;
        report.ArrivalTimeOffsets[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(copy.CongestionControlFeedback?.ReportTimestamp,
                Is.EqualTo(0x9ABCDEF0U));
            Assert.That(copy.CongestionControlFeedback?.Reports[0].SourceIdentifier,
                Is.EqualTo(0x12345678U));
            Assert.That(copy.CongestionControlFeedback?.Reports[0].BeginSequenceNumber,
                Is.EqualTo(65534));
            Assert.That(copy.CongestionControlFeedback?.Reports[0].ReportCount, Is.EqualTo(3));
            Assert.That(copy.CongestionControlFeedback?.Reports[0].Received,
                Is.EqualTo(new bool[] { true, false, true }));
            Assert.That(copy.CongestionControlFeedback?.Reports[0].EcnMarks,
                Is.EqualTo(new byte[] { 1, 0, 3 }));
            Assert.That(copy.CongestionControlFeedback?.Reports[0].ArrivalTimeOffsets,
                Is.EqualTo(new ushort[] { 5, 0, 8191 }));
            Assert.That(copy.CongestionControlFeedback?.Reports,
                Is.Not.SameAs(congestion.Reports));
        });
    }

    [Test]
    public void ExtendedReportCopyIsolatesEveryNestedBlockRepresentation()
    {
        RtcpRunLengthChunk chunk = new RtcpRunLengthChunk();
        chunk.IsBitVector = true;
        chunk.PositiveRun = true;
        chunk.RunLength = 13;
        chunk.BitVector = 0x4123;
        RtcpDelaySinceLastReceiverReport delay = new RtcpDelaySinceLastReceiverReport();
        delay.ReceiverSourceIdentifier = 0x11111111U;
        delay.LastReceiverReport = 0x22222222U;
        delay.DelaySinceLastReceiverReport = 0x33333333U;
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.StatisticsSummary;
        block.WireType = 6;
        block.TypeSpecific = 0xE8;
        block.BlockLengthInWordsMinusOne = 9;
        block.SourceIdentifier = 0x44444444U;
        block.Thinning = 2;
        block.BeginSequenceNumber = 100;
        block.EndSequenceNumber = 120;
        block.RunLengthChunks = new RtcpRunLengthChunk[] { chunk };
        block.ReceiptTimes = new uint[] { 10U, 20U };
        block.ReceiverReferenceTime = 0x0123456789ABCDEFUL;
        block.DelayReports = new RtcpDelaySinceLastReceiverReport[] { delay };
        block.ReportsLoss = true;
        block.ReportsDuplicates = true;
        block.ReportsJitter = true;
        block.TimeToLiveOrHopLimitMode = 3;
        block.LostPackets = 7U;
        block.DuplicatePackets = 8U;
        block.MinimumJitter = 9U;
        block.MaximumJitter = 10U;
        block.MeanJitter = 11U;
        block.JitterDeviation = 12U;
        block.MinimumTimeToLiveOrHopLimit = 50;
        block.MaximumTimeToLiveOrHopLimit = 64;
        block.MeanTimeToLiveOrHopLimit = 58;
        block.TimeToLiveOrHopLimitDeviation = 4;
        block.LossRate = 1;
        block.DiscardRate = 2;
        block.BurstDensity = 3;
        block.GapDensity = 4;
        block.BurstDuration = 5;
        block.GapDuration = 6;
        block.RoundTripDelay = 7;
        block.EndSystemDelay = 8;
        block.SignalLevel = 9;
        block.NoiseLevel = 10;
        block.ResidualEchoReturnLoss = 11;
        block.MinimumGapThreshold = 12;
        block.RFactor = 13;
        block.ExternalRFactor = 14;
        block.MeanOpinionScoreListeningQuality = 15;
        block.MeanOpinionScoreConversationalQuality = 16;
        block.ReceiverConfiguration = 17;
        block.JitterBufferNominalDelay = 18;
        block.JitterBufferMaximumDelay = 19;
        block.JitterBufferAbsoluteMaximumDelay = 20;
        block.Data = new byte[] { 21, 22, 23, 24 };
        RtcpExtendedReport extendedReport = new RtcpExtendedReport();
        extendedReport.SenderSynchronizationSource = 0x55555555U;
        extendedReport.Blocks = new RtcpExtendedReportBlock[] { block };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ExtendedReport;
        packet.ExtendedReport = extendedReport;

        RtcpPacket copy = packet.Copy();
        chunk.BitVector = 0;
        delay.LastReceiverReport = 0U;
        block.ReceiptTimes[0] = 0U;
        block.Data[0] = 0;

        RtcpExtendedReportBlock? copiedBlock = copy.ExtendedReport?.Blocks[0];
        Assert.Multiple(() =>
        {
            Assert.That(copy.ExtendedReport?.SenderSynchronizationSource,
                Is.EqualTo(0x55555555U));
            Assert.That(copiedBlock?.Type, Is.EqualTo(RtcpExtendedReportBlockType.StatisticsSummary));
            Assert.That(copiedBlock?.WireType, Is.EqualTo(6));
            Assert.That(copiedBlock?.TypeSpecific, Is.EqualTo(0xE8));
            Assert.That(copiedBlock?.BlockLengthInWordsMinusOne, Is.EqualTo(9));
            Assert.That(copiedBlock?.SourceIdentifier, Is.EqualTo(0x44444444U));
            Assert.That(copiedBlock?.Thinning, Is.EqualTo(2));
            Assert.That(copiedBlock?.BeginSequenceNumber, Is.EqualTo(100));
            Assert.That(copiedBlock?.EndSequenceNumber, Is.EqualTo(120));
            Assert.That(copiedBlock?.RunLengthChunks[0].IsBitVector, Is.True);
            Assert.That(copiedBlock?.RunLengthChunks[0].PositiveRun, Is.True);
            Assert.That(copiedBlock?.RunLengthChunks[0].RunLength, Is.EqualTo(13));
            Assert.That(copiedBlock?.RunLengthChunks[0].BitVector, Is.EqualTo(0x4123));
            Assert.That(copiedBlock?.ReceiptTimes, Is.EqualTo(new uint[] { 10U, 20U }));
            Assert.That(copiedBlock?.ReceiverReferenceTime,
                Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(copiedBlock?.DelayReports[0].ReceiverSourceIdentifier,
                Is.EqualTo(0x11111111U));
            Assert.That(copiedBlock?.DelayReports[0].LastReceiverReport,
                Is.EqualTo(0x22222222U));
            Assert.That(copiedBlock?.DelayReports[0].DelaySinceLastReceiverReport,
                Is.EqualTo(0x33333333U));
            Assert.That(copiedBlock?.ReportsLoss, Is.True);
            Assert.That(copiedBlock?.ReportsDuplicates, Is.True);
            Assert.That(copiedBlock?.ReportsJitter, Is.True);
            Assert.That(copiedBlock?.TimeToLiveOrHopLimitMode, Is.EqualTo(3));
            Assert.That(copiedBlock?.LostPackets, Is.EqualTo(7U));
            Assert.That(copiedBlock?.DuplicatePackets, Is.EqualTo(8U));
            Assert.That(copiedBlock?.MinimumJitter, Is.EqualTo(9U));
            Assert.That(copiedBlock?.MaximumJitter, Is.EqualTo(10U));
            Assert.That(copiedBlock?.MeanJitter, Is.EqualTo(11U));
            Assert.That(copiedBlock?.JitterDeviation, Is.EqualTo(12U));
            Assert.That(copiedBlock?.MinimumTimeToLiveOrHopLimit, Is.EqualTo(50));
            Assert.That(copiedBlock?.MaximumTimeToLiveOrHopLimit, Is.EqualTo(64));
            Assert.That(copiedBlock?.MeanTimeToLiveOrHopLimit, Is.EqualTo(58));
            Assert.That(copiedBlock?.TimeToLiveOrHopLimitDeviation, Is.EqualTo(4));
            Assert.That(copiedBlock?.LossRate, Is.EqualTo(1));
            Assert.That(copiedBlock?.DiscardRate, Is.EqualTo(2));
            Assert.That(copiedBlock?.BurstDensity, Is.EqualTo(3));
            Assert.That(copiedBlock?.GapDensity, Is.EqualTo(4));
            Assert.That(copiedBlock?.BurstDuration, Is.EqualTo(5));
            Assert.That(copiedBlock?.GapDuration, Is.EqualTo(6));
            Assert.That(copiedBlock?.RoundTripDelay, Is.EqualTo(7));
            Assert.That(copiedBlock?.EndSystemDelay, Is.EqualTo(8));
            Assert.That(copiedBlock?.SignalLevel, Is.EqualTo(9));
            Assert.That(copiedBlock?.NoiseLevel, Is.EqualTo(10));
            Assert.That(copiedBlock?.ResidualEchoReturnLoss, Is.EqualTo(11));
            Assert.That(copiedBlock?.MinimumGapThreshold, Is.EqualTo(12));
            Assert.That(copiedBlock?.RFactor, Is.EqualTo(13));
            Assert.That(copiedBlock?.ExternalRFactor, Is.EqualTo(14));
            Assert.That(copiedBlock?.MeanOpinionScoreListeningQuality, Is.EqualTo(15));
            Assert.That(copiedBlock?.MeanOpinionScoreConversationalQuality, Is.EqualTo(16));
            Assert.That(copiedBlock?.ReceiverConfiguration, Is.EqualTo(17));
            Assert.That(copiedBlock?.JitterBufferNominalDelay, Is.EqualTo(18));
            Assert.That(copiedBlock?.JitterBufferMaximumDelay, Is.EqualTo(19));
            Assert.That(copiedBlock?.JitterBufferAbsoluteMaximumDelay, Is.EqualTo(20));
            Assert.That(copiedBlock?.Data, Is.EqualTo(new byte[] { 21, 22, 23, 24 }));
            Assert.That(copy.ExtendedReport?.Blocks, Is.Not.SameAs(extendedReport.Blocks));
            Assert.That(copiedBlock?.RunLengthChunks, Is.Not.SameAs(block.RunLengthChunks));
            Assert.That(copiedBlock?.DelayReports, Is.Not.SameAs(block.DelayReports));
            Assert.That(copiedBlock?.Data, Is.Not.SameAs(block.Data));
        });
    }
}
