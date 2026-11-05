using rtp;

namespace rtp.Tests.Wire;

public class RtcpCompoundValidationTests
{
    [Test]
    public void StrictCodecAcceptsCanonicalOrderingAndFinalPacketPadding()
    {
        IRtcpPacketCodec codec = CreateCodec(true);
        RtcpPacket goodbye = CreateGoodbyePacket();
        goodbye.Header.HasPadding = true;
        goodbye.Padding = new byte[] { 0x11, 0x22, 0x33, 4 };
        RtcpCompoundPacket compound = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(),
            CreateReceiverReportPacket(),
            CreateSourceDescriptionPacket(true),
            CreateApplicationPacket(),
            goodbye
        });

        byte[] encoded = codec.EncodeRtcpCompoundPacketToArray(compound);
        RtcpCompoundPacket decoded = codec.DecodeRtcpCompoundPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Packets.Length, Is.EqualTo(5));
            Assert.That(decoded.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(decoded.Packets[1].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(decoded.Packets[2].Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(decoded.Packets[3].Type,
                Is.EqualTo(RtcpPacketType.ApplicationDefined));
            Assert.That(decoded.Packets[4].Type, Is.EqualTo(RtcpPacketType.Goodbye));
            Assert.That(decoded.Packets[4].Padding,
                Is.EqualTo(new byte[] { 0x11, 0x22, 0x33, 4 }));
        });
    }

    [Test]
    public void StrictCodecRequiresAtLeastTwoPacketsAndAnInitialReport()
    {
        IRtcpPacketCodec strictCodec = CreateCodec(true);
        IRtcpPacketCodec tolerantCodec = CreateCodec(false);
        RtcpCompoundPacket onePacket = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket()
        });
        RtcpCompoundPacket wrongFirst = CreateCompound(new RtcpPacket[]
        {
            CreateApplicationPacket(),
            CreateSourceDescriptionPacket(true)
        });
        byte[] wrongFirstWire = tolerantCodec.EncodeRtcpCompoundPacketToArray(wrongFirst);

        Assert.Multiple(() =>
        {
            Assert.That(strictCodec.TryEncodeRtcpCompoundPacket(
                onePacket,
                new byte[64]).Success, Is.False);
            Assert.That(strictCodec.TryEncodeRtcpCompoundPacket(
                wrongFirst,
                new byte[64]).Success, Is.False);
            Assert.That(strictCodec.TryDecodeRtcpCompoundPacket(wrongFirstWire).Success,
                Is.False);
            Assert.That(tolerantCodec.TryDecodeRtcpCompoundPacket(wrongFirstWire).Success,
                Is.True);
            Assert.Throws<RtcpPacketFormatException>(() =>
                strictCodec.GetRtcpCompoundPacketEncodedSize(wrongFirst));
        });
    }

    [Test]
    public void StrictCodecRequiresCnameAndKeepsReportsBeforeSdes()
    {
        IRtcpPacketCodec strictCodec = CreateCodec(true);
        RtcpCompoundPacket missingCanonicalName = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(),
            CreateSourceDescriptionPacket(false)
        });
        RtcpCompoundPacket lateSourceDescription = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(),
            CreateApplicationPacket(),
            CreateSourceDescriptionPacket(true)
        });
        RtcpCompoundPacket lateReceiverReport = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(),
            CreateSourceDescriptionPacket(true),
            CreateReceiverReportPacket()
        });
        RtcpCompoundPacket secondSenderReport = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(),
            CreateSenderReportPacket(),
            CreateSourceDescriptionPacket(true)
        });

        Assert.Multiple(() =>
        {
            Assert.That(strictCodec.TryEncodeRtcpCompoundPacket(
                missingCanonicalName,
                new byte[128]).Success, Is.False);
            Assert.That(strictCodec.TryEncodeRtcpCompoundPacket(
                lateSourceDescription,
                new byte[128]).Success, Is.False);
            Assert.That(strictCodec.TryEncodeRtcpCompoundPacket(
                lateReceiverReport,
                new byte[128]).Success, Is.False);
            Assert.That(strictCodec.TryEncodeRtcpCompoundPacket(
                secondSenderReport,
                new byte[128]).Success, Is.True);
        });
    }

    [Test]
    public void StrictCodecAllowsGoodbyePacketsOnlyAtTheTail()
    {
        IRtcpPacketCodec strictCodec = CreateCodec(true);
        RtcpCompoundPacket misplacedGoodbye = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(),
            CreateSourceDescriptionPacket(true),
            CreateGoodbyePacket(),
            CreateApplicationPacket()
        });
        RtcpCompoundPacket goodbyeTail = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(),
            CreateSourceDescriptionPacket(true),
            CreateApplicationPacket(),
            CreateGoodbyePacket(),
            CreateGoodbyePacket()
        });

        Assert.Multiple(() =>
        {
            Assert.That(strictCodec.TryEncodeRtcpCompoundPacket(
                misplacedGoodbye,
                new byte[128]).Success, Is.False);
            Assert.That(strictCodec.TryEncodeRtcpCompoundPacket(
                goodbyeTail,
                new byte[128]).Success, Is.True);
        });
    }

    [Test]
    public void NonFinalPaddingFailsAtomicallyAndTolerantModeCanInspectIt()
    {
        IRtcpPacketCodec strictCodec = CreateCodec(true);
        IRtcpPacketCodec tolerantCodec = CreateCodec(false);
        RtcpPacket firstPacket = CreateReceiverReportPacket();
        firstPacket.Header.HasPadding = true;
        firstPacket.Padding = new byte[] { 0, 0, 0, 4 };
        RtcpCompoundPacket compound = CreateCompound(new RtcpPacket[]
        {
            firstPacket,
            CreateSourceDescriptionPacket(true)
        });
        byte[] tolerantWire = tolerantCodec.EncodeRtcpCompoundPacketToArray(compound);
        byte[] destination = new byte[64];
        int destinationIndex = 0;
        while (destinationIndex < destination.Length)
        {
            destination[destinationIndex] = 77;
            destinationIndex = destinationIndex + 1;
        }

        RtcpCodecResult encodeResult = strictCodec.TryEncodeRtcpCompoundPacket(
            compound,
            destination);
        RtcpCodecResult strictDecode = strictCodec.TryDecodeRtcpCompoundPacket(
            tolerantWire);
        RtcpCodecResult tolerantDecode = tolerantCodec.TryDecodeRtcpCompoundPacket(
            tolerantWire);

        Assert.Multiple(() =>
        {
            Assert.That(encodeResult.Success, Is.False);
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.That(strictDecode.Success, Is.False);
            Assert.That(strictDecode.CompoundPacket, Is.Null);
            Assert.That(tolerantDecode.Success, Is.True);
            Assert.That(tolerantDecode.CompoundPacket?.Packets[0].Padding.Length,
                Is.EqualTo(4));
        });
    }

    static RtcpCompoundPacket CreateCompound(RtcpPacket[] packets)
    {
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = packets;
        return compound;
    }

    static RtcpPacket CreateReceiverReportPacket()
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ReceiverReport;
        packet.ReceiverReport = new RtcpReceiverReport();
        packet.ReceiverReport.SenderSynchronizationSource = 1U;
        return packet;
    }

    static RtcpPacket CreateSenderReportPacket()
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.SenderReport;
        packet.SenderReport = new RtcpSenderReport();
        packet.SenderReport.SenderSynchronizationSource = 1U;
        return packet;
    }

    static RtcpPacket CreateSourceDescriptionPacket(bool includeCanonicalName)
    {
        RtcpSourceDescriptionItem item = new RtcpSourceDescriptionItem();
        if (includeCanonicalName)
        {
            item.Type = RtcpSdesItemType.CanonicalName;
        }
        else
        {
            item.Type = RtcpSdesItemType.Name;
        }

        item.Value = new byte[] { 0x78 };
        RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = 1U;
        chunk.Items = new RtcpSourceDescriptionItem[] { item };
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[] { chunk };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.SourceDescription;
        packet.SourceDescription = description;
        return packet;
    }

    static RtcpPacket CreateApplicationPacket()
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ApplicationDefined;
        packet.ApplicationDefined = new RtcpApplicationDefined();
        packet.ApplicationDefined.SourceIdentifier = 1U;
        packet.ApplicationDefined.Name = new byte[] { 0x54, 0x45, 0x53, 0x54 };
        return packet;
    }

    static RtcpPacket CreateGoodbyePacket()
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.Goodbye;
        packet.Goodbye = new RtcpGoodbye();
        packet.Goodbye.Sources = new uint[] { 1U };
        return packet;
    }

    static IRtcpPacketCodec CreateCodec(bool strictValidation)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = strictValidation;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        configuration.MaximumRtcpCompoundPacketCount = 32;
        return new RtcpPacketCodec(configuration);
    }
}
