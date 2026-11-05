using rtp;

namespace rtp.Tests.Wire;

public class RtcpCompoundModeTests
{
    [Test]
    public void MinimalFeedbackModeRoundTripsTheExactAvpfPacketSequence()
    {
        IRtcpPacketCodec codec = CreateCodec(RtcpCompoundMode.MinimalFeedback);
        RtcpCompoundPacket compound = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(7U),
            CreateSourceDescriptionPacket(7U, false),
            CreateFeedbackPacket()
        });

        byte[] encoded = codec.EncodeRtcpCompoundPacketToArray(compound);
        RtcpCodecResult result = codec.TryDecodeRtcpCompoundPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(encoded.Length));
            Assert.That(result.CompoundPacket?.Packets.Length, Is.EqualTo(3));
            Assert.That(result.CompoundPacket?.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.ReceiverReport));
            Assert.That(result.CompoundPacket?.Packets[1].Type,
                Is.EqualTo(RtcpPacketType.SourceDescription));
            Assert.That(result.CompoundPacket?.Packets[2].Type,
                Is.EqualTo(RtcpPacketType.PayloadFeedback));
        });
    }

    [Test]
    public void MinimalFeedbackModeRejectsNonMinimalContentAndWrongCnameSource()
    {
        IRtcpPacketCodec codec = CreateCodec(RtcpCompoundMode.MinimalFeedback);
        RtcpCompoundPacket optionalSdesItem = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(7U),
            CreateSourceDescriptionPacket(7U, true),
            CreateFeedbackPacket()
        });
        RtcpCompoundPacket wrongCnameSource = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(7U),
            CreateSourceDescriptionPacket(8U, false),
            CreateFeedbackPacket()
        });
        RtcpCompoundPacket nonFeedbackTail = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(7U),
            CreateSourceDescriptionPacket(7U, false),
            CreateApplicationPacket()
        });
        RtcpCompoundPacket missingFeedback = CreateCompound(new RtcpPacket[]
        {
            CreateReceiverReportPacket(7U),
            CreateSourceDescriptionPacket(7U, false)
        });

        Assert.Multiple(() =>
        {
            Assert.That(TryEncode(codec, optionalSdesItem).Success, Is.False);
            Assert.That(TryEncode(codec, wrongCnameSource).Success, Is.False);
            Assert.That(TryEncode(codec, nonFeedbackTail).Success, Is.False);
            Assert.That(TryEncode(codec, missingFeedback).Success, Is.False);
        });
    }

    [Test]
    public void ReducedSizeModeAcceptsSinglePacketsWithoutWeakeningFullMode()
    {
        IRtcpPacketCodec reducedCodec = CreateCodec(RtcpCompoundMode.ReducedSize);
        IRtcpPacketCodec fullCodec = CreateCodec(RtcpCompoundMode.Full);
        RtcpCompoundPacket reducedPacket = CreateCompound(new RtcpPacket[]
        {
            CreateFeedbackPacket()
        });

        byte[] encoded = reducedCodec.EncodeRtcpCompoundPacketToArray(reducedPacket);
        RtcpCodecResult reducedDecode = reducedCodec.TryDecodeRtcpCompoundPacket(encoded);
        RtcpCodecResult fullDecode = fullCodec.TryDecodeRtcpCompoundPacket(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(reducedDecode.Success, Is.True);
            Assert.That(reducedDecode.CompoundPacket?.Packets.Length, Is.EqualTo(1));
            Assert.That(reducedDecode.CompoundPacket?.Packets[0].Type,
                Is.EqualTo(RtcpPacketType.PayloadFeedback));
            Assert.That(fullDecode.Success, Is.False);
            Assert.That(TryEncode(fullCodec, reducedPacket).Success, Is.False);
        });
    }

    [Test]
    public void EveryStrictModeRejectsPaddingBeforeTheFinalPacket()
    {
        RtcpPacket paddedReport = CreateReceiverReportPacket(7U);
        paddedReport.Header.HasPadding = true;
        paddedReport.Padding = new byte[] { 0, 0, 0, 4 };
        RtcpCompoundPacket compound = CreateCompound(new RtcpPacket[]
        {
            paddedReport,
            CreateFeedbackPacket()
        });

        Assert.Multiple(() =>
        {
            Assert.That(TryEncode(
                CreateCodec(RtcpCompoundMode.Full),
                compound).Success, Is.False);
            Assert.That(TryEncode(
                CreateCodec(RtcpCompoundMode.MinimalFeedback),
                compound).Success, Is.False);
            Assert.That(TryEncode(
                CreateCodec(RtcpCompoundMode.ReducedSize),
                compound).Success, Is.False);
        });
    }

    [Test]
    public void CodecSnapshotsAndValidatesTheConfiguredCompoundMode()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(
            RtcpCompoundMode.MinimalFeedback);
        IRtcpPacketCodec codec = new RtcpPacketCodec(configuration);
        configuration.RtcpCompoundMode = RtcpCompoundMode.ReducedSize;
        RtpSessionConfiguration invalidConfiguration = CreateConfiguration(
            (RtcpCompoundMode)99);
        RtcpCompoundPacket reducedPacket = CreateCompound(new RtcpPacket[]
        {
            CreateFeedbackPacket()
        });

        Assert.Multiple(() =>
        {
            Assert.That(TryEncode(codec, reducedPacket).Success, Is.False);
            Assert.Throws<RtpConfigurationException>(() =>
                new RtcpPacketCodec(invalidConfiguration));
            Assert.That((int)RtcpCompoundMode.Full, Is.EqualTo(0));
            Assert.That((int)RtcpCompoundMode.MinimalFeedback, Is.EqualTo(1));
            Assert.That((int)RtcpCompoundMode.ReducedSize, Is.EqualTo(2));
        });
    }

    static RtcpCodecResult TryEncode(
        IRtcpPacketCodec codec,
        RtcpCompoundPacket compoundPacket)
    {
        return codec.TryEncodeRtcpCompoundPacket(compoundPacket, new byte[256]);
    }

    static RtcpCompoundPacket CreateCompound(RtcpPacket[] packets)
    {
        RtcpCompoundPacket compound = new RtcpCompoundPacket();
        compound.Packets = packets;
        return compound;
    }

    static RtcpPacket CreateReceiverReportPacket(uint sourceIdentifier)
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ReceiverReport;
        packet.ReceiverReport = new RtcpReceiverReport();
        packet.ReceiverReport.SenderSynchronizationSource = sourceIdentifier;
        return packet;
    }

    static RtcpPacket CreateSourceDescriptionPacket(
        uint sourceIdentifier,
        bool includeOptionalItem)
    {
        RtcpSourceDescriptionItem canonicalName = new RtcpSourceDescriptionItem();
        canonicalName.Type = RtcpSdesItemType.CanonicalName;
        canonicalName.Value = new byte[] { 0x78 };
        RtcpSourceDescriptionChunk chunk = new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = sourceIdentifier;
        if (includeOptionalItem)
        {
            RtcpSourceDescriptionItem name = new RtcpSourceDescriptionItem();
            name.Type = RtcpSdesItemType.Name;
            name.Value = new byte[] { 0x79 };
            chunk.Items = new RtcpSourceDescriptionItem[] { canonicalName, name };
        }
        else
        {
            chunk.Items = new RtcpSourceDescriptionItem[] { canonicalName };
        }

        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[] { chunk };
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.SourceDescription;
        packet.SourceDescription = description;
        return packet;
    }

    static RtcpPacket CreateFeedbackPacket()
    {
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.Format = 1;
        feedback.PayloadFormat = RtcpPayloadFeedbackFormat.PictureLossIndication;
        feedback.SenderSourceIdentifier = 1U;
        feedback.MediaSourceIdentifier = 1U;
        feedback.PictureLossIndication = new RtcpPictureLossIndication();
        feedback.PictureLossIndication.Requested = true;
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.PayloadFeedback;
        packet.Feedback = feedback;
        return packet;
    }

    static RtcpPacket CreateApplicationPacket()
    {
        RtcpPacket packet = new RtcpPacket();
        packet.Type = RtcpPacketType.ApplicationDefined;
        packet.ApplicationDefined = new RtcpApplicationDefined();
        packet.ApplicationDefined.SourceIdentifier = 7U;
        packet.ApplicationDefined.Name = new byte[] { 0x54, 0x45, 0x53, 0x54 };
        return packet;
    }

    static IRtcpPacketCodec CreateCodec(RtcpCompoundMode compoundMode)
    {
        return new RtcpPacketCodec(CreateConfiguration(compoundMode));
    }

    static RtpSessionConfiguration CreateConfiguration(RtcpCompoundMode compoundMode)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.RtcpCompoundMode = compoundMode;
        configuration.MaximumRtcpCompoundPacketSize = 65535;
        configuration.MaximumRtcpCompoundPacketCount = 32;
        return configuration;
    }
}
