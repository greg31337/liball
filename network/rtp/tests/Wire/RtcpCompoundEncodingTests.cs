using rtp;

namespace rtp.Tests.Wire;

public class RtcpCompoundEncodingTests
{
    [Test]
    public void CompoundSizingAndBothSpanEncodersUseTheExactAggregateLength()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpCompoundPacket compound = CreateCompound(false);
        byte[] exactDestination = new byte[40];
        byte[] largerDestination = new byte[44];
        int index = 0;
        while (index < largerDestination.Length)
        {
            largerDestination[index] = 77;
            index = index + 1;
        }

        int encodedSize = codec.GetRtcpCompoundPacketEncodedSize(compound);
        int written = codec.EncodeRtcpCompoundPacket(compound, exactDestination);
        RtcpCodecResult tryResult = codec.TryEncodeRtcpCompoundPacket(
            compound,
            largerDestination);
        byte[] allocated = codec.EncodeRtcpCompoundPacketToArray(compound);

        Assert.Multiple(() =>
        {
            Assert.That(encodedSize, Is.EqualTo(40));
            Assert.That(written, Is.EqualTo(encodedSize));
            Assert.That(tryResult.Success, Is.True);
            Assert.That(tryResult.WrittenCount, Is.EqualTo(encodedSize));
            Assert.That(largerDestination[..encodedSize], Is.EqualTo(exactDestination));
            Assert.That(largerDestination[encodedSize..], Has.All.EqualTo(77));
            Assert.That(allocated.Length, Is.EqualTo(encodedSize));
            Assert.That(allocated, Is.EqualTo(exactDestination));
            Assert.That(exactDestination[2..4], Is.EqualTo(new byte[] { 0, 1 }));
            Assert.That(exactDestination[10..12], Is.EqualTo(new byte[] { 0, 2 }));
            Assert.That(exactDestination[22..24], Is.EqualTo(new byte[] { 0, 4 }));
        });
    }

    [Test]
    public void CapacityFailureIsAtomicForThrowingAndExceptionLessEncoding()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpCompoundPacket compound = CreateCompound(false);
        byte[] destination = CreateFilledDestination(39);

        RtcpCodecResult result = codec.TryEncodeRtcpCompoundPacket(
            compound,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.ErrorMessage, Does.Contain("too small"));
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.Throws<RtpBufferCapacityException>(() =>
                codec.EncodeRtcpCompoundPacket(compound, destination));
            Assert.That(destination, Has.All.EqualTo(77));
        });
    }

    [Test]
    public void ValidationFailureInALaterPacketIsAtomicForBothEncodingForms()
    {
        IRtcpPacketCodec codec = CreateCodec();
        RtcpCompoundPacket compound = CreateCompound(true);
        byte[] destination = CreateFilledDestination(64);

        RtcpCodecResult result = codec.TryEncodeRtcpCompoundPacket(
            compound,
            destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(result.ErrorMessage, Does.Contain("exactly four bytes"));
            Assert.That(destination, Has.All.EqualTo(77));
            Assert.Throws<RtcpPacketFormatException>(() =>
                codec.EncodeRtcpCompoundPacket(compound, destination));
            Assert.That(destination, Has.All.EqualTo(77));
        });
    }

    static byte[] CreateFilledDestination(int size)
    {
        byte[] destination = new byte[size];
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 77;
            index = index + 1;
        }

        return destination;
    }

    static RtcpCompoundPacket CreateCompound(bool invalidApplicationName)
    {
        RtcpPacket receiverReport = new RtcpPacket();
        receiverReport.Header.Count = 31;
        receiverReport.Header.PacketType = 1;
        receiverReport.Header.LengthInWordsMinusOne = 99;
        receiverReport.Type = RtcpPacketType.ReceiverReport;
        receiverReport.ReceiverReport = new RtcpReceiverReport();
        receiverReport.ReceiverReport.SenderSynchronizationSource = 1U;

        RtcpSourceDescriptionItem canonicalName = new RtcpSourceDescriptionItem();
        canonicalName.Type = RtcpSdesItemType.CanonicalName;
        canonicalName.Value = new byte[] { 0x78 };
        RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = 1U;
        chunk.Items = new RtcpSourceDescriptionItem[] { canonicalName };
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[] { chunk };
        RtcpPacket sourceDescription = new RtcpPacket();
        sourceDescription.Header.Count = 31;
        sourceDescription.Header.PacketType = 1;
        sourceDescription.Header.LengthInWordsMinusOne = 99;
        sourceDescription.Type = RtcpPacketType.SourceDescription;
        sourceDescription.SourceDescription = description;

        RtcpApplicationDefined application = new RtcpApplicationDefined();
        application.Subtype = 5;
        application.SourceIdentifier = 1U;
        if (invalidApplicationName)
        {
            application.Name = new byte[] { 0x41, 0x50, 0x50 };
        }
        else
        {
            application.Name = new byte[] { 0x41, 0x50, 0x50, 0x31 };
        }

        application.Data = new byte[] { 1, 2, 3, 4 };
        RtcpPacket applicationPacket = new RtcpPacket();
        applicationPacket.Header.HasPadding = true;
        applicationPacket.Header.Count = 31;
        applicationPacket.Header.PacketType = 1;
        applicationPacket.Header.LengthInWordsMinusOne = 99;
        applicationPacket.Type = RtcpPacketType.ApplicationDefined;
        applicationPacket.ApplicationDefined = application;
        applicationPacket.Padding = new byte[] { 9, 8, 7, 4 };

        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = new RtcpPacket[]
        {
            receiverReport,
            sourceDescription,
            applicationPacket
        };
        return compound;
    }

    static IRtcpPacketCodec CreateCodec()
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.StrictValidation = true;
        configuration.PreservePadding = true;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        configuration.MaximumRtcpCompoundPacketCount = 32;
        return new RtcpPacketCodec(configuration);
    }
}
