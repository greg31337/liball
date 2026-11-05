using rtp;

namespace rtp.Tests.Api;

public class RtcpDataModelTests
{
    [Test]
    public void RtcpModelsProvideUsableEmptyDefaults()
    {
        RtcpHeader header = new RtcpHeader();
        RtcpSenderReport senderReport = new RtcpSenderReport();
        RtcpReceiverReport receiverReport = new RtcpReceiverReport();
        RtcpSourceDescriptionItem item = new RtcpSourceDescriptionItem();
        RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
        RtcpSourceDescription description = new RtcpSourceDescription();
        RtcpGoodbye goodbye = new RtcpGoodbye();
        RtcpApplicationDefined application = new RtcpApplicationDefined();
        RtcpUnknownPacket unknown = new RtcpUnknownPacket();
        RtcpPacket packet = new RtcpPacket();
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        RtcpCodecResult result = new RtcpCodecResult();

        Assert.Multiple(() =>
        {
            Assert.That(header.Version, Is.EqualTo(RtcpProtocolConstants.Version));
            Assert.That(senderReport.SenderInformation, Is.Not.Null);
            Assert.That(senderReport.ReceptionReports, Is.Empty);
            Assert.That(senderReport.ProfileExtension, Is.Empty);
            Assert.That(receiverReport.ReceptionReports, Is.Empty);
            Assert.That(receiverReport.ProfileExtension, Is.Empty);
            Assert.That(item.Value, Is.Empty);
            Assert.That(item.PrivatePrefix, Is.Empty);
            Assert.That(chunk.Items, Is.Empty);
            Assert.That(description.Chunks, Is.Empty);
            Assert.That(goodbye.Sources, Is.Empty);
            Assert.That(goodbye.Reason, Is.Empty);
            Assert.That(application.Name, Is.Empty);
            Assert.That(application.Data, Is.Empty);
            Assert.That(unknown.Data, Is.Empty);
            Assert.That(packet.Header, Is.Not.Null);
            Assert.That(packet.Padding, Is.Empty);
            Assert.That(compound.Packets, Is.Empty);
            Assert.That(result.Data, Is.Empty);
        });
    }

    [Test]
    public void SenderAndReceiverReportCopiesIsolateNestedReportData()
    {
        RtcpReceptionReport reception = new RtcpReceptionReport();
        reception.SourceIdentifier = 0x01020304U;
        reception.FractionLost = 128;
        reception.CumulativePacketsLost = -123;
        reception.ExtendedHighestSequenceNumber = 0x0001FFFFU;
        reception.InterarrivalJitter = 456U;
        reception.LastSenderReport = 0x12345678U;
        reception.DelaySinceLastSenderReport = 0x00018000U;
        RtcpSenderInformation senderInformation = new RtcpSenderInformation();
        senderInformation.NtpTimestamp = 0x0123456789ABCDEFUL;
        senderInformation.RtpTimestamp = 0x89ABCDEFU;
        senderInformation.PacketCount = 100U;
        senderInformation.OctetCount = 5000U;
        RtcpSenderReport sender = new RtcpSenderReport();
        sender.SenderSynchronizationSource = 0x11111111U;
        sender.SenderInformation = senderInformation;
        sender.ReceptionReports = new RtcpReceptionReport[] { reception };
        sender.ProfileExtension = new byte[] { 1, 2, 3, 4 };
        RtcpReceiverReport receiver = new RtcpReceiverReport();
        receiver.SenderSynchronizationSource = 0x22222222U;
        receiver.ReceptionReports = new RtcpReceptionReport[] { reception };
        receiver.ProfileExtension = new byte[] { 5, 6, 7, 8 };

        RtcpSenderReport senderCopy = sender.Copy();
        RtcpReceiverReport receiverCopy = receiver.Copy();
        sender.SenderInformation.PacketCount = 999U;
        reception.CumulativePacketsLost = 999;
        sender.ProfileExtension[0] = 99;
        receiver.ProfileExtension[0] = 98;

        Assert.Multiple(() =>
        {
            Assert.That(senderCopy.SenderSynchronizationSource, Is.EqualTo(0x11111111U));
            Assert.That(senderCopy.SenderInformation, Is.Not.SameAs(sender.SenderInformation));
            Assert.That(senderCopy.SenderInformation.NtpTimestamp,
                Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(senderCopy.SenderInformation.RtpTimestamp, Is.EqualTo(0x89ABCDEFU));
            Assert.That(senderCopy.SenderInformation.PacketCount, Is.EqualTo(100U));
            Assert.That(senderCopy.SenderInformation.OctetCount, Is.EqualTo(5000U));
            Assert.That(senderCopy.ReceptionReports[0].SourceIdentifier,
                Is.EqualTo(0x01020304U));
            Assert.That(senderCopy.ReceptionReports[0].FractionLost, Is.EqualTo(128));
            Assert.That(senderCopy.ReceptionReports[0].CumulativePacketsLost, Is.EqualTo(-123));
            Assert.That(senderCopy.ReceptionReports[0].ExtendedHighestSequenceNumber,
                Is.EqualTo(0x0001FFFFU));
            Assert.That(senderCopy.ReceptionReports[0].InterarrivalJitter, Is.EqualTo(456U));
            Assert.That(senderCopy.ReceptionReports[0].LastSenderReport,
                Is.EqualTo(0x12345678U));
            Assert.That(senderCopy.ReceptionReports[0].DelaySinceLastSenderReport,
                Is.EqualTo(0x00018000U));
            Assert.That(senderCopy.ProfileExtension, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(receiverCopy.SenderSynchronizationSource, Is.EqualTo(0x22222222U));
            Assert.That(receiverCopy.ReceptionReports[0].CumulativePacketsLost, Is.EqualTo(-123));
            Assert.That(receiverCopy.ProfileExtension, Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
            Assert.That(senderCopy.ReceptionReports, Is.Not.SameAs(sender.ReceptionReports));
            Assert.That(receiverCopy.ReceptionReports, Is.Not.SameAs(receiver.ReceptionReports));
        });
    }

    [Test]
    public void SourceDescriptionCopyIsolatesChunksItemsAndPrivateData()
    {
        RtcpSourceDescriptionItem canonicalName = new RtcpSourceDescriptionItem();
        canonicalName.Type = RtcpSdesItemType.CanonicalName;
        canonicalName.WireType = 1;
        canonicalName.Value = new byte[] { 97, 64, 98 };
        RtcpSourceDescriptionItem privateItem = new RtcpSourceDescriptionItem();
        privateItem.Type = RtcpSdesItemType.Private;
        privateItem.WireType = 8;
        privateItem.PrivatePrefix = new byte[] { 97, 112, 112 };
        privateItem.Value = new byte[] { 1, 2 };
        RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = 0x12345678U;
        chunk.Items = new RtcpSourceDescriptionItem[] { canonicalName, privateItem };
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[] { chunk };

        RtcpSourceDescription copy = description.Copy();
        canonicalName.Value[0] = 99;
        privateItem.PrivatePrefix[0] = 100;
        privateItem.Value[0] = 101;
        chunk.SourceIdentifier = 0U;

        Assert.Multiple(() =>
        {
            Assert.That(copy.Chunks, Is.Not.SameAs(description.Chunks));
            Assert.That(copy.Chunks[0], Is.Not.SameAs(chunk));
            Assert.That(copy.Chunks[0].SourceIdentifier, Is.EqualTo(0x12345678U));
            Assert.That(copy.Chunks[0].Items[0].Type,
                Is.EqualTo(RtcpSdesItemType.CanonicalName));
            Assert.That(copy.Chunks[0].Items[0].WireType, Is.EqualTo(1));
            Assert.That(copy.Chunks[0].Items[0].Value, Is.EqualTo(new byte[] { 97, 64, 98 }));
            Assert.That(copy.Chunks[0].Items[1].PrivatePrefix,
                Is.EqualTo(new byte[] { 97, 112, 112 }));
            Assert.That(copy.Chunks[0].Items[1].Value, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(copy.Chunks[0].Items[1].PrivatePrefix,
                Is.Not.SameAs(privateItem.PrivatePrefix));
        });
    }

    [Test]
    public void GoodbyeApplicationAndUnknownCopiesOwnTheirArrays()
    {
        RtcpGoodbye goodbye = new RtcpGoodbye();
        goodbye.Sources = new uint[] { 1U, 2U };
        goodbye.HasReason = true;
        goodbye.Reason = new byte[] { 100, 111, 110, 101 };
        RtcpApplicationDefined application = new RtcpApplicationDefined();
        application.Subtype = 17;
        application.SourceIdentifier = 3U;
        application.Name = new byte[] { 84, 69, 83, 84 };
        application.Data = new byte[] { 4, 5, 6, 7 };
        RtcpUnknownPacket unknown = new RtcpUnknownPacket();
        unknown.PacketType = 222;
        unknown.Count = 9;
        unknown.Data = new byte[] { 8, 9, 10, 11 };

        RtcpGoodbye goodbyeCopy = goodbye.Copy();
        RtcpApplicationDefined applicationCopy = application.Copy();
        RtcpUnknownPacket unknownCopy = unknown.Copy();
        goodbye.Sources[0] = 99U;
        goodbye.Reason[0] = 99;
        application.Name[0] = 99;
        application.Data[0] = 99;
        unknown.Data[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(goodbyeCopy.Sources, Is.EqualTo(new uint[] { 1U, 2U }));
            Assert.That(goodbyeCopy.HasReason, Is.True);
            Assert.That(goodbyeCopy.Reason, Is.EqualTo(new byte[] { 100, 111, 110, 101 }));
            Assert.That(applicationCopy.Subtype, Is.EqualTo(17));
            Assert.That(applicationCopy.SourceIdentifier, Is.EqualTo(3U));
            Assert.That(applicationCopy.Name, Is.EqualTo(new byte[] { 84, 69, 83, 84 }));
            Assert.That(applicationCopy.Data, Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(unknownCopy.PacketType, Is.EqualTo(222));
            Assert.That(unknownCopy.Count, Is.EqualTo(9));
            Assert.That(unknownCopy.Data, Is.EqualTo(new byte[] { 8, 9, 10, 11 }));
        });
    }

    [Test]
    public void PacketCopyIsolatesEveryCoreTypedVariant()
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Header.Version = 2;
        packet.Header.HasPadding = true;
        packet.Header.Count = 1;
        packet.Header.PacketType = 200;
        packet.Header.LengthInWordsMinusOne = 7;
        packet.Type = RtcpPacketType.SenderReport;
        packet.SenderReport = new RtcpSenderReport();
        packet.SenderReport.ProfileExtension = new byte[] { 1 };
        packet.ReceiverReport = new RtcpReceiverReport();
        packet.ReceiverReport.ProfileExtension = new byte[] { 2 };
        packet.SourceDescription = new RtcpSourceDescription();
        packet.SourceDescription.Chunks = new RtcpSourceDescriptionChunk[]
        {
            new RtcpSourceDescriptionChunk()
        };
        packet.Goodbye = new RtcpGoodbye();
        packet.Goodbye.Sources = new uint[] { 3U };
        packet.ApplicationDefined = new RtcpApplicationDefined();
        packet.ApplicationDefined.Data = new byte[] { 4 };
        packet.UnknownPacket = new RtcpUnknownPacket();
        packet.UnknownPacket.Data = new byte[] { 5 };
        packet.Feedback = new RtcpFeedbackPacket();
        packet.Feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        RtcpSourceDescriptionItem compatibilityItem = new RtcpSourceDescriptionItem();
        compatibilityItem.Value = new byte[] { 6 };
        packet.SourceDescriptionItems = new RtcpSourceDescriptionItem[] { compatibilityItem };
        RtcpExtendedReportBlock block = new RtcpExtendedReportBlock();
        block.Type = RtcpExtendedReportBlockType.ReceiverReferenceTime;
        packet.ExtendedReportBlocks = new RtcpExtendedReportBlock[] { block };
        packet.Padding = new byte[] { 0, 2 };

        RtcpPacket copy = packet.Copy();
        packet.Header.Count = 9;
        packet.SenderReport.ProfileExtension[0] = 99;
        packet.ReceiverReport.ProfileExtension[0] = 99;
        packet.Goodbye.Sources[0] = 99U;
        packet.ApplicationDefined.Data[0] = 99;
        packet.UnknownPacket.Data[0] = 99;
        packet.SourceDescriptionItems[0].Value[0] = 99;
        packet.Padding[0] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(copy.Header, Is.Not.SameAs(packet.Header));
            Assert.That(copy.Header.Count, Is.EqualTo(1));
            Assert.That(copy.Header.PacketType, Is.EqualTo(200));
            Assert.That(copy.Header.LengthInWordsMinusOne, Is.EqualTo(7));
            Assert.That(copy.SenderReport?.ProfileExtension, Is.EqualTo(new byte[] { 1 }));
            Assert.That(copy.ReceiverReport?.ProfileExtension, Is.EqualTo(new byte[] { 2 }));
            Assert.That(copy.SourceDescription?.Chunks.Length, Is.EqualTo(1));
            Assert.That(copy.Goodbye?.Sources, Is.EqualTo(new uint[] { 3U }));
            Assert.That(copy.ApplicationDefined?.Data, Is.EqualTo(new byte[] { 4 }));
            Assert.That(copy.UnknownPacket?.Data, Is.EqualTo(new byte[] { 5 }));
            Assert.That(copy.Feedback?.PacketType,
                Is.EqualTo(RtcpFeedbackPacketType.TransportLayer));
            Assert.That(copy.SourceDescriptionItems[0].Value, Is.EqualTo(new byte[] { 6 }));
            Assert.That(copy.ExtendedReportBlocks[0].Type,
                Is.EqualTo(RtcpExtendedReportBlockType.ReceiverReferenceTime));
            Assert.That(copy.Padding, Is.EqualTo(new byte[] { 0, 2 }));
        });
    }

    [Test]
    public void CompoundAndCodecResultCopiesIsolatePacketGraphsAndEncodedData()
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.Unknown;
        packet.UnknownPacket = new RtcpUnknownPacket();
        packet.UnknownPacket.PacketType = 220;
        packet.UnknownPacket.Data = new byte[] { 1, 2, 3, 4 };
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[] { packet };
        RtcpCodecResult result = new RtcpCodecResult();
        result.Success = true;
        result.ConsumedCount = 8;
        result.WrittenCount = 8;
        result.Packet = packet;
        result.CompoundPacket = compound;
        result.Data = new byte[] { 128, 220, 0, 1 };
        result.ErrorMessage = "retained";

        RtcpCompoundPacket compoundCopy = compound.Copy();
        RtcpCodecResult resultCopy = result.Copy();
        packet.UnknownPacket.Data[0] = 99;
        result.Data[0] = 99;
        result.ErrorMessage = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(compoundCopy.Packets, Is.Not.SameAs(compound.Packets));
            Assert.That(compoundCopy.Packets[0], Is.Not.SameAs(packet));
            Assert.That(compoundCopy.Packets[0].UnknownPacket?.Data,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(resultCopy.Success, Is.True);
            Assert.That(resultCopy.ConsumedCount, Is.EqualTo(8));
            Assert.That(resultCopy.WrittenCount, Is.EqualTo(8));
            Assert.That(resultCopy.Packet, Is.Not.SameAs(result.Packet));
            Assert.That(resultCopy.Packet?.UnknownPacket?.Data,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(resultCopy.CompoundPacket, Is.Not.SameAs(result.CompoundPacket));
            Assert.That(resultCopy.CompoundPacket?.Packets[0].UnknownPacket?.Data,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(resultCopy.Data, Is.EqualTo(new byte[] { 128, 220, 0, 1 }));
            Assert.That(resultCopy.Data, Is.Not.SameAs(result.Data));
            Assert.That(resultCopy.ErrorMessage, Is.EqualTo("retained"));
        });
    }
}
